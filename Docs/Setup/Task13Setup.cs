using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
namespace NeonTunnelRunner
{
    public static class Task13Setup
    {
        const string R="Assets/NeonTunnelRunner/";
        static void Set(UnityEngine.Object o,string field,UnityEngine.Object value)
        {var s=new SerializedObject(o);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(o);}
        static Transform Child(Transform parent,string name)
        {var t=parent?parent.Find(name):GameObject.Find(name)?.transform;if(t)return t;var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;}
        static AudioSource Source(Transform t,AudioClip clip,AudioMixerGroup group,bool loop)
        {
            var s=t.GetComponent<AudioSource>();if(!s)s=t.gameObject.AddComponent<AudioSource>();
            s.playOnAwake=false;s.loop=loop;s.clip=clip;s.outputAudioMixerGroup=group;s.spatialBlend=0;s.dopplerLevel=0;s.volume=0;
            s.pitch=1;s.mute=false;s.ignoreListenerPause=false;s.priority=loop?64:128;EditorUtility.SetDirty(s);return s;
        }
        public static object Configure()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)throw new Exception("Stop and save Game before authoring audio.");
            var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>(R+"Audio/Mixers/RunnerMixer.mixer");
            if(!mixer || new[]{"Music","Engine","SFX"}.Any(n=>mixer.FindMatchingGroups(n).Length!=1))
                throw new Exception("Create RunnerMixer with sibling Music, Engine and SFX groups using Unity's Audio Mixer window first.");
            var config=AssetDatabase.LoadAssetAtPath<AudioConfig>(R+"Configs/Audio.asset");
            if(!config){config=ScriptableObject.CreateInstance<AudioConfig>();AssetDatabase.CreateAsset(config,R+"Configs/Audio.asset");}
            var files=new[]{"VectorNight","HoverEngine","Impact","CoinChime","FuelCharge"};var fields=new[]{"music","engine","collision","coin","fuel"};
            for(int i=0;i<files.Length;i++)
            {
                string path=R+"Audio/Clips/"+files[i]+".wav";AssetDatabase.ImportAsset(path);
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);
                importer.forceToMono=i!=0;importer.loadInBackground=false;importer.ambisonic=false;
                var sample=importer.defaultSampleSettings;sample.loadType=i==0?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
                sample.compressionFormat=i==0?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;sample.quality=.8f;sample.preloadAudioData=true;
                sample.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=sample;importer.SaveAndReimport();
                Set(config,fields[i],AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }
            Set(config,"musicGroup",mixer.FindMatchingGroups("Music")[0]);Set(config,"engineGroup",mixer.FindMatchingGroups("Engine")[0]);Set(config,"sfxGroup",mixer.FindMatchingGroups("SFX")[0]);
            var manager=UnityEngine.Object.FindAnyObjectByType<GameManager>();var loop=manager.Loop;
            // Add to the existing player prefab; Unity preserves its GUID and existing scene overrides.
            string pp=R+"Prefabs/Player/PlayerHovercar.prefab";var prefab=PrefabUtility.LoadPrefabContents(pp);
            Source(Child(prefab.transform,"EngineAudio"),config.Engine,config.EngineGroup,true);
            PrefabUtility.SaveAsPrefabAsset(prefab,pp);PrefabUtility.UnloadPrefabContents(prefab);
            var root=Child(null,"Audio");var controller=root.GetComponent<AudioController>();if(!controller)controller=root.gameObject.AddComponent<AudioController>();
            Set(controller,"gameManager",manager);Set(controller,"configuration",config);
            Set(controller,"musicSource",Source(Child(root,"Music"),config.Music,config.MusicGroup,true));
            Set(controller,"engineSource",loop.Player.transform.Find("EngineAudio").GetComponent<AudioSource>());
            var serialized=new SerializedObject(controller);var bank=serialized.FindProperty("voices");bank.arraySize=8;
            for(int i=0;i<8;i++)bank.GetArrayElementAtIndex(i).objectReferenceValue=Source(Child(root,"SfxVoice"+i),null,config.SfxGroup,false);
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(controller);Set(loop,"audioController",controller);
            if(!controller.ValidateRequired(loop,out string error))throw new Exception(error);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            return new {saved=true,sources=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length,
                listeners=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include).Length,
                groups=mixer.FindMatchingGroups("").Select(g=>g.name).ToArray()};
        }
    }
}
