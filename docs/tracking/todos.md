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

## AI-First Priority Queue (2026-03-05)

1. Add reproducible regression tests for camera boundary behavior (scroll + right-drag)  
   Why first: Turns two long-lived bugs into deterministic pass/fail checks.
2. Fix camera boundary bugs based on new regression cases  
   Why second: Player-facing issue with clear expected behavior and bounded surface area.
3. Implement Puff target stickiness + cone-aware target preference  
   Why third: Addresses active combat AI bug with direct balance impact.
4. Verify/fix Wisp single-target behavior in simulation and projectile path  
   Why fourth: Likely targeting/projectile pipeline issue with clear combat correctness outcome.
5. Replace `CampaignProgress.PendingReward` dictionary with `PendingRewardData`  
   Why fifth: Type-safety win with moderate refactor cost and strong semantic benefit.
6. Continue typed-internal service handler refactors (string boundary only at GDScript edge)  
   Why sixth: Important cleanup, but lower immediate player impact than gameplay bugs.

---

## Ranked Gameplay

### 🟡 MEDIUM PRIORITY

#### Implement Ranked Gameplay Mode
**Status:** 🔄 In Progress (Phase 4 of 4)
**Category:** Core Game Systems / Multiplayer
**Effort:** Large

**Description:**
Add a ranked competitive mode where players battle against others (or AI) with matchmaking, rankings, and seasonal progression.

**Completed (Phase 2-4):**
- ✅ C# Protocol layer (Messages, MessageSerializer)
- ✅ Session-layer architecture with `HostSession` / `ClientSession`
- ✅ P2PTransport (ENet-based)
- ✅ NetworkIdRegistry for entity sync
- ✅ StateSnapshotBuilder and DesyncDetector
- ✅ Nakama SDK integration (NakamaGameClient)
- ✅ ELO rating system (EloCalculator, RankingService)
- ✅ Matchmaking service
- ✅ Match reporting service
- ✅ Leaderboard service
- ✅ Reconnection handling
- ✅ Ranked UI screen (online_screen)
- ✅ Opponent deck/summoner exchange (commit `2d8bfca4`)

**Remaining (Phase 4):**
- [ ] Polish: queue UI, match found animation
- [ ] End-to-end testing with Nakama server

**Technical Considerations:**
- Host-authority model (client is render-only, no local simulation)
- 10 Hz state snapshots for sync
- Client prediction not yet implemented — currently pure snapshot interpolation
- See `docs/multiplayer/architecture.md` for architecture boundaries
- See `docs/technical/simulation-architecture.md` for simulation layer details

**Related Systems:**
- Deck building/validation
- Battle system
- Profile progression

---

#### Add Client-Side Prediction
**Status:** ⬜ Not Started
**Category:** Multiplayer / Simulation
**Effort:** Medium

**Description:**
The client currently operates as a pure renderer — it applies host snapshots but does not run local simulation. Adding client-side prediction would reduce perceived input lag by running `Simulation.Tick()` locally on the client with local inputs, then reconciling when the authoritative snapshot arrives.

**Tasks:**
- [ ] Run `Simulation.Tick()` on client with local commands
- [ ] Implement snapshot reconciliation (rollback + replay on mismatch)
- [ ] Handle misprediction correction (smooth visual snapping)
- [ ] Ensure deterministic parity between host and client simulation

**Notes:**
- The simulation layer is already pure and Godot-free, making client prediction feasible
- `DesyncDetector` already compares local vs host state — extend for prediction reconciliation
- Low priority until latency becomes a user-facing issue

---

## Campaign Economy & Systems

### 🟡 MEDIUM PRIORITY

#### Refactor Service Handlers to Typed-Only Internal Methods
**Status:** ⬜ Not Started
**Category:** Architecture / Type Safety
**Effort:** Medium

**Description:**
Service handlers currently accept `string` parameters and convert to typed IDs at the start of each method. As more internal C# code calls these handlers, this creates unnecessary conversions and loses type safety benefits.

**Current Pattern:**
```csharp
// Public method accepts string (GDScript calls this)
public void SaveProgress()
{
    var summonerId = _getActiveSummonerFunc();  // Returns string
    var typedSummonerId = new SummonerId(summonerId);  // Convert immediately
    var progress = _profileRepo.GetCampaignProgress(typedSummonerId);
    // ...
}
```

**North Star Pattern:**
```csharp
// Public method for GDScript - accepts string, delegates to internal
public void SaveProgress(string summonerId)
{
    SaveProgressInternal(new SummonerId(summonerId));
}

// Internal method uses typed IDs throughout
internal void SaveProgressInternal(SummonerId summonerId)
{
    var progress = _profileRepo.GetCampaignProgress(summonerId);
    // All internal calls use typed IDs
}

// C# callers use typed API directly
_progressHandler.SaveProgressInternal(activeSummoner);
```

**Benefits:**
- C# code maintains type safety end-to-end
- String conversion happens once at GDScript boundary
- Clearer distinction between public API and internal implementation
- Enables future migration where GDScript also uses typed objects

**Files to Refactor:**
- `CampaignProgressHandler.cs` - SaveProgress, LoadProgress, ResetProgress
- `CampaignRewardHandler.cs` - SetPendingReward, GetPendingReward, ClearPendingReward
- `ChoiceTracker.cs` - Already has dual API pattern (RecordChoice vs RecordChoiceFromString)
- `CardOwnershipHandler.cs` - GrantCards, GetCard, RemoveCard
- `CardProgressionHandler.cs` - GrantXp, LevelUpCard
- `DeckCrudHandler.cs` - CreateDeck, DeleteDeck, ValidateDeck
- `EconomyService.cs` - AddCampaignGold, SpendCampaignGold
- `SummonerProgressionService.cs` - GrantXp, LevelUp, GetProgressInfo

**Notes:**
- Lower priority - current pattern works correctly
- Consider during natural refactoring of these files
- `ChoiceTracker` already demonstrates the target pattern

---

#### Complete PendingReward Typed Domain Object Migration
**Status:** ⬜ Not Started
**Category:** Architecture / Type Safety
**Effort:** Small

**Description:**
Finish migrating fixed-schema dictionaries to typed domain objects:
- `CampaignProgress.PendingReward` → `PendingRewardData` class (`battle_id`, `reward_type`, `choice_index`)
- Keep `StoryArcProgress.Flags` as dictionary (legitimately dynamic)

**Audit Note (2026-03-05):**
- `ProfileRepository.UpdateCard()` typed DTO migration is already complete (`CardUpdate`, merged 2026-01-27 in `#219`) and has been moved to `todos-completed.md`.

This reduces `ObjectToVariant` conversion complexity and improves type safety where schema is fixed.

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

#### Complete DamageProfile-Based Armor/MagicResist Integration
**Status:** ⬜ Not Started
**Category:** Units & Combat / Stats
**Effort:** Medium

**Description:**
Defense reduction is now active in `SimDamage` via `PhysicalDefense`/`MagicDefense`, but `DamageProfile` split damage is still not integrated.

**Current State:**
- Defense reduction is applied in `SimDamage` (diminishing returns: `100 / (100 + defense)`)
- `DamageType` pipeline works for physical vs magic damage
- `DamageProfile` exists but is still marked as a stub and not used for mixed physical/elemental split attacks

**Requirements:**
- Integrate unit `DamageProfile` into damage pipeline for mixed attack splits
- Apply split portions through appropriate defense lanes (physical vs magic)
- Add UI indicators for damage types on cards

**Related Files:**
- `scripts/csharp/Battle/Simulation/Combat/SimDamage.cs` - Damage calculation
- `scripts/csharp/Infrastructure/Data/Units/DamageProfile.cs` - Physical/elemental ratio
- `scripts/csharp/Infrastructure/Data/Units/UnitDefinition.cs` - DamageProfile property

---

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
- `scenes/battle/projectiles/base_projectile_3d.tscn` - Projectile collision shape
- `scripts/csharp/Battle/View/ProjectileVisual.cs` - Visual hit/impact (formerly projectile_3d.gd)
- `scenes/battle/units/*.tscn` - Unit collision shapes

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
- scripts/csharp/Battle/View/UnitVisual.cs (visual shell / movement sync)
- scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs (behavior logic, formerly in Unit3D/RangedUnit3D)
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
- UnitVisual passes these to visual component via property setters

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
- `scripts/csharp/Battle/View/Visual/SpriteVisualComponent.cs`
- `scripts/csharp/Battle/View/UnitVisual.cs`
- `scenes/battle/units/puff_3d.tscn`

---

### 🟡 MEDIUM PRIORITY

#### Investigate Pathfinding & Targeting System Robustness
**Status:** ⬜ Not Started
**Category:** Units & Combat / Performance
**Effort:** Medium

**Description:**
Audit the current pathfinding and targeting systems for robustness and efficiency. Identify potential issues with edge cases, performance bottlenecks, and areas for improvement.

**Areas to Investigate:**
- Target acquisition logic (in `SimBehavior.cs` / `SimTargeting.cs`)
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
- Add `AttackConeAngle` property to unit data/sim behavior (0 = full circle, 90 = forward half, etc.)
- Modify attack range check in `SimBehavior` to check if target is within the cone angle
- Add `AttackHitboxShape` enum (Sphere, Box, Capsule) for melee behavior
- Add `AttackHitboxSize` vector for non-sphere shapes
- Modify melee hitbox spawning to use configured shape (box for narrow forward attacks)

**Example Use Cases:**
- Frog tongue: narrow forward box hitbox, ~45° targeting cone
- Dragon bite: wide forward arc, large box hitbox
- Standard melee: full circle (current behavior, AttackConeAngle = 0)

**Related Files:**
- `scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs` - attack range, behavior logic (formerly in Unit3D/MeleeUnit3D)
- `scripts/csharp/Battle/Simulation/Combat/Hitbox/HitboxComponent.cs` - CreateBoxShape already exists

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
- `scripts/csharp/Meta/Services/Cards/Handlers/CardProgressionHandler.cs` - card progression
- `scripts/infrastructure/data/card_upgrade_catalog.gd` - upgrade definitions

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

### 🟡 MEDIUM PRIORITY

#### Audit Summoner Secondary Stats (damage_bonus, damage_reduction)
**Status:** ⬜ Not Started
**Category:** Summoners / Stats
**Effort:** Small

**Description:**
Summoner secondary stats (`damage_bonus`, `damage_reduction`) are computed internally via the trait system but are no longer displayed in the Summoner Screen UI. Audit whether these stats are:
1. Actually being applied in combat calculations
2. Useful for the gameplay design
3. Documented appropriately

**Context:**
- These stats were removed from UI display (they cluttered the summoner screen with confusing "Defense: +X%" rows)
- They exist in `SummonerInstance.get_computed_stats()` and are populated by traits
- The trait "Fortune Favors the Bold" grants `damage_bonus` as a modifier
- Unclear if `SimDamage` / `SimBehavior` or other combat code actually uses these values

**Questions to Answer:**
- Are `damage_bonus` and `damage_reduction` actually applied during damage calculations?
- Should these remain as internal modifiers or be removed entirely?
- If kept, should they be surfaced differently (e.g., in trait tooltips)?

**Related Files:**
- `scripts/infrastructure/data/summoner_instance.gd` - `get_computed_stats()`
- `scripts/csharp/Battle/Simulation/Combat/SimDamage.cs` - damage calculations
- `scripts/infrastructure/data/trait_catalog.gd` - trait definitions

---

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
- `scenes/meta/components/summoner_icon_widget.tscn`
- `scripts/infrastructure/summoner_config.gd`

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

### 🔴 HIGH PRIORITY

#### Audit Sim/Visual State Desync Points
**Status:** ⬜ Not Started
**Category:** Architecture / Simulation
**Effort:** Medium

**Description:**
Audit the codebase for places where the simulation (MatchState/UnitData) and the visual layer (UnitVisual, GDScript controllers) track the same concept independently, creating desync risks.

**Known Pattern:**
The sim and visual layers sometimes manage parallel state (phase, activation, position) without a single source of truth. When one side changes state, the other may not be notified, leading to desyncs. Example: spawn reveal sets visual UnitVisual to Inactive while the sim UnitData is Active — the sim moves the unit while the visual stays frozen.

**Audit Checklist:**
- [ ] Phase state: GDScript `current_phase` vs sim `GamePhase` — are all transitions synced?
- [ ] Unit activation: visual `ActivationState` vs sim `UnitData.ActivationState` — any gaps?
- [ ] Position: are there other cases where the visual stops reading from sim while the sim keeps updating?
- [ ] HP/death: can the sim kill a unit while the visual thinks it's alive (or vice versa)?
- [ ] Targeting: does the visual layer ever hold stale target references after the sim re-targets?

**Guiding Principle:**
If data belongs to an entity, put it on the entity. Avoid solving per-entity problems with global sweeps or cross-system coordination.

**Related Files:**
- `scripts/csharp/Battle/Simulation/SimulationNode.cs` — sim-visual bridge
- `scripts/csharp/Battle/View/UnitVisual.cs` — visual reads from sim
- `scripts/csharp/Battle/View/BattleScene.cs` — phase tracking (formerly game_controller_3d.gd)

---

#### Eliminate Dynamic Call() in BattleSessionFactory
**Status:** ⬜ Not Started
**Category:** Architecture / Type Safety
**Effort:** Small

**Description:**
`BattleSessionFactory.cs` uses Godot's `Call()` (string-based dynamic dispatch) to invoke methods on C# autoloads (`Decks`, `CardService`, `ProfileRepo`, `SummonerSelection`). Since `Call()` takes a string method name, typos and renames fail silently at runtime — the compiler can't catch them.

All 5 target services are C# classes with strongly-typed public methods. Replace `Call()` with direct typed access using `GetNodeOrNull<T>()` (already used elsewhere in the codebase):

| Current (dynamic) | Replacement (typed) |
|---|---|
| `caller.GetNodeOrNull("/root/Decks")` + `Call("GetDeckDict", ...)` | `caller.GetNodeOrNull<DeckService>("/root/Decks")?.GetDeck(...)` |
| `caller.GetNodeOrNull("/root/CardService")` + `Call("GetCardDict", ...)` | `caller.GetNodeOrNull<CardService>("/root/CardService")?.GetCard(...)` |
| `caller.GetNodeOrNull("/root/ProfileRepo")` + `Call("GetActiveProfileDict")` | `ProfileRepository.Instance?.GetActiveProfileDict()` (static access, already used on line 282) |
| `caller.GetNodeOrNull("/root/SummonerSelection")` + `Call("GetActiveSummonerId")` | `caller.GetNodeOrNull<SummonerSelectionService>(...)?.GetActiveSummonerId()` |

**Benefits:**
- Compile-time method name validation (renames break the build, not silently at runtime)
- IDE IntelliSense for method signatures
- Consistent with patterns already used by DeckService, CampaignService

**Related Files:**
- `scripts/csharp/Battle/Session/BattleSessionFactory.cs` — 5 Call() sites to replace
- `scripts/csharp/Meta/Services/Deck/DeckService.cs` — typed methods available
- `scripts/csharp/Meta/Services/Cards/CardService.cs` — typed methods available
- `scripts/csharp/Infrastructure/Persistence/ProfileRepository.cs` — Instance pattern
- `scripts/csharp/Meta/Services/Summoner/SummonerSelectionService.cs` — typed methods available

---

#### Audit for Global-Coordination-over-Local-State Anti-pattern
**Status:** ⬜ Not Started
**Category:** Architecture / Design Principles
**Effort:** Medium

**Description:**
Audit the codebase for places where per-entity concerns are solved via global sweeps or cross-system event coordination instead of putting the data directly on the entity.

**The Anti-pattern:**
Instead of giving an entity the information it needs to manage itself, we infer its state from an unrelated system event and sweep all entities matching some filter. This is indirect, fragile, and relies on assumptions about system state that may not hold.

**Example (fixed):**
Units needed to stay inactive during spawn reveal. Instead of giving each UnitData a `SpawnTimer` (local state), the initial approach was to activate all inactive units for a team when the summoner's casting completed (global sweep triggered by unrelated event). This breaks if multiple casts overlap, units are inactive for other reasons, etc.

**Principle:** If data belongs to an entity, put it on the entity.

**Areas to Audit:**
- [ ] Any `foreach unit where team == X` sweeps that could be per-unit timers/flags
- [ ] Any signal handlers that modify entities they don't own
- [ ] Any activation/deactivation logic driven by external events rather than self-contained state
- [ ] Phase transitions that sweep-modify entities vs entities reacting to phase themselves

---

#### Refactor Reward System to Typed RewardSpec Classes
**Status:** ⬜ Not Started
**Category:** Architecture / Flag Proliferation
**Effort:** Medium

**Description:**
Replace the dictionary-based reward spec with polymorphic C# classes. The `get_reward_spec()` method returns a unified dictionary with flags (`is_replay`, `requires_choice`, etc.) - these should be type-specific classes.

**Current Problem:**
- `reward_service.gd:85-95` builds spec dictionary with multiple flags
- `reward_screen.gd:124-173` checks `is_replay` and `requires_choice` flags
- Flag combinations create complex conditional logic

**Ideal State:**
```csharp
// scripts/csharp/Infrastructure/Data/Rewards/RewardSpec.cs
public abstract class RewardSpec
{
    public int GoldReward { get; set; }
    public int SummonerXp { get; set; }
    public int CardXp { get; set; }

    public abstract RewardSpecType Type { get; }
}

public class FixedRewardSpec : RewardSpec
{
    public override RewardSpecType Type => RewardSpecType.Fixed;
    public string CardId { get; set; }
}

public class FlexibleRewardSpec : RewardSpec
{
    public override RewardSpecType Type => RewardSpecType.Flexible;
    public List<string> CardOptions { get; set; }
    public bool PlayerSelects { get; set; }
    public int? ChosenIndex { get; set; }
}

public class ReplayRewardSpec : RewardSpec
{
    public override RewardSpecType Type => RewardSpecType.Replay;
    // XP only - no cards or gold
}

public class NoRewardSpec : RewardSpec
{
    public override RewardSpecType Type => RewardSpecType.None;
}
```

**Consumer Pattern:**
```csharp
// Instead of checking flags:
switch (spec)
{
    case ReplayRewardSpec replay:
        ShowReplayMessage();
        break;
    case FlexibleRewardSpec flexible when flexible.PlayerSelects:
        ShowChoiceUI(flexible.CardOptions);
        break;
    case FixedRewardSpec fixed:
        ShowFixedReward(fixed.CardId);
        break;
}
```

**Files to Create:**
- `scripts/csharp/Infrastructure/Data/Rewards/RewardSpec.cs` (base + subclasses)
- `scripts/csharp/Meta/Services/Rewards/RewardSpecFactory.cs`

**Files to Refactor:**
- `scripts/csharp/Meta/Services/RewardService.cs` (formerly reward_service.gd)
- `scripts/meta/screens/reward_screen.gd`

---

### 🟢 LOW PRIORITY

#### Move Campaign Data Definitions to C#
**Status:** ⬜ Not Started
**Category:** Architecture / Consistency
**Effort:** Medium
**Depends On:** EventCatalog + CampaignCatalog (completed; next step is data-definition migration)

**Description:**
Once EventCatalog and CampaignCatalog exist, move the actual campaign data definitions from GDScript to C#. This completes the migration to fully typed campaign data.

**Current State:**
- `summoners_path_data.gd` defines Summoner's Path campaign
- `test_arena_data.gd` defines Test Arena campaign
- Data is in GDScript dictionaries, loaded into C# on startup

**Ideal State:**
```csharp
// scripts/csharp/Infrastructure/Data/Campaigns/SummonersPathCampaign.cs
public static class SummonersPathCampaign
{
    public static CampaignDefinition Definition => new()
    {
        Id = "summoners_path",
        NameKey = "campaign.summoners_path.name",
        Nodes = new List<CampaignNode>
        {
            new() { EventId = "first_trial", Position = new Vector2(100, 300) },
            // ...
        }
    };
}

// Events defined separately in EventCatalog
EventCatalog.Register(new BattleEventDefinition
{
    Id = "first_trial",
    BiomeId = "summer_plains",
    Difficulty = 1,
    IsTutorial = true,
    RequiresDeck = true,
    // ...
});
```

**Benefits:**
- Full type safety at definition time
- No GDScript→C# conversion on load
- IDE refactoring support for event IDs

**Files to Delete (after migration):**
- `scripts/infrastructure/data/campaigns/summoners_path_data.gd`
- `scripts/infrastructure/data/campaigns/test_arena_data.gd`

**Files to Create:**
- `scripts/csharp/Infrastructure/Data/Campaigns/SummonersPathCampaign.cs`
- `scripts/csharp/Infrastructure/Data/Campaigns/TestArenaCampaign.cs`

---

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
        if (target is UnitVisual u) return u.Team;
        return GetInt(target, "Team", "team");
    }

    public static T Get<T>(Node3D node, string pascal, string snake, T fallback) { ... }
}
```

**Files with Duplicated Pattern:**
- `scripts/csharp/Battle/Simulation/Combat/SimDamage.cs` - IsAlive, Team checks (formerly DamageSystem.cs)
- `scripts/csharp/Battle/Simulation/Combat/SimTargeting.cs` - IsAlive, Team checks (formerly ValidTargetFilter.cs)
- `scripts/csharp/Spells/Effects/SpellEffect.cs` - IsAlive check
- `scripts/csharp/Battle/View/UnitVisual.cs` - Target property access (formerly Unit3D.cs)
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
- `UnitVisual._PhysicsProcess()` runs every frame for every active unit
- `SimSteering.CalculateSeparationForce()` queries spatial grid every frame
- `SimSteering.CalculateFlankForce()` queries spatial grid when blocked
- `UnitMovement.CorrectOverlaps()` triggers additional steering queries
- Render priority recalculates every frame even when position unchanged

**Proposed Fix:**
- Throttle steering queries to run every 2-3 frames instead of every frame
- Cache steering results between updates
- Consolidate multiple `GetUnitsInRadius` calls into single query where possible
- Skip render priority calculation when position unchanged

**Related Files:**
- `scripts/csharp/Battle/View/UnitVisual.cs` (visual shell, formerly Unit3D.cs)
- `scripts/csharp/Battle/Simulation/Movement/SimSteering.cs` (steering logic, formerly UnitSteering.cs)
- `scripts/csharp/Battle/Simulation/Movement/` (movement logic)

---

### 🟢 LOW PRIORITY

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
- `scripts/meta/screens/title_screen.gd`
- `scripts/meta/screens/event_screen.gd`

**Notes:**
- Lower priority - not causing observed issues currently

---

## Multiplayer

### 🟢 LOW PRIORITY

#### Investigate MP Client Casting Signal
**Status:** ⬜ Not Started

Currently, SummonerVisual.PollMatchState emits CastingStarted and CastingCompleted with null card references because the MP client polling path has no card data in MatchState. Investigate whether the casting card ID should be stored in SummonerData so clients can reconstruct the Card object.

**Related Files:**
- `scripts/csharp/Battle/View/SummonerVisual.cs` (PollMatchState method)

---
