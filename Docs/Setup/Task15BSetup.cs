using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace NeonTunnelRunner
{
    public static class Task15BSetup
    {
        static void Assign(UnityEngine.Object obj,string field,UnityEngine.Object value)
        { var so=new SerializedObject(obj); so.FindProperty(field).objectReferenceValue=value; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(obj); }
        public static object Configure()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play before wiring saved Game.");
            var scene=EditorSceneManager.GetActiveScene();
            if (scene.path!="Assets/NeonTunnelRunner/Scenes/Game.unity") throw new InvalidOperationException("Expected saved Game.");
            var config=AssetDatabase.LoadAssetAtPath<GameplayConfig>("Assets/NeonTunnelRunner/Configs/Gameplay.asset");
            var so=new SerializedObject(config); so.FindProperty("laneTransitionDuration").floatValue=.12f;
            so.FindProperty("trafficDensityMultiplier").floatValue=1.3f; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(config);
            var audio=UnityEngine.Object.FindAnyObjectByType<AudioController>();
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NeonTunnelRunner/Audio/Clips/NearMiss_IntoTheVoid_06-12.wav");
            if (!clip) throw new InvalidOperationException("Missing supplied excerpt.");
            string path=AssetDatabase.GetAssetPath(clip);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);
            var settings=importer.defaultSampleSettings; settings.loadType=AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData=true; settings.compressionFormat=AudioCompressionFormat.PCM; settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings=settings; importer.forceToMono=false;  importer.SaveAndReimport();
            Assign(audio.Configuration,"nearMiss",clip);
            var special=audio.transform.Find("NearMissMusic");
            if (!special) { special=new GameObject("NearMissMusic").transform; special.SetParent(audio.transform,false); }
            var source=special.GetComponent<AudioSource>(); if (!source) source=special.gameObject.AddComponent<AudioSource>();
            source.clip=clip; source.loop=false; source.playOnAwake=false; source.pitch=1f; source.volume=0f;
            source.spatialBlend=0f; source.dopplerLevel=0f; source.outputAudioMixerGroup=audio.Configuration.MusicGroup;
            Assign(audio,"nearMissSource",source);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return new { assigned=clip.name,clip.length,clip.channels,clip.frequency,laneDuration=config.LaneTransitionDuration,
                density=config.TrafficDensityMultiplier,sources=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length,
                scene=scene.path,best=PlayerPrefs.GetInt("NTR.HighScore.v1",0) };
        }
    }
}
