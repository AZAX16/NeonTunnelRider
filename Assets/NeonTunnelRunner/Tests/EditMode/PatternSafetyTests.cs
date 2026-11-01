using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class PatternSafetyTests
    {
        private WorldConfig world;
        private GameplayConfig gameplay;
        private GameObject root;
        private SpawnDirector planner;
        [SetUp] public void Setup()
        {
            world = ScriptableObject.CreateInstance<WorldConfig>();
            gameplay = ScriptableObject.CreateInstance<GameplayConfig>();
            root = new GameObject("PatternFixture"); planner = root.AddComponent<SpawnDirector>();
            Assign(planner, "configuration", world); Assign(planner, "gameplay", gameplay);
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); Object.DestroyImmediate(world); Object.DestroyImmediate(gameplay); }

        [TestCase(-1,-1)] [TestCase(-1,0)] [TestCase(-1,1)]
        [TestCase(0,-1)] [TestCase(0,0)] [TestCase(0,1)]
        [TestCase(1,-1)] [TestCase(1,0)] [TestCase(1,1)]
        public void EveryProtectedLaneTransitionFitsAtActualMaximumSpeed(int from, int to)
        {
            Assert.That(world.TryValidate(gameplay, out var error), Is.True, error);
            Assert.That(PatternValidator.CanTransition(from, to, world, gameplay), Is.True);
            Assert.That(PatternValidator.Validate(new BlockPlan(12, 1, from, to, 0), world, gameplay, out _), Is.True);
            Assert.That(120d / gameplay.MaximumSpeed, Is.EqualTo(2.4d));
            Assert.That(world.ReactionTime + 2d * gameplay.LaneTransitionDuration + world.TransitionMargin, Is.EqualTo(1.56d).Within(0.000001d));
        }
        [Test] public void UnsafeTimingAndNonFiniteConfigurationAreRejected()
        {
            Assign(gameplay, "maximumSpeed", 100f);
            Assert.That(world.TryValidate(gameplay, out _), Is.False);
            Assert.That(PatternValidator.Validate(new BlockPlan(0, 1, -1, 1, 0), world, gameplay, out var reason), Is.False);
            Assert.That(reason, Is.EqualTo(PatternRejection.UnsafeTiming));
            Assign(gameplay, "maximumSpeed", float.NaN);
            Assert.That(PatternValidator.CanTransition(0, 0, world, gameplay), Is.False);
        }
        [TestCase(7u, PatternRejection.InvalidOccupancy)]
        [TestCase(2u, PatternRejection.ProtectedLaneOccupied)]
        [TestCase(1u << 18, PatternRejection.InvalidOccupancy)]
        public void UnsafeOccupancyProposalsAreRejected(uint mask, PatternRejection expected)
        {
            Assert.That(PatternValidator.Validate(new BlockPlan(0, 1, 0, 1, mask), world, gameplay, out var reason), Is.False);
            Assert.That(reason, Is.EqualTo(expected));
        }
        [Test] public void WarmupAndInvalidLaneProposalsAreRejected()
        {
            Assert.That(PatternValidator.Validate(new BlockPlan(0, 0, 0, 1, 1), world, gameplay, out var reason), Is.False);
            Assert.That(reason, Is.EqualTo(PatternRejection.WarmupOccupied));
            Assert.That(PatternValidator.Validate(new BlockPlan(0, 0, 2, 0, 0), world, gameplay, out reason), Is.False);
            Assert.That(reason, Is.EqualTo(PatternRejection.InvalidLane));
        }
        [Test] public void ThousandSeedsHaveClearProtectedLanesWarmupBuffersAndStableOrdering()
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                planner.ResetRun(seed);
                long previousSequence = -1;
                for (long block = 0; block < 21; block++)
                {
                    var plan = planner.PlanAt(block);
                    Assert.That(PatternValidator.Validate(plan, world, gameplay, out _), Is.True);
                    Assert.That(plan.AbsoluteEnd - plan.BufferStart, Is.EqualTo(120d));
                    Assert.That(plan.NextProtectedLane, Is.EqualTo(planner.PlanAt(block + 1).ProtectedLane));
                    for (int row = 0; row < 6; row++)
                        for (int lane = -1; lane <= 1; lane++)
                            if (plan.TryGetCandidate(row, lane, out var candidate))
                            {
                                Assert.That(candidate.AbsolutePosition, Is.GreaterThanOrEqualTo(180d).And.LessThan(plan.BufferStart));
                                Assert.That(candidate.Lane, Is.Not.EqualTo(plan.ProtectedLane));
                                Assert.That(candidate.SpawnSequence, Is.GreaterThan(previousSequence));
                                previousSequence = candidate.SpawnSequence;
                            }
                }
            }
        }
        [Test] public void BatchSizeResetAndLargeAbsoluteIndicesDoNotChangePlans()
        {
            planner.ResetRun(12345);
            var expected = new BlockPlan[210];
            for (int i = 0; i < expected.Length; i++) expected[i] = planner.PlanAt(i);
            foreach (int batch in new[] { 1, 3, 17, 210 })
            {
                planner.ResetRun(12345);
                for (int start = 0; start < expected.Length; start += batch)
                    for (int i = start; i < System.Math.Min(start + batch, expected.Length); i++)
                    {
                        planner.FillWindow(i * 480d + 123.25d);
                        Assert.That(planner.PlanAt(i), Is.EqualTo(expected[i]));
                        Assert.That(planner.PlanCount, Is.InRange(2,4));
                    }
            }
            const long index = 1000000000000L;
            Assert.That(planner.PlanAt(index).AbsoluteStart, Is.EqualTo(index * 480d));
            Assert.That(planner.PlanAt(index), Is.EqualTo(planner.PlanAt(index)));
            Assert.That(RoadManager.LocalStart(index, index * 60d - 12.25d), Is.EqualTo(12.25f));
            planner.ResetRun(999);
            Assert.That(planner.PlanAt(17), Is.Not.EqualTo(expected[17]));
        }
        [Test] public void ScheduleStartsOpenAndMatchesEveryUsedEnvironmentSeam()
        {
            var expected = new[] { PoolKey.RoadOpen, PoolKey.RoadOpen, PoolKey.RoadOpen, PoolKey.RoadEntrance,
                PoolKey.RoadTunnel, PoolKey.RoadTunnel, PoolKey.RoadTunnel, PoolKey.RoadExit };
            for (long i = -80; i < 80; i++)
                Assert.That(RoadManager.KeyAt(i), Is.EqualTo(expected[(int)((i % 8 + 8) % 8)]));
        }
        private static void Assign(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
