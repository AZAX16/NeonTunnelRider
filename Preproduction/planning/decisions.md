# Decision log

| ID | Decision | Reason / consequence |
| --- | --- | --- |
| D01 | Use installed6000.6.3f1; preserve beta CLI/plugin/Pipeline pins | User supplied setup overrides initial6.3LTS suggestion; no unrequested migration |
| D02 | URP plus integrated Volume Bloom | Neon-friendly, achievable; document PDF package wording interpretation |
| D03 | One Game scene; one scene-owned GameManager singleton | Required pattern without persistent-scene duplication |
| D04 | GameManager owns state; RunSession owns run data | Prevents duplicated UI/player resource values |
| D05 | Explicit GameplayLoop FixedUpdate order | Makes contact/refuel/death behavior deterministic and testable |
| D06 | True+Z movement with periodic origin rebase | Stationary versus moving obstacles have natural world semantics |
| D07 | Logical relative swept boxes | Reliable high-speed contact including moving targets; no trigger double handling |
| D08 | Linear logical lane motion, presentation-only easing/roll | Collision math uses predictable poses; visual feel stays responsive |
| D09 | Abstract Interactable under pooled base, four overrides | Directly satisfies assignment inheritance requirement |
| D10 | Prewarm bounded pools before play | No normal-play instantiate/destroy; explicit overflow policy |
| D11 | Protected lanes with clear120m transitions | Conservative fairness proof at maximum speed; reject unsafe moving envelopes |
| D12 | Moving cars remain in lane at8m/s | Distinct required category without unsafe AI lane-change complexity |
| D13 | Fuel guarantee by≤220m spacing | Avoids RNG-caused unavoidable fuel death |
| D14 | Distance plus coin scoring; PlayerPrefs best | Formula is a design choice; persistence follows PDF |
| D15 | DirectionA; simple meshes/shared materials | Highest readability and achievable student asset scope |
| D16 | uGUI/TMP with InputSystemUIInputModule | Straightforward menus and inspectable references |
| D17 | Linux development build; graderOS unresolved | Matches user's machine without assuming grader compatibility |
| D18 | Generated images remain reference-only | No false claim of meshes, actual screenshots or implemented effects |
| D19 | Keep supplied.gitignore byte-for-byte | PDF specifically mandates the accompanying file |
| D20 | Preproduction ZIP distinct from course submission | Current deliverable contains no game build or implementation evidence |

Any implementation change affecting these contracts must update the architecture, relevant prompts, validation and requirement evidence together. Favor a small documented change over silently diverging code.
