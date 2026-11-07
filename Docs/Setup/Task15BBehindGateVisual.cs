using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace NeonTunnelRunner
{
    public static class Task15BBehindGateVisual
    {
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static GameManager m;
        static string output="Evidence/Task15B/BehindGate/Visual";
        static void Check(bool b,string reason){if(!b)throw new Exception(reason);}
        static async Task Capture(string name)
        {ScreenCapture.CaptureScreenshot(output+"/"+name+".png");await Task.Delay(120);}
        static Interactable Begin(int lane)
        {
            Check(m.State==GameState.Menu && m.StartRun(),"Start");
            typeof(AudioController).GetMethod("SetApplicationSuspension",F).Invoke(m.Loop.Audio,new object[]{false,false});
            double z=m.Session.Snapshot.Distance+35d;
            Check(m.Pools.TryRent(new SpawnContext(PoolKey.Stationary,new Vector3(lane*3.2f,.6f,(float)(z-m.Loop.Origin.Offset)),
                Quaternion.identity,m.Loop.Planner.OriginRoot,lane,z,12345,0,900000,m.Loop.Planner),out var item),"Rent");
            return (Interactable)item;
        }
        static async Task Menu()
        {
            if(m.State==GameState.Playing)Check(m.Pause(),"Pause cleanup");
            await Task.Delay(16);
            if(m.State==GameState.Paused)Check(m.ReturnToMenu(),"Return to Menu cleanup");
        }
        public static async Task<object> Run()
        {
            m=GameManager.Instance;Check(m && m.State==GameState.Menu,"Saved Game Menu required");Directory.CreateDirectory(output);
            string key="NTR.Tests.Task15B.BehindGate."+Guid.NewGuid().ToString("N");
            typeof(GameManager).GetField("highScores",F).SetValue(m,new HighScoreStore(key));
            var cases=new List<object>();var trace=new List<object>();bool dodge=false;
            Action<RunSnapshot> guide=s=>
            {
                if(dodge && m.State==GameState.Playing && m.NearMiss.Active && m.NearMiss.Age>=m.NearMiss.ApproachEnd &&
                    m.Loop.Player.LaneIndex==0 && !m.Loop.Player.IsTransitioning)m.Loop.Player.RequestLane(1);
            };
            m.Session.SnapshotChanged+=guide;
            try
            {
                var beside=Begin(1);double stop=Time.realtimeSinceStartupAsDouble+2.2d;
                while(Time.realtimeSinceStartupAsDouble<stop) {Check(!m.NearMiss.Active,"Beside car started effect");await Task.Delay(16);}
                await Capture("beside-no-effect");cases.Add(new{scenario="beside",accepted=m.NearMiss.Accepted,passed=true});await Menu();
                var early=Begin(0);var timeout=DateTime.UtcNow.AddSeconds(6);
                while(!m.NearMiss.Active){Check(DateTime.UtcNow<timeout,"No behind attempt");await Task.Delay(16);}
                while(m.NearMiss.Age<1f){Check(DateTime.UtcNow<timeout,"Behind hold did not advance");await Task.Delay(16);}
                await Capture("holding-behind");float exitAge=m.NearMiss.Age;m.Loop.Player.RequestLane(-1);
                while(m.NearMiss.Active){Check(DateTime.UtcNow<timeout,"Early exit did not cancel");await Task.Delay(16);}
                Check(!m.NearMissPassCompleted && !m.Loop.Audio.NearMissSource.isPlaying,"Early exit retained feedback");
                await Capture("early-exit-cancelled");cases.Add(new{scenario="early departure",exitAge,passCompleted=false,passed=true});await Menu();
                var hit=Begin(0);timeout=DateTime.UtcNow.AddSeconds(7);
                while(!hit.Encountered){Check(DateTime.UtcNow<timeout,"Expected normal contact when no dodge");await Task.Delay(16);}
                Check(!m.NearMiss.Active && !m.NearMissPassCompleted && m.Session.Snapshot.Lives==2,"Hit did not cancel");
                await Capture("hit-cancelled");cases.Add(new{scenario="hit",lives=m.Session.Snapshot.Lives,passed=true});await Menu();
                dodge=true;var car=Begin(0);timeout=DateTime.UtcNow.AddSeconds(12);
                while(!m.NearMiss.Active){Check(DateTime.UtcNow<timeout,"No valid hold");await Task.Delay(8);}
                double started=Time.realtimeSinceStartupAsDouble-m.NearMiss.Age;bool passShot=false,afterShot=false;
                while(m.NearMiss.Active)
                {
                    Check(DateTime.UtcNow<timeout,"Valid attempt timed out");
                    trace.Add(new{age=m.NearMiss.Age,x=m.Loop.Player.CurrentPosition.x,playerLane=m.Loop.Player.LaneIndex,
                        gap=car.AbsoluteZ-m.Session.Snapshot.Distance,completed=m.NearMissPassCompleted,scale=Time.timeScale,
                        cueSamples=m.Loop.Audio.NearMissSource.timeSamples,frame=Time.frameCount});
                    if(!passShot && m.NearMiss.Age>2.9f)passShot=true; // Keep blocking screenshot readback outside the one-second pass.
                    if(!afterShot && m.NearMiss.Age>4f){afterShot=true;await Capture("earned-aftermath");}
                    await Task.Delay(16);
                }
                using(var debug=new StreamWriter(output+"/successful-attempt-trace.txt",false))
                { foreach(var row in trace)debug.WriteLine(row.ToString()); }
                Check(m.NearMissPassCompleted && !car.Encountered && m.Session.Snapshot.Lives==3,
                    "Valid hold/pass was not earned: age="+m.NearMiss.Age+" completed="+m.NearMissPassCompleted+
                    " hit="+car.Encountered+" lives="+m.Session.Snapshot.Lives+" gap="+(car.AbsoluteZ-m.Session.Snapshot.Distance)+
                    " x="+m.Loop.Player.CurrentPosition.x+" lane="+m.Loop.Player.LaneIndex);
                Check(!m.Loop.Audio.NearMissSource.isPlaying && Time.timeScale==1f,"Valid effect failed to finish");
                cases.Add(new{scenario="correct hold then clean pass",seconds=Time.realtimeSinceStartupAsDouble-started,completed=true,passed=true});
                return new{passed=true,cases,trace,best=PlayerPrefs.GetInt("NTR.HighScore.v1",0),created=m.Pools.TotalCreated,misses=m.Pools.Misses,
                    sources=UnityEngine.Object.FindObjectsByType<AudioSource>().Length,method="Saved Game, real frames; controlled cars and ordinary adjacent move requests; no direct effect trigger or resource grants; isolated test score"};
            }
            finally{m.Session.SnapshotChanged-=guide;await Menu();PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
        }
    }
}
