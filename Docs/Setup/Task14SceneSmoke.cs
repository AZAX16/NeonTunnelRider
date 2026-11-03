using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
namespace NeonTunnelRunner
{
    public static class Task14SceneSmoke
    {
        static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static void Check(bool b,string message){if(!b)throw new Exception(message);}
        static Keyboard keyboard;static Mouse mouse;static GameManager m;static Transform canvas;
        static async Task Tap(Key key)
        {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));await Task.Delay(70);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(70);}
        static async Task Click(string path)
        {
            var r=canvas.Find(path).GetComponent<RectTransform>();var p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
            InputSystem.QueueStateEvent(mouse,new MouseState {position=p});await Task.Delay(70);
            InputSystem.QueueStateEvent(mouse,new MouseState {position=p}.WithButton(MouseButton.Left));await Task.Delay(70);
            InputSystem.QueueStateEvent(mouse,new MouseState {position=p});await Task.Delay(70);
        }
        static TMP_Text Text(string p)=>canvas.Find(p).GetComponent<TMP_Text>();
        static async Task Capture(string prefix,string label)
        {
            string path="Evidence/Task14/"+prefix+label+".png";
            ScreenCapture.CaptureScreenshot(path);await Task.Delay(180);Check(System.IO.File.Exists(path),"Screen capture did not complete");
        }
        static void CompareHud()
        {
            var s=m.Session.Snapshot;
            Check(Text("HUD/SpeedBlock/SpeedValue").text==Mathf.FloorToInt(s.ActualSpeed*3.6f+.5f)+" km/h","HUD actual speed");
            Check(Text("HUD/ScoreBlock/ScoreValue").text==s.Score.ToString(),"HUD score");Check(Text("HUD/ScoreBlock/BestValue").text==s.BestScore.ToString(),"HUD best");
            Check(Text("HUD/LivesBlock/LivesValue").text==s.Lives+" / 3","HUD lives");
            Check(Text("HUD/FuelBlock/FuelValue").text==Mathf.FloorToInt(Mathf.Clamp01(s.Fuel/m.Configuration.FuelCapacity)*100+.5f)+"%","HUD fuel percent");
            Check(Math.Abs(canvas.Find("HUD/FuelBlock/FuelTrack/FuelFill").GetComponent<UnityEngine.UI.Image>().fillAmount-Mathf.Clamp01(s.Fuel/m.Configuration.FuelCapacity))<.00001,"HUD fuel fill");
        }
        public static async Task<object> Run()
        {
            m=GameManager.Instance;Check(m && m.State==GameState.Menu,"Saved Game must enter Menu");canvas=UnityEngine.Object.FindAnyObjectByType<MenuPresenter>().transform;
            string prefix=System.IO.File.ReadAllText("Evidence/Task14/capture-prefix.txt");
            bool background=Application.runInBackground;string key="NTR.Tests.Task14.Scene."+Guid.NewGuid().ToString("N");
            Set(m,"highScores",new HighScoreStore(key));keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
            var l=m.Loop;var audio=l.Audio;var vfx=l.Vfx;
            var sourceIds=UnityEngine.Object.FindObjectsByType<AudioSource>().Select(x=>x.GetEntityId().ToString()).OrderBy(x=>x).ToArray();ulong created=m.Pools.TotalCreated,music=audio.MusicStarts;
            int initialFrame=Time.frameCount;var checks=new List<object>();
            try
            {
                Application.runInBackground=true;
                await Task.Delay(150);Check(Time.frameCount>initialFrame,"Player loop stalled");
                Check(EventSystem.current.currentSelectedGameObject.name=="Start","Menu focus");await Capture(prefix,"main");
                await Tap(Key.DownArrow);Check(EventSystem.current.currentSelectedGameObject.name=="Quit","Menu arrow focus");await Capture(prefix,"quit-focus");
                await Tap(Key.UpArrow);await Click("MainMenu/Card/Start");Check(m.State==GameState.Playing,"Pointer Start");Check(!EventSystem.current.currentSelectedGameObject,"Hidden selection");CompareHud();
                await Capture(prefix,"hud");
                var fireworkDeadline=DateTime.UtcNow.AddSeconds(8);
                while(!Enumerable.Range(0,vfx.ActiveCount).Select(vfx.GetActive).Any(e=>e.Key==PoolKey.Firework && e.GetComponentsInChildren<ParticleSystem>().Any(p=>p!=e.Primary && p.particleCount>0)))
                {Check(DateTime.UtcNow<fireworkDeadline,"Opening firework not observed");await Task.Delay(16);}
                await Capture(prefix,"hud-firework");await Tap(Key.Escape);Check(m.State==GameState.Paused && Time.timeScale==0,"Escape exactly once");CompareHud();await Capture(prefix,"pause");
                var frozen=m.Session.Snapshot;double start=Time.realtimeSinceStartupAsDouble;await Task.Delay(10100);Check(m.Session.Snapshot.Equals(frozen),"Paused data advanced");Check(!audio.EngineSource.isPlaying,"Paused engine");double pausedSeconds=Time.realtimeSinceStartupAsDouble-start;
                await Click("PauseMenu/Card/Resume");Check(m.State==GameState.Playing,"Pointer Resume");await Tap(Key.Escape);await Tap(Key.DownArrow);await Tap(Key.Enter);Check(m.State==GameState.Playing,"Keyboard paused Restart");
                await Tap(Key.Escape);await Click("PauseMenu/Card/MainMenu");Check(m.State==GameState.Menu,"Pointer Main Menu");await Tap(Key.Enter);Check(m.State==GameState.Playing,"Keyboard Start");
                // Deterministic fixture guide travels the existing safe route to a real rebase.
                while(l.Origin.RebaseCount==0)
                {
                    var s=m.Session.Snapshot;var p=l.Planner.PlanAt((long)(s.Distance/480));int lane=s.Distance-p.AbsoluteStart>=395?p.NextProtectedLane:p.ProtectedLane;
                    if(!l.Player.IsTransitioning && l.Player.LaneIndex!=lane)l.Player.RequestLane(Math.Sign(lane-l.Player.LaneIndex));
                    Check(l.Step(.1f)&&m.State==GameState.Playing,"Safe route to rebase");
                }
                double offset=l.Origin.Offset;l.enabled=false;
                // Fifty resets use real Buttons via action events, alternating pause and both terminal causes.
                for(int i=0;i<50;i++)
                {
                    Call(audio,"Request",AudioCue.Fuel);var before=m.Session.Snapshot;
                    if(i%2==0){await Tap(Key.Escape);await Click("PauseMenu/Card/Restart");}
                    else {Call(m.Session,"SetResources",i%4==1?0:2,i%4==1?40f:0f);m.EndRun(i%4==1?GameOverReason.Lives:GameOverReason.Fuel);await Tap(Key.Enter);}
                    var s=m.Session.Snapshot;Check(m.State==GameState.Playing&&s.RunId!=before.RunId&&s.Seed==12345&&s.Lives==3&&s.Fuel>99&&s.Score<100&&s.BestScore>=before.BestScore,"UI restart snapshot i="+i+" state="+m.State+" fuel="+s.Fuel+" lives="+s.Lives+" score="+s.Score+" seed="+s.Seed);
                    Check(l.Origin.Offset==0&&audio.ActiveVoices==0&&vfx.ActiveCount==0,"UI reset leaked effects/origin");
                    Check(m.Pools.TotalCreated==created&&m.Pools.TotalDestroyed==0&&m.Pools.Misses==0,"Pool regression");Check(l.Input.ConsumeLaneCommand()==0,"Stale lane command");CompareHud();
                }
                // Fixture-only low-fuel/new-record and large integer projection; no production control.
                l.enabled=false;Call(m.Session,"SetResources",1,12.34f);Call(m.Session,"SetScore",0,int.MaxValue);Call(m.Session,"RetainBestScore",int.MaxValue);Call(m.Session,"PublishSnapshot");CompareHud();await Capture(prefix,"low-fuel-large-best");
                foreach(var reason in new[]{GameOverReason.Fuel,GameOverReason.Lives})
                {
                    Call(m.Session,"SetResources",reason==GameOverReason.Lives?0:1,reason==GameOverReason.Fuel?0f:12f);m.EndRun(reason);CompareHud();
                    var final=m.FinalRunSnapshot.Value;Check(Text("GameOver/Card/FinalScore").text==final.Score.ToString()&&Text("GameOver/Card/FinalBest").text==final.BestScore.ToString(),"Final result");await Capture(prefix,"over-"+reason);
                    checks.Add(new{reason=reason.ToString(),final.Score,final.BestScore,final.Fuel,final.Lives,final.ActualSpeed});
                    await Click("GameOver/Card/Restart");Check(m.State==GameState.Playing,"Pointer terminal Restart");
                }
                l.enabled=true;await Tap(Key.Escape);await Tap(Key.Backspace);Check(m.State==GameState.Playing,"Backspace Resume");
                await Tap(Key.Escape);await Tap(Key.DownArrow);await Tap(Key.DownArrow);await Tap(Key.Enter);Check(m.State==GameState.Menu,"Keyboard Main Menu");
                await Tap(Key.DownArrow);await Tap(Key.Enter);await Capture(prefix,"editor-quit-intent");Check(m.State==GameState.Menu,"Editor Quit state");
                var after=UnityEngine.Object.FindObjectsByType<AudioSource>().Select(x=>x.GetEntityId().ToString()).OrderBy(x=>x).ToArray();Check(sourceIds.SequenceEqual(after)&&after.Length==10,"Fixed source identities");Check(audio.MusicStarts==music,"Music loop duplicated");
                foreach(int width in new[]{1280,960,1920,2560})
                {
                    var c=l.Camera.ViewCamera;float a=c.aspect;c.aspect=width/(width>=1920?1080f:720f);var p=c.WorldToViewportPoint(l.Player.CurrentPosition+new Vector3(0,0,120));c.aspect=a;
                    checks.Add(new{width,horizonX=p.x,horizonY=p.y,depth=p.z});Check(p.z>0&&p.y>.1&&p.y<.9,"120m framing");
                }
                return new {passed=true,width=Screen.width,height=Screen.height,pausedSeconds,rebaseOffset=offset,restartCycles=50,created=m.Pools.TotalCreated,destroyed=m.Pools.TotalDestroyed,misses=m.Pools.Misses,audioSources=after.Length,listeners=UnityEngine.Object.FindObjectsByType<AudioListener>().Length,musicStarts=audio.MusicStarts,engineStarts=audio.EngineStarts,checks};
            }
            finally
            {l.enabled=true;InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);Application.runInBackground=background;Call(m,"Shutdown");PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
        }
    }
}
