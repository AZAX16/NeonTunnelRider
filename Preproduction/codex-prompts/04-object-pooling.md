# 04 — Bounded pooling infrastructure

## Task name and objective

Provide reusable lifecycle-safe pools before any endless spawning.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R08 (HW2 p.2): Procedural endless road. [Mandatory]
- R09 (HW2 p.2): Dynamically place segments ahead and recycle behind. [Mandatory]
- R22 (HW2 p.3): Pool road segments, obstacle cars, coins and fuel cans. [Mandatory]
- R23 (HW2 p.3): No repeated Instantiate/Destroy during normal play. [Mandatory]

## Integration dependencies

Tasks01–03 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

Scripts/Pooling/{IPoolable,PoolKey,PooledObject,PoolService}.cs; Scripts/Config/PoolConfig.cs; Configs/Pools.asset; Core/GameManager.cs; Tests/PlayMode/PoolLifecycleTests.cs; Game.unity; Docs/Verification/04-object-pooling.md.

## Expected behavior and required architecture

Implement prewarm-before-Playing, per-key inactive stacks and active sets, TryRent/Return/ReturnAll, counters and monotonically increasing activation IDs. SpawnContext carries key, pose, lane, absolute distance, seed/speed and spawn sequence as appropriate. Public pool callbacks are explicit and idempotent. Rent sets pose/reset data before enabling; return unregisters safely, clears transient state and disables. OnDisable must not recursively return to the pool. Instantiate only at bootstrap; no Playing growth. Use initial capacity guidance from doc03, filling prefab entries only when assets exist. Invalid required entries prevent Start with a readable error; a test dummy can validate pool behavior outside normal gameplay. Pool misses omit optional content; required road/fuel policy is implemented by callers.

## Inspector, scene and prefab setup

Create Pools inactive storage root; link PoolConfig to PoolService and GameManager bootstrap. Use typed keys, no Resources.Load per spawn. Configure dummy test prefab in test fixtures rather than shipping it as a required asset. Make the validator report duplicate keys, zero capacities and missing prefabs.

## Edge cases

Double return; return from callback; stale activation ID; unregistered object; ReturnAll during reset; pool exhausted; missing key; reuse of particles/materials later. Do not allow silent allocation to hide insufficient capacity.

## Tests and acceptance criteria

V06:1,000 rents/returns retain stable instance IDs and correct flags; capacity never grows during Playing; double returns cannot duplicate entries; forced exhaustion returns false/diagnostic;50 reset cycles recover every object. Record bootstrap allocation separately from gameplay.

## Definition of Done

Reusable pool API and lifecycle tests pass; all forthcoming recurring systems can rent/return without implementing their own Instantiate/Destroy loops.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No road generator, interactable behavior, runtime auto-expansion, Addressables or generalized resource framework.
