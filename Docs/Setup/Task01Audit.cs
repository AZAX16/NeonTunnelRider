// Read-only snapshot through Pipeline script run; never included in a player.
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NeonTunnelRunner
{
    public static class Task01Audit
    {
        public static object Inspect()
        {
            ProjectSetupValidator.ValidateProject();
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ProjectSetupValidator.GameScene)
                throw new InvalidOperationException("Open Game for the scene snapshot.");
            var objects = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var camera = objects.Select(t => t.GetComponent<Camera>()).Single(c => c);
            var volume = objects.Select(t => t.GetComponent<Volume>()).Single(v => v);
            var urp = (UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline;
            var renderer = (UniversalRendererData)new SerializedObject(urp).FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue;
            var profile = BuildProfile.GetActiveBuildProfile();
            string[] ids = { "com.unity.pipeline", "com.unity.render-pipelines.universal", "com.unity.render-pipelines.core",
                "com.unity.inputsystem", "com.unity.test-framework", "com.unity.ugui" };
            return new
            {
                unityVersion = Application.unityVersion,
                compiling = EditorApplication.isCompiling,
                compilationFailed = EditorUtility.scriptCompilationFailed,
                target = EditorUserBuildSettings.activeBuildTarget.ToString(),
                linuxModuleInstalled = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64),
                backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone).ToString(),
                graphicsDevice = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceVersion,
                packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Where(p => ids.Contains(p.name)).Select(p => new { p.name, p.version, source = p.source.ToString() }).ToArray(),
                pipeline = new { asset = AssetDatabase.GetAssetPath(urp), renderer = AssetDatabase.GetAssetPath(renderer), renderingMode = renderer.renderingMode.ToString(), urp.supportsHDR, drawer = urp.gpuResidentDrawerMode.ToString() },
                quality = Enumerable.Range(0, QualitySettings.names.Length).Select(i => new { name = QualitySettings.names[i], asset = AssetDatabase.GetAssetPath(QualitySettings.GetRenderPipelineAssetAt(i)) }).ToArray(),
                buildProfile = new { name = profile ? profile.name : "Platform profile", path = AssetDatabase.GetAssetPath(profile), scenes = profile.GetScenesForBuild().Select(s => s.path).ToArray(), development = EditorUserBuildSettings.development },
                scene = new { scene.path, scene.isDirty, roots = scene.GetRootGameObjects().Select(r => r.name).ToArray(), objects = objects.Select(t => new { name = t.name, parent = t.parent ? t.parent.name : null, position = t.localPosition.ToString("F3"), scale = t.localScale.ToString("F3") }).ToArray() },
                camera = new { camera.allowHDR, camera.fieldOfView, camera.nearClipPlane, camera.farClipPlane, postProcessing = camera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing, volumeMask = camera.GetComponent<UniversalAdditionalCameraData>().volumeLayerMask.value },
                volume = new { volume.isGlobal, profile = AssetDatabase.GetAssetPath(volume.sharedProfile), overrides = volume.sharedProfile.components.Count, layer = volume.gameObject.layer },
                editorAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor).Where(a => a.name.StartsWith("NeonTunnelRunner.")).Select(a => new { a.name, references = a.assemblyReferences.Select(r => r.name).ToArray() }).ToArray(),
                playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player).Where(a => a.name.StartsWith("NeonTunnelRunner.")).Select(a => new { a.name, references = a.assemblyReferences.Select(r => r.name).ToArray() }).ToArray()
            };
        }
    }
}
