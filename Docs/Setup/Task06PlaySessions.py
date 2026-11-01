"""Smoke the saved scene with native input, including normal and disabled domain/scene reloads."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

sys.dont_write_bytecode = True

from Task06Tests import command, PROJECT, CLI, EVIDENCE


def evaluate(code):
    result = command(["script", "eval", "--code", code])["data"]["result"]
    if result.get("success") is False:
        raise RuntimeError(result)
    return result["result"]


def settle():
    process = subprocess.run([str(CLI), "status", "--until-ready", "--timeout", "45",
                              "--project-path", str(PROJECT), "--format", "json"],
                             capture_output=True, text=True, timeout=55, check=False)
    if process.returncode or not json.loads(process.stdout)["success"]:
        raise RuntimeError(process.stdout)


def main():
    original = evaluate("return new { playing = UnityEditor.EditorApplication.isPlaying, "
                        "enabled = UnityEditor.EditorSettings.enterPlayModeOptionsEnabled, "
                        "options = (int)UnityEditor.EditorSettings.enterPlayModeOptions };")
    if original["playing"]:
        raise RuntimeError("Smoke requires a stopped Editor.")
    paths = [PROJECT / "Assets/NeonTunnelRunner/Configs" / name for name in ["Pools.asset", "Gameplay.asset", "World.asset"]]
    before = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
    report = {"originalOptions": original, "cycles": [], "passed": False}
    try:
        for fast in [False, True]:
            evaluate("UnityEditor.EditorSettings.enterPlayModeOptionsEnabled = " + str(fast).lower() +
                     "; UnityEditor.EditorSettings.enterPlayModeOptions = "
                     "UnityEditor.EnterPlayModeOptions.DisableDomainReload | "
                     "UnityEditor.EnterPlayModeOptions.DisableSceneReload; return true;")
            command(["editor", "set_auto_tick", "--enable", "true", "--interval_ms", "16"])
            command(["editor.playmode", "start"])
            settle()
            smoke = command(["script", "run", "--file", "Docs/Setup/Task06Smoke.cs",
                             "--entry", "NeonTunnelRunner.Task06Smoke.Run", "--", "--timeout", "200"], timeout=220)["data"]["result"]
            if not smoke.get("success") or not smoke["result"]["passed"]:
                raise RuntimeError(smoke)
            command(["editor.playmode", "stop"])
            settle()
            stopped = evaluate("return new { playing = UnityEditor.EditorApplication.isPlaying, "
                               "instanceClear = NeonTunnelRunner.GameManager.Instance == null, "
                               "timeScale = UnityEngine.Time.timeScale };")
            assert not stopped["playing"] and stopped["instanceClear"] and stopped["timeScale"] == 1
            report["cycles"].append({"reloadsEnabled": not fast, "smoke": smoke["result"], "exit": stopped})
            print(f"PASS: native scene smoke, reloads enabled={not fast}", flush=True)
        after = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
        report.update({"configBefore": before, "configAfter": after, "passed": before == after})
        assert report["passed"]
    finally:
        if evaluate("return UnityEditor.EditorApplication.isPlaying;"):
            command(["editor.playmode", "stop"])
            settle()
        evaluate("UnityEditor.EditorSettings.enterPlayModeOptionsEnabled = " + str(original["enabled"]).lower() +
                 "; UnityEditor.EditorSettings.enterPlayModeOptions = (UnityEditor.EnterPlayModeOptions)" +
                 str(original["options"]) + "; return true;")
        command(["editor", "set_auto_tick", "--enable", "false"])
        (EVIDENCE / "play-sessions.json").write_text(json.dumps(report, indent=2) + "\n")


if __name__ == "__main__":
    main()
