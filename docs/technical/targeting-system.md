# Targeting System

The targeting system controls how units acquire targets, validate attack constraints, and move when constraints aren't satisfied.

## Architecture Overview

```
UnitVisual
  └── GetTargetingConfig()
        ├── TargetingConfigRegistry.GetConfig(UnitId)  // Primary lookup
        ├── Exported TargetingConfig                    // Fallback
        └── DefaultTargetingConfig                      // Default
```

## TargetingConfigRegistry

A static registry that builds targeting configurations programmatically. This approach bypasses .tres resource loading issues and provides a centralized place to define unit-specific targeting behavior.

**Location**: `scripts/csharp/Targeting/TargetingConfigRegistry.cs`

### Usage

Units specify their `UnitId` in the scene file:
```
UnitId = "puff"
```

The registry is queried automatically via `UnitVisual.GetTargetingConfig()`:
```csharp
public static TargetingConfig GetConfig(string unitId)
```

### Adding New Unit Configs

1. Create a new registration method in `TargetingConfigRegistry.cs`
2. Call it from `EnsureInitialized()`
3. Set the `UnitId` on the unit's scene file

Example:
```csharp
private static void RegisterMyUnitConfig()
{
    var config = new TargetingConfig
    {
        Filter = new SimTargetFilter(),
        Scorer = new DistanceScorer { MaxDistance = 10f },
        AttackConstraint = new RangeConstraint(),
        AggroRadius = 10f,
        FallbackMovement = FallbackMovementStyle.MoveToward
    };
    _configs["my_unit"] = config;
}
```

## TargetingConfig Components

### Filters
Control which targets are valid candidates.

- **SimTargetFilter** (in `SimTargeting.cs`): Filters out dead/invalid units (default)

### Scorers
Rank valid targets by priority. Higher scores = higher priority.

- **DistanceScorer**: Prefer closer targets
- **BelowTargetScorer**: Bonus for targets below the unit (useful for flying units)
- **CompositeScorer**: Combines multiple scorers with weights

### Attack Constraints
Validate whether an attack is allowed given unit/target positions.

- **RangeConstraint**: Target must be within attack range
- **HorizontalConeConstraint**: Target must be within a horizontal cone (unit must face target)
- **CompositeConstraint**: All constraints must pass

#### Constraint Resolution Strategies

Constraints implement `TryResolve()` which can use different strategies:

1. **Immediate resolution**: Modify unit state directly (e.g., `SetFacing()`) and return `true`
2. **Deferred resolution**: Return `false` to let `FallbackMovement` handle it
3. **Passive check**: Just return `IsAttackValid()` (default behavior)

`HorizontalConeConstraint` uses deferred resolution - it returns `false` when the target isn't in the cone, allowing `Strafe` movement to naturally bring the target into view. This prevents rapid facing oscillation when targets are directly above/below the unit.

## FallbackMovementStyle

When a unit has a target in range but attack constraints aren't satisfied (e.g., not facing the target), the `FallbackMovement` setting determines behavior:

| Style | Behavior | Use Case |
|-------|----------|----------|
| `MoveToward` | Move directly toward target | Melee units (default) |
| `Strafe` | Circle around target to get it in attack cone | Ranged units with cone constraints |
| `Idle` | Stay in place | Stationary units (turrets, dummies) |

### Strafe Movement Algorithm

The strafe algorithm (`UnitVisual.StrafeAroundTarget`) moves the unit perpendicular to the target direction to bring the target into the attack cone:

1. Calculate angle to target on XZ plane
2. Determine optimal facing direction (toward target's half-plane)
3. Calculate perpendicular strafe direction
4. Choose direction that reduces angle difference to bring target into cone
5. Extend target lock to prevent mid-strafe target switching

```
Target above unit, facing right:
          T
          ↑
    U → strafe up to bring T into cone
```

## Example Configurations

### Puff (Ranged with Cone)
- **Constraint**: RangeConstraint + HorizontalConeConstraint (±30°)
- **Fallback**: Strafe - circles around targets to face them
- **Scorer**: Distance + BelowTargetScorer (prefers ground targets)

### Rock (Stationary Dummy)
- **Constraint**: RangeConstraint only
- **Fallback**: Idle - cannot move
- **AggroRadius**: 0 - doesn't acquire targets
- **AttackSpeed**: 0 - cannot attack

## Integration with UnitVisual

The targeting system integrates with `UnitVisual.UpdateBehavior()`:

```
1. No target? → MoveForward()
2. Target out of range? → MoveTowardTarget()
3. Target in range but constraint fails?
   → TryResolveConstraint()
   → If not resolved: Use FallbackMovement
4. Target in range and constraint passes?
   → Attack (if cooldown ready and AttackSpeed > 0)
```
