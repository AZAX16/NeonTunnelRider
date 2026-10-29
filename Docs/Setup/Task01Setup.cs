// Run through Unity Pipeline's script run command; this file is outside Assets.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NeonTunnelRunner
{
    public static class Task01Setup
    {
        private const string Root = "Assets/NeonTunnelRunner";
        private const string GameScene = Root + "/Scenes/Game.unity";

        public static string Configure()
        {
            if (Application.unityVersion != "6000.6.3f1")
                throw new InvalidOperationException("Task 01 requires Editor 6000.6.3f1.");
            if (File.Exists(GameScene))
                throw new InvalidOperationException("Game already exists. Use ProjectSetupValidator; setup never overwrites it.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save the open scenes before setup.");

            string[] folders =
            {
                "Scenes", "Scripts/Core", "Scripts/Input", "Scripts/Player", "Scripts/World",
                "Scripts/Pooling", "Scripts/Interactables", "Scripts/Presentation", "Scripts/Audio",
                "Scripts/UI", "Scripts/Persistence", "Scripts/Config", "Scripts/Editor", "Input",
                "Configs", "Prefabs/Player", "Prefabs/Road", "Prefabs/Interactables", "Prefabs/VFX",
                "Materials", "Meshes", "Textures", "Audio/Clips", "Audio/Mixers", "UI/Fonts",
                "UI/Icons", "Settings/URP", "Settings/Volumes", "Settings/BuildProfiles",
                "Tests/EditMode", "Tests/PlayMode"
            };
            foreach (string folder in folders) EnsureFolder(Root + "/" + folder);
            Directory.CreateDirectory("Docs/Verification");
            Directory.CreateDirectory("Evidence/Task01");
            Directory.CreateDirectory("Builds");

            foreach (string name in new[] { "PC_RPAsset", "PC_Renderer", "Mobile_RPAsset",
                         "Mobile_Renderer", "UniversalRenderPipelineGlobalSettings" })
                MoveAsset("Assets/Settings/" + name + ".asset", Root + "/Settings/URP/" + name + ".asset");

            var pc = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root + "/Settings/URP/PC_RPAsset.asset");
            var mobile = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root + "/Settings/URP/Mobile_RPAsset.asset");
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root + "/Settings/URP/PC_Renderer.asset");
            if (!pc || !mobile || !renderer) throw new InvalidOperationException("Expected URP template assets missing.");
            renderer.renderingMode = RenderingMode.Forward;
            pc.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
            pc.supportsHDR = mobile.supportsHDR = true;
            GraphicsSettings.defaultRenderPipeline = pc;
            EditorUtility.SetDirty(pc);
            EditorUtility.SetDirty(mobile);
            EditorUtility.SetDirty(renderer);
            int originalQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                // Retain the template's mobile/desktop quality distinction.
                QualitySettings.renderPipeline = QualitySettings.names[i] == "Mobile" ? mobile : pc;
            }
            QualitySettings.SetQualityLevel(originalQuality, false);
            EditorSettings.serializationMode = SerializationMode.ForceText;
            UnityEditor.VersionControlSettings.mode = "Visible Meta Files";
            EditorSettings.projectGenerationRootNamespace = "NeonTunnelRunner";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            playerSettings.FindProperty("activeInputHandler").intValue = 1;
            playerSettings.ApplyModifiedPropertiesWithoutUndo();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Systems");
            var world = new GameObject("World").transform;
            var presentation = new GameObject("Presentation").transform;
            new GameObject("UI");
            var road = new GameObject("ReferenceRoad").transform;
            road.SetParent(world, false);
            var asphalt = CreateMaterial("ReferenceRoad", new Color(0.16f, 0.19f, 0.23f));
            var marking = CreateMaterial("ReferenceMarking", new Color(0.8f, 0.83f, 0.86f));
            for (int lane = -1; lane <= 1; lane++)
                Cube("Lane" + (lane + 2), road, new Vector3(lane * 3.2f, -0.1f, 30f), new Vector3(3.2f, 0.2f, 60f), asphalt);
            foreach (float x in new[] { -4.8f, -1.6f, 1.6f, 4.8f })
                Cube("Marking" + x.ToString(System.Globalization.CultureInfo.InvariantCulture), road,
                    new Vector3(x, 0.012f, 30f), new Vector3(0.07f, 0.02f, 60f), marking);

            var cameraObject = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(presentation, false);
            cameraObject.transform.position = new Vector3(0f, 5f, -10f);
            cameraObject.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            camera.allowHDR = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.11f, 0.16f);
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = 1; // Default layer, shared with GlobalVolume.
            cameraData.SetRenderer(0);
            var lightObject = new GameObject("ReferenceLight", typeof(Light));
            lightObject.transform.SetParent(presentation, false);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.25f, 0.28f, 0.32f);
            var volumeObject = new GameObject("GlobalVolume", typeof(Volume));
            volumeObject.transform.SetParent(presentation, false);
            var volume = volumeObject.GetComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, Root + "/Settings/Volumes/GlobalVolumeProfile.asset");
            volume.sharedProfile = profile; // Deliberately empty; Bloom belongs to task 12.
            EditorSceneManager.SaveScene(scene, GameScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameScene, true) };
            AssetDatabase.SaveAssets();
            return "Task 01 scene and settings saved. Template scene/input and all moved asset GUIDs preserved.";
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void MoveAsset(string from, string to)
        {
            if (AssetDatabase.LoadMainAssetAtPath(to)) return;
            string error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, Root + "/Materials/" + name + ".mat");
            return material;
        }

        private static void Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
