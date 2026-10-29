# Task 02 — GameManager and run lifecycle

Verified 2026-10-29–30 (Asia/Tehran), starting from clean `main` at `bcb8c8b`. No repository instructions or subsequent user changes were found. The required architecture, requirements, structure, acceptance and task 01 environment/verification documents were read. Unity 6000.6.3f1, CLI beta.13 (`unity.previous`), Pipeline 0.9.1-exp.1 and all resolved packages were retained; nothing was reinstalled or upgraded.

## Implementation and ownership

- `Scripts/Core/GameState.cs` defines Menu, Playing, Paused, GameOver and None/Lives/Fuel terminal reasons.
- `GameManager.cs` is the sole singleton and owns one `RunSession`. Its seven lifecycle commands and Quit return acceptance; rejected/repeated commands leave run data and events unchanged. State, snapshot and terminal events carry typed values. Transition publication is guarded against reentrant commands, with state/time/speed committed before observers run. The first accepted EndRun reason/result wins.
- `RunSession.cs` is plain C# with an immutable `RunSnapshot` containing run ID, seed, lives, fuel, elapsed time, double distance, coins, score, best, cruise and actual speed. Controlled mutations are internal to the runtime assembly; existing test assemblies have friend access through `AssemblyInfo.cs`. Mutations are published once after a lifecycle transaction or tick. Restart reuses this model, generates a new Guid, resets current data, and preserves supplied best and the configured seed. There is no PlayerPrefs or score formula.
- `GameplayLoop.cs` owns the only FixedUpdate and an explicit `Step(dt)` contract. It checks owner/state/enable status even if timeScale is changed externally. Task 02 advances elapsed time and tick index only; distance, fuel and score do not tick. ActualSpeed currently mirrors CruiseSpeed while Playing and is zero when frozen; travel/measured-speed integration comes with the player system. Center start is configuration lane 0; live lane/pose ownership remains task 03.
- `Scripts/Config/GameplayConfig.cs` exposes private serialized authoring fields through getters only. Runtime systems do not write the asset. Values: lives 3, fuel 100, speed 22–50 m/s, lane width 3.2 m, transition 0.28 s, grace 1 s, drain 2/s, refill 30, coin value 100, difficulty 180 s / 6000 m. Later-system values are data only.
- Bootstrap is idempotent on ordinary enable/disable. Static ownership resets on Play entry; a one-time scene bootstrap handles both reloads disabled. Rejected duplicates disable/destroy only their manager component. Only the owner clears Instance, observers and loop state and restores timeScale during destruction/application quit. No DontDestroyOnLoad, static event bus or future-system dependency was added.

## Scene and configuration assignments

`Assets/NeonTunnelRunner/Scenes/Game.unity` retains GUID `2ff1f62cd218111b6af6f6ff50581601`. Its existing **Systems** root has exactly one enabled GameManager and GameplayLoop:

| Serialized field | Assignment |
| --- | --- |
| GameManager.gameplayConfig | `Assets/NeonTunnelRunner/Configs/Gameplay.asset`, GUID `50e71278e264ab6f3b9bd533f16b2e30` |
| GameManager.gameplayLoop | Systems → GameplayLoop |
| GameManager.runSeed / initialBestScore | 12345 / 0 |
| GameplayLoop.gameManager | Systems → GameManager |

The scene initializes in Menu with timeScale 1; Paused/GameOver use 0. Systems' GameManager context menu exposes Editor-only Lifecycle commands during Play Mode. No temporary keyboard input was added. The validator now also checks singleton component counts, Systems placement, enable states, both reference directions and valid assigned config.

The graybox, camera, light, Volume, renderer assignments and Build Profile are preserved. Scene serialization changes only Systems' two new component references and their component blocks. URP's automatically added light metadata was removed through the Editor before saving to retain the original light serialization. All four existing asmdefs and original meta files remain unchanged.

Unity itself resaved `ProjectSettings/TimeManager.asset` in its 6000.6 rational-timestep format when timeScale was exercised: nominal 0.02 s, reported `fixedDeltaTime` 0.0199999921, final timeScale 1. This is the only ProjectSettings diff; no timestep tuning was introduced. `Docs/Setup/Task02Setup.cs` records the Editor scene/config construction recipe; `Task02PlaySessions.py` records repeatable real Play entry/exit checks. Both stay outside Assets.

## Executed checks

| Check / requirements | Result | Evidence under `Evidence/Task02/` |
| --- | --- | --- |
| Compilation / V01 | PASS, zero reported compile errors/warnings; final compiled-state audit also reports compilationFailed=false. An obsolete lookup overload was replaced with the installed 6000.6 API before final tests. | `compile.json`, `validator.json` |
| EditMode / R27, R12, R15/R18 lifecycle, R46; V04/V12/V13 | **39 passed, 0 failed, 0 skipped.** Initial Menu; every permitted state transition; 20 rejected state/command pairs; single events; pause/resume; reset/best; both terminal reasons and repeated end; invalid arguments/reentrancy; immutable snapshot; Editor Quit; no automatic resource death. | `editmode-start.json`, `editmode-results.json` (per-case results) |
| PlayMode / V04/V12/V13 | **5 passed, 0 failed, 0 skipped.** Actual FixedUpdate gating; 50 alternating paused/GameOver restarts and enable cycles; duplicate destruction while paused; owner teardown/observer cleanup at zero time; three real Game scene loads/unloads starting in Menu. | `playmode-start.json`, `playmode-results.json` (per-case results) |
| Real Play sessions / V04 | **4 scripted cycles passed:** twice with both domain/scene reloads disabled, twice with normal reloads. Each entered Menu, started/paused and exited with Instance null/timeScale 1; distinct run IDs. Original Editor reload settings restored. | `play-sessions.json` |
| Config immutability | PASS, Gameplay.asset SHA-256 before/after real sessions identical: `6806f58c5028fbd67a4f729578dbf2f4ba79f52dcc2a7a16fbe2d21827d02d0c`. | `play-sessions.json` |
| Existing foundation / V01/V22 | PASS, extended validator and saved-scene inspection. Versions, renderer/assets, build scene/profile, package pins, asmdefs, preproduction and supplied/root ignore bytes retained; no missing metas or duplicate GUIDs. Ordinary player assembly graph contains only NeonTunnelRunner.Runtime, with no UnityEditor/Pipeline dependency. | `validator.json`, `foundation-audit.json`, `preservation.json`, `scene-assignments.json` |
| Final Editor state | Edit mode, clean saved Game, one correctly wired core pair, Instance cleared and timeScale 1. | `validator.json`, `console.json` |

Tests ran through the installed Pipeline `test run` command with assembly filters, `async_tests=true`, then `test status` until completed. The saved JSON contains real test verdicts, not inferred outcomes. Fixtures destroy their own objects and restore global timeScale; owner tests exercise the production manager rather than a separate state machine. The final Unity Console sample has zero errors and one expected duplicate-manager warning; the Pipeline's separate historical counters retain earlier messages. No player build, movement/input test, fuel-depletion simulation, persistence, UI or art verification was run or claimed.

Repeat commands with `TASK02_CLI=/home/alireza/.local/bin/unity.previous` and `TASK02_PROJECT` set to this repository:

```bash
"$TASK02_CLI" recompile --project-path "$TASK02_PROJECT" --format json
"$TASK02_CLI" command --caller plugin --skill unity-cli test run --mode editor --filter NeonTunnelRunner.EditModeTests --filter_type assembly --async_tests true --project-path "$TASK02_PROJECT" --format json
"$TASK02_CLI" command --caller plugin --skill unity-cli test status --project-path "$TASK02_PROJECT" --format json
# After EditMode completion, run the same commands with mode=playmode and filter=NeonTunnelRunner.PlayModeTests.
python3 Docs/Setup/Task02PlaySessions.py
```

No task 02 checks are blocked. R15/R18 now accept explicit terminal reasons; resource evaluation remains task 08. Menu input wiring, movement, pooling, world, scoring/persistence and presentation remain their assigned tasks. Ready for **Task 03 — New Input System and three-lane player**. Stop task 02 here.
