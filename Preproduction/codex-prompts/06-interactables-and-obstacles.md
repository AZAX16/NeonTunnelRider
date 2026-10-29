# 06 — Interactable inheritance and traffic collisions

## Task name and objective

Add both obstacle types and reliable high-speed encounters.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R13 (HW2 p.2): Obstacle collision removes one life. [Mandatory]
- R22 (HW2 p.3): Pool road segments, obstacle cars, coins and fuel cans. [Mandatory]
- R24 (HW2 p.3): Include stationary and moving obstacle vehicles. [Mandatory]
- R26 (HW2 p.3): All four categories inherit a shared abstract Interactable and use virtual/override methods. [Mandatory]

## Integration dependencies

Tasks01–05 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

Scripts/Interactables/{Interactable,StationaryCar,MovingCar}.cs; Scripts/World/SweptBounds.cs; Scripts/Player/{InteractionSensor,PlayerSurvival}.cs; World/SpawnDirector.cs; GameplayLoop; Prefabs/Interactables/{StationaryCar,MovingCar}.prefab; PoolConfig; Tests/EditMode/SweptBoundsTests.cs; Tests/PlayMode/InteractableTests.cs; Docs/Verification/06-interactables.md.

## Expected behavior and required architecture

Create abstract Interactable : PooledObject with guarded TryInteract and protected virtual OnInteract; override it in StationaryCar and MovingCar, and virtual pool reset as needed. Define InteractionContext in the base file with typed access to current run/survival and reward services as introduced; avoid references to undefined future classes. Implement PlayerSurvival.TryDamage and AddFuel now so later pickups can integrate; task08 adds full fuel ticking and terminal resolution. Stationary cars have world speed0; moving cars advance+Z at8m/s, no lane changes. Spawn only validated block candidates, with full moving envelopes through despawn horizon. Recycle cars independently from road segments.

Implement relative swept AABB slab test from doc03 against logical start/end poses, including both movers and initial overlap. Reuse contact buffers, sort deterministic TOI/category/spawn order and defer pool returns. Root colliders mirror bounds but no duplicate OnTrigger damage path. Mark encounter once per activation even during grace; accepted damage costs1 and grants1 playing second grace.

## Inspector, scene and prefab setup

Create distinct orange/red graybox car prefabs, BoxColliders and moving kinematic Rigidbody; scale1/rotation0 roots. Register active interactables only on rent; remove on return. Prewarm64 stationary/32 moving. Wire sensor/survival/loop references and snapshot events. Add debug seed/block log and collider gizmos.

## Edge cases

50m/s player, moving targets, thin boxes, lane transitions, exact grazing, zero relative axis velocity, two contacts in same tick, persistent overlap, rebase and reused activation IDs. Moving candidate crossing a clear buffer is rejected, not “fixed” by moving the obstacle near player.

## Tests and acceptance criteria

V08,V09,V21: reflection/static inheritance audit; stationary world z unchanged except rebase, moving +8m/s; swept hit tests including parallel and missed cases; one accepted damage per activation; invulnerability prevents immediate second damage; moving envelopes never enter reserved areas. Require at least one valid moving obstacle in seeded test plans.

## Definition of Done

Both cars present in graybox, inherited overrides are real, swept collision and damage semantics verified, safe generation and pool reset intact. Fuel/death completion remains task08.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No enemy lane-changing AI, rigidbody crash simulation, wheel physics, new singleton or trigger-only collision shortcuts.
