# Implementation dependencies

| Task | Hard predecessor contracts | Supplies |
| --- | --- | --- |
| 01 | Existing environment/source | Project, assemblies, conventions |
| 02 | 01 | State, model, config, fixed loop entry |
| 03 | 02 | Input actions, player poses, lane timing |
| 04 | 02 | Pool identities and reset hooks |
| 05 | 03,04 | Endless road, origin, safe block plan |
| 06 | 03–05 | Base inheritance, traffic, swept contacts, damage API |
| 07 | 06 | Coin/fuel overrides, reward API and supply guarantee |
| 08 | 02,06,07 | Fuel ticking, grace, terminal behavior, basic flash |
| 09 | 05,07,08 | Progression curve, score, persistent best |
| 10 | 03,05,08 | Camera and visual motion |
| 11 | 05,09,10 | Models, night, lights, speed-neon |
| 12 | 04,07,08,11 | Pooled VFX, Bloom, sky fireworks |
| 13 | 07,08,09 | Audio events and bounded voices |
| 14 | 02,03,08,09; presentation10–13 | Complete menus/HUD |
| 15 | 01–14 | Evidence-backed release candidate |
| 16 | 15; actual identity/repository/target | Build/report/ZIP and student upload handoff |

The dependency graph permits some independent asset work, but this prompt pack intentionally uses a single linear implementation stream to minimize integration conflicts. No parallel coding agents are required.
