# 13 — Music, engine and interaction audio

## Task name and objective

Complete required sound cues with bounded reusable sources.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R28 (HW2 p.3): Artistic and visual quality is assessed. [Explicitly graded; no numeric rubric supplied]
- R41 (HW2 p.4): Background music. [Mandatory]
- R42 (HW2 p.4): Vehicle engine audio. [Mandatory]
- R43 (HW2 p.4): Collision sound. [Mandatory]
- R44 (HW2 p.4): Coin and fuel pickup sounds. [Mandatory]

## Integration dependencies

Tasks01–12 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

Audio/AudioController.cs; Config/AudioConfig.cs; Configs/Audio.asset; Audio/Clips/; Audio/Mixers/RunnerMixer.mixer; Game.unity Audio root; CREDITS.md; Docs/Verification/13-audio.md.

## Expected behavior and required architecture

Integrate licensed/original background music, engine loop, collision sound, coin chime and fuel cue. Use two persistent loop AudioSources plus8 precreated one-shot voices; no runtime source creation. Engine pitch.8..1.5 follows actual speed. Consume accepted damage and pickup events once; no collision sounds for rejected grace contacts. Pause engine/SFX explicitly; music may continue at reduced volume. GameOver stops engine; restart clears voices but does not duplicate music. Balance mixerMusic/Engine/SFX with headroom; record exact source links/licenses.

## Inspector, scene and prefab setup

Assign clips, mixer groups and spatialBlend intentionally (HUD-like pickups can be2D; engine attached to player). Ensure one listener. Preview seamless loop endpoints; choose compression/load modes from clip use. Missing clips produce clear validation errors, not exceptions. If no audio is available, create original simple clips or document the remaining asset work accurately rather than claiming silent placeholders meet the requirement.

## Edge cases

Repeated restart; voice pool full; engine at paused speed0; mixed sample rates; clipped one-shots; unsupported clip import; sound event fires twice; application loses focus.

## Tests and acceptance criteria

V18,V12,V13: listen in standalone to all5 cue categories, distinct pickups, pitch response, pause/resume and50 restarts; inspect voice count remains fixed and no duplicate loops. Verify license entries and missing-clip validator.

## Definition of Done

Required music/engine/collision/coin/fuel audio genuinely audible, levels balanced, source counts bounded, reset and pause behavior verified.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No audio middleware, adaptive music system, paid assets without authorization or sound generated from image references.
