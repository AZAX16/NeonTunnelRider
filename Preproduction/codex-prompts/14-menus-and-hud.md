# 14 — Menus, HUD and UI integration

## Task name and objective

Expose every required action and live value using New Input System UI.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R11 (HW2 p.2): All input exclusively uses the New Input System and action maps; legacy implementation gets no marks in this section. [Mandatory; explicit section zero-mark risk]
- R12 (HW2 p.2): Begin with three lives and show them in UI. [Mandatory]
- R19 (HW2 p.3): Display remaining fuel as a bar. [Mandatory]
- R28 (HW2 p.3): Artistic and visual quality is assessed. [Explicitly graded; no numeric rubric supplied]
- R45 (HW2 p.4): Main menu: Start and Quit. [Mandatory]
- R46 (HW2 p.4): Pause menu: Resume, Restart, Main Menu. [Mandatory]
- R47 (HW2 p.4): Game Over: score, high score, Restart. [Mandatory]
- R48 (HW2 p.4): Live HUD shows speed, fuel bar, lives, score and best. [Mandatory]

## Integration dependencies

Tasks01–13 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

UI/{HudPresenter,MenuPresenter}.cs; UI Canvas/panels/prefabs; Input/Runner.inputactions; EventSystem; Core/GameManager.cs command bindings; Tests/PlayMode/UiBindingTests.cs; Docs/Verification/14-ui.md.

## Expected behavior and required architecture

Main menu:Start/Quit. Pause:Resume/Restart/Main Menu. GameOver:final score/high score/Restart plus reason text. HUD:actual speed in km/h, normalized fuel bar and percentage, remaining lives, current score, best. Present snapshots immediately on enable and on change; no copied gameplay state. Use uGUI/TMP real controls and text following art guide; concepts are appearance references only. System/Pause Escape is the sole owner of pause toggle; UI Cancel Backspace prevents same-frame double processing. Navigation arrows/Enter/mouse come from UI action map through InputSystemUIInputModule. Disable gameplay map outsidePlaying; UI stays functional at timeScale0. Deterministic initial focus on Start/Resume/Restart.

## Inspector, scene and prefab setup

Canvas Scaler1920×1080, match.5, safe48px margins; anchor score/best upperleft, lives upperright, speed lowerleft, fuel lowerright. Panel style consistent with main/menu concepts. Remove StandaloneInputModule and temporary Editor debug UI that overlaps gameplay. Buttons invoke GM commands; Restart resets all subsystems via existing transaction.

## Edge cases

First HUD frame stale; high score exceeded midrun; low fuel0; no selected button; Escape processed twice; pointer click steals navigation; narrow/wide aspect ratios; Quit in Editor versus build; missing panel references.

## Tests and acceptance criteria

V02,V12,V14,V19,V13: every button by pointer/keyboard; five fields equal snapshot; fuel fill0..1; pausedUI responsive; high score survives restart/process exit; render720p/1080p,4:3/16:9/21:9; Quit closes standalone. No legacy input in UI code.

## Definition of Done

Complete UI requirement set works in standalone and Editor, values remain synchronized through every state, and focus/layout evidence recorded.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No settings/shop/login/localization system, flattened image-based buttons, new state machine or gameplay rewrite.
