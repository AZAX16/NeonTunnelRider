# Technical architecture

## Environment contract

Use the user's installed **Unity Editor 6000.6.3f1** on **Linux Mint Virginia, glibc 2.35**. Do not downgrade to the initially considered 6.3 LTS or silently upgrade. This is a Unity 6.6 update release, not “6.3 LTS.” Pin `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json` and `Packages/packages-lock.json` to the actual successfully resolved environment.

The user reports Unity CLI `1.0.0-beta.13`, official `Unity-Technologies/unity-agent-plugin` `0.1.8-beta`, and `com.unity.pipeline` `0.9.1-exp.1`; preserve these. **Unity Pipeline is Editor automation, not the Universal Render Pipeline.** URP is `com.unity.render-pipelines.universal`. Inspect the manifest and Graphics/Quality assets before changing rendering. The existing Input System and URP/Test Framework package versions were not supplied: record their actual resolved versions in task 01, do not guess numerical pins.

Unity's current CLI documentation lists glibc 2.34+; the reported 2.35 exceeds that listed minimum, but this is not a local smoke test of beta.13 or a blanket claim of official Mint support. Do not replace system glibc. If automation fails, retain the Editor and use its built-in workflows or batchmode with the same Editor. See [environment setup](../planning/environment-setup.md) and [sources](../planning/sources.md).

Rendering: URP Forward renderer, Linear color, HDR camera, modest MSAA if GPU permits, one Global Volume with Bloom and fixed exposure/tonemapping. uGUI + TextMeshPro; InputSystemUIInputModule. Standard ParticleSystem, no VFX Graph requirement. Linux x86_64 is the initial development executable; confirm the grader's OS before final delivery. Mono is the simplest provisional desktop scripting backend if installed and supported; record actual backend and modules.

## Ownership and communication

`GameManager` is the only singleton. It owns state and a plain C# `RunSession`; it orchestrates lifecycle commands but delegates simulation, pools, score, audio and UI. Services are scene components linked through serialized references. No service locator framework, dependency-injection container, generic event bus, ECS or Addressables is needed.

`RunSession` owns run values and emits typed C# events. Publishers invoke events after state mutation; presenters subscribe in OnEnable and unsubscribe in OnDisable and render the current snapshot immediately after subscribing. Events carry value data, not pooled object references that could be reused before a listener acts. Avoid static event channels. Config ScriptableObjects hold immutable authoring data; runtime values never write back into assets.

## Lifecycle and transition table

| Current | Command | Next | Side effects |
| --- | --- | --- | --- |
| Menu | StartRun | Playing | Prewarmed services; reset transaction; start simulation |
| Playing | Pause | Paused | Disable gameplay map/clear queue; timeScale0; pause effects/audio |
| Paused | Resume | Playing | timeScale1; enable gameplay map; no stale input |
| Paused | Restart | Playing | Reset transaction then enable play |
| Playing | EndRun | GameOver | Snapshot final score/reason; update/flush best once; freeze |
| GameOver | Restart | Playing | Reset transaction |
| Paused | ReturnToMenu | Menu | Commit best; return active pools; reset visuals; timeScale1 |
| Menu | Quit | Application exit | Flush best; Application.Quit (Editor logs intent) |

All other transitions are ignored with a development diagnostic. Duplicate commands are idempotent. Direct Restart during Playing is not exposed. GameOver→Menu is an optional internal command only if a later UI explicitly adds it; it is outside the required UI. State freezes simulation even when Menu timeScale is 1. OnDestroy restores timeScale=1 and clears Instance only if it owns it. Duplicate scene managers are rejected at bootstrap; the one-time duplicate cleanup is outside normal play. Use one Game.unity scene; do not use DontDestroyOnLoad for this single-scene MVP.

Reset order: disable input and tick → commit outgoing best if needed → clear contact/queued commands → stop/clear pooled VFX and one-shot audio → return all interactables → return road segments → reset origin and pose caches → reset RunSession (new run ID, seed, 3 lives, fuel100, distance/time/score0, speed22, preserved best) → reset progression/RNG/corridor → rent initial road → spawn safe warmup/pickups → center player → snap camera and visuals → refresh HUD → set Playing/timeScale1 → enable gameplay actions. Every reset is explicit; OnEnable must never imply a new run.

## Fixed-step simulation contract

Only GameplayLoop owns FixedUpdate (baseline 0.02 s). A tick does:

1. Confirm Playing; read one buffered command and apply it to the lane state.
2. Evaluate current difficulty/speed; cache previous poses and predict player/traffic end poses.
3. Build relative swept contacts against the registered active interactables; sort by time-of-impact, category tie priority, stable spawn sequence.
4. Dispatch one encounter per activation; process coins/fuel/damage without immediately changing state mid-iteration.
5. Commit poses through kinematic Rigidbody.MovePosition where used; update elapsed, distance and actual forward speed from logical travel, not transform differences.
6. Drain fuel and advance grace in playing time; recompute score/best; resolve terminal cause once (Lives takes priority over Fuel).
7. If still Playing, recycle/fill world and validate new spawn plans; rebase if needed. Publish final snapshot for that tick.

Do not let callback order between MonoBehaviours decide gameplay. No separate per-object Update moves obstacle roots. Presentation may interpolate visual children; the sensor uses authoritative logical poses. Fuel collected during the tick applies before drain. After a terminal tick no new content spawns. Freeze actual speed at 0 for Paused/GameOver while retain configured cruise target for resume; engine and neon respond to that actual simulation speed.

## Reliable high-speed contacts

Player logical bounds: center at root, half extents (0.85,0.45,1.8) m. Cars use half extents ≤(1.1,0.6,2.2); coin/fuel interaction boxes approximately (0.5,0.6,0.5). Root rotation stays identity and scale1. Visual roll/bob belongs to a child. BoxCollider authoring shapes mirror these extents for inspection; logical sweeps are authoritative. Set interaction layers and trigger flags consistently but **do not also damage/collect in OnTriggerEnter**.

For player endpoints a0,a1 and object endpoints b0,b1, compute relative start r=a0-b0, relative displacement v=(a1-a0)-(b1-b0), and expanded half extents h=ha+hb. Solve slab entry/exit intervals for r+t*v in [-h,h] over t∈[0,1]. For near-zero v on an axis, reject only if r is outside that slab. A hit exists when max(entry axes,0) ≤ min(exit axes,1). Initial overlap has TOI0. A reusable contact array sized to maximum active interactables avoids allocation; overflow must fail visibly in development, never silently miss damage. This covers moving targets, thin pickups and lane changes at maximum speed. CCD may supplement physical contacts but is not evidence of reliable trigger collection.

Activation IDs prevent duplicate encounters after pooled reuse. Mark an obstacle encountered on first contact even when grace blocks damage. Never mutate the active registry while iterating it: collect pending returns and execute after dispatch. Re-rented instances receive a new activation ID and pose history. Test near-zero velocity, grazing, simultaneous contacts, rebase, and slow render frames with multiple fixed ticks.

## World coordinates and floating origin

The player truly advances along +Z. Stationary cars remain stationary relative to the road; moving cars advance at 8 m/s. Track total run distance as double independently of local scene coordinates. At local player z≥1000 m, subtract a whole 60 m multiple from all active world positions so the player returns near the origin. Keep absolute segment/spawn indices and accumulated origin offset separately.

Shift each top-level object once: player Rigidbody, each active segment root, each active interactable root, camera, and free VFX/background roots. Do not shift both a parent and its child. Teleport rigidbody positions for this operation rather than sweeping through 1000 m; shift both previous and current logical poses, camera target/damping caches, trails and particle buffers or use local-space effects under shifted roots. Rebase occurs after contact dispatch. It must neither count as distance nor trigger a collision. Pool storage is not part of the active shift list; rent computes current local position from absolute distance minus origin offset.

## Procedural segments and fair spawning

RoadSegment has exact z0/z60 endpoints, 9.6 m width and matching seams. RoadManager uses ordered long indices and ensures ≥300 m ahead and ≥60 m behind, renting extra before returning if a frame skips multiple boundaries. Prewarm enough for the coverage bound plus two spare modules. Every segment type shares endpoints; choose open/tunnel/entrance/exit variants via a seeded schedule. Physics floor can be simple boxes; the runner does not depend on gravity or wheel simulation.

Traffic uses 480 m blocks: 360 m protected-lane traffic span + 120 m fully clear transition buffer. Rows every 60 m; at most two occupied non-safe lane slots. First 180 m contain no traffic. Pickups occupy separate validated slots. A block may choose any next safe lane only when the clear buffer and bounded lane timing permit it. Generation plans are made before spawning and independent of the player's last-second lane choice, so obstacles never spawn in reaction to an evasive action.

For moving traffic, reserve its full longitudinal envelope until its despawn point, with horizon H=(spawnZ-playerZ+despawnBehind)/(minimumPlayerSpeed-trafficSpeed). Require denominator>0. Use max traffic speed and min player speed, expanded car extents, and reject any envelope crossing a safe-lane/transition reservation. Moving traffic never changes lane. Near boundaries substitute a stationary obstacle or omit the slot. Omission is safer and respects pooling exhaustion. Fuel every ≤220 m must be on the safe route; avoid obstacle overlap and transition timing. Record seed, block index, config hash and rejection reason for failures. Keep gameplay RNG separate from cosmetic fireworks RNG.

## Pool contract and starting capacities

Prewarm in bootstrap/Menu before accepting Start: RoadOpen8, RoadTunnel8, RoadEntrance4, RoadExit4; Stationary64, Moving32, Coin160, Fuel24; Spark16, CoinBurst16, FuelBurst8, Firework8. These are conservative starting capacities to be measured in task15, not fixed requirements. Pool all recurring VFX as well as every PDF-listed object. Streetlights and lane markers are children of pooled road segments, so no separate dynamic creation is needed. Audio has eight reusable one-shot sources created before play.

Rent: reserve inactive instance → increment activation ID → set parent/position, logical pose histories, rotation and scale → assign spawn context → clear consumed/grace-independent flags and trails → register → enable renderers/colliders → activate. Return: unregister/defer safely → disable colliders → cancel coroutines/timers → stop audio/particles with Clear → restore materials/visual transforms → deactivate → push once. ReturnAll uses a stable snapshot or reverse index and is idempotent. No return-on-OnDisable recursion. Pool miss: omit optional obstacle/coin/VFX; required fuel uses reserved capacity and replaces an optional pickup slot; road miss prevents entering Playing or freezes with an explicit diagnostic, never silently leaves a gap or allocates during play. Release all pools only on shutdown.

## Script and system contracts

Paths below are relative to `Assets/NeonTunnelRunner/Scripts/`. Classes with shared support enums/structs are named in their row to avoid needless one-line files. Public APIs are contracts; final signatures can use readonly structs. Update this document and affected prompts if a signature must change.

| Script | Responsibility | Public API | Data ownership | Dependencies / config | Unity lifecycle | Events and interactions | Tests |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Core/GameState.cs | GameState and GameOverReason enums | Menu, Playing, Paused, GameOver; None, Lives, Fuel | No mutable data | None | None | Transition contract | V04 |
| Core/RunSession.cs | Single authoritative run model | Reset(seed); read-only Snapshot; internal mutation methods | Lives, fuel, elapsed, double distance, int score/coins/best, actual speed, run ID | GameplayConfig; no presentation | Plain C#; explicit Reset | SnapshotChanged; Damaged; Collected; typed payload structs here | V09,V10,V11,V13 |
| Core/GameManager.cs | Required singleton; lifecycle commands only | Instance; State; StartRun(); Pause(); Resume(); Restart(); ReturnToMenu(); EndRun(reason); Quit() | State and RunSession instance; terminal guard | Serialized scene services; HighScoreStore | Awake validates singleton; Start initializes; OnDestroy clears self/timeScale; application pause/quit flush best | StateChanged(old,new); RunEnded(snapshot,reason) | V04,V12,V13 |
| Core/GameplayLoop.cs | Explicit deterministic simulation order | ResetRun(seed); Step(dt); IsRunning | Tick index; references; no second copy of run values | GM, player, survival, progression, roads, spawn, sensor, origin | FixedUpdate calls Step only while Playing | Publishes through model; calls EndRun at most once | V10,V12,V13 |
| Input/InputRouter.cs | Action asset lifetime and command buffer | ConsumeLaneCommand(); Clear(); ApplyState(state) | One pending input batch; map enable state | Runner.inputactions; GM; no device polling | Awake caches actions; OnEnable subscribe; OnDisable unsubscribe/disable | Pause request; lane press callbacks | V02,V03 |
| Player/PlayerController.cs | Three-lane motion and authoritative poses | ResetRun(); RequestLane(delta); PredictStep(dt,speed); CommitPose(); ShiftOrigin(delta) | Lane index, transition phase, queued command, previous/current logical pose | InputRouter; GameplayConfig; kinematic Rigidbody | Awake caches; called by GameplayLoop; visual interpolation only in presentation | Pose data to sensor; no damage or UI ownership | V03,V08 |
| Player/PlayerSurvival.cs | Lives, grace and fuel rules | TryDamage(encounter); AddFuel(amount); StepFuel(dt); EvaluateEnd() | Grace deadline only; resources live in RunSession | RunSession; GameplayConfig | Explicit Step from GameplayLoop; no own Update | Writes Damaged and snapshot; returns reason | V09,V10 |
| Player/InteractionSensor.cs | Continuous contact ordering | CollectContacts(playerStart,playerEnd,active); DispatchContacts(context) | Reusable fixed-capacity contact buffer; activation IDs | SweptBounds; active Interactables; player shape | Explicit step before pose commit | Dispatches TryInteract sorted by TOI then pickup/obstacle priority then spawn sequence | V07,V08,V09 |
| World/SweptBounds.cs | Pure relative swept AABB mathematics | TryHit(a0,a1,ha,b0,b1,hb,out t) | No state; axis-aligned half extents | Vector3 math only | None | No events | V08 |
| World/RoadManager.cs | Endless module coverage and seam agreement | Reset(seed); EnsureCoverage(distance); RecycleBehind(); IsOpenSkyAt(z) | Ordered active segments; long segment index | PoolService; WorldConfig; seeded System.Random stream | Explicit step; no per-segment Update | Environment kind for fireworks | V05,V06 |
| World/RoadSegment.cs | Pooled 60 m module and endpoint data | OnRent(context); OnReturn(); Length; EnvironmentKind | Index and local origin; mesh markers; no gameplay child ownership | PooledObject; mesh/light children | Awake caches; pool callbacks reset | No events; decorative children travel with segment | V05,V06 |
| World/WorldOrigin.cs | Keep local coordinates bounded | RebaseIfNeeded(); Reset(); Offset | Double accumulated offset | All top-level world roots; player bodies; camera and pose caches | End of tick; never during sensor dispatch | OriginShifted(delta) for particles/camera | V05,V15 |
| World/SpawnDirector.cs | Deterministic traffic and pickups | Reset(seed); FillAhead(); RecycleBehind(); ActiveInteractables | Next row/fuel distances; active registry; spawn sequence | PoolService; PatternValidator; WorldConfig; RunSession | Explicit step; no presentation random calls | Spawn context includes lane, z, seed, speed, activation | V06,V07,V21 |
| World/PatternValidator.cs | Reject unsafe spawn candidates | ValidateBlock(plan,horizon); ValidateMovingEnvelope(); ReachableLanes() | No mutable state; block struct definitions here | WorldConfig; lane timing; bounding extents | Pure C# calls | Diagnostic rejection reason, seed and block ID | V21 |
| World/ProgressionSystem.cs | Difficulty, distance score, best calculation | Step(dt,travel); AddCoin(); RefreshBest() | No duplicate resources; cached curve result only | RunSession; GameplayConfig; HighScoreStore | Explicit tick; save request only on lifecycle boundaries | SnapshotChanged; BestChanged as snapshot field | V11 |
| Pooling/IPoolable.cs | Explicit lifecycle interface | OnRent(SpawnContext); OnReturn() | No state; SpawnContext type declared here | PoolKey; value types | None | No events | V06 |
| Pooling/PoolKey.cs | Stable typed prefab keys | RoadOpen, RoadTunnel, RoadEntrance, RoadExit, Stationary, Moving, Coin, Fuel, Spark, CoinBurst, FuelBurst, Firework | Enum only | None | None | No events | V06 |
| Pooling/PooledObject.cs | Pool identity and activation token | ActivationId; IsRented; ReturnToPool(); virtual OnRent/OnReturn | Pool handle; monotonically incremented activation ID | PoolService; IPoolable | Awake caches; never return implicitly in OnDisable | Explicit registration/unregistration through pool lifecycle | V06 |
| Pooling/PoolService.cs | Bounded prewarmed pools | Prewarm(); TryRent(key,context,out item); Return(item); ReturnAll(); Counters | Inactive stacks and active sets per key | PoolConfig; no singleton | Initialize before Playing; shutdown destruction allowed | Exhaustion diagnostic; no gameplay-time growth | V06,V20 |
| Interactables/Interactable.cs | Required shared abstract base | TryInteract(context); protected virtual OnInteract(context); override OnRent/OnReturn | Consumed/encountered flags; logical bounds and poses | PooledObject; interaction context services | No trigger damage route; pool and loop driven | Virtual hook overridden by all four concrete categories | V08,V21 |
| Interactables/StationaryCar.cs | Stationary obstacle | override OnInteract; PredictStep | Zero longitudinal speed before world rebase | Interactable; PlayerSurvival via context | Explicit step preserves road-space z | Calls TryDamage; marks encountered once even during grace | V08,V09 |
| Interactables/MovingCar.cs | Moving obstacle in fixed lane | override OnInteract; PredictStep(dt) | Forward speed 8 m/s; initial lane; no lane AI | Interactable; WorldConfig; PlayerSurvival | Explicit step predicts/commits +Z | Damage through context; trajectory validated at spawn | V08,V21 |
| Interactables/Coin.cs | Coin reward | override OnInteract | No reward state outside consumed flag | Interactable; ProgressionSystem via context | Visual spin on child only; reset on rent | AddCoin; Collected(Coin,point); return | V07 |
| Interactables/FuelCan.cs | Fuel reward | override OnInteract | No independent fuel store | Interactable; PlayerSurvival via context | Child-only bob; reset on rent | AddFuel(30); Collected(Fuel,point); return | V07,V10 |
| Presentation/ChaseCamera.cs | Rear camera, smoothing and damage impulse | ResetPose(); OnOriginShift(delta); AddImpulse(amount) | Camera damping velocity and shake phase | Player; PresentationConfig; damage events | LateUpdate follows interpolated pose; subscribe OnEnable/OnDisable | Never changes simulation transform | V15 |
| Presentation/VehicleVisuals.cs | Body roll/bob/flash, lights and thrusters | ResetVisuals(); ApplySpeed(actualSpeed) | Visual-only offsets; material property block; flash timer | RunSession; Player; PresentationConfig | Update/LateUpdate while appropriate state; cleanup subscriptions | Speed from model; damage from model | V16,V17 |
| Presentation/VfxController.cs | Route gameplay cues to pooled effects | PlayDamage(point); PlayPickup(kind,point); ResetEffects() | Active effect references; no gameplay decisions | PoolService; RunSession events; PresentationConfig | OnEnable/OnDisable; Update lifetime while Playing | Visual subscriber only | V17 |
| Presentation/PooledVfx.cs | Reset reusable particle bursts | override OnRent/OnReturn; IsComplete | Particle children, trails, lifetime | PooledObject; ParticleSystem | Explicit completion polling in VfxController; no StopAction.Destroy | Return when all child particles complete | V06,V17 |
| Presentation/FireworkDirector.cs | Sky launch/burst cadence | Reset(seed); Tick(dt,openSky) | Independent cosmetic RNG and countdown | RoadManager; PoolService; PresentationConfig | Update scaled playing time; paused timers freeze | Launches pooled rocket with burst sub-emitter | V17 |
| Audio/AudioController.cs | Music/engine and bounded one-shot voices | ApplyState(); ResetVoices(); SetVolumes() | Two loop AudioSources, eight precreated SFX voices | AudioConfig; mixer; RunSession events | Subscribe OnEnable/OnDisable; Update engine pitch | Audio-only subscriber; no global singleton | V18 |
| UI/HudPresenter.cs | Live HUD snapshot projection | Refresh(snapshot) | Cached formatted values; references to five fields | RunSession; TMP/uGUI | OnEnable subscribes and immediately refreshes; OnDisable unsubscribes | Never owns score/lives/fuel | V14 |
| UI/MenuPresenter.cs | Panels, focus and commands | ShowFor(state); OnStart/Resume/Restart/Menu/Quit | Panel refs; first selected button | GM; EventSystem; InputRouter UI map | OnEnable/OnDisable state subscription | Commands to GM; buttons remain active at timeScale0 | V19 |
| Persistence/HighScoreStore.cs | Only PlayerPrefs adapter | Load(); SetIfHigher(score); Flush() | Cached best and dirty flag; key NTR.HighScore.v1 | PlayerPrefs; injectable test key | Plain C#; explicit flush from GM lifecycle | No per-frame disk writes | V11 |
| Config/GameplayConfig.cs | Immutable gameplay tuning asset | Fields: lanes, transition, lives, fuel, speed, score, difficulty | Serialized authoring values only | ScriptableObject | OnValidate clamps authoring ranges | No runtime mutation | V03,V10,V11 |
| Config/WorldConfig.cs | World/pattern authoring asset | Fields: segment length, coverage, spacing, corridor and traffic bounds | Serialized seed and generation limits | ScriptableObject | OnValidate rejects nonpositive coverage and unsafe timing | No runtime mutation | V05,V21 |
| Config/PoolConfig.cs | Prefab capacities and keys | Entries: key,prefab,prewarmCount | Serialized catalog only | ScriptableObject | OnValidate unique keys and positive counts | No runtime mutation | V06 |
| Config/PresentationConfig.cs | Camera/light/VFX style tuning | Fields: camera offsets, light range, bloom guidance, firework interval | Serialized visual settings | ScriptableObject | OnValidate bounds | No gameplay writes | V15,V16,V17 |
| Config/AudioConfig.cs | Licensed clips and audio tuning | Fields: five clips, mixer groups, pitch/volume ranges | Serialized audio data | ScriptableObject | OnValidate missing clip checks | No gameplay writes | V18 |
| Editor/ProjectSetupValidator.cs | Editor-only wiring and requirement audit | ValidateProject() menu command | Validation results only | Editor APIs; scene/config inspection | Editor menu; never included in player assembly | Reports missing references, maps, pool entries, required overrides | V01,V02,V21 |
| Editor/BuildDelivery.cs | Editor-only repeatable build entry | BuildLinux(); ValidateBuildProfile() | Build output configuration | Editor BuildPipeline/Build Profiles | Invoked explicitly by Editor or documented batchmode | Produces BuildReport and exit code; no imagined success | V23 |

## Rendering, UI, audio and persistence details

Neon intensity uses u=InverseLerp(0,50,actualForwardSpeed), with light intensity Lerp(0.3,3,u) and emissive multiplier Lerp(1,5,u). Tune in a fixed exposure scene; these are starting values. Avoid Renderer.material every frame; cache a MaterialPropertyBlock for the single player, accepting that it may affect SRP batching. Many repeated world meshes share materials. Use a small number of shadow-casting lights; emissive geometry alone does not illuminate nearby road, so headlights and a modest underglow light are real Light components.

InputRouter owns Gameplay/Left and Gameplay/Right Button actions; System/Pause remains enabled in Playing and Paused. UI map is enabled for menus; remove StandaloneInputModule and use InputSystemUIInputModule. UI/Cancel is not also bound to Escape while System/Pause owns it: use Backspace for generic UI cancel, or route Escape through exactly one System handler. No Keyboard.current polling or legacy Input calls—even if they come from a sample. UI action bindings include point, click, scroll, Navigate (arrows), Submit (Enter) and Cancel (Backspace).

HUD displays speed in km/h (m/s×3.6), fuel normalized0..1 and percent, lives3..0, integer score and best. UI presents snapshots, not per-widget independent calculations. Main/Pause/Over panels have deterministic initial button focus; Canvas Scaler reference1920×1080, match0.5; keep 48 px safe margins.

Audio mixer groups: Music, Engine, SFX. Engine loop pitch0.8..1.5 with speed; pause loop sources and one-shots explicitly; music may continue quietly. GameOver stops engine; restart does not duplicate music. Separate coin/fuel clips. PlayerPrefs key `NTR.HighScore.v1`; load max(0,value); best=max(best,score). Mark dirty as needed but Save only on run end, menu/quit and application pause. Clamp long scores to int.MaxValue. Automated tests use isolated keys and do not erase the player's real best.
