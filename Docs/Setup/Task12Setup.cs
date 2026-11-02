// One-time Edit-mode particle authoring. No runtime asset generation.
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace NeonTunnelRunner
{
    public static class Task12Setup
    {
        const string R="Assets/NeonTunnelRunner/";
        static void Set(UnityEngine.Object o,string n,UnityEngine.Object v)
        {var s=new SerializedObject(o);s.FindProperty(n).objectReferenceValue=v;s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(o);}
        static void Array(UnityEngine.Object o,string n,UnityEngine.Object[] v)
        {var s=new SerializedObject(o);var a=s.FindProperty(n);a.arraySize=v.Length;for(int i=0;i<v.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=v[i];s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(o);}
        static Transform Child(Transform p,string n)
        {var t=p.Find(n);if(t)return t;var g=new GameObject(n);g.transform.SetParent(p,false);return g.transform;}
        static Color C(string hex,float hdr=1f) {ColorUtility.TryParseHtmlString("#"+hex,out var c);return new Color(c.linear.r*hdr,c.linear.g*hdr,c.linear.b*hdr,1);}
        static Material Material(string n,bool additive,bool streak=false)
        {
            string texPath=R+"Textures/"+(streak?"ParticleStreak":"ParticleDot")+".png";
            var tex=new Texture2D(32,32,TextureFormat.RGBA32,false);
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {float dx=(x-15.5f)/15.5f,dy=(y-15.5f)/15.5f;float a=Mathf.Clamp01(1-Mathf.Sqrt(dx*dx+dy*dy));a=a*a;tex.SetPixel(x,y,new Color(1,1,1,a));}
            tex.Apply();System.IO.File.WriteAllBytes(texPath,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(texPath);
            var imp=(TextureImporter)AssetImporter.GetAtPath(texPath);imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.wrapMode=TextureWrapMode.Clamp;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
            string path=R+"Materials/"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(m,path);}
            m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));m.SetColor("_BaseColor",additive?new Color(3,3,3,1):Color.white);
            m.SetFloat("_Surface",1);m.SetFloat("_Blend",additive?2:0);m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);
            UnityEditor.BaseShaderGUI.SetMaterialKeywords(m,null,UnityEditor.Rendering.Universal.ShaderGUI.ParticleGUI.SetMaterialKeywords);
            EditorUtility.SetDirty(m);return m;
        }
        static ParticleSystem PS(Transform parent,string n,Material material,int max,float lifetime,float speed,float size,Color color,bool loop=false)
        {
            var t=Child(parent,n);t.gameObject.layer=LayerMask.NameToLayer("VFX");var ps=t.GetComponent<ParticleSystem>();if(!ps)ps=t.gameObject.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=loop;main.duration=loop?5:.15f;main.startLifetime=lifetime;main.startSpeed=speed;main.startSize=size;main.startColor=color;main.maxParticles=max;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.stopAction=ParticleSystemStopAction.None;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;main.useUnscaledTime=false;
            var emission=ps.emission;emission.rateOverTime=loop?30:0;emission.SetBursts(loop?new ParticleSystem.Burst[0]:new[]{new ParticleSystem.Burst(0,(short)max)});
            var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.08f;
            var fade=ps.colorOverLifetime;fade.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.8f,.25f),new GradientAlphaKey(0,1)});fade.color=g;
            var scale=ps.sizeOverLifetime;scale.enabled=true;scale.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,1),new Keyframe(1,.15f)));
            var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.maxParticleSize=.08f;
            return ps;
        }
        static GameObject Effect(PoolKey key,Material dot,Material streak)
        {
            string path=R+"Prefabs/VFX/"+key+".prefab";var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go=asset?PrefabUtility.LoadPrefabContents(path):new GameObject(key.ToString());
            go.layer=LayerMask.NameToLayer("VFX");var v=go.GetComponent<PooledVfx>();if(!v)v=go.AddComponent<PooledVfx>();
            ParticleSystem ps;
            if(key==PoolKey.Firework)
            {
                ps=PS(go.transform,"Rocket",dot,1,.9f,12f,.42f,C("FFD76A"));var main=ps.main;main.duration=.1f;
                var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=0;shape.radius=.01f;ps.transform.localEulerAngles=new Vector3(-90,0,0);
                var trails=ps.trails;trails.enabled=true;trails.ratio=1;trails.lifetime=.25f;trails.dieWithParticles=false;trails.worldSpace=false;trails.minVertexDistance=.08f;trails.widthOverTrail=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.8f),new Keyframe(1,0)));
                ps.GetComponent<ParticleSystemRenderer>().trailMaterial=dot;
                var burst=PS(ps.transform,"Burst",dot,180,1.05f,5.5f,.6f,C("FF4AC8"));
                var bm=burst.main;bm.startSpeed=new ParticleSystem.MinMaxCurve(3,8);bm.gravityModifier=.18f;
                var sub=ps.subEmitters;sub.enabled=true;while(sub.subEmittersCount>0)sub.RemoveSubEmitter(0);sub.AddSubEmitter(burst,ParticleSystemSubEmitterType.Death,ParticleSystemSubEmitterProperties.InheritNothing);
                Set(v,"colorfulBurst",burst);
            }
            else
            {
                bool spark=key==PoolKey.Spark,coin=key==PoolKey.CoinBurst;
                ps=PS(go.transform,"Burst",spark?streak:dot,spark?36:24,spark?.42f:.5f,spark?5:2.4f,spark?.16f:.21f,C(spark?"FF8B4D":coin?"FFD76A":"69F5A9",1));
                if(spark){ps.transform.localPosition=Vector3.up*.8f;var main=ps.main;main.startColor=new ParticleSystem.MinMaxGradient(C("FF8B4D"),C("FFF2D9"));main.gravityModifier=.4f;var r=ps.GetComponent<ParticleSystemRenderer>();r.renderMode=ParticleSystemRenderMode.Stretch;r.velocityScale=.055f;r.lengthScale=2;}
                if(!coin&&!spark){var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=.35f;ps.transform.localEulerAngles=new Vector3(-90,0,0);}
            }
            Set(v,"primary",ps);Array(v,"systems",go.GetComponentsInChildren<ParticleSystem>(true));
            var saved=PrefabUtility.SaveAsPrefabAsset(go,path);if(asset)PrefabUtility.UnloadPrefabContents(go);else UnityEngine.Object.DestroyImmediate(go);return saved;
        }
        public static object Configure()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)throw new Exception("Stop/save first.");
            var dot=Material("ParticleAdditive",true);var smoke=Material("ParticleSmoke",false);var streak=Material("ParticleStreak",true,true);
            var keys=new[]{PoolKey.Spark,PoolKey.CoinBurst,PoolKey.FuelBurst,PoolKey.Firework};var prefabs=keys.Select(k=>Effect(k,dot,streak)).ToArray();
            string playerPath=R+"Prefabs/Player/PlayerHovercar.prefab";var player=PrefabUtility.LoadPrefabContents(playerPath);
            foreach(string side in new[]{"Left","Right"})
            {
                var anchor=player.transform.Find("Visual/EffectAnchors/Exhaust"+side);
                var flame=PS(anchor,"Flame",dot,48,.22f,3,.25f,C("FF4AC8"),true);
                var fr=flame.GetComponent<ParticleSystemRenderer>();fr.renderMode=ParticleSystemRenderMode.Stretch;fr.lengthScale=2;fr.velocityScale=.04f;
                var shape=flame.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=8;shape.radius=.12f;
                var sm=PS(anchor,"Smoke",smoke,32,.65f,1.3f,.42f,new Color(.16f,.2f,.32f,.24f),true);
                var ss=sm.shape;ss.shapeType=ParticleSystemShapeType.Cone;ss.angle=18;ss.radius=.13f;
                var scale=sm.sizeOverLifetime;scale.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.4f),new Keyframe(1,1.4f)));
            }
            PrefabUtility.SaveAsPrefabAsset(player,playerPath);PrefabUtility.UnloadPrefabContents(player);
            var catalog=AssetDatabase.LoadAssetAtPath<PoolConfig>(R+"Configs/Pools.asset");var serialized=new SerializedObject(catalog);var entries=serialized.FindProperty("entries");
            for(int k=0;k<keys.Length;k++)
            {
                int index=-1;for(int i=0;i<entries.arraySize;i++)if(entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").enumValueIndex==(int)keys[k])index=i;
                if(index<0){index=entries.arraySize;entries.InsertArrayElementAtIndex(index);}var e=entries.GetArrayElementAtIndex(index);e.FindPropertyRelative("key").enumValueIndex=(int)keys[k];e.FindPropertyRelative("prefab").objectReferenceValue=prefabs[k];e.FindPropertyRelative("capacity").intValue=k<2?16:8;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(catalog);
            var m=UnityEngine.Object.FindAnyObjectByType<GameManager>();var l=m.Loop;var config=AssetDatabase.LoadAssetAtPath<PresentationConfig>(R+"Configs/Presentation.asset");
            var settings=new SerializedObject(config);settings.FindProperty("firstFireworkInterval").vector2Value=new Vector2(2,4);settings.FindProperty("fireworkInterval").vector2Value=new Vector2(8,14);settings.FindProperty("maximumVisibleFireworks").intValue=2;settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(config);
            var presentation=GameObject.Find("Presentation").transform;var root=Child(GameObject.Find("World").transform,"ActiveVFX");root.gameObject.layer=LayerMask.NameToLayer("VFX");
            var owner=Child(presentation,"VFX").gameObject;var vfx=owner.GetComponent<VfxController>();if(!vfx)vfx=owner.AddComponent<VfxController>();var director=owner.GetComponent<FireworkDirector>();if(!director)director=owner.AddComponent<FireworkDirector>();
            Set(vfx,"gameManager",m);Set(vfx,"pools",m.Pools);Set(vfx,"origin",l.Origin);Set(vfx,"activeRoot",root);Set(vfx,"configuration",config);Set(vfx,"fireworks",director);
            Array(vfx,"exhaust",new[]{l.Player.transform.Find("Visual/EffectAnchors/ExhaustLeft/Flame").GetComponent<ParticleSystem>(),l.Player.transform.Find("Visual/EffectAnchors/ExhaustLeft/Smoke").GetComponent<ParticleSystem>(),l.Player.transform.Find("Visual/EffectAnchors/ExhaustRight/Flame").GetComponent<ParticleSystem>(),l.Player.transform.Find("Visual/EffectAnchors/ExhaustRight/Smoke").GetComponent<ParticleSystem>()});
            Set(director,"gameManager",m);Set(director,"roads",l.Roads);Set(director,"origin",l.Origin);Set(director,"chaseCamera",l.Camera);Set(director,"effects",vfx);Set(director,"configuration",config);Set(l,"vfx",vfx);
            var os=new SerializedObject(l.Origin);var participants=os.FindProperty("additionalParticipants");bool found=false;for(int i=0;i<participants.arraySize;i++)if(participants.GetArrayElementAtIndex(i).objectReferenceValue==vfx)found=true;
            if(!found){participants.InsertArrayElementAtIndex(participants.arraySize);participants.GetArrayElementAtIndex(participants.arraySize-1).objectReferenceValue=vfx;os.ApplyModifiedPropertiesWithoutUndo();}
            var volume=UnityEngine.Object.FindAnyObjectByType<Volume>();var profile=volume.sharedProfile;
            if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.threshold.Override(1.15f);bloom.intensity.Override(.28f);bloom.scatter.Override(.55f);bloom.clamp.Override(12);bloom.highQualityFiltering.Override(false);
            if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.Neutral);
            if(!profile.TryGet<ColorAdjustments>(out var color)){color=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(color,profile);}color.postExposure.Override(0);
            EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(tone);EditorUtility.SetDirty(color);EditorUtility.SetDirty(profile);
            var camera=l.Camera.ViewCamera;camera.allowHDR=true;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            return new {saved=true,profile=AssetDatabase.GetAssetPath(profile),capacities=new[]{16,16,8,8},exhaustMax=160,bloom=new{threshold=1.15,intensity=.28,scatter=.55}};
        }
    }
}
