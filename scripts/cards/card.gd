extends Resource
class_name Card

## Represents a single-use card (unit summon or spell)
## Cards are drawn from the player's deck and used once per match

enum CardType { SUMMON, SPELL }

## Card identity
@export var catalog_id: String = ""  # ID in CardCatalog for looking up full data
@export var card_name: String = "Unknown Card"
@export var card_type: CardType = CardType.SUMMON
@export var description: String = ""

## Cost and gameplay
@export var mana_cost: int = 1
@export var cooldown: float = 2.0  # Seconds before another card can be played

## Summon-specific data
@export var unit_scene: PackedScene = null  # The unit to spawn (if CardType.SUMMON)
@export var spawn_count: int = 1  # How many units to spawn

## Spell-specific data
@export var spell_damage: float = 0.0
@export var spell_radius: float = 0.0
@export var spell_duration: float = 0.0
@export var projectile_id: String = ""  # If set, spell spawns a projectile instead of instant cast
@export var spell_vfx: String = ""  # VFX ID to spawn when spell hits (for instant AOE spells)

## Visual
@export var card_icon: Texture2D = null

## Validate if this card can be played
func can_play(current_mana: int) -> bool:
	return current_mana >= mana_cost

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
func play_3d(play_position: Vector3, team: Unit3D.Team, battlefield: Node, modifier_system: Node = null) -> void:
	match card_type:
		CardType.SUMMON:
			_summon_unit_3d(play_position, team, battlefield, modifier_system)
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
	var target_group: String = "enemy_units" if team == Unit.Team.PLAYER else "player_units"
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
func _summon_unit_3d(spawn_pos: Vector3, team: Unit3D.Team, battlefield: Node, modifier_system: Node = null) -> void:
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
		"team": team
	}

	# Get modifiers from ModifierSystem
	var modifiers: Array = _get_modifiers_from_system("unit", categories, context, modifier_system)

	# Card data for apply_modifiers
	var card_data: Dictionary = {
		"card_name": card_name,
		"mana_cost": mana_cost
	}

	for i: int in spawn_count:
		var unit: Unit3D = unit_scene.instantiate() as Unit3D
		if unit:
			unit.team = team

			# Apply stats from card catalog (single source of truth)
			# Get catalog data - MUST exist
			var catalog_data: Dictionary = CardCatalog.get_card(catalog_id)
			assert(not catalog_data.is_empty(), "Card catalog data must exist for catalog_id: '%s'" % catalog_id)

			# Apply stats from catalog - MUST have all required stats (NO FALLBACKS!)
			assert(catalog_data.has("max_hp"), "Card '%s' missing max_hp in catalog!" % catalog_id)
			assert(catalog_data.has("attack_damage"), "Card '%s' missing attack_damage in catalog!" % catalog_id)
			assert(catalog_data.has("attack_speed"), "Card '%s' missing attack_speed in catalog!" % catalog_id)
			assert(catalog_data.has("move_speed"), "Card '%s' missing move_speed in catalog!" % catalog_id)

			unit.max_hp = catalog_data.max_hp
			unit.attack_damage = catalog_data.attack_damage
			unit.attack_speed = catalog_data.attack_speed
			unit.move_speed = catalog_data.move_speed

			# Attack range is optional (different defaults for melee vs ranged)
			if catalog_data.has("attack_range"):
				print("Card: Setting attack_range from catalog: %.2f for card '%s'" % [catalog_data.attack_range, card_name])
				unit.attack_range = catalog_data.attack_range
			else:
				print("Card: No attack_range in catalog for '%s', using scene default: %.2f" % [card_name, unit.attack_range])

			# Initialize with modifiers AFTER catalog stats applied
			unit.initialize_with_modifiers(modifiers, card_data)

			# Add to tree first, then set position
			gameplay_layer.add_child(unit)
			unit.global_position = spawn_pos + Vector3(i * 2.0, 0, 0)
		else:
			push_error("Card._summon_unit_3d: Failed to instantiate unit from scene for card '%s'! Check unit_scene validity." % card_name)
			assert(false, "Unit must instantiate successfully!")

## Execute spell effect at the 3D position
func _cast_spell_3d(cast_pos: Vector3, team: Unit3D.Team, battlefield: Node, modifier_system: Node = null) -> void:
	# Get card definition from catalog
	var card_def: Dictionary = {}
	if not catalog_id.is_empty() and CardCatalog:
		card_def = CardCatalog.get_card(catalog_id)

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
		"team": team
	}

	# Get modifiers from ModifierSystem
	var modifiers: Array = _get_modifiers_from_system("spell", categories, context, modifier_system)

	# Apply modifiers to spell damage
	var modified_spell_damage: float = _apply_spell_modifiers(spell_damage, modifiers)

	# Check for instant spell VFX first (spawns immediately at click location)
	if not spell_vfx.is_empty():
		# Spawn VFX immediately with damage parameters
		# VFX will handle damage application at impact time
		VFXManager.play_effect(spell_vfx, cast_pos, {
			"radius": spell_radius,
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
func _spawn_spell_projectile(target_position: Vector3, team: Unit3D.Team, battlefield: Node, damage: float = 0.0) -> void:
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
func _cast_command_spell(target_pos: Vector3, team: Unit3D.Team, battlefield: Node, card_def: Dictionary) -> void:
	var command_type: String = card_def.get("command_type", "")
	var selection_radius: float = card_def.get("selection_radius", 8.0)
	print("Card: _cast_command_spell - card_name='%s', command_type='%s', catalog_id='%s'" % [card_name, command_type, catalog_id])

	# Select all friendly units in radius
	var scene_tree: SceneTree = battlefield.get_tree()
	if not scene_tree:
		return

	var all_units: Array[Node] = scene_tree.get_nodes_in_group("units")
	var selected_units: Array[Unit3D] = []

	for node: Node in all_units:
		if not node is Unit3D:
			continue

		var unit: Unit3D = node as Unit3D

		# Only select friendly units that are alive
		if unit.team != team or not unit.is_alive:
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
func _apply_rally_command(units: Array[Unit3D], rally_point: Vector3, _card_def: Dictionary) -> void:
	for unit: Unit3D in units:
		unit.rally_point = rally_point
		unit.rally_mode = true
		print("Card: Unit %s rallied to %s" % [unit.name, rally_point])

## Apply Guard command to selected units
func _apply_guard_command(units: Array[Unit3D], center: Vector3, _card_def: Dictionary) -> void:
	# Use Unit3D's static formation calculator
	Unit3D.calculate_formation_positions(units, center)
	print("Card: Formed guard formation with %d units at %s" % [units.size(), center])

## Apply Charge command to selected units (focus-fire on closest enemy)
func _apply_charge_command(units: Array[Unit3D], charge_dest: Vector3, caster_team: Unit3D.Team, _card_def: Dictionary) -> void:
	if units.is_empty():
		return

	# Find the closest enemy to the charge destination (units, bases, or structures)
	var closest_enemy: Node3D = RedirectManager.find_nearest_enemy(
		charge_dest,
		caster_team,
		RedirectManager.TARGET_SEARCH_RADIUS
	)

	if not closest_enemy:
		print("Card: No valid enemy targets found for Charge spell")
		return

	# Set forced target for all selected units
	var charge_duration: float = 30.0  # Focus on this target for 30 seconds
	for unit: Unit3D in units:
		unit.forced_target = closest_enemy
		unit.forced_target_timer = charge_duration
		print("Card: Unit %s charging toward %s" % [unit.name, closest_enemy.name])

## Find the base for the given team
func _find_base_by_team(team: Unit3D.Team, battlefield: Node) -> Node3D:
	var scene_tree: SceneTree = battlefield.get_tree()
	if not scene_tree:
		return null

	# Try to find base in the scene
	var bases: Array[Node] = scene_tree.get_nodes_in_group("bases")
	for base: Node in bases:
		if "team" in base:
			var base_team: Variant = base.get("team")
			if base_team == team:
				return base as Node3D

	# Fallback: just return battlefield root if no base found
	return battlefield as Node3D

## Apply AOE damage to enemies in 3D range
func _apply_aoe_damage_3d(aoe_center: Vector3, team: Unit3D.Team, battlefield: Node, damage: float = 0.0) -> void:
	# Use provided damage or fall back to spell_damage
	var final_damage: float = damage if damage > 0 else spell_damage

	var target_group: String = "enemy_units" if team == Unit3D.Team.PLAYER else "player_units"
	var scene_tree: SceneTree = battlefield.get_tree()
	if scene_tree == null:
		return

	var enemies: Array[Node] = scene_tree.get_nodes_in_group(target_group)

	for enemy: Node in enemies:
		if enemy is Unit3D:
			var enemy_unit: Unit3D = enemy
			if enemy_unit.is_alive:
				var distance: float = enemy_unit.global_position.distance_to(aoe_center)
				if distance <= spell_radius:
					enemy_unit.take_damage(final_damage)

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
	if VFXManager and VFXManager.has_effect("spell_fizzle"):
		VFXManager.play_effect("spell_fizzle", fail_pos)
		return

	# Fallback: Simple procedural fizzle effect
	_spawn_placeholder_fizzle(fail_pos)

## Spawn Rally VFX (selection circle + rally point marker)
func _spawn_rally_vfx(rally_point: Vector3) -> void:
	# Fixed visual marker radius (not gameplay-related, just for visual feedback)
	var visual_radius: float = 2.0

	# Try to use VFXManager if available
	if VFXManager and VFXManager.has_effect("rally_circle"):
		VFXManager.play_effect("rally_circle", rally_point, {"radius": visual_radius})
		return

	# Fallback: Simple procedural marker
	_spawn_placeholder_circle(rally_point, visual_radius, Color(0.2, 0.5, 1.0, 0.6))

## Spawn Guard VFX (formation position markers)
func _spawn_guard_vfx(units: Array[Unit3D]) -> void:
	# Try to use VFXManager if available
	if VFXManager and VFXManager.has_effect("guard_marker"):
		for unit: Unit3D in units:
			VFXManager.play_effect("guard_marker", unit.formation_position)
		return

	# Fallback: Simple procedural markers
	for unit: Unit3D in units:
		_spawn_placeholder_marker(unit.formation_position, Color(0.8, 0.3, 0.2, 0.6))

## Spawn Charge VFX (attack target marker)
func _spawn_charge_vfx(charge_point: Vector3) -> void:
	# Fixed visual marker radius
	var visual_radius: float = 2.0

	# Try to use VFXManager if available
	if VFXManager and VFXManager.has_effect("charge_marker"):
		VFXManager.play_effect("charge_marker", charge_point, {"radius": visual_radius})
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

	mesh_instance.global_position = fizzle_pos
	root.add_child(mesh_instance)

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

	mesh_instance.global_position = circle_pos
	root.add_child(mesh_instance)

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

	mesh_instance.global_position = circle_pos
	root.add_child(mesh_instance)

	# Auto-cleanup after 1.0 seconds
	var timer: SceneTreeTimer = scene_tree.create_timer(1.0)
	timer.timeout.connect(func() -> void:
		if is_instance_valid(mesh_instance):
			mesh_instance.queue_free()
	)
