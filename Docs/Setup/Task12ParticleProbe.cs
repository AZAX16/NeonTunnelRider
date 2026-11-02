using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace NeonTunnelRunner
{
 public static class Task12ParticleProbe
 {
 public static object Run()
 {
 var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NeonTunnelRunner/Prefabs/VFX/Firework.prefab");var results=new List<object>();
 for(int mode=0;mode<4;mode++)
 {
 var go=UnityEngine.Object.Instantiate(asset);var ps=go.transform.Find("Rocket").GetComponent<ParticleSystem>();var child=ps.transform.GetChild(0).GetComponent<ParticleSystem>();var data=new ParticleSystem.Trails();data.capacity=512;
 ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.Play(mode==0);
 var steps=new List<object>();
 for(int i=0;i<400;i++)
 {
 ps.Simulate(.02f,mode<2,false,false);if(mode>=2)child.Simulate(.02f,false,false,false);
 if(mode!=3)ps.Pause(true);
 if(i%25==0)steps.Add(new{i,time=ps.time,root=ps.particleCount,child=child.particleCount,alive=ps.IsAlive(true),trailCount=ps.GetTrails(ref data),rootAlive=ps.IsAlive(false),childAlive=child.IsAlive(false),emitting=ps.isEmitting,sub=ps.subEmitters.subEmittersCount});
 }
 ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var cleared=new{root=ps.particleCount,child=child.particleCount,alive=ps.IsAlive(true),trailCount=ps.GetTrails(ref data)};
 results.Add(new{mode,steps,cleared});UnityEngine.Object.DestroyImmediate(go);
 }
 return results;
 }
 }
}
