using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
namespace NeonTunnelRunner
{
    public static class Task13SceneSmoke
    {
        static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static object Call(object o,string n,params object[] v)=>o.GetType().GetMethod(n,F).Invoke(o,v);
        static void Check(bool b,string m){if(!b)throw new Exception(m);}
        static async Task Wait(Func<bool> condition,double seconds=8)
        {var until=DateTime.UtcNow.AddSeconds(seconds);while(!condition()){Check(DateTime.UtcNow<until,"Native simulation did not reach condition");await Task.Delay(16);}}
        static void Capture(GameManager m,string prefix,string label)
        {
            var c=m.Loop.Camera.ViewCamera;float aspect=c.aspect;var target=c.targetTexture;var active=RenderTexture.active;
            foreach(int width in new[]{1280,960})
            {
                var rt=RenderTexture.GetTemporary(width,720,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(width,720,TextureFormat.RGB24,false);
                try{c.aspect=width/720f;c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,720),0,0);tex.Apply();System.IO.File.WriteAllBytes("Evidence/Task13/"+prefix+label+"-"+width+".png",tex.EncodeToPNG());}
                finally{c.aspect=aspect;c.targetTexture=target;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
            }
        }
        public static async Task<object> Run()
        {
            var m=GameManager.Instance;Check(m && m.State==GameState.Menu,"Saved Game must enter Menu.");var l=m.Loop;var a=l.Audio;var v=l.Vfx;
            string prefix=System.IO.File.ReadAllText("Evidence/Task13/capture-prefix.txt");string key="NTR.Tests.Task13.Scene."+Guid.NewGuid().ToString("N");Set(m,"highScores",new HighScoreStore(key));
            bool background=Application.runInBackground;var ids=UnityEngine.Object.FindObjectsByType<AudioSource>().Select(s=>s.GetEntityId().ToString()).OrderBy(x=>x).ToArray();
            try
            {
                Application.runInBackground=true;Call(a,"SetApplicationSuspension",false,false);Check(ids.Length==10,"Source baseline");Check(m.StartRun(),"Start");
                ulong music=a.MusicStarts,engine=a.EngineStarts,created=m.Pools.TotalCreated;Check(a.EngineSource.pitch>1.10f && a.EngineSource.pitch<1.12f,"Starting engine pitch");
                await Wait(()=>v.Fireworks.Launches==1);double firstLaunch=v.Fireworks.LastLaunchTime;
                await Wait(()=>v.ActiveFireworks>0 && Enumerable.Range(0,v.ActiveCount).Select(v.GetActive).Any(e=>e.Key==PoolKey.Firework && e.GetComponentsInChildren<ParticleSystem>().Any(p=>p!=e.Primary&&p.particleCount>0)));
                Capture(m,prefix,"opening-burst");
                Call(a,"Request",AudioCue.Fuel);await Task.Delay(30);m.Pause();int voices=a.ActiveVoices;ulong token=a.VoiceGeneration(0);int sample=a.EngineSource.timeSamples;var frozen=m.Session.Snapshot;double pauseStart=Time.realtimeSinceStartupAsDouble;
                await Task.Delay(10100);Call(a,"RefreshPlayback");Check(a.ActiveVoices==voices && a.VoicePaused(0),"Pause ownership");Check(!a.EngineSource.isPlaying && a.EngineSource.timeSamples==sample,"Paused engine");Check(m.Session.Snapshot.Distance==frozen.Distance && m.Session.Snapshot.Fuel==frozen.Fuel,"Paused simulation");double pauseSeconds=Time.realtimeSinceStartupAsDouble-pauseStart;
                Call(a,"SetApplicationSuspension",true,true);Call(a,"SetApplicationSuspension",false,false);Check(m.State==GameState.Paused&&!a.EngineSource.isPlaying,"Focus must preserve pause");
                m.Resume();Check(a.MusicStarts==music&&a.EngineStarts==engine,"Resume loop duplication");await Task.Delay(450);Call(a,"RefreshPlayback");Check(a.ActiveVoices==0,"Resumed completion");
                // Native simulation plus a fixture-only protected-lane driver.
                m.Pause();m.Restart();Call(m.Session,"Advance",180f,0d);bool tunnel=false,rebase=false;var end=DateTime.UtcNow.AddSeconds(40);var horizon=new List<object>();
                while(!rebase)
                {
                    Check(DateTime.UtcNow<end&&m.State==GameState.Playing,"Protected route stopped");var s=m.Session.Snapshot;var plan=l.Planner.PlanAt((long)(s.Distance/480));int desired=s.Distance-plan.AbsoluteStart>=395?plan.NextProtectedLane:plan.ProtectedLane;
                    if(!l.Player.IsTransitioning&&l.Player.LaneIndex!=desired)l.Player.RequestLane(Math.Sign(desired-l.Player.LaneIndex));
                    if(!tunnel&&s.Distance>=265){Capture(m,prefix,"tunnel-cap");tunnel=true;}
                    if(l.Origin.RebaseCount>0){Capture(m,prefix,"rebase-cap");rebase=true;}
                    await Task.Delay(16);
                }
                m.Pause();Check(a.EngineSource.isPlaying==false,"Pause cap");
                foreach(int width in new[]{1280,960})for(int lane=-1;lane<=1;lane++)
                {var c=l.Camera.ViewCamera;float old=c.aspect;c.aspect=width/720f;var p=c.WorldToViewportPoint(l.Player.CurrentPosition+new Vector3(lane*3.2f,0,120));c.aspect=old;Check(p.z>0&&p.x>.05&&p.x<.95&&p.y>.05&&p.y<.95,"120m framing");horizon.Add(new{width,lane,p.x,p.y,p.z});}
                Check(a.EngineSource.pitch==.8f,"Paused actual speed must be zero");m.Restart();Call(a,"SetApplicationSuspension",false,false);
                // Saturation remains separate from gameplay and VFX pool misses.
                for(int i=0;i<8;i++)Call(a,"Request",AudioCue.Fuel);Check(!(bool)Call(a,"Request",AudioCue.Coin),"Saturation drop");Check((bool)Call(a,"Request",AudioCue.Collision),"Collision priority");int peak=a.PeakVoices;ulong dropped=a.Dropped,stolen=a.Stolen;
                Call(m.Session,"SetResources",1,10f);var snapshot=m.Session.Snapshot;
                l.Survival.TryDamage(new Encounter(PoolKey.Stationary,991100,991100,0,snapshot.Distance,l.Player.CurrentPosition,snapshot.ElapsedTime));m.EndRun(GameOverReason.Lives);
                Check(!a.EngineSource.isPlaying && a.ActiveVoices==8,"Terminal engine/voice policy");Check(v.PlayedSparks==1 && m.Session.Snapshot.Lives==0,"Audio saturation blocked accepted damage/VFX");await Task.Delay(600);Call(a,"RefreshPlayback");Check(a.ActiveVoices==0,"Terminal completion at timeScale0");
                for(int run=0;run<50;run++){Check(m.Restart(),"Restart");Check(a.ActiveVoices==0,"Reset leaked voice");Call(a,"Request",AudioCue.Coin);m.EndRun(GameOverReason.Lives);}
                m.Restart();m.Pause();m.ReturnToMenu();Check(a.MusicStarts==music,"Music duplicated across lifecycle");
                var after=UnityEngine.Object.FindObjectsByType<AudioSource>().Select(s=>s.GetEntityId().ToString()).OrderBy(x=>x).ToArray();Check(ids.SequenceEqual(after),"Source identity changed");Check(m.Pools.TotalCreated==created&&m.Pools.TotalDestroyed==0&&m.Pools.Misses==0,"Gameplay/VFX pool regression");
                return new{passed=true,firstLaunch,pauseSeconds,sources=after.Length,listeners=UnityEngine.Object.FindObjectsByType<AudioListener>().Length,musicStarts=a.MusicStarts,engineStarts=a.EngineStarts,peak,dropped,stolen,created=m.Pools.TotalCreated,destroyed=m.Pools.TotalDestroyed,misses=m.Pools.Misses,horizon};
            }
            finally{Application.runInBackground=background;Call(m,"Shutdown");PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
        }
    }
}
