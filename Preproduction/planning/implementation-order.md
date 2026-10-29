# Implementation order

Read requirements → game design → architecture → scene/prefab structure before editing Unity. Copy this entire package under repository-root `Preproduction/`, retaining its relative links. Do not put it beneath Assets. The root `.gitignore` inside this package is the provided assignment file; copy it to the real Unity project root as task01 instructs.

Execute01–16 in order. Pooling is04 and world generation05; this deliberately avoids temporary Instantiate/Destroy gameplay. Task02 introduces core data/config,03 motion,06 the minimal survival contact API,07 reward scoring,08 full fuel/terminal logic and09 difficulty/persistence. Each task expands compiling contracts rather than adding references to future undefined classes.

Architecture is in place before the playable prototype, not retrofitted after a polished game. UI completes in14 because it consumes the already stable states and events. Use Editor context commands for early graybox testing; the final game uses only action maps and real menu controls.

Stop after each prompt's Definition of Done, inspect actual results, then paste the next. Re-run only affected checks after a targeted fix, plus the current integration gate. Never ask Codex to execute the complete pack in one huge generation step.
