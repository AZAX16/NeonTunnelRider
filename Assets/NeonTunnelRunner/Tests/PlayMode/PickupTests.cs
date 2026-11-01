using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class PickupTests
    {
        private GameObject root;
        private GameplayConfig config;
        private GameManager manager;
        private GameplayLoop loop;
        private SpawnDirector planner;
        [SetUp] public void Setup()
        {
            config=ScriptableObject.CreateInstance<GameplayConfig>(); root=PlayModeMovementRig.Create("PickupFixture",config);
            manager=root.GetComponent<GameManager>(); loop=manager.Loop; planner=loop.Planner;
            Assert.That(manager.StartRun(),Is.True); planner.ReleaseAll();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if (manager) manager.Shutdown(); if (root) Object.Destroy(root); if (config) Object.Destroy(config); Time.timeScale=1f; yield return null; Assert.That(GameManager.Instance,Is.Null); }
        private Interactable Rent(PoolKey key,double z=0d,int lane=0,long sequence=100000)
        {
            Assert.That(manager.Pools.TryRent(new SpawnContext(key,new Vector3(lane*3.2f,.6f,(float)(z-loop.Origin.Offset)),Quaternion.identity,
                planner.OriginRoot,lane,z,12345,0f,sequence,planner),out var item),Is.True);
            return (Interactable)item;
        }
        private InteractionContext Context(Interactable item,ulong token=0) => new InteractionContext(manager.Session,loop.Survival,
            new Encounter(item.Key,token==0 ? item.ActivationId : token,item.SpawnSequence,item.Lane,item.AbsoluteZ,item.CurrentPosition,manager.Session.Snapshot.ElapsedTime),loop.Progression);
        [Test] public void AllFourConcreteClassesOverrideTheRealVirtualHook()
        {
            Assert.That(typeof(Interactable).IsAbstract,Is.True);
            foreach (Type type in new[] { typeof(StationaryCar),typeof(MovingCar),typeof(Coin),typeof(FuelCan) })
            {
                Assert.That(type.BaseType,Is.EqualTo(typeof(Interactable)));
                var method=type.GetMethod("OnInteract",BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.That(method.DeclaringType,Is.EqualTo(type)); Assert.That(method.GetBaseDefinition().DeclaringType,Is.EqualTo(typeof(Interactable)));
            }
            Assert.That(loop.Sensor.Capacity,Is.EqualTo(280)); Assert.That(planner.Capacity,Is.EqualTo(280));
            manager.Pools.TryGetCounters(PoolKey.Coin,out var coins); manager.Pools.TryGetCounters(PoolKey.Fuel,out var fuel);
            Assert.That(coins.Capacity,Is.EqualTo(160)); Assert.That(fuel.Capacity,Is.EqualTo(24));
        }
        [TestCase(PoolKey.Coin)] [TestCase(PoolKey.Fuel)]
        public void ReentrantAndDuplicateDeliveryRewardEventAndReturnOnceThenReuseRejectsStaleToken(PoolKey key)
        {
            var item=Rent(key); ulong old=item.ActivationId; var context=Context(item); int events=0;
            manager.Session.SetResources(3,20f); ulong returns=manager.Pools.TotalReturns;
            Action<CollectedEvent> onCollected=e=>
            {
                events++; Assert.That(item.Encountered,Is.True); Assert.That(item.TryInteract(context),Is.False);
                Assert.That(item.GetComponent<BoxCollider>().enabled,Is.False); Assert.That(item.transform.Find("Visual").gameObject.activeSelf,Is.False);
                Assert.That(e.RunId,Is.EqualTo(manager.Session.Snapshot.RunId)); Assert.That(e.Kind,Is.EqualTo(key));
                Assert.That(e.AbsoluteZ,Is.Zero); Assert.That(e.LocalPosition,Is.EqualTo(new Vector3(0,.6f,0)));
            };
            manager.Session.Collected+=onCollected;
            Assert.That(item.TryInteract(context),Is.True); Assert.That(item.TryInteract(context),Is.False);
            Assert.That(events,Is.EqualTo(1)); Assert.That(manager.Pools.TotalReturns,Is.EqualTo(returns+1));
            Assert.That(manager.Session.Snapshot.Coins,Is.EqualTo(key==PoolKey.Coin ? 1 : 0));
            Assert.That(manager.Session.Snapshot.Score,Is.EqualTo(key==PoolKey.Coin ? 100 : 0));
            Assert.That(manager.Session.Snapshot.Fuel,Is.EqualTo(key==PoolKey.Fuel ? 50f : 20f));
            manager.Session.Collected-=onCollected;
            var reused=Rent(key); Assert.That(reused,Is.SameAs(item)); Assert.That(reused.ActivationId,Is.Not.EqualTo(old));
            Assert.That(reused.Encountered,Is.False); Assert.That(reused.TryInteract(context),Is.False);
            Assert.That(planner.DeferReturn(reused,old),Is.False); Assert.That(reused.ReturnToPool(old),Is.False);
            Assert.That(reused.PreviousPosition,Is.EqualTo(reused.CurrentPosition));
            Assert.That(reused.transform.Find("Visual").gameObject.activeSelf,Is.True); Assert.That(reused.TryInteract(Context(reused)),Is.True);
            Assert.That(manager.Session.Snapshot.Coins,Is.EqualTo(key==PoolKey.Coin ? 2 : 0));
            Assert.That(manager.Session.Snapshot.Fuel,Is.EqualTo(key==PoolKey.Fuel ? 80f : 20f));
        }
        [TestCase(95f,100f,5f)] [TestCase(100f,100f,0f)] [TestCase(10f,40f,30f)]
        public void FuelClampsAndFullFuelStillCollects(float start,float expected,float applied)
        {
            manager.Session.SetResources(3,start); var fuel=Rent(PoolKey.Fuel); int events=0;
            manager.Session.Collected+=e=> { events++; Assert.That(e.Value,Is.EqualTo(30f)); Assert.That(e.AppliedValue,Is.EqualTo(applied)); };
            loop.Step(.02f); Assert.That(manager.Session.Snapshot.Fuel,Is.EqualTo(expected)); Assert.That(events,Is.EqualTo(1)); Assert.That(fuel.IsRented,Is.False);
        }
        [Test] public void GraceDoesNotBlockEitherPickupAndExactTieSortsPickupsThenSequence()
        {
            Rent(PoolKey.Stationary,0,sequence:1); loop.Step(.02f); Assert.That(loop.Survival.GraceRemaining,Is.GreaterThan(0d));
            manager.Session.SetResources(2,40f);
            Rent(PoolKey.Stationary,manager.Session.Snapshot.Distance,sequence:0);
            Rent(PoolKey.Coin,manager.Session.Snapshot.Distance,sequence:20); Rent(PoolKey.Fuel,manager.Session.Snapshot.Distance,sequence:10);
            var order=new List<PoolKey>(); manager.Session.Collected+=e=>order.Add(e.Kind);
            loop.Step(.02f); CollectionAssert.AreEqual(new[] {PoolKey.Fuel,PoolKey.Coin},order);
            Assert.That(manager.Session.Snapshot.Coins,Is.EqualTo(1)); Assert.That(manager.Session.Snapshot.Fuel,Is.EqualTo(70f)); Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(2));
            planner.ReleaseAll(); manager.Session.Advance(2f,0d); order.Clear();
            Rent(PoolKey.Stationary,manager.Session.Snapshot.Distance,sequence:0); Rent(PoolKey.Coin,manager.Session.Snapshot.Distance,sequence:100);
            manager.Session.Damaged+=_=>order.Add(PoolKey.Stationary); loop.Step(.02f);
            CollectionAssert.AreEqual(new[] {PoolKey.Coin,PoolKey.Stationary},order);
        }
        [Test] public void TimeOfImpactPrecedesCategoryAndHighSpeedCrossesThinPickups()
        {
            var order=new List<PoolKey>(); manager.Session.Damaged+=e=>order.Add(e.Encounter.Kind); manager.Session.Collected+=e=>order.Add(e.Kind);
            Rent(PoolKey.Coin,15d,sequence:1); Rent(PoolKey.Fuel,25d,sequence:2); Rent(PoolKey.Stationary,5d,sequence:100);
            manager.Session.SetCruiseSpeed(50f); loop.Step(1f);
            CollectionAssert.AreEqual(new[] {PoolKey.Stationary,PoolKey.Coin,PoolKey.Fuel},order);
            Assert.That(manager.Session.Snapshot.Score,Is.EqualTo(150)); Assert.That(manager.Session.Snapshot.Coins,Is.EqualTo(1));
        }
        [Test] public void SensorDefersReturnsAndRejectsLifecycleOrRentalReentryDuringCollection()
        {
            var coin=Rent(PoolKey.Coin); ulong token=coin.ActivationId; bool callback=false;
            manager.Session.Collected+=e=>
            {
                callback=true; Assert.That(planner.IsDispatching,Is.True); Assert.That(coin.IsRented,Is.True);
                Assert.That(coin.TryInteract(Context(coin)),Is.False); Assert.That(coin.ReturnToPool(token),Is.True);
                Assert.That(manager.Pause() || manager.EndRun(GameOverReason.Lives) || loop.Step(.1f),Is.False);
                Assert.That(manager.Pools.TryRent(new SpawnContext(PoolKey.Coin,Vector3.zero,Quaternion.identity,registry:planner),out _),Is.False);
                Assert.That(manager.Pools.LastRentFailure,Is.EqualTo(PoolRentFailure.Busy));
            };
            ulong returns=manager.Pools.TotalReturns; loop.Step(.02f);
            Assert.That(callback,Is.True); Assert.That(coin.IsRented,Is.False); Assert.That(manager.Pools.TotalReturns,Is.EqualTo(returns+1));
        }
        [TestCase(PoolKey.Coin)] [TestCase(PoolKey.Fuel)]
        public void CapturedContactCannotConsumeNewActivation(PoolKey key)
        {
            var item=Rent(key); ulong old=item.ActivationId;
            loop.Player.PredictStep(.02f,22f); planner.PredictTraffic(.02f,0d); loop.Sensor.CollectContacts(); Assert.That(loop.Sensor.ContactCount,Is.EqualTo(1));
            item.ReturnToPool(old); var reused=Rent(key,100d); loop.Sensor.DispatchContacts(manager.Session,.02f);
            Assert.That(reused,Is.SameAs(item)); Assert.That(reused.Encountered,Is.False); Assert.That(manager.Session.Snapshot.Coins,Is.Zero);
            Assert.That(manager.Session.Snapshot.Fuel,Is.EqualTo(100f));
        }
        [Test] public void AllConfiguredContactsAndReturnsFitWithoutOverflow()
        {
            int events=0; manager.Session.Collected+=_=>events++;
            foreach (var key in new[] {PoolKey.Coin,PoolKey.Fuel,PoolKey.Stationary,PoolKey.Moving})
            {
                manager.Pools.TryGetCounters(key,out var counters);
                for (int i=0;i<counters.Capacity;i++) Rent(key,sequence:(long)key*1000+i);
            }
            loop.Step(.02f); Assert.That(events,Is.EqualTo(184)); Assert.That(loop.Sensor.TotalContacts,Is.EqualTo(280));
            Assert.That(manager.Session.Snapshot.Coins,Is.EqualTo(160)); Assert.That(manager.Session.Snapshot.Score,Is.EqualTo(16000));
            Assert.That(planner.ActiveCount,Is.EqualTo(96)); Assert.That(loop.Sensor.ContactCount,Is.Zero);
        }
        [Test] public void ScoreDistancePauseRebaseAndRestartUseOneAuthoritativeFormula()
        {
            loop.Step(1.25f); Assert.That(manager.Session.Snapshot.Score,Is.EqualTo(27));
            var coin=Rent(PoolKey.Coin,manager.Session.Snapshot.Distance); int score=manager.Session.Snapshot.Score;
            Assert.That(coin.TryInteract(Context(coin)),Is.True); Assert.That(manager.Session.Snapshot.Score,Is.EqualTo(score+100));
            loop.Step(45f); loop.Step(48f); var snapshot=manager.Session.Snapshot;
            Assert.That(loop.Origin.RebaseCount,Is.GreaterThanOrEqualTo(2)); Assert.That(snapshot.Score,Is.EqualTo((int)Math.Floor(snapshot.Distance)+100*snapshot.Coins));
            manager.Pause(); Assert.That(loop.Step(10f),Is.False); Assert.That(manager.Session.Snapshot.Score,Is.EqualTo(snapshot.Score));
            Assert.That(manager.Restart(),Is.True); Assert.That(manager.Session.Snapshot.Coins,Is.Zero); Assert.That(manager.Session.Snapshot.Score,Is.Zero);
        }
        [Test] public void VisualChildrenAnimatePauseAndResetWithoutMovingLogicalBoundsOrStorage()
        {
            var coin=Rent(PoolKey.Coin,1100d,1); var fuel=Rent(PoolKey.Fuel,1200d,-1);
            var visual=coin.transform.Find("Visual"); var original=visual.localPosition; var rotation=visual.localRotation;
            var inactive=manager.Pools.StorageRoot.GetComponentsInChildren<Interactable>(true); var positions=Array.ConvertAll(inactive,i=>i.transform.position);
            planner.PredictTraffic(.5f,0d); planner.CommitTraffic(); Assert.That(visual.localPosition,Is.Not.EqualTo(original));
            Assert.That(coin.CurrentPosition,Is.EqualTo(new Vector3(3.2f,.6f,1100f))); Assert.That(coin.GetComponent<BoxCollider>().size,Is.EqualTo(new Vector3(1,1.2f,1)));
            manager.Pause(); var pose=visual.localPosition; var spin=visual.localRotation; Assert.That(loop.Step(10f),Is.False);
            Assert.That(visual.localPosition,Is.EqualTo(pose)); Assert.That(visual.localRotation,Is.EqualTo(spin)); manager.Resume();
            Assert.That(loop.Origin.RebaseIfNeeded(1001d),Is.True);
            Assert.That(coin.PreviousAbsoluteZ,Is.EqualTo(1100d)); Assert.That(fuel.PreviousAbsoluteZ,Is.EqualTo(1200d));
            Assert.That(coin.CurrentPosition.z,Is.EqualTo(1100d-loop.Origin.Offset)); Assert.That(coin.PreviousPosition.z,Is.EqualTo(coin.CurrentPosition.z));
            for (int i=0;i<inactive.Length;i++) Assert.That(inactive[i].transform.position,Is.EqualTo(positions[i]));
            ulong old=coin.ActivationId; coin.ReturnToPool(old); Assert.That(visual.localPosition,Is.EqualTo(original)); Assert.That(visual.localRotation,Is.EqualTo(rotation));
            var reused=Rent(PoolKey.Coin,1300d,1); Assert.That(reused,Is.SameAs(coin)); Assert.That(visual.localPosition,Is.EqualTo(original));
            planner.PredictTraffic(.5f,loop.Origin.Offset); Assert.That(visual.localPosition,Is.EqualTo(pose)); Assert.That(visual.localRotation,Is.EqualTo(spin));
        }
        [Test] public void CoinExhaustionOmitsPermanentlyWithoutGrowthOrPlanChanges()
        {
            var plan=planner.PlanAt(1); ulong created=manager.Pools.TotalCreated; var held=new List<TrafficLease>();
            while (manager.Pools.TryRent(new SpawnContext(PoolKey.Coin,Vector3.forward*5000,Quaternion.identity,absoluteDistance:5000),out var item)) held.Add(new TrafficLease((Interactable)item,item.ActivationId));
            Assert.That(loop.Step(2f),Is.True); Assert.That(planner.OmittedCoins,Is.GreaterThan(0)); Assert.That(manager.Pools.LastMissKey,Is.EqualTo(PoolKey.Coin));
            ulong misses=manager.Pools.Misses; foreach (var lease in held) lease.Item.ReturnToPool(lease.ActivationId);
            Assert.That(loop.Step(.02f),Is.True); Assert.That(manager.Pools.Misses,Is.EqualTo(misses)); Assert.That(planner.PlanAt(1),Is.EqualTo(plan));
            Assert.That(manager.Pools.TotalCreated,Is.EqualTo(created)); Assert.That(manager.Pools.TotalDestroyed,Is.Zero);
        }
        [Test] public void FuelExhaustionPausesBeforeTravelAndNeverStealsActiveRequiredRentals()
        {
            var held=new List<TrafficLease>(); for (int i=0;i<24;i++) { var item=Rent(PoolKey.Fuel,5000d,sequence:200000+i); held.Add(new TrafficLease(item,item.ActivationId)); }
            var plan=planner.PlanAt(1); long frontier=planner.NextFuelIndex; var snapshot=manager.Session.Snapshot; ulong created=manager.Pools.TotalCreated;
            LogAssert.Expect(LogType.Error,new Regex(@"\[GameplayLoop\] Required fuel rental failed after safe reclaim: Exhausted"));
            Assert.That(loop.Step(10f),Is.False); Assert.That(manager.State,Is.EqualTo(GameState.Paused));
            Assert.That(manager.Session.Snapshot.Distance,Is.EqualTo(snapshot.Distance)); Assert.That(loop.TickIndex,Is.Zero); Assert.That(planner.NextFuelIndex,Is.EqualTo(frontier));
            Assert.That(loop.Player.CurrentPosition,Is.EqualTo(config.StartingPosition)); Assert.That(planner.PlanAt(1),Is.EqualTo(plan));
            Assert.That(loop.Roads.CoverageStart,Is.LessThanOrEqualTo(-60d)); Assert.That(loop.Roads.CoverageEnd,Is.GreaterThanOrEqualTo(300d));
            foreach (var lease in held) Assert.That(lease.IsCurrent,Is.True);
            Assert.That(planner.FuelFailures,Is.EqualTo(1)); Assert.That(planner.DiagnosticCount,Is.InRange(1,64));
            Assert.That(manager.Resume(),Is.False); Assert.That(manager.Pools.TotalCreated,Is.EqualTo(created)); Assert.That(manager.Pools.TotalDestroyed,Is.Zero);
            Assert.That(manager.Restart(),Is.True); Assert.That(planner.NextFuelIndex,Is.EqualTo(2)); Assert.That(planner.FuelFailures,Is.Zero);
        }
        [Test] public void ExhaustedFuelReclaimsOnlyLegitimatelyBehindBoundsAndRetries()
        {
            var recyclable=Rent(PoolKey.Fuel,-100d); ulong old=recyclable.ActivationId; var held=new List<TrafficLease>();
            for (int i=0;i<23;i++) { var item=Rent(PoolKey.Fuel,5000d,sequence:300000+i); held.Add(new TrafficLease(item,item.ActivationId)); }
            Assert.That(loop.Step(10f),Is.True); Assert.That(planner.FuelReclaims,Is.EqualTo(1)); Assert.That(planner.FuelFailures,Is.Zero);
            Assert.That(recyclable.ActivationId,Is.Not.EqualTo(old)); Assert.That(recyclable.AbsoluteZ,Is.EqualTo(450d));
            foreach (var lease in held) Assert.That(lease.IsCurrent,Is.True);
            Assert.That(manager.Pools.TotalCreated,Is.EqualTo(304));
        }
        [Test] public void UnboundedCatchUpFailsExplicitlyInsteadOfSkippingMandatorySupply()
        {
            LogAssert.Expect(LogType.Error,new Regex(@"\[GameplayLoop\] Required fuel window exceeds reserved capacity"));
            Assert.That(loop.Step(1000f),Is.False); Assert.That(manager.Session.Snapshot.Distance,Is.Zero); Assert.That(planner.NextFuelIndex,Is.EqualTo(2));
        }
        [Test] public void PartialRequiredFuelReservationRollsBackWithoutAdvancingFrontier()
        {
            for (int i=0;i<22;i++) Rent(PoolKey.Fuel,5000d,sequence:400000+i);
            ulong rents=manager.Pools.TotalRents, returns=manager.Pools.TotalReturns;
            LogAssert.Expect(LogType.Error,new Regex(@"\[GameplayLoop\] Required fuel rental failed after safe reclaim: Exhausted"));
            Assert.That(loop.Step(30f),Is.False); Assert.That(planner.NextFuelIndex,Is.EqualTo(2));
            Assert.That(manager.Pools.TotalRents,Is.EqualTo(rents+2)); Assert.That(manager.Pools.TotalReturns,Is.EqualTo(returns+2));
            Assert.That(planner.ActiveCount,Is.EqualTo(22)); Assert.That(manager.Session.Snapshot.Distance,Is.Zero);
            Assert.That(manager.Pools.TotalCreated,Is.EqualTo(304));
        }
        [TestCase(PoolKey.Coin)] [TestCase(PoolKey.Fuel)]
        public void MissingRealPickupPoolBlocksStartWithoutPublishingAPartialRun(PoolKey missing)
        {
            manager.Pause(); manager.ReturnToMenu(); var before=manager.Session.Snapshot;
            var catalog=ScriptableObject.CreateInstance<PoolConfig>(); var entries=new List<PoolConfig.Entry>();
            for (int i=0;i<manager.Pools.Configuration.EntryCount;i++)
            { var entry=manager.Pools.Configuration.GetEntry(i); if (entry.Key!=missing) entries.Add(entry); }
            typeof(PoolConfig).GetField("entries",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(catalog,entries.ToArray());
            manager.Pools.Shutdown(); typeof(PoolService).GetField("configuration",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager.Pools,catalog);
            try
            {
                Assert.That(manager.Pools.Initialize(),Is.True);
                LogAssert.Expect(LogType.Error,new Regex(@"\[GameManager\] Cannot start: Missing/invalid required interactable pool "+missing));
                Assert.That(manager.StartRun(),Is.False); Assert.That(manager.Session.Snapshot,Is.EqualTo(before));
                Assert.That(manager.Pools.ActiveCount,Is.Zero); Assert.That(manager.State,Is.EqualTo(GameState.Menu));
            }
            finally { manager.Pools.Shutdown(); Object.Destroy(catalog); }
        }
        [Test] public void CoinCountSaturatesSafelyAndPausedRewardsDoNotMutateIt()
        {
            manager.Session.SetScore(int.MaxValue,int.MaxValue); loop.Progression.AddCoin();
            Assert.That(manager.Session.Snapshot.Coins,Is.EqualTo(int.MaxValue)); Assert.That(manager.Session.Snapshot.Score,Is.EqualTo(int.MaxValue));
            manager.Pause(); var before=manager.Session.Snapshot; loop.Progression.AddCoin(); Assert.That(manager.Session.Snapshot,Is.EqualTo(before));
        }
        [TestCase(22f,12345)] [TestCase(50f,12345)] [TestCase(50f,0)] [TestCase(50f,999)]
        public void PlannedFuelBecomesRealRentalsAndIsCollectibleThroughEveryProtectedTransition(float speed,int seed)
        {
            manager.Pause(); typeof(GameManager).GetField("runSeed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,seed);
            Assert.That(manager.Restart(),Is.True); manager.Session.SetResources(3,10f); manager.Session.SetCruiseSpeed(speed);
            int fuels=0,coins=0; var seen=new HashSet<long>();
            manager.Session.Collected+=e=>
            {
                if (e.Kind==PoolKey.Fuel) { fuels++; Assert.That(seen.Add(e.Encounter.SpawnSequence),Is.True); }
                else coins++;
            };
            while (manager.Session.Snapshot.Distance<2880d)
            {
                double distance=manager.Session.Snapshot.Distance; long block=(long)(distance/480d); var plan=planner.PlanAt(block);
                int target=distance-plan.AbsoluteStart>=360d+speed*planner.Configuration.ReactionTime ? plan.NextProtectedLane : plan.ProtectedLane;
                if (!loop.Player.IsTransitioning && loop.Player.LaneIndex!=target) loop.Player.RequestLane(Math.Sign(target-loop.Player.LaneIndex));
                Assert.That(loop.Step(.05f),Is.True);
                Assert.That(planner.PlanCount,Is.LessThanOrEqualTo(4)); Assert.That(planner.ActiveCount,Is.LessThanOrEqualTo(280));
            }
            Assert.That(fuels,Is.EqualTo(18)); Assert.That(coins,Is.GreaterThanOrEqualTo(24)); Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(3));
            Assert.That(manager.Session.Snapshot.Fuel,Is.EqualTo(100f)); Assert.That(planner.SpawnedFuel,Is.GreaterThanOrEqualTo(18));
            Assert.That(loop.Origin.RebaseCount,Is.GreaterThanOrEqualTo(2)); Assert.That(manager.Pools.Misses,Is.Zero);
            Debug.Log($"[Task07] live route seed={seed}, speed={speed}, distance={manager.Session.Snapshot.Distance:R}, fuel collections={fuels}, coins={coins}, spawned fuel={planner.SpawnedFuel}, rebases={loop.Origin.RebaseCount}, created={manager.Pools.TotalCreated}, peak={manager.Pools.PeakActive}.");
        }
        [Test] public void FiftyResetsClearPickupReservationsContactsAndOldTokensWithBothTypesCollected()
        {
            for (int cycle=0;cycle<50;cycle++)
            {
                Rent(PoolKey.Coin); var fuel=Rent(PoolKey.Fuel); var context=Context(fuel); ulong old=fuel.ActivationId;
                loop.Step(.02f); Assert.That(manager.Session.Snapshot.Coins,Is.EqualTo(1));
                loop.Step(48f); loop.Step(48f); Assert.That(loop.Origin.RebaseCount,Is.GreaterThanOrEqualTo(2));
                if ((cycle&1)==0) manager.Pause(); else manager.EndRun(GameOverReason.Lives);
                Assert.That(manager.Restart(),Is.True); Assert.That(fuel.TryInteract(context),Is.False); Assert.That(fuel.ReturnToPool(old),Is.False);
                Assert.That(manager.Session.Snapshot.Coins,Is.Zero); Assert.That(manager.Session.Snapshot.Score,Is.Zero); Assert.That(manager.Session.Snapshot.Fuel,Is.EqualTo(100f));
                Assert.That(planner.NextFuelIndex,Is.EqualTo(2)); Assert.That(loop.Sensor.ContactCount,Is.Zero); Assert.That(loop.Origin.Offset,Is.Zero);
                Assert.That(manager.Pools.TotalCreated,Is.EqualTo(304)); Assert.That(manager.Pools.TotalDestroyed,Is.Zero);
                planner.ReleaseAll();
            }
            manager.Pause(); Assert.That(manager.ReturnToMenu(),Is.True); Assert.That(planner.ActiveCount,Is.Zero); Assert.That(planner.PlanCount,Is.Zero); Assert.That(planner.NextFuelIndex,Is.Zero);
        }
    }
}
