# Retained Task 16 diagnostics

- Initial Editor-only builder compilation: two CS0266 int-to-uint errors in the metadata DTO; corrected the DTO to the actual int API fields. The first test invocation was BLOCKED before running, and remains retained. Fresh completed suites subsequently passed 161 + 250.
- Capture attempt 1: the external in-memory fixture could not call internal SpawnDirector.ReleaseAll directly. Corrected to reflection; no production code changed. Retained in CaptureAttempt1/.
- Initial firework screenshot preceded the visible burst and pickup particles were partly occluded. Original pixels are retained in EffectsAttempt1/. Later genuine frames observe the actual burst and use an ordinary adjacent move after pickup.
- First packaging invocation hit Python 3.10 missing hashlib.file_digest before creating the archive. Replaced it with streamed SHA-256; completed verifier evidence is separate.
- Windows build and isolated import both report the bundled LiberationSans.ttf.meta importer version warning (2 below 4). It is not hidden, upgraded or counted as zero warnings.
- BuildOptions.ToString in this Editor lists overlapping legacy labels including Il2CPP. The actual native options value is 536870912 / 0x20000000, requested DetailedBuildReport only; backend is Mono2x. build-options-native.json and the emitted MonoBleedingEdge runtime disambiguate this.
- BuildPlayer switched the Editor target to Windows. Restored the original Linux development target after capturing; no Linux final player was built. The existing LinuxDevelopment profile flag was restored in memory by the builder, then explicitly saved through native AssetDatabase.SaveAssetIfDirty to restore its original disk bytes. preservation.json confirms every initial production input is unchanged.
- Historical Pipeline counters are retained; the fresh ground-truth Console sample has compilationFailed=false, 0 errors and 1 warning. No global historical-clean-console claim.
