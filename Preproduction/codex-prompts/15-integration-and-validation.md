# 15 — Full integration, fairness and performance audit

## Task name and objective

Close implementation gaps with reproducible evidence for every requirement.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R01 (HW2 p.1): Credit collaborators and exact external source addresses. [Mandatory]
- R02 (HW2 p.1): Use the exact ZIP naming pattern. [Mandatory]
- R03 (HW2 p.1): Upload the final ZIP to the course system. [Mandatory]
- R04 (HW2 p.1): Student is responsible for file integrity; corrupted files receive no marks; download and check after upload. [Mandatory; explicit zero-mark risk]
- R05 (HW2 p.2): Implement a 3D endless car game in Unity. [Mandatory]
- R06 (HW2 p.2): Exactly three lanes and a third-person camera. [Mandatory]
- R07 (HW2 p.2): Automatic continuous forward driving. [Mandatory]
- R08 (HW2 p.2): Procedural endless road. [Mandatory]
- R09 (HW2 p.2): Dynamically place segments ahead and recycle behind. [Mandatory]
- R10 (HW2 p.2): A/D and left/right arrows change lanes. [Mandatory]
- R11 (HW2 p.2): All input exclusively uses the New Input System and action maps; legacy implementation gets no marks in this section. [Mandatory; explicit section zero-mark risk]
- R12 (HW2 p.2): Begin with three lives and show them in UI. [Mandatory]
- R13 (HW2 p.2): Obstacle collision removes one life. [Mandatory]
- R14 (HW2 p.2): Collision has visual feedback. [Mandatory; examples are alternatives]
- R15 (HW2 p.2): Zero lives ends the run. [Mandatory]
- R16 (HW2 p.2): Fuel decreases over time. [Mandatory]
- R17 (HW2 p.2): Fuel-can pickups restore fuel. [Mandatory]
- R18 (HW2 p.3): Zero fuel ends the run. [Mandatory]
- R19 (HW2 p.3): Display remaining fuel as a bar. [Mandatory]
- R20 (HW2 p.2): Collect coins. [Mandatory]
- R21 (HW2 p.3): Difficulty increases with elapsed time/distance through both speed and obstacle density. [Mandatory]
- R22 (HW2 p.3): Pool road segments, obstacle cars, coins and fuel cans. [Mandatory]
- R23 (HW2 p.3): No repeated Instantiate/Destroy during normal play. [Mandatory]
- R24 (HW2 p.3): Include stationary and moving obstacle vehicles. [Mandatory]
- R25 (HW2 p.3): Include both collectible categories. [Mandatory]
- R26 (HW2 p.3): All four categories inherit a shared abstract Interactable and use virtual/override methods. [Mandatory]
- R27 (HW2 p.3): Singleton GameManager manages the four named states. [Mandatory]
- R28 (HW2 p.3): Artistic and visual quality is assessed. [Explicitly graded; no numeric rubric supplied]
- R29 (HW2 p.3): Nighttime atmosphere. [Mandatory within graded art section]
- R30 (HW2 p.3): Starry or cloudy night skybox. [Mandatory within graded art section]
- R31 (HW2 p.3): Dark surroundings and suitable road illumination. [Mandatory within graded art section]
- R32 (HW2 p.3): Player has headlights. [Mandatory within graded art section]
- R33 (HW2 p.3): Player has neon underglow. [Mandatory within graded art section]
- R34 (HW2 p.3): Underglow intensity and glow increase with actual vehicle speed. [Mandatory within graded art section]
- R35 (HW2 p.3): Street lighting is recommended. [Recommended, not stated mandatory]
- R36 (HW2 p.3): Periodically/randomly launch colorful sky fireworks. [Mandatory within graded art section]
- R37 (HW2 p.4): Engine exhaust smoke/fire particles. [Mandatory within graded art section]
- R38 (HW2 p.4): Collision spark particles. [Mandatory within graded art section]
- R39 (HW2 p.4): Collectible disappearance effects. [Mandatory within graded art section]
- R40 (HW2 p.4): Bloom for neon and fireworks. [Mandatory; pipeline wording requires interpretation]
- R41 (HW2 p.4): Background music. [Mandatory]
- R42 (HW2 p.4): Vehicle engine audio. [Mandatory]
- R43 (HW2 p.4): Collision sound. [Mandatory]
- R44 (HW2 p.4): Coin and fuel pickup sounds. [Mandatory]
- R45 (HW2 p.4): Main menu: Start and Quit. [Mandatory]
- R46 (HW2 p.4): Pause menu: Resume, Restart, Main Menu. [Mandatory]
- R47 (HW2 p.4): Game Over: score, high score, Restart. [Mandatory]
- R48 (HW2 p.4): Live HUD shows speed, fuel bar, lives, score and best. [Mandatory]
- R49 (HW2 p.4): Persist high score using PlayerPrefs across exits. [Mandatory]
- R50 (HW2 p.5): Unity project must be in a Git repository. [Mandatory]
- R51 (HW2 p.5): Include a text file containing repository URL in submission ZIP. [Mandatory]
- R52 (HW2 p.5): Use the exact supplied .gitignore; exclude bulky Library and other generated folders. [Mandatory]
- R53 (HW2 p.5): Include executable game build. [Mandatory]
- R54 (HW2 p.5): Short implementation report explaining structure with real final-game screenshots. [Mandatory]

## Integration dependencies

Tasks01–14 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

All existing systems only where a failing check requires a fix; Tests/EditMode and PlayMode suites; Editor/ProjectSetupValidator.cs; Docs/Verification/15-integration.md; Evidence/requirement-evidence.csv; test XML/logs/profiler records.

## Expected behavior and required architecture

Inspect complete repo and existing verification notes first. Execute document09 validation cases V01–V25 where applicable; mark delivery-only cases pending16. Cross-check all54 R rows and U constraints. Audit new-only input, actual virtual overrides for all4 Interactables, exactly1 singleton GM, pool use for all recurring objects, correct reset and no GameManager god-class creep. Run1,000 seeds at start/mid/max difficulty over10km with trajectory-aware corridor checks; save failures with seed/config/block. Run100km accelerated segment check and30-minute visual soak. Profile10minutes at maximum density after warmup on recorded Linux hardware/resolution. Target60fps/p95≤16.7ms at1080p, zero steady-state gameplay-script GC allocations, memory stable within10%. These are project targets, not invented course marks.

## Inspector, scene and prefab setup

Use Test Runner or locally discovered Pipeline test commands and poll to completion; keep Editor Console clean. Save standalone profiler captures where needed; do not report Editor frame times as player performance. Test Linux graphics API actually used. Make only fixes tied to observed failures; rerun affected suites plus integration smoke test.

## Edge cases

CLI disconnected, Unity absent, tests skipped, flaky random runs, process exit persistence, hidden pool exhaustion, camera rebase after long run, terminal event ordering. Do not call a validation PASS just because code compiles or a screenshot exists.

## Tests and acceptance criteria

Record each V case PASS/FAIL/BLOCKED/NOT RUN with command or manual steps, timestamp, commit, result and evidence path. For every R row link implementation files and evidence. All functional mandatory rows must pass before16 release; final archive/upload gates remain separately pending.

## Definition of Done

No known critical functional failures, exact unresolved items named, fair patterns and reset stable, performance measured honestly, requirement evidence register complete enough for delivery review.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No feature expansion, unexplained refactor, silent test deletion, fabricated performance results or premature submission claims.
