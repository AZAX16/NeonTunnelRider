# Task 05 — Procedural road, origin and safe-pattern data

Implemented and verified 2026-11-01–02 (Asia/Tehran), starting from clean `main` at `a6acbd0`. No newer commits or user edits were present. Inspected repository/ancestor instructions (none found), Task 03/04 verification, the Task 04 implementation/tests/API handoff, original Task 05/06 prompts, requirements/design/architecture/structure/acceptance documents and environment setup before integration. Previous 106-test results are historical; the results below are new.

Environment retained: Linux Mint 21.3 Virginia/glibc 2.35; Editor **6000.6.3f1**; CLI **1.0.0-beta.13** at `/home/alireza/.local/bin/unity.previous`; official plugin **0.1.8-beta**; Pipeline **0.9.1-exp.1**; URP/Core **17.6.0**, Input **1.20.0**, Test Framework **1.8.0**, uGUI/TMP **2.6.0**. The pre-existing PATH CLI is beta.14; it was used only for initial version/discovery, then all construction/verification used beta.13. No upgrades, package changes, glibc replacement or external downloads. The connected Editor was stopped, with this checkout open. The `unity:unity-cli` skill supplied the Editor workflow.

## Implementation and changed files

All asset paths here are under `Assets/NeonTunnelRunner/`.

- `Scripts/World/RoadManager.cs`: fixed arrays for seven committed and seven target leases; absolute long segment indices; atomic target-window reservation with token-checked rollback/recycling. All rentals use the actual Task 04 SpawnContext/PoolService API. Required road validation happens before a run transaction. Optional future catalog entries remain unnecessary.
- `Scripts/World/RoadSegment.cs`: real PooledObject subclass with authored environment/endpoint references, fresh absolute start/index and deterministic decoration metadata before activation. Pool return clears rental metadata. Geometry/decoration is prefab-owned, with no independent Update or runtime primitive/material creation.
- `Scripts/World/WorldOrigin.cs`: end-of-tick whole-module rebases, independent double offset/count and explicit disjoint participant ownership. Player owns the existing child camera; road manager owns each active segment. Storage and inactive instances never shift.
- `Scripts/World/SpawnDirector.cs`, `PatternValidator.cs`: bounded immutable block/candidate value data, counter-based gameplay RNG, occupancy/warmup/timing validation. No traffic/pickup objects or future gameplay classes were introduced.
- `Scripts/Config/WorldConfig.cs`, `Configs/World.asset`: immutable read access to coverage, rebase, reaction/margin and candidate probability. Shared geometry/block dimensions are constants used by all callers. Gameplay speed and lane duration come from the actual GameplayConfig.
- `Prefabs/Road/{RoadOpen60,RoadTunnel60,RoadEntrance60,RoadExit60}.prefab`, `Materials/RoadArchitecture.mat`, populated existing `Configs/Pools.asset` and saved `Scenes/Game.unity`.
- `Scripts/Core/GameplayLoop.cs`: explicit world dependencies, run preparation/release, target-coverage preflight, double-to-local player pose assignment, bounded planning and end-of-tick rebase.
- `Scripts/Core/GameManager.cs`: validates required world before cleanup/session mutation, commits a new session only after successful coverage, and rejects Resume after a diagnosed road failure until Restart/Menu.
- `Scripts/Player/PlayerController.cs`: double previous/current absolute Z plus origin offset; local positions are converted after double subtraction. Rebase teleports Rigidbody/transform and both histories, temporarily disabling/restoring Rigidbody interpolation. Existing lane/input/queue semantics remain.
- Existing `Scripts/Editor/ProjectSetupValidator.cs`: checks required world services/config/prefabs, camera/participant ownership, separate roots and disabled static floor.
- Added `Tests/PlayMode/RoadContinuityTests.cs`, `Tests/EditMode/PatternSafetyTests.cs`; extended movement fixture wiring and existing PoolLifecycleTests. Core/input/pool regression assertions remain. The former empty-catalog Start test now correctly expects rejection by the required-road caller; PoolService itself still accepts an empty catalog. The isolated 12-key probe reuse test runs without starting a road run; real road tests separately verify no growth while Playing.
- Repository-root `Docs/Setup/Task05{Setup.cs,Smoke.cs,RebaseCapture.cs,Tests.py,PlaySessions.py}`, this note, `Evidence/Task05/`, and `CREDITS.md`. Recipes are outside Assets/player assemblies.

Existing asset GUIDs, pool implementation, input actions/maps, player prefab, configuration defaults and packages are preserved. Unity serialized the new prefabs/assets/scene and generated their metas. The Task 04 handoff exists and matched the implementation; no handoff documentation gap was found.

## Saved assignments and geometry

| Object / asset | Assignment |
| --- | --- |
| Systems / existing GameManager | Existing Gameplay.asset, GameplayLoop and PoolService; seed **12345** unchanged |
| Systems / GameplayLoop | Existing input/player plus RoadManager, WorldOrigin and SpawnDirector on Systems |
| RoadManager | `Configs/World.asset`; existing scene PoolService; `World/RoadSegments` |
| WorldOrigin | Same World.asset, RoadManager, existing player, existing Main Camera; `additionalParticipants=[]` |
| SpawnDirector | Same World.asset and Gameplay.asset |
| PoolService | Existing owner/Pools.asset/inactive top-level Pools storage, unchanged Task 04 API |
| World | Neutral root; neutral RoadSegments and separate empty Interactables children |
| World / ReferenceRoad | **Disabled**, original geometry retained for provenance; no overlapping active floor |
| Existing camera | Still parented to Player/CameraAnchor, local pose unchanged; no ChaseCamera/damping cache exists yet |
| World.asset | GUID `a3f0b189244308867b42882f15967d21`; ahead300, behind60, rebase1000, reaction0.7, margin0.3, candidateProbability0.65 |

| Pool key | Real prefab | Capacity | Prefab GUID |
| --- | --- | --- | --- |
| RoadOpen | RoadOpen60 | 8 | `d22b4640bb487dfb081b276161c4fe40` |
| RoadTunnel | RoadTunnel60 | 8 | `413c84f91350ee86587485145379c4c4` |
| RoadEntrance | RoadEntrance60 | 4 | `a1bed7ac9f140d1cabcdb6745e0bc845` |
| RoadExit | RoadExit60 | 4 | `e38623c39070b31a2a24fd1cb0bf0edb` |

Only these four entries are present: **24 prewarmed instances**. Traffic, pickup and VFX entries are absent and valid for this task. No placeholder entries or second pool service.

All roots have identity rotation/unit scale. Each surface is a **9.6 × 0.2 × 60 m** cube at `(0,-0.1,30)`, top y=0; entry `(0,0,0)`, exit `(0,0,60)`. Lane centers are x=-3.2/0/3.2, dividers ±1.6, edges ±4.8. Thin markings have no colliders. Rails, lamps and arch posts stay outside the driving corridor; ceiling underside exceeds 8 m. Only the surface has a collider. Open modules have primitive lamp meshes; tunnel modules have four arches/roof, entrance has architecture over its rear half, exit over its front half. Roof surfaces meet at common heights; arches are not duplicated at shared endpoints. ReferenceRoad/ReferenceMarking and one shared RoadArchitecture material are used. These are readable primitive grayboxes, without later lighting/art polish.

## Schedule, coverage and capacity proof

For absolute index modulo eight, the exact repeating slots are:

`0 Open, 1 Open, 2 Open, 3 Entrance, 4 Tunnel, 5 Tunnel, 6 Tunnel, 7 Exit`.

Transition modules are **additional** 60 m slots between the three open and three tunnel modules: one 480 m road cycle. Index 0 begins open; index -1 is the preceding Exit needed for initial behind coverage. Road cycles and protected-lane blocks both happen to be 480 m, but have independent data/RNG ownership. RoadStream generates DecorationVariant metadata per seed/index; GameplayStream generates lanes/occupancy. No UnityEngine.Random or cosmetic state is read. Road type rhythm is fixed; the seed changes variation metadata and gameplay plans.

For absolute player distance p, required indices are `floor((p-60)/60)` through `ceil((p+300)/60)-1`, inclusive. Exact boundaries need six modules; fractional positions need seven. Initial coverage is [-60,300], including negative index -1, before Playing. A large skipped interval calculates only its final required window, with no traversal/history accumulation.

Any seven-slot window contains at most 3 Open, 3 Tunnel, 1 Entrance and 1 Exit. Two disjoint windows need at most **6/6/2/2** of the individual keys and at most **14 simultaneous rentals overall**. Requested capacities **8/8/4/4** therefore leave at least two per key and ten overall even during worst reservation; committed coverage leaves at least seventeen overall. Actual 100 km tests reached peak reservations14, committed7. Capacities did not change. WorldConfig rejects larger coverage until its fixed storage/capacity proof is deliberately revised.

Coverage is preflighted synchronously **before player/session mutation**: retain old leases → reserve all missing target modules → if any rental fails, return just new reservations → otherwise return obsolete old leases and commit the complete sorted target window. This placement extends the architecture's end-of-tick maintenance with a safety preflight so even an unusually large tick cannot move into unreserved road. There is no rendered intermediate frame. Planning/rebase still run in the explicit end-of-tick world phase. Required runtime failure logs the index/key/failure and pauses before movement; Resume stays gated, while Restart/Menu can clean up. There is no skipped module, fallback allocation or growth.

## Coordinates, tick and reset ordering

RunSession.Distance is authoritative double forward travel. WorldOrigin.Offset is a separate double; SegmentIndex/BlockIndex/SpawnSequence are long. Road local Z is `(float)((double)index*60 - offset)`. Player keeps double absolute previous/current Z, and converts each local history after subtracting the offset. No subtraction of already-rounded large floats is used for these primary poses.

Tick: confirm Playing → reserve target coverage using double distance + configured cruise speed × dt → consume buffered lane command → predict player → assign absolute forward endpoint → MovePosition → record actual travel/time once → increment tick → fill bounded planning window → rebase → publish once. Task 06 inserts prediction/contact dispatch before pose commit and leaves rebase after contact processing.

At local z≥1000, delta is `floor(localZ/60)*60`; add it to Offset. Typical ordinary crossing shifts960 m, leaving about40 m; a large catch-up can shift farther in one operation. Recompute active segment poses from absolute coordinates. Teleport player body/transform, rebase both logical histories and reset interpolation history. CameraAnchor/MainCamera inherit that one player teleport; neither is shifted separately. No additional follow/damping caches exist in the current implementation. Absolute distance, lanes/transition phase, seed, plans and indices do not change. Storage stays neutral/inactive.

Start/restart: validate required configuration/readiness first → gate tick/input and clear queued commands → token-return current roads and clear plan/road arrays → PoolService.ReturnAll for other rentals → reset origin and road/planner seed → reset player pose/lanes/history → establish complete initial coverage and plan window → only on success reset Session with the supplied seed/new run ID/preserved best → enter Playing and enable gameplay actions. The parented camera snaps with the reset player. Session is not reset on invalid setup/failed coverage. Pause/resume keeps leases/tokens/plans/offset; GameOver freezes presentation. Return-to-menu gates, releases active content and clears bookkeeping. The next start resets origin/poses. Shutdown alone destroys pool-owned instances.

## Executed checks

All evidence paths below are relative to `Evidence/Task05/`. Test verdicts are actual Pipeline results.

| Check | New result | Evidence |
| --- | --- | --- |
| Compilation | **0 errors, 0 warnings**, compilationFailed=false | `compile.json` |
| Full EditMode | **81 passed, 0 failed/skipped** (64 prior cases +17 pattern cases) | `editmode-results.json`, `test-summary.json` |
| Full PlayMode | **65 passed, 0 failed/skipped** (42 prior cases +23 road cases) | `playmode-results.json`, `test-summary.json` |
| 22 m/s deterministic simulation | **100,592.71101155877 m**, 540 steps, **77 rebases** | Test results; `instrumentation.json` |
| 50 m/s deterministic simulation | **100,046.29900157452 m**, 239 steps, **47 rebases** | Same |
| V05 coverage/seams/catch-up | Every checked window covers ≥300 ahead/60 behind; seam error <0.001 m; exact/fractional/negative initial indices and up to31.25 s steps (1,562.5 m at cap) | RoadContinuityTests |
| Bounded memory / real pools | committed≤7, observed plans≤3 (fixed capacity4), peak simultaneous rentals14, created24, destroyed0, misses0 in both distance simulations | `instrumentation.json` |
| Rebase invariants | Body/transform, both logical histories, lane/transition, relative camera pose, segment/block indices, snapshot and inactive instances checked before/after; repeated rebases also exercised by long runs/restarts | RoadContinuityTests |
| Long absolute indices | Index **10^12** road coverage/local conversion, seams and block plans checked; double subtraction preserves fractional local placement | RoadContinuityTests / PatternSafetyTests |
| Prefab reset/seams | All six used type adjacencies, surface/endpoints/rotation/scale, metadata after reuse, surface-only collider verified | RoadContinuityTests |
| Deterministic safe data | **1,000 seeds ×21 blocks** (>10 km per seed), warmup/clear buffers/protected lane/stable ordering, all nine lane transitions; batch sizes1/3/17/210 and resets | PatternSafetyTests |
| Unsafe data rejection | Occupied protected lane, all-three occupancy, extra row bits, invalid lane, warmup occupancy, unsafe/nonfinite timing rejected | PatternSafetyTests |
| 50 road restarts / lifecycle | Alternating Paused/GameOver; each run rebases at least twice; same seed plans/initial coverage, zero origin/history residue, exact cleanup count and rejected stale tokens; menu releases all | RoadContinuityTests |
| Existing pool lifecycle | 12,000 probe cycles and 50 callback-instrumented restarts retained; lifecycle fixture now includes real road entries as needed | PoolLifecycleTests |
| Failure policy | Each missing required key and disabled origin blocks unchanged Menu/session/input; exhaustion after two reservations rolls those back and pauses before travel; stale lease detected before travel, restart recovers | RoadContinuityTests |
| Managed allocation samples | Initial reset/coverage **0 current-thread managed bytes** in each distance fixture (after prewarm); 1,000 warmed synchronous world/player/planning ticks **0 bytes** | `instrumentation.json` |
| Native saved-scene smoke | **Both cycles passed**, each at2,020.039202325046 m, tick4591, 2 rebases, offset1920, seam error0; see `play-sessions.json`: actual StartRun context command, every lane binding, native FixedUpdates beyond300 m and through two rebases, Escape pause/resume, Paused/GameOver restart, return-to-menu; normal and disabled reloads | `play-sessions.json`; `native-*.png` |
| Additional native cap-speed capture | **2,021.9992015510798 m** at50 m/s, 2 rebases, offset1920, player local101.999, camera relative(0,4.4,-10), created24/destroyed0/misses0; paused before frame-end screenshot/cleanup | `native-rebase-capture.json`, `native-after-two-rebases.png` |
| Final Editor / validator | Saved Game clean, stopped; Instance clear/timeScale1; ordinary player graph only NeonTunnelRunner.Runtime with InputSystem/uGUI/TMP references; Console **0 errors / 0 warnings** | `validator.json`, `final-editor.json`, `console.json` |
| Visual seam inspection | Captured Game views at open/entrance/tunnel/exit and after rebase inspected; three lanes, joined floors/markings, architecture outside corridor | `native-170.png`, `native-240.png`, `native-350.png`, `native-450.png`, `native-1005.png` |
| Serialized preservation | **221 protected files byte-identical**, including all old metas, pooling/input/player prefab, Packages/ProjectSettings/Preproduction; no missing metas/duplicate GUIDs; ignore matches assignment | `preservation.json` |

The first run passed81 EditMode and57/59 PlayMode cases. Its two failures were obsolete test expectations: comparing local player Z to absolute distance after rebase, and assuming >50 rebases despite 1.56 km catch-up ticks. Assertions now use the origin offset and an appropriate repeated-rebase lower bound; new long-index/missing-key cases were added. Final numeric review also added rejection of an unrepresentable empty coverage window (for example1e20 m), with a regression asserting that existing leases are retained. The failed results remain in `playmode-first-results.json`; they are not passing evidence. Initial external setup-recipe compilation reported two deprecated FindFirstObjectByType warnings; the recipe now uses FindAnyObjectByType. Project compilation was clean. The original final smoke screenshot completed after reset/Menu, so it is labeled `native-menu-after-reset.png`; the supplemental cap-speed capture pauses before frame-end capture, and the recipe now awaits capture before cleanup. Test Console history includes deliberately expected failure diagnostics; it was archived and cleared before native smoke/final Console checks.

Allocation scope: pool prewarm creates24 real modules and bootstrap creates the session/input clone/fixed arrays. **Bootstrap bytes were not measured.** Reset and warm-tick samples above exclude that bootstrap, asynchronous engine/physics/render frames, snapshots with subscribers, screenshot tooling and future systems. They do not establish whole-game zero GC, native-memory stability or FPS. No Profiler capture, player build, 30-minute visible soak, full moving-vehicle envelope validation or fuel placement was run/claimed. Those remain pending in their scheduled tasks.

## Task 06 API handoff

- `GameplayLoop.Roads`, `.Origin`, `.Planner`, `.Player`, `.Input` are explicit read-only service accessors. `Step(dt)` is the sole simulation tick. Extend it with active traffic prediction, swept-contact collection/dispatch and deferred returns **before** commit; world origin remains after contacts/commit. Road target coverage is already guaranteed before mutation. No per-object Update movement.
- `SpawnDirector.ResetRun(seed)` and `FillWindow(absoluteDistance)` are internal loop-owned lifecycle operations. `PlanCount`/`GetPlan(i)` expose only the fixed current window; do not retain pooled object references or append an unbounded history. `PlanAt(long blockIndex)` deterministically reconstructs immutable data in O(1) space. Invalid proposals throw before becoming live content; startup WorldConfig validation rejects unsafe global timing. There is no dependency on the player's chosen lane, generation call count, origin or cosmetic RNG.
- `BlockPlan`: Seed, long BlockIndex, ProtectedLane, NextProtectedLane, PackedRows; double AbsoluteStart, ProtectedEnd/BufferStart, AbsoluteEnd. Protected span is `[start,start+360)`; entire tail `[start+360,start+480)` is traffic-free and permits transition to NextProtectedLane, which matches the next block. `OccupancyAt(row)` is a three-bit mask for lanes -1/0/+1. `RowPosition(row)` returns absolute Z for six rows at offsets0/60/120/180/240/300. Warmup candidates with absolute Z<180 are absent. Protected lane is always absent; each row has at most two candidates.
- `TryGetCandidate(row,lane,out TrafficCandidate)` returns value data: Seed, BlockIndex, Row, Lane, double AbsolutePosition, long SpawnSequence. Consume in ascending block→row→lane(-1..+1) order. Stable order is `block*18 + row*3 + lane+1`; omitted slots leave gaps. **These are candidates, without a vehicle type/speed or bounds guarantee.** Task 06 must apply the documented full stationary/moving bounds/envelope reservations (including neighboring spans/buffers and despawn horizon) before renting. It may reject/omit unsafe candidates. Moving cars at8 m/s, at least one valid moving seeded case, collisions and damage remain Task 06; fuel guarantees remain Task 07/08. Do not infer safety for moving bodies from row-center validation.
- `PatternValidator.Validate(plan, worldConfig, gameplayConfig, out PatternRejection)` validates the current data subset. `CanTransition(from,to,world,gameplay)` uses **actual** MaximumSpeed and LaneTransitionDuration. Defaults:120/50=2.4 s >0.7+2×0.28+0.3=1.56 s. WorldConfig rejects globally unsafe timing before Playing. No fairness repair reacts to last-second player input.
- Pool API is **unchanged** from Task 04: `TryRent(new SpawnContext(key, localPosition, Quaternion.identity, activeParent, lane, absoluteDistance, seed, speed, spawnSequence), out PooledObject)`. Local position is relative to the assigned neutral root and computed **after** double absolute-minus-offset subtraction. Capture the returned `ActivationId`; use `Return(item,capturedToken)` or `ReturnToPool(capturedToken)`. Pool owns callbacks/validation/activation. Do not invoke hooks manually or bypass stale-token validation. No growth/fallback allocation.
- `RoadLease` is readonly Segment+ActivationId; IsCurrent includes token/rental/active checks. RoadManager owns its arrays and returns. Future traffic belongs under **World/Interactables**, never beneath an individual RoadSegment. Independent traffic rental lifetime is required.
- `WorldOrigin.Offset` is double; `RebaseIfNeeded(absolutePlayerPosition)` is loop-only. `IWorldOriginParticipant` has `OriginRoot` and `ShiftOrigin(double newOffset,double delta)`. Add a future registry/VFX owner component to the serialized `additionalParticipants` array: its root must be disjoint from player, roads, storage and other participant roots. That owner shifts only its active instances, teleports bodies and updates both logical histories/caches from absolute values. Register the owning root/registry once; never also shift its children or inactive pool storage. No speculative Interactable/VFX classes are introduced here.

## Reproduction and requirement status

Run from repository root with Game saved and the Editor stopped:

```bash
TASK05_CLI=/home/alireza/.local/bin/unity.previous
TASK05_PROJECT=/home/alireza/NeonTunnelRider/NeonTunnelRunner
"$TASK05_CLI" recompile --project-path "$TASK05_PROJECT" --format json
"$TASK05_CLI" status --until-ready --project-path "$TASK05_PROJECT" --format json
python3 Docs/Setup/Task05Tests.py
python3 Docs/Setup/Task05PlaySessions.py
"$TASK05_CLI" command --caller plugin --skill unity-cli script run \
  --file Docs/Setup/Task05Setup.cs --entry NeonTunnelRunner.Task05Setup.Inspect \
  --project-path "$TASK05_PROJECT" --format json
```

The test runner discovers real suites with `test run --mode editor|playmode --filter NeonTunnelRunner.EditModeTests|NeonTunnelRunner.PlayModeTests --filter_type assembly --async_tests true`, polling `test status` sequentially. Smoke restores action focus/device/reload settings, disables its auto-tick override, and hashes the three config assets. Setup.Configure authors the Task 05 assets/wiring; use Inspect for ordinary validation.

**R08/R09:** direct passing scoped evidence for runtime incremental endless coverage/recycling. **R06:** three lanes and existing third-person camera preserved/tested. **R05:** endless-world portion only; later game features are incomplete. **R22:** first real road-prefab integration passes; traffic/coin/fuel integration remains pending. **R23:** no recurring allocation of objects/destruction/growth in the tested normal road path; full-game/future-system profiling remains pending. **V05/V06/V21:** task-specific geometry/pool/pattern-data subsets pass; the acceptance plan's 30-minute soak, Profiler and full moving/pickup fairness checks remain pending.

Task 05 only. No traffic/pickups/collision/damage, survival completion, score/persistence, menus, audio/VFX, camera polish or generalized resource framework. Local checkpoint commit only; no push.

Next: **Task 06 — Interactable inheritance and traffic collisions**.
