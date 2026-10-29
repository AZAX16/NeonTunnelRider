# 02 — GameManager and run lifecycle

## Task name and objective

Implement the required singleton and testable run/state ownership.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R12 (HW2 p.2): Begin with three lives and show them in UI. [Mandatory]
- R15 (HW2 p.2): Zero lives ends the run. [Mandatory]
- R27 (HW2 p.3): Singleton GameManager manages the four named states. [Mandatory]
- R46 (HW2 p.4): Pause menu: Resume, Restart, Main Menu. [Mandatory]

## Integration dependencies

Tasks01–01 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

Scripts/Core/{GameState,RunSession,GameManager,GameplayLoop}.cs; Scripts/Config/GameplayConfig.cs; Configs/Gameplay.asset; Tests/EditMode/GameStateTests.cs; Tests/PlayMode/RestartTests.cs; Game.unity; Docs/Verification/02-game-manager.md.

## Expected behavior and required architecture

Implement all four states and the exact transition table in architecture doc03. GameManager.Instance is the sole singleton, scene-owned, without DontDestroyOnLoad. RunSession owns lives/fuel/time/distance/score/coins/best/speed/run ID, with immutable snapshot access and typed Damaged/Collected/Snapshot events. Create config defaults from doc02. Keep mutation methods internal to gameplay. GameManager performs idempotent StartRun/Pause/Resume/Restart/ReturnToMenu/EndRun/Quit; GameplayLoop owns the fixed-tick entry but does not reference nonexistent future classes. Add composition references only as later tasks introduce them. Reset has explicit ordered hooks; no generic event bus or placeholder NotImplementedException. Time freezes by state gate and pause timeScale; OnDestroy restores scale and clears owned singleton. Introduce the terminal guard and reason priority, with completion in task08.

## Inspector, scene and prefab setup

Put one GameManager and GameplayLoop on Systems; link GameplayConfig. Scene starts in Menu. Use an Editor-only context command for early Start/Pause/Restart checks, not a new gameplay input path. Add serialized fields incrementally as their components exist. Ensure configuration assets are never modified at runtime.

## Edge cases

Repeated commands; illegal transitions; duplicate manager; restart while paused; exiting Play Mode at timeScale0; domain reload settings; no future UI subscribers. Preserve working bootstrap.

## Tests and acceptance criteria

V04,V12,V13 core subset: enumerate legal/illegal transitions; duplicate commands emit one event; initial lives3/fuel100/score0; pause freezes tick; reset preserves best while resetting other values; teardown restores timeScale. Mark resource/visual reset checks pending later tasks.

## Definition of Done

State tests pass, one singleton survives valid initialization, reset and terminal contracts compile, scene references assigned and verification distinguishes completed core tests from future integration.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No traffic, full UI, VFX/audio, persistence implementation, or additional singletons.
