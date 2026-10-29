using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NeonTunnelRunner
{
    /// <summary>Read-only foundation checks. Gameplay wiring is added in later tasks.</summary>
    public static class ProjectSetupValidator
    {
        public const string Root = "Assets/NeonTunnelRunner";
        public const string GameScene = Root + "/Scenes/Game.unity";
        private static readonly string[] RequiredFolders =
        {
            "Scenes", "Scripts/Core", "Scripts/Input", "Scripts/Player", "Scripts/World",
            "Scripts/Pooling", "Scripts/Interactables", "Scripts/Presentation", "Scripts/Audio",
            "Scripts/UI", "Scripts/Persistence", "Scripts/Config", "Scripts/Editor", "Input",
            "Configs", "Prefabs/Player", "Prefabs/Road", "Prefabs/Interactables", "Prefabs/VFX",
            "Materials", "Meshes", "Textures", "Audio/Clips", "Audio/Mixers", "UI/Fonts",
            "UI/Icons", "Settings/URP", "Settings/Volumes", "Tests/EditMode", "Tests/PlayMode"
        };

        [MenuItem("Neon Tunnel Runner/Validate Project Setup")]
        public static void ValidateProject()
        {
            var errors = new List<string>();
            Require(Application.unityVersion == "6000.6.3f1", "Editor must be 6000.6.3f1.", errors);
            foreach (string folder in RequiredFolders)
                Require(AssetDatabase.IsValidFolder(Root + "/" + folder), "Missing folder: " + folder, errors);
            foreach (string folder in new[] { "Docs/Verification", "Evidence" })
                Require(Directory.Exists(folder), "Missing folder: " + folder, errors);
            Require(EditorSettings.serializationMode == SerializationMode.ForceText, "Enable Force Text.", errors);
            Require(VersionControlSettings.mode == "Visible Meta Files", "Enable Visible Meta Files.", errors);
            Require(PlayerSettings.colorSpace == ColorSpace.Linear, "Enable Linear color.", errors);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            Require(settings.FindProperty("activeInputHandler").intValue == 1, "Use New Input System only.", errors);
            CheckPipeline(GraphicsSettings.defaultRenderPipeline, "Graphics", errors);
            for (int i = 0; i < QualitySettings.names.Length; i++)
                CheckPipeline(QualitySettings.GetRenderPipelineAssetAt(i), "Quality " + QualitySettings.names[i], errors);
            Require(BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64),
                "Install Linux x86_64 build support for this Editor.", errors);
            Require(EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64,
                "Select the provisional Linux x86_64 development target.", errors);
            Require(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == GameScene), "Register Game in Build Profiles.", errors);
            var profile = BuildProfile.GetBuildProfileAtPath(Root + "/Settings/BuildProfiles/LinuxDevelopment.asset");
            Require(profile && profile.GetScenesForBuild().Any(s => s.enabled && s.path == GameScene),
                "LinuxDevelopment Build Profile must include Game.", errors);
            if (File.Exists(GameScene)) CheckScene(errors);
            else errors.Add("Missing scene: " + GameScene);
            if (errors.Count > 0) throw new BuildFailedException("Project setup failed:\n" + string.Join("\n", errors));
            Debug.Log("[ProjectSetupValidator] PASS: Editor, folders, serialization, input setting, URP/HDR, Linux profile and scene references.");
        }

        // Direct Editor batchmode entry; never quits an interactive Editor.
        public static void ValidateBatch()
        {
            try
            {
                ValidateProject();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void CheckPipeline(RenderPipelineAsset asset, string label, List<string> errors)
        {
            var urp = asset as UniversalRenderPipelineAsset;
            Require(urp, label + ": assign a URP asset.", errors);
            if (!urp) return;
            Require(urp.supportsHDR, label + ": enable HDR.", errors);
            var serialized = new SerializedObject(urp);
            var renderers = serialized.FindProperty("m_RendererDataList");
            int index = serialized.FindProperty("m_DefaultRendererIndex").intValue;
            Require(index >= 0 && index < renderers.arraySize &&
                renderers.GetArrayElementAtIndex(index).objectReferenceValue is UniversalRendererData,
                label + ": assign a valid Universal renderer.", errors);
        }

        private static void CheckScene(List<string> errors)
        {
            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(GameScene);
            bool openedForValidation = !scene.isLoaded;
            if (openedForValidation) scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                foreach (string name in new[] { "Systems", "World", "Presentation", "UI" })
                    Require(roots.Count(r => r.name == name) == 1, "Require one scene root: " + name, errors);
                var objects = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
                foreach (var go in objects)
                {
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) == 0, "Missing script on " + go.name, errors);
                    foreach (var component in go.GetComponents<Component>().Where(c => c))
                    {
                        Require(component.GetType().FullName != "UnityEngine.EventSystems.StandaloneInputModule",
                            "Remove legacy input module: " + go.name, errors);
                        var serialized = new SerializedObject(component);
                        var property = serialized.GetIterator();
                        while (property.NextVisible(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference)
                                Require(property.objectReferenceValue != null || property.objectReferenceEntityIdValue == default,
                                    "Missing reference: " + go.name + "." + property.propertyPath, errors);
                    }
                }
                var cameras = objects.SelectMany(g => g.GetComponents<Camera>()).ToArray();
                Require(cameras.Length == 1 && cameras[0].CompareTag("MainCamera") && cameras[0].allowHDR,
                    "Require one MainCamera with HDR.", errors);
                var data = cameras.Length == 1 ? cameras[0].GetComponent<UniversalAdditionalCameraData>() : null;
                Require(data && data.renderPostProcessing, "Enable camera Post Processing.", errors);
                var volumes = objects.SelectMany(g => g.GetComponents<Volume>()).ToArray();
                Require(volumes.Length == 1 && volumes[0].isGlobal && volumes[0].sharedProfile,
                    "Require one GlobalVolume with a profile.", errors);
                if (data && volumes.Length == 1)
                    Require((data.volumeLayerMask.value & (1 << volumes[0].gameObject.layer)) != 0,
                        "Camera Volume mask must include GlobalVolume.", errors);
                Require(objects.Sum(g => g.GetComponents<AudioListener>().Length) == 1, "Require one AudioListener.", errors);
                Require(objects.All(g => g.GetComponents<Renderer>().All(r => r.sharedMaterials.All(m => m && m.shader && m.shader.isSupported))),
                    "Resolve missing or unsupported graybox materials/shaders.", errors);
            }
            finally
            {
                if (openedForValidation) EditorSceneManager.CloseScene(scene, true);
                if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            }
        }

        private static void Require(bool condition, string message, List<string> errors)
        {
            if (!condition) errors.Add(message);
        }
    }
}
