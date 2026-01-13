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

#### Target Points
Shows where projectiles aim on each unit:
- **Green sphere**: Unit has explicit `ProjectileTargetPoint` marker
- **Orange sphere**: Fallback calculation (50% of sprite height)

Sphere size is fixed (0.5 units) for visibility - not related to hitbox size.

#### Hitboxes
Shows each unit's `CollisionRadius` as a blue sphere centered on the unit's body.

- Sphere size = unit's `CollisionRadius` property (varies per unit)
- Position = 50% of sprite height (body center)
- Used for: spawn spacing, unit separation, targeting

**Note**: Collision is **2D** (X/Z plane only). The sphere visualization is approximate - actual collision ignores Y height.

### Performance Counters

Displays real-time metrics (only when panel is visible):
- Active unit count
- Summoner lookups per frame
- Spatial grid queries (total and per-unit)
- Target acquisitions per frame
- Physics processing time (total and per-unit in microseconds)

## Files

- `scripts/debug/debug_menu.gd` - Main debug menu autoload
- `scripts/csharp/Units/Unit3D.cs` - Target point and hitbox visualization
- `scripts/csharp/Systems/SpatialGrid.cs` - Grid visualization and perf counters
