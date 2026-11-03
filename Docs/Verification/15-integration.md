# Task 15 — Integration, fairness and performance audit

**BLOCKED; no unconditional Task 16 release recommendation.** Audited local source `2923f56b79c6cba4b24627fee4bde2e902d872ab` on **2026-11-03, Asia/Tehran (+03:30)**. The initial tree was clean. This checkpoint adds audit tooling/evidence and fixes one reproduced documentation-tool failure; it changes no gameplay, scene, prefab, configuration, package or mixer asset. Current Unity execution and the outstanding real-time checks could not be completed in this managed shell. A strict serialized-reference scan also **FAILS** with 12 unresolved GUID references, classified below. No critical gameplay failure was demonstrated by a current execution, and absence of such an execution is not a functional PASS.

Full inventory: [canonical R01–R54 / U01–U06 register](../../Evidence/requirement-evidence.csv), [58 V01–V25 subchecks](../../Evidence/Task15/validation-cases.csv), [machine-readable summary](../../Evidence/Task15/audit-summary.json). Register `status` preserves the original mandatory/recommended distinction; `result` is this audit's verdict. **R35 remains recommended.** Four scoped source/package criteria pass, 49 requirement/constraint rows are blocked and seven delivery rows remain NOT RUN. The failed V22 reference subcheck is separate from R52's passing byte-identical ignore criterion. Historical PASS artifacts remain explicitly historical.

## Inspection, identities and preserved state

Read the repository status/history, Environment, Task14 including API/evidence/audio handoff, the actual runtime/tests/configs/saved scene/UI/player/road/VFX/audio assets, earlier relevant verification records, original prompts15/16, requirements/architecture/structure/acceptance/submission documents, requirements/validation JSON and environment plan. No AGENTS.md was found. Used the installed official Unity CLI skill; no subagents, publishing, push, course upload or final ZIP.

Pins verified or retained: Editor **6000.6.3f1 /45d8eee7de74**, pinned CLI **1.0.0-beta.13** at `/home/alireza/.local/bin/unity.previous`, official plugin **0.1.8-beta**, Pipeline **0.9.1-exp.1**, URP/Core **17.6.0**, Input **1.20.0**, Test Framework **1.8.0**, uGUI/TMP **2.6.0**, Mint21.3 Virginia/glibc **2.35**. The pre-existing default `unity` binary reports beta.14; audit automation uses beta.13. A read-only default-CLI update check failed DNS; no update was installed. Pipeline automation and URP rendering remain separate packages.

[static-audit.json](../../Evidence/Task15/static-audit.json) contains production input hashes, one input-set hash, saved component references, action bindings, configured pool entries, source call sites, installed dependency versions, ignore probes, historical build file hashes and prior-evidence hashes. Of **617** protected tracked Assets/Packages/ProjectSettings/Preproduction/ignore files, only `Preproduction/tools/audit_package.py` intentionally changed. Tracked production files and all prior GUIDs remain unchanged. Supplied ignore SHA-256: `c735b75517ecf18563ce4bf269eb4fe158a5539f7d554823578a85eaec795f45`; Library/Temp/Obj/Builds/Logs/UserSettings probes are ignored.

The user reopened Unity during this audit and confirmed the new untracked `Assets/_Recovery.meta` and `Assets/_Recovery/` are concurrent user changes. **Preserved; excluded from the Task15 checkpoint.** Their hashes are recorded separately in the production manifest. Do not treat them as an audit-authored scene or delete them during cleanup. The build scene list still selects Game only.

Approved audio change retained byte-for-byte: **RunnerMixer Master snapshot +4.552889dB**, mixer SHA-256 `8208f1e38bbf4cc91fd2cf4b8a17308f690989ab86ebc3a194b2ac8862c048b3`. Task13 listening/clipping results predate that gain and do not establish the present mix's headroom.

## Executed attempts and environmental blockers

[environment-preflight.json](../../Evidence/Task15/environment-preflight.json) records timezone, commands, exits, CPU/RAM, attempted Editor/player arguments and binary hash. The user initially confirmed Unity was closed. The exact pinned graphical Editor was attempted and exited **1**: licensing IPC failed with `0x80000000`, then `Error opening default X display` / `cannot open display: :0`. Direct X11 socket connection returns **EPERM / Operation not permitted**. `/dev/dri` and `/dev/nvidia0` are unavailable; `glxinfo` cannot open the display, `nvidia-smi` exits9 and PulseAudio connection fails.

The historical development binary was attempted with `-force-glcore -screen-width 1920 -screen-height 1080 -screen-fullscreen 0`. Its actual log reports **llvmpipe (LLVM15.0.7,256bits), OpenGL4.5, Mesa23.2.1**, no native displays and **FMOD emulated software output with no sound**. This probe was not visible gameplay, a new Task15 binary, audible acceptance, a Quit test, a profile or a soak. It was still running at the12s prerequisite observation; no application-Quit exit code is asserted. Raw [player-launch.log](../../Evidence/Task15/player-launch.log) and stdio are retained, including these new environmental errors; they are not merged into the historical four-URP-warning count.

After the user reopened Unity, beta.13 detects the project as open but reports **STATUS_PIPELINE_LOAD_PENDING**; a15s ready wait still returned exit6, and X11 socket EPERM persists. See [reopened-editor-status.json](../../Evidence/Task15/reopened-editor-status.json) and [ready-wait](../../Evidence/Task15/reopened-editor-ready-wait.json). No package reinstall, blind scene edit, different Editor, external bug upload or sandbox bypass was used. Current Console/compilation status is unavailable; historical zero warnings is not substituted.

Both assembly test-run launches returned exit6 before Test Runner started: [test-attempts.json](../../Evidence/Task15/test-attempts.json). The new isolated runner's `tests`, `fairness`, `scene`, `build` and `smoke` attempts each retain their own timestamped input/working-tree manifest and blocked Editor status under [Runs](../../Evidence/Task15/Runs). **Current executed tests:0.** Current failure/skip/inconclusive totals are unavailable because neither suite ran; they are not reported as zero successful failures/skips.

A local independent clone of `2923f56` succeeded at `/tmp/neon-task15-import-2923f56`. Its pinned batchmode/no-graphics import also failed licensing IPC, was stopped by the harness after25s and exited **−15**. [fresh-import-attempt.json](../../Evidence/Task15/fresh-import-attempt.json) and raw log preserve the failure. This was an attempted functional import, not a graphics/performance substitute. It preceded the user's reopening; no second Editor is now launched against the live checkout. The temporary clone is not durable evidence or proof of remote grader access; the retained attempt/log are the evidence.

## Demonstrated fix and new isolated audit helpers

| Reproducer | Change | Executed result |
| --- | --- | --- |
|`python3 -B Preproduction/tools/audit_package.py` raised FileNotFoundError for `Preproduction/.gitignore`|If the standalone-package ignore is absent, resolve the exact supplied ignore at the enclosing Unity repository root; neither ignore file is edited|**PASS**:54 requirements,25 validation cases,16 prompts,12 concept references,0 package errors. [Before](../../Evidence/Task15/package-audit-before-fix.json), [after](../../Evidence/Task15/preproduction-package-audit.json). This is a package audit, not Unity tests.|

No game behavior was changed without a demonstrated runtime failure and affected rerun. In particular, the HUD allocation sites and unresolved optional/obsolete asset references are carried as findings; no speculative optimization or blanket asset regeneration was made while Unity verification is blocked.

- [Task15StaticAudit.py](../Setup/Task15StaticAudit.py): repeatable read-only source/assets/hash/provenance inspection. Its aggregate result is FAIL while unresolved serialized GUIDs remain; preservation and other scoped checks keep separate verdicts.
- [Task15Fairness.cs](../Setup/Task15Fairness.cs): isolated Editor helper outside Assets. Loads actual immutable World/Gameplay assets, uses the actual planner/validators and moving-selection method, covers seeds0–999 over10km windows starting0/3000/6000m, checks full moving envelopes through conservative despawn, required sequential fuel gaps≤180m, all nine transitions, accepted pickups/traffic overlap, deterministic commitment, bounded storage, nonempty moving/stationary acceptance and deliberate unsafe moving proposals. Compares aggregate first-kilometre density bands, without requiring neighboring blocks to be monotone. Flushes incremental progress and records seed/context/block/slot/config hashes/difficulty/speed/reason/replay on failures. Window boundaries include surrounding blocks conservatively. Runtime row density is committed by absolute distance; different runtime speed labels do not redraw identical rows.
- [Task15Run.py](../Setup/Task15Run.py): uses installed Pipeline `test run` / `test status` and existing Task14 scene/build/smoke helpers, with a new evidence directory on each attempt. Preserves history. Tests retain per-case JSON and, after completed suites, derived JUnit XML with explicit origin/revision; it does not fabricate native Test Runner XML when unavailable. The build step targets `Builds/Task15Integration`, records all output hashes and preserves the existing build-cache snapshots plus ReferenceRoad material bytes. Smoke restores reload/background/device settings and its temporary viewports/auto-tick state. None enters normal startup or player assemblies.

The fairness helper compiled with the pinned bundled Roslyn compiler and cached Editor references, exit0/no diagnostics: [fairness-helper-compile.json](../../Evidence/Task15/fairness-helper-compile.json). This verifies helper syntax/API references only, **not Unity compilation or fairness execution**. Runtime branches of new helpers remain unverified until access is restored.

## Functional and ownership review

`GameManager` remains the only project gameplay singleton and scene lifecycle owner. `RunSession` owns resources, absolute distance/time, score/best and snapshots. Serialized dependencies stay private; configuration properties are read-only; no runtime config-writing route was introduced. Menu commands still implement Menu→Start, Playing→Pause/EndRun, Paused→Resume/Restart/Menu, GameOver→Restart and Menu→Quit, rejecting other transitions. No direct Playing restart or duplicate Escape owner was added.

Input remains action-only: Gameplay left/right bind A/D and arrows; System/Pause alone owns Escape in Playing/Paused; UI Cancel uses Backspace. One saved EventSystem/InputSystemUIInputModule is explicitly bound by InputRouter to its one cloned Runner asset. No legacy polling, direct Keyboard/Mouse/Gamepad polling, OnGUI/debug input or extra startup pilot was found anywhere in project Assets C# sources. The original template input asset remains an unused asset, not a second consumer.

Only GameplayLoop owns project FixedUpdate. Actual order inspected: reserve required fuel/road coverage before movement, consume buffered lane command, predict player/traffic, collect/sort/dispatch relative swept contacts, commit poses/movement/score, drain fuel, advance visuals/VFX, resolve one terminal result, then maintenance/rebase/publication if alive. Terminal EndRun occurs after the stepping guard clears. Lives wins a tie. Same-tick pickup/reward processing precedes fuel drain. Captured activation IDs and stable sequences guard pooled reuse; registry returns defer until dispatch ends.

Reset is one coordinated transaction: guard commands/tick and flush outgoing best, disable/clear input, sensor, voices/VFX, interactables and road leases; reset origin/session/planner/player/survival; establish initial road and required fuel; prepare camera/visuals/audio/VFX; publish Playing and enable actions. No presenter has an independent resource/reset path. Tests/fixtures use isolated preference stores/keys; this task did not DeleteAll, inject production scores or write5705. The current best was not read or replaced.

Pool authoring totals **352 =24 road +96 traffic +184 pickups +48 VFX**. Configured counts:8/8/4/4 roads;64 stationary,32 moving,160 coins,24 fuel;16 sparks,16 coin bursts,8 fuel bursts,8 fireworks. Saved source accounting is **9 scene-owned +1 inherited player engine =10 (2 loops +8 voices)**; the prefab engine reference is not counted twice. One listener is retained. Source shows bootstrap-only Instantiate, shutdown destruction and one-time duplicate-manager cleanup; no successful-rent fallback allocation. This is source/config evidence, not current created/active/peak/miss or source-identity trends. Required road/fuel preflight failure freezes/stops movement rather than allocating or leaving holes; optional traffic/coin/VFX misses are consumed safely.

Night/headlight/neon/Bloom and local-space exhaust/pooled effects remain wired. Neon and engine parameters use actual speed. Fireworks retain normal2–4s opening/8–14s later schedule, open-sky sightline checks, rocket ascent/burst and max2 visible rockets. Historical120m framing and visible burst remain history. Two retained Task14 frames were visually reviewed now: clear three lanes, headlight cones, underglow/exhaust/burst and readable corner HUD, including ten-digit fixture values at21:9. This is a subjective review of historical renders, with no numerical art-grade promise or new frame claim. [Historical media inventory](../../Evidence/Task15/historical-media-inventory.json) preserves actual paths/hashes, including available prior audio captures.

## Saved GUID findings — strict scan FAIL

No missing Assets metas or duplicate GUIDs were found. The raw serialized-reference scan nevertheless finds **12 unresolved references** against all Assets and installed PackageCache metas:

| File | Count / classification | Disposition |
| --- | --- | --- |
|`Assets/NeonTunnelRunner/Settings/URP/PC_Renderer.asset`|7 `probeVolumeResources` debug shaders/compute/mesh/texture fields. This serialized field is absent from the pinned current renderer-data class; obsolete-field interpretation is an inference from installed source.|No active missing project script identified. Live deserialization/import check remains blocked; no blanket clearing/regeneration.|
|`Assets/Settings/DefaultVolumeProfile.asset`|4 missing script GUIDs on OutlineVolumeComponent, TestAnimationCurveVolumeComponent, VolumeComponentSupportedEverywhere and TestVolume subassets. These and five null-script test subassets are **not referenced by the profile's components list**.|Orphan serialized data, not evidence that an active required Volume effect is missing. Strict raw-reference gate still fails; reconcile in Editor before declaring no missing GUIDs.|
|`Assets/TextMesh Pro/Resources/TMP Settings.asset`|1 default EmojiOne sprite asset reference (`c41005c129ba4d66911b75229fd70b45`), absent from imported Assets. Installed uGUI conversion data names that resource.|No required emoji use or current glyph defect demonstrated. Fresh import/reference resolution and any scoped repair remain pending.|

Exact GUIDs/file paths and orphan component membership are retained in static-audit.json. None was silently relabeled resolved. The final fresh-import gate and current player glyph/render checks remain blocked independently.

## Fairness, long distance and current test totals

**Current fairness: BLOCKED,0 seeds executed. Current100km regressions: BLOCKED.** No failing seed was discarded, no reseeding was attempted and no new seeded summary is labeled PASS. The expanded helper has not run. Existing Task14 per-case results were actually reopened: **153 EditMode +212 PlayMode =365 historical passes**,0 historical failures/skips/inconclusive. They include the1000-seed route/fuel/envelope tests, both100km cases, pool hot-path allocation microbenchmark, terminal ordering and50 UI restarts. This audit did not reproduce those totals.

Existing100km tests use real fuel drain/collection, actual stationary/moving traffic, normal contacts and repeated rebases, checking seams<0.001m,≥300m ahead/≥60m behind, local player z0..1000m, monotonic absolute distance, bounded road/planning/pool counts and actual survival. Their starting fuel/cap clock are explicit accelerated fixture setup; they are not a legitimate real-time profiling pilot. New execution and current numeric seam/rebase/counter summaries remain unavailable.

Fuel authoring still produces120/300/450m opportunities per480m block: sequential gaps **180/150/150m**. The existing general link validator allows220m but also requires the exact authored slots, so the actual schedule retains the stronger180m guarantee. Do not relax the new180m assertion. Moving envelopes use minimum player22 minus maximum traffic8m/s through despawn, expanded longitudinal bounds and reconstructed reservations outside the visible plan window. Actual statistical acceptance/rejections must be saved by the current run; old tests' speed labels alone do not prove three distinct fixed-density runs.

| V case | Current aggregate | Scope still requiring execution |
| --- | --- | --- |
|V01|BLOCKED|Editor compile/Console and valid standalone|
|V02|BLOCKED|Current action/UI behavior; static input/wiring passes|
|V03|BLOCKED|Lane commands/holds and60s continuous play|
|V04|BLOCKED|Command/duplicate/reload/teardown execution|
|V05|BLOCKED|Both100km runs plus separate visible soak|
|V06|BLOCKED|Rent/exhaust/reset/runtime allocations/counts|
|V07|BLOCKED|Reward, duplicate and activation-reuse execution|
|V08|BLOCKED|Current sweeps/moving cars; inheritance source passes|
|V09|BLOCKED|Damage/grace and visible flash/sparks|
|V10|BLOCKED|Both deaths, tie, rescue and final snapshot|
|V11|BLOCKED|Progression and current Quit/relaunch best|
|V12|BLOCKED|10s pause across simulation/input/audio/VFX|
|V13|BLOCKED|50UI resets and both reload configurations|
|V14|BLOCKED|Four current viewport/HUD sessions|
|V15|BLOCKED|Current120m framing and camera/rebase motion|
|V16|BLOCKED|Current night/headlights/actual-speed neon review|
|V17|BLOCKED|Current effects/Bloom/load and warning reproduction|
|V18|BLOCKED|Five audible cues, approved-gain clipping and lifecycle|
|V19|BLOCKED|Current native pointer/keyboard menus/Quit|
|V20|BLOCKED|10min profile, allocation/memory/count evidence,30min soak|
|V21|BLOCKED|Current1000×10km×3 actual-planner execution|
|V22|FAIL|12 raw unresolved references; import also blocked; remote access pending16|
|V23|NOT RUN|Final extracted player/repository acceptance — Task16|
|V24|NOT RUN|Final report/screenshots/credits acceptance — Task16|
|V25|NOT RUN|Identity, final ZIP/upload/re-download acceptance — Task16|

The detailed CSV splits methods, verdict-record timestamps, revision/build, observations and evidence for each subcheck, so source passes do not hide runtime/delivery gaps. Actual execution/attempt timestamps are retained inside the linked run manifests and raw-command records; a NOT RUN row's timestamp is not an execution claim.

## Standalone performance and visible soak

[performance-status.json](../../Evidence/Task15/performance-status.json) records the absence of valid measurements. **No10-minute profile or30-minute visible soak completed or started a valid gameplay window.** No raw Profiler capture, frame samples or memory time series exists for this task; no estimated completion/duration is substituted.

| Project target | Current valid measured result | Verdict |
| --- | --- | --- |
|1080p60fps, p95≤16.7ms|0 samples /0 valid seconds; p95/mean/CPU/GPU unavailable|BLOCKED|
|Zero recurring gameplay-script GC.Alloc, including HUD/audio/VFX|No current capture; HUD Render has changed-value score/best ToString and speed ToString+concatenation sites|BLOCKED; allocation risk open|
|No pool/source growth|Configured352 and2+8 verified; runtime created/active/peak/miss/identity series unavailable|BLOCKED|
|Memory stable within10% after warmup|No comparable baseline/end/peak/trend/managed/native/graphics/RSS samples|BLOCKED|
|Separate30min visible full-feature soak|0 valid visible gameplay seconds /1800 required|BLOCKED|

Current host CPU is **Intel i5-8250U,8 logical CPUs**; system RAM **16,653,312,000 bytes (15.51GiB)**. Historical environment records Intel UHD620 and NVIDIA930MX/580.178.04; these are not asserted as the used GPU of the invalid current llvmpipe probe. Declared attempted build: historical Linux x86_64/Mono Development, Game only, PC Forward URP, HDR, renderScale1, MSAA1, main shadows2048/4 cascades/50m, per-pixel additional lights limit7, SSAO retained, Bloom threshold1.15/intensity.28/scatter.55/clamp12, fixed exposure0/Neutral tonemapping. Saved VSync0; no project targetFrameRate override found; actual running quality/frame cap/focus and GPU frame timing remain unverified. Default runInBackground is false. Do not benchmark a stalled/unfocused/minimized player or software/no-graphics substitute.

Resume protocol: restore licensed IPC and real visible graphics/audio, build current development player, hash every output/input, record actual OS/driver/API/backend/quality/resolution/VSync/cap/tools/focus and seed/config. Warm up with normal gameplay until maximum difficulty/density (at least180 playing seconds), then capture a **600s** primary frame window with HUD/audio/required VFX active and deep profiling off. Retain individual frame samples; p95 uses sorted samples at `ceil(.95*n)-1`, with actual n/window recorded. Record CPU/GPU independently where available. Keep allocation call-stack diagnostics/deep profiling overhead separate from primary timing; observe recurring production HUD/audio/VFX costs without subtraction. The HUD ToString sites require attribution; no asset/feature/mixer reduction is pre-authorized by an unmeasured bottleneck.

Then separately log **1800s** visible real-time gameplay. A scoped development pilot may read the actual route/snapshot and send ordinary lane actions; keep real drain, contacts, required fuel, lives, threats, effects and audio enabled. Do not reuse the short Task14 elapsed-time route estimate as an unverified30-minute cap-speed pilot; do not teleport, grant resources or edit the clock. Record manual versus automated control. Check real particle/light/voice load and native visible/focus state throughout, with periodic screenshots and incremental logs. Exercise real damage/sparks/audio before consuming the final life; safe-route survival must collect real fuel.

Sample one named consistent memory metric, proposed **process RSS in MiB (bytes/1,048,576)**, with a postwarmup baseline and full trend/end/peak, not only final snapshot. Record managed/native/graphics separately if available. Compare the same metric to its same baseline and report sustained/peak movement relative to10%, retaining all samples. Poll pool TotalCreated/Destroyed/Active/Peak/Misses/per-key counters and10 source identities, audio active/peak voices/loop starts, VFX active/dropped/rocket counts, elapsed/distance/resources, road coverage/origin offset/rebases and errors. Retain failures/partial runs. If a measurement fails, capture the call site/bottleneck, make a small measured fix and rerun affected measurements; unavailable prerequisites remain BLOCKED. These budgets are project targets, not invented course marks.

## Four URP player messages and evidence integrity

[warning-review.json](../../Evidence/Task15/warning-review.json) re-reads and hashes the actual Task14 final Lives/Fuel player logs. Both historical launches contain these exact messages, on Intel/Mesa OpenGL Core as recorded by those logs:

- Twice: `Shader 'Hidden/Universal Render Pipeline/GaussianDepthOfField' is not supported or has been stripped from the build (in 'GaussianDepthOfField'). PostProcessing render passes will not execute.`
- Once: `Shader 'Hidden/Universal Render Pipeline/BokehDepthOfField' is not supported or has been stripped from the build (in 'BokehDepthOfField'). PostProcessing render passes will not execute.`
- Once: `Shader 'Hidden/Universal Render Pipeline/PaniniProjection' is not supported or has been stripped from the build (in 'PaniniProjection'). PostProcessing render passes will not execute.`

Installed URP `Runtime/PostProcessUtils.cs:19–29` emits that text when a **non-null shader has isSupported=false**, returning a null material. `Runtime/PostProcess.cs:56–64` creates these pass objects before activity checks. Gaussian constructor lines24–25 load two materials from the same shader, explaining the duplicate; Bokeh line26 and Panini line19 load once. Their RecordRenderGraph methods gate activity (Gaussian75/Bokeh65/Panini43). The saved game profile includes Bloom/Tonemapping/ColorAdjustments; default DOF mode is off and Panini distance is0. Bloom uses its separate pass. This supports an **inactive DOF/Panini pass** classification; unsupported-versus-stripped remains unresolved, and current required-feature render impact is **BLOCKED**, not assumed harmless from the wording. Historical neon/burst frames supply limited historical render support only.

Disposition: carry all four with logs/source context; no suppression, shader stripping-rule change, required rendering disablement or package upgrade. Obtain current build/GPU logs plus required Bloom/firework review before release. Historical SDL `Could not fetch DPI for display (0): Couldn't get DPI` occurs10 times per final Task14 log; current invalid-display probe has different SDL diagnostics and silent FMOD errors. Keep those environmental issues separate. **Historical Editor compile/Console0errors/0warnings is not current player warning-free evidence.**

The historical player is `Builds/Task14UI/NeonTunnelRunner.x86_64`; launcher hash `de3009bc2c3a8a7c25f347c0c0a098a1c49dbbb6ee6ec100562e61fb50cae261`, UnityPlayer.so `76ad72abeda8d274883e4a833cd9cd9f279c2668e708ea1a7321554e01536696`, runtime DLL `eb8c5bb39e28596fb62d854510cf24807ca11fefba1c655acd61c5115d5af17c`. The launcher alone is not the build identity; all available files are hashed in static-audit.json. Only project Runtime.dll is in that historical build's project Managed assemblies.

Its recorded build-input hash differs from current tracked source in **ReferenceRoad.mat only**: the exact recorded hash is reproduced by changing serialized `m_LightmapFlags` from current2 to6, consistent with Task14's documented generated flag restoration. No source bytes were changed to test this hash. Current untracked recovery files are additional inputs, outside the Game scene list. Therefore this binary is carried as history, not a fresh exact-current-source acceptance build; final visual equivalence of that metadata difference is not claimed. A fresh build must record both prebuild inputs and restored state.

## Reproduce / evidence index / Task16 handoff

From the Unity repository root, once a licensed connected Editor is accessible and Game is open/stopped:

```bash
/home/alireza/.local/bin/unity.previous status --until-ready --project-path "$PWD" --format json
python3 -B Docs/Setup/Task15StaticAudit.py
python3 -B Preproduction/tools/audit_package.py
python3 -B Docs/Setup/Task15Run.py scene
python3 -B Docs/Setup/Task15Run.py fairness
python3 -B Docs/Setup/Task15Run.py tests
python3 -B Docs/Setup/Task15Run.py smoke
python3 -B Docs/Setup/Task15Run.py build
```

Runner manifests retain source revision, working-tree changes, input hashes, timestamps and exits/results. Full suites must finish/poll, with actual failed/skipped/inconclusive totals and retained JSON/XML; rerun after any functional fix. Native current-player pointer/keyboard Quit, both natural terminal causes, relaunch persistence, five-category listening/post-gain clipping, effects and affected viewports remain separate acceptance steps, followed by the full-duration profile/soak protocol above. Use isolated preference keys for synthetic cases. A current player or native Profiler session is not produced by the commands merely compiling.

Evidence index: static source/asset/build/provenance hashes; environment/Editor/player/import raw attempts; individual Runs manifests; test-attempts; helper compiler result; package before/after; warning review; performance-status; historical-media inventory; full validation CSV; canonical requirement CSV; audit-summary. All are in [Evidence/Task15](../../Evidence/Task15). Existing actual Task14 test JSON, four viewport/reload sessions, native terminal/Quit/persistence logs and images stay at their original paths. No current raw Profiler/long-soak artifact is claimed or replaced by planning files.

Task16 handoff: audited gameplay source remains2923f56 with exact input hashes and approved mixer retained; current Task15 build **unavailable**. Successful current checks are scoped source/asset ownership/pins/ignore/counts, helper offline compilation and the fixed package audit. Remaining gates are current full suites/smoke/native process/audio/VFX/frame review, three-context fairness, both100km regressions,12-reference reconciliation/fresh import, four-message current warning/render disposition,600s profiling including HUD allocation attribution and1800s visible soak. Do not give an unconditional release recommendation until required functional checks PASS.

Installed modules are LinuxStandaloneSupport, WindowsStandaloneSupport and WebGLSupport. Current development target is Linux x86_64/Mono, LinuxDevelopment Development profile, Game only; final grader OS remains unknown. No Git remote is configured; no repository URL/access was invented or published. Student first name, last name, ID, actual collaborator identities and course access remain unresolved. Current CREDITS contains original mathematical audio/art and Unity/TMP/OFL provenance/source addresses; historical runtime images/audio are inventoried. Final report, final-revision screenshots/credits acceptance, repository-link text, final executable acceptance, exact `GD_Hw2_[firstName]_[lastName]_[StudentId].zip`, extraction/launch/CRC/hash, upload receipt and re-downloaded verification remain **Task16 / NOT RUN**. No final archive was created or uploaded.

Next task is **16 — Build, report and submission verification**, after these named Task15 release gates are resolved. This record stops at Task15 and does not claim that the blocked checks completed.
