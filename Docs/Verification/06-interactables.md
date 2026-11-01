# Task 06 — Interactable inheritance and traffic collisions

Implemented and verified 2026-11-02 (Asia/Tehran), beginning on clean `main` at `22f4e5e9407464ecff4643b0e4bada7a195e76bf`. No newer commits, user edits or applicable AGENTS.md files were found. Inspected the actual pool/world/player/lifecycle implementations, saved scene, assets and tests; read the Task 04/05 handoffs, original Task 06–08 prompts, requirements/design/architecture/structure/acceptance and environment documents. Both handoffs existed and matched their implementations. Historical 146-test evidence was not reused as Task 06 verification.

Environment preserved: Unity **6000.6.3f1**, CLI **1.0.0-beta.13** (`/home/alireza/.local/bin/unity.previous`), official Unity plugin **0.1.8-beta**, Pipeline **0.9.1-exp.1**, Linux Mint Virginia/glibc **2.35**. Resolved URP/Core **17.6.0**, Input System **1.20.0**, Test Framework **1.8.0** remain unchanged. The pre-existing default `unity` is beta.14; only initial read-only discovery used it, then the pinned beta.13 drove the existing Editor. No package/tool upgrade, glibc replacement, external asset download, build or push. The installed `unity:unity-cli` skill supplied the automation workflow.

## Changes and ownership

Paths below are under `Assets/NeonTunnelRunner/` unless noted.

- Added `Scripts/Interactables/Interactable.cs`: actual abstract `PooledObject` subclass, guarded encounter dispatch, virtual interaction hook, logical bounds, previous/current local and double absolute poses, per-activation lane/speed/sequence, registration and visual reset. `InteractionContext`, `Encounter` and `InteractionCategory` are typed support values beside their owner.
- Added `StationaryCar.cs` and `MovingCar.cs`: both override `OnInteract` and call survival; their overridden absolute speeds are **0** and **8 m/s**. No independent Update/FixedUpdate, enemy steering, trigger damage or wheel physics.
- Added `Scripts/World/SweptBounds.cs`: pure relative swept AABB slab test, inclusive initial overlap/grazing, parallel-axis handling and TOI.
- Added `Scripts/Player/InteractionSensor.cs`: fixed contact array, segmented player sweeps against time-matched moving target endpoints, deterministic TOI/category/sequence ordering, captured-token revalidation and deferred dispatch cleanup. Overflow throws visibly.
- Added `PlayerSurvival.cs`: damage acceptance, one playing-second grace deadline, clamped AddFuel and Inspector debug lives/grace. Lives/fuel remain owned solely by RunSession. Added value-only `DamageEvent` and instance `RunSession.Damaged`; observers detach on shutdown.
- Extended `PlayerController.cs`: up to three motion legs per prediction (active transition, queued transition, remaining straight travel); rebase/reset covers these caches and the existing absolute histories. Input/actions and lane queue semantics are retained.
- Extended `SpawnDirector.cs`: keeps the original counter-based block generator/streams and four-plan window; adds a capacity-derived registry and deferred lease arrays, bounded traffic frontier, rent/recycle/predict/commit/origin operations and 64-entry diagnostic ring. Traffic is independent of road leases.
- Extended `PatternValidator.cs`: full conservative longitudinal envelopes against reconstructed neighboring reservations; invalid proposals fall back to validated stationary cars or omission. No unknown-future reservation is assumed safe.
- Extended `SpawnContext` with optional explicit `SpawnDirector Registry`; existing positional callers remain compatible. `PoolService` rejects registry rentals during dispatch and defers matching traffic returns until dispatch ends; its fixed catalog, monotonic tokens, road entries and no-growth policy remain intact.
- Extended `GameplayLoop`, `GameManager`, `WorldOrigin` and existing `ProjectSetupValidator`: explicit dependencies, phase ordering, reentry guard, reset cleanup and one disjoint traffic origin participant. No new singleton. A small debug refresh after Session.Reset avoids displaying outgoing lives in the Inspector until the next tick.
- Added real `Prefabs/Interactables/{StationaryCar,MovingCar}.prefab` and shared orange/red URP/Lit materials. Updated existing `Pools.asset`, player prefab and `Scenes/Game.unity` through the Editor. Added Player/Obstacle layers through TagManager; Unity also migrated that asset's serialization to its current version.
- Added `Tests/EditMode/SweptBoundsTests.cs`, `Tests/PlayMode/InteractableTests.cs` and test-only `InteractionProbe.cs`. Extended pattern and existing fixture/road/pool lifecycle tests. The probe exercises the future pickup category without providing a production pickup or reward implementation. Existing asmdefs needed no changes.
- Repository-root `Docs/Setup/Task06{Setup.cs,Audit.cs,Tests.py,Smoke.cs,PlaySessions.py}`, `Evidence/Task06/`, this note and CREDITS.md record construction and verification. Setup/smoke recipes stay outside Assets/player assemblies.

All old metas/GUIDs, input actions, road prefabs, Gameplay/World configs, packages, Preproduction and supplied ignore bytes were preserved: **205 protected tracked files unchanged**, no missing asset metas or duplicate GUIDs (`preservation.json`). Existing core tests remain; expected pool totals were updated for the new real car entries. No prerequisite behavior required replacement.

## Saved Inspector and prefab assignments

| Owner | Assignment |
| --- | --- |
| Systems/GameManager | Existing config, loop, pool service and seed **12345** retained |
| GameplayLoop | Existing player/input/roads/planner/origin; Sensor and Survival on existing Player prefab instance |
| Player/InteractionSensor | Same PlayerController; scene SpawnDirector registry; same PlayerSurvival |
| Player/PlayerSurvival | Existing scene GameManager; session supplied explicitly at bootstrap |
| SpawnDirector | Existing World.asset and Gameplay.asset; existing PoolService; neutral **World/Interactables** |
| WorldOrigin | Existing player/roads/camera; additionalParticipants contains **SpawnDirector once** |
| Player prefab | Existing Rigidbody, logical box, Visual and CameraAnchor retained; new sensor/survival components; root layer **Player (8)** |
| Car prefabs | Identity/unit-scale roots, layer **Obstacle (9)**, enabled root Interactable subclass; root BoxCollider is a trigger, center0, size **2.2×1.2×4.4 m** |
| Car visuals | Separate Visual child; body, dark cabin and two tail lamps; stationary orange, moving red; no child colliders or runtime material creation |
| Moving prefab | Kinematic gravity-free Rigidbody, interpolation, frozen rotation/X/Y; +Z movement supplied only by loop prediction/commit |
| Active traffic/storage | Traffic children remain under World/Interactables. Root remains neutral; each active item shifts its own body/histories. Inactive Pools storage never shifts. |

Pool catalog is now **120 prewarmed instances**, ready before Playing:

| Key | Capacity |
| --- | ---: |
| RoadOpen / RoadTunnel | 8 / 8, unchanged |
| RoadEntrance / RoadExit | 4 / 4, unchanged |
| Stationary / Moving | **64 / 32** |

Pickup/VFX entries are still absent and valid. The runtime registry, contact buffer and deferred-return buffer each hold **96** interactables, derived from actual Interactable catalog entries during bootstrap. The test-only pickup catalog verifies automatic growth to **97 before Playing**. There is no gameplay-time pool/buffer expansion.

## Deterministic generation and safety

The original BlockPlan/TrafficCandidate values, protected lane schedule and occupancy RNG are retained: **180 m warmup**, **60 m rows**, **480 m blocks**, **360 m protected span**, **120 m completely clear transition buffer**, at most two non-protected lane candidates per row. Counter addresses64+row×3+lane+1 select moving/stationary proposals independently of occupancy/road cosmetics and rental success. Iteration remains block/row/lane -1→0→+1; gaps in the existing long sequence represent omitted proposals.

For a candidate, minimum player speed is configured StartingSpeed **22**, maximum traffic speed **8**. The center horizon is

`H = (spawnZ - playerZ + Behind + obstacleHalfZ) / (22 - 8)`.

Behind is **60 m**. The additional obstacle half length accounts for recycling only after the entire car passes the behind boundary. The complete interval `[spawnZ - (obstacleHalfZ+playerHalfZ), spawnZ + speed*H + (obstacleHalfZ+playerHalfZ)]` is checked. Authored obstacle/player half lengths **2.2/1.8 m** therefore provide **4 m** longitudinal padding. Lane centers3.2 m apart exceed combined lateral half extents1.95 m. Every touched block is reconstructed by PlanAt, including outside FillWindow; touching a clear buffer or the lane's protected span rejects the proposal. A bounded lookahead limit rejects pathological inputs rather than iterating indefinitely. Nonpositive/nonfinite speed denominators and invalid extents fail validation.

Reservations are conservatively treated as forbidden for the entire envelope: this may reject traffic that a time-dependent planner could allow, but it preserves an empty route/buffer. Row0's full bounds overlap the previous buffer, so that proposal is omitted. The center at180 m is also omitted because its bounds enter the warmup. Interior early rows still accept real moving cars. An invalid moving path is never repositioned: a separately validated stationary fallback may keep that exact candidate pose, otherwise the slot is omitted.

The frontier visits a candidate once. Ordinary ticks introduce rows at roughly300 m ahead. Large catch-up ticks omit skipped rows and admit only candidates at least180 m ahead; they never replay a missed near-player spawn. Rental exhaustion omits the selected candidate without an extra RNG sample, allocation or deferred retry. Same seed/config/tick schedule gives the same plan and decisions; deliberately skipped catch-up content can differ across tick schedules, as allowed by the original deterministic contract.

Diagnostics retain the last64 values: candidate seed/block/row/lane/spawn sequence, selected kind, rejection or pool miss, ahead/behind/minimum/maximum speed/probability. Latest seed/block/sequence/rejection/miss are visible in the SpawnDirector Inspector without recurring log/string allocation. Car gizmos show logical boxes. The test log records aggregate seed/config and actual acceptance counts.

## Tick, contact and callback order

The existing road target-window preflight still occurs before movement/session mutation. Required-road failure retains coverage and pauses before travel. Then the sole GameplayLoop tick:

1. Guards Playing and recursive Step; consumes one existing buffered lane command.
2. Predicts player motion legs/absolute endpoint and every active traffic endpoint.
3. Sweeps each player leg against the corresponding target time interval using both logical movers, then insertion-sorts contacts by **TOI → Pickup(0)/Obstacle(1) → spawn sequence**.
4. Captures/revalidates activation leases; constructs value Encounter data at the contact time; calls guarded TryInteract. The guard marks encountered **before** OnInteract/damage/events, even when grace rejects damage.
5. Ends dispatch; flushes deferred returns with their captured tokens; clears contact slots. Registry mutation/rental during dispatch is forbidden. Matching direct PoolService/ReturnToPool traffic returns are intercepted into the same queue.
6. Commits player/traffic poses and records distance/time once; refreshes Inspector survival values.
7. Maintains bounded plans, recycles/fills traffic, rebases active histories/bodies once, publishes snapshot.

No damage comes from physics callbacks. Trigger colliders document geometry and prevent physical shoving; kinematic bodies mirror the logical path. Broadphase/CCD is not the acceptance mechanism.

TryDamage removes exactly one life if lives>0 and the encounter playing time has reached GraceUntil. The deadline becomes contactPlayingTime+configured1 second. TOI times allow two contacts more than a second apart in one unusually long tick to be evaluated correctly. Pause freezes authoritative elapsed time; persistent overlap and grace-rejected activations stay encountered forever for that rental. An obstacle remains visible until ordinary behind recycling. Re-renting resets all guard/pose/visual state with a fresh pool token. Damaged publishes once only for accepted damage, carrying RunId/Encounter/RemainingLives values and no reusable object reference.

GameManager commands and recursive Step are rejected during a simulation transaction, including event callbacks; no GameOver occurs inside iteration. Existing external EndRun behavior is retained. Task 08 must add final terminal evaluation at a safe phase after contact iteration and coordinate it with this transaction guard. AddFuel accepts finite nonnegative amounts, clamps at configured capacity and mutates Session only while Playing; no fuel drain or pickups are present.

## Reset, pause and origin

Cleanup gates simulation and input, clears lane/contact/deferred work, returns **interactables before roads**, resets grace and registry/planning state, then lets existing PoolService.ReturnAll clean any remaining rentals. New-run preparation resets origin, deterministic frontier/seed, player histories/lanes, initial road coverage and safe warmup traffic. GameManager resets the authoritative Session and refreshes debug lives, then commits Playing/input. No new pool instances are created on restart.

Pause/resume preserves traffic leases, poses, encountered flags and grace; Menu cleanup releases all traffic. WorldOrigin calls SpawnDirector once after commit/contact processing; each active car recomputes previous/current local Z from its double absolute history and teleports its Rigidbody/transform. The registry root and storage remain neutral. Absolute movement, spawn indices, lanes and encounter guards are unaffected. Existing player-owned camera reset/rebase remains with the player.

## Executed verification

Evidence paths here are relative to `Evidence/Task06/`. Results below are Task 06 executions.

| Check | Actual result / evidence |
| --- | --- |
| Compilation | Final **0 errors, 0 warnings**, compilationFailed=false; `compile-final.json` |
| Full EditMode | **96 passed**, 0 failed/skipped; `editmode-results.json` |
| Full PlayMode | **82 passed**, 0 failed/skipped; `playmode-results.json` |
| Total | **178 passed**; `test-summary.json` |
| Inheritance / motion | Reflection asserts abstract base and both real overrides; 0/+8 absolute speeds, pause, both histories/rebase and untouched inactive storage; InteractableTests |
| V08 swept contacts | 22/50 m/s, moving targets, disjoint endpoints, initial overlap, parallel/near-zero axes, exact grazing, thin box, lane crossing, bent queued transition, TOI and invalid inputs; SweptBoundsTests/InteractableTests |
| V09 damage subset | One life/event per accepted encounter; duplicate/persistent overlap; new activation during grace consumed without damage or later damage; grace expiry, reused tokens, pause-frozen deadline, same-tick contacts >1 s apart, clamp to0; no automatic terminal resolution claimed |
| Callback safety | Direct returns deferred until after dispatch; stale collected contacts and stale returns rejected after reuse; reentrant Pause/EndRun/Restart/Step rejected; artificial contact overflow throws; synthetic Pickup category wins exact tie and returns after dispatch |
| Seed safety | **1,000 seeds ×21 blocks (>10 km) ×2 player speed cases (22/50)**. Accepted **52,142 moving / 213,156 stationary**, omitted **54,402**; full extents and horizon checked. Unsafe bounds, previous-buffer overlap, neighboring-block paths, protected lanes and nonpositive denominator rejected; existing tests cover all9 lane transitions. `instrumentation.json` / PatternSafetyTests |
| 100 km @22 | **100,592.71101155877 m**, 540 steps, **77 rebases**, moving108/stationary330 spawned, contacts170, created120/destroyed0/misses0, peak simultaneous rentals22 |
| 100 km @50 | **100,046.29900157452 m**, 239 steps, **47 rebases**, moving51/stationary157 spawned, contacts66, created120/destroyed0/misses0, peak simultaneous rentals21 |
| Road/input regression | Both long runs retain coverage≥300 ahead/60 behind, seams<0.001 m, committed roads≤7, plans≤3 and bounded local coordinates. Existing action/lane/pool/lifecycle tests pass. `instrumentation.json`, test verdicts |
| Pool exhaustion | Both real traffic pools exhausted; no growth/registration corruption, candidate omission and no near-player retry; deterministic plan unchanged; stale activation tokens cannot return current rentals |
| Reset stress | New50 traffic resets plus existing50 road and50 callback-instrumented pool resets; active traffic, pause/GameOver, multiple rebases, clear contacts/grace/origin, new tokens and Menu cleanup verified |
| Rebase contact | Actual moving-car contact on a threshold tick; one damage event, correct absolute +8 travel and no artificial next-tick hit |
| Allocations | A retained **1,000 warmed synchronous tick sample measured0 current-thread managed bytes** with traffic, and initial reset sample0 excluding prewarm. This narrow sample is **not a Profiler result** or evidence of whole-game/engine zero GC. `instrumentation.json` |
| Preservation | 205 protected tracked files byte-identical, old metas/GUIDs intact, no missing metas/duplicate GUIDs, supplied ignore unchanged; `preservation.json` |

A first validator compile exposed an internal/public access mismatch; fixed before setup. An immediate command during ordinary domain reload initially found no Pipeline instance; status --until-ready confirmed the same Editor and setup then completed. These are not successful verification claims. `compile-wiring.json` retains that diagnosed compile error; `compile-final.json` is the final clean result. No failed tests were observed: first full run96+80 passed; pickup-priority and paused/foreign-session guard regressions raised the final result to96+82. A final API review added owner/session/Playing validation before the encounter flag, preventing direct paused or foreign-context calls from consuming an activation.

Saved-scene smoke **passed twice**, with ordinary reloads and with both domain/scene reload disabled (`play-sessions.json`). Each run used the Editor StartRun command and native A/D/LeftArrow/RightArrow events, Escape pause/resume, Paused/GameOver restart and ReturnToMenu. All native frames/ticks advanced: final snapshots were **2020.879202 /2020.879202 m**, ticks **2022 /2022** (normal/disabled reloads), with **2 rebases**, offset**1920**, local Z **100.879200 /100.879200 m**, seam error**0**. Metrics are sampled together before the screenshot wait; a <0.001 m distance-minus-offset check now guards the native log. Each saw both real car types and spawned **5 moving /25 stationary** cars; active traffic8, pool peak16, created120/destroyed0/misses0. Each recorded **one accepted damage event**, lives3→2, sequence12 at absoluteZ240 and playing time**4.742399860715182 s**. Configuration asset hashes were unchanged, exiting cleared Instance and restored timeScale1, and Editor reload options were restored.

`native-{170,240,350,450,560,1005,2020}.png` are native Game view captures from the smoke (the second cycle overwrites the same milestone files). Inspected `native-240.png` showing the orange encounter and `native-560.png` showing moving red and stationary orange traffic with joined three-lane road. These are graybox visuals; no collision flash or production presentation is claimed.

Final validator/audit **passed**, saved Game clean/stopped, Instance clear, timeScale1, auto-tick disabled. The ordinary player assembly graph contains only NeonTunnelRunner.Runtime with InputSystem/uGUI/TMP references, with no Pipeline/Editor dependency. Final Console ground truth: **0 errors /0 warnings**, compiling=false, compilationFailed=false (`final-editor.json`, `console-status.json`, `console.json`). Expected test diagnostics were archived in `test-console.json` and cleared before native smoke; the final Console was sampled after smoke/audit.

## Commands and evidence reproduction

Use the pinned beta.13 CLI and this checkout's Editor; do not run another setup task's destructive construction recipe on the completed scene.

```bash
TASK06_CLI=/home/alireza/.local/bin/unity.previous
TASK06_PROJECT=/home/alireza/NeonTunnelRider/NeonTunnelRunner
"$TASK06_CLI" status --until-ready --project-path "$TASK06_PROJECT" --format json
"$TASK06_CLI" recompile --project-path "$TASK06_PROJECT" --format json
"$TASK06_CLI" command --caller plugin --skill unity-cli --project-path "$TASK06_PROJECT" --format json script run --file Docs/Setup/Task06Setup.cs --entry NeonTunnelRunner.Task06Setup.Configure
python3 Docs/Setup/Task06Tests.py
python3 Docs/Setup/Task06PlaySessions.py
"$TASK06_CLI" command --caller plugin --skill unity-cli --project-path "$TASK06_PROJECT" --format json script eval --code 'NeonTunnelRunner.ProjectSetupValidator.ValidateProject();'
"$TASK06_CLI" command --caller plugin --skill unity-cli --project-path "$TASK06_PROJECT" --format json console status
```

Setup runs in a saved stopped Game scene and appends/updates only the two traffic entries, preserving required roads. Tests use actual Pipeline `test run --mode editor|playmode --filter NeonTunnelRunner.EditModeTests|NeonTunnelRunner.PlayModeTests --filter_type assembly --async_tests true`, followed by `test status` to a completed verdict. The Python harness checks all verdicts and writes the per-case results. Smoke uses `editor set_auto_tick`, native Input System state events, the Editor StartRun context command and actual FixedUpdates. Its cap-speed selection is fixture-only reflection outside Assets; no cheat key polling ships. Fixtures restore timeScale and dispose temporary objects/devices/input settings.

## Task 07 API handoff

| API / data | Contract to extend |
| --- | --- |
| `abstract Interactable : PooledObject` | Add Coin/FuelCan as real subclasses overriding protected `OnInteract(InteractionContext)`. Use inherited `TryInteract` encounter guard; do not add another collision path. Override Category to Pickup. If overriding pool callbacks, preserve `base.OnRent(context)` / `base.OnReturn()` registration/reset behavior. |
| `InteractionContext` | Read-only Session, Survival and value Encounter. Add the real Progression service when Task 07 implements it; do not create a second session or use pooled references in event payloads. |
| `Encounter` | Kind, captured ActivationId, long SpawnSequence, Lane, double AbsoluteZ, LocalPosition and authoritative PlayingTime at TOI. The local position is the target root center at contact time; capture values before requesting return. |
| Category ordering | Pickup=0, Obstacle=1; sort TOI first, then category on exact tie, then stable spawn sequence. Pickups are processed during grace. Synthetic probe test establishes this ordering. |
| Reward/events | Base marks consumed before virtual callback. Implement actual AddCoin/score and value-only Collected events in Task 07; no placeholder reward service/event was added here. Existing `RunSession.Damaged` and `DamageEvent` show instance event/value ownership. |
| `PlayerSurvival.AddFuel(float)` | Finite nonnegative amount; clamps current session fuel to GameplayConfig.FuelCapacity while Playing. Task 07 can call configured refill30. Full-fuel pickup must still consume/publish its own one collection event. No drain/EvaluateEnd yet. |
| `SpawnContext(..., long spawnSequence=0, SpawnDirector registry=null)` | Preserve existing parameters; pass this registry for every live interactable. Parent=World/Interactables, local Z=(float)(absoluteZ-Origin.Offset), identity rotation, unit authored scale, explicit lane/seed/sequence. Pool assigns token; caller captures it after rent. |
| Pool leases | Existing `TryRent(context,out PooledObject)`, `Return(item,capturedToken)`, `ReturnToPool(capturedToken)` unchanged in meaning. `TrafficLease` stores Item/captured ActivationId and IsCurrent. All stale/zero/foreign/duplicate tokens fail. Never derive expected token later from a reused object for delayed work. |
| Deferred recycle | `SpawnDirector.DeferReturn(item,capturedToken)` queues during dispatch and returns immediately otherwise. Matching direct pool returns also defer. Registry mutations/rentals must wait until dispatch completes. Obstacles stay visible; pickups may request their return after reward/event values are captured. |
| Registry/capacity | `GetActive(index)`, ActiveCount/Capacity and captured leases are reusable array data. Bootstrap sizes registry/sensor/deferred buffers from **all actual Interactable prefab capacities**, so adding Coin160/Fuel24 yields280 automatically. No normal-play resize. |
| Safe-slot data | Existing `BlockPlan` exposes ProtectedLane/NextProtectedLane, six OccupancyAt rows, absolute start/protected end/buffer/end; `TrafficCandidate` carries seed/block/row/lane/Z/sequence. Packed occupancy describes proposals, not guaranteed live cars. `TrySelectTraffic(candidate,playerZ,out key,out rejection)` applies traffic safety/fallback. Reserve reachable fuel slots on protected spans/clear buffers; validate pickup extents/intervals and actual traffic envelopes rather than treating rejected proposal bits as geometry. |
| `PatternValidator.ValidateMovingEnvelope(...)` | Uses configured minimum22/max traffic8, full bounds/behind horizon, deterministic PlanAt lookahead and conservative reservations. Keep its proof when adding pickups; do not replace unknown future data with safe assumptions. |
| `SpawnDirector.PlanAt(block)` | Still reconstructs the original immutable plan independent of live pools/current player lane/cosmetic RNG. Plans remain bounded to4 in FillWindow; no growing history. Use a distinct deterministic address/sequence domain for new pickup choices. |
| Origin/recycle | SpawnDirector already owns independent active interactables, shifts each active item's logical histories/body exactly once and recycles behind by full half length. Coin/Fuel roots must remain under the same root; do not register them separately with WorldOrigin or parent them to roads. Visual spin/bob belongs on child; extend/reset child state without moving logical bounds. |
| Reset / Task 08 | Sensor.Clear → planner returns interactables → roads release → origin/session/frontier/player reset. Preserve phase guards. Task 08 adds fuel ticking/final lives/fuel evaluation after contacts, with no EndRun inside dispatch and a deliberate safe exit from IsStepping before lifecycle transition. |

R13 damage and R24 both obstacle types have executed evidence. R22/R23 are demonstrated for traffic/roads through bounded creation/reuse counters and source paths; whole-game performance still requires later profiling and remaining pickup/VFX implementations. **R26 remains partial** until Task 07 adds Coin and FuelCan. **V09 visual feedback, V21 pickup fairness/guaranteed fuel, full survival/terminal completion and presentation remain pending.** No pickups, fuel drain, damage flash/particles/audio, progression, HUD/menu production or persistence were implemented.

**NOT RUN:** standalone build, Profiler capture, 30-minute visible soak, the full V21 30-minute schedules/mid-difficulty/pickup-fairness matrix. The executed1,000-seed10 km traffic sweep is the scoped safety evidence; it does not complete those later checks. No tooling blocker remained for the completed automated work. Next: **Task 07 — Coins and fuel pickups**. This task's checkpoint stays local; nothing is pushed.
