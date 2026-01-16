# Code Structure Checklist

Anti-patterns and code smells to watch for during development and PR reviews.

---

## 1. Redundant C#/GDScript Interop Aliases

**Bad:** Creating snake_case aliases in C# for GDScript compatibility.

```csharp
// DON'T DO THIS - Godot 4 auto-converts case
public static void ToggleDebugHurtbox() { ... }
public static void toggle_debug_hurtbox() => ToggleDebugHurtbox();  // Redundant!

public bool IsAlive => _health.IsAlive;
public bool is_alive => IsAlive;  // Redundant!
```

**Why it's bad:**
- Godot 4 automatically converts between PascalCase and snake_case for method calls
- Doubles maintenance burden
- Creates confusion about which to use

**Fix:** Remove snake_case aliases. GDScript can call `Unit3D.ToggleDebugHurtbox()` directly.

**Exception:** Duck-typing property checks with `"property" in node` don't auto-convert. Use helper functions:
```gdscript
func _has_combat_properties(node: Node) -> bool:
    return ("is_alive" in node or "IsAlive" in node) and ("team" in node or "Team" in node)
```

---

## 2. Magic Strings That Should Be Enums/Constants

**Bad:** Hardcoded string literals scattered throughout code.

```gdscript
# DON'T DO THIS
if unit_type == "melee":
    ...
card.element = "fire"
node.add_to_group("units")
```

**Why it's bad:**
- Typos cause silent failures
- No autocomplete or compile-time checking
- Refactoring requires find-and-replace across files

**Fix:** Use typed constants or enums:

```gdscript
# In constants file
class_name UnitTypes
const MELEE: StringName = &"melee"
const RANGED: StringName = &"ranged"

# In constants file
class_name GroupIDs
const UNITS: StringName = &"units"

# Usage
if unit_type == UnitTypes.MELEE:
node.add_to_group(GroupIDs.UNITS)
```

**For C#:** Use `readonly record struct` for type-safe IDs:
```csharp
public readonly record struct UnitId(string Value);
public static class UnitIds
{
    public static readonly UnitId FireElemental = new("fire_elemental");
}
```

---

## 3. Logic in GDScript That Belongs in C#

**Signs code should move to C#:**
- Performance-critical loops (targeting, pathfinding, spatial queries)
- Complex data structures or algorithms
- Code that needs strong typing for safety
- Systems that interact heavily with other C# systems

**Bad:** Heavy computation in GDScript:
```gdscript
# DON'T DO THIS - O(n²) in GDScript is slow
func find_nearest_targets():
    for unit in all_units:
        for target in all_targets:
            var dist = unit.position.distance_to(target.position)
            ...
```

**Fix:** Move to C# and use optimized data structures:
```csharp
// SpatialGrid provides O(k) queries where k = local density
var nearby = SpatialGrid.Instance.GetUnitsInRadius(position, radius);
```

**Keep in GDScript:**
- UI logic and animations
- Scene setup and signal wiring
- Simple game flow control
- Anything that benefits from hot-reload during development

---

## 4. Duplicated Logic (DRY Violations)

**Bad:** Same logic copy-pasted in multiple places.

```csharp
// In GhostUnit3D.cs
if (movementLayer == MovementLayer.Air)
    altitude = flightAltitude;

// In UnitSpawner.cs (same logic!)
if (movementLayer == MovementLayer.Air)
    altitude = flightAltitude;
```

**Why it's bad:**
- Bug fixes need to be applied in multiple places
- Easy to miss one location during updates
- Behavior can drift between copies

**Fix:** Create single source of truth:
```csharp
// In Unit3D.cs
public float GetSpawnAltitude()
{
    return MovementLayer == MovementLayer.Air ? FlightAltitude : 0f;
}

// Everywhere else
float altitude = unit.GetSpawnAltitude();
```

---

## 5. Hardcoded Numeric Values (Magic Numbers)

**Bad:** Numbers without context or explanation.

```gdscript
await get_tree().create_timer(2.0).timeout  # Why 2 seconds?
if health < 50:  # What does 50 represent?
unit.position.y = 2.5  # Magic altitude
```

**Fix:** Use named constants with context:
```gdscript
const SPAWN_REVEAL_DURATION: float = 2.0  # Time for spawn animation
const LOW_HEALTH_THRESHOLD: float = 50.0  # Below this, show warning
const DEFAULT_FLIGHT_ALTITUDE: float = 2.5  # Standard flying unit height
```

---

## 6. Overly Broad Try-Catch / Silent Fallbacks

**Bad:** Swallowing errors or returning fake defaults.

```csharp
// DON'T DO THIS
try {
    return LoadConfig();
} catch {
    return new Config();  // Silently returns empty config!
}

// DON'T DO THIS
var value = dict.Get("key") ?? "unknown";  // Hides missing data
```

**Why it's bad:**
- Bugs are hidden, not fixed
- Debugging becomes impossible
- Data issues propagate silently

**Fix:** Fail loudly or log clearly:
```csharp
try {
    return LoadConfig();
} catch (Exception ex) {
    GD.PrintErr($"[ConfigLoader] Failed to load: {ex.Message}");
    throw;  // Or return with clear error state
}
```

---

## 7. God Classes / Mega Functions

**Bad:** Single class/function doing too many things.

```csharp
// DON'T DO THIS
public class GameManager
{
    void Update() {
        UpdateInput();
        UpdatePhysics();
        UpdateUI();
        UpdateAudio();
        UpdateNetworking();
        SaveGame();
        // 500 more lines...
    }
}
```

**Fix:** Split into focused components:
- `InputManager` - handles input
- `UIManager` - handles UI
- `AudioManager` - handles audio
- Each with single responsibility

---

## 8. Mixing Concerns in Scene Files

**Bad:** Business logic embedded in .tscn files or configured via inspector when it should be in code.

**Signs of the problem:**
- Changing game balance requires editing scene files
- Same values repeated across multiple scene files
- Logic split between scene config and code

**Fix:** Centralize configuration in code catalogs:
```csharp
// CardCatalog.cs - single source of truth for card stats
public static class CardCatalog
{
    public static readonly CardDefinition FireElemental = new()
    {
        MaxHp = 60f,
        AttackDamage = 12f,
        // ...
    };
}
```

---

## 9. Inconsistent Naming Conventions

**Bad:** Mixed naming styles in the same codebase.

```
scripts/
  playerController.gd      # camelCase
  enemy_spawner.gd         # snake_case
  GameManager.gd           # PascalCase
```

**Standard for this project:**
- **GDScript files:** `snake_case.gd`
- **C# files:** `PascalCase.cs`
- **GDScript variables/functions:** `snake_case`
- **C# members:** `PascalCase`
- **Constants:** `SCREAMING_SNAKE_CASE` (GDScript) or `PascalCase` (C#)

---

## 10. Circular Dependencies

**Bad:** A depends on B, B depends on A.

```
UnitManager → TargetingSystem → UnitManager (circular!)
```

**Signs:**
- Autoload order matters and breaks if changed
- "Cannot find class" errors at startup
- Needing to use `call_deferred` to avoid null references

**Fix:**
- Extract shared logic to a third class
- Use signals/events instead of direct calls
- Dependency injection

---

## 11. Leaky Abstractions

**Bad:** Implementation details exposed through public API.

```csharp
// DON'T DO THIS - exposes internal dictionary
public Dictionary<string, Unit> _units;  // Public field with underscore!

// DON'T DO THIS - returns mutable internal collection
public List<Unit> GetUnits() => _units;  // Caller can modify!
```

**Fix:** Hide implementation, expose clean interface:
```csharp
private readonly Dictionary<string, Unit> _units = new();

public IReadOnlyCollection<Unit> GetUnits() => _units.Values;
public Unit? GetUnit(string id) => _units.GetValueOrDefault(id);
```

---

## 12. Missing Validation at Boundaries

**Bad:** Trusting all input without validation.

```csharp
// DON'T DO THIS
public void SpawnUnit(string unitId, Vector3 position)
{
    var unit = UnitCatalog.Get(unitId);  // What if unitId is invalid?
    unit.Position = position;  // What if position is off-map?
}
```

**Fix:** Validate at system boundaries:
```csharp
public void SpawnUnit(string unitId, Vector3 position)
{
    if (!UnitCatalog.Contains(unitId))
    {
        GD.PrintErr($"[Spawner] Unknown unit ID: {unitId}");
        return;
    }

    if (!Battlefield.IsValidPosition(position))
    {
        GD.PrintErr($"[Spawner] Invalid position: {position}");
        return;
    }

    // Safe to proceed
}
```

---

## Quick Reference Checklist

Use during PR reviews:

- [ ] No snake_case aliases for C# methods/properties
- [ ] No magic strings - use constants/enums
- [ ] Performance-critical code is in C#, not GDScript
- [ ] No copy-pasted logic - single source of truth
- [ ] No magic numbers - named constants with context
- [ ] No silent error swallowing - fail loudly or log clearly
- [ ] No god classes - single responsibility per class
- [ ] Configuration centralized in code, not scattered in scenes
- [ ] Consistent naming conventions
- [ ] No circular dependencies
- [ ] Internal state not exposed through public API
- [ ] Input validated at system boundaries
