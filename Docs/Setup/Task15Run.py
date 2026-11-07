"""Resume one real Unity audit step, with a new evidence directory on every attempt.

python3 -B Docs/Setup/Task15Run.py tests|fairness|scene|build|smoke
Reuses Task14's installed Pipeline APIs. Never overwrites historical evidence.
Requires a licensed connected graphical Editor. The smoke step changes temporary
Editor view/reload settings through the existing fixture and restores them.
This is not the standalone performance/soak runner.
"""
import argparse
import datetime
import hashlib
import fcntl
import json
import subprocess
import tempfile
import os
import xml.etree.ElementTree as ET
from pathlib import Path
import Task14Tools as unity


def preserve_player_best(folder):
    """Sync a standalone-earned best into the already-open Editor's prefs cache.

    Linux PlayerPrefs.Save flushes the process cache, including other keys. Tests
    use isolated keys, but their Save must not overwrite a newer standalone best.
    Call only with no concurrent player. Back up the private prefs outside Git;
    the evidence contains only the game score, never unrelated preference values.
    """
    identity = unity.evaluate('return new { company=UnityEngine.Application.companyName, product=UnityEngine.Application.productName };')
    prefs = Path.home() / ".config/unity3d" / identity["company"] / identity["product"] / "prefs"
    key = "NTR.HighScore.v1"
    disk = 0
    backup = None
    if prefs.exists():
        raw = prefs.read_bytes()
        root = ET.fromstring(raw)
        node = next((p for p in root.findall("pref") if p.get("name") == key), None)
        if node is not None:
            if node.get("type") != "int":
                raise RuntimeError("Production best has an unexpected PlayerPrefs type.")
            disk = max(0, int(node.text))
        fd, backup = tempfile.mkstemp(prefix="neon-task15-prefs-", suffix=".xml")
        with os.fdopen(fd, "wb") as stream:
            stream.write(raw)
    proof = unity.evaluate('var key="NTR.HighScore.v1"; var before=UnityEngine.PlayerPrefs.GetInt(key,0); '
                           'var retained=System.Math.Max(before,' + str(disk) + '); '
                           'if(retained!=before){UnityEngine.PlayerPrefs.SetInt(key,retained);UnityEngine.PlayerPrefs.Save();} '
                           'return new { before, retained };')
    (folder / "preference-cache-guard.json").write_text(json.dumps({
        "key": key, "diskBefore": disk, **proof,
        "privateBackup": backup,
        "method": "Preserve the greater existing disk/Editor best before isolated test-key Save; no score invented",
    }, indent=2) + "\n")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("step", choices=["tests", "fairness", "scene", "build", "smoke"])
    parser.add_argument("--evidence-root", default="Evidence/Task15/Runs", help="New run directory parent; historical outputs are never reused")
    args = parser.parse_args()
    project = unity.PROJECT
    # Editor tests, scene fixtures and builds mutate the same live session.
    # Refuse overlapping audit processes before either touches the Editor.
    lock_path = project / "Library/Pipeline/task15-audit.lock"
    lock_path.parent.mkdir(parents=True, exist_ok=True)
    lock_handle = lock_path.open("a+")
    try:
        fcntl.flock(lock_handle, fcntl.LOCK_EX | fcntl.LOCK_NB)
    except BlockingIOError:
        lock_handle.close()
        raise RuntimeError("Another Task15Run step owns the Editor; wait for it to finish.")
    lock_handle.seek(0); lock_handle.truncate()
    lock_handle.write(json.dumps({"pid": os.getpid(), "step": args.step})); lock_handle.flush()
    stamp = datetime.datetime.now().astimezone()
    folder = project / args.evidence_root / (stamp.strftime("%Y%m%dT%H%M%S%z-") + args.step)
    folder.mkdir(parents=True)
    unity.EVIDENCE = folder
    manifest = {
        "started": stamp.isoformat(), "step": args.step, "status": "NOT RUN",
        "sourceRevision": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=project, text=True).strip(),
        "treeStatus": subprocess.check_output(["git", "status", "--short"], cwd=project, text=True),
        "productionInputs": {str(p.relative_to(project)): hashlib.sha256(p.read_bytes()).hexdigest()
                             for name in ["Assets", "Packages", "ProjectSettings"]
                             for p in sorted((project / name).rglob("*")) if p.is_file()},
        "build": "Editor step; no standalone performance claim",
    }
    save = lambda: (folder / "run-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    save()
    ready = subprocess.run([str(unity.CLI), "status", "--project-path", str(project), "--format", "json"],
                           capture_output=True, text=True, timeout=15)
    (folder / "editor-status.json").write_text(ready.stdout)
    if ready.returncode:
        manifest.update(status="BLOCKED", reason="No connected Editor; inspect editor-status.json", finished=datetime.datetime.now().astimezone().isoformat())
        save()
        print(json.dumps({"status": manifest["status"], "evidence": str(folder)}, indent=2))
        return 2

    with tempfile.TemporaryDirectory(prefix="task15-script-") as scratch:
        def run(file, entry, timeout=60):
            source = (project / file).read_text()
            # Adapt only evidence/build locations in proven helpers, retaining their API/lifecycle.
            source = source.replace("Evidence/Task14", str(folder.relative_to(project)))
            source = source.replace("Builds/Task14UI", "Builds/Task15Integration")
            source = source.replace('private const string Output = "Evidence/Task15";',
                                    'private const string Output = "' + str(folder.relative_to(project)) + '";')
            source = source.replace('"Task14 verification"', '"Task15 verification"')
            if args.step == "smoke" and Path(file).name == "Task14SceneSmoke.cs":
                # Input events must span real player frames at every viewport, including
                # timeScale 0. Fixed 70ms sleeps can coalesce down/up under Editor load.
                source = source.replace("static async Task Tap(Key key)", """static async Task InputFrames()
        {
            int target = Time.frameCount + 4;
            int updates = 0;
            double notBefore = Time.realtimeSinceStartupAsDouble + .12d;
            Action afterUpdate = () => updates++;
            var deadline = DateTime.UtcNow.AddSeconds(5);
            InputSystem.onAfterUpdate += afterUpdate;
            try
            {
                while (Time.frameCount < target || updates < 2 || Time.realtimeSinceStartupAsDouble < notBefore)
                {
                    Check(DateTime.UtcNow < deadline, "Input fixture frame/update timeout");
                    await Task.Delay(16);
                }
            }
            finally { InputSystem.onAfterUpdate -= afterUpdate; }
        }
        static async Task Tap(Key key)""")
                source = source.replace("await Task.Delay(70)", "await InputFrames()")
                source = source.replace("after.Length==10", "after.Length==11")
                # Keep virtual test devices active when CLI requests take desktop focus.
                source = source.replace("bool background=Application.runInBackground;", """var oldBackground=InputSystem.settings.backgroundBehavior;
            var oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            bool background=Application.runInBackground;""")
                source = source.replace("Application.runInBackground=true;", """Application.runInBackground=true;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;""")
                source = source.replace("Set(m,\"highScores\",new HighScoreStore(key));keyboard=InputSystem.AddDevice<Keyboard>();", """Set(m,"highScores",new HighScoreStore(key));
            var desktopDevices=InputSystem.devices.Where(d=>d.enabled).ToArray();
            foreach(var device in desktopDevices)InputSystem.DisableDevice(device);
            keyboard=InputSystem.AddDevice<Keyboard>();""")
                source = source.replace("InputSystem.RemoveDevice(mouse);", "InputSystem.RemoveDevice(mouse);foreach(var device in desktopDevices)if(device.added)InputSystem.EnableDevice(device);")
                source = source.replace("Application.runInBackground=background;", """Application.runInBackground=background;
                InputSystem.settings.backgroundBehavior=oldBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;""")
            (folder / (Path(file).stem + "-executed.cs")).write_text(source)
            if args.step == "build":
                source = source.replace('var paths=new[]{', 'var paths=new[]{"Assets/NeonTunnelRunner/Materials/ReferenceRoad.mat",')
            path = Path(scratch) / Path(file).name
            path.write_text(source)
            return unity.run(str(path), entry, timeout)

        try:
            if unity.evaluate("return UnityEditor.EditorApplication.isPlaying;"):
                manifest.update(status="BLOCKED", reason="Editor is in Play Mode; stop it before running isolated fixtures")
                return 2
            if unity.evaluate('return (bool)typeof(UnityEditor.TestTools.TestRunner.Api.TestRunnerApi).GetMethod("IsRunActive",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);'):
                manifest.update(status="BLOCKED", reason="A native Test Runner job is active; wait for it to finish")
                return 2
            readiness = unity.evaluate('return new { compiling=UnityEditor.EditorApplication.isCompiling, updating=UnityEditor.EditorApplication.isUpdating, failed=UnityEditor.EditorUtility.scriptCompilationFailed };')
            if readiness["compiling"] or readiness["updating"] or readiness["failed"]:
                manifest.update(status="BLOCKED", reason="Editor compilation/import is not ready", readiness=readiness)
                return 2
            preserve_player_best(folder)
            manifest["status"] = "RUNNING"
            save()
            if args.step == "tests":
                import Task14Tests as tests
                tests.EVIDENCE = folder
                original_test_command = tests.command
                def native_export_code(path=None):
                    code = '''var api=typeof(UnityEditor.TestTools.TestRunner.Api.TestRunnerApi);
var holder=api.GetProperty("CallbacksHolder",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
var callbacks=(UnityEditor.TestTools.TestRunner.Api.ICallbacks[])holder.GetType().GetMethod("GetAll").Invoke(holder,null);
foreach(var cb in callbacks) if(cb.GetType().Name=="ResultsSavingCallbacks" &&
((string)cb.GetType().GetField("m_ResultFilePath").GetValue(cb)).StartsWith(''' + json.dumps(str(folder)) + '''))
UnityEditor.TestTools.TestRunner.Api.TestRunnerApi.UnregisterTestCallback(cb);
'''
                    if path is not None:
                        code += '''var type=api.Assembly.GetType("UnityEditor.TestTools.TestRunner.CommandLineTest.ResultsSavingCallbacks");
var export=UnityEngine.ScriptableObject.CreateInstance(type); export.name="Task15NativeXmlExport";
type.GetField("m_ResultFilePath").SetValue(export,''' + json.dumps(str(path)) + ''');
UnityEditor.TestTools.TestRunner.Api.TestRunnerApi.RegisterTestCallback((UnityEditor.TestTools.TestRunner.Api.ICallbacks)export);
'''
                    return code + "return true;"
                def with_native_export(arguments, timeout=60):
                    if arguments[:2] == ["test", "run"]:
                        mode = arguments[arguments.index("--mode") + 1]
                        label = "editmode" if mode == "editor" else "playmode"
                        unity.evaluate(native_export_code(folder / (label + "-native.xml")))
                    return original_test_command(arguments, timeout)
                tests.command = with_native_export
                try:
                    tests.main()
                finally:
                    unity.evaluate(native_export_code())
                summaries = json.loads((folder / "test-summary.json").read_text())
                for mode in ["editmode", "playmode"]:
                    result = json.loads((folder / (mode + "-results.json")).read_text())["data"]["result"]
                    suite = ET.Element("testsuite", name=mode, tests=str(result["summary"]["total"]),
                                       failures=str(result["summary"]["failed"]), skipped=str(result["summary"]["skipped"]))
                    props = ET.SubElement(suite, "properties")
                    ET.SubElement(props, "property", name="origin", value="Derived from retained Pipeline per-case JSON; not native Test Runner XML")
                    ET.SubElement(props, "property", name="revision", value=manifest["sourceRevision"])
                    for case in result["results"]:
                        node = ET.SubElement(suite, "testcase", name=case["FullName"], time=str(case["Duration"]))
                        if case["Status"] == "Failed":
                            ET.SubElement(node, "failure", message=case.get("Message") or "").text = case.get("StackTrace") or ""
                        elif case["Status"] != "Passed":
                            ET.SubElement(node, "skipped", message=case["Status"])
                    ET.indent(suite)
                    ET.ElementTree(suite).write(folder / (mode + "-results.xml"), encoding="utf-8", xml_declaration=True)
                manifest["summary"] = summaries
                manifest["status"] = "PASS" if all(x["skipped"] == 0 and x["inconclusive"] == 0 for x in summaries.values()) else "FAIL"
            elif args.step == "smoke":
                import Task14PlaySessions as smoke
                smoke.EVIDENCE = folder
                smoke.run = run
                # Preserve the operator's existing Console instead of Task14's historical clear.
                original_command = smoke.command
                def preserve_console(arguments, *positional, **keywords):
                    if arguments == ["console", "clear"]:
                        snapshot = original_command(["console", "get"], *positional, **keywords)
                        unity.save("console-before-smoke.json", snapshot)
                        return snapshot
                    return original_command(arguments, *positional, **keywords)
                smoke.command = preserve_console
                auto_tick = unity.evaluate('return UnityEditor.SessionState.GetBool("Unity.Pipeline.AutoTick.Enabled",true);')
                try:
                    smoke.main()
                finally:
                    # Existing smoke restores reload/background/devices. Restore its temporary
                    # viewports too, without deleting any historical Task14 custom sizes.
                    original_file = folder / "viewport-original.json"
                    if original_file.exists():
                        original = json.loads(original_file.read_text())["data"]["result"]["result"]["oldIndex"]
                        unity.evaluate('''var f=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static;
var a=typeof(UnityEditor.Editor).Assembly;var t=a.GetType("UnityEditor.GameViewSizes");
var s=typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(t).GetProperty("instance",f).GetValue(null);
var g=t.GetProperty("currentGroup",f).GetValue(s);var c=(System.Collections.IList)g.GetType().GetField("m_Custom",f).GetValue(g);
for(int i=c.Count-1;i>=0;i--)if((string)c[i].GetType().GetProperty("baseText",f).GetValue(c[i])=="Task15 verification")c.RemoveAt(i);
t.GetMethod("Changed",f).Invoke(s,null);var v=a.GetType("UnityEditor.GameView");var w=UnityEditor.EditorWindow.GetWindow(v);
v.GetProperty("selectedSizeIndex",f).SetValue(w,''' + str(original) + ''');w.Repaint();return true;''')
                    unity.command(["editor", "set_auto_tick", "--enable", str(auto_tick).lower()])
                manifest["status"] = "PASS"
            else:
                file, entry, timeout = {
                    "fairness": ("Docs/Setup/Task15Fairness.cs", "NeonTunnelRunner.Task15Fairness.Run", 600),
                    "scene": ("Docs/Setup/Task14Audit.cs", "NeonTunnelRunner.Task14Audit.Inspect", 60),
                    "build": ("Docs/Setup/Task14Build.cs", "NeonTunnelRunner.Task14Build.Run", 600),
                }[args.step]
                result = run(file, entry, timeout)
                (folder / (args.step + "-result.json")).write_text(json.dumps(result, indent=2) + "\n")
                value = result["data"]["result"]["result"]
                if args.step == "build":
                    if value["result"] != "Succeeded" or value["totalErrors"]:
                        raise AssertionError(value)
                    build = project / "Builds/Task15Integration"
                    manifest["buildFiles"] = {str(p.relative_to(build)): hashlib.sha256(p.read_bytes()).hexdigest()
                                              for p in sorted(build.rglob("*")) if p.is_file()}
                if args.step == "fairness" and not value["passed"]:
                    raise AssertionError(value)
                manifest["status"] = "PASS"
        except Exception as error:
            manifest.update(status="FAIL", reason=repr(error))
            raise
        finally:
            manifest["finished"] = datetime.datetime.now().astimezone().isoformat()
            save()
            print(json.dumps({"status": manifest["status"], "evidence": str(folder)}, indent=2), flush=True)
    return 0 if manifest["status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
