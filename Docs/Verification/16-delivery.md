# Task 16 — Windows build, Persian report and submission verification

Student: **علیرضا پویان — Alireza Pooyan — 400414039**  
Course: **Computer Game Design — HW2**  
Preparation: 2026-11-07–08, Linux Mint; pinned Unity **6000.6.3f1 (45d8eee7de74)**.  
Tested release source: **`e97572aba25d8bcff7e336c4bf6ef8b6e3c540fc`**.  
Repository: **https://github.com/AZAX16/NeonTunnelRider.git**.

**Local preparation PASS. Native Windows functional acceptance BLOCKED. Remote release publication, course upload and post-upload download PENDING.** These are separate gates. This archive is locally prepared; it is not a course submission receipt or a claim that every original acceptance requirement passed.

## Deliverables

- `Build/Windows/NeonTunnelRunner.exe`, complete Unity Windows output: **196 files**, including `_Data`, `UnityPlayer.dll`, `MonoBleedingEdge/`, `D3D12/`, DirectStorage DLLs and crash handler. No emitted companion was reconstructed or omitted.
- `Docs/ImplementationReport.fa.md`: editable Persian source using the supplied report template, with all ten requested sections and accurate Task 15B behavior/limitations.
- `ImplementationReport.pdf`: **12 pages**, shaped RTL Persian, embedded DejaVu Sans/Bold/Mono, genuine runtime images and English identifiers. Final SHA-256: `cd35246f1c2c58a37e3f6065068a9ca1004a11a57aa5717c1509dda567776d0b`.
- `delivery/Screenshots/`: **16 genuine unedited Editor Play Mode screenshots**, 13 at 1920×1080 and three effect captures at 3840×2160. These are supplementary Linux Editor evidence, **not Windows standalone captures**. Manifest records source commit, dimensions, hashes and method evidence.
- `delivery/repository-link.txt`, `delivery/README-run.txt`, updated `CREDITS.md` with confirmed identity, AI disclosure, precise known sources and bundled font license notices.
- **`GD_Hw2_Alireza_Pooyan_400414039.zip`**, **52,944,086 bytes**, **218 file entries**, and its separate `.sha256` file.

Archive SHA-256:

```text
80a9ab2b2e2458d2e20a05bb08fcbca29f020b2b2683534c1d9dd2a3d9d199b5
```

Windows executable SHA-256:

```text
96b492cb271111251fe42b8646e65370a1b7b566773a1e35b34c3f2d1ae70873
```

## Source review and preservation

Initial branch `main`, HEAD `ecb192e9fd855c24ed67a05bffb0208548f98bc6`, 44 modified tracked files, **1,100 untracked files / 304,841,773 bytes**, nothing staged. No repository instruction file was found in the project or parent search. Read the requested preproduction documents, earlier verification notes, current code/configuration/prefab/scene diffs and credits before delivery edits. The user's explicit Windows requirement supersedes the older preproduction Linux default.

`Evidence/Task16/initial-status.txt`, `initial-working.diff`, `initial-staged.diff`, `initial-input-hashes.json`, `initial-untracked-inventory.json` and `initial-environment.json` preserve the starting checkpoint. Every untracked file has a hash, size and disposition: 10 production-source candidates, three protected recovery files, 20 recipes/docs and 1,067 historical evidence files. Existing historical evidence and recovery remain local and preserved; they were not indiscriminately added to Git or the submission ZIP.

The explicitly reviewed file list is `Evidence/Task16/reviewed-release-files.json`. Local source commit `e97572a` records the actual Task 15B implementation and its WAV/metas/tests, existing native-reference helper, relevant development recipes/notes and the new Editor-only builder. No push or publication occurred. Later delivery documentation/artifacts are a separate local documentation checkpoint; the tested game source remains the above release commit. No runtime change was made during Task 16.

`Evidence/Task16/preservation.json`: **PASS**. Every initial `Assets`, `Packages` and `ProjectSettings` file is byte-identical to the initial Task 16 input. Only `BuildDelivery.cs` and its native `.meta` were added. All 1,100 original untracked files remain present and byte-identical, even where explicitly reviewed source files became tracked. Asset GUIDs, earned best **27,316**, configuration, mixer, recovery scene and previous evidence are preserved. No reset/clean/destructive checkout, mass staging, package/system upgrade, preference deletion or stopped soak restart was performed.

## Windows release build — R53

Builder: `Assets/NeonTunnelRunner/Scripts/Editor/BuildDelivery.cs` in the existing Editor assembly. Windows Build Support was present in the installed Editor; license/build tooling worked. It validates the pinned version, supported target, saved scene, enabled scene list, output directory and `BuildReport.summary.result`; failures throw `BuildFailedException`. A nonempty output directory is rejected to avoid mixing stale files. Move the previous whole output aside before an intentional rebuild.

Actual build:

| Field | Observed value |
| --- | --- |
| Target / architecture | `StandaloneWindows64` / AMD64 x86_64 |
| Backend | `Mono2x` (Mono) |
| Scene | Sole enabled `Assets/NeonTunnelRunner/Scenes/Game.unity` |
| Scene GUID | `2ff1f62cd218111b6af6f6ff50581601` |
| Options | Requested `BuildOptions.DetailedBuildReport`; actual native numeric **536870912 / 0x20000000** |
| Development / profiler / script debugging / deep profiling | **false / false / false / false** in the actual BuildReport |
| Result | **Succeeded**, **0 errors**, **1 warning** |
| Duration / BuildReport size | **714.484671 seconds / 119,954,203 bytes** |
| Build time UTC | 2026-11-08 02:32:52–02:44:50 |
| Executable format | `PE32+ executable (GUI) x86-64, for MS Windows`; PE machine **0x8664** |

The one warning is retained verbatim: `Serialized file "Assets/TextMesh Pro/Fonts/LiberationSans.ttf.meta" contains a TrueTypeFontImporter object at version 2, below the supported minimum (4). Open and re-save the file to upgrade.` No font, package or importer upgrade was made to silence it. The native fresh import reports the same warning. The build is not reported as zero-warning.

This Editor's `BuildOptions.ToString()` expands overlapping legacy labels (including `Il2CPP`) for the requested flag. `build-options-native.json` records the actual numeric value and **Mono2x** backend; the emitted runtime is `MonoBleedingEdge`. The raw enum spelling in the original metadata is not a claim that IL2CPP was used.

Evidence: `Evidence/Task16/Build-20261108T0232525993600Z/build-metadata.json` (complete output hash/size inventory and step messages), `LastBuild.buildreport` (genuine Unity report), `windows-build-command.json`, `build-options-native.json`, `archive-verification.json` and `package-run.log`.

The builder snapshots/restores URP-generated authoring data and development/debug flags. BuildPlayer changed the Editor target to Windows; the original Linux development Editor target was restored afterward through the native target-switch command, without producing any Linux final player. The original `LinuxDevelopment` profile flag was restored in memory and explicitly saved through `AssetDatabase.SaveAssetIfDirty`; final input hashes match. No runtime scene, prefab or configuration assignment was changed to build.

## Fresh clone/import — V22 / R50–R52

**PASS, local clone of exact release source.** Fresh `git clone --no-hardlinks` into an isolated data-drive directory, with **no Library copied and no Library present before import**. Used the exact 6000.6.3f1 Editor in batchmode to import and run `ProjectSetupValidator.ValidateBatch`. No player was built in this step. Exit **0**, validator **PASS**, no C# compilation errors. Saved Game opens in a native preview and required scene/prefab/component/input/HUD/audio/URP/pool assignments validate. Required packages resolved at their pinned versions.

After import, every release `Assets`, `Packages` and `ProjectSettings` file matches the commit exactly. Only the folder-dependent VS Code solution setting and generated `.slnx` differ; neither is production input. Static GUID scanning of the imported clone found zero missing GUID pairs, duplicate GUIDs, missing metas or package identity errors. Static imported/builtin fileIDs are correctly left pending by the scanner; that scanner is not mislabeled as native acceptance. Native saved-scene validation and the separate working-tree native audit supply their stated scopes.

Evidence: `Evidence/Task16/clone.json` (isolated location and initial Library absence), `fresh-import-command.json`, full retained local `fresh-import.log`, `fresh-import-log-excerpt.txt`, `fresh-import-exit.json`, `fresh-import-summary.json`, `fresh-import-references.json`, `fresh-import-reference-summary.txt`.

The GitHub page is publicly reachable and `git ls-remote` succeeds, but returned **no refs**: the remote was empty when checked. `origin` now uses the student's supplied URL. This isolated clone came from the local release repository, **not from GitHub**. Remote/grader access to this release commit is **PENDING**. See `remote-verification.json`; no automatic push or permission change.

## Fresh Unity tests, references and runtime evidence

| Gate | Status | Actual scope / evidence |
| --- | --- | --- |
| Current EditMode suite | PASS | **161/161**, 0 failed/skipped/inconclusive; native XML and per-case JSON in `Evidence/Task16/Runs/20261107T225046+0330-tests/` |
| Current PlayMode suite | PASS | **250/250**, 0 failed/skipped/inconclusive; same fresh run, release source `e97572a` |
| Fresh test total | PASS | **411/411**, includes strict hold/pass cancellation, collision, audio/focus/pause, FOV, warnings, traffic, pools, input, survival, persistence fixture and 100 km regressions |
| Working-tree native references | PASS | **294/294**, 0 failed/unverified, no changed inputs; `Evidence/Task15Recovery/Native/20261108T0232448018500Z/native-pointers.json` |
| GUID scan | PASS | Zero missing GUIDs in whole/tracked/production/recovery scopes; `Evidence/Task16/references.json`; imported/builtin semantic checks above are separate |
| Saved scene and assignments | PASS | `Evidence/Task16/Runs/20261108T060216+0330-scene/` and final `20261108T062023+0330-scene/` |
| Near-miss real-frame fixture | PASS | Beside does not arm, early departure cancels, hit cancels and a correct hold/pass earns the full effect. Successful observed sequence **6.023178 s**; 352 pooled instances, 0 misses, 11 sources; `near-miss-capture-result.json` |
| Genuine screenshot capture | PASS | 16 PNGs, advancing real frames, controlled scenarios disclosed in `capture-result.json`, `effects-capture-result.json`, `near-miss-capture-result.json` and screenshot manifest |
| Final compilation/Console sample | PASS | `compilationFailed=false`, not compiling, native Console **0 errors / 1 warning**; historical Pipeline counters are retained and not claimed clean; `final-console.json` |
| V19 actual Windows Quit and menus | BLOCKED | No native Windows environment; student explicitly confirmed no Windows VM |
| Full Windows functional acceptance / cross-launch persistence | BLOCKED | No Windows process executed. Editor tests do not substitute for it |
| Final strict-rule visual check at every viewport | NOT RUN | Fresh captures cover the recorded dimensions; historical four-viewport checks retain their earlier scope |
| Full ten-minute CPU/GPU/frame/memory acceptance | NOT RUN | No fresh certification |
| Whole-game zero-GC certification | NOT RUN | Short historical helper/profiler observations do not certify the whole game |
| Separate stopped 30-minute visible soak | NOT RUN | Not resumed; requires explicit permission |
| Paired Bloom on/off comparison | NOT RUN | No paired comparison claimed |
| Human listening acceptance of latest whole-game music staging | PENDING | No new human listening approval claimed |

The 411-test, 294-reference and Development build results in Task 15B notes are **historical**. The new 411-test run and native audit above are independently executed Task 16 evidence. Historical measured traffic increases remain **23.9%, 21.4%, 18.9%**, not 30%. Historical start/mid/cap lane-change observations were 2/1/0; the cap sample did not observe an accepted NPC lane change. No ten-minute/zero-GC/soak claim is inferred from those samples.

Initial delivery diagnostics remain documented in `Evidence/Task16/diagnostics.md`: an Editor-only metadata DTO compile correction, the pre-compilation BLOCKED test attempt, an external capture-fixture access error, replaced-but-retained effect frames and a Python 3.10 hash-helper compatibility correction. None is counted as a successful test or hidden by deleting evidence.

## Report, screenshots, credits — V24 / R01, R54

**PASS** for report source/PDF, real images and disclosure. `Task16Report.py` renders the editable source through installed headless Chromium/Skia with actual DejaVu Persian glyph shaping and bidi isolation. No font/image was downloaded or generated for implementation evidence. Inspected every rendered page for connected letters, punctuation, English identifiers, image readability and margins. After isolating English identity fields, final page 1 was reinspected and pages 2–12 were pixel-identical to the previously inspected pages. `pdf-inspection.json` records the final PDF hash, embedded fonts and comparisons. PNG page renders and HTML remain local rendering evidence.

All image clusters requested are represented: main menu; open-night HUD; tunnel; low/high-speed headlights/neon; visible natural firework burst; collision spark; coin/fuel detached bursts; Pause; Game Over with score/best; rear warning; near-miss behind hold/earned aftermath, plus cancellation examples. Pickups use actual pool rentals and swept interactions followed by ordinary steering to expose their detached particles. Near-miss fixtures never call the effect trigger directly. These staged Editor frames are identified accurately; no human playthrough or Windows capture is implied. Initial partly occluded/pre-burst frames remain in `EffectsAttempt1/`.

**BLOCKED** for fully resolved near-miss attribution/redistribution metadata. The local WAV and previous notes trace it to the owner-supplied six-second excerpt, 44.1 kHz stereo PCM, source 00:06–00:12. Catalog investigation identifies **POLTERGST & RØØTZ — Into The Void (feat. Jordan Lindley)**, ℗ 2025 Spinnin' Records, with an exact public catalog address in `CREDITS.md`. That address is **not proof of this WAV's original download source or a redistribution license**. No source permission document was found or invented. The requested excerpt is retained; the student must supply provenance/permission before course delivery. AI assistance, original project art/synthesis, Unity resources, Liberation Sans OFL and DejaVu font permissions are disclosed. No additional human collaborator was named in the supplied records.

## Archive and upload gates — V23/V25 / R02–R04, R51–R54

The task's V23 ignore gate is recorded below; the older preproduction table also places ignore under V22 and extracted launch/repository access under V23. Both obligations are retained.

| Check | Status | Evidence / limit |
| --- | --- | --- |
| R50 Git repository and exact local release commit | PASS | Explicit reviewed source commit `e97572a`; no source snapshot omission of Task 15B |
| V23 / R52 supplied `.gitignore` byte identity | PASS | `preservation.json`; `git check-ignore` confirms Library, Temp, Obj, Logs, UserSettings, Build and Builds ignored |
| R51 actual HTTPS repository link file | PASS | Root archive `repository-link.txt`, full tested commit, honest publication status |
| R51 remote contains exact release / grader access | PENDING | Public repository empty at verification |
| R53 Windows build and full companions | PASS | All **196 emitted files**, paths and SHA-256 match the BuildReport inventory |
| R02 exact ZIP filename | PASS | `GD_Hw2_Alireza_Pooyan_400414039.zip` |
| R54 PDF and genuine screenshots in ZIP | PASS | Extracted PDF/screenshots match final local bytes |
| Supplied submission verifier | PASS | Usage inspected, invoked with actual positional archive path; exit **0**; `supplied-archive-verifier.txt` |
| Independent CRC / duplicate and unsafe paths | PASS | `zipfile.testzip() is None`; no duplicate/absolute/traversal/backslash/drive paths |
| Exact expected file inventory | PASS | **218/218**, no Library or unrelated generated folders; every Unity output file included |
| Extraction into a new clean directory | PASS | Full extracted-file SHA-256 comparison **218/218**; location in `archive-verification.json` |
| Executable target after extraction | PASS | AMD64 PE format and same executable hash; does not establish launch |
| Extracted Windows executable launch | BLOCKED | No native Windows host/VM. No Linux Editor or Wine result substituted |
| R03 course upload | PENDING | No authorized course session supplied; no upload/receipt/time fabricated |
| R04 actual post-upload download, CRC/hash and launch | PENDING | No downloaded course copy exists yet |

Archive structure contains root `repository-link.txt`, `README-run.txt`, `ImplementationReport.pdf`, `CREDITS.md`, `Build/Windows/` and `Screenshots/`. The PDF does not embed the ZIP hash because that would be self-referential; the finished archive hash is provided separately here and in the `.sha256` sidecar.

## Reproduction and remaining student actions

Use the existing installed CLI/Pipeline and pinned Editor; do not upgrade packages or restore an older source snapshot. Build from saved Game outside Play Mode. For a repeat build, move the **entire** prior `Build/Windows` output to a retained directory first, then invoke `NeonTunnelRunner.BuildDelivery.BuildWindowsRelease` via the existing Pipeline `script eval` or Editor menu. Preserve/restore the original development profile disk state as above. This release build must always target Windows x86_64; restoring the Editor's original development target is a separate step.

Fresh tests and saved-scene checks extend the existing runner:

```text
python3 -B Docs/Setup/Task15Run.py tests --evidence-root Evidence/Task16/Runs
python3 -B Docs/Setup/Task15Run.py scene --evidence-root Evidence/Task16/Runs
python3 -B Docs/Setup/Task16Capture.py
python3 -B Docs/Setup/Task16Report.py
python3 -B Docs/Setup/Task16Package.py
python3 Preproduction/tools/verify_submission.py GD_Hw2_Alireza_Pooyan_400414039.zip
```

The package helper rejects an existing ZIP and validates every build file against Unity's original inventory. Retain the previous archive before an intentional rebuild; a changed PDF, credit or repository file requires a new ZIP, hash and extraction verification. The separate improved effect capture recipe and evidence identify its later captures; do not assume rerunning the general capture alone reproduces those specific final frames.

Student actions still required:

1. **Publish the reviewed source** to the supplied public repository and check anonymous access to `e97572aba25d8bcff7e336c4bf6ef8b6e3c540fc`. `origin` is configured; a student-authorized `git push -u origin main` can publish the local reviewed commits. Existing unrelated dirty work remains uncommitted; do not mass-stage it. Recheck the release URL before handing it to the grader. No push was performed here.
2. **Resolve the excerpt's original source URL and redistribution permission**. The public song listing does not establish permission. Any content correction requires a deliberately reviewed rebuild/report/archive update and new hash; no automatic substitution was made.
3. **Use a native Windows 64-bit PC** (for example an available institution/fellow student's Windows computer) to extract the ZIP and run `Build/Windows/NeonTunnelRunner.exe`. There is no available VM here. Follow every functional item in `README-run.txt`: menus/keyboard/three lanes, traffic/following/rear warnings/overtakes, strict near-miss success/cancellations, zoom and music/pause/focus, fuel/coins/lives, both Game Over causes, score, lights/VFX and **actual Quit**. Quit fully and launch again to verify earned best persists. Record Windows version, date, commit, results and any exact launch/security error. Do not claim success merely from PE/CRC checks.
4. **Upload the exact named ZIP to the course system** using authorized student access, keeping its SHA-256 separately and retaining the real receipt/time.
5. **Download the uploaded archive again**. Compare its full SHA-256 to the value above, rerun CRC/structure validation, extract it into another clean directory and execute that downloaded Windows copy. Record the actual downloaded hash and result. If the course legitimately rewrites ZIP bytes, compare every extracted file hash and document the difference explicitly.

Final Editor: saved Game loaded and clean, outside Play Mode; timeScale 1, original rational fixed step `0.0199999921`, best **27316**, background false, prior auto-tick true, original Linux development target restored. `final-editor-state.json` records this state. No scene/prefab/config assignments changed during delivery. The only new asset is Editor-only `BuildDelivery.cs` and its Unity-generated meta.

Task 16 stops here. **Next prompt number: 17, if a subsequent prompt is supplied; no Task 17 is implemented.**
