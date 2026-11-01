using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonTunnelRunner
{
    public static class Task06Setup
    {
        private const string Root="Assets/NeonTunnelRunner";
        public static object Configure()
        {
            if (Application.isPlaying || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Configure a saved stopped scene.");
            var scene=EditorSceneManager.OpenScene(ProjectSetupValidator.GameScene);
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers=tags.FindProperty("layers");
            foreach (var entry in new[] { (8,"Player"),(9,"Obstacle") })
            { var layer=layers.GetArrayElementAtIndex(entry.Item1); if (layer.stringValue!="" && layer.stringValue!=entry.Item2) throw new InvalidOperationException("Layer occupied."); layer.stringValue=entry.Item2; }
            tags.ApplyModifiedPropertiesWithoutUndo();
            var prefabs=new GameObject[2];
            for (int i=0;i<2;i++)
            {
                string name=i==0 ? "StationaryCar" : "MovingCar";
                var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+name+".mat");
                if (!material) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,Root+"/Materials/"+name+".mat"); }
                material.SetColor("_BaseColor",i==0 ? new Color(1f,0.36f,0.035f) : new Color(0.85f,0.035f,0.06f));
                var go=new GameObject(name); go.layer=9;
                try
                {
                    Interactable car=i==0 ? go.AddComponent<StationaryCar>() : go.AddComponent<MovingCar>();
                    var box=go.GetComponent<BoxCollider>(); box.center=Vector3.zero; box.size=car.HalfExtents*2f; box.isTrigger=true;
                    if (i==1) { var body=go.AddComponent<Rigidbody>(); body.isKinematic=true; body.useGravity=false; body.interpolation=RigidbodyInterpolation.Interpolate; body.constraints=RigidbodyConstraints.FreezeRotation|RigidbodyConstraints.FreezePositionX|RigidbodyConstraints.FreezePositionY; }
                    var visual=new GameObject("Visual").transform; visual.SetParent(go.transform,false);
                    Box(visual,"Body",new Vector3(0,-0.1f,0),new Vector3(2.2f,0.65f,4.4f),material);
                    Box(visual,"Cabin",new Vector3(0,0.37f,0.15f),new Vector3(1.65f,0.45f,1.9f),AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/RoadArchitecture.mat"));
                    foreach (float x in new[] {-0.78f,0.78f}) Box(visual,"TailLamp",new Vector3(x,-0.02f,-2.21f),new Vector3(0.4f,0.18f,0.035f),AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/ReferenceMarking.mat"));
                    Assign(car,"visual",visual);
                    prefabs[i]=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/Interactables/"+name+".prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
            var catalog=AssetDatabase.LoadAssetAtPath<PoolConfig>(Root+"/Configs/Pools.asset");
            var so=new SerializedObject(catalog); var entries=so.FindProperty("entries");
            for (int i=0;i<2;i++)
            {
                int key=(int)(i==0 ? PoolKey.Stationary : PoolKey.Moving), at=-1;
                for (int j=0;j<entries.arraySize;j++) if (entries.GetArrayElementAtIndex(j).FindPropertyRelative("key").enumValueIndex==key) at=j;
                if (at<0) { at=entries.arraySize; entries.arraySize++; }
                var entry=entries.GetArrayElementAtIndex(at); entry.FindPropertyRelative("key").enumValueIndex=key;
                entry.FindPropertyRelative("prefab").objectReferenceValue=prefabs[i]; entry.FindPropertyRelative("capacity").intValue=i==0 ? 64 : 32;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            var playerPrefab=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/Player/PlayerHovercar.prefab");
            try
            {
                playerPrefab.layer=8;
                var survival=playerPrefab.GetComponent<PlayerSurvival>() ?? playerPrefab.AddComponent<PlayerSurvival>();
                var sensor=playerPrefab.GetComponent<InteractionSensor>() ?? playerPrefab.AddComponent<InteractionSensor>();
                Assign(sensor,"player",playerPrefab.GetComponent<PlayerController>()); Assign(sensor,"survival",survival);
                PrefabUtility.SaveAsPrefabAsset(playerPrefab,Root+"/Prefabs/Player/PlayerHovercar.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(playerPrefab); }
            var manager=UnityEngine.Object.FindAnyObjectByType<GameManager>(); var loop=manager.Loop;
            var player=loop.Player; var sceneSurvival=player.GetComponent<PlayerSurvival>(); var sceneSensor=player.GetComponent<InteractionSensor>();
            Assign(sceneSurvival,"gameManager",manager); Assign(sceneSensor,"registry",loop.Planner);
            Assign(loop,"survival",sceneSurvival); Assign(loop,"interactionSensor",sceneSensor);
            Assign(loop.Planner,"pools",manager.Pools); Assign(loop.Planner,"activeRoot",GameObject.Find("World").transform.Find("Interactables"));
            var origin=new SerializedObject(loop.Origin); var participants=origin.FindProperty("additionalParticipants");
            participants.arraySize=1; participants.GetArrayElementAtIndex(0).objectReferenceValue=loop.Planner; origin.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            ProjectSetupValidator.ValidateProject();
            return new { saved=scene.path, catalogEntries=catalog.EntryCount, stationary=64, moving=32, root=loop.Planner.OriginRoot.name,
                sensor=sceneSensor.name, survival=sceneSurvival.name, originRegistered=loop.Origin.HasParticipant(loop.Planner) };
        }
        private static void Assign(UnityEngine.Object target,string field,UnityEngine.Object value)
        { var so=new SerializedObject(target); so.FindProperty(field).objectReferenceValue=value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        { var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=material; UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); }
    }
}
