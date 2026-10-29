# 09 — Difficulty, score and persistent high score

## Task name and objective

Progress challenge and scoring without sacrificing fairness.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R07 (HW2 p.2): Automatic continuous forward driving. [Mandatory]
- R20 (HW2 p.2): Collect coins. [Mandatory]
- R21 (HW2 p.3): Difficulty increases with elapsed time/distance through both speed and obstacle density. [Mandatory]
- R48 (HW2 p.4): Live HUD shows speed, fuel bar, lives, score and best. [Mandatory]
- R49 (HW2 p.4): Persist high score using PlayerPrefs across exits. [Mandatory]

## Integration dependencies

Tasks01–08 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

World/ProgressionSystem.cs; Persistence/HighScoreStore.cs; Core/{RunSession,GameManager,GameplayLoop}.cs; Config/GameplayConfig.cs; World/SpawnDirector.cs; Tests/EditMode/ProgressionTests.cs; Tests/PlayMode/PersistenceTests.cs; Docs/Verification/09-progression.md.

## Expected behavior and required architecture

d=clamp01(max(elapsed/180,distance/6000)); cruise speed=Lerp(22,50,d); expected non-safe slot occupancy=Lerp(.25,.8,d). Evaluate at tick start, update elapsed/distance from actual logical travel, set actual speed independently from configured target. Score=floor(double distance)+100*coins; clamp to int.MaxValue safely. Density increases statistically for seeded plans; do not claim every random neighboring block is strictly denser. Keep corridor and moving envelopes intact at cap. HighScoreStore uses PlayerPrefs keyNTR.HighScore.v1, Load clamps negative values, best=max(old,current), dirty flag and lifecycle Flush. Save on end/menu/quit/application pause, not each tick. Paused/GameOver actual speed0 while target cruise remains for resume.

## Inspector, scene and prefab setup

Assign config and store ownership via GameManager; no extra singleton. Hook score changes into existing snapshots. Configure development seed and display/log it. Use isolated PlayerPrefs test key; preserve real best. No UI duplication; fields bind in14.

## Edge cases

Very long run/int overflow, missing/corrupt-negative preference, repeated save boundary, simultaneous reward/death, paused progress, distance after rebase, random density fluctuations.

## Tests and acceptance criteria

V11,V12,V21: evaluate curve endpoints/monotonicity; compare seeded occupancy over sufficient blocks; verify distance+coins formula; restart preserves best; quit standalone process and relaunch to verify persistence; no Save every frame; safe route holds at max density/speed.

## Definition of Done

Difficulty and best-score persistence are implemented and tested; all core gameplay complete with documented baseline tuning and no unsafe patterns.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No cloud leaderboard, user accounts, achievements, unlock systems or unlimited speed growth.
