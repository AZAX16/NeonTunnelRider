using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace NeonTunnelRunner
{
    public sealed class PersistenceTests
    {
        private sealed class Preferences : IHighScorePreferences
        {
            public int Value,Reads,Writes,Saves;
            public int Read(string key) { Reads++; return Value; }
            public void Write(string key,int value) { Writes++; Value=value; }
            public void Save() => Saves++;
        }
        [TestCase(0)] [TestCase(-42)] [TestCase(123)]
        public void LoadOnceAndFlushOnlyHigherDirtyCandidates(int value)
        {
            var p=new Preferences {Value=value}; var s=new HighScoreStore("isolated",p);
            Assert.That(s.Load(),Is.EqualTo(Math.Max(0,value))); s.Load(); Assert.That(p.Reads,Is.EqualTo(1));
            Assert.That(s.Flush(),Is.False); Assert.That(s.SetIfHigher(-1),Is.False);
            Assert.That(s.SetIfHigher(700),Is.True); s.SetIfHigher(200); Assert.That(p.Writes+p.Saves,Is.Zero);
            Assert.That(s.Flush(),Is.True); Assert.That(p.Value,Is.EqualTo(700)); Assert.That(s.Flush(),Is.False);
            Assert.That(p.Saves,Is.EqualTo(1)); Assert.That(p.Writes,Is.EqualTo(1));
            s.SetIfHigher(800); p.Value=900; s.Flush(); Assert.That(s.Best,Is.EqualTo(900));
        }
        [Test] public void RealPlayerPrefsUsesOnlyAUniqueKeyAndSurvivesStoreReconstruction()
        {
            string key="NTR.Tests.Task09."+Guid.NewGuid().ToString("N");
            try
            {
                Assert.That(new HighScoreStore(key).Load(),Is.Zero);
                PlayerPrefs.SetInt(key,-5); Assert.That(new HighScoreStore(key).Load(),Is.Zero);
                var store=new HighScoreStore(key); store.SetIfHigher(4321); store.Flush();
                var next=new HighScoreStore(key); Assert.That(next.Load(),Is.EqualTo(4321));
                Assert.That(next.SetIfHigher(100),Is.False); Assert.That(next.Flush(),Is.False);
            }
            finally { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
        }
        [TestCase("end")] [TestCase("menu")] [TestCase("quit")] [TestCase("applicationPause")] [TestCase("shutdown")] [TestCase("restart")]
        public void BoundariesFlushTheOutgoingBestOnceWithoutFrameSaves(string boundary)
        {
            var p=new Preferences(); var store=new HighScoreStore("isolated",p);
            var config=ScriptableObject.CreateInstance<GameplayConfig>();
            var previous=GameManager.HighScoreTestFactory; GameManager.HighScoreTestFactory=()=>store;
            GameObject root=null;
            try
            {
                root=PlayModeMovementRig.Create("PersistenceFixture",config); var gm=root.GetComponent<GameManager>();
                gm.StartRun();
                for(int i=0;i<20;i++) gm.Loop.Step(.02f);
                gm.Loop.Progression.AddCoin(); var s=gm.Session.Snapshot;
                Assert.That(s.BestScore,Is.EqualTo(s.Score)); Assert.That(s.Score,Is.GreaterThanOrEqualTo(100));
                for(int i=0;i<20;i++) gm.Session.PublishSnapshot();
                gm.Pause(); Assert.That(p.Saves+p.Writes,Is.Zero,"Ordinary pause and snapshots must not flush.");
                switch(boundary)
                {
                    case "end": gm.Resume(); gm.EndRun(GameOverReason.Fuel); gm.EndRun(GameOverReason.Fuel); break;
                    case "menu": gm.ReturnToMenu(); gm.ReturnToMenu(); break;
                    case "quit": gm.ReturnToMenu(); LogAssert.Expect(LogType.Log,"[GameManager] Quit requested (Editor intent only)."); gm.Quit(); gm.Quit(); break;
                    case "applicationPause": Callback(gm,"OnApplicationPause",false); Assert.That(p.Saves,Is.Zero); Callback(gm,"OnApplicationPause",true); Callback(gm,"OnApplicationPause",true); break;
                    case "shutdown": Callback(gm,"OnApplicationQuit"); Callback(gm,"OnApplicationQuit"); break;
                    case "restart": gm.Restart(); Assert.That(gm.Session.Snapshot.Score,Is.Zero); Assert.That(gm.Session.Snapshot.BestScore,Is.EqualTo(s.Score)); break;
                }
                Assert.That(p.Value,Is.EqualTo(s.Score)); Assert.That(p.Saves,Is.EqualTo(1)); Assert.That(p.Writes,Is.EqualTo(1));
                if(gm.IsOwner) { gm.Shutdown(); Assert.That(p.Saves,Is.EqualTo(1)); }
            }
            finally { if(root) Object.DestroyImmediate(root); Object.DestroyImmediate(config); GameManager.HighScoreTestFactory=previous; }
        }
        [Test] public void ExplicitMenuQuitFlushesAnAlreadyDirtyCandidate()
        {
            var p=new Preferences(); var store=new HighScoreStore("isolated",p);
            var config=ScriptableObject.CreateInstance<GameplayConfig>(); var factory=GameManager.HighScoreTestFactory;
            GameManager.HighScoreTestFactory=()=>store; GameObject root=null;
            try
            {
                root=PlayModeMovementRig.Create("MenuQuitPersistence",config); var gm=root.GetComponent<GameManager>();
                store.SetIfHigher(500);
                LogAssert.Expect(LogType.Log,"[GameManager] Quit requested (Editor intent only).");
                gm.Quit(); gm.Quit();
                Assert.That(p.Value,Is.EqualTo(500)); Assert.That(p.Saves,Is.EqualTo(1));
                Assert.That(gm.Session.Snapshot.BestScore,Is.EqualTo(500));
            }
            finally { if(root) Object.DestroyImmediate(root); Object.DestroyImmediate(config); GameManager.HighScoreTestFactory=factory; }
        }
        [Test] public void TerminalTickCapturesCoinAndFinalDistanceBeforeFlushAndRestart()
        {
            var p=new Preferences(); var store=new HighScoreStore("isolated",p);
            var config=ScriptableObject.CreateInstance<GameplayConfig>(); var factory=GameManager.HighScoreTestFactory;
            GameManager.HighScoreTestFactory=()=>store; GameObject root=null;
            try
            {
                root=PlayModeMovementRig.Create("TerminalPersistence",config); var gm=root.GetComponent<GameManager>(); gm.StartRun();
                gm.Session.SetResources(1,.04f);
                foreach(var key in new[] {PoolKey.Stationary,PoolKey.Coin})
                    Assert.That(gm.Pools.TryRent(new SpawnContext(key,config.StartingPosition,Quaternion.identity,gm.Loop.Planner.OriginRoot,0,0,12345,0,(long)key,gm.Loop.Planner),out _),Is.True);
                gm.RunEnded+=(s,r)=> { Assert.That(s.BestScore,Is.GreaterThanOrEqualTo(s.Score)); Assert.That(p.Value,Is.EqualTo(s.BestScore)); };
                gm.Loop.Step(.1f); var final=gm.Session.Snapshot;
                Assert.That(final.Score,Is.EqualTo(102)); Assert.That(final.BestScore,Is.EqualTo(102)); Assert.That(p.Saves,Is.EqualTo(1));
                gm.Restart(); Assert.That(gm.Session.Snapshot.BestScore,Is.EqualTo(102));
                gm.EndRun(GameOverReason.Fuel); Assert.That(p.Value,Is.EqualTo(102)); Assert.That(p.Saves,Is.EqualTo(1));
            }
            finally { if(root) Object.DestroyImmediate(root); Object.DestroyImmediate(config); GameManager.HighScoreTestFactory=factory; }
        }
        private static void Callback(GameManager gm,string name,params object[] args)=>typeof(GameManager).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gm,args);
    }
}
