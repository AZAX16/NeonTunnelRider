using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace NeonTunnelRunner
{
    public sealed class AudioAssetTests
    {
        const string R="Assets/NeonTunnelRunner/";
        [Test] public void RequiredImportedAssetsHaveExplicitSupportedSettings()
        {
            var c=AssetDatabase.LoadAssetAtPath<AudioConfig>(R+"Configs/Audio.asset");Assert.That(c,Is.Not.Null);
            Assert.That(c.TryValidate(out var error),Is.True,error);
            int i=0;foreach(var clip in new[]{c.Music,c.Engine,c.Collision,c.Coin,c.Fuel})
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
                Assert.That(clip.channels,Is.EqualTo(i==0?2:1));Assert.That(clip.frequency,Is.EqualTo(44100));
                Assert.That(importer.defaultSampleSettings.loadType,Is.EqualTo(i==0?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad));
                Assert.That(importer.defaultSampleSettings.compressionFormat,Is.EqualTo(i==0?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM));
                Assert.That(importer.loadInBackground,Is.False);Assert.That(importer.defaultSampleSettings.preloadAudioData,Is.True);
                if(i>0){var samples=new float[clip.samples];Assert.That(clip.GetData(samples,0),Is.True);Assert.That(System.Array.Exists(samples,v=>Mathf.Abs(v)>.1f),Is.True);}
                i++;
            }
            Assert.That(c.Music.length,Is.EqualTo(64).Within(.02));Assert.That(c.Engine.length,Is.EqualTo(2).Within(.001));
            Assert.That(c.Collision,Is.Not.SameAs(c.Coin));Assert.That(c.Coin,Is.Not.SameAs(c.Fuel));
        }
        [Test] public void SuppliedExcerptIsSixSecondStereoPcmAndAssignedAtLocalZero()
        {
            var c=AssetDatabase.LoadAssetAtPath<AudioConfig>(R+"Configs/Audio.asset");
            Assert.That(c.NearMiss,Is.Not.Null); Assert.That(c.NearMiss.length,Is.EqualTo(6).Within(.0001));
            Assert.That(c.NearMiss.frequency,Is.EqualTo(44100)); Assert.That(c.NearMiss.channels,Is.EqualTo(2));
            var i=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(c.NearMiss));
            Assert.That(i.defaultSampleSettings.compressionFormat,Is.EqualTo(AudioCompressionFormat.PCM));
            Assert.That(i.defaultSampleSettings.loadType,Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
        }
        [TestCase("nearMiss","NearMiss_IntoTheVoid_06-12")]
        [TestCase("music","Music")] [TestCase("engine","Engine")] [TestCase("collision","Collision")]
        [TestCase("coin","Coin")] [TestCase("fuel","Fuel")]
        [TestCase("musicGroup","Music, Engine and SFX")] [TestCase("engineGroup","Music, Engine and SFX")] [TestCase("sfxGroup","Music, Engine and SFX")]
        public void MissingAssetHasActionableDiagnostic(string field,string expected)
        {
            var c=Object.Instantiate(AssetDatabase.LoadAssetAtPath<AudioConfig>(R+"Configs/Audio.asset"));
            try{typeof(AudioConfig).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,null);
                Assert.That(c.TryValidate(out var error),Is.False);StringAssert.Contains(expected,error);}
            finally{Object.DestroyImmediate(c);}
        }
        [Test] public void ActualSpeedMappingIsMonotoneClampedAndFinite()
        {
            var c=AssetDatabase.LoadAssetAtPath<AudioConfig>(R+"Configs/Audio.asset");float previous=.8f;
            for(int s=-1;s<=60;s++){float p=c.EnginePitch(s);Assert.That(p,Is.InRange(.8f,1.5f));Assert.That(p,Is.GreaterThanOrEqualTo(previous));previous=p;}
            Assert.That(c.EnginePitch(22),Is.EqualTo(1.108f).Within(.00001));Assert.That(c.EnginePitch(50),Is.EqualTo(1.5f));
            Assert.That(c.EnginePitch(float.NaN),Is.EqualTo(.8f));
        }
        [Test] public void SavedGameHasExactlyElevenSourcesAndTheExistingCameraListener()
        {
            var scene=EditorSceneManager.OpenPreviewScene(R+"Scenes/Game.unity");
            try
            {
                var roots=scene.GetRootGameObjects();int sources=0,listeners=0,controllers=0;
                foreach(var root in roots){sources+=root.GetComponentsInChildren<AudioSource>(true).Length;listeners+=root.GetComponentsInChildren<AudioListener>(true).Length;controllers+=root.GetComponentsInChildren<AudioController>(true).Length;}
                Assert.That(sources,Is.EqualTo(11));Assert.That(listeners,Is.EqualTo(1));Assert.That(controllers,Is.EqualTo(1));
                foreach(var root in roots)foreach(var a in root.GetComponentsInChildren<AudioController>(true))
                {Assert.That(a.Owner.Loop.Audio,Is.SameAs(a));Assert.That(a.ValidateRequired(a.Owner.Loop,out var error),Is.True,error);}
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
