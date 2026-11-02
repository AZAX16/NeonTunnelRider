// Runtime photographs of the saved scene through its real camera; fixture-only control stays outside Assets.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
namespace NeonTunnelRunner
{
    public static class Task11Visual
    {
        static readonly List<object> captures=new List<object>();
        public static async Task<object> Run()
        {
            var m=GameManager.Instance;Require(m && m.State==GameState.Menu,"Saved Game must start in Menu.");
            var l=m.Loop;string key="NTR.Tests.Task11.Visual."+Guid.NewGuid().ToString("N");var store=new HighScoreStore(key);Set(m,"highScores",store);
            bool background=Application.runInBackground;var behavior=InputSystem.settings.editorInputBehaviorInPlayMode;var focus=InputSystem.settings.backgroundBehavior;
            var keyboard=InputSystem.AddDevice<Keyboard>("Task11VisualKeyboard");captures.Clear();
            try
            {
                Application.runInBackground=true;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;l.Input.Actions.devices=new InputDevice[]{keyboard};
                // Deterministic fixed-pose comparison. Advance only the model clock through its existing test seam; progression still chooses actual speed.
                m.StartRun();Compare(m,"near-start");m.Pause();m.Restart();Call(m.Session,"Advance",180f,0d);l.Step(.02f);Call(l.Player,"HoldPose");Compare(m,"cap");
                m.Pause();Capture(m,"paused",1280);m.Resume();Capture(m,"resumed",1280);
                // Real contact dispatch with a changing neon value and the original accepted-hit flash.
                Rent(m,PoolKey.Stationary,0,4.1f,990100);l.Step(.04f);Call(l.Player,"HoldPose");Capture(m,"accepted-hit",1280);
                m.Pause();Capture(m,"hit-paused",1280);m.Resume();l.Step(.3f);Call(l.Player,"HoldPose");Capture(m,"flash-expired",1280);
                m.EndRun(GameOverReason.Lives);Capture(m,"gameover",1280);m.Restart();Capture(m,"restart",1280);
                // Native FixedUpdates + actual actions along the protected route, without disabling survival/supply.
                Call(m.Session,"Advance",180f,0d);await Until(()=>l.TickIndex>3);ulong created=m.Pools.TotalCreated;
                bool held=false;int commanded=0,capture=0;double[] distances={125d,180d,260d,430d,490d,1005d};string[] labels={"open-sky","entrance","tunnel","exit","open-after-exit","rebase"};
                var deadline=DateTime.UtcNow.AddSeconds(65);int frame=Time.frameCount;
                while(capture<distances.Length)
                {
                    Require(DateTime.UtcNow<deadline && m.State==GameState.Playing,"Visual route stopped.");var s=m.Session.Snapshot;var plan=l.Planner.PlanAt((long)(s.Distance/480d));
                    int desired=s.Distance-plan.AbsoluteStart>=395d?plan.NextProtectedLane:plan.ProtectedLane;
                    if(held&&!l.Player.IsTransitioning&&l.Player.LaneIndex==commanded){InputSystem.QueueStateEvent(keyboard,new KeyboardState());held=false;}
                    else if(!held&&!l.Player.IsTransitioning&&l.Player.LaneIndex!=desired){commanded=l.Player.LaneIndex+Math.Sign(desired-l.Player.LaneIndex);InputSystem.QueueStateEvent(keyboard,new KeyboardState(desired<l.Player.LaneIndex?Key.A:Key.D));held=true;}
                    if(s.Distance>=distances[capture])
                    {
                        Capture(m,labels[capture],1280);Capture(m,labels[capture]+"-4x3",960);
                        if(capture==0) {ScreenCapture.CaptureScreenshot("Evidence/Task11/native-open.png");}
                        if(capture==2) {ScreenCapture.CaptureScreenshot("Evidence/Task11/native-tunnel.png");}
                        capture++;
                    }
                    await Task.Delay(16);
                }
                Require(Time.frameCount>frame && l.Origin.RebaseCount>0 && m.Pools.TotalCreated==created,"Native frames/rebase/pool stability failed.");
                // Inspect both outer lanes at cap through the production movement path in a clean opening.
                m.Pause();m.Restart();Call(m.Session,"Advance",180f,0d);
                foreach(int lane in new[]{-1,1})
                {
                    while(l.Player.LaneIndex!=lane){l.Player.RequestLane(Math.Sign(lane-l.Player.LaneIndex));l.Step(.28f);Call(l.Player,"HoldPose");}
                    Call(l.Camera,"Render",1f);Call(l.Visuals,"Render",.02f);Capture(m,"cap-outer"+lane,1280);Capture(m,"cap-outer"+lane+"-4x3",960);
                }
                m.Pause();return new {passed=true,method="Saved Game runtime camera renders plus native ScreenCapture",captures=captures.ToArray(),framesAdvanced=Time.frameCount-frame,created=m.Pools.TotalCreated,misses=m.Pools.Misses};
            }
            finally
            {
                if(m.State==GameState.Playing)m.Pause();l.Input.Actions.devices=null;InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.editorInputBehaviorInPlayMode=behavior;InputSystem.settings.backgroundBehavior=focus;Application.runInBackground=background;
                store.Flush();PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
            }
        }
        static void Compare(GameManager m,string label)
        {
            var l=m.Loop;Call(l.Player,"HoldPose");Call(l.Camera,"ResetPresentation");Call(l.Visuals,"Render",0f);
            // Explicitly populated silhouettes at chase distances; no collider/root edits and no progression bypass in player code.
            Rent(m,PoolKey.Stationary,-1,26f,990000);Rent(m,PoolKey.Moving,1,38f,990001);Rent(m,PoolKey.Coin,-1,14f,990002);Rent(m,PoolKey.Fuel,1,18f,990003);
            var leases=new List<PooledObject>();
            for(int lane=-1;lane<=1;lane++) leases.Add(Rent(m,PoolKey.Stationary,lane,120f,990010+lane));
            Capture(m,label,1280);Capture(m,label+"-4x3",960);
            var v=l.Visuals;bool a=v.LeftHeadlight.enabled,b=v.RightHeadlight.enabled;v.LeftHeadlight.enabled=v.RightHeadlight.enabled=false;
            Capture(m,label+"-headlights-off",1280);v.LeftHeadlight.enabled=a;v.RightHeadlight.enabled=b;
            foreach(var item in leases)item.ReturnToPool(item.ActivationId);
        }
        static PooledObject Rent(GameManager m,PoolKey key,int lane,float forward,long sequence)
        {
            var l=m.Loop;var s=m.Session.Snapshot;float height=key==PoolKey.Coin||key==PoolKey.Fuel?1.2f:.6f;
            Require(m.Pools.TryRent(new SpawnContext(key,new Vector3(lane*3.2f,height,l.Player.CurrentPosition.z+forward),Quaternion.identity,l.Planner.OriginRoot,lane,s.Distance+forward,s.Seed,0f,sequence,l.Planner),out var item),"Visual rental failed.");return item;
        }
        static void Capture(GameManager m,string label,int width)
        {
            var l=m.Loop;Call(l.Player,"HoldPose");Call(l.Camera,"HoldView");var c=l.Camera.ViewCamera;var v=l.Visuals;var s=m.Session.Snapshot;var block=new MaterialPropertyBlock();v.UnderglowRenderer.GetPropertyBlock(block);
            var oldTarget=c.targetTexture;float oldAspect=c.aspect;var rt=RenderTexture.GetTemporary(width,720,24,RenderTextureFormat.ARGB32);var oldActive=RenderTexture.active;
            string path="Evidence/Task11/"+label+".png";var texture=new Texture2D(width,720,TextureFormat.RGB24,false);
            try {c.targetTexture=rt;c.aspect=(float)width/720;c.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,width,720),0,0);texture.Apply();System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally{c.targetTexture=oldTarget;c.aspect=oldAspect;RenderTexture.active=oldActive;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(texture);}
            int activeStreet=0;float seam=0;for(int i=0;i<l.Roads.ActiveCount;i++){var road=l.Roads.GetLease(i).Segment;if(road.Streetlight&&road.Streetlight.enabled)activeStreet++;if(i>0)seam=Mathf.Max(seam,Vector3.Distance(l.Roads.GetLease(i-1).Segment.Exit.position,road.Entry.position));}
            var horizon=new List<object>();float aspect=c.aspect;c.aspect=(float)width/720;
            for(int lane=-1;lane<=1;lane++){var point=c.WorldToViewportPoint(new Vector3(lane*3.2f,.6f,l.Player.CurrentPosition.z+120));Require(point.x>0&&point.x<1&&point.y>.15f&&point.y<.85f,"120m horizon lost.");horizon.Add(new{lane,viewport=point.ToString("F5")});}c.aspect=aspect;
            Require(activeStreet<=3&&seam<.001,"Streetlight/seam budget failed.");
            captures.Add(new{label,path,width,height=720,state=m.State.ToString(),s.ActualSpeed,s.CruiseSpeed,s.ElapsedTime,s.Distance,lane=l.Player.LaneIndex,
                light=v.UnderglowLight.intensity,emission=v.EmissionMultiplier,emissionColor=block.GetColor("_EmissionColor").ToString("F5"),headlights=v.LeftHeadlight.enabled&&v.RightHeadlight.enabled,
                flash=v.IsFlashing,flashRemaining=v.FlashRemaining,activeStreet,moon=.65f,exposure="Skybox=1; no exposure/tonemapping/Bloom overrides",camera=c.transform.position.ToString("F4"),player=l.Player.CurrentPosition.ToString("F4"),
                offset=l.Origin.Offset,rebases=l.Origin.RebaseCount,seam,roadAhead=l.Roads.CoverageEnd-s.Distance,horizon=horizon.ToArray(),stats=new{drawCalls=UnityStats.drawCalls,triangles=UnityStats.triangles,vertices=UnityStats.vertices}});
        }
        static void Set(object o,string f,object value)=>o.GetType().GetField(f,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
        static object Call(object o,string method,params object[] values)=>o.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,values);
        static void Require(bool yes,string message){if(!yes)throw new InvalidOperationException(message);}
        static async Task Until(Func<bool> predicate){for(int i=0;i<300;i++){if(predicate())return;await Task.Delay(16);}throw new TimeoutException("Native frame condition timed out.");}
    }
}
