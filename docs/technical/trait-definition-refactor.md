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
    // ...all 23 traits
}
```

### 2. TraitDefinitions.cs (NEW)

Created new file with static readonly fields for all 23 traits:

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

## Trait Inventory (23 traits)

### Elemental Affinity Traits (6)
- `trait_fire_affinity`, `trait_water_affinity`, `trait_wind_affinity`
- `trait_earth_affinity`, `trait_lightning_affinity`, `trait_life_affinity`

### Summoner Innate Traits (6)
- `trait_burning_spirit` (Fire)
- `trait_tidal_resilience` (Water)
- `trait_swift_casting` (Wind)
- `trait_stone_fortitude` (Earth)
- `trait_storm_charge` (Lightning)
- `trait_vitality_surge` (Life)

### Combat Traits (5)
- `trait_warriors_might`, `trait_iron_will`, `trait_quick_reflexes`
- `trait_precision_strike`, `trait_battle_hardened`

### Triggered Traits (3)
- `trait_berserker` (BelowHpPercent trigger)
- `trait_vengeful` (OnTakeHit trigger)
- `trait_soul_harvest` (OnKill trigger)

### Mana Traits (3)
- `trait_mana_surge`, `trait_efficient_casting`, `trait_arcane_reservoir`

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
