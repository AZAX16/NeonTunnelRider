// Run through Pipeline script run in Edit mode. No player or scene YAML is hand-authored.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NeonTunnelRunner
{
    public static class Task03Setup
    {
        public static object Inspect()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Inspect the saved scene in Edit mode.");
            ProjectSetupValidator.ValidateProject();
            var scene = SceneManager.GetActiveScene();
            var systems = scene.GetRootGameObjects().Single(g => g.name == "Systems");
            var manager = systems.GetComponent<GameManager>();
            var loop = manager.Loop;
            var input = loop.Input;
            var player = loop.Player;
            var camera = Camera.main;
            var box = player.GetComponent<BoxCollider>();
            return new
            {
                scene.path, scene.isDirty, playing = Application.isPlaying,
                instanceClear = GameManager.Instance == null, timeScale = Time.timeScale,
                reloads = new { enabled = EditorSettings.enterPlayModeOptionsEnabled, options = (int)EditorSettings.enterPlayModeOptions },
                manager = new { config = AssetDatabase.GetAssetPath(manager.Configuration), loop = loop.name },
                input = new { input.name, owner = input.Owner.name, asset = AssetDatabase.GetAssetPath(input.SourceActions),
                    sourceDisabled = !input.SourceActions.enabled, projectWideActionsUnassigned = InputSystem.actions == null,
                    maps = input.SourceActions.actionMaps.Select(m => new { m.name, actions = m.actions.Select(a => new {
                        a.name, type = a.type.ToString(), a.expectedControlType, a.interactions, a.wantsInitialStateCheck,
                        bindings = a.bindings.Select(b => new { b.name, b.path, b.isComposite, b.isPartOfComposite }).ToArray()
                    }).ToArray() }).ToArray() },
                player = new { player.name, position = player.transform.position.ToString("F3"),
                    scale = player.transform.localScale.ToString("F3"), rotation = player.transform.eulerAngles.ToString("F3"),
                    config = AssetDatabase.GetAssetPath(player.Configuration), prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player),
                    player.Body.isKinematic, player.Body.useGravity, constraints = player.Body.constraints.ToString(),
                    interpolation = player.Body.interpolation.ToString(), boxSize = box.size.ToString("F3"),
                    visual = player.transform.Find("Visual").name, anchorPosition = player.CameraAnchor.localPosition.ToString("F3"),
                    anchorRotation = player.CameraAnchor.localEulerAngles.ToString("F3") },
                camera = new { camera.name, parent = camera.transform.parent.name, position = camera.transform.position.ToString("F3"),
                    localPosition = camera.transform.localPosition.ToString("F3"), camera.allowHDR,
                    postProcessing = camera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing,
                    listenerEnabled = camera.GetComponent<AudioListener>().enabled }
            };
        }

        public static object WirePlayer()
        {
            if (Application.isPlaying || Application.unityVersion != "6000.6.3f1")
                throw new InvalidOperationException("Use Editor 6000.6.3f1 in Edit mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene changes before setup.");
            const string root = "Assets/NeonTunnelRunner/";
            const string actionPath = root + "Input/Runner.inputactions";
            const string prefabPath = root + "Prefabs/Player/PlayerHovercar.prefab";
            var scene = EditorSceneManager.OpenScene(root + "Scenes/Game.unity", OpenSceneMode.Single);
            var systems = scene.GetRootGameObjects().Single(g => g.name == "Systems");
            var manager = systems.GetComponent<GameManager>();
            var loop = manager.Loop;
            var config = manager.Configuration;
            EditorUtility.SetDirty(config); // Serialize the newly introduced default height and box fields.
            AssetDatabase.ImportAsset(actionPath, ImportAssetOptions.ForceUpdate);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(actionPath);
            if (!actions) throw new InvalidOperationException("Runner actions were not imported.");

            // The template remains on disk, but project-wide actions auto-enable every map.
            InputSystem.actions = null;
            EditorBuildSettings.RemoveConfigObject("com.unity.input.settings.actions");

            if (!AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath))
            {
                var authored = new GameObject("PlayerHovercar");
                try
                {
                    authored.transform.position = config.StartingPosition;
                    var controller = authored.AddComponent<PlayerController>();
                    var body = authored.GetComponent<Rigidbody>();
                    body.isKinematic = true;
                    body.useGravity = false;
                    body.interpolation = RigidbodyInterpolation.None;
                    body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
                    var collider = authored.GetComponent<BoxCollider>();
                    collider.center = Vector3.zero;
                    collider.size = config.PlayerHalfExtents * 2f;
                    var visual = new GameObject("Visual").transform;
                    visual.SetParent(authored.transform, false);
                    var white = AssetDatabase.LoadAssetAtPath<Material>(root + "Materials/ReferenceMarking.mat");
                    var dark = AssetDatabase.LoadAssetAtPath<Material>(root + "Materials/ReferenceRoad.mat");
                    Cube("Body", visual, Vector3.zero, new Vector3(1.6f, 0.6f, 3.4f), white);
                    Cube("Canopy", visual, new Vector3(0f, 0.3f, -0.2f), new Vector3(1.3f, 0.25f, 1.35f), dark);
                    var anchor = new GameObject("CameraAnchor").transform;
                    anchor.SetParent(authored.transform, false);
                    anchor.localPosition = new Vector3(0f, 4.4f, -10f);
                    anchor.localRotation = Quaternion.Euler(15f, 0f, 0f);
                    Assign(controller, "configuration", config);
                    Assign(controller, "body", body);
                    Assign(controller, "cameraAnchor", anchor);
                    PrefabUtility.SaveAsPrefabAsset(authored, prefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(authored); }
            }
            var player = scene.GetRootGameObjects().Select(g => g.GetComponent<PlayerController>()).FirstOrDefault(p => p);
            if (!player)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), scene);
                instance.name = "Player";
                player = instance.GetComponent<PlayerController>();
            }
            player.transform.SetPositionAndRotation(config.StartingPosition, Quaternion.identity);
            var input = systems.GetComponent<InputRouter>() ?? Undo.AddComponent<InputRouter>(systems);
            Assign(input, "gameManager", manager);
            Assign(input, "actionsAsset", actions);
            Assign(loop, "inputRouter", input);
            Assign(loop, "player", player);
            var camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).Single();
            camera.transform.SetParent(player.CameraAnchor, false);
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;
            var road = scene.GetRootGameObjects().Single(g => g.name == "World").transform.Find("ReferenceRoad");
            foreach (Transform piece in road)
            {
                var scale = piece.localScale; scale.z = 300f; piece.localScale = scale;
                var position = piece.localPosition; position.z = 150f; piece.localPosition = position;
            }
            // Retain the original light serialization instead of saving URP's auto-added metadata.
            var extraLight = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UniversalAdditionalLightData>()).ToArray();
            foreach (var data in extraLight) UnityEngine.Object.DestroyImmediate(data);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return new
            {
                sceneGuid = AssetDatabase.AssetPathToGUID(scene.path),
                prefab = prefabPath, prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath),
                actions = actionPath, actionsGuid = AssetDatabase.AssetPathToGUID(actionPath),
                loopPlayer = loop.Player.name, loopInput = loop.Input.name,
                cameraParent = camera.transform.parent.name,
                cameraPosition = camera.transform.position.ToString("F3"),
                roadLength = 300, projectWideActions = InputSystem.actions == null ? "unassigned" : "assigned"
            };
        }

        private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var fields = new SerializedObject(target);
            fields.FindProperty(field).objectReferenceValue = value;
            fields.ApplyModifiedProperties();
        }

        private static void Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<BoxCollider>());
            cube.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
