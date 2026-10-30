# Task 03 — New Input System and three-lane player

Verified 2026-10-30 (Asia/Tehran), starting from clean `main` at `5129ee8`. Repository instructions, the environment and task 01/02 verification notes, existing runtime APIs, and the requested design/architecture/structure/acceptance documents were inspected before editing. No subsequent user changes were present. The installed official `unity:unity-cli` skill was used. Editor **6000.6.3f1**, URP **17.6.0**, Input System **1.20.0**, Test Framework **1.8.0**, uGUI/TMP **2.6.0**, Pipeline **0.9.1-exp.1** and plugin **0.1.8-beta** remain unchanged. Commands used `/home/alireza/.local/bin/unity.previous`, the installed CLI **1.0.0-beta.13**; the pre-existing newer PATH executable was not changed.

## Implementation and changed files

- `Scripts/Input/InputRouter.cs` owns a private runtime clone of the explicitly assigned action asset. Callbacks collect each input update, then `InputSystem.onAfterUpdate` resolves simultaneous opposites to zero. One pending batch is overwritten by the latest batch and consumed once by GameplayLoop. Pause wins over lane input in the same update. State changes clear buffers and apply maps directly through the existing lifecycle transaction. There is no additional state machine, input polling, generated input wrapper, or PlayerInput component.
- `Scripts/Player/PlayerController.cs` owns lane/transition state, one queued adjacent command and authoritative previous/current logical positions. LaneIndex denotes the transition's destination. Invalid outer-edge presses are ignored; the latest valid queued direction replaces the previous direction. The current transition finishes before the queued transition starts, using remaining delta time when applicable. Interpolation is linear over 0.28 seconds, with a small completion tolerance for the existing rational timestep. Forward travel is 22 m/s along +Z. Height, root rotation and scale stay fixed. Ordinary physics movement uses kinematic Rigidbody.MovePosition; reset/freeze synchronizes the body to the logical pose.
- `Scripts/Core/GameplayLoop.cs` retains the only FixedUpdate. Its order is consume command → predict player pose → commit Rigidbody position → record elapsed/forward distance/measured speed once → publish one snapshot. Step checks Playing and ownership explicitly, independent of timeScale. RunSession retains double-precision forward distance; the player's local poses use Unity Vector3. No fuel, score or difficulty ticking was added.
- `GameManager.cs` has only lifecycle integration changes: initialize Menu before the input map policy, initialize the owner before other scene components, apply state to the loop within each accepted transaction, and shut down input with the owner. `RunSession.cs` gains an internal controlled movement update. Existing snapshot ownership, singleton/duplicate handling, terminal results, best score and reset semantics remain intact.
- `GameplayConfig.cs` and `Configs/Gameplay.asset` add private serialized root height and logical box half extents with read-only getters. Existing lane width 3.2, duration 0.28 and initial speed 22 are reused. Runtime code never edits the config or source action asset.
- Added `Input/Runner.inputactions`, `Prefabs/Player/PlayerHovercar.prefab`, `Tests/EditMode/LaneMovementTests.cs`, `Tests/PlayMode/InputBindingTests.cs` and `PlayerLifecycleTests.cs`, with Unity-generated metas. Existing GameStateTests/RestartTests fixtures now supply the real input/player dependencies; the old elapsed-only distance assertion now expects forward travel.
- Extended the existing Editor validator to inspect input bindings, explicit ownership, baseline movement settings, prefab/body/box, separate Visual/CameraAnchor, camera attachment and conflicting template wiring. The existing Editor asmdef adds Unity.InputSystem; the existing PlayMode asmdef adds Unity.InputSystem and Unity.InputSystem.TestFramework. Runtime and EditMode asmdefs are unchanged. No assemblies or packages were created/reinstalled.
- `Scenes/Game.unity` contains the integration below. `ProjectSettings/EditorBuildSettings.asset` removes only the template project-wide input registration; the Game build scene remains registered. `CREDITS.md` records the primitive hovercar, input package source use and AI assistance. Editor setup/inspection and repeatable visual/session recipes are under `Docs/Setup/Task03*.cs` / `Task03PlaySessions.py`, outside Assets.

## Input asset and policy

Runner GUID: `b20f5e96314e6f39896d51fa432a895a`. Gameplay and System buttons use `Press(behavior=0)` with initial-state checks disabled. Holding a key produces one request. Disabling/re-enabling Gameplay does not convert a held key into a new press; tests exercise pause and restart while a key remains held. System remains enabled across Playing/Paused, so a held Escape does not immediately toggle back.

| Map/action | Type | Bindings |
| --- | --- | --- |
| Gameplay/Left | Button, press-only | `<Keyboard>/a`, `<Keyboard>/leftArrow` |
| Gameplay/Right | Button, press-only | `<Keyboard>/d`, `<Keyboard>/rightArrow` |
| System/Pause | Button, press-only | `<Keyboard>/escape` |
| UI/Navigate | PassThrough Vector2 | 2DVector composite: up/down/left/right arrows |
| UI/Submit | Button | `<Keyboard>/enter` |
| UI/Cancel | Button | `<Keyboard>/backspace` |
| UI/Point | PassThrough Vector2 | `<Mouse>/position` |
| UI/Click | PassThrough Button | `<Mouse>/leftButton` |
| UI/ScrollWheel | PassThrough Vector2 | `<Mouse>/scroll` |

| State | Gameplay | System | UI |
| --- | --- | --- | --- |
| Playing | enabled | enabled | disabled |
| Paused | disabled | enabled | enabled |
| Menu / GameOver | disabled | disabled | enabled |

Escape is owned exclusively by System/Pause. The UI map is prepared for the later InputSystemUIInputModule; no menus or UI module were implemented. The untouched template `Assets/InputSystem_Actions.inputactions` remains in the repository, but its project-wide automatic activation is unregistered. Runner's runtime clone alone is enabled by InputRouter.

## Scene, prefab and Inspector assignments

Game retains GUID `2ff1f62cd218111b6af6f6ff50581601`; Gameplay.asset retains `50e71278e264ab6f3b9bd533f16b2e30`. PlayerHovercar prefab GUID is `3fe156509481777c8b2a4c1d32d2e622`.

| Object/component | Assignment |
| --- | --- |
| Systems / GameManager | Existing Gameplay.asset and GameplayLoop; seed 12345 / initial best 0 unchanged |
| Systems / GameplayLoop | Existing GameManager; InputRouter on Systems; PlayerController on scene Player |
| Systems / InputRouter | GameManager on Systems; Runner.inputactions |
| Player | One connected PlayerHovercar prefab instance, root `(0, 0.6, 0)`, identity rotation, unit scale |
| Player / PlayerController | Same Gameplay.asset; root Rigidbody; CameraAnchor child |
| Player / Rigidbody | Kinematic; gravity off; FreezeRotation and FreezePositionY; interpolation None |
| Player / BoxCollider | Center zero; size `(1.7, 0.9, 3.6)`, half extents `(0.85, 0.45, 1.8)` |
| Player / Visual | Separate child with primitive Body and Canopy meshes; existing ReferenceMarking/ReferenceRoad materials; no extra colliders |
| Player / CameraAnchor | Separate stable child, local `(0, 4.4, -10)`, rotation `(15, 0, 0)` |
| Existing Main Camera | Parent CameraAnchor; local position zero / rotation identity; initial world `(0, 5, -10)`; original camera, URP, post-processing and AudioListener settings retained |
| World / ReferenceRoad | Existing static lane and marking geometry extended from 60 to 300 metres, centered at z=150; original widths/materials retained |

The scene starts in Menu. To try it, enter Play Mode and use Systems/GameManager's **Lifecycle/Start Run** context command, then A/D or arrows and Escape. Existing context commands also support Restart/Main Menu/End Run; menu implementation remains later work. Restart resets lane, transition progress, queued/pending input and both logical poses together. Pause clears pending/queued input while retaining the active transition and position. Camera reset follows the stable parent automatically.

Final diff review found URP's automatically added light metadata. It was removed through the Editor before the final save; the original light, camera, AudioListener and Global Volume component blocks are byte-identical to task 02.

## Executed checks and evidence

All paths below are under `Evidence/Task03/`.

| Check / requirements | Executed result | Evidence |
| --- | --- | --- |
| Compilation / V01 | PASS, final source Editor recompilation: 0 errors, 0 warnings, compilationFailed=false | `compile.json`, `foundation-audit.json` |
| EditMode / R06,R07; V03 plus task 02 regression | **53 passed, 0 failed/skipped**: all 39 task 02 cases plus 14 lane cases. Covers linear intermediate positions over six step sizes, exact lane endpoints, forward travel over four step sizes, bounds, latest valid queued command, remaining timestep across adjacent transitions, clear/reset and both poses. | `editmode-results.xml`, `editmode-cli.json`, `test-summary.json` |
| PlayMode / R06,R07,R10,R11,R27; V02/V03/V04/V12/V13 scope | **27 passed, 0 failed/skipped**: all 5 task 02 tests plus 15 input cases and 7 player lifecycle cases. Real InputTestFixture keyboards exercise all four bindings, hold, same-update cancellation, one pending batch, Escape, map policy, held keys on resume/restart, 20 router enable cycles, pause in transition, progression once per step, state gating, best-preserving reset, Rigidbody/anchor behavior. | `playmode-results.xml`, `playmode-cli.json`; source Editor independently passed all 27 in `playmode-results.json` / `playmode-start.json` |
| Real Play Mode entry/exit / R27,V04 | **4 cycles passed**, twice with both reloads disabled and twice with normal reloads. Each entered Menu at the center, delivered exactly one A action callback, moved through the actual FixedUpdate loop, accepted exactly two Escape toggles, then exited while paused with Instance null/timeScale 1. Distinct run IDs; original reload settings restored. | `play-sessions.json`; recipe `Docs/Setup/Task03PlaySessions.py` |
| Configuration immutability | PASS; before/after real-session SHA-256 both `a90e5b2a51873e65c9dae592129973e2188169ea97856e950d2c4f114efa9203`; source actions remain disabled | `play-sessions.json`, `validator.json` |
| Saved scene and foundation / V01,V22 | PASS extended validator, no missing scripts/references, one correctly wired owner/loop/router/player; normal player assembly graph contains only NeonTunnelRunner.Runtime and its existing runtime references | `validator.json`, `foundation-audit.json`, `scene-assignments.json` |
| Preservation / R11,R50,R52 | PASS: 136 protected files and all 82 original metas byte-identical to 5129ee8; no duplicate GUIDs or missing Unity-importable metas. Packages, URP/renderers, Build Profile, TimeManager and preproduction retained. Root/supplied .gitignore hashes match; check-ignore excludes Library/Temp/Obj/Builds. | `preservation.json` |
| Rear-camera visual / R06,R07 | Captured and inspected a genuine 1280×720 Game-view screenshot sequence, following native input and FixedUpdate movement: center z=64.68, left z=80.96, right z=103.84. Camera follows behind/above; car remains visible in all three lanes. Rightward travel from left used two separate adjacent action presses. | `01-center.png`, `02-left.png`, `03-right.png`; corresponding `.json` pose/tick/action-callback records and `-capture.json` |
| Final Editor state | Edit mode, saved scene isDirty=false, Instance null, timeScale 1, original reload options restored; current Console sample has 0 errors and 0 warnings | `validator.json`, `console.json` |

The CLI tests ran against an isolated copy of Assets/Packages/ProjectSettings at `/tmp/neon-task03-verification`, without the source Library or user state. `verification-copy.json` records the input hashes and the scene/validator refresh before the final full test pass. The final source matches that tested snapshot. The final validator was also recompiled and executed in the source Editor. Tests clean up fixtures/devices and restore global time/input state. No alternate production state machine was introduced for testing.

### Tooling attempts and their resolution

The initial Pipeline asynchronous EditMode discovery stalled; cancellation did not stop its eventual test execution, so its cancelled collector record is **not** used as passing evidence (`pipeline-attempt-editmode-*.json`). A test-activity probe timed out; the Editor remained responsive and out of Safe Mode. The first isolated CLI test attempt reached its 360-second timeout during cold import. After that import, the explicit CLI rerun completed and produced the passing NUnit reports above. No package upgrade or replacement Editor was used. The source Pipeline PlayMode run also completed normally.

An initial InputActionAsset.Dispose assumption caused a compile error and was removed before successful compilation/tests; InputActionAsset destruction performs its own cleanup. A newer-plugin `editor.dialog` probe is unavailable in pinned Pipeline 0.9.1. The Pipeline's historical console buffer retains these two resolved diagnostics and an expected duplicate-manager test warning; its final ground-truth Console counts are zero errors/warnings. No external bug report or logs were sent.

Early screenshot-helper input was discarded by unfocused Game-view handling. The final Editor-only helper temporarily uses IgnoreFocus plus AllDeviceInputAlwaysGoesToGameView, injects Input System device events, waits for native updates, and restores the original policies/devices in finally. Production input/settings are unchanged; no direct player movement is used to obtain lane screenshots.

## Scope and remaining checks

Task 03 has no blocked checks. The road is finite: 300 metres gives roughly **13.6 seconds** from the start at 22 m/s. The car can travel beyond its end; there is no endless generator or road-boundary gameplay yet. This is the requested short graybox demonstration. No player build, long visible soak, full UI input interaction, camera smoothing or broader future-system acceptance was run or claimed. V02's menu pointer/navigation interaction remains task 14; full camera acceptance remains task 10 and integration/soak checks remain task 15.

The existing native TimeManager serialization was preserved without timestep tuning. Native Unity scene serialization adds blank `m_Name: ` values; a generic whitespace diff check flags those generated lines. Code and documentation have no trailing whitespace.

Reproduce with the pinned CLI (set `TASK03_PROJECT` to this repository and `TASK03_TEST_PROJECT` to an isolated current copy of Assets/Packages/ProjectSettings):

```bash
TASK03_CLI=/home/alireza/.local/bin/unity.previous
"$TASK03_CLI" recompile --project-path "$TASK03_PROJECT" --format json
"$TASK03_CLI" command --caller plugin --skill unity-cli script run --file Docs/Setup/Task03Setup.cs --entry NeonTunnelRunner.Task03Setup.Inspect --project-path "$TASK03_PROJECT" --format json
"$TASK03_CLI" test "$TASK03_TEST_PROJECT" --editor-version 6000.6.3f1 --mode EditMode --filter NeonTunnelRunner --output /tmp/task03-editmode.xml --timeout 360 --format json -- -nographics
"$TASK03_CLI" test "$TASK03_TEST_PROJECT" --editor-version 6000.6.3f1 --mode PlayMode --filter NeonTunnelRunner --output /tmp/task03-playmode.xml --timeout 180 --format json -- -nographics
python3 Docs/Setup/Task03PlaySessions.py
```

Ready for **Task 04 — Object pooling**. Stop task 03 here.
