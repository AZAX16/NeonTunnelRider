using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace NeonTunnelRunner
{
    public sealed class NightAssetTests
    {
        const string Root="Assets/NeonTunnelRunner/";
        [TestCase(-20,.3f,1)] [TestCase(0,.3f,1)] [TestCase(12.5f,.975f,2)]
        [TestCase(22,1.488f,2.76f)] [TestCase(25,1.65f,3)] [TestCase(50,3,5)] [TestCase(100,3,5)]
        public void AuthoredCurveClampsActualSpeed(float actual,float light,float emission)
        {
            var c=AssetDatabase.LoadAssetAtPath<PresentationConfig>(Root+"Configs/Presentation.asset");
            Assert.That(c.UnderglowIntensity(actual),Is.EqualTo(light).Within(.00001f));Assert.That(c.UnderglowEmission(actual),Is.EqualTo(emission).Within(.00001f));
        }
        [Test] public void ProductionMeshesHaveFiniteUnitNormalsAndPositiveChildTransforms()
        {
            foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{Root+"Meshes"}))
            {
                var m=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(m.vertexCount,Is.GreaterThan(0),m.name);
                Assert.That(m.normals.Length,Is.EqualTo(m.vertexCount),m.name);
                foreach(var n in m.normals) Assert.That(n.magnitude,Is.EqualTo(1f).Within(.001f),m.name);
                foreach(var v in m.vertices) Assert.That(float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z),Is.True,m.name);
            }
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{Root+"Prefabs"}))
            {
                var g=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach(var t in g.GetComponentsInChildren<Transform>(true))Assert.That(t.localScale.x>0&&t.localScale.y>0&&t.localScale.z>0,Is.True,g.name+"/"+t.name);
            }
        }
        [Test] public void SavedPrefabsKeepGameplayBoxesPivotsEndpointsAndBoundedLights()
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/Player/PlayerHovercar.prefab");var v=p.GetComponent<VehicleVisuals>();
            Assert.That(p.GetComponent<BoxCollider>().size,Is.EqualTo(new Vector3(1.7f,.9f,3.6f)));
            Assert.That(v.VisualPivot.localPosition,Is.EqualTo(Vector3.zero));Assert.That(v.VisualPivot.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(p.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().Count(),Is.LessThanOrEqualTo(3));
            Assert.That(p.GetComponentsInChildren<Light>(true).Length,Is.EqualTo(3));Assert.That(v.LeftHeadlight.type,Is.EqualTo(LightType.Spot));Assert.That(v.RightHeadlight.type,Is.EqualTo(LightType.Spot));
            Assert.That(v.UnderglowRenderer.sharedMaterial.IsKeywordEnabled("_EMISSION"),Is.True);
            var catalog=AssetDatabase.LoadAssetAtPath<PoolConfig>(Root+"Configs/Pools.asset");
            for(int i=0;i<catalog.EntryCount;i++)
            {
                var e=catalog.GetEntry(i);var segment=e.Prefab.GetComponent<RoadSegment>();var item=e.Prefab.GetComponent<Interactable>();
                if(segment)
                {
                    Assert.That(segment.IsValidFor(e.Key),Is.True);Assert.That(segment.GetComponentsInChildren<Collider>(true).Length,Is.EqualTo(1));
                    var surface=segment.transform.Find("Surface");Assert.That(surface.localPosition,Is.EqualTo(new Vector3(0,-.1f,30)));Assert.That(surface.localScale,Is.EqualTo(new Vector3(9.6f,.2f,60)));
                    Assert.That(segment.GetComponentsInChildren<Light>(true).Length,Is.EqualTo(1));Assert.That(segment.Streetlight.shadows,Is.EqualTo(LightShadows.None));
                    Assert.That(segment.Streetlight.enabled,Is.False);
                    foreach(var f in segment.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.EndsWith("Structure")))
                    foreach(var point in f.sharedMesh.vertices.Where(p=>Mathf.Abs(p.x)<4.8f && p.y>2f))
                        Assert.That(point.y,Is.GreaterThanOrEqualTo(8f),"Preserve the full-width tunnel clearance.");
                }
                if(item)
                {
                    var half=item.Category==InteractionCategory.Pickup?new Vector3(.5f,.6f,.5f):new Vector3(1.1f,.6f,2.2f);
                    Assert.That(item.HalfExtents,Is.EqualTo(half));Assert.That(item.GetComponent<BoxCollider>().size,Is.EqualTo(half*2));Assert.That(item.transform.Find("Visual").GetComponentsInChildren<Collider>(true),Is.Empty);
                    foreach(var mesh in item.transform.Find("Visual").GetComponentsInChildren<MeshFilter>(true))
                    foreach(var vertex in mesh.sharedMesh.vertices)
                    {
                        var point=item.transform.InverseTransformPoint(mesh.transform.TransformPoint(vertex));
                        Assert.That(Mathf.Abs(point.x),Is.LessThanOrEqualTo(half.x+.001f),item.name);
                        Assert.That(Mathf.Abs(point.z),Is.LessThanOrEqualTo(half.z+.001f),item.name);
                        Assert.That(point.y,Is.GreaterThanOrEqualTo(-half.y),item.name);
                        Assert.That(point.y+(item.Category==InteractionCategory.Pickup?.08f:0f),Is.LessThanOrEqualTo(half.y+.001f),item.name);
                    }
                }
            }
            var materials=AssetDatabase.FindAssets("t:Material",new[]{Root+"Materials"}).Select(id=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id))).Where(m=>m.shader.name=="Universal Render Pipeline/Lit").ToArray();
            Assert.That(materials.Length,Is.LessThanOrEqualTo(10));
        }
    }
}
