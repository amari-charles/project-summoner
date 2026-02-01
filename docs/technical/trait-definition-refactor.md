# TraitCatalog → TraitDefinitions Refactor

## Overview

Converted TraitCatalog's inline dictionary entries to public static readonly fields in TraitDefinitions.cs, following the UnitDefinitions and CardDefinitions patterns.

**Status: Completed**

---

## Changes Made

### 1. TraitId Struct (scripts/csharp/Data/Traits/TraitId.cs)

Converted from const string pattern to a strongly-typed struct:

**Before:**
```csharp
public static class TraitId
{
    public const string FireAffinity = "trait_fire_affinity";
    public const string BurningSpirit = "trait_burning_spirit";
    // ...
}
```

**After:**
```csharp
public readonly record struct TraitId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator string(TraitId id) => id.Value;
    public bool HasValue => !string.IsNullOrEmpty(Value);
    public static readonly TraitId None = new("");
}

public static class TraitIds
{
    public static readonly TraitId FireAffinity = new("trait_fire_affinity");
    public static readonly TraitId BurningSpirit = new("trait_burning_spirit");
    // ...all 24 traits
}
```

### 2. TraitDefinitions.cs (NEW)

Created new file with static readonly fields for all 24 traits:

```csharp
public static class TraitDefinitions
{
    // =========================================================================
    // INNATE TRAITS - Fire Summoner
    // =========================================================================
    public static readonly TraitDefinition FireAffinity = new() { ... };
    public static readonly TraitDefinition BurningSpirit = new() { ... };
    // ...

    // =========================================================================
    // LOOKUP
    // =========================================================================
    private static readonly Dictionary<string, TraitDefinition> _lookup = new() { ... };

    public static TraitDefinition? Get(TraitId id) => _lookup.GetValueOrDefault(id);
    public static TraitDefinition? Get(string id) => _lookup.GetValueOrDefault(id);
    public static bool Has(TraitId id) => _lookup.ContainsKey(id);
    public static IReadOnlyCollection<TraitDefinition> All => _lookup.Values;
    public static IReadOnlyCollection<string> AllIds => _lookup.Keys;
    public static int Count => _lookup.Count;
}
```

### 3. TraitDefinition.cs Updated

Changed `Id` property from `string` to `TraitId`:

```csharp
public required TraitId Id { get; init; }
```

### 4. TraitCatalog.cs Simplified

Now delegates to TraitDefinitions for data, keeps utility/query methods:

```csharp
public static TraitDefinition? GetTrait(string id) => TraitDefinitions.Get(id);
public static TraitDefinition? GetTrait(TraitId id) => TraitDefinitions.Get(id);
public static bool HasTrait(string id) => TraitDefinitions.Has(id);
// Query methods iterate TraitDefinitions.All
```

---

## Trait Inventory (24 traits)

### Innate Summoner Traits - Fire (2)
- `trait_fire_affinity` - +10% fire damage, +10% attack for fire units
- `trait_burning_spirit` - +5% fire damage

### Innate Summoner Traits - Water (2)
- `trait_water_affinity` - +10% water damage, +10% attack for water units
- `trait_tidal_resilience` - +10% max health

### Innate Summoner Traits - Wind (2)
- `trait_wind_affinity` - +10% wind damage, +10% attack for wind units
- `trait_swift_casting` - +10% cast speed

### Innate Summoner Traits - Earth (2)
- `trait_earth_affinity` - +10% earth damage, +10% attack for earth units
- `trait_stone_fortitude` - +5 flat damage reduction

### Innate Summoner Traits - Lightning (1)
- `trait_lightning_affinity` - +15% lightning damage, +15% attack for lightning units

### Innate Summoner Traits - Life (1)
- `trait_life_affinity` - +15% healing, +10% max health for life units

### Innate Summoner Traits - Death (1)
- `trait_death_affinity` - +10% death damage, +5% lifesteal

### Acquirable Summoner Traits - Global Pool (4)
- `trait_iron_will` - +5 flat damage reduction (Level 2+)
- `trait_quick_recovery` - +10% mana regen (Level 2+)
- `trait_vitality_boost` - +100 flat max health (Level 2+)
- `trait_swift_strike` - +10% attack speed (Level 3+)

### Acquirable Summoner Traits - Triggered (3)
- `trait_berserker` - +20% attack when below 50% HP (Level 3+)
- `trait_vengeful` - +10% attack speed for 5s on hit, 1s cooldown (Level 4+)
- `trait_soul_harvest` - +5 heal on kill (Level 4+)

### Acquirable Summoner Traits - Element Mastery (2)
- `trait_inferno_mastery` - +15% fire damage, +15% attack for fire units (Level 5+, requires FireAffinity)
- `trait_tidal_mastery` - +15% water damage, +15% max health for water units (Level 5+, requires WaterAffinity)

### Summon Traits - Global Pool (4)
- `trait_fortitude` - +8% max HP (Level 2+)
- `trait_power` - +6% attack damage (Level 2+)
- `trait_swiftness` - +5% attack speed (Level 2+)
- `trait_agility` - +5% move speed (Level 2+)

---

## Benefits

1. **Type Safety**: `TraitId` struct prevents typos at compile time
2. **IDE Support**: Static fields provide autocomplete (`TraitIds.` shows all traits)
3. **Consistency**: Matches UnitDefinitions and CardDefinitions patterns
4. **Auditable**: Easy to compare traits side-by-side in one file
5. **Testable**: Static fields can be referenced in tests without magic strings
6. **GDScript Interop**: Implicit string conversion preserves compatibility

---

## Migration Notes

### Implicit Conversions

`TraitId` implicitly converts to `string`, so most existing code works:
```csharp
string id = TraitIds.FireAffinity;  // Works
```

But `string` does NOT implicitly convert to `TraitId`:
```csharp
TraitId id = "trait_fire_affinity";  // Error
TraitId id = new TraitId("trait_fire_affinity");  // OK
```

### Test Assertions

When comparing `TraitId` in test assertions, cast to string:
```csharp
AssertThat((string)trait!.Id).IsEqual((string)TraitIds.FireAffinity);
```
