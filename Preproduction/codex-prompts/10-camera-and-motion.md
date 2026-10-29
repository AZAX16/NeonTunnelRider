# 10 — Rear camera and hovercar motion polish

## Task name and objective

Improve motion readability without changing gameplay collision geometry.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R06 (HW2 p.2): Exactly three lanes and a third-person camera. [Mandatory]
- R14 (HW2 p.2): Collision has visual feedback. [Mandatory; examples are alternatives]
- R28 (HW2 p.3): Artistic and visual quality is assessed. [Explicitly graded; no numeric rubric supplied]

## Integration dependencies

Tasks01–09 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

Presentation/ChaseCamera.cs; Presentation/VehicleVisuals.cs; Config/PresentationConfig.cs; Configs/Presentation.asset; Player prefab; MainCamera; Tests/PlayMode/CameraResetTests.cs; Docs/Verification/10-camera.md.

## Expected behavior and required architecture

Follow from behind/above in LateUpdate, initial offset(0,4.2,-8), look ahead12m, FOV65°. Keep all lanes and≥120m useful horizon visible; adjust from real screenshots. Smooth lateral follow with bounded lag; small damage impulse≤.12m. Visual roll≤8° and bob≤.08m live on mesh child; root remains fixed rotation. Origin shifts offset current camera position, target and damping cache exactly once so there is no long spring-back. Reset snaps camera and clears shake. Respect pause; no accumulating velocity while frozen.

## Inspector, scene and prefab setup

Assign player, config and RunSession event subscriptions. One MainCamera/AudioListener. Validate clipping planes against road coverage, tunnel interior and vehicle. Use simple custom controller; no Cinemachine dependency required. Keep existing graybox camera behavior until replacement is wired.

## Edge cases

Fast alternating lanes, maximum speed, corridor edge, tunnel portal, pause mid-shake, restart50 times, origin shift during lane transition. Do not use camera motion to fake forward speed.

## Tests and acceptance criteria

V15,V13: screenshot rear/above view in all lanes; verify unchanged collision roots; no camera jump on reset/rebase; no tunnel clipping or HUD conflict; automated pose-cache reset test where meaningful.

## Definition of Done

Camera supports gameplay at speed, bounded visual motion and reset/origin handling are verified, data ownership unchanged.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No cinematic cutscenes, dynamic FOV gimmicks, free-look steering, camera collision framework or gameplay changes.
