# Cross-Cutting Type Migration Plan

Migration plan for shared types, data catalogs, and utilities that span multiple layers. Each section covers the current state, target layer assignment, Godot dependencies, and migration steps.

For layer definitions, see [../architecture/target-architecture.md](../architecture/target-architecture.md). For the full layer map, see [layer-map.md](layer-map.md).

---

## 1. Card Definition Types

**Files:** `CardDefinition.cs`, `CardDefinitions.cs`, `Card.cs`, `CardId.cs`, `CardType.cs`, `Rarity.cs`, `Element.cs`, `UnitType.cs`, `SummonRole.cs`, `CreatureType.cs`, `SpellCategory.cs`, `SpellTargeting.cs`, `CardFlags.cs`, `VisualTrait.cs`, `UnlockCondition.cs`, `CardInstanceId.cs`, `CardTraitId.cs`

**Assessment: Cross-cutting — stay where they are.**

| File | Godot Deps | Layer |
|------|-----------|-------|
| Enums (`CardType`, `Element`, `Rarity`, etc.) | None | Cross-cutting |
| `CardId.cs`, `CardInstanceId.cs`, `CardTraitId.cs` | None (record structs) | Cross-cutting |
| `CardDefinition.cs` | None | Cross-cutting |
| `CardDefinitions.cs` | None (static data) | Cross-cutting |
| `Card.cs` | **Yes** — extends `Resource`, uses `Vector3`, `Node`, `Texture2D` | See below |

### Card.cs Godot Dependency

`Card.cs` is an abstract `Resource` subclass with `Play3D()` methods. This is a View-layer concern (playing a card visually) baked into a data type.

**Target:** Split into two types:
- **`SimCardData`** (cross-cutting) — Pure data: `CatalogId`, `ManaCost`, `CardType`, `Element`. Used by simulation and session. No Godot deps.
- **`Card` Resource** (stays) — GDScript-facing resource for UI rendering (card icon, visual config). `Play3D()` moves to CardFactory or View layer.

`SimCardData` already exists in stub form (referenced by `BattleConfig.PlayerConfig.Deck` in design-specs.md §5). The existing `Card` Resource stays for editor/UI use.

### Action: No changes needed now.

Enums and IDs are already clean. `SimCardData` will be created when Session layer is implemented (Phase 3 execution). `Card` Resource stays for UI.

---

## 2. Card Configs (Godot Resources)

**Files:** `CardConfig.cs` (207 lines), `SpellCardConfig.cs` (137 lines), `SummonCardConfig.cs` (208 lines), `SpawnConfig.cs`

**Assessment: Cross-cutting — stay as Godot Resources.**

These are editor-facing serialized data. They define card properties via `[Export]` attributes so designers can edit them in the Godot inspector. They extend `Resource`.

| Concern | Status |
|---------|--------|
| Godot `Resource` base | Required for editor integration |
| `FromDictionary()` / `ToDictionary()` | GDScript interop — keep while GDScript catalogs exist |
| Stat definitions (MaxHp, AttackDamage, etc.) | Read by `UnitStatCalculator` — cross-cutting data |
| VFX/scene paths (UnitScenePath, SpellVFX) | View-layer data carried in cross-cutting config |

### Action: No migration needed.

Card configs are data definitions. They carry both sim-relevant data (ManaCost, MaxHp) and view-relevant data (CardIcon, UnitScenePath). This is fine — cross-cutting types serve both layers. The simulation reads the stats it needs; the view reads the visuals it needs.

---

## 3. Card Effects System

**Files:** 14 files in `Cards/Effects/` — `ISpellEffect.cs`, `SpellEffect.cs`, `SpellContext.cs`, `DamageEffect.cs`, `CommandEffect.cs`, `CompositeEffect.cs`, `ConditionalEffect.cs`, `ISpellCondition.cs`, `HPThresholdCondition.cs`, `ITargetingStrategy.cs`, `CircleTargeting.cs`, `NearestEnemyTargeting.cs`, `ITargetFilter.cs`

**Assessment: Currently View-layer execution masquerading as game logic. Must split into Simulation + View.**

### The Problem

Effects currently execute by directly manipulating Godot nodes:
- `DamageEffect` calls `unit.TakeDamage()` on `Node3D` references
- `CommandEffect` calls `unit.Set("rally_point", ...)`, `unit.Set("guard_mode", true)`
- `SpellContext` holds `Node? Battlefield`, `SceneTree?`, `Node3D? Caster`
- Effects spawn projectiles via `ProjectileService.Instance`
- Effects play VFX via `VFXManager` autoload lookup

This means spell behavior runs on the View/Godot side, not in the simulation. In multiplayer, the host executes these effects on Godot nodes, which breaks the simulation-layer boundary.

### Target Design

**Simulation layer** handles spell logic:
- When a spell is cast (via `PlayCardCommand`), the simulation looks up the spell definition and applies its effects to `MatchState`
- Damage: `SimTargeting` finds units in radius → simulation applies damage to `UnitData.Hp`
- Commands (Rally/Guard/Charge): simulation sets behavioral state on `UnitData` (rally point, guard mode, forced target)
- Results emitted as `SimEvent`s: `SpellDamageEvent`, `UnitDamagedEvent`, `BuffAppliedEvent`

**View layer** handles presentation:
- `EntityManager` receives `SimEvent`s and triggers VFX/projectiles
- No `ISpellEffect.Execute()` call on the View side — effects are simulation-only

### Migration Approach

This is a **large migration** that happens when the simulation absorbs spell behavior. It's not a rename — the current effect system is replaced by simulation-internal spell processing.

**Phase A — Sim spell processing:**
```
Simulation receives PlayCardCommand for a spell:
1. Look up SpellDefinition (from CardDefinition)
2. SpellDefinition contains: damage, radius, targeting, command type, duration
3. Simulation applies effects to MatchState directly
4. Emit SimEvents (SpellCastEvent, UnitDamagedEvent, etc.)
```

**Phase B — Delete current effects:**
Once simulation handles all spell behavior, the current `Effects/` directory is deleted. `SpellContext`, `DamageEffect`, `CommandEffect`, etc. are no longer needed.

**Phase C — View VFX:**
VFX and projectile visuals are triggered by EntityManager reacting to SimEvents, not by spell effect execution.

### Action: Plan only — execution is a Phase 7 (deletion) blocker.

The Effects system can't be migrated until the simulation handles spell logic. This is documented as a dependency in the delete queue.

---

## 4. Card Spawning

**Files:** `SummonSpec.cs`, `UnitSpawnEntry.cs`, `SpawnPlacement.cs` (enum), `SpawnConfig.cs`

**Assessment: Cross-cutting data definitions. Stay.**

| File | Godot Deps | Notes |
|------|-----------|-------|
| `SummonSpec.cs` | None | Data-driven multi-unit spawn spec (Mama Duck pattern) |
| `UnitSpawnEntry.cs` | None | Per-unit entry: UnitId, Count, Modifier, Placement |
| `SpawnPlacement.cs` | None | Enum: Formation, BehindLeader, AroundLeader |
| `SpawnConfig.cs` | **Yes** — `Resource` base | Godot editor config |

### Action: No migration needed.

`SummonSpec` and `UnitSpawnEntry` are pure data that the simulation reads to know what units to spawn. `SpawnConfig` is a Godot Resource for editor use. Both layers can reference these types.

---

## 5. Card Formations

**Files:** `IFormationStrategy.cs`, `LineFormation.cs`, `RingFormation.cs`, `GridFormation.cs`, `GroupedLineFormation.cs`, `FormationPresets.cs`

**Assessment: Simulation-layer math. Move to simulation.**

Formations calculate spawn positions using pure math (trig, spacing). No Godot types — uses `System.Numerics.Vector3` or `SimVector3`.

**Current:** Uses `Godot.Vector3` for position calculations.

**Target:** Uses `SimVector3` (simulation's Godot-free vector type). Formations are called by the simulation when processing a `PlayCardCommand` for a summon card.

### Action: Replace `Godot.Vector3` with `SimVector3` when formations move into simulation.

This is a straightforward type swap with no logic changes. Happens when simulation absorbs spawn processing.

---

## 6. CardFactory Migration

**Current:** `CardFactory.cs` (678 lines) — `Node` autoload. Calls `UnitSpawner`, `SpawnPositionCalculator`, `ModifierService`. Handles both legacy single-unit and multi-unit (`SummonSpec`) spawning.

**Phase 1 Decision (#14):** Cross-cutting utility for base stats only. Stat modifications happen at session init.

### Target Design

CardFactory **shrinks dramatically**. Most of its current code does things the simulation will handle:

| Current Responsibility | Target |
|-----------------------|--------|
| `execute_summon()` — spawn units at positions | Simulation (processes PlayCardCommand) |
| `execute_spell()` — run spell effects | Simulation (processes spell logic) |
| `get_safe_spawn_positions()` — calculate positions | Simulation (uses Formations + BattlefieldBounds) |
| `GetBaseStats()` — look up unit stats | **Stays** — cross-cutting utility |
| GDScript interop (Dictionary conversion) | Stays while GDScript UI exists |

**Target CardFactory:**
```csharp
public static class CardFactory
{
    public static UnitStats GetBaseStats(CardId cardId)
    public static CardDefinition GetDefinition(CardId cardId)
    // No spawning, no effects, no Godot Node inheritance
}
```

### Action: Shrink when simulation absorbs spawning/effects.

CardFactory can't be simplified until the simulation handles `PlayCardCommand` execution (spawning + effects). Until then, it stays as-is.

---

## 7. CardCatalog / CardCatalogBridge

**Files:** `CardCatalog.cs` (279 lines, static), `CardCatalogBridge.cs` (82 lines, Node autoload), `card_catalog.gd` (GDScript)

### Current Pattern

```
GDScript UI → card_catalog.gd → Dictionary-based card data
C# systems  → CardCatalog.cs  → typed CardDefinition objects
GDScript ↔ C# → CardCatalogBridge.cs (Node autoload wrapping CardCatalog)
```

### Assessment: Keep pattern, simplify over time.

| Component | Action |
|-----------|--------|
| `card_catalog.gd` | **Keep** — GDScript UI reads card data for rendering (icon, name, cost) |
| `CardCatalog.cs` | **Keep** — C# simulation reads typed definitions |
| `CardCatalogBridge.cs` | **Keep** — bridges the two |

The GDScript catalog and C# catalog serve different consumers. As long as GDScript UI exists (which it will for a while — HandUI, collection screen, deck editor are all GDScript), the bridge pattern is necessary.

### Future Simplification

When/if UI migrates to C#, `card_catalog.gd` and `CardCatalogBridge.cs` can be deleted. `CardCatalog.cs` becomes the single source. But that's beyond the current migration scope.

### Action: No changes needed.

---

## 8. UnitStatCalculator Dependencies

**Files:** `UnitStatCalculator.cs` (257 lines), `UnitStats.cs` (299 lines), `StatKey.cs` (287 lines)

### Godot Dependencies

| File | Godot Deps | Details |
|------|-----------|---------|
| `UnitStats.cs` | `Godot.Collections.Dictionary`, `Variant.Type` | Interop methods: `FromGodotDictionary()`, `WithGodotOverrides()` |
| `UnitStatCalculator.cs` | `Godot.Collections.Dictionary` | `CalculateFromGodotDictionary()` entry point |
| `StatKey.cs` | None | Pure enum + string conversion |

### Assessment: Simulation-layer logic with GDScript interop bolted on.

The stat calculation pipeline (base → modifier → upgrades → adds → mults → overrides) is pure game logic — it should live in the simulation layer.

The Godot interop methods (`FromGodotDictionary`, `CalculateFromGodotDictionary`) exist because GDScript callers (UnitSpawner, CardFactory) pass Dictionary data.

### Target Design

**Core types stay cross-cutting** (both sim and view need to read stats):
- `UnitStats` — immutable record, stays cross-cutting
- `StatKey` — enum, stays cross-cutting

**Calculator moves to simulation:**
- `UnitStatCalculator.Calculate()` — called during unit spawn in simulation
- Remove `Godot.Collections.Dictionary` overloads when GDScript callers are migrated

### Action: Move calculator into simulation namespace when sim handles spawning. Remove Godot interop methods when GDScript unit spawning is retired.

---

## 9. Constants Layer Assignment

**Files:** `BattlefieldBounds.cs` (148 lines), `ElementMatchups.cs` (123 lines), `ElementColors.cs` (43 lines), `GroupIDs.cs` (149 lines), `UnitId.cs` (73 lines)

| File | Target Layer | Rationale |
|------|-------------|-----------|
| `BattlefieldBounds.cs` | **Simulation** | Spawn validation, position clamping — game rules. Already references `SimVector3`. |
| `ElementMatchups.cs` | **Simulation** | Damage multipliers — game rule. No Godot deps. |
| `ElementColors.cs` | **View** | RGB colors for tinting — purely visual. Uses `Godot.Color`. |
| `GroupIDs.cs` | **Cross-cutting** | String constants used by both GDScript and C# for scene tree queries. No game logic. |
| `UnitId.cs` | **Cross-cutting** | Record struct + static ID constants. Used everywhere. |

### Action: Move `BattlefieldBounds` and `ElementMatchups` to `Fateforged.Simulation` namespace. Move `ElementColors` to View namespace. Others stay.

These are namespace moves, not logic changes. Can be done incrementally.

---

## 10. Capabilities Interfaces

**Files:** `IDamageable.cs`, `IRangedAttacker.cs`, `IAreaAttacker.cs`, `IVfxAttacker.cs`, `IStatModifier.cs`

**Assessment: Retire with Unit3D.**

These interfaces define combat capabilities on Godot `Node3D` objects (`Unit3D`). In the target architecture:
- Damage is applied to `UnitData.Hp` in MatchState (simulation handles it)
- Ranged attacks are simulation behavior on `UnitData` (SimBehavior)
- Area attacks are simulation behavior
- VFX attacks are View-layer presentation triggered by SimEvents
- Stat modifiers are applied during unit stat calculation (simulation)

### Action: Delete when Unit3D is retired.

Unit3D retirement is in Phase 7 (deletion queue, Tier 2). Capabilities interfaces are deleted alongside Unit3D — they have no purpose once visual shells replace Unit3D.

---

## 11. SpatialGrid Migration

**Current:** `scripts/csharp/Systems/SpatialGrid.cs` (564 lines) — Node autoload. Spatial hash grid for O(k) proximity queries. Used by targeting, spell effects, redirect.

### Current Consumers

| Consumer | Usage | Target |
|----------|-------|--------|
| `SimTargeting` (simulation) | Unit proximity queries | Sim has its own targeting from MatchState |
| `DamageEffect` (effects) | Find units in spell radius | Simulation handles spell targeting |
| `RedirectManager` | Find units near click point | Simulation handles redirect targeting |
| View (potential) | VFX targeting, debug visualization | Could use SpatialGrid or MatchState |

### Assessment: Delete when simulation handles all targeting.

The simulation already has access to all `UnitData` positions via `MatchState`. `SimTargeting` (or equivalent) can iterate unit positions directly — the data set is small enough (typically <50 units) that spatial hashing isn't needed.

If View needs proximity queries (e.g., "find nearest unit to mouse for tooltip"), it can iterate MatchState positions directly.

### Action: Delete when all targeting consumers are migrated to simulation.

SpatialGrid deletion is Phase 7 (deletion queue). It's blocked by spell effects migration (§3 above) and redirect migration (Phase 4, §3).

---

## 12. GDScript Data Catalogs

**Files:** `card_catalog.gd`, `summoner_catalog.gd`, `trait_catalog.gd`, `card_trait_catalog.gd`, `cosmetics_catalog.gd`, `emotes_catalog.gd`, `content_binding.gd`, `profile_repository.gd`, `json_profile_repository.gd`, `deck_constants.gd`

**Assessment: Keep. GDScript UI depends on these.**

GDScript catalogs are the data backbone for all GDScript UI: collection screen, deck editor, shop, campaign map. They load JSON/Resource data and expose it via Dictionary APIs.

C# has parallel typed access via bridge autoloads (`CardCatalogBridge`, etc.).

### Decision: Keep GDScript catalogs unchanged.

Migrating catalogs to C# would require migrating all GDScript UI consumers — that's far beyond the current migration scope. The bridge pattern works and avoids forcing a full UI rewrite.

### Future: If/when GDScript UI migrates to C#, catalogs consolidate into C#-only.

### Action: No changes needed.

---

## 13. GDScript Constants (ID Files)

**Files:** `card_ids.gd`, `battle_ids.gd`, `campaign_ids.gd`, `biome_ids.gd`, `summoner_ids.gd`, `rarity_ids.gd`, `node_type_ids.gd`, `event_type_ids.gd`, `reward_type_ids.gd`, `win_condition_ids.gd`, `vfx_ids.gd`, `currency_type_ids.gd`, `purchase_limit_type_ids.gd`, `element_category_ids.gd`, `unit_type_ids.gd`, `element_name_ids.gd`, `group_ids.gd`, `unit_constants.gd`

**Assessment: Keep. Cross-cutting constants used by GDScript code.**

These define `StringName` constants and mirror enums used throughout GDScript. `unit_constants.gd` explicitly mirrors C# enums (documented in CLAUDE.md as the Mirror Enum Pattern).

### Decision: Keep GDScript constants unchanged.

Same rationale as catalogs — GDScript consumers depend on these, and the mirror enum pattern is explicitly documented as the interop strategy.

### Action: No changes needed. Continue using mirror enum pattern for new enums.

---

## Summary: What Actually Needs Migration

Most cross-cutting types **stay where they are**. The significant migrations are:

| Item | Action | Blocked By |
|------|--------|-----------|
| Card Effects system (§3) | Rewrite as simulation-internal spell processing | Sim must handle spell logic |
| CardFactory (§6) | Shrink to stats-only utility | Sim must handle spawning + effects |
| Formations (§5) | `Vector3` → `SimVector3` | Sim must handle spawning |
| UnitStatCalculator (§8) | Move to simulation namespace | Sim must handle stat calculation |
| BattlefieldBounds (§9) | Move to simulation namespace | Namespace move only |
| ElementMatchups (§9) | Move to simulation namespace | Namespace move only |
| ElementColors (§9) | Move to View namespace | Namespace move only |
| Capabilities (§10) | Delete | Unit3D retirement (Phase 7) |
| SpatialGrid (§11) | Delete | All targeting in sim (Phase 7) |
| `Card.cs` split (§1) | Create `SimCardData` | Session implementation |

Items with "no changes needed": Card configs (§2), Card spawning data (§4), CardCatalog/Bridge (§7), GDScript catalogs (§12), GDScript constants (§13).
