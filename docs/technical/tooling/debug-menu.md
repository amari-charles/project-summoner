# Debug Menu

Development utility panel for testing and debugging. Only active in debug builds.

## Access

- **Toggle UI**: ` (backtick) or F12
- **Autoload**: `DebugMenu` (scripts/debug/debug_menu.gd)

## Features

The panel is grouped by task and each tab scrolls independently:

| Tab | Purpose |
|-----|---------|
| Quick | Battle flow controls and frame-rate targets used repeatedly during playtesting |
| Arena | Curated debug battle lists, biome override, and direct battle launch |
| Visuals | Unit, projectile, rules, logging, and camera diagnostics |
| Tools | Command console, autocomplete, output, and profile snapshots |

### FPS Controls

| Button | Hotkey | Description |
|--------|--------|-------------|
| 30 FPS | F5 | Low-end mobile simulation |
| 60 FPS | F6 | Standard refresh rate |
| 120 FPS | F7 | High refresh rate |
| Uncapped | F8 | No FPS limit |

### Debug Toggles

#### Test Arena Map

`Open Debug Arena` opens the direct authored-battle chooser. Select one of the predefined debug battles to enter combat.

The `Debug Arena Battles` section launches those battles directly. Its `Arena List` selector filters the available launch buttons, while its `Biome` selector overrides the authored biome for the next direct launch. The selected list and biome persist in `user://debug_menu_settings.cfg`; authored event data is not changed.

#### Grid Lines
Visualizes the SpatialGrid cells used for unit proximity queries.

#### Skip Prep Phase
Immediately transitions from prep phase to battle phase. Useful for testing combat without waiting.

#### Hurtboxes
Shows each unit's combat hit detection volume as a green capsule.

- Capsule radius = `max(0.5, unit.SeparationRadius)` (current gameplay size proxy)
- Capsule height = sprite height (calculated from visual component)
- Used for: visualizing unit body volume

#### Separation Radius
Shows each unit's movement separation radius as a purple circle on the ground.

- Circle radius = unit's `SeparationRadius` property
- Used for: unit-to-unit spacing during movement and projectile first-contact sizing

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

#### Projectile Hit Geometry
Shows projectile gameplay hit volumes.

- Disc marker for `GroundCylinder` hit-space projectiles
- Sphere marker for `Sphere3D` hit-space projectiles
- AoE marker shown when projectile has `AoeRadius > 0`
- Helpful for validating projectile `hit_radius` and separation-radius edge contacts

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
- `scripts/csharp/Battle/View/ProjectileVisual.cs` - Projectile hit geometry debug markers
- `scripts/csharp/Debug/BattlefieldDebugService.cs` - C# autoload bridge for debug flags used by GDScript + C#
- `scripts/battle/battlefield/camera_controller_3d.gd` - Camera clamp math + pan bounds overlay

## Architecture Notes

- `DebugMenu` talks to `BattlefieldDebug` autoload directly (no `/root/...` lookup required in this Node script).
- `BattlefieldDebugService` owns shared debug flag state.
- `UnitVisual` reads those flags each frame and creates/frees marker meshes as needed.
- Camera bounds are computed once by battlefield setup and passed into `CameraController3D` via `set_map_bounds(...)` (single source of truth).
