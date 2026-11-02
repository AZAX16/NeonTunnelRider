using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonTunnelRunner
{
    public static class Task08Setup
    {
        public static object Configure()
        {
            if (Application.isPlaying || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Require stopped, saved Game scene.");
            const string path="Assets/NeonTunnelRunner/Prefabs/Player/PlayerHovercar.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var visuals=prefab.GetComponent<VehicleVisuals>() ?? prefab.AddComponent<VehicleVisuals>();
                Set(visuals,"flashRenderer",prefab.transform.Find("Visual/Body").GetComponent<Renderer>());
                PrefabUtility.SaveAsPrefabAsset(prefab,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var manager=UnityEngine.Object.FindAnyObjectByType<GameManager>();
            var loop=manager.Loop; var player=loop.Player;
            var sceneVisuals=player.GetComponent<VehicleVisuals>();
            if (!sceneVisuals) throw new InvalidOperationException("Player prefab did not propagate.");
            Set(sceneVisuals,"gameManager",manager);
            Set(loop,"vehicleVisuals",sceneVisuals);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            return new {scene=SceneManager.GetActiveScene().path,prefab=path,renderer=sceneVisuals.FlashRenderer.name,owner=sceneVisuals.Owner.name};
        }
        private static void Set(UnityEngine.Object target,string field,UnityEngine.Object value)
        {
            var serialized=new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue=value;
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
