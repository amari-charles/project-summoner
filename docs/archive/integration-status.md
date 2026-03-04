# Foundational Systems Integration Status

## ✅ C# Migration (Completed)

The core combat systems have been migrated from GDScript to C# for better type safety and performance.

### Migrated Systems

| System | GDScript (Old) | C# (New) |
|--------|----------------|----------|
| Unit3D | `scripts/units/unit_3d.gd` | `scripts/csharp/Units/Unit3D.cs` |
| DamageSystem | `scripts/combat/damage_system.gd` | `scripts/csharp/Battle/Simulation/Combat/DamageSystem.cs` |
| SpatialGrid | `scripts/spatial/spatial_grid.gd` | `scripts/csharp/Systems/SpatialGrid.cs` |
| CombatEvent | `scripts/combat/combat_event.gd` | `scripts/csharp/Battle/Simulation/Combat/CombatEvent.cs` |

### GDScript → C# Interop Patterns

When calling C# methods from GDScript:

```gdscript
# For autoloads, use get_node() to access the instance
var damage_system: Node = get_node("/root/DamageSystem")
damage_system.ApplyDamage(source, target, damage, damage_type)

# Godot 4 auto-converts between snake_case and PascalCase, so both work:
# damage_system.ApplyDamage() and damage_system.apply_damage() are equivalent
```

**Key Learnings:**
1. C# methods with default parameters don't work from GDScript (Godot bug #59025) - use explicit overloads
2. Godot 4 auto-converts PascalCase to snake_case for GDScript - no aliases needed
3. Access autoloads via `get_node("/root/Name")` for instance methods

### C# Project Files

- `Fateforged.csproj` - C# project file
- `Fateforged.sln` - Solution file
- `scripts/csharp/` - All C# source code

---

## ✅ Completed Integrations

### 1. DamageSystem
- **Status:** ✅ Migrated to C#
- **Changes:**
  - All code uses `DamageSystem.Instance.ApplyDamage()` (Godot 4 auto-converts for GDScript callers)
  - Combat events emitted via signals
  - Summoner trait bonuses applied automatically
- **Files:**
  - `scripts/csharp/Battle/Simulation/Combat/DamageSystem.cs`
  - `scripts/csharp/Battle/Simulation/Combat/CombatEvent.cs`

### 2. HPBarManager
- **Status:** ✅ Fully Integrated
- **Changes:**
  - HP bars auto-spawn when units spawn (`_ready()`)
  - HP bars auto-remove when units die (`_die()`)
  - HP bars react to `hp_changed` signal
- **Files Modified:**
  - `scripts/units/unit_3d.gd`

### 3. ProjectileManager
- **Status:** ✅ Fully Integrated
- **Changes:**
  - Added `projectile_id` export variable to Unit3D
  - Updated `_spawn_projectile()` to use ProjectileManager
  - Backwards compatible with old `projectile_scene` system
  - Archer unit updated to use `projectile_id = "arrow"`
- **Files Modified:**
  - `scripts/units/unit_3d.gd`
  - `scenes/battle/units/archer_3d.tscn`

## 🔧 Bug Fixes Applied

1. ✅ Autoload class_name conflicts (removed class_name from singletons)
2. ✅ Animation sprite references (updated for Character2D5Component)
3. ✅ Projectile type string-to-enum conversion
4. ✅ Typed array assignments (tags, footstep_frames)
5. ✅ AI loader syntax error (removed stray "Ok" text)
6. ✅ `.has()` method calls on nodes (changed to `"property" in node`)

## 📊 Systems Overview

### Active Autoloads
- `ProjectileCatalog` - Projectile data loading from JSON (C#)
- `VFXManager` - Visual effects with pooling
- `DamageSystem` - Centralized damage/healing
- `HPBarManager` - 3D health bars
- `ProjectileService` - Data-driven projectiles (C#)

### Current Data Files
```
data/
├── animations/
│   └── orc_animations.json
├── cards/
│   ├── archer_card.json
│   ├── fireball_card.json
│   ├── training_dummy_card.json
│   ├── wall_card.json
│   └── warrior_card.json
├── projectiles/
│   └── arrow.json
└── units/
    ├── archer.json
    ├── training_dummy.json
    ├── wall.json
    └── warrior.json
```

## 🎮 How Units Work Now (C#)

### Unit Spawning
```csharp
// Unit3D._Ready()
public override void _Ready()
{
    CurrentHp = MaxHp;
    SetupGroups();
    SpatialGrid.Instance?.RegisterUnit(this);  // ← Spatial partitioning
    hpBarManager?.Call("create_bar_for_unit", this);  // ← HP bar (still GDScript)
}
```

### Unit Attacking
```csharp
// Melee attack
protected void DealDamageTo(Node3D target)
{
    DamageSystem.Instance?.ApplyDamage(this, target, AttackDamage, "physical");  // ← C# direct call
}

// Ranged attack (via ProjectileManager, still GDScript)
protected void SpawnProjectile()
{
    ProjectileManager.Call("spawn_projectile", projectileId, this, target, damage, "physical");
}
```

### Unit Taking Damage
```csharp
public void TakeDamage(float amount, string damageType)
{
    if (!IsAlive || IsDying) return;
    OnTakeDamage(amount, damageType);
}

protected virtual void OnTakeDamage(float amount, string damageType)
{
    CurrentHp = Mathf.Max(CurrentHp - amount, 0);
    EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);  // ← HP bar updates
    if (CurrentHp <= 0) Die();
}
```

### Unit Death
```csharp
protected void Die()
{
    IsDying = true;
    IsAlive = false;
    SpatialGrid.Instance?.UnregisterUnit(this);  // ← Cleanup from spatial grid
    hpBarManager?.Call("remove_bar_from_unit", this);  // ← HP bar cleanup
    OnDeath();  // ← Virtual hook for subclasses
}
```

## ⏳ Pending Integrations (Optional)

### UnitAnimationController
- **Status:** ⏳ Not Yet Integrated
- **Why Optional:** Current animation system works
- **What it Adds:**
  - Frame-based events (damage on frame 3)
  - Priority system (attack can't be interrupted)
  - Auto-transitions between states
  - VFX/audio on animation events
- **Integration Steps:**
  1. Add UnitAnimationController as child node to unit scenes
  2. Create animation configs for soldier, archer, wall
  3. Replace `_update_animation()` calls with controller

### ProjectileCatalog (C#)
- **Status:** ✅ Integrated
- **What it Does:**
  - Loads projectile definitions from JSON (`data/projectiles/*.json`)
  - Used by ProjectileService and Unit3D for projectile spawning
  - Single source of truth for projectile data (no GDScript/C# duplication)
- **Note:** Card and unit data are managed by CardCatalog (C#). The dual catalog system was consolidated in PR #77, and ProjectileCatalog was migrated from GDScript to C# to eliminate property desync bugs.

## 🧪 Testing Checklist

To test the integrated systems:

1. **Launch a battle:**
   - Scene: `scenes/battle/battlefield/campaign_battle_3d.tscn`
   - Or: Run any campaign battle from main menu

2. **Verify DamageSystem:**
   - [ ] Units take damage when hit
   - [ ] Damage numbers are reasonable
   - [ ] Units die when HP reaches 0

3. **Verify HPBarManager:**
   - [ ] HP bars appear above units
   - [ ] HP bars face camera (billboard)
   - [ ] HP bars change color (green → yellow → red)
   - [ ] HP bars update when unit takes damage
   - [ ] HP bars disappear when unit dies

4. **Verify ProjectileManager:**
   - [ ] Archer fires arrows
   - [ ] Arrows fly toward target
   - [ ] Arrows hit and deal damage
   - [ ] Arrows disappear after hitting

## 📝 Known Limitations

1. **Animation System:** Still using manual `_update_animation()` calls
   - Not critical, works fine
   - Can integrate UnitAnimationController later for advanced features

2. **VFX Library:** Empty (`resources/vfx/` has no effects yet)
   - VFXManager works, just no effects defined
   - Can add VFXDefinition resources later

3. **Projectile Visuals:** Arrow projectile has no visual mesh yet
   - Projectile spawns and moves correctly
   - Just invisible until visual scene is added

4. **Base Damage:** Bases still use old damage system
   - Unit3D is updated
   - Base classes need similar integration

## 🚀 Next Steps

### Immediate
1. Test in-game to verify all systems work
2. Fix any runtime errors discovered

### Short-term
1. Add visual mesh to arrow projectile
2. Integrate DamageSystem into Base classes
3. Create VFX effects (explosion, hit impact, etc.)

### Long-term
1. Integrate UnitAnimationController for advanced animations
2. Create more projectile types (homing, arc, ballistic)
3. Add more units via JSON files
