"""Strict saved PPtr inventory. No imports, asset writes, or GUID allowlist.

Only serialized pointer mappings are dependencies; strings containing GUIDs are not.
Text subasset IDs are checked. Imported/built-in IDs require the pinned Editor.
"""
import argparse
from collections import Counter, deque
import datetime
import json
from pathlib import Path
import re
import subprocess

SERIALIZED = {".unity", ".prefab", ".asset", ".mat", ".mixer", ".controller", ".overrideController", ".meta"}
HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)(?: stripped)?$", re.M)
POINTER = re.compile(r"^( *)([\w.]+):\s*\{([^{}]*\bfileID:\s*-?\d+[^{}]*)\}", re.M)
# Exact engine containers documented by UnityDataTools/Documentation/textdumper.md.
# Container identity does NOT establish that a requested fileID exists.
BUILTIN_CONTAINERS = {"0000000000000000e000000000000000": "Library/unity default resources",
                      "0000000000000000f000000000000000": "Resources/unity_builtin_extra"}


def pointers(text):
    """Flow-style Unity PPtrs, anchored to a property (including list items)."""
    # A list can hold a pointer directly: `  - {fileID: ...}`.
    normalized = re.sub(r"^( *)- (\{fileID:)", r"\1_list_item: \2", text, flags=re.M)
    for match in POINTER.finditer(normalized):
        values = dict(re.findall(r"(fileID|guid|type):\s*([\w-]+)", match[3]))
        if "guid" not in values or not re.fullmatch(r"[0-9a-f]{32}", values["guid"]):
            continue
        prior = list(HEADER.finditer(normalized, 0, match.start()))
        yield {"property": match[2], "ownerFileID": int(prior[-1][2]) if prior else None,
               "ownerClassID": int(prior[-1][1]) if prior else None,
               "line": normalized.count("\n", 0, match.start()) + 1,
               "guid": values["guid"], "fileID": int(values["fileID"]),
               "type": int(values.get("type", -1))}


def package_roots(root):
    """Resolved packages only; a stray cached package must not satisfy a reference."""
    resolution = root / "Library/PackageManager/projectResolution.json"
    roots, errors = [], []
    lock = json.loads((root / "Packages/packages-lock.json").read_text())["dependencies"]
    resolved = json.loads(resolution.read_text()).get("outputs", {}) if resolution.exists() else {}
    for name, pin in lock.items():
        embedded = root / "Packages" / name
        entry = next((v for v in resolved.values() if v.get("name") == name), {})
        path = embedded if embedded.is_dir() else Path(entry.get("resolvedPath", "/__unresolved__"))
        # Local packages can be available before PackageManager has written its cache.
        if not path.is_dir() and pin.get("source") == "local":
            path = (root / "Packages" / pin["version"].removeprefix("file:")).resolve()
        manifest = path / "package.json"
        if not manifest.exists():
            errors.append({"package": name, "reason": "resolved package unavailable", "path": str(path)})
            continue
        actual = json.loads(manifest.read_text())
        if actual.get("name") != name or (pin.get("source") in {"registry", "builtin"} and actual.get("version") != pin["version"]):
            errors.append({"package": name, "reason": "resolved identity/version differs from lock", "path": str(path)})
            continue
        roots.append(path)
    return roots, errors


def index_metas(root, roots):
    index, duplicates, dangling = {}, [], []
    for folder in [root / "Assets", *roots]:
        for meta in sorted(folder.rglob("*.meta")):
            # Package Samples~/Documentation~ are not imported package assets. If copied
            # into Assets by Unity's sample importer, their new location is scanned normally.
            if folder != root / "Assets" and any(part.endswith("~") for part in meta.relative_to(folder).parts[:-1]):
                continue
            text = meta.read_text(errors="replace")
            guid = re.search(r"^guid: ([0-9a-f]{32})$", text, re.M)
            if not guid:
                continue
            asset = meta.with_suffix("")
            if not asset.exists():
                dangling.append(str(meta))
                continue
            if guid[1] in index and index[guid[1]] != asset:
                duplicates.append({"guid": guid[1], "assets": [str(index[guid[1]]), str(asset)]})
            index[guid[1]] = asset
    return index, duplicates, dangling


def resolve(ref, index):
    target = index.get(ref["guid"])
    if target is None:
        if ref["guid"] in BUILTIN_CONTAINERS and ref["type"] == 0:
            return {"resolution": "BUILTIN_ID_PENDING", "target": BUILTIN_CONTAINERS[ref["guid"]]}
        # No prefix-based exemption. Engine container IDs are pending, never a PASS.
        return {"resolution": "UNRESOLVED_GUID", "target": None}
    # Prefab Asset Handle is created by the importer and absent from YAML anchors.
    # Unity's serialization-language article documents precisely this combination.
    if (target.suffix == ".prefab" and ref["property"] == "m_SourcePrefab" and
            ref.get("ownerClassID") == 1001 and ref["fileID"] == 100100000 and ref["type"] == 3):
        return {"resolution": "IMPORT_ID_PENDING", "target": str(target)}
    if target.is_file():
        text = target.read_text(errors="replace") if target.suffix in SERIALIZED else ""
        ids = {int(m[2]) for m in HEADER.finditer(text)}
        if ids:
            return {"resolution": "TEXT_ID_PRESENT" if ref["fileID"] in ids else "INVALID_FILE_ID", "target": str(target)}
        if target.suffix == ".cs":
            return {"resolution": "SCRIPT_ID_CONVENTION" if ref["fileID"] == 11500000 else "INVALID_FILE_ID", "target": str(target)}
    return {"resolution": "IMPORT_ID_PENDING", "target": str(target)}


def scan(root, tracked=None, roots=None):
    root = Path(root).resolve()
    if tracked is None:
        tracked = set(subprocess.check_output(["git", "ls-files"], cwd=root, text=True).splitlines())
    selected_roots, errors = package_roots(root) if roots is None else (roots, [])
    index, duplicates, dangling = index_metas(root, selected_roots)
    records, missing_metas = [], []
    for folder in [root / "Assets", root / "ProjectSettings"]:
        for path in sorted(folder.rglob("*")):
            rel = path.relative_to(root).as_posix()
            if folder.name == "Assets" and path.suffix != ".meta" and not path.name.startswith(".") and not path.with_name(path.name + ".meta").exists():
                missing_metas.append(rel)
            if not path.is_file() or path.suffix not in SERIALIZED:
                continue
            for ref in pointers(path.read_text(errors="replace")):
                records.append({"file": rel, "tracked": rel in tracked,
                    "protectedRecovery": rel == "Assets/_Recovery.meta" or rel.startswith("Assets/_Recovery/"),
                    **ref, **resolve(ref, index)})
    # Conservative saved graph: enabled scenes, project settings, Resources and build profiles.
    # Whole-asset edges deliberately include orphan data; this cannot prove build stripping.
    by_file = {}
    for ref in records:
        by_file.setdefault(ref["file"], []).append(ref)
    roots_graph = {p for p in by_file if p.startswith("ProjectSettings/") or
        "/Resources/" in p or "/Settings/BuildProfiles/" in p}
    settings = root / "ProjectSettings/EditorBuildSettings.asset"
    if settings.exists():
        roots_graph.update(re.findall(r"enabled: 1\s+path: (.+)", settings.read_text()))
    reachable, queue = set(), deque(sorted(roots_graph))
    while queue:
        name = queue.popleft()
        if name in reachable:
            continue
        reachable.add(name)
        for ref in by_file.get(name, []):
            if ref["target"] and Path(ref["target"]).is_absolute() and Path(ref["target"]).is_relative_to(root):
                queue.append(Path(ref["target"]).relative_to(root).as_posix())
    for ref in records:
        ref["conservativelyReachable"] = ref["file"] in reachable
    counts = {}
    for name, subset in {"wholeWorkingTree": records, "tracked": [v for v in records if v["tracked"]],
                         "productionGraph": [v for v in records if v["conservativelyReachable"]],
                         "protectedRecovery": [v for v in records if v["protectedRecovery"]]}.items():
        counts[name] = {"pointers": len(subset), "resolutions": dict(Counter(v["resolution"] for v in subset)),
            "missingGuidPairs": len({(v["file"], v["guid"]) for v in subset if v["resolution"] == "UNRESOLVED_GUID"})}
    return {"timestamp": datetime.datetime.now().astimezone().isoformat(),
        "method": "Saved PPtrs and exact text anchors; imported/native/built-in resolution still requires pinned Editor. Exact engine containers are counted as pending, never ignored.",
        "graphMethod": "Conservative file-level closure from enabled build scenes, all ProjectSettings, Resources, build profiles; includes inactive/orphan serialized data, not a build dependency proof.",
        "packageRoots": [str(p) for p in selected_roots], "packageErrors": errors,
        "indexedAssetGuids": sum(p.is_relative_to(root / "Assets") for p in index.values()),
        "indexedPackageGuids": sum(not p.is_relative_to(root / "Assets") for p in index.values()),
        "duplicateGuids": duplicates, "danglingMetas": dangling, "missingMetas": missing_metas,
        "counts": counts, "graphRoots": sorted(roots_graph), "reachableFiles": sorted(reachable), "references": records,
        "result": "FAIL" if errors or duplicates or dangling or missing_metas or any(v["resolution"] in {"UNRESOLVED_GUID", "INVALID_FILE_ID"} for v in records) else "BLOCKED",
        "importConfirmation": "BLOCKED: this scanner never establishes successful Unity import"}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = scan(Path(__file__).resolve().parents[2])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({k: result[k] for k in ["result", "counts", "packageErrors", "duplicateGuids", "missingMetas"]}, indent=2))
    raise SystemExit(1 if result["result"] == "FAIL" else 2)
