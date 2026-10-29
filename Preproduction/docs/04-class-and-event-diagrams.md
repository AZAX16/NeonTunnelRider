# Architecture diagrams

Mermaid source is deliberately editable. Relationships are scoped to avoid a single unreadable class graph; the complete script contracts are in document03.

## Required inheritance

```mermaid
classDiagram
direction TB
class PooledObject {
  +int ActivationId
  +OnRent(context)
  +OnReturn()
}
class Interactable {
  <<abstract>>
  +TryInteract(context)
  #OnInteract(context)
}
class StationaryCar {
  #OnInteract(context)
}
class MovingCar {
  +PredictStep(dt)
  #OnInteract(context)
}
class Coin {
  #OnInteract(context)
}
class FuelCan {
  #OnInteract(context)
}
PooledObject <|-- Interactable
Interactable <|-- StationaryCar
Interactable <|-- MovingCar
Interactable <|-- Coin
Interactable <|-- FuelCan
```

`OnInteract` is virtual in the abstract base and overridden in all four children. The base guards once-per-activation handling; derived classes implement damage/reward. This is real inheritance, not an enum-only replacement.

## Core ownership

```mermaid
classDiagram
direction TB
class GameManager {
  +GameManager Instance
  +GameState State
  +StartRun()
  +Pause()
  +Resume()
  +Restart()
  +EndRun(reason)
}
class RunSession {
  +Snapshot Snapshot
  +Reset(seed)
}
class GameplayLoop {
  +Step(dt)
  +ResetRun(seed)
}
class PlayerSurvival {
  +TryDamage(encounter)
  +AddFuel(amount)
}
class ProgressionSystem {
  +Step(dt,travel)
  +AddCoin()
}
GameManager *-- RunSession
GameManager --> GameplayLoop
GameplayLoop --> PlayerSurvival
GameplayLoop --> ProgressionSystem
PlayerSurvival --> RunSession
ProgressionSystem --> RunSession
```

## Subsystem dependencies

Arrows mean “depends on / invokes.” The dashed presentation arrow means event subscription; presentation cannot change run values directly.

```mermaid
flowchart TD
  Input["Input Actions and UI commands"] --> GM["GameManager"]
  Input --> Player["PlayerController"]
  GM --> Loop["GameplayLoop"]
  Loop --> Player
  Loop --> World["Road and SpawnDirector"]
  Loop --> Contact["InteractionSensor"]
  Contact --> Rules["Survival and progression"]
  Rules --> Model["RunSession"]
  World --> Pools["PoolService"]
  World --> Safety["PatternValidator"]
  GM --> Store["HighScoreStore"]
  Model -.-> View["HUD, camera, VFX and audio"]
  View --> Pools
```

## Gameplay states

```mermaid
stateDiagram-v2
  direction TB
  [*] --> Menu: bootstrap
  Menu --> Playing: StartRun / reset
  Playing --> Paused: Pause
  Paused --> Playing: Resume
  Paused --> Playing: Restart / reset
  Paused --> Menu: Main Menu / cleanup
  Playing --> GameOver: lives or fuel zero
  GameOver --> Playing: Restart / reset
  Menu --> [*]: Quit
```

## Gameplay event flow

Five participants; presentation observes final state and typed events. Return-to-pool is deferred until contacts are dispatched so the registry stays stable.

```mermaid
sequenceDiagram
  participant I as InputRouter
  participant L as GameplayLoop
  participant C as Interactable
  participant S as RunSession
  participant P as Presentation
  I->>L: buffered lane command
  L->>L: predict poses and swept contacts
  L->>C: TryInteract(context)
  alt obstacle
    C->>S: survival accepts one damage
    S-->>P: Damaged(point,lives)
  else coin or fuel
    C->>S: add score or fuel
    S-->>P: Collected(kind,point)
  end
  L->>L: deferred pool returns
  L->>S: distance, fuel drain, score
  S-->>P: SnapshotChanged
  opt terminal resources
    L->>L: GameManager.EndRun once
    L-->>P: state becomes GameOver
  end
```
