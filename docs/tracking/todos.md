# Project TODOs

This document tracks planned features, improvements, and tasks for Fateforged.

For completed tasks, see [todos-completed.md](todos-completed.md).

**Status Legend:**
- ⬜ Not Started
- 🔄 In Progress
- ✅ Completed
- 🚫 Blocked

**Priority Levels:**
- 🔴 High Priority
- 🟡 Medium Priority
- 🟢 Low Priority

---

## Campaign Economy & Systems

### 🟡 MEDIUM PRIORITY

#### Type-safe domain objects for Dictionary<string, object>
**Status:** ⬜ Not Started
**Category:** Architecture / Type Safety
**Effort:** Small

**Description:**
Replace loose `Dictionary<string, object>` patterns with proper typed domain objects where the schema is fixed:
- `CampaignProgress.PendingReward` → `PendingRewardData` class (battle_id, reward_type, choice_index)
- `ProfileRepository.UpdateCard()` param → `CardUpdateDto` class (xp, level, upgrades)
- Keep `StoryArcProgress.Flags` as dictionary (legitimately dynamic)

This eliminates `ObjectToVariant` conversion complexity and improves type safety.

---

#### Migrate deck_service.gd → DeckService.cs
**Status:** ⬜ Not Started
**Category:** Architecture / C# Migration
**Effort:** Medium

**Description:**
Migrate deck management service from GDScript to C#.

---

#### Migrate summoner_progression_service.gd → SummonerProgressionService.cs
**Status:** ⬜ Not Started
**Category:** Architecture / C# Migration
**Effort:** Medium

**Description:**
Migrate summoner progression/leveling service from GDScript to C#.

---

### 🟢 LOW PRIORITY

#### Migrate campaign_service.gd → CampaignService.cs
**Status:** ⬜ Not Started
**Category:** Architecture / C# Migration
**Effort:** Large

**Description:**
Migrate campaign management service from GDScript to C#.

---

#### Migrate shop_service.gd → ShopService.cs
**Status:** ⬜ Not Started
**Category:** Architecture / C# Migration
**Effort:** Large

**Description:**
Migrate shop/caravan service from GDScript to C#.

---

## Camera & Controls

### 🟡 MEDIUM PRIORITY

#### Allow Camera Panning Up to Boundary When Zoomed In
**Status:** ⬜ Not Started
**Category:** Camera / Controls
**Effort:** Small

**Description:**
When the camera is zoomed in, players should be able to pan closer to the battlefield boundaries than when zoomed out. Currently the panning limits may be too restrictive when zoomed in, preventing players from seeing units near the edges.

**Requirements:**
- Calculate dynamic pan limits based on current zoom level
- Allow panning to show content up to the battlefield boundary
- Ensure boundary enforcement is consistent across zoom levels

**Notes:**
- Related to camera boundary bugs (scroll wheel, right-click drag)
- Should feel natural and not restrict visibility unnecessarily

---

## Units & Combat

### 🟡 MEDIUM PRIORITY

#### Improve Projectile Collision Detection for 2.5D Sprites
**Status:** ⬜ Not Started
**Category:** Units & Combat / Projectiles
**Effort:** Medium
**Priority:** 🟡 Medium

**Description:**
Projectile collision with 2.5D sprite units is too precise - requires nearly pixel-perfect accuracy to hit. Need a more forgiving collision system that accounts for visual sprite bounds rather than just the capsule collision shape.

**Current Behavior:**
- Projectiles use small sphere colliders (radius 0.2)
- Units have capsule collision shapes that may not match visual sprite bounds
- Hitting a unit requires the projectile to intersect the capsule precisely
- Visually appears like projectiles "pass through" sprites

**Proposed Solutions:**
1. **Larger projectile collision shapes**: Increase projectile hitbox size
2. **Sprite-aware collision**: Use sprite bounding box for hit detection
3. **Proximity-based hits**: Trigger hit when projectile is within threshold distance of unit center
4. **Separate visual vs physics collision**: Large trigger area for projectile hits, smaller shape for unit-unit collision

**Related Files:**
- `scenes/projectiles/base_projectile_3d.tscn` - Projectile collision shape
- `scripts/projectiles/projectile_3d.gd` - Hit detection logic
- `scenes/units/*.tscn` - Unit collision shapes

---

#### Investigate Units Getting Stuck in Idle When Blocked
**Status:** ⬜ Not Started
**Category:** Units & Combat / Pathfinding
**Effort:** Medium
**Priority:** 🔴 High

**Description:**
Puff units (and possibly other units) get stuck in idle when blocked by other characters. They don't move forward or find alternate positions. Affects both top and bottom units in formations - they may be stuck in pathfinding mode rather than truly idle.

**Investigation Areas:**
- Why do blocked units stop trying to move?
- Are units stuck in a pathfinding state? (not truly idle)
- Is the flanking/pathfinding logic triggering correctly?
- Is collision preventing all movement attempts?
- Do units have a target but fail to path to attack range?
- Is there a timeout or failure state in pathfinding that leaves units frozen?

**Related Bug:** See bugs.md "Puff Units Get Stuck in Idle When Blocked by Other Units"

**Related Files:**
- scripts/csharp/Units/Unit3D.cs (UpdateBehavior, movement logic)
- scripts/csharp/Units/RangedUnit3D.cs
- Blocked detection / flanking systems

---

#### Shift Puff Attack Angle Downward
**Status:** ⬜ Not Started
**Category:** Units & Combat / Ranged
**Effort:** Small

**Description:**
Rotate Puff's projectile firing angle cone downward. Keep the same angular spread, but offset the center of the cone so it aims lower.

**Example:**
If current range is -30° to +30° (60° spread centered at 0°), shift to something like -50° to +10° (still 60° spread, but centered at -20°).

**Related Files:**
- Puff unit scene or ranged attack logic
- Projectile spawn angle calculations

---

### 🟢 LOW PRIORITY

#### Refactor Character-Specific Animation Logic to Composition
**Status:** ⬜ Not Started
**Category:** Architecture / Units
**Effort:** Medium

**Description:**
Move character-specific animation logic (breathing, bobbing, attack styles) out of the base `SpriteCharacter2D5Component` class into composable components.

**Current State:**
- Breathing animation: `enable_breathing`, `breathing_amplitude`, `breathing_speed` in base class
- Bobbing animation: `enable_bobbing`, `bob_speed`, `bob_amplitude` in base class
- Attack styles: `attack_style`, `cycle_attack_styles` in base class
- Unit3D passes these to visual component via property setters

**Problems:**
- Every unit carries unused animation parameters
- Base class grows with each new character type
- Adding new behaviors requires modifying shared code

**Proposed Solution:**
Use composition pattern - create separate animation behavior components:
- `BreathingAnimationComponent` - for cloud-like units (Puff)
- `BobbingAnimationComponent` - for floating units
- `AttackEffectComponent` - for single-frame sprite attack effects

Units attach only the components they need.

**Related Files:**
- `scripts/csharp/Visual/SpriteVisualComponent.cs`
- `scripts/csharp/Units/Unit3D.cs`
- `scenes/units/puff_3d.tscn`

---

### 🟡 MEDIUM PRIORITY

#### Investigate Pathfinding & Targeting System Robustness
**Status:** ⬜ Not Started
**Category:** Units & Combat / Performance
**Effort:** Medium

**Description:**
Audit the current pathfinding and targeting systems for robustness and efficiency. Identify potential issues with edge cases, performance bottlenecks, and areas for improvement.

**Areas to Investigate:**
- Target acquisition logic (`AcquireTarget()` in Unit3D.cs)
- Target lock timer and re-acquisition behavior
- Flanking/pathfinding when blocked
- Performance with large unit counts (N² targeting checks?)
- Edge cases: targets dying mid-attack, multiple units targeting same enemy
- Redirect system robustness (forced targets, guard mode)

**Questions to Answer:**
- How does targeting scale with 50+ units on screen?
- Are there race conditions in target switching?
- Is the blocked detection / flanking logic reliable?
- Should we use spatial partitioning for target queries?

**Notes:**
- Related to lane-based movement todo (may affect targeting behavior)
- Consider profiling with large battles before optimizing

---


#### Implement Directional/Cone Attack System
**Status:** ⬜ Not Started
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Add support for melee attacks that only hit in a forward cone/arc instead of a full circle. Useful for units with lunge attacks, tongue attacks, or other forward-facing abilities.

**Current Behavior:**
- `IsInAttackRange()` checks distance only (circular range)
- `SpawnMeleeHitbox()` creates a sphere hitbox positioned forward
- Any enemy within AttackRange distance can be targeted, regardless of direction

**Requirements:**
- Add `AttackConeAngle` property to Unit3D/MeleeUnit3D (0 = full circle, 90 = forward half, etc.)
- Modify `IsInAttackRange()` to check if target is within the cone angle
- Add `AttackHitboxShape` enum (Sphere, Box, Capsule) to MeleeUnit3D
- Add `AttackHitboxSize` vector for non-sphere shapes
- Modify `SpawnMeleeHitbox()` to use configured shape (box for narrow forward attacks)

**Example Use Cases:**
- Frog tongue: narrow forward box hitbox, ~45° targeting cone
- Dragon bite: wide forward arc, large box hitbox
- Standard melee: full circle (current behavior, AttackConeAngle = 0)

**Related Files:**
- `scripts/csharp/Units/Unit3D.cs` - IsInAttackRange, base properties
- `scripts/csharp/Units/MeleeUnit3D.cs` - SpawnMeleeHitbox, PerformAttackAction
- `scripts/csharp/Combat/Hitbox/HitboxComponent.cs` - CreateBoxShape already exists

---

#### Implement Single Target vs Multi Target Attack System
**Status:** ⬜ Not Started
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Add system to differentiate between single target attacks and multi target/AoE attacks for units.

**Current State:**
- Spells have AoE via `spell_radius` (Fireball works)
- Units only attack single targets - no unit-level AoE/splash damage

**Requirements:**
- Define attack target type in unit data (single, multi, aoe)
- Implement multi-target selection logic for units
- Add AoE/splash damage radius for area attacks on units
- Visual indicators for AoE attacks
- Balance damage for multi-target vs single-target

**Notes:**
- Foundation for unit variety (e.g., dragons with breath attacks)
- Multi-target may need reduced damage per target
- Consider different AoE shapes (circle, cone, line)

---

#### Improve Unit Hitboxes
**Status:** ⬜ Not Started
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Flesh out and refine unit hitboxes for better collision detection and combat interactions.

**Requirements:**
- Review current hitbox sizes and shapes
- Adjust hitboxes to better match visual models
- Test with various unit types (melee, ranged, large, small)
- Ensure proper interaction with projectiles and melee attacks

**Notes:**
- Important for combat feel and fairness
- May need different hitbox sizes for different unit types
- Consider separate hitboxes for collision vs damage

---


#### Add Death Animations for Units
**Status:** 🟡 Partial (Infrastructure Ready)
**Category:** Visual Polish
**Effort:** Medium

**Description:**
Create death animations for all unit types to improve visual feedback when units are defeated.

**Current State:**
- Infrastructure exists: `_die()` calls `_update_animation("death")` with 1.0s delay before queue_free()
- Missing: Actual animation frames/assets for death animations

**Requirements:**
- Design death animation for each unit type
- Create animation assets/frames
- Add fade-out or removal timing

**Notes:**
- Consider particle effects (blood, sparks, etc.)
- Should not block gameplay flow

---

#### Add More Summon Unit Cards
**Status:** ⬜ Not Started
**Category:** Content
**Effort:** Variable (per card)

**Description:**
Design and implement additional summon cards to expand unit variety.

**Requirements:**
- Design unit stats and abilities
- Create unit models/visuals
- Balance against existing units
- Create card art and data

**Notes:**
- Follow existing unit creation patterns
- Test balance before adding to decks

---

#### Add More Spell Cards
**Status:** ⬜ Not Started
**Category:** Content
**Effort:** Variable (per card)

**Description:**
Design and implement additional spell cards for more strategic variety.

**Requirements:**
- Design spell effects and mechanics
- Implement spell logic
- Create VFX for spells
- Create card art and data

**Notes:**
- Consider direct damage, buffs, debuffs, board manipulation
- Balance mana costs carefully

---

## Database & Data Layer

### 🟢 LOW PRIORITY

---

## Core Game Systems

### 🟡 MEDIUM PRIORITY

---

#### Investigate Mobile/Desktop Compatibility
**Status:** ⬜ Not Started
**Category:** Core Game Systems / Platform
**Effort:** Medium

**Description:**
Review the current game setup to ensure compatibility with both mobile and desktop platforms.

**Areas to Investigate:**
- UI scaling and touch input
- Resolution and aspect ratio handling
- Input method differences (touch vs mouse/keyboard)
- Performance on mobile devices
- Export settings for each platform

**Notes:**
- Important to address early to avoid major refactoring later
- May need platform-specific adjustments

---


### 🟢 LOW PRIORITY

#### Support Upgrade-Specific Resource Costs (Future)
**Status:** ⬜ Not Started
**Category:** Core Game Systems / Progression
**Effort:** Small
**Dependencies:** Card Level System (implemented)

**Description:**
Add optional support for upgrade-specific resource costs (essence, fragments, etc.) defined in CardUpgradeCatalog.

**Current Behavior:**
- Card/summoner level-ups require only XP (no gold cost)
- Gold is campaign-scoped and used only for Caravan shop purchases

**Future Enhancement:**
- Individual upgrades can optionally specify resource costs (essence, fragments, etc.)
- CardUpgradeCatalog already has structure to support this
- Would allow rare/powerful upgrades to require special resources from events

**Related Code:**
- `scripts/csharp/Services/Cards/Handlers/CardProgressionHandler.cs` - card progression
- `scripts/data/card_upgrade_catalog.gd` - upgrade definitions

**Notes:**
- Low priority - XP-only system is the core design
- Resources add optional depth for specific powerful upgrades

---

## Visual Polish

### 🟡 MEDIUM PRIORITY

#### Improve Hit Flash Feedback for Large Units
**Status:** ⬜ Not Started
**Category:** Visual Polish
**Effort:** Small

**Description:**
Large units (like Fire Titan) appear permanently lit up when taking continuous damage from multiple attackers. The hit flash effect doesn't scale well for tanky units.

**Possible Solutions:**
1. **Threshold-based flashing**: Only show hit flash when damage exceeds a % of max HP (e.g., 5-10%)
2. **Cooldown-based**: Add minimum time between flashes regardless of hits
3. **Damage accumulation**: Accumulate damage over a short window, flash once for the total
4. **Visual variation**: Use different flash intensity based on damage amount
5. **Alternative feedback**: Replace constant flash with damage numbers, screen shake, or other effects for large units

**Notes:**
- Current `flash_white()` in `sprite_character_2d5_component.gd` triggers on every hit
- Problem is most noticeable on high-HP units being attacked by multiple enemies
- Solution should still provide clear feedback that damage is occurring

---

#### Improve Card Visual UI
**Status:** ⬜ Not Started
**Category:** UI/UX
**Effort:** Medium

**Description:**
Enhance the visual design of card display including layout, typography, and effects.

**Requirements:**
- Refine card frame and borders
- Improve text readability
- Add card hover effects
- Polish card animations

**Notes:**
- Should work with existing 3D tilt effect
- Consider glow/highlight for playable cards

---

### 🟢 LOW PRIORITY

#### Clean Up Non-Production VFX
**Status:** ⬜ Not Started
**Category:** Visual Polish / VFX
**Effort:** Medium

**Description:**
Audit existing VFX and replace or remove effects that don't meet production quality standards. Some VFX were created as quick placeholders and need polish or replacement.

**VFX to Review:**
- Fireball effects (explosion, trail, spell) - check if quality matches new wind_puff style
- Lightning strike - verify visual consistency
- Any placeholder effects still in use

**Quality Criteria:**
- Consistent color palettes per element (fire=orange/red, wind=cyan/white, etc.)
- Appropriate particle counts (not too sparse or too heavy)
- Smooth fade-in/fade-out curves
- Proper additive blending where appropriate
- Pooling configured correctly for performance

**Notes:**
- Wind puff impact (wind_puff_impact) can serve as reference for quality bar
- Consider creating a VFX style guide document

---

## Audio

### 🟡 MEDIUM PRIORITY

#### Add Victory/Defeat Music
**Status:** 🟡 Partial (Infrastructure Ready)
**Category:** Audio
**Effort:** Small
**Description:**
Add musical stings or short tracks for win/loss conditions.

**Current State:**
- AudioManager infrastructure exists with crossfade support
- Battle music stops on game end
- 2-second delay after battle end before callback
- Missing: Actual victory/defeat audio files

**Requirements:**
- Victory fanfare audio file
- Defeat music audio file
- Wire up to battle end logic

**Notes:**
- Should be short and impactful
- Clear emotional distinction between victory/defeat

---

#### Add Unit Attack Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Medium

**Description:**
Add sound effects for all unit attack actions.

**Requirements:**
- Source/create attack sounds for each unit type
- Integrate with attack animations
- Vary sounds to avoid repetition

**Notes:**
- Different sounds for melee vs ranged
- Consider unique sounds per unit type

---

#### Add Unit Movement Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Medium

**Description:**
Add footstep and movement sound effects for units.

**Requirements:**
- Source/create movement sounds
- Integrate with movement animations
- Handle different terrain types (optional)

**Notes:**
- Should be subtle, not overwhelming
- Consider speed-based variation

---

#### Add Unit Death Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Small
**Dependencies:** Add Death Animations for Units

**Description:**
Add sound effects when units are defeated.

**Requirements:**
- Source/create death sounds for each unit type
- Integrate with death animations
- Mix appropriately with other sounds

**Notes:**
- Should be clear but not overly gory
- Vary by unit type

---

#### Add Spell Cast Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Medium

**Description:**
Add sound effects for spell casting actions.

**Requirements:**
- Source/create spell cast sounds
- Integrate with spell card play
- Unique sounds for different spell types

**Notes:**
- Should feel magical and impactful
- Coordinate with spell VFX

---

#### Add Projectile Impact Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Small

**Description:**
Add sound effects when projectiles hit their targets.

**Requirements:**
- Source/create impact sounds
- Integrate with projectile hit detection
- Vary by projectile type

**Notes:**
- Should sync with visual impact
- Consider different sounds for hit vs miss

---

#### Add Building Damage Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Small
**Dependencies:** Add Building Hit/Damage Animation

**Description:**
Add sound effects when buildings take damage.

**Requirements:**
- Source/create building impact sounds
- Integrate with damage events
- Should feel weighty and important

**Notes:**
- Should be distinct from unit damage
- Critical audio feedback for game state

---

#### Add Mana Gain Sounds
**Status:** ⬜ Not Started
**Category:** Audio
**Effort:** Small

**Description:**
Add sound effect for mana regeneration/gain events.

**Requirements:**
- Source/create mana gain sound
- Integrate with mana system
- Should be noticeable but not intrusive

**Notes:**
- Helps players track mana availability
- Consider subtle vs prominent sound

---

## UI Revamp

### 🟡 MEDIUM PRIORITY

#### Revamp Battle HUD
**Status:** ⬜ Not Started
**Category:** UI/UX
**Effort:** Medium

**Description:**
Redesign the in-battle HUD elements for better clarity and visual appeal.

**Requirements:**
- Improve HP display for summoners
- Better resource (mana) visibility
- Turn indicator clarity
- Proper information hierarchy

**Notes:**
- Must not obstruct battlefield
- Critical information should be immediately readable

---

#### Add Loading Screen with Asset Preloading
**Status:** ⬜ Not Started
**Category:** UI/UX / Performance
**Effort:** Medium

**Description:**
Create a loading screen that displays during battle transitions and preloads all unit assets asynchronously. This eliminates first-spawn initialization delays and provides a polished user experience.

**Requirements:**
- Loading screen scene with progress bar
- Use `ResourceLoader.load_threaded_request()` for async loading
- Preload all unit scenes from CardCatalog
- Optionally show tips, lore, or artwork during loading

**Technical Notes:**
- Async unit preloading is now implemented (uses `ResourceLoader.load_threaded_request()`)
- Remaining work: Add visual loading screen with progress bar
- Consider caching loaded resources for session duration

**Notes:**
- Part of polish phase
- Professional games use this approach for zero gameplay hitches

---


#### Revamp Settings Screen UI
**Status:** 🟡 Partial (Functional)
**Category:** UI/UX
**Effort:** Small

**Description:**
Redesign settings/options screen for better usability and visual consistency.

**Current State:**
- Basic settings screen exists with audio volume sliders
- Music and SFX sliders with value labels
- "Coming soon" placeholder for future settings
- Settings persist via ProfileRepo

**Remaining Work:**
- Visual polish to match other UI screens
- Add more setting categories (graphics, controls, accessibility)
- Consider accessibility options

**Notes:**
- Functional but visually basic

---

### 🟢 LOW PRIORITY

---

## Summoner System

### 🟢 LOW PRIORITY

#### Per-Summoner Portrait Cropping Configuration
**Status:** ⬜ Not Started
**Category:** Summoners / UI
**Effort:** Small

**Description:**
The summoner icon widget uses a circular clip shader with UV offset/scale params to crop and zoom portraits. Currently these params are hardcoded in the scene file and tuned for Terravorn's portrait.

**Current State:**
- `circular_clip.gdshader` has `uv_offset` and `uv_scale` uniforms
- Values are set in `summoner_icon_widget.tscn` shader material
- Works for Terravorn but other portraits may need different cropping

**Future Enhancement:**
- Add `portrait_uv_offset` and `portrait_uv_scale` fields to `SummonerConfig`
- Have `summoner_icon_widget.gd` read these values and apply to shader
- Or create pre-cropped square portrait assets per summoner

**Related Files:**
- `shaders/ui/circular_clip.gdshader`
- `scenes/ui/components/summoner_icon_widget.tscn`
- `scripts/core/summoner_config.gd`

**Notes:**
- Low priority until more summoner portraits are added
- Pre-cropped assets may be simpler than per-summoner shader config

---

#### Implement Summoner Special Abilities
**Status:** ⬜ Not Started (Phase 3/4)
**Category:** Summoners
**Effort:** Large

**Description:**
Implement the system for summoner active and passive abilities.

**Notes:**
- Phase 3: Level Traits (trait selection at level-up)
- Phase 4: Ultimate Traits (level 10 capstone abilities)
- Foundation is ready via TraitCatalog modifier system

---


## Developer Tools

### 🟢 LOW PRIORITY

#### Hide/Remove Debug Menu Before Release
**Status:** ⬜ Not Started
**Category:** Developer Tools / Release Prep
**Effort:** Trivial

**Description:**
The Debug Menu (`scripts/debug/debug_menu.gd`) is hidden by default but can be shown with ` or F12 in debug builds. Before release, either:
- Remove the autoload entirely, or
- Ensure it only activates with a specific dev flag/command

**Current Behavior:**
- Panel hidden by default, toggle with ` or F12
- Already disabled in release builds via `OS.is_debug_build()` check

**Notes:**
- Low priority - it's already hidden in release builds
- May want to keep for internal testing but hide from players in beta/early access

---

#### Campaign Level Editor (Dev-Only Tool)
**Status:** ⬜ Not Started
**Category:** Developer Tools
**Effort:** Large

**Description:**
A UI tool for developers to design and configure campaign battles without touching code.

**Purpose:**
- Allow designers to create/edit campaign battles without touching code
- Configure enemy decks, AI behavior, rewards, difficulty
- Test battles directly from the editor

**Requirements:**
- **Access**: Dev-only tool (not accessible to players)
- **Location**: Separate scene, accessible from main menu in debug builds or via dev console
- Drag-and-drop cards to build enemy deck
- Set deck size (no player limits for enemies)
- Configure AI behavior (aggression, card priority, play speed)
- Set battle metadata (name, description, difficulty)
- Define reward structure (fixed/choice/random cards)
- Set unlock requirements (which battles must be completed first)
- Preview/test battle
- Save battle definitions to `campaign_service.gd` or separate JSON files

**Notes:**
- Hardcoded decks in `campaign_service.gd` work fine for now
- Only needed when managing 20+ battles becomes cumbersome

---

## Card & Spell System

### 🟡 MEDIUM PRIORITY

#### Deprecate Command Spells
**Status:** ⬜ Not Started
**Category:** Card & Spell System / Design
**Effort:** Medium

**Description:**
Command spells (spells that give commands/orders to units) should be deprecated and removed from the game design. Evaluate which command spells exist and plan their removal or replacement.

**Requirements:**
- Audit existing command spell implementations
- Identify any command spells in card catalog
- Remove or replace with non-command alternatives
- Update any documentation referencing command spells

---

## Architecture & Code Quality

### 🟢 LOW PRIORITY

#### Create Property Interop Helper for GDScript/C# Duck Typing
**Status:** ⬜ Not Started
**Category:** Architecture / Interop
**Effort:** Medium

**Description:**
The same PascalCase/snake_case property fallback pattern is duplicated in 5+ files when accessing properties on nodes that could be either C# or GDScript. Create a centralized helper to eliminate this duplication.

**Current Pattern (Duplicated):**
```csharp
// Check PascalCase (C#)
var val = node.Get("IsAlive");
if (val.VariantType == Variant.Type.Nil)
{
    // Fallback to snake_case (GDScript)
    val = node.Get("is_alive");
}
```

**Proposed Solution:**
Create `scripts/csharp/Interop/NodePropertyHelper.cs`:

```csharp
public static class NodePropertyHelper
{
    public static bool IsAlive(Node3D target)
    {
        if (target is IDamageable d) return d.IsAlive;
        return GetBool(target, "IsAlive", "is_alive", true);
    }

    public static int? GetTeam(Node3D target)
    {
        if (target is Unit3D u) return u.Team;
        return GetInt(target, "Team", "team");
    }

    public static T Get<T>(Node3D node, string pascal, string snake, T fallback) { ... }
}
```

**Files with Duplicated Pattern:**
- `scripts/csharp/Combat/DamageSystem.cs` - IsAlive, Team checks
- `scripts/csharp/Targeting/Filters/ValidTargetFilter.cs` - IsAlive, Team checks
- `scripts/csharp/Spells/Effects/SpellEffect.cs` - IsAlive check
- `scripts/csharp/Units/Unit3D.cs` - Target property access
- `scripts/gdscript/systems/spatial_grid.gd` - Team access

**Notes:**
- Lower priority since current code works, just duplicated
- Consider when touching these files for other reasons
- Type-safe accessors prevent typo bugs

---

## Performance

### 🔴 HIGH PRIORITY

#### Throttle Hot-Path Work in _PhysicsProcess
**Status:** 🔄 In Progress
**Category:** Performance / Units
**Effort:** Medium

**Description:**
Every unit runs targeting + behavior + 3+ spatial grid queries per physics frame. This becomes the primary performance bottleneck at scale (40-100 units).

**Current Behavior:**
- `Unit3D._PhysicsProcess()` runs every frame for every active unit
- `UnitSteering.CalculateSeparationForce()` queries spatial grid every frame
- `UnitSteering.CalculateFlankForce()` queries spatial grid when blocked
- `UnitMovement.CorrectOverlaps()` triggers additional steering queries
- Render priority recalculates every frame even when position unchanged

**Proposed Fix:**
- Throttle steering queries to run every 2-3 frames instead of every frame
- Cache steering results between updates
- Consolidate multiple `GetUnitsInRadius` calls into single query where possible
- Skip render priority calculation when position unchanged

**Related Files:**
- `scripts/csharp/Units/Unit3D.cs:463-494`
- `scripts/csharp/Movement/UnitSteering.cs:56-136`
- `scripts/csharp/Movement/UnitMovement.cs`

---

#### Replace Synchronous Unit Preloading with Async Loading
**Status:** ⬜ Not Started
**Category:** Performance / Loading
**Effort:** Medium

**Description:**
Synchronous `load()` calls block the entire game during battle startup, causing visible stutter.

**Current Behavior:**
- `_preload_unit_scenes()` loops through all card definitions
- Calls `load()` + `instantiate()` + `queue_free()` synchronously in `_ready()`
- Comment acknowledges: "NOTE: This is a synchronous stopgap that may cause brief stutter"

**Proposed Fix:**
- Use `ResourceLoader.load_threaded_request()` for unit scenes
- Add loading screen scene with progress bar
- Show loading screen during battle transitions
- Remove synchronous `_preload_unit_scenes()` stopgap

**Related Files:**
- `scripts/core/game_controller_3d.gd:125-141`
- New: `scenes/ui/loading_screen.tscn`
- New: `scripts/ui/screens/loading_screen.gd`

---

### 🟡 MEDIUM PRIORITY

---

### 🟢 LOW PRIORITY

#### Replace /root/VFXManager Lookup in Projectile3D
**Status:** ⬜ Not Started
**Category:** Architecture / Maintainability
**Effort:** Trivial

**Description:**
`Projectile3D.cs` uses `GetNodeOrNull("/root/VFXManager")` to access the VFX manager autoload. Per code structure guidelines, Node-based scripts should use autoload globals directly.

**Current Behavior:**
```csharp
var vfxManager = GetNodeOrNull("/root/VFXManager");
vfxManager?.Call("play_effect", HitVfx, impactPosition);
```

**Proposed Fix:**
Access `VFXManager` autoload directly without the `/root/` path prefix.

**Related Files:**
- `scripts/csharp/Projectiles/Projectile3D.cs:453-454`

---

#### Refactor Hard-coded /root/ Paths to Service Locator
**Status:** ⬜ Not Started
**Category:** Architecture / Maintainability
**Effort:** Large

**Description:**
88+ files use hard-coded `/root/...` lookups for autoload services. This is fragile and creates hidden dependencies.

**Current Behavior:**
- `get_node("/root/Campaign")`, `get_node("/root/ProfileRepo")`, etc.
- Dynamic path construction: `get_node_or_null("/root/" + signal_source)`
- If autoloads are renamed, lookups fail silently

**Proposed Fix:**
- Create `Services` autoload with typed accessors
- Migrate one service at a time (Campaign, ProfileRepo, etc.)
- Update callers to use `Services.Campaign` instead of `get_node("/root/Campaign")`

**Notes:**
- Large refactor touching 88+ files
- Defer until natural refactoring or dedicated cleanup sprint
- Consider incremental migration during other work

---

#### Add Timeouts to UI Async Waits
**Status:** ⬜ Not Started
**Category:** Performance / Reliability
**Effort:** Small

**Description:**
UI flow often depends on timers/awaits. If a signal never fires, the UI can hang or block progression.

**Current Behavior:**
- Title screen waits 0.5s then animation_finished
- Event screen uses sync `load()` for sequences
- No timeout or fallback if awaited signal fails

**Proposed Fix:**
- Add timeout paths or fallback for awaited signals
- Use explicit state machines that can be interrupted
- Ensure process_mode is set correctly for async sequences

**Related Files:**
- `scripts/ui/screens/title_screen.gd`
- `scripts/ui/screens/event_screen.gd`

**Notes:**
- Lower priority - not causing observed issues currently

---
