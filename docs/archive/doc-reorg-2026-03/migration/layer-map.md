# Layer Map

Comprehensive system-to-layer assignment for the four-layer migration. Every significant system in the codebase is listed with its current location, target layer, and migration action.

**Actions:** Stays (already correct), Moves (relocates to target layer), Renames (same layer, new name), Splits (decomposed across layers), Deletes (retired), New (stub or future), Bridge (thin adapter during migration)

---

## Simulation Layer

Pure C#, zero Godot imports, no networking. Testable without the engine.

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `Simulation` | `Simulation/Simulation.cs` | Simulation | Stays | Core tick loop |
| `MatchState` | `Simulation/MatchState.cs` | Simulation | Stays | All game state |
| `MatchStateInvariants` | `Simulation/MatchStateInvariants.cs` | Simulation | Stays | State validation |
| `UnitData` | `Simulation/UnitData.cs` | Simulation | Stays | Unit state (NetworkId fields removed later, issue #5) |
| `SummonerData` | `Simulation/SummonerData.cs` | Simulation | Stays | Summoner state |
| `SimProjectileData` | `Simulation/SimProjectileData.cs` | Simulation | Stays | Projectile state |
| `SimCardData` | `Simulation/SimCardData.cs` | Simulation | Stays | Card state in sim |
| `SimConstants` | `Simulation/SimConstants.cs` | Simulation | Stays | Sim-layer constants |
| `SimVector3` | `Simulation/SimVector3.cs` | Simulation | Stays | Godot-free vector type |
| `SimUtils` | `Simulation/SimUtils.cs` | Simulation | Stays | Shared sim utilities |
| `BehaviorState` | `Simulation/BehaviorState.cs` | Simulation | Stays | Enum (replaced const ints) |
| `Team` | `Simulation/Team.cs` | Simulation | Stays | Value type stub |
| `GamePhase` | `Simulation/GamePhase.cs` | Simulation | Stays | Phase enum |
| `WinCondition` | `Simulation/WinCondition.cs` | Simulation | Stays | Win/loss types |
| `EffectTypes` | `Simulation/EffectTypes.cs` | Simulation | Stays | Buff/debuff types |
| `DeterministicRng` | `Simulation/DeterministicRng.cs` | Simulation | Stays | Deterministic RNG for sim |
| `SimBehavior` | `Simulation/Combat/SimBehavior.cs` | Simulation | Stays | Unit behavior state machine |
| `SimDamage` | `Simulation/Combat/SimDamage.cs` | Simulation | Stays | Damage calculation |
| `SimProjectile` | `Simulation/Combat/SimProjectile.cs` | Simulation | Stays | Projectile movement/collision |
| `SimTargeting` | `Simulation/Combat/SimTargeting.cs` | Simulation | Stays | Target selection |
| `SimEffects` | `Simulation/SimEffects.cs` | Simulation | Stays | Buff/debuff processing |
| `SimMovement` | `Simulation/Movement/SimMovement.cs` | Simulation | Stays | Movement calculation |
| `SimSteering` | `Simulation/Movement/SimSteering.cs` | Simulation | Stays | Steering behaviors |
| `ICommand` | `Simulation/Commands/ICommand.cs` | Simulation | Stays | Command interface |
| `PlayCardCommand` | `Simulation/Commands/PlayCardCommand.cs` | Simulation | Stays | Card play command |
| `ForfeitCommand` | `Simulation/Commands/ForfeitCommand.cs` | Simulation | Stays | Forfeit command |
| `SimEvent` types | `Simulation/ISimEventVisitor.cs` | Simulation | Stays | Event types |
| `SimEventCategory` | `Simulation/SimEventCategory.cs` | Simulation | Stays | Event categorization |

---

## Session Layer

Orchestrates simulation execution. Hides SP/MP differences behind `IGameSession`.

### New Components (Stubs Exist)

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `IGameSession` | `Session/IGameSession.cs` | Session | New | Single interface for Input+View |
| `LocalSession` | `Session/LocalSession.cs` | Session | New | Singleplayer session |
| `NetworkSession` | `Session/NetworkSession.cs` | Session | New | Abstract MP base |
| `HostSession` | `Session/HostSession.cs` | Session | New | Host-authoritative session |
| `ClientSession` | `Session/ClientSession.cs` | Session | New | Client session |
| `CommandRouter` | `Session/CommandRouter.cs` | Session | New | Universal command validation |
| `IdentityMap` | `Session/IdentityMap.cs` | Session | New | UnitId ↔ NetworkId bimap |
| `SnapshotCodec` | `Session/SnapshotCodec.cs` | Session | New | MatchState serialization |

### Current Systems Being Replaced

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `SimulationNode` | `Simulation/SimulationNode.cs` (942 lines) | Session / Bridge | Splits | God class → thin bridge; tick/snapshot/ID logic moves to Session |
| `HostRunner` | `Multiplayer/Authority/HostRunner.cs` | Session | Deletes | Replaced by `HostSession` |
| `ClientRunner` | `Multiplayer/Client/ClientRunner.cs` | Session | Deletes | Replaced by `ClientSession` |
| `MatchSession` | `Multiplayer/Core/MatchSession.cs` | Session | Deletes | Partially replaced by `NetworkSession` |
| `RequestValidator` | `Multiplayer/Authority/RequestValidator.cs` | Session | Deletes | Replaced by `CommandRouter` |
| `NetworkIdRegistry` | `Multiplayer/Core/NetworkIdRegistry.cs` | Session | Deletes | Replaced by `IdentityMap` |
| `StateSnapshotBuilder` | `Multiplayer/Sync/StateSnapshotBuilder.cs` | Session | Deletes | Replaced by `SnapshotCodec` |
| `DesyncDetector` | `Multiplayer/Sync/DesyncDetector.cs` | Session | Renames | → `DesyncChecker` (reads MatchState, not scene tree) |
| `HostEventBroadcaster` | `Multiplayer/Authority/HostEventBroadcaster.cs` | Session | Moves | Broadcast logic absorbed into `HostSession` |
| `StateInterpolator` | `Battle/View/StateInterpolator.cs` | View | Moves | Render-only smoothing owned by EntityManager; does not mutate MatchState |
| `PredictionBuffer` | `Multiplayer/Client/PredictionBuffer.cs` | Session | Moves | Client prediction, owned by `ClientSession` |
| `ReconnectionHandler` | `Multiplayer/Core/ReconnectionHandler.cs` | Session | Moves | Reconnection logic, owned by `NetworkSession` |
| `CoordinateTransform` | `Multiplayer/Core/CoordinateTransform.cs` | Session | Moves | Team-relative coordinate remapping at session boundary |
| `LocalPlayer` | `Multiplayer/Core/LocalPlayer.cs` | Session | Moves | Local player identity |
| `TeamIndex` | `Multiplayer/Core/TeamIndex.cs` | Session | Moves | `LocalTeam`/`NetworkTeam` structs → session boundary types |
| `IMatchRunner` | `Multiplayer/Core/IMatchRunner.cs` | — | Deletes | Old runner interface, not needed |
| `IMessageBroadcaster` | `Multiplayer/Core/IMessageBroadcaster.cs` | — | Deletes | Old broadcast interface, not needed |

---

## View Layer

Reads game state and renders it. No game logic, no mutation.

### New Components (Stubs Exist)

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `BattleScene` | `View/BattleScene.cs` | View | New | Top-level facade (replaces GameController3D) |
| `EntityManager` | `View/EntityManager.cs` | View | New | Entity lifecycle + event dispatch + registry |
| `UnitVisual` | `View/UnitVisual.cs` | View | New | Visual shell (replaces Unit3D) |
| `ProjectileVisual` | `View/ProjectileVisual.cs` | View | New | Visual shell (replaces Projectile3D) |
| `SummonerVisual` | `View/SummonerVisual.cs` | View | New | Visual shell (replaces visual code in summoner.gd) |

### Current Systems Migrating to View

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `Unit3D` | `Units/Unit3D.cs` (2304 lines) | View | Splits | Visual code → `UnitVisual`; game logic → sim subsystems |
| `MeleeUnit3D` | `Units/MeleeUnit3D.cs` | — | Deletes | Behavior differences handled by sim config |
| `RangedUnit3D` | `Units/RangedUnit3D.cs` | — | Deletes | Behavior differences handled by sim config |
| `DucklingUnit3D` | `Units/DucklingUnit3D.cs` | — | Deletes | Behavior differences handled by sim config |
| `Projectile3D` | `Projectiles/Projectile3D.cs` (1128 lines) | View | Splits | Visual code → `ProjectileVisual`; logic → SimProjectile |
| `GameController3D` | `scripts/core/game_controller_3d.gd` (1048 lines) | View | Splits | Wiring → `BattleScene`; game logic → Session |
| `summoner.gd` (visual parts) | `scripts/core/summoner.gd` | View | Splits | Visual rendering → `SummonerVisual`; deck/mana → Session init; input → InputCollector |
| `SimEventSignalEmitter` | `Simulation/SimEventSignalEmitter.cs` | — | Deletes | EntityManager reads `SimEventsEmitted` directly |
| `IVisualComponent` | `Visual/IVisualComponent.cs` | View | Stays | Visual component interface |
| `ShadowComponent` | `Visual/ShadowComponent.cs` | View | Stays | Shadow rendering |
| `SkeletalVisualComponent` | `Visual/SkeletalVisualComponent.cs` | View | Stays | Skeletal animation |
| `SpriteVisualComponent` | `Visual/SpriteVisualComponent.cs` | View | Stays | Sprite rendering |
| `SpawnRevealComponent` | `Units/Components/SpawnRevealComponent.cs` | View | Stays | Spawn-in visual effect |
| `FloatingHPBar` | `UI/FloatingHPBar.cs` | View | Stays | HP bar rendering |
| `VFXManager` | `scripts/battle/vfx/vfx_manager.gd` | View | Stays | VFX pooling + spawning |
| `vfx_definition.gd` | `scripts/battle/vfx/vfx_definition.gd` | View | Stays | VFX configuration |
| `vfx_instance.gd` | `scripts/battle/vfx/vfx_instance.gd` | View | Stays | VFX instance |
| `VfxId` | `Vfx/VfxId.cs` | View | Stays | VFX identifier type |
| Spell VFX scripts | `scripts/battle/vfx/fireball_spell_vfx.gd`, `lightning_strike_vfx.gd` | View | Stays | Spell visual effects |
| `redirect_indicator.gd` | ~~`scripts/battle/vfx/redirect_indicator.gd`~~ | View | Deleted | Dead code — referenced deleted RedirectManager |
| `BattlefieldVisuals3D` | `scripts/battle/battlefield/battlefield_visuals_3d.gd` | View | Renames | → `BattlefieldEnvironment` |
| `base_battlefield_3d.gd` | `scripts/battle/battlefield/base_battlefield_3d.gd` | View | Stays | Base battlefield scene |
| `biome_config.gd` | `scripts/battle/battlefield/biome_config.gd` | View | Stays | Biome visual configuration |
| `battlefield_constants.gd` | `scripts/battle/battlefield/battlefield_constants.gd` | View | Stays | Battlefield dimensions |
| `CameraController3D` | `scripts/battle/battlefield/camera_controller_3d.gd` | View | Renames | → `BattleCamera` |
| `UnitSteering` | `Movement/UnitSteering.cs` | View | Stays | Godot-side movement (reads SimMovement results) |
| `SummonPreview` | `Input/SummonPreview.cs` | Input | Stays | Spawn preview visualization |
| `UnitGhost` | `Input/UnitGhost.cs` | Input | Stays | Ghost preview unit |
| `UnitDebugService` | `Units/UnitDebugService.cs` | View/Debug | Stays | Debug visualization |
| `DamageProfile` | `Units/DamageProfile.cs` | View | Stays | Visual damage representation |
| `Enums` | `Units/Enums.cs` | Cross-cutting | Stays | `ActivationState`, shared enums |
| Animation scripts | `scripts/battle/animations/*.gd` (3 files) | View | Stays | Animation configuration and control |
| Unit rig scripts | `scripts/battle/animations/*_rig.gd` (3 files) | View | Stays | Skeletal rig scripts |

### Battle HUD (View — Independent Peer)

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `hand_ui.gd` | `scripts/battle/ui/hand_ui.gd` | View + Input | Splits | Card rendering → View (HUD); drag gesture → Input |
| `game_ui.gd` | `scripts/battle/ui/game_ui.gd` | View | Stays | Battle HUD container |
| `stat_bar.gd` | `scripts/battle/ui/stat_bar.gd` | View | Stays | HP/mana bars |
| `pause_button.gd` | `scripts/battle/ui/pause_button.gd` | View | Stays | UI button |
| `speed_button.gd` | `scripts/battle/ui/speed_button.gd` | View | Stays | Speed control UI |
| `spell_preview.gd` | `scripts/battle/ui/spell_preview.gd` | View | Stays | Spell preview rendering |
| `spawn_zone_overlay.gd` | `scripts/battle/ui/spawn_zone_overlay.gd` | View | Stays | Spawn zone visualization |
| `HPBarService` | `Services/HPBarService.cs` (autoload) | View | Moves | Creates HP bars — View-layer service |

---

## Input Layer

Captures player intent, produces Commands.

### New Components (Stubs Exist)

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `InputCollector` | `Input/InputCollector.cs` | Input | New | Gesture → Command translation |

### Current Systems Migrating to Input

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `battlefield_drop_zone.gd` | `scripts/battle/ui/battlefield_drop_zone.gd` | Input | Moves | Card drop detection → InputCollector |
| `player_input_3d.gd` | `scripts/core/player_input_3d.gd` | Input | Deletes | Replaced by InputCollector |
| `player_input.gd` | `scripts/core/player_input.gd` | Input | Deletes | Replaced by InputCollector |
| `summoner.gd` (input parts) | `scripts/core/summoner.gd` | Input | Splits | `play_card_3d()` command production → InputCollector |

### AI System (Input Peer — Decision #12)

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `ai_controller.gd` | `scripts/ai/ai_controller.gd` | Input | Stays | AI reads `IGameSession.GetState()`, calls `SubmitCommand()` |
| `heuristic_ai.gd` | `scripts/ai/heuristic_ai.gd` | Input | Stays | Heuristic AI strategy |
| `scripted_ai.gd` | `scripts/ai/scripted_ai.gd` | Input | Stays | Scripted AI strategy |
| `ai_loader.gd` | `scripts/ai/ai_loader.gd` | Input | Stays | AI loading utility |
| `simple_ai.gd` | `scripts/core/simple_ai.gd` | Input | Stays | Simplified AI |

### Current Autoloads to Retire (Input Replacement)

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `SpellTargetingManager` | `scripts/battle/ui/spell_targeting_manager.gd` (autoload) | — | Deletes | Gesture → InputCollector; visuals → View |
| `RedirectManager` | `scripts/managers/redirect_manager.gd` (autoload) | — | Deletes | Redirect gesture → InputCollector producing RedirectCommand |

---

## Cross-Cutting

Types shared across multiple layers. Pure data, no game logic.

### Cards/

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `Card` | `Cards/Card.cs` | Cross-cutting | Stays | Core card type |
| `CardDefinition` | `Cards/CardDefinition.cs` | Cross-cutting | Stays | Card definition data |
| `CardDefinitions` | `Cards/CardDefinitions.cs` | Cross-cutting | Stays | Card definition registry |
| `CardCatalog` | `Cards/CardCatalog.cs` | Cross-cutting | Stays | C# catalog |
| `CardCatalogBridge` | `Cards/CardCatalogBridge.cs` | Cross-cutting | Stays | GDScript ↔ C# bridge |
| `CardId` | `Cards/CardId.cs` | Cross-cutting | Stays | ID type |
| `CardInstanceId` | `Cards/CardInstanceId.cs` | Cross-cutting | Stays | Instance ID type |
| `CardTraitId` | `Cards/CardTraitId.cs` | Cross-cutting | Stays | Trait ID type |
| `CardTraitIds` | `Cards/CardTraitIds.cs` | Cross-cutting | Stays | Known trait IDs |
| `CardType` | `Cards/CardType.cs` | Cross-cutting | Stays | Enum |
| `CardFlags` | `Cards/CardFlags.cs` | Cross-cutting | Stays | Flags enum |
| `Rarity` | `Cards/Rarity.cs` | Cross-cutting | Stays | Rarity enum |
| `Element` | `Cards/Element.cs` | Cross-cutting | Stays | Element enum |
| `CreatureType` | `Cards/CreatureType.cs` | Cross-cutting | Stays | Creature type enum |
| `UnitType` | `Cards/UnitType.cs` | Cross-cutting | Stays | Unit type enum |
| `SummonRole` | `Cards/SummonRole.cs` | Cross-cutting | Stays | Role enum |
| `SpellCategory` | `Cards/SpellCategory.cs` | Cross-cutting | Stays | Spell category enum |
| `SpellTargeting` | `Cards/SpellTargeting.cs` | Cross-cutting | Stays | Targeting type enum |
| `UnlockCondition` | `Cards/UnlockCondition.cs` | Cross-cutting | Stays | Unlock data |
| `VisualTrait` | `Cards/VisualTrait.cs` | Cross-cutting | Stays | Visual trait data |
| `SummonCard` | `Cards/SummonCard.cs` | Cross-cutting | Stays | Summon card type |
| `SpellCard` | `Cards/SpellCard.cs` | Cross-cutting | Stays | Spell card type |
| `SummonBuilder` | `Cards/SummonBuilder.cs` | Cross-cutting | Stays | Builder pattern |
| `SpellBuilder` | `Cards/SpellBuilder.cs` | Cross-cutting | Stays | Builder pattern |
| `CardConfig` | `Cards/Configs/CardConfig.cs` | Cross-cutting | Stays | Godot Resource config |
| `SpawnConfig` | `Cards/Configs/SpawnConfig.cs` | Cross-cutting | Stays | Spawn configuration |
| `SpellCardConfig` | `Cards/Configs/SpellCardConfig.cs` | Cross-cutting | Stays | Spell card Resource |
| `SummonCardConfig` | `Cards/Configs/SummonCardConfig.cs` | Cross-cutting | Stays | Summon card Resource |
| `CardFactory` | `Cards/CardFactory.cs` (autoload) | Cross-cutting | Stays | Card instance creation (pending Phase 1 decision) |
| `Affinity` | `Cards/Effects/Core/Affinity.cs` | Cross-cutting | Stays | Element affinity data |

### Cards/Effects — Spell Effect System

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `ISpellEffect` | `Cards/Effects/Core/ISpellEffect.cs` | Cross-cutting | Stays | Effect interface |
| `SpellEffect` | `Cards/Effects/Core/SpellEffect.cs` | Cross-cutting | Stays | Base effect class |
| `SpellContext` | `Cards/Effects/Core/SpellContext.cs` | Cross-cutting | Stays | Effect execution context |
| `CommandEffect` | `Cards/Effects/Concrete/CommandEffect.cs` | Cross-cutting | Stays | Effect that produces commands |
| `CompositeEffect` | `Cards/Effects/Concrete/CompositeEffect.cs` | Cross-cutting | Stays | Composite effect |
| `ConditionalEffect` | `Cards/Effects/Concrete/ConditionalEffect.cs` | Cross-cutting | Stays | Conditional effect |
| `DamageEffect` | `Cards/Effects/Concrete/DamageEffect.cs` | Cross-cutting | Stays | Damage dealing effect |
| `ISpellCondition` | `Cards/Effects/Conditions/ISpellCondition.cs` | Cross-cutting | Stays | Condition interface |
| `HPThresholdCondition` | `Cards/Effects/Conditions/HPThresholdCondition.cs` | Cross-cutting | Stays | HP-based condition |
| `ITargetFilter` | `Cards/Effects/Filters/ITargetFilter.cs` | Cross-cutting | Stays | Target filter interface |
| `ITargetingStrategy` | `Cards/Effects/Targeting/ITargetingStrategy.cs` | Cross-cutting | Stays | Targeting strategy |
| `CircleTargeting` | `Cards/Effects/Targeting/CircleTargeting.cs` | Cross-cutting | Stays | Circle area targeting |
| `NearestEnemyTargeting` | `Cards/Effects/Targeting/NearestEnemyTargeting.cs` | Cross-cutting | Stays | Nearest enemy targeting |

### Cards/Formations

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `IFormationStrategy` | `Cards/Formations/IFormationStrategy.cs` | Cross-cutting | Stays | Formation interface |
| `FormationPresets` | `Cards/Formations/FormationPresets.cs` | Cross-cutting | Stays | Preset formations |
| `GridFormation` | `Cards/Formations/GridFormation.cs` | Cross-cutting | Stays | Grid layout |
| `GroupedLineFormation` | `Cards/Formations/GroupedLineFormation.cs` | Cross-cutting | Stays | Line layout |
| `LineFormation` | `Cards/Formations/LineFormation.cs` | Cross-cutting | Stays | Line layout |
| `RingFormation` | `Cards/Formations/RingFormation.cs` | Cross-cutting | Stays | Ring layout |

### Cards/Spawning

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `SpawnPlacement` | `Cards/Spawning/SpawnPlacement.cs` | Cross-cutting | Stays | Spawn position data |
| `SummonSpec` | `Cards/Spawning/SummonSpec.cs` | Cross-cutting | Stays | Summon specification |
| `UnitSpawnEntry` | `Cards/Spawning/UnitSpawnEntry.cs` | Cross-cutting | Stays | Spawn entry data |

### Summons

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `UnitSpawner` | `Summons/UnitSpawner.cs` | Cross-cutting | Stays | Creates unit data from definitions |
| `SpawnPositionCalculator` | `Summons/SpawnPositionCalculator.cs` | Cross-cutting | Stays | Position math |
| `SummonResult` | `Summons/SummonResult.cs` | Cross-cutting | Stays | Spawn result data |
| `UnitSummon` | `Summons/UnitSummon.cs` | Cross-cutting | Stays | Summon data |

### Data Catalogs (C#)

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `CardTraitCatalog` | `Data/CardTraitCatalog.cs` | Cross-cutting | Stays | Trait definitions |
| `ProjectileCatalog` | `Data/Projectiles/ProjectileCatalog.cs` | Cross-cutting | Stays | Projectile definitions |
| `ProjectileDefinitions` | `Data/Projectiles/ProjectileDefinitions.cs` | Cross-cutting | Stays | Projectile data |
| `SummonerCatalog` | `Data/Summoners/SummonerCatalog.cs` | Cross-cutting | Stays | Summoner definitions |
| `SummonerCatalogBridge` | `Data/Summoners/SummonerCatalogBridge.cs` | Cross-cutting | Stays | GDScript ↔ C# bridge |
| `SummonerDefinition` | `Data/Summoners/SummonerDefinition.cs` | Cross-cutting | Stays | Summoner data |
| `SummonerId` | `Data/Summoners/SummonerId.cs` | Cross-cutting | Stays | ID type |
| `SummonerUnlockCondition` | `Data/Summoners/SummonerUnlockCondition.cs` | Cross-cutting | Stays | Unlock data |
| `TraitCatalog` | `Data/Traits/TraitCatalog.cs` | Cross-cutting | Stays | Trait catalog |
| `TraitCatalogBridge` | `Data/Traits/TraitCatalogBridge.cs` | Cross-cutting | Stays | GDScript ↔ C# bridge |
| `TraitDefinition` | `Data/Traits/TraitDefinition.cs` | Cross-cutting | Stays | Trait data |
| `TraitDefinitions` | `Data/Traits/TraitDefinitions.cs` | Cross-cutting | Stays | Trait registry |
| `TraitId` | `Data/Traits/TraitId.cs` | Cross-cutting | Stays | ID type |
| `TraitCategory` | `Data/Traits/TraitCategory.cs` | Cross-cutting | Stays | Category enum |
| `TraitTags` | `Data/Traits/TraitTags.cs` | Cross-cutting | Stays | Tag constants |
| `TraitTargetType` | `Data/Traits/TraitTargetType.cs` | Cross-cutting | Stays | Target type enum |
| `ModifierType` | `Data/Traits/ModifierType.cs` | Cross-cutting | Stays | Modifier type enum |
| `EventCatalog` | `Data/Events/EventCatalog.cs` | Cross-cutting | Stays | Event definitions |
| `EventDefinition` | `Data/Events/EventDefinition.cs` | Cross-cutting | Stays | Event data |
| `CampaignCatalog` | `Data/Events/CampaignCatalog.cs` | Cross-cutting | Stays | Campaign definitions |
| `CampaignDefinition` | `Data/Events/CampaignDefinition.cs` | Cross-cutting | Stays | Campaign data |
| Various event IDs/types | `Data/Events/*.cs` (10+ files) | Cross-cutting | Stays | ID types, enums |
| `ItemCatalog` | `Data/Items/ItemCatalog.cs` | Cross-cutting | Stays | Item definitions |
| `ItemDefinition` / `ItemDefinitions` | `Data/Items/*.cs` | Cross-cutting | Stays | Item data |
| `ItemId` / `ItemSlot` | `Data/Items/*.cs` | Cross-cutting | Stays | ID and slot types |
| `UnitDefinition` | `Units/UnitDefinition.cs` | Cross-cutting | Stays | Unit config data |
| `UnitDefinitions` | `Units/UnitDefinitions.cs` | Cross-cutting | Stays | Unit registry |

### Constants (C#)

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `BattlefieldBounds` | `Constants/BattlefieldBounds.cs` | Simulation | Moves | Bounds used by sim for movement/targeting |
| `ElementMatchups` | `Constants/ElementMatchups.cs` | Simulation | Moves | Used by SimDamage |
| `ElementColors` | `Constants/ElementColors.cs` | View | Moves | Visual colors for elements |
| `GroupIDs` | `Constants/GroupIDs.cs` | Cross-cutting | Stays | Godot group name constants |
| `UnitId` (Constants) | `Constants/UnitId.cs` | Cross-cutting | Stays | Unit ID constants |

### Stats

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `UnitStatCalculator` | `Stats/UnitStatCalculator.cs` | Cross-cutting | Stays | Stat computation (review deps) |
| `UnitStats` | `Stats/UnitStats.cs` | Cross-cutting | Stays | Stat data structure |
| `StatKey` | `Stats/StatKey.cs` | Cross-cutting | Stays | Stat identifier enum |

### Combat Types (non-sim)

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `CombatEvent` | `Combat/CombatEvent.cs` | Cross-cutting | Stays | Event data type (review if sim-only) |
| `DamageType` | `Combat/DamageType.cs` | Cross-cutting | Stays | Damage type enum |
| `SpellId` | `Combat/SpellId.cs` | Cross-cutting | Stays | Spell ID type |

### Projectile Types (non-service)

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `ProjectileId` | `Projectiles/ProjectileId.cs` | Cross-cutting | Stays | ID type |
| `ProjectileMovementType` | `Projectiles/ProjectileMovementType.cs` | Cross-cutting | Stays | Movement enum |
| `SpeedEasingType` | `Projectiles/SpeedEasingType.cs` | Cross-cutting | Stays | Easing enum |
| `ProjectileData` | `Projectiles/ProjectileData.cs` | Cross-cutting | Stays | Projectile config data (review if redundant with SimProjectileData) |
| `IProjectilePath` | `Projectiles/Paths/IProjectilePath.cs` | View | Stays | Path interface for visual interpolation |
| `ArcPath` | `Projectiles/Paths/ArcPath.cs` | View | Stays | Visual arc path |
| `BallisticPath` | `Projectiles/Paths/BallisticPath.cs` | View | Stays | Visual ballistic path |
| `StraightPath` | `Projectiles/Paths/StraightPath.cs` | View | Stays | Visual straight path |

### GDScript Data Files

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `card_catalog.gd` | `scripts/infrastructure/data/card_catalog.gd` | Cross-cutting | Stays | GDScript catalog autoload |
| `summoner_catalog.gd` | `scripts/infrastructure/data/summoner_catalog.gd` | Cross-cutting | Stays | GDScript catalog autoload |
| `trait_catalog.gd` | `scripts/infrastructure/data/trait_catalog.gd` | Cross-cutting | Stays | GDScript catalog autoload |
| `card_trait_catalog.gd` | `scripts/infrastructure/data/card_trait_catalog.gd` | Cross-cutting | Stays | GDScript catalog |
| `cosmetics_catalog.gd` | `scripts/infrastructure/data/cosmetics_catalog.gd` | Cross-cutting | Stays | Cosmetics data |
| `emotes_catalog.gd` | `scripts/infrastructure/data/emotes_catalog.gd` | Cross-cutting | Stays | Emotes data |
| `content_binding.gd` | `scripts/infrastructure/data/content_binding.gd` | Cross-cutting | Stays | Content binding |
| `unit_constants.gd` | `scripts/infrastructure/data/unit_constants.gd` | Cross-cutting | Stays | Mirror enum pattern for C# interop |
| `deck_constants.gd` | `scripts/infrastructure/data/deck_constants.gd` | Cross-cutting | Stays | Deck rules |
| All `*_ids.gd` files | `scripts/infrastructure/data/*_ids.gd` (17 files) | Cross-cutting | Stays | ID constant files |
| `card_config.gd` | `scripts/cards/card_config.gd` | Cross-cutting | Stays | GDScript card config |
| `card.gd` | `scripts/cards/card.gd` | Cross-cutting | Stays | GDScript card class |
| `json_profile_repository.gd` | `scripts/infrastructure/data/json_profile_repository.gd` | Infrastructure | Stays | GDScript profile persistence |
| `profile_repository.gd` | `scripts/infrastructure/data/profile_repository.gd` | Infrastructure | Stays | Profile repo interface |

### GDScript Core Utilities

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `element_types.gd` | `scripts/infrastructure/element_types.gd` (autoload) | Cross-cutting | Stays | Element type data |
| `element_registry.gd` | `scripts/infrastructure/element_registry.gd` | Cross-cutting | Stays | Element lookup |
| `fonts.gd` | `scripts/infrastructure/fonts.gd` (autoload) | View | Stays | Font resources |
| `safe_type_utils.gd` | `scripts/infrastructure/safe_type_utils.gd` | Cross-cutting | Stays | Type safety utilities |
| `base.gd` | `scripts/core/base.gd` | Cross-cutting | Stays | Base class |
| `csharp_autoloads.gd` | `scripts/infrastructure/csharp_autoloads.gd` | Infrastructure | Stays | C# autoload initialization |
| `physics_layers.gd` | `scripts/infrastructure/physics_layers.gd` (autoload) | Cross-cutting | Stays | Physics layer constants |
| `summoner_config.gd` | `scripts/infrastructure/summoner_config.gd` | Cross-cutting | Stays | Summoner configuration |
| `summoner_instance.gd` | `scripts/core/summoner_instance.gd` | Cross-cutting | Stays | Summoner instance data |
| `summoner_registry.gd` | `scripts/infrastructure/summoner_registry.gd` | Cross-cutting | Stays | Summoner lookup |
| `deck_loader.gd` | `scripts/core/deck_loader.gd` | Cross-cutting | Stays | Deck loading utility |
| `enemy_deck_loader.gd` | `scripts/core/enemy_deck_loader.gd` | Cross-cutting | Stays | Enemy deck loading |
| `player_camera.gd` | `scripts/battle/player_camera.gd` | View | Stays | Non-battle camera |

---

## Meta-Game Services

Services and domain objects outside the battle loop. Operate between battles.

### C# Services

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `CampaignService` + handlers | `Services/Campaign/` (14 files) | Meta-game | Stays | Campaign progression |
| `CardService` + handlers | `Services/Cards/` (3 files) | Meta-game | Stays | Card ownership/progression |
| `DeckService` + handlers | `Services/Deck/` (5 files) | Meta-game | Stays | Deck CRUD |
| `EconomyService` | `Services/Economy/EconomyService.cs` | Meta-game | Stays | Currency management |
| `ItemService` + handlers | `Services/Items/` (3 files) | Meta-game | Stays | Item ownership |
| `RewardService` + types | `Services/Rewards/` (5 files) | Meta-game | Stays | Battle rewards |
| `ShopService` + types | `Services/Shop/` (7 files) | Meta-game | Stays | Shop logic |
| `SummonerProgressionService` | `Services/Summoner/SummonerProgressionService.cs` | Meta-game | Stays | Summoner leveling |
| `SummonerSelectionService` | `Services/Summoner/SummonerSelectionService.cs` | Meta-game | Stays | Summoner selection |
| `LevelCapService` | `Services/LevelCapService.cs` | Meta-game | Stays | Level cap data |
| `BattleId` / `ChoiceId` / `NodeId` / `DeckId` | `Services/Campaign/*.cs`, `Services/Deck/*.cs` | Meta-game | Stays | ID types |

### C# Domain

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `ProfileData` | `Domain/Profile/ProfileData.cs` | Meta-game | Stays | Player profile root |
| `ProfileId` | `Domain/Profile/ProfileId.cs` | Meta-game | Stays | Profile ID type |
| Account types | `Domain/Profile/Account/` (7 files) | Meta-game | Stays | Account data |
| Campaign progress | `Domain/Profile/Campaign/CampaignProgress.cs` | Meta-game | Stays | Progress tracking |
| Collection types | `Domain/Profile/Collection/` (2 files) | Meta-game | Stays | Card collection |
| Deck types | `Domain/Profile/Decks/Deck.cs` | Meta-game | Stays | Deck data |
| Inventory types | `Domain/Profile/Inventory/` (2 files) | Meta-game | Stays | Item inventory |
| Shop state | `Domain/Profile/Shop/ShopRefreshState.cs` | Meta-game | Stays | Shop refresh tracking |
| Summoner instances | `Domain/Profile/Summoners/SummonerInstance.cs` | Meta-game | Stays | Owned summoners |
| Enum/ID types | `Domain/Profile/Enums/`, `CosmeticId.cs`, `EmoteId.cs`, `SkinId.cs` | Meta-game | Stays | Domain enums |

### Service Interfaces (C#)

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `ICardFactory` | `Services/Interfaces/ICardFactory.cs` | Cross-cutting | Stays | CardFactory interface |
| `IDamageSystem` | `Services/Interfaces/IDamageSystem.cs` | — | Deletes | DamageSystem being deleted |
| `IModifierService` | `Services/Interfaces/IModifierService.cs` | — | Deletes | ModifierService being deleted |

### GDScript Service Facades

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `campaign_service.gd` | `scripts/services/campaign_service.gd` (autoload) | Meta-game | Stays | GDScript facade for `CampaignServiceCS` |
| `deck_service.gd` | `scripts/services/deck_service.gd` (autoload) | Meta-game | Stays | GDScript facade for `DeckServiceCS` |
| `economy_service.gd` | `scripts/services/economy_service.gd` (autoload) | Meta-game | Stays | GDScript facade for `EconomyServiceCS` |
| `item_service.gd` | `scripts/services/item_service.gd` (autoload) | Meta-game | Stays | GDScript facade for `ItemServiceCS` |
| `reward_service.gd` | `scripts/services/reward_service.gd` (autoload) | Meta-game | Stays | GDScript facade for `RewardServiceCS` |
| `shop_service.gd` | `scripts/services/shop_service.gd` (autoload) | Meta-game | Stays | GDScript facade for `ShopServiceCS` |
| `summoner_progression_service.gd` | `scripts/services/summoner_progression_service.gd` (autoload) | Meta-game | Stays | GDScript facade for `SummonerProgressionCS` |
| `DialogueManager` | `scripts/application/dialogue_manager.gd` (autoload) | Meta-game | Stays | Dialogue orchestration |
| `EventSequencer` | `scripts/application/event_sequencer.gd` (autoload) | Meta-game | Stays | Campaign event sequences |
| `CapabilityManager` | `scripts/application/capability_manager.gd` (autoload) | Meta-game | Stays | Feature flags |

### GDScript Resources

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `event_sequence.gd` | `scripts/resources/event_sequence.gd` | Meta-game | Stays | Event sequence resource |
| `event_step.gd` | `scripts/resources/event_step.gd` | Meta-game | Stays | Event step resource |
| `shop_offering.gd` | `scripts/resources/shop_offering.gd` | Meta-game | Stays | Shop offering resource |
| `shop_purchase_context.gd` | `scripts/resources/shop_purchase_context.gd` | Meta-game | Stays | Purchase context |

### Dialogue System

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `dialogue_data.gd` | `scripts/infrastructure/dialogue/dialogue_data.gd` | Meta-game | Stays | Dialogue data |
| `dialogue_choice.gd` | `scripts/infrastructure/dialogue/dialogue_choice.gd` | Meta-game | Stays | Dialogue choice data |
| `battle_dialogue_controller.gd` | `scripts/battle/battle_dialogue_controller.gd` | View | Stays | In-battle dialogue rendering |
| `dialogue_box.gd` | `scripts/shared/dialogue_box.gd` | View | Stays | Dialogue UI component |

---

## Infrastructure

Transport, persistence, matchmaking, ranking, scene navigation.

### Multiplayer Infrastructure

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `IMatchTransport` | `Multiplayer/Transport/IMatchTransport.cs` | Infrastructure | Stays | Transport interface |
| `NakamaMatchTransport` | `Multiplayer/Transport/NakamaMatchTransport.cs` | Infrastructure | Stays | Nakama transport impl |
| `P2PTransport` | `Multiplayer/Transport/P2PTransport.cs` | Infrastructure | Stays | P2P transport impl |
| `NakamaGameClient` | `Multiplayer/Backend/NakamaGameClient.cs` | Infrastructure | Stays | Backend client |
| `MatchmakingService` | `Multiplayer/Matchmaking/MatchmakingService.cs` | Infrastructure | Stays | Matchmaking |
| `Messages` | `Multiplayer/Protocol/Messages.cs` | Infrastructure | Stays | Protocol messages |
| `MessageSerializer` | `Multiplayer/Protocol/MessageSerializer.cs` | Infrastructure | Stays | Serialization |
| `EloCalculator` | `Multiplayer/Ranking/EloCalculator.cs` | Infrastructure | Stays | Elo math |
| `LeaderboardService` | `Multiplayer/Ranking/LeaderboardService.cs` | Infrastructure | Stays | Leaderboard |
| `MatchReporter` | `Multiplayer/Ranking/MatchReporter.cs` | Infrastructure | Stays | Match reporting |
| `RankingService` | `Multiplayer/Ranking/RankingService.cs` | Infrastructure | Stays | Ranking |

### Persistence

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `IProfileRepository` | `Infrastructure/Persistence/IProfileRepository.cs` | Infrastructure | Stays | Repo interface |
| `ProfileRepository` (C#) | `Infrastructure/Persistence/ProfileRepository.cs` | Infrastructure | Stays | C# implementation |
| `DtoConverters` | `Infrastructure/Persistence/DtoConverters.cs` | Infrastructure | Stays | DTO mapping |

### Scene Navigation and Battle Setup

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `SceneManager` | `scripts/application/scene_manager.gd` (autoload) | Infrastructure | Stays | Scene transitions |
| `SceneCoordinator` | `scripts/application/scene_coordinator.gd` (autoload) | Infrastructure | Stays | Scene flow coordination |
| `NavigationContext` | `scripts/application/navigation_context.gd` (autoload) | Infrastructure | Stays | Navigation state |
| `BattleContext` | `scripts/application/battle_context.gd` (autoload) | Infrastructure | Stays | Builds typed `BattleConfig` for session constructors (Decision #13) |
| `EventContext` | `scripts/application/event_context.gd` (autoload) | Infrastructure | Stays | Campaign event context (same pattern as BattleContext) |

### GDScript Multiplayer

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `network_state.gd` | `scripts/multiplayer/core/network_state.gd` (autoload) | Infrastructure | Stays | Network state tracking |
| `authority_provider.gd` | `scripts/multiplayer/authority/authority_provider.gd` | Infrastructure | Stays | Authority abstraction |
| `local_authority.gd` | `scripts/multiplayer/authority/local_authority.gd` | Infrastructure | Stays | Local authority impl |
| `multiplayer_authority.gd` | `scripts/multiplayer/authority/multiplayer_authority.gd` | Infrastructure | Stays | MP authority impl |

### Billing

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `billing_catalog.gd` | `scripts/infrastructure/billing/billing_catalog.gd` (autoload) | Infrastructure | Stays | Billing product catalog |
| `billing_product.gd` | `scripts/infrastructure/billing/billing_product.gd` | Infrastructure | Stays | Product data |
| `billing_provider.gd` | `scripts/infrastructure/billing/billing_provider.gd` | Infrastructure | Stays | Provider interface |
| `platform_billing.gd` | `scripts/infrastructure/billing/platform_billing.gd` (autoload) | Infrastructure | Stays | Platform integration |
| `stub_billing_provider.gd` | `scripts/infrastructure/billing/stub_billing_provider.gd` | Infrastructure | Stays | Stub for dev |

---

## Standalone Services (Outside Layer Model)

Services callable by any layer. Not owned by Simulation, Session, View, or Input. See Decision #10.

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `AudioManager` | `scripts/infrastructure/audio_manager.gd` (autoload) | Standalone | Stays | Audio triggered from View (battle SFX), HUD (UI clicks), meta-game (music). No single layer owns all audio needs. |

---

## UI Screens (Outside Battle)

UI screens and modals — all GDScript, all View layer. Listed for completeness.

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `title_screen.gd` | `scripts/meta/screens/title_screen.gd` | View | Stays | Title screen |
| `campaign_map.gd` | `scripts/meta/screens/campaign_map.gd` | View | Stays | Campaign map |
| `collection_screen.gd` | `scripts/meta/screens/collection_screen.gd` | View | Stays | Card collection |
| `summoner_screen.gd` | `scripts/meta/screens/summoner_screen.gd` | View | Stays | Summoner details |
| `summoner_selection.gd` | `scripts/meta/screens/summoner_selection.gd` | View | Stays | Summoner select |
| `summoner_switch_screen.gd` | `scripts/meta/screens/summoner_switch_screen.gd` | View | Stays | Switch summoner |
| `shop_screen.gd` | `scripts/meta/screens/shop_screen.gd` | View | Stays | Shop UI |
| `caravan_screen.gd` | `scripts/meta/screens/caravan_screen.gd` | View | Stays | Caravan UI |
| `event_screen.gd` | `scripts/meta/screens/event_screen.gd` | View | Stays | Event UI |
| `reward_screen.gd` | `scripts/meta/screens/reward_screen.gd` | View | Stays | Reward UI |
| `settings_screen.gd` | `scripts/meta/screens/settings_screen.gd` | View | Stays | Settings |
| `multiplayer_lobby.gd` | `scripts/meta/screens/multiplayer_lobby.gd` | View | Stays | MP lobby |
| `online_screen.gd` | `scripts/meta/screens/online_screen.gd` | View | Stays | Online menu |
| `premium_store_screen.gd` | `scripts/meta/screens/premium_store_screen.gd` | View | Stays | Premium store |
| `special_events_screen.gd` | `scripts/meta/screens/special_events_screen.gd` | View | Stays | Special events |
| `first_card_selection.gd` | `scripts/meta/screens/first_card_selection.gd` | View | Stays | First card pick |
| `snapshot_manager.gd` | `scripts/meta/screens/snapshot_manager.gd` | View | Stays | Debug snapshots |
| All UI components | `scripts/meta/components/*.gd` (20+ files) | View | Stays | Shared UI components |
| All UI modals | `scripts/ui/modals/*.gd` (6 files) | View | Stays | Modal dialogs |
| UI styles | `scripts/shared/button_style_factory.gd` | View | Stays | Style utilities |
| Node panels | `scripts/meta/components/node_panels/*.gd` (7 files) | View | Stays | Campaign node panels |
| Debug UI | `scripts/battle/ui/debug/*.gd` (4 files) | Debug | Stays | Debug UI components |

---

## Debug

| System | Current Location | Target Layer | Action | Rationale |
|--------|-----------------|-------------|--------|-----------|
| `DevConsole` | `scripts/debug/dev_console.gd` (autoload) | Debug | Stays | Developer console |
| `DebugMenu` | `scripts/debug/debug_menu.gd` (autoload) | Debug | Stays | Debug menu |
| `DebugSnapshots` | `scripts/debug/debug_snapshots.gd` (autoload) | Debug | Stays | Snapshot debugging |
| `PerformanceCounters` | `Debug/PerformanceCounters.cs` | Debug | Stays | Perf tracking |
| `debug_arena_controller.gd` | `scripts/core/debug_arena_controller.gd` | Debug | Stays | Debug battle scene |
| `test_game_controller.gd` | `scripts/core/test_game_controller.gd` | Debug | Stays | Test battle setup |
| `test_collision_controller.gd` | `scripts/core/test_collision_controller.gd` | Debug | Stays | Collision testing |
| `dialogue_test.gd` | `scripts/dialogue_test.gd` | Debug | Stays | Dialogue testing |
| `rally_guard_test_setup.gd` | `scripts/test/rally_guard_test_setup.gd` | Debug | Stays | Rally guard testing |
| `dialogue_resource_generator.gd` | `scripts/tools/dialogue_resource_generator.gd` | Debug | Stays | Tool script |

---

## Delete Queue

Systems to be retired. Grouped by blocker. See `docs/migration/planning-checklist.md` Phase 7 for the full deletion sequence.

### Blocked by Unit3D → UnitVisual Migration

| System | Current Location | Blocked By | Action |
|--------|-----------------|-----------|--------|
| `DamageSystem.cs` + `.tscn` | `Combat/DamageSystem.cs` | Unit3D uses it for Godot-side damage | Deletes |
| `ModifierService.cs` + `.tscn` | `Systems/Modifiers/ModifierService.cs` | Unit3D applies modifiers through it | Deletes |
| Modifier system (8 files) | `Systems/Modifiers/*.cs` | ModifierService deletion | Deletes |
| `ProjectileService.cs` + `.tscn` | `Projectiles/ProjectileService.cs` | RangedUnit3D, DamageEffect reference it | Deletes |
| `IDamageSystem` | `Services/Interfaces/IDamageSystem.cs` | DamageSystem deletion | Deletes |
| `IModifierService` | `Services/Interfaces/IModifierService.cs` | ModifierService deletion | Deletes |

### Blocked by View Layer Migration

| System | Current Location | Blocked By | Action |
|--------|-----------------|-----------|--------|
| `Unit3D` + subtypes | `Units/Unit3D.cs`, `MeleeUnit3D.cs`, `RangedUnit3D.cs`, `DucklingUnit3D.cs` | UnitVisual must be complete | Deletes |
| `UnitHealth` / `UnitMovement` | `Units/Components/*.cs` | Unit3D components | Deletes |
| `Projectile3D` | `Projectiles/Projectile3D.cs` | ProjectileVisual must be complete | Deletes |
| `SimEventSignalEmitter` | `Simulation/SimEventSignalEmitter.cs` | EntityManager reads SimEventsEmitted directly | Deletes |

### Blocked by Session Layer Migration

| System | Current Location | Blocked By | Action |
|--------|-----------------|-----------|--------|
| `HostRunner` | `Multiplayer/Authority/HostRunner.cs` | HostSession implementation | Deletes |
| `ClientRunner` | `Multiplayer/Client/ClientRunner.cs` | ClientSession implementation | Deletes |
| `MatchSession` | `Multiplayer/Core/MatchSession.cs` | NetworkSession implementation | Deletes |
| `RequestValidator` | `Multiplayer/Authority/RequestValidator.cs` | CommandRouter implementation | Deletes |
| `NetworkIdRegistry` | `Multiplayer/Core/NetworkIdRegistry.cs` | IdentityMap implementation | Deletes |
| `IMatchRunner` | `Multiplayer/Core/IMatchRunner.cs` | Runner pattern retired | Deletes |
| `IMessageBroadcaster` | `Multiplayer/Core/IMessageBroadcaster.cs` | Broadcast pattern retired | Deletes |
| `HostEventBroadcaster` | `Multiplayer/Authority/HostEventBroadcaster.cs` | Absorbed into HostSession | Deletes |

### Blocked by Input Layer Migration

| System | Current Location | Blocked By | Action |
|--------|-----------------|-----------|--------|
| `SpellTargetingManager` (autoload) | `scripts/battle/ui/spell_targeting_manager.gd` | InputCollector handles spells | Deletes |
| `RedirectManager` (autoload) | `scripts/managers/redirect_manager.gd` | InputCollector handles redirects | Deletes |

### Retired by Decision (No Blocker)

| System | Current Location | Retired By | Action |
|--------|-----------------|-----------|--------|
| `BattleRNG` (autoload) | `scripts/multiplayer/rng/battle_rng.gd` | Decision #16 — sim uses `DeterministicRng` | Deletes |
| `rng_domain.gd` | `scripts/multiplayer/rng/rng_domain.gd` | Decision #16 — retired with `BattleRNG` | Deletes |

### Blocked by Full Migration (All Layers)

| System | Current Location | Blocked By | Action |
|--------|-----------------|-----------|--------|
| Capabilities (5 interfaces) | `Capabilities/*.cs` | Unit3D interfaces — delete with Unit3D | Deletes |
| Targeting system (17 files) | `Targeting/**/*.cs` | SimTargeting replaces; Unit3D targeting removed | Deletes |
| Hitbox system (6 files) | `Combat/Hitbox/*.cs` | SimProjectile + SimDamage replace; Projectile3D removed | Deletes |
| `SpatialGrid` (autoload) | `Systems/SpatialGrid.cs` | Review if still needed for View-layer queries | Deletes (pending review) |

### Already Deleted

| System | Former Location | Status |
|--------|----------------|--------|
| `BaseAbility.cs` | `Abilities/BaseAbility.cs` | Deleted — was dead code |
| `SlowOnHitAbility.cs` | `Abilities/SlowOnHitAbility.cs` | Deleted — was dead code |
| `IAbilityConfig.cs` | `Abilities/IAbilityConfig.cs` | Deleted — was dead code |
| `AuthorityProvider` dead signals | `authority_provider.gd` | Deleted — dead signal declarations |

---

## Resolved (Formerly Unresolved — Phase 1 Complete)

All Phase 1 decisions are settled. See `docs/architecture/decisions.md` #9–#16. Systems previously listed here have been assigned:

| System | Current Location | Assigned Layer | Decision |
|--------|-----------------|---------------|----------|
| `AudioManager` | `scripts/infrastructure/audio_manager.gd` (autoload) | Standalone service | Decision #10 |
| AI system (5 files) | `scripts/ai/*.gd` + `scripts/core/simple_ai.gd` | Input (peer) | Decision #12 |
| `BattleContext` | `scripts/application/battle_context.gd` (autoload) | Infrastructure (builds typed `BattleConfig` for Session) | Decision #13 |
| `EventContext` | `scripts/application/event_context.gd` (autoload) | Infrastructure (same pattern as BattleContext) | Decision #13 |
| `GameStateEvents` | `scripts/services/game_state_events.gd` (autoload) | Meta-game (kept for non-battle; revisit Phase 6) | Decision #15 |
| `BattleRNG` | `scripts/multiplayer/rng/battle_rng.gd` (autoload) | Delete queue (retired for gameplay; sim uses `DeterministicRng`) | Decision #16 |
| `rng_domain.gd` | `scripts/multiplayer/rng/rng_domain.gd` | Delete queue (retired with `BattleRNG`) | Decision #16 |
| `summoner.gd` (full) | `scripts/core/summoner.gd` | Splits: visual → View, deck/mana → Session init, input → Input | Decisions #9, #13 |
| `hand_ui.gd` (input parts) | `scripts/battle/ui/hand_ui.gd` | Splits: card rendering → View (HUD), drag gesture → Input | Decision #9 |

---

## Autoload Verification

Every autoload from `project.godot` mapped to its layer:

| Autoload Name | Script/Scene | Layer |
|---------------|-------------|-------|
| `ElementTypes` | `element_types.gd` | Cross-cutting |
| `Fonts` | `fonts.gd` | View |
| `Loc` | `localization_service.gd` | Cross-cutting |
| `BattleRNG` | `battle_rng.gd` | **Delete queue** (retired for gameplay RNG) |
| `NetworkState` | `network_state.gd` | Infrastructure |
| `CardCatalogCS` | `CardCatalogBridge.tscn` | Cross-cutting |
| `CardCatalog` | `card_catalog.gd` | Cross-cutting |
| `CardFactory` | `CardFactory.tscn` | Cross-cutting (pending) |
| `SummonerCatalogCS` | `SummonerCatalogBridge.tscn` | Cross-cutting |
| `SummonerCatalog` | `summoner_catalog.gd` | Cross-cutting |
| `TraitCatalogCS` | `TraitCatalogBridge.tscn` | Cross-cutting |
| `TraitCatalog` | `trait_catalog.gd` | Cross-cutting |
| `ProjectileCatalog` | `ProjectileCatalog.tscn` | Cross-cutting |
| `CosmeticsCatalog` | `cosmetics_catalog.gd` | Cross-cutting |
| `EmotesCatalog` | `emotes_catalog.gd` | Cross-cutting |
| `VFXManager` | `vfx_manager.gd` | View |
| `DamageSystem` | `DamageSystem.tscn` | **Delete queue** |
| `HitResolver` | `HitResolver.tscn` | **Delete queue** |
| `HPBarService` | `HPBarService.tscn` | View |
| `ProjectileService` | `ProjectileService.tscn` | **Delete queue** |
| `ProfileRepo` | `json_profile_repository.gd` | Infrastructure |
| `ProfileRepositoryCS` | `ProfileRepository.tscn` | Infrastructure |
| `AudioManager` | `audio_manager.gd` | Standalone service |
| `EconomyServiceCS` | `EconomyService.tscn` | Meta-game |
| `Economy` | `economy_service.gd` | Meta-game |
| `CardService` | `CardService.tscn` | Meta-game |
| `SummonerProgressionCS` | `SummonerProgressionService.tscn` | Meta-game |
| `SummonerProgression` | `summoner_progression_service.gd` | Meta-game |
| `SummonerSelection` | `SummonerSelectionService.tscn` | Meta-game |
| `DeckServiceCS` | `DeckService.tscn` | Meta-game |
| `Decks` | `deck_service.gd` | Meta-game |
| `RewardServiceCS` | `RewardService.tscn` | Meta-game |
| `RewardService` | `reward_service.gd` | Meta-game |
| `ItemServiceCS` | `ItemService.tscn` | Meta-game |
| `Items` | `item_service.gd` | Meta-game |
| `ShopServiceCS` | `ShopService.tscn` | Meta-game |
| `Shop` | `shop_service.gd` | Meta-game |
| `BillingCatalog` | `billing_catalog.gd` | Infrastructure |
| `PlatformBilling` | `platform_billing.gd` | Infrastructure |
| `CampaignServiceCS` | `CampaignService.tscn` | Meta-game |
| `Campaign` | `campaign_service.gd` | Meta-game |
| `BattleContext` | `battle_context.gd` | Infrastructure (builds `BattleConfig`) |
| `EventContext` | `event_context.gd` | Infrastructure |
| `NavigationContext` | `navigation_context.gd` | Infrastructure |
| `CapabilityManager` | `capability_manager.gd` | Meta-game |
| `GameStateEvents` | `game_state_events.gd` | Meta-game (kept for non-battle) |
| `EventSequencer` | `event_sequencer.gd` | Meta-game |
| `DialogueManager` | `dialogue_manager.gd` | Meta-game |
| `DevConsole` | `dev_console.gd` | Debug |
| `DebugSnapshots` | `debug_snapshots.gd` | Debug |
| `ModifierService` | `ModifierService.tscn` | **Delete queue** |
| `SceneManager` | `scene_manager.gd` | Infrastructure |
| `SceneCoordinator` | `scene_coordinator.gd` | Infrastructure |
| `RedirectManager` | `redirect_manager.gd` | **Delete queue** (after Input migration) |
| `PhysicsLayers` | `physics_layers.gd` | Cross-cutting |
| `SpellTargetingManager` | `spell_targeting_manager.gd` | **Delete queue** (after Input migration) |
| `DebugMenu` | `debug_menu.gd` | Debug |
| `SpatialGrid` | `SpatialGrid.tscn` | **Delete queue** (pending review) |
| `TargetingConfigRegistryCS` | `TargetingConfigRegistryCS.tscn` | **Delete queue** (Targeting system retired) |
| `LevelCapService` | `LevelCapService.tscn` | Meta-game |
| `UnitDebugService` | `UnitDebugService.cs` | Debug |
| `NakamaGameClient` | `NakamaGameClient.tscn` | Infrastructure |
| `RankingService` | `RankingService.tscn` | Infrastructure |
| `MatchReporter` | `MatchReporter.tscn` | Infrastructure |
| `MatchmakingService` | `MatchmakingService.tscn` | Infrastructure |
| `LeaderboardService` | `LeaderboardService.tscn` | Infrastructure |
| `ReconnectionHandler` | `ReconnectionHandler.tscn` | Session |
