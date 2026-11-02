using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
namespace NeonTunnelRunner
{
    public static class Task10Aspect
    {
        public static async Task<object> Run()
        {
            var m=GameManager.Instance; if(!m || m.State!=GameState.Menu) throw new InvalidOperationException("Enter Game in Menu.");
            string key="NTR.Tests.Task10.Aspect."+Guid.NewGuid().ToString("N"); var store=new HighScoreStore(key);
            typeof(GameManager).GetField("highScores",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(m,store);
            bool old=Application.runInBackground; Application.runInBackground=true;
            try
            {
                m.StartRun();
                typeof(RunSession).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m.Session,new object[]{180f,0d});
                var l=m.Loop; int desired=l.Planner.PlanAt(0).ProtectedLane;
                while(l.Player.LaneIndex!=desired) { l.Player.RequestLane(Math.Sign(desired-l.Player.LaneIndex)); await Task.Delay(320); }
                for(int i=0;m.Session.Snapshot.Distance<260d;i++) { if(i>1000 || m.State!=GameState.Playing) throw new InvalidOperationException("Aspect route stopped."); await Task.Delay(16); }
                float actual=m.Session.Snapshot.ActualSpeed; m.Pause(); await Task.Delay(60);
                ScreenCapture.CaptureScreenshot("Evidence/Task10/cap-tunnel-4x3.png"); await Task.Delay(150);
                return new {passed=true,width=Screen.width,height=Screen.height,aspect=l.Camera.ViewCamera.aspect,actual,
                    distance=m.Session.Snapshot.Distance,lane=l.Player.LaneIndex,relative=(l.Camera.transform.position-l.Player.transform.position).ToString("F4")};
            }
            finally { Application.runInBackground=old; store.Flush(); PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
        }
    }
}
