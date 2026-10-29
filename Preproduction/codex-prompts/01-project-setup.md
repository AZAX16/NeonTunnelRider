# 01 — Project setup and environment audit

## Task name and objective

Establish a clean, reproducible project foundation without implementing gameplay.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R01 (HW2 p.1): Credit collaborators and exact external source addresses. [Mandatory]
- R05 (HW2 p.2): Implement a 3D endless car game in Unity. [Mandatory]
- R11 (HW2 p.2): All input exclusively uses the New Input System and action maps; legacy implementation gets no marks in this section. [Mandatory; explicit section zero-mark risk]
- R40 (HW2 p.4): Bloom for neon and fireworks. [Mandatory; pipeline wording requires interpretation]
- R50 (HW2 p.5): Unity project must be in a Git repository. [Mandatory]
- R52 (HW2 p.5): Use the exact supplied .gitignore; exclude bulky Library and other generated folders. [Mandatory]

## Integration dependencies

No implementation prerequisites; inspect existing repository first.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

`ProjectSettings/ProjectVersion.txt`; `Packages/manifest.json`; `Packages/packages-lock.json`; root `.gitignore`; `Assets/NeonTunnelRunner/Scenes/Game.unity`; `Assets/NeonTunnelRunner/Settings/URP/`; runtime/Editor/EditMode/PlayMode `.asmdef` files in their respective folders; `Assets/NeonTunnelRunner/Scripts/Editor/ProjectSetupValidator.cs`; `Docs/Environment.md`; `Docs/Verification/01-project-setup.md`; `CREDITS.md`. Create the folders in Preproduction/docs/05-unity-project-structure.md. Do not create the remaining gameplay scripts yet.

## Expected behavior and required architecture

Inspect `git status`, existing Assets, scene references, project version, package manifest/lock and project instructions before edits. Preserve work and GUIDs. The installed Editor is6000.6.3f1, not6.3 LTS. Read installed official Unity plugin instructions and discover CLI commands with version/help; record Codex CLI version if accessible. Preserve Unity CLI1.0.0-beta.13, plugin0.1.8-beta and com.unity.pipeline0.9.1-exp.1. Pipeline is automation, not URP. Inspect actual renderer and package versions; never guess their pins or silently upgrade.

For a new project use the installed Editor's URP template. For an existing project adapt only required settings after inspecting compatibility. Copy Preproduction/source/assignment.gitignore byte-for-byte to the Unity project root; preserve any extra existing ignore needs in a separately documented extension rather than replacing the assignment rules. Track Assets with meta, Packages and ProjectSettings. Set Force Text, Visible Meta Files, Linear color, New Input System only, and Linux x86_64 as the provisional development target. No legacy Input module. Create one Game scene with named empty Systems/World/Presentation/UI roots, a temporary camera and simple lit three-lane reference geometry. Register Game in the Build Profile. Create a small Editor-only validator for version, pipeline assignment and required folders. Record unresolved renderer/Input/Test Framework versions after resolution.

## Inspector, scene and prefab setup

Assign a URP asset and renderer in Graphics and all active Quality levels; enable HDR where available. Prepare camera Post Processing and an empty Global Volume for later Bloom. Verify required target module, TMP/uGUI and compatible Test Framework via Package Manager; do not reinstall existing automation packages. Runtime assemblies must not reference UnityEditor or Pipeline editor APIs. Use the installed command discovery if automating scene creation; otherwise document exact Editor setup.

## Edge cases

Existing nonempty project; missing URP; multiple open Editors; unavailable CLI/Editor; unknown Codex version; case-sensitive Linux paths; .gitignore already present. Do not replace system glibc or change global tools. If tools cannot execute, mark checks BLOCKED and state the needed local step.

## Tests and acceptance criteria

V01 and V22: open and reimport with6000.6.3f1; zero compile errors; verify actual resolved versions and render asset. Confirm supplied/root .gitignore hashes match and git check-ignore excludes Library/Temp/Obj/Builds. Verify runtime/test assembly separation and no missing references. Report checks actually run; no gameplay tests required yet.

## Definition of Done

A project that opens cleanly, the assigned .gitignore preserved, exact environment recorded, required scene/folders/assemblies present and a concise verification note. Stop at this foundation.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No player controller, pooling, traffic, scoring, art production, package upgrades, publication or whole-game generation.
