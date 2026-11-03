// Isolated Editor audit, outside Assets. Never changes a saved configuration or preferences.
// Pipeline: script run --file Docs/Setup/Task15Fairness.cs --entry NeonTunnelRunner.Task15Fairness.Run -- --timeout 600
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NeonTunnelRunner
{
    public static class Task15Fairness
    {
        private const string Output = "Evidence/Task15";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static int seed, regime;
        private static long block;
        private static string slot, configHash;
        private static readonly Func<TrafficCandidate, bool> ProposesMoving =
            (Func<TrafficCandidate, bool>)Delegate.CreateDelegate(typeof(Func<TrafficCandidate, bool>),
                typeof(SpawnDirector).GetMethod("ProposesMoving", BindingFlags.Static | BindingFlags.NonPublic));

        private static void Check(bool condition, string reason)
        {
            if (!condition) throw new InvalidOperationException(reason);
        }
        private static string Hash(string file)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file))).Replace("-", "").ToLowerInvariant();
        }
        private static void Invoke(SpawnDirector planner, string method, params object[] args) =>
            typeof(SpawnDirector).GetMethod(method, Private).Invoke(planner, args);

        public static object Run()
        {
            Check(!EditorApplication.isPlaying, "Stop Play Mode before isolated fairness audit.");
            Directory.CreateDirectory(Output);
            var world = AssetDatabase.LoadAssetAtPath<WorldConfig>("Assets/NeonTunnelRunner/Configs/World.asset");
            var gameplay = AssetDatabase.LoadAssetAtPath<GameplayConfig>("Assets/NeonTunnelRunner/Configs/Gameplay.asset");
            Check(world && gameplay, "Missing actual configuration assets.");
            Check(world.TryValidate(gameplay, out var configurationError), configurationError);
            configHash = Hash(AssetDatabase.GetAssetPath(world)) + ":" + Hash(AssetDatabase.GetAssetPath(gameplay));
            var root = new GameObject("Task15IsolatedFairness");
            var planner = root.AddComponent<SpawnDirector>();
            typeof(SpawnDirector).GetField("configuration", Private).SetValue(planner, world);
            typeof(SpawnDirector).GetField("gameplay", Private).SetValue(planner, gameplay);
            long moving = 0, stationary = 0, omittedTraffic = 0, acceptedCoins = 0, omittedCoins = 0, fuel = 0;
            int maxPlans = 0;
            double maxGap = 0;
            var proposals = new long[3]; var eligible = new long[3];
            var rejectionCounts = new Dictionary<string, long>();
            Action<PatternRejection> reject = reason => {
                var name = reason.ToString(); rejectionCounts.TryGetValue(name, out long n); rejectionCounts[name] = n + 1;
            };
            using (var log = new StreamWriter(Output + "/fairness-progress.jsonl", false))
            {
                try
                {
                    // Actual immutable configuration: row density commits by absolute row address.
                    // Windows begin at distance difficulty 0/.5/1; do not pretend the same
                    // 0..10km plan changes when a different runtime speed variable is declared.
                    foreach (int context in new[] { 0, 1, 2 })
                    {
                        regime = context;
                        double start = context * 3000d, end = start + 10000d;
                        float difficulty = ProgressionSystem.EvaluateDifficulty(0d, start, gameplay);
                        Check(Math.Abs(difficulty - context * .5f) < .00001f, "Unexpected difficulty context.");
                        for (seed = 0; seed < 1000; seed++)
                        {
                            Invoke(planner, "ResetRun", seed);
                            long first = (long)Math.Floor(start / 480d), last = (long)Math.Floor(end / 480d);
                            long sequence = -1;
                            for (block = first; block <= last; block++)
                            {
                                slot = "block";
                                var plan = planner.PlanAt(block);
                                Check(plan.Equals(planner.PlanAt(block)), "Non-deterministic sampling.");
                                Check(PatternValidator.Validate(plan, world, gameplay, out var why), "Unsafe block: " + why);
                                Check(plan.NextProtectedLane == planner.PlanAt(block + 1).ProtectedLane, "Discontinuous route.");
                                Check(plan.AbsoluteEnd - plan.BufferStart == 120d, "Transition span changed.");
                                Invoke(planner, "FillWindow", Math.Max(start, plan.AbsoluteStart));
                                maxPlans = Math.Max(maxPlans, planner.PlanCount);
                                Check(planner.PlanCount >= 2 && planner.PlanCount <= 4, "Unbounded planning storage.");
                                Check(plan.Equals(planner.PlanAt(block)), "Window commitment changed row sample.");
                                for (int row = 0; row < 6; row++)
                                for (int lane = -1; lane <= 1; lane++)
                                {
                                    slot = "traffic/" + row + "/" + lane;
                                    double z = plan.RowPosition(row);
                                    bool inWindow = z >= start && z < end;
                                    if (inWindow && z < start + 1000d && z >= 180d && lane != plan.ProtectedLane)
                                        eligible[context]++;
                                    if (!plan.TryGetCandidate(row, lane, out var candidate)) continue;
                                    if (inWindow && z < start + 1000d) proposals[context]++;
                                    Check(candidate.SpawnSequence > sequence, "Unstable sequence ordering.");
                                    sequence = candidate.SpawnSequence;
                                    Check(z >= 180d && lane != plan.ProtectedLane, "Warmup/protected lane occupied.");
                                    double playerZ = Math.Max(0d, z - world.Ahead);
                                    if (!planner.TrySelectTraffic(candidate, playerZ, out var key, out why))
                                    { omittedTraffic++; reject(why); continue; }
                                    if (why != PatternRejection.None) reject(why); // moving -> stationary fallback
                                    float velocity = key == PoolKey.Moving ? 8f : 0f;
                                    if (velocity > 0) moving++; else stationary++;
                                    // Independently check the full swept envelope through conservative despawn.
                                    double horizon = (z - playerZ + world.Behind + 2.2d) / (gameplay.StartingSpeed - 8d);
                                    double padding = 2.2d + gameplay.PlayerHalfExtents.z;
                                    double low = z - padding, high = z + velocity * horizon + padding;
                                    for (long reached = (long)Math.Floor(low / 480d); reached <= (long)Math.Floor(high / 480d); reached++)
                                    {
                                        var reserved = planner.PlanAt(reached);
                                        Check(high < reserved.BufferStart || low > reserved.AbsoluteEnd, "Envelope enters clear transition.");
                                        Check(lane != reserved.ProtectedLane || high < reserved.AbsoluteStart || low > reserved.ProtectedEnd,
                                            "Envelope enters protected corridor.");
                                    }
                                }
                                for (int pickupSlot = 0; pickupSlot < 11; pickupSlot++)
                                {
                                    slot = "pickup/" + pickupSlot;
                                    var pickup = pickupSlot < 3 ? plan.GetFuel(pickupSlot) : plan.GetCoin(pickupSlot - 3);
                                    bool accepted = PatternValidator.ValidatePickup(planner, pickup, out why);
                                    if (!pickup.Required && !accepted) { omittedCoins++; reject(why); continue; }
                                    Check(accepted, "Required fuel rejected: " + why);
                                    if (pickup.Required)
                                    {
                                        fuel++;
                                        long index = block * 3 + pickupSlot;
                                        var next = planner.FuelAt(index + 1);
                                        double gap = next.AbsolutePosition - pickup.AbsolutePosition;
                                        maxGap = Math.Max(maxGap, gap);
                                        Check(gap > 0d && gap <= 180d, "Fuel opportunity gap exceeds 180m.");
                                        Check(PatternValidator.ValidateFuelLink(planner, pickup, next), "Unreachable sequential fuel.");
                                    }
                                    else acceptedCoins++;
                                    // Independent overlap check for every possibly accepted preceding moving car.
                                    double maxTravel = 8d * (world.Ahead + world.Behind + 2.2d) / (gameplay.StartingSpeed - 8d);
                                    const double overlapPadding = 2.7d;
                                    long from = Math.Max(0L, (long)Math.Floor((pickup.AbsolutePosition - maxTravel - overlapPadding) / 480d));
                                    long to = (long)Math.Floor((pickup.AbsolutePosition + overlapPadding) / 480d);
                                    for (long other = from; other <= to; other++)
                                    {
                                        var traffic = planner.PlanAt(other);
                                        for (int row = 0; row < 6; row++)
                                        {
                                            if (!traffic.TryGetCandidate(row, pickup.Lane, out var candidate)) continue;
                                            if (!PatternValidator.ValidateMovingEnvelope(planner, pickup.Lane, candidate.AbsolutePosition,
                                                candidate.AbsolutePosition, 0f, new Vector3(1.1f,.6f,2.2f), out _)) continue;
                                            bool proposesMoving = ProposesMoving(candidate);
                                            double endZ = proposesMoving ? Math.Min(candidate.AbsolutePosition + maxTravel,
                                                traffic.BufferStart - (2.2d + gameplay.PlayerHalfExtents.z)) : candidate.AbsolutePosition;
                                            Check(pickup.AbsolutePosition + overlapPadding < candidate.AbsolutePosition ||
                                                pickup.AbsolutePosition - overlapPadding > endZ, "Accepted pickup overlaps traffic envelope.");
                                        }
                                    }
                                }
                            }
                            if (seed % 100 == 0)
                            {
                                log.WriteLine(JsonUtility.ToJson(new Progress { timestamp = DateTimeOffset.Now.ToString("o"),
                                    seed = seed, context = context, block = block, configHash = configHash }));
                                log.Flush();
                            }
                        }
                    }
                    for (int from = -1; from <= 1; from++) for (int to = -1; to <= 1; to++)
                        Check(PatternValidator.CanTransition(from, to, world, gameplay), "Unsafe lane combination.");
                    Invoke(planner, "ResetRun", 12345);
                    var extent = new Vector3(1.1f,.6f,2.2f);
                    int safeLane = planner.PlanAt(1).ProtectedLane;
                    Check(!PatternValidator.ValidateMovingEnvelope(planner, safeLane, 540d, 240d, 8f, extent, out _), "Protected moving proposal accepted.");
                    Check(!PatternValidator.ValidateMovingEnvelope(planner, safeLane == 0 ? 1 : 0, 780d, 480d, 8f, extent, out _), "Transition moving proposal accepted.");
                    Check(moving > 1000 && stationary > 1000 && omittedTraffic > 0, "Degenerate/empty safety evidence.");
                    double[] rates = eligible.Select((n,i) => proposals[i] / (double)n).ToArray();
                    Check(rates[0] < rates[1] && rates[1] < rates[2], "Statistical density progression failed.");
                    return new { passed = true, timestamp = DateTimeOffset.Now.ToString("o"), configHash,
                        seedRange = "0..999 inclusive", metresPerWindow = 10000, windowStarts = new[] { 0, 3000, 6000 },
                        method = "Actual immutable row-distance schedule; no runtime difficulty override or reseeding",
                        moving, stationary, omittedTraffic, acceptedCoins, omittedCoins, fuel, maxGap, maxPlans,
                        firstKilometreProposals = proposals, firstKilometreEligible = eligible, rates, rejectionCounts,
                        transitions = 9, unsafeMovingProposalsRejected = 2 };
                }
                catch (Exception error)
                {
                    string replay = "/home/alireza/.local/bin/unity.previous command --caller plugin --skill unity-cli --project-path . --format json script run --file Docs/Setup/Task15Fairness.cs --entry NeonTunnelRunner.Task15Fairness.Run -- --timeout 600";
                    File.WriteAllText(Output + "/fairness-failure.json", JsonUtility.ToJson(new Failure {
                        timestamp = DateTimeOffset.Now.ToString("o"), seed = seed, context = regime, block = block, slot = slot,
                        configHash = configHash, reason = error.ToString(), replay = replay,
                        contextDifficulty = regime * .5f,
                        contextSpeed = Mathf.Lerp(gameplay.StartingSpeed, gameplay.MaximumSpeed, regime * .5f),
                        rowDifficulty = ProgressionSystem.EvaluateDifficulty(0d, block * 480d, gameplay) }, true));
                    throw;
                }
                finally { Object.DestroyImmediate(root); }
            }
        }
        [Serializable] private class Progress { public string timestamp, configHash; public int seed, context; public long block; }
        [Serializable] private sealed class Failure { public string timestamp, configHash, slot, reason, replay; public int seed, context; public long block; public float contextDifficulty, contextSpeed, rowDifficulty; }
    }
}
