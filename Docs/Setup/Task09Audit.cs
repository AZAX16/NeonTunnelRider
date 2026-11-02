using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonTunnelRunner
{
    public static class Task09Audit
    {
        public static object Inspect()
        {
            ProjectSetupValidator.ValidateProject();
            var manager=Object.FindAnyObjectByType<GameManager>(); var loop=manager.Loop; var visuals=loop.Visuals;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NeonTunnelRunner/Prefabs/Player/PlayerHovercar.prefab");
            return new {
                editor=Application.unityVersion,playing=Application.isPlaying,scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,
                instanceClear=GameManager.Instance==null,timeScale=Time.timeScale,autoTick=SessionState.GetBool("Unity.Pipeline.AutoTick.Enabled",true),
                ownerWired=visuals && visuals.Owner==manager && loop.Survival.Owner==manager && loop.Sensor.Survival==loop.Survival,
                flashRenderer=visuals.FlashRenderer.name,shader=visuals.FlashRenderer.sharedMaterial.shader.name,
                prefabFlashRenderer=prefab.GetComponent<VehicleVisuals>().FlashRenderer.name,
                defaults=new {manager.Configuration.StartingLives,manager.Configuration.FuelCapacity,manager.Configuration.FuelDrainPerSecond,
                    manager.Configuration.FuelRefill,manager.Configuration.DamageGraceDuration,manager.Configuration.StartingSpeed, manager.Configuration.MaximumSpeed, manager.Configuration.DifficultyDuration, manager.Configuration.DifficultyDistance, manager.Configuration.StartingOccupancy, manager.Configuration.MaximumOccupancy},
                packages=UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Where(p=>new[] {
                    "com.unity.pipeline","com.unity.render-pipelines.universal","com.unity.inputsystem","com.unity.test-framework"
                    }.Contains(p.name)).Select(p=>new {p.name,p.version}).ToArray(),
                playerAssemblies=CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies).Where(a=>a.name.StartsWith("NeonTunnelRunner")).Select(a=>new {
                    a.name,references=a.assemblyReferences.Select(r=>r.name).ToArray() }).ToArray()
            };
        }
    }
}
