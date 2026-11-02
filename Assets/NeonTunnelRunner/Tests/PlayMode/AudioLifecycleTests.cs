using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace NeonTunnelRunner
{
    public sealed class AudioLifecycleTests
    {
        GameObject root;GameplayConfig gameplay;GameManager manager;GameplayLoop loop;AudioController audio;
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
        [SetUp] public void Setup()
        {
            gameplay=ScriptableObject.CreateInstance<GameplayConfig>();root=PlayModeMovementRig.Create("AudioFixture",gameplay);manager=root.GetComponent<GameManager>();loop=manager.Loop;
            var go=new GameObject("Audio");go.transform.SetParent(root.transform,false);audio=go.AddComponent<AudioController>();
            var c=UnityEditor.AssetDatabase.LoadAssetAtPath<AudioConfig>("Assets/NeonTunnelRunner/Configs/Audio.asset");
            Set(audio,"gameManager",manager);Set(audio,"configuration",c);
            AudioSource Source(string n,Transform parent,AudioClip clip,UnityEngine.Audio.AudioMixerGroup group,bool looping)
            {var g=new GameObject(n);g.transform.SetParent(parent,false);var s=g.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=looping;s.clip=clip;s.outputAudioMixerGroup=group;s.dopplerLevel=0;return s;}
            Set(audio,"musicSource",Source("Music",go.transform,c.Music,c.MusicGroup,true));
            Set(audio,"engineSource",Source("Engine",loop.Player.transform,c.Engine,c.EngineGroup,true));
            Set(audio,"voices",Enumerable.Range(0,8).Select(i=>Source("SFX"+i,go.transform,null,c.SfxGroup,false)).ToArray());
            Set(loop,"audioController",audio);audio.Initialize(manager.Session);Assert.That(manager.StartRun(),Is.True);
        }
        [TearDown] public void Cleanup(){if(root)Object.DestroyImmediate(root);if(gameplay)Object.DestroyImmediate(gameplay);Time.timeScale=1;}
        Encounter Hit(long id=1)=>new Encounter(PoolKey.Stationary,(ulong)id,id,0,manager.Session.Snapshot.Distance,loop.Player.CurrentPosition,manager.Session.Snapshot.ElapsedTime);
        PooledObject Pickup(PoolKey key,long id)
        {Assert.That(manager.Pools.TryRent(new SpawnContext(key,loop.Player.CurrentPosition,Quaternion.identity,loop.Planner.OriginRoot,0,manager.Session.Snapshot.Distance,12345,0,id,loop.Planner),out var p),Is.True);return p;}
        [Test] public void AcceptedDamageGraceAndFullFuelEmitOneDistinctCueEach()
        {
            Assert.That(loop.Survival.TryDamage(Hit()),Is.True);Assert.That(loop.Survival.TryDamage(Hit(2)),Is.False);
            Pickup(PoolKey.Coin,900001);Pickup(PoolKey.Fuel,900002);float applied=-1;manager.Session.Collected+=e=>{if(e.Kind==PoolKey.Fuel)applied=e.AppliedValue;};loop.Step(.02f);
            Assert.That(audio.CollisionRequests,Is.EqualTo(1));Assert.That(audio.CoinRequests,Is.EqualTo(1));Assert.That(audio.FuelRequests,Is.EqualTo(1));Assert.That(applied,Is.Zero);
            Assert.That(audio.VoiceSource(0).clip,Is.EqualTo(audio.Configuration.Collision));Assert.That(audio.VoiceSource(1).clip,Is.EqualTo(audio.Configuration.Coin));Assert.That(audio.VoiceSource(2).clip,Is.EqualTo(audio.Configuration.Fuel));
        }
        [Test] public void SaturationDropsRewardsAndCollisionStealsOldestRewardThenOldestCollision()
        {
            for(int i=0;i<8;i++)Assert.That(audio.Request(AudioCue.Coin),Is.True);
            Assert.That(audio.Request(AudioCue.Fuel),Is.False);Assert.That(audio.Dropped,Is.EqualTo(1));
            ulong token=audio.VoiceGeneration(0);Assert.That(audio.Request(AudioCue.Collision),Is.True);Assert.That(audio.Stolen,Is.EqualTo(1));
            Assert.That(audio.VoiceCue(0),Is.EqualTo(AudioCue.Collision));Assert.That(audio.VoiceGeneration(0),Is.GreaterThan(token));
            Assert.That(audio.TryCompleteVoice(0,token,double.MaxValue),Is.False);Assert.That(audio.VoiceBusy(0),Is.True);
            for(int i=0;i<7;i++)audio.Request(AudioCue.Collision);token=audio.VoiceGeneration(0);audio.Request(AudioCue.Collision);
            Assert.That(audio.VoiceGeneration(0),Is.GreaterThan(token));Assert.That(audio.ActiveVoices,Is.EqualTo(8));Assert.That(audio.PeakVoices,Is.EqualTo(8));
            Assert.That(root.GetComponentsInChildren<AudioSource>(true).Length,Is.EqualTo(10));
        }
        [Test] public void SaturationNeverDelaysRewardsOrChangesGameplayOrVfxRouting()
        {
            for(int i=0;i<8;i++)audio.Request(AudioCue.Coin);
            Pickup(PoolKey.Coin,900003);Pickup(PoolKey.Fuel,900004);loop.Survival.TryDamage(Hit());loop.Step(.02f);
            Assert.That(manager.Session.Snapshot.Coins,Is.EqualTo(1));Assert.That(manager.Session.Snapshot.Lives,Is.EqualTo(2));Assert.That(manager.Session.Snapshot.Fuel,Is.EqualTo(99.96f).Within(.001));
            Assert.That(audio.Dropped,Is.EqualTo(2));Assert.That(audio.Stolen,Is.EqualTo(1));
        }
        [Test] public void EngineReadsMeasuredSpeedInsteadOfCruiseAndDoesNotRestartOnSnapshots()
        {
            ulong starts=audio.EngineStarts;Assert.That(audio.EngineSource.pitch,Is.EqualTo(1.108f).Within(.00001));
            manager.Session.RecordMovement(.02f,1);manager.Session.PublishSnapshot();Assert.That(manager.Session.Snapshot.CruiseSpeed,Is.EqualTo(22));Assert.That(audio.EngineSource.pitch,Is.EqualTo(1.5f));
            manager.Session.RecordMovement(.02f,.1);manager.Session.PublishSnapshot();Assert.That(audio.EngineSource.pitch,Is.EqualTo(.87f).Within(.00001));
            Assert.That(audio.EngineStarts,Is.EqualTo(starts));manager.Pause();Assert.That(audio.EngineSource.isPlaying,Is.False);Assert.That(audio.EnginePaused,Is.True);Assert.That(audio.EngineSource.pitch,Is.EqualTo(.8f));
            manager.Resume();Assert.That(audio.EngineStarts,Is.EqualTo(starts));Assert.That(audio.EnginePaused,Is.False);
        }
        [UnityTest] public IEnumerator PauseRetainsBusyVoicesAndPlayheadsAcrossWallTime()
        {
            audio.Request(AudioCue.Fuel);yield return new WaitForSecondsRealtime(.04f);manager.Pause();
            ulong token=audio.VoiceGeneration(0),music=audio.MusicStarts,engine=audio.EngineStarts;int sample=audio.VoiceSource(0).timeSamples,engineSample=audio.EngineSource.timeSamples;
            yield return new WaitForSecondsRealtime(.5f);audio.RefreshPlayback();
            Assert.That(audio.ActiveVoices,Is.EqualTo(1));Assert.That(audio.VoicePaused(0),Is.True);Assert.That(audio.VoiceSource(0).isPlaying,Is.False);
            Assert.That(audio.VoiceSource(0).timeSamples,Is.EqualTo(sample));Assert.That(audio.EngineSource.timeSamples,Is.EqualTo(engineSample));Assert.That(audio.TryCompleteVoice(0,token,double.MaxValue),Is.False);
            Assert.That(audio.Request(AudioCue.Collision),Is.False);manager.Resume();Assert.That(audio.VoiceGeneration(0),Is.EqualTo(token));Assert.That(audio.VoicePaused(0),Is.False);
            Assert.That(audio.EngineStarts,Is.EqualTo(engine));Assert.That(audio.MusicStarts,Is.EqualTo(music));yield return new WaitForSecondsRealtime(.45f);audio.RefreshPlayback();Assert.That(audio.ActiveVoices,Is.Zero);Assert.That(audio.VoiceSource(0).clip,Is.Null);
        }
        [UnityTest] public IEnumerator TerminalTickCuesFinishAtFrozenGameTimeAndRejectStaleRequests()
        {
            manager.Session.SetResources(1,10);Pickup(PoolKey.Coin,900011);Pickup(PoolKey.Fuel,900012);Pickup(PoolKey.Stationary,900013);loop.Step(.02f);
            Assert.That(manager.State,Is.EqualTo(GameState.GameOver));Assert.That(audio.EngineSource.isPlaying,Is.False);Assert.That(audio.ActiveVoices,Is.EqualTo(3));Assert.That(audio.Requests,Is.EqualTo(3));
            manager.Session.PublishDamage(new DamageEvent(manager.Session.Snapshot.RunId,Hit(20),0));Assert.That(audio.Requests,Is.EqualTo(3));
            yield return new WaitForSecondsRealtime(.6f);audio.RefreshPlayback();Assert.That(audio.ActiveVoices,Is.Zero);Assert.That(manager.Session.Snapshot.ActualSpeed,Is.Zero);
        }
        [Test] public void FiftyRestartsKeepAllSourceIdentitiesAndOneMusicLoop()
        {
            var ids=root.GetComponentsInChildren<AudioSource>(true).Select(s=>s.GetEntityId().ToString()).OrderBy(x=>x).ToArray();ulong music=audio.MusicStarts,created=manager.Pools.TotalCreated;
            for(int i=0;i<50;i++)
            {
                for(int v=0;v<8;v++)audio.Request(AudioCue.Fuel);ulong old=audio.VoiceGeneration(0);
                if(i%2==0)manager.Pause();else manager.EndRun(GameOverReason.Lives);
                Assert.That(manager.Restart(),Is.True);Assert.That(audio.ActiveVoices,Is.Zero);
                for(int v=0;v<8;v++){Assert.That(audio.VoiceSource(v).clip,Is.Null);Assert.That(audio.VoiceBusy(v)||audio.VoicePaused(v),Is.False);}
                audio.Request(AudioCue.Coin);Assert.That(audio.TryCompleteVoice(0,old,double.MaxValue),Is.False);audio.ResetVoices();
            }
            Assert.That(root.GetComponentsInChildren<AudioSource>(true).Select(s=>s.GetEntityId().ToString()).OrderBy(x=>x),Is.EqualTo(ids));Assert.That(audio.MusicStarts,Is.EqualTo(music));
            Assert.That(audio.EngineStarts,Is.EqualTo(51));Assert.That(manager.Pools.TotalCreated,Is.EqualTo(created));Assert.That(manager.Pools.TotalDestroyed,Is.Zero);
            manager.Pause();manager.ReturnToMenu();Assert.That(audio.ActiveVoices,Is.Zero);Assert.That(audio.EngineSource.isPlaying,Is.False);
        }
        [TestCase(GameState.Playing)] [TestCase(GameState.Paused)] [TestCase(GameState.GameOver)]
        public void FocusAndApplicationPauseRestoreOnlyCurrentState(GameState state)
        {
            if(state==GameState.Paused)manager.Pause();if(state==GameState.GameOver)manager.EndRun(GameOverReason.Lives);
            ulong starts=audio.EngineStarts;audio.SetApplicationSuspension(true,false);Assert.That(audio.EngineSource.isPlaying,Is.False);
            audio.SetApplicationSuspension(true,true);audio.SetApplicationSuspension(false,true);Assert.That(audio.MusicSource.isPlaying,Is.False);
            audio.SetApplicationSuspension(false,false);Assert.That(manager.State,Is.EqualTo(state));Assert.That(audio.EngineStarts,Is.EqualTo(starts));Assert.That(audio.EngineSource.isPlaying,Is.EqualTo(state==GameState.Playing));
        }
        [Test] public void DisableEnableDoesNotDuplicateSubscriptionsAndResetInvalidatesOldRunEvents()
        {
            for(int i=0;i<5;i++){audio.enabled=false;Assert.That(audio.ActiveVoices,Is.Zero);audio.enabled=true;}
            var old=manager.Session.Snapshot.RunId;loop.Survival.TryDamage(Hit());Assert.That(audio.CollisionRequests,Is.EqualTo(1));
            manager.Pause();manager.Restart();manager.Session.PublishDamage(new DamageEvent(old,Hit(),2));Assert.That(audio.Requests,Is.Zero);
            loop.Survival.TryDamage(Hit(9));Assert.That(audio.CollisionRequests,Is.EqualTo(1));
        }
        [Test] public void RebaseInheritsPlayerAttachmentOnceWithoutChangingPitchOrDetached2DCues()
        {
            audio.Request(AudioCue.Coin);loop.Roads.EnsureCoverage(1005,0);loop.Player.SetAbsoluteForward(1005,0);loop.Player.HoldPose();
            float before=audio.EngineSource.transform.position.z,pitch=audio.EngineSource.pitch;Vector3 voice=audio.VoiceSource(0).transform.position;
            Assert.That(loop.Origin.RebaseIfNeeded(1005),Is.True);Assert.That(audio.EngineSource.transform.position.z,Is.EqualTo(before-960).Within(.001));
            Assert.That(audio.VoiceSource(0).transform.position,Is.EqualTo(voice));Assert.That(audio.EngineSource.pitch,Is.EqualTo(pitch));Assert.That(audio.EngineSource.dopplerLevel,Is.Zero);
        }
        [TestCase("voices")] [TestCase("musicSource")] [TestCase("engineSource")] [TestCase("configuration")] [TestCase("gameManager")]
        public void MissingSourceAssignmentsFailBeforeStartingPlayback(string field)
        {
            Set(audio,field,null);Assert.That(audio.ValidateRequired(loop,out var error),Is.False);StringAssert.Contains("assign",error.ToLowerInvariant());
        }
    }
}
