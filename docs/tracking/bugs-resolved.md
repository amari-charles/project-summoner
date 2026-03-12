# Resolved Bugs Archive

This document archives bugs that have been fixed. For active bugs, see [bugs.md](bugs.md).

---

## 2026-03 Fixes

### RID/Resource Leaks at Exit in Headless Mode
**Resolved:** 2026-03-12
**Component:** Unit Testing / Godot Headless

**Description:**
Headless GUT runs were intermittently ending with `Leaked unsafe reference`, `ObjectDB instances leaked`, and mono binding fatal shutdown signatures.

**Root Cause:**
`JsonProfileStore` created `DirAccess` and `Json` Godot objects without deterministic disposal in multiple load/save paths, leaving unsafe references at process teardown.

**Solution Implemented:**
1. Added deterministic disposal (`using var`) for `DirAccess` and `Json` instances in `JsonProfileStore`.
2. Re-ran required review validation suites and checked shutdown signatures with the specified headless GUT command.
3. Confirmed no matching leak/fatal signatures in the validation output.

**Related Files:**
- `scripts/csharp/Infrastructure/Persistence/JsonProfileStore.cs`
- `docs/tracking/bugs.md`

---

### Puff Units Get Stuck in Idle When Blocked by Other Units
**Resolved:** 2026-03-08
**Component:** Units / Pathfinding / Movement

![Units stuck in idle when blocked](images/bug-units-stuck-idle-blocked.png)

**Description:**
Puff units could get stuck in idle/pathfinding failure states when blocked by other units, reducing effective army participation.

**Root Cause:**
Blocked-navigation reset and movement intent handling had edge cases that could leave units in non-advancing behavior loops under congestion.

**Solution Implemented:**
1. Reworked movement intent + blocked-navigation pipeline and fixed blocked-reset edge case.
2. Added deterministic regression coverage for the blocked-unit repro scenario.
3. Completed manual in-battle validation signoff (mixed formations/top-bottom lane congestion).

**PR/Commit Context:**
- PR `#287` (`refactor(simulation): movement intent + ORCA pipeline and stability fixes`)
- Commit `27462750` (blocked-nav reset edge-case fix)

**Related Files:**
- `scripts/csharp/Battle/Simulation/Movement/BlockedNavigationController.cs`
- `scripts/csharp/Battle/Simulation/Movement/SimMovement.cs`
- `scripts/csharp/Battle/Simulation/Movement/SimSteering.cs`
- `tests/csharp/Simulation/BlockedUnitReproTest.cs`

---

### Battlefield Ground Checker/Biome Visuals Regress to Default
**Resolved:** 2026-03-08
**Component:** Battlefield / Biome Visuals / GDScript Interop

**Description:**
Ground checker/biome visuals appeared incorrect after typed-safety refactors. The battlefield fell back to default biome behavior despite valid battle context.

**Root Cause:**
`BattleContext.biome_id` is stored as `StringName`, but biome extraction path switched to `SafeTypeUtils.string(...)` at a point where the helper accepted only `String`. This produced an empty biome ID and triggered visual fallback.

**Solution Implemented:**
1. Updated `SafeTypeUtils.string()` to accept both `String` and `StringName`.
2. Performed a broader coercion sweep to replace strict `Variant is String` guards in key UI/event/data flows.
3. Added regression tests for StringName-safe coercion and typed event/property paths.

**PR Merge Date:** 2026-03-08 (`#290`)

**Related Files:**
- `scripts/infrastructure/safe_type_utils.gd`
- `scripts/battle/battlefield/base_battlefield_3d.gd`
- `scripts/meta/components/node_panels/typed_event_data.gd`
- `tests/unit/test_safe_type_utils.gd`
- `tests/unit/test_typed_event_data.gd`

---

### Homing Projectiles Orbit Dead Targets Indefinitely
**Resolved:** 2026-03-06
**Component:** Simulation / Projectiles

**Description:**
Homing and WeavingHoming projectiles would orbit the last known position of a dead target until lifetime expiry (up to 6 seconds), making it look like projectiles "won't despawn."

**Root Cause:**
`TickHoming` continued steering toward `TargetPosition` when `GetAliveUnit` returned null. No path-completion check existed for Homing/WeavingHoming types.

**Solution Implemented:**
Track `PreviousDistanceToTarget` and `TargetLost` state on `SimProjectileData`. When the target dies and the projectile's distance to last known position starts increasing (it has passed through), kill it immediately.

**Related Files:**
- `scripts/csharp/Battle/Simulation/Combat/SimProjectile.cs`
- `scripts/csharp/Battle/Simulation/Data/SimProjectileData.cs`

---

### Straight/Arc Projectiles Miss Moving Targets (Tracking Property Dead Code)
**Resolved:** 2026-03-06
**Component:** Simulation / Projectiles

**Description:**
WindPuff and Rock projectiles had `Tracking = true` in their definitions but the property was never read by the simulation. If the target moved, projectiles hit empty space and flew off until lifetime.

**Root Cause:**
`ProjectileData.Tracking` was defined and parsed but `SimProjectile.TickStraight()` and `TickArc()` never checked it — they interpolated a fixed path from start to original target position.

**Solution Implemented:**
Added `Tracking` field to `SimProjectileData`, threaded it through `Spawn()`, and implemented target position updates in `TickStraight`/`TickArc` via shared `ProjectileMovement` methods.

**Related Files:**
- `scripts/csharp/Battle/Simulation/Combat/SimProjectile.cs`
- `scripts/csharp/Battle/Simulation/Combat/ProjectileMovement.cs` (new)
- `scripts/csharp/Battle/Simulation/Data/SimProjectileData.cs`

---

### Client Arc/Ballistic Projectiles Render as Straight Lines
**Resolved:** 2026-03-06
**Component:** Multiplayer / Projectile Visuals

**Description:**
On multiplayer clients, Arc and Ballistic projectiles flew in straight lines instead of following their intended curves.

**Root Cause:**
`ClientSession.TickClientProjectileMovement()` used `direction * speed * delta` for all non-Homing types — a straight line. Arc and Ballistic path math was never implemented on the client.

**Solution Implemented:**
Extracted shared `ProjectileMovement` class with `TickArc()`, `TickBallistic()`, and all geometry helpers. Both `SimProjectile` (host) and `ClientSession` (client) now call the same shared methods, eliminating the discrepancy.

**Related Files:**
- `scripts/csharp/Battle/Simulation/Combat/ProjectileMovement.cs` (new)
- `scripts/csharp/Battle/Session/ClientSession.cs`

---

### Client Speed Easing Exponent Mismatch
**Resolved:** 2026-03-06
**Component:** Multiplayer / Projectile Visuals

**Description:**
Projectile speed curves differed between host and client, causing visual desync for projectiles with speed easing.

**Root Cause:**
Host clamped exponent to `MathF.Max(exponent, 0.0001f)` while client clamped to `MathF.Max(exponent, 1f)`, producing different easing curves.

**Solution Implemented:**
Shared `ProjectileMovement.EvaluateSpeedEasing()` with correct `0.0001f` minimum is used by both host and client.

**Related Files:**
- `scripts/csharp/Battle/Simulation/Combat/ProjectileMovement.cs` (new)

---

### Client Weaving Projectiles Veer in Wrong Direction
**Resolved:** 2026-03-06
**Component:** Multiplayer / Projectile Visuals

**Description:**
WeavingHoming projectiles on client could veer in the opposite direction from the host because the client derived veer direction from `ProjectileId & 1` while the host used deterministic RNG.

**Solution Implemented:**
Added `VeerDirection` and `CounterVeerDirection` fields to `ProjectileSpawned` and `ActiveProjectileSeed` network messages. Host sends the exact veer vectors it computed; client uses them directly.

**Related Files:**
- `scripts/csharp/Battle/Session/Protocol/Messages.cs`
- `scripts/csharp/Battle/Session/Protocol/MessageSerializer.cs`
- `scripts/csharp/Battle/Session/HostSession.cs`
- `scripts/csharp/Battle/Session/ClientSession.cs`

---

### Puff Pivot Point Off-Center When Turning
**Resolved:** 2026-03-05
**Component:** Units / Visual / Sprites

**Description:**
Puff shifted/teleported sideways when changing facing direction because sprite flipping mirrored around texture center while Puff art was visually off-center.

**Root Cause:**
Horizontal compensation was previously tied to world-space visual offset patterns and lacked robust per-animation pivot metadata in sprite space.

**Solution Implemented:**
1. Replaced manual sprite-offset workflow with metadata-driven per-animation pivots (`AnimationPivotOffsets`).
2. Applied pivot offset in viewport sprite space (`CharacterSprite.Position.X`) and mirrored on flip, while keeping `Sprite3D.Position.X = 0` so spawn/world anchors stay centered.
3. Added runtime alignment refresh on animation/frame/scale changes to prevent drift during animated scale effects.
4. Added regression tests for:
   - flip mirror behavior with stable world anchor
   - frame-size-driven re-alignment
   - scale-change re-alignment
5. Tuned Puff scene grounding/offset config (`FeetOffsetPixels`, pivot map) and validated in battle.

**Related Files:**
- `scripts/csharp/Battle/View/Visual/SpriteVisualComponent.cs`
- `scenes/battle/units/puff_3d.tscn`
- `tests/csharp/View/SpriteVisualComponentTest.cs`

---

### Camera Boundary Issues (Scroll Wheel + Right-Click Drag)
**Resolved:** 2026-03-05
**Component:** Camera / Input

**Description:**
Two camera boundary bugs were active:
1. Scroll-wheel zoom could expose space outside the battlefield.
2. Right-click drag at boundary edges could feel glitchy (overshoot then snap back).

**Root Cause:**
- Zoom ceiling used a simplified size formula that did not account for the actual projected ground footprint.
- Drag panning applied full movement first, then corrected by clamping, causing visible snap-back near edges.
- Edge-pan and drag-pan needed explicit separation.

**Solution Implemented:**
1. Reworked bounds logic to use real projected footprint (`get_ground_footprint_xz`) for clamping.
2. Added zoom-limit solver (`_solve_max_ortho_size`) that binary-searches max safe orthographic size.
3. Added viewport resize handling (`size_changed`) to recompute safe zoom limits.
4. Constrained drag movement before applying (`_constrain_pan_motion_to_map`) so panning stops cleanly at edges.
5. Disabled edge-pan during active drag panning.
6. Added temporary debug overlay toggle on camera to visualize:
   - Green rectangle = map bounds
   - Red rectangle = current camera footprint
7. Added regression tests for zoom/pan boundary invariants and drag/edge-pan behavior.

**Validation:**
- Local suite run via `tools/run_tests.sh --gut-only`: 180/180 passing.

**Superseded:** 2026-03-09

Orthographic battle camera paths referenced in this fix (including `_solve_max_ortho_size`) were removed in PR #297 (`refactor(camera): remove orthographic battle camera support`). Battle camera runtime is now perspective-only.

**Related Files:**
- `scripts/battle/battlefield/camera_controller_3d.gd`
- `tests/unit/test_camera_controller_3d.gd`
- `docs/tracking/bugs.md`

---

### Puff Units Switch Targets Unnecessarily
**Resolved:** 2026-03-05
**Component:** Units / Targeting / Ranged AI

**Description:**
Puff units were switching targets too aggressively, often abandoning a currently attackable cone target to chase a different "higher score" target that required movement.

**Root Cause:**
Target re-acquisition used score-first selection after lock expiry and did not preserve a valid current target. Cone-attack units could switch to a closer but less immediately attackable target.

**Solution Implemented:**
1. Introduced policy-based target selection with explicit `TargetPolicyId` strategies.
2. Added `PreferAttackableAndStickTargetPolicy` to keep current targets when still attackable.
3. Updated targeting to prioritize attackable-now candidates before fallback score-only selection.
4. Wired unit targeting profiles to use `PreferAttackableAndStick` where appropriate.
5. Added simulation coverage for keep-current and cone-aware switching behavior.

**PR Merge Date:** 2026-03-05 (`#270`)

**Related Files:**
- `scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs`
- `scripts/csharp/Battle/Simulation/Combat/SimTargeting.cs`
- `scripts/csharp/Battle/Simulation/Combat/Targeting/PreferAttackableAndStickTargetPolicy.cs`
- `scripts/csharp/Infrastructure/Data/Units/UnitDefinitions.cs`
- `tests/csharp/Simulation/SimBehaviorTest.cs`
- `tests/csharp/Simulation/SimTargetingTest.cs`

---

### Wisps Attack Multiple Enemies Simultaneously
**Resolved:** 2026-03-05
**Component:** Units / Combat / Targeting

**Description:**
Wisp units were previously reported as attacking multiple enemies simultaneously instead of honoring a single target.

**Root Cause:**
This issue appears to have been tied to pre-refactor targeting/combat flow. After the major host-authoritative simulation migration and follow-up targeting policy refactor, the old multi-target behavior is no longer reproducible.

**Resolution Outcome:**
1. Confirmed current sim path uses single-target acquisition/execution per unit attack tick.
2. Verified post-refactor behavior in current build: wisps now behave as single-target units.
3. No additional code fix required beyond merged refactor work.

**Refactor Context:**
- Host-authoritative simulation rewrite (merged 2026-03-04, `#260`)
- Policy-based targeting refactor (merged 2026-03-05, `#270`)

**Related Files:**
- `scripts/csharp/Battle/Simulation/Combat/SimBehavior.cs`
- `scripts/csharp/Battle/Simulation/Combat/SimTargeting.cs`
- `scripts/csharp/Infrastructure/Data/Units/UnitDefinitions.cs`

---

### Enemy Spawn Debug Mode Issues
**Resolved:** 2026-03-04
**Component:** Debug Tools / Spawning

**Description:**
Debug unit spawns with "Spawn as Enemy" could end up in invalid positions and did not consistently respect spawn-side boundary rules or debug bypass state.

**Root Cause:**
`InputCollector` debug DnD path did not validate/clamp debug spawn positions with the shared boundary utilities, and bypass state was not routed through the same source-of-truth.

**Solution Implemented:**
1. Updated debug spawn preview/drop path to use `BattlefieldBounds.IsValidSpawnPositionForTeam()` and `BattlefieldBounds.ClampToValidSpawnZone()`
2. Applied clamping before `card.SpawnAt(...)` in debug drop flow
3. Wired debug boundary bypass toggle through `BattlefieldDebugService` for consistent behavior with debug settings

**PR Merge Date:** 2026-03-04 (`8cab9d0d`, merge commit not tagged with PR number)

**Related Files:**
- `scripts/csharp/Battle/Input/InputCollector.cs`
- `scripts/debug/debug_menu.gd`
- `scripts/csharp/Debug/BattlefieldDebugService.cs`
- `scripts/csharp/Infrastructure/Constants/BattlefieldBounds.cs`

---

### CardIDs.DUCKLING References Non-Existent Card
**Resolved:** 2026-03-04
**Component:** Data / Card Catalog

**Description:**
`CardIDs` included `DUCKLING`, but duckling is a spawned unit (from `mama_duck`) rather than a playable card. This produced a startup validation error for a non-existent card ID.

**Root Cause:**
Stale `CardIDs` constant after card/unit catalog cleanup and C# catalog migration.

**Solution Implemented:**
Removed `DUCKLING` from `card_ids.gd` and kept duckling modeled only as a unit ID.

**PR Merge Date:** 2026-03-04 (`#260`)

**Related Files:**
- `scripts/infrastructure/data/card_ids.gd`

---

## 2026-01 Fixes

### HP Bar Management Issues
**Resolved:** 2026-01-09
**Component:** UI / HP Bar Manager

**Description:**
HP bars had lifecycle and positioning issues, especially around mass cleanup scenarios (swarm units, clear-all, scene transitions).

**Root Cause:**
Legacy GDScript HP bar lifecycle relied on cleanup assumptions that failed during bulk frees and scene teardown.

**Solution Implemented:**
Migrated HP bars to C# service-driven lifecycle with explicit cleanup and integration tests. Legacy GDScript HP bar manager/scripts were removed.

**PR Merge Date:** 2026-01-09 (`Merge feature/hp-bar-csharp-migration`)

**Related Files:**
- `scripts/csharp/Meta/Services/HPBarService.cs`
- `scripts/csharp/Battle/View/UI/FloatingHPBar.cs`
- `tests/integration/test_hp_bar_lifecycle.gd`

---

### Puff Projectiles Not Triggering Hit Flashes
**Resolved:** 2026-01-29
**Component:** Combat / VFX / Projectiles

**Description:**
Puff unit projectiles were not triggering hit flash effects when they hit enemies.

**Root Cause:**
Architecture inconsistency: Melee hits went through HitResolver (which emits `HitConfirmed` signal that VFX systems listen to), but projectiles called DamageSystem directly, bypassing the signal emission.

**Solution Implemented:**
1. Added `ResolveProjectileHit()` method to HitResolver that takes raw parameters (source, target, damage, damageType, hitPosition) instead of requiring HitboxComponent
2. Refactored HitResolver to share core logic via `ResolveHitCore()` between melee and projectile hits
3. Updated Projectile3D to route all damage through HitResolver:
   - `HitTarget()` - direct projectile hits
   - `HitTargetViaHurtbox()` - hits detected via hurtbox collision
   - `ApplyAoeDamage()` - area-of-effect damage
4. Added configurable `FlashColor` export to SpriteVisualComponent for light-colored units (Puff uses pink flash instead of white)
5. Added matching `FlashColor` export to SkeletalVisualComponent for consistency

**Related Files:**
- `scripts/csharp/Battle/Simulation/Combat/Hitbox/HitResolver.cs` - Added ResolveProjectileHit, ResolveHitCore
- `scripts/csharp/Projectiles/Projectile3D.cs` - Route all damage through HitResolver
- `scripts/csharp/Battle/View/Visual/SpriteVisualComponent.cs` - Added FlashColor export
- `scripts/csharp/Battle/View/Visual/SkeletalVisualComponent.cs` - Added FlashColor export
- `scenes/battle/units/puff_3d.tscn` - Set FlashColor to pink (1.3, 0.85, 0.85, 1)

---

### Units Stuck in FlashWhite Visual State After Being Attacked
**Resolved:** 2026-01-27
**Component:** Units / Visual / Combat Feedback

**Description:**
Units were getting stuck in the white flash visual state after being attacked. The FlashWhite effect that triggers on damage was not properly resetting.

**Root Cause:**
Race condition when `FlashWhite()` was called multiple times in rapid succession (unit taking damage from multiple sources). Each call captured the current modulate as `originalColor`. If a second call occurred during an active flash tween, it would capture the bright white color (2.0, 2.0, 2.0, 1.0) as "original" instead of the true original color. When tweens completed, units would reset to the wrong color.

**Solution Implemented:**
1. Added `_originalModulate` field to store the true original color once (initialized to `Colors.White`)
2. Added `_flashTween` field to track the active tween
3. Modified `FlashWhite()` to:
   - Kill any existing flash tween before starting a new one
   - Reset modulate to stored original before starting new flash
   - Always tween back to the stored original color

Applied fix to both visual components:
- `SkeletalVisualComponent.cs` (skeletal rigs like Fire Wisp)
- `SpriteVisualComponent.cs` (sprite-based units like Puff)

**Related Files:**
- scripts/csharp/Battle/View/Visual/SkeletalVisualComponent.cs
- scripts/csharp/Battle/View/Visual/SpriteVisualComponent.cs

---

### Battle Victory Rewards UI Missing Localization
**Resolved:** 2026-01-27
**Component:** UI / Rewards / Localization

**Description:**
After completing battles, the victory rewards screen showed `[[MISSING:ui.rewards.guaranteed]]` for guaranteed reward badges.

**Root Cause:**
Localization key mismatch. `reward_screen.gd` used plural `ui.rewards.guaranteed` but `en.json` defined the key at singular `ui.reward.guaranteed`.

**Solution Implemented:**
Fixed the key in `reward_screen.gd:356` to use `ui.reward.guaranteed` matching the localization file.

**Related Files:**
- scripts/meta/screens/reward_screen.gd
- localization/data/en.json

---

### Fire Wisp Missing Right Leg
**Resolved:** 2026-01-27
**Component:** Units / Art / Visual

**Description:**
The Fire Wisp unit was missing its right leg in the visual representation.

**Root Cause:**
Commit c8b7f1e4 refactored `SkeletalVisualComponent.cs` from dynamic bounds-based viewport sizing to explicit `FeetLocalPosition` parameters. The wisp units were not updated with proper `ViewportSize` values, leaving them at the default 1200x1200. This oversized viewport caused rendering issues with the scaled-down content (0.15 scale factor), resulting in the right leg not being visible.

**Solution Implemented:**
Added explicit `ViewportSize = Vector2i(300, 350)` to all wisp unit scenes to match their scaled content size. This provides a viewport properly sized for the `ContentSize = Vector2(500, 800)` at `ScaleFactor = Vector2(0.15, 0.15)`.

Applied fix to all 8 wisp variants:
- fire_wisp_3d.tscn
- water_wisp_3d.tscn
- earth_wisp_3d.tscn
- wind_wisp_3d.tscn
- lightning_wisp_3d.tscn
- life_wisp_3d.tscn
- death_wisp_3d.tscn
- shadow_wisp_3d.tscn

**Related Files:**
- scenes/battle/units/*_wisp_3d.tscn
- scripts/csharp/Battle/View/Visual/SkeletalVisualComponent.cs

---

### Campaign State Not Persisting on Restart
**Resolved:** 2026-01-27
**Component:** Campaign / Save System / Persistence

**Description:**
Campaign progress was lost when the game restarted. Players could not resume campaigns where they left off.

**Root Cause:**
Two bugs in the GDScript/C# interop layer:

1. **Reading issue**: `DtoConverters.FromProfileDict()` was not converting the `meta` dictionary from GDScript to C#. This meant `ProfileData.Meta.SelectedSummoner` was always empty when loaded. When `SummonerSelectionService.GetActiveSummonerId()` returned empty string, `CampaignProgressHandler.LoadProgress()` silently returned without loading any progress.

2. **Writing issue**: `SummonerSelectionService.SetActiveSummoner()` was modifying a *snapshot copy* of the profile (from `GetProfileSnapshot()`) instead of calling the GDScript `update_profile_meta()` function. The subsequent `SaveProfile()` call saved the unchanged GDScript `_data`, so the `selected_summoner` change was never persisted.

**Solution Implemented:**
1. Added `FromMetaDict()` helper method in `DtoConverters.cs` to convert the `meta` dictionary including `selected_summoner`
2. Added meta conversion in `FromProfileDict()` to populate `ProfileData.Meta`
3. Added `UpdateProfileMeta()` method to `IProfileRepository` interface
4. Implemented `UpdateProfileMeta()` in `ProfileRepository.cs` to call the GDScript `update_profile_meta()`
5. Updated `SummonerSelectionService.SetActiveSummoner()` to use `UpdateProfileMeta()` instead of modifying a snapshot

**Related Files:**
- `scripts/csharp/Infrastructure/Persistence/DtoConverters.cs` - Added FromMetaDict, updated FromProfileDict
- `scripts/csharp/Infrastructure/Persistence/IProfileRepository.cs` - Added UpdateProfileMeta
- `scripts/csharp/Infrastructure/Persistence/ProfileRepository.cs` - Implemented UpdateProfileMeta
- `scripts/csharp/Meta/Services/Summoner/SummonerSelectionService.cs` - Fixed SetActiveSummoner to use UpdateProfileMeta

---

### Mana Bolt Bounces on Ground Impact
**Resolved:** 2026-01-26
**Component:** Projectiles / Spells

**Description:**
Homing arc projectiles (like Mana Bolt) would bounce repeatedly when approaching ground level instead of smoothly arcing to their target.

**Root Cause:**
The homing arc calculation in `MoveHoming()` used 3D distance to calculate progress, which created a Y feedback loop. When Y was low (near ground), the 3D traveled distance was smaller than expected, resulting in a lower progress value. The arc formula then calculated a HIGHER Y value, causing the projectile to "bounce" back up.

**Solution Implemented:**
Refactored to a path-based movement architecture using the Strategy pattern. Instead of calculating arc height dynamically with direction vectors, projectiles now follow parameterized Bézier curves (IProjectilePath interface) from start to end. This eliminates the Y feedback loop entirely since progress is time/distance-based rather than position-based.

New path classes:
- `StraightPath` - Linear interpolation for straight projectiles
- `ArcPath` - Quadratic Bézier curve for arc/homing projectiles
- `BallisticPath` - Pre-computed parabolic trajectory for ballistic projectiles

**Related Files:**
- `scripts/csharp/Projectiles/Projectile3D.cs` - Refactored to use IProjectilePath
- `scripts/csharp/Projectiles/Paths/IProjectilePath.cs` - Strategy interface
- `scripts/csharp/Projectiles/Paths/StraightPath.cs` - Linear path implementation
- `scripts/csharp/Projectiles/Paths/ArcPath.cs` - Bézier arc implementation
- `scripts/csharp/Projectiles/Paths/BallisticPath.cs` - Parabolic path implementation
- `tests/csharp/Projectiles/Paths/PathTests.cs` - Unit tests for path classes
- `docs/technical/runtime/projectile-system.md` - Updated documentation

---

### CampaignService Unit Tests Fail Due to C# Architecture Mismatch
**Resolved:** 2026-01-23
**Component:** Test Infrastructure

**Description:**
28 tests in `test_campaign_service.gd` failed because the CampaignService was migrated to a hybrid GDScript/C# architecture. Tests created standalone instances with `CampaignServiceScript.new()`, but `_cs_service` (CampaignServiceCS autoload) was null.

**Root Cause:**
1. Test creates instance not added to scene tree
2. `_ready()` calls `get_node_or_null("/root/CampaignServiceCS")` which returns null
3. `init_for_testing()` didn't inject a mock C# service
4. All methods checked `if _cs_service == null: return []`

**Solution Implemented:**
Created `MockCampaignServiceCS` GDScript mock and updated `init_for_testing()` to accept it:
- Created `tests/mocks/mock_campaign_service_cs.gd` - Full mock with signals, methods, and call tracking
- Updated `campaign_service.gd` `init_for_testing()` to accept `cs_service_mock: Node` parameter
- Mock properly handles campaign loading, battle completion, pending rewards, and progress tracking

**Related Files:**
- `tests/mocks/mock_campaign_service_cs.gd` (created)
- `scripts/services/campaign_service.gd` (modified)
- `tests/unit/test_campaign_service.gd` (modified)

---

### ShopService Ownership Tests Fail - Offerings Not Returned
**Resolved:** 2026-01-23
**Component:** Test Infrastructure

**Description:**
Bug report claimed 3 tests failed and 7 were skipped in `test_shop_ownership.gd`. Investigation revealed the tests were actually passing (11/11).

**Root Cause:**
The bug report was outdated. Tests were passing when verified on 2026-01-23.

**Solution:**
No code changes needed - removed outdated bug report.

---

### HP Bar Lifecycle Integration Tests Fail in CLI Mode
**Resolved:** 2026-01-23
**Component:** Test Infrastructure

**Description:**
7 HP bar lifecycle integration tests in `test_hp_bar_lifecycle.gd` failed when running with Godot .NET (Mono) via CLI.

**Issues Found:**
1. Tests used `get_tree().current_scene.add_child(unit)` which fails in CLI mode (`current_scene` is null)
2. Tests called `HPBarService.create_bar_for_unit(unit)` without the required `settings` parameter
3. C# nullable default parameters (`Dictionary? settings = null`) aren't exposed to GDScript - GDScript sees them as required

**Root Cause:**
GDScript/C# interop limitation: C# methods with nullable default parameters (e.g., `= null`) don't have their default values exposed to GDScript's method binding. The `default_args` array is empty, making the parameter appear required.

**Solution Implemented:**
1. Changed `get_tree().current_scene.add_child(unit)` to `add_child(unit)` (adds to test node)
2. Used explicit node reference: `_hp_service = get_node_or_null("/root/HPBarService")` instead of autoload global
3. Passed `null` explicitly: `_hp_service.create_bar_for_unit(unit, null)`
4. Fixed incorrect assertion in `test_hp_bar_removed_on_unit_queue_free` (expected `initial_pooled`, not `initial_pooled + 1`)

**Related Files:**
- `tests/integration/test_hp_bar_lifecycle.gd` (modified)

---

### Fire Titans Cannot Attack Each Other
**Resolved:** 2026-01-15
**Component:** Combat / Unit Configuration

**Description:**
Fire Titans were unable to attack each other in combat. The issue was that their attack range did not extend outside their collision bodies, so when two Fire Titans stood next to each other, neither could reach the other.

**Root Cause:**
Attack range is measured from unit center, but large units like Fire Titan have large separation radii. If `AttackRange <= SeparationRadius * 2`, the unit cannot reach outside its own body to hit adjacent units.

**Solution Implemented:**
Increased attack ranges for all melee units to account for body sizes:
- Fire Titan: 3.0 → 5.0
- Fire Ant: 1.8 → 3.0
- Fire Elemental: 2.0 → 3.0
- Earth Sprite: 2.0 → 3.0
- Rock: 2.0 → 3.0

Updated both scene files (.tscn) and CardCatalog.cs (which overrides scene values when cards are played).

Rule: `AttackRange > SeparationRadius + TargetBodySize`

**Related Files:**
- `scenes/battle/units/fire_titan_3d.tscn` - AttackRange and SeparationRadius values
- `scenes/battle/units/fire_ant_3d.tscn` - AttackRange values
- `scenes/battle/units/fire_elemental_3d.tscn` - AttackRange values
- `scenes/battle/units/earth_sprite_3d.tscn` - AttackRange values
- `scenes/battle/units/rock_3d.tscn` - AttackRange values
- `scripts/csharp/Cards/CardCatalog.cs` - AttackRange values for all card definitions
- `scripts/csharp/Units/MeleeUnit3D.cs` - Attack range check logic

---

### Summoner Combat Interactions Broken
**Resolved:** 2026-01-14
**Component:** Combat / Summoner / Projectiles

**Description:**
Multiple issues with how the summoner interacts with combat systems:
1. Projectiles cannot hit summoner
2. Units cannot hit summoner
3. Summoner blocks friendly projectiles

**Root Cause:**
The summoner was missing a HurtboxComponent. Units create a HurtboxComponent in `_Ready()` via `SetupHurtbox()` that places a collision shape on Layer 5 (hurtbox layer), allowing hitboxes (melee attacks) and projectiles (on Layer 6) to detect hits. The summoner had none of this - only a StaticBody3D for physical collision which is on the wrong layer for combat detection.

**Solution Implemented:**
Added `_setup_hurtbox()` method to `summoner.gd` that:
1. Loads and instantiates the C# HurtboxComponent
2. Configures it with team, `HurtboxCategory.Summoner`, and appropriate size (radius 1.0, height 4.0)
3. Called from `_ready()` after HP bar setup

The existing `take_damage()` method and damage system were already working - the issue was purely collision detection.

**Related Files:**
- `scripts/core/summoner.gd` - Added `_setup_hurtbox()` method and hurtbox configuration constants
- `scripts/csharp/Battle/Simulation/Combat/Hitbox/HurtboxComponent.cs` - Existing component, no changes needed
- `scripts/csharp/Battle/Simulation/Combat/DamageSystem.cs` - Already supported GDScript `take_damage()` method

---

### Units Can Move/Fly Out of Bounds
**Resolved:** 2026-01-13
**Component:** Unit Movement / Boundaries

**Description:**
Units could move or fly outside the battlefield boundaries. There was no boundary enforcement for unit movement.

**Solution Implemented:**
Added unified boundary enforcement system:
1. Created `BattlefieldBounds.cs` - C# boundary constants and utilities (X: -50 to +50, Z: -40 to +40)
2. Added `EnforceBattlefieldBounds()` in `Unit3D.ApplyMovementResult()` - clamps position after all physics
3. Added boundary clamping in `UnitSteering.CorrectOverlaps()` - prevents pushing units out of bounds

**Related Files:**
- `scripts/csharp/Infrastructure/Constants/BattlefieldBounds.cs` - New boundary constants and utilities
- `scripts/csharp/Units/Unit3D.cs` - Added EnforceBattlefieldBounds() call
- `scripts/csharp/Movement/UnitSteering.cs` - Added boundary clamping in push logic

---

### Small Units Can Push Large Units Off Screen
**Resolved:** 2026-01-13
**Component:** Unit Movement / Collision

**Description:**
Spawning many small units (Ants) around a large unit (Fire Titan) caused the large unit to be pushed off screen. The pushed unit then got stuck perpetually trying to move back into attack range.

**Solution Implemented:**
Added mass-based push resistance to `UnitSteering.CorrectOverlaps()`:
1. Mass derived from CollisionRadius^3 (2x radius = 8x mass)
2. Push ratio calculated as `otherMass / totalMass` - lighter units pushed more
3. Example: Fire Titan (r=1.5, mass=3.375) vs Fire Ant (r=0.3, mass=0.027) = 125:1 ratio
4. Combined with boundary enforcement to prevent any unit from leaving battlefield

**Related Files:**
- `scripts/csharp/Movement/UnitSteering.cs` - Mass-based push resistance + boundary clamping
- `scripts/csharp/Infrastructure/Constants/BattlefieldBounds.cs` - Boundary utilities

---

### Unit Spawn Boundary Can Be Bypassed When Blocked
**Resolved:** 2026-01-13
**Component:** Unit Spawning / Boundaries

**Description:**
When spawning a unit on your half of the battlefield, if there were already units blocking the intended spawn location, the system found the "closest available point." However, this closest point could end up past the player's half boundary (on the enemy's side), effectively bypassing the spawn restriction.

**Solution Implemented:**
Added team boundary enforcement to `SpawnPositionCalculator`:
1. Added `team` parameter to `CalculateFormationPositions()`, `FindSafeSpawnPosition()`, and `IsSpawnPositionSafe()`
2. `IsSpawnPositionSafe()` now checks team spawn boundary (player: X <= 0, enemy: X > 0) and battlefield bounds
3. Fallback now clamps to team's valid zone instead of returning invalid position
4. Updated `CardFactory.get_safe_spawn_positions()` and `execute_summon()` to pass team

**Related Files:**
- `scripts/csharp/Summons/SpawnPositionCalculator.cs` - Added team boundary enforcement
- `scripts/csharp/Cards/CardFactory.cs` - Updated to pass team parameter
- `scripts/csharp/Meta/Services/Interfaces/ICardFactory.cs` - Updated interface
- `scripts/csharp/Infrastructure/Constants/BattlefieldBounds.cs` - Team spawn validation utilities

---

### Unit Spawns at Cursor Position Instead of Preview Position
**Resolved:** 2026-01-06
**Component:** Spawn System / Card Playing

**Description:**
When spawning a unit in an occupied location, the spawn preview correctly snapped to the nearest available position. However, the actual unit spawned at the original cursor position instead of the preview position, causing existing units to be displaced.

**Root Cause:**
DRY violation - safe spawn position calculation had two separate implementations:
1. `BattlefieldConstants.find_safe_spawn_position()` (GDScript) - used by preview
2. `CardFactory.FindSafeSpawnPosition()` (C#) - used by actual spawn

Additionally, preview calculated all positions at once, but actual spawn calculated sequentially (each spawned unit affected the next position).

**Solution Implemented:**
- Added `CardFactory.get_safe_spawn_positions()` as single source of truth
- Updated `BattlefieldDropZone` to call C# method for preview
- Updated `execute_summon()` to pre-calculate all positions before spawning
- Deleted `BattlefieldConstants.find_safe_spawn_position()` (GDScript duplicate)

**Related Files:**
- `scripts/csharp/Cards/CardFactory.cs` - Added get_safe_spawn_positions(), refactored execute_summon()
- `scripts/battle/ui/battlefield_drop_zone.gd` - Now calls C# method
- `scripts/battle/battlefield/battlefield_constants.gd` - Removed duplicate functions

---

### Spawn Preview and Actual Spawning Use Separate Formation Systems
**Resolved:** 2026-01-06
**Component:** Architecture / Formation System

**Description:**
Formation logic was duplicated across multiple files (Card.gd, CardFactory.cs, FormationHelper.cs). Adding a new formation type required updating 4+ separate implementations.

**Solution Implemented:**
- CardFactory.get_formation_offset() is now the single source of truth
- Card.gd now delegates to CardFactory instead of having duplicate methods
- Deleted FormationHelper.cs (redundant)
- SpawnPreview.cs uses simple inline default for initial positioning

**Architecture Document:** See `docs/archive/transformation-roadmap.md` for full details (archived — superseded by layered architecture migration).

---

### Fire Swarm Units Get Stuck on Spawn
**Resolved:** 2026-01-04
**Component:** Spawning / SpatialGrid / Multi-Unit Spawn

**Description:**
When playing the Fire Swarm card (spawns 12 fire elementals), units would get stuck and not behave correctly after the spawn reveal animation completed.

**Root Cause:**
Two related issues in the multi-unit spawn flow:

1. **SpatialGrid stale cell data during spawn reveal:**
   - Units register with SpatialGrid at (0,0,0) during `_Ready()` before position is set
   - After position is set, SpatialGrid cell is not updated
   - During spawn reveal (2.5s), units are inactive so `_PhysicsProcess` returns early
   - `UpdateSpatialGridPosition()` never runs until unit activates
   - First frame after activation uses stale cell data for steering/targeting

2. **Safe spawn position checking against self:**
   - Each newly spawned unit joins UNITS group at (0,0,0) before position is set
   - `is_spawn_position_safe()` checks ALL units including the one being spawned
   - The unit could be checking against itself at the wrong position

**Solution Implemented:**
1. In `scripts/cards/card.gd`: Call `SpatialGrid.update_unit_position(unit)` immediately after setting `unit.global_position`
2. In `scripts/battle/battlefield/battlefield_constants.gd`: Added `exclude_unit` parameter to `find_safe_spawn_position()` and `is_spawn_position_safe()` to skip the unit being spawned

**Related Files:**
- `scripts/cards/card.gd:293-297` - SpatialGrid update after position set
- `scripts/battle/battlefield/battlefield_constants.gd:63,87` - exclude_unit parameter

---

### Aggro Manipulation Exploit - Units Can Be Permanently Occupied
**Resolved:** 2026-01-03
**Component:** AI / Combat / Targeting

**Description:**
Players could permanently keep enemy units occupied by spawning new units, as all enemies would immediately switch aggro to the newly spawned unit.

**Solution Implemented:**
Multi-layered defensive system in `scripts/csharp/Units/Unit3D.cs`:

1. **Target Lock Mechanism** (Line 38): `TargetLockDuration = 0.5f`
   - Units lock onto their current target for 0.5 seconds
   - Cannot switch targets during this window even if new units spawn

2. **Health-Weighted Scoring** (`scripts/csharp/Targeting/HealthScorer.cs`):
   - Weight = 10.0 (high priority)
   - Prioritizes damaged targets over fresh spawns
   - Cheap fodder units score lower than engaged targets

3. **UpdateTargeting Logic** (Lines 728-752):
   - Respects target lock timer before re-evaluating
   - Only switches when current target is invalid or lock expires

**Related Files:**
- `scripts/csharp/Units/Unit3D.cs` - Target lock implementation
- `scripts/csharp/Targeting/HealthScorer.cs` - Health-weighted scoring
- `scripts/csharp/Targeting/DistanceScorer.cs` - Distance scoring (weight 1.0)

---

### Large Units Render In Front of Smaller Units Despite Z-Position
**Resolved:** 2026-01-03
**Component:** Rendering / Sprite3D / Depth Sorting

**Description:**
Large units (e.g., Fire Titan with 800px ViewportPadding) rendered in front of smaller units even when positioned behind them on the Z-axis.

**Root Cause:**
`SetupSpriteAlignment()` in `SkeletalVisualComponent` positioned the Sprite3D assuming feet were at the viewport bottom. However, `ViewportPadding` creates empty space below the feet. This caused the Sprite3D origin (used for depth sorting) to be below the visual feet position, making large units sort as "in front" of where they appeared.

**Solution Implemented:**
Added `FeetOffsetPixels` property and updated `SetupSpriteAlignment()` to account for viewport padding:

```csharp
// Calculate feet offset from viewport bottom
float feetOffsetPx = FeetOffsetPixels >= 0 ? FeetOffsetPixels : ViewportPadding;
float feetOffsetWorld = feetOffsetPx * ScaleFactor.Y * _sprite3D.PixelSize;

// Position Sprite3D so feet (not viewport bottom) are at Y=0
pos.Y = (worldHeight / 2.0f) - feetOffsetWorld;
```

**Related Files:**
- `scripts/csharp/Battle/View/Visual/SkeletalVisualComponent.cs` - Added FeetOffsetPixels, fixed SetupSpriteAlignment()

---

### Hand UI Area Blocks Unit Spawning
**Resolved:** 2025-12-17
**Component:** UI / Battlefield Drop Zone

**Description:**
The card hand UI area at the bottom of the screen prevented unit spawning in that region of the battlefield. Attempting to drag and drop a summon card to spawn a unit where the hand UI was rendered failed because the hand UI intercepted the drop.

**Solution Implemented:**
Hide the entire hand UI when dragging a card. This ensures the battlefield drop zone receives all drop events during card drag operations. The hand reappears when the drag ends (drop or cancel).

**Related Files:**
- `scripts/ui/hand_ui.gd:261` - Hide hand on drag start
- `scripts/ui/hand_ui.gd:274-276` - Show hand on drag end via NOTIFICATION_DRAG_END

---

### Summoner Stats Not Cached in Campaign Mode
**Resolved:** 2025-12-17
**Component:** DamageSystem / Summoner / SummonerCatalog

**Description:**
Warning appeared during battles: "DamageSystem: No summoner stats cached in campaign mode - trait bonuses not applied"

**Root Cause:**
Three issues:
1. **String/StringName type mismatch**: `SummonerCatalog._catalog` uses `StringName` keys, but `get_summoner_config()` was receiving `String` parameters. GDScript 4 treats these as different types.
2. **Summoner loading coupled to deck loading**: `DeckLoader.load_player_deck()` required a deck to exist to get the summoner_id, but summoners exist independently in the profile.
3. When no decks existed (common in dev/test scenarios), summoner instance loading was skipped entirely.

**Solution Implemented:**
1. Fixed `SummonerCatalog.get_summoner_config()` to convert String to StringName before lookup
2. **Decoupled summoner loading from deck loading**: New `_load_summoner_from_profile()` function loads summoner directly via `SummonerSelection.get_active_summoner_id()` and `ProfileRepo.get_summoner_instance()`, independent of deck data
3. Summoner bonuses now applied even when using `dev_player_deck` or when no decks exist

**Related Files:**
- scripts/core/summoner.gd - New `_load_summoner_from_profile()` function
- scripts/infrastructure/data/summoner_catalog.gd - String to StringName conversion in lookup methods
- scripts/core/deck_loader.gd - Removed bandaid fallback, now focuses only on card loading

---

### Orphaned Nodes from Autoload Object Pools During Unit Tests
**Resolved:** 2025-11-28
**Component:** Unit Testing / Object Pools

**Description:**
GUT reported ~155 orphaned nodes during test runs from autoload object pools (VFXManager, HPBarManager, ProjectileManager).

**Root Cause:**
Autoload managers pre-instantiated object pools at startup. These pooled objects were stored in arrays *outside* the scene tree, making them "orphans" by Godot's definition. GUT detects orphans using `Node.get_orphan_node_ids()` which finds any node not in the scene tree.

**Solution Implemented:**
Keep pooled objects IN the scene tree by adding them to a dedicated pool container node:

1. **Added `pool_container: Node3D`** to each manager - a hidden child node that holds pooled objects
2. **On pool creation:** Add instances to `pool_container` instead of just storing in arrays
3. **On retrieval:** Remove from `pool_container` before adding to active container
4. **On return:** Add back to `pool_container` after removing from active container

This ensures pooled objects are always in the scene tree (either in `pool_container` or `active_container`), eliminating orphan warnings. The scene tree also automatically handles cleanup when the autoload exits.

**Related Files:**
- `scripts/battle/vfx/vfx_manager.gd` - Added pool_container, updated _init_pools, _get_from_pool, _on_effect_finished
- `scripts/ui/hp_bar_manager.gd` - Added pool_container, updated _init_pool, create_bar_for_unit, _return_to_pool
- `scripts/projectiles/projectile_manager.gd` - Added pool_container, updated _create_pool_for, _return_to_pool

---

### Exiting Battle Mid-Fight Incorrectly Completes Event
**Resolved:** 2025-11-28
**Component:** Campaign / Battle System

**Description:**
When a player exits a battle in the middle of it (via pause menu), the event/battle could be incorrectly marked as completed, breaking campaign progression.

**Root Cause:**
The battle system lacked explicit state tracking for the battle lifecycle. When quitting mid-battle:
1. `current_battle` in profile persisted (never cleared on exit)
2. No distinction between quit/loss/victory/crash states
3. Stale state could cause confusion on subsequent battle attempts

**Solution Implemented:**
Added explicit battle state machine to BattleContext:

1. **BattleState enum** - Tracks lifecycle: `NONE → CONFIGURED → IN_PROGRESS → VICTORY/DEFEAT/ABANDONED`
2. **`abandon_battle()`** - Called on pause menu quit, clears `current_battle` from profile and pending rewards
3. **`origin_scene`** tracking - Returns player to correct scene (campaign map, game mode menu, etc.)
4. **State transitions** - GameController3D calls `start_battle()`, `end_battle_victory()`, `end_battle_defeat()`
5. **RewardScreen guard** - Validates `BattleState.VICTORY` before showing rewards, redirects otherwise

**Related Files:**
- `scripts/application/battle_context.gd` - BattleState enum, abandon_battle(), origin_scene tracking
- `scripts/ui/pause_menu.gd` - Calls abandon_battle() on quit
- `scripts/core/game_controller_3d.gd` - Sets battle states on start/end
- `scripts/ui/reward_screen.gd` - State validation guard

---

### Slimes Getting Stuck Between Life and Death
**Resolved:** 2025-11-26
**Component:** Units / Combat

**Description:**
Slime units sometimes got stuck in a state between being alive and dead. They would stop functioning properly but not fully die or despawn.

**Root Cause:**
Race condition in `unit_3d.gd:_die()`. Multiple damage events in the same frame could call `_die()` multiple times before `is_alive` was set to false. Additionally, using `await` for death animation could fail silently if the scene tree changed.

**Solution Implemented:**
1. Added `is_dying` guard flag to prevent multiple `_die()` calls
2. Changed from `await get_tree().create_timer()` to a `Tween` for more reliable cleanup
3. Updated `take_damage()` to check `is_dying` flag
4. Updated `_is_valid_target()` to exclude dying units
5. Updated `_acquire_target()` to skip dying units

**Related Files:**
- `scripts/units/unit_3d.gd:97` - Added `is_dying` flag
- `scripts/units/unit_3d.gd:957-974` - Improved `_die()` function
- `scripts/units/unit_3d.gd:935-937` - Updated `take_damage()`
- `scripts/units/unit_3d.gd:525-533` - Updated `_is_valid_target()`
- `scripts/units/unit_3d.gd:582-586` - Updated target acquisition

---

### Mana Bar Uses Hardcoded Values Instead of Hero System
**Resolved:** 2025-11-26
**Component:** UI / Mana System

**Description:**
The mana bar had hardcoded values and MANA_MAX was a constant in Summoner3D instead of using HeroInstance stats.

**Root Cause:**
`summoner_3d.gd` defined `const MANA_MAX: float = 10.0` instead of a variable that could be set from HeroInstance. The `_apply_hero_bonuses()` function had a TODO to apply max_mana but it was never implemented.

**Solution Implemented:**
1. Changed `const MANA_MAX` to `var max_mana` in summoner_3d.gd
2. Updated `_apply_hero_bonuses()` to set max_mana from HeroInstance stats
3. Updated all references from `MANA_MAX` to `max_mana`
4. Updated `mana_bar.gd:update_mana()` to update `progress_bar.max_value` when maximum changes
5. Added `DEFAULT_MAX_MANA` constant to mana_bar.gd for clarity

**Related Files:**
- `scripts/core/summoner_3d.gd:28` - Changed const to var
- `scripts/core/summoner_3d.gd:365-388` - Updated `_apply_hero_bonuses()`
- `scripts/ui/mana_bar.gd:29-30` - Added DEFAULT_MAX_MANA constant
- `scripts/ui/mana_bar.gd:123-127` - Update max_value in update_mana()

---

### Projectile Pooling Race Condition with Deferred Removal
**Resolved:** 2025-11-26
**Component:** Projectiles / Pooling System

**Description:**
Projectiles spawned rapidly in succession would cause errors: "Parent node is busy setting up children, cannot add child". This happened because pooled projectiles were being reused before their deferred removal from the scene tree had completed.

**Root Cause:**
In `ProjectileManager._return_to_pool()`, projectiles were removed from the scene tree using `remove_child.call_deferred()` to avoid physics callback issues. However, the projectile was immediately returned to the pool and could be grabbed by `_get_from_pool()` before the deferred removal completed.

**Solution Implemented:**
Added synchronous parent check in `_get_from_pool()`:
```gdscript
# Ensure projectile is removed from any parent (handles deferred removal race condition)
if pooled_projectile.get_parent():
    pooled_projectile.get_parent().remove_child(pooled_projectile)
```

This ensures that if a projectile is retrieved from the pool while still technically parented (due to pending deferred removal), it gets synchronously unparented before being added to the new container.

**Related Files:**
- `scripts/projectiles/projectile_manager.gd:153-156` - Added parent check in `_get_from_pool()`

---

### Mission Rewards Auto-Accepted Without Player Choice
**Resolved:** 2025-11-25
**Component:** Campaign / Rewards

**Description:**
If a mission finished and the player didn't explicitly accept rewards (e.g., closed the game or crashed), the rewards could be auto-accepted or lost. This was problematic for reward screens requiring player choice.

**Root Cause:**
The RewardScreen called `complete_battle()` immediately when loading, BEFORE the player had a chance to make a choice for "choice" type rewards. If the game exited before the player clicked Continue, the battle was marked complete but no reward was granted.

**Solution Implemented:**
Added pending reward state tracking:
1. Added `pending_reward` field to profile campaign_progress schema
2. Added CampaignService methods: `set_pending_reward()`, `get_pending_reward()`, `update_pending_choice()`, `clear_pending_reward()`, `claim_pending_reward()`
3. RewardScreen now:
   - Sets pending reward on first load (doesn't complete battle yet)
   - Checks for pending reward on load (resumes if found)
   - Only grants reward AND completes battle when Continue is pressed
   - Saves choice to pending state immediately when player picks (for choice rewards)

**Related Files:**
- `scripts/infrastructure/data/json_profile_repository.gd` - Added `pending_reward` to schema
- `scripts/services/campaign_service.gd` - Added pending reward management methods
- `scripts/ui/reward_screen.gd` - Complete rewrite of reward flow
- `localization/data/en.json` - Added ui.reward localization keys

---

### Cards Cannot Be Played in Campaign Battles
**Resolved:** 2025-11-25
**Component:** Cards / Battle System

**Description:**
Cards could not be played during campaign battles - dragging cards to the battlefield did nothing.

**Root Cause:**
`BattlefieldDropZone._can_drop_data()` was checking `summoner.get("is_alive")`, but a previous refactor renamed this property to `is_enabled` in Summoner3D. Since the property didn't exist, `get()` returned null, which defaulted to `false`, blocking all drops.

**Solution Implemented:**
Changed `is_alive` to `is_enabled` in `battlefield_drop_zone.gd:116-118`.

**Related Files:**
- `scripts/ui/battlefield_drop_zone.gd`

---

### Charge Spell Causes Units to Bounce When Targeting Above Base
**Resolved:** 2025-11-25
**Component:** Spells / Unit Movement

**Description:**
When using the Charge spell with a target location visually "above" the enemy base, units bounced back and forth instead of attacking. The issue was that `find_nearest_enemy()` found the EnemySummoner (at Z=0) instead of EnemyBase (at Z=-7.5), and Summoner had no collision shape for unit spreading.

**Root Cause:**
Summoner3D was in the "bases" group, making it a valid attack target. This was legacy code from when the Summoner was intended to be attackable, but in the actual game design only the Nexus (Base3D) should be attackable.

**Solution Implemented:**
1. Removed `add_to_group("bases")` from Summoner3D - summoners are no longer found as attack targets
2. Removed vestigial HP/death code from Summoner3D (max_hp, current_hp, take_damage, _die, summoner_died signal)
3. Removed `_on_summoner_died` handler from GameController3D
4. Documented intended architecture in `docs/design/hero-and-nexus.md`

**Related Files:**
- `scripts/core/summoner_3d.gd` - Removed HP/death code and bases group membership
- `scripts/core/game_controller_3d.gd` - Removed summoner_died signal handling
- `docs/design/hero-and-nexus.md` - New architecture documentation

---

### AI Scoring Magic Numbers Extracted to Constants
**Resolved:** 2025-11-25
**Component:** AI System

**Description:**
The HeuristicAI class used many hardcoded magic numbers for card scoring and decision-making thresholds, making AI tuning difficult.

**Solution Implemented:**
Extracted ~41 magic numbers to named class-level constants organized by category:
- Card scoring (SCORE_BASE_SUMMON, SCORE_MANA_EFFICIENCY_BASE, etc.)
- Enemy count thresholds (ENEMY_COUNT_THRESHOLD_LOSING_BADLY, etc.)
- Personality bonuses (PERSONALITY_AGGRESSIVE_SUMMON_BONUS, etc.)
- Battlefield state thresholds (STATE_LOSING_BADLY_THRESHOLD, etc.)
- Difficulty/randomness (DIFFICULTY_RANDOMNESS_MULTIPLIER, etc.)
- Play timing multipliers (TIMING_LOSING_BADLY_MULTIPLIER, etc.)
- Spawn zones (SPAWN_ENEMY_DEFENSIVE_MIN, SPAWN_PLAYER_NEUTRAL_MAX, etc.)

**Related Files:**
- `scripts/ai/heuristic_ai.gd` - All constants added at top of file

---

### WAL Uses Inconsistent Key Names
**Resolved:** 2025-11-25
**Component:** Database / ProfileRepository

**Description:**
The Write-Ahead Log used inconsistent key formats - some entries used "action"/"params" while others used "op".

**Solution Implemented:**
Standardized all WAL entries to use `{"action": "...", "params": {...}}` format:
- `unlock_hero` - changed from `"op"` to `"action"/"params"`
- `set_starting_hero` - changed from `"op"` to `"action"/"params"`

**Related Files:**
- `scripts/infrastructure/data/json_profile_repository.gd:246, 269-272`

---

### UUID Generation Weak Entropy
**Resolved:** 2025-11-25
**Component:** Database / ProfileRepository

**Description:**
The `_generate_uuid()` function used weak entropy sources (only ticks_msec and single randi) that could cause collisions.

**Solution Implemented:**
Added more entropy sources:
- `Time.get_unix_time_from_system()` - absolute timestamp
- `Time.get_ticks_usec()` - microsecond precision
- Two `randi()` calls instead of one
- Format: `"%x-%x-%x-%x"` with 4 components

**Related Files:**
- `scripts/infrastructure/data/json_profile_repository.gd:1013-1019`

---

### Backup Rotation Happens After Write Success
**Resolved:** 2025-11-25
**Component:** Database / ProfileRepository

**Description:**
Backup files were rotated after the main write succeeded, meaning a crash between write and rotation could lose a backup generation.

**Solution Implemented:**
Reordered operations: rotate backups BEFORE writing new data. This ensures old data is preserved in backup chain before being overwritten.

**Related Files:**
- `scripts/infrastructure/data/json_profile_repository.gd:833-842`

---

## 2025-11 Fixes

### VFX Pooling System Resource Isolation
**Resolved:** 2025-11-24
**Component:** VFX / Pooling System

**Description:**
The VFX pooling system didn't properly isolate shared resources (meshes, materials) between pooled instances. Modifying properties like mesh.size or material colors affected all instances using that resource, causing bugs when VFX objects were reused.

**Solution Implemented:**
Added resource isolation helpers to `VFXInstance` base class:
- `isolate_mesh_resources(mesh_instance, isolate_mesh, isolate_materials)` - Makes a MeshInstance3D's resources unique
- `isolate_all_mesh_resources()` - Convenience method for all descendant meshes (recursive)
- Documentation in class header explaining safe patterns for pooled VFX
- Updated `fireball_spell_vfx.gd` to use the new helper

Safe patterns documented:
1. Use node transforms (scale, modulate) instead of resource properties
2. Call `isolate_mesh_resources()` in `_ready()` for nodes you'll modify
3. Create resources dynamically in code (they're unique per-instance)

**Related Files:**
- `scripts/battle/vfx/vfx_instance.gd` - Added isolation helpers and documentation
- `scripts/battle/vfx/fireball_spell_vfx.gd` - Uses new helper method

---

### Projectile Cleanup Not Working Properly
**Resolved:** 2025-11-24
**Component:** Projectiles / Memory Management

**Description:**
Projectiles were not being cleaned up properly after impact or expiration, causing memory leaks and orphaned nodes in the scene tree.

**Solution Implemented:**
Fixed projectile lifecycle management in ProjectileManager to ensure proper cleanup on hit/miss/expire. Projectiles are now correctly returned to pool or freed.

**Related Files:**
- `scripts/projectiles/projectile_manager.gd` - Pool management fixes
- `scripts/projectiles/projectile_3d.gd` - Lifecycle logic fixes

**PR:** #65

---

### Projectile Targeting on Moving Units
**Resolved:** 2025-11-24
**Component:** Combat / Projectiles

**Description:**
Projectiles did not properly track or predict the position of moving units, causing misses or incorrect targeting.

**Solution Implemented:**
Added target position prediction - projectiles now calculate where the target will be upon landing based on current velocity, rather than aiming at current position. This allows arc projectiles to lead moving targets.

**Related Files:**
- `scripts/projectiles/projectile_3d.gd` - Target position prediction logic

---

### Ranged Units Perpetually Miss Targets at Melee Range
**Resolved:** 2025-11-24
**Component:** Combat / Ranged Attacks

**Description:**
When a melee unit (e.g., slime) gets directly on top of a ranged unit (e.g., archer), the archer perpetually misses even though the target is stationary and extremely close.

**Root Cause:**
Arc projectiles had a fixed `arc_height` (1.5 units) regardless of distance. At close range, arrows would arc UP and OVER the target, never passing through their hitbox.

**Solution Implemented:**
Scale arc height proportionally to distance in `_move_arc()`:
- `arc_scale = clamp(distance / 5.0, 0.0, 1.0)`
- At 5+ units: full 1.5 unit arc
- At 2.5 units: 0.75 unit arc
- At 1 unit: 0.3 unit arc (essentially flat)
- Added `max(distance, 0.1)` guard against division by near-zero

**Related Files:**
- `scripts/projectiles/projectile_3d.gd:141-170` - Arc movement with scaled height

---

### Battles Not Working on First Play with Dialogue
**Resolved:** 2025-01-24
**Component:** Battle System / Dialogue / Event Sequencer

**Description:**
Battles are not functioning properly the first time they are played when dialogue or event sequences are involved. Dialogue doesn't show and enemies don't spawn on first load.

**Root Cause:**
Race condition between DialogueManager (autoload) and DialogueBox (scene node):
1. HeroSelection scene's DialogueBox calls `DialogueManager.notify_ui_connected()` setting `_is_system_ready = true`
2. When battle scene loads, DialogueManager is autoload so `_is_system_ready` stays true
3. But the OLD DialogueBox from HeroSelection is gone, NEW DialogueBox hasn't connected yet
4. EventSequencer checks `is_system_ready()`, sees true, starts dialogue immediately
5. DialogueBox misses the dialogue_started/dialogue_line_displayed signals

**Solution Implemented:**
Reset `_is_system_ready = false` in `DialogueManager.reset()` so each new scene's DialogueBox must reconnect. This ensures EventSequencer properly waits for the new DialogueBox to be ready.

**Related Files:**
- `scripts/application/dialogue_manager.gd:305` - Added `_is_system_ready = false` in reset()
- `scripts/battle/battle_dialogue_controller.gd` - Calls EventSequencer.play_sequence()
- `scripts/application/event_sequencer.gd:196-207` - Checks is_system_ready() before dialogue

---

### Charge Spell Not Attacking - Only Moving to Destination
**Resolved:** 2025-11-24
**Component:** Spells / Charge Ability

**Description:**
The Charge spell (granted in first card selection tutorial) is not working correctly. Units only move to the designated spot but do not attack the nearest enemy upon arrival. Additionally, debug logs incorrectly reference "rally" instead of "charge".

**Root Cause:**
The Charge spell used `RedirectManager.TARGET_SEARCH_RADIUS` (10.0 units) to search for enemies near the charge destination. If no enemy was within 10 units of where the player dragged the arrow, no target was found and the spell did nothing.

**Solution Implemented:**
- Changed Charge spell to use a large search radius (999.0 units) to find the nearest enemy on the entire battlefield
- This differs from regular redirect (which intentionally uses a small radius for local control)
- Also added `original_redirect_point` storage for fallback targeting when the primary target dies

**Related Files:**
- `scripts/cards/card.gd:403-429` - Fixed `_apply_charge_command()` search radius

**Notes:**
- The "rally_destination" variable name in SpellTargetingManager is reused for all command spells (Rally, Guard, Charge) - this is a naming quirk but doesn't affect functionality

---

### Battle Marked Complete When Starting Event Sequence
**Resolved:** 2025-11-24
**Component:** Campaign / Battle System

**Description:**
When a battle with an event sequence is started (like charge_tutorial), the campaign system could potentially mark the battle as completed prematurely if signal connections weren't properly cleaned up.

**Investigation Results:**
- Battle completion is ONLY triggered in `reward_screen.gd` when player wins
- EventScreen was missing `_exit_tree` cleanup for EventSequencer.sequence_finished connection
- If player navigated away mid-event, stale signal connection could persist
- However, signal cleanup in Godot when node is freed should prevent this

**Solution Implemented:**
- Added `_exit_tree()` cleanup to EventScreen to explicitly disconnect from EventSequencer.sequence_finished
- ShopScreen already had proper cleanup in place
- This prevents any potential signal leak when navigating away mid-sequence

**Related Files:**
- `scripts/ui/event_screen.gd:46-51` - Added _exit_tree cleanup
- `scripts/ui/reward_screen.gd:74-75` - Where battle completion is triggered
- `scripts/application/battle_context.gd:119-127` - Where victory triggers reward screen

---

### Battle Rewards Not Validated Against Configuration
**Resolved:** 2025-11-24
**Component:** Rewards / Campaign System

**Description:**
There was no validation that the rewards displayed to the player match the battle configuration, or that reward cards actually exist in the card catalog.

**Solution Implemented:**
Added validation at two points:

1. **Startup Validation** (CampaignService._validate_battle_rewards):
   - Runs when battles are loaded in _init_battles()
   - Validates all reward_cards in all battles exist in CardCatalog
   - Logs errors with battle_id and catalog_id if invalid
   - Counts total invalid rewards and logs summary

2. **Runtime Validation** (RewardScreen._validate_rewards):
   - Runs before displaying rewards to player
   - Double-checks that reward cards still exist in catalog
   - Logs errors if player could receive invalid rewards
   - Acts as safety net for any config that slipped past startup validation

**Related Files:**
- `scripts/services/campaign_service.gd:173-201` - Startup validation
- `scripts/ui/reward_screen.gd:260-298` - Runtime validation

---

### Dialogue Speaker Names Not Properly Localized
**Resolved:** 2025-11-24
**Component:** Dialogue / Localization

**Description:**
The dialogue system had inconsistent formats - some dialogues used localization keys while others used raw text strings, causing `[MISSING:...]` warnings and broken localization.

**Solution Implemented:**
Standardized ALL 17 dialogue files to use localization keys:

1. **Dialogue .tres files** now use consistent format:
   - `character_name = "dialogue.{id}.speaker"`
   - `lines = ["dialogue.{id}.line_1", "dialogue.{id}.line_2", ...]`
   - `choice_text = "dialogue.{id}.choice_1"` (for choices)

2. **en.json** contains all dialogue text:
   ```json
   "dialogue": {
     "first_trial_intro": {
       "speaker": "Headmaster Merlin",
       "line_1": "Welcome to the training grounds, Initiate.",
       "line_2": "Your affinity chosen, your companion bound..."
     }
   }
   ```

3. **dialogue_manager.gd** simplified to just call `Loc.t()`:
   ```gdscript
   var line_text: String = Loc.t(line_key)
   var character: String = Loc.t(current_dialogue.character_name)
   ```

4. **dialogue_box.gd** updated to localize choice text:
   ```gdscript
   button.text = Loc.t(choice.choice_text)
   ```

**Related Files:**
- `scripts/application/dialogue_manager.gd` - Simplified localization
- `scripts/ui/dialogue_box.gd` - Added choice text localization
- `localization/data/en.json` - All dialogue text entries
- `resources/dialogue/*.tres` - All 17 dialogue files standardized

---

## 2025-01 Fixes

### Battle Rewards Re-Granted on Replay
**Resolved:** 2025-01-06
**Component:** Campaign / Rewards System

**Description:**
When replaying a completed battle, the player received reward cards again.

**Solution Implemented:**
- Added `is_replay` detection in `reward_screen.gd`
- Only grants rewards if battle not already completed
- Shows "Battle Already Completed" message on replay
- Uses `campaign.is_battle_completed()` check

---

### Enemy AI Not Spawning in Campaign Battles
**Resolved:** 2025-01-06
**Component:** AI / Campaign System

**Description:**
Enemy summoner was not playing cards during campaign battles, making them impossible to lose.

**Solution Implemented:**
- Fixed autoload name mismatch (CampaignService vs Campaign)
- Fixed AIController type signature to accept both Summoner and Summoner3D
- Added dynamic AI loading in GameController3D
- AI now properly instantiated from campaign config

---

### Cards Reference 2D Units Instead of 3D
**Resolved:** 2025-01-06
**Component:** Cards / Units

**Description:**
Several card resources (archer, warrior, wall, training_dummy) referenced 2D unit scenes, breaking 3D battles.

**Solution Implemented:**
- Created 3D versions of all missing units
- Updated card resources to reference new 3D scenes
- All cards now work in 2.5D battlefield

---

### Debug Print Statements in Production Code
**Resolved:** 2025-01-06
**Component:** Code Quality

**Description:**
Multiple files contained debug print statements that should not be in production.

**Solution Implemented:**
- Removed all debug prints from scripted_ai.gd
- Removed all debug prints from game_controller_3d.gd
- Removed debug helper function `_get_hand_names()`
- Kept only push_warning/push_error for actual issues
