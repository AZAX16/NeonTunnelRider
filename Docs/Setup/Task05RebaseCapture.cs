using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace NeonTunnelRunner
{
    public static class Task05RebaseCapture
    {
        public static async Task<object> Run()
        {
            var manager=GameManager.Instance;
            if (!Application.isPlaying || !manager || manager.State!=GameState.Menu || !manager.StartRun())
                throw new InvalidOperationException("Start this native capture in the saved scene's Menu.");
            bool background=Application.runInBackground;
            Application.runInBackground=true;
            try
            {
                // Test-only speed injection through the existing session API; immutable config is untouched.
                typeof(RunSession).GetMethod("SetCruiseSpeed", BindingFlags.Instance|BindingFlags.NonPublic)
                    .Invoke(manager.Session,new object[] { manager.Configuration.MaximumSpeed });
                int firstFrame=Time.frameCount;
                for (int ms=0; manager.Session.Snapshot.Distance<2020d; ms+=20)
                {
                    if (ms>80000) throw new TimeoutException("Native maximum-speed capture did not reach 2020m.");
                    await Task.Delay(20);
                }
                manager.Pause();
                var loop=manager.Loop; var snapshot=manager.Session.Snapshot;
                if (loop.Origin.RebaseCount<2 || Time.frameCount<=firstFrame || manager.Pools.TotalCreated!=24 ||
                    manager.Pools.Misses!=0 || loop.Roads.CoverageEnd<snapshot.Distance+300d)
                    throw new InvalidOperationException("Native capture invariants failed.");
                ScreenCapture.CaptureScreenshot("Evidence/Task05/native-after-two-rebases.png");
                await Task.Delay(250); // Let the paused Game view render before returning any road leases.
                var result=new { passed=true, speed=manager.Configuration.MaximumSpeed, snapshot.Distance,
                    snapshot.ElapsedTime, frame=Time.frameCount, tick=loop.TickIndex,
                    offset=loop.Origin.Offset, rebases=loop.Origin.RebaseCount,
                    localPlayer=loop.Player.CurrentPosition.ToString("F3"),
                    cameraOffset=(loop.Origin.ChaseCamera.position-loop.Player.transform.position).ToString("F3"),
                    loop.Roads.ActiveCount, manager.Pools.TotalCreated, manager.Pools.TotalDestroyed, manager.Pools.Misses };
                manager.ReturnToMenu();
                return result;
            }
            finally { Application.runInBackground=background; }
        }
    }
}
