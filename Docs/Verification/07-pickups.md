# Task 07 — Coins and fuel pickups

Implemented 2026-11-02 (Asia/Tehran), beginning on clean `main` at Task 06 checkpoint `30047a80124f71fd18fd69cbe7072265786d11c1`. The Git/Unity project root is the inner `NeonTunnelRunner/` directory. No applicable AGENTS.md, missing handoff, newer work or initial user edits were found. Inspected the actual pool, registry, sensor, survival, session, block plans, tests, prefabs, configuration and saved Game scene; read the Task 04–06 handoffs, original Tasks 07–09 prompts, requirements/design/architecture/structure/acceptance and environment documents. Previous 178-test results are historical evidence only.

Environment retained: Unity **6000.6.3f1**, CLI **1.0.0-beta.13** (`/home/alireza/.local/bin/unity.previous`), official Unity plugin **0.1.8-beta**, Pipeline **0.9.1-exp.1**, Linux Mint 21.3 Virginia/glibc **2.35**. URP/Core **17.6.0**, Input **1.20.0**, Test Framework **1.8.0**, NUnit **2.1.0** remain resolved and unchanged. The pre-existing default PATH CLI is beta.14; only initial read-only discovery used it. All editing/verification used beta.13 and the same running Editor. The installed `unity:unity-cli` skill supplied the workflow. No upgrades, glibc changes, new packages, external assets, new input bindings, push or deployment.

## Changes and ownership

Paths here are under `Assets/NeonTunnelRunner/` unless explicitly repository-root.

- Added `Scripts/Interactables/Coin.cs` and `FuelCan.cs`, both directly deriving from the real abstract `Interactable` and overriding `OnInteract`; both override Category to Pickup. StationaryCar and MovingCar remain real overriding subclasses. The shared activation guard marks Encountered before virtual behavior and any callback.
- Extended `Interactable` with child-only pickup spin/bob, phase/visibility restoration, immediate visual/collider hiding and token-checked recycle helpers. Logical roots and boxes never spin/bob. Animation advances only in the loop's playing prediction phase. No extra sensor, trigger handler, Update or physics damage/collection path.
- Added `Scripts/World/ProgressionSystem.cs`: explicit GameManager/session dependency, `AddCoin()`, `RecomputeScore()`, safe pure `ComputeScore(double,int,int)`, Inspector coin/score mirrors. RunSession still exclusively owns distance, coins and score. No difficulty or persistence.
- Extended `Core/RunSession.cs` with instance `Collected` and value-only `CollectedEvent`; `InteractionContext`/sensor now carry the explicit progression service. Added fuel Inspector mirror to PlayerSurvival, preserving its existing AddFuel/grace rules.
- Extended `World/PatternValidator.cs` with immutable `PickupPlacement`, `BlockPlan.GetFuel/GetCoin`, fuel-route/link and conservative pickup-envelope validation. Extended `WorldConfig.TryValidate` to reject a buffer fuel deadline that is stricter than merely reaching the next block. Actual configuration assets are unchanged.
- Extended `SpawnDirector`: typed fuel preflight/rollback transaction, dedicated optional coin frontier, safe fuel reclaim/retry, pickup diagnostics and counters. Existing road/traffic random addresses, occupancy, selection and omission behavior remain unchanged. `RoadSegment.cs` adds only a separate PickupStream constant beside the existing counter-based generator.
- Extended GameplayLoop/GameManager with progression wiring/debug refresh, score recomputation and fuel rental preflight. Preserved damage, contact sorting, deferred returns, traffic prediction/commit, origin and lifecycle ownership.
- Added `Prefabs/Interactables/{Coin,FuelCan}.prefab`, shared CoinGold/FuelMint materials and one authored `Meshes/CoinRing.asset`. Appended only Coin160 and Fuel24 to the existing pool catalog. Added layer10 Pickup. Wired one ProgressionSystem on Systems into the existing loop and player's shared sensor in saved Game through Editor APIs. Player/car/road prefabs and all old metas/GUIDs remain unchanged.
- Added `Tests/PlayMode/PickupTests.cs` and EditMode `PickupScoreTests.cs`/`PickupPlanTests.cs`; extended existing fixtures and regression tests. Test-only pooling/category probes now use Spark as their isolated key, retaining both real pickup pools; this adds no production VFX entry. Updated obsolete total-capacity and distance-score expectations. The traffic exhaustion test still asserts zero active cars while permitting the new pickups. Menu cleanup still asserts zero total interactables. Required-road rollback now accounts for four staged fuel returns plus its original two staged road returns. No regression assertion was relaxed to hide a failure.
- Repository-root `Docs/Setup/Task07{Setup.cs,Audit.cs,Smoke.cs,Tests.py,PlaySessions.py,Preservation.py}`, `Evidence/Task07/`, this note and CREDITS.md hold reproducible setup/verification. These recipes stay outside Assets and player assemblies. Existing asmdefs and supplied ignore bytes are unchanged.

## Rewards, score and callback order

`Coin.OnInteract` hides the visual/box, calls ProgressionSystem.AddCoin, publishes Collected, and requests recycle in finally. AddCoin increments the authoritative count once and recomputes using configured CoinScore100. FuelCan hides, calls existing AddFuel(configured30), publishes even when applied fuel is zero, and requests recycle in finally. Damage grace is unrelated to either reward.

Score is `floor(Session.Snapshot.Distance) + Configuration.CoinScore * collectedCoins`, safely saturated to int.MaxValue. Multiplication uses long; distance is compared before any integer conversion. Coin count itself saturates at int.MaxValue. AddCoin recomputes immediately; after `RecordMovement` the loop recomputes again from authoritative distance/count, with no additional direct +100. At unchanged distance one ordinary coin changes score by exactly100. No local-transform distance, second score owner, PlayerPrefs or difficulty curve.

`CollectedEvent`: RunId, value Encounter, convenience Kind/LocalPosition/AbsoluteZ, Value (configured nominal reward) and AppliedValue (actual score/fuel delta). Full fuel still publishes nominal30/applied0. Encounter retains kind, captured activation token, long sequence, lane, double absolute Z, target local root position and contact playing time. No payload contains a pooled object reference. Subscribers use absolute Z if presentation survives a rebase; LocalPosition belongs to the contact's origin frame. SnapshotChanged remains the normal end-of-tick/lifecycle publication, not a second per-pickup stream.

The existing sensor order remains **TOI → Pickup before Obstacle on an exact tie → stable spawn sequence**. The guard runs before reward mutation/publication, so repeated delivery and reentrant callbacks cannot reward twice. During shared dispatch, pickup return is deferred; its visual is already hidden. EndDispatch flushes captured tokens and clears contacts before pose commit. Direct pool returns from callbacks enter the same deferred queue. Re-rent restores guard, phase, child transforms/visibility, box and both pose histories. Old contacts/returns fail after reuse or reset.

## Fuel route and deterministic plan

Each immutable 480m BlockPlan reserves these mandatory placements before optional coins:

| Offset | Lane | Gap from preceding opportunity |
| --- | --- | --- |
| 120m | ProtectedLane | First120m; otherwise150m after previous block's450 |
| 300m | ProtectedLane | 180m |
| 450m | NextProtectedLane, inside clear tail | 150m |

The following block starts with ProtectedLane equal to the preceding NextProtectedLane. Thus all gaps are150/180m, independent of seed, rent success, steering, origin offset and traversal history. Coins at offsets60/150/240/330 have protected-route and optional alternate-lane slots. No optional coin is needed for fuel access; no pickup boxes overlap one another.

Reachability includes the route and expanded bounds, not merely an empty center. Stay on the protected lane through the360m span, enter the completely traffic-free120m buffer, allow reaction0.7s, at most two0.28s lane moves and margin0.3s, and reach the450m fuel before its expanded near face. At cap50m/s, `(450 - (1.8 + .5) - 360)/50 = 1.754s > 1.56s`. The prior300m fuel is fully before the buffer; the450m fuel is fully before the next block. Fuel link validation checks adjacent reservation indices,≤220m gaps and lane continuity. Startup now rejects e.g.60m/s where the overall120m transition fits but the earlier fuel deadline does not.

Pickup bounds are checked against stationary traffic and the union of potentially accepted moving encounters, including shorter late-introduction horizons that could admit a moving proposal rejected earlier. The validator reconstructs neighboring block proposals, uses the22m/s player /8m/s traffic conservative horizon, and clips possible movement at the existing expanded buffer reservation. It never assumes a candidate is safe because a pool missed. Optional coins are conservatively rejected where traffic may encounter them. Mandatory fuel lies inside the protected reservations and therefore remains clear. Road/traffic RNG addresses and results are preserved; only new pickup choices use PickupStream and a separate sequence domain `2^62 + block*16 + slot`. Traffic retains `block*18 + row*3 + lane+1`. Fuel positions require no RNG.

Only the existing four-plan window, primitive frontiers, preallocated registry/return/contact arrays, a fuel transaction array and64 diagnostics persist. PlanAt/FuelAt reconstruct values on demand; no growing route history.

## Actual rental and exhaustion policy

Initial preparation rents required fuel through300m before Playing and before optional coin placement. Before every tick's travel, PrepareFuelSupply rents **all** not-yet-issued required cans through targetDistance+300m. Long catch-up ticks therefore sweep real rentals for crossed reservations. Fuel preflight precedes road preflight and player/resource mutation; a subsequent road failure rolls back its newly staged fuel rentals and restores the fuel frontier. Fuel failures likewise roll back partial staging. Existing road coverage is intact on a fuel failure.

All24 Fuel instances are dedicated to required fuel; production code makes no optional Fuel rentals. Required fuel is never substituted with a Coin prefab. On a rent miss the director records it, returns only registered FuelCan leases whose entire bounds are behind the **current** player minus60m, then retries once. It never steals an active opportunity or another owner's rental. A second miss records a bounded diagnostic, fails explicitly and pauses before travel, with Resume blocked until Restart. A catch-up window exceeding the reserved capacity also fails before movement instead of skipping fuel or growing the pool. Restart reuses the prewarm and resets failure/frontier state.

Coin exhaustion omits that optional slot permanently. No growth, delayed near-player retry, extra RNG sample or fuel omission. Traffic's original safe omission policy remains. Counters expose SpawnedCoins/SpawnedFuel/OmittedCoins/FuelFailures/FuelReclaims/NextFuelIndex; GetDiagnostic exposes pickup placement, seed/config, reason, pool miss and sequence. The last64 records and latest Inspector diagnostic remain bounded.

## Inspector, prefab and lifecycle assignments

| Owner | Saved assignment / behavior |
| --- | --- |
| Systems/ProgressionSystem | Same scene GameManager; session supplied at bootstrap; debugCoins/debugScore |
| GameplayLoop | Existing Player/Survival/Sensor/SpawnDirector; new same ProgressionSystem |
| Player/InteractionSensor | Existing controller/registry/survival plus same scene ProgressionSystem |
| Player/PlayerSurvival | Existing manager/session; debugLives/debugFuel/debugGraceRemaining |
| Coin / FuelCan roots | Identity rotation, unit scale, layer Pickup10, logical half extents(.5,.6,.5), matching trigger box1×1.2×1; no child colliders |
| Coin Visual | Gold shared torus, outer radius.43m; one mesh/material asset, no runtime creation |
| FuelCan Visual | Mint can, open handle, dark cap/label; shared primitive meshes/materials |
| Active pickups | Same neutral World/Interactables, independent of road leases |
| Origin | SpawnDirector remains registered exactly once; only active item histories/roots shift; inactive storage stays put |

Catalog totals **304**: RoadOpen8, RoadTunnel8, Entrance4, Exit4, Stationary64, Moving32, **Coin160, Fuel24**. Registry, sensor contacts and deferred returns each hold **280** interactables; fuel staging holds24. The280-contact stress case collects all184 pickups and dispatches all96 obstacles without overflow. No future VFX entry is required.

Pause freezes playing time, reward dispatch and child animation while preserving leases/guards. Cleanup clears sensor/deferred work, returns interactables before roads, resets fuel/coin/traffic frontiers, plans, RNG seed and visual offsets, and retains prewarmed instances. New-run preparation also clears diagnostics/counters. Session.Reset restores coins/score0 and fuel100; existing lives, player/input/camera/origin reset order is retained. Menu releases active pickups; GameOver freezes existing world until restart.

## Executed verification

Final test and saved-scene results are recorded below after execution. Evidence paths are relative to `Evidence/Task07/`. Historical or failed runs do not count as passing evidence.

| Check | Actual result / evidence |
| --- | --- |
| Final compilation | **0 errors / 0 warnings**, compilationFailed=false; `compile-final.json` |
| Full EditMode | **109 passed**,0 failed/skipped; `editmode-results.json` |
| Full PlayMode | **109 passed**,0 failed/skipped; `playmode-results.json` |
| Total | **218 passed**; `test-summary.json` |
| R26 / hierarchy | Reflection asserts abstract base and all four concrete overrides; real typed catalog checked; PickupTests |
| V07 / reward lifecycle | One reward/event/return per activation; callback reentrancy, overlapping280-contact row, stale captured contact/return after reuse/reset; both real types collected |
| Score | Distance-only floor, exactly100 per coin at unchanged distance, duplicate rejection, formula through pause/rebase/restart, configured reward, long double distances and integer/count saturation; PickupScoreTests/PickupTests and both100km runs |
| V10 refill subset |95+30→100,10+30→40, full100→100 still one event with applied0; damage grace does not block either reward; no depletion-boundary rescue claim |
| Shared sensor | Actual pickups on exact pickup/obstacle ties and stable pickup sequence; earlier obstacle TOI still wins;50m/s thin-target crossings; callback returns deferred and lifecycle/rental reentry rejected |
| V21 pickup fairness subset | **1,000 seeds0..999 ×21 blocks =10,080m/seed**,63,000 required reservations; maximum fuel gap**180m**,13,884 protected-lane changes;111,279 accepted /56,721 rejected optional coin slots. Independent accepted stationary/moving envelopes checked at300m and180m introduction frontiers. Config50m/s cap,22 minimum,.28s lane move,.7s reaction,.3s margin. `configuration.json`, `instrumentation.json`, PickupPlanTests |
| Actual reachable rental / collection | Four PlayMode route cases: seed12345 at22/50, seeds0/999 at50; each travels≥2,880m, collects**18 fuel /24 coins**, keeps3 lives, has2 rebases,20 fuel spawned,304 created and zero misses. Peak23/24/24/22. `instrumentation.json` |
| Existing traffic safety | Re-executed1,000 seeds ×21 blocks ×22/50 cases;52,142 moving /213,156 stationary accepted,54,402 omitted, matching unchanged traffic streams; `instrumentation.json` |
|100km at22 | **100,592.71101155877m**,540 steps,77 rebases, coins168/fuel194 collected,598 coins/630 fuel spawned; moving108/stationary330, contacts532, peak33,304 created/0 destroyed/0 misses |
|100km at50 | **100,046.29900157452m**,239 steps,47 rebases, coins96/fuel194 collected,310 coins/627 fuel spawned; moving51/stationary157, contacts356, peak35,304 created/0 destroyed/0 misses |
| Road regression bounds | Both long runs retain≥300m ahead/60m behind, seams<.001m,≤7 committed roads,≤3 current plans and bounded local coordinates; original pool peak bound retained |
| Exhaustion | Optional coins omitted permanently with stable plan/RNG and no growth; required fuel exhausted/no stealing pauses before travel; legitimate behind-only reclaim retries successfully; partial staging rolls back two rentals/frontier; oversized catch-up fails explicitly; missing Coin/Fuel blocks startup |
| Lifecycle/reset | New50 pickup resets plus existing road/traffic/callback pool reset suites; both rewards exercised, pause/GameOver, multiple rebases, cleared contacts/reservations, new tokens and zero Menu active rentals |
| Visual/logical/origin | Child animation freezes on pause and resets on return/rent; fixed boxes/roots, both absolute/local histories shift once and inactive storage is unchanged |
| Scoped allocation sample |1,000 warmed synchronous loop ticks measured**0 current-thread managed bytes**; excludes engine frames, observers and bootstrap. This is **not** whole-game zero allocation or a Profiler result; `instrumentation.json` |
| Preservation |**254 protected files byte-identical**, all old metas/GUIDs/prefabs, input, asmdefs, Packages, configuration and unrelated ProjectSettings retained; no missing metas/duplicate GUIDs; ignore matches supplied SHA-256. `preservation.json` |

Saved-scene native smoke passed with normal reloads and with both domain/scene reload disabled (`play-sessions.json`). Both use all four actual lane bindings, native Escape pause/resume, paused and GameOver restart, and Menu cleanup. Frames and ticks advance; this is not just an Enter Play command or frozen screenshot. Each run collects **17 coins /12 fuel**, observes the first refill20→50, keeps all3 lives, spawns26 coins/14 fuel plus25 stationary/5 moving cars, and uses304 created/0 destroyed/0 misses with peak23. All config hashes match before/after, Instance clears/timeScale1 on exit, and reload/auto-tick/input/focus settings are restored.

Normal reloads: **2021.319201820m**, score**3721**, coins**17**, fuel**100**, lives**3**; ticks**2023**, rebases**2**, offset**1920**, localZ**101.319200m**, seam**0**.

Domain/scene reload disabled: **2020.879201993m**, score**3720**, coins**17**, fuel**100**, lives**3**; ticks**2022**, rebases**2**, offset**1920**, localZ**100.879200m**, seam**0**.

`native-{44,104,125,450,1005,2020}.png` are native Game captures from the final smoke; the disabled-reload cycle replaces the same milestone names. Inspected native104 showing mint can and gold rings, and native1005 showing the ring on the protected lane with both traffic types and joined road after rebase. Visuals are grayboxes; no final disappearance burst, audio, HUD or production art is claimed.

The initial two smoke cycles also passed, but the CLI's JavaScript JSON path rounded large long spawn sequences. The recipe now exports those evidence fields as decimal strings and both cycles were refreshed; exact unique sequences are retained in `play-sessions.json`. The earlier rounded metadata is preserved in `play-sessions-before-sequence-string.json`. Runtime sequences remain long throughout; the C# route/activation tests and revised evidence verify distinct values.

The first full execution passed108 EditMode and104/105 PlayMode cases. Its sole failure was the stale expected literal `Pools entry0 (Coin)` after that test-only probe moved to Spark. The expectation was corrected; no production reward/route failure occurred. The next full run passed108+109; final startup fuel-timing validation and its regression raised the final result to109+109. Initial and intermediate verdicts are retained under `*-first-results.json` / `*-before-timing-validation.json`; they are not substituted for final results. Every executed compilation was clean. Standard Unity-generated blank YAML values retain Unity's whitespace; `git -c core.whitespace=-blank-at-eol diff --check` passed.

## Reproduce

Use the pinned binary and exact project:

```bash
TASK07_CLI=/home/alireza/.local/bin/unity.previous
TASK07_PROJECT=/home/alireza/NeonTunnelRider/NeonTunnelRunner
"$TASK07_CLI" status --until-ready --project-path "$TASK07_PROJECT" --format json
"$TASK07_CLI" recompile --project-path "$TASK07_PROJECT" --format json
python3 Docs/Setup/Task07Tests.py
python3 Docs/Setup/Task07PlaySessions.py
python3 Docs/Setup/Task07Preservation.py
"$TASK07_CLI" command --caller plugin --skill unity-cli --project-path "$TASK07_PROJECT" --format json script run --file Docs/Setup/Task07Audit.cs --entry NeonTunnelRunner.Task07Audit.Inspect
"$TASK07_CLI" command --caller plugin --skill unity-cli --project-path "$TASK07_PROJECT" --format json console status
```

Tests use discovered Pipeline `test run --mode editor|playmode --filter NeonTunnelRunner.EditModeTests|NeonTunnelRunner.PlayModeTests --filter_type assembly --async_tests true`, then test status to completion. The smoke drives native Input System state events and actual FixedUpdates, uses established fixture-only reflection for fuel/speed, checks frame/tick advancement, captures native Game views and restores devices/focus/reload/auto-tick settings. Task07Setup.Configure is the Editor asset-construction recipe, not a gameplay reset; it requires a saved stopped scene and preserves existing pool entries/GUIDs.

## Task 08 API handoff

| API / phase | Contract |
| --- | --- |
| RunSession resources | Still sole owner; internal SetResources validates lives and finite fuel within config. Snapshot exposes lives/fuel/distance/time/coins/score. SetScore remains internal; normal play goes through ProgressionSystem. |
| PlayerSurvival | `TryDamage(Encounter)` applies one life only while Playing and contact PlayingTime≥GraceUntil; accepted damage sets deadline to contact time+configured1s and publishes Damaged. Grace rejection consumes that obstacle activation permanently. `AddFuel(float)` accepts finite nonnegative values, clamps to configured capacity while Playing and ignores grace; ResetRun clears deadline. No drain/EvaluateEnd yet. |
| Pickup rewards | Coin calls Progression.AddCoin; FuelCan calls Survival.AddFuel(configured30). Inherited guard precedes mutation/events. Collected fires once even at full fuel. Use value payloads for later audio/VFX. Final disappearance VFX is still pending. |
| Context | `InteractionContext(session,survival,encounter,progression=null)` preserves old obstacle callers; sensor always passes its serialized ProgressionSystem. Pickup contexts must belong to the same manager/session. |
| Contact order | Predict player and all interactables; collect segmented relative sweeps; sort TOI/category/sequence; dispatch damage/rewards; flush deferred captured-token returns; commit poses. No EndRun inside callbacks/iteration. |
| Registry/world API | Existing `PredictTraffic`/`CommitTraffic` and origin shift operate on **all** registered interactables. `MaintainTraffic` recycles all behind bounds, rents traffic and optional coins. Required fuel is handled separately by preflight. `FillWindow` retains at most4 plans; `FuelAt(index)` reconstructs a value reservation. |
| Fuel/road preflight | PrepareFuelSupply(current,target,offset) stages actual required fuel before road preflight; CommitFuelSupply after roads succeed, RollbackFuelSupply if roads fail. Failure sets WorldFailure and pauses before movement; Resume rejects a WorldFailure. Do not move/drain after a failed preflight. |
| Score / snapshot phase | After contacts/commit: Session.RecordMovement(dt,travel), Progression.RecomputeScore, TickIndex/debug refresh. Add Task08 drain and final terminal evaluation here, after pickups. Normal final snapshot remains after world/origin maintenance; terminal ticks must publish the final recomputed score once and avoid new spawning. |
| Lifecycle transaction | IsStepping and GameManager.Accepts still reject Pause/EndRun/Restart/recursive Step during the transaction. Task08 must deliberately exit the stepping guard before the single terminal transition. Lives wins a simultaneous resource terminal tie. No terminal completion implemented here. |
| Deferred returns | `DeferReturn(item,capturedToken)`, PoolService.Return and ReturnToPool use the same token queue during dispatch. Registry mutation/rental waits until EndDispatch. All buffers are sized from actual catalog capacity before Playing; no per-contact allocation or silent overflow. |
| Reset | Sensor.Clear → planner.Clear/return interactables → survival grace reset → roads release. Reset origin/plans/frontiers/player/roads/actual fuel rentals; Session.Reset then survival/progression debug refresh; commit Playing/input. Session.DetachObservers now clears Collected too. Reuse keeps prewarmed instances and monotonically new tokens. |

**R17/R20/R25:** real fuel/coin reward and score integration covered by executed tests below. **R22:** all four required recurring categories plus roads now use real typed pools. **R23:** source/counter/no-growth and scoped warmed tick evidence only; whole-game allocation/performance still requires profiling. **R26:** complete four-concrete-class inheritance/override behavior. **R39:** immediate hiding/recycle and typed feedback hook implemented; the final graded collectible disappearance effect is **pending Task12/presentation**.

**NOT RUN:** standalone build, Profiler capture,30-minute visible soak, final VFX/audio/HUD, fuel drain/depletion-boundary rescue and terminal evaluation, difficulty curves and high-score persistence. Full V21 mid/cap-difficulty/30-minute matrix remains for later tasks; this task's seed sweep covers pickup fairness at the existing traffic density and cap-speed timing. No later-task completion is claimed.

Final setup validator/audit **passed**. Game is saved/clean/stopped, Instance clear, timeScale1, auto-tick disabled; player assembly graph contains only NeonTunnelRunner.Runtime with existing InputSystem/uGUI/TMP references. Final Console reports **0 errors /0 warnings**, compilationFailed=false (`final-audit.json`, `console-final.json`). No tooling blocker remains for Task07. Build/Profiler/soak and later task work above remain NOT RUN/pending.

Next: **Task 08 — Lives, fuel, feedback and Game Over**. Stop after Task07; local checkpoint only, no push.
