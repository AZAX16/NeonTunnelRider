# User environment and compatibility checkpoint

| Component | Baseline | Action |
| --- | --- | --- |
| OS | Linux Mint Virginia; glibc2.35 | Preserve; record architecture and GPU/driver |
| Editor | 6000.6.3f1 | Exact pin; no upgrade/downgrade in prompts |
| Unity CLI | 1.0.0-beta.13 | Verify locally with installed version/help; do not self-update |
| Official Unity Codex plugin | Unity-Technologies/unity-agent-plugin 0.1.8-beta | Preserve and inspect installed skills before using commands |
| Unity Pipeline | com.unity.pipeline 0.9.1-exp.1 | Editor automation; preserve manifest entry |
| URP | Not reported | Inspect for com.unity.render-pipelines.universal and actual assigned assets |
| Input | New Input System | New-only input handling; record resolved package version |
| Codex | VS Code extension + CLI; CLI version unknown | Record installed CLI version; no assumption of tool availability here |
| Unity Test Framework | Not reported | Use package version compatible with Editor; record resolved lock |

The supplied setup is a user report, not an environment this preproduction task has executed. Current official pages confirm the Editor release, plugin manifest and role of Unity Pipeline. Installed beta command syntax must still be discovered locally.

## First local inspection

Use read-only commands in the Unity project directory. Inspect command help before adapting syntax:

```bash
git status --short
unity --version
unity --help
unity pipeline --help
unity pipeline list
unity command --help
unity command
codex --version
ldd --version
```

Capture versions and command availability in `Docs/Environment.md`; do not include credentials, tokens or unrelated project details. If multiple Editors are running, select the exact project using the selector exposed by installed command help. Read the plugin's installed instructions; do not copy unverified flat Pipeline command names from older tutorials. Use available automation for inspection, scene/prefab edits, compilation and tests, wait for completion and check Console output after each batch. No blind package reinstall, `self-update`, `pipeline upgrade`, forced upgrade or global glibc replacement.

When the local CLI cannot connect, inspect its returned error and Editor logs. Continue through the same Editor's normal UI or documented batchmode commands. A missing CLI is a tooling limitation, not permission to swap the render pipeline or omit tests. If the Unity Editor itself is unavailable, generate files only where appropriate, label verification BLOCKED, and provide exact Editor steps—never invent successful builds.

URP creation is appropriate for a new empty project. For an existing project, preserve working content and check render compatibility first; do not overwrite scenes, meta files, settings or packages wholesale. Keep this specification in `Preproduction/` at the Unity repository root, not under Assets. Runtime assemblies must not reference Editor-only Pipeline automation APIs. Test an ordinary standalone build with Editor connection unavailable.
