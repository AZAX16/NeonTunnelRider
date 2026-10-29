# Codex prompt index

Paste one complete numbered file at a time after copying this specification into the Unity repository as `Preproduction/`. Execute in numeric order. Pooling intentionally precedes world generation. The first tasks build the graybox; later tasks add presentation. Every prompt is self-contained about environment/rules and points to shared detailed contracts.

| Prompt | Objective |
| --- | --- |
| [01 — Project setup and environment audit](01-project-setup.md) | Establish a clean, reproducible project foundation without implementing gameplay. |
| [02 — GameManager and run lifecycle](02-game-manager.md) | Implement the required singleton and testable run/state ownership. |
| [03 — New Input System and three-lane player](03-input-and-player.md) | Make a responsive, continuously advancing graybox car using only actions/maps. |
| [04 — Bounded pooling infrastructure](04-object-pooling.md) | Provide reusable lifecycle-safe pools before any endless spawning. |
| [05 — Procedural road, origin and safe-pattern data](05-endless-world.md) | Deliver a gap-free endless graybox road and generation rules ready for traffic. |
| [06 — Interactable inheritance and traffic collisions](06-interactables-and-obstacles.md) | Add both obstacle types and reliable high-speed encounters. |
| [07 — Coins and fuel pickups](07-pickups.md) | Complete the required four-type hierarchy with pooled rewards. |
| [08 — Lives, fuel, feedback and Game Over](08-survival-and-game-over.md) | Finish the complete graybox survival loop and terminal behavior. |
| [09 — Difficulty, score and persistent high score](09-difficulty-score-persistence.md) | Progress challenge and scoring without sacrificing fairness. |
| [10 — Rear camera and hovercar motion polish](10-camera-and-motion.md) | Improve motion readability without changing gameplay collision geometry. |
| [11 — Night environment, assets and dynamic neon](11-night-neon-environment.md) | Build the selected Retro Vector visual identity and required lighting. |
| [12 — Particles, Bloom and fireworks](12-vfx-bloom-fireworks.md) | Implement every assessed particle and post-processing effect. |
| [13 — Music, engine and interaction audio](13-audio.md) | Complete required sound cues with bounded reusable sources. |
| [14 — Menus, HUD and UI integration](14-menus-and-hud.md) | Expose every required action and live value using New Input System UI. |
| [15 — Full integration, fairness and performance audit](15-integration-and-validation.md) | Close implementation gaps with reproducible evidence for every requirement. |
| [16 — Build, report and submission verification](16-build-and-delivery.md) | Prepare the executable, evidence report and correctly named course archive. |

After each task, inspect the diff, execute its checks and keep a checkpoint commit when the user’s repository workflow permits. Do not paste all sixteen prompts at once.
