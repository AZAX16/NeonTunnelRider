using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NeonTunnelRunner
{
    /// <summary>Explicit read-only native companion to Docs/Setup/Task15References.py.</summary>
    public static class Task15NativePointers
    {
        [Serializable] public class Pointer
        {
            public string file, property, guid, resolution, target;
            public long ownerFileID, fileID;
            public int ownerClassID, line, type;
            public bool tracked, protectedRecovery, conservativelyReachable;
        }
        [Serializable] public class HashEntry { public string path, sha256; }
        [Serializable] public class Inventory
        {
            public string projectRoot, sourceRevision, result;
            public Pointer[] references;
            public HashEntry[] inputHashes;
        }
        [Serializable] public class Check
        {
            public Pointer pointer;
            public string result = "UNVERIFIED", reason, nativeProperty, expectedType, actualType, nativePath, diagnostic;
            public string resolvedGuid;
            public long resolvedFileID;
            public string ownerType, method, sourceProperty, sourceManagedType;
            public long sourceManagedReferenceID;
            public string[] ownerCandidates, candidateProperties;
        }
        [Serializable] public class Report
        {
            public string startedUtc, finishedUtc, projectRoot, sourceRevision, editorVersion;
            public string inventoryPath, inventorySha256, helperSha256, result = "BLOCKED", error;
            public int originalPending = 292, pending, passed, failed, unverified;
            public int processed, executed;
            public bool validationFinished;
            public string phase = "starting", exportError;
            public string[] added, removed, changedInputs;
            public string helperVersion = "5-expanded-scope-stable-occurrence-coverage";
            public string[] ownerDiscovery;
            public Check[] checks;
            public string limitation = "Only native pointer acceptance; no Unity tests, render/audio or performance claim. UNVERIFIED requires a supported native/type-specific follow-up, never an allowlist.";
        }

        const string Baseline = "Evidence/Task15Recovery/references-after.json";
        const string Helper = "Assets/NeonTunnelRunner/Scripts/Editor/Task15NativePointers.cs";
        const string GameScene = "Assets/NeonTunnelRunner/Scenes/Game.unity";

        [MenuItem("Neon Tunnel Runner/Task 15/Validate Native Pointers")]
        public static void RunMenu()
        {
            try
            {
                string root = ProjectRoot();
                string directory = ProjectFile(root, "Evidence/Task15Recovery");
                string path = EditorUtility.OpenFilePanel("Select fresh Task15References.py inventory",
                    Directory.Exists(directory) ? directory : root, "json");
                if (string.IsNullOrEmpty(path))
                {
                    Debug.Log("[Task15NativePointers] Cancelled file selection; no validation was executed.");
                    return;
                }
                string output = RunCore(path, out Report report);
                if (string.IsNullOrEmpty(output))
                {
                    EditorUtility.DisplayDialog("Native pointer export failed",
                        "No report could be saved. Check the Console for the exact path and error.\n\n" +
                        (report.exportError ?? report.error ?? "Unknown export error"), "OK");
                    return;
                }
                string summary = $"{report.result}: {report.passed} PASS, {report.failed} FAIL, " +
                    $"{report.unverified} UNVERIFIED / {report.pending}\n" +
                    $"Native checks attempted: {report.executed}; phase: {report.phase}\n\n" +
                    Path.Combine(output, "native-pointers.json");
                if (!string.IsNullOrEmpty(report.error)) summary += "\n\n" + report.error;
                if (EditorUtility.DisplayDialog("Native pointer report exported", summary, "Reveal report", "Close"))
                    EditorUtility.RevealInFinder(Path.Combine(output, "native-pointers.json"));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Native pointer menu failed", e.Message + "\n\nSee the Console for details.", "OK");
            }
        }

        // Also callable through the existing Pipeline script eval surface once reachable.
        public static string Run(string inventoryPath)
        {
            return RunCore(inventoryPath, out _);
        }

        static string RunCore(string inventoryPath, out Report report)
        {
            report = new Report { startedUtc = DateTime.UtcNow.ToString("o"), editorVersion = Application.unityVersion };
            string output = null;
            bool exported = false;
            try
            {
                report.projectRoot = ProjectRoot();
                output = Path.Combine(report.projectRoot, "Evidence/Task15Recovery/Native",
                    DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ"));
                Directory.CreateDirectory(output);
                report.inventoryPath = ProjectFile(report.projectRoot, inventoryPath);
                // Export a BLOCKED checkpoint before any hashes, preflight or native asset loading.
                SaveReport(output, report);
                Debug.Log("[Task15NativePointers] Started; partial BLOCKED report: " +
                    Path.Combine(output, "native-pointers.json"));
                inventoryPath = report.inventoryPath;
                report.helperSha256 = Hash(ProjectFile(report.projectRoot, Helper));
                report.phase = "reading inventory";
                report.inventorySha256 = Hash(inventoryPath);
                File.Copy(inventoryPath, Path.Combine(output, "inventory.json"));
                var inventory = JsonUtility.FromJson<Inventory>(File.ReadAllText(inventoryPath));
                Require(inventory != null && inventory.references != null, "Inventory has no references array; select references.json from Task15References.py.");
                Require(!inventory.references.Any(p => p == null), "Inventory contains a null reference entry.");
                var pending = inventory.references.Where(Pending).ToArray();
                report.pending = pending.Length;
                report.checks = pending.Select(p => new Check { pointer = p, reason = "Preflight did not complete" }).ToArray();
                report.phase = "preflight";
                SaveReport(output, report);
                string baselinePath = ProjectFile(report.projectRoot, Baseline);
                Require(File.Exists(baselinePath), "Original baseline file is missing: " + baselinePath);
                var baseline = JsonUtility.FromJson<Inventory>(File.ReadAllText(baselinePath));
                Require(baseline != null && baseline.references != null && !baseline.references.Any(p => p == null), "Original baseline has no valid references array.");
                var original = baseline.references.Where(Pending).ToArray();
                report.originalPending = original.Length;
                report.added = OccurrenceKeys(pending).Except(OccurrenceKeys(original)).ToArray();
                report.removed = OccurrenceKeys(original).Except(OccurrenceKeys(pending)).ToArray();
                Require(original.Length == 292, "Original 292-check baseline differs; reconcile before running.");
                Require(pending.Length > 0, "Empty inventory is not a pass.");
                Require(Application.unityVersion == "6000.6.3f1", "Use pinned Editor 6000.6.3f1.");
                Require(!EditorApplication.isCompiling && !EditorApplication.isUpdating &&
                    !EditorApplication.isPlayingOrWillChangePlaymode, "Wait for import/compile; stop Play Mode.");
                Require(SceneManager.GetActiveScene().path == GameScene, "Open saved " + GameScene + " first.");
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    Require(!SceneManager.GetSceneAt(i).isDirty, "A scene is unsaved; preserve it before validation.");
                Require(!string.IsNullOrEmpty(inventory.projectRoot) && SamePath(inventory.projectRoot, report.projectRoot),
                    "Inventory is from another project or predates native provenance support; regenerate it.");
                report.sourceRevision = GitRevision(report.projectRoot);
                Require(report.sourceRevision == inventory.sourceRevision, "Revision changed; regenerate inventory.");
                Require(inventory.inputHashes != null && inventory.inputHashes.Length > 0, "Missing input hashes.");
                report.changedInputs = ChangedInputs(inventory, report.projectRoot);
                Require(report.changedInputs.Length == 0, "Inputs changed; regenerate inventory.");
                Require(inventory.result != "FAIL", "Static inventory has failures; inspect them before native validation.");
                report.phase = "validating";
                SaveReport(output, report);
                var discovery = new List<string>();
                // Each preview scene is separate from the user's open scenes. Never save it.
                foreach (var group in report.checks.GroupBy(c => c.pointer.file))
                {
                    Scene preview = default;
                    try
                    {
                        CheckCancellation(report, group.Key);
                        if (group.Key.EndsWith(".unity", StringComparison.Ordinal))
                        {
                            preview = EditorSceneManager.OpenPreviewScene(group.Key);
                            discovery.Add(group.Key + ": preview components; scene singleton roles use explicit active-scene context");
                            report.ownerDiscovery = discovery.ToArray();
                        }
                        var owners = Owners(group.Key, preview).Where(o => o).Distinct().ToArray();
                        var source = File.ReadAllLines(ProjectFile(report.projectRoot, group.Key));
                        foreach (var check in group)
                        {
                            CheckCancellation(report, group.Key);
                            report.executed++;
                            try { Validate(check, owners, source); }
                            catch (Exception e) { check.reason = e.GetType().Name + ": " + e.Message; }
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception e) { foreach (var check in group) check.reason = "Owner load: " + e.Message; }
                    finally { if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview); }
                    report.processed += group.Count();
                    SaveReport(output, report);
                }
                report.phase = "final provenance check";
                report.changedInputs = ChangedInputs(inventory, report.projectRoot);
                Require(report.changedInputs.Length == 0, "Inputs changed during validation; results cannot close gate.");
                Require(GitRevision(report.projectRoot) == report.sourceRevision, "Revision changed during validation.");
                report.passed = report.checks.Count(c => c.result == "PASS");
                report.failed = report.checks.Count(c => c.result == "FAIL");
                report.unverified = report.checks.Count(c => c.result == "UNVERIFIED");
                // Task15B authorizes new assets. Every added occurrence still receives the
                // same native/type validation above. Preserve full original coverage and
                // export the added/removed lists; do not force the expanded scope to 292.
                report.result = report.failed > 0 ? "FAIL" : report.unverified > 0 ||
                    report.removed.Length > 0 ? "BLOCKED" : "PASS";
                report.validationFinished = true;
                report.phase = "finished";
            }
            catch (Exception e)
            {
                report.error = "Phase " + report.phase + ": " + e;
                report.phase = e is OperationCanceledException ? "cancelled" : "aborted";
                report.result = report.checks?.Any(c => c.result == "FAIL") == true ? "FAIL" : "BLOCKED";
            }
            finally
            {
                try { EditorUtility.ClearProgressBar(); }
                catch (Exception e) { Debug.LogWarning("[Task15NativePointers] Progress UI cleanup: " + e.Message); }
                report.finishedUtc = DateTime.UtcNow.ToString("o");
                try
                {
                    Require(!string.IsNullOrEmpty(output), "Could not establish a project export directory.");
                    Directory.CreateDirectory(output);
                    SaveReport(output, report);
                    exported = true;
                }
                catch (Exception e)
                {
                    report.exportError = e.ToString();
                    Debug.LogError("[Task15NativePointers] Could not export native-pointers.json to " +
                        (output ?? "<unavailable>") + ": " + e);
                }
            }
            if (exported)
            {
                string summary = $"[Task15NativePointers] {report.result}: {report.passed} PASS, {report.failed} FAIL, " +
                    $"{report.unverified} UNVERIFIED / {report.pending}; native checks attempted: {report.executed}; report: {output}";
                if (!string.IsNullOrEmpty(report.error)) Debug.LogWarning(summary + "\n" + report.error);
                else Debug.Log(summary);
            }
            return exported ? output : string.Empty;
        }

        static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static string ProjectFile(string root, string path)
        {
            Require(!string.IsNullOrWhiteSpace(path), "A required file path is empty.");
            return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(root, path));
        }
        static bool SamePath(string left, string right) =>
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ==
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        static void SaveReport(string output, Report report)
        {
            report.passed = report.checks?.Count(c => c.result == "PASS") ?? 0;
            report.failed = report.checks?.Count(c => c.result == "FAIL") ?? 0;
            report.unverified = report.checks?.Count(c => c.result == "UNVERIFIED") ?? 0;
            File.WriteAllText(Path.Combine(output, "native-pointers.json"), JsonUtility.ToJson(report, true) + "\n");
        }
        static void CheckCancellation(Report report, string owner)
        {
            if (EditorUtility.DisplayCancelableProgressBar("Task 15 native pointers",
                $"{report.executed}/{report.pending} native checks attempted: {owner}",
                report.pending > 0 ? (float)report.executed / report.pending : 0f))
                throw new OperationCanceledException("Cancelled by operator; partial evidence remains BLOCKED/FAIL.");
        }

        static void Validate(Check check, Object[] owners, string[] source)
        {
            var p = check.pointer;
            check.nativePath = AssetDatabase.GUIDToAssetPath(p.guid);
            if (string.IsNullOrEmpty(check.nativePath) && p.resolution == "BUILTIN_ID_PENDING")
                check.nativePath = p.target; // Exact container path from scanner; never a PASS by itself.
            if (!string.IsNullOrEmpty(check.nativePath))
            {
                try { check.actualType = AssetDatabase.GetTypeFromPathAndFileID(check.nativePath, p.fileID)?.AssemblyQualifiedName; }
                catch (Exception e) { check.diagnostic = "Optional type lookup: " + e.GetType().Name + ": " + e.Message; }
            }
            if (TryImporterScript(check, owners)) return;
            if (TrySceneSettings(check)) return;
            if (TryPackageSettings(check)) return;
            // Find the exact owning object, never an object with a matching name alone.
            var matchingOwners = owners.Where(o => LocalID(o) == p.ownerFileID).ToArray();
            if (matchingOwners.Length != 1)
            {
                check.ownerCandidates = owners.Take(32).Select(DescribeOwner).ToArray();
                check.reason = "Exact owner fileID unavailable/ambiguous through supported APIs";
                return;
            }
            var owner = matchingOwners[0];
            check.ownerType = owner.GetType().AssemblyQualifiedName;
            using var serialized = new SerializedObject(owner);
            if (TryFontSchema(check, owner, serialized)) return;
            if (TryManagedLocation(p, source, out ManagedLocation location, out string locationError))
            {
                if (!string.IsNullOrEmpty(locationError)) { check.reason = locationError; return; }
                ValidateManagedProperty(check, serialized, location);
                return;
            }
            var matches = new List<SerializedProperty>();
            var fields = new List<SerializedProperty>();
            var it = serialized.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                bool name = p.property == "_list_item" ? it.propertyPath.EndsWith("]", StringComparison.Ordinal) : it.name == p.property;
                if (!name) continue;
                fields.Add(it.Copy());
                if (Identity(it.objectReferenceValue, out string guid, out long id) && guid == p.guid && id == p.fileID)
                    matches.Add(it.Copy());
            }
            check.candidateProperties = fields.Take(64).Select(DescribeProperty).ToArray();
            if (matches.Count != 1)
            {
                // A single typed property returning null is a demonstrated failed native binding.
                // Unexposed/deprecated/list/ambiguous data needs type-specific follow-up instead.
                if (fields.Count == 1)
                {
                    check.expectedType = fields[0].type;
                    check.nativeProperty = fields[0].propertyPath;
                    if (fields[0].objectReferenceValue)
                    {
                        check.actualType = fields[0].objectReferenceValue.GetType().AssemblyQualifiedName;
                        Identity(fields[0].objectReferenceValue, out check.resolvedGuid, out check.resolvedFileID);
                    }
                }
                check.result = matches.Count == 0 && fields.Count == 1 && Typed(fields[0]) &&
                    p.property != "_list_item" ? "FAIL" : "UNVERIFIED";
                check.reason = $"Expected exact GUID/fileID at owner/property; native matches={matches.Count}, candidate fields={fields.Count}";
                if (fields.Count == 0)
                {
                    var exposed = new List<string>();
                    var probe = serialized.GetIterator();
                    while (probe.Next(true))
                        if (probe.name.IndexOf("font", StringComparison.OrdinalIgnoreCase) >= 0)
                            exposed.Add(probe.propertyPath + " : " + probe.type);
                    check.candidateProperties = exposed.Take(64).ToArray();
                }
                return;
            }
            var property = matches[0];
            if (owner is AudioSource audio && p.ownerClassID == 82 &&
                property.propertyPath == "m_Resource" && property.objectReferenceValue is AudioClip)
            {
                // m_Resource is PPtr<Object> in this Editor; the public clip getter supplies its semantics.
                RecordValue(check, property);
                check.method = "AudioSource.clip plus serialized m_Resource";
                check.expectedType = typeof(AudioClip).AssemblyQualifiedName;
                var clip = audio.clip;
                check.result = clip && clip == property.objectReferenceValue &&
                    Identity(clip, out string guid, out long id) && guid == p.guid && id == p.fileID ? "PASS" : "FAIL";
                check.reason = check.result == "PASS" ?
                    "Exact owning AudioSource returns the same GUID/fileID through its typed AudioClip getter" :
                    "Serialized AudioClip and the owning AudioSource.clip binding disagree";
                return;
            }
            ValidateProperty(check, property, "typed serialized property");
        }

        static void RecordValue(Check check, SerializedProperty property)
        {
            check.nativeProperty = property.propertyPath;
            check.expectedType = property.type;
            var value = property.objectReferenceValue;
            check.actualType = value ? value.GetType().AssemblyQualifiedName : null;
            check.nativePath = value ? AssetDatabase.GetAssetPath(value) : null;
            Identity(value, out check.resolvedGuid, out check.resolvedFileID);
        }

        const BindingFlags NativeFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static bool TrySceneSettings(Check check)
        {
            var p = check.pointer;
            if (!p.file.EndsWith(".unity", StringComparison.Ordinal) ||
                (p.ownerClassID != 104 && p.ownerClassID != 157)) return false;
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(p.file);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            try
            {
                if (opened) scene = EditorSceneManager.OpenScene(p.file, OpenSceneMode.Additive);
                Require((SceneManager.GetActiveScene() == scene || SceneManager.SetActiveScene(scene)) &&
                    SceneManager.GetActiveScene().path == p.file, "Cannot establish exact scene context.");
                Type type = p.ownerClassID == 104 ? typeof(RenderSettings) : typeof(LightmapEditorSettings);
                string getter = p.ownerClassID == 104 ? "GetRenderSettings" : "GetLightmapSettings";
                var owner = (Object)type.GetMethod(getter, NativeFlags).Invoke(null, null);
                Require(p.ownerClassID == 104 ? owner is RenderSettings : owner is LightmapSettings,
                    "Native scene-settings role returned the wrong type.");
                check.ownerType = owner.GetType().AssemblyQualifiedName;
                check.ownerCandidates = new[] { "Explicit active scene: " + scene.path + "; handle=" + scene.handle +
                    "; source role class=" + p.ownerClassID + "; source fileID=" + p.ownerFileID };
                using var serialized = new SerializedObject(owner);
                ValidateProperty(check, serialized.FindProperty(p.property),
                    "explicit active-scene handle/path, native singleton role getter and typed field; no localID0 match");
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
            return true;
        }

        static bool TryPackageSettings(Check check)
        {
            var p = check.pointer;
            if (p.file != "ProjectSettings/PackageManagerSettings.asset" || p.property != "m_Script") return false;
            var type = typeof(Editor).Assembly.GetType("UnityEditor.PackageManager.UI.Internal.PackageManagerProjectSettings", true);
            var attribute = type.GetCustomAttributesData().Single(a => a.AttributeType.Name == "FilePathAttribute");
            Require(attribute.ConstructorArguments.Any(a => Equals(a.Value, p.file)) &&
                type.BaseType.GetGenericArguments().Single() == type, "Package singleton filepath/type mismatch.");
            var owner = (Object)type.BaseType.GetProperty("instance", NativeFlags).GetValue(null);
            check.ownerType = type.AssemblyQualifiedName;
            check.ownerCandidates = new[] { attribute.ToString() };
            using var serialized = new SerializedObject(owner);
            ValidateProperty(check, serialized.FindProperty("m_Script"), "actual package ScriptableSingleton, exact FilePath and native MonoScript");
            if (check.result == "PASS" && (serialized.FindProperty("m_Script").objectReferenceValue as MonoScript)?.GetClass() != type)
            { check.result = "FAIL"; check.reason = "Exact script identity has the wrong native class."; }
            return true;
        }

        static bool TryFontSchema(Check check, Object owner, SerializedObject serialized)
        {
            var p = check.pointer;
            if (!(owner is TMPro.TMP_FontAsset) || p.property != "m_SourceFontFile_EditorRef") return false;
            var type = owner.GetType();
            var field = type.GetField(p.property, NativeFlags);
            var guid = serialized.FindProperty("m_SourceFontFileGUID");
            var getter = type.GetProperty("SourceFont_EditorRef", NativeFlags);
            bool obsolete = serialized.FindProperty(p.property) == null && field != null && !field.IsPublic &&
                !field.IsDefined(typeof(SerializeField), true);
            check.method = "schema disposition: obsolete unserialized cache; exact owner's serialized source GUID and real Font getter";
            check.sourceProperty = "m_SourceFontFileGUID";
            check.nativeProperty = getter?.Name;
            check.expectedType = typeof(Font).AssemblyQualifiedName;
            var value = getter?.GetValue(owner) as Font;
            check.actualType = value ? value.GetType().AssemblyQualifiedName : null;
            Identity(value, out check.resolvedGuid, out check.resolvedFileID);
            check.result = obsolete && guid?.stringValue == p.guid && check.resolvedGuid == p.guid &&
                check.resolvedFileID == p.fileID ? "PASS" : "UNVERIFIED";
            check.reason = check.result == "PASS" ?
                "Original saved PPtr retained; current schema/getter verifies the exact Font. This is a schema disposition, not a live old-field binding." :
                "Current font schema/getter does not prove the original source identity.";
            return true;
        }

        static void ValidateProperty(Check check, SerializedProperty property, string method)
        {
            check.method = method;
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            { check.reason = "Exact source property is not exposed as a native object reference"; return; }
            RecordValue(check, property);
            if (!Typed(property))
            { check.reason = "Generic Object property; expected semantics require a type-specific native check"; return; }
            check.result = Identity(property.objectReferenceValue, out string guid, out long id) &&
                guid == check.pointer.guid && id == check.pointer.fileID ? "PASS" : "FAIL";
            check.reason = check.result == "PASS" ?
                "Native typed owning property resolves exact GUID and local fileID; no name or reserved-GUID exemption" :
                "Exact typed owning property is null or resolves a different GUID/fileID";
        }

        static bool TryImporterScript(Check check, Object[] owners)
        {
            var p = check.pointer;
            if (!p.file.EndsWith(".meta", StringComparison.Ordinal) || p.ownerFileID != 0 ||
                p.ownerClassID != 0 || p.property != "script") return false;
            // In scanner metadata rows, zero is a synthetic importer role, not a Unity object fileID.
            string assetPath = p.file.Substring(0, p.file.Length - 5);
            var importers = owners.OfType<AssetImporter>().Where(o => o.assetPath == assetPath).ToArray();
            check.method = "exact asset-path importer script binding";
            if (importers.Length != 1)
            { check.reason = "Exact metadata importer is unavailable/ambiguous"; return true; }
            var importer = importers[0];
            check.ownerType = importer.GetType().AssemblyQualifiedName;
            using var serialized = new SerializedObject(importer);
            var fields = new[] { serialized.FindProperty("script"), serialized.FindProperty("m_Script") }
                .Where(f => f != null && f.propertyType == SerializedPropertyType.ObjectReference).ToArray();
            check.candidateProperties = fields.Select(DescribeProperty).ToArray();
            if (fields.Length != 1)
            { check.reason = "Importer exposes no unique native script property; no importer/type allowlist used"; return true; }
            RecordValue(check, fields[0]);
            check.expectedType = typeof(MonoScript).AssemblyQualifiedName;
            var script = fields[0].objectReferenceValue as MonoScript;
            if (!script || !Identity(script, out string guid, out long id) || guid != p.guid || id != p.fileID)
            { check.result = "FAIL"; check.reason = "Exact importer's script property does not resolve the recorded MonoScript GUID/fileID"; return true; }
            if (script.GetClass() != importer.GetType())
            { check.reason = "MonoScript resolves, but its native class does not establish the exact importer type"; return true; }
            check.result = "PASS";
            check.reason = "Exact asset-path importer binds the recorded MonoScript GUID/fileID and that script's class equals the native importer type";
            return true;
        }

        sealed class ManagedLocation
        {
            public long id;
            public string typeName, relativePath;
        }

        // Deliberately limited to Unity's version-2 managed-reference YAML: direct fields and
        // contiguous inline-PPtr arrays. Unsupported layouts stay UNVERIFIED, never match by name.
        static bool TryManagedLocation(Pointer p, string[] source, out ManagedLocation location, out string error)
        {
            location = null; error = null;
            int line = p.line - 1;
            if (line < 0 || line >= source.Length) return false;
            int document = line;
            while (document >= 0 && !source[document].StartsWith("--- !u!", StringComparison.Ordinal)) document--;
            if (document < 0) return false;
            int references = -1;
            for (int i = document + 1; i < line; i++)
                if (source[i] == "  references:") references = i;
            if (references < 0) return false;
            error = "Managed-reference source layout could not be mapped exactly; no broad field matching used";
            var header = Regex.Match(source[document], @"^--- !u!(\d+) &(-?\d+)$");
            if (!header.Success || !long.TryParse(header.Groups[2].Value, out long owner) ||
                owner != p.ownerFileID || header.Groups[1].Value != p.ownerClassID.ToString()) return true;
            if (references + 2 >= source.Length || source[references + 1] != "    version: 2" ||
                source[references + 2] != "    RefIds:") return true;
            int ridLine = line;
            while (ridLine > references && !source[ridLine].StartsWith("    - rid: ", StringComparison.Ordinal)) ridLine--;
            if (ridLine <= references || ridLine + 3 > line) return true;
            if (!long.TryParse(source[ridLine].Substring("    - rid: ".Length), out long rid)) return true;
            var type = Regex.Match(source[ridLine + 1],
                @"^      type: \{class: ([A-Za-z_][A-Za-z0-9_/]*), ns: ([A-Za-z0-9_.]*), asm: ([A-Za-z0-9_.-]+)\}$");
            if (!type.Success || source[ridLine + 2] != "      data:") return true;
            string relative;
            if (p.property == "_list_item")
            {
                int first = line;
                while (first > ridLine + 3 && source[first - 1].StartsWith("        - {", StringComparison.Ordinal)) first--;
                var field = Regex.Match(source[first - 1], @"^        ([A-Za-z_][A-Za-z0-9_]*):\s*$");
                if (!field.Success) return true;
                for (int i = first; i <= line; i++)
                    if (!InlinePointer(source[i], out long _, out string _, out int _) ||
                        !source[i].StartsWith("        - {", StringComparison.Ordinal)) return true;
                relative = field.Groups[1].Value + ".Array.data[" + (line - first) + "]";
            }
            else
            {
                if (!Regex.IsMatch(p.property, @"^[A-Za-z_][A-Za-z0-9_]*$") ||
                    !source[line].StartsWith("        " + p.property + ": {", StringComparison.Ordinal)) return true;
                relative = p.property;
            }
            if (!InlinePointer(source[line], out long id, out string guid, out int pointerType) ||
                id != p.fileID || guid != p.guid || pointerType != p.type) return true;
            string ns = type.Groups[2].Value;
            location = new ManagedLocation { id = rid, relativePath = relative,
                typeName = type.Groups[3].Value + " " + (ns.Length == 0 ? "" : ns + ".") + type.Groups[1].Value };
            error = null;
            return true;
        }

        static bool InlinePointer(string line, out long id, out string guid, out int pointerType)
        {
            id = 0; guid = null; pointerType = 0;
            var match = Regex.Match(line, @"\{fileID: (-?\d+), guid: ([a-f0-9]{32}), type: (\d+)\}\s*$");
            if (!match.Success || !long.TryParse(match.Groups[1].Value, out id) ||
                !int.TryParse(match.Groups[3].Value, out pointerType)) return false;
            guid = match.Groups[2].Value;
            return true;
        }

        static void ValidateManagedProperty(Check check, SerializedObject serialized, ManagedLocation location)
        {
            check.method = "exact source managed-reference ID, dynamic type and relative property";
            check.sourceManagedReferenceID = location.id;
            check.sourceManagedType = location.typeName;
            check.sourceProperty = location.relativePath;
            var roots = new List<SerializedProperty>();
            var it = serialized.GetIterator();
            while (it.Next(true))
                if (it.propertyType == SerializedPropertyType.ManagedReference && it.managedReferenceId == location.id &&
                    it.managedReferenceFullTypename == location.typeName) roots.Add(it.Copy());
            check.candidateProperties = roots.Select(r => r.propertyPath + " : " + r.managedReferenceFullTypename).ToArray();
            if (roots.Count != 1)
            { check.reason = "Exact managed-reference ID/type is unavailable/ambiguous; native roots=" + roots.Count; return; }
            var property = roots[0].FindPropertyRelative(location.relativePath);
            ValidateProperty(check, property, check.method);
        }

        static string DescribeOwner(Object value)
        {
            try { return value.GetType().FullName + "; localID=" + LocalID(value) +
                "; globalID=" + GlobalObjectId.GetGlobalObjectIdSlow(value); }
            catch (Exception e) { return value.GetType().FullName + "; identity diagnostic: " + e.Message; }
        }

        static string DescribeProperty(SerializedProperty property)
        {
            var value = property.objectReferenceValue;
            Identity(value, out string guid, out long id);
            return property.propertyPath + " : " + property.type + "; valueType=" +
                (value ? value.GetType().FullName : "null") + "; guid=" + guid + "; fileID=" + id;
        }

        static IEnumerable<Object> Owners(string file, Scene preview)
        {
            if (preview.IsValid())
            {
                foreach (var root in preview.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    yield return transform.gameObject;
                    foreach (var component in transform.GetComponents<Component>()) yield return component;
                    yield return PrefabUtility.GetPrefabInstanceHandle(transform.gameObject);
                }
            }
            else if (file.EndsWith(".meta", StringComparison.Ordinal))
                yield return AssetImporter.GetAtPath(file.Substring(0, file.Length - 5));
            else
            {
                foreach (var value in AssetDatabase.LoadAllAssetsAtPath(file))
                {
                    yield return value;
                    if (value is GameObject go)
                        foreach (var component in go.GetComponentsInChildren<Component>(true)) yield return component;
                }
            }
        }

        static long LocalID(Object value)
        {
            if (!value) return long.MinValue;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string _, out long id)) return id;
            return unchecked((long)GlobalObjectId.GetGlobalObjectIdSlow(value).targetObjectId);
        }
        static bool Identity(Object value, out string guid, out long id)
        {
            guid = null; id = 0;
            return value && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out guid, out id);
        }
        static bool Pending(Pointer p) => p.resolution == "IMPORT_ID_PENDING" || p.resolution == "BUILTIN_ID_PENDING";
        static bool Typed(SerializedProperty p) => !string.IsNullOrEmpty(p.type) && p.type.StartsWith("PPtr<", StringComparison.Ordinal) &&
            p.type != "PPtr<Object>" && p.type != "PPtr<$Object>";
        // Source lines move when a valid component is added. Stable ownership/target
        // identity plus an occurrence ordinal preserves duplicate-pointer multiplicity.
        static string Key(Pointer p) => $"{p.file}:{p.ownerFileID}:{p.property}:{p.guid}:{p.fileID}:{p.type}";
        static IEnumerable<string> OccurrenceKeys(IEnumerable<Pointer> pointers) => pointers.GroupBy(Key)
            .SelectMany(group => group.OrderBy(p => p.line).Select((p, ordinal) => group.Key+"#"+ordinal));
        static string[] ChangedInputs(Inventory inventory, string root)
        {
            var changed = inventory.inputHashes.Where(h => !File.Exists(ProjectFile(root, h.path)) ||
                Hash(ProjectFile(root, h.path)) != h.sha256).Select(h => h.path);
            var declared = new HashSet<string>(inventory.inputHashes.Select(h => ProjectFile(root, h.path)));
            var added = new[] { "Assets", "Packages", "ProjectSettings" }.SelectMany(folder =>
                Directory.GetFiles(ProjectFile(root, folder), "*", SearchOption.AllDirectories)).Where(path => !declared.Contains(Path.GetFullPath(path)));
            return changed.Concat(added).ToArray();
        }
        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        static string GitRevision(string root)
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", "rev-parse HEAD")
                { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true });
            string revision = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(); Require(process.ExitCode == 0 && revision.Length == 40, "Cannot establish current revision.");
            return revision;
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}

