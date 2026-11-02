// Original project geometry, authored once in Edit mode. Never compiled into the player.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NeonTunnelRunner
{
    public static class Task11Art
    {
        const string Root="Assets/NeonTunnelRunner/";
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static Color Swatch(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out var c); return c.linear; }
        static Material Mat(string name,string hex,float emission=0f)
        {
            string path=Root+"Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
            m.SetColor("_BaseColor",Swatch(hex)); m.SetFloat("_Smoothness",.28f); m.SetFloat("_Metallic",.12f);
            m.SetColor("_EmissionColor",Swatch(hex)*emission); m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(m); materials[name]=m; return m;
        }
        static void Assign(UnityEngine.Object o,string name,UnityEngine.Object value)
        { var s=new SerializedObject(o); s.FindProperty(name).objectReferenceValue=value; s.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(o); }
        static Transform Child(Transform p,string name)
        { var t=p.Find(name); if(t) return t; t=new GameObject(name).transform; t.SetParent(p,false); t.gameObject.layer=p.gameObject.layer; return t; }
        static void Clear(Transform p) { for(int i=p.childCount-1;i>=0;i--) UnityEngine.Object.DestroyImmediate(p.GetChild(i).gameObject); }
        static Mesh SaveMesh(string name,Shape shape)
        {
            string path=Root+"Meshes/"+name+".asset"; var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh) { mesh=new Mesh {name=name}; AssetDatabase.CreateAsset(mesh,path); }
            mesh.Clear(); mesh.SetVertices(shape.vertices); mesh.SetTriangles(shape.indices,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh); return mesh;
        }
        static MeshRenderer Geometry(Transform parent,string name,Shape shape,string material)
        {
            var t=Child(parent,name); t.localPosition=Vector3.zero; t.localRotation=Quaternion.identity; t.localScale=Vector3.one;
            var filter=t.GetComponent<MeshFilter>(); if(!filter) filter=t.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh=SaveMesh(name,shape);
            var r=t.GetComponent<MeshRenderer>(); if(!r) r=t.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterial=materials[material];
            r.shadowCastingMode=ShadowCastingMode.Off; r.receiveShadows=false; return r;
        }
        static Light Light(Transform parent,string name,LightType type,Vector3 position,Vector3 euler,Color color,float intensity,float range,float angle=70f)
        {
            var t=Child(parent,name); t.localPosition=position; t.localEulerAngles=euler;
            var l=t.GetComponent<Light>(); if(!l) l=t.gameObject.AddComponent<Light>(); l.type=type; l.color=color; l.intensity=intensity;
            l.range=range; l.spotAngle=angle; l.innerSpotAngle=angle*.58f; l.shadows=LightShadows.None;
            l.cullingMask=1|(1<<LayerMask.NameToLayer("Obstacle"))|(1<<LayerMask.NameToLayer("Player"))|(1<<LayerMask.NameToLayer("Pickup"));
            l.renderMode=LightRenderMode.ForcePixel; l.GetUniversalAdditionalLightData(); return l;
        }
        public static object Configure()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Stop and save the scene first.");
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                var extra=light.GetComponent<UniversalAdditionalLightData>();
                if(extra && PrefabUtility.IsAddedComponentOverride(extra)) PrefabUtility.RevertAddedComponent(extra,InteractionMode.AutomatedAction);
            }
            Mat("ReferenceRoad","141B3F"); Mat("RoadArchitecture","141B3F",.18f); Mat("ReferenceMarking","35E6FF",.5f);
            Mat("StationaryCar","FF8B4D",.3f); Mat("MovingCar","FF8B4D",.18f);
            Mat("CoinGold","FFD76A",.65f); Mat("FuelMint","69F5A9",.55f);
            Mat("PlayerIndigo","141B3F",.4f); Mat("EnergyMagenta","FF4AC8",.65f); Mat("DepthViolet","8B5CFF",.15f);
            EditPrefab("Player/PlayerHovercar",Player);
            EditPrefab("Interactables/StationaryCar",g=>Hazard(g,false));
            EditPrefab("Interactables/MovingCar",g=>Hazard(g,true));
            EditPrefab("Interactables/Coin",Coin); EditPrefab("Interactables/FuelCan",Fuel);
            foreach(string name in new[]{"RoadOpen60","RoadTunnel60","RoadEntrance60","RoadExit60"}) EditPrefab("Road/"+name,Road);
            var cfg=AssetDatabase.LoadAssetAtPath<PresentationConfig>(Root+"Configs/Presentation.asset"); EditorUtility.SetDirty(cfg);
            // Serialize new defaults without modifying camera/motion tuning.
            var so=new SerializedObject(cfg); so.FindProperty("neonMaximumSpeed").floatValue=50f;
            so.FindProperty("underglowLightRange").vector2Value=new Vector2(.3f,3f);
            so.FindProperty("underglowEmissionRange").vector2Value=new Vector2(1f,5f); so.ApplyModifiedPropertiesWithoutUndo();
            Sky();
            UnityEngine.Object.FindAnyObjectByType<Camera>().clearFlags=CameraClearFlags.Skybox;
            var moon=GameObject.Find("ReferenceLight").GetComponent<Light>(); moon.type=LightType.Directional;
            moon.transform.eulerAngles=new Vector3(42f,-28f,0f); moon.color=new Color(.48f,.57f,1f); moon.intensity=.65f; moon.shadows=LightShadows.None;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.18f,.22f,.4f); RenderSettings.ambientEquatorColor=new Color(.12f,.15f,.27f);
            RenderSettings.ambientGroundColor=new Color(.06f,.08f,.16f); RenderSettings.ambientIntensity=1f;
            RenderSettings.fog=false; RenderSettings.defaultReflectionMode=DefaultReflectionMode.Skybox; RenderSettings.reflectionIntensity=.25f;
            // Four pixel lights per renderer was the template default. Seven covers the three player sources + bounded street sources.
            var rp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"Settings/URP/PC_RPAsset.asset");
            rp.maxAdditionalLightsCount=7; EditorUtility.SetDirty(rp);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            ProjectSetupValidator.ValidateProject();
            return new {saved=SceneManager.GetActiveScene().path,materials=materials.Count,moon=moon.intensity,pixelLightLimit=rp.maxAdditionalLightsCount};
        }
        static void EditPrefab(string relative,Action<GameObject> edit)
        {
            string path=Root+"Prefabs/"+relative+".prefab"; var go=PrefabUtility.LoadPrefabContents(path);
            try { edit(go); PrefabUtility.SaveAsPrefabAsset(go,path); } finally { PrefabUtility.UnloadPrefabContents(go); }
        }
        static void Player(GameObject go)
        {
            var visual=go.transform.Find("Visual");
            // Preserve the existing Body/Canopy renderer identities, pivot and serialized gameplay components.
            var body=visual.Find("Body"); var canopy=visual.Find("Canopy");
            body.localPosition=canopy.localPosition=Vector3.zero; body.localRotation=canopy.localRotation=Quaternion.identity;
            body.localScale=canopy.localScale=Vector3.one;
            var hull=new Shape(); hull.Hull(new[]{new Vector3(.7f,-.29f,-1.7f),new Vector3(.84f,-.14f,-1.25f),new Vector3(.84f,-.12f,.6f),new Vector3(.55f,-.12f,1.72f)},.33f,.62f);
            hull.Box(new Vector3(-.59f,-.02f,-1.38f),new Vector3(.44f,.4f,.55f)); hull.Box(new Vector3(.59f,-.02f,-1.38f),new Vector3(.44f,.4f,.55f));
            Geometry(visual,"Body",hull,"PlayerIndigo");
            var glass=new Shape(); glass.Hull(new[]{new Vector3(.29f,.12f,-.35f),new Vector3(.32f,.13f,.1f),new Vector3(.23f,.16f,.75f)},.39f,.72f);
            Geometry(visual,"Canopy",glass,"ReferenceMarking");
            var trim=new Shape();
            foreach(float x in new[]{-.62f,.62f}) { trim.Box(new Vector3(x,.09f,-1.67f),new Vector3(.32f,.17f,.04f)); trim.Box(new Vector3(x,.18f,-.98f),new Vector3(.055f,.025f,.72f)); }
            Geometry(visual,"TwinThrusters",trim,"EnergyMagenta");
            var ribs=new Shape(); foreach(float x in new[]{-.51f,.51f}) for(int i=0;i<4;i++) ribs.Box(new Vector3(x,.19f,-.6f-i*.17f),new Vector3(.23f,.025f,.065f));
            Geometry(visual,"DeckVents",ribs,"PlayerIndigo");
            var glow=new Shape(); glow.Box(new Vector3(0f,-.31f,-.15f),new Vector3(1.35f,.045f,2.8f));
            foreach(float x in new[]{-.81f,.81f}) glow.Box(new Vector3(x,-.24f,-.15f),new Vector3(.04f,.065f,2.8f));
            glow.Box(new Vector3(0,-.23f,-1.73f),new Vector3(1.38f,.065f,.04f));
            var renderer=Geometry(visual,"Underglow",glow,"ReferenceMarking");
            var lights=Child(go.transform,"Lights");
            var left=Light(lights,"HeadlightLeft",LightType.Spot,new Vector3(-.55f,-.08f,1.55f),new Vector3(3f,-7f,0),new Color(.65f,.87f,1f),220f,38f,50f);
            var right=Light(lights,"HeadlightRight",LightType.Spot,new Vector3(.55f,-.08f,1.55f),new Vector3(3f,7f,0),new Color(.65f,.87f,1f),220f,38f,50f);
            var under=Light(lights,"UnderglowLight",LightType.Point,new Vector3(0f,-.37f,-.2f),Vector3.zero,new Color(.08f,.78f,1f),.3f,3.8f);
            var lenses=new Shape(); foreach(float x in new[]{-.48f,.48f}) lenses.Box(new Vector3(x,-.045f,1.59f),new Vector3(.23f,.07f,.07f));
            Geometry(visual,"HeadlightLenses",lenses,"ReferenceMarking");
            var fx=Child(visual,"EffectAnchors"); foreach(float x in new[]{-.62f,.62f}) {var a=Child(fx,x<0?"ExhaustLeft":"ExhaustRight");a.localPosition=new Vector3(x,.08f,-1.72f);a.localRotation=Quaternion.Euler(0,180,0);}
            var v=go.GetComponent<VehicleVisuals>(); Assign(v,"leftHeadlight",left);Assign(v,"rightHeadlight",right);Assign(v,"underglowLight",under);Assign(v,"underglowRenderer",renderer);
        }
        static void Hazard(GameObject go,bool moving)
        {
            var visual=go.transform.Find("Visual"); Clear(visual); string name=moving?"Moving":"Stationary";
            var body=new Shape(); body.Hull(new[]{new Vector3(.8f,-.4f,-2.05f),new Vector3(1.04f,-.3f,-1.5f),new Vector3(1.04f,-.3f,1.45f),new Vector3(.75f,-.25f,2.05f)},moving?.03f:.16f,.9f);
            body.Hull(new[]{new Vector3(.72f,.02f,-1.1f),new Vector3(.72f,.02f,-.6f),new Vector3(.65f,.02f,.65f),new Vector3(.5f,.02f,1.15f)},moving?.38f:.56f,.75f);
            Geometry(visual,name+"Hull",body,moving?"MovingCar":"StationaryCar");
            var dark=new Shape(); dark.Box(new Vector3(0,moving?.31f:.49f,.3f),new Vector3(1.12f,.07f,.62f));
            foreach(float x in new[]{-.94f,.94f}) foreach(float z in new[]{-1.25f,1.2f}) dark.Box(new Vector3(x,-.29f,z),new Vector3(.2f,.5f,.65f));
            dark.Box(new Vector3(0,-.14f,-2.08f),new Vector3(1.6f,.14f,.1f));
            Geometry(visual,name+"Chassis",dark,"RoadArchitecture");
            var lamps=new Shape();
            foreach(float x in new[]{-.64f,.64f}) {lamps.Box(new Vector3(x,.04f,-2.11f),new Vector3(.47f,.075f,.055f)); if(!moving) lamps.Box(new Vector3(x,-.11f,-2.11f),new Vector3(.47f,.075f,.055f));}
            // Warning bar silhouette distinguishes the chunky parked blocker.
            if(!moving) lamps.Box(new Vector3(0,.585f,-.1f),new Vector3(.85f,.03f,.15f));
            Geometry(visual,name+"Lamps",lamps,moving?"MovingCar":"StationaryCar");
        }
        static void Coin(GameObject go)
        {
            var visual=go.transform.Find("Visual"); Clear(visual); var ring=new Shape();
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI*2/12,b=(i+1)*Mathf.PI*2/12;
                Vector3 A(float angle,float radius,float z)=>new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,z);
                ring.Quad(A(a,.43f,-.075f),A(a,.26f,-.075f),A(b,.26f,-.075f),A(b,.43f,-.075f));
                ring.Quad(A(a,.43f,.075f),A(b,.43f,.075f),A(b,.26f,.075f),A(a,.26f,.075f));
                ring.Quad(A(a,.43f,-.075f),A(b,.43f,-.075f),A(b,.43f,.075f),A(a,.43f,.075f));
                ring.Quad(A(a,.26f,-.075f),A(a,.26f,.075f),A(b,.26f,.075f),A(b,.26f,-.075f));
            }
            // Keep the pre-existing CoinRing asset GUID; the renderer uses this mesh.
            var t=Child(visual,"GoldRing"); t.gameObject.AddComponent<MeshFilter>().sharedMesh=SaveMesh("CoinRing",ring);
            var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=materials["CoinGold"];r.shadowCastingMode=ShadowCastingMode.Off;
        }
        static void Fuel(GameObject go)
        {
            var v=go.transform.Find("Visual");Clear(v); var can=new Shape();
            can.Hull(new[]{new Vector3(.28f,-.44f,-.18f),new Vector3(.34f,-.38f,-.14f),new Vector3(.34f,-.38f,.14f),new Vector3(.28f,-.44f,.18f)},.28f,.85f);
            can.Box(new Vector3(0,.31f,0),new Vector3(.53f,.1f,.3f));
            can.Box(new Vector3(-.08f,.48f,0),new Vector3(.34f,.065f,.12f));
            foreach(float x in new[]{-.22f,.06f}) can.Box(new Vector3(x,.405f,0),new Vector3(.06f,.17f,.12f));
            Geometry(v,"MintCan",can,"FuelMint");
            var detail=new Shape();detail.Box(new Vector3(.22f,.37f,0),new Vector3(.14f,.12f,.2f));
            foreach(float z in new[]{-.185f,.185f})
            { detail.Beam(new Vector3(-.18f,-.3f,z),new Vector3(.18f,.16f,z),.035f,.025f); detail.Beam(new Vector3(.18f,-.3f,z),new Vector3(-.18f,.16f,z),.035f,.025f); }
            Geometry(v,"CanCapAndStamp",detail,"RoadArchitecture");
        }
        static void Road(GameObject go)
        {
            var segment=go.GetComponent<RoadSegment>();
            // Preserve exact surface collider and endpoint objects. Replace decoration only.
            foreach(var t in go.transform.Cast<Transform>().ToArray()) if(t.name!="Surface" && t.name!="EntryMarker" && t.name!="ExitMarker") UnityEngine.Object.DestroyImmediate(t.gameObject);
            go.transform.Find("Surface").GetComponent<Renderer>().sharedMaterial=materials["ReferenceRoad"];
            var deco=Child(go.transform,"Scenery"); var cyan=new Shape();var structure=new Shape();var magenta=new Shape();var city=new Shape();var violet=new Shape();
            foreach(float x in new[]{-4.8f,-1.6f,1.6f,4.8f}) cyan.Box(new Vector3(x,.012f,30f),new Vector3(.065f,.02f,60f));
            foreach(float x in new[]{-5.1f,5.1f}) {structure.Box(new Vector3(x,.36f,30),new Vector3(.3f,.42f,60));}
            bool open=segment.Environment==RoadEnvironment.Open;
            float start=segment.Environment==RoadEnvironment.Entrance?30f:0f, end=segment.Environment==RoadEnvironment.Exit?30f:60f;
            if(!open)
            {
                Vector3[] arch={new Vector3(-5.45f,0,0),new Vector3(-5.45f,8.2f,0),new Vector3(-3.2f,8.6f,0),new Vector3(3.2f,8.6f,0),new Vector3(5.45f,8.2f,0),new Vector3(5.45f,0,0)};
                for(float z=start;z<end;z+=15)
                {
                    for(int i=0;i<arch.Length-1;i++)
                    {
                        structure.Beam(arch[i]+Vector3.forward*z,arch[i+1]+Vector3.forward*z,.42f,.5f);
                        magenta.Beam(arch[i]+Vector3.forward*(z-.27f),arch[i+1]+Vector3.forward*(z-.27f),.065f,.025f);
                    }
                }
                // Solid panels close the roof while the lower walls stay open to the side skyline.
                structure.Box(new Vector3(0,8.88f,(start+end)/2),new Vector3(6.7f,.25f,end-start));
                foreach(float sign in new[]{-1f,1f})
                    structure.Beam(new Vector3(sign*5.45f,8.35f,(start+end)/2),new Vector3(sign*3.25f,8.7f,(start+end)/2),.22f,end-start);
            }
            for(int side=-1;side<=1;side+=2)
            {
                // Poles and skyline are outside the corridor; every piece shares the segment lifecycle.
                float z=side<0?20f:45f, x=side*6.3f;
                structure.Beam(new Vector3(x,0,z),new Vector3(x,5.6f,z),.14f,.18f);
                structure.Beam(new Vector3(x,5.6f,z),new Vector3(side*5.1f,6.1f,z),.14f,.18f);
                magenta.Box(new Vector3(side*5.1f,6f,z),new Vector3(.8f,.07f,1.1f));
                for(int i=0;i<4;i++)
                {
                    float height=5+(i*7+side+9)%12, bx=side*(19f+i*6), bz=7+i*14;
                    city.Box(new Vector3(bx,height/2-1,bz),new Vector3(3.5f,height,8f));
                    if(i%2==0) violet.Box(new Vector3(bx-side*1.76f,height*.7f,bz),new Vector3(.025f,.09f,3f));
                }
            }
            Geometry(deco,go.name+"LaneMarks",cyan,"ReferenceMarking");Geometry(deco,go.name+"Structure",structure,"RoadArchitecture");
            Geometry(deco,go.name+"ArchTrim",magenta,"EnergyMagenta");Geometry(deco,go.name+"Skyline",city,"PlayerIndigo");Geometry(deco,go.name+"Windows",violet,"DepthViolet");
            var light=Light(deco,"Streetlight",LightType.Spot,new Vector3(-5.1f,5.9f,20),new Vector3(62f,90f,0),new Color(.48f,.51f,1f),20f,20f,110f);
            light.enabled=false; Assign(segment,"streetlight",light);
        }
        static void Sky()
        {
            const int width=2048,height=1024;var pixels=new Color[width*height];
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            {
                float latitude=(float)y/height;float cloud=(Mathf.Sin(x*.008f+Mathf.Sin(y*.012f)*2)+Mathf.Sin(x*.017f-y*.019f))*.5f;
                pixels[y*width+x]=Color.Lerp(Swatch("070B22"),Swatch("141B3F"),Mathf.Clamp01(.3f+cloud*.1f+(.5f-Mathf.Abs(latitude-.5f))*.3f));
            }
            var random=new System.Random(11031);
            for(int i=0;i<1500;i++) {int x=random.Next(width),y=random.Next(height/2,height-12);float b=.25f+(float)random.NextDouble()*.65f;pixels[y*width+x]=new Color(.65f*b,.8f*b,b);if(i%7==0) pixels[y*width+(x+1)%width]=new Color(.18f,.25f,.4f);}
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false,true);texture.SetPixels(pixels);texture.Apply();
            string path=Root+"Textures/RetroNightSky.png";System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            string matPath=Root+"Materials/RetroNightSky.mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(!sky) {sky=new Material(Shader.Find("Skybox/Panoramic"));AssetDatabase.CreateAsset(sky,matPath);}
            sky.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(path));sky.SetFloat("_Exposure",1f);sky.SetColor("_Tint",Color.gray);EditorUtility.SetDirty(sky);RenderSettings.skybox=sky;
        }
        public sealed class Shape
        {
            public readonly List<Vector3> vertices=new List<Vector3>(); public readonly List<int> indices=new List<int>();
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d) {int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            public void Triangle(Vector3 a,Vector3 b,Vector3 c) {int n=vertices.Count;vertices.AddRange(new[]{a,b,c});indices.AddRange(new[]{n,n+1,n+2});}
            public void Box(Vector3 p,Vector3 size,Quaternion? rotation=null)
            {
                var h=size*.5f;var q=rotation??Quaternion.identity;Vector3 V(float x,float y,float z)=>p+q*Vector3.Scale(new Vector3(x,y,z),h);
                Quad(V(-1,-1,-1),V(-1,1,-1),V(1,1,-1),V(1,-1,-1));Quad(V(1,-1,1),V(1,1,1),V(-1,1,1),V(-1,-1,1));
                Quad(V(-1,-1,1),V(-1,1,1),V(-1,1,-1),V(-1,-1,-1));Quad(V(1,-1,-1),V(1,1,-1),V(1,1,1),V(1,-1,1));
                Quad(V(-1,1,-1),V(-1,1,1),V(1,1,1),V(1,1,-1));Quad(V(-1,-1,1),V(-1,-1,-1),V(1,-1,-1),V(1,-1,1));
            }
            public void Beam(Vector3 a,Vector3 b,float width,float depth) {var delta=b-a;Box((a+b)*.5f,new Vector3(width,delta.magnitude,depth),Quaternion.FromToRotation(Vector3.up,delta.normalized));}
            public void Hull(Vector3[] halfOutline,float top,float topWidth)
            {
                var outline=halfOutline.Concat(halfOutline.Reverse().Select(v=>new Vector3(-v.x,v.y,v.z))).ToArray();
                var upper=outline.Select(v=>new Vector3(v.x*topWidth,top,v.z*.83f)).ToArray();
                for(int i=0;i<outline.Length;i++) {int j=(i+1)%outline.Length;Quad(outline[i],upper[i],upper[j],outline[j]);
                    Triangle(new Vector3(0,top,0),upper[j],upper[i]);Triangle(Vector3.up*halfOutline.Min(v=>v.y),outline[i],outline[j]);}
            }
        }
    }
}
