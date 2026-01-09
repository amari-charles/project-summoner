# Summon Abstraction Issue

**Severity:** MEDIUM-HIGH
**Status:** RESOLVED
**Created:** 2026-01-08
**Resolved:** 2026-01-09

## Resolution Summary

Implemented as part of "Summon Abstraction + Stat Pipeline Unification" plan:

**Files Created:**
- `scripts/csharp/Summons/UnitSummon.cs` - Tracks spawned units with death events
- `scripts/csharp/Summons/SummonResult.cs` - Result wrapper for summon operations
- `scripts/csharp/Summons/SpawnPositionCalculator.cs` - Safe position calculation
- `scripts/csharp/Summons/UnitSpawner.cs` - Unit instantiation logic

**Key Changes:**
- `CardFactory.execute_summon()` now returns `SummonResult` containing `UnitSummon`
- `card.gd` stores `_active_summon` reference and exposes `get_spawned_units()`
- CardFactory reduced from 631 to 431 lines via component extraction
- Cards can now track their spawned units and receive death notifications

---

## Problem Summary

Cards directly invoke `CardFactory.execute_summon()` which instantiates units, sets stats via string keys, and activates them. There is no intermediate "Summon" concept. This tight coupling prevents:
- Tracking spawned units
- Pre/post-summon hooks for events or effects
- Cancellation or modification of summons in progress
- Type-safe stat application

## Current Architecture

```
Card.play_3d()
  → _summon_unit_3d()
    → _execute_csharp_summon()
      → CardFactory.execute_summon(catalogId, position, effectiveStats, ...)
        → Instantiate unit scene
        → Set stats via unit.Set("MaxHp", ...)
        → Apply modifiers
        → Call unit.Activate()
        → (no reference returned to card)
```

### Problems with Current Flow

| Problem | Impact |
|---------|--------|
| Card loses unit references | Cannot track, buff, or interact with summoned units |
| No summon events | Cannot trigger on-summon effects, animations, or UI |
| Stats passed as Dictionary | Type safety lost, string keys error-prone |
| CardFactory is god object | 360+ lines, handles too many responsibilities |
| No summon validation | Cannot check if summon is legal before executing |
| No cancellation | Once started, summon cannot be interrupted |

## Code References

### Card initiates summon (`scripts/cards/card.gd:209-266`)

```gdscript
func _summon_unit_3d(target_position: Vector3, team: int) -> void:
    var factory := get_node_or_null("/root/CardFactory") as Node
    if factory == null:
        push_error("CardFactory not found")
        return

    var effective_stats := get_effective_stats()
    _execute_csharp_summon(factory, target_position, team, effective_stats)
    # No reference to spawned units returned
```

### CardFactory executes summon (`scripts/csharp/Cards/CardFactory.cs:187-377`)

```csharp
public void execute_summon(string catalogId, Vector3 position,
    Godot.Collections.Dictionary effectiveStats, ...)
{
    // 190 lines of:
    // - Scene loading
    // - Unit instantiation
    // - Stat application (string keys)
    // - Formation positioning
    // - Modifier application
    // - Spawn reveal animation
    // - Activation
}
```

### Stats applied via strings (`scripts/csharp/Cards/CardFactory.cs:300-310`)

```csharp
unit.Set("MaxHp", GetFloat(effectiveStats, "max_hp", 100f));
unit.Set("AttackDamage", GetFloat(effectiveStats, "attack_damage", 10f));
unit.Set("AttackSpeed", GetFloat(effectiveStats, "attack_speed", 1f));
unit.Set("MoveSpeed", GetFloat(effectiveStats, "move_speed", 3f));
unit.Set("AttackRange", GetFloat(effectiveStats, "attack_range", 2f));
// Only 5 stats applied - others silently ignored
```

## Proposed Solution

### Introduce `UnitSummon` Class

Create an intermediate object representing "a summon in progress":

```csharp
// scripts/csharp/Cards/UnitSummon.cs
public class UnitSummon
{
    // Configuration
    public string CatalogId { get; }
    public CardDefinition CardDefinition { get; }
    public UnitStats Stats { get; }
    public List<StatModifier> Modifiers { get; }
    public Vector3 SpawnPosition { get; }
    public Team Team { get; }
    public int UnitCount { get; }
    public FormationType Formation { get; }

    // State
    public SummonState State { get; private set; }
    public List<Unit3D> SpawnedUnits { get; } = new();

    // Events
    public event Action<UnitSummon> OnValidated;
    public event Action<UnitSummon, Unit3D> OnUnitSpawned;
    public event Action<UnitSummon> OnCompleted;
    public event Action<UnitSummon, string> OnFailed;

    // Methods
    public bool Validate();
    public void Execute(Node parent);
    public void Cancel();
}

public enum SummonState
{
    Pending,
    Validating,
    Spawning,
    Completed,
    Cancelled,
    Failed
}
```

### Introduce Type-Safe `UnitStats`

Replace string-keyed dictionaries with strongly-typed stats:

```csharp
// scripts/csharp/Units/UnitStats.cs
public class UnitStats
{
    public float MaxHp { get; set; }
    public float AttackDamage { get; set; }
    public float AttackSpeed { get; set; }
    public float MoveSpeed { get; set; }
    public float AttackRange { get; set; }
    // Add new stats here - compiler enforces usage

    public static UnitStats FromDictionary(Dictionary dict);
    public Dictionary ToDictionary();
    public void ApplyTo(Unit3D unit);
}
```

### Refactor Card to Create Summon Objects

```gdscript
# In card.gd
func _summon_unit_3d(target_position: Vector3, team: int) -> void:
    var summon := UnitSummon.new()
    summon.catalog_id = catalog_id
    summon.stats = get_effective_stats()
    summon.spawn_position = target_position
    summon.team = team

    # Connect to events
    summon.on_unit_spawned.connect(_on_unit_spawned)
    summon.on_completed.connect(_on_summon_completed)

    # Validate and execute
    if summon.validate():
        summon.execute(get_parent())
        _active_summons.append(summon)  # Track our summons
    else:
        push_error("Summon validation failed")
```

### Split CardFactory Responsibilities

Extract concerns from the god object:

```
CardFactory (coordinator)
  ├── UnitSpawner (instantiation)
  ├── UnitStatsApplicator (stat application)
  ├── FormationCalculator (positioning)
  └── SpawnEffectHandler (reveal animations)
```

## Implementation Plan

### Phase 1: Create UnitStats Class

1. Create `UnitStats.cs` with typed properties
2. Add `FromDictionary()` and `ApplyTo()` methods
3. Update `CardFactory.execute_summon()` to use `UnitStats`
4. No external API changes yet

### Phase 2: Create UnitSummon Class

1. Create `UnitSummon.cs` with configuration and state
2. Move validation logic from CardFactory
3. Add events (OnUnitSpawned, OnCompleted)
4. CardFactory creates and returns UnitSummon objects

### Phase 3: Update Card to Use UnitSummon

1. Card creates UnitSummon via factory
2. Card connects to summon events
3. Card tracks active/completed summons
4. Emit card-level events (card_summoned_units)

### Phase 4: Split CardFactory

1. Extract UnitSpawner service
2. Extract FormationCalculator service
3. CardFactory becomes thin coordinator
4. Services are independently testable

## Files to Modify/Create

| File | Action | Purpose |
|------|--------|---------|
| `scripts/csharp/Units/UnitStats.cs` | Create | Type-safe stat container |
| `scripts/csharp/Cards/UnitSummon.cs` | Create | Summon abstraction |
| `scripts/csharp/Cards/CardFactory.cs` | Refactor | Use UnitSummon, reduce responsibilities |
| `scripts/cards/card.gd` | Modify | Create and track UnitSummon objects |
| `scripts/csharp/Cards/UnitSpawner.cs` | Create | Unit instantiation service |

## Completion Criteria

- [ ] `UnitStats` class with typed properties
- [ ] `UnitSummon` class with events and state machine
- [ ] Card receives reference to spawned units
- [ ] Summon events emitted (OnUnitSpawned, OnCompleted)
- [ ] No string-keyed stat application in CardFactory
- [ ] CardFactory under 200 lines
- [ ] Unit tests for UnitSummon validation
- [ ] Integration test: card tracks its summoned units

## Benefits After Implementation

| Before | After |
|--------|-------|
| Card forgets units after summon | Card tracks all units it summoned |
| No summon events | Rich events for effects, UI, analytics |
| String-keyed stats | Compiler-enforced stat names |
| 360-line god object | Focused, testable services |
| Can't cancel summon | Cancellation supported |
| Can't validate before summon | Pre-validation with errors |

## Related

- [stat-pipeline.md](stat-pipeline.md) - Stats flow through this abstraction
- [hp-bar-lifecycle.md](hp-bar-lifecycle.md) - Unit tracking helps with cleanup
- `docs/architecture/system-architecture.md` - Update after implementation
