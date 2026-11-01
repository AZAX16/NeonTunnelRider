// Execute through the connected Editor. Unity serializes assets/metas and preserves existing GUIDs.
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonTunnelRunner
{
    public static class Task05Setup
    {
        private const string Root = "Assets/NeonTunnelRunner";
        public static object Configure()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Configure in Edit mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save user scene edits first.");
            var scene = EditorSceneManager.OpenScene(ProjectSetupValidator.GameScene, OpenSceneMode.Single);
            var config = AssetDatabase.LoadAssetAtPath<WorldConfig>(Root + "/Configs/World.asset");
            if (!config) { config = ScriptableObject.CreateInstance<WorldConfig>(); AssetDatabase.CreateAsset(config, Root + "/Configs/World.asset"); }
            var floor = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/ReferenceRoad.mat");
            var marking = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/ReferenceMarking.mat");
            var architecture = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/RoadArchitecture.mat");
            if (!architecture)
            {
                architecture = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                architecture.SetColor("_BaseColor", new Color(0.24f, 0.08f, 0.24f));
                AssetDatabase.CreateAsset(architecture, Root + "/Materials/RoadArchitecture.mat");
            }
            string[] names = { "RoadOpen60", "RoadTunnel60", "RoadEntrance60", "RoadExit60" };
            // Pool keys are Open,Tunnel,Entrance,Exit; environment enum uses the same order.
            var prefabs = new GameObject[4];
            for (int key = 0; key < names.Length; key++)
            {
                string path = Root + "/Prefabs/Road/" + names[key] + ".prefab";
                var module = new GameObject(names[key]);
                try
                {
                    var segment = module.AddComponent<RoadSegment>();
                    Box(module.transform, "Surface", new Vector3(0f,-0.1f,30f), new Vector3(9.6f,0.2f,60f), floor, true);
                    foreach (float x in new[] { -4.8f, -1.6f, 1.6f, 4.8f })
                        Box(module.transform, "LaneMark " + x, new Vector3(x,0.012f,30f), new Vector3(0.045f,0.02f,60f), marking);
                    foreach (float x in new[] { -5.05f, 5.05f })
                        Box(module.transform, "OuterRail", new Vector3(x,0.4f,30f), new Vector3(0.3f,0.3f,60f), architecture);
                    if (key == 0)
                    {
                        foreach (float x in new[] { -5.4f, 5.4f })
                        {
                            Box(module.transform, "LightPost", new Vector3(x,3f,30f), new Vector3(0.15f,6f,0.15f), architecture);
                            Box(module.transform, "Lamp", new Vector3(x,6f,30f), new Vector3(0.5f,0.15f,1.5f), marking);
                        }
                    }
                    else
                    {
                        float start = key == 2 ? 30f : 0f;
                        float end = key == 3 ? 30f : 60f;
                        for (float z = start; z < end; z += 15f)
                        {
                            foreach (float x in new[] { -5.3f, 5.3f })
                                Box(module.transform, "ArchPost", new Vector3(x,4.2f,z), new Vector3(0.4f,8.4f,0.4f), architecture);
                            Box(module.transform, "ArchTop", new Vector3(0f,8.5f,z), new Vector3(11f,0.4f,0.4f), architecture);
                        }
                        Box(module.transform, "Ceiling", new Vector3(0f,8.8f,(start+end)*0.5f), new Vector3(11f,0.25f,end-start), architecture);
                    }
                    var entry = new GameObject("EntryMarker").transform;
                    entry.SetParent(module.transform, false);
                    var exit = new GameObject("ExitMarker").transform;
                    exit.SetParent(module.transform, false); exit.localPosition = new Vector3(0f,0f,60f);
                    Assign(segment, "entry", entry); Assign(segment, "exit", exit);
                    var serialized = new SerializedObject(segment);
                    serialized.FindProperty("environment").enumValueIndex = key;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    prefabs[key] = PrefabUtility.SaveAsPrefabAsset(module, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(module); }
            }
            var pools = AssetDatabase.LoadAssetAtPath<PoolConfig>(Root + "/Configs/Pools.asset");
            var catalog = new SerializedObject(pools);
            var entries = catalog.FindProperty("entries"); entries.arraySize = 4;
            for (int key = 0; key < 4; key++)
            {
                var entry = entries.GetArrayElementAtIndex(key);
                entry.FindPropertyRelative("key").enumValueIndex = key;
                entry.FindPropertyRelative("prefab").objectReferenceValue = prefabs[key];
                entry.FindPropertyRelative("capacity").intValue = key < 2 ? 8 : 4;
            }
            catalog.ApplyModifiedPropertiesWithoutUndo();
            var systems = scene.GetRootGameObjects().Single(g => g.name == "Systems");
            var manager = systems.GetComponent<GameManager>();
            var loop = systems.GetComponent<GameplayLoop>();
            var roads = systems.GetComponent<RoadManager>() ?? systems.AddComponent<RoadManager>();
            var origin = systems.GetComponent<WorldOrigin>() ?? systems.AddComponent<WorldOrigin>();
            var director = systems.GetComponent<SpawnDirector>() ?? systems.AddComponent<SpawnDirector>();
            var world = scene.GetRootGameObjects().Single(g => g.name == "World");
            var active = Child(world.transform, "RoadSegments");
            Child(world.transform, "Interactables");
            var old = world.transform.Find("ReferenceRoad"); if (old) old.gameObject.SetActive(false);
            Assign(roads, "configuration", config); Assign(roads, "pools", manager.Pools); Assign(roads, "activeRoot", active);
            Assign(origin, "configuration", config); Assign(origin, "roads", roads); Assign(origin, "player", loop.Player);
            Assign(origin, "chaseCamera", UnityEngine.Object.FindAnyObjectByType<Camera>().transform);
            Assign(director, "configuration", config); Assign(director, "gameplay", manager.Configuration);
            Assign(loop, "roads", roads); Assign(loop, "worldOrigin", origin); Assign(loop, "spawnDirector", director);
            foreach (var light in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>(true)))
                UnityEngine.Object.DestroyImmediate(light);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return Inspect();
        }
        public static object Inspect()
        {
            ProjectSetupValidator.ValidateProject();
            var manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            return new { scene = SceneManager.GetActiveScene().path, dirty = SceneManager.GetActiveScene().isDirty,
                world = AssetDatabase.GetAssetPath(manager.Loop.Roads.Configuration),
                pools = Enumerable.Range(0, manager.Pools.Configuration.EntryCount).Select(i => {
                    var e = manager.Pools.Configuration.GetEntry(i); return new { key=e.Key.ToString(), e.Capacity, prefab=AssetDatabase.GetAssetPath(e.Prefab) }; }).ToArray(),
                activeRoot = manager.Loop.Roads.ActiveRoot.name, cameraOwnedByPlayer = manager.Loop.Origin.ChaseCamera.IsChildOf(manager.Loop.Player.transform) };
        }
        private static Transform Child(Transform parent, string name)
        { var child = parent.Find(name); if (!child) { child = new GameObject(name).transform; child.SetParent(parent, false); } return child; }
        private static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool collider = false)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name; box.transform.SetParent(parent, false);
            box.transform.localPosition = position; box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
        }
        private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
        { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
