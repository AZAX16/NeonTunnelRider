using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace NeonTunnelRunner
{
    public static class Task10Motion
    {
        public static async Task<object> Run()
        {
            var m=GameManager.Instance; if(!m || m.State!=GameState.Menu) throw new InvalidOperationException("Enter saved Game in Menu.");
            string key="NTR.Tests.Task10.Motion."+Guid.NewGuid().ToString("N"); var store=new HighScoreStore(key);
            typeof(GameManager).GetField("highScores",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(m,store);
            bool background=Application.runInBackground; var behavior=InputSystem.settings.editorInputBehaviorInPlayMode;
            var focus=InputSystem.settings.backgroundBehavior; var keyboard=InputSystem.AddDevice<Keyboard>("Task10MotionKeyboard");
            try
            {
                Application.runInBackground=true;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                var l=m.Loop; l.Input.Actions.devices=new InputDevice[]{keyboard}; m.StartRun();
                typeof(RunSession).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m.Session,new object[]{180f,0d});
                await Until(()=>l.TickIndex>2);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));
                await Until(()=>l.Player.IsTransitioning && l.Visuals.Roll < -2f);
                float actual=m.Session.Snapshot.ActualSpeed; m.Pause();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                var motion=new {roll=l.Visuals.Roll,bob=l.Visuals.BobOffset,phase=l.Player.TransitionProgress,
                    actual,rootRotation=l.Player.transform.rotation.ToString("F5"),pivotRotation=l.Visuals.VisualPivot.localRotation.ToString("F5"),
                    camera=(l.Camera.transform.position-l.Player.transform.position).ToString("F5"),frame=Time.frameCount};
                ScreenCapture.CaptureScreenshot("Evidence/Task10/motion-roll-paused.png"); await Task.Delay(150);
                m.Resume(); await Until(()=>!l.Player.IsTransitioning && Mathf.Abs(l.Visuals.Roll)<.03f); m.Pause();
                ScreenCapture.CaptureScreenshot("Evidence/Task10/motion-resumed.png"); await Task.Delay(100);
                m.Restart(); m.Pause();
                var reset=new {roll=l.Visuals.Roll,bob=l.Visuals.BobOffset,pivot=l.Visuals.VisualPivot.localPosition.ToString("F5"),
                    camera=(l.Camera.transform.position-l.Player.transform.position).ToString("F5"),distance=m.Session.Snapshot.Distance};
                if(reset.roll!=0f || reset.bob!=0f || reset.distance!=0d) throw new InvalidOperationException("Reset did not snap.");
                ScreenCapture.CaptureScreenshot("Evidence/Task10/motion-reset-neutral.png"); await Task.Delay(100);
                m.ReturnToMenu(); return new {passed=true,width=Screen.width,height=Screen.height,motion,reset};
            }
            finally
            {
                m.Loop.Input.Actions.devices=null; InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.editorInputBehaviorInPlayMode=behavior; InputSystem.settings.backgroundBehavior=focus;
                Application.runInBackground=background; store.Flush(); PlayerPrefs.DeleteKey(key); PlayerPrefs.Save();
            }
        }
        private static async Task Until(Func<bool> test)
        { for(int i=0;i<300;i++) { if(test()) return; await Task.Delay(16); } throw new TimeoutException("Native motion condition failed."); }
    }
}
