using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace NeonTunnelRunner
{
    public sealed class VfxAssetTests
    {
        const string Root="Assets/NeonTunnelRunner/";
        [Test] public void EffectAssetsAreColliderFreeLocalAndWithinTheirParticleBudgets()
        {
            var config=AssetDatabase.LoadAssetAtPath<PoolConfig>(Root+"Configs/Pools.asset");
            for(int i=0;i<config.EntryCount;i++)
            {
                var e=config.GetEntry(i);if(e.Key<PoolKey.Spark)continue;
                Assert.That(e.Capacity,Is.EqualTo((int)e.Key<10?16:8));Assert.That(e.Prefab.GetComponent<PooledVfx>(),Is.Not.Null);
                Assert.That(e.Prefab.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(e.Prefab.GetComponentsInChildren<Light>(true),Is.Empty);
                var systems=e.Prefab.GetComponentsInChildren<ParticleSystem>(true);
                Assert.That(systems.Sum(p=>p.main.maxParticles),Is.LessThanOrEqualTo(e.Key==PoolKey.Firework?250:e.Key==PoolKey.Spark?40:24));
                foreach(var p in systems)
                {
                    Assert.That(p.main.playOnAwake||p.main.loop,Is.False);Assert.That(p.main.simulationSpace,Is.EqualTo(ParticleSystemSimulationSpace.Local));
                    Assert.That(p.main.stopAction,Is.EqualTo(ParticleSystemStopAction.None));Assert.That(p.trails.enabled&&p.trails.worldSpace,Is.False);
                    Assert.That(p.main.startLifetime.mode,Is.EqualTo(ParticleSystemCurveMode.Constant),"Completion horizon relies on authored constant lifetime bounds.");
                }
                if(e.Key==PoolKey.Firework)
                {
                    var rocket=e.Prefab.transform.Find("Rocket").GetComponent<ParticleSystem>();Assert.That(rocket.main.maxParticles,Is.EqualTo(1));
                    Assert.That(rocket.trails.enabled,Is.True);Assert.That(rocket.trails.dieWithParticles,Is.False);
                    Assert.That(rocket.subEmitters.subEmittersCount,Is.EqualTo(1));Assert.That(rocket.subEmitters.GetSubEmitterType(0),Is.EqualTo(ParticleSystemSubEmitterType.Death));
                    Assert.That(rocket.transform.forward.y,Is.GreaterThan(.99f));
                }
            }
        }
        [Test] public void SharedParticleMaterialsProvideHdrAdditiveAndAlphaBlendingWithoutDepthWrites()
        {
            foreach(var name in new[]{"ParticleAdditive","ParticleStreak","ParticleSmoke"})
            {
                var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/"+name+".mat");
                Assert.That(m.shader.name,Is.EqualTo("Universal Render Pipeline/Particles/Unlit"));Assert.That(m.shader.isSupported,Is.True);
                Assert.That(m.GetFloat("_Surface"),Is.EqualTo(1));Assert.That(m.GetFloat("_ZWrite"),Is.Zero);Assert.That(m.GetTexture("_BaseMap"),Is.Not.Null);
                Assert.That(m.GetFloat("_Blend"),Is.EqualTo(name=="ParticleSmoke"?0:2));
                if(name!="ParticleSmoke")Assert.That(m.GetColor("_BaseColor").maxColorComponent,Is.GreaterThan(1));
            }
        }
        [Test] public void ExhaustRemainsAttachedToBothExistingPivotsWithAtMost160Particles()
        {
            var player=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/Player/PlayerHovercar.prefab");var all=player.GetComponentsInChildren<ParticleSystem>(true);
            Assert.That(all.Length,Is.EqualTo(4));Assert.That(all.Sum(p=>p.main.maxParticles),Is.EqualTo(160));
            foreach(var side in new[]{"Left","Right"})foreach(var kind in new[]{"Flame","Smoke"})
            {
                var p=player.transform.Find("Visual/EffectAnchors/Exhaust"+side+"/"+kind).GetComponent<ParticleSystem>();
                Assert.That(p.main.playOnAwake,Is.False);Assert.That(p.main.loop,Is.True);Assert.That(p.main.simulationSpace,Is.EqualTo(ParticleSystemSimulationSpace.Local));Assert.That(p.transform.forward.z,Is.LessThan(-.99f));
            }
        }
    }
}
