using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
namespace NeonTunnelRunner
{
    public static class Task15BTrafficVisual
    {
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static void Guide(GameplayLoop loop)
        {
            double d=loop.Owner.Session.Snapshot.Distance;var plan=loop.Planner.PlanAt((long)(d/480d));
            int target=d-plan.AbsoluteStart>=360d+loop.Owner.Session.Snapshot.CruiseSpeed*loop.Planner.Configuration.ReactionTime ? plan.NextProtectedLane : plan.ProtectedLane;
            if(!loop.Player.IsTransitioning && loop.Player.LaneIndex!=target)loop.Player.RequestLane(Math.Sign(target-loop.Player.LaneIndex));
        }
        static void Reset(GameManager m,int seed)
        {
            if(m.State==GameState.Playing)m.Pause();
            typeof(GameManager).GetField("runSeed",F).SetValue(m,seed);
            bool ok=m.State==GameState.Menu ? m.StartRun() : m.Restart();if(!ok)throw new Exception("Reset failed");
        }
        public static async Task<object> Run()
        {
            var m=GameManager.Instance;if(!m || m.State!=GameState.Menu)throw new Exception("Menu required");
            var seedField=typeof(GameManager).GetField("runSeed",F);int originalSeed=(int)seedField.GetValue(m);
            string key="NTR.Tests.Task15B.TrafficVisual."+Guid.NewGuid().ToString("N");
            typeof(GameManager).GetField("highScores",F).SetValue(m,new HighScoreStore(key));
            string output="Evidence/Task15B/TrafficVisual";Directory.CreateDirectory(output);
            bool oldEnabled=Profiler.enabled,oldBinary=Profiler.enableBinaryLog,oldStacks=Profiler.enableAllocationCallstacks;string oldLog=Profiler.logFile;
            int seedFound=-1,tickFound=0;var loop=m.Loop;var trace=new List<object>();
            try
            {
                for(int seed=0;seed<32 && seedFound<0;seed++)
                {
                    Reset(m,seed);
                    for(int tick=0;tick<2400;tick++)
                    {
                        Guide(loop);if(!loop.Step(.1f))throw new Exception("Replay pre-roll failure");
                        if(loop.Planner.AcceptedLaneChanges==0)continue;
                        seedFound=seed;tickFound=tick;break;
                    }
                }
                if(seedFound<0)throw new Exception("No seeded lane maneuver found");
                Reset(m,seedFound);
                for(int tick=0;tick<Math.Max(0,tickFound-25);tick++){Guide(loop);loop.Step(.1f);}
                typeof(PlayerController).GetMethod("HoldPose",F).Invoke(loop.Player,null);
                loop.Camera.ResetPresentation();
                Profiler.enabled=false;Profiler.enableBinaryLog=false;Profiler.logFile="Evidence/Task15B/traffic-focused.raw";Profiler.enableAllocationCallstacks=true;Profiler.enableBinaryLog=true;Profiler.enabled=true;
                int frame=Time.frameCount;double began=Time.realtimeSinceStartupAsDouble;
                bool maneuver=false,pass=false,side=false;
                while(Time.realtimeSinceStartupAsDouble-began<16d)
                {
                    Guide(loop);
                    for(int i=0;i<loop.Planner.ActiveCount;i++)
                    {
                        var lease=loop.Planner.GetActive(i);if(!(lease.Item is MovingCar car) || !car.IsOvertaker)continue;
                        double relative=car.AbsoluteZ-m.Session.Snapshot.Distance;
                        trace.Add(new{real=Time.realtimeSinceStartupAsDouble,frame=Time.frameCount,activation=lease.ActivationId,car.SpawnSequence,
                            playerZ=m.Session.Snapshot.Distance,z=car.AbsoluteZ,relative,x=car.CurrentPosition.x,car.Lane,car.TargetLane,car.ChangingLane,
                            car.Speed,playerSpeed=m.Session.Snapshot.CruiseSpeed,scale=Time.timeScale,car.AcceptedLaneChanges,
                            coverageStart=loop.Roads.CoverageStart,coverageEnd=loop.Roads.CoverageEnd});
                        string label=null;
                        if(!maneuver && car.ChangingLane){maneuver=true;label="seeded-adjacent-maneuver";}
                        else if(!side && relative>-4 && relative<4){side=true;label="catch-up-and-pass";}
                        else if(!pass && relative>6){pass=true;label="continuing-ahead";}
                        if(label!=null){ScreenCapture.CaptureScreenshot(output+"/"+label+".png");await Task.Delay(180);}
                    }
                    if(maneuver && pass && side && loop.Planner.CompletedOvertakes>0)break;
                    await Task.Delay(33);
                }
                if(!maneuver || !pass || !side)throw new Exception("Seeded visual event incomplete");
                return new{passed=true,seed=seedFound,preRollTicks=Math.Max(0,tickFound-25),frames=Time.frameCount-frame,maneuver,side,pass,trace,
                    spawned=loop.Planner.SpawnedOvertakers,changes=loop.Planner.AcceptedLaneChanges,completed=loop.Planner.CompletedOvertakes,
                    created=m.Pools.TotalCreated,misses=m.Pools.Misses,
                    method="Actual seeded gameplay pre-roll through Step(.1), then advancing real Editor frames. Test guide follows protected route. No car teleport or resource grants; isolated score key."};
            }
            finally
            {
                Profiler.enabled=false;Profiler.enableBinaryLog=false;Profiler.logFile=oldLog;Profiler.enableAllocationCallstacks=oldStacks;Profiler.enableBinaryLog=oldBinary;Profiler.enabled=oldEnabled;
                if(m.State==GameState.Playing)m.Pause();if(m.State==GameState.Paused)m.ReturnToMenu();seedField.SetValue(m,originalSeed);
                PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
            }
        }
    }
}
