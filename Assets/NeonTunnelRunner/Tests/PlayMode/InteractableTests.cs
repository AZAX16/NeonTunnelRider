using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class InteractableTests
    {
        private GameObject root;
        private GameplayConfig config;
        private GameManager manager;
        private GameplayLoop loop;
        private SpawnDirector traffic;
        private float previousScale;
        [SetUp] public void Setup()
        {
            previousScale=Time.timeScale; config=ScriptableObject.CreateInstance<GameplayConfig>();
            root=PlayModeMovementRig.Create("TrafficFixture",config); manager=root.GetComponent<GameManager>(); loop=manager.Loop; traffic=loop.Planner;
            Assert.That(manager.StartRun(),Is.True); traffic.ReleaseAll();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if (manager) manager.Shutdown(); if (root) Object.Destroy(root); if (config) Object.Destroy(config); Time.timeScale=previousScale; yield return null; Assert.That(GameManager.Instance,Is.Null); }
        private Interactable Rent(PoolKey key,double z,int lane=0,long sequence=10000)
        {
            var context=new SpawnContext(key,new Vector3(lane*3.2f,0.6f,(float)(z-loop.Origin.Offset)),Quaternion.identity,traffic.OriginRoot,lane,z,12345,key==PoolKey.Moving ? 8f : 0f,sequence,traffic);
            Assert.That(manager.Pools.TryRent(context,out var item),Is.True); return (Interactable)item;
        }
        [Test] public void RealInheritanceAndOverridesHaveIndependentPools()
        {
            Assert.That(typeof(Interactable).IsAbstract,Is.True);
            foreach (Type type in new[] {typeof(StationaryCar),typeof(MovingCar)})
            { Assert.That(type.BaseType,Is.EqualTo(typeof(Interactable))); var method=type.GetMethod("OnInteract",BindingFlags.Instance|BindingFlags.NonPublic); Assert.That(method.DeclaringType,Is.EqualTo(type)); Assert.That(method.GetBaseDefinition().DeclaringType,Is.EqualTo(typeof(Interactable))); }
            var stationary=Rent(PoolKey.Stationary,100,1); var moving=Rent(PoolKey.Moving,200,-1);
            Assert.That(stationary is StationaryCar && moving is MovingCar,Is.True);
            Assert.That(traffic.ActiveCount,Is.EqualTo(2)); Assert.That(loop.Sensor.Capacity,Is.EqualTo(280));
        }
        [Test] public void WorldMotionPauseAndRebasePreserveBothAbsoluteHistories()
        {
            var stationary=Rent(PoolKey.Stationary,1100,1); var moving=Rent(PoolKey.Moving,1200,-1);
            var inactive=manager.Pools.StorageRoot.GetComponentsInChildren<Interactable>(true);
            var positions=Array.ConvertAll(inactive,i=>i.transform.position);
            traffic.PredictTraffic(2f,0d); traffic.CommitTraffic();
            Assert.That(stationary.AbsoluteZ,Is.EqualTo(1100d)); Assert.That(moving.AbsoluteZ,Is.EqualTo(1216d));
            manager.Pause(); Assert.That(loop.Step(50f),Is.False); Assert.That(moving.AbsoluteZ,Is.EqualTo(1216d)); manager.Resume();
            Assert.That(loop.Origin.RebaseIfNeeded(1001d),Is.True);
            Assert.That(stationary.PreviousAbsoluteZ,Is.EqualTo(1100d)); Assert.That(moving.PreviousAbsoluteZ,Is.EqualTo(1200d));
            Assert.That(stationary.CurrentPosition.z,Is.EqualTo(1100d-loop.Origin.Offset));
            Assert.That(moving.PreviousPosition.z,Is.EqualTo(1200d-loop.Origin.Offset)); Assert.That(moving.CurrentPosition.z,Is.EqualTo(1216d-loop.Origin.Offset));
            for (int i=0;i<inactive.Length;i++) Assert.That(inactive[i].transform.position,Is.EqualTo(positions[i]));
            traffic.PredictTraffic(0.5f,loop.Origin.Offset); Assert.That(moving.AbsoluteZ,Is.EqualTo(1220d));
        }
        [Test] public void EncounterGuardGraceExpiryAndReusedActivationHaveExactlyOneEventPerDamage()
        {
            int events=0; var payloads=new List<DamageEvent>(); manager.Session.Damaged+=damage=>{events++; payloads.Add(damage);};
            var first=Rent(PoolKey.Stationary,0); loop.Step(0.02f);
            Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(2)); Assert.That(first.Encountered,Is.True); Assert.That(events,Is.EqualTo(1));
            var second=Rent(PoolKey.Stationary,loop.Player.CurrentPosition.z,sequence:10001); loop.Step(0.02f);
            Assert.That(second.Encountered,Is.True); Assert.That(events,Is.EqualTo(1));
            manager.Pause(); double grace=loop.Survival.GraceRemaining; Assert.That(loop.Step(10f),Is.False); Assert.That(loop.Survival.GraceRemaining,Is.EqualTo(grace)); manager.Resume();
            manager.Session.Advance(1.1f,0d); loop.Step(0.02f); Assert.That(events,Is.EqualTo(1));
            ulong old=first.ActivationId; Assert.That(first.ReturnToPool(old),Is.True);
            var reused=Rent(PoolKey.Stationary,manager.Session.Snapshot.Distance,sequence:10002);
            Assert.That(reused,Is.SameAs(first)); Assert.That(reused.Encountered,Is.False); Assert.That(reused.ActivationId,Is.Not.EqualTo(old));
            Assert.That(reused.ReturnToPool(old),Is.False); loop.Step(0.02f);
            Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(1)); Assert.That(events,Is.EqualTo(2));
            Assert.That(payloads[0].Encounter.ActivationId,Is.EqualTo(old)); Assert.That(payloads[0].RemainingLives,Is.EqualTo(2));
        }
        [Test] public void PausedAndForeignSessionContextsCannotConsumeAnActivation()
        {
            var item=Rent(PoolKey.Stationary,0);
            var encounter=new Encounter(item.Key,item.ActivationId,item.SpawnSequence,item.Lane,item.AbsoluteZ,item.CurrentPosition,manager.Session.Snapshot.ElapsedTime);
            manager.Pause();
            Assert.That(item.TryInteract(new InteractionContext(manager.Session,loop.Survival,encounter)),Is.False);
            Assert.That(item.Encountered,Is.False); manager.Resume();
            Assert.That(item.TryInteract(new InteractionContext(new RunSession(config),loop.Survival,encounter)),Is.False);
            Assert.That(item.Encountered,Is.False);
            Assert.That(item.TryInteract(new InteractionContext(manager.Session,loop.Survival,encounter)),Is.True);
            Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(2));
        }

        [Test] public void PersistentOverlapNeverBecomesDelayedDamageAfterGrace()
        {
            var first=Rent(PoolKey.Stationary,0); loop.Step(0.02f);
            var second=Rent(PoolKey.Stationary,0,sequence:10001);
            manager.Session.SetCruiseSpeed(0f); loop.Step(0.02f); loop.Step(1.5f);
            Assert.That(first.Encountered && second.Encountered,Is.True); Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(2));
            Rent(PoolKey.Stationary,loop.Player.CurrentPosition.z,sequence:10002); loop.Step(0.02f); Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(1));
        }
        [Test] public void SimultaneousContactsUseSpawnSequenceAndMarkGraceRejectedCars()
        {
            var later=Rent(PoolKey.Stationary,10,sequence:200); var earlier=Rent(PoolKey.Moving,10,sequence:100);
            // Equal world speed for an exact initial-overlap tie.
            traffic.ReleaseAll(); later=Rent(PoolKey.Stationary,0,sequence:200); earlier=Rent(PoolKey.Stationary,0,sequence:100);
            long damaged=-1; manager.Session.Damaged+=e=>damaged=e.Encounter.SpawnSequence;
            loop.Step(0.02f); Assert.That(damaged,Is.EqualTo(100)); Assert.That(later.Encountered && earlier.Encountered,Is.True); Assert.That(loop.Sensor.TotalContacts,Is.EqualTo(2));
        }
        [Test] public void ContactsAtDifferentTimesInOneLongTickCanOutlastGrace()
        {
            Rent(PoolKey.Stationary,5,sequence:100); Rent(PoolKey.Stationary,45,sequence:200); int events=0; manager.Session.Damaged+=_=>events++;
            loop.Step(2.5f); Assert.That(events,Is.EqualTo(2)); Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(1));
        }
        [Test] public void SweptContactsAtFiftyWithBothMoversCrossDisjointEndpoints()
        {
            var moving=Rent(PoolKey.Moving,10); manager.Session.SetCruiseSpeed(50f); loop.Step(1f);
            Assert.That(moving.Encountered,Is.True); Assert.That(moving.AbsoluteZ,Is.EqualTo(18d)); Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(2));
        }
        [Test] public void BentLanePathSweepsEachLegInsteadOfAChord()
        {
            loop.Player.RequestLane(1); loop.Player.RequestLane(-1);
            var car=Rent(PoolKey.Stationary,6.16d,1); loop.Step(0.56f);
            Assert.That(loop.Player.CurrentPosition.x,Is.EqualTo(0f)); Assert.That(loop.Player.MotionLegCount,Is.EqualTo(2));
            Assert.That(car.Encountered,Is.True); Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(2));
        }
        [Test] public void ReturnedAndRerentedBetweenCollectionAndDispatchCannotReceiveStaleContact()
        {
            var item=Rent(PoolKey.Stationary,0); ulong old=item.ActivationId;
            loop.Player.PredictStep(0.02f,22f); traffic.PredictTraffic(0.02f,0d); loop.Sensor.CollectContacts();
            Assert.That(loop.Sensor.ContactCount,Is.EqualTo(1)); item.ReturnToPool(old); var reused=Rent(PoolKey.Stationary,300);
            Assert.That(reused,Is.SameAs(item)); loop.Sensor.DispatchContacts(manager.Session,0.02f);
            Assert.That(reused.Encountered,Is.False); Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(3));
        }
        [Test] public void CallbackReturnsAreDeferredAndLifecycleReentryIsRejected()
        {
            var item=Rent(PoolKey.Stationary,0); ulong captured=item.ActivationId; bool callback=false;
            manager.Session.Damaged+=_=>
            {
                callback=true; Assert.That(traffic.IsDispatching,Is.True); Assert.That(item.ReturnToPool(captured),Is.True);
                Assert.That(item.IsRented,Is.True); Assert.That(traffic.ActiveCount,Is.EqualTo(1));
                Assert.That(manager.Pause() || manager.EndRun(GameOverReason.Lives) || manager.Restart() || loop.Step(0.1f),Is.False);
            };
            loop.Step(0.02f); Assert.That(callback,Is.True); Assert.That(item.IsRented,Is.False); Assert.That(traffic.IsDispatching,Is.False);
            Assert.That(manager.State,Is.EqualTo(GameState.Playing));
        }
        [Test] public void ContactOverflowFailsVisibly()
        {
            Rent(PoolKey.Stationary,0,sequence:1); Rent(PoolKey.Stationary,0,sequence:2);
            var field=typeof(InteractionSensor).GetField("contacts",BindingFlags.Instance|BindingFlags.NonPublic);
            field.SetValue(loop.Sensor,Array.CreateInstance(field.FieldType.GetElementType(),1));
            loop.Player.PredictStep(0.02f,22f); traffic.PredictTraffic(0.02f,0d);
            Assert.Throws<InvalidOperationException>(()=>loop.Sensor.CollectContacts());
        }
        [Test] public void FuelMutationClampsAndThreeHitsResolveLivesOnce()
        {
            manager.Session.SetResources(3,95f); loop.Survival.AddFuel(30f); Assert.That(manager.Session.Snapshot.Fuel,Is.EqualTo(100f));
            Assert.Throws<ArgumentOutOfRangeException>(()=>loop.Survival.AddFuel(-1));
            for (int i=0;i<3;i++) { Rent(PoolKey.Stationary,manager.Session.Snapshot.Distance,sequence:10000+i); loop.Step(1.1f); }
            Assert.That(manager.Session.Snapshot.Lives,Is.Zero); Assert.That(manager.State,Is.EqualTo(GameState.GameOver)); Assert.That(manager.EndReason,Is.EqualTo(GameOverReason.Lives));
        }
        [Test] public void ExhaustionOmitsWithoutGrowthRetryOrChangedPlans()
        {
            var plan=traffic.PlanAt(1); ulong created=manager.Pools.TotalCreated;
            var held=new List<TrafficLease>();
            foreach (var key in new[] { PoolKey.Stationary,PoolKey.Moving })
                while (manager.Pools.TryRent(new SpawnContext(key,Vector3.forward*5000,Quaternion.identity,absoluteDistance:5000),out var item)) held.Add(new TrafficLease((Interactable)item,item.ActivationId));
            loop.Step(20f); Assert.That(traffic.Omitted,Is.GreaterThan(0)); Assert.That(CountCars(),Is.Zero);
            Assert.That(traffic.PlanAt(1),Is.EqualTo(plan)); Assert.That(manager.Pools.TotalCreated,Is.EqualTo(created));
            ulong misses=manager.Pools.Misses; foreach (var lease in held) lease.Item.ReturnToPool(lease.ActivationId);
            loop.Step(0.02f); Assert.That(manager.Pools.Misses,Is.EqualTo(misses)); Assert.That(CountCars(),Is.Zero);
            Assert.That(traffic.DiagnosticCount,Is.InRange(1,64));
        }
        [Test] public void PickupCategoryWinsExactTieAndDefersItsReturn()
        {
            manager.Shutdown(); Object.DestroyImmediate(root);
            var catalog=ScriptableObject.CreateInstance<PoolConfig>();
            var original=UnityEditor.AssetDatabase.LoadAssetAtPath<PoolConfig>("Assets/NeonTunnelRunner/Configs/Pools.asset");
            var entries=new PoolConfig.Entry[original.EntryCount+1];
            for (int i=0;i<original.EntryCount;i++) entries[i]=original.GetEntry(i);
            var template=new GameObject("InteractionProbeTemplate"); template.SetActive(false); template.AddComponent<InteractionProbe>();
            entries[entries.Length-1]=new PoolConfig.Entry(PoolKey.Spark,template,1);
            typeof(PoolConfig).GetField("entries",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(catalog,entries);
            try
            {
                root=new GameObject("CategoryFixture"); root.SetActive(false);
                manager=root.AddComponent<GameManager>(); loop=root.AddComponent<GameplayLoop>();
                Assign(manager,"gameplayConfig",config); Assign(manager,"gameplayLoop",loop); Assign(loop,"gameManager",manager);
                PlayModeMovementRig.Wire(root,manager,loop,config); Assign(manager.Pools,"configuration",catalog);
                root.SetActive(true); Assert.That(manager.StartRun(),Is.True); traffic=loop.Planner; traffic.ReleaseAll();
                var obstacle=Rent(PoolKey.Stationary,0,sequence:1);
                var pickup=(InteractionProbe)Rent(PoolKey.Spark,0,sequence:999);
                var order=new List<string>();
                pickup.Interacted=context=>{ order.Add("pickup"); Assert.That(pickup.ReturnToPool(context.Encounter.ActivationId),Is.True); Assert.That(pickup.IsRented,Is.True); };
                manager.Session.Damaged+=_=>order.Add("damage");
                loop.Step(0.02f); CollectionAssert.AreEqual(new[] { "pickup","damage" },order);
                Assert.That(pickup.IsRented,Is.False); Assert.That(obstacle.Encountered,Is.True); Assert.That(loop.Sensor.Capacity,Is.EqualTo(281));
            }
            finally { manager.Shutdown(); Object.DestroyImmediate(template); Object.DestroyImmediate(catalog); }
        }
        private int CountCars()
        { int count=0; for (int i=0;i<traffic.ActiveCount;i++) if (traffic.GetActive(i).Item.Category==InteractionCategory.Obstacle) count++; return count; }
        private static void Assign(object target,string field,object value) => target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);

        [Test] public void ContactOnRebaseTickDoesNotCreateAdditionalHits()
        {
            loop.Step(45f); traffic.ReleaseAll(); var item=Rent(PoolKey.Moving,1000d); int events=0; manager.Session.Damaged+=_=>events++;
            manager.Session.SetCruiseSpeed(50f); loop.Step(0.3f);
            Assert.That(item.Encountered,Is.True); Assert.That(events,Is.EqualTo(1)); Assert.That(loop.Origin.RebaseCount,Is.EqualTo(1));
            Assert.That(item.AbsoluteZ,Is.EqualTo(1002.4d).Within(0.00001d));
            loop.Step(0.02f); Assert.That(events,Is.EqualTo(1));
        }
        [Test] public void FiftyResetsWithTrafficPauseGameOverAndRebasesLeaveNoOldWork()
        {
            for (int cycle=0;cycle<50;cycle++)
            {
                SurvivalRoute.Advance(loop,48f); SurvivalRoute.Advance(loop,48f); Assert.That(loop.Origin.RebaseCount,Is.GreaterThanOrEqualTo(2));
                traffic.ReleaseAll(); var item=Rent(PoolKey.Stationary,manager.Session.Snapshot.Distance,loop.Player.LaneIndex); ulong token=item.ActivationId;
                loop.Step(0.02f); Assert.That(item.Encountered,Is.True);
                if (cycle%2==0) manager.Pause(); else manager.EndRun(GameOverReason.Lives);
                Assert.That(manager.Restart(),Is.True); Assert.That(item.ReturnToPool(token),Is.False);
                Assert.That(loop.Origin.Offset,Is.Zero); Assert.That(loop.Sensor.ContactCount,Is.Zero); Assert.That(loop.Survival.GraceRemaining,Is.Zero);
                Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(3)); Assert.That(traffic.ActiveCount,Is.GreaterThan(0));
                Assert.That(manager.Pools.TotalCreated,Is.EqualTo(304)); Assert.That(manager.Pools.TotalDestroyed,Is.Zero);
            }
            manager.Pause(); manager.ReturnToMenu(); Assert.That(traffic.ActiveCount,Is.Zero); Assert.That(manager.Pools.ActiveCount,Is.Zero);
        }
    }
}
