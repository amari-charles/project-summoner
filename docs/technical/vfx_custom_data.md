# VFX Custom Data Passing

## Overview

VFXManager supports passing runtime data to VFX instances via the `data` Dictionary parameter in `play_effect()`. This allows gameplay code (Cards, abilities, etc.) to customize VFX behavior without hardcoding values in VFX scenes.

## Passing Data to VFX

When spawning a VFX, pass a dictionary of custom data:

```gdscript
# From Card, Ability, or other gameplay code
VFXManager.play_effect("my_vfx_id", spawn_position, {
    "radius": spell_radius,
    "damage": modified_damage,
    "color": element_color
})
```

## Receiving Data in VFX

Override the `receive_data()` method in your VFX script:

```gdscript
extends VFXInstance

@export var radius: float = 10.0  # Fallback default
@export var damage: float = 100.0

## Receive custom data from VFXManager
func receive_data(data: Dictionary) -> void:
    # Extract and validate radius
    if data.has("radius"):
        if data.radius is float or data.radius is int:
            radius = float(data.radius)
        else:
            push_warning("MyVFX: Invalid radius type")

    # Extract and validate damage
    if data.has("damage"):
        if data.damage is float or data.damage is int:
            damage = float(data.damage)
```

## Built-in Data Keys

VFXManager automatically applies these built-in Node3D properties:

| Key | Type | Description | Applied to |
|-----|------|-------------|-----------|
| `scale` | float | Uniform scale multiplier | `instance.scale` |
| `rotation` | Vector3 | Euler rotation (radians) | `instance.rotation` |

**Note:** Built-in properties are applied AFTER `receive_data()` is called.

## Timing Guarantees

The VFX data flow happens in this order:

1. VFX scene is instantiated (or retrieved from pool)
2. `_ready()` is called (may have already happened for pooled instances)
3. `global_position` is set by VFXManager
4. **`receive_data(data)` is called** ← Custom data applied HERE
5. Built-in properties (`scale`, `rotation`) are applied
6. VFX is added to scene tree
7. `play()` → `_on_play()` is called

**Safe zones for using overridden properties:**
- ✅ `_on_play()` and later
- ✅ Event handlers (signals, callbacks)
- ❌ `_ready()` - NOT safe (may run before `receive_data()`)

## Type Safety Best Practices

Always validate data types when receiving custom data:

```gdscript
func receive_data(data: Dictionary) -> void:
    if data.has("my_value"):
        # Accept multiple numeric types
        if data.my_value is float or data.my_value is int:
            my_value = float(data.my_value)
        elif data.my_value is String:
            my_value = float(data.my_value)  # Parse if needed
        else:
            push_warning("MyVFX: Invalid type for 'my_value': %s" % typeof(data.my_value))
            # Use fallback default (from @export)
```

## Example: Fireball Spell

**Gameplay code (Card.gd):**
```gdscript
# Spell has spell_radius = 10.0 in CardCatalog
VFXManager.play_effect("fireball_spell", position, {"radius": spell_radius})
```

**VFX code (fireball_spell_vfx.gd):**
```gdscript
@export var damage_radius: float = 10.0  # Fallback if not provided

func receive_data(data: Dictionary) -> void:
    if data.has("radius"):
        if data.radius is float or data.radius is int:
            damage_radius = float(data.radius)

func _on_impact() -> void:
    # Use damage_radius to size the AOE indicator
    var diameter: float = damage_radius * 2.0
    aoe_indicator.scale = Vector3(diameter / base_size, diameter / base_size, 1.0)
```

## Fallback Values

Always provide `@export` defaults for properties that might be overridden:

```gdscript
@export var radius: float = 10.0  ## Fallback radius (overridden by gameplay at runtime)
```

This ensures:
- VFX works standalone in editor
- VFX works if spawned without custom data
- Clear documentation of expected value ranges

## Common Patterns

### Pattern 1: Elemental Effects
```gdscript
# Pass element type to VFX
VFXManager.play_effect("elemental_burst", pos, {
    "element": ElementTypes.FIRE,
    "intensity": 1.5
})

# VFX changes color based on element
func receive_data(data: Dictionary) -> void:
    if data.has("element"):
        match data.element:
            ElementTypes.FIRE: particle_color = Color.ORANGE
            ElementTypes.ICE: particle_color = Color.CYAN
```

### Pattern 2: Scaled Damage Indicators
```gdscript
# Pass damage and max_hp to show relative size
VFXManager.play_effect("damage_number", pos, {
    "damage": actual_damage,
    "max_hp": target.max_hp
})

# VFX scales font size based on damage percentage
func receive_data(data: Dictionary) -> void:
    if data.has("damage") and data.has("max_hp"):
        var percent: float = data.damage / data.max_hp
        font_size = base_font_size * (1.0 + percent * 2.0)
```

### Pattern 3: Multi-Target Effects
```gdscript
# Pass array of target positions
VFXManager.play_effect("chain_lightning", origin, {
    "targets": [pos1, pos2, pos3]
})

# VFX creates lightning chain between positions
func receive_data(data: Dictionary) -> void:
    if data.has("targets") and data.targets is Array:
        target_positions = data.targets
```

## Migration from Hardcoded Properties

**Before (Old Pattern):**
```gdscript
# VFXManager had hardcoded:
if data.has("radius"):
    if "damage_radius" in instance:
        instance.set("damage_radius", data.radius)

# VFX relied on VFXManager's magic
@export var damage_radius: float = 10.0
```

**After (Formalized Pattern):**
```gdscript
# VFX explicitly receives data
func receive_data(data: Dictionary) -> void:
    if data.has("radius"):
        damage_radius = float(data.radius)
```

**Benefits:**
- Type safety controlled by VFX
- No VFXManager changes for new VFX types
- Clear contract for developers

## Troubleshooting

### Property Not Being Set

**Symptoms:** VFX uses fallback value instead of passed data

**Checklist:**
- [ ] Is `receive_data()` method defined in VFX script?
- [ ] Is data key spelled correctly? (`"radius"` not `"raduis"`)
- [ ] Is data type correct? (Use debugger or `push_warning()`)
- [ ] Is property being used before `_on_play()`? (timing issue)

### Type Mismatch Errors

**Symptoms:** Warning messages or incorrect values

**Solution:** Add type validation in `receive_data()`:
```gdscript
if data.my_value is float or data.my_value is int:
    my_property = float(data.my_value)
else:
    push_warning("Expected float, got: %s" % typeof(data.my_value))
```

### Pooled VFX Retains Old Data

**Symptoms:** Second spawn of VFX uses data from first spawn

**Solution:** Reset properties in `_on_reset()`:
```gdscript
func _on_reset() -> void:
    # Reset to fallback defaults
    damage_radius = 10.0
    element_type = ElementTypes.NEUTRAL
```

---

*Last Updated: 2025-01-15 - Initial formalization of VFX data passing pattern*
