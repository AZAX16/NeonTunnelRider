"""Read-only Task 15 source/asset/evidence audit. Does not launch Unity or touch preferences.

Run from any directory: python3 -B Docs/Setup/Task15StaticAudit.py
PASS here means the named static check only, never executed gameplay or a successful import.
"""
import datetime
import hashlib
import json
import re
import subprocess
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Evidence/Task15"
BASE = "2923f56b79c6cba4b24627fee4bde2e902d872ab"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()


def documents(path):
    # Inspect saved serialization only; Unity is still needed for subasset/fileID validation.
    return [(int(kind), int(fid), body) for kind, fid, body in re.findall(
        r"^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)",
        path.read_text(), re.M | re.S)]


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    result = {
        "timestamp": datetime.datetime.now().astimezone().isoformat(),
        "sourceRevision": git("rev-parse", "HEAD"),
        "preservationBaseline": BASE,
        "command": "python3 -B Docs/Setup/Task15StaticAudit.py",
        "scope": "Saved bytes and source inspection; no Unity lifecycle, rendering, audibility or performance result",
    }
    protected = git("ls-tree", "-r", "--name-only", BASE).splitlines()
    protected = [p for p in protected if p.startswith(("Assets/", "Packages/", "ProjectSettings/", "Preproduction/")) or p == ".gitignore"]
    changes = []
    for name in protected:
        path = ROOT / name
        before = subprocess.check_output(["git", "show", BASE + ":" + name], cwd=ROOT)
        if not path.exists() or path.read_bytes() != before:
            changes.append(name)
    expected_changes = {"Preproduction/tools/audit_package.py"}
    unexpected_changes = [p for p in changes if p not in expected_changes]
    result["preservation"] = {"checkedFiles": len(protected), "changed": changes,
        "expectedToolingChange": "audit_package.py now locates the supplied ignore at the Unity repository root",
        "unexpectedChanges": unexpected_changes,
        "productionChanges": [p for p in changes if p.startswith(("Assets/", "Packages/", "ProjectSettings/"))],
        "result": "PASS" if not unexpected_changes else "FAIL"}
    inputs = {str(p.relative_to(ROOT)): digest(p) for folder in ["Assets", "Packages", "ProjectSettings"]
              for p in sorted((ROOT / folder).rglob("*")) if p.is_file()}
    result["productionInputs"] = inputs
    result["untrackedProductionFiles"] = [p for p in git("ls-files", "--others", "--exclude-standard").splitlines()
        if p.startswith(("Assets/", "Packages/", "ProjectSettings/"))]
    result["concurrentUserChange"] = "User confirmed reopening Unity and creating Assets/_Recovery during this audit; preserve and exclude from Task15 checkpoint"
    result["productionInputSetSha256"] = hashlib.sha256(json.dumps(inputs, sort_keys=True, separators=(",", ":")).encode()).hexdigest()
    ignore = ROOT / ".gitignore"
    probes = ["Library/probe", "Temp/probe", "Obj/probe", "Builds/probe", "Logs/probe", "UserSettings/probe"]
    result["repository"] = {
        "gitignoreSha256": digest(ignore),
        "suppliedIgnoreIdentical": ignore.read_bytes() == (ROOT / "Preproduction/source/assignment.gitignore").read_bytes(),
        "ignoredProbes": subprocess.run(["git", "check-ignore", *probes], cwd=ROOT, capture_output=True, text=True).stdout.splitlines(),
        "remotes": git("remote", "-v"),
        "trackedGenerated": [p for p in git("ls-files").splitlines() if re.match(r"(?i)(library|temp|obj|builds|logs|usersettings)/", p)],
        "trackedRootCounts": {name: sum(p.startswith(name + "/") for p in git("ls-files").splitlines()) for name in ["Assets", "Packages", "ProjectSettings"]},
    }
    asset_guids, duplicates, missing_metas = {}, [], []
    for p in sorted((ROOT / "Assets").rglob("*")):
        if p.suffix != ".meta" and not p.name.startswith(".") and not p.with_name(p.name + ".meta").exists():
            missing_metas.append(str(p.relative_to(ROOT)))
        if p.suffix == ".meta":
            m = re.search(r"^guid: (\w+)$", p.read_text(), re.M)
            if m:
                if m[1] in asset_guids:
                    duplicates.append([asset_guids[m[1]], str(p.relative_to(ROOT))])
                asset_guids[m[1]] = str(p.relative_to(ROOT))
    package_guids = set()
    package_paths = {}
    for p in (ROOT / "Library/PackageCache").glob("**/*.meta"):
        m = re.search(r"^guid: (\w+)$", p.read_text(errors="replace"), re.M)
        if m:
            package_guids.add(m[1])
            package_paths[m[1]] = str(p.relative_to(ROOT)).removesuffix(".meta")
    unresolved = []
    for p in (ROOT / "Assets").rglob("*"):
        if p.is_file() and p.suffix in [".unity", ".prefab", ".asset", ".mat", ".mixer"]:
            for guid in set(re.findall(r"guid: ([0-9a-f]{32})", p.read_text(errors="replace"))):
                if guid not in asset_guids and guid not in package_guids and not guid.startswith("0000000000000000"):
                    unresolved.append({"file": str(p.relative_to(ROOT)), "guid": guid})
    result["assetIntegrity"] = {"assetGuids": len(asset_guids), "packageGuids": len(package_guids), "missingMetas": missing_metas,
        "duplicateGuids": duplicates, "unresolvedGuidReferences": unresolved,
        "limitation": "Does not prove fileID/subasset or fresh-import resolution"}
    defaults = documents(ROOT / "Assets/Settings/DefaultVolumeProfile.asset")
    profile = next(b for _, fid, b in defaults if fid == 11400000)
    profile_ids = set(re.findall(r"fileID: (-?\d+)", profile))
    result["templateVolumeOrphans"] = [{"fileID": fid, "name": re.search(r"m_Name: (.*)", body)[1],
        "referencedByProfileComponents": str(fid) in profile_ids}
        for _, fid, body in defaults if "m_Script: {fileID: 0}" in body or
        any(x["guid"] in body for x in unresolved if x["file"] == "Assets/Settings/DefaultVolumeProfile.asset")]
    scene_path = ROOT / "Assets/NeonTunnelRunner/Scenes/Game.unity"
    scene = documents(scene_path)
    refs = []
    for kind, fid, body in scene:
        if kind == 114 and "NeonTunnelRunner.Runtime::" in body:
            refs.append({"fileID": fid, "class": re.search(r"m_EditorClassIdentifier: (.*)", body)[1],
                         "references": re.findall(r"^  (\w+): (\{fileID:.*\})$", body, re.M)})
    result["savedScene"] = {
        "path": str(scene_path.relative_to(ROOT)), "sha256": digest(scene_path),
        "componentKinds": dict(Counter(kind for kind, _, _ in scene)), "projectComponentReferences": refs,
        "uiPrefabInstances": sum("guid: " + (re.search(r"guid: (\w+)", (ROOT / "Assets/NeonTunnelRunner/UI/RunnerCanvas.prefab.meta").read_text())[1]) in b for k, _, b in scene if k == 1001),
        "scriptRecords": [{"fileID": fid, "script": asset_guids.get(m[1], package_paths.get(m[1], m[1]))}
            for kind, fid, body in scene if kind == 114
            for m in [re.search(r"m_Script: \{fileID: \d+, guid: (\w+)", body)] if m],
    }
    configs = {p.stem: p.read_text() for p in (ROOT / "Assets/NeonTunnelRunner/Configs").glob("*.asset")}
    capacities = [dict(key=int(k), capacity=int(n), prefab=asset_guids.get(g, g).removesuffix(".meta"))
        for k, g, n in re.findall(r"- key: (\d+)\n    prefab: \{fileID: -?\d+, guid: (\w+), type: \d+\}\n    capacity: (\d+)", configs["Pools"])]
    result["pools"] = {"entries": capacities, "configuredTotal": sum(e["capacity"] for e in capacities),
        "runtimeCreatedActivePeakMisses": "BLOCKED: no current valid gameplay execution"}
    mixer = ROOT / "Assets/NeonTunnelRunner/Audio/Mixers/RunnerMixer.mixer"
    result["audio"] = {"mixerSha256": digest(mixer), "masterSnapshotDb": float(re.search(r"e5fc3107d5446535ca5ddeef738e5672: ([\d.]+)", mixer.read_text())[1]),
        "savedSceneSources": sum(k == 82 for k, _, _ in scene),
        "sceneOwnedSources": sum(k == 82 and "m_CorrespondingSourceObject: {fileID: 0}" in b for k, _, b in scene),
        "inheritedSourceReferences": sum(k == 82 and "m_CorrespondingSourceObject: {fileID: 0}" not in b for k, _, b in scene),
        "prefabSources": [{"path": str((ROOT / e["prefab"]).relative_to(ROOT)), "count": sum(k == 82 for k, _, _ in documents(ROOT / e["prefab"]))}
                          for e in capacities if any(k == 82 for k, _, _ in documents(ROOT / e["prefab"]))],
        "playerPrefabSources": sum(k == 82 for k, _, _ in documents(ROOT / "Assets/NeonTunnelRunner/Prefabs/Player/PlayerHovercar.prefab")),
        "audibilityAndPostGainClipping": "BLOCKED: no sound output/capture device"}
    source_root = ROOT / "Assets/NeonTunnelRunner/Scripts"
    sources = {str(p.relative_to(ROOT)): p.read_text() for p in source_root.rglob("*.cs")}
    def matches(pattern):
        return [{"file": p, "line": i, "text": line.strip()} for p, code in sources.items()
                for i, line in enumerate(code.splitlines(), 1) if re.search(pattern, line)]
    result["sourceScan"] = {
        "legacyOrDirectPolling": matches(r"\bInput\.(?:Get|anyKey|mousePosition)|(?:Keyboard|Mouse|Gamepad)\.current|void OnGUI\("),
        "singletonDeclarations": matches(r"static \w+ Instance\s*\{"),
        "fixedUpdates": matches(r"void FixedUpdate\("),
        "creationAndDestruction": matches(r"\bInstantiate\(|\bDestroy(?:Immediate)?\(|AddComponent<|new GameObject"),
        "recurringHudStringSites": matches(r"(?:score|best|speed)\.ToString"),
        "preferenceSites": matches(r"PlayerPrefs\.|ProductionKey"),
        "concreteOverrides": {name: "protected override void OnInteract" in (source_root / ("Interactables/" + name + ".cs")).read_text()
                              for name in ["StationaryCar", "MovingCar", "Coin", "FuelCan"]},
    }
    action_path = ROOT / "Assets/NeonTunnelRunner/Input/Runner.inputactions"
    actions = json.loads(action_path.read_text())
    result["input"] = {"newOnlySetting": re.search(r"activeInputHandler: (\d+)", (ROOT / "ProjectSettings/ProjectSettings.asset").read_text())[1],
        "maps": [{"name": m["name"], "actions": [a["name"] for a in m["actions"]],
                  "bindings": [{k: b[k] for k in ["action", "path"]} for b in m["bindings"]]} for m in actions["maps"]]}
    result["packages"] = json.loads((ROOT / "Packages/manifest.json").read_text())["dependencies"]
    result["editorPin"] = (ROOT / "ProjectSettings/ProjectVersion.txt").read_text().strip()
    # Hash every actual historical build file, not just the common Unity launcher executable.
    build = ROOT / "Builds/Task14UI"
    result["historicalBuild"] = {"scope": "Historical Task14, no new Task15 build", "path": str(build.relative_to(ROOT)),
        "files": {str(p.relative_to(build)): {"bytes": p.stat().st_size, "sha256": digest(p)} for p in sorted(build.rglob("*")) if p.is_file()}}
    previous_inputs = json.loads((ROOT / "Evidence/Task14/build-inputs.json").read_text())["productionFiles"]
    result["historicalBuild"]["currentInputDifferences"] = [{"path": p, "recordedHash": h, "currentHash": inputs.get(p)}
        for p, h in previous_inputs.items() if inputs.get(p) != h]
    result["historicalBuild"]["currentInputAdditions"] = [p for p in inputs if p not in previous_inputs]
    result["historicalEvidence"] = {
        "scope": "Re-read Task14 artifacts, not a fresh run",
        "testSummary": json.loads((ROOT / "Evidence/Task14/test-summary.json").read_text()),
        "suiteCases": {mode: dict(Counter(case["Status"] for case in json.loads((ROOT / ("Evidence/Task14/" + mode + "-results.json")).read_text())["data"]["result"]["results"])) for mode in ["editmode", "playmode"]},
        "artifactHashes": {p: digest(ROOT / p) for p in ["Evidence/Task14/editmode-results.json", "Evidence/Task14/playmode-results.json", "Evidence/Task14/play-sessions.json", "Evidence/Task14/Standalone/acceptance.json", "Evidence/Task14/console-final.json", "Evidence/Task14/build-inputs.json"]},
    }
    result["staticResult"] = "PASS" if not (unexpected_changes or missing_metas or duplicates or unresolved or result["sourceScan"]["legacyOrDirectPolling"]) else "FAIL"
    (OUT / "static-audit.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({k: result[k] for k in ["timestamp", "staticResult", "preservation", "assetIntegrity", "audio"]}, indent=2))
    return result


if __name__ == "__main__":
    raise SystemExit(0 if main()["staticResult"] == "PASS" else 1)
