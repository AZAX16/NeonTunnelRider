using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
namespace NeonTunnelRunner
{
    public static class Task15BWarningVisual
    {
        const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
        static void Guide(GameplayLoop l)
        {
            double d=l.Owner.Session.Snapshot.Distance;var p=l.Planner.PlanAt((long)(d/480d));
            int target=d-p.AbsoluteStart>=360d+l.Owner.Session.Snapshot.CruiseSpeed*l.Planner.Configuration.ReactionTime ? p.NextProtectedLane : p.ProtectedLane;
            if(!l.Player.IsTransitioning && l.Player.LaneIndex!=target)l.Player.RequestLane(Math.Sign(target-l.Player.LaneIndex));
        }
        public static async Task<object> Run()
        {
            var m=GameManager.Instance;if(!m || m.State!=GameState.Menu)throw new Exception("Menu required");
            var key="NTR.Tests.Task15B.Warning."+Guid.NewGuid().ToString("N");
            typeof(GameManager).GetField("highScores",F).SetValue(m,new HighScoreStore(key));
            var l=m.Loop;var rows=new List<object>();string output="Evidence/Task15B/WarningVisual";Directory.CreateDirectory(output);
            bool wasEnabled=Profiler.enabled,wasBinary=Profiler.enableBinaryLog,wasStacks=Profiler.enableAllocationCallstacks;string oldFile=Profiler.logFile;
            try
            {
                m.StartRun();for(int tick=0;tick<2400 && l.Planner.RearWarningCount==0;tick++){Guide(l);l.Step(.02f);}
                if(l.Planner.RearWarningCount==0)throw new Exception("Seed did not produce warning");
                int lane=-2;for(int i=-1;i<=1;i++)if(l.Planner.RearWarningRemaining(i)>0)lane=i;
                Profiler.enabled=false;Profiler.enableBinaryLog=false;Profiler.logFile="Evidence/Task15B/warning-focused.raw";Profiler.enableAllocationCallstacks=true;Profiler.enableBinaryLog=true;Profiler.enabled=true;
                ulong spawned=l.Planner.SpawnedOvertakers;double start=Time.realtimeSinceStartupAsDouble;int frames=Time.frameCount;
                bool captured=false;
                while(l.Planner.RearWarningRemaining(lane)>0)
                {
                    Guide(l);float left=l.Planner.RearWarningRemaining(lane);
                    rows.Add(new{real=Time.realtimeSinceStartupAsDouble,lane,left,spawned=l.Planner.SpawnedOvertakers,frame=Time.frameCount});
                    if(l.Planner.SpawnedOvertakers!=spawned)throw new Exception("Car spawned before warning completed");
                    if(!captured && Time.realtimeSinceStartupAsDouble-start>.15d)
                    {captured=true;ScreenCapture.CaptureScreenshot(output+"/lane-hazard-before-rear-entry.png");await Task.Delay(180);}
                    await Task.Delay(16);
                }
                double duration=Time.realtimeSinceStartupAsDouble-start;
                if(l.Planner.SpawnedOvertakers!=spawned+1)throw new Exception("Warned lane was unexpectedly blocked");
                if(duration<1.3d || duration>1.8d)throw new Exception("Warning real-time window mismatch "+duration);
                await Task.Delay(250);ScreenCapture.CaptureScreenshot(output+"/after-rear-entry.png");await Task.Delay(180);
                return new{passed=true,seconds=duration,lane,spawnedBefore=spawned,spawnedAfter=l.Planner.SpawnedOvertakers,
                    frames=Time.frameCount-frames,trace=rows,created=m.Pools.TotalCreated,misses=m.Pools.Misses,
                    method="Real seeded pending entry, advancing gameplay frames, HUD flash then rear rental; no manual warning or effect trigger"};
            }
            finally
            {
                Profiler.enabled=false;Profiler.enableBinaryLog=false;Profiler.logFile=oldFile;Profiler.enableAllocationCallstacks=wasStacks;Profiler.enableBinaryLog=wasBinary;Profiler.enabled=wasEnabled;
                if(m.State==GameState.Playing)m.Pause();if(m.State==GameState.Paused)m.ReturnToMenu();
                PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
            }
        }
    }
}
