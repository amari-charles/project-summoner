# VFX and Pooling Best Practices

**Created**: 2025-01-16
**Updated**: 2025-11-28
**Purpose**: Document lessons learned from fireball spell debugging to avoid similar issues in the future

---

## Table of Contents

1. [Pool Container Architecture](#pool-container-architecture)
2. [Resources vs Nodes for Timing Logic](#resources-vs-nodes-for-timing-logic)
3. [VFX Pooling State Management](#vfx-pooling-state-management)
4. [VFX Lifecycle in Pooled Systems](#vfx-lifecycle-in-pooled-systems)
5. [Debugging Approach](#debugging-approach)

---

## Pool Container Architecture

### The Problem

Pooled objects stored in arrays outside the scene tree are considered "orphaned nodes" by Godot. This causes:
- Test frameworks (GUT) reporting orphan warnings
- Objects not properly cleaned up when the manager exits
- Nodes existing outside the scene tree lifecycle

### The Solution: Pool Container Pattern

Keep pooled objects **in the scene tree** by adding them to a dedicated container node:

```gdscript
var effects_container: Node3D = null  ## Parent for active effects
var pool_container: Node3D = null     ## Parent for pooled effects

func _ready() -> void:
    # Container for active effects (visible, in use)
    effects_container = Node3D.new()
    effects_container.name = "VFXContainer"
    add_child(effects_container)

    # Container for pooled effects (hidden, waiting for reuse)
    pool_container = Node3D.new()
    pool_container.name = "VFXPool"
    add_child(pool_container)
```

### Pool Lifecycle with Containers

```
┌─────────────────────────────────────────────────────────────┐
│ POOL INITIALIZATION                                          │
│ 1. Create instance                                           │
│ 2. instance.visible = false  ← Hide while pooled             │
│ 3. pool_container.add_child(instance)  ← In scene tree       │
│ 4. pool_array.append(instance)                               │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│ RETRIEVAL (get from pool)                                    │
│ 1. instance = pool_array.pop_back()                          │
│ 2. pool_container.remove_child(instance)  ← Remove from pool │
│ 3. instance.visible = true  ← Show for use                   │
│ 4. effects_container.add_child(instance)  ← Add to active    │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│ RETURN (back to pool)                                        │
│ 1. effects_container.remove_child(instance)                  │
│ 2. instance.reset()                                          │
│ 3. instance.visible = false  ← Hide while pooled             │
│ 4. pool_container.add_child(instance)  ← Back in pool        │
│ 5. pool_array.append(instance)                               │
└─────────────────────────────────────────────────────────────┘
```

### Benefits

1. **No orphan warnings** - All objects are in scene tree
2. **Automatic cleanup** - Scene tree frees children when manager exits
3. **Clear separation** - Active vs pooled objects in different containers
4. **Debuggable** - Can inspect pool contents in Remote scene tree

### Managers Using This Pattern

- `VFXManager` - pool_container for VFX instances
- `HPBarManager` - pool_container for floating HP bars
- `ProjectileManager` - pool_container for projectiles

---

## Resources vs Nodes for Timing Logic

### The Problem

**Resources cannot reliably handle timing logic with `await`.**

```gdscript
# ❌ BAD - This doesn't work reliably in a Resource
class_name Card extends Resource

func _cast_spell_3d(...):
    VFXManager.play_effect(...)
    var scene_tree: SceneTree = battlefield.get_tree()
    await scene_tree.create_timer(0.8).timeout  # ⚠️ Unreliable!
    _apply_damage(...)  # May never execute
```

### Why It Fails

1. **Resources are not part of the scene tree** - They have no guaranteed lifecycle
2. **The `await` callback becomes orphaned** - When the function returns, the Resource may go out of scope
3. **No error messages** - The code compiles, but the continuation after `await` may never execute
4. **Timing is unpredictable** - Even if it fires, it may execute in an invalid context

### The Solution

**Move timing-dependent logic into Nodes**, which have guaranteed scene tree lifecycle:

```gdscript
# ✅ GOOD - VFX (a Node) handles its own timing
class_name FireballSpellVFX extends VFXInstance

func _on_play():
    # Start descent animation
    var tween: Tween = create_tween()
    tween.tween_property(self, "global_position", target_pos, 0.8)
    tween.finished.connect(_on_impact)  # ✅ Reliable, Node-based timing

func _on_impact():
    _apply_aoe_damage()  # ✅ Executes reliably
```

### Architectural Principle

**Resources define WHAT (data, configuration)**
**Nodes define WHEN and HOW (timing, execution, animation)**

For the Card/VFX architecture:
- **Card (Resource)**: Defines spell damage, radius, which VFX to use
- **VFX (Node)**: Handles animation timing, applies damage at the right moment
- **Communication**: Card passes data to VFX via `receive_data()`, VFX handles the rest

---

## VFX Pooling State Management

### The Problem

Pooled VFX instances are reused multiple times. **Any state not properly reset will leak between uses**, causing bugs like:
- VFX appearing in wrong location
- Animations playing incorrectly
- Visual elements showing/hiding inconsistently

### Complete State Management Checklist

When implementing a pooled VFX, track and reset ALL of these:

#### ✅ State Management Checklist

- [ ] **All Tweens** (not just the obvious ones!)
  - Descent/movement tweens
  - Fade/alpha tweens
  - Scale/rotation tweens
  - Any other animated properties

- [ ] **Visibility States**
  - Main VFX node visibility
  - Child sprite visibility (AnimatedSprite3D, Sprite3D, etc.)
  - Indicator/overlay visibility (AOE indicators, rings, etc.)

- [ ] **Positions and Transforms**
  - `global_position`
  - `rotation`
  - `scale`
  - Target positions (for movement)

- [ ] **Custom Runtime Data**
  - Damage values
  - Team/faction
  - References to battlefield/targets
  - Radii, durations, colors

- [ ] **Animation States**
  - Stop all AnimatedSprite/AnimationPlayer instances
  - Reset animation frames to initial state

### Example: Tracking ALL Tweens

```gdscript
# ❌ BAD - Fade tween not tracked
func _on_impact():
    var fade_tween: Tween = create_tween()  # ⚠️ Local variable!
    fade_tween.tween_interval(2.0)
    fade_tween.finished.connect(func(): stop())

func _on_reset():
    if tween:  # Only kills descent tween
        tween.kill()
    # fade_tween still running! ⚠️
```

```gdscript
# ✅ GOOD - All tweens tracked
var tween: Tween = null
var fade_tween: Tween = null  # ✅ Instance variable

func _on_impact():
    if fade_tween:
        fade_tween.kill()  # ✅ Kill previous if exists

    fade_tween = create_tween()
    fade_tween.tween_interval(2.0)
    fade_tween.finished.connect(func(): stop())

func _on_reset():
    if tween:
        tween.kill()
        tween = null

    if fade_tween:  # ✅ Clean up fade tween too
        fade_tween.kill()
        fade_tween = null
```

### Example: Complete Reset Method

```gdscript
func _on_reset() -> void:
    # Kill ALL tweens
    if descent_tween:
        descent_tween.kill()
        descent_tween = null

    if fade_tween:
        fade_tween.kill()
        fade_tween = null

    # Reset visibility
    if animated_sprite:
        animated_sprite.stop()
        animated_sprite.visible = false

    if aoe_indicator:
        aoe_indicator.visible = false
        aoe_indicator.scale = Vector3.ONE

    # Reset positions
    target_position = Vector3.ZERO

    # Reset custom data
    spell_damage = 0.0
    spell_team = 0
    spell_battlefield = null
```

### Testing Your Pooling

**Mental test**: Trace through 3 complete cycles:

1. **Cast 1**: Fresh from pool → play → finish → reset → return to pool
2. **Cast 2**: Retrieve from pool → play → finish → reset → return to pool
3. **Cast 3**: Retrieve from pool → play → finish → reset → return to pool

At each "play" step, verify:
- Is ALL state from the previous cast cleared?
- Are ALL tweens killed?
- Is ALL visibility reset?
- Are ALL positions reset?

If **any** state leaks through, you'll see bugs on cast 2 or 3.

---

## VFX Lifecycle in Pooled Systems

### The `_ready()` Auto-Play Trap

**Problem**: `VFXInstance._ready()` automatically calls `play()`, which runs **before pooled instances have valid data**.

#### Bad Lifecycle (Old Code)

```gdscript
# VFXInstance._ready()
func _ready() -> void:
    play()  # ❌ Runs immediately when instance is created
```

**For pooled instances**:
1. VFXManager creates instance for pool → `_ready()` fires → `play()` runs
2. But instance has no position, no custom data, not in scene tree yet!
3. VFX plays with invalid/default data
4. Later, VFXManager sets data and calls `play()` again
5. But state from first invalid play() persists → bugs!

**Symptom**: First cast shows buggy behavior (stuck, wrong position, etc.)

#### Good Lifecycle (Fixed Code)

```gdscript
# VFXInstance._ready()
func _ready() -> void:
    # Don't auto-play for pooled instances - VFXManager will call play() after setup
    if not is_pooled:
        play()
```

**For pooled instances**:
1. VFXManager creates instance → `_ready()` fires → skips auto-play ✅
2. Instance added to pool, waits
3. Later: VFXManager retrieves instance, sets position, calls `receive_data()`, adds to scene, calls `play()`
4. Now `play()` runs with valid data ✅

**For non-pooled instances**: Still auto-plays immediately as before ✅

### Complete Pooled VFX Lifecycle

```
Pool Initialization Phase:
┌─────────────────────────────────────┐
│ 1. VFXManager._init_pools()         │
│ 2. instance = scene.instantiate()   │
│ 3. instance._ready() fires          │
│    → is_pooled = true                │
│    → skip auto-play() ✅            │
│ 4. instance.reset()                 │
│ 5. Add to pool array                │
└─────────────────────────────────────┘

First Use (and all subsequent uses):
┌─────────────────────────────────────┐
│ 1. VFXManager.play_effect()         │
│ 2. Get instance from pool           │
│ 3. Set global_position              │
│ 4. Call receive_data(custom_params) │
│ 5. Add to scene tree                │
│ 6. Call play() ← NOW it's safe ✅   │
│ 7. VFX animates...                  │
│ 8. finish() → effect_finished       │
│ 9. Remove from scene                │
│ 10. reset() → return to pool        │
└─────────────────────────────────────┘
```

### Key Principle

**Pooled instances must receive data BEFORE playing**

Order matters:
1. ✅ Set position → receive data → add to scene → play()
2. ❌ Create → play() immediately (has no data yet!)

---

## Debugging Approach

### What Went Wrong During Fireball Debugging

#### ❌ Incremental Patching Approach

1. "Fireball not doing damage" → Add await to delay damage
2. "await doesn't work" → Move damage to VFX
3. "VFX not visible on first cast" → Add visibility management
4. "AOE doesn't show every other cast" → Track fade tween
5. "Fireball stuck on first cast" → Fix _ready() auto-play

**Result**: 5 rounds of fixes for what should have been caught with proper architectural thinking

#### ✅ Architectural Approach

Should have started with:

1. **Step back and assess**: "This is a VFX pooling system, what are the architectural requirements?"
2. **List all state**: Position, tweens (ALL), visibility (ALL), custom data
3. **Design lifecycle**: Pool init → retrieval → setup → play → finish → reset → return
4. **Trace through 3 cycles**: First use, second use, third use
5. **Implement with checklist**: Verify each state element is properly managed

**Result**: Would have caught all 5 issues before writing any code

### Debugging Checklist for Pooled Systems

When debugging pooled VFX (or any pooled system):

#### 1. Identify ALL State
- [ ] List every mutable variable
- [ ] List every Tween (movement, fade, scale, etc.)
- [ ] List every visibility flag
- [ ] List every position/transform
- [ ] List every custom runtime parameter

#### 2. Trace Through Multiple Cycles
- [ ] Mentally simulate: Pool init
- [ ] Mentally simulate: First use (fresh from pool)
- [ ] Mentally simulate: Reset and return to pool
- [ ] Mentally simulate: Second use (reuse)
- [ ] Mentally simulate: Third use (verify consistency)

#### 3. Verify Lifecycle Timing
- [ ] When does _ready() fire?
- [ ] When does data get set?
- [ ] When does play() fire?
- [ ] Is data valid when play() fires?

#### 4. Check for Orphaned State
- [ ] Are ALL tweens killed in reset()?
- [ ] Is ALL visibility explicitly managed?
- [ ] Are ALL positions zeroed?
- [ ] Are ALL custom params cleared?

### Red Flags to Watch For

🚩 **Using `await` in a Resource class**
- Resources can't reliably handle timing
- Move timing logic to Nodes

🚩 **Creating Tweens as local variables**
- They won't be tracked for cleanup
- Use instance variables for ALL tweens

🚩 **Assuming default visibility states**
- Explicitly set visibility in play() and reset()
- Don't rely on scene defaults

🚩 **Auto-playing in _ready() for pooled instances**
- Pooled instances need data before playing
- Guard auto-play with `if not is_pooled`

🚩 **Not tracing through 3+ pooling cycles**
- First use may work by luck
- Bugs appear on reuse when state leaks
- Always test the second and third use

---

## Summary: Core Principles

1. **Resources define WHAT, Nodes define WHEN**
   - No timing logic in Resources (no `await`)
   - Timing belongs in Nodes with proper lifecycle

2. **Track ALL state for pooled objects**
   - Every tween (not just obvious ones)
   - Every visibility flag
   - Every position, transform, custom data
   - Use a checklist

3. **Design lifecycle before coding**
   - How does pool initialization work?
   - What's the order of setup?
   - When is data valid?
   - Trace through 3 complete cycles

4. **Pooled instances: data first, play second**
   - Don't auto-play in _ready() for pooled instances
   - Receive data → set position → add to scene → play()

5. **Test pooling with multiple cycles**
   - First use, reset, second use, reset, third use
   - State leaks appear on reuse, not first use

---

## Case Study: Fireball Spell

The fireball spell debugging session revealed all of these issues:

| Issue | Root Cause | Lesson |
|-------|------------|--------|
| No damage | `await` in Resource (Card.gd) | Resources can't handle timing |
| No VFX on first cast | Sprite visibility not managed | Explicitly manage ALL visibility |
| AOE alternates visibility | Fade tween not tracked | Track ALL tweens as instance vars |
| Stuck at top on first cast | _ready() auto-play before data set | Guard auto-play for pooled instances |

**Total debugging rounds**: 5
**What it should have been**: 0 (with proper architectural design upfront)

**Conclusion**: Architectural thinking upfront prevents multiple debugging rounds. Use checklists, trace through cycles, understand the system before patching symptoms.
