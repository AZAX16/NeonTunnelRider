using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace NeonTunnelRunner
{
    // Editor-only ephemeral fixture. Native FixedUpdates, real shared swept contacts, saved scene.
    public static class Task11Smoke
    {
        public static async Task<object> Run()
        {
            var manager=GameManager.Instance;
            Require(Application.isPlaying && manager && manager.State==GameState.Menu,"Enter saved Game in Menu.");
            string testKey="NTR.Tests.Task11.Smoke."+Guid.NewGuid().ToString("N");
            var isolatedStore=new HighScoreStore(testKey);
            typeof(GameManager).GetField("highScores",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,isolatedStore);
            var loop=manager.Loop; var actions=loop.Input.Actions;
            var oldInput=InputSystem.settings.editorInputBehaviorInPlayMode; var oldBackground=InputSystem.settings.backgroundBehavior;
            bool oldRunBackground=Application.runInBackground;
            var keyboard=InputSystem.AddDevice<Keyboard>("Task11SmokeKeyboard");
            var bindings=new List<object>(); var collections=new List<object>(); var milestones=new List<object>(); var damages=new List<object>(); var ends=new List<object>();
            int performed=0,coinCount=0,fuelCount=0,damageCount=0; Color baseline=ReadColor(loop.Visuals);
            Action<InputAction.CallbackContext> onLane=_=>performed++;
            Action<CollectedEvent> onCollect=e=> {
                if (e.Kind==PoolKey.Coin) coinCount++; else fuelCount++;
                collections.Add(new {kind=e.Kind.ToString(),e.Value,e.AppliedValue,e.AbsoluteZ,sequence=e.Encounter.SpawnSequence.ToString(),e.Encounter.PlayingTime}); };
            Action<DamageEvent> onDamage=e=> { damageCount++; damages.Add(new {e.RemainingLives,e.Encounter.PlayingTime,token=e.Encounter.ActivationId.ToString(),flash=loop.Visuals.IsFlashing}); };
            Action<RunSnapshot,GameOverReason> onEnd=(s,r)=>ends.Add(new {reason=r.ToString(),s.Lives,s.Fuel,s.Distance,s.ElapsedTime,s.Coins,s.Score,s.BestScore,s.Difficulty,s.AllowedSlotOccupancy,s.ActualSpeed,s.CruiseSpeed,neonLight=loop.Visuals.UnderglowLight.intensity,neonEmission=loop.Visuals.EmissionMultiplier,flash=loop.Visuals.FlashRemaining});
            var left=actions.FindAction("Gameplay/Left",true); var right=actions.FindAction("Gameplay/Right",true);
            try
            {
                Application.runInBackground=true;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                actions.devices=new InputDevice[] {keyboard}; left.performed+=onLane; right.performed+=onLane;
                manager.Session.Collected+=onCollect; manager.Session.Damaged+=onDamage; manager.RunEnded+=onEnd;
                Context(manager,"ContextStart");
                foreach (Key key in new[] {Key.A,Key.D,Key.LeftArrow,Key.RightArrow})
                {
                    int target=key==Key.A || key==Key.LeftArrow ? -1 : 1; int before=performed; ulong tick=loop.TickIndex;
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)); await Until(()=>loop.Player.LaneIndex==target && !loop.Player.IsTransitioning);
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState()); await Task.Delay(60);
                    Require(loop.TickIndex>tick && performed==before+1,"Binding must advance native ticks exactly once.");
                    bindings.Add(new {key=key.ToString(),lane=loop.Player.LaneIndex,tick=loop.TickIndex,callbacks=performed-before});
                    manager.Pause(); Context(manager,"ContextRestart");
                }
                // Accept one real obstacle during a lane move; pause freezes the visible flash for V12's full ten wall seconds.
                ScreenCapture.CaptureScreenshot("Evidence/Task11/native-baseline.png"); await Task.Delay(80);
                Rent(manager,PoolKey.Stationary,900000); InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));
                await Until(()=>damageCount==1 && loop.Player.IsTransitioning); manager.Pause(); InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                CheckNeon(manager); var paused=manager.Session.Snapshot; Require(paused.ActualSpeed==0f && paused.BestScore>=paused.Score,"Paused best/speed incoherent."); var playerPose=loop.Player.CurrentPosition; double grace=loop.Survival.GraceRemaining,flash=loop.Visuals.FlashRemaining;
                var camera=loop.Camera; var cameraPose=camera.transform.position; var cameraBase=camera.BasePosition;
                var impulse=camera.ImpulseOffset; var visualPose=loop.Visuals.VisualPivot.localPosition; var visualRotation=loop.Visuals.VisualPivot.localRotation;
                Require(impulse.magnitude>0f && impulse.magnitude<=.120001f,"Accepted native hit must create a bounded impulse.");
                Color hitColor=ReadColor(loop.Visuals); Require(hitColor!=baseline && flash>0d,"Accepted hit must visibly tint the body.");
                var traffic=new List<Vector3>(); for (int i=0;i<loop.Planner.ActiveCount;i++) traffic.Add(loop.Planner.GetActive(i).Item.CurrentPosition);
                ScreenCapture.CaptureScreenshot("Evidence/Task11/native-damage-paused.png");
                DateTime pausedAt=DateTime.UtcNow; await Task.Delay(10100);
                Require(manager.Session.Snapshot.Equals(paused) && loop.Player.CurrentPosition==playerPose && loop.Survival.GraceRemaining==grace && loop.Visuals.FlashRemaining==flash && ReadColor(loop.Visuals)==hitColor,"V12 pause changed values/flash.");
                for (int i=0;i<traffic.Count;i++) Require(loop.Planner.GetActive(i).Item.CurrentPosition==traffic[i],"Pause moved traffic.");
                Require(camera.transform.position==cameraPose && camera.BasePosition==cameraBase && camera.ImpulseOffset==impulse &&
                    loop.Visuals.VisualPivot.localPosition==visualPose && loop.Visuals.VisualPivot.localRotation==visualRotation,"Pause changed presentation.");
                double pauseWallSeconds=(DateTime.UtcNow-pausedAt).TotalSeconds;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape)); await Until(()=>manager.State==GameState.Playing);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); await Until(()=>!loop.Player.IsTransitioning && !loop.Visuals.IsFlashing);
                Require(loop.Player.LaneIndex==1 && loop.Player.QueuedCommand==0 && ReadColor(loop.Visuals)==baseline,"Resume/reset flash or lane queue failed.");
                ScreenCapture.CaptureScreenshot("Evidence/Task11/native-damage-resumed.png"); await Task.Delay(100);
                manager.Pause(); Context(manager,"ContextRestart");
                // Natural safe-route supply at cap speed with drain/damage fully enabled.
                Set(manager.Session,"Advance",180f,0d); Set(manager.Session,"SetResources",3,20f);
                int firstFrame=Time.frameCount,startCoins=coinCount,startFuel=fuelCount,capture=0; double[] targets={125d,450d,1005d,2020d};
                bool held=false; int commanded=0; var deadline=DateTime.UtcNow.AddSeconds(100);
                while (manager.Session.Snapshot.Distance<2020d || capture<targets.Length)
                {
                    Require(DateTime.UtcNow<deadline && manager.State==GameState.Playing,"Natural route stopped/timed out.");
                    var s=manager.Session.Snapshot; var plan=loop.Planner.PlanAt((long)(s.Distance/480d));
                    int desired=s.Distance-plan.AbsoluteStart>=395d ? plan.NextProtectedLane : plan.ProtectedLane;
                    if (held && !loop.Player.IsTransitioning && loop.Player.LaneIndex==commanded)
                    { InputSystem.QueueStateEvent(keyboard,new KeyboardState()); held=false; await Task.Delay(30); }
                    else if (!held && !loop.Player.IsTransitioning && loop.Player.LaneIndex!=desired)
                    { int direction=Math.Sign(desired-loop.Player.LaneIndex); commanded=loop.Player.LaneIndex+direction; InputSystem.QueueStateEvent(keyboard,new KeyboardState(direction<0 ? Key.A : Key.D)); held=true; }
                    if (capture<targets.Length && s.Distance>=targets[capture])
                    {
                        float seam=0; for (int i=1;i<loop.Roads.ActiveCount;i++) seam=Mathf.Max(seam,Vector3.Distance(loop.Roads.GetLease(i-1).Segment.Exit.position,loop.Roads.GetLease(i).Segment.Entry.position));
                        Require(seam<.001f && loop.Roads.CoverageEnd>=s.Distance+300d && loop.Roads.CoverageStart<=s.Distance-60d,"Coverage/seam failed.");
                        CheckNeon(manager); milestones.Add(new {neonLight=loop.Visuals.UnderglowLight.intensity,neonEmission=loop.Visuals.EmissionMultiplier,target=targets[capture],s.Distance,s.ElapsedTime,s.Lives,s.Fuel,s.Coins,s.Score,s.BestScore,s.Difficulty,s.CruiseSpeed,s.ActualSpeed,s.AllowedSlotOccupancy,tick=loop.TickIndex,frame=Time.frameCount,rebases=loop.Origin.RebaseCount,offset=loop.Origin.Offset,seam});
                        ScreenCapture.CaptureScreenshot("Evidence/Task11/native-"+(int)targets[capture]+".png"); capture++;
                    }
                    await Task.Delay(20);
                }
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); manager.Pause();
                Require(Time.frameCount>firstFrame && loop.Origin.RebaseCount>=2 && manager.Session.Snapshot.Lives==3 && manager.Session.Snapshot.Fuel>0,"Natural survival/rebase failed.");
                Require(fuelCount-startFuel>=12 && coinCount-startCoins>=16,"Both actual pickup types must collect.");
                Require(manager.Session.Snapshot.Difficulty==1f && manager.Session.Snapshot.CruiseSpeed==50f,"Cap progression was lost.");
                var route=new {coins=coinCount-startCoins,fuel=fuelCount-startFuel,manager.Session.Snapshot.Fuel,manager.Session.Snapshot.Lives,loop.Origin.RebaseCount,loop.Planner.SpawnedFuel,loop.Planner.SpawnedMoving,loop.Planner.SpawnedStationary,loop.Planner.EligibleSlots,loop.Planner.ProposedTraffic,loop.Planner.RejectedTraffic,loop.Planner.TrafficPoolMisses,loop.Planner.MovingFallbacks};
                int routeBest=manager.Session.Snapshot.BestScore; Context(manager,"ContextRestart"); Require(manager.Session.Snapshot.BestScore==routeBest,"Paused restart lost best.");
                // Controlled Fuel terminal: native tick, no pickup encountered, real drain and evaluation.
                 Set(manager.Session,"SetResources",3,.04f);
                await Until(()=>manager.State==GameState.GameOver); Require(manager.EndReason==GameOverReason.Fuel,"Fuel cause failed.");
                ScreenCapture.CaptureScreenshot("Evidence/Task11/native-fuel-gameover.png"); await Task.Delay(100);
                var fuelEnd=manager.Session.Snapshot; ulong rents=manager.Pools.TotalRents;
                Require(!loop.Step(1f) && !manager.EndRun(GameOverReason.Lives) && manager.Session.Snapshot.Equals(fuelEnd) && manager.Pools.TotalRents==rents,"Terminal idempotence failed.");
                Context(manager,"ContextRestart"); Require(manager.Session.Snapshot.Fuel==100f && manager.Session.Snapshot.Lives==3,"GameOver restart failed.");
                 int priorDamage=damageCount;
                for (int i=0;i<3;i++)
                {
                    Rent(manager,PoolKey.Stationary,900100+i); await Until(()=>damageCount==priorDamage+i+1);
                    if (i<2) await Task.Delay(1100);
                }
                Require(manager.State==GameState.GameOver && manager.EndReason==GameOverReason.Lives && loop.Visuals.IsFlashing,"Three real hits must end Lives with frozen final flash.");
                var livesEnd=manager.Session.Snapshot; Color finalColor=ReadColor(loop.Visuals); double finalFlash=loop.Visuals.FlashRemaining;
                ScreenCapture.CaptureScreenshot("Evidence/Task11/native-lives-gameover.png"); await Task.Delay(200);
                Require(loop.Camera.ImpulseOffset.magnitude>0f && loop.Camera.ImpulseOffset.magnitude<=.120001f,"Final hit impulse missing.");
                Require(manager.Session.Snapshot.Equals(livesEnd) && ReadColor(loop.Visuals)==finalColor && loop.Visuals.FlashRemaining==finalFlash,"GameOver appearance progressed.");
                Context(manager,"ContextRestart"); Require(ReadColor(loop.Visuals)==baseline && !loop.Visuals.IsFlashing && manager.Session.Snapshot.Score==0 && manager.Session.Snapshot.Distance==0d,"Clean restart failed.");
                ScreenCapture.CaptureScreenshot("Evidence/Task11/native-restart.png"); await Task.Delay(80); manager.Pause(); Context(manager,"ContextReturnToMenu");
                Require(manager.State==GameState.Menu && manager.Pools.ActiveCount==0 && Time.timeScale==1f,"Menu cleanup failed.");
                Require(manager.Pools.TotalCreated==304 && manager.Pools.TotalDestroyed==0 && manager.Pools.Misses==0,"Pool growth/miss.");
                Context(manager,"ContextStart");
                var heldTraffic=new List<TrafficLease>(); var stablePlan=loop.Planner.PlanAt(1);
                foreach(var key in new[]{PoolKey.Stationary,PoolKey.Moving})
                    while(manager.Pools.TryRent(new SpawnContext(key,Vector3.forward*5000,Quaternion.identity,absoluteDistance:5000),out var item)) heldTraffic.Add(new TrafficLease((Interactable)item,item.ActivationId));
                Require(loop.Step(20f),"Saturation tick failed.");
                Require(loop.Planner.TrafficPoolMisses>0 && loop.Planner.PlanAt(1).Equals(stablePlan),"Saturation was hidden or changed plans.");
                var saturation=new {loop.Planner.EligibleSlots,loop.Planner.ProposedTraffic,loop.Planner.AcceptedTraffic,loop.Planner.RejectedTraffic,loop.Planner.SpawnedMoving,loop.Planner.SpawnedStationary,loop.Planner.TrafficPoolMisses,loop.Planner.MovingFallbacks,manager.Pools.TotalCreated};
                foreach(var lease in heldTraffic) lease.Item.ReturnToPool(lease.ActivationId);
                ulong misses=loop.Planner.TrafficPoolMisses; loop.Step(.02f);
                Require(loop.Planner.TrafficPoolMisses==misses && loop.Planner.PlanAt(1).Equals(stablePlan),"Pool release retried an omitted slot.");
                manager.Pause(); manager.Restart(); manager.Pause();
                return new {passed=true,width=Screen.width,height=Screen.height,exitWhilePaused=true,pauseWallSeconds,baseline=baseline.ToString(),hitColor=hitColor.ToString(),pausedImpulse=impulse.ToString("F5"),pausedVisual=visualPose.ToString("F5"),pausedRoll=visualRotation.eulerAngles.ToString("F3"),route,saturation,
                    bindings=bindings.ToArray(),collections=collections.ToArray(),milestones=milestones.ToArray(),damages=damages.ToArray(),ends=ends.ToArray(),
                    created=manager.Pools.TotalCreated,misses=manager.Pools.Misses,pausedRestart=true,gameOverRestart=true,menuCleanup=true};
            }
            finally
            {
                manager.Session.Collected-=onCollect; manager.Session.Damaged-=onDamage; manager.RunEnded-=onEnd;
                left.performed-=onLane; right.performed-=onLane; actions.devices=null; InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.editorInputBehaviorInPlayMode=oldInput; InputSystem.settings.backgroundBehavior=oldBackground;
                Application.runInBackground=oldRunBackground;
                isolatedStore.Flush(); PlayerPrefs.DeleteKey(testKey); PlayerPrefs.Save();
            }
        }
        private static void Rent(GameManager manager,PoolKey key,long sequence)
        {
            var loop=manager.Loop; var s=manager.Session.Snapshot;
            Require(manager.Pools.TryRent(new SpawnContext(key,loop.Player.CurrentPosition+Vector3.forward*3.9f,Quaternion.identity,loop.Planner.OriginRoot,loop.Player.LaneIndex,s.Distance+3.9d,s.Seed,0f,sequence,loop.Planner),out _),"Smoke rental failed.");
        }
        private static Color ReadColor(VehicleVisuals visuals)
        { var p=new MaterialPropertyBlock(); visuals.FlashRenderer.GetPropertyBlock(p); return p.HasColor("_BaseColor") ? p.GetColor("_BaseColor") : visuals.FlashRenderer.sharedMaterial.GetColor("_BaseColor"); }
        private static void Set(object target,string method,params object[] values) => target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,values);
        private static void Context(GameManager manager,string method) { Set(manager,method); CheckNeon(manager); }
        private static void CheckNeon(GameManager m)
        {
            var v=m.Loop.Visuals;float speed=m.Session.Snapshot.ActualSpeed;
            Require(Mathf.Abs(v.UnderglowLight.intensity-v.Configuration.UnderglowIntensity(speed))<.00001f && Mathf.Abs(v.EmissionMultiplier-v.Configuration.UnderglowEmission(speed))<.00001f,"Native neon differs from actual speed.");
            var block=new MaterialPropertyBlock();v.UnderglowRenderer.GetPropertyBlock(block);
            Require(Vector4.Distance(block.GetColor("_EmissionColor"),v.UnderglowRenderer.sharedMaterial.GetColor("_EmissionColor")*v.EmissionMultiplier)<.00001f,"Native emission property differs.");
            foreach(var light in m.Pools.StorageRoot.GetComponentsInChildren<Light>(true)) Require(!light.enabled && !light.gameObject.activeInHierarchy,"Storage light leak.");
        }
        private static void Require(bool condition,string message) { if (!condition) throw new InvalidOperationException(message); }
        private static async Task Until(Func<bool> condition)
        { for (int i=0;i<250;i++) { if (condition()) return; await Task.Delay(20); } throw new TimeoutException("Native condition failed."); }
    }
}
