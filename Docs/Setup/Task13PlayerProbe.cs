#if NTR_AUDIO_VERIFY
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace NeonTunnelRunner
{
    // One-build fixture only. The build script copies this outside Docs temporarily.
    // No shipping input, UI, gameplay service or per-cue source/clip generation.
    internal sealed class Task13PlayerProbe : MonoBehaviour
    {
        const string Key="NTR.Tests.Task13.AudioCapture";
        GameManager manager;AudioController audio;double started;string folder;bool focusInterval;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Isolate()
        {UnityEngine.Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include).ConfigureHighScores(new HighScoreStore(Key));}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()=>new GameObject("Task13AudioFixture").AddComponent<Task13PlayerProbe>();
        void Mark(string label)
        {
            var s=manager.Session.Snapshot;
            File.AppendAllText(Path.Combine(folder,"timeline.jsonl"),JsonUtility.ToJson(new Stamp{label=label,utc=DateTime.UtcNow.ToString("O"),seconds=Time.realtimeSinceStartupAsDouble-started,state=manager.State.ToString(),actualSpeed=s.ActualSpeed,pitch=audio.EngineSource.pitch,voices=audio.ActiveVoices,peak=audio.PeakVoices,requests=(long)audio.Requests,dropped=(long)audio.Dropped,stolen=(long)audio.Stolen,musicStarts=(long)audio.MusicStarts,engineStarts=(long)audio.EngineStarts,sources=FindObjectsByType<AudioSource>().Length,pooled=(long)manager.Pools.TotalCreated,misses=(long)manager.Pools.Misses})+"\n");
        }
        [Serializable] class Stamp{public string label,utc,state;public double seconds;public float actualSpeed,pitch;public int voices,peak,sources;public long requests,dropped,stolen,musicStarts,engineStarts,pooled,misses;}
        void Update()
        {
            // Capture fixture advances while the desktop is used; production focus policy is unchanged.
            if(audio)audio.SetApplicationSuspension(focusInterval,false);
        }
        void Tick()
        {var l=manager.Loop;l.enabled=true;try{if(!l.Step(.02f))throw new Exception("Fixture tick rejected");l.Player.HoldPose();}finally{l.enabled=false;}}
        void Pickup(PoolKey key,long id)
        {
            var l=manager.Loop;var s=manager.Session.Snapshot;
            if(!manager.Pools.TryRent(new SpawnContext(key,l.Player.CurrentPosition,Quaternion.identity,l.Planner.OriginRoot,0,s.Distance,s.Seed,0,id,l.Planner),out var item))throw new Exception("Fixture pickup rent failed");
            Tick();
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-ntr-evidence");folder=i>=0?args[i+1]:"Evidence/Task13/Standalone";Directory.CreateDirectory(folder);
            manager=GameManager.Instance;audio=manager.Loop.Audio;manager.Loop.enabled=false;Application.runInBackground=true;
            started=Time.realtimeSinceStartupAsDouble;Mark("menu/music-only");yield return new WaitForSecondsRealtime(3);
            manager.StartRun();Mark("engine/actual22");yield return new WaitForSecondsRealtime(3);
            manager.Session.Advance(180,0);Tick();Mark("engine/actual50");ScreenCapture.CaptureScreenshot(Path.Combine(folder,"player-1280x720.png"));yield return new WaitForSecondsRealtime(3);
            Pickup(PoolKey.Coin,991001);Mark("coin");yield return new WaitForSecondsRealtime(1);
            manager.Session.SetResources(3,100);Pickup(PoolKey.Fuel,991002);Mark("full-fuel");yield return new WaitForSecondsRealtime(1);
            Pickup(PoolKey.Stationary,991003);Mark("accepted-collision");yield return new WaitForSecondsRealtime(1);
            for(int v=0;v<8;v++)audio.Request(AudioCue.Fuel);audio.Request(AudioCue.Coin);audio.Request(AudioCue.Collision);Mark("saturation/8/drop/steal");yield return new WaitForSecondsRealtime(1);
            audio.Request(AudioCue.Fuel);yield return new WaitForSecondsRealtime(.06f);manager.Pause();Mark("paused-owned-fuel");yield return new WaitForSecondsRealtime(2);
            manager.Resume();Mark("resumed-fuel");yield return new WaitForSecondsRealtime(1);
            focusInterval=true;Mark("focus-lost");yield return new WaitForSecondsRealtime(1);focusInterval=false;yield return null;Mark("focus-returned");
            manager.Session.Advance(2,0);manager.Session.SetResources(1,10);Pickup(PoolKey.Stationary,991004);Mark("terminal-collision/engine-stopped");yield return new WaitForSecondsRealtime(1);
            Mark("terminal-completed");manager.Restart();Mark("restart");yield return new WaitForSecondsRealtime(1);
            manager.Pause();manager.ReturnToMenu();Mark("menu/music-preserved");
            // Let the imported streaming music cross its 64-second loop seam at normal pitch.
            while(Time.realtimeSinceStartupAsDouble-started<68)yield return null;
            Mark("music-after-loop-join");
            for(int run=0;run<50;run++){manager.StartRun();audio.Request(AudioCue.Coin);manager.Pause();manager.Restart();manager.Pause();manager.ReturnToMenu();}
            Mark("50-reset-cycles-complete");manager.Shutdown();PlayerPrefs.DeleteKey(Key);PlayerPrefs.Save();yield return new WaitForSecondsRealtime(.25f);Application.Quit();
        }
    }
}
#endif
