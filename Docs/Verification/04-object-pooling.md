# Task 04 — Bounded pooling infrastructure

Verified **2026-11-01** (Asia/Tehran), beginning on clean `main` at `29a13ae1947bb4effa8db95dbfc25222b59e756f`. No newer commits, user edits or repository instruction files were present. Tasks 01–03 notes/evidence, current runtime/tests/validator/scene/config, the Task 04/05 prompts, requirements, architecture, project structure, acceptance plan and environment contract were inspected. Previous 80-test results were baseline evidence only.

Environment retained: **Unity 6000.6.3f1**, pinned CLI **1.0.0-beta.13** at `/home/alireza/.local/bin/unity.previous`, official plugin **0.1.8-beta**, Pipeline **0.9.1-exp.1**, Linux Mint **21.3 Virginia**, glibc **2.35**. Actual registered packages remain URP/Core **17.6.0**, Input System **1.20.0**, Test Framework **1.8.0**, uGUI/TMP **2.6.0**. Unity was initially closed; the CLI opened this repository with the pinned Editor. No tools, packages, OS libraries or project settings were upgraded.

## Changes and ownership

All asset paths below are under `Assets/NeonTunnelRunner/`:

- `Scripts/Pooling/PoolKey.cs`: the twelve stable typed keys, RoadOpen through Firework.
- `Scripts/Pooling/IPoolable.cs`: explicit lifecycle interface and immutable value-type `SpawnContext`.
- `Scripts/Pooling/PooledObject.cs`: pool-owned identity, context, activation token, rental status and virtual lifecycle hooks. No implicit return in OnDisable.
- `Scripts/Pooling/PoolService.cs`: explicit validated prewarm, fixed-capacity stacks/active sets, stable instance arrays, guarded rent/return/reset/shutdown and read-only diagnostics/counters. No singleton, Resources lookup or runtime growth.
- `Scripts/Config/PoolConfig.cs`, `Configs/Pools.asset`: private serialized entries with value getters and read-only validation. The real catalog intentionally has **zero entries**. No test template or future gameplay prefab is shipped in it.
- `Scripts/Core/GameManager.cs`, `GameplayLoop.cs`: serialized pool dependency, bootstrap/readiness check, input/simulation gating before return/reset, and shutdown ownership.
- `Scripts/Editor/ProjectSetupValidator.cs`: validates one correctly wired service, separate inactive storage and every configured entry. Empty is valid; invalid/duplicate keys, nonpositive capacity, null prefab, missing/disabled root pooled component or nested pooled identities are rejected.
- `Tests/EditMode/PoolConfigTests.cs`, `Tests/PlayMode/PoolLifecycleTests.cs`, `PoolProbe.cs`: validation and lifecycle tests. Existing EditMode/PlayMode movement fixture helpers now wire the real pool dependency. Existing assertions and input behavior remain intact. No assembly definition changes were necessary.
- `Scenes/Game.unity`: adds only PoolService, its owner/config/storage assignments, the manager's pool reference and a separate inactive Pools root. Camera, light, static road, player prefab, input actions and original scene blocks remain intact.

Unity generated new asset/meta/scene serialization through the Editor. Existing `.meta` files and GUIDs were preserved. `Docs/Setup/Task04Setup.cs` is the repeatable Editor wiring/inspection recipe; `Task04Tests.py`, `Task04Smoke.cs` and `Task04PlaySessions.py` reproduce verification. These recipes stay outside Assets. `CREDITS.md` records Task 04 AI assistance; no external gameplay assets/code were downloaded.

## Final API for Task 05

| API | Contract |
| --- | --- |
| `PoolConfig.Entry(PoolKey, GameObject prefab, int capacity)` | Authoring data; prefab root must carry exactly one enabled `PooledObject` or subclass. No nested pooled identities. `EntryCount`, `GetEntry(index)`, `TryValidate(out string error)` expose read-only access/validation. |
| `SpawnContext(key, localPosition, localRotation, activeParent = null, lane = 0, absoluteDistance = 0d, seed = 0, speed = 0f, spawnSequence = 0)` | Read-only struct carrying caller data. Position/rotation are local to ActiveParent; null means a scene root. Double absolute distance and long spawn sequence remain separate from local coordinates. Scale is restored from the authored prefab. Identity/token are never supplied by the caller. |
| `PoolService.Initialize()` | Validates the complete catalog before allocating instances. Explicit Menu/bootstrap prewarm; repeated calls while ready reuse existing instances. Returns false with `LastError` on invalid setup. No automatic retry/growth during Playing, Paused or GameOver. |
| `TryRent(SpawnContext context, out PooledObject instance)` | Returns false/null on failure; never allocates a replacement. Use `LastRentFailure`, `LastMissKey`, `Misses` and per-key counters. Unknown keys and exhaustion are distinct. Inactive parents/storage parenting, invalid pose or invalid inactive instance also fail. |
| `Return(PooledObject instance, ulong expectedActivationId)` | Ownership, registry, rental status and captured token must all match. Foreign, duplicate, zero or stale tokens fail without changing the current rental. |
| `PooledObject.ReturnToPool(ulong expectedActivationId)` | Convenience wrapper requiring the captured token. There is deliberately no token-free delayed-return helper. |
| `ReturnAll()` | Uses fixed bootstrap arrays; accepts callback-triggered returns and repeated calls. Nested ReturnAll is a no-op; each rental is cleaned once. |
| `Shutdown()` | Rejects new rentals, returns outstanding rentals and destroys owned instances. Deferred until the outer operation exits if requested by a hook. Repeated shutdown is harmless. Ordinary restart never calls it. |
| `TryGetCounters(key, out PoolCounters)` | Per-key Capacity, Created, Active, Inactive, PeakActive and Misses. Service aggregates expose Capacity, ActiveCount, InactiveCount, PeakActive, TotalCreated, TotalDestroyed, TotalRents, TotalReturns and Misses. |

`PooledObject` exposes read-only Owner, Key, ActivationId, IsRented and Context. `IPoolable.OnRent(SpawnContext)` and `OnReturn()` are public virtual hooks on the base class for later RoadSegment/Interactable/VFX overrides. Hooks belong to PoolService; callers use rent/return commands. Derived hooks should perform repeatable resets of their own pose history, consumed flags, colliders, timers/coroutines, particles/audio/trails and visual state. Those future systems are not implemented here.

Rent ordering: reserve from inactive stack → assign new globally monotonic activation ID, context and rental status → register active → set active parent/local pose/authored scale while inactive → OnRent → activate. OnEnable sees the new rental's data. Inactive staging during prewarm also prevents an active prefab's OnEnable from firing early.

Return ordering: validate → mark Returning → unregister/decrement active → OnReturn once → deactivate → restore storage parent/neutral pose/authored scale → clear context → mark Inactive → push once. Objects temporarily in cleanup are reserved and in neither active nor inactive counts. Totals are consistent when the operation completes. TotalRents counts reservations, including an activation explicitly cancelled during its callback; TotalReturns counts accepted cleanup operations. Cumulative counters and activation IDs are not reset by ReturnAll/restart; per-key peaks/misses persist until shutdown.

Reentrancy: nested rentals during any pool operation/OnRent/OnReturn/OnEnable/OnDisable are rejected as Busy. Return/ReturnAll remain allowed from hooks. A rental returned in OnRent or OnEnable makes the outer TryRent fail/null as ReturnedDuringCallback. A Returning instance cannot recursively return. Shutdown requests are deferred safely. Lifecycle reset/menu commands reject entry from an in-progress pool callback. External disable leaves the rental registered until explicit token-checked return; it does not silently free an instance another caller still owns. Do not destroy pooled instances or change pool ownership externally.

Exceptions from explicit hooks are logged; cleanup attempts restore pool integrity. Exception/diagnostic paths are outside the measured allocation-free success path. Callers must treat any such diagnostic as a defect, not successful content spawning.

Task 05 should author real road prefab entries, explicitly require the needed keys, and provide road-coverage/exhaustion policies. Optional later content may be skipped; guaranteed fuel needs a reservation/fail-safe policy in its own caller. This service does not implement those decisions.

## Scene and future catalog assignments

| Assignment | Saved value |
| --- | --- |
| Systems / GameManager.poolService | Systems / PoolService |
| Systems / PoolService.gameManager | Systems / GameManager |
| PoolService.configuration | `Configs/Pools.asset`, GUID `241596b69c8a69f3f8abd11ab4e827f1` |
| PoolService.storageRoot | Separate **Pools** scene root, inactive, outside World/player/camera hierarchies |
| Pools.asset entries | Empty; no implemented system yet requires a pooled asset |

Prewarm runs during owner bootstrap, before StartRun can enter Playing. A failure keeps Menu, the prior snapshot/run ID and disabled Gameplay map; StartRun reports an actionable error. Begin/reset and return-to-menu gate input/ticks before ReturnAll. Restart returns rentals before resetting the session/player, preserving capacity and IDs across reuse. Pause/resume retains rentals and tokens. EndRun/GameOver freezes the existing presentation without returning its rentals. Owner shutdown gates input/simulation before pool shutdown. No recurring Instantiate/Destroy was added to GameplayLoop or pooled objects.

Configure these **future starting capacities** from architecture doc03 as actual assets arrive; they are guidance, not placeholder entries:

| Keys | Capacities |
| --- | --- |
| RoadOpen / RoadTunnel / RoadEntrance / RoadExit | 8 / 8 / 4 / 4 |
| Stationary / Moving | 64 / 32 |
| Coin / Fuel | 160 / 24 |
| Spark / CoinBurst / FuelBurst / Firework | 16 / 16 / 8 / 8 |

## Executed verification

All evidence below is under repository-root `Evidence/Task04/`.

| Check | Actual result | Evidence |
| --- | --- | --- |
| Source compilation | **PASS**, zero errors/warnings, compilationFailed=false | `compile.json`, `foundation-audit.json` |
| Full EditMode suite | **64 passed, 0 failed/skipped**: original 53 plus 11 pool validation cases | `editmode-start.json`, `editmode-results.json`, `test-summary.json` |
| Full PlayMode suite | **42 passed, 0 failed/skipped**: original 27 plus 15 pooling cases | `playmode-start.json`, `playmode-results.json`, `test-summary.json` |
| Prewarm / V06 | Exact configured capacities, repeated initialization creates nothing, active-prefab OnEnable suppressed in staging, context/reset before activation | Per-case PlayMode results |
| Reuse / V06 | **1,000 rent/return cycles for each of all 12 keys: 12,000 total**, stable EntityIds, monotonic activation IDs, restored context/flags/counts; capacity/created **24**, destroyed **0** during Playing | `instrumentation.json`, PlayMode results |
| Exhaustion / ownership / V06 | Exhausted and unknown keys fail, misses recorded, no growth; captured old-token callback cannot return re-rented instance; duplicate/foreign returns harmless | PlayMode results |
| Reentrancy / V06 | Returns from rent/enable hooks, nested ReturnAll and returns, external disable and callback shutdown all retain integrity without recursive cleanup | PlayMode results |
| Reset / V13 | **50 restarts** alternating Paused/GameOver recover two rentals every cycle; **100** restart cleanup callbacks, then **1** extra menu cleanup; capacity/created **4**, destroyed **0**. Pause/resume retains rentals/tokens. | `instrumentation.json`, PlayMode results |
| Configuration failure | Invalid catalog blocks Start with unchanged Menu/snapshot and no Gameplay map; valid empty catalog starts | PlayMode + EditMode results |
| Managed allocation sample | After 32 warmup cycles, **1,000 successful synchronous rent/return cycles allocated 0 managed bytes** on the current thread using `GC.GetAllocatedBytesForCurrentThread` | `instrumentation.json`, named PlayMode test |
| Native saved-scene smoke / V02,V03,V04 | **2 cycles passed**, with normal reloads and with domain/scene reloads disabled. Actual Editor StartRun context method, A/D/LeftArrow/RightArrow, one callback per press, native FixedUpdates, Escape pause/resume, paused/GameOver restart, return to Menu. Per-run lane-check distances **8.36–15.84 m**, within the static road. | `play-sessions.json`; `Task04Smoke.cs` recipe |
| Config immutability | Pools.asset and Gameplay.asset SHA-256 unchanged across both real Play sessions; input focus/device/reload settings restored | `play-sessions.json` |
| Validator / scene / assembly | Saved scene valid and clean; correct empty catalog/storage wiring; ordinary player graph still only NeonTunnelRunner.Runtime with its existing runtime references, no UnityEditor/Pipeline dependency | `validator.json`, `foundation-audit.json` |
| Preservation / V22 | **262 protected files** byte-identical; all original metas unchanged, no missing metas/duplicate GUIDs; supplied/root ignore hashes identical; generated folders remain ignored | `preservation.json`, `ignore-check.json` |
| Final Editor | Edit mode, saved Game isDirty=false, Instance clear, timeScale=1; current Console **0 errors / 0 warnings** | `validator.json`, `console.json` |

Bootstrap creation, fixture-template construction and fixture teardown are separate from normal-play operations. Successful steady-state pool calls use preallocated stacks/sets/arrays and no per-call allocations in the measured path. This does **not** establish whole-game zero GC, native-memory stability or frame-rate budgets. No Profiler capture, player build, endless-road soak, future prefab/VFX reset verification or new visual screenshot was run/claimed. Tests currently use a test-only in-memory probe; production catalog integration is pending real assets.

### Resolved findings

The first test compilation used GetInstanceID, which this Editor rejects; tests now use its installed GetEntityId API. The first pooling run exposed early Awake ordering: isActiveAndEnabled can be false before the service's OnEnable even though its component/object are enabled. Bootstrap now checks authored enabled/active hierarchy state, and explicit prewarm succeeds before Start. One pose test needed a quaternion-angle tolerance because Unity normalizes rotations. These failed results remain in `compile-first-tests.json`, `playmode-first-results.json` and `playmode-before-pose-tolerance.json`; they are not passing evidence. A test launch immediately after recompilation also hit the short domain-reload disconnect; the repeatable runner now waits for CLI readiness first. No tool/package replacement was needed.

Passing fixtures intentionally exercise an invalid Start diagnostic and duplicate-owner warning. `console-after-tests.json` records those test messages/historical counters. Console was then cleared before the two real scene smoke checks and final audit; `console.json` records the clean current Console, not an inference from historical counts.

## Reproduction and requirement status

Run from the repository root; all Editor-driving commands target this exact project:

```bash
TASK04_CLI=/home/alireza/.local/bin/unity.previous
TASK04_PROJECT=/home/alireza/NeonTunnelRider/NeonTunnelRunner
"$TASK04_CLI" recompile --project-path "$TASK04_PROJECT" --format json
"$TASK04_CLI" status --until-ready --project-path "$TASK04_PROJECT" --format json
python3 Docs/Setup/Task04Tests.py
python3 Docs/Setup/Task04PlaySessions.py
"$TASK04_CLI" command --caller plugin --skill unity-cli script run --file Docs/Setup/Task04Setup.cs --entry NeonTunnelRunner.Task04Setup.Inspect --project-path "$TASK04_PROJECT" --format json
```

The test runner executes `test run --mode editor|playmode --filter NeonTunnelRunner.EditModeTests|NeonTunnelRunner.PlayModeTests --filter_type assembly --async_tests true`, sequentially polling `test status` until complete. JSON includes each real verdict and stack trace, not inferred outcomes. Smoke verification requires a stopped Editor with saved Game open and restores original reload settings. Configure is only for adding the Task 04 scene wiring, not a runtime reset command.

**R22:** infrastructure and test-template reuse covered; road/traffic/pickup integration remains pending Tasks 05–07. **R23:** no-growth/no-destruction and allocation-free successful pool path verified in the scoped fixture; whole-game/future VFX integration and Profiler verification remain pending. **R08/R09:** endless road generation/recycling not implemented or accepted here; Task 05 owns them. **V06:** infrastructure subset passes; final real-prefab/Profiler coverage remains pending.

No Task 04 checks are blocked. No road generator, origin rebasing, traffic, pickups, survival, scoring, menus, presentation polish, runtime expansion or Addressables were implemented. The current road remains static and 300 m long. Local checkpoint commit only; no push/publication.

Next: **Task 05 — endless world generation**. Stop Task 04 here.
