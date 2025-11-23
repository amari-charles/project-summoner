# Modifier System Architecture

## Overview

The modifier system provides a flexible, data-driven framework for applying bonuses, penalties, and behaviors to cards, units, heroes, and other game entities. It supports:

- **Hero affinity bonuses** (fire hero boosts fire units)
- **Card interactions** (Solar Warrior doubles sun_blessed bonuses)
- **Runtime behaviors** (lifesteal, execute, double-cast)
- **Temporary buffs** (mid-battle status effects)
- **Extensibility** (add new modifier sources without refactoring)

**Design Philosophy:** Keep it simple. Use dictionaries for data, two-phase resolution for stats, and flags for behaviors. Extend only when needed.

---

## Core Concepts

### 1. Modifiers

A **modifier** is a dictionary that describes a change to apply to a target. Modifiers come from various sources:
- Heroes (affinity bonuses)
- Cards (self-modifying or buff-granting)
- Items (future)
- Temporary buffs (poison, strengthen)
- Map/battlefield effects (future)

**Modifier Structure:**
```gdscript
{
    "source": "fire_hero",              # Who/what provides this modifier
    "tags": ["sun_blessed"],            # Tags for amplification targeting
    "conditions": {                     # When this applies
        "elemental_affinity": "fire"
    },
    "stat_adds": {                      # Flat additions (applied first)
        "max_hp": 10,
        "attack_damage": 5
    },
    "stat_mults": {                     # Multiplicative bonuses (applied second)
        "max_hp": 1.3,                  # 30% increase
        "attack_damage": 1.2            # 20% increase
    },
    "flags": {                          # Behavior flags (checked by target)
        "has_lifesteal": true,
        "lifesteal_percent": 0.2
    },
    "priority": 10                      # Optional: higher = applied later
}
```

### 2. Tags

**Tags** are string identifiers attached to modifiers for categorization and amplification.

Examples:
- `"sun_blessed"` - fire hero's affinity tag
- `"earth_guardian"` - earth hero's affinity tag
- `"temporary"` - marks buffs that expire
- `"defensive"` - for grouping defensive bonuses

Tags enable generic amplification without hardcoding specific interactions.

### 3. Amplifiers

An **amplifier** is a special modifier that scales other modifiers by tag.

**Amplifier Structure:**
```gdscript
{
    "source": "solar_warrior_card",
    "amplify_tag": "sun_blessed",      # Which tag to amplify
    "factor": 2.0                       # Multiply bonuses by this
}
```

**Amplification Formula:**
```gdscript
# For a modifier with tag "sun_blessed" and 30% HP bonus:
base_bonus = 0.3
amplifier = 2.0
amplified_bonus = base_bonus * amplifier  # 0.6 (60% bonus)
final_mult = 1.0 + amplified_bonus         # 1.6x HP
```

**Multiple Amplifiers:**
If multiple amplifiers target the same tag, they stack multiplicatively:
```gdscript
# Two amplifiers: 2.0x and 1.5x
total_amplifier = 2.0 * 1.5  # 3.0x
final_bonus = 0.3 * 3.0       # 0.9 (90% bonus)
```

### 4. Categories

**Categories** are properties on cards/units used for condition matching.

**Example Card Categories:**
```gdscript
{
    "catalog_id": "warrior",
    "categories": {
        "elemental_affinity": "fire",
        "unit_type": "grounded",
        "card_category": "unit",
        "tags": ["melee", "tank"]
    }
}
```

Modifiers check categories via `conditions` field to determine if they apply.

---

## Resolution Algorithm

### Two-Phase Stat Calculation

Based on **Path of Exile's** proven approach: additive bonuses sum first, then multiplicative bonuses multiply.

**Phase 1: Collect Modifiers**
1. Gather all modifiers from all providers (hero, card, buffs, etc.)
2. Filter by conditions (does card match modifier's requirements?)
3. Apply amplification (adjust modifier values by tag)

**Phase 2: Sum Additive Bonuses**
```gdscript
for each stat (max_hp, attack_damage, etc.):
    total_add = 0
    for each modifier:
        if modifier.stat_adds.has(stat):
            total_add += modifier.stat_adds[stat]
```

**Phase 3: Multiply Multiplicative Bonuses**
```gdscript
for each stat:
    total_mult = 1.0
    for each modifier:
        if modifier.stat_mults.has(stat):
            # Convert mult to bonus: 1.3 → 0.3
            bonus = modifier.stat_mults[stat] - 1.0
            total_mult += bonus  # Additive within mult phase!
```

**Phase 4: Apply Final Values**
```gdscript
final_stat = (base_stat + total_add) * total_mult
```

**Example:**
```
Base HP: 100
Modifier 1: +10 HP (flat)
Modifier 2: +5 HP (flat)
Modifier 3: ×1.3 HP (30% bonus)
Modifier 4: ×1.2 HP (20% bonus)

Phase 2: 100 + 10 + 5 = 115
Phase 3: 115 * (1.0 + 0.3 + 0.2) = 115 * 1.5 = 172.5
Final: 172 HP
```

### Amplification Resolution

Applied **before** phase 2.

```gdscript
# Pseudo-code
func apply_amplification(modifiers: Array) -> Array:
    # Step 1: Find all amplifiers
    var amplifiers = {}
    for mod in modifiers:
        if mod.has("amplify_tag"):
            var tag = mod.amplify_tag
            if not amplifiers.has(tag):
                amplifiers[tag] = 1.0
            amplifiers[tag] *= mod.factor

    # Step 2: Amplify tagged modifiers
    for mod in modifiers:
        if mod.has("tags"):
            var total_amp = 1.0
            for tag in mod.tags:
                if amplifiers.has(tag):
                    total_amp *= amplifiers[tag]

            # Amplify bonuses (not base values)
            for stat in mod.stat_adds.keys():
                mod.stat_adds[stat] *= total_amp

            for stat in mod.stat_mults.keys():
                var bonus = mod.stat_mults[stat] - 1.0
                bonus *= total_amp
                mod.stat_mults[stat] = 1.0 + bonus

    return modifiers
```

---

## Behavior Flags

Flags are key-value pairs stored in modifiers that units check at runtime for conditional logic.

**Common Flags:**
- `has_lifesteal`: bool
- `lifesteal_percent`: float (0.0 - 1.0)
- `has_double_cast`: bool
- `execute_threshold`: float (HP % threshold)
- `execute_multiplier`: float (damage multiplier)
- `aoe_radius`: float
- `crit_chance`: float
- `fire_aura_radius`: float

**Flag Storage:**
Units merge all modifier flags into a single `active_modifiers` dictionary:

```gdscript
# unit_3d.gd
var active_modifiers: Dictionary = {}

func apply_modifiers(modifiers: Array):
    # ... stat calculation ...

    # Merge all flags
    for mod in modifiers:
        active_modifiers.merge(mod.get("flags", {}), true)
```

**Flag Usage:**
Units check flags during combat logic:

```gdscript
# Example: Lifesteal
func _deal_damage_to_target(target: Unit3D, damage: float):
    target.take_damage(damage)

    if active_modifiers.get("has_lifesteal", false):
        var percent = active_modifiers.get("lifesteal_percent", 0.2)
        heal(damage * percent)

# Example: Execute (double damage to low HP targets)
func calculate_damage(target: Unit3D) -> float:
    var dmg = attack_damage

    if active_modifiers.has("execute_threshold"):
        var threshold = active_modifiers["execute_threshold"]
        var multiplier = active_modifiers["execute_multiplier"]

        if target.current_hp <= target.max_hp * threshold:
            dmg *= multiplier

    return dmg

# Example: Double cast
func _perform_attack():
    _deal_damage_to_target(current_target, attack_damage)

    if active_modifiers.get("has_double_cast", false):
        await get_tree().create_timer(0.3).timeout
        _deal_damage_to_target(current_target, attack_damage)
```

**Why Flags Instead of Callbacks:**
- **Serializable:** Can save/load to disk
- **Debuggable:** Inspect dictionary in debugger
- **Readable:** Clear data, logic stays in unit code
- **Simple:** No complexity of dynamic code execution

---

## Complete Examples

### Example 1: Fire Hero Affinity

**Fire Hero Provides:**
```gdscript
{
    "source": "fire_hero",
    "tags": ["sun_blessed"],
    "conditions": {
        "elemental_affinity": "fire"
    },
    "stat_mults": {
        "max_hp": 1.3,          # +30% HP
        "attack_damage": 1.3    # +30% attack
    }
}
```

**Normal Fire Unit (Warrior):**
```
Base: 100 HP, 15 attack
After modifier: 130 HP, 19.5 attack
```

### Example 2: Solar Warrior + Fire Hero

**Solar Warrior Card Provides:**
```gdscript
{
    "source": "solar_warrior_card",
    "amplify_tag": "sun_blessed",
    "factor": 2.0
}
```

**Resolution:**
1. Fire hero provides 30% bonus tagged "sun_blessed"
2. Solar Warrior amplifies "sun_blessed" by 2.0×
3. Final bonus: 30% × 2.0 = 60%

**Result:**
```
Base: 100 HP, 15 attack
After amplified modifier: 160 HP, 24 attack
```

### Example 3: Execute Mechanic

**Executioner Hero Provides:**
```gdscript
{
    "source": "executioner_hero",
    "flags": {
        "execute_threshold": 0.5,   # 50% HP or less
        "execute_multiplier": 2.0   # Double damage
    }
}
```

**Unit Combat Logic:**
```gdscript
func calculate_damage(target: Unit3D) -> float:
    var dmg = attack_damage

    if active_modifiers.has("execute_threshold"):
        var threshold = active_modifiers["execute_threshold"]
        var multiplier = active_modifiers["execute_multiplier"]

        if target.current_hp <= target.max_hp * threshold:
            dmg *= multiplier
            print("EXECUTE! Damage doubled!")

    return dmg
```

**Result:**
- Attack enemy at 100/200 HP: normal damage
- Attack enemy at 80/200 HP: normal damage
- Attack enemy at 100/200 HP (50%): **double damage**

### Example 4: Combining Multiple Modifiers

**Active Modifiers:**
1. Fire hero: +30% HP/attack (sun_blessed)
2. Equipment: +20 flat HP
3. Map buff: +20% HP
4. Solar Warrior: 2× sun_blessed

**Resolution:**
```
Base: 100 HP

Phase 1: Amplification
- Fire hero 30% × solar warrior 2.0 = 60%

Phase 2: Additive
- Flat: +20 HP
- Total: 100 + 20 = 120 HP

Phase 3: Multiplicative
- Fire hero (amplified): +60% → 0.6
- Map buff: +20% → 0.2
- Sum: 1.0 + 0.6 + 0.2 = 1.8
- Total: 120 × 1.8 = 216 HP

Final: 216 HP
```

---

## Integration Points

### 1. Unit Spawn (card.gd)

When a card spawns a unit:

```gdscript
# card.gd
func _summon_unit_3d(position: Vector3, team: int, gameplay_layer: Node):
    var unit = unit_scene.instantiate() as Unit3D

    # Get modifiers from system
    var context = {
        "card_data": self,
        "hero_id": get_hero_id(),  # From ProfileRepo
        "team": team
    }
    var modifiers = ModifierSystem.get_modifiers_for("unit", categories, context)

    # Apply modifiers BEFORE adding to scene
    unit.apply_modifiers(modifiers, card_data)

    gameplay_layer.add_child(unit)
```

### 2. Unit Initialization (unit_3d.gd)

```gdscript
# unit_3d.gd
var base_max_hp: float
var base_attack_damage: float
var active_modifiers: Dictionary = {}

func _ready():
    # Store base stats
    base_max_hp = max_hp
    base_attack_damage = attack_damage

func apply_modifiers(modifiers: Array, card_data: Dictionary):
    # Phase 1: Amplification
    modifiers = _apply_amplification(modifiers)

    # Phase 2 & 3: Calculate final stats
    var stats = {
        "max_hp": base_max_hp,
        "attack_damage": base_attack_damage
    }

    # Sum additive bonuses
    for mod in modifiers:
        for stat in mod.get("stat_adds", {}).keys():
            stats[stat] += mod.stat_adds[stat]

    # Apply multiplicative bonuses
    for mod in modifiers:
        for stat in mod.get("stat_mults", {}).keys():
            var bonus = mod.stat_mults[stat] - 1.0
            stats[stat] *= (1.0 + bonus)

    # Apply final values
    max_hp = stats.max_hp
    attack_damage = stats.attack_damage
    current_hp = max_hp

    # Merge flags
    for mod in modifiers:
        active_modifiers.merge(mod.get("flags", {}), true)
```

### 3. ModifierSystem (Autoload)

Central service that collects and filters modifiers:

```gdscript
# modifier_system.gd (autoload)
extends Node

var _providers: Dictionary = {}  # provider_id -> ModifierProvider

func register_provider(id: String, provider: ModifierProvider):
    _providers[id] = provider

func get_modifiers_for(target_type: String, categories: Dictionary, context: Dictionary) -> Array:
    var all_mods = []

    # Collect from all providers
    for provider in _providers.values():
        all_mods.append_array(provider.get_modifiers())

    # Filter by target type and conditions
    var filtered = []
    for mod in all_mods:
        if _matches_conditions(mod, categories, context):
            filtered.append(mod)

    return filtered

func _matches_conditions(mod: Dictionary, categories: Dictionary, context: Dictionary) -> bool:
    var conditions = mod.get("conditions", {})
    for key in conditions.keys():
        if categories.get(key) != conditions[key]:
            return false
    return true
```

### 4. Hero Provider (hero_modifier_provider.gd)

```gdscript
class_name HeroModifierProvider extends RefCounted

var hero_id: String

func _init(id: String):
    hero_id = id

func get_modifiers() -> Array:
    var mods = []

    match hero_id:
        "fire_hero":
            mods.append({
                "source": "fire_hero",
                "tags": ["sun_blessed"],
                "conditions": {"elemental_affinity": "fire"},
                "stat_mults": {"max_hp": 1.3, "attack_damage": 1.3}
            })
        "earth_hero":
            mods.append({
                "source": "earth_hero",
                "tags": ["stone_guardian"],
                "conditions": {"elemental_affinity": "earth"},
                "stat_mults": {"max_hp": 1.5},
                "stat_adds": {"armor": 5}
            })

    return mods
```

### 5. Battle Initialization (game_controller_3d.gd)

```gdscript
func _ready():
    # Register hero provider
    var profile = ProfileRepo.get_active_profile()
    var hero_id = profile.get("meta", {}).get("selected_hero", "")

    if hero_id:
        var hero_provider = HeroModifierProvider.new(hero_id)
        ModifierSystem.register_provider("hero", hero_provider)
```

---

## Implementation Steps

### Phase 1: Core System
1. Create `ModifierSystem` autoload
2. Implement condition matching
3. Implement two-phase resolution algorithm
4. Add amplification logic

### Phase 2: Hero Integration
5. Create `HeroModifierProvider` class
6. Define hero modifiers for fire/earth/air/water
7. Register hero provider at battle start
8. Update `unit_3d.gd` to apply modifiers on spawn

### Phase 3: Card Categories
9. Add `categories` field to `card_catalog.gd`
10. Define categories for existing cards (warrior, archer, wall)
11. Pass categories when requesting modifiers

### Phase 4: Card Amplification
12. Add amplifier support to cards (e.g., Solar Warrior)
13. Test amplification math

### Phase 5: Behavior Flags
14. Add lifesteal flag and implementation
15. Add execute flag and implementation
16. Add double-cast flag and implementation

---

## Future Enhancements

### Temporary Buffs
Use same system, but add/remove modifiers dynamically:

```gdscript
# Add temporary buff
func apply_buff(unit: Unit3D, duration: float):
    var buff_mod = {
        "source": "strength_potion",
        "stat_adds": {"attack_damage": 10},
        "duration": duration
    }
    unit.active_modifiers_list.append(buff_mod)
    unit.recalculate_stats()

# Remove expired buffs
func _process(delta):
    for mod in active_modifiers_list:
        if mod.has("duration"):
            mod.duration -= delta
            if mod.duration <= 0:
                active_modifiers_list.erase(mod)
                recalculate_stats()
```

### Callbacks
If flags become limiting, add callback support:

```gdscript
{
    "flags": {
        "on_attack": func(attacker, target):
            # Custom logic here
            pass
    }
}
```

But avoid this unless truly necessary.

### Query API
For filtering/analyzing modifiers:

```gdscript
# Get all fire modifiers
var fire_mods = ModifierSystem.query() \
    .with_condition("elemental_affinity", "fire") \
    .with_tag("sun_blessed") \
    .execute()
```

---

## Testing Strategy

### Unit Tests
- Modifier matching (conditions)
- Two-phase calculation (additive + multiplicative)
- Amplification math
- Multiple amplifier stacking

### Integration Tests
- Hero spawns with correct stats
- Solar Warrior doubles bonuses correctly
- Execute triggers at correct HP threshold
- Lifesteal heals correctly

### Balance Tests
- Compare damage with/without modifiers
- Verify amplification doesn't trivialize content
- Test extreme cases (10× amplification)

---

## References

- **Path of Exile:** Additive/multiplicative stacking rules
- **Slay the Spire:** Power/buff system with event hooks
- **Magic: The Gathering:** Layer system for continuous effects
- **Hearthstone:** Event-driven buffs and triggers

---

*Last Updated: 2025-01-11*
*Status: Design Complete - Ready for Implementation*
