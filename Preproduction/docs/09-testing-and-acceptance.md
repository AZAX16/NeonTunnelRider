# Testing and acceptance

These are planned tests, not executed Unity results. Use EditMode for pure state/math/pattern logic, PlayMode for component integration and pools, and a standalone player for performance, audio and persistence. A visual screenshot cannot establish a passing automated test.

| ID | Method | Procedure | Pass criteria | Tasks |
| --- | --- | --- | --- | --- |
| V01 | Manual / editor | Create and reopen Unity URP project; verify Console and standalone 3D graybox. | No compile errors; recorded Editor/package versions and working scene. | 01,15 |
| V02 | EditMode + PlayMode + manual | InputTestFixture presses/releases A,D,LeftArrow,RightArrow; test System/Pause and UI mouse, navigation, Submit, Cancel at paused timeScale; search sources/settings. | Every binding changes exactly one lane per press; legacy Input/GetKey/GetAxis/OnGUI input absent; New-only setting and all actions/maps wired. | 03,14,15 |
| V03 | EditMode + PlayMode | 100 alternating and repeated edge commands, simultaneous left/right, queued opposite input, hold key; run 60 playing seconds. | Indices always -1..1, target x -3.2/0/3.2; hold never auto-repeats; continuous forward progress only in Playing. | 03,15 |
| V04 | EditMode + PlayMode | Enumerate transition table, duplicate singleton and duplicate commands; exit Play Mode while paused. | Exactly one owner; illegal transitions no-op; state event once; Time.timeScale restored on destroy. | 02,15 |
| V05 | PlayMode + visual | Travel equivalent 100 km with deterministic stepping; inspect seams at ordinary and origin-shift ticks; 30-minute visible soak. | Adjacent endpoints differ <0.001 m; >=300 m road ahead, no visible gaps or holes; local positions bounded; distance monotonic. | 05,15 |
| V06 | PlayMode + Profiler | Rent/return each key 1,000 times; exhaust all pools; restart 50 times; inspect identity and counters. | Every instance reused, clean flags/colliders/particles; no double-return; no Playing Instantiate/Destroy; bounded active counts; exhaustion policy safe. | 04,15 |
| V07 | EditMode + PlayMode | Collect coin and fuel, stay overlapping and duplicate delivery, re-rent same object, collect while full. | Exactly +100 score / +30 fuel (clamped) per activation; pooling restores usability; feedback exactly once even when full. | 07,15 |
| V08 | EditMode + PlayMode | Inspect inheritance; run stationary and moving cars; test swept box at 22 and 50 m/s, lane crossing, thin pickup, initial overlap and two moving bodies. | All four types derive abstract Interactable and override virtual methods; stationary z constant before rebase; moving +8 m/s; every crossing detected once. | 06,15 |
| V09 | PlayMode + visual | Hit a car, overlap for >1 s, hit a new car within grace, then another after grace. | Lives 3→2 on first contact; consumed collision cannot damage twice; grace blocks new damage; next eligible collision removes exactly one; flash/sparks visible. | 08,15 |
| V10 | EditMode + PlayMode | Fuel drains 100→0 in 50 playing seconds without pickup; refuel at 95; test life zero, fuel zero, both same tick and fuel arrival on depletion tick. | Fuel max100; each death path one GameOver event; Lives reason wins ties; contact/refuel before end-of-tick drain allows rescue; no changes after death. | 08,15 |
| V11 | EditMode + standalone | Evaluate difficulty at time/distance endpoints; collect coins; set best, quit process and relaunch. | Speed and expected blocked slots nondecreasing to cap; score floor(distance)+100*coins; best=max(old,current), survives process exit; no Save every frame. | 09,15 |
| V12 | PlayMode + manual | Pause for 10 wall-clock seconds while in lane transition and near pickup; resume with buttons/Escape. | Distance, fuel, grace timers, traffic and fireworks unchanged; gameplay input queue cleared; UI responsive; no engine loop during pause. | 02,14,15 |
| V13 | PlayMode | 50 restarts from pause and GameOver; compare initial snapshot, active counts, callbacks and road layout. | Center lane, lives3, fuel100, score0, elapsed/distance0, speed22, no stale contacts/VFX/voices, same seed schedule; best preserved. | 02,15 |
| V14 | PlayMode + visual | Compare snapshot to five HUD fields at 720p/1080p and 4:3/21:9. | All values agree (speed×3.6 displayed in km/h); fuel fill=value/100; no clipping; updated on panel enable. | 14,15 |
| V15 | Manual / visual | Inspect rear camera in all lanes, tunnel entries, moving and paused; trigger rebase and damage. | Car and ≥120 m threat horizon visible; camera behind/above; no shake/rebase jump; roll does not alter collider. | 10,15 |
| V16 | Manual + PlayMode | Inspect sky/road/headlights; force measured speed22 then50, pause/resume, render fixed exposure comparison. | Night sky visible on open sections; lit road and headlights; underglow light/emission monotone with actual speed; no exposure washout; streetlights visible. | 11,15 |
| V17 | Manual + Profiler | Capture exhaust, spark, both pickup bursts, and open-sky launch/burst fireworks; toggle Bloom; pause mid-effect. | All ParticleSystems present; fireworks visible within first open section; Bloom visibly affects emissives without hiding lanes; effects freeze/resume and return to pool. | 12,15 |
| V18 | Manual / standalone | Listen to music, engine, collision, coin and fuel; pause/restart repeatedly, saturate voice pool. | Five cues audible, distinct pickups, engine speed response, no duplicated loops/clipping; correct mute/resume and bounded voices. | 13,15 |
| V19 | Manual / standalone | Exercise every required menu button with pointer and keyboard, including Quit in executable. | Main: Start/Quit; Pause: Resume/Restart/Main Menu; Over: score/best/Restart; correct focus and no double action on Escape. | 14,16 |
| V20 | Profiler / performance | Record hardware; profile 10 minutes at 1080p after warmup at maximum density, then 30-minute soak. | Target60 fps (p95≤16.7 ms) on declared PC; zero steady-state gameplay-script GC.Alloc; no pool growth; memory stable within10% after warmup. Targets are project budgets, not PDF grading thresholds. | 15 |
| V21 | Static audit + seed tests | Audit all R rows and test 1,000 seeds, maximum difficulty and 30-minute schedules using the corridor validator. | Every required row has evidence; every generated block has a reachable protected route; moving traffic cannot enter it; pickups reachable; same seed gives same schedule. | 15 |
| V22 | Repository / fresh clone | Compare assignment.gitignore hash with project root; run git check-ignore and fresh-clone import. | Exact supplied .gitignore preserved; Library/Temp/Obj/Builds ignored; Assets/meta, Packages and ProjectSettings present; no missing GUID references. | 01,16 |
| V23 | Manual / delivery | Extract submission to a new path on target OS; launch full build; open repository link from a second session. | Executable and its data/dependencies work; accessible exact Git URL provided; no Editor needed. | 16 |
| V24 | Document review | Inspect final short report, screenshots, sources and credits. | Architecture explained; real runtime images for each visual cluster; collaborators and exact source addresses credited; AI use disclosed honestly. | 16 |
| V25 | Archive / upload | Validate naming, zipfile.testzip CRC, extract, compare SHA-256 to re-downloaded course ZIP and run extracted build. | Three identity fields resolved, all delivery files present, no corruption; post-upload download verified by student. | 16 |

## Test implementation and evidence

Place EditMode tests in `Assets/NeonTunnelRunner/Tests/EditMode`, PlayMode tests in `Tests/PlayMode`; use Unity Test Framework assemblies with runtime references. Test fixtures must restore timeScale, destroy their fixture objects outside normal gameplay, restore isolated PlayerPrefs keys, and disable random decorators. Automated destruction in fixture teardown is not a runtime pooling violation.

Suggested suites: GameStateTests, LaneMovementTests, PoolLifecycleTests, RoadContinuityTests, SweptBoundsTests, InteractableTests, SurvivalTests, ProgressionTests, PatternSafetyTests, RestartTests and UiBindingTests. Use exact simulation stepping, not long wall-clock waits. Assert meaningful public behavior and edge cases, not copied implementation details.

For seeded generation run 1,000 seeds × 10 km at start/mid/cap difficulty. Check full moving-car swept footprints against the protected corridor and transition buffers. Save failing seed, block index, config hash and reason. Determinism applies to gameplay schedule with the same seed/config/tick rate, not bit-identical GPU particles across hardware.

Evidence lives in `Evidence/` in the implementation repo: TestResults XML, build commit, console summary, Profiler captures, hardware/resolution, annotated runtime screenshots, and requirement evidence CSV. Store large captures outside Git if necessary and link them. Every row starts NOT RUN; only record PASS after execution. Partial automation must list what still needs manual checking.

## Regression gates

- After 03: lane inputs and pause routing; after 05: no gaps and bounded pools.
- After 08: a complete graybox run with both death conditions and restart.
- After 09: seed fairness and persistence.
- After 14: every required UI/audio/VFX feature; after 15: full matrix and performance.
- After 16: fresh clone, extracted player, ZIP integrity, then student performs post-upload verification.
