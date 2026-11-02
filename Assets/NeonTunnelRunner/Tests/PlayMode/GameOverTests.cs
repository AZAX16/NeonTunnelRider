using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class GameOverTests
    {
        private GameObject root;
        private GameplayConfig config;
        private GameManager manager;
        private GameplayLoop loop;
        [SetUp] public void Setup()
        {
            config=ScriptableObject.CreateInstance<GameplayConfig>(); root=PlayModeMovementRig.Create("SurvivalFixture",config,700);
            manager=root.GetComponent<GameManager>(); loop=manager.Loop; Assert.That(manager.StartRun(),Is.True);
            loop.Planner.ReleaseAll();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (manager) manager.Shutdown(); if (root) Object.Destroy(root); if (config) Object.Destroy(config);
            yield return null; Assert.That(GameManager.Instance,Is.Null); Assert.That(Time.timeScale,Is.EqualTo(1f));
        }
        private Interactable Rent(PoolKey key,double z=double.NaN,long sequence=100000,int lane=0)
        {
            if(double.IsNaN(z)) z=manager.Session.Snapshot.Distance;
            Assert.That(manager.Pools.TryRent(new SpawnContext(key,new Vector3(lane*3.2f,.6f,(float)(z-loop.Origin.Offset)),Quaternion.identity,
                loop.Planner.OriginRoot,lane,z,12345,0,sequence,loop.Planner),out var item),Is.True);
            return (Interactable)item;
        }
        private Color ColorNow()
        { var block=new MaterialPropertyBlock(); loop.Visuals.FlashRenderer.GetPropertyBlock(block); return block.HasColor("_BaseColor") ? block.GetColor("_BaseColor") : loop.Visuals.FlashRenderer.sharedMaterial.GetColor("_BaseColor"); }

        [TestCase(.01f)] [TestCase(.02f)] [TestCase(.1f)] [TestCase(.5f)]
        public void TankDepletesAfterFiftyPlayingSecondsWithOneFuelEnd(float dt)
        {
            // Controlled no-encounter fixture: return optional/required content after each tick.
            // Movement and progression remain real; this fixture deliberately supplies no pickups.
            manager.Session.SnapshotChanged+=_=>loop.Planner.ReleaseAll(); int ended=0; manager.RunEnded+=(_,reason)=> { ended++; Assert.That(reason,Is.EqualTo(GameOverReason.Fuel)); };
            int ticks=(int)Math.Round(50d/dt);
            for (int i=0;i<ticks;i++)
            {
                Assert.That(manager.State,Is.EqualTo(GameState.Playing)); Assert.That(manager.Session.Snapshot.Fuel,Is.GreaterThan(0f));
                Assert.That(loop.Step(dt),Is.True);
            }
            Assert.That(manager.Session.Snapshot.ElapsedTime,Is.EqualTo(50d).Within(.000005d));
            Assert.That(manager.Session.Snapshot.Fuel,Is.Zero); Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(3));
            Assert.That(manager.EndReason,Is.EqualTo(GameOverReason.Fuel)); Assert.That(ended,Is.EqualTo(1));
            var final=manager.Session.Snapshot; ulong rents=manager.Pools.TotalRents;
            for (int i=0;i<5;i++) { Assert.That(loop.Step(1f),Is.False); Assert.That(manager.EndRun(GameOverReason.Lives),Is.False); }
            Assert.That(manager.Session.Snapshot,Is.EqualTo(final)); Assert.That(manager.Pools.TotalRents,Is.EqualTo(rents)); Assert.That(ended,Is.EqualTo(1));
        }
        [Test] public void ZeroAndInvalidDeltaDoNotConsumeInputTimeResourcesOrSpawn()
        {
            var before=manager.Session.Snapshot; ulong rents=manager.Pools.TotalRents;
            Assert.That(loop.Step(0f),Is.False);
            foreach (float dt in new[] {-1f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
                Assert.Throws<ArgumentOutOfRangeException>(()=>loop.Step(dt));
            Assert.That(manager.Session.Snapshot,Is.EqualTo(before)); Assert.That(loop.TickIndex,Is.Zero); Assert.That(manager.Pools.TotalRents,Is.EqualTo(rents));
        }
        [Test] public void NativeFixedDeltaQuantizationDoesNotAddADepletionTick()
        {
            manager.Session.SnapshotChanged+=_=>loop.Planner.ReleaseAll(); float dt=Time.fixedDeltaTime;
            int ticks=(int)Math.Round(50d/dt),ends=0; manager.RunEnded+=(_,__)=>ends++;
            for (int i=0;i<ticks;i++)
            {
                Assert.That(manager.State,Is.EqualTo(GameState.Playing)); Assert.That(manager.Session.Snapshot.Fuel,Is.GreaterThan(0f));
                Assert.That(loop.Step(dt),Is.True);
            }
            Assert.That(manager.Session.Snapshot.ElapsedTime,Is.EqualTo(50d).Within(.00005d));
            Assert.That(manager.Session.Snapshot.Fuel,Is.Zero); Assert.That(manager.EndReason,Is.EqualTo(GameOverReason.Fuel)); Assert.That(ends,Is.EqualTo(1));
            Debug.Log($"[Task08] native fixedDelta={dt:R}, ticks={ticks}, depleted at elapsed={manager.Session.Snapshot.ElapsedTime:R}, tolerance={RunSession.FuelBoundaryTolerance:R} fuel.");
        }
        [Test] public void SweptFuelOnDepletionTickRescuesBeforeDrainAndSharesContactOrdering()
        {
            manager.Session.SetResources(3,2f); var fuel=Rent(PoolKey.Fuel,10d); Rent(PoolKey.Coin,10d,100001);
            int events=0; manager.Session.Collected+=_=>events++;
            Assert.That(loop.Step(1f),Is.True);
            Assert.That(fuel.IsRented,Is.False); Assert.That(events,Is.EqualTo(2));
            Assert.That(manager.Session.Snapshot.Fuel,Is.EqualTo(30f)); Assert.That(manager.State,Is.EqualTo(GameState.Playing));
            Assert.That(manager.Session.Snapshot.Score,Is.EqualTo(122)); Assert.That(loop.Sensor.TotalContacts,Is.EqualTo(2));
        }
        [TestCase(false)] [TestCase(true)]
        public void LivesWinsTerminalTickWithCoinAndOptionalFuelAndPublishesOnlyTheFinalSnapshot(bool includeFuel)
        {
            manager.Session.SetResources(1,.04f); Rent(PoolKey.Stationary); Rent(PoolKey.Coin,sequence:100001);
            if (includeFuel) Rent(PoolKey.Fuel,sequence:100002);
            int snapshots=0,ends=0,damage=0,collections=0;
            manager.Session.SnapshotChanged+=s=> { snapshots++; Assert.That(s.ActualSpeed,Is.Zero); Assert.That(s.Coins,Is.EqualTo(1)); Assert.That(manager.EndReason,Is.EqualTo(GameOverReason.Lives)); };
            manager.Session.Damaged+=_=>damage++; manager.Session.Collected+=_=>collections++;
            manager.RunEnded+=(s,r)=> { ends++; Assert.That(r,Is.EqualTo(GameOverReason.Lives)); Assert.That(s,Is.EqualTo(manager.Session.Snapshot)); };
            long frontier=loop.Planner.NextFuelIndex; ulong spawned=loop.Planner.SpawnedCoins; long rebases=loop.Origin.RebaseCount;
            loop.Player.RequestLane(1); loop.Player.RequestLane(-1);
            Assert.That(loop.Step(.02f),Is.True);
            Assert.That(manager.State,Is.EqualTo(GameState.GameOver)); Assert.That(manager.Session.Snapshot.Lives,Is.Zero);
            Assert.That(manager.Session.Snapshot.Fuel,includeFuel ? Is.EqualTo(30f).Within(.00001f) : Is.Zero);
            Assert.That(manager.Session.Snapshot.Score,Is.EqualTo(100)); Assert.That(manager.Session.Snapshot.Distance,Is.EqualTo(22d*.02f));
            Assert.That(snapshots,Is.EqualTo(1)); Assert.That(ends,Is.EqualTo(1)); Assert.That(damage,Is.EqualTo(1)); Assert.That(collections,Is.EqualTo(includeFuel ? 2 : 1));
            Assert.That(loop.Planner.NextFuelIndex,Is.EqualTo(frontier)); Assert.That(loop.Planner.SpawnedCoins,Is.EqualTo(spawned)); Assert.That(loop.Origin.RebaseCount,Is.EqualTo(rebases));
            Assert.That(loop.Player.QueuedCommand,Is.Zero); Assert.That(loop.Input.ConsumeLaneCommand(),Is.Zero);
            Assert.That(loop.Player.transform.position,Is.EqualTo(loop.Player.CurrentPosition));
            Assert.That(loop.Visuals.IsFlashing,Is.True); var color=ColorNow(); double flash=loop.Visuals.FlashRemaining;
            var final=manager.Session.Snapshot; ulong rents=manager.Pools.TotalRents;
            for (int i=0;i<3;i++) { Assert.That(loop.Step(1f),Is.False); Assert.That(manager.EndRun(GameOverReason.Fuel),Is.False); loop.Survival.AddFuel(30f); loop.Progression.AddCoin(); }
            Assert.That(loop.Survival.EvaluateEnd(),Is.EqualTo(GameOverReason.None)); Assert.That(manager.Session.Snapshot,Is.EqualTo(final)); Assert.That(manager.Pools.TotalRents,Is.EqualTo(rents));
            Assert.That(ColorNow(),Is.EqualTo(color)); Assert.That(loop.Visuals.FlashRemaining,Is.EqualTo(flash)); Assert.That(ends,Is.EqualTo(1));
        }
        [Test] public void ThreeAcceptedHitsWithGraceRejectedOverlapAndReusedActivationFlashExactlyThreeTimes()
        {
            int damage=0,ends=0; manager.Session.Damaged+=_=>damage++; manager.RunEnded+=(_,__)=>ends++;
            Color baseline=ColorNow(); var first=Rent(PoolKey.Stationary); ulong token=first.ActivationId; loop.Step(.02f);
            Assert.That(ColorNow(),Is.Not.EqualTo(baseline)); double until=loop.Survival.GraceUntil;
            var rejected=Rent(PoolKey.Stationary,sequence:100001); double flash=loop.Visuals.FlashRemaining; loop.Step(.02f);
            Assert.That(rejected.Encountered,Is.True); Assert.That(loop.Survival.GraceUntil,Is.EqualTo(until)); Assert.That(loop.Visuals.FlashRemaining,Is.EqualTo(flash-.02f).Within(.000001d));
            loop.Step(1f); Assert.That(damage,Is.EqualTo(1)); Assert.That(loop.Visuals.IsFlashing,Is.False); Assert.That(ColorNow(),Is.EqualTo(baseline));
            first.ReturnToPool(token); var reused=Rent(PoolKey.Stationary,sequence:100002); Assert.That(reused,Is.SameAs(first)); Assert.That(reused.ActivationId,Is.Not.EqualTo(token)); loop.Step(.02f);
            loop.Step(1f); Rent(PoolKey.Stationary,sequence:100003); loop.Step(.02f);
            Assert.That(damage,Is.EqualTo(3)); Assert.That(loop.Visuals.AcceptedFlashes,Is.EqualTo(3)); Assert.That(ends,Is.EqualTo(1)); Assert.That(manager.EndReason,Is.EqualTo(GameOverReason.Lives));
        }
        [Test] public void GraceExpiryAcceptsExactDeadlineButRejectsBeforeItAndNeverDelaysConsumedOverlap()
        {
            Rent(PoolKey.Stationary); loop.Step(.02f); loop.Step(.97998f);
            var rejected=Rent(PoolKey.Stationary,sequence:100001); loop.Step(.00001f); Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(2)); Assert.That(rejected.Encountered,Is.True);
            loop.Step((float)(loop.Survival.GraceUntil-manager.Session.Snapshot.ElapsedTime));
            Rent(PoolKey.Stationary,sequence:100002); loop.Step(.02f); Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(1)); Assert.That(loop.Visuals.AcceptedFlashes,Is.EqualTo(2));
        }
        [Test] public void PauseFreezesResourcesGraceTrafficLaneAndFlashAndResumeClearsQueuedLane()
        {
            Rent(PoolKey.Stationary); var moving=Rent(PoolKey.Moving,200d); loop.Player.RequestLane(1); loop.Player.RequestLane(-1); loop.Step(.02f);
            manager.Pause(); var snapshot=manager.Session.Snapshot; var pose=loop.Player.CurrentPosition; var traffic=moving.CurrentPosition;
            double grace=loop.Survival.GraceRemaining,flash=loop.Visuals.FlashRemaining; float progress=loop.Player.TransitionProgress;
            Assert.That(loop.Step(10f),Is.False); loop.Visuals.FinishTick();
            Assert.That(manager.Session.Snapshot,Is.EqualTo(snapshot)); Assert.That(loop.Player.CurrentPosition,Is.EqualTo(pose)); Assert.That(moving.CurrentPosition,Is.EqualTo(traffic));
            Assert.That(moving.transform.position,Is.EqualTo(traffic)); Assert.That(loop.Survival.GraceRemaining,Is.EqualTo(grace)); Assert.That(loop.Visuals.FlashRemaining,Is.EqualTo(flash)); Assert.That(loop.Player.TransitionProgress,Is.EqualTo(progress));
            Assert.That(loop.Player.QueuedCommand,Is.Zero); manager.Resume(); loop.Step(.3f);
            Assert.That(loop.Player.LaneIndex,Is.EqualTo(1)); Assert.That(loop.Visuals.IsFlashing,Is.False); Assert.That(manager.Session.Snapshot.Fuel,Is.LessThan(snapshot.Fuel));
        }
        [Test] public void FlashPreservesOtherPropertyOverridesAndEnableCyclesDoNotMultiplyListeners()
        {
            var renderer=loop.Visuals.FlashRenderer; var material=renderer.sharedMaterial; Color baseline=ColorNow();
            for (int i=0;i<5;i++) { loop.Visuals.enabled=false; loop.Visuals.enabled=true; }
            var block=new MaterialPropertyBlock(); renderer.GetPropertyBlock(block); block.SetFloat("_Smoothness",.123f); renderer.SetPropertyBlock(block);
            Rent(PoolKey.Stationary); loop.Step(.02f); Assert.That(loop.Visuals.AcceptedFlashes,Is.EqualTo(1));
            renderer.GetPropertyBlock(block); block.SetFloat("_Smoothness",.789f); renderer.SetPropertyBlock(block);
            loop.Visuals.enabled=false; renderer.GetPropertyBlock(block); Assert.That(block.GetFloat("_Smoothness"),Is.EqualTo(.789f)); Assert.That(ColorNow(),Is.EqualTo(baseline));
            Assert.That(renderer.sharedMaterial,Is.SameAs(material)); Assert.That(material.GetColor("_BaseColor"),Is.EqualTo(baseline));
            loop.Visuals.enabled=true; manager.Pause(); manager.ReturnToMenu(); Assert.That(loop.Visuals.IsFlashing,Is.False);
        }
        [Test] public void FiftyRestartsAfterRebasesOrFinalHitsRestoreResourcesRendererAndRejectStaleWork()
        {
            Color baseline=ColorNow(); var plan=loop.Planner.PlanAt(2); int ends=0; manager.RunEnded+=(_,__)=>ends++;
            manager.Session.RetainBestScore(1500); int initialCount=0;
            for (int i=0;i<50;i++)
            {
                SurvivalRoute.Advance(loop,96f); Assert.That(loop.Origin.RebaseCount,Is.GreaterThanOrEqualTo(2));
                loop.Planner.ReleaseAll(); var coin=Rent(PoolKey.Coin,manager.Session.Snapshot.Distance,lane:loop.Player.LaneIndex); var car=Rent(PoolKey.Stationary,manager.Session.Snapshot.Distance,100001,loop.Player.LaneIndex); ulong token=car.ActivationId;
                var encounter=new Encounter(car.Key,token,car.SpawnSequence,0,car.AbsoluteZ,car.CurrentPosition,manager.Session.Snapshot.ElapsedTime);
                manager.Session.SetResources((i&1)==0 ? 3 : 1,40f); loop.Step(.02f); Assert.That(loop.Visuals.IsFlashing,Is.True);
                if ((i&1)==0) manager.Pause(); else Assert.That(manager.State,Is.EqualTo(GameState.GameOver));
                int outgoingBest=manager.Session.Snapshot.BestScore;
                Assert.That(manager.Restart(),Is.True); var s=manager.Session.Snapshot;
                Assert.That(s.Lives,Is.EqualTo(3)); Assert.That(s.Fuel,Is.EqualTo(100f)); Assert.That(s.ElapsedTime+s.Distance+s.Score+s.Coins,Is.Zero);
                Assert.That(s.ActualSpeed,Is.EqualTo(22f)); Assert.That(s.Seed,Is.EqualTo(12345)); Assert.That(s.BestScore,Is.EqualTo(outgoingBest));
                Assert.That(loop.Player.LaneIndex,Is.Zero); Assert.That(loop.Player.CurrentPosition,Is.EqualTo(config.StartingPosition)); Assert.That(loop.Player.Body.position,Is.EqualTo(config.StartingPosition));
                Assert.That(loop.Survival.GraceUntil,Is.Zero); Assert.That(loop.Visuals.IsFlashing,Is.False); Assert.That(ColorNow(),Is.EqualTo(baseline)); Assert.That(loop.Sensor.ContactCount,Is.Zero);
                Assert.That(loop.Planner.PlanAt(2),Is.EqualTo(plan)); Assert.That(loop.Origin.Offset,Is.Zero); Assert.That(manager.EndReason,Is.EqualTo(GameOverReason.None));
                Assert.That(car.ReturnToPool(token),Is.False); Assert.That(car.TryInteract(new InteractionContext(manager.Session,loop.Survival,encounter,loop.Progression)),Is.False);
                if (i==0) initialCount=manager.Pools.ActiveCount; Assert.That(manager.Pools.ActiveCount,Is.EqualTo(initialCount)); Assert.That(manager.Pools.TotalCreated,Is.EqualTo(304));
            }
            Assert.That(ends,Is.EqualTo(25)); Assert.That(loop.Visuals.AcceptedFlashes,Is.EqualTo(50)); Assert.That(manager.Pools.Misses,Is.Zero);
            manager.Pause(); manager.ReturnToMenu(); Assert.That(manager.Pools.ActiveCount,Is.Zero); Assert.That(Time.timeScale,Is.EqualTo(1f));
        }
    }
}
