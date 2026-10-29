# Unity project, scenes and prefabs

The downloadable package is a specification. Create or adapt the real Unity project in task01. Copy this package into repository-root `Preproduction/`; copy **source/assignment.gitignore to the Unity repository root**. Do not place planning art or the PDF under Assets unless used deliberately in the game.

## Folders

```text
RepositoryRoot/
  .gitignore
  Assets/
    NeonTunnelRunner/
      Scenes/Game.unity
      Scripts/
        Core/
        Input/
        Player/
        World/
        Pooling/
        Interactables/
        Presentation/
        Audio/
        UI/
        Persistence/
        Config/
        Editor/
      Input/Runner.inputactions
      Configs/
      Prefabs/Player/
      Prefabs/Road/
      Prefabs/Interactables/
      Prefabs/VFX/
      Materials/
      Meshes/
      Textures/
      Audio/Clips/
      Audio/Mixers/
      UI/Fonts/
      UI/Icons/
      Settings/URP/
      Settings/Volumes/
      Tests/EditMode/
      Tests/PlayMode/
  Packages/
  ProjectSettings/
  Docs/Environment.md
  Docs/Verification/
  Evidence/
  CREDITS.md
  Preproduction/
  Builds/                 (ignored by supplied .gitignore)
```

Use `NeonTunnelRunner` as C# namespace root, PascalCase types/methods, private serialized camelCase fields and explicit read-only state access. Create runtime, Editor, EditMode and PlayMode assembly definitions; runtime references Input System, uGUI/TMP as actually resolved. Editor assembly references runtime and UnityEditor only. Test assemblies reference runtime and Test Framework. No dependency on com.unity.pipeline in player code. Preserve `.meta` GUIDs; use Editor asset moves, not destructive regeneration. Force Text serialization and Visible Meta Files.

## Single Game.unity scene

| Root | Components / children | Responsibility |
| --- | --- | --- |
| Systems | GameManager, GameplayLoop, PoolService, RoadManager, SpawnDirector, WorldOrigin, ProgressionSystem | Serialized composition root; exactly one GM |
| Player | PlayerController, PlayerSurvival, InteractionSensor, kinematic Rigidbody, BoxCollider; Visual child | Kinematic lane movement; visual-only roll and bob |
| World | RoadSegments root; Interactables root; Background root | Explicit active transform sets; no double origin shifting |
| Pools | Inactive storage by PoolKey | Never moves with road; each rent gets local coordinates |
| Presentation | MainCamera/AudioListener, ChaseCamera, GlobalVolume, VfxController, FireworkDirector | Render presentation |
| Audio | AudioController; Music, Engine and 8 SfxVoice children | Fixed source pool and AudioMixer groups |
| UI | ScreenSpaceOverlay Canvas; HUD, MainMenu, PauseMenu, GameOver | Panel presentation and focus |
| EventSystem | EventSystem + InputSystemUIInputModule | Remove legacy StandaloneInputModule |

Scene opens in Menu. Keep every Systems reference assigned; ProjectSetupValidator reports missing data before entering play. Bootstrap prewarms then exposes Start. An Editor-only context-menu start command may support early graybox tests until task14 adds menus; remove temporary controls when real UI is wired.

## Prefab anatomy

| Prefab | Root and children | Required reset/wiring |
| --- | --- | --- |
| PlayerHovercar | Root: controller/survival/sensor/Rigidbody/box; Visual/Body, Canopy, ThrusterLeft, ThrusterRight, UnderGlowMesh; Lights/HeadLeft,HeadRight,UnderGlow | Root scale1, rotation0, +Z forward; roll visual child only; two headlights and attached exhaust |
| RoadOpen60 | RoadSegment/PooledObject; Surface, 2 InteriorDividers, 2 OuterEdges, Rails, Streetlights, EntryMarker(z0), ExitMarker(z60) | Width9.6; no colliders protruding into three lanes; exact seam |
| RoadTunnel60 | Same road base plus octagonal Arch modules and ceiling | Same endpoints; roof height≥8 m; readable horizon |
| RoadEntrance60 / RoadExit60 | Same base with taper only in architecture | Road plane stays flat and straight; no geometry discontinuity |
| StationaryCar | StationaryCar; BoxCollider; Visual/body/tail lamps | Consumed=false, speed0, pose history reset on rent |
| MovingCar | MovingCar; kinematic Rigidbody; BoxCollider; Visual/body/tail lamps | Fixed lane and +8 m/s; trajectory checked; body never changes lane |
| Coin | Coin; BoxCollider trigger matching logical bounds; Visual/gold ring | Reset rotation/bob; one reward per activation |
| FuelCan | FuelCan; BoxCollider trigger; Visual/can/handle/cap | Mint color and can silhouette; clamp fuel reward |
| Spark / CoinBurst / FuelBurst | PooledVfx; ParticleSystem children | Stop/clear on return; local space; no StopAction.Destroy |
| Firework | PooledVfx; rocket ParticleSystem plus burst sub-emitter | Launch then burst above horizon; all particles pooled together |

No Interactable is parented to a road segment: traffic can outlive the segment it initially appeared on. SpawnDirector returns interactables independently behind the player. Decorating lights are children of segments and recycled with them.

## Layer and setup contract

Layers: Player, Road, Obstacle, Pickup, Background, VFX, UI. Keep logical interaction masks explicit. The collision sensor queries its active registry, not every collider in the scene. Root BoxColliders document contact sizes; use trigger mode for interactables without a duplicate trigger handler. Ground physics and rigidbody forces are not required for the fixed-height hovercar. Background/VFX never affect gameplay. The camera's Volume mask includes the GlobalVolume layer; Post Processing is enabled. AudioListener exists only on MainCamera.

## Complete script inventory

| File under Scripts/ | Responsibility |
| --- | --- |
| Core/GameState.cs | GameState and GameOverReason enums |
| Core/RunSession.cs | Single authoritative run model |
| Core/GameManager.cs | Required singleton; lifecycle commands only |
| Core/GameplayLoop.cs | Explicit deterministic simulation order |
| Input/InputRouter.cs | Action asset lifetime and command buffer |
| Player/PlayerController.cs | Three-lane motion and authoritative poses |
| Player/PlayerSurvival.cs | Lives, grace and fuel rules |
| Player/InteractionSensor.cs | Continuous contact ordering |
| World/SweptBounds.cs | Pure relative swept AABB mathematics |
| World/RoadManager.cs | Endless module coverage and seam agreement |
| World/RoadSegment.cs | Pooled 60 m module and endpoint data |
| World/WorldOrigin.cs | Keep local coordinates bounded |
| World/SpawnDirector.cs | Deterministic traffic and pickups |
| World/PatternValidator.cs | Reject unsafe spawn candidates |
| World/ProgressionSystem.cs | Difficulty, distance score, best calculation |
| Pooling/IPoolable.cs | Explicit lifecycle interface |
| Pooling/PoolKey.cs | Stable typed prefab keys |
| Pooling/PooledObject.cs | Pool identity and activation token |
| Pooling/PoolService.cs | Bounded prewarmed pools |
| Interactables/Interactable.cs | Required shared abstract base |
| Interactables/StationaryCar.cs | Stationary obstacle |
| Interactables/MovingCar.cs | Moving obstacle in fixed lane |
| Interactables/Coin.cs | Coin reward |
| Interactables/FuelCan.cs | Fuel reward |
| Presentation/ChaseCamera.cs | Rear camera, smoothing and damage impulse |
| Presentation/VehicleVisuals.cs | Body roll/bob/flash, lights and thrusters |
| Presentation/VfxController.cs | Route gameplay cues to pooled effects |
| Presentation/PooledVfx.cs | Reset reusable particle bursts |
| Presentation/FireworkDirector.cs | Sky launch/burst cadence |
| Audio/AudioController.cs | Music/engine and bounded one-shot voices |
| UI/HudPresenter.cs | Live HUD snapshot projection |
| UI/MenuPresenter.cs | Panels, focus and commands |
| Persistence/HighScoreStore.cs | Only PlayerPrefs adapter |
| Config/GameplayConfig.cs | Immutable gameplay tuning asset |
| Config/WorldConfig.cs | World/pattern authoring asset |
| Config/PoolConfig.cs | Prefab capacities and keys |
| Config/PresentationConfig.cs | Camera/light/VFX style tuning |
| Config/AudioConfig.cs | Licensed clips and audio tuning |
| Editor/ProjectSetupValidator.cs | Editor-only wiring and requirement audit |
| Editor/BuildDelivery.cs | Editor-only repeatable build entry |

All public APIs, data ownership, dependencies, lifecycle, events/config and test responsibilities are specified in document03. No placeholder “Manager” class owns everything. Tests listed in document09 are additional files created alongside the corresponding system, not runtime scripts.
