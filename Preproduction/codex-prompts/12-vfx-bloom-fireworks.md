# 12 — Particles, Bloom and fireworks

## Task name and objective

Implement every assessed particle and post-processing effect.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R14 (HW2 p.2): Collision has visual feedback. [Mandatory; examples are alternatives]
- R23 (HW2 p.3): No repeated Instantiate/Destroy during normal play. [Mandatory]
- R28 (HW2 p.3): Artistic and visual quality is assessed. [Explicitly graded; no numeric rubric supplied]
- R36 (HW2 p.3): Periodically/randomly launch colorful sky fireworks. [Mandatory within graded art section]
- R37 (HW2 p.4): Engine exhaust smoke/fire particles. [Mandatory within graded art section]
- R38 (HW2 p.4): Collision spark particles. [Mandatory within graded art section]
- R39 (HW2 p.4): Collectible disappearance effects. [Mandatory within graded art section]
- R40 (HW2 p.4): Bloom for neon and fireworks. [Mandatory; pipeline wording requires interpretation]

## Integration dependencies

Tasks01–11 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

Presentation/{VfxController,PooledVfx,FireworkDirector}.cs; Prefabs/VFX/{Spark,CoinBurst,FuelBurst,Firework}.prefab; Player exhaust ParticleSystems; Settings/Volumes/NightVolume.asset; PoolConfig; Config/PresentationConfig.cs; Tests/PlayMode/VfxPoolingTests.cs; Docs/Verification/12-vfx.md.

## Expected behavior and required architecture

Add engine smoke/flame ParticleSystems; damage spark burst; distinct coin/fuel disappearance bursts; sky rocket launch and colorful burst: first launch after2–4 playing seconds in the opening sky, then every8–14 playing seconds while open sky is visible. Cosmetic RNG is independent; postpone timer while tunnel blocks sky, then fire promptly in a visible open span. Keep effects above/lateral to horizon. Subscribe to typed gameplay events; pickup returns immediately while detached pooled burst continues. Prewarm all repeatable effects; StopAction must not Destroy. Stop/clear trails, particles and sub-emitter state on return and reset. Use URP integrated Global Volume Bloom and camera Post Processing. Explain PDF “Post-Processing package” interpretation; never install incompatible PSv2 alongside URP.

## Inspector, scene and prefab setup

Enable HDR where supported, camera Post Processing and matching Volume mask; set conservative Bloom threshold/intensity and fixed tonemapping. Assign particle materials with correct blend modes. Firework prefab has launch trail and burst sub-emitter; all children included in completion check. Use local-space particles under explicitly shifted effect roots to survive origin rebase.

## Edge cases

Pool saturation; particle callback after return; sub-emitter remains alive; pause during rocket; restart leaves sparks; rebase causes stretched trails; too much glow hides road; fireworks never visible because scheduler fires only under roofs.

## Tests and acceptance criteria

V17,V06,V12,V20 visual subset: show each required effect and both pickup types; Bloom on/off comparison; visible launch+burst in first open stretch; paused particles/timers freeze; pooled effects fully reset; bounded particle counts/no runtime Instantiate/Destroy.

## Definition of Done

All required particles and Bloom are visible in real Unity, fireworks genuinely launch and burst, pools/reset/pause hold and screenshots support each requirement.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No VFX Graph dependency, expensive volumetrics, per-particle lights, gameplay-affecting fireworks or prerecorded particle videos.
