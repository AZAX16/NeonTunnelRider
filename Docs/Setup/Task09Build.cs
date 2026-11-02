using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
namespace NeonTunnelRunner
{
    public static class Task09Build
    {
        public static object Run()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop play first.");
            // Restore build-generated URP stripping/runtime lists and schema serialization.
            // Snapshot current bytes so this helper also preserves later user tuning.
            var paths=new[]{"Assets/NeonTunnelRunner/Settings/URP/PC_RPAsset.asset",
                "Assets/NeonTunnelRunner/Settings/URP/UniversalRenderPipelineGlobalSettings.asset",
                "Assets/Settings/DefaultVolumeProfile.asset"};
            var originals=paths.ToDictionary(p=>p,p=>File.ReadAllBytes(p));
            try
            {
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/NeonTunnelRunner/Scenes/Game.unity"},
                target=BuildTarget.StandaloneLinux64,
                locationPathName="Builds/Task09Persistence/NeonTunnelRunner.x86_64",
                options=BuildOptions.Development | BuildOptions.DetailedBuildReport,
                extraScriptingDefines=new[]{"NTR_PERSISTENCE_VERIFY"}
            });
            var s=report.summary;
            return new {result=s.result.ToString(),s.totalErrors,s.totalWarnings,s.totalSize,seconds=s.totalTime.TotalSeconds,
                s.outputPath,steps=report.steps.Select(x=>new {x.name,duration=x.duration.TotalSeconds}).ToArray()};
            }
            finally
            {
                foreach(var pair in originals)
                {
                    File.WriteAllBytes(pair.Key,pair.Value);
                    AssetDatabase.ImportAsset(pair.Key,ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }
}
