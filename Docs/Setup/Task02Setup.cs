// Editor automation recipe, outside Assets; invoked through the installed Pipeline script run command.
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonTunnelRunner
{
    public static class Task02Setup
    {
        public static object WireCore()
        {
            if (Application.isPlaying || Application.unityVersion != "6000.6.3f1")
                throw new InvalidOperationException("Use Editor 6000.6.3f1 in Edit mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save existing scene changes before wiring the core.");
            const string configPath = "Assets/NeonTunnelRunner/Configs/Gameplay.asset";
            var scene = EditorSceneManager.OpenScene(ProjectSetupValidator.GameScene, OpenSceneMode.Single);
            var systems = scene.GetRootGameObjects().Single(g => g.name == "Systems");
            var config = AssetDatabase.LoadAssetAtPath<GameplayConfig>(configPath);
            if (!config)
            {
                if (AssetDatabase.LoadMainAssetAtPath(configPath))
                    throw new InvalidOperationException("Another asset already occupies the config path.");
                config = ScriptableObject.CreateInstance<GameplayConfig>();
                AssetDatabase.CreateAsset(config, configPath);
            }
            var loop = systems.GetComponent<GameplayLoop>() ?? Undo.AddComponent<GameplayLoop>(systems);
            var manager = systems.GetComponent<GameManager>() ?? Undo.AddComponent<GameManager>(systems);
            var managerFields = new SerializedObject(manager);
            managerFields.FindProperty("gameplayConfig").objectReferenceValue = config;
            managerFields.FindProperty("gameplayLoop").objectReferenceValue = loop;
            managerFields.ApplyModifiedProperties();
            var loopFields = new SerializedObject(loop);
            loopFields.FindProperty("gameManager").objectReferenceValue = manager;
            loopFields.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            ProjectSetupValidator.ValidateProject();
            return new
            {
                scene = scene.path,
                sceneGuid = AssetDatabase.AssetPathToGUID(scene.path),
                systems = systems.name,
                config = AssetDatabase.GetAssetPath(manager.Configuration),
                configGuid = AssetDatabase.AssetPathToGUID(configPath),
                managerCount = systems.GetComponents<GameManager>().Length,
                loopCount = systems.GetComponents<GameplayLoop>().Length,
                managerReferencesLoop = manager.Loop == loop,
                loopReferencesManager = loop.Owner == manager,
                seed = managerFields.FindProperty("runSeed").intValue,
                initialBestScore = managerFields.FindProperty("initialBestScore").intValue,
                defaults = new { config.StartingLives, config.FuelCapacity, config.StartingSpeed, config.MaximumSpeed,
                    config.StartingLane, config.LaneWidth, config.LaneTransitionDuration }
            };
        }
    }
}
