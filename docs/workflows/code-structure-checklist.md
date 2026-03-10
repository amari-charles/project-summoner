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
    public static readonly UnitId FireWisp = new("fire_wisp");
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
// In UnitGhost.cs
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
    public static readonly CardDefinition FireWisp = new()
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

## 13. Strongly-Typed ID Architecture

**North Star:** All domain identifiers should be strongly-typed `readonly record struct` types, not raw strings.

### The Pattern

Every ID type in the codebase should follow this structure:

```csharp
public readonly record struct CardId(string Value)
{
    public override string ToString() => Value;

    // Implicit TO string (for GDScript interop and serialization)
    public static implicit operator string(CardId id) => id.Value;

    // Explicit FROM string (forces intentional conversion)
    public static explicit operator CardId(string value) => new(value);

    public bool HasValue => !string.IsNullOrEmpty(Value);
    public static readonly CardId None = new("");
}
```

### Why This Pattern Exists

**Prevents type confusion:**
```csharp
// BAD: Easy to pass wrong ID type - compiles fine, fails at runtime
void ProcessCard(string cardId, string deckId, string playerId) { ... }
ProcessCard(deckId, cardId, playerId);  // Oops! Wrong order, no compiler error

// GOOD: Compiler catches mistakes
void ProcessCard(CardId cardId, DeckId deckId, ProfileId playerId) { ... }
ProcessCard(deckId, cardId, playerId);  // Compile error!
```

**Enables IDE support:**
- Autocomplete shows available IDs of the correct type
- Find all references works for specific ID types
- Refactoring is safer and more precise

### Conversion Guidelines

**Implicit conversion TO string:** Allow for easy serialization and GDScript interop.
```csharp
string s = cardId;  // Works - implicit
dict["card_id"] = cardId;  // Works - implicit to Variant
```

**Explicit conversion FROM string:** Requires intentional cast to create typed ID.
```csharp
CardId id = "card_001";  // Compile error - must be explicit
CardId id = (CardId)"card_001";  // Works - explicit cast
CardId id = new CardId("card_001");  // Works - constructor
```

### GDScript Boundary Pattern

**String parameters at GDScript boundary, typed IDs internally:**

```csharp
// Public API accepts strings (called from GDScript)
public void RecordChoice(string nodeId, string choiceId)
{
    RecordChoiceInternal(new NodeId(nodeId), new ChoiceId(choiceId));
}

// Internal methods use typed IDs
private void RecordChoiceInternal(NodeId nodeId, ChoiceId choiceId)
{
    _choices[nodeId] = choiceId;
}
```

**Repository pattern:** Convert at the boundary when calling GDScript.
```csharp
public CampaignProgress GetCampaignProgress(SummonerId summonerId)
{
    // Convert to string only when crossing to GDScript
    var dict = _gdRepo.Call("get_campaign_progress", (string)summonerId);
    return DtoConverters.FromCampaignDict(dict);
}
```

### Well-Known ID Constants

For IDs that are referenced throughout the codebase, create a companion static class:

```csharp
public static class CardIds
{
    public static readonly CardId FireWisp = new("fire_wisp");
    public static readonly CardId IceGolem = new("ice_golem");
    // ... compile-time validated, autocomplete-friendly
}
```

### ID Types Hierarchy

Organize ID types by domain:
- **Cards:** `CardId`, `CardInstanceId`, `CardTraitId`
- **Campaign:** `CampaignId`, `EventId`, `BattleId`, `NodeId`, `ChoiceId`
- **Profile:** `ProfileId`, `DeckId`, `SummonerId`
- **Cosmetics:** `CosmeticId`, `EmoteId`, `SkinId`
- **Shop:** `ShopId`, `OfferingId`

### Migration Strategy

When migrating from `string` to typed IDs:
1. Create the ID type with implicit/explicit operators
2. Update domain models to use typed ID
3. Update repository interface signatures
4. Update service handlers (convert at boundaries)
5. Update DTO converters for serialization
6. Update tests to use typed IDs

---

## 14. Duplicate Rule Sources Across Layers

**Bad:** Re-implementing gameplay constraints in multiple places (input, sim, debug UI) with slightly different logic.

```csharp
// DON'T DO THIS
if (team == 0 && pos.X <= 0f) ...
if (team == 1 && pos.X > 0f) ...
// Same rules duplicated elsewhere with different edge handling
```

**Why it's bad:**
- Debug toggles only affect one path
- Preview/validation/clamping drift apart
- Fixes land in one layer but regress in another

**Fix:** Use one shared authority for each rule domain (for example, `BattlefieldBounds`) and route all callers through it:
- Input validation
- Preview coloring and clamping
- Debug bypass toggles
- Server/sim guard rails

---

## 15. Snapshot Payload Bloat from Visual-Only State

**Bad:** Sending high-frequency visual/transient data in every state snapshot by default.

```csharp
// DON'T DO THIS FOR PURE VISUALS
StateSnapshot(..., Projectiles: allActiveProjectiles, ...)
```

**Why it's bad:**
- Bandwidth scales with effect count, not gameplay authority
- Client interpolation cost grows with cosmetic intensity
- Reconnect and regular sync concerns get mixed together

**Fix:** Default to event-driven visuals for non-authoritative entities. Add explicit reconnect seed messages only when needed:
- Runtime: spawn/impact/despawn events
- Reconnect: compact seed list of currently active visuals
- Snapshot: authoritative gameplay state only

---

## 16. Protocol Messages Without Replay/Reconnect Contract

**Bad:** Adding new message/event types without defining how reconnect/replay should reconstruct client view state.

**Why it's bad:**
- Mid-match reconnects lose transient state
- Different systems implement ad-hoc recovery paths
- Networking behavior becomes implicit and brittle

**Fix:** Each networked message type should explicitly declare one of:
- Fully reconstructible from snapshot state
- Requires dedicated reconnect seed payload
- Fire-and-forget visual event (no recovery expected)

Document this in protocol comments and enforce it in session handlers/tests.

---

## 17. File Placement Without Layer Ownership Check

**Bad:** Adding new files in whichever folder is nearby without checking domain ownership.

```text
// DON'T DO THIS
Battle/Simulation/VirtualLanes.cs
// "It was convenient near Simulation.cs"
```

**Why it's bad:**
- Cross-cutting logic gets scattered and hard to discover
- Namespace/folder drift makes architecture harder to reason about
- Future contributors duplicate concepts in different layers

**Fix:** Run a quick placement rubric before creating/moving files:

1. **Who owns this rule?**  
If deterministic runtime owns it, keep it in simulation (not View/scene folders).

2. **What changes when this code changes?**  
- World geometry/partitions/zones -> `Simulation/Spatial`  
- Unit locomotion/steering -> `Simulation/Movement`  
- Target selection/damage/attacks -> `Simulation/Combat`

3. **Who consumes it?**  
If multiple simulation slices consume it, prefer a shared simulation domain (for example `Spatial`) over placing it under one consumer.

4. **Can we explain placement in one sentence?**  
If not, folder choice is probably wrong; revisit before landing.

**Example:**
- `VirtualLanes` is world partition math used by spawn/targeting/movement, so it belongs in `Simulation/Spatial`, not `Movement` and not View `Battlefield`.

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
- [ ] Domain IDs are strongly-typed (not raw strings)
- [ ] Gameplay rules have one shared authority (no duplicated boundary logic)
- [ ] Visual-only/transient state is not bloating regular snapshots
- [ ] Every protocol message has explicit reconnect/replay behavior
- [ ] New/relocated files pass the layer-ownership placement rubric (owner, consumers, change axis)
