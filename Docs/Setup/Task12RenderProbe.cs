using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace NeonTunnelRunner
{
public static class Task12RenderProbe
{
 public static object Run()
 {
 var a=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NeonTunnelRunner/Prefabs/VFX/Firework.prefab");var go=UnityEngine.Object.Instantiate(a);go.transform.position=new Vector3(-16,10,132);var root=go.transform.Find("Rocket").GetComponent<ParticleSystem>();var results=new List<object>();root.Play(false);
 var p=new ParticleSystem.Particle[250];for(int i=0;i<130;i++){root.Simulate(.02f,true,false,false);root.Pause(true);if(i%15!=0)continue;var ps=go.GetComponentsInChildren<ParticleSystem>().Select(s=>{int n=s.GetParticles(p);var mean=Vector3.zero;for(int k=0;k<n;k++)mean+=s.transform.TransformPoint(p[k].position);if(n>0)mean/=n;return new{name=s.name,n,mean=new[]{mean.x,mean.y,mean.z},color=n>0?p[0].startColor.ToString():"none",material=s.GetComponent<ParticleSystemRenderer>().sharedMaterial.GetColor("_BaseColor").ToString()};}).ToArray();results.Add(new{age=(i+1)*.02f,ps});}
 UnityEngine.Object.DestroyImmediate(go);return results;
 }
}
}
