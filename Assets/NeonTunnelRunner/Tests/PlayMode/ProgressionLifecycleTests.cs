using System;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace NeonTunnelRunner
{
    public sealed class ProgressionLifecycleTests
    {
        private GameObject root;
        private GameplayConfig config;
        private GameManager gm;
        [SetUp] public void Setup()
        {
            config=ScriptableObject.CreateInstance<GameplayConfig>();
            root=PlayModeMovementRig.Create("ProgressionFixture",config);
            gm=root.GetComponent<GameManager>(); Assert.That(gm.StartRun(),Is.True);
        }
        [TearDown] public void Cleanup() { gm.Shutdown(); Object.DestroyImmediate(root); Object.DestroyImmediate(config); }
        [TestCase(90f,0d,.5f)] [TestCase(.01f,3000d,.5f)] [TestCase(180f,0d,1f)] [TestCase(.01f,6000d,1f)]
        public void TickStartUsesAuthoritativeDoubleDistanceOrPlayingTime(float elapsed,double distance,float difficulty)
        {
            gm.Session.Advance(elapsed,distance);
            if(distance>0) {
                // Isolated distance-only fixture starts at a world frontier without renting skipped fuel.
                gm.Loop.Planner.ReleaseAll();
                typeof(SpawnDirector).GetField("nextFuelIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(gm.Loop.Planner,(long)(distance/480d)*3L);
                gm.Loop.Player.SetAbsoluteForward(distance,0d); gm.Loop.Player.CommitPose(); gm.Loop.Player.HoldPose(); }
            var before=gm.Session.Snapshot;
            Assert.That(gm.Loop.Step(.02f),Is.True);
            var s=gm.Session.Snapshot;
            Assert.That(s.Difficulty,Is.EqualTo(difficulty)); Assert.That(s.CruiseSpeed,Is.EqualTo(22+28*difficulty));
            Assert.That(s.ActualSpeed,Is.EqualTo(s.CruiseSpeed));
            Assert.That(s.Distance-before.Distance,Is.EqualTo((double)s.CruiseSpeed*.02f).Within(1e-9));
            Assert.That(s.AllowedSlotOccupancy,Is.EqualTo(.25f+.55f*difficulty).Within(1e-6));
            gm.Pause(); var paused=gm.Session.Snapshot;
            Assert.That(paused.ActualSpeed,Is.Zero); Assert.That(gm.Loop.Step(1000f),Is.False);
            Assert.That(gm.Session.Snapshot,Is.EqualTo(paused)); gm.Resume();
            Assert.That(gm.Session.Snapshot.CruiseSpeed,Is.EqualTo(s.CruiseSpeed));
            Assert.That(gm.Session.Snapshot.ActualSpeed,Is.EqualTo(s.CruiseSpeed));
            gm.EndRun(GameOverReason.Fuel); var final=gm.Session.Snapshot;
            Assert.That(final.ActualSpeed,Is.Zero); Assert.That(gm.Loop.Step(10f),Is.False); Assert.That(gm.Session.Snapshot,Is.EqualTo(final));
            gm.Restart(); var reset=gm.Session.Snapshot;
            Assert.That(reset.Difficulty,Is.Zero); Assert.That(reset.CruiseSpeed,Is.EqualTo(22f));
            Assert.That(reset.AllowedSlotOccupancy,Is.EqualTo(.25f)); Assert.That(reset.Score,Is.Zero); Assert.That(reset.BestScore,Is.EqualTo(final.BestScore));
        }
        [Test] public void RealProtectedRouteReachesCapAndRebasesWithoutChangingCommittedRows()
        {
            var plan=gm.Loop.Planner.PlanAt(13);
            SurvivalRoute.Advance(gm.Loop,181f);
            var s=gm.Session.Snapshot;
            Assert.That(s.Difficulty,Is.EqualTo(1f)); Assert.That(s.CruiseSpeed,Is.EqualTo(50f));
            Assert.That(s.Lives,Is.EqualTo(3)); Assert.That(s.Fuel,Is.GreaterThan(0f)); Assert.That(s.BestScore,Is.EqualTo(s.Score));
            Assert.That(gm.Loop.Origin.RebaseCount,Is.GreaterThanOrEqualTo(6));
            Assert.That(gm.Loop.Planner.PlanAt(13),Is.EqualTo(plan));
            Assert.That(gm.Loop.Planner.SpawnedMoving,Is.GreaterThan(0)); Assert.That(gm.Loop.Planner.TrafficPoolMisses,Is.Zero);
            var before=gm.Session.Snapshot; gm.Loop.Origin.RebaseIfNeeded(s.Distance+1000d);
            Assert.That(gm.Session.Snapshot,Is.EqualTo(before),"Origin shifts cannot advance progression/score.");
            Debug.Log($"[Task09 cap] distance={s.Distance:R}, elapsed={s.ElapsedTime:R}, difficulty={s.Difficulty}, actual={s.ActualSpeed}, fuel={s.Fuel}, score={s.Score}, best={s.BestScore}, rebases={gm.Loop.Origin.RebaseCount}");
        }
    }
}
