using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
namespace NeonTunnelRunner
{
    public static class Task16EffectsCapture
    {
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        public static async Task<object> Run()
        {
            var m=GameManager.Instance;
            if(!m || m.State!=GameState.Menu)throw new Exception("Menu required");
            var field=typeof(GameManager).GetField("highScores",F);var old=field.GetValue(m);
            string key="NTR.Tests.Task16.Effects."+Guid.NewGuid().ToString("N");
            PlayerPrefs.SetInt(key,PlayerPrefs.GetInt("NTR.HighScore.v1",0));field.SetValue(m,new HighScoreStore(key));
            var rows=new List<object>();
            try
            {
                m.StartRun();
                typeof(AudioController).GetMethod("SetApplicationSuspension",F).Invoke(m.Loop.Audio,new object[]{false,false});
                PooledVfx firework=null;var deadline=DateTime.UtcNow.AddSeconds(12);
                while(firework==null || firework.Age<1.4f || firework.ParticleCount<20)
                {
                    if(DateTime.UtcNow>deadline)throw new Exception("Natural burst not observed");
                    for(int i=0;i<m.Loop.Vfx.ActiveCount;i++)if(m.Loop.Vfx.GetActive(i).Key==PoolKey.Firework)firework=m.Loop.Vfx.GetActive(i);
                    await Task.Delay(8);
                }
                rows.Add(new{name="editor-fireworks",method="Natural scheduled firework; wait for actual burst particles",firework.Age,firework.ParticleCount,frame=Time.frameCount});
                ScreenCapture.CaptureScreenshot("delivery/Screenshots/editor-fireworks.png");await Task.Delay(180);
                m.Pause();m.ReturnToMenu();
                foreach(var type in new[]{PoolKey.Coin,PoolKey.Fuel})
                {
                    m.StartRun();typeof(SpawnDirector).GetMethod("ReleaseAll",F).Invoke(m.Loop.Planner,null);
                    double z=m.Session.Snapshot.Distance+5d;
                    if(!m.Pools.TryRent(new SpawnContext(type,new Vector3(0,.6f,(float)z),Quaternion.identity,
                        m.Loop.Planner.OriginRoot,0,z,12345,0,880000,m.Loop.Planner),out var item))throw new Exception("Rent");
                    deadline=DateTime.UtcNow.AddSeconds(3);
                    while(item.IsRented){if(DateTime.UtcNow>deadline)throw new Exception("Pickup contact");await Task.Delay(4);}
                    m.Loop.Player.RequestLane(1);await Task.Delay(95);
                    string name=type==PoolKey.Coin?"editor-coin-pickup":"editor-fuel-pickup";
                    rows.Add(new{name,method="Actual controlled pooled pickup contact, then ordinary adjacent move exposes detached burst",score=m.Session.Snapshot.Score,fuel=m.Session.Snapshot.Fuel,frame=Time.frameCount});
                    ScreenCapture.CaptureScreenshot("delivery/Screenshots/"+name+".png");await Task.Delay(180);
                    m.Pause();m.ReturnToMenu();
                }
                return new{passed=true,rows,origin="Editor Play Mode on Linux; no composition or particle/config change"};
            }
            finally
            {
                if(m.State==GameState.Playing)m.Pause();if(m.State==GameState.Paused)m.ReturnToMenu();
                field.SetValue(m,old);PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
            }
        }
    }
}
