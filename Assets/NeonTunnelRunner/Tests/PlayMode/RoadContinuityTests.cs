using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class RoadContinuityTests
    {
        private GameObject root;
        private GameplayConfig config;
        private GameManager manager;
        private GameplayLoop loop;
        private RoadManager roads;
        [SetUp] public void Setup()
        {
            config = ScriptableObject.CreateInstance<GameplayConfig>();
            root = PlayModeMovementRig.Create("EndlessWorldFixture", config);
            manager = root.GetComponent<GameManager>(); loop = manager.Loop; roads = loop.Roads;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (manager) manager.Shutdown();
            if (root) Object.Destroy(root);
            if (config) Object.Destroy(config);
            Time.timeScale = 1f;
            yield return null;
            Assert.That(GameManager.Instance, Is.Null);
        }
        private void CheckCoverage(double distance)
        {
            Assert.That(roads.CoverageStart, Is.LessThanOrEqualTo(distance - 60d));
            Assert.That(roads.CoverageEnd, Is.GreaterThanOrEqualTo(distance + 300d));
            Assert.That(roads.ActiveCount, Is.InRange(6, 7));
            for (int i = 0; i < roads.ActiveCount; i++)
            {
                var lease = roads.GetLease(i); var segment = lease.Segment;
                Assert.That(lease.IsCurrent, Is.True);
                Assert.That(segment.Key, Is.EqualTo(RoadManager.KeyAt(segment.SegmentIndex)));
                Assert.That(segment.Context.SpawnSequence, Is.EqualTo(segment.SegmentIndex));
                Assert.That(segment.Context.AbsoluteDistance, Is.EqualTo(segment.AbsoluteStart));
                Assert.That(segment.transform.localPosition.z, Is.EqualTo((float)(segment.AbsoluteStart - loop.Origin.Offset)));
                Assert.That(segment.transform.rotation, Is.EqualTo(Quaternion.identity));
                Assert.That(segment.transform.localScale, Is.EqualTo(Vector3.one));
                if (i > 0)
                {
                    Assert.That(segment.SegmentIndex, Is.EqualTo(roads.GetLease(i-1).Segment.SegmentIndex + 1));
                    Assert.That(Vector3.Distance(roads.GetLease(i-1).Segment.Exit.position, segment.Entry.position), Is.LessThan(0.001f));
                }
            }
        }
        [TestCase(22f)] [TestCase(50f)]
        public void HundredKilometresMaintainsSeamsCoverageAndBoundedPools(float speed)
        {
            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            bool started = manager.StartRun();
            long bootstrap = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
            Assert.That(started, Is.True);
            manager.Session.SetCruiseSpeed(speed);
            CheckCoverage(0d);
            float[] steps = { 0.02f, 0.137f, 1.2f, 9.73f, 31.25f };
            int iteration = 0, maxActive = 0, maxPlans = 0;
            double previous = 0d;
            while (manager.Session.Snapshot.Distance < 100000d)
            {
                float dt = steps[iteration++ % steps.Length];
                Assert.That(loop.Step(dt), Is.True);
                var snapshot = manager.Session.Snapshot;
                Assert.That(snapshot.Distance, Is.EqualTo(previous + (double)speed * dt));
                Assert.That(loop.Player.CurrentPosition.z, Is.EqualTo((float)(snapshot.Distance - loop.Origin.Offset)));
                Assert.That(loop.Player.CurrentPosition.z, Is.InRange(0f, 1000f));
                CheckCoverage(snapshot.Distance);
                Assert.That(loop.Planner.PlanCount, Is.InRange(2,4));
                maxActive = Math.Max(maxActive, roads.ActiveCount); maxPlans = Math.Max(maxPlans, loop.Planner.PlanCount);
                previous = snapshot.Distance;
            }
            Assert.That(loop.Origin.RebaseCount, Is.GreaterThan(40));
            Assert.That(manager.Pools.TotalCreated, Is.EqualTo(24));
            Assert.That(manager.Pools.TotalDestroyed, Is.Zero);
            Assert.That(manager.Pools.PeakActive, Is.LessThanOrEqualTo(14));
            Assert.That(manager.Pools.Misses, Is.Zero);
            Debug.Log($"[Task05] speed={speed}; distance={previous:R}; steps={iteration}; rebases={loop.Origin.RebaseCount}; maxActive={maxActive}; maxPlans={maxPlans}; created={manager.Pools.TotalCreated}; peakReservations={manager.Pools.PeakActive}; initial reset/coverage sample={bootstrap} bytes (excludes prewarm).");
        }
        [TestCase(0d)] [TestCase(0.25d)] [TestCase(59.999d)] [TestCase(60d)] [TestCase(60.001d)]
        [TestCase(480d)] [TestCase(17117.125d)]
        public void ExactFractionalAndSkippedBoundariesEstablishCompleteWindow(double distance)
        {
            Assert.That(manager.StartRun(), Is.True);
            Assert.That(roads.EnsureCoverage(distance, 0d), Is.True, roads.LastError);
            CheckCoverage(distance);
            if (distance == 0d) Assert.That(roads.GetLease(0).Segment.SegmentIndex, Is.EqualTo(-1));
        }
        [Test] public void LargeAbsoluteIndicesConvertAfterDoubleSubtraction()
        {
            Assert.That(manager.StartRun(), Is.True);
            const long index = 1000000000000L;
            double offset = index * 60d, distance = offset + 0.125d;
            Assert.That(roads.EnsureCoverage(distance, offset), Is.True);
            Assert.That(roads.CoverageStart, Is.LessThanOrEqualTo(distance-60d));
            Assert.That(roads.CoverageEnd, Is.GreaterThanOrEqualTo(distance+300d));
            for (int i=0;i<roads.ActiveCount;i++)
            {
                var segment=roads.GetLease(i).Segment;
                Assert.That(segment.transform.localPosition.z, Is.EqualTo((float)((segment.SegmentIndex-index)*60d)));
                Assert.That(segment.SegmentIndex, Is.GreaterThanOrEqualTo(index-1));
                if (i>0) Assert.That(Vector3.Distance(roads.GetLease(i-1).Segment.Exit.position, segment.Entry.position), Is.LessThan(0.001f));
            }
        }
        [Test] public void UnrepresentableAbsoluteWindowFailsWithoutDiscardingExistingCoverage()
        {
            Assert.That(manager.StartRun(), Is.True);
            ulong rents=manager.Pools.TotalRents, returns=manager.Pools.TotalReturns;
            // At this magnitude double cannot distinguish the 360m window; reject it explicitly.
            Assert.That(roads.EnsureCoverage(1e20d, 1e20d), Is.False);
            Assert.That(roads.LastError, Is.Not.Empty);
            Assert.That(manager.Pools.TotalRents, Is.EqualTo(rents));
            Assert.That(manager.Pools.TotalReturns, Is.EqualTo(returns));
            CheckCoverage(0d);
        }
        [Test] public void EveryScheduledPrefabCombinationHasAlignedSurfaceAndArchitecture()
        {
            Assert.That(manager.StartRun(), Is.True);
            var combinations = new HashSet<string>();
            for (int index = 0; index < 24; index++)
            {
                Assert.That(roads.EnsureCoverage(index * 60d + 0.5d, 0d), Is.True);
                CheckCoverage(index * 60d + 0.5d);
                for (int i = 1; i < roads.ActiveCount; i++)
                    combinations.Add(roads.GetLease(i-1).Segment.Key + ":" + roads.GetLease(i).Segment.Key);
                for (int i = 0; i < roads.ActiveCount; i++)
                {
                    var segment = roads.GetLease(i).Segment;
                    var surface = segment.transform.Find("Surface");
                    Assert.That(surface.localPosition, Is.EqualTo(new Vector3(0f,-0.1f,30f)));
                    Assert.That(surface.localScale, Is.EqualTo(new Vector3(9.6f,0.2f,60f)));
                    Assert.That(segment.GetComponentsInChildren<Collider>().Length, Is.EqualTo(1));
                }
            }
            CollectionAssert.AreEquivalent(new[] { "RoadOpen:RoadOpen", "RoadOpen:RoadEntrance", "RoadEntrance:RoadTunnel",
                "RoadTunnel:RoadTunnel", "RoadTunnel:RoadExit", "RoadExit:RoadOpen" }, combinations);
        }
        [Test] public void RebaseTeleportsBodyBothLogicalHistoriesAndCameraOnceWithoutChangingPlans()
        {
            Assert.That(manager.StartRun(), Is.True);
            loop.Step(45f); // 990m, immediately before threshold.
            loop.Player.RequestLane(1);
            var player = loop.Player;
            double travel = player.PredictStep(0.25f, 50f);
            manager.Session.RecordMovement(0.25f, travel);
            player.SetAbsoluteForward(manager.Session.Snapshot.Distance, loop.Origin.Offset);
            player.HoldPose(); // Complete the ordinary pose commit before testing the end-of-tick phase.
            Vector3 previous = player.PreviousPosition, current = player.CurrentPosition;
            Vector3 camera = loop.Origin.ChaseCamera.position;
            Vector3 cameraOffset = camera - player.transform.position;
            var snapshot = manager.Session.Snapshot; int lane = player.LaneIndex; float progress = player.TransitionProgress;
            var block = loop.Planner.PlanAt(2);
            var indices = new long[roads.ActiveCount]; var positions = new Vector3[roads.ActiveCount];
            for (int i=0;i<indices.Length;i++) { indices[i]=roads.GetLease(i).Segment.SegmentIndex; positions[i]=roads.GetLease(i).Segment.Entry.position; }
            var inactive = manager.Pools.StorageRoot.GetComponentsInChildren<RoadSegment>(true);
            var inactivePositions = Array.ConvertAll(inactive, s => s.transform.position);
            Assert.That(loop.Origin.RebaseIfNeeded(snapshot.Distance), Is.True);
            float delta = (float)loop.Origin.Offset;
            Assert.That(delta % 60f, Is.Zero);
            Assert.That(manager.Session.Snapshot, Is.EqualTo(snapshot));
            Assert.That(player.CurrentPosition, Is.EqualTo(current - Vector3.forward * delta));
            Assert.That(player.PreviousPosition, Is.EqualTo(previous - Vector3.forward * delta));
            Assert.That(player.Body.position, Is.EqualTo(player.CurrentPosition));
            Assert.That(player.transform.position, Is.EqualTo(player.CurrentPosition));
            Assert.That(loop.Origin.ChaseCamera.position, Is.EqualTo(camera - Vector3.forward * delta));
            Assert.That(loop.Origin.ChaseCamera.position - player.transform.position, Is.EqualTo(cameraOffset));
            Assert.That(player.LaneIndex, Is.EqualTo(lane)); Assert.That(player.TransitionProgress, Is.EqualTo(progress));
            Assert.That(loop.Planner.PlanAt(2), Is.EqualTo(block));
            for (int i=0;i<indices.Length;i++)
            {
                Assert.That(roads.GetLease(i).Segment.SegmentIndex, Is.EqualTo(indices[i]));
                Assert.That(roads.GetLease(i).Segment.Entry.position, Is.EqualTo(positions[i] - Vector3.forward * delta));
            }
            for (int i=0;i<inactive.Length;i++) Assert.That(inactive[i].transform.position, Is.EqualTo(inactivePositions[i]));
            CheckCoverage(snapshot.Distance);
        }
        [Test] public void FiftyRestartsAfterRebasesRetainNoLeasesPlansOrOffsets()
        {
            Assert.That(manager.StartRun(), Is.True);
            var initial = loop.Planner.PlanAt(2);
            for (int cycle=0;cycle<50;cycle++)
            {
                loop.Step(120f); loop.Step(100f);
                Assert.That(loop.Origin.RebaseCount, Is.GreaterThanOrEqualTo(2));
                var old = roads.GetLease(0); int count = roads.ActiveCount;
                if (cycle%2==0)
                {
                    manager.Pause(); var origin = loop.Origin.Offset; var plan = loop.Planner.GetPlan(0);
                    Assert.That(loop.Step(20f), Is.False);
                    Assert.That(roads.GetLease(0).ActivationId, Is.EqualTo(old.ActivationId));
                    Assert.That(loop.Origin.Offset, Is.EqualTo(origin)); Assert.That(loop.Planner.GetPlan(0), Is.EqualTo(plan));
                }
                else manager.EndRun(GameOverReason.Lives);
                ulong returns = manager.Pools.TotalReturns;
                Assert.That(manager.Restart(), Is.True);
                Assert.That(manager.Pools.TotalReturns, Is.EqualTo(returns + (ulong)count));
                Assert.That(manager.Pools.Return(old.Segment, old.ActivationId), Is.False);
                Assert.That(loop.Origin.Offset, Is.Zero); Assert.That(loop.Origin.RebaseCount, Is.Zero);
                Assert.That(loop.Player.CurrentPosition, Is.EqualTo(config.StartingPosition));
                Assert.That(loop.Player.PreviousPosition, Is.EqualTo(config.StartingPosition));
                Assert.That(loop.Planner.PlanAt(2), Is.EqualTo(initial));
                Assert.That(manager.Session.Snapshot.Distance, Is.Zero);
                CheckCoverage(0d);
            }
            manager.Pause(); Assert.That(manager.ReturnToMenu(), Is.True);
            Assert.That(roads.ActiveCount, Is.Zero); Assert.That(loop.Planner.PlanCount, Is.Zero);
            Assert.That(manager.Pools.ActiveCount, Is.Zero); Assert.That(manager.Pools.TotalCreated, Is.EqualTo(24));
            Assert.That(manager.Pools.TotalDestroyed, Is.Zero);
        }
        [Test] public void PauseNearThresholdThenResumeRebasesExactlyOnce()
        {
            manager.StartRun(); loop.Step(45.45f);
            manager.Pause(); double offset=loop.Origin.Offset;
            Assert.That(loop.Step(1f), Is.False); Assert.That(loop.Origin.Offset, Is.EqualTo(offset));
            manager.Resume(); loop.Step(0.1f);
            Assert.That(loop.Origin.RebaseCount, Is.EqualTo(1));
            CheckCoverage(manager.Session.Snapshot.Distance);
        }
        [Test] public void RequiredPoolExhaustionRollsBackAndStopsBeforeForwardMovement()
        {
            Assert.That(manager.StartRun(), Is.True);
            var held = new List<RoadLease>();
            while (manager.Pools.TryRent(new SpawnContext(PoolKey.RoadTunnel, Vector3.zero, Quaternion.identity,
                roads.ActiveRoot, absoluteDistance: 99999d, spawnSequence: 99999), out var item))
                held.Add(new RoadLease((RoadSegment)item, item.ActivationId));
            var snapshot = manager.Session.Snapshot; var position=loop.Player.CurrentPosition;
            int active=roads.ActiveCount; ulong created=manager.Pools.TotalCreated;
            ulong rents=manager.Pools.TotalRents, returns=manager.Pools.TotalReturns;
            LogAssert.Expect(LogType.Error, new Regex(@"\[GameplayLoop\] Required road rental failed.*Exhausted"));
            Assert.That(loop.Step(30f), Is.False);
            Assert.That(manager.State, Is.EqualTo(GameState.Paused));
            Assert.That(manager.Session.Snapshot.Distance, Is.EqualTo(snapshot.Distance));
            Assert.That(loop.Player.CurrentPosition, Is.EqualTo(position));
            Assert.That(roads.ActiveCount, Is.EqualTo(active)); CheckCoverage(0d);
            Assert.That(manager.Pools.TotalRents, Is.EqualTo(rents+2));
            Assert.That(manager.Pools.TotalReturns, Is.EqualTo(returns+2));
            Assert.That(manager.Pools.TotalCreated, Is.EqualTo(created));
            Assert.That(manager.Resume(), Is.False);
            foreach (var lease in held) manager.Pools.Return(lease.Segment, lease.ActivationId);
            Assert.That(manager.Restart(), Is.True); CheckCoverage(0d);
        }
        [Test] public void MissingRequiredPoolOrWorldBlocksStartupWithoutPublishingRun()
        {
            var before=manager.Session.Snapshot;
            var origin=loop.Origin; origin.enabled=false;
            LogAssert.Expect(LogType.Error, new Regex(@"\[GameManager\] Cannot start: Assign WorldOrigin"));
            Assert.That(manager.StartRun(), Is.False);
            Assert.That(manager.Session.Snapshot, Is.EqualTo(before));
            Assert.That(manager.State, Is.EqualTo(GameState.Menu));
            Assert.That(manager.Pools.ActiveCount, Is.Zero);
            origin.enabled=true;
            Assert.That(manager.StartRun(), Is.True);
        }
        [TestCase(PoolKey.RoadOpen)] [TestCase(PoolKey.RoadTunnel)]
        [TestCase(PoolKey.RoadEntrance)] [TestCase(PoolKey.RoadExit)]
        public void EachMissingRequiredPoolBlocksBeforeSessionOrInputChanges(PoolKey missing)
        {
            var before=manager.Session.Snapshot;
            var catalog=ScriptableObject.CreateInstance<PoolConfig>();
            var entries=new List<PoolConfig.Entry>();
            var original=manager.Pools.Configuration;
            for (int i=0;i<original.EntryCount;i++) if (original.GetEntry(i).Key!=missing) entries.Add(original.GetEntry(i));
            typeof(PoolConfig).GetField("entries", BindingFlags.Instance|BindingFlags.NonPublic).SetValue(catalog, entries.ToArray());
            manager.Pools.Shutdown();
            typeof(PoolService).GetField("configuration", BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager.Pools, catalog);
            try
            {
                Assert.That(manager.Pools.Initialize(), Is.True);
                LogAssert.Expect(LogType.Error, new Regex(@"\[GameManager\] Cannot start: Required road pool " + missing));
                Assert.That(manager.StartRun(), Is.False);
                Assert.That(manager.State, Is.EqualTo(GameState.Menu));
                Assert.That(manager.Session.Snapshot, Is.EqualTo(before));
                Assert.That(manager.Pools.ActiveCount, Is.Zero);
                Assert.That(loop.Input.Actions.FindActionMap("Gameplay").enabled, Is.False);
            }
            finally { manager.Pools.Shutdown(); Object.Destroy(catalog); }
        }
        [Test] public void StaleLeaseDetectedBeforeMovementAndResetRestoresIt()
        {
            manager.StartRun(); var lease=roads.GetLease(0);
            manager.Pools.Return(lease.Segment, lease.ActivationId);
            double distance=manager.Session.Snapshot.Distance;
            LogAssert.Expect(LogType.Error, new Regex(@"\[GameplayLoop\] Stale or disabled required road lease"));
            Assert.That(loop.Step(0.02f), Is.False);
            Assert.That(manager.Session.Snapshot.Distance, Is.EqualTo(distance));
            Assert.That(manager.Restart(), Is.True); CheckCoverage(0d);
        }
        [Test] public void WarmWorldMaintenanceAllocationIsMeasuredSeparatelyFromPrewarm()
        {
            manager.StartRun();
            for (int i=0;i<32;i++) loop.Step(0.137f);
            long before=GC.GetAllocatedBytesForCurrentThread();
            bool success=true;
            for (int i=0;i<1000;i++) success &= loop.Step(0.137f);
            long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(success, Is.True);
            Debug.Log($"[Task05] 1000 warm synchronous world/player/planning ticks allocated {bytes} managed bytes on current thread; excludes engine frames/observers/bootstrap.");
        }
    }
}
