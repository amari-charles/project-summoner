# CardCatalog → CardDefinitions Refactor

## Overview

Converted CardCatalog's inline dictionary entries to public static readonly fields in CardDefinitions.cs, following the UnitDefinitions pattern established for units.

**Status: ✅ Completed**

---

## Changes Made

### 1. CardId Struct (scripts/csharp/Cards/CardId.cs)

Converted from const string pattern to a strongly-typed struct:

**Before:**
```csharp
public static class CardId
{
    public const string FireWisp = "fire_wisp";
    public const string Fireball = "fireball";
    // ...
}
```

**After:**
```csharp
public readonly record struct CardId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator string(CardId id) => id.Value;
    public bool HasValue => !string.IsNullOrEmpty(Value);
    public static readonly CardId None = new("");
}

public static class CardIds
{
    public static readonly CardId FireWisp = new("fire_wisp");
    public static readonly CardId Fireball = new("fireball");
    // ...all 27 cards
}
```

### 2. CardDefinitions.cs (NEW)

Created new file with static readonly fields for all 27 cards:

```csharp
public static class CardDefinitions
{
    // =========================================================================
    // SPELLS
    // =========================================================================
    public static readonly CardDefinition Fireball = new() { ... };
    public static readonly CardDefinition Rally = new() { ... };
    // ...

    // =========================================================================
    // WISPS
    // =========================================================================
    public static readonly CardDefinition FireWisp = new() { ... };
    // ...

    // =========================================================================
    // LOOKUP
    // =========================================================================
    private static readonly Dictionary<string, CardDefinition> _lookup = new() { ... };

    public static CardDefinition? Get(CardId id) => _lookup.GetValueOrDefault(id);
    public static CardDefinition? Get(string id) => _lookup.GetValueOrDefault(id);
    public static bool TryGet(CardId id, out CardDefinition? definition) => ...;
    public static bool Has(CardId id) => _lookup.ContainsKey(id);
    public static IReadOnlyCollection<CardDefinition> All => _lookup.Values;
}
```

### 3. CardDefinition.cs Updated

Changed `Id` property from `string` to `CardId`:

```csharp
public required CardId Id { get; init; }
```

### 4. CardCatalog.cs Simplified

Now delegates to CardDefinitions for data, keeps utility/query methods:

```csharp
public static CardDefinition? GetCard(string id) => CardDefinitions.Get(id);
public static CardDefinition? GetCard(CardId id) => CardDefinitions.Get(id);
// Query methods still iterate CardDefinitions.All
```

### 5. Other Files Updated

- **CardFactory.cs**: Uses `CardIds.MamaDuck` instead of string literal
- **SpellBuilder.cs**: Uses `CardIds.X` for spell effect lookups
- **EventCatalog.cs**: Uses `CardIds.X` for deck entries
- **SummonerCatalog.cs**: Uses `CardIds.X` for starter cards
- **SummonerDefinition.cs**: Uses `CardIds.X` for default starter card
- **Test files**: Cast `CardId` to string where needed for assertions

---

## Card Inventory (27 cards)

### Spells (5)
- `fireball`, `mana_bolt`, `rally`, `guard`, `charge`

### Wisps (9)
- `fire_wisp`, `water_wisp`, `wind_wisp`, `earth_wisp`
- `lightning_wisp`, `life_wisp`, `death_wisp`, `shadow_wisp`
- `fire_wisp_swarm`

### Fire Element (5)
- `fire_titan`, `fire_ant`, `fire_ant_swarm`, `fire_boar`, `fire_spider`

### Earth Element (4)
- `pebbloom`, `rock`, `stone_ape`, `earth_rock_thrower`

### Wind Element (2)
- `puff`, `cloud_swarm`

### Water Element (2)
- `water_frog`, `mama_duck`

---

## Benefits

1. **Type Safety**: `CardId` struct prevents typos at compile time
2. **IDE Support**: Static fields provide autocomplete (`CardIds.` shows all cards)
3. **Consistency**: Matches UnitDefinitions pattern
4. **Auditable**: Easy to compare cards side-by-side in one file
5. **Testable**: Static fields can be referenced in tests without magic strings
6. **GDScript Interop**: Implicit string conversion preserves compatibility

---

## Migration Notes

### Implicit Conversions

`CardId` implicitly converts to `string`, so most existing code works:
```csharp
// This works because CardId → string is implicit
string id = CardIds.FireWisp;
```

But `string` does NOT implicitly convert to `CardId`:
```csharp
// This fails - need explicit conversion
CardId id = "fire_wisp";  // ❌ Error
CardId id = new CardId("fire_wisp");  // ✅ OK
```

### Test Assertions

When comparing `CardId` in test assertions, cast to string:
```csharp
// ❌ Fails - GdUnit4 doesn't know how to compare CardId to string
AssertThat(card.Id).IsEqual("fire_wisp");

// ✅ Works - explicit cast
AssertThat((string)card.Id).IsEqual("fire_wisp");
```

### HashSet/Contains

When checking if a `CardId` is in a `HashSet<string>`, cast first:
```csharp
var excludeIds = new HashSet<string> { "fire_wisp" };
// ❌ Fails - CardId can't be argument to Contains<string>
excludeIds.Contains(card.Id);

// ✅ Works
excludeIds.Contains((string)card.Id);
```
