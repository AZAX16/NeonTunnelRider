# Task 11 — Night environment, assets and dynamic neon

Implemented from clean local `397a8af`; no newer commits or initial user changes were present. Unity/Git root is the inner NeonTunnelRunner directory. No applicable AGENTS.md exists. Read Environment, Task10/API handoff, earlier pose/origin/pooling/pickup/survival handoffs, original11/12 prompts, requirements/architecture/project structure/art/assets/acceptance/environment setup and visual system. Inspected actual saved scene/prefabs, pivots, speed/state/reset ownership, packages and installed automation schemas. Concept sheets were viewed as direction only.

Pins retained: Editor6000.6.3f1, CLI1.0.0-beta.13 at `/home/alireza/.local/bin/unity.previous`, official plugin0.1.8-beta, Pipeline0.9.1-exp.1; URP/Core17.6.0, Input1.20.0, Test Framework1.8.0/NUnit2.1.0; Mint Virginia/glibc2.35. Unity CLI skill used. PATH beta.14 was only queried during initial discovery; no tool/package/system upgrades. No input/GUID/identity/ignore changes or push.

## Changes and saved ownership

Paths are under Assets/NeonTunnelRunner unless prefixed Docs/Evidence.

- `VehicleVisuals` extends its existing child roll/bob and accepted-hit flash with cached headlights, underglow Light/renderer, emission property ID and reusable merged MaterialPropertyBlock. `_BaseColor` remains flash-owned; `_EmissionColor` is neon-owned. Flash cleanup never restores emission or clears unrelated fields. Tests change measured speed while a flash is still active and again as it expires. No Renderer.material or shared-material runtime writes.
- `PresentationConfig`/`Configs/Presentation.asset` add immutable maximum neon speed and light/emission endpoint ranges. Existing camera/motion tuning is untouched.
- `GameplayLoop.ApplyState` refreshes lighting immediately after Session's actual-speed state change. `FinishTick` refreshes after committed movement, and `Render` refreshes before its motion freeze gate, including delta0. Pause/GameOver freeze roll/bob/flash/impulse but use low-speed neon. Cleanup/disable disables the three player lights; restart recomputes22m/s values.
- `RoadSegment` explicitly owns one optional serialized streetlight. Rent/return disables it. `RoadManager.EnsureCoverage` refreshes selection from absolute segment/player distances: abs(segment.AbsoluteStart+30-playerDistance)<=85m. One source per60m segment bounds enabled street sources to **three**. This adds no per-segment Update, allocations or new singleton. Inactive storage stays dark. Scenery is segment-owned and inherits its one origin shift.
- Original production meshes, shared materials and deterministic panorama are authored/imported in Edit mode by `Docs/Setup/Task11Art.cs`, outside player assemblies. Existing mesh/material/prefab GUIDs are retained. No runtime mesh/primitive/material construction, external art download or speculative asset framework.
- Existing player `Visual`, Body/Canopy renderer identities and flash reference are retained. New art stays under Visual. `Lights` is a stable sibling of Visual under the player, so roll/bob cannot steer the headlights or change logical poses. `Visual/EffectAnchors/ExhaustLeft` and `ExhaustRight` are empty Task12 attachment transforms; no particles are added.
- Existing obstacle/pickup Visual children receive original faceted meshes: bulky parked blocker with two tail bands and warning bar, lower moving car with one tail band, twelve-sided gold ring with a real .52m opening, mint can with handle opening/cap/X stamp. All gameplay roots, BoxColliders, Rigidbody data, logical extents, pickup heights, stable Visual pivots, endpoint objects, pool entries/capacities/tokens and seeded generation are preserved.
- Four road prefabs retain the exact Surface collider/9.6×60m mesh transform, y0 top and z0/60 markers. Shared mesh children provide only four cyan lines at x±1.6/±4.8, rails outside the corridor, angular poles, octagonal upper arches and sparse box skyline/window accents. Roof central underside is8.755m; arch crown underside>8.3m. The roof shoulders stay above8m across the full drivable width, retaining the Task10 clearance assumption. Open is unroofed; Entrance roofs only z30–60, Exit only z0–30. Schedule remains Open/Open/Open/Entrance/Tunnel/Tunnel/Tunnel/Exit. No decorative colliders or background Instantiate loop.
- `ProjectSetupValidator` now also checks headlight/underglow ownership, emission support and saved camera/skybox wiring. New `NightAssetTests`/`NeonLightingTests` cover the new contracts. Existing tests/assertions were preserved.

## Asset/source inventory and budgets

All new production art is original project-authored with Codex assistance; source is `Docs/Setup/Task11Art.cs`. `CREDITS.md` distinguishes it from supplied generated references and Unity template/shader resources. No external asset license is invented or required for a downloaded asset: none was downloaded. `Textures/RetroNightSky.png` is a2048×1024 deterministic star/cloud panorama, uncompressed RGB, linear import, mipmapped, repeat wrap. It is a sky image, not a claimed PBR map.

| Saved prefab | Triangles | Renderers | Distinct shared materials | Real lights authored |
| --- | ---: | ---: | ---: | ---: |
| PlayerHovercar | 296 | 6 | 3 | 3 |
| StationaryCar | 196 | 3 | 2 | 0 |
| MovingCar | 160 | 3 | 2 | 0 |
| Coin | 96 | 1 | 1 | 0 |
| FuelCan | 140 | 2 | 2 | 0 |
| RoadOpen60 | 300 | 6 | 6 | 1 |
| RoadTunnel60 | 816 | 6 | 6 | 1 |
| RoadEntrance60 / RoadExit60 | 576 each | 6 each | 6 each | 1 each |

Geometry is deliberately economical, below doc07's triangle allowances. New mesh transforms have baked dimensions/positive unit scales, finite vertices/unit normals and centered pivots; legacy Surface retains its original positive cube scale. Pickup/car geometry fits existing logical boxes, including pickup bob allowance. No transparent glass or realtime reflection probes.

Exactly **10 shared URP/Lit environment/vehicle materials**, plus the separate skybox material. Display palette is converted from the specified sRGB swatches to linear authoring values. Emission is a separate multiplier:

| Material | Display role | Authored emission multiplier | Assignment |
| --- | --- | ---: | --- |
| ReferenceRoad | Surface #141B3F | 0 | Exact road surfaces |
| RoadArchitecture | Surface #141B3F | .18 | Rails, structure, hazard chassis, can cap/stamp |
| PlayerIndigo | Surface #141B3F | .4 | Wedge body/deck vents, skyline |
| ReferenceMarking | Track #35E6FF | .5 | Four road lines, canopy/lenses, underbody baseline |
| EnergyMagenta | Energy #FF4AC8 | .65 | Twin thruster faces, arch/pole trims |
| DepthViolet | Depth #8B5CFF | .15 | Sparse windows |
| StationaryCar | Hazard #FF8B4D | .3 | Parked hull/lamps |
| MovingCar | Hazard #FF8B4D | .18 | Lower moving hull/lamps |
| CoinGold | Coin #FFD76A | .65 | Ring |
| FuelMint | Fuel #69F5A9 | .55 | Can and handle |

All Lit materials have `_EMISSION` support retained through URP validation using BakedEmissive flags; no baked lighting or realtime GI is claimed. Solids are opaque, metallic .12, smoothness .28. Exact material/mesh/prefab inventories and bounds are in `Evidence/Task11/audit-final.json`.

Available Editor native-frame samples during the cap route show **108–152 draw calls** and **8,511–11,631 triangles** across tunnel/open/rebase samples (visual-route.json). Synchronous extra camera renders in one fixture frame can accumulate substantially higher Editor totals; those are not a single-camera frame budget verdict. The overall ≤250 draw/≤150k triangle guardrails remain performance targets, not measured frame-time acceptance. Profiler and target60fps/p95/GC/memory validation are **NOT RUN**.

## Exact lighting/render assignments

- `Game.unity` uses `Materials/RetroNightSky.mat`, Unity `Skybox/Panoramic`, panorama assigned, Exposure1/Tint.5. MainCamera clear flags=Skybox; same camera/ChaseCamera, FOV65, offset(0,4.2,-8), look-ahead12, clips.1/280, lag<=.65, one MainCamera/AudioListener. No framing or gameplay adjustment.
- Ambient Trilight: sky(.18,.22,.4), equator(.12,.15,.27), ground(.06,.08,.16), intensity1. Fog off. Default sky reflection intensity.25; no realtime probes. `Presentation/ReferenceLight` is the sole moon directional, Euler(42,-28,0), color(.48,.57,1), intensity.65, shadows None.
- `Player/Lights/HeadlightLeft` and Right: Spot, local positions(-.55,-.08,1.55)/(.55,-.08,1.55), Euler(3,-7,0)/(3,7,0), intensity220, range38m, outer50°/inner29°, color(.65,.87,1), shadows None, ForcePixel. These are useful road cones, verified with headlights on/off. Intensity was tuned under fixed exposure; it is independent of the mandated underglow curve.
- `Player/Lights/UnderglowLight`: Point, local(0,-.37,-.2), range3.8m, color(.08,.78,1), shadows None, ForcePixel, intensity from the curve. `Visual/Underglow` uses ReferenceMarking through its own property block; rear/side geometry makes emission visible without Bloom.
- Every road `Scenery/Streetlight` is positioned at(−5.1,5.9,20), beneath the left emissive lamp, Euler(62,90,0): a nonshadowing Spot, range20m, intensity20, color(.48,.51,1), outer110°/inner63.8°, aimed down/across the road. Angular poles have mostly emissive lamps. One selected real source per nearby module, maximum three enabled; distant/inactive module sources disabled. Recommended R35 work, distinct from mandatory R29–R34.
- Player/street local lights cull Default plus Player/Obstacle/Pickup (mask1793); road/render children intentionally retain existing Default layer. UniversalAdditionalLightData is authored on light prefabs rather than left as scene instance overrides. No Light on strips/arches or in skyline.
- At runtime: **one moon + two headlights + one underglow + at most three enabled streetlights = at most seven enabled lights**, six additional. Road pools contain24 disabled authored streetlight components when prewarmed; that is not24 enabled lights.
- PC URP remains Forward/HDR with existing SSAO. Additional per-object pixel-light limit raised4→7 to accommodate the bounded lighting set. Global main/additional shadow capability settings are preserved, while all Task11 Light components deliberately use None. Mobile URP/settings are untouched. Camera HDR/post-processing enabled; Volume mask1 includes Default/GlobalVolume. Existing GlobalVolumeProfile retains **zero overrides**, so Bloom/exposure/tonemapping are not silently introduced. Fixed linear lighting and exposure apply to every comparison.

## Actual-speed mapping and visual evidence

u=InverseLerp(0,50,Snapshot.ActualSpeed), light=Lerp(.3,3,u), emission=Lerp(1,5,u). Negative/above-cap speeds clamp; intermediate12.5/25 values are tested through both configuration and the production Render0 path with cruise22; measured75 clamps at the cap. No curve tuning or cruise/score/time/bob substitute. Base cyan emission RGB=(.017800657,.395649016,.5); per-instance emission is that RGB times the multiplier. HDR alpha is irrelevant to these opaque materials.

| Executed state | Actual m/s | Light | Emission | Image under Evidence/Task11 |
| --- | ---: | ---: | ---: | --- |
| Initial run | 22 | 1.488 | 2.76 | near-start.png / near-start-4x3.png |
| Capped progression tick | 50 | 3 | 5 | cap.png / cap-4x3.png |
| Paused from cap | 0 | .3 | 1 | paused.png |
| Resume | 50 | 3 | 5 | resumed.png |
| GameOver | 0 | .3 | 1 | gameover.png |
| Restart | 22 | 1.488 | 2.76 | restart.png |

`visual-route.json` records29 real runtime camera renders with actual snapshots, state, light/emission/block values, viewport, poses, seams, street counts, horizon positions and sampled Editor statistics. Both1280×720 and960×720 use the saved Game camera/assets, not concepts/recolored mockups. The Editor-only fixture synchronizes committed root/HoldView before offscreen renders; it does not alter camera tuning. Near-start/cap use existing deterministic model-clock/Step seams outside Assets; production progression computes actual speed. No cheat input or alternate runtime scene was shipped.

Native ScreenCapture images are also provided: `native-open.png`, `native-tunnel.png` at1280×720, and `cap-tunnel-4x3.png` at960×720. The latter is intentionally Paused with actual0/light.3/emission1; the JSON's pre-pause actual50 is separately labeled. `visual-ticking.json` and advancing native frames establish live play. Open sky, entrance, tunnel, exit, post-exit and rebase appear in the matching named images; `cap-outer-1[/-4x3].png` and `cap-outer1[/-4x3].png` cover both outer lanes at actual50. All three120m lane centers remain within the measured useful viewport. No near-plane roof/car collision was observed.

Headlight on/off comparison: `near-start-headlights-off.png`/`cap-headlights-off.png` against matching on images. `accepted-hit.png`, `hit-paused.png`, `flash-expired.png` show accepted flash and independent changing neon. Native smoke images show frozen final-hit flash, Fuel/Lives terminals, reset and rebase. Player/hazard/ring/can shapes remain identifiable with Bloom disabled; cyan/magenta retain hue. Native sky/portal images are direct R29/R30 evidence; cones/readable road support R31/R32; local point+emissive mesh and measured state comparisons support R33/R34. **R28 is art progress, not completed presentation grading.** R35 is recommended work implemented here.

## Executed checks and commands

All paths below are Evidence/Task11. Final unique suite totals: **139 EditMode +159 PlayMode =298 passed**,0 failed/skipped/inconclusive (`test-summary.json`, per-case `*-results.json`). An additional saved-asset recheck passed all9 NightAsset cases, separately recorded rather than added twice to the unique total; the final full suite includes the stronger roof-clearance assertion.

- Curve endpoints/intermediates/clamping, production Step/ApplyState/Render0 transitions, pause with cruise50/actual0, resume/restart/Menu/disable, same-renderer flash+emission+unrelated field coexistence, final-hit freeze,20 repeated rebased lighting restarts and dark storage.
- Finite unit normals, positive scales, collider-free visuals, exact boxes/surfaces/endpoints, mesh fit/pickup bob clearance, two Spot references, three player lights, emission keyword and shared-material budgets.
- All previous camera/render-cadence/roll/bob/impulse/reset tests, pool/token/exhaustion tests,100km routes, safety/progression/survival/input/persistence tests reran. Existing1,000-seed safety/supply sweeps and the **≤180m reachable fuel guarantee** remain intact; gameplay configuration and generator code are unchanged.
- Both saved-scene native smoke modes passed: normal reloads and both domain/scene reload disabled. Each paused>10 wall seconds, collected17 coins/12 fuel on a natural2km cap route with3 lives, rebased twice, exercised accepted/rejected damage, both terminal causes, reset/Menu and intentional pool saturation.304 created/0 destroyed; natural-route traffic misses0; final global misses3 are the deliberate exhaustion checks. No runtime pool growth, inactive-light leak or immutable-config mutation. Original reload options restored.
- Preservation audit passes:278 protected baseline files byte-identical, all nine prefabs' gameplay components/root/Visual/CameraAnchor poses protected,19 existing scene owner/prefab components unchanged; no missing metas/duplicate GUIDs. Existing standalone persistence evidence remains byte-identical historical evidence. No logical bounds/colliders/seeded plans/input controls/GUIDs altered.

Executed via pinned CLI, explicit project selector and `--caller plugin --skill unity-cli`:

```bash
/home/alireza/.local/bin/unity.previous status --until-ready --project-path /home/alireza/NeonTunnelRider/NeonTunnelRunner --format json
# Schema discovery: command --query script/test/console/screenshot/stats/playmode; saved commands-*.json.
# Authoring and audits: command script run --file Docs/Setup/Task11Art.cs --entry NeonTunnelRunner.Task11Art.Configure
PYTHONDONTWRITEBYTECODE=1 python3 Docs/Setup/Task11Tests.py
PYTHONDONTWRITEBYTECODE=1 python3 Docs/Setup/Task11VisualRun.py
PYTHONDONTWRITEBYTECODE=1 python3 Docs/Setup/Task11PlaySessions.py
PYTHONDONTWRITEBYTECODE=1 python3 Docs/Setup/Task11AspectRun.py
PYTHONDONTWRITEBYTECODE=1 python3 Docs/Setup/Task11RestoreViewport.py
PYTHONDONTWRITEBYTECODE=1 python3 Docs/Setup/Task11Preservation.py
# Final: recompile; command script run Task11Audit.Inspect; editor status; console get/clear/get.
```

Resolved during implementation: URP removes `_EMISSION` when the material GI flags are None; saved emissive flags now preserve support. A new test initially used int for an ulong pool counter, then exact floating-point Color equality; both were fixed before the passing suites. External capture compilation required reflection for internal HoldPose and the installed UnityStats API. A final clearance audit raised only visual roof shoulders to retain the full-width>=8m envelope. An old solid camera background initially hid the assigned sky; saved Skybox clear mode fixes it. A CLI connection dropped during reload, then the old Editor exited and a new same-project Editor opened; readiness was re-established at the pinned version. No version/package replacement or evidence fabrication was used.

Final saved-scene/assembly/lighting audit, compilation and Console records are `audit-final.json`, `compile-final.json`, `console-before-clear.json`, `console-final.json`, `editor-final.json`; viewport restoration is recorded. Final compilation is up_to_date with **0 errors/0 warnings**. Console ground truth is **0 errors/0 warnings** both before and after clear; compiling=false/compilationFailed=false. Saved Game is clean/stopped, Instance clear, timeScale1, auto-tick off, original viewport/reload options restored. The ordinary PlayerWithoutTestAssemblies graph contains only NeonTunnelRunner.Runtime with InputSystem/uGUI/TMP references; no Pipeline/UnityEditor dependency. The final local checkpoint/tree/push status is reported with the task response.

## Task 12 API handoff

- Keep `VehicleVisuals` as light/motion/flash owner. Actual speed is immutable `GameManager.Session.Snapshot.ActualSpeed`; Paused/GameOver0, CruiseSpeed is the retained target. Use PresentationConfig's `UnderglowIntensity(actual)`/`UnderglowEmission(actual)` and existing explicit lifecycle; no second speed/input owner.
- All Lit materials have authored `_EMISSION` and HDR emission support; ReferenceMarking is shared across static track/canopy and dynamic underbody. Change the dynamic underbody only through its merged MPB. Flash owns only `_BaseColor` (fallback `_Color`); never clear a renderer block or restore cached emission on flash expiry/reset. Existing tests deliberately share the renderer to protect coexistence.
- PC URP HDR, camera HDR/post-processing and Volume mask1/Default are ready. GlobalVolumeProfile has zero overrides. Add integrated URP Volume Bloom and deliberate fixed tonemapping in Task12; preserve existing settings/SSAO. No PSv2/HDRP component/package. Current images prove Bloom-disabled readability; future Bloom has no acceptance evidence here.
- Player `Visual/EffectAnchors/ExhaustLeft`/Right local positions(−.62/.62,.08,−1.72), yaw180 face rear. They inherit mesh roll/bob and the player origin shift once. Stable headlights/point light remain under sibling Lights; do not let VFX ownership modify them or register either player child as an origin participant.
- Accepted `RunSession.Damaged(DamageEvent)` and `Collected(CollectedEvent)` are typed value hooks. Damage events already drive flash/camera once per accepted hit; grace rejection emits neither. Payload includes RunId and Encounter kind/token/sequence/lane/absolute/local position/playing time. Subscribe/unsubscribe once per prepared run, validate state/run/token and avoid retaining a pooled Interactable. Pickup returns immediately via existing deferred captured-token path; detached bursts need separate prewarmed VFX leases.
- Pool API: TryRent(SpawnContext,out PooledObject), capture ActivationId, Return/ReturnToPool(capturedToken). No VFX pool entries exist yet; add only actual Task12 effects/capacities, Stop/Clear all child particles/trails on return/reset, no StopAction.Destroy. Required road/fuel preflight and optional omission policy remain untouched.
- RoadManager exposes ActiveCount/GetLease/KeyAt and RoadSegment.Environment/AbsoluteStart. **No IsOpenSkyAt helper currently exists**: doc03's planned API is not implemented. At a queried local/absolute z, inspect the actual active interval and roof footprint: Open unroofed, Tunnel roofed, Entrance roofed local30–60, Exit roofed0–30. Open spans leave upper/lateral sky visible above the sparse skyline, verified at both aspects. Keep launch/burst above/lateral to the central120m decision horizon; no Task11 fireworks/timer exists.
- `WorldOrigin` has **no event**. Use `IWorldOriginParticipant.OriginRoot/ShiftOrigin(newOffset,delta)` for a new independent VFX owner and explicitly add a disjoint serialized participant. Existing participants are SpawnDirector and ChaseCamera. Player/lights/road scenery inherit their one parent shift; inactive pool storage is not shifted. Keep absolute-minus-offset subtraction in double before float conversion; local-space particles under a shifted VFX root avoid stretched trails/double translation.
- GameplayLoop owns reset/cleanup: clear input/contacts/registry, return rentals, reset origin/session/player, PrepareRun presenters, restore coverage/supply, then commit Playing and actual speed. VFX must integrate with that explicit transaction, freeze under state gates, and stop/clear on reset/Menu/shutdown. Do not change the camera/root geometry or repeat enable as a new run.

**NOT RUN:** Profiler,30-minute soak, a Task11 standalone build, rebuilt-player art/render validation, full HUD overlap and final delivery acceptance. Task09's three-launch standalone persistence **PASS remains historical evidence**, not relabeled unrun and not acceptance of these new materials. Full HUD/menus, audio and final delivery remain later work. No outstanding Task11 implementation/tooling blocker remains after the recorded final checks.

Task11 only. Next: **Task12 — Particles, Bloom and fireworks**.
