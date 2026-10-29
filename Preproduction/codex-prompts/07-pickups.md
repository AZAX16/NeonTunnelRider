# 07 — Coins and fuel pickups

## Task name and objective

Complete the required four-type hierarchy with pooled rewards.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R17 (HW2 p.2): Fuel-can pickups restore fuel. [Mandatory]
- R20 (HW2 p.2): Collect coins. [Mandatory]
- R22 (HW2 p.3): Pool road segments, obstacle cars, coins and fuel cans. [Mandatory]
- R25 (HW2 p.3): Include both collectible categories. [Mandatory]
- R26 (HW2 p.3): All four categories inherit a shared abstract Interactable and use virtual/override methods. [Mandatory]
- R39 (HW2 p.4): Collectible disappearance effects. [Mandatory within graded art section]

## Integration dependencies

Tasks01–06 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

Scripts/Interactables/{Coin,FuelCan}.cs; Scripts/World/ProgressionSystem.cs; World/SpawnDirector.cs; Core/RunSession.cs; Player/PlayerSurvival.cs; Prefabs/Interactables/{Coin,FuelCan}.prefab; PoolConfig; Tests/PlayMode/PickupTests.cs; Docs/Verification/07-pickups.md.

## Expected behavior and required architecture

Coin and FuelCan inherit abstract Interactable and each overrides OnInteract. Coin awards100 points through ProgressionSystem.AddCoin; introduce score recompute now, with full difficulty/persistence in task09. Fuel calls AddFuel(30), clamped0..100. Mark consumed before reward/event, then defer return. Full-fuel pickup is still consumed with one feedback event. Typed Collected payload stores kind/position, not pooled object reference. Add child-only spin/bob reset; roots/bounds do not animate. Reserve one reachable fuel can every≤220m on protected route or clear buffer, with no traffic overlap. Coins may offer optional alternative lanes but not imply an unavoidable route. Prewarm160 coins/24 fuel; reserve fuel capacity.

## Inspector, scene and prefab setup

Use simple gold ring and mint-can geometry, logical pickup half extents.5/.6/.5 and matching trigger boxes. Add both pool entries. Wire existing sensor to all four category types; do not write a separate pickup collision path. Create temporary readable reward indication using existing debug/inspector snapshot until VFX/UI tasks.

## Edge cases

Repeated contact events, same instance rented again, fuel already full, pickup reached on final-fuel tick (completed in08), pickup during grace, forced fuel pool exhaustion, overlapping collectible rows.

## Tests and acceptance criteria

V07,V10 subset,V21: duplicate contact awards once; re-rent restores reward; full fuel remains100; coin count/score consistent; fuel interval bound checked across1,000 seeds; pickups reachable through validator and never inside obstacle bounds.

## Definition of Done

All four concrete types derive Interactable; coin and fuel reward/recycle correctly; supply guarantee exists; typed events ready for later VFX/audio without coupling.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No inventory, shop, magnet power-up, per-pickup Instantiate/Destroy, particle asset production or full HUD.
