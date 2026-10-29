# 16 — Build, report and submission verification

## Task name and objective

Prepare the executable, evidence report and correctly named course archive.

## Working contract and current assumptions

Implement **only this task** in the existing Neon Tunnel Runner Unity project. First inspect repository instructions, git status, current scripts/scenes/prefabs/packages and earlier verification notes; preserve working behavior and user changes. Read `Preproduction/docs/01-requirements-matrix.md`, `03-technical-architecture.md`, `05-unity-project-structure.md`, `09-testing-and-acceptance.md` and `Preproduction/planning/environment-setup.md`. If this package is elsewhere, locate it before editing; do not invent missing project state.

Baseline: Linux Mint Virginia/glibc2.35; Editor6000.6.3f1; Unity CLI1.0.0-beta.13; official Unity Codex plugin0.1.8-beta; com.unity.pipeline0.9.1-exp.1; VS Code+Codex CLI. Preserve versions; discover installed automation commands and actual URP/Input/Test Framework versions. URP and Unity Pipeline are different packages. Do not upgrade tools or replace system glibc. If Editor/CLI is unavailable, state what could not be verified.

Use namespace NeonTunnelRunner, private serialized dependencies, explicit data ownership and immutable configuration assets. GameManager is the only singleton. All input uses New Input System actions/maps, including menus; no legacy Input or direct keyboard polling. All recurring road/traffic/pickup/VFX instances use prewarmed pools. Preserve abstract Interactable and concrete virtual/override behavior. Keep visuals separate from gameplay. No whole-game rewrite.

## Relevant HW2 requirements

- R01 (HW2 p.1): Credit collaborators and exact external source addresses. [Mandatory]
- R02 (HW2 p.1): Use the exact ZIP naming pattern. [Mandatory]
- R03 (HW2 p.1): Upload the final ZIP to the course system. [Mandatory]
- R04 (HW2 p.1): Student is responsible for file integrity; corrupted files receive no marks; download and check after upload. [Mandatory; explicit zero-mark risk]
- R50 (HW2 p.5): Unity project must be in a Git repository. [Mandatory]
- R51 (HW2 p.5): Include a text file containing repository URL in submission ZIP. [Mandatory]
- R52 (HW2 p.5): Use the exact supplied .gitignore; exclude bulky Library and other generated folders. [Mandatory]
- R53 (HW2 p.5): Include executable game build. [Mandatory]
- R54 (HW2 p.5): Short implementation report explaining structure with real final-game screenshots. [Mandatory]

## Integration dependencies

Tasks01–15 completed in order, with their recorded checks. Extend introduced contracts incrementally; if a prerequisite fails, repair only the blocking integration and record it before continuing.

## Exact classes and files

All shortened script paths below are relative to `Assets/NeonTunnelRunner/Scripts/`; other Assets paths are under `Assets/NeonTunnelRunner/` unless explicitly repository-root. Preserve existing matching files and GUIDs; do not create duplicate implementations.

Scripts/Editor/BuildDelivery.cs; Build Profile; Builds/Linux/; Docs/ImplementationReport.md or finalPDF; CREDITS.md; delivery/repository-link.txt; delivery/README-run.txt; delivery/Screenshots/; final GD_Hw2_[firstName]_[lastName]_[StudentId].zip; Docs/Verification/16-delivery.md.

## Expected behavior and required architecture

Run final fresh-clone import and smoke checks with pinned Editor. Build Linux x86_64 by default; resolve graderOS before release and add its target module/build if required. Include the entire runnable build output (executable, *_Data, UnityPlayer.so and any emitted companions), not the executable alone. Preserve executable permission and document chmod fallback after ZIP extraction. Produce a short report from Preproduction/templates/implementation-report.md with real final-game screenshots, architecture summary, requirement evidence and exact sources/credits. Concept art is not implementation evidence. Put the tested accessible Git URL and commit into repository-link.txt; use supplied.gitignore at repo root. Student firstName,lastName,StudentId and course upload access must be real; do not fabricate them.

Use final nameGD_Hw2_[firstName]_[lastName]_[StudentId].zip, replace placeholders, preserve case. Include repository-link.txt, Build/, short report, required screenshots and run instructions. Validate CRC, extraction and launch in a new directory. Record SHA-256. Prepare a clear student handoff for upload to course system, then re-download, recheck hash/CRC and launch. Actual upload/re-download may require student action; mark it pending until performed.

## Inspector, scene and prefab setup

Discover build automation from installed Pipeline help or use a small Editor BuildPipeline entry with explicit Linux target and BuildReport failure handling. Run with the exact Editor, available license and installed build module. Disable unnecessary development diagnostics for final player but retain test evidence. Never embed credentials or local absolute paths in the release.

## Edge cases

Missing build module/license, wrong targetOS, broken repo permissions, missing.so/_Data folder, lost executablebit, archive paths unsafe, corrupt ZIP, report screenshots stale relative to commit, unavailable student identity, blocked course login.

## Tests and acceptance criteria

V19,V22,V23,V24,V25: actual standalone Quit; fresh clone; exact supplied.gitignore preserved; accessible repo link; build launches after extraction; report has real screenshots/credits; filename valid; zipfile.testzip passes; post-upload download separately verified. Use Preproduction/tools/verify_submission.py for archive structure/CRC, then manual executable launch; script cannot prove playability.

## Definition of Done

Concrete build/report/archive available with hashes and verification record, or exact blocker and remaining student step recorded. Final handoff distinguishes local preparation from actual course upload. Never label an incomplete preproduction ZIP as the course submission.

Explain changed files, design decisions, exact scene/prefab/config assignments, tests actually run and their results. Save a task verification note with requirement IDs, evidence paths, remaining manual steps and blockers. Do not claim Unity compilation, tests, screenshots or builds unless executed. Stop after this task and provide the next prompt number; do not implement later tasks opportunistically.

## Explicit non-goals

No automatic repository publication, sharing/permission changes, invented identity, fabricated screenshots/build success, or upload to the course system without authorized access.
