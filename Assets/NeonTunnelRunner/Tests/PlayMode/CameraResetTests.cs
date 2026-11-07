using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class CameraResetTests
    {
        private GameObject root;
        private GameplayConfig gameplay;
        private GameManager manager;
        private GameplayLoop loop;
        private PlayerController player;
        private ChaseCamera camera;
        private VehicleVisuals visuals;
        private float oldScale;
        private static void Assign(object o,string field,object value) => o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
        [SetUp] public void SetUp()
        {
            oldScale=Time.timeScale;
            gameplay=ScriptableObject.CreateInstance<GameplayConfig>();
            root=PlayModeMovementRig.Create("CameraFixture",gameplay);
            manager=root.GetComponent<GameManager>(); loop=manager.Loop; player=loop.Player; visuals=loop.Visuals;
            var go=new GameObject("Camera"); go.transform.SetParent(root.transform,false);
            var lens=go.AddComponent<Camera>(); lens.enabled=false;
            camera=go.AddComponent<ChaseCamera>();
            Assign(camera,"gameManager",manager); Assign(camera,"player",player);
            Assign(camera,"configuration",visuals.Configuration); Assign(camera,"viewCamera",lens); Assign(camera,"worldOrigin",loop.Origin);
            Assign(loop,"chaseCamera",camera); Assign(loop.Origin,"chaseCamera",camera.transform);
            Assign(loop.Origin,"additionalParticipants",new MonoBehaviour[] {loop.Planner,camera});
            camera.Initialize(manager.Session);
            Assert.That(manager.StartRun(),Is.True);
        }
        [TearDown] public void TearDown()
        { if(root) Object.DestroyImmediate(root); if(gameplay) Object.DestroyImmediate(gameplay); Time.timeScale=oldScale; }
        private void Tick(float dt=.02f)
        { Assert.That(loop.Step(dt),Is.True); player.HoldPose(); }
        private void Render(float dt)
        { camera.Render(dt); visuals.Render(dt); }
        private static void Near(Vector3 a,Vector3 b,float epsilon=.0002f) => Assert.That(Vector3.Distance(a,b),Is.LessThan(epsilon));
        private void Hit(long token=1)
        {
            var e=new Encounter(PoolKey.Stationary,(ulong)token,token,player.LaneIndex,
                manager.Session.Snapshot.Distance,player.CurrentPosition,manager.Session.Snapshot.ElapsedTime);
            Assert.That(loop.Survival.TryDamage(e),Is.True);
        }

        [TestCase(30, false)] [TestCase(60, false)] [TestCase(144, false)]
        [TestCase(30, true)] [TestCase(60, true)] [TestCase(240, true)]
        public void RenderRatesKeepBoundedLagAndUseTheSameStablePose(int hz,bool cap)
        {
            if(cap) manager.Session.Advance(180f,0d);
            double fixedClock=0d; int tick=0; float maxLag=0;
            for(int frame=1;frame<=hz*3;frame++)
            {
                double renderTime=(double)frame/hz;
                while(fixedClock+.02d<=renderTime+1e-9)
                {
                    if(tick%18==0) player.RequestLane((tick/18)%2==0 ? 1 : -1);
                    if(tick%18==4) player.RequestLane((tick/18)%2==0 ? -1 : 1);
                    Tick(); fixedClock+=.02d; tick++;
                }
                Render(1f/hz);
                var p=camera.transform.position;
                Assert.That(float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z),Is.True);
                Assert.That(camera.BasePosition.z,Is.EqualTo(player.transform.position.z-8f).Within(.0001f));
                Near(camera.TargetPosition,player.transform.position);
                maxLag=Mathf.Max(maxLag,Mathf.Abs(camera.BasePosition.x-player.transform.position.x));
                Assert.That(maxLag,Is.LessThanOrEqualTo(camera.Configuration.MaximumLateralLag+.00001f));
                Assert.That(Mathf.Abs(visuals.Roll),Is.LessThanOrEqualTo(8f));
                Assert.That(Mathf.Abs(visuals.BobOffset),Is.LessThanOrEqualTo(.08001f));
            }
            for(int i=0;i<hz;i++) Render(1f/hz);
            Assert.That(Mathf.Abs(camera.BasePosition.x-player.transform.position.x),Is.LessThan(.0001f));
            TestContext.WriteLine($"hz={hz}, cap={cap}, maxLag={maxLag}, final={camera.BasePosition}, actual={manager.Session.Snapshot.ActualSpeed}");
        }
        [Test]
        public void PresentationOnOffPreservesAuthoritativeMotionColliderResourcesAndPlans()
        {
            var poses=new List<Vector3>(); var bounds=new List<Bounds>(); var snapshots=new List<RunSnapshot>();
            var plans=new List<BlockPlan>(); var contacts=new List<ulong>();
            for(int pass=0;pass<2;pass++)
            {
                if(pass==1) { manager.Pause(); manager.Restart(); camera.enabled=false; visuals.enabled=false; }
                manager.Session.Advance(180f,0d);
                manager.Session.SetResources(3,50f);
                foreach(var key in new[]{PoolKey.Coin,PoolKey.Fuel,PoolKey.Stationary})
                    Assert.That(manager.Pools.TryRent(new SpawnContext(key,player.CurrentPosition+Vector3.forward*3.9f,
                        Quaternion.identity,loop.Planner.OriginRoot,0,3.9d,12345,0f,990000+(long)key,loop.Planner),out _),Is.True);
                for(int i=0;i<140;i++)
                {
                    if(i%20==0) player.RequestLane((i/20)%2==0 ? 1 : -1);
                    if(i%20==5) player.RequestLane((i/20)%2==0 ? -1 : 1);
                    Tick(); Render(.02f);
                    var s=manager.Session.Snapshot; var box=player.GetComponent<BoxCollider>();
                    if(pass==0) { poses.Add(player.CurrentPosition); bounds.Add(box.bounds); snapshots.Add(s); plans.Add(loop.Planner.PlanAt(0)); contacts.Add(loop.Sensor.TotalContacts); }
                    else
                    {
                        Near(player.CurrentPosition,poses[i]); Assert.That(box.bounds,Is.EqualTo(bounds[i]));
                        Assert.That(loop.Sensor.TotalContacts,Is.EqualTo(contacts[i]));
                        var a=snapshots[i]; Assert.That(s.Distance,Is.EqualTo(a.Distance)); Assert.That(s.ElapsedTime,Is.EqualTo(a.ElapsedTime));
                        Assert.That(s.Fuel,Is.EqualTo(a.Fuel)); Assert.That(s.Lives,Is.EqualTo(a.Lives)); Assert.That(s.Score,Is.EqualTo(a.Score));
                        Assert.That(s.ActualSpeed,Is.EqualTo(a.ActualSpeed)); Assert.That(s.Coins,Is.EqualTo(a.Coins));
                    }
                    Assert.That(player.transform.rotation,Is.EqualTo(Quaternion.identity));
                    Assert.That(player.transform.localScale,Is.EqualTo(Vector3.one));
                    Assert.That(player.Body.interpolation,Is.EqualTo(RigidbodyInterpolation.None));
                    if(pass==1) Assert.That(loop.Planner.PlanAt(0),Is.EqualTo(plans[i]));
                    if(i==139) { Assert.That(s.Lives,Is.EqualTo(2)); Assert.That(s.Coins,Is.GreaterThanOrEqualTo(1)); Assert.That(loop.Sensor.TotalContacts,Is.GreaterThanOrEqualTo(3)); }
                }
            }
        }
        [Test] public void AcceptedDamageReleasesZoomSmoothlyWithoutChangingTheCameraBaseline()
        {
            manager.AcceptNearMiss(); manager.NearMiss.Advance(.2f,manager.Configuration); camera.Render(.016f);
            float zoom=camera.ViewCamera.fieldOfView; Assert.That(zoom,Is.EqualTo(58f).Within(.001));
            Hit(); Assert.That(camera.ViewCamera.fieldOfView,Is.EqualTo(zoom));
            camera.Render(.1f); Assert.That(camera.ViewCamera.fieldOfView,Is.GreaterThan(zoom).And.LessThan(65f));
            camera.Render(.4f); Assert.That(camera.ViewCamera.fieldOfView,Is.EqualTo(65f));
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(1)]
        public void OriginShiftsBaseTargetAndCurrentPoseExactlyOnceDuringMotionAndImpulse(int lane)
        {
            player.RequestLane(lane); Tick(.1f); Render(.016f); Hit(); Render(.016f);
            // Establish a legitimate local position at the threshold without changing lane history.
            player.SetAbsoluteForward(1005d,0d); player.HoldPose(); Render(.016f);
            var relative=camera.transform.position-player.transform.position;
            var baseBefore=camera.BasePosition; var targetBefore=camera.TargetPosition;
            var impulse=camera.ImpulseOffset; var pivot=visuals.VisualPivot.localPosition;
            var snapshot=manager.Session.Snapshot; var rotation=player.transform.rotation;
            Assert.That(loop.Origin.RebaseIfNeeded(1005d),Is.True);
            var shift=Vector3.back*960f;
            Near(camera.BasePosition,baseBefore+shift); Near(camera.TargetPosition,targetBefore+shift);
            Near(camera.transform.position-player.transform.position,relative);
            Near(camera.ImpulseOffset,impulse); Near(visuals.VisualPivot.localPosition,pivot);
            Assert.That(manager.Session.Snapshot,Is.EqualTo(snapshot)); Assert.That(player.transform.rotation,Is.EqualTo(rotation));
            Render(0f); Near(camera.transform.position-player.transform.position,relative);
            Render(.001f);
            Assert.That(Mathf.Abs(camera.BasePosition.z-(player.transform.position.z-8f)),Is.LessThan(.0001f));
        }
        [Test]
        public void AcceptedOnlyImpulseIsBoundedWithOverlapAndFreezesInPauseAndFinalHit()
        {
            Tick(); Hit(); Assert.That(camera.AcceptedImpulses,Is.EqualTo(1));
            // An encounter in grace does not emit accepted damage or extend either effect.
            var rejected=new Encounter(PoolKey.Stationary,2,2,0,0,player.CurrentPosition,manager.Session.Snapshot.ElapsedTime);
            Assert.That(loop.Survival.TryDamage(rejected),Is.False); Assert.That(camera.AcceptedImpulses,Is.EqualTo(1));
            // Typed accepted events may overlap: exercise the presenter's bound independently of grace policy.
            for(int i=0;i<20;i++) manager.Session.PublishDamage(new DamageEvent(manager.Session.Snapshot.RunId,rejected,2));
            Assert.That(camera.ImpulseOffset.magnitude,Is.LessThanOrEqualTo(.120001f)); Render(.01f);
            manager.Pause(); Time.timeScale=1f;
            var position=camera.transform.position; var pivot=visuals.VisualPivot.localPosition; var roll=visuals.Roll;
            double flash=visuals.FlashRemaining; var impulse=camera.ImpulseOffset;
            for(int i=0;i<100;i++) Render(10f);
            Near(camera.transform.position,position); Near(visuals.VisualPivot.localPosition,pivot); Near(camera.ImpulseOffset,impulse);
            Assert.That(visuals.Roll,Is.EqualTo(roll)); Assert.That(visuals.FlashRemaining,Is.EqualTo(flash));
            manager.Resume(); Render(0f); Near(camera.transform.position,position); Render(.001f);
            Assert.That(Vector3.Distance(camera.transform.position,position),Is.LessThan(.01f));
            for(int i=0;i<100;i++) Render(.01f);
            Near(camera.ImpulseOffset,Vector3.zero);
            manager.Pause(); manager.Restart(); manager.Session.SetResources(1,100f); Tick(); Hit(); manager.EndRun(GameOverReason.Lives);
            position=camera.transform.position; impulse=camera.ImpulseOffset; Assert.That(impulse.magnitude,Is.GreaterThan(0f));
            Render(100f); Near(camera.transform.position,position); Near(camera.ImpulseOffset,impulse); Assert.That(visuals.IsFlashing,Is.True);
        }
        [Test]
        public void AuthoredNeutralPivotIsRestoredAndFlashExpiryDoesNotResetMeshMotion()
        {
            var pivot=new GameObject("AuthoredPivot").transform; pivot.SetParent(player.transform,false);
            visuals.VisualPivot.SetParent(pivot,false);
            var position=new Vector3(.12f,.03f,-.07f); var rotation=Quaternion.Euler(2f,3f,4f); var scale=new Vector3(.95f,1f,1f);
            pivot.localPosition=position; pivot.localRotation=rotation; pivot.localScale=scale;
            Assign(visuals,"visualPivot",pivot); Assign(visuals,"neutralCached",false);
            visuals.Initialize(manager.Session); visuals.PrepareRun();
            player.RequestLane(1); Tick(.1f); Render(.05f); Hit();
            var animated=pivot.localPosition; var animatedRotation=pivot.localRotation;
            Tick(.3f); // Flash expires in the simulation phase, before another presentation frame.
            Assert.That(visuals.IsFlashing,Is.False); Near(pivot.localPosition,animated);
            Assert.That(pivot.localRotation,Is.EqualTo(animatedRotation));
            Assert.That(Mathf.Abs(pivot.localPosition.y-position.y),Is.LessThanOrEqualTo(.08f));
            visuals.ResetVisuals(); Near(pivot.localPosition,position);
            Assert.That(pivot.localRotation,Is.EqualTo(rotation)); Assert.That(pivot.localScale,Is.EqualTo(scale));
            Assert.That(player.transform.rotation,Is.EqualTo(Quaternion.identity));
        }
        [Test]
        public void PauseBetweenFixedAndLateUpdateHoldsFinalRootWithoutAdvancingFeedback()
        {
            Hit(); Render(.01f); var impulse=camera.ImpulseOffset;
            player.RequestLane(1); Tick(.1f); // No render update before state changes.
            manager.Pause();
            Near(camera.TargetPosition,player.transform.position);
            Assert.That(camera.BasePosition.z,Is.EqualTo(player.transform.position.z-8f).Within(.0001f));
            Assert.That(Mathf.Abs(camera.BasePosition.x-player.transform.position.x),Is.LessThanOrEqualTo(.65001f));
            Near(camera.ImpulseOffset,impulse); var position=camera.transform.position;
            Time.timeScale=1f; Render(100f); Near(camera.transform.position,position);
            manager.Resume(); Render(.000001f); Near(camera.transform.position,position);
        }
        [Test]
        public void FiftyRestartsAndEnableCyclesClearAllCosmeticStateAndKeepOneDamageListener()
        {
            var neutral=visuals.VisualPivot.localPosition; var rotation=visuals.VisualPivot.localRotation;
            for(int i=0;i<50;i++)
            {
                player.RequestLane(i%2==0?1:-1); Tick(.1f); Render(.05f);
                player.SetAbsoluteForward(1005d,0d); player.HoldPose(); Render(.01f); loop.Origin.RebaseIfNeeded(1005d);
                ulong before=camera.AcceptedImpulses, flashes=visuals.AcceptedFlashes;
                for(int j=0;j<3;j++) { camera.enabled=false; visuals.enabled=false; camera.enabled=true; visuals.enabled=true; }
                Hit(i+1); Assert.That(camera.AcceptedImpulses,Is.EqualTo(before+1)); Assert.That(visuals.AcceptedFlashes,Is.EqualTo(flashes+1));
                if(i%2==0) manager.Pause(); else manager.EndRun(GameOverReason.Lives);
                Assert.That(manager.Restart(),Is.True);
                Near(camera.BasePosition,player.transform.position+camera.Configuration.CameraOffset); Near(camera.ImpulseOffset,Vector3.zero);
                Near(camera.TargetPosition,player.transform.position); Near(visuals.VisualPivot.localPosition,neutral);
                Assert.That(visuals.VisualPivot.localRotation,Is.EqualTo(rotation)); Assert.That(visuals.Roll,Is.Zero);
                Assert.That(visuals.BobOffset,Is.Zero); Assert.That(visuals.IsFlashing,Is.False); Assert.That(loop.Origin.Offset,Is.Zero);
            }
            manager.Pause(); manager.ReturnToMenu();
            var handlers=(System.Delegate)typeof(RunSession).GetField("Damaged",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(manager.Session);
            if(handlers!=null) foreach(var handler in handlers.GetInvocationList())
                Assert.That(ReferenceEquals(handler.Target,camera) || ReferenceEquals(handler.Target,visuals),Is.False,"Menu must detach presentation observers.");
            Time.timeScale=1f; var pose=camera.transform.position;
            Render(100f); Near(camera.transform.position,pose); Near(visuals.VisualPivot.localPosition,neutral);
        }
    }
}
