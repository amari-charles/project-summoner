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

## Execute the card effect at the given position
## Note: This is called from Summoner which has scene tree access
func play(position: Vector2, team: Unit.Team, battlefield: Node) -> void:
	match card_type:
		CardType.SUMMON:
			_summon_unit(position, team, battlefield)
		CardType.SPELL:
			_cast_spell(position, team, battlefield)

## Execute the card effect at the given 3D position
## modifier_system: Optional ModifierSystem reference for more efficient access
## spawn_duration: If > 0, applies spawn reveal effect over this duration (for summon cards)
func play_3d(play_position: Vector3, team: UnitConstants.Team, battlefield: Node, modifier_system: Node = null, spawn_duration: float = 0.0) -> void:
	match card_type:
		CardType.SUMMON:
			_summon_unit_3d(play_position, team, battlefield, modifier_system, spawn_duration)
		CardType.SPELL:
			_cast_spell_3d(play_position, team, battlefield, modifier_system)

## Spawn unit(s) at the position
func _summon_unit(position: Vector2, team: Unit.Team, battlefield: Node) -> void:
	if unit_scene == null:
		push_error("Card '%s' has no unit_scene assigned!" % card_name)
		return

	for i: int in spawn_count:
		var unit: Unit = unit_scene.instantiate() as Unit
		if unit:
			unit.global_position = position + Vector2(i * 40, 0)  # Slight offset for multiple units
			unit.team = team
			battlefield.add_child(unit)

## Execute spell effect at the position
func _cast_spell(position: Vector2, team: Unit.Team, battlefield: Node) -> void:
	if spell_damage > 0:
		_apply_aoe_damage(position, team, battlefield)

## Apply AOE damage to enemies in range
func _apply_aoe_damage(position: Vector2, team: Unit.Team, battlefield: Node) -> void:
	var target_group: StringName = GroupIDs.enemy_units_for(team)
	var scene_tree: SceneTree = battlefield.get_tree()
	if scene_tree == null:
		return

	var enemies: Array[Node] = scene_tree.get_nodes_in_group(target_group)

	for enemy: Node in enemies:
		if enemy is Unit:
			var enemy_unit: Unit = enemy
			if enemy_unit.is_alive:
				var distance: float = enemy_unit.global_position.distance_to(position)
				if distance <= spell_radius:
					enemy_unit.take_damage(spell_damage)

	# Visual effect placeholder
	var explosion: ColorRect = ColorRect.new()
	explosion.size = Vector2(spell_radius * 2, spell_radius * 2)
	explosion.position = position - explosion.size / 2
	explosion.color = Color(1, 0.5, 0, 0.5)  # Orange translucent

	battlefield.add_child(explosion)
	await scene_tree.create_timer(0.5).timeout
	explosion.queue_free()

## Spawn unit(s) at the 3D position
## spawn_duration: If > 0, applies spawn reveal effect (ghost materialize animation)
func _summon_unit_3d(spawn_pos: Vector3, team: UnitConstants.Team, battlefield: Node, modifier_system: Node = null, spawn_duration: float = 0.0) -> void:
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
func _cast_spell_3d(cast_pos: Vector3, team: UnitConstants.Team, battlefield: Node, modifier_system: Node = null) -> void:
	# Get card definition WITH upgrade modifiers applied
	var card_def: Dictionary = get_effective_stats()

	# Use effective spell_damage and spell_radius from upgraded stats
	var effective_spell_damage: float = card_def.get("spell_damage", spell_damage)
	var effective_spell_radius: float = card_def.get("spell_radius", spell_radius)

	# Check for tactical command spells (Rally/Guard) - handle separately
	if card_def.has("command_type"):
		_cast_command_spell(cast_pos, team, battlefield, card_def)
		return

	# Get card categories from catalog
	var categories: Dictionary = {}
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
	var modifiers: Array = _get_modifiers_from_system("spell", categories, context, modifier_system)

	# Apply modifiers to spell damage (base is already upgrade-modified)
	var modified_spell_damage: float = _apply_spell_modifiers(effective_spell_damage, modifiers)

	# Check for instant spell VFX first (spawns immediately at click location)
	if not spell_vfx.is_empty():
		# Spawn VFX immediately with damage parameters
		# VFX will handle damage application at impact time
		VFXManager.play_effect(spell_vfx, cast_pos, {
			"radius": effective_spell_radius,
			"damage": modified_spell_damage,
			"team": team,
			"battlefield": battlefield
		})
	# If spell uses a projectile, spawn it instead of instant cast
	elif not projectile_id.is_empty():
		_spawn_spell_projectile(cast_pos, team, battlefield, modified_spell_damage)
	elif modified_spell_damage > 0:
		# Fallback to instant AOE damage (legacy behavior)
		_apply_aoe_damage_3d(cast_pos, team, battlefield, modified_spell_damage)

## Apply modifiers to spell damage
##
## Spells can be affected by two types of modifiers:
## - "attack_damage": Generic damage modifiers (affects both units and spells)
## - "spell_damage": Spell-specific modifiers (affects only spells)
##
## NOTE: Both keys are checked and summed. If a modifier has both keys,
## both values will be applied (this allows for future flexibility).
func _apply_spell_modifiers(base_damage: float, modifiers: Array) -> float:
	var damage: float = base_damage

	# Phase 1: Sum additive bonuses
	var add_bonus: float = 0.0
	for mod: Dictionary in modifiers:
		var empty_adds: Dictionary = {}
		var stat_adds: Dictionary = mod.get("stat_adds", empty_adds)
		add_bonus += stat_adds.get("attack_damage", 0.0)
		add_bonus += stat_adds.get("spell_damage", 0.0)

	damage += add_bonus

	# Phase 2: Apply multiplicative bonuses
	var mult_bonus: float = 0.0
	for mod: Dictionary in modifiers:
		var empty_mults: Dictionary = {}
		var stat_mults: Dictionary = mod.get("stat_mults", empty_mults)
		# Convert multipliers (1.1 → 0.1) and sum
		if stat_mults.has("attack_damage"):
			mult_bonus += stat_mults.attack_damage - 1.0
		if stat_mults.has("spell_damage"):
			mult_bonus += stat_mults.spell_damage - 1.0

	damage *= (1.0 + mult_bonus)

	return damage

## Spawn a spell projectile
func _spawn_spell_projectile(target_position: Vector3, team: UnitConstants.Team, battlefield: Node, damage: float = 0.0) -> void:
	# Use provided damage or fall back to spell_damage
	var final_damage: float = damage if damage > 0 else spell_damage

	# Find source (player or enemy base)
	var source: Node3D = _find_base_by_team(team, battlefield)
	if not source:
		push_warning("Card: Could not find source base for spell projectile")
		# Fallback to instant damage
		_apply_aoe_damage_3d(target_position, team, battlefield, final_damage)
		return

	# Spawn projectile using ProjectileManager
	var projectile: Node = ProjectileManager.spawn_projectile(
		projectile_id,
		source,
		null,  # No target unit, targeting a position
		final_damage,
		"spell",
		{
			"start_position": source.global_position,
			"target_position": target_position
		}
	)

	if not projectile:
		push_error("Card: Failed to spawn spell projectile '%s' for card '%s'. Check projectile_id in catalog or ProjectileManager registration." % [projectile_id, card_name])
		assert(false, "Spell projectile must spawn successfully!")

## Cast tactical command spell (Rally/Guard)
func _cast_command_spell(target_pos: Vector3, team: UnitConstants.Team, battlefield: Node, card_def: Dictionary) -> void:
	var command_type: String = card_def.get("command_type", "")
	var selection_radius: float = card_def.get("selection_radius", 8.0)
	print("Card: _cast_command_spell - card_name='%s', command_type='%s', catalog_id='%s'" % [card_name, command_type, catalog_id])

	# Select all friendly units in radius
	var scene_tree: SceneTree = battlefield.get_tree()
	if not scene_tree:
		return

	var all_units: Array[Node] = scene_tree.get_nodes_in_group(GroupIDs.UNITS)
	var selected_units: Array[Node3D] = []

	for node: Node in all_units:
		if not node is Node3D:
			continue

		var unit: Node3D = node as Node3D

		# Get properties using get() for C#/GDScript interop (C# uses PascalCase)
		var unit_team: Variant = unit.get("Team")
		var unit_alive: Variant = unit.get("IsAlive")

		# Skip if missing required properties
		if unit_team == null or unit_alive == null:
			continue

		# Only select friendly units that are alive
		if int(unit_team) != int(team) or not unit_alive:
			continue

		# Check if unit is within selection radius
		var distance: float = target_pos.distance_to(unit.global_position)
		if distance <= selection_radius:
			selected_units.append(unit)

	if selected_units.is_empty():
		# Failed cast: No units in range
		print("Card: No units in radius for command spell '%s'" % card_name)
		_spawn_failed_cast_vfx(target_pos)
		return

	# Successful cast: Execute command and show VFX
	match command_type:
		"rally":
			# Check SpellTargetingManager singleton for rally_destination from two-stage targeting
			# If available, use that as the rally point; otherwise use circle center
			var rally_target: Vector3 = target_pos
			var rally_dest: Vector3 = SpellTargetingManager.get_rally_destination()
			if rally_dest != Vector3.ZERO:
				rally_target = rally_dest
				print("Card: Using rally_destination: %s (circle was at %s)" % [rally_target, target_pos])
				SpellTargetingManager.clear_rally_destination()
			else:
				print("Card: No rally_destination found, using circle center: %s" % target_pos)

			_apply_rally_command(selected_units, rally_target, card_def)
			_spawn_rally_vfx(rally_target)
		"guard":
			# Check SpellTargetingManager singleton for guard formation position from two-stage targeting
			var guard_position: Vector3 = target_pos
			var guard_dest: Vector3 = SpellTargetingManager.get_rally_destination()
			if guard_dest != Vector3.ZERO:
				guard_position = guard_dest
				print("Card: Using guard formation position: %s (circle was at %s)" % [guard_position, target_pos])
				SpellTargetingManager.clear_rally_destination()
			else:
				print("Card: No guard destination found, using circle center: %s" % target_pos)

			_apply_guard_command(selected_units, guard_position, card_def)
			_spawn_guard_vfx(selected_units)
		"charge":
			# Check SpellTargetingManager singleton for charge_destination from two-stage targeting
			var charge_destination: Vector3 = target_pos
			var charge_dest: Vector3 = SpellTargetingManager.get_rally_destination()
			if charge_dest != Vector3.ZERO:
				charge_destination = charge_dest
				print("Card: Using charge destination: %s (circle was at %s)" % [charge_destination, target_pos])
				SpellTargetingManager.clear_rally_destination()
			else:
				print("Card: No charge destination found, using circle center: %s" % target_pos)

			_apply_charge_command(selected_units, charge_destination, team, card_def)
			_spawn_charge_vfx(charge_destination)
		_:
			push_warning("Card: Unknown command_type '%s' for card '%s'" % [command_type, card_name])

	print("Card: Applied %s command to %d units" % [command_type, selected_units.size()])

## Apply Rally command to selected units
func _apply_rally_command(units: Array[Node3D], rally_point: Vector3, _card_def: Dictionary) -> void:
	for unit: Node3D in units:
		unit.rally_point = rally_point
		unit.rally_mode = true
		print("Card: Unit %s rallied to %s" % [unit.name, rally_point])

## Apply Guard command to selected units
func _apply_guard_command(units: Array[Node3D], center: Vector3, _card_def: Dictionary) -> void:
	# Calculate formation positions for guard mode
	_calculate_formation_positions(units, center)
	print("Card: Formed guard formation with %d units at %s" % [units.size(), center])

## Calculate formation positions for guard command (ported from Unit3D)
func _calculate_formation_positions(units: Array[Node3D], center: Vector3) -> void:
	if units.is_empty():
		return

	# Separate melee and ranged units
	var melee_units: Array[Node3D] = []
	var ranged_units: Array[Node3D] = []
	for unit: Node3D in units:
		if "unit_type" in unit and unit.unit_type == UnitConstants.UnitType.RANGED:
			ranged_units.append(unit)
		else:
			melee_units.append(unit)

	# Formation constants
	const BASE_RADIUS: float = 2.0
	const UNIT_SPACING: float = 1.5
	const GUARD_DURATION: float = 25.0

	# Calculate front arc (melee) and back arc (ranged)
	var front_radius: float = BASE_RADIUS + (melee_units.size() * UNIT_SPACING / PI)
	var back_radius: float = front_radius + 2.0

	# Position melee in front arc (180 degrees facing forward)
	for i: int in melee_units.size():
		var angle: float = PI + (float(i) / max(melee_units.size() - 1, 1)) * PI - PI / 2
		var pos: Vector3 = center + Vector3(cos(angle) * front_radius, 0, sin(angle) * front_radius)
		var unit: Node3D = melee_units[i]
		if "formation_position" in unit:
			unit.formation_position = pos
		if "guard_mode" in unit:
			unit.guard_mode = true
		if "guard_timer" in unit:
			unit.guard_timer = GUARD_DURATION

	# Position ranged in back arc
	for i: int in ranged_units.size():
		var angle: float = PI + (float(i) / max(ranged_units.size() - 1, 1)) * PI - PI / 2
		var pos: Vector3 = center + Vector3(cos(angle) * back_radius, 0, sin(angle) * back_radius)
		var unit: Node3D = ranged_units[i]
		if "formation_position" in unit:
			unit.formation_position = pos
		if "guard_mode" in unit:
			unit.guard_mode = true
		if "guard_timer" in unit:
			unit.guard_timer = GUARD_DURATION

## Apply Charge command to selected units (focus-fire on closest enemy)
func _apply_charge_command(units: Array[Node3D], charge_dest: Vector3, caster_team: UnitConstants.Team, _card_def: Dictionary) -> void:
	if units.is_empty():
		return

	# Find the closest enemy to the charge destination (units, bases, or structures)
	# Use INF radius so Charge can target any enemy on the entire battlefield
	# This differs from regular redirect which only affects nearby enemies
	var closest_enemy: Node3D = RedirectManager.find_nearest_enemy(
		charge_dest,
		caster_team,
		INF
	)

	if not closest_enemy:
		return

	# Set forced target for all selected units
	var charge_duration: float = 30.0  # Focus on this target for 30 seconds
	for unit: Node3D in units:
		unit.forced_target = closest_enemy
		unit.forced_target_timer = charge_duration
		# Also store original point for fallback targeting
		unit.original_redirect_point = charge_dest

## Find the base for the given team
func _find_base_by_team(team: UnitConstants.Team, battlefield: Node) -> Node3D:
	var scene_tree: SceneTree = battlefield.get_tree()
	if not scene_tree:
		return null

	# Try to find base in the scene
	var bases: Array[Node] = scene_tree.get_nodes_in_group(GroupIDs.BASES)
	for base: Node in bases:
		if "team" in base:
			var base_team: Variant = base.get("team")
			if base_team == team:
				return base as Node3D

	# Fallback: just return battlefield root if no base found
	return battlefield as Node3D

## Apply AOE damage to enemies in 3D range
func _apply_aoe_damage_3d(aoe_center: Vector3, team: UnitConstants.Team, battlefield: Node, damage: float = 0.0) -> void:
	# Use provided damage or fall back to spell_damage
	var final_damage: float = damage if damage > 0 else spell_damage

	var target_group: StringName = GroupIDs.enemy_units_for(team)
	var scene_tree: SceneTree = battlefield.get_tree()
	if scene_tree == null:
		return

	var enemies: Array[Node] = scene_tree.get_nodes_in_group(target_group)

	for enemy: Node in enemies:
		if "IsAlive" in enemy and "Team" in enemy:
			var enemy_unit: Node3D = enemy
			if enemy_unit.get("IsAlive"):
				var distance: float = enemy_unit.global_position.distance_to(aoe_center)
				if distance <= spell_radius:
					enemy_unit.TakeDamage(final_damage)

## Helper to safely access ModifierSystem
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

## =============================================================================
## VFX HELPERS (Placeholder visuals for Rally/Guard spells)
## TODO: Replace with proper VFX scenes once art assets are ready
## =============================================================================

## Spawn failed cast VFX (fizzle effect)
func _spawn_failed_cast_vfx(fail_pos: Vector3) -> void:
	# Try to use VFXManager if available
	if VFXManager and VFXManager.has_effect(VFXIDs.SPELL_FIZZLE):
		VFXManager.play_effect(VFXIDs.SPELL_FIZZLE, fail_pos)
		return

	# Fallback: Simple procedural fizzle effect
	_spawn_placeholder_fizzle(fail_pos)

## Spawn Rally VFX (selection circle + rally point marker)
func _spawn_rally_vfx(rally_point: Vector3) -> void:
	# Fixed visual marker radius (not gameplay-related, just for visual feedback)
	var visual_radius: float = 2.0

	# Try to use VFXManager if available
	if VFXManager and VFXManager.has_effect(VFXIDs.RALLY_CIRCLE):
		VFXManager.play_effect(VFXIDs.RALLY_CIRCLE, rally_point, {"radius": visual_radius})
		return

	# Fallback: Simple procedural marker
	_spawn_placeholder_circle(rally_point, visual_radius, Color(0.2, 0.5, 1.0, 0.6))

## Spawn Guard VFX (formation position markers)
func _spawn_guard_vfx(units: Array[Node3D]) -> void:
	# Try to use VFXManager if available
	if VFXManager and VFXManager.has_effect(VFXIDs.GUARD_MARKER):
		for unit: Node3D in units:
			VFXManager.play_effect(VFXIDs.GUARD_MARKER, unit.formation_position)
		return

	# Fallback: Simple procedural markers
	for unit: Node3D in units:
		_spawn_placeholder_marker(unit.formation_position, Color(0.8, 0.3, 0.2, 0.6))

## Spawn Charge VFX (attack target marker)
func _spawn_charge_vfx(charge_point: Vector3) -> void:
	# Fixed visual marker radius
	var visual_radius: float = 2.0

	# Try to use VFXManager if available
	if VFXManager and VFXManager.has_effect(VFXIDs.CHARGE_MARKER):
		VFXManager.play_effect(VFXIDs.CHARGE_MARKER, charge_point, {"radius": visual_radius})
		return

	# Fallback: Simple procedural marker (red/orange for aggressive command)
	_spawn_placeholder_circle(charge_point, visual_radius, Color(1.0, 0.4, 0.2, 0.7))

## Spawn a simple fizzle effect using procedural geometry
func _spawn_placeholder_fizzle(fizzle_pos: Vector3) -> void:
	var main_loop: MainLoop = Engine.get_main_loop()
	if not main_loop or not main_loop is SceneTree:
		return

	var scene_tree: SceneTree = main_loop
	var root: Window = scene_tree.root
	if not root:
		return

	# Create small sphere mesh for fizzle
	var mesh_instance: MeshInstance3D = MeshInstance3D.new()
	var sphere: SphereMesh = SphereMesh.new()
	sphere.radius = 0.3
	sphere.height = 0.6
	mesh_instance.mesh = sphere

	# Simple unshaded material
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = Color(0.6, 0.4, 0.6, 0.8)
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mesh_instance.material_override = material

	root.add_child(mesh_instance)
	mesh_instance.global_position = fizzle_pos

	# Auto-cleanup after 0.5 seconds
	var timer: SceneTreeTimer = scene_tree.create_timer(0.5)
	timer.timeout.connect(func() -> void:
		if is_instance_valid(mesh_instance):
			mesh_instance.queue_free()
	)

## Spawn a simple circle marker using procedural geometry
func _spawn_placeholder_circle(circle_pos: Vector3, radius: float, color: Color) -> void:
	var main_loop: MainLoop = Engine.get_main_loop()
	if not main_loop or not main_loop is SceneTree:
		return

	var scene_tree: SceneTree = main_loop
	var root: Window = scene_tree.root
	if not root:
		return

	# Create cylinder mesh for circle outline
	var mesh_instance: MeshInstance3D = MeshInstance3D.new()
	var cylinder: CylinderMesh = CylinderMesh.new()
	cylinder.top_radius = radius
	cylinder.bottom_radius = radius
	cylinder.height = 0.1
	mesh_instance.mesh = cylinder

	# Simple unshaded material
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = color
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mesh_instance.material_override = material

	root.add_child(mesh_instance)
	mesh_instance.global_position = circle_pos

	# Auto-cleanup after 1.5 seconds
	var timer: SceneTreeTimer = scene_tree.create_timer(1.5)
	timer.timeout.connect(func() -> void:
		if is_instance_valid(mesh_instance):
			mesh_instance.queue_free()
	)

## Spawn a simple marker using procedural geometry
func _spawn_placeholder_marker(position: Vector3, color: Color) -> void:
	var main_loop: MainLoop = Engine.get_main_loop()
	if not main_loop or not main_loop is SceneTree:
		return

	var scene_tree: SceneTree = main_loop
	var root: Window = scene_tree.root
	if not root:
		return

	# Create box mesh for marker
	var mesh_instance: MeshInstance3D = MeshInstance3D.new()
	var box: BoxMesh = BoxMesh.new()
	box.size = Vector3(0.5, 0.2, 0.5)
	mesh_instance.mesh = box

	# Simple unshaded material
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = color
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mesh_instance.material_override = material

	root.add_child(mesh_instance)
	mesh_instance.global_position = position

	# Auto-cleanup after 1.0 seconds
	var timer: SceneTreeTimer = scene_tree.create_timer(1.0)
	timer.timeout.connect(func() -> void:
		if is_instance_valid(mesh_instance):
			mesh_instance.queue_free()
	)
