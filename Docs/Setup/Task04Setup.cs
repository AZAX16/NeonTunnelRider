// Execute through Pipeline in Edit mode; Unity owns asset/meta/scene serialization.
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonTunnelRunner
{
    public static class Task04Setup
    {
        public static object Configure()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Configure in Edit mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save user scene edits first.");
            var scene = EditorSceneManager.OpenScene(ProjectSetupValidator.GameScene, OpenSceneMode.Single);
            const string path = ProjectSetupValidator.Root + "/Configs/Pools.asset";
            var config = AssetDatabase.LoadAssetAtPath<PoolConfig>(path);
            if (!config)
            {
                config = ScriptableObject.CreateInstance<PoolConfig>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssetIfDirty(config);
            }
            var systems = scene.GetRootGameObjects().Single(g => g.name == "Systems");
            var manager = systems.GetComponent<GameManager>();
            var pools = systems.GetComponent<PoolService>() ?? systems.AddComponent<PoolService>();
            var storage = scene.GetRootGameObjects().SingleOrDefault(g => g.name == "Pools") ?? new GameObject("Pools");
            SceneManager.MoveGameObjectToScene(storage, scene);
            storage.SetActive(false);
            Assign(pools, "gameManager", manager);
            Assign(pools, "configuration", config);
            Assign(pools, "storageRoot", storage.transform);
            Assign(manager, "poolService", pools);
            // URP adds unused metadata when the old light is inspected; retain its original serialization.
            foreach (var light in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>(true)))
                UnityEngine.Object.DestroyImmediate(light);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return Inspect();
        }

        public static object Inspect()
        {
            ProjectSetupValidator.ValidateProject();
            var scene = SceneManager.GetSceneByPath(ProjectSetupValidator.GameScene);
            var pools = scene.GetRootGameObjects().Single(g => g.name == "Systems").GetComponent<PoolService>();
            return new { scene.path, scene.isDirty, config = AssetDatabase.GetAssetPath(pools.Configuration),
                entries = pools.Configuration.EntryCount, owner = pools.Owner.name, storage = pools.StorageRoot.name,
                storageInactive = !pools.StorageRoot.gameObject.activeSelf,
                instanceClear = GameManager.Instance == null, timeScale = Time.timeScale };
        }

        private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
