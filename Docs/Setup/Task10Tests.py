"""Run the actual Unity suites sequentially and retain Pipeline's per-case verdicts."""
import json
from pathlib import Path
import subprocess
import time
import sys
sys.dont_write_bytecode = True

PROJECT = Path(__file__).resolve().parents[2]
CLI = Path.home() / ".local/bin/unity.previous"
EVIDENCE = PROJECT / "Evidence/Task10"


def command(arguments, timeout=60):
    process = subprocess.run(
        [str(CLI), "command", "--caller", "plugin", "--skill", "unity-cli", "--project-path", str(PROJECT), "--format", "json",
         "--timeout", str(timeout), *arguments],
        capture_output=True, text=True, timeout=timeout+10, check=False)
    envelope = json.loads(process.stdout)
    if process.returncode or not envelope["success"]:
        raise RuntimeError(envelope)
    return envelope


def main():
    ready = subprocess.run([str(CLI), "status", "--until-ready", "--timeout", "45",
                            "--project-path", str(PROJECT), "--format", "json"],
                           capture_output=True, text=True, timeout=55, check=False)
    if ready.returncode or not json.loads(ready.stdout)["success"]:
        raise RuntimeError(ready.stdout)
    summaries = {}
    for mode, assembly in [("editor", "EditMode"), ("playmode", "PlayMode")]:
        label = "editmode" if mode == "editor" else "playmode"
        start = command(["test", "run", "--mode", mode, "--filter", f"NeonTunnelRunner.{assembly}Tests",
                         "--filter_type", "assembly", "--async_tests", "true"])
        if start['data']['result'].get('success') is False:
            raise RuntimeError(start)
        (EVIDENCE / f"{label}-start.json").write_text(json.dumps(start, indent=2) + "\n")
        print(f"Started {label}", flush=True)
        deadline = time.monotonic() + 600
        while True:
            time.sleep(2)
            status = command(["test", "status"])
            result = status["data"]["result"]
            if result.get("status") == "completed":
                break
            if time.monotonic() > deadline:
                raise TimeoutError(f"{label} did not finish; last status: {result}")
        (EVIDENCE / f"{label}-results.json").write_text(json.dumps(status, indent=2) + "\n")
        summaries[label] = result["summary"]
        print(label, result["summary"], flush=True)
        if result["summary"]["failed"] or result["summary"]["total"] == 0:
            print(json.dumps([r for r in result["results"] if r["Status"] != "Passed"], indent=2), flush=True)
            raise AssertionError(f"{label} suite failed")
    (EVIDENCE / "test-summary.json").write_text(json.dumps(summaries, indent=2) + "\n")


if __name__ == "__main__":
    main()
