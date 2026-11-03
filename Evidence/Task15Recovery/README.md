# Task 15 recovery evidence and executable handoff

**BLOCKED. Task 16 ready: NO.** This continuation starts at local `6a102af74e1322d2018f2ee5d2088cc0b992428e`, with four exact offline asset/metadata repairs. The audited production input hashes are in `Static/static-audit.json`. The audit did not execute gameplay tests, player build, fairness seeds, 100 km runs, graphical profile or visible soak. A later external desktop session logged three native asset import completions; full reference/import acceptance remains BLOCKED.

The previous run remains in `Evidence/Task15/`. Its 365 Task14 passes are historical. The current nine Python scanner fixtures and offline Roslyn compilation are tooling checks and add **zero** Unity passes.

## Reference investigation and repair

`reference-dispositions.csv` records all twelve original pointers and one additional metadata pointer, including owning document/property, GUID/fileID, type, reachability, evidence, repair and remaining verification. `reference-investigation.json` retains original bytes, history, pinned source and the nine orphan documents' empty incoming-reference lists. `package-resource-proof.json` verifies current resource owners, package identities/hashes and the actual TMP archive contents.

- Seven `PC_Renderer.probeVolumeResources` pointers were removed. Contrary to the earlier report, these public serialized fields **still exist** in pinned URP `Runtime/Deprecated.cs:249–305`. They are obsolete, with no current runtime reads found outside that declaration. Their current owners, `ProbeVolumeDebugResources` and `ProbeVolumeRuntimeResources`, are already assigned to the real pinned assets in `UniversalRenderPipelineGlobalSettings.asset:440–473`. Those current resources and SSAO were retained. No guessed mesh subasset rebinding was performed.
- Nine unreferenced documents were removed from the template `DefaultVolumeProfile`: four absent scripts and five already-null scripts. All 19 entries of its actual `components` list and every retained document are byte-identical. The four missing scripts belong to **OutlineVolumeComponent, TestAnimationCurveVolumeComponent, OasisFogVolumeComponent and TestVolume**. The earlier association with VolumeComponentSupportedEverywhere was incorrect; that document had a null script. The profile itself is reachable through URP global settings; its removed subassets were not listed components and had no serialized incoming pointers, including from the protected scene.
- TMP's optional default EmojiOne pointer was cleared. Original TMP Settings exactly match the asset shipped in pinned uGUI 2.6.0's Essential Resources archive, which contains the pointer but no EmojiOne asset. Conversion data establishes its identity; neither Git history nor resolved sources contain the asset to restore. The game has no sprite tags, `TMP_SpriteAsset` or default-sprite API usage, and its saved HUD labels use ordinary glyphs. Required fonts, materials, styles, linebreak assets and emoji-support settings were retained. Current glyph/render verification remains blocked.
- The expanded scan also found `Readme.cs.meta:MonoImporter.icon` pointing to absent texture `a186f8a87ca4f4d3aa864638ad5dfb65`, fileID 2800000. This optional tutorial editor icon is outside the conservative production graph. Only the icon pointer was cleared; the script GUID, default references and script were retained.

All repairs are **offline preparations**, pending import. Exact four-file diffs/hashes are in `repairs.json`. Backup bytes are outside Assets at `/tmp/neon-task15-recovery-before`; Git retains the authoritative originals. No recovery scene was edited or staged.

| Scope | Missing GUID pointers before → after | Remaining imported/native IDs | Remaining built-in IDs |
| --- | --- | --- | --- |
| Whole working tree, including recovery | 13 → 0 | 246 | 46 |
| Tracked production inputs | 13 → 0 | 235 | 37 |
| Conservative Game/build/Resources/settings graph | 12 → 0 | 231 | 31 |
| Protected recovery file | 0 → 0 | 11 | 9 |

Counts are pointer occurrences, with one distinct GUID per original finding. Whole-tree inventory remains nonzero: **292 imported/native/built-in pointer checks await Editor confirmation**, plus script IDs checked by convention rather than current import. This is a **BLOCKED reference gate**, not a clean-import PASS. Text subasset anchors are checked; a matching GUID with an invalid text fileID fails.

The scanner uses resolved package identities/versions, supports embedded/local packages, rejects dangling metadata, and scans importer metadata as well as serialized assets and project settings. Package `Samples~`/`Documentation~` contents are not imported package assets; copying a sample into Assets brings it into the normal inventory. The initial scanner-development output is retained in `scanner-initial-diagnostics.json`: its four apparent invalid prefab handles were a scanner defect, not broken scenes. Unity documents that [Prefab Asset Handle fileID 100100000 is created during import and absent from YAML](https://unity.com/blog/engine-platform/understanding-unitys-serialization-language-yaml). Only that exact property/class/type/target combination is pending import. [UnityDataTools documents the exact two engine container GUIDs](https://github.com/Unity-Technologies/UnityDataTools/blob/main/Documentation/textdumper.md); their individual IDs remain pending, including arbitrary invalid IDs. No zero-prefix exclusion remains.

The conservative file graph includes orphan/inactive serialized data, all Resources, build profiles and ProjectSettings. It does not prove Unity build stripping or dynamically loaded package dependencies. `isolated-tracked-snapshot.json` documents a local clone at `/tmp/neon-task15-recovery-tracked-6a102af`, the four overlays, and exact equality to current tracked production bytes. Recovery files are absent from that isolated copy. Package lookup used the original project's resolved packages read-only. No isolated import or remote-access PASS is claimed.

## Bounded capability diagnosis

`cli-preflight.json` records actual beta.13 syntax/version and status exit 6 (`STATUS_NO_INSTANCES`). `capability-diagnostics.json` separately records:

| Capability | Executed diagnostic | Outcome / required next action |
| --- | --- | --- |
| License | Pinned `license status --format json` | Exit 6, `LICENSING_CLIENT_UNAVAILABLE`. This does not prove an invalid license. Prior normal desktop log shows initialized Unity Personal entitlement. Open the exact Editor through the normal desktop/Hub; activate through Hub only if it actually requests activation. |
| Pipeline / project | Pinned `pipeline list --format json` | Exit 0 inventory lists this exact project with Pipeline 0.9.1-exp.1, but zero reachable servers. Its `isRunning:true` contradicts the user's closed-Editor confirmation and logged shutdown; treat it as stale/discovery information, not live readiness. No endpoint descriptor currently exists. No Safe Mode compile error is reported. |
| Desktop / graphics | Current DISPLAY :0; X11 socket; `glxinfo -B` | Socket EPERM; glxinfo exit 255. No GPU device paths available in this shell. Use the actual desktop session; run the blocked command from its normal terminal or permit the specific required local IPC/device access. Do not guess another display or substitute software rendering for performance. |
| Audio | Actual session PulseAudio socket; `pactl info` / sink list | Socket EPERM; both commands exit 1. No sound devices exposed. Verify a physical audible sink/listening and loopback capture in the desktop before audio acceptance. A dummy sink is insufficient. |

The user confirmed **Unity is closed**. `session-log-observations.json` preserves hashes and selected lines of the prior desktop logs, including Pipeline startup then shutdown. They predate these repairs and do not establish current compilation or Console cleanliness. Existing previous owned graphical/batch attempts already failed licensing IPC/display. No competing Editor, repeat unchanged launch, process termination, package reinstall, version change, license bypass or external upload was attempted. There was no demonstrated invocation defect to repair. OS/glibc, Editor, plugin and package identities are recorded in `static-validation.json`.

## Resume in the normal desktop session

Use this exact project and Editor 6000.6.3f1. Preserve the user's recovery data and unsaved work. Open saved **Game**, allow import/compile to complete, inspect current Console without erasing prior logs, and select the existing LinuxDevelopment profile. Reimport **only the four repaired files** through the Project window if automatic import did not run. Verify the renderer's current global probe resources, its existing SSAO feature, all 19 default Volume components, TMP normal glyphs and default-font/style assignments. Run **Neon Tunnel Runner → Validate Project Setup**. Inspect imported subasset IDs via AssetDatabase for every pending entry in `references-after.json`, including the FBX mesh, engine resources, importer-created Prefab handles and all material/mixer group IDs. Record actual type, GUID and local fileID; do not resolve them by name alone.

First run the small EditMode test `NeonTunnelRunner.SweptBoundsTests.InitialOverlapAndEqualMotionHaveTimeZero` in **Window → General → Test Runner**, wait for completion and export native XML. Launch Game graphically, verify multiple advancing frames and an audible real output. Save logs/results before starting long jobs. A startup/import log alone is insufficient.

From the repository root in the same normal desktop terminal:

```bash
/home/alireza/.local/bin/unity.previous status --until-ready --timeout 30 --project-path "$PWD" --format json
python3 -B Docs/Setup/test_task15_references.py
python3 -B Preproduction/tools/audit_package.py
python3 -B Docs/Setup/Task15StaticAudit.py
python3 -B Docs/Setup/Task15Run.py scene
python3 -B Docs/Setup/Task15Run.py tests
python3 -B Docs/Setup/Task15Run.py fairness
python3 -B Docs/Setup/Task15Run.py smoke
python3 -B Docs/Setup/Task15Run.py build
```

Static audit returns exit 2 while import verification is pending. Its default output is a new recovery Static directory. Task15Run creates a new timestamped run directory, polls suites to completion and preserves actual per-case results and derived XML; export native Test Runner XML separately. Check every exit/result. A failed prerequisite is a stop for dependent jobs, not a reason to repeat the whole command list. Fairness must retain 1,000 seeds × 10 km at start/mid/cap, full moving envelopes, reachable fuel gaps ≤180 m and failure/replay data. Full suites include both established 100 km regressions; retain current seam/coverage/rebase/survival/bounded-count data.

If Pipeline alone still fails while this Editor is licensed and usable, use its Test Runner UI for the small test and both suites, exporting XML; the saved validator menu covers Game wiring. Use the same pinned Editor's normal Build Profiles workflow for a new Development build. The supported functional batch fallback is available **only on the isolated copy while no Editor uses that copy**:

```bash
/home/alireza/Unity/Hub/Editor/6000.6.3f1/Editor/Unity -batchmode -nographics -projectPath /tmp/neon-task15-recovery-tracked-6a102af -executeMethod NeonTunnelRunner.ProjectSetupValidator.ValidateBatch -logFile /tmp/task15-recovery-import.log
```

Do not run that command from the restricted shell while its unchanged IPC prerequisites fail. The validator's batch entry exits with an explicit success/failure code. Copy its log/results to a new evidence run and record the isolated input manifest. It does not replace graphical acceptance, all imported subasset checks, fairness, suites or build. For the isolated fairness helper when Pipeline is unavailable, copy the unchanged `Docs/Setup/Task15Fairness.cs` **only into that isolated copy's** `Assets/Editor`, adapt its Output constant to a new evidence directory, and invoke `NeonTunnelRunner.Task15Fairness.Run()` from an Editor harness. Record that extra audit input and restore nothing into the user's Assets. This fallback has not executed here.

Exercise current pointer/keyboard menus, ten-second pause, 50 resets/reload configurations, natural Lives/Fuel endings and tie/rescue tests, Quit/relaunch persistence, four HUD viewports, ≥120 m framing, camera/rebase, all VFX/Bloom and five audio categories at the approved gain. Keep production preferences intact; synthetic fixtures use isolated keys. Reproduce/classify the four historical URP messages independently in the current Editor/player, preserving exact counts/logs and required Bloom/firework render impact. Existing source classification is evidence, not current harmlessness.

Then follow the full protocol in `Docs/Verification/15-integration.md`: current hashed standalone, actual 1080p graphics, recorded hardware/driver/API/settings/focus, normal max difficulty after at least 180 playing seconds of warmup; **600 seconds of primary profiling**, then a **separate 1,800-second visible soak**. Keep fuel drain, moving hazards, required pickups, HUD, effects and approved mix active. Use ordinary lane actions for legitimate steering. Retain raw frame samples/captures, timestamped timeline, CPU/GPU data, script GC attribution, consistent postwarmup RSS baseline/end/peak/trend and pool/source/VFX counters. Targets remain p95 ≤16.7 ms, zero steady-state gameplay-script GC.Alloc, bounded counts and memory within 10%. Failed/partial captures stay evidence. No current warmup or valid sample exists yet.

`validation-cases.csv` preserves all 58 V01–V25 subchecks with current verdict/method/revision/time and prior verdicts. The canonical requirement register preserves all 60 R/U rows and mandatory/recommended distinctions, with a byte-exact prior-register snapshot here. `gate-status.json` carries explicit absent/current versus historical counts. Delivery-only archive/report/upload acceptance remains NOT RUN for Task 16.

## Later external session observation

At final checking, a new desktop session/descriptor appeared and the ignored user layout changed. `desktop-import-observation.json` and its numbered excerpt retain three completed NativeFormatImporter records for the repaired PC renderer, default Volume profile and TMP settings; Readme has a QuickSearch observation only. One justified new status probe still exited 6 with STATUS_PIPELINE_LOAD_PENDING. This supersedes initial closed-session/absent-descriptor observations. It does not supply imported fileID, test, current Console, controlled frame/audio or profile/soak acceptance. The audit made no new asset edits or Editor/player launches. `concurrent-user-layout-change.json` records the layout mismatch; current bytes were preserved without restoration or staging. All recovery files and approved production assets outside the four repairs still match their initial hashes.
