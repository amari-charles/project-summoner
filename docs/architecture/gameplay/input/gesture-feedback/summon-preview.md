# SummonPreview

C# class extending `Node3D`. Visual preview showing ghost units at spawn location during summon card drag.

**Old name:** `SpawnPreview` (renamed to use domain term — "Summon" pairs with future `SpellPreview`)

## What It Is

A gesture feedback component that creates and manages a formation of ghost unit children. Shows the player exactly where and how their units will appear when the card is played.

## API

| Method | Purpose |
|--------|---------|
| `Initialize(unitScene, spawnCount, team, catalogId)` | One-shot init. Resolves separation radius + flight altitude from UnitDefinitions. Creates ghost formation. |
| `UpdatePositions(positions)` | Move ghosts to actual spawn positions. Called each frame while dragging. |
| `SetValid(isValid)` | Toggle valid (blue) / invalid (red) tint on all ghosts. |
| `Cleanup()` | Destroy all ghosts and self. |

## Dependencies

| Direction | Component | Relationship |
|-----------|-----------|-------------|
| Reads | `UnitDefinitions` | Resolves `separationRadius` and `flightAltitude` from `catalogId` |
| Created by | `BattlefieldDropZone` | Today: lifecycle managed by `_create_spawn_preview()` / `_cleanup_spawn_preview()` |
| Created by (target) | `InputCollector` | Target: InputCollector owns lifecycle |

## What It Does NOT Do

- Produce Commands — it's purely visual feedback
- Validate spawn position — the caller (BattlefieldDropZone / InputCollector) handles validity
- Manage its own lifecycle — created and destroyed by its owner
- Handle spell cards — `SpellPreview` handles those

## Fallback Behavior

If no ghosts produce valid visuals (e.g., unit scene has no Visual child), SummonPreview falls back to a flat circle marker mesh. This ensures the player always sees *something* during drag.

## UnitGhost (Internal)

`UnitGhost` is an internal class in the same file — it represents a single transparent ghost unit within the formation. SummonPreview creates one per `spawnCount`.

Each ghost instantiates the unit scene, extracts its Visual child, and applies a semi-transparent tint. Supports C# `IVisualComponent`, `SkeletalVisualComponent`, `SpriteVisualComponent`, and GDScript visual components via duck typing.

Tint colors:
- **Valid:** `(0.7, 0.85, 1.0, 0.5)` — light blue, 50% alpha
- **Invalid:** `(1.0, 0.5, 0.5, 0.5)` — red, 50% alpha

## Today

`scripts/csharp/Input/SummonPreview.cs`, namespace `Fateforged.Input`. Both `SummonPreview` and `UnitGhost` live in this file. Lifecycle managed by `BattlefieldDropZone._create_spawn_preview()` / `_cleanup_spawn_preview()`. Target: InputCollector.
