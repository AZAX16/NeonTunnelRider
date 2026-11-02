using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace NeonTunnelRunner
{
 public static class Task12Audit
 {
 const string R="Assets/NeonTunnelRunner/";
 static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
 public static object Inspect()
 {
  Require(!EditorApplication.isPlaying,"Stop before final asset audit.");
  ProjectSetupValidator.ValidateProject();
  var m=UnityEngine.Object.FindAnyObjectByType<GameManager>();var l=m.Loop;var v=l.Vfx;var camera=l.Camera.ViewCamera;
  Require(v && l.Origin.HasParticipant(v),"Missing VFX/origin owner.");
  Require(UnityEngine.Object.FindObjectsByType<VfxController>(FindObjectsInactive.Exclude).Length==1 && UnityEngine.Object.FindObjectsByType<FireworkDirector>(FindObjectsInactive.Exclude).Length==1,"Duplicate VFX owners.");
  Require(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude).Length==1 && UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length==1,"Camera/listener count.");
  var volumes=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Exclude);Require(volumes.Length==1 && volumes[0].isGlobal && volumes[0].weight==1,"One global Volume.");
  var volume=volumes[0];var profile=volume.sharedProfile;var data=camera.GetUniversalAdditionalCameraData();var urp=UniversalRenderPipeline.asset;
  Require(urp && urp.supportsHDR && camera.allowHDR && data.renderPostProcessing && (data.volumeLayerMask.value&(1<<volume.gameObject.layer))!=0,"HDR/post processing/mask.");
  Require(profile.TryGet<Bloom>(out var bloom) && bloom.active && bloom.threshold.overrideState && bloom.intensity.overrideState,"Bloom missing.");
  Require(profile.TryGet<Tonemapping>(out var tone) && tone.mode.value==TonemappingMode.Neutral,"Fixed Neutral tone mapping.");Require(profile.TryGet<ColorAdjustments>(out var exposure) && exposure.postExposure.value==0,"Fixed exposure.");
  var inventory=new List<object>();var config=m.Pools.Configuration;int total=0;
  for(int i=0;i<config.EntryCount;i++)
  {
   var e=config.GetEntry(i);total+=e.Capacity;if(e.Key<PoolKey.Spark)continue;
   var prefab=e.Prefab;var ps=prefab.GetComponentsInChildren<ParticleSystem>(true);int max=ps.Sum(p=>p.main.maxParticles);
   Require(prefab.GetComponent<PooledVfx>() && prefab.GetComponentsInChildren<Collider>(true).Length==0 && prefab.GetComponentsInChildren<Light>(true).Length==0,"Cosmetic-only prefab.");
   Require(e.Capacity==((int)e.Key<10?16:8),"Pool capacity.");Require(max<=(e.Key==PoolKey.Firework?250:e.Key==PoolKey.Spark?40:24),"Particle budget.");
   foreach(var p in ps){Require(!p.main.playOnAwake && !p.main.loop && p.main.simulationSpace==ParticleSystemSimulationSpace.Local && p.main.stopAction==ParticleSystemStopAction.None,"Particle lifecycle authoring.");Require(!p.trails.enabled||!p.trails.worldSpace,"Local trails.");var renderer=p.GetComponent<ParticleSystemRenderer>();Require(renderer.sharedMaterial.shader.name=="Universal Render Pipeline/Particles/Unlit" && renderer.sharedMaterial.GetFloat("_ZWrite")==0,"Particle shader/blend.");}
   if(e.Key==PoolKey.Firework){var rocket=prefab.transform.Find("Rocket").GetComponent<ParticleSystem>();Require(rocket.trails.enabled && rocket.subEmitters.subEmittersCount==1 && rocket.subEmitters.GetSubEmitterType(0)==ParticleSystemSubEmitterType.Death,"Real rocket death burst.");}
   inventory.Add(new{key=e.Key.ToString(),e.Capacity,prefab=AssetDatabase.GetAssetPath(prefab),maxParticles=max,systems=ps.Select(p=>new{p.name,max=p.main.maxParticles,lifetime=p.main.startLifetime.constantMax,duration=p.main.duration,trail=p.trails.enabled,trailLifetime=p.trails.lifetime.constantMax,blend=p.GetComponent<ParticleSystemRenderer>().sharedMaterial.GetFloat("_Blend"),material=AssetDatabase.GetAssetPath(p.GetComponent<ParticleSystemRenderer>().sharedMaterial)}).ToArray()});
  }
  Require(total==352,"Total prewarm budget.");var exhaust=v.Exhaust;Require(exhaust.Count==4 && exhaust.Sum(p=>p.main.maxParticles)==160,"Exhaust budget.");
  foreach(var p in exhaust)Require(p && p.transform.IsChildOf(l.Visuals.VisualPivot) && !p.main.playOnAwake && p.main.loop && p.main.simulationSpace==ParticleSystemSimulationSpace.Local,"Attached exhaust.");
  Require(!SceneManager.GetActiveScene().isDirty,"Saved scene is dirty.");
  return new{passed=true,scene=SceneManager.GetActiveScene().path,totalPrewarm=total,exhaustMax=160,inventory,profile=AssetDatabase.GetAssetPath(profile),bloom=new{threshold=bloom.threshold.value,intensity=bloom.intensity.value,scatter=bloom.scatter.value,clamp=bloom.clamp.value},tone=tone.mode.value.ToString(),exposure=exposure.postExposure.value,HDR=urp.supportsHDR,post=data.renderPostProcessing,volumeMask=data.volumeLayerMask.value,originRoot=v.OriginRoot.name,sceneDirty=SceneManager.GetActiveScene().isDirty,assemblies=UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.PlayerWithoutTestAssemblies).Where(a=>a.name.StartsWith("NeonTunnelRunner")).Select(a=>new{a.name,references=a.assemblyReferences.Select(r=>r.name).ToArray()}).ToArray()};
 }
 }
}
