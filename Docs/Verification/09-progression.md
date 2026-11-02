# Task 09 — Difficulty, score and persistent high score

Implemented on top of clean local Task08 `bdc647e`. The Git/Unity root is the inner NeonTunnelRunner directory. No applicable AGENTS.md, newer commits, initial user edits or missing handoffs were found. Inspected actual runtime/score/final-snapshot/lifecycle/cruise/plan APIs, saved Game, tests/configuration, Task05–08 handoffs, original09/10 prompts, requirements/design/architecture/structure/acceptance and environment setup. Prior245-test results are historical only.

Pinned environment retained: Unity6000.6.3f1, CLI1.0.0-beta.13 (`/home/alireza/.local/bin/unity.previous`), official plugin0.1.8-beta, Pipeline0.9.1-exp.1, URP/Core17.6.0, Input1.20.0, Test Framework1.8.0, NUnit2.1.0; Linux Mint Virginia/glibc2.35. No upgrade, package/identity/input/ignore change or push. Unity CLI skill used; command schemas discovered locally. Verification commands explicitly select this checkout.

## Changes and saved assignments

Paths below are under Assets/NeonTunnelRunner unless prefixed Docs/Evidence.

- Existing Scripts/World/ProgressionSystem: pure EvaluateDifficulty/Occupancy, tick-start evaluation, shared score/best flow and Inspector difficulty/target/actual/occupancy/score/best mirrors. Existing coin guard/recomputation retained.
- Existing Scripts/Core/RunSession: immutable snapshot Difficulty/AllowedSlotOccupancy alongside CruiseSpeed/ActualSpeed and existing seed/resources/double time/distance/coins/score/best. Session remains authoritative.
- Existing Scripts/Core/GameplayLoop: evaluate progression before preflight, then preserve contacts→movement/score/best→drain→terminal/continuing order. Continuing snapshots refresh actual-speed debug state after commit.
- Scripts/Persistence/HighScoreStore: small plain C# PlayerPrefs adapter with Load(), SetIfHigher(int), bool Flush(), read-only Best/IsDirty, production key constant, isolated-key constructor and internal counting-backend seam.
- Existing Scripts/Core/GameManager: composes/loads the store, CaptureBest(), flushes at required boundaries and before outgoing values are discarded. No save subscriptions/presenter/singleton added. Editor-only factory isolates test suites; internal ConfigureHighScores supports the temporary build probe before bootstrap.
- Existing Scripts/World/SpawnDirector: stateless row-address density and separate proposal/rejection/fallback/rental/miss counters. No RNG, envelope, traffic speed, protected-lane, fuel or pool-capacity change. PatternValidator/BlockPlan APIs stay intact.
- Existing Scripts/Config/GameplayConfig and Configs/Gameplay.asset: author StartingOccupancy=.25 and MaximumOccupancy=.8 alongside the existing22/50,180s/6000m and CoinScore100. Remove obsolete fixed .65 CandidateProbability from WorldConfig/World.asset so authoring has one density owner. Existing ProjectSetupValidator verifies the authored progression defaults.
- Existing Scenes/Game.unity: same Systems/GameManager→Gameplay.asset, one loop/progression/sensor and seed12345; saved Inspector mirrors. HighScoreStore is plain composition data and has no scene component/reference. Existing player/camera/damage-flash prefab and GUIDs retained. No HUD or new controls.
- New EditMode ProgressionTests, PlayMode PersistenceTests/ProgressionLifecycleTests, scoped HighScoreIsolation fixtures; focused existing lifecycle/contact/survival/long-run updates. Docs/Setup/Task09* reproduce setup, suites, scene smoke, preservation and limited standalone evidence.

## Formulas and deterministic planning

| d | Cruise target | Nominal allowed-slot occupancy |
| --- | --- | --- |
|0|22m/s|.25|
|.5|36m/s|.525|
|1|50m/s|.8|

`d=clamp01(max(elapsedPlayingSeconds/180, doubleAbsoluteRunDistance/6000))`. Evaluate at tick start; record actual travel/time afterward. Pause/GameOver actual0 retains target/difficulty. Restart resets d0/target22/occupancy.25 and run score/counts, preserving best. Rebasing changes neither input. Score remains floor(double absolute distance)+configured100*coins, safely saturated to int.MaxValue before narrowing.

The deliberate planner extension is documented in [the runtime architecture contract](../Architecture/09-progression-and-persistence.md). Each row finalizes at its immutable absolute world address: RowOccupancy(block,row) uses the curve's distance input at block*480+row*60, elapsed0. Current Session occupancy is nominal current challenge; world rows have their own immutable distance context. Time-only progression never redraws committed/visible rows. Same seed/config/address remains stable across generation batches, origin shifts, resets, reverse/out-of-window queries and pool misses, with existing per-slot RNG addresses and bounded four-plan storage.

Density applies only to eligible non-protected traffic slots. Warmup, two-lane maximum, clear120m tails, reaction/transition timing, fuel120/300/450 and full moving-envelope/pickup safety remain unchanged. Traffic remains0/8m/s; validation conservatively assumes player22..50. Required actual fuel preflight still fails before travel on unresolved capacity failure. Probability does not conceal rejection/fallback/saturation; diagnostics/counters report stages separately.

## Persistence and lifecycle

Load once per composition; missing/negative→0. CaptureBest caches max(loaded,previous,current) and retains it in Session. Score updates/snapshots/in-game Pause do not write preferences or Save. Flush only dirty state at finalized run end, return-to-menu, explicit quit, application pause(true), application quit/teardown, and before restart discards an outgoing run. Terminal rewards and final travel precede capture/flush; Lives tie priority and final guard remain intact. Lower runs and duplicate boundaries cannot lower best or cause redundant writes. Paused restart preserves a newly earned best.

Suites use fresh in-memory preferences per manager and unique real-PlayerPrefs keys for adapter tests. Saved-scene smoke uses unique keys. Standalone uses a unique `NTR.Tests.Task09.Process.*` key injected before GameManager Awake. No DeleteAll, production-key write or identity change. Tests distinguish ordinary Pause from the application callback.

## Executed evidence

Evidence is under Evidence/Task09. New full suites: **130 EditMode +140 PlayMode =270 passed**,0 failed/skipped (`test-summary.json`, full per-case `*-results.json`).

| Check | Executed result |
| --- | --- |
| Curve/math | Both inputs separately, max-of-inputs, endpoints/midpoint, monotonicity/caps, invalid numeric input; actual travel from tick-start target; frozen Paused/GameOver target/actual snapshots; reset/rebase |
| Density/safety |1,000 seeds ×21 blocks (>10km/seed) at each of three fixed challenge levels; 246,000 eligible addressed slots/level; complete accepted moving envelopes and every required fuel placement validated |
| Default evolving world sweep |1,000 seeds ×21 blocks ×two22/50 envelope cases:51,554 moving/210,926 stationary accepted;52,174 omitted. The minimum22-versus8 horizon remains used at both speeds |
| Supply sweep |63,000 required fuel reservations,113,122 accepted/54,878 rejected optional coins,13,884 lane changes, **maximum gap180m**; fuel links/full pickup/car bounds retained |
| Real cap run |181 playing seconds,6,554.21204166728m, actual/target50,d1, fuel99.2,lives3,score/best12,054,6 ordinary rebases (the fixture's subsequent invariant-only rebase makes its logged count7) |
|100km natural progression |101,229.86457051983m,245 varied batches subdivided into <=.1s route ticks,105 rebases,844 coins/632 fuel collected;337 moving/1,348 stationary spawned;2,013 proposed/328 rejected/0 traffic misses;peak26 |
|100km cap-clock fixture |100,046.29900157452m,239 varied batches,104 rebases,834 coins/625 fuel;333 moving/1,329 stationary;1,986 proposed/324 rejected/0 misses;peak26 |
| Long-run invariants |Both reach d1/target50, retain3 lives/positive fuel, >=300m ahead/60m behind, seams<.001m, bounded local coordinates, <=7 roads/3 observed plans,304 created/0 destroyed/0 misses, distance+coins score |
| Persistence lifecycle |Counted adapter proves load-once/cache-only updates, dirty/clean flush behavior at end/menu/quit/application pause/application quit/teardown/restart; ordinary in-game Pause and repeated snapshots do not Save; lower later runs preserve best |
| Final tick and resets |Final coin plus2.2m travel→score/best102 is flushed before RunEnded; second lower run preserves102. Lives-over-Fuel, shared contact-before-drain rescue, repeated terminal guards and50-cycle reset/traffic/pickup/pool/feedback stress remain passing |

The22-labelled100km case now starts naturally at22 and progresses to50; the50-labelled case explicitly preloads elapsed180 with distance0 to test cap movement. Neither is mislabeled as100km at constant22. A separate181-second real route starts with an untouched session and proves natural cap arrival. Distance-only unit integration fixtures deliberately initialize their player/fuel frontier at3,000/6,000m; they are separate from actual safe-route travel.

| Fixed challenge | Eligible | Proposed (rate) | Accepted | Rejected | Accepted moving | Moving→stationary fallbacks |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
|0|246,000|61,424 (.249691)|51,066|10,358|9,953|15,576|
|.5|246,000|128,907 (.524012)|107,002|21,905|20,982|32,568|
|1|246,000|196,805 (.800020)|163,229|33,576|31,954|49,559|

Tolerance is ±.006 absolute proposal rate, greater than six binomial standard errors for this sample size. This is an aggregate assertion, not a claim that adjacent blocks/individual random seeds must be strictly denser. Pure planning has no rental stage: pool misses are N/A there. Integrated long runs and saved-scene saturation measure actual rentals separately. `instrumentation.json` retains the observed logs.

The first run had129/130 EditMode passes and117/134 PlayMode passes. Failures came from fixed-speed/stationary fixtures, a stale best1500 expectation after real score exceeded it, and final-score/best equality incorrectly asserted on a lower later run. Updated fixtures use time-based cap context for high-speed crossing, return content in the explicitly no-encounter depletion fixture while preserving real progression/drain, place repeat hits at the moving player, integrate variable travel bounds, and assert retained best. Second run130/130 and138/139 had the remaining lower-run callback assertion; corrected to best>=score. Historical `*-first-results.json`/`playmode-second-results.json` remain explicitly nonpassing. The final270-pass suites were run after these corrections; no regression was bypassed by disabling progression or changing runtime survival.

Saved Game smoke **passed in both established reload configurations** (`play-sessions.json`): normal reloads and domain+scene reload disabled. Ten wall-second pauses lasted **10.115673s /10.108051s**, freezing snapshot/difficulty/target, actual0, lane pose, traffic, grace, flash timer/color; native Escape resumes. Every lane binding fires once; Paused/GameOver restart, both terminal causes, terminal immutability, menu cleanup and exiting while Paused pass. Input/device/focus/reload/auto-tick settings and config hashes are restored.

Each native route collected17 coins/12 fuel with3 lives,14 fuel/2 moving/18 stationary spawns,2 rebases,304 created and0 natural misses. At the2,020m milestone both reported **2,020.9992019459605m, elapsed220.4199840389192s,score/best3,720,d1,target/actual50,occupancy.8,fuel94.64,offset1920,seam0**. This route deliberately preloads the180s cap clock after restart; natural cap progression is the separate181s integration test above. The later paused route snapshots occur a few ticks after the milestone and have slightly different fuel. Final speed is0 in both native terminal causes.

Saturation smoke in each mode:10 eligible,4 proposed,3 accepted,1 safety rejection,2 realized stationary,0 realized moving,1 traffic pool miss,2 moving→stationary fallbacks;304 instances unchanged. Held traffic pools deliberately cause the miss; releasing them does not retry the omitted slot or alter PlanAt(1). This complements the non-vacuous moving traffic in the natural route/seed/100km checks.

Inspected `native-damage-paused.png` visibly shows the red player beside the orange obstacle; `native-2020.png` shows the gray player/clear route after two rebases. `native-{baseline,125,450,1005,fuel-gameover,lives-gameover,restart}.png` retain the other native milestones. The second smoke cycle replaces the same screenshot names. Screenshots establish current graybox/feedback preservation, not a completed HUD/camera/art/VFX requirement.

The pinned CLI can return a successful compile before Pipeline finishes its domain-reload restart. The first dependent setup/build call saw “No Pipeline instance found”; the same Editor remained running, no build was started in that attempt, and source compilation was0 errors/0 warnings. The standalone recipe now waits after import and confirms readiness/Editor status before building. No tool/package change or fallback Editor was used. URP also auto-added default AdditionalLightData during preview; setup removes that unsaved incidental component before saving, retaining only the intended five scene debug fields.

**Standalone fresh-process persistence PASS** (`standalone-processes.json`, `process-{establish,lower,verify}.log`). The limited Linux x86_64 Mono Development build of current Game succeeded with **0 build errors/0 build warnings** (`persistence-build.json`). No compatible prior build existed. Output remains ignored at `Builds/Task09Persistence/NeonTunnelRunner.x86_64`; source/config/package hashes are retained in `build-source-hashes.json`. Probe source lives in Docs/Setup, is copied temporarily into the runtime assembly only for this build with `extraScriptingDefines=NTR_PERSISTENCE_VERIFY`, then source/meta are removed and the Editor recompiled. No project-wide define, preferences identity, scene cheat control or production UI was added.

| Fresh process | PID | Loaded | Completed score | Best after end | Exit |
| --- | ---: | ---: | ---: | ---: | ---: |
| Establish |121727|0|4,321|4,321|0|
| Lower run |121807|4,321|123|4,321|0|
| Verify lower run persisted |121871|4,321|123|4,321|0|

The harness injects an isolated `NTR.Tests.Task09.Process.d1fe2faebb4c4a199845960133dfe8e6` store before Awake, invokes existing StartRun/session movement-score/EndRun APIs, and uses real lifecycle PlayerPrefs Flush. It does not simulate4,321m of world traversal; world survival is verified by the Editor runs. The last process deletes only this test key after checking it. All processes use the normal application identity, `-batchmode -force-glcore` with640×360/windowed flags; they require no Editor connection or runtime automation command. This establishes R49/V11 process persistence, including a lower later run. It is not Task16 packaging or extracted-delivery acceptance.

Historical first process attempt failed because BeforeSceneLoad searches must include inactive objects; its early quit also emitted native teardown diagnostics. The probe was corrected to FindObjectsInactive.Include and delayed startup/quit. A subsequent `-nographics` attempt correctly found composition but the existing flash renderer's shader-property guard could not initialize under NullGfx; that process failed before the persistence run. Both failed reports/logs remain as `first-*`/`second-*`; neither counts as persistence evidence. The same compatible development build then passed all three rendered OpenGL processes, with clean exit0 and no retained runtime errors. A10MB crash dump from the first failed fixture was removed; its diagnostic log is retained. No runtime/material safety guard was bypassed. Headless NullGfx operation is not claimed.

Final compilation is **0 errors/0 warnings**, up_to_date (`compile-final.json`); final Console ground truth **0 errors/0 warnings**, compiling=false/compilationFailed=false (`console-final.json`). The pre-clear Console also had0 errors/0 warnings; routine logs were archived in `console-after-smoke-build.json`. Final validator/audit passes with saved Game clean/stopped, explicit owners/defaults/pins intact, Instance clear/timeScale1, auto-tick false, ordinary player graph only NeonTunnelRunner.Runtime referencing InputSystem/uGUI/TMP, and the temporary probe absent (`audit-final.json`, `editor-final.json`).

The build auto-wrote URP stripping/runtime-setting caches and serialized a default Bloom filter field. Only those three task-generated asset changes were restored to their inspected pre-task bytes and reimported; gameplay tuning was untouched. Task09Build now snapshots/restores those current asset bytes around future verification builds. Final preservation audit covers the rendering assets as well as packages/settings/input/prefabs/old metas and supplied ignore; no missing/duplicate GUIDs (`preservation.json`). These generated cache changes were not user edits or a renderer migration.

## Reproduce and requirement status

```bash
TASK09_CLI=/home/alireza/.local/bin/unity.previous
TASK09_PROJECT=/home/alireza/NeonTunnelRider/NeonTunnelRunner
"$TASK09_CLI" status --until-ready --project-path "$TASK09_PROJECT" --format json
"$TASK09_CLI" recompile --project-path "$TASK09_PROJECT" --format json
python3 Docs/Setup/Task09Tests.py
python3 Docs/Setup/Task09PlaySessions.py
python3 Docs/Setup/Task09Standalone.py
# Or reuse the compatible probe build only if recorded source/config hashes still match:
python3 Docs/Setup/Task09Standalone.py --reuse
python3 Docs/Setup/Task09Preservation.py
"$TASK09_CLI" command --caller plugin --skill unity-cli --project-path "$TASK09_PROJECT" --format json script run --file Docs/Setup/Task09Audit.cs --entry NeonTunnelRunner.Task09Audit.Inspect
"$TASK09_CLI" command --caller plugin --skill unity-cli --project-path "$TASK09_PROJECT" --format json console status
```

Task09Setup.Configure is an explicit saved/stopped scene authoring recipe; do not run it during someone else's Play session. Pipeline suites use discovered test run modes editor/playmode, assembly filters, async_tests=true and completed-status polling. Smoke uses real FixedUpdates, native Input System state events, real contacts/feedback and restores settings. External reflection/clock/resource/frontier fixtures remain outside normal player controls. Seed12345 is the saved/integrated baseline; seeds0..999/21 blocks and immutable defaults above drive the sweeps. Hashes record exact build-source/config/package inputs.

Direct **R21 PASS**: actual Playing speed progresses/caps and eligible-slot proposals increase statistically, with safety/supply/envelopes preserved. **R49 PASS / Task09 V11 PASS**: lifecycle-flushed PlayerPrefs best survives independent process exits and lower later runs. R07 continuous forward movement and R20 once-only configured coin score remain passing. **R48 snapshot boundary is ready; full HUD remains Task14.** V12/V13 pause/reset and the Task09 V21 seed/safety scope pass; the whole-game V21 requirements matrix/30-minute schedule and later final delivery remain unverified. No Task09 blocker remains. Headless NullGfx support is outside this rendered-game persistence check.

Local checkpoint only, no push; final commit/tree status is reported with the task response.

## Task 10 API handoff

- RunSession.Snapshot (and SnapshotChanged) exposes immutable seed, resources, elapsed/double absolute distance, coins, Score/BestScore, Difficulty/AllowedSlotOccupancy, CruiseSpeed target and ActualSpeed. Actual speed is travel/dt while Playing and0 in Paused/GameOver; use ActualSpeed for presentation. Target/difficulty freeze at the last evaluated tick; resume retains target and reevaluates on the next tick. Reset starts22/d0. Bind the later HUD here; no separate score/best cache or PlayerPrefs presenter.
- GameplayLoop remains the sole simulation owner. Its Player/Origin/Planner/Visuals/Survival/Progression properties provide existing explicit owners. Simulation poses commit before snapshots; presentation must not change the logical collision root or take over travel.
- PlayerController exposes LaneIndex, transition progress, PreviousPosition/CurrentPosition, private double absolute Z histories and MotionLegCount/GetMotionLeg(i); the public logical poses are the Vector3 histories, kinematic Body and stable CameraAnchor. There is no custom render-pose API yet; current rendering uses the Rigidbody/root Transform and authored Rigidbody interpolation=None. Use public logical histories for any Task10 visual interpolation. Root rotation/scale/box remain gameplay-owned. Vehicle mesh children are the future roll/bob surface.
- Session.Damaged(DamageEvent) is accepted-damage-only, with RunId, encounter and RemainingLives; no grace-rejection event. Existing once-only Collected payloads remain. No new duplicate subscriptions; unsubscribe on disable/rebootstrap as VehicleVisuals already does.
- VehicleVisuals owns the accepted-hit playing-clock flash (.25s), Renderer and merged MaterialPropertyBlock `_BaseColor` (fallback `_Color`) with cached baseline. FinishTick reconciles deadlines. Preserve unrelated override properties and shared materials; no Renderer.material cloning. Pause freezes flash/color; terminal freezes final appearance; ResetVisuals restores baseline and clears timer. Sparks remain Task12.
- WorldOrigin rebases after contact/pose/score/fuel commit. Player root owns the current CameraAnchor/MainCamera child, so it inherits exactly one player teleport; no separate chase/damping/shake cache exists yet. Player rebases both logical histories from double absolute coordinates and resets render histories. SpawnDirector shifts its own registered active objects once; inactive storage stays neutral. Task10 must explicitly update new camera position/target/damping/shake caches exactly once via IWorldOriginParticipant/serialized additionalParticipants (there is currently no origin-shift event), and snap/clear caches on reset.
- Pause gates ticks and pending lane input while holding player/traffic pose histories. Resume does not accumulate unscaled movement. Reset cleanup restores vehicle feedback, centered player/body/pose histories, origin/plans/frontiers, then rebuilds the initial world before Playing. Keep GameOver/restart best and persistence ordering intact.

NOT RUN unless added below: Profiler,30-minute soak, unrelated build/delivery acceptance. Full HUD/menus, camera/motion polish, final VFX/sparks/audio remain pending. This task stops at09; next is **Task10 — Rear camera and hovercar motion polish**.
