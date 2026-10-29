# 05 — Procedural road, origin and safe-pattern data

## Task name and objective

Deliver a gap-free endless graybox road and generation rules ready for traffic.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R05 (HW2 p.2): Implement a 3D endless car game in Unity. [Mandatory]
- R06 (HW2 p.2): Exactly three lanes and a third-person camera. [Mandatory]
- R08 (HW2 p.2): Procedural endless road. [Mandatory]
- R09 (HW2 p.2): Dynamically place segments ahead and recycle behind. [Mandatory]
- R22 (HW2 p.3): Pool road segments, obstacle cars, coins and fuel cans. [Mandatory]

## Integration dependencies

Tasks01–04 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

Scripts/World/{RoadManager,RoadSegment,WorldOrigin,SpawnDirector,PatternValidator}.cs; Scripts/Config/WorldConfig.cs; Configs/World.asset; Prefabs/Road/{RoadOpen60,RoadTunnel60,RoadEntrance60,RoadExit60}.prefab; PoolConfig entries; GameplayLoop; Tests/PlayMode/RoadContinuityTests.cs; Tests/EditMode/PatternSafetyTests.cs; Game.unity; Docs/Verification/05-endless-world.md.

## Expected behavior and required architecture

Road modules are exactly60m long and9.6m wide with entry0/exit60. Maintain ≥300m ahead and≥60m behind; choose3 open/3 tunnel schedule with matching transition variants. Rent all modules from task04. Long absolute segment indices and double total distance are independent from local positions. Rebase at local z≥1000 by a whole60m multiple; shift top-level active objects exactly once and both logical poses/camera caches; no artificial distance/contact. Prepare deterministic SpawnDirector block plans and PatternValidator:480m blocks,360m protected-lane span,120m clear buffer, rows60m, no traffic first180m. Do not yet spawn missing interactable prefabs. Preserve seed/config across reset. Separate road, gameplay and later cosmetic RNG streams.

## Inspector, scene and prefab setup

Build primitive road/arches, two interior dividers at x±1.6 and outer edges±4.8. All variants share endpoint markers and road surface height. Add sufficient prewarmed road entries from doc03. WorldRoot holds road and interactable roots separately; register origin-shift participants. Wire loop order explicitly.

## Edge cases

Multiple segments crossed in a tick; seams at entrance/exit; road pool exhaustion; resets after rebase; first/last module visibility; very long runs; moving traffic envelopes are validated when introduced in task06.

## Tests and acceptance criteria

V05,V06,V21 data subset: simulate100km equivalent; gap<.001m and coverage bound always true; active counts bounded; rebase preserves distance and relative poses; same seed gives same module/block plans; all block transitions fit two lane shifts plus reaction margin. Visually inspect seams in Play Mode.

## Definition of Done

Playable endless three-lane graybox with pooled segments, no gaps, bounded local coordinates, reproducible safe-block plan and verified reset. Traffic appearance remains pending.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No art polish, curved roads, physics suspension, unpooled decoration spawning or arbitrary traffic without safety validation.
