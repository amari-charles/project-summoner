# BattleCamera

GDScript class extending `Camera3D`. Standalone camera controller for the battlefield.

**Old name:** `CameraController3D` (renamed to describe role, not engine type)

## What It Is

A standalone camera controller that handles panning, zooming, and camera shake. Not managed by EntityManager — it exists independently in the scene tree with its own input handling.

## Responsibilities

### Pan
Supports keyboard, mouse drag, touch drag, and edge-of-screen panning. Converts screen-space input into world-space camera movement. Respects configurable map boundaries so the camera can't pan beyond the battlefield edges.

### Zoom
Orthographic zoom (adjusting `size` property) with configurable min/max limits. Optionally restricts vertical panning when not zoomed in.

### Shake
Camera shake for impact feedback (damage, explosions). Driven by events — not self-polling.

### Boundary Clamping
Axis-aligned world bounds on the ground plane (XZ). If the view is larger than the map at max zoom-out, optionally centers the camera.

## What It Does NOT Do

- Know about game state or MatchState
- Respond to SimEvents (shake could be triggered by EntityManager or directly)
- Manage any other visual components
- Depend on EntityManager

## Configuration

| Export | Purpose | Default |
|--------|---------|---------|
| `pan_speed` | Base pan speed | 20.0 |
| `keyboard_pan_enabled` | Enable WASD/arrow panning | true |
| `mouse_pan_enabled` | Enable mouse drag panning | true |
| `touch_pan_enabled` | Enable touch drag panning | true |
| `map_rect_xz` | World boundaries | (-50,-40) 100x80 |
| `default_ortho_size` | Starting zoom level | 40.0 |
| `min_ortho_size` / `max_ortho_size` | Zoom limits | 20.0 / 50.0 |
| `edge_pan_enabled` | Enable edge-of-screen panning | true |

## Dependencies

| Direction | Component | Relationship |
|-----------|-----------|-------------|
| None | — | Standalone; no dependencies on other View components |

## Today

`camera_controller_3d.gd` already functions as a standalone controller. The main rename and architectural change is:
- Rename from `CameraController3D` to `BattleCamera` (role-based naming)
- No structural refactoring needed — it's already well-isolated
