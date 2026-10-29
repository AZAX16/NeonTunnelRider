# Verified environment — task 01

Inspected locally on 2026-10-29. The existing project was created from the installed Universal 3D template; it contained SampleScene, template renderer/Volume assets, template input actions and tutorial scripts. There was no Git repository, project-specific code, prefabs or earlier implementation verification note. `Preproduction/planning/package-audit.md` covers specification integrity only. No `AGENTS.md` was found in the project or its ancestor directories.

| Component | Actual result |
| --- | --- |
| OS / architecture | Linux Mint 21.3 Virginia; x86_64; glibc 2.35 |
| Editor | **6000.6.3f1**, revision **45d8eee7de74**; Unity 6.6 update release |
| Editor binary | `/home/alireza/Unity/Hub/Editor/6000.6.3f1/Editor/Unity` |
| Unity CLI used for task | **1.0.0-beta.13**, `/home/alireza/.local/bin/unity.previous` |
| Default PATH CLI | `unity` reports **1.0.0-beta.14**. This pre-existing difference was discovered; no global binary, tool version or PATH was changed. |
| Official Unity Codex plugin | **0.1.8-beta**, confirmed in installed `.codex-plugin/plugin.json` |
| Unity Pipeline automation | **com.unity.pipeline 0.9.1-exp.1**; absent initially, installed once at this exact version with the pinned CLI |
| Universal Render Pipeline / Core | **17.6.0 / 17.6.0** |
| Input System | **1.20.0** |
| Unity Test Framework / NUnit | **1.8.0 / com.unity.ext.nunit 2.1.0** |
| uGUI / TMP | **com.unity.ugui 2.6.0**, including `UnityEngine.UI` and `Unity.TextMeshPro` assemblies; no separate TMP package needed |
| Codex CLI | **0.164.0** |
| VS Code | **1.143.1**, commit `e646868227c3e3a9e3f24277c8c7220be42c0be7`, x64 |
| Active Editor graphics | Intel UHD Graphics 620; OpenGL 4.6; Mesa **23.2.1-1ubuntu3.1~22.04.4** |
| Other installed GPU | NVIDIA GeForce 930MX; NVIDIA driver **580.178.04**; the current Editor uses Intel |
| Development target / backend | **StandaloneLinux64 / Mono** (`Mono2x` API value), Linux Player module verified installed |
| Other installed build platforms | Windows Player, Windows Server, Web; not targeted by this task |

`Packages/manifest.json` and `packages-lock.json` contain the actual resolved pins. All original direct dependency versions were preserved; only the absent Pipeline dependency and its resolution were added. Pipeline supplies automation; URP supplies rendering. No Post Processing Stack v2, package upgrade, tool upgrade or glibc replacement was performed. Renderer/Input/Test Framework versions are resolved, with no outstanding version guess.

## Saved project configuration

- Force Text; Visible Meta Files; Linear color; Active Input Handling = **Input System Package (New)** (`activeInputHandler: 1`); script generation namespace `NeonTunnelRunner`.
- Graphics default: `Assets/NeonTunnelRunner/Settings/URP/PC_RPAsset.asset`, renderer `PC_Renderer.asset`, index 0, **Forward**, HDR enabled. GPU Resident Drawer disabled for the Forward/OpenGL baseline. Existing SSAO renderer feature retained.
- Quality **PC** uses `PC_RPAsset`; **Mobile** retains `Mobile_RPAsset` with its `Mobile_Renderer`, HDR enabled. All quality levels have explicit valid URP assets. The five moved URP/global-settings assets retain their original GUIDs.
- `LinuxDevelopment.asset` in `Assets/NeonTunnelRunner/Settings/BuildProfiles/` targets Linux Player x86_64, enables Development, and overrides its scene list with **Game only**. The global Build Profiles scene list also contains only Game. The profile is active in this workspace. Active-profile selection is Editor-local; select it after a new checkout.
- `Game.unity` has grouping roots `Systems`, `World`, `Presentation`, `UI`. Systems/UI are empty. There are no gameplay components or prefabs yet.
- `World/ReferenceRoad` contains three static 3.2 × 0.2 × 60 m lane cubes centered at x = −3.2, 0, +3.2, y = −0.1, z = 30. Four 0.07 × 0.02 × 60 m markings sit at x = −4.8, −1.6, +1.6, +4.8, y = 0.012. Surface top is y = 0. Both materials use URP/Lit; no custom shader or imported art.
- `Presentation/MainCamera`: position (0, 5, −10), Euler (15, 0, 0), FOV 60°, clipping 0.1–500 m, MainCamera tag, HDR and Post Processing enabled, renderer index 0, one AudioListener. `ReferenceLight`: directional, Euler (50, −30, 0), intensity 1.4, soft shadows.
- `Presentation/GlobalVolume`: Default layer, global, weight 1, assigned `Settings/Volumes/GlobalVolumeProfile.asset` with **zero overrides**. Camera Volume mask = 1, including Default. R40 will use integrated URP Volume Bloom in task 12; Bloom is not implemented here.
- Runtime assembly references the resolved Input System, uGUI and TMP assemblies. Editor adds only the runtime and URP/Core references, with Editor platform restriction. EditMode/PlayMode test assemblies use `UNITY_INCLUDE_TESTS`, Test Framework and NUnit; EditMode is Editor-only. AssemblyInfo files contain metadata only, so the four scaffolds actually compile without dummy gameplay or dummy tests. Player assembly inspection contains only `NeonTunnelRunner.Runtime`; it references no UnityEditor or Pipeline APIs.
- The original `Assets/Scenes/SampleScene.unity`, `Assets/InputSystem_Actions.inputactions`, tutorial content and other template assets remain. SampleScene is excluded from builds. Game has no EventSystem or input consumer yet; there is no legacy input module. `Runner.inputactions` and UI wiring belong to tasks 03/14.

## Repeatable commands

The official plugin's `unity-cli` and `unity-package-management` skills were read from `/home/alireza/.codex/plugins/cache/unity-agent-plugin/unity/0.1.8-beta/skills/`. Command syntax was discovered using the installed CLI's version/help and the connected Editor's command schemas. One Editor was open initially; every command explicitly selected this project. The missing Pipeline initially produced `STATUS_NO_INSTANCES`, then `STATUS_PIPELINE_LOAD_PENDING`; bringing the existing Editor window to the foreground allowed package import, after which `status --until-ready` reported ready. No Editor was closed and no logs were sent externally.

```bash
TASK01_CLI=/home/alireza/.local/bin/unity.previous
TASK01_PROJECT=/home/alireza/NeonTunnelRider/NeonTunnelRunner
"$TASK01_CLI" --version
"$TASK01_CLI" status --until-ready --project-path "$TASK01_PROJECT" --format json
"$TASK01_CLI" command --caller plugin --skill unity-cli --query script --project-path "$TASK01_PROJECT" --format json
"$TASK01_CLI" recompile --project-path "$TASK01_PROJECT" --format json
"$TASK01_CLI" command --caller plugin --skill unity-cli script eval --code 'NeonTunnelRunner.ProjectSetupValidator.ValidateProject();' --project-path "$TASK01_PROJECT" --format json
"$TASK01_CLI" command --caller plugin --skill unity-cli script run --file Docs/Setup/Task01Audit.cs --entry NeonTunnelRunner.Task01Audit.Inspect --project-path "$TASK01_PROJECT" --format json
```

Use **Neon Tunnel Runner → Validate Project Setup** in the Editor as the equivalent validator entry. Open `Game.unity` before running the detailed audit. Both scripts in `Docs/Setup/` stay outside Assets and run through Pipeline; they never enter player assemblies. `Task01Setup.Configure` is the original template-to-foundation construction recipe and refuses to overwrite an existing Game scene. It moves assets using `AssetDatabase.MoveAsset`; do not rerun it on the completed scene. Build Profile creation used discovered `build list_platforms`, `build create_profile`, `build set_profile` commands; see their saved results in `Evidence/Task01/`.

If Pipeline must be restored in a future copy where it is genuinely absent, beta.13 uses `pipeline install --package-version 0.9.1-exp.1 --project-path <path>`; beta.14's `--version` spelling is **not** beta.13's package selector. Do not reinstall it in this project. The CLI offered a beta.14 update, which was not applied.

For a new checkout, open with this exact Editor, select the **LinuxDevelopment** Build Profile, open Game, allow import to finish, and run the validator. Test assemblies intentionally have no tests yet; no EditMode/PlayMode gameplay test run or player build is claimed.

## Git and generated-file extension

The supplied/root `.gitignore` SHA-256 is `c735b75517ecf18563ce4bf269eb4fe158a5539f7d554823578a85eaec795f45`. Both files are byte-identical. Library, Temp, Obj and Builds are excluded by the supplied rules. The template generated `NeonTunnelRunner.slnx`, which that older supplied file does not cover. Its separate exclusion is documented in [`Setup/gitignore-extension.txt`](Setup/gitignore-extension.txt) and installed locally in `.git/info/exclude`. After cloning, append that extension to your clone's `.git/info/exclude` if desired; do not edit the assignment `.gitignore`. `.gitkeep` files preserve empty planned asset folders in Git; their Unity folder meta files are tracked. Existing VS Code settings are preserved.

See [`Verification/01-project-setup.md`](Verification/01-project-setup.md) for executed checks, evidence and remaining delivery steps.
