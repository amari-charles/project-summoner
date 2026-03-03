# Gesture Feedback

Visual feedback for in-progress gestures. Renders *input* state (drag position, targeting) as 3D visuals.

## Why in Input

Gesture feedback lifecycles are gesture-driven — created on drag start, destroyed on drag end. MatchState has no "drag in progress" concept, so EntityManager can't manage these components. InputCollector owns their lifecycles, so they live in Input, not View. See [documentation-guide.md principle #10](../../../migration/documentation-guide.md).

Gesture feedback CAN read `IGameSession.GetState()` for card data (Input already depends on Session).

## Components

| Component | Purpose | Status |
|-----------|---------|--------|
| [`SummonPreview`](summon-preview.md) | Ghost unit formation during summon card drag (includes internal `UnitGhost` class) | Implemented |
| `SpellPreview` | Circle + arrow during spell targeting | Implemented (GDScript) |
| `SpawnZoneOverlay` | Valid/invalid zone highlight during summon drag | Implemented (GDScript) |
| `RedirectIndicator` | Circle + arrow during redirect gesture | Future |

## Dependencies

| Direction | Component | Relationship |
|-----------|-----------|-------------|
| Reads | `IGameSession` | Card data, unit definitions |
| Lifecycle | `BattlefieldDropZone` | Today: creates/destroys previews during drag |
| Lifecycle (target) | `InputCollector` | Target: InputCollector owns all gesture feedback lifecycles |

## Today

Lifecycle is managed by `BattlefieldDropZone._create_spawn_preview()` / `_cleanup_spawn_preview()`. The target architecture consolidates this into `InputCollector`.
