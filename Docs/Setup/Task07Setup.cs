using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonTunnelRunner
{
    public static class Task07Setup
    {
        private const string Root="Assets/NeonTunnelRunner";
        public static object Configure()
        {
            if (Application.isPlaying || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Configure a saved stopped scene.");
            var scene=EditorSceneManager.OpenScene(ProjectSetupValidator.GameScene);
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layer=tags.FindProperty("layers").GetArrayElementAtIndex(10);
            if (layer.stringValue!="" && layer.stringValue!="Pickup") throw new InvalidOperationException("Layer 10 occupied.");
            layer.stringValue="Pickup"; tags.ApplyModifiedPropertiesWithoutUndo();
            var gold=Material("CoinGold",new Color(1f,0.67f,0.055f));
            var mint=Material("FuelMint",new Color(0.16f,1f,0.62f));
            var dark=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/RoadArchitecture.mat");
            var ring=Ring();
            var prefabs=new GameObject[2];
            for (int i=0;i<2;i++)
            {
                string name=i==0 ? "Coin" : "FuelCan";
                var go=new GameObject(name); go.layer=10;
                try
                {
                    Interactable pickup=i==0 ? go.AddComponent<Coin>() : go.AddComponent<FuelCan>();
                    var visual=new GameObject("Visual").transform; visual.SetParent(go.transform,false);
                    var so=new SerializedObject(pickup);
                    so.FindProperty("halfExtents").vector3Value=PatternValidator.PickupHalfExtents;
                    so.FindProperty("visual").objectReferenceValue=visual; so.ApplyModifiedPropertiesWithoutUndo();
                    var box=go.GetComponent<BoxCollider>(); box.center=Vector3.zero; box.size=PatternValidator.PickupHalfExtents*2f; box.isTrigger=true;
                    if (i==0)
                    {
                        var shape=new GameObject("GoldRing"); shape.transform.SetParent(visual,false); shape.AddComponent<MeshFilter>().sharedMesh=ring;
                        shape.AddComponent<MeshRenderer>().sharedMaterial=gold;
                    }
                    else
                    {
                        Box(visual,"Can",new Vector3(0f,-0.08f,0f),new Vector3(0.65f,0.7f,0.38f),mint);
                        Box(visual,"Shoulder",new Vector3(0f,0.3f,0f),new Vector3(0.5f,0.1f,0.34f),mint);
                        Box(visual,"HandleTop",new Vector3(-0.08f,0.47f,0f),new Vector3(0.35f,0.08f,0.13f),mint);
                        foreach (float x in new[] {-0.22f,0.06f}) Box(visual,"HandleSide",new Vector3(x,0.4f,0f),new Vector3(0.07f,0.13f,0.13f),mint);
                        Box(visual,"Cap",new Vector3(0.22f,0.37f,0f),new Vector3(0.15f,0.1f,0.2f),dark);
                        Box(visual,"Label",new Vector3(0f,-0.07f,-0.196f),new Vector3(0.38f,0.3f,0.015f),dark);
                        Box(visual,"FuelMark",new Vector3(0f,-0.07f,-0.208f),new Vector3(0.075f,0.2f,0.015f),mint);
                    }
                    prefabs[i]=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/Interactables/"+name+".prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
            var catalog=AssetDatabase.LoadAssetAtPath<PoolConfig>(Root+"/Configs/Pools.asset");
            var catalogObject=new SerializedObject(catalog); var entries=catalogObject.FindProperty("entries");
            for (int i=0;i<2;i++)
            {
                int key=(int)(i==0 ? PoolKey.Coin : PoolKey.Fuel), at=-1;
                for (int j=0;j<entries.arraySize;j++) if (entries.GetArrayElementAtIndex(j).FindPropertyRelative("key").enumValueIndex==key) at=j;
                if (at<0) { at=entries.arraySize; entries.arraySize++; }
                var entry=entries.GetArrayElementAtIndex(at); entry.FindPropertyRelative("key").enumValueIndex=key;
                entry.FindPropertyRelative("prefab").objectReferenceValue=prefabs[i]; entry.FindPropertyRelative("capacity").intValue=i==0 ? 160 : 24;
            }
            catalogObject.ApplyModifiedPropertiesWithoutUndo();
            var manager=UnityEngine.Object.FindAnyObjectByType<GameManager>(); var loop=manager.Loop;
            var progression=manager.GetComponent<ProgressionSystem>() ?? manager.gameObject.AddComponent<ProgressionSystem>();
            Assign(progression,"gameManager",manager); Assign(loop,"progression",progression); Assign(loop.Sensor,"progression",progression);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            ProjectSetupValidator.ValidateProject();
            return new { saved=scene.path, catalogEntries=catalog.EntryCount, coins=160, fuel=24, total=304, contactCapacity=280,
                progression=progression.name, sensor=loop.Sensor.name, root=loop.Planner.OriginRoot.name };
        }
        private static Material Material(string name,Color color)
        {
            string path=Root+"/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path); }
            material.SetColor("_BaseColor",color); material.SetFloat("_Smoothness",0.45f); return material;
        }
        private static Mesh Ring()
        {
            string path=Root+"/Meshes/CoinRing.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing) return existing;
            const int segments=24, sides=8;
            var vertices=new Vector3[segments*sides]; var normals=new Vector3[vertices.Length]; var triangles=new int[segments*sides*6];
            for (int i=0;i<segments;i++) for (int j=0;j<sides;j++)
            {
                float a=i*2f*Mathf.PI/segments, b=j*2f*Mathf.PI/sides;
                var radial=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0f);
                normals[i*sides+j]=radial*Mathf.Cos(b)+Vector3.forward*Mathf.Sin(b);
                vertices[i*sides+j]=radial*0.34f+normals[i*sides+j]*0.09f;
                int a0=i*sides+j, a1=((i+1)%segments)*sides+j, b0=i*sides+(j+1)%sides, b1=((i+1)%segments)*sides+(j+1)%sides;
                int t=(i*sides+j)*6; triangles[t]=a0; triangles[t+1]=a1; triangles[t+2]=b0; triangles[t+3]=a1; triangles[t+4]=b1; triangles[t+5]=b0;
            }
            var mesh=new Mesh { name="CoinRing", vertices=vertices, normals=normals, triangles=triangles }; mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,path); return mesh;
        }
        private static void Assign(UnityEngine.Object target,string field,UnityEngine.Object value)
        { var so=new SerializedObject(target); so.FindProperty(field).objectReferenceValue=value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        { var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=material; UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); }
    }
}
