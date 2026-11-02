# Task 13 — Music, engine and interaction audio

Task 13 only. Inspected the clean inner Unity/Git repository at `5f8c84b`, Environment, Task12's API/reset/event/coordinate handoffs, relevant earlier survival/pickup/state/persistence notes, original Task13/14 prompts, requirements, architecture, project structure, asset plan, acceptance and environment setup. No applicable AGENTS.md. All pinned packages, existing GUIDs, New Input System maps and the supplied ignore file are preserved. No push, middleware, runtime synthesis, gameplay RNG use, new input/state system or Task14 UI.

Environment: Unity 6000.6.3f1; `/home/alireza/.local/bin/unity.previous` 1.0.0-beta.13; official Unity plugin 0.1.8-beta; Pipeline 0.9.1-exp.1; URP/Core 17.6.0; Input 1.20.0; Test Framework 1.8.0/NUnit 2.1.0; Mint Virginia/glibc 2.35. PATH beta.14 was discovered but not used for task automation. The pinned Editor was initially closed and was opened through the pinned CLI. Command syntax was discovered from that Editor's catalog/schemas. The Unity CLI/audio skills were read. Mixer structure was authored through Unity's Audio Mixer UI; its existing final group was renamed through its public Object name property. No mixer YAML authoring or private mixer-creation API was used.

Implementation and assignments

- `Scripts/Audio/AudioController.cs`: one scene-owned observer, explicit playback ownership and DSP-time completion; no singleton, coroutine, delayed cue queue, source growth, PlayOneShot stacking or PlayClipAtPoint. `Scripts/Config/AudioConfig.cs` and `Configs/Audio.asset`: immutable clip/group/level and speed authoring. `GameplayLoop` adds initialize/prepare/cleanup/state/shutdown integration. `ProjectSetupValidator` requires exactly one controller, ten sources and explicit valid assignments. Existing isolated gameplay fixtures may omit audio; the saved Game validator may not.
- Scene root **Audio** has AudioController, **Audio/Music**, and **Audio/SfxVoice0..7**. The existing **PlayerHovercar** prefab adds **Player/EngineAudio**; it inherits exactly one player origin shift. `GameplayLoop.audioController`, controller GameManager/config/two loops/eight sources, all five clips and all groups are explicitly assigned. The only listener remains **Presentation/MainCamera**. `Evidence/Task13/audit.json` contains the actual saved assignments and importer readback.
- `Audio/Mixers/RunnerMixer.mixer`: Master with three sibling groups **Music**, **Engine**, **SFX**; sources route to the corresponding group. All group/master gains remain Unity's default **0 dB**, pitch 100%, attenuation only, no sends/reverb/limiter/extra effects. Levels are deliberately controlled at sources, without multiplying hidden mixer attenuation. Saved sources start at volume0, Play On Awake off; Music/Engine Loop on, SFX Loop off. Initialization applies the chosen levels before normal playback.
- All ten sources are **2D**, centered pan, Doppler0; engine is attached to the logical player, independent of hover bob. No distance attenuation or 3D voice shift registry is needed. SFX sources stay under the neutral Audio root; events are consumed as values, with no pooled transform retained. Rebase does not alter engine pitch, reward positions or listener/camera ownership.

Original clips and import inventory

All audio is original project-authored composition/synthesis with Codex assistance. No external samples/recordings, paid assets, external audio URL or external license is asserted. `CREDITS.md`, `Docs/Audio/README.md`, and reproducible offline `Docs/Audio/synthesize.py` retain attribution and authoring information. No runtime generator is included. `Evidence/Task13/masters.json` records hashes, measured PCM peaks/RMS/DC and boundary differences. Every master is PCM16, **44,100 Hz**, peak .65; non-loop cues have tapered zero endpoints and zero-mean compensation. Loop joins are periodic musical/oscillator continuations, not concatenated silent placeholders. Auditory join quality remains a separate check.

| Clip | Duration/channels | Content | Unity import |
| --- | --- | --- | --- |
| VectorNight.wav | 64.00 s, stereo | Original 32 bars, 120 BPM, Am–F–C–G, four arrangement phrases, pad/bass/arp/lead and synthesized percussion; circular stereo delay | Streaming, Vorbis quality .8, preserve sample rate, preload on, background load off, force mono off |
| HoverEngine.wav | 2.00 s, mono | Periodic 55 Hz harmonic hover motor, 2 Hz phase modulation and 3/8 Hz amplitude modulation | DecompressOnLoad, PCM, preserve sample rate, preload on, background load off, force mono on |
| Impact.wav | .34 s, mono | Shaped noise/metal/body impact | Same short-clip PCM settings |
| CoinChime.wav | .18 s, mono | Bright 1318.51/1975.53/2637.02 Hz chime | Same short-clip PCM settings |
| FuelCharge.wav | .34 s, mono | Lower rising 220–440 Hz body, 660 Hz overtone | Same short-clip PCM settings |

No deliberate duration deviation from the starting production targets. Engine stays PCM to avoid repeated codec padding. Music streaming avoids retaining the full 64-second decoded stereo buffer; short cues preload for immediate delivery. In this Editor, `preloadAudioData` is part of AudioImporterSampleSettings, not the obsolete AudioImporter property. Actual importer/channel/load support is checked by EditMode tests and the saved audit, rather than inferred from WAV headers alone.

Mapping, state and bounded voice policy

`u = Clamp01(Snapshot.ActualSpeed / 50)`, `pitch = Lerp(.8, 1.5, u)`, `engine volume = Lerp(.05, .075, u)`. Actual22 gives **pitch1.108 / volume.061**; actual50 gives **1.5 / .075**; actual0 gives .8/.05, with explicit pause/stop providing silence outside active simulation. Tick snapshots update pitch/level only; the loop starts once per run and does not restart on snapshots/resume. CruiseSpeed, score, Transform differences and visual bob are never the audio input.

| State/command | Music source volume / behavior | Engine | Voices |
| --- | --- | --- | --- |
| Menu | .16, continuing single loop | Stopped | Cleared |
| Playing | .24, same loop/playhead | Start once per run, measured-speed pitch | Bounded requests |
| Paused | .08, same continuing loop | Explicit Pause, playhead retained | Explicit Pause, busy ownership retained |
| Resume | .24 without Play restart | UnPause once | UnPause existing generations, no redispatch |
| GameOver | .14, same continuing loop | Stop | Already accepted terminal cues finish in DSP time; new/stale requests rejected |
| Restart / return to Menu | Preserve music ownership/playhead | Stop/reset, restart only after Playing commit | Stop, clear clip/volume/pitch/ownership/remaining time before new run |
| Focus loss / application pause | Explicit Pause | Explicit Pause | Explicit Pause with retained ownership |
| Focus return | Resume for current GM state | Resume only if current state is Playing | Remain paused if GM is Paused; never command GM.Resume |
| Disable/shutdown | Stop/unsubscribe | Stop | Stop/clear/unsubscribe |

Focus behavior extends the existing `runInBackground=false` policy and never changes GM state, input policy or simulation. It does not auto-resume a paused or terminal run. Fixture captures may temporarily run in the background and invoke focus callbacks explicitly; that is not a saved shipping setting.

Collision/coin volumes .14, fuel .12, pitch1. The music/engine/SFX frequency choices and conservative gains leave arithmetic headroom: at master peaks .65, the sum of eight worst-gain voices plus music and capped engine is at most **.93275** before codec/resampling effects. This estimate is not a listening or mixed-output acceptance result. Saturated digital output and listening status are recorded below separately.

Each of eight voices owns one AudioSource.clip and one monotonically increasing generation. Use lowest-index free voice first. When full, a reward drops immediately; collision steals the oldest reward first, then the oldest collision if all eight are collisions. No allocation fallback, stale queue or gameplay/VFX gate. Requests/category counts, active/peak, dropped/stolen, music/engine starts are read-only diagnostics. Busy state never comes from `isPlaying`. Pausing stores remaining DSP duration and never frees a voice. Completion checks busy/paused/generation/deadline; reset clears handles and a stale completion cannot affect a newer clip. Completion continues in Update at timeScale0 for valid terminal cues.

Existing `Session.Damaged` and typed `Collected` subscriptions are attached once per prepared run; run IDs and Playing/transition guards reject stale data. Grace-rejected contacts emit no Damaged event. A full-fuel event with AppliedValue0 still requests one fuel cue. Requests occur synchronously during accepted event dispatch, before the coherent terminal commit. GameOver stops engine without clearing those voices. No damage/reward/pool/physics/VFX scheduling mutation is performed by audio.

Executed verification

Automated final suite results: **348 passed = 153 EditMode + 195 PlayMode**, zero failed/skipped/inconclusive (`test-summary.json`, complete per-case `*-results.json`). This includes 28 new audio cases. Checks cover five imported assets/groups, deliberate missing clips/groups/sources, monotone bounded actual22/50 pitch, accepted/grace/full-fuel routing, saturation/drop/steal order, generation protection, explicit paused silence and retained playheads, terminal completion at frozen time, stale events, focus/application-pause state restoration, disable/re-enable subscriptions, rebase and 50 reset cycles with stable source identities. Existing state/input/pool/world/safety/survival/score/camera/VFX regressions are included. No audio source is added to the gameplay/VFX pool catalog.

Saved-scene, standalone, capture, final compilation and preservation results are appended after execution below. V18/R41–R44 audible acceptance is **not** implied by automated tests; the separate human review is recorded below.

Development issues: the first pinned recompile lost its connection during assembly reload; the same Editor returned ready. New tests were adapted from obsolete GetInstanceID to GetEntityId. An initial 192/193 PlayMode run exposed GM Awake preceding Audio OnEnable; bootstrap validation now checks authored enabled/active state. An initial saved-scene probe overlapped a deferred forced recompile, losing Pipeline and invalidating nonserializable presentation caches; that failed attempt is not acceptance. Play was stopped, imports settled and the saved scene reopened before retry. A mistaken audit during a running test session was rejected by EditorSceneManager; final audit ran stopped. No runtime VFX redesign was made for probe/recompile artifacts. Two pre-existing Task12 test BakeTrailsMesh deprecation warnings may occur on rebuilding the PlayMode test assembly; final results report their actual scope rather than treating them as new audio warnings.

Task 14 API handoff

Next: **Task 14 — Menus, HUD and UI integration**, with the recorded auditory review complete. Bind buttons to existing GM `StartRun`, `Pause`, `Resume`, `Restart`, `ReturnToMenu`, `Quit`; transition legality remains Menu→Start, Playing→Pause/End, Paused→Resume/Restart/Menu, GameOver→Restart. Do not invoke audio/VFX reset directly from UI or create a second AudioController/listener. Existing Editor lifecycle context commands remain until the Task14 integration removes temporary debug controls.

Project HUD values directly from `Session.Snapshot` and `SnapshotChanged`: ActualSpeed×3.6, Fuel/capacity, Lives, Score, BestScore; subscribe and immediately refresh on enable. State panels observe `StateChanged(previous,next)` and final `RunEnded(snapshot,reason)` after terminal commit. GM is the sole singleton. System/Pause Escape remains the only pause-toggle owner; UI Cancel is Backspace, with New Input System maps and InputSystemUIInputModule.

Reset transaction: clear input/contact queue → Audio.CleanupRun stops engine/voices and unsubscribes run observers → VFX.CleanupRun returns detached effects/stops exhaust → return gameplay pools → reset origin/roads/planner/session/player/survival/visual/camera → restore coverage and protected reachable fuel → prepare VFX and Audio observers → commit Playing/actual speed and publish snapshot. Music stays owned across run cleanup. GameOver keeps valid audio cues finishing in DSP time while VFX feedback freezes at its valid initial terminal state. Paused engine/SFX explicitly retain playheads; music continues reduced. Focus return never invokes Resume. UI must use the current GM state; do not replay pickup/damage sounds when refreshing panels/snapshots.

Existing MainCamera/AudioListener, ChaseCamera framing, Visual motion/lights, VFX/director/origin participants and 352 gameplay/VFX pooled instances retain ownership. Audio uses a separate fixed 2+8 source bank. No shipping harness/cheat inputs, full menu/HUD, settings screen, final delivery package or new persistence claim is included.

Saved-scene regressions: `Evidence/Task13/play-sessions.json` records **PASS in both established reload modes**, normal domain/scene reload and both disabled. Each retained exactly **10 source identities**, **1 listener**, **352 prewarmed gameplay/VFX instances**, **0 normal-play destruction**, **0 gameplay/VFX pool misses**; voice-bank stress separately recorded **peak8 / dropped1 / stolen1**. Each exercised 50 terminal restarts, resume without loop duplication, terminal cue completion, full saturation plus accepted damage/VFX, focus return while still Paused, and native capped-speed travel through tunnel/open/rebase. MusicStarts stayed1; EngineStarts54 corresponds to the explicitly requested runs. Pauses lasted **10.122 s / 10.159 s**. Both scheduled the first firework at **2.340 s**. All config hashes matched before/after. Editor auto-tick/reload settings were restored by the fixture; the production runInBackground setting remains false.

Current camera captures `normal-*` / `fast-*` cover 1280×720 and 960×720 at opening firework ignition, capped tunnel and rebase. Despite the filename `opening-burst`, these new shots catch the first child ignition, not the fully expanded radial burst; Task12's richer launch/ascent/burst/Bloom-on/off evidence remains historical evidence for that unchanged visual scope. Current 120 m projection checks passed at both aspects: y≈.6381, x≈.4888–.5112 (16:9) / .4851–.5149 (4:3), depth126.13. No camera, neon/Bloom, exhaust, firework or fuel-supply tuning was changed. Numeric PCM checks (`pcm-checks.json`) passed for nonzero data, <.66 peaks and near-zero DC; this is explicitly not auditory evidence.

Current development-player build and listening

`Evidence/Task13/build.json`: **Succeeded**, Linux x86_64/Mono Development, **145.905 s**, **207,937,504 bytes**, **0 errors / 0 warnings**. Output: ignored `Builds/Task13Audio/NeonTunnelRunner.x86_64`. This is the newly rebuilt current saved **Game**, all five current audio assets/mixer and current visuals, with a temporary one-build `NTR_AUDIO_VERIFY` fixture. `build-inputs.json` records the baseline commit plus SHA-256 for every production Assets/Packages/ProjectSettings input; `Standalone/build-identity.json` identifies the executable, UnityPlayer and actual runtime assembly. The fixture source is retained only in `Docs/Setup/Task13PlayerProbe.cs`; its temporary Assets copy/meta were removed after building, and no global scripting define/build-profile change was made. This is limited audio development verification, not final delivery, a new persistence test, profiling or a soak.

The build helper initially returned Unknown because fixture import had queued script compilation; that is saved as `development-build-compiling.json`, not a build PASS. Waiting outside Pipeline for fixture compilation/domain reload before invoking BuildPipeline resolved it. Build-generated URP/default-volume serialization is restored to its captured bytes. The preservation audit also detects Unity's incidental ReferenceRoad lightmap flag serialization; its original flag is restored before final checkpoint. No game/visual tuning is changed to obtain a build.

`Docs/Setup/Task13Capture.py` launched the actual rendered player with `-force-glcore`, 1280×720, without `-batchmode`, `-nographics` or `-noaudio`. FMOD ignored PULSE_SINK on the first attempt; that terminated/unrouted attempt is explicitly labeled `Standalone/Development-Unrouted/` and supplies no auditory PASS. The successful run identifies only the exact player process ID and moves its PulseAudio input to a temporary isolated sink; it never captures other desktop/media output, and unloads the sink on exit. The system default sink was unmuted/100%; production source mute flags were false. No other application's output routing/volume or system default was modified.

Successful capture: **2026-11-02 21:38:46–21:39:54 UTC** (Nov3 local), player exit0, **70.917 s stereo/48 kHz PCM**, peak **.420258**, RMS **.018438**, **0 clipped samples**. These are actual output values, not source flags/meters. `Standalone/timeline.jsonl`/`capture.json`/`routing.json` retain timestamps and conditions. Every checkpoint had **10 AudioSources /352 pooled gameplay-VFX instances /0 pool misses**. The 50-cycle standalone fixture intentionally invokes Start and Restart in each cycle; EngineStarts102 equals two initial runs plus100 requested runs, while MusicStarts stays1. Native voice completion at GameOver/timeScale0 is observed separately from frozen VFX/gameplay. The isolated fixture temporarily drives lifecycle and ticks/sets progression time for deterministic actual-speed measurements; this is not a shipping input path or full natural-route run.

`Standalone/interactions.wav` retains approximately the first24 seconds after output onset: Menu/music, actual22 engine, actual50 engine, coin, full-fuel, collision, eight-voice saturation with one drop/one steal, paused retained fuel, resume, focus interval, valid terminal collision, engine stop, terminal completion and restart/menu. `music-loop-join.wav` retains a five-second window around the imported streaming track's first loop boundary (including its one-second focus pause). `window-stats.json` gives supporting window measurements; the low-frequency engine peak shifts ≈**60.95→82.38 Hz**, consistent with the authored55 Hz core ×1.108/1.5. Saturated peak is .420258. `player-1280x720.png` is the actual current rendered player.

**Human auditory review PASS, reported by the project user in this task conversation:** “All listed checks sound good.” The review request explicitly covered music, low/high actual-speed engine, collision, distinct coin/fuel, balance, pause/resume and music loop join in these current standalone captures. The exact response/scope is retained in `Standalone/listening.json`. This is user-observed listening evidence; Codex does not claim an independent auditory observation. The captured saturation/terminal/restart sequence and automated state/source checks supply the complementary lifecycle evidence. No audible acceptance is inferred solely from nonzero samples, source flags or mixer meters.

| Requirement | Current evidence |
| --- | --- |
| R41 | Original64 s composition, imported/routed current player loop, no duplicate music across resets, recorded join and user listening PASS |
| R42 | Player-attached mono loop, actual22/50 pitch1.108/1.5, native output frequency shift, explicit pause/stop/resume and user listening PASS |
| R43 | Accepted-damage only, grace rejection tests, current collision/terminal capture, terminal completion and user collision listening PASS |
| R44 | Typed coin/full-fuel events exactly once, distinct clips/captured cues, user distinction/balance listening PASS |
| R23 / V12 / V13 / V18 | Fixed2+8 sources, explicit ownership/generation guards, no runtime source/clip creation, automated saturation/50 resets, both reload modes, native current-player capture and scoped human review |

Commands actually executed use the pinned CLI with explicit project selection and `--caller plugin --skill unity-cli` (initial mixer routing/menu invocation labeled audio-setup-mixers). Python helpers run with `PYTHONDONTWRITEBYTECODE=1`:

```bash
/home/alireza/.local/bin/unity.previous open /home/alireza/NeonTunnelRider/NeonTunnelRunner --editor-version 6000.6.3f1 --format json
# status / command query and script schemas; Editor menu/UI mixer authoring
python3 Docs/Audio/synthesize.py
python3 Docs/Setup/Task13Tools.py
python3 Docs/Setup/Task13Tests.py
python3 Docs/Setup/Task13PlaySessions.py
python3 Docs/Setup/Task13Build.py
python3 Docs/Setup/Task13Capture.py
python3 Docs/Setup/Task13Preservation.py
# script run Task13Audit.Inspect; recompile; console get/clear/get; final saved-scene/owner check
```

Remaining scope: Profiler/GC.Alloc/frame-time acceptance and the **30-minute soak are NOT RUN**. Task09's historical standalone persistence PASS retains only its recorded scope. Full menus/HUD, final target/packaging/delivery, student identity/repository-link/upload obligations remain later tasks. The audio capture fixture substitutes for pending Task14 controls; no final menu/pointer/keyboard UI or manual native OS-focus interaction acceptance is claimed. Task13 auditory review is no longer blocked after the user's recorded listening response.


Final audit and checkpoint readiness

Final strict compile is **up_to_date, 0 errors /0 warnings** (`compile-final.json`). Final saved Game is stopped/clean, not compiling, scriptCompilationFailed=false, Instance clear, timeScale1 (`editor-final.json`, `audit-final.json`). Final Console is checked after clearing intentional negative-fixture diagnostics and beta.13 recompile's diagnostic about its obsolete internal `console_status` spelling; that helper diagnostic is retained as `development-console-recompile-helper.json`. Negative tests intentionally log invalid/missing pool/lease and duplicate-owner diagnostics while passing their expected-log assertions. Earlier Task12 BakeTrailsMesh warnings are retained in the pre-clear record; no Task12 test code was rewritten for this task.

The **player log separately contains four URP warnings for stripped/unsupported unused Gaussian DoF (twice), Bokeh DoF and Panini passes**, plus display-DPI notices. No AudioController exception was logged. Installed URP's PostProcess constructor creates these passes separately from Bloom; the saved Game does not use those effects. This is a nonblocking limitation of this limited player build's runtime log, not a claim of a warning-free player or complete standalone visual/package acceptance. Renderer/stripping/package settings remain pinned and unchanged.

Final preservation audit **PASS**: **389 protected files**, all existing prefab collider/Rigidbody/script/particle components and poses, **25 protected scene components**, original meta GUIDs/input/ignore/config/URP/settings are unchanged except the explicit audio additions/composition link. No missing meta or duplicate GUID. Supplied ignore SHA-256 remains `c735b75517ecf18563ce4bf269eb4fe158a5539f7d554823578a85eaec795f45`. `Standalone/final-input-comparison.json` has **zero differences** between the successful build's production inputs and final Assets/Packages/ProjectSettings; the temporary one-build fixture is separately hashed. Existing protected fuel/survival/visual contracts, including the ≤180 m reachable fuel guarantee, retain their implementation and regression coverage.

No outstanding Task13 audio implementation/listening blocker. Native OS-focus manual interaction and broad player rendering validation retain the explicit unrun/limited scope above. Profiler,30-minute soak and final delivery remain NOT RUN/pending. A local checkpoint is made after these recorded checks; no push.
