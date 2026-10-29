# Risks and assumptions

| Risk / assumption | Consequence | Mitigation / checkpoint |
| --- | --- | --- |
| Legacy input sneaks in from sample/UI | PDF says zero marks for input section | New-only settings; action asset audit; V02 |
| Pool misses allocate in Playing | Violates explicit assignment rule | Bounded prewarm, safe omission/reservation, counters; V06 |
| Four types do not share abstract Interactable | OOP requirement missed | Reflection/static inheritance audit; V08,V21 |
| Collision reported by both trigger and sweep | Several lives lost / duplicate reward | One authoritative sensor; activation IDs; V08–V10 |
| “One empty lane” changes too quickly or traffic drifts into it | Impossible formations | Protected spans/buffers and full moving envelopes; V21 |
| Fuel purely randomized | Unavoidable failure | ≤220m safe-route guarantee; V10,V21 |
| Pooled state/callbacks survive restart | Ghost damage, pickups fail, resource leaks | Explicit reset transaction,50 reset test; V06,V13 |
| Bright effects hide obstacles | Art polish lowers playability | Fixed exposure, shape/color separation, real camera checks |
| Tunnel-only road hides sky/fireworks | Assessed visual requirements missed | Open/tunnel schedule; sky visibility at runtime; V16,V17 |
| URP/PSv2 confusion | Incompatible post processing | Built-in URP Bloom with written interpretation; instructor clarification if literal package demanded |
| Hovercar judged unlike a car | Theme interpretation risk | Car silhouette/headlights/road rules; swap only visual mesh if needed |
| Mint/Pipeline beta workflow differs from current docs | Automation blocked | Preserve pins; inspect installed help; Editor fallback; no glibc replacement |
| Actual URP/Input/Test Framework versions unknown | Wrong API/package assumptions | Resolve and record from local manifest/lock in01 |
| Codex CLI version unknown | Unverified automation behavior | Record installed version; no invented compatibility result |
| Device hardware unknown | Performance budget may be unrealistic | Record PC/GPU; measure standalone; simplify decoration first |
| GraderOS unknown | Executable may not launch | Confirm before16; Linux default is development only |
| Student identity/Git URL/course access absent | Cannot produce truthful final course ZIP name or upload | Required student handoff in16; no fabrication |
| Concept art treated as delivered Unity work | Misleading report/evidence | Only real gameplay screenshots in final report |
| Missing exact source credits | Violates page1 rule | CREDITS plus asset/license ledger; V24 |
| ZIP corrupt or missing build companions | No marks for corrupt files | CRC/extract/run, re-download after upload; V23,V25 |

No mark values, due date, target OS, minimum fps or mandatory Unity patch are stated in the PDF. Numeric tuning/budgets, UI style, scoring formula, grace duration and implementation estimates are proposals. No Unity runtime test, build, repository publication or course upload has been performed in this preproduction phase.
