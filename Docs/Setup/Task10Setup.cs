using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonTunnelRunner
{
    public static class Task10Setup
    {
        private const string Root="Assets/NeonTunnelRunner/";
        private static void Assign(UnityEngine.Object target,string name,UnityEngine.Object value)
        { var s=new SerializedObject(target); s.FindProperty(name).objectReferenceValue=value; s.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target); }
        public static object Configure()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save/stop Game first.");
            var manager=UnityEngine.Object.FindAnyObjectByType<GameManager>(); var loop=manager.Loop;
            var config=AssetDatabase.LoadAssetAtPath<PresentationConfig>(Root+"Configs/Presentation.asset");
            if (!config) { config=ScriptableObject.CreateInstance<PresentationConfig>(); AssetDatabase.CreateAsset(config,Root+"Configs/Presentation.asset"); }
            string path=Root+"Prefabs/Player/PlayerHovercar.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var visuals=prefab.GetComponent<VehicleVisuals>();
                Assign(visuals,"player",prefab.GetComponent<PlayerController>());
                Assign(visuals,"configuration",config); Assign(visuals,"visualPivot",prefab.transform.Find("Visual"));
                PrefabUtility.SaveAsPrefabAsset(prefab,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var camera=UnityEngine.Object.FindAnyObjectByType<Camera>();
            var follow=camera.GetComponent<ChaseCamera>(); if (!follow) follow=camera.gameObject.AddComponent<ChaseCamera>();
            Assign(follow,"gameManager",manager); Assign(follow,"player",loop.Player); Assign(follow,"configuration",config);
            Assign(follow,"viewCamera",camera); Assign(follow,"worldOrigin",loop.Origin);
            Assign(loop,"chaseCamera",follow);
            var origin=new SerializedObject(loop.Origin); var participants=origin.FindProperty("additionalParticipants");
            participants.arraySize=2; participants.GetArrayElementAtIndex(0).objectReferenceValue=loop.Planner;
            participants.GetArrayElementAtIndex(1).objectReferenceValue=follow; origin.ApplyModifiedPropertiesWithoutUndo();
            camera.transform.SetParent(GameObject.Find("Presentation").transform,true);
            Assign(loop.Origin,"chaseCamera",camera.transform);
            camera.fieldOfView=config.FieldOfView; camera.nearClipPlane=config.NearClip; camera.farClipPlane=config.FarClip;
            follow.ResetPresentation();
            var light=GameObject.Find("ReferenceLight"); var extra=light.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
            if (extra) UnityEngine.Object.DestroyImmediate(extra);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(manager.gameObject.scene); EditorSceneManager.SaveScene(manager.gameObject.scene);
            ProjectSetupValidator.ValidateProject();
            return Inspect();
        }
        public static object Inspect()
        {
            var m=UnityEngine.Object.FindAnyObjectByType<GameManager>(); var l=m.Loop; var c=l.Camera;
            if (!c || c.Owner!=m || c.Player!=l.Player || c.Origin!=l.Origin || !l.Origin.HasParticipant(c) ||
                l.Origin.ChaseCamera!=c.transform || c.Configuration!=l.Visuals.Configuration ||
                c.transform.parent.name!="Presentation" || !l.Origin.TryValidate(out _) ||
                l.Visuals.VisualPivot.GetComponentsInChildren<Collider>(true).Length!=0 ||
                UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude).Length!=1 ||
                UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length!=1)
                throw new InvalidOperationException("Task10 wiring invalid.");
            return new {passed=true, scene=m.gameObject.scene.path, dirty=m.gameObject.scene.isDirty,
                camera=c.name, parent=c.transform.parent.name, config=AssetDatabase.GetAssetPath(c.Configuration),
                position=c.transform.position.ToString("F3"), euler=c.transform.eulerAngles.ToString("F3"),
                c.ViewCamera.fieldOfView,c.ViewCamera.nearClipPlane,c.ViewCamera.farClipPlane,
                interpolation=l.Player.Body.interpolation.ToString(), pivot=l.Visuals.VisualPivot.name,
                playerPrefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(l.Player),
                playing=EditorApplication.isPlaying, timeScale=Time.timeScale, instanceClear=GameManager.Instance==null};
        }
    }
}
