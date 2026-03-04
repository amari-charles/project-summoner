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

## Tag Systems (Two Distinct Concepts)

The codebase has two different "tag" systems that serve completely different purposes:

| Aspect | Trait Eligibility Tags | Modifier Tags |
|--------|------------------------|---------------|
| **Defined in** | `TraitTags.cs` constants | `StatModifier.Tags` property |
| **Used by** | `TraitDefinition.Tags[]`, `CardDefinition.TraitEligibilityTags[]`, `SummonerDefinition.TraitEligibilityTags[]` | `StatModifier` instances |
| **Purpose** | Determine which entities can acquire which traits | Mark modifiers for amplification |
| **Examples** | `TraitTags.Summoner`, `TraitTags.Fire`, `TraitTags.Global` | `"sun_blessed"`, `"earth_guardian"` |

**Trait Eligibility Tags** answer: "Can Cole acquire the Inferno Mastery trait?"
- Cole has tags `[Summoner, Global, Fire, Cole]`
- Inferno Mastery requires tags `[Summoner, Fire]`
- Match! Cole can acquire it.

**Modifier Tags** answer: "Should Solar Warrior's 2x amplifier affect this modifier?"
- Fire Affinity trait provides a modifier tagged `"sun_blessed"`
- Solar Warrior amplifies all `"sun_blessed"` tagged modifiers by 2x
- The modifier's bonus is doubled.

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

### 5. Conditions Dictionary Reference

The `conditions` field in a modifier filters which entities the modifier applies to.

**Valid Condition Keys:**

| Key | Type | Description | Example |
|-----|------|-------------|---------|
| `elemental_affinity` | string | Unit's element | `"fire"`, `"water"`, `"earth"` |
| `creature_type` | string | Creature classification | `"elemental"`, `"beast"`, `"humanoid"` |
| `card_id` | string | Specific card ID | `"fire_wisp"`, `"earth_sprite"` |
| `team` | int | Team ID | `0` (player), `1` (enemy) |
| `unit_type` | string | Combat type | `"melee"`, `"ranged"` |

**How Matching Works:**
- All conditions must match (AND logic)
- Missing condition = no restriction for that key
- Empty conditions = applies to everything

```gdscript
# This modifier only affects fire elementals
{
    "conditions": {
        "elemental_affinity": "fire",
        "creature_type": "elemental"
    }
}
```

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
func _deal_damage_to_target(target: UnitVisual, damage: float):
    target.take_damage(damage)

    if active_modifiers.get("has_lifesteal", false):
        var percent = active_modifiers.get("lifesteal_percent", 0.2)
        heal(damage * percent)

# Example: Execute (double damage to low HP targets)
func calculate_damage(target: UnitVisual) -> float:
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
func calculate_damage(target: UnitVisual) -> float:
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
    var unit = unit_scene.instantiate() as UnitVisual

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

### 3. SimEffects (Simulation Layer)

Central C# system that collects and filters modifiers (formerly `ModifierService` autoload):

```csharp
// SimEffects.cs (buff/debuff system, replaces former ModifierService autoload)
[GlobalClass]
public partial class SimEffects : Node, IModifierService
{
    private readonly Dictionary<string, IModifierProvider> _providers = new();

    public void RegisterProvider(IModifierProvider provider)
    {
        _providers[provider.ProviderId] = provider;
    }

    public List<StatModifier> GetModifiers(ModifierContext context)
    {
        var allModifiers = new List<StatModifier>();

        // Collect from all providers
        foreach (var provider in _providers.Values)
            allModifiers.AddRange(provider.GetModifiers());

        // Filter by conditions and instance scope
        var filtered = FilterModifiers(allModifiers, context);

        // Apply amplification
        ApplyAmplification(filtered);

        return filtered;
    }

    // GDScript interop (snake_case)
    public Godot.Collections.Array get_modifiers_for(
        string targetType,
        Godot.Collections.Dictionary categories,
        Godot.Collections.Dictionary context)
    {
        var modContext = ModifierContext.FromDictionaries(categories, context);
        var modifiers = GetModifiers(modContext);

        var result = new Godot.Collections.Array();
        foreach (var mod in modifiers)
            result.Add(mod.ToDictionary());
        return result;
    }
}
```

### 4. Summoner Provider (SummonerModifierProvider.cs)

```csharp
// SummonerModifierProvider.cs
public class SummonerModifierProvider : RefCounted, IModifierProvider
{
    private readonly SummonerInstance _summonerInstance;
    public string ProviderId => "summoner";

    public SummonerModifierProvider(SummonerInstance summonerInstance)
    {
        _summonerInstance = summonerInstance;
    }

    public List<StatModifier> GetModifiers()
    {
        var modifiers = new List<StatModifier>();

        foreach (var traitId in _summonerInstance.ActiveTraitIds)
        {
            var trait = TraitCatalog.GetTrait(traitId);
            if (trait == null) continue;

            var modifier = new StatModifier
            {
                Source = $"trait_{traitId}",
                Tags = new List<string>(trait.Tags)
            };

            // Add conditions (e.g., elemental affinity)
            if (!string.IsNullOrEmpty(trait.ElementalAffinity))
                modifier.Conditions["elemental_affinity"] = trait.ElementalAffinity;

            // Add stat bonuses
            foreach (var kvp in trait.StatMults)
                modifier.StatMults[kvp.Key] = kvp.Value;

            modifiers.Add(modifier);
        }

        return modifiers;
    }
}
```

### 5. Battle Initialization (BattleScene.cs)

```gdscript
func _register_summoner_provider() -> void:
    # Load summoner instance from profile
    var summoner_instance = SummonerInstance.from_dict(summoner_data)

    # Register summoner modifier provider with C# SimEffects
    # Uses factory method since GDScript can't instantiate C# classes directly
    var sim_effects: Node = get_node_or_null("/root/SimEffects")
    if sim_effects and sim_effects.has_method("register_summoner_provider"):
        sim_effects.call("register_summoner_provider", summoner_instance, summoner_id)
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
func apply_buff(unit: UnitVisual, duration: float):
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

*Last Updated: 2026-01-31*
*Status: Implemented in C# - SimEffects in simulation layer (replaces former ModifierService autoload)*

## Current Implementation Files

- `scripts/csharp/Battle/Simulation/Combat/SimEffects.cs` - Buff/debuff/trigger system (replaces former ModifierService)
- `scripts/csharp/Battle/Simulation/Stats/StatModifier.cs` - Typed modifier class
- `scripts/csharp/Systems/Modifiers/ModifierContext.cs` - Query context
- `scripts/csharp/Systems/Modifiers/IModifierProvider.cs` - Provider interface
- `scripts/csharp/Systems/Modifiers/CardModifierProvider.cs` - Card upgrade modifiers
- `scripts/csharp/Systems/Modifiers/SummonerModifierProvider.cs` - Summoner trait modifiers
- `scripts/csharp/Systems/Modifiers/ItemModifierProvider.cs` - Equipped item modifiers
- `scripts/csharp/Battle/Simulation/Stats/TriggerCondition.cs` - Trigger condition enum

---

## Triggered Modifiers

Triggered modifiers activate conditionally based on combat events rather than always being active. They support duration-based effects and cooldowns.

### Trigger Conditions

```csharp
public enum TriggerCondition
{
    Always,           // Default - always active
    OnHit,            // Activates when dealing damage
    OnTakeHit,        // Activates when taking damage
    OnKill,           // Activates when killing an enemy
    OnDeath,          // Activates when the unit dies
    BelowHpPercent,   // Activates when HP falls below threshold
    AboveHpPercent,   // Activates when HP is above threshold
    Periodic          // Activates every N seconds
}
```

### Trigger Fields in StatModifier

```csharp
public class StatModifier
{
    // ... existing fields ...

    // When this modifier activates
    public TriggerCondition Trigger { get; set; } = TriggerCondition.Always;

    // Threshold for HP-based triggers (0.0 - 1.0)
    public float TriggerThreshold { get; set; }

    // How long the effect lasts (0 = permanent while condition holds)
    public float TriggerDuration { get; set; }

    // Minimum time between activations
    public float TriggerCooldown { get; set; }

    // Returns true if this is a triggered modifier
    public bool IsTriggered => Trigger != TriggerCondition.Always;
}
```

### Partitioned Modifier Resolution

The `SimEffects.GetModifiersPartitioned()` method separates modifiers into:
- **Static modifiers**: Always active, applied at unit spawn
- **Triggered modifiers**: Stored and activated by combat events

```csharp
var (staticMods, triggeredMods) = SimEffects.Instance.GetModifiersPartitioned(context);

// Apply static modifiers immediately
unit.InitializeWithModifiers(staticMods);

// Or use the combined method
unit.InitializeWithPartitionedModifiers(staticMods, triggeredMods);
```

### Example Triggered Traits

**Berserker** - +20% damage when below 50% HP:
```csharp
new TraitModifier
{
    Target = "unit",
    StatMults = new() { ["attack_damage"] = 1.20f },
    Trigger = "BelowHpPercent",
    TriggerThreshold = 0.5f
}
```

**Vengeful** - +10% attack speed for 5s after taking damage:
```csharp
new TraitModifier
{
    Target = "unit",
    StatMults = new() { ["attack_speed"] = 1.10f },
    Trigger = "OnTakeHit",
    TriggerDuration = 5.0f,
    TriggerCooldown = 1.0f
}
```

**Soul Harvest** - Heal 5 HP on kill:
```csharp
new TraitModifier
{
    Target = "unit",
    StatAdds = new() { ["heal_on_kill"] = 5.0f },
    Trigger = "OnKill"
}
```

### UnitVisual Trigger Processing

UnitVisual tracks triggered modifiers and their active state:

```csharp
// Combat event handlers
unit.OnDealDamage(amount, target);   // Checks OnHit triggers
unit.OnKill(target);                  // Checks OnKill triggers
// OnTakeDamage checks OnTakeHit and HP triggers automatically

// Active triggers are updated every physics frame
// - Duration countdown
// - Cooldown countdown
// - HP-based trigger state changes
```

See `docs/technical/trait-system-architecture.md` for implementation details.

---

## Glossary

| Term | Definition |
|------|------------|
| **Trait** | An acquirable passive ability (e.g., Fire Affinity). Stored in `TraitCatalog`. |
| **Modifier** | A stat/behavior change applied to an entity. Represented by `StatModifier` class. |
| **Trait Eligibility Tag** | String constant (from `TraitTags.cs`) determining trait acquisition eligibility. |
| **Modifier Tag** | String label on a `StatModifier` for amplification targeting. |
| **Static Modifier** | Always-active modifier applied at unit spawn (Trigger = Always). |
| **Triggered Modifier** | Conditional modifier that activates on events (e.g., "below 50% HP", "on kill"). |
| **Summoner** | Player character (Cole, Celine, etc.). Acquires traits at level-up. |
| **Summon** | Creature type (Fire Wisp, etc.). Can acquire upgrades at card level-up. |
| **Unit** | Ephemeral battlefield instance of a summon. Does NOT level up - receives modifiers at spawn. |
| **Provider** | Class implementing `IModifierProvider` that supplies modifiers (e.g., `SummonerModifierProvider`). |
| **Amplifier** | A modifier that multiplies bonuses from other modifiers with matching tags. |
