using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonTunnelRunner
{
    public static class Task07Audit
    {
        public static object Inspect()
        {
            ProjectSetupValidator.ValidateProject();
            var manager=Object.FindAnyObjectByType<GameManager>();
            return new {
                editor=Application.unityVersion, playing=Application.isPlaying,
                scene=SceneManager.GetActiveScene().path, dirty=SceneManager.GetActiveScene().isDirty,
                instanceClear=GameManager.Instance==null, timeScale=Time.timeScale,
                ownerWired=manager.Loop.Sensor.Player==manager.Loop.Player && manager.Loop.Survival.Owner==manager && manager.Loop.Progression.Owner==manager && manager.Loop.Sensor.Progression==manager.Loop.Progression,
                originRegistered=manager.Loop.Origin.HasParticipant(manager.Loop.Planner),
                entries=Enumerable.Range(0,manager.Pools.Configuration.EntryCount).Select(i=> {
                    var e=manager.Pools.Configuration.GetEntry(i); return new { key=e.Key.ToString(),e.Capacity,
                        prefab=AssetDatabase.GetAssetPath(e.Prefab),guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e.Prefab)) }; }).ToArray(),
                packages=UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Where(p=>new[] {
                    "com.unity.pipeline","com.unity.render-pipelines.universal","com.unity.inputsystem","com.unity.test-framework"
                    }.Contains(p.name)).Select(p=>new {p.name,p.version}).ToArray(),
                autoTick=SessionState.GetBool("Unity.Pipeline.AutoTick.Enabled",true),
                playerAssemblies=CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies).Where(a=>a.name.StartsWith("NeonTunnelRunner")).Select(a=>new {
                    a.name,references=a.assemblyReferences.Select(r=>r.name).ToArray() }).ToArray()
            };
        }
    }
}
