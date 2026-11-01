using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace NeonTunnelRunner
{
    // Ephemeral Editor recipe: native actions and actual FixedUpdates in the saved scene.
    public static class Task07Smoke
    {
        public static async Task<object> Run()
        {
            var manager=GameManager.Instance;
            Require(Application.isPlaying && manager && manager.State==GameState.Menu,"Enter saved Game in Menu.");
            Require(manager.Pools.IsReady && manager.Pools.Capacity==304,"Expected 304 prewarmed instances.");
            var loop=manager.Loop; var actions=loop.Input.Actions;
            var previousInput=InputSystem.settings.editorInputBehaviorInPlayMode; var previousBackground=InputSystem.settings.backgroundBehavior;
            bool previousRunBackground=Application.runInBackground;
            var keyboard=InputSystem.AddDevice<Keyboard>("Task07SmokeKeyboard");
            var bindings=new List<object>(); var collections=new List<object>(); var milestones=new List<object>();
            int performed=0,coinCount=0,fuelCount=0; bool sawRefill=false;
            Action<InputAction.CallbackContext> onLane=_=>performed++;
            Action<CollectedEvent> collected=e=>
            {
                var snapshot=manager.Session.Snapshot;
                if (e.Kind==PoolKey.Coin) { coinCount++; Require(e.Value==100f && e.AppliedValue==100f,"Coin was not exactly 100 points."); }
                else { fuelCount++; if (e.AppliedValue>0f) sawRefill=true; Require(e.Value==30f,"Fuel refill value changed."); }
                Require(snapshot.Score==(int)Math.Floor(snapshot.Distance)+100*snapshot.Coins,"Event snapshot score mismatch.");
                collections.Add(new {kind=e.Kind.ToString(),e.Value,e.AppliedValue,e.AbsoluteZ,sequence=e.Encounter.SpawnSequence.ToString(),
                    e.Encounter.PlayingTime,snapshot.Coins,snapshot.Score,snapshot.Fuel,snapshot.Distance});
            };
            var left=actions.FindAction("Gameplay/Left",true); var right=actions.FindAction("Gameplay/Right",true);
            try
            {
                Application.runInBackground=true;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                actions.devices=new InputDevice[] {keyboard}; left.performed+=onLane; right.performed+=onLane; manager.Session.Collected+=collected;
                Context(manager,"ContextStart"); Require(manager.State==GameState.Playing,"Start context failed.");
                foreach (Key key in new[] {Key.A,Key.D,Key.LeftArrow,Key.RightArrow})
                {
                    int target=key==Key.A || key==Key.LeftArrow ? -1 : 1;
                    ulong tick=loop.TickIndex; int before=performed;
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));
                    await Until(()=>loop.Player.LaneIndex==target && !loop.Player.IsTransitioning);
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState()); await Task.Delay(80);
                    Require(loop.TickIndex>tick && performed==before+1,"Expected advancing native ticks and one binding callback.");
                    bindings.Add(new {key=key.ToString(),lane=loop.Player.LaneIndex,tick=loop.TickIndex,manager.Session.Snapshot.Distance,callbacks=performed-before});
                    manager.Pause(); Context(manager,"ContextRestart");
                }
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape)); await Until(()=>manager.State==GameState.Paused);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); ulong pausedTick=loop.TickIndex; var paused=manager.Session.Snapshot;
                await Task.Delay(180); Require(loop.TickIndex==pausedTick && manager.Session.Snapshot.Equals(paused),"Pause advanced data.");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape)); await Until(()=>manager.State==GameState.Playing && loop.TickIndex>pausedTick);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); Require(manager.Session.Snapshot.RunId==paused.RunId,"Resume reset run.");
                // Fixture-only resource/speed setup outside Assets. No shipping cheats or direct pose edits.
                typeof(RunSession).GetMethod("SetCruiseSpeed",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager.Session,new object[] {50f});
                typeof(RunSession).GetMethod("SetResources",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager.Session,new object[] {3,20f});
                int firstFrame=Time.frameCount, capture=0; double[] targets={44d,104d,125d,450d,1005d,2020d};
                bool keyHeld=false; int commandedLane=0; bool sawMoving=false,sawStationary=false;
                var deadline=DateTime.UtcNow.AddSeconds(120);
                while (manager.Session.Snapshot.Distance<2020d || capture<targets.Length)
                {
                    Require(DateTime.UtcNow<deadline,"Native pickup smoke timed out.");
                    var snapshot=manager.Session.Snapshot; long block=(long)(snapshot.Distance/480d); var plan=loop.Planner.PlanAt(block);
                    int desired=snapshot.Distance-plan.AbsoluteStart>=395d ? plan.NextProtectedLane : plan.ProtectedLane;
                    if (keyHeld && !loop.Player.IsTransitioning && loop.Player.LaneIndex==commandedLane)
                    { InputSystem.QueueStateEvent(keyboard,new KeyboardState()); keyHeld=false; await Task.Delay(40); }
                    else if (!keyHeld && !loop.Player.IsTransitioning && loop.Player.LaneIndex!=desired)
                    {
                        int direction=Math.Sign(desired-loop.Player.LaneIndex); commandedLane=loop.Player.LaneIndex+direction;
                        InputSystem.QueueStateEvent(keyboard,new KeyboardState(direction<0 ? Key.A : Key.D)); keyHeld=true;
                    }
                    for (int i=0;i<loop.Planner.ActiveCount;i++)
                    { var item=loop.Planner.GetActive(i).Item; sawMoving|=item is MovingCar; sawStationary|=item is StationaryCar; }
                    if (capture<targets.Length && snapshot.Distance>=targets[capture])
                    {
                        float seam=0f; var roads=loop.Roads;
                        for (int i=1;i<roads.ActiveCount;i++) seam=Mathf.Max(seam,Vector3.Distance(roads.GetLease(i-1).Segment.Exit.position,roads.GetLease(i).Segment.Entry.position));
                        Require(seam<.001f && roads.CoverageStart<=snapshot.Distance-60d && roads.CoverageEnd>=snapshot.Distance+300d,"Road coverage/seam failed.");
                        Require(Math.Abs(loop.Player.CurrentPosition.z-(snapshot.Distance-loop.Origin.Offset))<.001d,"Origin mismatch.");
                        milestones.Add(new {target=targets[capture],snapshot.Distance,snapshot.Score,snapshot.Coins,snapshot.Fuel,snapshot.Lives,
                            frame=Time.frameCount,tick=loop.TickIndex,rebases=loop.Origin.RebaseCount,offset=loop.Origin.Offset,localZ=loop.Player.CurrentPosition.z,
                            active=loop.Planner.ActiveCount,roads=roads.ActiveCount,seam,coinCount,fuelCount});
                        ScreenCapture.CaptureScreenshot("Evidence/Task07/native-"+(int)targets[capture]+".png"); capture++;
                    }
                    await Task.Delay(20);
                }
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); await Task.Delay(100);
                Require(Time.frameCount>firstFrame && loop.Origin.RebaseCount>=2,"Native frames and rebases must advance.");
                Require(coinCount>=16 && fuelCount>=12 && sawRefill,"Both natural pickup categories must actually reward.");
                Require(manager.Session.Snapshot.Lives==3 && sawMoving && sawStationary,"Protected route or real traffic regression.");
                Require(manager.Pools.TotalCreated==304 && manager.Pools.TotalDestroyed==0 && manager.Pools.Misses==0,"Unexpected pool growth/miss.");
                var world=new {coinCount,fuelCount,sawRefill,sawMoving,sawStationary,coinsSpawned=loop.Planner.SpawnedCoins,fuelSpawned=loop.Planner.SpawnedFuel,
                    stationary=loop.Planner.SpawnedStationary,moving=loop.Planner.SpawnedMoving,capacity=loop.Sensor.Capacity,created=manager.Pools.TotalCreated,
                    destroyed=manager.Pools.TotalDestroyed,misses=manager.Pools.Misses,peak=manager.Pools.PeakActive};
                manager.EndRun(GameOverReason.Fuel); Context(manager,"ContextRestart");
                Require(manager.Session.Snapshot.Coins==0 && manager.Session.Snapshot.Score==0 && manager.Session.Snapshot.Fuel==100f,"Restart resource reset failed.");
                manager.Pause(); Context(manager,"ContextReturnToMenu"); Require(manager.State==GameState.Menu && manager.Pools.ActiveCount==0,"Menu did not release active pickups.");
                return new {passed=true,world,bindings=bindings.ToArray(),collections=collections.ToArray(),milestones=milestones.ToArray(),
                    nativePauseResume=true,pausedRestart=true,gameOverRestart=true,menuCleanup=true};
            }
            finally
            {
                manager.Session.Collected-=collected; left.performed-=onLane; right.performed-=onLane; actions.devices=null; InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.editorInputBehaviorInPlayMode=previousInput; InputSystem.settings.backgroundBehavior=previousBackground;
                Application.runInBackground=previousRunBackground; if (manager.State==GameState.Playing) manager.Pause();
            }
        }
        private static void Context(GameManager manager,string name) => typeof(GameManager).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,null);
        private static void Require(bool condition,string message) { if (!condition) throw new InvalidOperationException(message); }
        private static async Task Until(Func<bool> condition)
        { for (int i=0;i<200;i++) { if (condition()) return; await Task.Delay(20); } throw new TimeoutException("Native action condition did not complete."); }
    }
}
