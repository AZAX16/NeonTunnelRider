# 03 — New Input System and three-lane player

## Task name and objective

Make a responsive, continuously advancing graybox car using only actions/maps.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R05 (HW2 p.2): Implement a 3D endless car game in Unity. [Mandatory]
- R06 (HW2 p.2): Exactly three lanes and a third-person camera. [Mandatory]
- R07 (HW2 p.2): Automatic continuous forward driving. [Mandatory]
- R10 (HW2 p.2): A/D and left/right arrows change lanes. [Mandatory]
- R11 (HW2 p.2): All input exclusively uses the New Input System and action maps; legacy implementation gets no marks in this section. [Mandatory; explicit section zero-mark risk]

## Integration dependencies

Tasks01–02 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

Scripts/Input/InputRouter.cs; Scripts/Player/PlayerController.cs; Input/Runner.inputactions; Prefabs/Player/PlayerHovercar.prefab; Core/GameplayLoop.cs; Config/GameplayConfig.cs; Tests/EditMode/LaneMovementTests.cs; Tests/PlayMode/InputBindingTests.cs; Game.unity; Docs/Verification/03-input-and-player.md.

## Expected behavior and required architecture

Create Gameplay/Left and Right Button actions: A+LeftArrow and D+RightArrow respectively. System/Pause uses Escape in Playing/Paused. Create UI map (Navigate arrows, Submit Enter, Cancel Backspace, Point/Click/Scroll mouse) for task14. Use callbacks, not Keyboard.current or legacy Input. One press→one adjacent transition, no hold repeat; opposite presses in one input update cancel. Clamp lane index -1..1, centers -3.2/0/3.2. Transition .28s; one queued command overwritten by latest press; never teleport over center. Clear commands when gameplay disabled. Player root advances +Z at22m/s through GameplayLoop; keep fixed y and rotation, kinematic Rigidbody, box bounds. Store logical previous/current poses; presentation roll goes on a child. Existing scene may use static graybox ground until task05; do not build an unpooled endless loop.

## Inspector, scene and prefab setup

Add InputRouter to Systems; assign action asset and state reference. Add Player at center x0,y.6,z0 with Rigidbody isKinematic=true and interpolation, scale1. Root box half extents .85/.45/1.8. Place a simple static chase camera behind/above so movement is reviewable; camera polish is task10. Set New-only handling; no StandaloneInputModule.

## Edge cases

Held keys; simultaneous keys; repeated edge presses; input during Menu/Paused/GameOver; pause during a transition; different render rates; no road beyond graybox yet.

## Tests and acceptance criteria

V02,V03 and V12 input subset: use InputTestFixture for all four bindings, holds and opposite presses; step simulation and assert bounded target indices, exact final x and smooth intermediate positions. Confirm continuous forward movement and zero movement while paused. Verify no legacy input API or direct device polling.

## Definition of Done

Player moves and changes three lanes with every required binding, action maps obey state, tests and scene wiring verified; no infinite-road claim before task05.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No free steering, jump, shoot, throttle control, road spawning, collision rewards or visual polish.
