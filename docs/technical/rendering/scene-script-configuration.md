# Scene vs Script Configuration Standard

## Problem Statement

Configuration conflicts arise when properties are set in both scene files (.tscn) and scripts (.gd), leading to:
- **Hidden overrides**: Scripts silently override scene values
- **Configuration drift**: Two sources of truth become out of sync
- **Maintenance burden**: Changes required in multiple locations
- **Debugging difficulty**: Unclear which system controls behavior

## Core Principle: Scripts as Source of Truth

Based on official Godot best practices, **scripts should be the primary source of configuration** through @export variables, with scene files only storing per-instance overrides.

### Why This Approach?

1. **Performance**: Scenes use declarative data (serialized) processed faster than imperative code
2. **Visibility**: @export variables appear in inspector, making overrides explicit
3. **Maintainability**: Single source of truth prevents configuration drift
4. **Godot Pattern**: Aligns with how Godot manages scene inheritance

## The Rules

### Rule 1: Use @export for Configurable Properties

Properties that might need per-instance customization should be @export in the script:

```gdscript
# GOOD - Configurable via inspector
@export var radius: float = 4.0
@export var offset_y: float = 3.2
@export var indicator_color: Color = Color(1.0, 0.5, 0.0, 0.6)
```

Scene files can then override these values in the inspector for specific instances.

### Rule 2: Use Constants for Fixed Values

Properties that should never be customized use const:

```gdscript
# GOOD - Fixed value, not configurable
const BASE_HEIGHT: float = 3.2
const MANA_DARK: Color = Color(0.3, 0.5, 0.8, 1.0)
```

These should NEVER appear in scene files.

### Rule 3: Scripts Control Dynamic Properties

If a script modifies a property at runtime (position, rotation, scale), the scene file must NOT set that property:

```gdscript
# GOOD - Script owns dynamic positioning
func _on_impact() -> void:
    var ground_pos: Vector3 = target_position
    ground_pos.y = 0.01  # Controlled by script
    aoe_indicator.global_position = ground_pos
```

Corresponding scene file:
```gdscript
# GOOD - No transform/position set, script controls it
[node name="AOEIndicator" type="MeshInstance3D" parent="."]
visible = false
mesh = SubResource("QuadMesh_aoe")
```

### Rule 4: Add Ownership Comments

Clarify which system controls what:

```gdscript
# In script
# Position just above ground to prevent z-fighting (script controls Y position)
var ground_pos: Vector3 = target_position
ground_pos.y = 0.01  # Slightly above ground, same as shadow components
```

### Rule 5: Billboard Unit Sprites Must Participate in Depth

For 2.5D unit body billboards (`Sprite3D`) that share screen space with projected silhouette shadows,
set `alpha_cut = 2` (discard) in the scene resource.

This is required so opaque body pixels write stable depth and correctly occlude the shadow pass,
especially at far camera distances where depth precision is lower.

See `shadow-system.md` for profile/preset architecture and override flow.

## Anti-Patterns to Avoid

### Anti-Pattern 1: Duplicate Configuration

```gdscript
# BAD - Scene sets transform offset
[node name="AOEIndicator" type="MeshInstance3D" parent="."]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0.01, 0)

# BAD - Script also sets position, overriding scene
func _on_impact() -> void:
    aoe_indicator.global_position.y = 0.0  # Overrides scene's 0.01
```

**Problem**: Scene's 0.01 offset is silently ignored, creating confusion.

### Anti-Pattern 2: Unused Scene Visuals

```gdscript
# BAD - Scene defines meshes that are never used
[node name="Background" type="MeshInstance3D" parent="."]
mesh = SubResource("QuadMesh_bg")

# BAD - Script creates completely different visuals
func _ready() -> void:
    _create_sprite_visuals()  # Ignores scene meshes
```

**Problem**: Scene file contains wasted configuration.

### Anti-Pattern 3: Scene Sets Dynamic Properties

```gdscript
# BAD - Scene sets rotation that script recalculates
[node name="Sprite" type="Sprite3D" parent="."]
rotation_degrees = Vector3(0, 45, 0)

# BAD - Script overrides it every frame
func _process(delta: float) -> void:
    sprite.rotation.y = _calculate_rotation()
```

**Problem**: Scene value is immediately replaced and never used.

## Implementation Examples

### Example 1: AOE Indicator (Correct Pattern)

**Script** (`fireball_spell_vfx.gd`):
```gdscript
@export var indicator_linger_duration: float = 2.0

func _on_impact() -> void:
    if aoe_indicator:
        # Script controls positioning
        var ground_pos: Vector3 = target_position
        ground_pos.y = 0.01  # Prevent z-fighting
        aoe_indicator.global_position = ground_pos
        aoe_indicator.visible = true
```

**Scene** (`fireball_spell.tscn`):
```gdscript
[node name="AOEIndicator" type="MeshInstance3D" parent="."]
visible = false
rotation_degrees = Vector3(-90, 0, 0)  # Static orientation
mesh = SubResource("QuadMesh_aoe")
# Note: No position/transform - controlled by script
```

### Example 2: HP Bar (Correct Pattern)

**Script** (`floating_hp_bar.gd`):
```gdscript
@export var bar_width: float = 3.0
@export var bar_height: float = 0.5
@export var offset_y: float = 3.2

const HP_FULL: Color = Color.GREEN
const HP_LOW: Color = Color.RED

func _ready() -> void:
    _create_sprite_visuals()  # Script creates all visuals
```

**Scene** (`floating_hp_bar.tscn`):
```gdscript
[node name="FloatingHPBar" type="Node3D"]
script = ExtResource("1_script")
# Scene is minimal - script creates all visuals at runtime
```

### Example 3: Card Positioning (Correct Pattern)

**Script** (`hand_ui.gd`):
```gdscript
const CARD_WIDTH: float = 120
const CARD_SPACING: float = 10

func _update_hand_layout() -> void:
    for i in range(cards.size()):
        var x_pos: float = start_x + i * (CARD_WIDTH + CARD_SPACING)
        cards[i].position = Vector2(x_pos, 10)
```

**Scene** (`card_visual.tscn`):
```gdscript
# Scene is just a template - hand_ui controls all positioning
[node name="CardVisual" type="Control"]
# No position set - controlled by parent hand_ui
```

## Migration Guide

When fixing existing scene/script conflicts:

1. **Identify the conflict**: Look for properties set in both places
2. **Determine ownership**: Who should control it (script or scene)?
3. **Apply the rules**:
   - Dynamic/runtime properties → Script only
   - Configurable properties → @export in script, override in scene if needed
   - Fixed values → const in script, nothing in scene
4. **Remove duplication**: Delete the redundant configuration
5. **Add comments**: Document the decision
6. **Test**: Verify behavior matches expectations

## Quick Reference

| Property Type | Script | Scene File |
|--------------|--------|-----------|
| Dynamic (position, rotation at runtime) | Set in code | Nothing (or default only) |
| Configurable (radius, color, offset) | @export var | Override in inspector if needed |
| Fixed (constants) | const | Nothing |
| Visual hierarchy (child nodes) | Nothing | Define structure |
| Static transforms (orientation) | Nothing | Set in scene |

## Real-World Fixes

### Fix 1: Fireball AOE Indicator Clipping

**Problem**: Indicator at y=0 was clipping with ground plane.

**Root Cause**: Scene set `transform.y = 0.01`, script overrode with `global_position.y = 0.0`.

**Solution**:
- Script now sets `ground_pos.y = 0.01`
- Scene removed transform offset
- Single source of truth: script

### Fix 2: Floating HP Bar Unused Meshes

**Problem**: Scene defined MeshInstance3D nodes that script never used.

**Root Cause**: Script creates Sprite3D visuals, ignoring scene meshes.

**Solution**:
- Removed Background and Bar MeshInstance3D from scene
- Scene now minimal (just root Node3D with script)
- Script fully owns visual creation

## See Also

- [Godot Best Practices - Scenes and Scripts](https://docs.godotengine.org/en/stable/getting_started/step_by_step/scenes_and_nodes.html)
- [GDScript Exports](https://docs.godotengine.org/en/stable/tutorials/scripting/gdscript/gdscript_exports.html)
