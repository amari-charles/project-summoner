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

**Tracker Sync (2026-03-05):** Removed completed `Replace /root/VFXManager Lookup in ProjectileVisual`, moved Puff target-stickiness work to completed (PR `#270`), and removed Wisp single-target verification from active queue after post-refactor validation.
**Audit Sync (2026-03-05, evening):** Moved completed camera boundary/pan task to `todos-completed.md` based on merged camera bounds fixes (`#267`) and unit tests; updated directional/cone attack TODO to reflect partial completion (cone gating in targeting is shipped, hitbox-shape work remains).
**Tracker Sync (2026-03-08):** Moved completed typed-internal service handler refactor and loading-screen preloading work to `todos-completed.md`; updated blocked-idle investigation and `_PhysicsProcess` throttling entries to reflect merged movement/perf commits and remaining verification scope.
**Tracker Sync (2026-03-08, late):** Added missing completion records for GDScript typed-API safety migration (`#288`) and StringName-safe coercion sweep (`#290`) to `todos-completed.md`; active queue unchanged aside from status notes.
**Tracker Sync (2026-03-08, final):** Closed blocked-unit idle freeze item after manual signoff; moved remaining notes to `todos-completed.md` and refreshed AI priority queue to only open items.
**Tracker Sync (2026-03-08, desync pass):** Closed sim/visual state desync audit task after phase sync hardening, summoner destroy signal dedupe, activation-state visual alignment fix, and regression coverage updates.
**Tracker Sync (2026-03-09, combat correctness):** Moved completed DamageProfile armor/magic-resist integration + summoner combat-modifier wiring to `todos-completed.md`; removed UI damage-type card indicator from this task per product direction.
**Tracker Sync (2026-03-10, attack vectors):** Updated `Implement Single Target vs Multi Target Attack System` to partial after runtime V1 delivery (vector recipient resolution + tests); visual telegraphs and balance pass remain.
**Tracker Sync (2026-03-11, attack vector target-limit semantics):** Recorded follow-up fix preserving explicit `TargetLimit` values across presets (`1` single-target, `0` unlimited) while keeping preset defaults when unset; remaining scope for this initiative is still visual telegraphs + balance tuning.
**Tracker Sync (2026-03-10, summoner design):** Added Summoner Oaths planning item (trait-backed permanent choices) and split trait work to prioritize curated, intentional trait design over placeholder AI-generated traits.
**Tracker Sync (2026-03-11, summon traits runtime):** Updated trait-curation item to reflect shipped summon stat-tree runtime (shared trait IDs, per-card/per-rarity overrides, additive + spawn-count hooks, rarity-gated Legion tiers, coverage); remaining scope narrowed to per-summoner identity lines and campaign-level ultimate/oath design validation.
**Tracker Sync (2026-03-11, per-summoner lines):** Simplified per-summoner identity lines to summoner-stat-only V1 (no unit modifiers/triggers) for Cole/Selene/Mei/Teo in `docs/design/summon-traits-v1.md`; remaining trait-curation scope is campaign-facing Ultimate/Oath candidate pass and permanence validation.
**Tracker Sync (2026-03-11, combat spatial v2):** Updated directional attack, multi-target, and hitbox tracker entries to reflect runtime geometry-channel split + debug overlay progress; engage-shape startup alignment remains open.
**Tracker Sync (2026-03-12, quick-win wave):** Closed targeted combat redirect/retarget robustness scope; completed simulation spatial namespace + spawn-rule ownership alignment; completed UI async timeout guards, Puff cone-center offset tuning, and large-unit hit-flash throttling; updated summoner stat audit status and recorded upgrade-cost scaffolding progress.

---

## AI-First Priority Queue (2026-03-08, desync pass)

1. Continue hot-path throttling in `_PhysicsProcess`
   Why first: Recent movement/perf fixes landed, but full scale-target optimization plan is not complete yet.
2. Investigate snapshot system overhaul for deterministic test-state setup
   Why now: Trait/system validation depends on quickly creating exact profile states; current snapshot flow is functional but too indirect and lacks schema/version guarantees.

---

## Ranked Gameplay

### 🟡 MEDIUM PRIORITY

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

## Units & Combat

### 🟡 MEDIUM PRIORITY

#### Shift Puff Attack Angle Downward
**Status:** ✅ Completed
**Category:** Units & Combat / Ranged
**Effort:** Small

**Description:**
Rotate Puff's projectile firing angle cone downward. Keep the same angular spread, but offset the center of the cone so it aims lower.

**Example:**
If current range is -30° to +30° (60° spread centered at 0°), shift to something like -50° to +10° (still 60° spread, but centered at -20°).

**Related Files:**
- Puff unit scene or ranged attack logic
- Projectile spawn angle calculations

**Resolution Update (2026-03-12):**
- ✅ Added cone-center offset support (`TargetingConeCenterOffsetDegrees`) through `UnitDefinition -> SimUnitTemplate -> UnitData`.
- ✅ Set Puff targeting cone center offset to `-20°` and validated with deterministic targeting coverage.

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
**Status:** ✅ Completed
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

**Progress Update (2026-03-10):**
- ✅ Added summoner-wrap movement targeting (`MovementTargetResolver`) so blocked units can route around occupied fronts.
- ✅ Added local crowd danger masking in context steering + tuned blocked-nav and ORCA neighbor search for dense clumps.
- ✅ Added 60-unit summoner-focus regression coverage to verify broad attacker contribution in dense swarms.
- ✅ Ran large-battle profiling pass (2026-03-10) via `dotnet test --settings test.runsettings --filter "FullyQualifiedName~BlockedUnitReproTest.SummonerFocus_DenseSwarm_HasBroadAttackerContribution" --logger "console;verbosity=detailed"`:
  - dense-swarm test case duration: ~1s (`60 units`, `1200` simulation ticks)
- filtered run total: `2.1013s` (test host + discovery + execution)
- ✅ Closed target-switch race/forced-target follow-up with deterministic tie-break selection and forced-target expiry/invalid-target release validation.

**Progress Update (2026-03-11, aggro + air targeting):**
- ✅ Added commit-lock aggro chase cap so units drop non-summoner targets that move beyond max chase distance (`max(aggro radius, attack range)`), preventing infinite far-chase behavior.
- ✅ Added `RetargetReason.OutOfAggroRange` for explicit retarget diagnostics when chase-cap drops occur.
- ✅ Updated ranged targeting profile wiring so ranged units default to `TargetLayer.Both` (air + ground) when definitions rely on the shared default filter.
- ✅ Added regression coverage for commit-lock drop-on-range-exit and ranged profile target-layer mapping.
- ✅ Added regression coverage for forced-target expiry release, invalid forced-target recovery, and stable tie-break ordering.

**Progress Update (2026-03-12, summoner preempt + objective advance):**
- ✅ Added summoner soft-lock aggro preempt so committed summoner targets switch to valid in-aggro enemy units within one tick (`RetargetReason.AggroPreempt`), while forced targets and active attack phases remain non-preemptable.
- ✅ Added no-target objective-advance steering (straight until engage band, then progressive curve toward enemy summoner) and wired it across direct/context movement paths.
- ✅ Fixed forward-rect slot topology minimum orbit radius for positive forward-offset attackers to prevent standstill/no-swing behavior in Pebloom-like melee profiles.
- ✅ Added deterministic regression coverage for summoner-preempt contract, objective-advance movement, and forward-rect idle/attack repros.
- ✅ Added shared summoner melee bubble targeting + stand-ring alignment (sloting, engage checks, and debug controls) to reduce summoner ring-around deadlocks in dense melee swarms.
- ✅ Fixed a radius-only slot topology regression where summoner slot reservation/occupancy metadata could be dropped during orbit-radius rebuilds; radius-only updates now preserve slot ownership.
- ✅ Added regression coverage for summoner bubble radius-only topology updates to ensure existing reservations persist across override/radius retunes.

**Notes:**
- Related to lane-based movement todo (may affect targeting behavior)
- Re-run this profile if future targeting policy changes alter commit-lock behavior.

---

#### Evaluate Non-Hard-Lane Phase 2 Experiments (Post Virtual-Lanes/Roles)
**Status:** ⬜ Not Started
**Category:** Units & Combat / Spatial Design
**Effort:** Medium

**Description:**
Now that virtual lanes + tactical roles are in, run a structured evaluation of the remaining non-hard-lane options from the research doc before committing to another implementation track.

**Candidates To Evaluate:**
- Command cohesion layer (formation/order memory after first contact)
- Engagement cells (soft locality partition for targeting/aggro)
- Frontline tension bands (readability + light behavior weighting)
- Reinforcement routing rules (spawn pressure-sector assignment)
- Objective anchors / side-value injection (off-center tactical value without hard rails)
- Role-specific pursuit budgets (deeper role discipline beyond current prototype)

**Evaluation Method:**
- Define one simulation-first prototype slice per candidate
- Run 40/80/100-unit scenarios with identical seed setups
- Compare against current prototype baseline (virtual lanes + tactical roles)
- Keep changes behavior-only first; defer UI unless candidate passes baseline gates

**Pass Criteria (must beat baseline):**
- Flanks remain viable without collapsing into center aggro
- Midline vortex reduced (more meaningful use of side space)
- Frontline location/readability improves in large fights
- Spawn decisions remain meaningful without hard rails
- CPU/tick cost remains bounded and measurable

**Primary References:**
- `docs/design/lane-system-research-no-lane-identity.md` (Section 5-7 option inventory)
- `scripts/csharp/Battle/Simulation/` (targeting/behavior/movement hot paths)

---


#### Implement Directional/Cone Attack System
**Status:** 🟡 Partial (Cone Targeting Shipped)
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Add support for melee attacks that only hit in a forward cone/arc instead of a full circle. Useful for units with lunge attacks, tongue attacks, or other forward-facing abilities.

**Current State:**
- ✅ Cone-aware reachability/attack checks exist in simulation targeting (`SimTargeting` / `SimBehavior`)
- ✅ Unit data includes cone tunables used by targeting profiles
- ✅ Runtime now separates movement footprint (`NavigationRadius`) from damage contact (`HurtboxRadius`) for projectile/AoE fairness.
- ✅ Debug overlays now split engage gate vs damage shape vs navigation footprint for tuning visibility.
- ⬜ Front-only melee engage geometry and strict engage-vs-damage-shape startup sync are still in progress.

**Requirements:**
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
**Status:** 🟡 Partial (Runtime V1 Shipped)
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Add system to differentiate between single target attacks and multi target/AoE attacks for units.

**Current State:**
- ✅ Spells have AoE via `spell_radius` (Fireball works)
- ✅ Units now support vector-driven multi-target recipient resolution in simulation (`single`, `area`, `line`, `chain`) with deterministic ordering.
- 🔄 Remaining unit-facing work is balance + visual telegraphing for multi-target attacks.

**Progress Update (2026-03-10):**
- ✅ Added grouped attack-vector contract across `UnitDefinition -> SimUnitTemplate -> UnitData`.
- ✅ Implemented deterministic recipient resolution for area (sphere/box/capsule), line corridor, and chain hops.
- ✅ Added simulation test coverage for target limits, deterministic tie-breaks, secondary death handling, and trigger-mode behavior.
- ✅ Follow-up fix (2026-03-11): explicit preset `TargetLimit` values are now preserved (`1` primary-only, `0` unlimited); preset default limits apply only when unset.
- ⬜ Remaining: visual indicators/telegraphs for AoE vectors and gameplay balance pass for multi-target damage tuning.

**Progress Update (2026-03-11):**
- ✅ Added combat spatial split wiring for navigation footprint vs hurtbox contact across simulation movement/projectile paths.
- ✅ Added independent debug overlays for engage range and damage-shape visualization.
- 🔄 Remaining: engage startup must align strictly with authored forward damage shapes for front-only melee readability.

**Requirements:**
- ✅ Define attack target type in unit data (single, multi, aoe)
- ✅ Implement multi-target selection logic for units
- ✅ Add AoE/splash damage radius for area attacks on units
- ⬜ Visual indicators for AoE attacks
- ⬜ Balance damage for multi-target vs single-target

**Notes:**
- Foundation for unit variety (e.g., dragons with breath attacks)
- Multi-target may need reduced damage per target
- Consider different AoE shapes (circle, cone, line)

---

#### Improve Unit Hitboxes
**Status:** 🟡 Partial (Runtime Channels Shipped)
**Category:** Units & Combat
**Effort:** Medium

**Description:**
Flesh out and refine unit hitboxes for better collision detection and combat interactions.

**Current State:**
- ✅ Runtime data now carries separate `NavigationRadius` and hurtbox fields (`HurtboxRadius`, `HurtboxHeight`, `HurtboxHorizontal`, `HurtboxOffset`).
- ✅ Projectile contact and AoE radius checks read hurtbox channels instead of movement spacing.
- 🔄 Remaining: finish per-unit authored shape tuning and align melee engage gating with forward attack shapes.

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
**Status:** 🟡 Partial (Scaffolding Added)
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

**Progress Update (2026-03-12):**
- ✅ Added optional level-up resource-cost contract in card progression info (`level_up_resource_cost`, `has_level_up_resource_cost`).
- ✅ Added CardService-level spend/refund wiring + API bridge accessor.
- ⬜ Remaining: author real catalog-defined costs and UI affordability display/UX.

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
**Status:** ✅ Completed
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

**Resolution Update (2026-03-12):**
- ✅ Added configurable flash rate-limiting in both `SpriteVisualComponent` and `SkeletalVisualComponent`.
- ✅ Added separate minimum flash interval for large units via width threshold tuning.

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

#### Add Battle-Start Cinematic Camera Nudge
**Status:** ⬜ Not Started
**Category:** Visual Polish / Camera
**Effort:** Small

**Description:**
Add a subtle automatic camera intro at battle start (slight zoom-in motion) to improve scene presentation before normal player camera control takes over.

**Requirements:**
- Trigger once at battle start (non-looping)
- Keep motion subtle and brief
- Preserve current gameplay camera framing after intro
- Avoid interfering with manual zoom/pan behavior

**Notes:**
- Keep this strictly cosmetic (no gameplay timing impact)
- Coordinate with existing battle initialization flow in `BattleScene`

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
**Status:** ✅ Completed
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

**Resolution Update (2026-03-12):**
- ✅ Verified `damage_bonus`/`damage_reduction` are actively consumed in simulation damage paths.
- ✅ Added clarifying in-code documentation for summoner-vs-unit and summoner-target lane behavior.
- ✅ No dead-field removal required in current runtime.

**Related Files:**
- `scripts/infrastructure/data/summoner_instance.gd` - `get_computed_stats()`
- `scripts/csharp/Battle/Simulation/Combat/SimDamage.cs` - damage calculations
- `scripts/csharp/Infrastructure/Data/Traits/TraitDefinitions.cs` - trait definitions

---

#### Define Summoner Oaths (Trait-Backed Campaign Choices)
**Status:** ⬜ Not Started
**Category:** Summoners / Identity
**Effort:** Medium

**Description:**
Add summoner Oaths as explicit, high-impact choices shown to the player in progression flow. Each chosen Oath is persisted as a permanent trait entry on that summoner and drives future gameplay modifiers/conditions.

**Requirements:**
- Define initial Oath sets per summoner (2-4 options each)
- Specify where Oath choice appears in summoner progression flow/UI
- Persist selected Oath as trait data on the summoner profile
- Ensure Oath traits resolve through existing trait runtime hooks (modifiers/triggers)
- Document Oath interactions with Level Traits, Story Traits, and Ultimate Traits

**Related Files:**
- `scripts/csharp/Infrastructure/Data/Traits/TraitDefinitions.cs`
- `docs/features/summoners/progression-system.md`
- `docs/design/trait-tree-screen-flow-spec.md`
- `scripts/csharp/Meta/Progression/Core/ProgressionState.cs`

**Notes:**
- Oaths should be irreversible per summoner to reinforce Fateforged's permanent-choice identity.
- Deferred until campaign-fleshing pass; trait curation remains the active priority now.

---

#### Curate Summoner Trait Catalog (Replace Placeholder AI Traits)
**Status:** 🔄 In Progress (Summon Runtime Pass Complete)
**Category:** Summoners / Traits
**Effort:** Large

**Description:**
Replace placeholder AI-generated trait content in the current workstream with curated, intentional trait lines that reinforce summoner identity, army doctrine, and meaningful tradeoffs.

**Tasks:**
- [x] Audit summon-trait entries and replace placeholder/generated stat lines with curated v1 values
- [x] Define summon-focused v1 trait sheet with tier bounds and stat priorities (`docs/design/summon-traits-v1.md`)
- [x] Author summon trait-line tier chains with prerequisites in runtime catalog
- [x] Implement card-rarity gating for `Legion` tiers (Common: IV, Rare: III, Epic: II, Legendary: none)
- [x] Implement per-card/per-rarity trait value override plumbing while keeping shared trait names
- [x] Wire additive stat and spawn-count trait effects through card effective stats + simulation runtime
- [x] Add deterministic coverage for evaluator gating, override resolution, and spawn-count/runtime behavior
- [x] Produce summon-focused curated trait draft: `docs/design/summon-traits-v1.md`
- [x] Define non-summon per-summoner identity trait lines (doctrine/tradeoff focus)
- [ ] Author campaign-facing Ultimate/Oath trait candidates and validate permanence/exclusivity interactions
- [ ] Evaluate simplifying the base trait tree: if traits are non-interconnected, represent each series stage with a single swappable UI element and add a clear progression display model

**Related Files:**
- `scripts/csharp/Infrastructure/Data/Traits/TraitDefinitions.cs`
- `docs/features/summoners/progression-system.md`
- `docs/project/vision.md`
- `docs/design/summon-traits-v1.md`

**Notes:**
- This replaces generic placeholder trait generation as the active trait work item.
- Favor mechanics that create visible doctrine changes over flat stat inflation.
- Summon stat-tree foundation is now implemented; remaining design scope is identity/campaign-layer trait work.

---

#### Implement Summoner Special Abilities
**Status:** 🟨 In Progress (Phase 2/5)
**Category:** Summoners
**Effort:** Large

**Description:**
Implement the runtime system for summoner active/passive abilities after the curated trait catalog is locked.

**Notes:**
- Unified trait runtime/data scaffolding (Pass 2) is in place and wired into simulation/session.
- Trait points are now deferred spend for summoners/cards; level-up no longer forces immediate picks.
- Current priority: curated non-placeholder trait + oath design pass.
- Pending runtime: evaluator/triggers, offer rolling, and full validation matrix completion.
- Phase 3: Curated Level Traits (trait selection at level-up)
- Phase 4: Ultimate Traits (level 10 capstone abilities)
- Phase 5: End-to-end validation matrix + progression tuning
- Foundation is ready via TraitCatalog modifier system

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

#### Replace Runtime Entity `int` IDs with Typed Value Objects
**Status:** ⬜ Not Started
**Category:** Architecture / Type Safety
**Effort:** Medium

**Description:**
Simulation/runtime entity references still use raw `int` IDs in many places (`UnitId`, projectile IDs, target IDs, network IDs). This is brittle because ID domains can be mixed accidentally and invalid combinations are not caught at compile time.

Migrate runtime IDs to strongly typed value objects (for example `UnitInstanceId`, `ProjectileInstanceId`, `NetworkEntityId`) while keeping deterministic behavior and snapshot/network compatibility.

**Goals:**
- Prevent ID-domain mixups at compile time
- Improve readability of hot-path code (`MatchState`, targeting, combat, snapshots)
- Reduce "magic negative ID" conventions for special targets

**Migration Notes:**
- Prefer `readonly record struct` wrappers around `int` values
- Keep wire format/snapshots stable by converting at serialization boundaries
- Do incrementally (type aliases/adapters first, then deep replacement)

**Likely Files:**
- `scripts/csharp/Battle/Simulation/Data/MatchState.cs`
- `scripts/csharp/Battle/Simulation/Data/UnitData.cs`
- `scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs`
- `scripts/csharp/Battle/Simulation/Combat/SimTargeting.cs`
- `scripts/csharp/Battle/Simulation/Simulation.cs`
- Session protocol/snapshot builders (`scripts/csharp/Battle/Session/...`)

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

#### Create Simulation Spatial Domain (Folder + Namespace Alignment)
**Status:** ✅ Completed
**Category:** Architecture / Layering
**Effort:** Small

**Description:**
Formalize simulation world-rule ownership by introducing a dedicated `Simulation/Spatial` slice for geometry/partition/zone logic. This prevents cross-cutting world logic from being dropped into arbitrary folders and keeps deterministic runtime ownership clear.

**Initial Scope:**
- Move `VirtualLanes` from simulation root into `scripts/csharp/Battle/Simulation/Spatial/VirtualLanes.cs`
- Use `namespace Fateforged.Simulation.Spatial`
- Update simulation consumers (`Simulation`, `Movement`, `Combat`) to depend on `Spatial` types
- Keep this refactor behavior-preserving (placement + namespace only)

**Placement Rule (for future files):**
- `Simulation/Spatial` = world geometry, partitions, lane/zone math, ownership maps
- `Simulation/Movement` = unit locomotion and steering decisions
- `Simulation/Combat` = targeting, damage, attack execution

**Likely Follow-up:**
- Evaluate moving `BattlefieldBounds` to simulation-owned spatial namespace once safe migration plan is defined

**Resolution Update (2026-03-12):**
- ✅ Moved `VirtualLanes` to `scripts/csharp/Battle/Simulation/Spatial/VirtualLanes.cs`.
- ✅ Updated simulation movement/combat consumers to `Fateforged.Simulation.Spatial`.
- ✅ Refactor remained behavior-preserving (namespace + placement only).

---

#### Refactor Reward System to Typed RewardSpec Classes
**Status:** 🟡 Partial (Reward Claim + Screen Hardening Landed)
**Category:** Architecture / Flag Proliferation
**Effort:** Medium

**Description:**
Replace the dictionary-based reward spec with polymorphic C# classes. The `get_reward_spec()` method returns a unified dictionary with flags (`is_replay`, `requires_choice`, etc.) - these should be type-specific classes.

**Current Problem:**
- `reward_service.gd:85-95` builds spec dictionary with multiple flags
- `reward_screen.gd:124-173` checks `is_replay` and `requires_choice` flags
- Flag combinations create complex conditional logic

**Progress Update (2026-03-09):**
- ✅ Mission completion reward flow now uses `BattleRewardSpec`-derived grants in `CampaignRewardHandler` (flexible rewards + campaign gold included)
- ✅ Pending flexible choice now persists stable `chosen_catalog_id` to prevent index drift across resume/claim
- ✅ RewardScreen current-battle resolution hardened for per-summoner campaign progress and pending-reward fallback
- ✅ Regression coverage added for claim flow, mission completion flow, and choice-drift scenario
- ⬜ Remaining: Replace dictionary flag checks in `RewardScreen` with typed `RewardSpec` subclasses and eliminate `is_replay`/`requires_choice` branching
- ⬜ Remaining: Introduce explicit `RewardSpec` class hierarchy + factory (`RewardSpecFactory`) and migrate remaining consumers

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

**Progress Update (2026-03-08):**
- ✅ Landed simulation-side movement/perf wins (ORCA neighbor capping + context buffer reuse)
- 🔄 Remaining: complete visual/update-frequency throttling and render-priority skip path

**Related Files:**
- `scripts/csharp/Battle/View/UnitVisual.cs` (visual shell, formerly Unit3D.cs)
- `scripts/csharp/Battle/Simulation/Movement/SimSteering.cs` (steering logic, formerly UnitSteering.cs)
- `scripts/csharp/Battle/Simulation/Movement/` (movement logic)

---

### 🟢 LOW PRIORITY

#### Introduce Battle Composition Root + Dependency Injection
**Status:** ⬜ Not Started
**Category:** Architecture / Maintainability
**Effort:** Medium

**Description:**
Battle systems still rely on service-locator style autoload lookups (`/root/...`) inside runtime systems (EntityManager, BattleScene, session wiring). This couples systems to scene tree naming and makes tests harder to isolate.

**Proposed Fix:**
- Define small service interfaces for battle dependencies (`IVfxService`, `IAudioService`, etc.)
- Resolve autoload nodes once in a composition root (`BattleScene`/factory layer)
- Inject typed dependencies into runtime systems instead of looking up autoloads from inside those systems
- Keep dynamic `Call()` isolated behind adapter classes at interop boundaries

**Related Files:**
- `scripts/csharp/Battle/View/BattleScene.cs`
- `scripts/csharp/Battle/View/EntityManager.cs`
- `scripts/csharp/Battle/Session/BattleSessionFactory.cs`

**Notes:**
- Do this incrementally during normal battle refactors, not as one large rewrite.
- Team decision: use lightweight, feature-scoped DI (small interfaces + explicit `Init(...)` injection from `BattleScene`).
- Team decision: avoid a large global `GameServices` container; it can become another service locator.
- Team decision: apply DI first to high fan-out battle services (`VFX`, `Audio`, session-related adapters), then expand only when it reduces duplication.

---

#### Move Tutorial Dialogue Triggers to Sim Events
**Status:** ⬜ Not Started
**Category:** Architecture / Battle Flow
**Effort:** Medium

**Description:**
`battle_dialogue_controller.gd` still evaluates gameplay proximity conditions from scene nodes in `_process`. This is high-level orchestration logic, but trigger criteria are gameplay-derived and should come from simulation events/state to avoid visual/sim drift.

**Proposed Fix:**
- Emit explicit sim-side event(s) for tutorial trigger conditions (example: enemy entered base threat radius)
- Consume those events in battle dialogue controller (or a dedicated bridge)
- Remove per-frame node-group distance scans from GDScript

**Related Files:**
- `scripts/battle/battle_dialogue_controller.gd`
- `scripts/csharp/Battle/Simulation/Simulation.cs`
- `scripts/csharp/Battle/View/EntityManager.cs`

---

#### Consolidate Battlefield Spawn Rules to C# Source-of-Truth
**Status:** ✅ Completed
**Category:** Architecture / Consistency
**Effort:** Small

**Description:**
`battlefield_constants.gd` still carries spawn-rule helpers (`is_valid_spawn_position_for_team`, `clamp_spawn_position_for_team`) that mirror C# `BattlefieldBounds`. Mirrored rule logic can drift and bypass debug flags.

**Proposed Fix:**
- Keep visual constants in `battlefield_constants.gd` (overlay offsets, sizes)
- Remove or deprecate spawn-rule helpers from GDScript
- Route all spawn validation/clamping through `BattlefieldBounds` in C#

**Related Files:**
- `scripts/battle/battlefield/battlefield_constants.gd`
- `scripts/csharp/Infrastructure/Constants/BattlefieldBounds.cs`
- `scripts/csharp/Battle/Input/InputCollector.cs`

**Resolution Update (2026-03-12):**
- ✅ Removed mirrored spawn-rule helpers from `battlefield_constants.gd`.
- ✅ Kept spawn validation/clamping authority in C# `BattlefieldBounds`.
- ✅ Updated GDScript tests to cover remaining conversion/constants behavior.

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
**Status:** ✅ Completed
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
- Completed with timeout + fallback guards in title/event screen async flows.

---

## Multiplayer

### 🟢 LOW PRIORITY
