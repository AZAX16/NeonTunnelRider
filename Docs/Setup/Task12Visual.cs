// Saved-scene runtime QA; fixture controls and capture allocations never enter a player assembly.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace NeonTunnelRunner
{
    public static class Task12Visual
    {
        static readonly List<object> frames=new List<object>();
        static readonly BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        static string prefix;
        static float[] Vec(Vector3 p)=>new[]{p.x,p.y,p.z};
        static void Require(bool b,string message){if(!b)throw new Exception(message);}
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
        static void Set(object o,string n,object value)=>o.GetType().GetField(n,Flags).SetValue(o,value);
        static async Task Until(Func<bool> condition,float seconds=10)
        {var end=DateTime.UtcNow.AddSeconds(seconds);while(!condition()){Require(DateTime.UtcNow<end,"Runtime condition timed out.");await Task.Delay(16);}}
        static string Signature(GameManager m)
        {
            var list=new List<string>();var particles=new ParticleSystem.Particle[250];
            foreach(var ps in m.Loop.Vfx.OriginRoot.GetComponentsInChildren<ParticleSystem>().Concat(m.Loop.Vfx.Exhaust))
            {int n=ps.GetParticles(particles);list.Add(ps.name+":"+ps.time+":"+n);for(int i=0;i<n;i++)list.Add(particles[i].position.ToString("F5")+":"+particles[i].remainingLifetime.ToString("F5"));}
            return string.Join("|",list);
        }
        public static async Task<object> Run()
        {
            var m=GameManager.Instance;Require(m && m.State==GameState.Menu,"Start saved Game in Menu.");var l=m.Loop;var v=l.Vfx;frames.Clear();
            prefix=System.IO.File.ReadAllText("Evidence/Task12/capture-prefix.txt");
            string scoreKey="NTR.Tests.Task12.Visual."+Guid.NewGuid().ToString("N");var store=new HighScoreStore(scoreKey);Set(m,"highScores",store);
            var volume=UnityEngine.Object.FindAnyObjectByType<Volume>();var original=volume.sharedProfile;var profile=UnityEngine.Object.Instantiate(original);volume.sharedProfile=profile;profile.TryGet<Bloom>(out var bloom);
            bool background=Application.runInBackground;var behavior=InputSystem.settings.editorInputBehaviorInPlayMode;var focus=InputSystem.settings.backgroundBehavior;
            var keyboard=InputSystem.AddDevice<Keyboard>("Task12Visual");
            try
            {
                Application.runInBackground=true;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;l.Input.Actions.devices=new InputDevice[]{keyboard};
                var reloads=new { enabled=EditorSettings.enterPlayModeOptionsEnabled, options=(int)EditorSettings.enterPlayModeOptions };
                Require(m.StartRun(),"Start failed.");ulong created=m.Pools.TotalCreated;int firstFrame=Time.frameCount;
                await Until(()=>v.Fireworks.Launches==1,7);double launchTime=v.Fireworks.LastLaunchTime;var launchPosition=v.Fireworks.LastLaunchPosition;bool launchSky=l.Roads.IsOpenSkySpan(m.Session.Snapshot.Distance-8,m.Session.Snapshot.Distance+100);
                Require(launchTime>=2 && launchTime<=4.021,"Opening schedule outside interval.");
                var fire=Enumerable.Range(0,v.ActiveCount).Select(v.GetActive).First(e=>e.Key==PoolKey.Firework);
                Both(m,"opening-launch");await Until(()=>fire.Age>=.45f);Both(m,"opening-ascent");
                Require(fire.GetComponentsInChildren<ParticleSystem>().Where(p=>p!=fire.Primary).All(p=>p.particleCount==0),"Burst preceded ascent.");
                m.Pause();Both(m,"ascent-paused");ScreenCapture.CaptureScreenshot("Evidence/Task12/"+prefix+"native-ascent-paused.png");string frozen=Signature(m);float countdown=v.Fireworks.Remaining;double elapsed=m.Session.Snapshot.ElapsedTime;DateTime paused=DateTime.UtcNow;
                await Task.Delay(10100);Require(Signature(m)==frozen && v.Fireworks.Remaining==countdown && m.Session.Snapshot.ElapsedTime==elapsed,"Ten second pause changed particles/countdown.");
                double pauseSeconds=(DateTime.UtcNow-paused).TotalSeconds;Both(m,"ascent-after-ten-seconds");m.Resume();
                await Until(()=>fire.Age>=.9f);Both(m,"opening-late-ascent");
                await Until(()=>fire.GetComponentsInChildren<ParticleSystem>().Any(p=>p!=fire.Primary && p.particleCount>0));Both(m,"opening-burst-ignition");await Until(()=>fire.Age>=1.52f);Both(m,"opening-burst");
                bloom.active=false;Both(m,"opening-burst-bloom-off");bloom.active=true;Both(m,"opening-burst-bloom-on");
                await Until(()=>!fire.IsRented,5);Require(v.ActiveFireworks==0,"Completed firework retained lease.");
                // Real typed pickup contacts and accepted/rejected damage, with normal deferred pickup returns.
                m.Pause();m.Restart();int contact=0;
                foreach(var key in new[]{PoolKey.Coin,PoolKey.Fuel,PoolKey.Stationary})
                {
                    if(key==PoolKey.Fuel)Call(m.Session,"SetResources",3,100f);
                    ulong before=key==PoolKey.Coin?v.PlayedCoins:key==PoolKey.Fuel?v.PlayedFuel:v.PlayedSparks;
                    var item=Rent(m,key,0,2.1f,990100+contact++);ulong token=item.ActivationId;
                    await Until(()=>(key==PoolKey.Coin?v.PlayedCoins:key==PoolKey.Fuel?v.PlayedFuel:v.PlayedSparks)>before);
                    await Task.Delay(100);Both(m,key==PoolKey.Coin?"coin-burst":key==PoolKey.Fuel?"fuel-burst":"collision-sparks");
                    if(key==PoolKey.Stationary)
                    {int lives=m.Session.Snapshot.Lives;ulong spark=v.PlayedSparks;Rent(m,key,0,2.1f,990190);await Task.Delay(100);Require(m.Session.Snapshot.Lives==lives && v.PlayedSparks==spark,"Grace rejection emitted spark.");}
                    else Require(!item.IsRented || item.ActivationId!=token,"Pickup held for VFX.");
                    await Task.Delay(600);
                }
                m.Pause();Both(m,"neon-bloom-on");bloom.active=false;Both(m,"neon-bloom-off");bloom.active=true;
                m.Restart();Call(m.Session,"Advance",180f,0d);
                // Native maximum-speed safe route across all roof transitions and two rebases.
                bool held=false;int commanded=0,capture=0;double[] targets={125,180,260,430,490,1005,2020};string[] labels={"cap-open","entrance","tunnel","exit","post-exit","rebase","second-rebase"};
                int maxEffects=0,maxParticles=0,maxFireworks=0,maxLights=0,maxMaterials=0;var deadline=DateTime.UtcNow.AddSeconds(100);
                while(capture<targets.Length)
                {
                    Require(DateTime.UtcNow<deadline && m.State==GameState.Playing,"Native safe route stopped.");var s=m.Session.Snapshot;var plan=l.Planner.PlanAt((long)(s.Distance/480));int desired=s.Distance-plan.AbsoluteStart>=395?plan.NextProtectedLane:plan.ProtectedLane;
                    if(held&&!l.Player.IsTransitioning&&l.Player.LaneIndex==commanded){InputSystem.QueueStateEvent(keyboard,new KeyboardState());held=false;}
                    else if(!held&&!l.Player.IsTransitioning&&l.Player.LaneIndex!=desired){commanded=l.Player.LaneIndex+Math.Sign(desired-l.Player.LaneIndex);InputSystem.QueueStateEvent(keyboard,new KeyboardState(desired<l.Player.LaneIndex?Key.A:Key.D));held=true;}
                    int particles=v.OriginRoot.GetComponentsInChildren<ParticleSystem>().Sum(p=>p.particleCount)+v.Exhaust.Sum(p=>p.particleCount);
                    maxEffects=Math.Max(maxEffects,v.ActiveCount);maxFireworks=Math.Max(maxFireworks,v.ActiveFireworks);maxParticles=Math.Max(maxParticles,particles);maxLights=Math.Max(maxLights,UnityEngine.Object.FindObjectsByType<Light>().Count(a=>a.enabled));
                    maxMaterials=Math.Max(maxMaterials,UnityEngine.Object.FindObjectsByType<Renderer>().Where(r=>r.enabled).SelectMany(r=>r.sharedMaterials).Where(a=>a).Distinct().Count());
                    if(s.Distance>=targets[capture]){Both(m,labels[capture]);capture++;}
                    await Task.Delay(16);
                }
                Require(l.Origin.RebaseCount>=2 && m.Pools.TotalCreated==created && maxFireworks<=2,"Native rebase/count contract.");
                m.Pause();m.Restart();Call(m.Session,"Advance",180f,0d);l.Step(.02f);Call(l.Player,"HoldPose");Call(l.Camera,"ResetPresentation");
                var horizon=new List<object>();for(int lane=-1;lane<=1;lane++){Rent(m,PoolKey.Stationary,lane,120f,990220+lane);var point=l.Player.CurrentPosition+new Vector3(lane*3.2f,0,120);foreach(int w in new[]{1280,960}){float old=l.Camera.ViewCamera.aspect;l.Camera.ViewCamera.aspect=w/720f;var p=l.Camera.ViewCamera.WorldToViewportPoint(point);horizon.Add(new{lane,width=w,viewport=Vec(p)});Require(p.z>0&&p.x>.05&&p.x<.95&&p.y>.05&&p.y<.95,"120m horizon clipped.");l.Camera.ViewCamera.aspect=old;}}
                Both(m,"horizon-cap");bloom.active=false;Both(m,"horizon-cap-bloom-off");bloom.active=true;
                m.Pause();m.Restart();Call(m.Session,"SetResources",1,10f);var hit=new Encounter(PoolKey.Stationary,990250,990250,0,0,l.Player.CurrentPosition,m.Session.Snapshot.ElapsedTime);Require(l.Survival.TryDamage(hit),"Final hit failed.");m.EndRun(GameOverReason.Lives);Both(m,"terminal-sparks");string terminal=Signature(m);await Task.Delay(150);Require(Signature(m)==terminal,"Terminal feedback advanced.");
                m.Restart();Both(m,"restart-clean");Require(v.ActiveCount==0&&v.QueuedCount==0,"Restart leaked effect.");m.Pause();m.ReturnToMenu();
                var pools=new List<object>();foreach(var k in new[]{PoolKey.Spark,PoolKey.CoinBurst,PoolKey.FuelBurst,PoolKey.Firework}){m.Pools.TryGetCounters(k,out var c);pools.Add(new{key=k.ToString(),c.Capacity,c.Created,c.Active,c.Inactive,c.PeakActive,c.Misses});}
                Require(m.Pools.Misses==0 && m.Pools.TotalCreated==created && m.Pools.TotalDestroyed==0,"Natural runtime pool growth/miss.");
                return new{passed=true,reloads,launchTime,launchPosition=Vec(launchPosition),launchSky,pauseSeconds,framesAdvanced=Time.frameCount-firstFrame,created=m.Pools.TotalCreated,destroyed=m.Pools.TotalDestroyed,misses=m.Pools.Misses,maxEffects,maxFireworks,maxParticles,maxLights,maxMaterials,horizon,pools,captures=frames.ToArray()};
            }
            finally
            {
                if(m.State==GameState.Playing)m.Pause();l.Input.Actions.devices=null;InputSystem.RemoveDevice(keyboard);Application.runInBackground=background;InputSystem.settings.editorInputBehaviorInPlayMode=behavior;InputSystem.settings.backgroundBehavior=focus;
                volume.sharedProfile=original;UnityEngine.Object.DestroyImmediate(profile);Call(m,"Shutdown");store.Flush();PlayerPrefs.DeleteKey(scoreKey);PlayerPrefs.Save();
            }
        }
        static PooledObject Rent(GameManager m,PoolKey key,int lane,float forward,long sequence)
        {
            var l=m.Loop;double z=m.Session.Snapshot.Distance+forward;float y=key==PoolKey.Coin||key==PoolKey.Fuel?1.2f:.6f;
            Require(m.Pools.TryRent(new SpawnContext(key,new Vector3(lane*3.2f,y,(float)(z-l.Origin.Offset)),Quaternion.identity,l.Planner.OriginRoot,lane,z,m.Session.Snapshot.Seed,0,sequence,l.Planner),out var item),"Contact fixture rent failed.");return item;
        }
        static object SystemState(ParticleSystem p)
        {
            var particles=new ParticleSystem.Particle[p.main.maxParticles];int n=p.GetParticles(particles);var mean=Vector3.zero;
            for(int i=0;i<n;i++)mean+=p.transform.TransformPoint(particles[i].position);
            if(n>0)mean/=n;
            return new{p.name,p.time,p.particleCount,localSpace=p.main.simulationSpace.ToString(),trail=p.trails.enabled,meanWorld=Vec(mean),color=n>0?particles[0].startColor.ToString():"none"};
        }
        static void Both(GameManager m,string label){Capture(m,label,1280);Capture(m,label+"-4x3",960);}
        static void Capture(GameManager m,string label,int width)
        {
            var l=m.Loop;Call(l.Player,"HoldPose");Call(l.Camera,"HoldView");var c=l.Camera.ViewCamera;
            var target=c.targetTexture;float aspect=c.aspect;var old=RenderTexture.active;var rt=RenderTexture.GetTemporary(width,720,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(width,720,TextureFormat.RGB24,false);
            string path="Evidence/Task12/"+prefix+label+".png";
            try{c.targetTexture=rt;c.aspect=width/720f;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,720),0,0);tex.Apply();System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());}
            finally{c.targetTexture=target;c.aspect=aspect;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
            var v=l.Vfx;var s=m.Session.Snapshot;var effects=new List<object>();for(int i=0;i<v.ActiveCount;i++){var e=v.GetActive(i);effects.Add(new{kind=e.Key.ToString(),e.Age,e.ParticleCount,position=Vec(e.transform.position),children=e.GetComponentsInChildren<ParticleSystem>().Select(p=>SystemState(p)).ToArray()});}
            frames.Add(new{path,width,frame=Time.frameCount,s.ElapsedTime,s.Distance,state=m.State.ToString(),s.ActualSpeed,l.Origin.Offset,l.Origin.RebaseCount,v.ActiveCount,v.ActiveFireworks,v.Fireworks.Launches,v.Fireworks.Remaining,v.Fireworks.LastLaunchTime,open=l.Roads.IsOpenSkySpan(s.Distance-8,s.Distance+Mathf.Max(100,s.ActualSpeed*2.4f+40)+20),exhaust=v.Exhaust.Sum(p=>p.particleCount),effects});
        }
    }
}
