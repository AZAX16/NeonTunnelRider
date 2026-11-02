using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace NeonTunnelRunner
{
    public sealed class NeonLightingTests
    {
        GameObject root;GameplayConfig config;GameManager manager;GameplayLoop loop;VehicleVisuals visuals;float oldScale;
        static void Assign(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
        [SetUp] public void Setup()
        {
            oldScale=Time.timeScale;config=ScriptableObject.CreateInstance<GameplayConfig>();
            root=PlayModeMovementRig.Create("NeonFixture",config);manager=root.GetComponent<GameManager>();loop=manager.Loop;visuals=loop.Visuals;
            // Deliberately share the flash renderer to exercise merged ownership of independent fields.
            Assign(visuals,"underglowRenderer",visuals.FlashRenderer);
            foreach(string name in new[]{"leftHeadlight","rightHeadlight","underglowLight"})
            {
                var go=new GameObject(name);go.transform.SetParent(loop.Player.transform,false);var light=go.AddComponent<Light>();
                light.type=name=="underglowLight"?LightType.Point:LightType.Spot;Assign(visuals,name,light);
            }
            visuals.Initialize(manager.Session);Assert.That(manager.StartRun(),Is.True);
        }
        [TearDown] public void Cleanup() {if(root)Object.DestroyImmediate(root);if(config)Object.DestroyImmediate(config);Time.timeScale=oldScale;}
        void Check(float intensity,float multiplier)
        {
            Assert.That(visuals.UnderglowLight.intensity,Is.EqualTo(intensity).Within(.00001f));
            Assert.That(visuals.EmissionMultiplier,Is.EqualTo(multiplier).Within(.00001f));
            var b=new MaterialPropertyBlock();visuals.UnderglowRenderer.GetPropertyBlock(b);
            Color expected=visuals.UnderglowRenderer.sharedMaterial.GetColor("_EmissionColor")*multiplier;
            Assert.That(Vector4.Distance(b.GetColor("_EmissionColor"),expected),Is.LessThan(.00001f));
        }
        [Test] public void ProductionStatePathUpdatesNeonEvenWhenAnimationIsFrozen()
        {
            Check(1.488f,2.76f);manager.Session.Advance(180f,0);loop.Step(.02f);Check(3f,5f);
            visuals.Render(.1f);var pose=visuals.VisualPivot.localPosition;var rotation=visuals.VisualPivot.localRotation;
            Assert.That(manager.Pause(),Is.True);Check(.3f,1f);Assert.That(manager.Session.Snapshot.CruiseSpeed,Is.EqualTo(50));
            visuals.Render(0);Assert.That(visuals.VisualPivot.localPosition,Is.EqualTo(pose));Assert.That(visuals.VisualPivot.localRotation,Is.EqualTo(rotation));
            manager.Resume();Check(3,5);manager.EndRun(GameOverReason.Fuel);Check(.3f,1);
            manager.Restart();Check(1.488f,2.76f);manager.Pause();manager.ReturnToMenu();
            Assert.That(visuals.UnderglowLight.enabled||visuals.LeftHeadlight.enabled||visuals.RightHeadlight.enabled,Is.False);
        }
        [Test] public void MeasuredTravelControlsIntermediateAndClampedNeonIndependentlyOfCruiseTarget()
        {
            float[] actual={0f,12.5f,25f,75f},light={.3f,.975f,1.65f,3f},emission={1f,2f,3f,5f};
            for(int i=0;i<actual.Length;i++)
            {
                manager.Session.RecordMovement(.01f,actual[i]*.01d);
                visuals.Render(0f);
                Assert.That(manager.Session.Snapshot.CruiseSpeed,Is.EqualTo(22f));
                Check(light[i],emission[i]);
            }
        }
        [Test] public void FlashExpiryResetAndDisablePreserveLatestEmissionAndUnrelatedFields()
        {
            var renderer=visuals.FlashRenderer;var mat=renderer.sharedMaterial;Color authored=mat.GetColor("_EmissionColor");
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetFloat("_Smoothness",.123f);renderer.SetPropertyBlock(block);
            var hit=new Encounter(PoolKey.Stationary,1,1,0,0,loop.Player.CurrentPosition,manager.Session.Snapshot.ElapsedTime);
            Assert.That(loop.Survival.TryDamage(hit),Is.True);Assert.That(visuals.IsFlashing,Is.True);
            manager.Session.RecordMovement(.01f,.3d);visuals.Render(0f);Check(1.92f,3.4f);Assert.That(visuals.IsFlashing,Is.True);
            manager.Session.Advance(180f,0);loop.Step(.02f);Check(3,5);Assert.That(visuals.IsFlashing,Is.False);
            renderer.GetPropertyBlock(block);Assert.That(block.GetFloat("_Smoothness"),Is.EqualTo(.123f));
            Assert.That(mat.GetColor("_EmissionColor"),Is.EqualTo(authored));Assert.That(renderer.sharedMaterial,Is.SameAs(mat));
            manager.Pause();manager.Restart();Check(1.488f,2.76f);
            visuals.enabled=false;Assert.That(visuals.UnderglowLight.enabled,Is.False);visuals.enabled=true;visuals.Render(0);Check(1.488f,2.76f);
        }
        [Test] public void FinalHitFreezesFlashButImmediatelyUsesZeroSpeedNeon()
        {
            manager.Session.SetResources(1,10);manager.Session.Advance(180f,0);loop.Step(.02f);Check(3,5);
            var hit=new Encounter(PoolKey.Stationary,1,1,0,0,loop.Player.CurrentPosition,manager.Session.Snapshot.ElapsedTime);
            Assert.That(loop.Survival.TryDamage(hit),Is.True);manager.EndRun(GameOverReason.Lives);
            Check(.3f,1);Assert.That(visuals.IsFlashing,Is.True);double remaining=visuals.FlashRemaining;
            visuals.Render(10);Assert.That(visuals.FlashRemaining,Is.EqualTo(remaining));Check(.3f,1);
            manager.Restart();Check(1.488f,2.76f);Assert.That(visuals.IsFlashing,Is.False);
        }
        [Test] public void RebaseAndRepeatedRestartsKeepLightOwnershipAndPoolStorageDark()
        {
            ulong created=manager.Pools.TotalCreated;
            for(int run=0;run<20;run++)
            {
                Assert.That(loop.Roads.EnsureCoverage(1005,0),Is.True);loop.Player.SetAbsoluteForward(1005,0);loop.Player.HoldPose();
                var local=visuals.UnderglowLight.transform.localPosition;var before=visuals.UnderglowLight.transform.position;
                Assert.That(loop.Origin.RebaseIfNeeded(1005),Is.True);
                Assert.That(visuals.UnderglowLight.transform.localPosition,Is.EqualTo(local));Assert.That(visuals.UnderglowLight.transform.position.z,Is.EqualTo(before.z-960).Within(.001));
                int enabled=0;for(int i=0;i<loop.Roads.ActiveCount;i++)if(loop.Roads.GetLease(i).Segment.Streetlight && loop.Roads.GetLease(i).Segment.Streetlight.enabled)enabled++;
                Assert.That(enabled,Is.LessThanOrEqualTo(3));
                manager.Pause();manager.Restart();Check(1.488f,2.76f);
            }
            manager.Pause();manager.ReturnToMenu();
            foreach(var light in manager.Pools.StorageRoot.GetComponentsInChildren<Light>(true)) Assert.That(light.enabled&&light.gameObject.activeInHierarchy,Is.False);
            Assert.That(manager.Pools.TotalCreated,Is.EqualTo(created));Assert.That(manager.Pools.Misses,Is.Zero);
        }
    }
}
