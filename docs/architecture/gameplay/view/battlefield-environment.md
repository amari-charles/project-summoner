# BattlefieldEnvironment

GDScript class extending `Node3D`. Standalone battlefield visual environment.

**Old name:** `BattlefieldVisuals3D` (renamed to describe role, not engine type)

## What It Is

A standalone component that sets up the visual environment for the battlefield — sky gradient, ground texture, and layer organization. Not managed by EntityManager. Exists independently in the scene tree and initializes itself on `_ready()`.

## Responsibilities

### Sky Gradient
Creates a procedural gradient texture applied to a `Sprite3D` sky layer. Top-to-bottom gradient from light sky blue through azure to warm peachy horizon.

### Ground Texture
Loads and applies the grass tile texture to a `Sprite3D` ground layer. Falls back gracefully if the texture is missing.

### Layer Organization
Provides access to the gameplay layer (`Node3D` for unit/projectile spawning) and UI layer (`CanvasLayer` for battlefield UI elements) via getter methods.

## What It Does NOT Do

- Know about game state or MatchState
- Respond to SimEvents
- Depend on EntityManager
- Handle biome-specific visuals (future: could swap textures/gradients based on biome config)

## API

| Method | Purpose |
|--------|---------|
| `get_gameplay_layer()` | Returns the Node3D where units and projectiles are spawned |
| `get_ui_layer()` | Returns the CanvasLayer for battlefield UI |

## Dependencies

| Direction | Component | Relationship |
|-----------|-----------|-------------|
| None | — | Standalone; no dependencies on other View components |

## Today

`battlefield_visuals_3d.gd` (~57 lines) is already a well-isolated component. The main changes are:
- Rename from `BattlefieldVisuals3D` to `BattlefieldEnvironment` (role-based naming)
- Future: biome-driven visuals (swap sky gradient and ground texture based on battle config)
- No structural refactoring needed — it's already standalone
