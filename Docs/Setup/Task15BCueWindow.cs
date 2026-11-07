using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace NeonTunnelRunner
{
    public static class Task15BCueWindow
    {
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static void Check(bool b,string m){if(!b)throw new Exception(m);}
        public static async Task<object> Run()
        {
            var m=GameManager.Instance;Check(m && m.State==GameState.Menu,"Menu required");
            string key="NTR.Tests.Task15B.CueWindow."+Guid.NewGuid().ToString("N");
            typeof(GameManager).GetField("highScores",F).SetValue(m,new HighScoreStore(key));
            var pauses=new List<object>();var a=m.Loop.Audio;
            Action<RunSnapshot> dodge=snapshot=>
            {
                if(m.State==GameState.Playing && m.NearMiss.Active && m.NearMiss.Age>=m.NearMiss.ApproachEnd &&
                    m.Loop.Player.LaneIndex==0 && !m.Loop.Player.IsTransitioning) m.Loop.Player.RequestLane(1);
            };
            m.Session.SnapshotChanged+=dodge;
            try
            {
                Check(m.StartRun(),"Start");typeof(AudioController).GetMethod("SetApplicationSuspension",F).Invoke(a,new object[]{false,false});
                var l=m.Loop;double z=m.Session.Snapshot.Distance+35d;
                Check(m.Pools.TryRent(new SpawnContext(PoolKey.Stationary,new Vector3(0f,.6f,(float)z),Quaternion.identity,l.Planner.OriginRoot,0,z,12345,0,900000,l.Planner),out var car),"Rental");
                var deadline=DateTime.UtcNow.AddSeconds(4);
                while(!m.NearMiss.Active){Check(DateTime.UtcNow<deadline,"No true close pass");await Task.Delay(8);}
                double start=Time.realtimeSinceStartupAsDouble-m.NearMiss.Age,paused=0;
                ulong musicStarts=a.MusicStarts;int normalStart=a.MusicSource.timeSamples;
                foreach(float at in new[]{.075f,2f,5.8f})
                {
                    while(m.NearMiss.Age<at && m.NearMiss.Active)await Task.Delay(8);
                    Check(m.NearMiss.Active,"Effect ended early");m.Pause();
                    double pauseStart=Time.realtimeSinceStartupAsDouble;float age=m.NearMiss.Age,weight=m.NearMiss.Weight;
                    int sample=a.NearMissSource.timeSamples;await Task.Delay(250);
                    Check(m.NearMiss.Age==age && m.NearMiss.Weight==weight,"Pause phase changed");
                    Check(a.NearMissSource.timeSamples==sample,"Pause cue sample changed");
                    Check(Time.timeScale==0,"Pause time");paused+=Time.realtimeSinceStartupAsDouble-pauseStart;
                    pauses.Add(new{requestedAge=at,age,weight,sample,pausedPositionExact=true});m.Resume();
                }
                while(m.NearMiss.Active)await Task.Delay(8);
                double ended=Time.realtimeSinceStartupAsDouble;
                Check(m.NearMissPassCompleted,"Behind hold or actual pass was not completed");
                Check(!a.NearMissSource.isPlaying,"Cue exceeds effect");
                Check(a.NearMissSource.pitch==1f && a.MusicStarts==musicStarts,"Pitch/continuity");
                Check(Math.Abs(ended-start-paused-6d)<.12d,"Six real Playing second window mismatch: playing="+(ended-start-paused)+" wall="+(ended-start)+" paused="+paused+" age="+m.NearMiss.Age);
                return new{passed=true,effectPlayingSeconds=ended-start-paused,effectConfiguredSeconds=m.NearMiss.Duration,
                    sourceExcerptSeconds=a.Configuration.NearMiss.length,sourceStartSeconds=0,pitch=a.NearMissSource.pitch,
                    wallSeconds=ended-start,pausedSeconds=paused,pauses,normalStart,normalEnd=a.MusicSource.timeSamples,
                    normalMusicStartsBefore=musicStarts,normalMusicStartsAfter=a.MusicStarts,nearMissCount=m.NearMiss.Accepted,
                    scaleRestored=Time.timeScale,cueStopped=true,passCompleted=m.NearMissPassCompleted,
                    method="Anticipated forward near miss with staged approach/pass/after; actual car bounds;, real frames, entry/hold/exit Pause and exact sample checks; no manual effect trigger."};
            }
            finally
            {
                m.Session.SnapshotChanged-=dodge;
                if(m.State==GameState.Playing)m.Pause();if(m.State==GameState.Paused)m.ReturnToMenu();
                PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
            }
        }
    }
}
