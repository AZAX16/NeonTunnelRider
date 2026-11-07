using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class VfxPoolingTests
    {
        GameObject root; GameplayConfig gameplay; GameManager manager; GameplayLoop loop; VfxController vfx;
        float oldScale;
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
        [SetUp] public void Setup()
        {
            oldScale=Time.timeScale;gameplay=ScriptableObject.CreateInstance<GameplayConfig>();
            root=PlayModeMovementRig.Create("VfxFixture",gameplay);manager=root.GetComponent<GameManager>();loop=manager.Loop;
            var cameraObject=new GameObject("VfxCamera");cameraObject.transform.SetParent(root.transform,false);
            var camera=cameraObject.AddComponent<ChaseCamera>();var lens=cameraObject.AddComponent<Camera>();lens.enabled=false;lens.aspect=16f/9f;
            Set(camera,"gameManager",manager);Set(camera,"player",loop.Player);Set(camera,"configuration",loop.Visuals.Configuration);Set(camera,"viewCamera",lens);Set(camera,"worldOrigin",loop.Origin);
            Set(loop,"chaseCamera",camera);Set(loop.Origin,"chaseCamera",camera.transform);camera.Initialize(manager.Session);
            var go=new GameObject("VfxOwner");go.transform.SetParent(root.transform,false);vfx=go.AddComponent<VfxController>();var director=go.AddComponent<FireworkDirector>();
            var active=new GameObject("ActiveVFX");active.transform.SetParent(root.transform,false);
            Set(vfx,"gameManager",manager);Set(vfx,"pools",manager.Pools);Set(vfx,"origin",loop.Origin);Set(vfx,"activeRoot",active.transform);Set(vfx,"configuration",loop.Visuals.Configuration);Set(vfx,"fireworks",director);
            var exhaust=new List<ParticleSystem>();var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NeonTunnelRunner/Prefabs/Player/PlayerHovercar.prefab");
            foreach(string side in new[]{"Left","Right"})foreach(string kind in new[]{"Flame","Smoke"})
                exhaust.Add(Object.Instantiate(prefab.transform.Find("Visual/EffectAnchors/Exhaust"+side+"/"+kind).gameObject,loop.Visuals.VisualPivot).GetComponent<ParticleSystem>());
            Set(vfx,"exhaust",exhaust.ToArray());
            Set(director,"gameManager",manager);Set(director,"roads",loop.Roads);Set(director,"origin",loop.Origin);Set(director,"chaseCamera",camera);Set(director,"effects",vfx);Set(director,"configuration",loop.Visuals.Configuration);
            Set(loop,"vfx",vfx);Set(loop.Origin,"additionalParticipants",new MonoBehaviour[]{loop.Planner,camera,vfx});
            vfx.Initialize(manager.Session);Assert.That(manager.StartRun(),Is.True);
        }
        [TearDown] public void Cleanup(){if(root)Object.DestroyImmediate(root);if(gameplay)Object.DestroyImmediate(gameplay);Time.timeScale=oldScale;}
        PooledVfx Rent(PoolKey key,long sequence=1)
        {
            Assert.That(manager.Pools.TryRent(new SpawnContext(key,new Vector3(0,1,10),Quaternion.identity,vfx.OriginRoot,seed:123,spawnSequence:sequence),out var o),Is.True);
            var p=(PooledVfx)o;p.Begin();return p;
        }
        static readonly PoolKey[] Keys={PoolKey.Spark,PoolKey.CoinBurst,PoolKey.FuelBurst,PoolKey.Firework};
        [TestCase(PoolKey.Spark)] [TestCase(PoolKey.CoinBurst)] [TestCase(PoolKey.FuelBurst)] [TestCase(PoolKey.Firework)]
        public void ThousandRentReturnsResetTokensParticlesTrailsAndMaterials(PoolKey key)
        {
            ulong created=manager.Pools.TotalCreated,last=0;var ids=new HashSet<PooledVfx>();
            for(int i=0;i<1000;i++)
            {
                var effect=Rent(key,i);ids.Add(effect);Assert.That(effect.ActivationId,Is.GreaterThan(last));ulong token=effect.ActivationId;last=token;
                effect.Advance(.04f);var material=effect.Primary.GetComponent<ParticleSystemRenderer>().sharedMaterial;
                Assert.That(effect.ReturnToPool(token),Is.True);Assert.That(effect.ParticleCount,Is.Zero);Assert.That(effect.HasLiveState,Is.False);
                Assert.That(effect.gameObject.activeSelf,Is.False);Assert.That(effect.Primary.GetComponent<ParticleSystemRenderer>().sharedMaterial,Is.SameAs(material));
                var later=Rent(key,i+1);Assert.That(effect.ReturnToPool(token),Is.False);Assert.That(later.IsRented,Is.True);later.ReturnToPool(later.ActivationId);
            }
            Assert.That(ids.Count,Is.EqualTo(1));Assert.That(manager.Pools.TotalCreated,Is.EqualTo(created));Assert.That(manager.Pools.TotalDestroyed,Is.Zero);
        }
        [TestCase(PoolKey.Spark)] [TestCase(PoolKey.CoinBurst)] [TestCase(PoolKey.FuelBurst)] [TestCase(PoolKey.Firework)]
        public void SaturationOmitsCosmeticsAndNeverGrowsPool(PoolKey key)
        {
            manager.Pools.TryGetCounters(key,out var c);ulong created=manager.Pools.TotalCreated;var rentals=new List<PooledVfx>();
            for(int i=0;i<c.Capacity;i++)rentals.Add(Rent(key,i));
            Assert.That(vfx.TryPlay(key,Vector3.zero,0,1,1),Is.False);Assert.That(vfx.DroppedRequests,Is.EqualTo(1));
            manager.Pools.TryGetCounters(key,out c);Assert.That(c.Active,Is.EqualTo(c.Capacity));Assert.That(c.Misses,Is.EqualTo(1));
            foreach(var e in rentals)e.ReturnToPool(e.ActivationId);
            Assert.That(manager.Pools.TotalCreated,Is.EqualTo(created));Assert.That(manager.Pools.TotalDestroyed,Is.Zero);
        }
        [Test] public void RocketRisesBeforeDeathSubEmitterAndWaitsForChildAndPersistentTrail()
        {
            var e=Rent(PoolKey.Firework);var systems=e.GetComponentsInChildren<ParticleSystem>();var child=systems.First(s=>s!=e.Primary);
            Assert.That(e.Primary.particleCount,Is.EqualTo(1));Assert.That(child.particleCount,Is.Zero,"Burst must not play at launch.");
            var particles=new ParticleSystem.Particle[250];e.Primary.GetParticles(particles);var start=particles[0].position;
            e.Advance(.55f);e.Primary.GetParticles(particles);Assert.That(Vector3.Distance(start,particles[0].position),Is.GreaterThan(5));Assert.That(child.particleCount,Is.Zero);
            e.Advance(.7f);Assert.That(e.IsRented,Is.True);Assert.That(child.particleCount,Is.GreaterThan(0));Assert.That(e.ParticleCount,Is.LessThanOrEqualTo(250));
            e.Advance(.4f);Assert.That(e.IsRented,Is.True,"Child outlives rocket.");
            for(int i=0;i<150 && e.IsRented;i++)e.Advance(.02f);
            Assert.That(e.IsRented,Is.False);Assert.That(e.HasLiveState,Is.False);Assert.That(e.ParticleCount,Is.Zero);
        }
        Encounter Hit(long sequence=99)=>new Encounter(PoolKey.Stationary,(ulong)sequence,sequence,0,manager.Session.Snapshot.Distance,loop.Player.CurrentPosition,manager.Session.Snapshot.ElapsedTime);
        PooledObject Pickup(PoolKey key,long sequence)
        {
            Assert.That(manager.Pools.TryRent(new SpawnContext(key,loop.Player.CurrentPosition,Quaternion.identity,loop.Planner.OriginRoot,0,manager.Session.Snapshot.Distance,12345,0,sequence,loop.Planner),out var p),Is.True);return p;
        }
        [Test] public void AcceptedDamageGraceFullFuelAndDetachedPickupReuseProduceExactlyOneTypedEffect()
        {
            Assert.That(loop.Survival.TryDamage(Hit()),Is.True);Assert.That(loop.Survival.TryDamage(Hit(100)),Is.False);
            var coin=Pickup(PoolKey.Coin,900001);ulong coinToken=coin.ActivationId;loop.Step(.02f);loop.Player.HoldPose();
            Assert.That(vfx.PlayedSparks,Is.EqualTo(1));Assert.That(vfx.PlayedCoins,Is.EqualTo(1));Assert.That(manager.Session.Snapshot.Coins,Is.EqualTo(1));
            var detached=vfx.GetActive(0);Assert.That(detached.transform.parent,Is.EqualTo(vfx.OriginRoot));Assert.That(coin.IsRented,Is.False);
            manager.Session.SetResources(3,100);var fuel=Pickup(PoolKey.Fuel,900002);float applied=-1;manager.Session.Collected+=e=>{if(e.Kind==PoolKey.Fuel)applied=e.AppliedValue;};
            loop.Step(.02f);loop.Player.HoldPose();Assert.That(applied,Is.Zero);Assert.That(vfx.PlayedFuel,Is.EqualTo(1));
            var reused=Pickup(PoolKey.Coin,900003);Assert.That(reused,Is.SameAs(coin));Assert.That(reused.ActivationId,Is.Not.EqualTo(coinToken));
            Assert.That(vfx.ActiveCount,Is.EqualTo(3));Assert.That(detached.IsRented,Is.True);reused.ReturnToPool(reused.ActivationId);
        }
        [Test] public void QueuedEventSurvivesRebaseAndActiveLocalParticlesShiftExactlyOnce()
        {
            loop.Roads.EnsureCoverage(1005,0);loop.Player.SetAbsoluteForward(1005,0);loop.Player.HoldPose();
            manager.Session.PublishCollection(new CollectedEvent(manager.Session.Snapshot.RunId,new Encounter(PoolKey.Coin,90,90,0,1005,new Vector3(0,1.2f,1005),0),100,100));
            Assert.That(vfx.QueuedCount,Is.EqualTo(1));Assert.That(loop.Origin.RebaseIfNeeded(1005),Is.True);vfx.FlushRequests();
            var effect=vfx.GetActive(0);Assert.That(effect.transform.position.z,Is.EqualTo(45).Within(.001));
            var particles=new ParticleSystem.Particle[250];int count=effect.Primary.GetParticles(particles);var positions=particles.Take(count).Select(p=>p.position).ToArray();float age=effect.Age;var rootPosition=effect.transform.position;
            vfx.ShiftOrigin(1920,960);effect.Primary.GetParticles(particles);
            Assert.That(effect.transform.position.z,Is.EqualTo(rootPosition.z-960).Within(.001));Assert.That(effect.Age,Is.EqualTo(age));
            for(int i=0;i<count;i++)Assert.That(particles[i].position,Is.EqualTo(positions[i]));
            Assert.That(manager.Pools.StorageRoot.localPosition,Is.EqualTo(Vector3.zero));
        }
        [Test] public void TerminalInitialFeedbackFreezesAndFiftyRestartsClearEverything()
        {
            ulong created=manager.Pools.TotalCreated;
            for(int run=0;run<50;run++)
            {
                manager.Session.SetResources(1,10);Assert.That(loop.Survival.TryDamage(Hit()),Is.True);
                manager.EndRun(GameOverReason.Lives);Assert.That(vfx.PlayedSparks,Is.EqualTo(1));var e=vfx.GetActive(0);
                Assert.That(e.ParticleCount,Is.GreaterThan(0));float age=e.Age;vfx.Step(10);Assert.That(e.Age,Is.EqualTo(age));
                Assert.That(manager.Restart(),Is.True);Assert.That(vfx.ActiveCount,Is.Zero);Assert.That(vfx.QueuedCount,Is.Zero);
                foreach(var ps in vfx.Exhaust)Assert.That(ps.particleCount,Is.Zero);
                Assert.That(vfx.TryPlay(PoolKey.Firework,new Vector3(16,10,80),80,4,4),Is.True);
                manager.Pause();Assert.That(manager.Restart(),Is.True);Assert.That(vfx.ActiveCount,Is.Zero);
            }
            manager.Pause();manager.ReturnToMenu();Assert.That(manager.Pools.ActiveCount,Is.Zero);Assert.That(manager.Pools.TotalCreated,Is.EqualTo(created));Assert.That(manager.Pools.TotalDestroyed,Is.Zero);
        }
        [UnityTest] public IEnumerator TenSecondPausePreservesExhaustBurstRocketTrailsAndCountdown()
        {
            vfx.Step(.2f);vfx.TryPlay(PoolKey.CoinBurst,Vector3.zero,0,1,1);vfx.TryPlay(PoolKey.Firework,new Vector3(16,10,80),80,1,1);vfx.Step(.4f);
            var systems=vfx.OriginRoot.GetComponentsInChildren<ParticleSystem>().Concat(vfx.Exhaust).ToArray();
            var times=systems.Select(p=>p.time).ToArray();var counts=systems.Select(p=>p.particleCount).ToArray();float remaining=vfx.Fireworks.Remaining;
            manager.Pause();yield return new WaitForSecondsRealtime(10.05f);
            Assert.That(vfx.Fireworks.Remaining,Is.EqualTo(remaining));for(int i=0;i<systems.Length;i++){Assert.That(systems[i].time,Is.EqualTo(times[i]));Assert.That(systems[i].particleCount,Is.EqualTo(counts[i]));}
            manager.Resume();vfx.Step(.02f);Assert.That(vfx.Fireworks.Remaining,Is.LessThan(remaining));
        }
        [Test] public void TerminalSweptContactTickCommitsPickupRewardsAndSeedsAllFeedbackBeforeFreeze()
        {
            manager.Session.SetResources(1,10);Pickup(PoolKey.Coin,900071);Pickup(PoolKey.Fuel,900072);Pickup(PoolKey.Stationary,900073);
            Assert.That(loop.Step(.02f),Is.True);Assert.That(manager.State,Is.EqualTo(GameState.GameOver));
            Assert.That(manager.Session.Snapshot.Coins,Is.EqualTo(1));Assert.That(manager.Session.Snapshot.Fuel,Is.EqualTo(39.96f).Within(.001f));Assert.That(manager.Session.Snapshot.Lives,Is.Zero);
            Assert.That(vfx.PlayedCoins,Is.EqualTo(1));Assert.That(vfx.PlayedFuel,Is.EqualTo(1));Assert.That(vfx.PlayedSparks,Is.EqualTo(1));Assert.That(vfx.ActiveCount,Is.EqualTo(3));
            var ages=Enumerable.Range(0,vfx.ActiveCount).Select(i=>vfx.GetActive(i).Age).ToArray();
            Assert.That(Enumerable.Range(0,vfx.ActiveCount).All(i=>vfx.GetActive(i).ParticleCount>0),Is.True);
            vfx.Step(10);Assert.That(loop.Step(.02f),Is.False);
            for(int i=0;i<ages.Length;i++)Assert.That(vfx.GetActive(i).Age,Is.EqualTo(ages[i]));
            Assert.That(manager.Session.Snapshot.Coins,Is.EqualTo(1));Assert.That(vfx.Fireworks.Launches,Is.Zero);
        }
        [Test] public void SaturatedFeedbackDoesNotHoldPickupsOrChangeFullFuelRewardAndDamage()
        {
            ulong created=manager.Pools.TotalCreated;var held=new List<PooledVfx>();
            foreach(var key in new[]{PoolKey.Spark,PoolKey.CoinBurst,PoolKey.FuelBurst})
            {manager.Pools.TryGetCounters(key,out var c);for(int i=0;i<c.Capacity;i++)held.Add(Rent(key,i));}
            var coin=Pickup(PoolKey.Coin,900081);var fuel=Pickup(PoolKey.Fuel,900082);float applied=-1;
            manager.Session.Collected+=e=>{if(e.Kind==PoolKey.Fuel)applied=e.AppliedValue;};
            Assert.That(loop.Survival.TryDamage(Hit()),Is.True);loop.Step(.02f);
            Assert.That(manager.Session.Snapshot.Coins,Is.EqualTo(1));Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(2));Assert.That(applied,Is.Zero);
            Assert.That(coin.IsRented||fuel.IsRented,Is.False);Assert.That(vfx.ActiveCount,Is.Zero);Assert.That(vfx.DroppedRequests,Is.EqualTo(3));
            foreach(var e in held)e.ReturnToPool(e.ActivationId);Assert.That(manager.Pools.TotalCreated,Is.EqualTo(created));
        }
        [Test] public void RebasePreservesRocketParticlePosesAndBakedLocalTrailVertices()
        {
            loop.Roads.EnsureCoverage(1005,0);loop.Player.SetAbsoluteForward(1005,0);loop.Player.HoldPose();loop.Camera.ResetPresentation();
            vfx.TryPlay(PoolKey.Firework,new Vector3(16,10,1085),1085,3,3);vfx.Step(.3f);var effect=vfx.GetActive(0);
            var particles=new ParticleSystem.Particle[250];int count=effect.Primary.GetParticles(particles);var before=particles.Take(count).Select(p=>p.position).ToArray();float time=effect.Primary.time,age=effect.Age;
            var renderer=effect.Primary.GetComponent<ParticleSystemRenderer>();var mesh=new Mesh();var afterMesh=new Mesh();
            try
            {
                renderer.BakeTrailsMesh(mesh,loop.Camera.ViewCamera,(ParticleSystemBakeMeshOptions)0);var vertices=mesh.vertices;Assert.That(vertices.Length,Is.GreaterThan(0));
                Assert.That(loop.Origin.RebaseIfNeeded(1005),Is.True);renderer.BakeTrailsMesh(afterMesh,loop.Camera.ViewCamera,(ParticleSystemBakeMeshOptions)0);
                Assert.That(afterMesh.vertexCount,Is.EqualTo(vertices.Length));var after=afterMesh.vertices;
                for(int i=0;i<vertices.Length;i++)Assert.That(Vector3.Distance(vertices[i],after[i]),Is.LessThan(.002f));
                effect.Primary.GetParticles(particles);for(int i=0;i<count;i++)Assert.That(particles[i].position,Is.EqualTo(before[i]));
                Assert.That(effect.Primary.time,Is.EqualTo(time));Assert.That(effect.Age,Is.EqualTo(age));
            }
            finally {Object.DestroyImmediate(mesh);Object.DestroyImmediate(afterMesh);}
        }
        [Test] public void ExhaustUsesActualSpeedAndKeepsItsAuthoredBoundWhenPaused()
        {
            manager.Session.RecordMovement(.02f,1d);vfx.Step(.02f);
            Assert.That(vfx.Exhaust[0].emission.rateOverTime.constant,Is.EqualTo(64));
            Assert.That(vfx.Exhaust.Sum(p=>p.main.maxParticles),Is.EqualTo(160));
            manager.Pause();Assert.That(manager.Session.Snapshot.ActualSpeed,Is.Zero);
            Assert.That(vfx.Exhaust[0].emission.rateOverTime.constant,Is.EqualTo(24));
            var times=vfx.Exhaust.Select(p=>p.time).ToArray();vfx.Step(10);
            for(int i=0;i<times.Length;i++)Assert.That(vfx.Exhaust[i].time,Is.EqualTo(times[i]));
        }
        [Test] public void ContinuouslyOpenSkyScheduleUsesBoundsAndIndependentRandomSequence()
        {
            var random=UnityEngine.Random.state;var plan=loop.Planner.PlanAt(0);float first=vfx.Fireworks.Remaining;Assert.That(first,Is.InRange(2,4));
            var intervals=new List<double>();double last=0;
            for(int i=0;i<4000;i++)
            {
                manager.Session.Advance(.02f,0);vfx.Step(.02f);
                if(vfx.Fireworks.LastLaunchTime>last){intervals.Add(vfx.Fireworks.LastLaunchTime-last);last=vfx.Fireworks.LastLaunchTime;}
                Assert.That(vfx.ActiveFireworks,Is.LessThanOrEqualTo(2));
            }
            Assert.That(intervals.Count,Is.GreaterThanOrEqualTo(6));Assert.That(intervals[0],Is.InRange(2d,4.021d));foreach(var dt in intervals.Skip(1))Assert.That(dt,Is.InRange(8d,14.021d));
            Assert.That(UnityEngine.Random.state,Is.EqualTo(random));Assert.That(loop.Planner.PlanAt(0).Equals(plan),Is.True);
            manager.Pause();manager.Restart();Assert.That(vfx.Fireworks.Remaining,Is.EqualTo(first));
        }
        [Test] public void VisibleRoofSpanBlocksOnePendingLaunchUntilOpenAndCapDefersIt()
        {
            Assert.That(loop.Roads.IsOpenSkySpan(0,209),Is.True);Assert.That(loop.Roads.IsOpenSkySpan(200,220),Is.False);Assert.That(loop.Roads.IsOpenSkySpan(430,449),Is.False);Assert.That(loop.Roads.IsOpenSkySpan(450,479),Is.False,"Outside coverage fails closed.");
            // A real entrance/tunnel corridor blocks a due launch; exit/open coverage releases it.
            manager.Session.Advance(.02f,200d);loop.Roads.EnsureCoverage(200,0);loop.Player.SetAbsoluteForward(200,0);loop.Player.HoldPose();loop.Camera.ResetPresentation();
            Assert.That(loop.Roads.IsOpenSkySpan(192,320),Is.False);
            vfx.Step(20);Assert.That(vfx.Fireworks.Pending,Is.True);Assert.That(vfx.Fireworks.Launches,Is.Zero);
            vfx.Step(30);Assert.That(vfx.Fireworks.Launches,Is.Zero);
            manager.Session.Advance(.02f,280d);loop.Roads.EnsureCoverage(480,0);loop.Player.SetAbsoluteForward(480,0);loop.Player.HoldPose();loop.Camera.ResetPresentation();
            Assert.That(loop.Roads.IsOpenSkySpan(430,449),Is.False);Assert.That(loop.Roads.IsOpenSkySpan(450,600),Is.True);
            vfx.Step(.02f);Assert.That(vfx.Fireworks.Launches,Is.EqualTo(1));Assert.That(vfx.Fireworks.Pending,Is.False);
            vfx.TryPlay(PoolKey.Firework,new Vector3(-16,10,80),80,1,88);Assert.That(vfx.ActiveFireworks,Is.EqualTo(2));
            Set(vfx.Fireworks,"<Remaining>k__BackingField",0f);vfx.Fireworks.Step(.02f);Assert.That(vfx.ActiveFireworks,Is.EqualTo(2));Assert.That(vfx.Fireworks.Pending,Is.True);
        }
    }
}
