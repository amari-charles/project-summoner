# Framerate Independence

This document describes the patterns and conventions for writing framerate-independent code in Fateforged.

## Overview

All gameplay code must run consistently regardless of the device's frame rate. A player on a 30 FPS mobile device should have the same gameplay experience as someone on a 144 Hz gaming monitor.

## Key Concepts

### Delta Time

Godot passes `delta` to `_process()` and `_physics_process()`, representing elapsed time since the last frame (in seconds). Always multiply time-dependent values by delta.

### Physics Interpolation

Physics interpolation is **enabled** in this project (`project.godot`). This smooths 3D object motion between physics ticks, eliminating jitter when render FPS exceeds physics FPS.

## Code Patterns

### Movement (CharacterBody3D)

For Godot 4's CharacterBody, `move_and_slide()` handles delta internally. Set velocity in units/sec, don't multiply by delta:

```gdscript
# CORRECT - velocity is units/sec, move_and_slide handles delta
func _physics_process(delta: float) -> void:
    velocity = direction * move_speed
    move_and_slide()

# WRONG - double delta multiplication
func _physics_process(delta: float) -> void:
    velocity = direction * move_speed * delta  # DON'T DO THIS
    move_and_slide()
```

### Direct Position Updates

When modifying position directly, always multiply by delta:

```gdscript
# CORRECT
func _physics_process(delta: float) -> void:
    global_position += direction * speed * delta

# WRONG - speed varies with framerate
func _physics_process(delta: float) -> void:
    global_position += direction * speed  # DON'T DO THIS
```

### Acceleration and Forces

Apply delta to forces and acceleration:

```gdscript
# CORRECT - gravity is units/sec²
func _physics_process(delta: float) -> void:
    velocity.y += gravity * delta
    velocity = move_and_slide()
```

### Timers and Cooldowns

Use time-based accumulators, not frame counts:

```gdscript
# CORRECT - time-based cooldown
var attack_cooldown: float = 0.0
const ATTACK_COOLDOWN_DURATION: float = 0.5

func _physics_process(delta: float) -> void:
    attack_cooldown = max(attack_cooldown - delta, 0.0)

    if attack_cooldown <= 0.0 and should_attack:
        perform_attack()
        attack_cooldown = ATTACK_COOLDOWN_DURATION

# WRONG - frame-based cooldown
var cooldown_frames: int = 30  # DON'T DO THIS
func _physics_process(_delta: float) -> void:
    cooldown_frames -= 1
```

### Tick Rates and Intervals

For periodic events, accumulate time:

```gdscript
# CORRECT - time-based tick
var tick_timer: float = 0.0
const TICK_RATE: float = 0.5  # Every 0.5 seconds

func _physics_process(delta: float) -> void:
    tick_timer -= delta
    if tick_timer <= 0.0:
        _on_tick()
        tick_timer = TICK_RATE
```

### Animations and Tweens

Use Godot's Tween system - it's automatically framerate-independent:

```gdscript
# CORRECT - Tween handles timing
var tween: Tween = create_tween()
tween.tween_property(sprite, "modulate:a", 0.0, 0.5)  # 0.5 seconds

# For AnimationPlayer, use time-based playback (default behavior)
animation_player.play("attack")
```

### Async Timers

Use `create_timer()` for delays:

```gdscript
# CORRECT - real-time delay
await get_tree().create_timer(0.5).timeout
_do_something()
```

## Testing

### FPS Test Tool

Press **F4** to toggle the FPS test panel overlay. The panel shows:
- Real-time FPS counter
- Current target FPS setting
- Clickable buttons for each FPS preset

Hotkeys (work even when panel is hidden):

| Key | Action |
|-----|--------|
| ` or F12 | Toggle FPS panel |
| F5 | 30 FPS (low-end mobile) |
| F6 | 60 FPS (standard) |
| F7 | 120 FPS (high refresh) |
| F8 | Uncapped |

### What to Verify

1. **Movement speed** - Units should travel the same distance per second at all FPS
2. **Attack timing** - Cooldowns should last the same real-world duration
3. **Animations** - Should play at consistent speed
4. **Mana regeneration** - Should fill at the same rate
5. **Visual smoothness** - Higher FPS should look smoother, not faster

## Common Mistakes

### Don't: Pre-multiply velocity for move_and_slide

```gdscript
# WRONG in Godot 4
velocity = direction * speed * delta
move_and_slide()  # This will be way too slow
```

### Don't: Use frame counts for timing

```gdscript
# WRONG
if frame_counter % 60 == 0:  # "Every second" at 60 FPS, but wrong at other FPS
    spawn_enemy()
```

### Don't: Assume a specific framerate

```gdscript
# WRONG
const SPEED: float = 5.0  # "5 pixels per frame" assumption
position.x += SPEED
```

### Do: Think in units per second

```gdscript
# CORRECT
const SPEED: float = 300.0  # 300 pixels per second
position.x += SPEED * delta
```

## Reference

- Project physics tick: 60 Hz (default)
- Physics interpolation: Enabled
- Render FPS: Variable (device-dependent)
