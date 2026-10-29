# Asset-generation prompts

The `reference-*.md` files reproduce the actual concept-generation prompts. The twelve images already generated live under `art/concepts` and `art/ui-concepts`. Further `texture-*.md` and `ui-*.md` prompts are ready to paste but are **not claimed as generated/imported game assets**.

Use the selected gameplay image as a style reference for future images. Preserve original faceted wedge shape, twin rectangular thrusters and color roles. Do not ask an image tool to provide exact topology, UVs, code, readable runtime text or guaranteed PBR maps. Follow each prompt's production steps and document07.

| Prompt family | Deliverable type | After generation |
| --- | --- | --- |
| reference-direction-* | Three art studies | Compare style/readability only |
| reference-selected-gameplay/player/environment/obstacle/pickup | Concept reference | Model, UV, import, collider, prefab and pool setup |
| reference-main-menu/hud-mockup/game-over | UI reference | Rebuild using real TMP text and uGUI controls |
| reference-vfx-reference-sheet | VFX reference | Author Unity ParticleSystems, lights and Volume Bloom |
| texture-road-albedo/metal-trim/stars | Texture candidates | Seam/color-space/map validation and import |
| texture-particle-softdot/streak | Alpha sprite candidates | Inspect transparency, padding and shader blending |
| ui-icon-reference/ui-panel-background | UI art candidates | Vector redraw/slicing or background-only import |

For a mesh tool, supply the relevant design sheet plus the exact dimensions/triangle/material budget from document07; request a low-poly mesh with applied transforms and no embedded branding. Inspect and repair topology/UVs/normals in a modeling editor, export FBX/glTF as supported, then create a Unity prefab. Never rename a PNG to a model extension or treat a beauty render as a ready prefab.

## Complete prompt-file index

- [reference-direction-a-retro-vector.md](reference-direction-a-retro-vector.md)
- [reference-direction-b-cyberpunk.md](reference-direction-b-cyberpunk.md)
- [reference-direction-c-industrial.md](reference-direction-c-industrial.md)
- [reference-environment-moodboard.md](reference-environment-moodboard.md)
- [reference-game-over.md](reference-game-over.md)
- [reference-hud-mockup.md](reference-hud-mockup.md)
- [reference-main-menu.md](reference-main-menu.md)
- [reference-obstacle-designs.md](reference-obstacle-designs.md)
- [reference-pickup-designs.md](reference-pickup-designs.md)
- [reference-player-design-sheet.md](reference-player-design-sheet.md)
- [reference-selected-gameplay.md](reference-selected-gameplay.md)
- [reference-vfx-reference-sheet.md](reference-vfx-reference-sheet.md)
- [texture-metal-trim.md](texture-metal-trim.md)
- [texture-particle-softdot.md](texture-particle-softdot.md)
- [texture-particle-streak.md](texture-particle-streak.md)
- [texture-road-albedo.md](texture-road-albedo.md)
- [texture-stars.md](texture-stars.md)
- [ui-icon-reference.md](ui-icon-reference.md)
- [ui-panel-background.md](ui-panel-background.md)
