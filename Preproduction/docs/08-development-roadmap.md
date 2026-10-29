# Development roadmap

Follow prompt numbers01→16. Architecture and pooling are established in milestone1, before spawning. The visual milestone precedes final UI closure so the prompt order stays linear; this changes milestone numbering from the suggested outline but preserves all requested outcomes. A functional graybox exists before art production. Estimates assume one student familiar with Unity and are planning ranges, not promised completion times; total76–116 focused hours, plus a20% contingency if toolchain/art work is unfamiliar.

| Milestone | Prompts | Prerequisites | Main work / scope | Expected working result | Tests | Effort estimate | Risks / dependencies |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 — Playable prototype | 01–05 | Existing setup or empty Unity project; read source and environment contract | Pin environment, state model, input/player, pool foundation, endless road, basic rear camera | Continuous three-lane graybox with pause/reset commands and no road gaps | V01–V06; V12/V13 core subset | 12–18 h | CLI/package mismatch, seam precision and premature unpooled spawning |
| 2 — Complete gameplay | 06–09 | Milestone1 stable and pooled | Four Interactable classes, swept contacts, safe traffic, coins/fuel, lives, both death modes, score/difficulty/persistence | A full repeatable graybox run with a survivable route and saved best | V07–V13, V21 seed tests | 20–30 h | Moving-traffic fairness, duplicate damage, fuel starvation, end-of-tick ordering |
| 3 — Visual and audio production | 10–13 | Complete graybox; DirectionA chosen | Camera/motion, low-poly assets, night lighting, speed neon, particles/Bloom/fireworks and required audio | The game expresses the selected look and every assessed visual/audio feature | V15–V18 plus pooling/reset regression | 24–36 h | GPU lights/overdraw, unavailable audio licenses, overly dark obstacles |
| 4 — Architecture and UI closure | 14; review02/04/06/09 | Working gameplay/presentation contracts | All menus/HUD, New Input System UI, focus/resolution checks; audit singleton/inheritance/pools/persistence | Complete playable assignment feature set with synchronized UI | V02,V12–V14,V19,V21 | 6–10 h | Escape double handling, stale UI values, pause/restart leaks |
| 5 — Polish, verification and delivery | 15–16 | Feature complete; actual student/repository/target details | Full audit, seeded/soak tests, optimization from evidence, build, report, archive, upload handoff | Verified standalone build and correct course ZIP; post-upload check recorded separately | V01–V25 with evidence | 14–22 h | Wrong OS/module, missing build companions, corrupt archive, inaccessible repo |

## Gate discipline

At each gate, run the assigned checks, inspect the diff, update Docs/Verification and make a recoverable checkpoint commit under the repository's normal workflow. Do not advance with a broken compiler, missing scene references, silent pool exhaustion or an unbounded generator. A red test caused by future work is marked pending only if the prompt explicitly leaves that feature for a later task.

For each work session: open the pinned Editor, confirm correct project/automation connection, paste one prompt, review its changes, run its checks, inspect the visible result and capture evidence. Write down the seed/config and observed issue for any failed run. Avoid changing version pins or gameplay constants while debugging unrelated art.

## Scope cuts if time runs short

Keep every mandatory PDF feature. Reduce decorative city geometry, material variation, camera polish, extra tunnel variants and optional streetlight real lights first. Keep at least a simple starry sky, headlights, speed-linked underglow, fireworks launch/burst, exhaust, sparks, pickup effects, Bloom, all5 audio cues and complete menus. Do not trade away Input Actions, pools, inheritance or delivery verification for polish.

## Student decisions needed at their natural checkpoints

- Task01 records actual resolved URP/Input/Test Framework and Codex CLI versions; no need to stop preproduction for these.
- Before final build choose the grader's executable OS and ensure its module is installed; Linux is the development baseline.
- Task16 supplies first/last name, studentID and accessible Git URL, generates the final filename, uploads through authorized course access and downloads the archive again.
