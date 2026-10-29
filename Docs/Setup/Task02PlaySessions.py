"""Verify real Play Mode entry/exit in both reload modes; restore the user's settings."""
import hashlib
import json
from pathlib import Path
import subprocess
import time


PROJECT = Path(__file__).resolve().parents[2]
CLI = Path.home() / ".local/bin/unity.previous"
OUTPUT = PROJECT / "Evidence/Task02/play-sessions.json"


def command(*arguments):
    result = subprocess.run(
        [str(CLI), "command", "--caller", "plugin", "--skill", "unity-cli",
         *arguments, "--project-path", str(PROJECT), "--format", "json"],
        text=True, capture_output=True, timeout=60, check=False,
    )
    envelope = json.loads(result.stdout)
    if result.returncode or not envelope["success"]:
        raise RuntimeError(envelope)
    value = envelope["data"]["result"]
    if isinstance(value, dict) and value.get("success") is False:
        raise RuntimeError(value)
    return value


def evaluate(code):
    return command("script", "eval", "--code", code)["result"]


def settle():
    # These transitions may reload the Editor domain. Wait for its own readiness protocol.
    time.sleep(1)
    result = subprocess.run(
        [str(CLI), "status", "--until-ready", "--timeout", "45",
         "--project-path", str(PROJECT), "--format", "json"],
        text=True, capture_output=True, timeout=55, check=False,
    )
    envelope = json.loads(result.stdout)
    if result.returncode or not envelope["success"]:
        raise RuntimeError(envelope)


def main():
    original = evaluate("return new { playing = UnityEditor.EditorApplication.isPlaying, "
                        "enabled = UnityEditor.EditorSettings.enterPlayModeOptionsEnabled, "
                        "options = (int)UnityEditor.EditorSettings.enterPlayModeOptions };")
    if original["playing"]:
        raise RuntimeError("Stop Play Mode before this verification.")
    config = PROJECT / "Assets/NeonTunnelRunner/Configs/Gameplay.asset"
    config_hash = hashlib.sha256(config.read_bytes()).hexdigest()
    report = {"originalOptions": original, "cycles": [], "passed": False}
    try:
        for reloads_enabled in [False, False, True, True]:
            fast = "false" if reloads_enabled else "true"
            evaluate("UnityEditor.EditorSettings.enterPlayModeOptionsEnabled = " + fast + "; "
                     "UnityEditor.EditorSettings.enterPlayModeOptions = "
                     "UnityEditor.EnterPlayModeOptions.DisableDomainReload | "
                     "UnityEditor.EnterPlayModeOptions.DisableSceneReload; return true;")
            command("editor.playmode", "start")
            settle()
            entry = evaluate(
                "var m = NeonTunnelRunner.GameManager.Instance; "
                "if (!UnityEditor.EditorApplication.isPlaying || !m || "
                "m.State != NeonTunnelRunner.GameState.Menu || UnityEngine.Time.timeScale != 1f || "
                "m.Session.Snapshot.RunId != System.Guid.Empty) "
                "throw new System.Exception(\"Expected a fresh Menu owner\"); "
                "m.StartRun(); m.Loop.Step(0.125f); m.Pause(); "
                "return new { state = m.State.ToString(), runId = m.Session.Snapshot.RunId.ToString(), "
                "lives = m.Session.Snapshot.Lives, fuel = m.Session.Snapshot.Fuel, "
                "timeScale = UnityEngine.Time.timeScale, scene = m.gameObject.scene.path };")
            command("editor.playmode", "stop")
            settle()
            exit_state = evaluate("return new { playing = UnityEditor.EditorApplication.isPlaying, "
                                  "instanceClear = NeonTunnelRunner.GameManager.Instance == null, "
                                  "timeScale = UnityEngine.Time.timeScale };")
            assert not exit_state["playing"] and exit_state["instanceClear"] and exit_state["timeScale"] == 1
            assert entry["state"] == "Paused" and entry["timeScale"] == 0
            assert entry["lives"] == 3 and entry["fuel"] == 100
            report["cycles"].append({"reloadsEnabled": reloads_enabled, "entry": entry, "exit": exit_state})
            print(f"PASS: cycle {len(report['cycles'])}, reloads enabled={reloads_enabled}", flush=True)
        report["configBeforeSha256"] = config_hash
        report["configAfterSha256"] = hashlib.sha256(config.read_bytes()).hexdigest()
        assert report["configBeforeSha256"] == report["configAfterSha256"]
        assert len({c["entry"]["runId"] for c in report["cycles"]}) == 4
        report["passed"] = True
    finally:
        if evaluate("return UnityEditor.EditorApplication.isPlaying;"):
            command("editor.playmode", "stop")
            settle()
        evaluate("UnityEditor.EditorSettings.enterPlayModeOptionsEnabled = " + str(original["enabled"]).lower() +
                 "; UnityEditor.EditorSettings.enterPlayModeOptions = (UnityEditor.EnterPlayModeOptions)" +
                 str(original["options"]) + "; return true;")
        OUTPUT.write_text(json.dumps(report, indent=2) + "\n")


if __name__ == "__main__":
    main()
