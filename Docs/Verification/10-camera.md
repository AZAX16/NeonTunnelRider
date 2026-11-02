# Task 10 — Rear camera and hovercar motion

Implemented from clean local `d41f4f6`; no newer commits or initial user changes were present. The Git/Unity root is the inner NeonTunnelRunner directory. No applicable AGENTS.md was found. Read Environment, Task09 including its Task10 API handoff, Task03/05/08 pose/origin/flash handoffs, original10/11 prompts, requirements, architecture, project structure, acceptance, environment setup and visual system. Inspected runtime APIs, tests, packages, hierarchy and serialized scene/prefab. `ChaseCamera`, `PresentationConfig` and `Presentation.asset` did **not** exist; they are added once. VehicleVisuals extends the existing implementation.

Pinned environment retained: Editor6000.6.3f1; CLI1.0.0-beta.13 (`/home/alireza/.local/bin/unity.previous`), official plugin0.1.8-beta, Pipeline0.9.1-exp.1; URP/Core17.6.0, Input1.20.0, Test Framework1.8.0/NUnit2.1.0; Linux Mint Virginia/glibc2.35. Unity CLI skill used; installed command schemas and capture implementation inspected. PATH's existing beta.14 was used only for the initial read-only version/status discovery. No upgrades, new packages, input/identity/ignore changes or push.

## Changes and saved assignments

Paths below are under Assets/NeonTunnelRunner unless prefixed Docs/Evidence.

- New Scripts/Presentation/ChaseCamera: LateUpdate rear follow, lateral filter, accepted-damage impulse, explicit lifecycle/reset and IWorldOriginParticipant caches. Private serialized dependencies; no singleton or input consumer.
- Existing Scripts/Presentation/VehicleVisuals: child-only roll/bob from cached authored neutral transforms. Existing renderer, shared material, flash duration/color and cached merged MaterialPropertyBlock retained. Flash expiry clears only color feedback, not mesh animation. Menu/disable detaches damage listeners; reset prepares one listener per presenter.
- New Scripts/Config/PresentationConfig and Configs/Presentation.asset: immutable shared camera/motion settings. No runtime writes to the asset.
- Existing Scripts/Core/GameplayLoop: serialized Camera accessor; initialize, prepare/reset, cleanup/shutdown and stopped-pose presentation calls. Fixed-step prediction/contact/commit/resources/progression/terminal/world/persistence ordering is unchanged.
- Existing Scripts/World/WorldOrigin: validates either the old parent-owned fixture anchor or a single independent registered camera owner. Existing serialized camera reference retained. ProjectSetupValidator requires the new saved-scene wiring and collider-free pivot.
- Existing PlayerHovercar.prefab gains only VehicleVisuals references: `player`=root PlayerController; `visualPivot`=`Visual`; `configuration`=Presentation.asset. Body/Canopy remain under Visual with authored local transforms. `flashRenderer`=`Visual/Body`, URP/Lit shared ReferenceMarking, flashDuration=.25s, flashColor=(1,.18,.1,1), unchanged. Scene overrides still assign GameManager to survival/visuals and existing sensor services. Rigidbody interpolation=None, root rotation identity/scale1, box1.7×.9×3.6 and all logical gameplay geometry unchanged.
- Existing Game.unity MainCamera is reparented from Player/CameraAnchor to neutral `Presentation`. Same camera, tag, URP data, post-processing/Volume mask and AudioListener; one enabled camera/listener. CameraAnchor remains unchanged as a retained stable graybox asset. No superseded follow component existed.
- MainCamera/ChaseCamera explicitly references Systems/GameManager, existing PlayerController, Presentation.asset, its own Camera, and Systems/WorldOrigin. GameplayLoop.Camera references this component. WorldOrigin.chaseCamera references this same Transform; additionalParticipants is exactly `[SpawnDirector, ChaseCamera]`. Player/roads/inactive storage are disjoint from the camera root. Visuals and camera share Presentation.asset (GUID `8cc0bd840e8337a3fbe4656ff60cee73`).
- New Tests/PlayMode/CameraResetTests; the two existing movement rig builders now assign the visual dependencies explicitly. No gameplay assertions weakened. Docs/Setup/Task10* retain Editor-only authoring, test, viewport, visual, native smoke and preservation recipes outside player assemblies.

## Final framing and motion

| Setting | Saved value / reason |
| --- | --- |
| Offset / look-ahead | (0,4.2,-8)m relative to stable player;12m; retained after runtime inspection |
| FOV / rotation | Fixed65° vertical; fixed yaw0/pitch11.86°; no speed FOV or camera-forward animation |
| Initial world camera | (0,4.8,-8), player starts at y.6 |
| Clips | .1–280m; far plane fits the actual >=300m ahead coverage, including the8m rear offset; avoids the old500m view beyond generated road |
| Lateral follow | Exponential time constant .08s, hard lag cap .65m; no forward lag/filter |
| Roll | <=6° authored, hard getter bound8°; .09s exponential response to existing logical lateral direction |
| Bob | <=.06m authored, hard getter bound.08m; amplitude uses Snapshot.ActualSpeed/50;1.4Hz base, scaled .8–1.2 by actual speed |
| Accepted damage | Deterministic .085m diagonal positional kick, .24s decaying oscillation; summed overlapping displacement clamped to total.12m; no gameplay/cosmetic RNG consumption |

The pitch/offset/look-ahead/FOV starting values were retained because runtime images keep the player in the lower center and the three-lane decision horizon clear through open road, roof interiors and both portals. Only the prior500m far plane was reduced. The camera y4.8 and maximum near-plane extent remain below the measured >8m roof underside; camera x stays within the corridor even at outer lanes plus bounded lag. Inspected images show no car/roof/wall near-plane intersection. HUD corner space and central decision area are reserved; a full HUD does not exist, so actual HUD overlap validation is **NOT RUN**.

The one pose source is the existing rendered stable player/root Transform with authored Rigidbody interpolation=None. No custom pose-history interpolation, second Rigidbody interpolation or LateUpdate root writes were added. Camera Y/Z follow that source directly; X is presentation framing only. Visual roll reads PreviousPosition/CurrentPosition direction; bob reads actual simulation speed. It never follows the bobbing mesh or derives speed from floating-origin transform deltas.

Camera caches: basePosition and targetPosition are world positions; impulseSeed/impulseOffset are displacement vectors. ApplyPose always uses base+impulse and a fixed orientation, never last frame's shaken Transform as filter input. There is no damping velocity/spring cache. WorldOrigin calls ShiftOrigin once after contacts/commit: translate Transform/base/target by -deltaZ; preserve impulse vectors/age and lateral filter state. Visuals inherit the one player-root shift and retain identical local offsets. Player's existing double-history conversion, roads, traffic and inactive storage ownership remain intact.

Pause and GameOver gate presentation through Loop.IsRunning, even with timeScale manually restored to1. No unscaled delta or hidden velocity accumulates. ApplyState first holds the final root, then HoldView reconciles its final Y/Z and bounds X without advancing lateral smoothing, bob, roll or impulse. This covers pausing between FixedUpdate and LateUpdate. Resume starts from that held pose. Final accepted damage immediately applies a camera kick; GameOver freezes it and the existing flash until reset/disable. The flash still uses its original playing-clock deadline; a flash already expired within a deliberately large terminal tick stays expired. No post-death unscaled fade was added.

Reset snaps base/target to the centered root, clears impulse seed/offset/age, roll and bob phase, restores authored local position/rotation/scale and the existing baseline color. Cleanup/menu unsubscribes both damage listeners; enable/disable cycles do not register new origin participants. Reset prepares subscriptions once. Camera enable snaps/clears feedback; visual disable restores neutral. Reset never changes the gameplay box or lane timing.

## Executed verification

Evidence is under Evidence/Task10. Final full suites: **130 EditMode +154 PlayMode =284 passed**,0 failed/skipped (`test-summary.json`, per-case `*-results.json`). This includes14 new presentation cases.

| Scope | Executed evidence |
| --- | --- |
| Presentation on/off | Same capped-speed commands, queued reversals, real swept coin/fuel/damage encounters; identical root/collider bounds, distance/time/speed/resources/score/coins, contact counts and seeded plans |
| Render rates | Same .02s fixed simulation at30/60/144 render Hz from starting speed and30/60/240Hz at cap; finite poses, direct source Y/Z, lag<=.65m, bounded roll/bob, no sustained drift after settling; synthetic render cadence, **not measured GPU FPS** |
| Rebase | Three lane cases during transition and impulse: current/base/target each shift960m once, relative pose/impulse/pivot invariant, snapshot unchanged; zero-delta/no spring-back; old world tests retain inactive-storage checks |
| Pause/end/reset | State gate with timeScale1, zero delta, continuous resume, rejected grace events, synthetic overlapping accepted-event bound, final-hit freeze, pause between fixed/render;50 rebased Paused/GameOver restarts plus repeated disable/enable, exactly one damage response and no menu damage observers |
| Neutral/material ownership | Nonzero authored position/rotation/scale restored; flash expiry leaves animated mesh pose intact. Existing unrelated MaterialPropertyBlock override/shared-material/flash tests still pass |
| Gameplay regressions | Existing input/held keys, swept contacts, pools/tokens, survival, score/best/lifecycle persistence, seeded safety/supply, both100km routes, origin/reset stress and natural181s cap run pass in the full suites |
| Seed/supply scope | Existing1,000-seed ×21-block safety/envelope sweeps and start/mid/cap density sweeps rerun;63,000 required fuel reservations retain measured maximum gap180m (`instrumentation.json`) |

The first full run passed130+152 before the menu-cleanup and stopped-pose refinements. The focused menu-observer assertion then caught an incorrect lifecycle call (11/12); corrected it to cleanup/unsubscribe, with12/12 afterward. Added paused-final-pose and authored-neutral/flash-expiry coverage. Final full284-pass verdicts follow all fixes, including a test-only reference-comparison compiler-warning cleanup. Historical `*-before-menu-cleanup.json` and `focused-before-cleanup-fix.json` are explicitly intermediate, not final acceptance. An initial focused command used invalid filter_type=name and was rejected; it was corrected to installed `testName`; no stale test status is counted.

Real saved-Game visual QA passed. `visual-ticking.json` proves advancing frames before capture. `visual-route.json` records **15 native1280×720 (16:9)** framing captures with actual speed/lane/pose/roll/bob/projection/coverage data. The natural-start group is22.081–25.226m/s as progression continues through490m; it is not mislabeled as constant22. Cap captures use an isolated180s elapsed fixture and actual50m/s. The independently executed natural181s cap test remains in the full suite. No alternate scene, concept image or generated visual was used.

A compact review set (paths relative to Evidence/Task10; additional raw smoke milestones are retained):

| Case | Runtime images |
| --- | --- |
| Starting speed, all three lanes | `start-open-lane-1.png`, `start-open-lane0.png`, `start-open-lane1.png` |
| Maximum speed and120m reference | `cap-open-lane1.png`, `cap-open-after-exit.png` |
| Entrance / tunnel / exit | `start-entrance.png`, `cap-tunnel.png`, `cap-exit.png` |
| Additional native4:3 viewport | `cap-tunnel-4x3.png`, **960×720**, actual50m/s at260.9999m; `aspect-4x3.json` |
| Native transition/damage rebase | `cap-before-native-rebase.png`, `cap-after-native-rebase.png`; offset0→960 during the transition/hit; caches checked immediately by the automated rebase cases |
| Mesh motion / resume / exact reset | `motion-roll-paused.png`, `motion-resumed.png`, `motion-reset-neutral.png`,1280×720; `native-motion.json` |
| Accepted flash, resume and final hit | `native-damage-paused.png`, `native-damage-resumed.png`, `native-lives-gameover.png`,960×720; `play-sessions.json` |

The120m check uses lane centers and full2.2×1.2×4.4m threat bounds projected by the actual runtime Camera, for16:9 and4:3 at each captured scene position. In the centered baseline these centers are at viewport x.48880/.50000/.51120,y.63811; adjacent centers are about14.3px apart at1280×720. Projected boxes are approximately9–11px wide and6.1px high. Near-road edge projections also remain inside the viewport. In `cap-open-after-exit.png`, two actual warm-colored pooled stationary-car references are deliberately placed exactly120m ahead in non-protected lanes while **paused**, rendered and inspected, then returned with their captured tokens before resume. They have no gameplay registry and do not change the seeded plan. This measured reference plus the visible separate car/lane silhouettes and clear tunnel/portal sightline supports the useful120m horizon; farClip alone is not the evidence. Future final art/lighting must retain that contrast and recheck recognizability.

The native motion capture paused at lane phase.285714, actual50, roll**-2.8663°**, bob**-.059993m**, root quaternion identity and camera relative(-.65,4.2,-8). Resume settled the bank; exact reset captured roll0,bob0,distance0,pivot0 and camera relative(0,4.2,-8). Inspected the actual rolling/neutral images and damage/rebase sequence; all three lanes and the stopped view remain coherent. These are runtime image sequences and measurements, not pixel-identical screenshot tests or a claimed video/Profiler run.

Saved Game smoke **passed in both established reload configurations**: normal reloads and domain+scene reload disabled. Ten-wall-second pauses were**10.108062s /10.111916s**, freezing snapshot/traffic/grace/flash and camera base/position/impulse/visual pivot/rotation; native Escape resumes. Both routes collected17 coins/12 fuel, retained3 lives/positive fuel, spawned14 fuel/2 moving/18 stationary, crossed two rebases and retained304 prewarmed instances with0 natural traffic misses. Each exercised all four lane bindings, Paused/GameOver restart, real three-hit Lives and Fuel terminal paths, frozen final flash+impulse, menu cleanup, deliberate saturation and exit while Paused. Config hashes match before/after. These smoke screenshots were captured at960×720; the separate main visual route uses1280×720.

Initial exploratory images used the pre-existing1101×598 free-aspect Game view. They were replaced by the final fixed-viewport captures after detecting that mismatch. An exploratory off-screen capture went under the automation authoring root; that generated Assets/Evidence folder was removed through AssetDatabase and replaced by the native4:3 capture. No evidence image remains under Assets. Input focus/device policies, run-in-background, reload settings and auto-tick are restored. `viewport-restored.json` confirms both temporary custom sizes removed and the original selected viewport index0 restored.

## Reproduce

Use the pinned CLI and this checkout's Editor; keep Game saved/stopped before starting recipes. No test harness enters a player assembly or adds a production control.

```bash
TASK10_CLI=/home/alireza/.local/bin/unity.previous
TASK10_PROJECT=/home/alireza/NeonTunnelRider/NeonTunnelRunner
"$TASK10_CLI" status --until-ready --project-path "$TASK10_PROJECT" --format json
"$TASK10_CLI" recompile --project-path "$TASK10_PROJECT" --format json
python3 Docs/Setup/Task10Tests.py
python3 Docs/Setup/Task10PlaySessions.py
python3 Docs/Setup/Task10Tools.py audit
python3 Docs/Setup/Task10Preservation.py
```

Task10Setup.Configure is the saved-scene/prefab authoring recipe; do not rerun it as an audit or after later tasks add origin owners. Task10VisualRun.py runs native ticks/actions and captures framing, projections and rebase; Task10AspectRun.py runs the additional native viewport. Task10Viewport.Configure establishes a temporary1280×720 Game view; Task10AspectRun establishes960×720. Run `python3 Docs/Setup/Task10RestoreViewport.py` afterward to restore the original selected index and remove both temporary custom sizes. Task10MotionRun.py runs the native roll/resume/neutral-reset capture, using the1280×720 index saved by the viewport setup. All these recipes require a stopped Editor that the caller owns. Final evidence records restoration. Suites use installed Pipeline `test run --mode editor|playmode --filter_type assembly --async_tests true`; focused filters use `testName`, followed by completed test status.

## Task 11 API handoff

- Keep PlayerHovercar's logical root, body/box, layer, rotation, scale, sensor and stable CameraAnchor unchanged. Replace/add art **under Visual**, the cached neutral unit pivot (current local position0/rotation identity/scale1); current children are Body and Canopy. VehicleVisuals owns this pivot's local roll/bob. Do not put colliders under it. The nonidentity-neutral test supports a future authored neutral transform; cache it before runtime animation.
- VehicleVisuals retains FlashRenderer and the .25s accepted-hit flash. It owns only `_BaseColor` (fallback `_Color`) in its cached MaterialPropertyBlock. New emissive/underglow properties must merge their owned values into the current block, preserve base color and unrelated values, use shared materials and avoid per-frame material creation. Preserve the current renderer reference/GUID or reassign explicitly when replacing Body. A flash ending must not reset mesh animation or future lighting.
- Actual speed is `GameManager.Session.Snapshot.ActualSpeed` (immutable snapshot,0 in Paused/GameOver); `CruiseSpeed` is the retained target. Task11 headlights/underglow are not present. Extend the same PresentationConfig/Presentation.asset and VehicleVisuals; do not create a second visual/speed/input owner. No new lighting/art/material production is part of Task10.
- Camera follows only stable player/root Transform with interpolation=None. If later visual interpolation is introduced, change the camera and visual render source coherently; do not independently interpolate the root twice. Keep fixed65° FOV, offset(0,4.2,-8), look-ahead12, near.1/far280 and <=.65m lateral lag unless new runtime art evidence justifies tuning. Current camera clearance assumes tunnel underside>8m and architecture outside9.6m road. Recheck final mesh/headlight silhouettes, roof/walls and real HUD when those exist.
- Accepted Session.Damaged value events drive flash and camera independently, once per run listener. Grace rejection emits neither. Impulse vectors never enter follow caches or gameplay RNG. Final hit freezes; pause/resume uses state gates. No VFX/sparks/audio listeners added yet.
- GameplayLoop explicitly initializes/prepares/cleans both presenters; reset centers player before camera/pivot reset. Preserve HoldView after final root hold, and CleanupRun detachment. No per-enable new run.
- WorldOrigin participants are `[SpawnDirector, ChaseCamera]`, both independently owned. Visual/Lights under the player inherit its translation and must not also register. Camera ShiftOrigin translates world-valued base/target/Transform once; displacement/velocity vectors stay untranslated. Add future independent background/VFX owners deliberately without overlapping roots or inactive storage. No origin event exists; use the actual participant API.

Final compilation is **0 errors/0 warnings**, up_to_date (`compile-final.json`); final Console ground truth **0 errors/0 warnings**, compiling=false/compilationFailed=false (`console-final.json`). Pre-clear post-smoke ground truth is also clean; `console-after-tests.json` retains deliberately expected test diagnostics and intermediate history. Final validator/audits pass with saved Game clean/stopped, Instance clear,timeScale1, auto-tick false, pinned packages/defaults intact and only NeonTunnelRunner.Runtime in the ordinary project player graph (InputSystem/uGUI/TMP references; no Pipeline/UnityEditor dependency).

Preservation audit passes: **288 protected baseline files byte-identical**, no missing metas/duplicate GUIDs, supplied ignore hash unchanged; the complete player prefab matches the baseline after removing only the three new visual reference lines (`preservation.json`). Packages, settings, input maps/actions/GUIDs, road/traffic/pickup prefabs, Gameplay/World/Pools configurations and prior standalone persistence evidence remain preserved.

**R06 / V15 PASS for Task10:** three-lane rear/above framing,120m measured horizon, bounded lag and correct rebase/reset are supported by runtime images and tests. **R14 PASS for the flash+camera-impulse scope:** accepted damage only, restrained bound and frozen feedback verified; sparks remain Task12. **R28:** presentation progress only; final environment/art, night/neon and later effects/UI/audio are not finished.

**NOT RUN:** Profiler,30-minute soak, Task10 player build/final-delivery acceptance and full HUD overlap checks. Task09's already-executed three-launch standalone persistence **PASS remains preserved**, not relabeled NOT RUN; it was not rerun because persistence/identity/save ordering did not change. Full HUD, night/neon production, VFX/sparks/audio and final delivery remain later work. No Task10 blocker remains. Local checkpoint only; final commit/tree status is reported with the response; no push.

Task10 only. Next: **Task11 — Night environment, assets and dynamic neon**.
