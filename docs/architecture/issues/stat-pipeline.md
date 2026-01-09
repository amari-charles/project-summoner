# Stat Pipeline Issue

**Severity:** MEDIUM
**Status:** RESOLVED
**Created:** 2026-01-08
**Resolved:** 2026-01-09

## Resolution Summary

Implemented as part of "Summon Abstraction + Stat Pipeline Unification" plan:

**Files Created:**
- `scripts/csharp/Stats/StatKey.cs` - Type-safe enum for all unit stats with string conversion
- `scripts/csharp/Stats/UnitStats.cs` - Immutable record for stat storage with modifier support
- `scripts/csharp/Stats/UnitStatCalculator.cs` - Centralized calculation with documented order

**Order of Operations (documented in UnitStatCalculator):**
1. Base stats from CardDefinition
2. Card upgrade multipliers (multiplicative)
3. Modifier adds (additive from ModifierService)
4. Modifier mults (multiplicative from ModifierService)
5. Custom overrides (replacement for event battles)

**Key Changes:**
- All 6 stats now applied (including AggroRadius which was previously ignored)
- Type-safe `StatKey` enum prevents silent failures from unknown stat keys
- `UnitStats` record provides compile-time safety and immutable stat containers
- CardFactory uses `UnitStatCalculator.CalculateFromDictionary()` for all stat calculations

---

## Problem Summary

The game has three separate stat pipelines that interact in unclear ways:
1. Base stats from `CardCatalog`
2. Personal card upgrades from `PlayerCardService`
3. Event-specific overrides from `card.custom_stat_overrides`

There is no documented order of operations, no validation that upgrade stat keys are actually applied, and summoner bonuses are defined but never applied to units.

## Current Architecture

### Three Stat Sources

```
┌─────────────────────────────────────────────────────────────────┐
│                     STAT SOURCES                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  CardCatalog          PlayerCardService      card.gd            │
│  ┌─────────────┐      ┌─────────────────┐    ┌──────────────┐  │
│  │ Base Stats  │  +   │ Upgrade Mults   │  + │ Overrides    │  │
│  │ max_hp: 40  │      │ max_hp: 1.2x    │    │ scale: 2.0   │  │
│  │ atk: 8      │      │ atk: 1.0x       │    │              │  │
│  └─────────────┘      └─────────────────┘    └──────────────┘  │
│         │                     │                     │          │
│         └──────────┬──────────┴─────────────────────┘          │
│                    ▼                                            │
│              CardFactory                                        │
│         ┌─────────────────┐                                     │
│         │ apply to unit   │                                     │
│         │ (string keys)   │                                     │
│         └─────────────────┘                                     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Problems

| Problem | Location | Impact |
|---------|----------|--------|
| **Only 5 stats applied** | `CardFactory.cs:300-310` | Other stats silently ignored |
| **No stat key validation** | `PlayerCardService.cs:376-400` | Upgrades to unknown stats fail silently |
| **Override behavior undefined** | `CardFactory.cs:312-318` | Only `scale_multiplier` handled |
| **Summoner bonuses never applied** | `Summoner.gd:150-152` | Defined but not connected to units |
| **Order of operations unclear** | N/A | No documentation, brittle |
| **ModifierService separate** | `ModifierService.cs` | Another stat source, unclear stacking |

## Code References

### Base Stats (`scripts/csharp/Cards/CardCatalog.cs`)

```csharp
new CardDefinition {
    CatalogId = CardCatalogId.FireFox,
    Stats = new Dictionary<string, float> {
        {"max_hp", 40f},
        {"attack_damage", 8f},
        {"attack_speed", 1.5f},
        {"move_speed", 4f},
        {"attack_range", 2f}
    }
}
```

### Upgrade Application (`scripts/csharp/Services/PlayerCardService.cs:376-400`)

```csharp
var modifiers = new Dictionary<string, float>();
foreach (var upgradeId in card.Upgrades)
{
    var upgrade = CardUpgradeCatalog.GetUpgrade(card.CatalogId, upgradeId);
    foreach (var (stat, mult) in upgrade.StatMods)
    {
        // NO VALIDATION that 'stat' is a known stat key
        if (!modifiers.ContainsKey(stat))
            modifiers[stat] = 1.0f;
        modifiers[stat] *= mult;
    }
}
```

### Stats Applied to Unit (`scripts/csharp/Cards/CardFactory.cs:300-310`)

```csharp
// ONLY these 5 stats are applied
unit.Set("MaxHp", GetFloat(effectiveStats, "max_hp", 100f));
unit.Set("AttackDamage", GetFloat(effectiveStats, "attack_damage", 10f));
unit.Set("AttackSpeed", GetFloat(effectiveStats, "attack_speed", 1f));
unit.Set("MoveSpeed", GetFloat(effectiveStats, "move_speed", 3f));
unit.Set("AttackRange", GetFloat(effectiveStats, "attack_range", 2f));

// If effectiveStats has "critical_chance", it's IGNORED
```

### Override Handling (`scripts/csharp/Cards/CardFactory.cs:312-318`)

```csharp
// Only scale_multiplier is handled
if (customOverrides.ContainsKey("scale_multiplier"))
{
    var multiplier = GetFloat(customOverrides, "scale_multiplier", 1f);
    unit.Scale = Vector3.One * multiplier;
}
// Other overrides like "max_hp" in customOverrides: IGNORED
```

### Summoner Bonuses - Defined But Not Applied (`scripts/core/summoner.gd:150-152`)

```gdscript
if _loaded_summoner_instance != null:
    _apply_summoner_bonuses(_loaded_summoner_instance)

func _apply_summoner_bonuses(instance: SummonerInstance) -> void:
    # Only modifies max_mana, NOT unit stats
    max_mana = base_max_mana + instance.get_bonus("max_mana")
```

## Silent Failure Example

```
1. Designer adds upgrade "Fire Fox: Crit Master"
   - StatMods: {"critical_chance": 1.5}  (50% more crit)

2. Player buys upgrade in shop

3. Player plays Fire Fox card

4. PlayerCardService calculates effective stats:
   - critical_chance: 0.1 * 1.5 = 0.15  ✓ (calculated)

5. CardFactory applies stats to unit:
   - MaxHp: applied ✓
   - AttackDamage: applied ✓
   - AttackSpeed: applied ✓
   - MoveSpeed: applied ✓
   - AttackRange: applied ✓
   - critical_chance: NOT APPLIED ✗

6. Unit spawns with 10% crit (base) instead of 15%

7. No error logged. Player doesn't know upgrade is broken.
```

## Proposed Solution

### Step 1: Define Canonical Stat Keys

Create an enum of all valid stat keys:

```csharp
// scripts/csharp/Units/StatKey.cs
public enum StatKey
{
    MaxHp,
    AttackDamage,
    AttackSpeed,
    MoveSpeed,
    AttackRange,
    CriticalChance,
    CriticalDamage,
    Armor,
    MagicResist,
    // Add new stats here - forces consideration of full pipeline
}

public static class StatKeyExtensions
{
    public static string ToSnakeCase(this StatKey key) => key switch
    {
        StatKey.MaxHp => "max_hp",
        StatKey.AttackDamage => "attack_damage",
        // ...
    };
}
```

### Step 2: Create UnitStatCalculator

Centralize all stat calculation with documented order:

```csharp
// scripts/csharp/Units/UnitStatCalculator.cs
public static class UnitStatCalculator
{
    /// <summary>
    /// Calculates final unit stats with documented order of operations:
    /// 1. Base stats from CardCatalog
    /// 2. + Card upgrades (multiplicative)
    /// 3. + Summoner bonuses (additive)
    /// 4. × ModifierService modifiers
    /// 5. Override replacements (if any)
    /// </summary>
    public static UnitStats Calculate(
        CardDefinition card,
        PlayerCardInstance? playerCard,
        SummonerInstance? summoner,
        IEnumerable<StatModifier> modifiers,
        Dictionary<string, object>? overrides = null)
    {
        var stats = new UnitStats();

        // Step 1: Base stats
        foreach (var (key, value) in card.Stats)
        {
            var statKey = ParseStatKey(key);
            stats.Set(statKey, value);
        }

        // Step 2: Card upgrades (multiplicative)
        if (playerCard != null)
        {
            var upgradeMults = GetUpgradeMultipliers(playerCard);
            foreach (var (key, mult) in upgradeMults)
            {
                ValidateStatKey(key); // Throws if invalid
                stats.Multiply(key, mult);
            }
        }

        // Step 3: Summoner bonuses (additive)
        if (summoner != null)
        {
            foreach (var (key, bonus) in summoner.GetUnitBonuses())
            {
                stats.Add(key, bonus);
            }
        }

        // Step 4: Modifier service (multiplicative)
        foreach (var mod in modifiers)
        {
            stats.ApplyModifier(mod);
        }

        // Step 5: Overrides (replacement)
        if (overrides != null)
        {
            foreach (var (key, value) in overrides)
            {
                if (IsStatKey(key))
                    stats.Set(ParseStatKey(key), Convert.ToSingle(value));
            }
        }

        return stats;
    }

    private static void ValidateStatKey(string key)
    {
        if (!IsValidStatKey(key))
        {
            throw new InvalidStatKeyException(
                $"Unknown stat key '{key}'. Add to StatKey enum if this is a new stat.");
        }
    }
}
```

### Step 3: Add Summoner Unit Bonuses

```csharp
// In SummonerInstance or SummonerConfig
public Dictionary<StatKey, float> GetUnitBonuses()
{
    var bonuses = new Dictionary<StatKey, float>();

    // Base summoner bonuses
    if (ElementalAffinity == Element.Fire)
    {
        bonuses[StatKey.AttackDamage] = level * 0.5f;
    }

    // Trait bonuses
    foreach (var trait in ActiveTraits)
    {
        foreach (var (stat, value) in trait.UnitBonuses)
        {
            bonuses[stat] = bonuses.GetValueOrDefault(stat) + value;
        }
    }

    return bonuses;
}
```

### Step 4: Update CardFactory to Use Calculator

```csharp
// In CardFactory.execute_summon()
var stats = UnitStatCalculator.Calculate(
    cardDef,
    playerCardInstance,
    activeSummoner,
    modifierService.GetModifiers(cardDef.CatalogId),
    customOverrides
);

stats.ApplyTo(unit);  // Type-safe application
```

## Implementation Plan

### Phase 1: Add StatKey Enum + Validation

1. Create `StatKey.cs` enum with all known stats
2. Add validation in `PlayerCardService.GetEffectiveStats()`
3. Log warnings for unknown stat keys in upgrades
4. No behavior changes yet

### Phase 2: Create UnitStatCalculator

1. Create `UnitStatCalculator.cs` with documented order
2. Update `CardFactory` to use calculator
3. Write unit tests for stat calculation order
4. Verify existing behavior unchanged

### Phase 3: Connect Summoner Bonuses

1. Add `GetUnitBonuses()` to `SummonerInstance`
2. Pass summoner to `UnitStatCalculator`
3. Test that summoner traits affect units
4. Update docs with new capability

### Phase 4: Handle All Overrides

1. Document override behavior (replace vs multiply)
2. Apply all stat overrides in CardFactory, not just scale
3. Add tests for override scenarios

## Files to Modify/Create

| File | Action | Purpose |
|------|--------|---------|
| `scripts/csharp/Units/StatKey.cs` | Create | Canonical stat key enum |
| `scripts/csharp/Units/UnitStats.cs` | Create | Type-safe stat container |
| `scripts/csharp/Units/UnitStatCalculator.cs` | Create | Centralized calculation |
| `scripts/csharp/Cards/CardFactory.cs` | Modify | Use UnitStatCalculator |
| `scripts/csharp/Services/PlayerCardService.cs` | Modify | Add validation |
| `scripts/core/summoner.gd` or C# equivalent | Modify | Add unit bonus methods |

## Completion Criteria

- [ ] `StatKey` enum with all valid stats
- [ ] Validation logs warning for unknown stat keys
- [ ] `UnitStatCalculator` with documented order of operations
- [ ] All CardCatalog stats applied (not just 5)
- [ ] Summoner bonuses affect spawned units
- [ ] Custom overrides for all stat keys work
- [ ] Unit tests for stat calculation
- [ ] Documentation of stat pipeline order

## Order of Operations (Final)

```
┌─────────────────────────────────────────────────────────────────┐
│                STAT CALCULATION ORDER                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. BASE STATS (CardCatalog)                                   │
│     max_hp = 100                                                │
│                                                                 │
│  2. CARD UPGRADES (multiplicative)                             │
│     max_hp = 100 * 1.2 = 120                                   │
│                                                                 │
│  3. SUMMONER BONUSES (additive)                                │
│     max_hp = 120 + 10 = 130                                    │
│                                                                 │
│  4. MODIFIERS (multiplicative, from ModifierService)           │
│     max_hp = 130 * 1.1 = 143                                   │
│                                                                 │
│  5. OVERRIDES (replacement, for boss battles etc.)             │
│     max_hp = 500 (if override specified)                       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Related

- [summon-abstraction.md](summon-abstraction.md) - UnitSummon uses UnitStats
- `docs/features/modifier-system.md` - ModifierService documentation
- `docs/features/summoners/progression-system.md` - Summoner traits/bonuses
