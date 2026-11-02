using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace NeonTunnelRunner
{
    // Native ticks and actions in the real saved Game. No alternate scene or concept imagery.
    public static class Task10Visual
    {
        public static async Task<object> Run()
        {
            var m=GameManager.Instance; Require(m && m.State==GameState.Menu,"Start saved Game in Menu.");
            var l=m.Loop; var camera=l.Camera.ViewCamera;
            string key="NTR.Tests.Task10.Visual."+Guid.NewGuid().ToString("N");
            var store=new HighScoreStore(key); SetField(m,"highScores",store);
            bool background=Application.runInBackground; var behavior=InputSystem.settings.editorInputBehaviorInPlayMode;
            var focus=InputSystem.settings.backgroundBehavior; var keyboard=InputSystem.AddDevice<Keyboard>("Task10VisualKeyboard");
            var captures=new List<object>(); var rebaseFrames=new List<object>();
            try
            {
                Application.runInBackground=true;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                l.Input.Actions.devices=new InputDevice[]{keyboard};
                foreach(bool cap in new[]{false,true})
                {
                    if(m.State==GameState.Menu) m.StartRun(); else m.Restart();
                    if(cap) Call(m.Session,"Advance",180f,0d);
                    await Until(()=>l.TickIndex>3);
                    foreach(int lane in new[]{0,-1,1})
                    {
                        while(l.Player.LaneIndex!=lane)
                        {
                            InputSystem.QueueStateEvent(keyboard,new KeyboardState(lane<l.Player.LaneIndex?Key.A:Key.D));
                            await Until(()=>l.Player.IsTransitioning); InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                            await Until(()=>!l.Player.IsTransitioning); await Task.Delay(80);
                        }
                        await Task.Delay(180);
                        await Capture(m,(cap?"cap":"start")+"-open-lane"+lane,captures);
                        m.Resume();
                    }
                    int capture=0; double[] distances={180d,260d,430d,490d}; string[] labels={"entrance","tunnel","exit","open-after-exit"};
                    bool held=false; int commanded=0; long rebase=l.Origin.RebaseCount; bool hitScheduled=false;
                    var deadline=DateTime.UtcNow.AddSeconds(cap?35:35);
                    while(m.Session.Snapshot.Distance<(cap?1020d:495d))
                    {
                        Require(DateTime.UtcNow<deadline && m.State==GameState.Playing,"Native visual route stopped.");
                        var s=m.Session.Snapshot; var plan=l.Planner.PlanAt((long)(s.Distance/480d));
                        int desired=s.Distance-plan.AbsoluteStart>=395d?plan.NextProtectedLane:plan.ProtectedLane;
                        if(held && !l.Player.IsTransitioning && l.Player.LaneIndex==commanded)
                        { InputSystem.QueueStateEvent(keyboard,new KeyboardState()); held=false; }
                        else if(!held && !l.Player.IsTransitioning && l.Player.LaneIndex!=desired)
                        { commanded=l.Player.LaneIndex+Math.Sign(desired-l.Player.LaneIndex); InputSystem.QueueStateEvent(keyboard,new KeyboardState(desired<l.Player.LaneIndex?Key.A:Key.D)); held=true; }
                        if(capture<distances.Length && s.Distance>=distances[capture])
                        {
                            await Capture(m,(cap?"cap":"start")+"-"+labels[capture],captures);
                            m.Resume(); capture++;
                        }
                        if(cap && !hitScheduled && s.Distance>=994d)
                        {
                            Require(m.Pools.TryRent(new SpawnContext(PoolKey.Stationary,l.Player.CurrentPosition+Vector3.forward*3.9f,
                                Quaternion.identity,l.Planner.OriginRoot,l.Player.LaneIndex,s.Distance+3.9d,s.Seed,0f,990010,l.Planner),out _),"Rebase hit rental failed.");
                            l.Player.RequestLane(l.Player.LaneIndex==1?-1:1); // Existing command API; fixture only, simultaneous transition/rebase stress.
                            ScreenCapture.CaptureScreenshot("Evidence/Task10/cap-before-native-rebase.png");
                            hitScheduled=true;
                        }
                        if(cap && s.Distance>=993d && s.Distance<=1015d)
                        {
                            rebaseFrames.Add(new {frame=Time.frameCount,s.Distance,offset=l.Origin.Offset,
                                relative=(l.Camera.transform.position-l.Player.transform.position).ToString("F4"),
                                impulse=l.Camera.ImpulseOffset.ToString("F4")});
                            if(l.Origin.RebaseCount!=rebase)
                            { await Capture(m,"cap-after-native-rebase",captures); m.Resume(); rebase=l.Origin.RebaseCount; }
                        }
                        await Task.Delay(16);
                    }
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState()); m.Pause();
                }
                return new {passed=true,captures=captures.ToArray(),rebaseFrames=rebaseFrames.ToArray(),
                    nativeFrames=Time.frameCount,created=m.Pools.TotalCreated,misses=m.Pools.Misses};
            }
            finally
            {
                if(m.State==GameState.Playing) m.Pause();
                l.Input.Actions.devices=null; InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.editorInputBehaviorInPlayMode=behavior; InputSystem.settings.backgroundBehavior=focus;
                Application.runInBackground=background; store.Flush(); PlayerPrefs.DeleteKey(key); PlayerPrefs.Save();
            }
        }
        private static async Task Capture(GameManager m,string label,List<object> captures)
        {
            float actual=m.Session.Snapshot.ActualSpeed; m.Pause(); await Task.Delay(60);
            var l=m.Loop; var c=l.Camera.ViewCamera; var s=m.Session.Snapshot;
            var references=new List<TrafficLease>();
            if(label=="cap-open-after-exit")
            {
                for(int lane=-1;lane<=1;lane++)
                {
                    if(lane==l.Planner.PlanAt((long)((s.Distance+120d)/480d)).ProtectedLane) continue;
                    var position=new Vector3(lane*3.2f,.6f,l.Player.CurrentPosition.z+120f);
                    Require(m.Pools.TryRent(new SpawnContext(PoolKey.Stationary,position,Quaternion.identity,l.Planner.OriginRoot,lane,s.Distance+120d,s.Seed,0f,991000+lane),out var item),"120m reference rental failed.");
                    references.Add(new TrafficLease((Interactable)item,item.ActivationId));
                }
            }
            ScreenCapture.CaptureScreenshot("Evidence/Task10/"+label+".png"); await Task.Delay(100);
            foreach(var reference in references) reference.Item.ReturnToPool(reference.ActivationId);
            var p=l.Player.transform.position;
            var threats=new List<object>();
            foreach(float aspect in new[]{16f/9f,4f/3f})
            {
                float old=c.aspect; c.aspect=aspect;
                for(int lane=-1;lane<=1;lane++)
                {
                    var center=new Vector3(lane*3.2f,.6f,p.z+120f);
                    var a=c.WorldToViewportPoint(center+new Vector3(-1.1f,-.6f,-2.2f));
                    var b=c.WorldToViewportPoint(center+new Vector3(1.1f,.6f,2.2f));
                    var middle=c.WorldToViewportPoint(center);
                    Require(middle.x>0f && middle.x<1f && middle.y>.15f && middle.y<.85f && middle.z>0f,"120m threat outside useful screen area.");
                    threats.Add(new {aspect,lane,center=middle.ToString("F5"),pixelsAt720=new Vector2(Mathf.Abs(b.x-a.x)*720*aspect,Mathf.Abs(b.y-a.y)*720).ToString("F3")});
                }
                c.aspect=old;
            }
            var rootHalf=m.Configuration.PlayerHalfExtents;
            var body=c.WorldToViewportPoint(p); var nearLeft=c.WorldToViewportPoint(new Vector3(-4.8f,0f,p.z+8f));
            var nearRight=c.WorldToViewportPoint(new Vector3(4.8f,0f,p.z+8f));
            var rayPoint=c.transform.position+c.transform.forward*.11f;
            Require(p.y+rootHalf.y<c.transform.position.y && c.transform.position.y<7.9f,"Camera clips road or tunnel roof.");
            Require(nearLeft.x>0 && nearRight.x<1,"Three lanes not framed.");
            captures.Add(new {label,path="Evidence/Task10/"+label+".png",width=Screen.width,height=Screen.height,aspect=c.aspect,
                lane=l.Player.LaneIndex,s.Distance,s.ElapsedTime,cruise=s.CruiseSpeed,actualBeforePause=actual,
                player=p.ToString("F3"),camera=c.transform.position.ToString("F3"),rotation=c.transform.eulerAngles.ToString("F3"),
                roll=l.Visuals.Roll,bob=l.Visuals.BobOffset,impulse=l.Camera.ImpulseOffset.ToString("F4"),body=body.ToString("F4"),nearLeft=nearLeft.ToString("F4"),nearRight=nearRight.ToString("F4"),
                roadAhead=l.Roads.CoverageEnd-s.Distance,offset=l.Origin.Offset,calibrationReferences=references.Count,referenceDistance=references.Count>0?120:0,threats=threats.ToArray()});
        }
        private static void SetField(object target,string name,object value) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        private static void Call(object target,string name,params object[] values) => target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,values);
        private static void Require(bool yes,string message) { if(!yes) throw new InvalidOperationException(message); }
        private static async Task Until(Func<bool> test)
        { for(int i=0;i<300;i++) { if(test()) return; await Task.Delay(16); } throw new TimeoutException("Native visual condition timed out."); }
    }
}
