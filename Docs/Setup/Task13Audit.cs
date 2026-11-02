using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
namespace NeonTunnelRunner
{
    public static class Task13Audit
    {
        public static object Inspect()
        {
            ProjectSetupValidator.ValidateProject();
            var manager=UnityEngine.Object.FindAnyObjectByType<GameManager>();var a=manager.Loop.Audio;var c=a.Configuration;
            return new {unity=Application.unityVersion,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,compiling=EditorApplication.isCompiling,compilationFailed=EditorUtility.scriptCompilationFailed,
                sources=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Select(s=>new {path=Path(s.transform),clip=s.clip?s.clip.name:null,group=s.outputAudioMixerGroup?s.outputAudioMixerGroup.name:null,s.loop,s.playOnAwake,s.spatialBlend,s.dopplerLevel,s.volume,s.mute}).ToArray(),
                listeners=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include).Select(s=>Path(s.transform)).ToArray(),
                clips=new[]{c.Music,c.Engine,c.Collision,c.Coin,c.Fuel}.Select(clip=>{
                    var path=AssetDatabase.GetAssetPath(clip);var i=(AudioImporter)AssetImporter.GetAtPath(path);var settings=i.defaultSampleSettings;
                    return new {path,clip.channels,clip.frequency,clip.samples,clip.length,load=clip.loadType.ToString(),loadState=clip.loadState.ToString(),format=settings.compressionFormat.ToString(),settings.quality,settings.preloadAudioData,i.loadInBackground,i.forceToMono,rate=settings.sampleRateSetting.ToString()};}).ToArray(),
                pitch=new[]{0f,22f,50f,60f}.Select(speed=>new {speed,pitch=c.EnginePitch(speed),volume=c.EngineLevel(speed)}).ToArray(),
                musicLevels=Enum.GetValues(typeof(GameState)).Cast<GameState>().Select(s=>new{state=s.ToString(),level=c.MusicLevel(s)}).ToArray()};
        }
        static string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
    }
}
