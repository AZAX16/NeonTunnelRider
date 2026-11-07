using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>Repeatable course release. Never substitutes another platform.</summary>
    public static class BuildDelivery
    {
        public const string GameScene = "Assets/NeonTunnelRunner/Scenes/Game.unity";
        public const string Output = "Build/Windows/NeonTunnelRunner.exe";
        [Serializable] public sealed class FileRecord { public string path, sha256; public long bytes; }
        [Serializable] public sealed class Message { public string step, type, content; }
        [Serializable] public sealed class Metadata
        {
            public string unity, target, backend, options, result, sourceCommit, output, startedUtc, finishedUtc;
            public bool development, connectProfiler, deepProfiling, scriptDebugging;
            public int errors, warnings;
            public ulong reportedBytes;
            public double seconds;
            public string[] scenes;
            public FileRecord[] files;
            public Message[] messages;
        }

        [MenuItem("Neon Tunnel Runner/Delivery/Build Windows Release")]
        public static void BuildWindowsRelease()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new BuildFailedException("Stop Play Mode and wait for import/compilation before delivery.");
            if (Application.unityVersion != "6000.6.3f1")
                throw new BuildFailedException("Delivery requires the pinned Unity 6000.6.3f1.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new BuildFailedException("Windows Build Support for StandaloneWindows64 is unavailable.");
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length != 1 || scenes[0] != GameScene || !File.Exists(GameScene))
                throw new BuildFailedException("The sole enabled entry scene must be saved Game.unity.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new BuildFailedException("Active scene has unsaved changes. Save/review explicitly before building.");
            ProjectSetupValidator.ValidateProject();
            string directory = Path.GetDirectoryName(Output);
            // A second build must not silently package stale companions. Retain an earlier
            // build by moving the whole directory, never deleting individual emitted files.
            if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
                throw new BuildFailedException("Build/Windows must be empty. Move the previous output aside before rebuilding.");
            Directory.CreateDirectory(directory);
            string evidence = "Evidence/Task16/Build-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ");
            Directory.CreateDirectory(evidence);
            var data = new Metadata {
                unity = Application.unityVersion, target = BuildTarget.StandaloneWindows64.ToString(),
                backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone).ToString(),
                options = BuildOptions.DetailedBuildReport.ToString(), scenes = scenes, output = Output,
                sourceCommit = GitCommit(), startedUtc = DateTime.UtcNow.ToString("o"), result = "NotStarted"
            };
            // URP's build processor serializes generated runtime/stripping lists. Preserve
            // the owner's current authoring bytes, as the existing Task14/15 builder does.
            string[] preserved = {
                "Assets/NeonTunnelRunner/Materials/ReferenceRoad.mat",
                "Assets/NeonTunnelRunner/Settings/URP/PC_RPAsset.asset",
                "Assets/NeonTunnelRunner/Settings/URP/UniversalRenderPipelineGlobalSettings.asset",
                "Assets/Settings/DefaultVolumeProfile.asset"
            };
            var originals = preserved.ToDictionary(p => p, File.ReadAllBytes);
            bool dev = EditorUserBuildSettings.development, profiler = EditorUserBuildSettings.connectProfiler;
            bool debug = EditorUserBuildSettings.allowDebugging, deep = EditorUserBuildSettings.buildWithDeepProfilingSupport;
            try
            {
                EditorUserBuildSettings.development = false;
                EditorUserBuildSettings.connectProfiler = false;
                EditorUserBuildSettings.allowDebugging = false;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = scenes, target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone, subtarget = (int)StandaloneBuildSubtarget.Player,
                    locationPathName = Output, options = BuildOptions.DetailedBuildReport
                });
                if (!report) throw new BuildFailedException("Unity returned no BuildReport.");
                var summary = report.summary;
                data.result = summary.result.ToString(); data.errors = summary.totalErrors; data.warnings = summary.totalWarnings;
                data.reportedBytes = summary.totalSize; data.seconds = summary.totalTime.TotalSeconds;
                data.development = (summary.options & BuildOptions.Development) != 0;
                data.connectProfiler = (summary.options & BuildOptions.ConnectWithProfiler) != 0;
                data.scriptDebugging = (summary.options & BuildOptions.AllowDebugging) != 0;
                data.deepProfiling = (summary.options & BuildOptions.EnableDeepProfilingSupport) != 0;
                data.messages = report.steps.SelectMany(step => step.messages.Select(m => new Message {
                    step = step.name, type = m.type.ToString(), content = m.content
                })).ToArray();
                if (File.Exists("Library/LastBuild.buildreport"))
                    File.Copy("Library/LastBuild.buildreport", evidence + "/LastBuild.buildreport");
                data.files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).OrderBy(p => p).Select(p => new FileRecord {
                    path = p.Replace('\\', '/'), bytes = new FileInfo(p).Length, sha256 = Hash(p)
                }).ToArray();
                if (summary.result != BuildResult.Succeeded || summary.totalErrors != 0)
                    throw new BuildFailedException("Windows release failed: " + summary.result + ", errors=" + summary.totalErrors);
                if (data.development || data.connectProfiler || data.deepProfiling || data.scriptDebugging)
                    throw new BuildFailedException("Unity emitted development/profiler/debug options in the release.");
                if (!File.Exists(Output) || !Directory.Exists(directory + "/NeonTunnelRunner_Data") ||
                    !File.Exists(directory + "/UnityPlayer.dll"))
                    throw new BuildFailedException("Unity output is missing a required Windows runtime companion.");
            }
            catch (Exception exception)
            {
                if (data.result == "NotStarted") data.result = "Exception: " + exception.Message;
                throw;
            }
            finally
            {
                data.finishedUtc = DateTime.UtcNow.ToString("o");
                File.WriteAllText(evidence + "/build-metadata.json", JsonUtility.ToJson(data, true) + "\n");
                foreach (var pair in originals)
                    if (!File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)) {
                        File.WriteAllBytes(pair.Key, pair.Value);
                        AssetDatabase.ImportAsset(pair.Key, ImportAssetOptions.ForceUpdate);
                    }
                EditorUserBuildSettings.development = dev; EditorUserBuildSettings.connectProfiler = profiler;
                EditorUserBuildSettings.allowDebugging = debug; EditorUserBuildSettings.buildWithDeepProfilingSupport = deep;
            }
            Debug.Log("Windows release " + data.result + "; evidence: " + evidence);
        }

        static string Hash(string path)
        {
            using var sha = SHA256.Create(); using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        static string GitCommit()
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", "rev-parse HEAD") {
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
            });
            string value = process.StandardOutput.ReadToEnd().Trim(); process.WaitForExit();
            if (process.ExitCode != 0 || value.Length != 40) throw new BuildFailedException("Cannot identify release source commit.");
            return value;
        }
    }
}
