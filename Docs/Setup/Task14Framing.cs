using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace NeonTunnelRunner
{
    public static class Task14Framing
    {
        public static async Task<object> Run()
        {
            var m=GameManager.Instance;if(!m || m.State!=GameState.Menu)throw new Exception("Open Game in Menu");
            var f=BindingFlags.NonPublic|BindingFlags.Instance;string key="NTR.Tests.Task14.Framing."+Guid.NewGuid().ToString("N");
            typeof(GameManager).GetField("highScores",f).SetValue(m,new HighScoreStore(key));
            var behavior=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();bool background=Application.runInBackground;Application.runInBackground=true;
            try
            {
                // This isolated camera measurement uses the existing command only to set up
                // the world. Required button interaction is proven by UiBindingTests/native runs.
                if(!m.StartRun())throw new Exception("Framing setup Start failed");
                var deadline=DateTime.UtcNow.AddSeconds(8);var v=m.Loop.Vfx;
                while(!Enumerable.Range(0,v.ActiveCount).Select(v.GetActive).Any(e=>e.Key==PoolKey.Firework&&e.GetComponentsInChildren<ParticleSystem>().Any(p=>p!=e.Primary&&p.particleCount>0)))
                {if(DateTime.UtcNow>deadline)throw new Exception("Firework unavailable");await Task.Delay(16);}
                await Task.Delay(250);ScreenCapture.CaptureScreenshot("Evidence/Task14/normal-1280x720-hud-firework.png");await Task.Delay(200);
                m.Pause();var c=m.Loop.Camera.ViewCamera;var result=new List<object>();
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(960,720),new Vector2Int(1920,1080),new Vector2Int(2520,1080)})
                {float old=c.aspect;c.aspect=(float)size.x/size.y;for(int lane=-1;lane<=1;lane++)
                    {var p=c.WorldToViewportPoint(m.Loop.Player.CurrentPosition+new Vector3(lane*3.2f,0,120));if(p.z<=0||p.x<.05||p.x>.95||p.y<.1||p.y>.9)throw new Exception("120 m framing");result.Add(new{width=size.x,height=size.y,lane,p.x,p.y,p.z});}c.aspect=old;}
                return new {passed=true,width=Screen.width,height=Screen.height,points=result};
            }
            finally {InputSystem.settings.editorInputBehaviorInPlayMode=behavior;InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);Application.runInBackground=background;typeof(GameManager).GetMethod("Shutdown",f).Invoke(m,null);PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
        }
    }
}
