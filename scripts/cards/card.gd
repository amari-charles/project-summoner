extends Resource
class_name Card

## Card executor - handles playing cards (summons, spells)
## Data is stored in CardConfig; this class handles execution only
##
## Architecture:
## - CardConfig: Pure data (catalog_id, mana_cost, formation_spacing, etc.)
## - Card: Executor with config reference and runtime state

## Preload CardConfig to avoid class loading order issues
const CardConfigClass = preload("res://scripts/cards/card_config.gd")

enum CardType { SUMMON, SPELL }

## =============================================================================
## CONFIGURATION
## =============================================================================

## Card configuration data (stats, costs, formation, etc.)
var config: Resource = null  # CardConfig instance (uses Resource to avoid load order issues)

## =============================================================================
## INSTANCE STATE (runtime, not config)
## =============================================================================

## Instance tracking (for progression system - unique per card in collection)
var instance_id: String = ""

## C# spell effect delegation - if set, spell execution delegates to CardFactory
## This is set by CardCatalog when creating spell cards with C# effects available
var _csharp_spell_id: String = ""

## C# summon execution delegation - if set, summon execution delegates to CardFactory
## This is set by CardCatalog when creating summon cards
var _csharp_summon_id: String = ""

## Event sequence stat overrides (set by EventSequencer before spawning)
var custom_stat_overrides: Dictionary = {}

## =============================================================================
## PROPERTY ACCESSORS (delegate to config for backward compatibility)
## =============================================================================

var catalog_id: String:
	get: return config.catalog_id if config else ""

var card_name: String:
	get: return config.card_name if config else "Unknown Card"

var card_type: int:
	get: return config.card_type if config else CardType.SUMMON

var description: String:
	get: return config.description if config else ""

var mana_cost: int:
	get: return config.mana_cost if config else 1

var cooldown: float:
	get: return config.cooldown if config else 2.0

var summon_time: float:
	get: return config.summon_time if config else 1.0

var unit_scene: PackedScene:
	get: return config.unit_scene if config else null

var spawn_count: int:
	get: return config.spawn_count if config else 1

var spell_damage: float:
	get: return config.spell_damage if config else 0.0

var spell_radius: float:
	get: return config.spell_radius if config else 0.0

var spell_duration: float:
	get: return config.spell_duration if config else 0.0

var projectile_id: String:
	get: return config.projectile_id if config else ""

var spell_vfx: String:
	get: return config.spell_vfx if config else ""

var card_icon: Texture2D:
	get: return config.card_icon if config else null

## Formation config (per-card customizable)
var formation_spacing: float:
	get: return config.formation_spacing if config else 1.8

var formation_row_offset: float:
	get: return config.formation_row_offset if config else 0.5

## =============================================================================
## FORMATION CONSTANTS (defaults for static method and tests)
## =============================================================================

const FORMATION_TWO_ROW_MAX: int = 20  ## Max units for 2-row formation
const FORMATION_LARGE_ROW_DENSITY: float = 3.0  ## Target units per row for 20+ swarms
const DEFAULT_FORMATION_SPACING: float = 1.8  ## Default spacing for static method
const DEFAULT_FORMATION_ROW_OFFSET: float = 0.5  ## Default row offset for static method


## Generate formation offset for staggered row spawning
## Uses per-card formation_spacing and formation_row_offset from config
func get_formation_offset(unit_index: int, unit_count: int) -> Vector3:
	if unit_count <= 1:
		return Vector3.ZERO

	var spacing: float = formation_spacing
	var row_offset: float = formation_row_offset

	# Calculate grid dimensions - prefer 2 rows for army-like formations
	var rows: int = 2 if unit_count <= FORMATION_TWO_ROW_MAX else ceili(sqrt(float(unit_count) / FORMATION_LARGE_ROW_DENSITY))
	var cols: int = ceili(float(unit_count) / float(rows))

	var row: int = unit_index / cols
	var col: int = unit_index % cols
	var units_in_row: int = mini(cols, unit_count - row * cols)

	# Stagger offset for alternating rows (brick pattern)
	var stagger: float = row_offset * spacing if row % 2 == 1 else 0.0

	# X axis = row depth
	var formation_depth: float = (rows - 1) * spacing
	var x_offset: float = row * spacing - formation_depth / 2.0

	# Z axis = column spread
	var row_width: float = (units_in_row - 1) * spacing
	var z_offset: float = col * spacing - row_width / 2.0 + stagger

	return Vector3(x_offset, 0, z_offset)


## Static helper for tests and preview (uses default config values)
## Uses class-level constants DEFAULT_FORMATION_SPACING and DEFAULT_FORMATION_ROW_OFFSET
static func generate_formation_offset(unit_index: int, unit_count: int) -> Vector3:
	if unit_count <= 1:
		return Vector3.ZERO

	var rows: int = 2 if unit_count <= FORMATION_TWO_ROW_MAX else ceili(sqrt(float(unit_count) / FORMATION_LARGE_ROW_DENSITY))
	var cols: int = ceili(float(unit_count) / float(rows))

	var row: int = unit_index / cols
	var col: int = unit_index % cols
	var units_in_row: int = mini(cols, unit_count - row * cols)

	var stagger: float = DEFAULT_FORMATION_ROW_OFFSET * DEFAULT_FORMATION_SPACING if row % 2 == 1 else 0.0

	var formation_depth: float = (rows - 1) * DEFAULT_FORMATION_SPACING
	var x_offset: float = row * DEFAULT_FORMATION_SPACING - formation_depth / 2.0

	var row_width: float = (units_in_row - 1) * DEFAULT_FORMATION_SPACING
	var z_offset: float = col * DEFAULT_FORMATION_SPACING - row_width / 2.0 + stagger

	return Vector3(x_offset, 0, z_offset)

## Validate if this card can be played
func can_play(current_mana: int) -> bool:
	return current_mana >= mana_cost

## Get effective stats with card upgrades applied
## Returns a Dictionary with the same structure as CardCatalog.get_card() but with
## upgrade modifiers (from CardProgressionService) applied multiplicatively.
func get_effective_stats() -> Dictionary:
	# Get base stats from catalog
	var base_stats: Dictionary = CardCatalog.get_card(catalog_id).duplicate(true)

	# If no instance_id, return base stats (enemy cards, test cards)
	if instance_id.is_empty():
		return base_stats

	# Get upgrade stat modifiers from CardProgressionService
	var progression_node: Node = _get_autoload_node("/root/CardProgression")
	if not progression_node:
		return base_stats

	var modifiers_result: Variant = progression_node.call("get_upgrade_stat_modifiers", instance_id)
	if not modifiers_result is Dictionary:
		return base_stats

	var modifiers: Dictionary = modifiers_result
	if modifiers.is_empty():
		return base_stats

	# Apply modifiers multiplicatively
	for stat_key: Variant in modifiers:
		if stat_key is String:
			var multiplier: float = modifiers[stat_key]
			if base_stats.has(stat_key):
				var base_val: Variant = base_stats[stat_key]
				if base_val is float:
					base_stats[stat_key] = base_val * multiplier
				elif base_val is int:
					base_stats[stat_key] = int(base_val * multiplier)

	return base_stats

## Helper to get autoload nodes (Card is a Resource, not a Node)
func _get_autoload_node(path: String) -> Node:
	var main_loop: MainLoop = Engine.get_main_loop()
	if main_loop is SceneTree:
		var tree: SceneTree = main_loop
		if tree and tree.root:
			return tree.root.get_node_or_null(path)
	return null

## Check if this card needs click-targeting (Rally/Guard with command_type)
func needs_click_targeting() -> bool:
	if card_type != CardType.SPELL:
		return false

	var card_def: Dictionary = CardCatalog.get_card(catalog_id)
	return card_def.has("command_type")

## Execute the card effect at the given 3D position
## modifier_system: Optional ModifierSystem reference for more efficient access
## spawn_duration: If > 0, applies spawn reveal effect over this duration (for summon cards)
func play_3d(play_position: Vector3, team: UnitConstants.Team, battlefield: Node, modifier_system: Node = null, spawn_duration: float = 0.0) -> void:
	match card_type:
		CardType.SUMMON:
			_summon_unit_3d(play_position, team, battlefield, modifier_system, spawn_duration)
		CardType.SPELL:
			_cast_spell_3d(play_position, team, battlefield, modifier_system)

## Spawn unit(s) at the 3D position
## spawn_duration: If > 0, applies spawn reveal effect (ghost materialize animation)
func _summon_unit_3d(spawn_pos: Vector3, team: UnitConstants.Team, battlefield: Node, modifier_system: Node = null, spawn_duration: float = 0.0) -> void:
	# Delegate to C# CardFactory if summon ID is set
	if not _csharp_summon_id.is_empty():
		_execute_csharp_summon(spawn_pos, team, battlefield, modifier_system, spawn_duration)
		return

	if unit_scene == null:
		push_error("Card '%s' has no unit_scene assigned! Fix card resource or catalog definition." % card_name)
		assert(false, "Summon card must have unit_scene!")
		return

	var gameplay_layer: Node = battlefield
	if battlefield.has_method("get_gameplay_layer"):
		gameplay_layer = battlefield.call("get_gameplay_layer")

	# Get card categories from catalog
	var categories: Dictionary = {}
	if not catalog_id.is_empty() and CardCatalog:
		var card_def: Dictionary = CardCatalog.get_card(catalog_id)
		if not card_def.is_empty():
			var empty_dict: Dictionary = {}
			categories = card_def.get("categories", empty_dict)

	# Build context for modifier system
	var context: Dictionary = {
		"card_name": card_name,
		"team": team,
		"card_instance_id": instance_id  # For instance-scoped modifier filtering
	}

	# Get modifiers from ModifierSystem
	var modifiers: Array = _get_modifiers_from_system("unit", categories, context, modifier_system)

	# Card data for apply_modifiers
	var card_data: Dictionary = {
		"card_name": card_name,
		"mana_cost": mana_cost
	}

	for i: int in spawn_count:
		var unit: Node3D = unit_scene.instantiate() as Node3D
		if unit:
			unit.set("Team", int(team))  # Use set() for C# property

			# Apply stats from card catalog WITH upgrade modifiers applied
			# get_effective_stats() returns catalog data with upgrade bonuses
			var catalog_data: Dictionary = get_effective_stats()
			assert(not catalog_data.is_empty(), "Card catalog data must exist for catalog_id: '%s'" % catalog_id)

			# Apply custom stat overrides from EventSequencer (if set)
			if not custom_stat_overrides.is_empty():
				for stat_key: String in custom_stat_overrides.keys():
					catalog_data[stat_key] = custom_stat_overrides[stat_key]
					print("Card: Applied custom override %s = %s for '%s'" % [stat_key, custom_stat_overrides[stat_key], card_name])

			# Apply stats from catalog - MUST have all required stats (NO FALLBACKS!)
			assert(catalog_data.has("max_hp"), "Card '%s' missing max_hp in catalog!" % catalog_id)
			assert(catalog_data.has("attack_damage"), "Card '%s' missing attack_damage in catalog!" % catalog_id)
			assert(catalog_data.has("attack_speed"), "Card '%s' missing attack_speed in catalog!" % catalog_id)
			assert(catalog_data.has("move_speed"), "Card '%s' missing move_speed in catalog!" % catalog_id)

			unit.set("MaxHp", catalog_data.max_hp)
			unit.set("AttackDamage", catalog_data.attack_damage)
			unit.set("AttackSpeed", catalog_data.attack_speed)
			unit.set("MoveSpeed", catalog_data.move_speed)

			# Attack range is optional (different defaults for melee vs ranged)
			if catalog_data.has("attack_range"):
				unit.set("AttackRange", catalog_data.attack_range)

			# Apply scale_multiplier override if present (not a catalog stat)
			if custom_stat_overrides.has("scale_multiplier"):
				var multiplier: float = custom_stat_overrides["scale_multiplier"]
				unit.scale = Vector3.ONE * multiplier

			# Add to tree FIRST so _Ready() runs and sets _base* values
			gameplay_layer.add_child(unit)

			# Initialize with modifiers AFTER add_child (requires _Ready to have run)
			unit.InitializeWithModifiers(modifiers, card_data)  # C# uses PascalCase

			# Calculate spawn offset - staggered row formation for multiple units
			# Uses per-card formation_spacing and formation_row_offset from config
			var offset: Vector3 = get_formation_offset(i, spawn_count)

			# Find a safe spawn position that doesn't overlap with existing units
			# Pass the current unit to exclude it from collision checks (it was just added to UNITS group
			# at position 0,0,0 and hasn't been moved yet)
			var desired_pos: Vector3 = spawn_pos + offset
			var collision_rad: float = unit.get("CollisionRadius") if unit.get("CollisionRadius") != null else 0.5
			var safe_pos: Vector3 = BattlefieldConstants.find_safe_spawn_position(
				desired_pos, gameplay_layer.get_tree(), collision_rad, unit
			)
			unit.global_position = safe_pos

			# Update SpatialGrid immediately with correct position
			# Without this, unit stays registered at (0,0,0) until it activates and runs _PhysicsProcess
			# This is critical for swarm spawns where units need accurate positions for steering/targeting
			if SpatialGrid:
				SpatialGrid.update_unit_position(unit)

			# Preserve flight altitude for flying units (spawn position is ground-level)
			var movement_layer: int = unit.get("MovementLayer") if unit.get("MovementLayer") != null else 0
			if movement_layer == UnitConstants.MovementLayer.AIR:
				var flight_alt: float = unit.get("FlightAltitude") if unit.get("FlightAltitude") != null else 2.5
				unit.global_position.y = flight_alt

			# Start spawn reveal effect if duration specified (ghost materialize animation)
			var has_spawn_animation: bool = spawn_duration > 0.0 and unit.has_method("start_spawn_reveal")
			if has_spawn_animation:
				unit.start_spawn_reveal(spawn_duration)

			# Activate unit if already in battle phase and no spawn animation
			# If spawn animation is playing, Unit3D.CompleteSpawnReveal() will activate when done
			if not has_spawn_animation:
				var game_controller: Node = gameplay_layer.get_tree().current_scene
				if game_controller and "current_phase" in game_controller and game_controller.current_phase == GameController3D.BattlePhase.BATTLE:
					unit.Activate()
		else:
			push_error("Card._summon_unit_3d: Failed to instantiate unit from scene for card '%s'! Check unit_scene validity." % card_name)
			assert(false, "Unit must instantiate successfully!")

## Execute spell effect at the 3D position
## All spells delegate to C# CardFactory for execution
func _cast_spell_3d(cast_pos: Vector3, team: UnitConstants.Team, battlefield: Node, modifier_system: Node = null) -> void:
	if _csharp_spell_id.is_empty():
		push_error("Card: Spell '%s' has no C# effect attached! All spells must use C# CardFactory." % card_name)
		return

	_execute_csharp_spell(cast_pos, team, battlefield, modifier_system)


## Execute spell via C# CardFactory
func _execute_csharp_spell(cast_pos: Vector3, team: UnitConstants.Team, battlefield: Node, modifier_system: Node = null) -> void:
	var factory: Node = _get_card_factory()
	if not factory:
		push_error("Card: CardFactory not available! C# may not be loaded. Spell '%s' cannot be cast." % _csharp_spell_id)
		return

	factory.execute_spell(_csharp_spell_id, cast_pos, int(team), battlefield, modifier_system, instance_id)


## Execute summon via C# CardFactory
func _execute_csharp_summon(spawn_pos: Vector3, team: UnitConstants.Team, battlefield: Node, modifier_system: Node = null, spawn_duration: float = 0.0) -> void:
	var factory: Node = _get_card_factory()
	if not factory:
		push_error("Card: CardFactory not available! C# may not be loaded. Summon '%s' cannot spawn." % _csharp_summon_id)
		return

	# Get card definition from catalog
	var card_def: Dictionary = CardCatalog.get_card(catalog_id)
	if card_def.is_empty():
		push_error("Card: Cannot get card definition for '%s'" % catalog_id)
		return

	# Get effective stats (with upgrades applied)
	var effective_stats: Dictionary = get_effective_stats()

	# Execute summon via factory
	factory.execute_summon(
		_csharp_summon_id,
		spawn_pos,
		int(team),
		battlefield,
		card_def,
		effective_stats,
		custom_stat_overrides,
		modifier_system,
		instance_id,
		spawn_duration
	)


## Get CardFactory autoload safely
func _get_card_factory() -> Node:
	var main_loop: MainLoop = Engine.get_main_loop()
	if not main_loop or not main_loop is SceneTree:
		return null

	var tree: SceneTree = main_loop
	if not tree.root:
		return null

	return tree.root.get_node_or_null("/root/CardFactory")


## Helper to safely access ModifierSystem (used by summons)
## Prefers passed reference, falls back to autoload lookup if not provided
func _get_modifiers_from_system(target_type: String, categories: Dictionary, context: Dictionary, modifier_system: Node = null) -> Array:
	var modifiers: Array = []

	# Use passed reference if available (preferred method)
	if modifier_system:
		if modifier_system.has_method("get_modifiers_for"):
			modifiers = modifier_system.call("get_modifiers_for", target_type, categories, context)
		else:
			push_error("Card: Passed modifier_system missing get_modifiers_for method")
		return modifiers

	# Fallback: Try to access ModifierSystem autoload (legacy compatibility)
	# Check if CardCatalog exists (another autoload) - if it does, ModifierSystem should too
	if not CardCatalog:
		push_warning("Card: ModifierSystem not passed and CardCatalog autoload not found, modifiers unavailable")
		return modifiers

	# Access ModifierSystem via root node
	var main_loop: MainLoop = Engine.get_main_loop()
	if not main_loop:
		push_warning("Card: ModifierSystem not passed and failed to access main loop, modifiers unavailable")
		return modifiers

	if not main_loop is SceneTree:
		push_warning("Card: Main loop is not SceneTree, modifiers unavailable")
		return modifiers

	var scene_tree: SceneTree = main_loop
	var root: Window = scene_tree.root
	if not root:
		push_warning("Card: ModifierSystem not passed and failed to access scene tree root, modifiers unavailable")
		return modifiers

	modifier_system = root.get_node_or_null("ModifierSystem")
	if not modifier_system:
		push_warning("Card: ModifierSystem not passed and autoload not found, modifiers unavailable")
		return modifiers

	if not modifier_system.has_method("get_modifiers_for"):
		push_error("Card: ModifierSystem missing get_modifiers_for method")
		return modifiers

	modifiers = modifier_system.call("get_modifiers_for", target_type, categories, context)
	return modifiers
