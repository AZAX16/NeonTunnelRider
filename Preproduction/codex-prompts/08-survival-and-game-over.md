# 08 — Lives, fuel, feedback and Game Over

## Task name and objective

Finish the complete graybox survival loop and terminal behavior.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R12 (HW2 p.2): Begin with three lives and show them in UI. [Mandatory]
- R13 (HW2 p.2): Obstacle collision removes one life. [Mandatory]
- R14 (HW2 p.2): Collision has visual feedback. [Mandatory; examples are alternatives]
- R15 (HW2 p.2): Zero lives ends the run. [Mandatory]
- R16 (HW2 p.2): Fuel decreases over time. [Mandatory]
- R17 (HW2 p.2): Fuel-can pickups restore fuel. [Mandatory]
- R18 (HW2 p.3): Zero fuel ends the run. [Mandatory]

## Integration dependencies

Tasks01–07 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

Player/PlayerSurvival.cs; Core/{RunSession,GameManager,GameplayLoop}.cs; Presentation/VehicleVisuals.cs (damage flash only); Config/GameplayConfig.cs; Player prefab; Tests/EditMode/SurvivalTests.cs; Tests/PlayMode/GameOverTests.cs; Docs/Verification/08-survival.md.

## Expected behavior and required architecture

Drain2 fuel units per playing second from100; retain refill30 and3 starting lives. Complete grace timing, damage flash and terminal resolution. Process sorted contacts first, then fuel drain, then one final cause: Lives if lives≤0, otherwise Fuel if fuel≤0. A same-tick refill can prevent depletion; damage during grace does not create feedback or delayed damage. EndRun freezes simulation, captures final score/reason and clears lane queue. Task09 supplies persistent best storage. Implement a cached material property block for short visible damage flash, restoring original visual state on reset. No physics root tint/state leaks into pooled objects.

## Inspector, scene and prefab setup

Assign Survival references and flash renderer; expose fuel/lives/reason in inspector/debug view during graybox. Wire GameplayLoop sequence from doc03; no immediate state transition inside collision iteration. Keep visual root separate from logical body. Provide fixture-only forcing of terminal resources rather than shipping cheat key polling.

## Edge cases

Lives and fuel reach0 same tick; refill at depletion boundary; paused grace/fuel; negative delta; repeated EndRun; restart from pause/death; late events after cleanup; damage on reused obstacle.

## Tests and acceptance criteria

V09,V10,V12,V13:100 fuel drains in50 playing seconds;95+30 clamps100; exactly3 accepted hits end run; test both terminal causes and tie priority; pause10 wall seconds changes no resources/timers;50 resets return initial values/flags. Visual flash observed; later sparks are still pending12.

## Definition of Done

A full graybox session can lose by lives or fuel and restart cleanly; no duplicate end events; visual collision feedback exists and core survival tests pass.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No score persistence, new resources, game balancing beyond baseline, full menus or final VFX.
