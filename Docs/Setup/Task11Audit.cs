using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace NeonTunnelRunner
{
    public static class Task11Audit
    {
        const string Root="Assets/NeonTunnelRunner/";
        public static object Inspect()
        {
            ProjectSetupValidator.ValidateProject();var m=UnityEngine.Object.FindAnyObjectByType<GameManager>();var l=m.Loop;var camera=l.Camera.ViewCamera;var data=camera.GetComponent<UniversalAdditionalCameraData>();
            var rp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"Settings/URP/PC_RPAsset.asset");
            var volume=UnityEngine.Object.FindAnyObjectByType<Volume>();
            return new {scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,playing=Application.isPlaying,instanceClear=GameManager.Instance==null,timeScale=Time.timeScale,
                camera=new{camera.fieldOfView,camera.nearClipPlane,camera.farClipPlane,camera.allowHDR,camera.clearFlags,data.renderPostProcessing,volumeMask=data.volumeLayerMask.value,count=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude).Length,listeners=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length},
                lighting=new{sky=AssetDatabase.GetAssetPath(RenderSettings.skybox),RenderSettings.ambientMode,ambientSky=RenderSettings.ambientSkyColor.ToString("F5"),ambientEquator=RenderSettings.ambientEquatorColor.ToString("F5"),ambientGround=RenderSettings.ambientGroundColor.ToString("F5"),RenderSettings.fog,
                    sceneLights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude).Select(light=>new{light.name,type=light.type.ToString(),light.intensity,light.range,light.spotAngle,light.innerSpotAngle,light.cullingMask,shadows=light.shadows.ToString(),position=light.transform.position.ToString("F3")}).ToArray()},
                pipeline=new{quality=QualitySettings.names[QualitySettings.GetQualityLevel()],activePipeline=AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline),rp.supportsHDR,rp.maxAdditionalLightsCount,rp.supportsMainLightShadows,rp.supportsAdditionalLightShadows,renderer="PC_Renderer Forward; existing SSAO retained",volume=AssetDatabase.GetAssetPath(volume.sharedProfile),overrides=volume.sharedProfile.components.Select(c=>c.name).ToArray()},
                materials=AssetDatabase.FindAssets("t:Material",new[]{Root+"Materials"}).Select(id=>{var mat=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));return new{path=AssetDatabase.GetAssetPath(mat),shader=mat.shader.name,baseColor=mat.HasProperty("_BaseColor")?mat.GetColor("_BaseColor").ToString("F5"):null,emission=mat.HasProperty("_EmissionColor")?mat.GetColor("_EmissionColor").ToString("F5"):null,keyword=mat.IsKeywordEnabled("_EMISSION")};}).ToArray(),
                prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{Root+"Prefabs"}).Select(id=>{var go=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(id));return new{path=AssetDatabase.GetAssetPath(go),renderers=go.GetComponentsInChildren<Renderer>(true).Length,triangles=go.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.triangles.Length/3),materials=go.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().Count(),materialAssignments=go.GetComponentsInChildren<Renderer>(true).Select(r=>new{r.name,materials=r.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray()}).ToArray(),colliders=go.GetComponentsInChildren<Collider>(true).Length,lights=go.GetComponentsInChildren<Light>(true).Select(light=>new{light.name,type=light.type.ToString(),light.intensity,light.range,light.spotAngle,light.cullingMask,light.enabled,position=light.transform.localPosition.ToString("F3"),euler=light.transform.localEulerAngles.ToString("F3"),shadows=light.shadows.ToString()}).ToArray()};}).ToArray(),
                meshes=AssetDatabase.FindAssets("t:Mesh",new[]{Root+"Meshes"}).Select(id=>{var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(id));return new{mesh.name,mesh.vertexCount,triangles=mesh.triangles.Length/3,bounds=mesh.bounds.ToString("F4")};}).ToArray(),
                playerAssemblies=UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.PlayerWithoutTestAssemblies).Where(a=>a.name.StartsWith("NeonTunnelRunner")).Select(a=>new{a.name,references=a.assemblyReferences.Select(r=>r.name).ToArray()}).ToArray()};
        }
    }
}
