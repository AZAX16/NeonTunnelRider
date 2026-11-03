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
import json
import subprocess
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path
import Task14Tools as unity


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("step", choices=["tests", "fairness", "scene", "build", "smoke"])
    args = parser.parse_args()
    project = unity.PROJECT
    stamp = datetime.datetime.now().astimezone()
    folder = project / "Evidence/Task15/Runs" / (stamp.strftime("%Y%m%dT%H%M%S%z-") + args.step)
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
            if args.step == "build":
                source = source.replace('var paths=new[]{', 'var paths=new[]{"Assets/NeonTunnelRunner/Materials/ReferenceRoad.mat",')
            path = Path(scratch) / Path(file).name
            path.write_text(source)
            return unity.run(str(path), entry, timeout)

        try:
            if unity.evaluate("return UnityEditor.EditorApplication.isPlaying;"):
                manifest.update(status="BLOCKED", reason="Editor is in Play Mode; stop it before running isolated fixtures")
                return 2
            manifest["status"] = "RUNNING"
            save()
            if args.step == "tests":
                import Task14Tests as tests
                tests.EVIDENCE = folder
                tests.main()
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
