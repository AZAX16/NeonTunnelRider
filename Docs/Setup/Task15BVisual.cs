using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
namespace NeonTunnelRunner
{
    public static class Task15BVisual
    {
        const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        static string output="Evidence/Task15B/StagedVisual";
        static void Call(object o,string method,params object[] a)=>o.GetType().GetMethod(method,F).Invoke(o,a);
        static void Check(bool b,string s){if(!b)throw new Exception(s);}
        static object group, singleton; static Type viewType,sizesType; static EditorWindow view; static int oldIndex;
        static void Viewport(int width,int height)
        {
            var a=typeof(Editor).Assembly;
            viewType=a.GetType("UnityEditor.GameView");view=EditorWindow.GetWindow(viewType);
            sizesType=a.GetType("UnityEditor.GameViewSizes");
            singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance",F).GetValue(null);
            group=sizesType.GetProperty("currentGroup",F).GetValue(singleton);
            var ctor=a.GetType("UnityEditor.GameViewSize").GetConstructors(F).First(c=>c.GetParameters().Length==4);
            var size=ctor.Invoke(new object[]{Enum.ToObject(a.GetType("UnityEditor.GameViewSizeType"),1),width,height,"Task15B verification"});
            group.GetType().GetMethod("AddCustomSize",F).Invoke(group,new[]{size});
            int count=(int)group.GetType().GetMethod("GetTotalCount",F).Invoke(group,null);
            viewType.GetProperty("selectedSizeIndex",F).SetValue(view,count-1);view.Repaint();
        }
        static async Task Capture(string name)
        {ScreenCapture.CaptureScreenshot(output+"/"+name+".png");await Task.Delay(180);Check(File.Exists(output+"/"+name+".png"),"Capture missing");}
        public static async Task<object> Run()
        {
            Directory.CreateDirectory(output);
            var m=GameManager.Instance; Check(m && m.State==GameState.Menu,"Game must be in Menu");
            string key="NTR.Tests.Task15B.Visual."+Guid.NewGuid().ToString("N");
            typeof(GameManager).GetField("highScores",F).SetValue(m,new HighScoreStore(key));
            bool background=Application.runInBackground;Application.runInBackground=true;
            var audio=m.Loop.Audio;
            var rows=new List<object>(); var framing=new List<object>();
            int initialFrame=Time.frameCount;
            var a=typeof(Editor).Assembly;viewType=a.GetType("UnityEditor.GameView");view=EditorWindow.GetWindow(viewType);
            oldIndex=(int)viewType.GetProperty("selectedSizeIndex",F).GetValue(view);
            try
            {
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(960,720),new Vector2Int(1920,1080),new Vector2Int(2520,1080)})
                {
                    Viewport(size.x,size.y);await Task.Delay(250);
                    if(m.State==GameState.Paused) m.Restart(); else Check(m.StartRun(),"Start");
                    var l=m.Loop;var p=l.Player; string prefix=size.x+"x"+size.y;
                    await Task.Delay(200); await Capture(prefix+"-normal");
                    if(size.x==1280) { Profiler.logFile="Evidence/Task15B/staged-focused.raw"; Profiler.enableAllocationCallstacks=true; Profiler.enableBinaryLog=true; Profiler.enabled=true; }
                    double z=m.Session.Snapshot.Distance+35d;
                    Check(m.Pools.TryRent(new SpawnContext(PoolKey.Stationary,new Vector3(3.2f,.6f,(float)(z-l.Origin.Offset)),Quaternion.identity,l.Planner.OriginRoot,1,z,12345,0f,900000,l.Planner),out var item),"Near miss fixture rental");
                    var deadline=DateTime.UtcNow.AddSeconds(5);
                    while(!m.NearMiss.Active){Check(DateTime.UtcNow<deadline,"No completed close pass");await Task.Delay(16);}
                    await Capture(prefix+"-trigger-car");
                    double began=Time.realtimeSinceStartupAsDouble;
                    bool hold=false,paused=false,exit=false,approachShot=false,afterShot=false;
                    while(m.NearMiss.Active)
                    {
                        rows.Add(new{viewport=prefix,utc=DateTime.UtcNow.ToString("o"),real=Time.realtimeSinceStartupAsDouble,
                            frame=Time.frameCount,age=m.NearMiss.Age,approachEnd=m.NearMiss.ApproachEnd,passEnd=m.NearMiss.PassEnd,relativeZ=((Interactable)item).AbsoluteZ-m.Session.Snapshot.Distance,weight=m.NearMiss.Weight,scale=Time.timeScale,
                            fixedStep=Time.fixedDeltaTime,fov=l.Camera.ViewCamera.fieldOfView,cueSamples=audio.NearMissSource.timeSamples,
                            cuePlaying=audio.NearMissSource.isPlaying,normalSamples=audio.MusicSource.timeSamples,
                            distance=m.Session.Snapshot.Distance,active=l.Planner.ActiveCount,created=m.Pools.TotalCreated});
                        if(!hold && m.NearMiss.Age>2.85f)
                        {
                            hold=true;await Capture(prefix+"-pass");var camera=l.Camera.ViewCamera;
                            for(int lane=-1;lane<=1;lane++)
                            {var point=camera.WorldToViewportPoint(p.CurrentPosition+new Vector3(lane*3.2f-p.CurrentPosition.x,0f,120f));
                             Check(point.z>0 && point.x>.05f && point.x<.95f && point.y>.1f && point.y<.9f,"Zoom 120m horizon");
                             framing.Add(new{viewport=prefix,lane,point.x,point.y,point.z,fov=camera.fieldOfView});}

                        }
                        if(!approachShot && m.NearMiss.Age>.4f){approachShot=true;await Capture(prefix+"-approach");}
                        if(!afterShot && m.NearMiss.Age>4f){afterShot=true;await Capture(prefix+"-after-pass");}
                        if(!paused && m.NearMiss.Age>1.5f)
                        {
                            paused=true;m.Pause();float age=m.NearMiss.Age;int samples=audio.NearMissSource.timeSamples;
                            await Task.Delay(400);Check(m.NearMiss.Age==age,"Paused phase drift");
                            Check(audio.NearMissSource.timeSamples==samples,"Paused cue drift");
                            await Capture(prefix+"-paused");m.Resume();
                        }
                        if(!exit && m.NearMiss.Age>5.7f) {exit=true;await Capture(prefix+"-exit");}
                        await Task.Delay(33);
                    }
                    await Task.Delay(200);await Capture(prefix+"-restored");
                    Check(audio.NearMissSource.isPlaying==false,"Orphan cue");
                    Check(Math.Abs(l.Camera.ViewCamera.fieldOfView-l.Camera.Configuration.FieldOfView)<.001f,"FOV baseline drift");
                    bool focusHeld=(bool)typeof(AudioController).GetField("focusLost",F).GetValue(audio);
                    if(focusHeld)typeof(AudioController).GetMethod("SetApplicationSuspension",F).Invoke(audio,new object[]{false,false});
                    Check(audio.MusicSource.isPlaying,"Normal track restoration after focus resume");
                    m.Pause();
                    if(size.x==1280) { Profiler.enabled=false; Profiler.enableBinaryLog=false; }
                }
                return new{passed=true,frames=Time.frameCount-initialFrame,framing,timeline=rows,sources=UnityEngine.Object.FindObjectsByType<AudioSource>().Length,
                    created=m.Pools.TotalCreated,misses=m.Pools.Misses,method="Controlled adjacent parked-car passes in saved playable Game; no score/resource grants; live frames, pause/resume and source samples recorded."};
            }
            finally
            {
                if(m.State==GameState.Playing)m.Pause();if(m.State==GameState.Paused)m.ReturnToMenu();
                Application.runInBackground=background;
                if(group!=null)
                {
                    var custom=(System.Collections.IList)group.GetType().GetField("m_Custom",F).GetValue(group);
                    for(int i=custom.Count-1;i>=0;i--)if((string)custom[i].GetType().GetProperty("baseText",F).GetValue(custom[i])=="Task15B verification")custom.RemoveAt(i);
                    sizesType.GetMethod("Changed",F).Invoke(singleton,null);viewType.GetProperty("selectedSizeIndex",F).SetValue(view,oldIndex);view.Repaint();
                }
                PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
            }
        }
    }
}
