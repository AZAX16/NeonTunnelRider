# 11 — Night environment, assets and dynamic neon

## Task name and objective

Build the selected Retro Vector visual identity and required lighting.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R28 (HW2 p.3): Artistic and visual quality is assessed. [Explicitly graded; no numeric rubric supplied]
- R29 (HW2 p.3): Nighttime atmosphere. [Mandatory within graded art section]
- R30 (HW2 p.3): Starry or cloudy night skybox. [Mandatory within graded art section]
- R31 (HW2 p.3): Dark surroundings and suitable road illumination. [Mandatory within graded art section]
- R32 (HW2 p.3): Player has headlights. [Mandatory within graded art section]
- R33 (HW2 p.3): Player has neon underglow. [Mandatory within graded art section]
- R34 (HW2 p.3): Underglow intensity and glow increase with actual vehicle speed. [Mandatory within graded art section]
- R35 (HW2 p.3): Street lighting is recommended. [Recommended, not stated mandatory]

## Integration dependencies

Tasks01–10 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

Presentation/VehicleVisuals.cs; Config/PresentationConfig.cs; Materials/; Meshes/; Settings/URP/; Prefabs/Player and Road/; Configs/Presentation.asset; Game.unity lighting settings; CREDITS.md; Docs/Verification/11-night-neon.md.

## Expected behavior and required architecture

Follow docs06–07 and art/style-guides/visual-system.md. Replace graybox visuals with a low-poly original wedge, warm obstacle cars, gold ring and mint can; preserve all roots/bounds/seams/pool keys. Build geometric arches and open skyline modules; alternate open sky/tunnel using existing schedule. Use dark night skybox with stars/clouds, locally lit readable road, two actual player headlight spotlights, emissive underbody and bounded local underglow Light. Include recommended streetlight geometry along roadside. Scale neon from actual simulation speed: u=InverseLerp(0,50,speed), lightLerp(.3,3,u), emissionLerp(1,5,u), tune fixed-exposure scene. No score-based substitute. Pool road decoration as segment children.

## Inspector, scene and prefab setup

URP is com.unity.render-pipelines.universal; preserve com.unity.pipeline separately. Keep shared materials, no Renderer.material in Update. Assign light ranges and road illumination; most streetlights use emission, with limited real lights. Keep camera Volume mask ready for task12. Use genuine modeled meshes or primitives; image references are not imported as3D models.

## Edge cases

Underlit obstacles; excessive Bloom exposure later; dark vehicle on dark road; too many realtime lights; actual speed0 while paused; asset pivots/scale mismatches; open section hidden by tunnel roof.

## Tests and acceptance criteria

V16,V05,V06: compare measured speed22/50 and pause under fixed exposure; headlight cones and underglow visible; night sky seen in open sections; three lanes clear; geometry and pooling tests still pass. Record licenses and source URLs for any external assets.

## Definition of Done

Required night/sky/headlight/neon features plus recommended streetlights work in Unity; selected art coherent; speed response proven with screenshots/values.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No rain simulation, ray tracing, dense city traffic, copied franchise assets, runtime mesh rebuilding or full-screen post effects beyond later Bloom.
