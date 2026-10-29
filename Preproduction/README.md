# Neon Tunnel Runner — production package

**Preproduction specification, not a built Unity game.** An original three-lane endless hovercar runner through geometric neon tunnels and open night highways. Avoid parked and moving cars, collect coins and fuel, survive with three lives and beat a persistent best score.

Prepared against the complete supplied Persian HW2.pdf, including its rules and submission pages. **54 assignment requirements** are mapped to implementation prompts and **25 validation cases**. There are **16 sequential Codex prompts**, **12 generated concept references**, an asset-production plan and a full delivery checklist. Runtime evidence is intentionally marked NOT RUN; no Unity code was compiled, tested or built in this phase.

## Your setup is the baseline

Unity Editor **6000.6.3f1**; Linux Mint Virginia/glibc **2.35**; Unity CLI **1.0.0-beta.13**; official Unity plugin **0.1.8-beta**; `com.unity.pipeline` **0.9.1-exp.1**; VS Code+Codex CLI; New Input System. Preserve these versions. Unity Pipeline automates the Editor; **URP is a separate render package**. Record actual URP/Input/Test Framework and Codex CLI versions locally in task01. No automatic tool upgrade or Editor downgrade is requested.

## Start here

1. Extract this package and read the requirement matrix, architecture and environment note.
2. Copy its contents into the Unity repository as `Preproduction/`, outside Assets.
3. Paste [prompt01](codex-prompts/01-project-setup.md) into Codex with that repository open. It inspects the existing project and sets up only the foundation.
4. Verify each task before proceeding through the [full prompt index](codex-prompts/README.md). Do not run all prompts as one task.
5. Produce the real game/build/report later using task16; this ZIP is not named as a final course submission.

## Architecture and planning documents

| Document | Purpose |
| --- | --- |
| [00-assignment-translation.md](docs/00-assignment-translation.md) | HW2 — complete English reading |
| [01-requirements-matrix.md](docs/01-requirements-matrix.md) | HW2 requirement traceability |
| [02-game-design-document.md](docs/02-game-design-document.md) | Neon Tunnel Runner — game design |
| [03-technical-architecture.md](docs/03-technical-architecture.md) | Technical architecture |
| [04-class-and-event-diagrams.md](docs/04-class-and-event-diagrams.md) | Architecture diagrams |
| [05-unity-project-structure.md](docs/05-unity-project-structure.md) | Unity project, scenes and prefabs |
| [06-art-direction.md](docs/06-art-direction.md) | Art direction and exploration |
| [07-asset-production-plan.md](docs/07-asset-production-plan.md) | Asset production specification |
| [08-development-roadmap.md](docs/08-development-roadmap.md) | Development roadmap |
| [09-testing-and-acceptance.md](docs/09-testing-and-acceptance.md) | Testing and acceptance |
| [10-submission-checklist.md](docs/10-submission-checklist.md) | Submission checklist |

## Planning references

| File | Purpose |
| --- | --- |
| [decisions.md](planning/decisions.md) | Decision log |
| [dependency-map.md](planning/dependency-map.md) | Implementation dependencies |
| [environment-setup.md](planning/environment-setup.md) | User environment and compatibility checkpoint |
| [implementation-order.md](planning/implementation-order.md) | Implementation order |
| [risks-and-assumptions.md](planning/risks-and-assumptions.md) | Risks and assumptions |
| [sources.md](planning/sources.md) | Sources and attribution ledger |

- [Requirements CSV](planning/requirements-matrix.csv) and [JSON](planning/requirements.json)
- [Validation cases JSON](planning/validation-cases.json)
- [Evidence register — NOT RUN](planning/requirement-evidence.csv)
- [Source assignment](source/HW2.pdf) and [exact supplied gitignore](source/assignment.gitignore)

## Art and asset package

Recommended: **Retro Vector Arcade**, low-poly indigo shapes, cyan road/player light, magenta architecture, orange hazards, gold coins and mint fuel. See [three-direction comparison and all12 images](docs/06-art-direction.md), [asset specifications](docs/07-asset-production-plan.md), [visual system](art/style-guides/visual-system.md) and [ready-to-use asset prompts](art/asset-prompts/README.md).

Generated PNGs are visual references, not Unity-ready meshes or final gameplay screenshots. The production plan specifies modeling, colliders, pivots, materials, prefab organization, pool resets and import checks. Exact generated prompts are included. The definitive game uses straight three-lane roads even if an exploratory concept suggests a distant curve.

## Required delivery details preserved

Use the supplied `.gitignore`; credit collaborators and exact external sources; final course ZIP name is `GD_Hw2_[firstName]_[lastName]_[StudentId].zip`. It contains a text Git link, executable build and short report with real final-game screenshots. Download and verify the ZIP again after course upload. See [submission checklist](docs/10-submission-checklist.md), [report template](templates/implementation-report.md) and [archive verifier](tools/verify_submission.py).

## Remaining external checks

The student naming fields, Git URL, graderOS and course access are not supplied. Linux is the development build default. Actual URP/Input/Test Framework package pins must be read locally. URP's integrated Bloom is the chosen interpretation of the PDF's Post-Processing wording; do not install incompatible Post Processing Stackv2. If staff specifically demands that older package or a conventional wheeled car, settle that interpretation before visual production. No implementation coverage row is left unmapped; actual compliance and grading cannot be guaranteed before implementing and validating.

## Package verification

[Actual preproduction audit](planning/package-audit.md) records checks performed here and distinguishes them from Unity tests that remain unexecuted. [Re-run package audit](tools/audit_package.py).
