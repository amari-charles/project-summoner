# Debug Menu

Development utility panel for testing and debugging. Only active in debug builds.

## Access

- **Toggle UI**: ` (backtick) or F12
- **Autoload**: `DebugMenu` (scripts/debug/debug_menu.gd)

## Features

### FPS Controls

| Button | Hotkey | Description |
|--------|--------|-------------|
| 30 FPS | F5 | Low-end mobile simulation |
| 60 FPS | F6 | Standard refresh rate |
| 120 FPS | F7 | High refresh rate |
| Uncapped | F8 | No FPS limit |

### Debug Toggles

#### Grid Lines
Visualizes the SpatialGrid cells used for unit proximity queries.

#### Skip Prep Phase
Immediately transitions from prep phase to battle phase. Useful for testing combat without waiting.

#### Hurtboxes
Shows each unit's combat hit detection volume as a green capsule.

- Capsule radius = unit's `HurtboxRadius` property (defaults to 0.5 if not set)
- Capsule height = sprite height (calculated from visual component)
- Used for: projectile collision detection, attack hit detection

#### Separation Radius
Shows each unit's movement separation radius as a purple circle on the ground.

- Circle radius = unit's `SeparationRadius` property
- Used for: unit-to-unit spacing during movement, preventing overlap
- This is separate from HurtboxRadius - units can have different values for combat hit detection vs movement spacing

#### Target Points
Shows where projectiles aim on each unit:
- **Orange sphere**: Calculated center-mass position (50% of sprite height)

Sphere radius is 0.3 units for visibility. Useful for debugging projectile targeting accuracy.

#### Attack Ranges
Shows each unit's attack range as a yellow shape on the ground.

- **Full circle**: Units without attack constraints (melee units, etc.)
- **Cone/wedge**: Units with cone constraints (e.g., Puff) - shows the actual attackable arc
- Radius = `GetEffectiveAttackRange()` (varies by unit type)
- Cone rotates with unit facing direction
- Position = ground level (Y = 0.05) centered on unit

Useful for debugging range issues (e.g., Fire Titans unable to attack each other) and understanding cone constraints.

#### Camera Overlay
Shows camera pan bounds directly on the battlefield.

- Toggle source: Debug Menu button (`Camera Overlay`)
- Overlay lives in `CameraController3D` and is debug-build only
- Green rectangle = configured map bounds (`map_rect_xz`)
- Red rectangle = current camera ground footprint (optional via `debug_show_camera_footprint_overlay`)

This is intended as a temporary validation aid when adjusting map bounds and pan clamp behavior.

### Performance Counters

Displays real-time metrics (only when panel is visible):
- Active unit count
- Summoner lookups per frame
- Spatial grid queries (total and per-unit)
- Target acquisitions per frame
- Physics processing time (total and per-unit in microseconds)

## Files

- `scripts/debug/debug_menu.gd` - Main debug menu autoload
- `scripts/csharp/Battle/View/UnitVisual.cs` - Unit debug markers (hurtbox, target point, attack range, separation radius)
- `scripts/csharp/Debug/BattlefieldDebugService.cs` - C# autoload bridge for debug flags used by GDScript + C#
- `scripts/csharp/Battle/Simulation/Subsystems/SimSpatialGrid.cs` - Grid visualization and perf counters
- `scripts/battle/battlefield/camera_controller_3d.gd` - Camera clamp math + pan bounds overlay

## Architecture Notes

- `DebugMenu` talks to `BattlefieldDebug` autoload directly (no `/root/...` lookup required in this Node script).
- `BattlefieldDebugService` owns shared debug flag state.
- `UnitVisual` reads those flags each frame and creates/frees marker meshes as needed.
- Camera bounds are computed once by battlefield setup and passed into `CameraController3D` via `set_map_bounds(...)` (single source of truth).
