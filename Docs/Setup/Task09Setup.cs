using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace NeonTunnelRunner
{
    public static class Task09Setup
    {
        public static object Configure()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save/stop Game first.");
            var manager=UnityEngine.Object.FindAnyObjectByType<GameManager>();
            var config=new SerializedObject(manager.Configuration);
            config.FindProperty("startingOccupancy").floatValue=.25f;
            config.FindProperty("maximumOccupancy").floatValue=.8f;
            config.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager.Configuration);
            var debug=new SerializedObject(manager.Loop.Progression);
            debug.FindProperty("debugTargetSpeed").floatValue=22f;
            debug.FindProperty("debugOccupancy").floatValue=.25f;
            debug.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager.Loop.Progression);
            AssetDatabase.SaveAssets();
            AssetDatabase.ForceReserializeAssets(new[]{"Assets/NeonTunnelRunner/Configs/World.asset"});
            // URP can add this default component during preview rendering. It was absent
            // in the inspected checkpoint; do not serialize unrelated light changes.
            var light=GameObject.Find("ReferenceLight");
            var extra=light ? light.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>() : null;
            if(extra) UnityEngine.Object.DestroyImmediate(extra);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            EditorSceneManager.SaveScene(manager.gameObject.scene);
            ProjectSetupValidator.ValidateProject();
            return new { passed=true, scene=manager.gameObject.scene.path, key=HighScoreStore.ProductionKey,
                manager.Configuration.StartingOccupancy,manager.Configuration.MaximumOccupancy };
        }
    }
}
