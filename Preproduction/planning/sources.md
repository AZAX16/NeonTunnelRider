# Sources and attribution ledger

Checked 2026-10-29. The supplied HW2.pdf overrides design preferences. Online sources establish tool/engine behavior, not a claim that this user's installed system was executed here.

| Source | Exact address | Use / limitation |
| --- | --- | --- |
| Supplied assignment | `source/HW2.pdf` and `source/assignment.gitignore` from HW2.zip | Printed pp.1–5; full visual and text review |
| User environment | Setup table in this conversation | Exact user-provided pins; not independently tested |
| Editor release | https://unity.com/releases/editor/whats-new/6000.6.3f1 | Confirms installed release identity |
| Official plugin | https://github.com/Unity-Technologies/unity-agent-plugin | Plugin purpose and Unity6+ compatibility |
| Plugin manifest | https://github.com/Unity-Technologies/unity-agent-plugin/blob/main/.codex-plugin/plugin.json | 0.1.8-beta listed at review time; pin installed version |
| Unity Pipeline | https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package | Editor HTTP automation, discovery, project selection |
| Pipeline patch announcement | https://discussions.unity.com/t/unity-pipeline-package-0-9-0-exp-1-is-available-now/1739888/30 | Unity staff announces0.9.1-exp.1 and upgrade caveat |
| Unity CLI | https://docs.unity.com/en-us/unity-cli/use-unity-cli | Current Linux glibc minimum; not a beta.13 binary verification |
| URP post-processing | https://docs.unity.com/en-us/engine/6000.3/manual/post-processing-and-full-screen-effects/urp/integration-with-post-processing | Stable integrated-Volume concept; verify installed6.6 API, not PSv2 |
| Add Bloom/Volume | https://docs.unity.com/en-us/engine/6000.0/manual/post-processing-and-full-screen-effects/urp/add-post-processing | Camera toggle, Volume and override setup |
| Input UI | https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/introduction-ui-input-module.md | uGUI InputSystemUIInputModule; inspect installed package docs |
| Kinematic movement | https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.MovePosition.html | Movement contract; installed-version docs take precedence |
| Persistence | https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerPrefs.html | PlayerPrefs API; use installed API |

Do not copy tutorial code that uses legacy Input. The game physics, seed/corridor rules and tuning in this package are proposed design decisions, not quotations from Unity documentation. Concept references were generated using OpenAI image generation; later production assets must be modeled, imported and validated separately. No external game assets were copied.

Before final submission, add exact downloaded asset/font/audio URLs, creator names, license texts, modifications and collaborator contributions to CREDITS.md. List AI assistance and prompts honestly under course policy. The brief references S.T.U.N. Runner and RoadBlasters only as atmosphere influences; no franchise logos or assets are part of the proposed production kit.
