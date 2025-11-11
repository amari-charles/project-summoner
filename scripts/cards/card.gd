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

## Visual
@export var card_icon: Texture2D = null

## Validate if this card can be played
func can_play(current_mana: int) -> bool:
	return current_mana >= mana_cost

## Execute the card effect at the given position
## Note: This is called from Summoner which has scene tree access
func play(position: Vector2, team: Unit.Team, battlefield: Node) -> void:
	match card_type:
		CardType.SUMMON:
			_summon_unit(position, team, battlefield)
		CardType.SPELL:
			_cast_spell(position, team, battlefield)

## Execute the card effect at the given 3D position
func play_3d(position: Vector3, team: Unit3D.Team, battlefield: Node) -> void:
	match card_type:
		CardType.SUMMON:
			_summon_unit_3d(position, team, battlefield)
		CardType.SPELL:
			_cast_spell_3d(position, team, battlefield)

## Spawn unit(s) at the position
func _summon_unit(position: Vector2, team: Unit.Team, battlefield: Node) -> void:
	if unit_scene == null:
		push_error("Card '%s' has no unit_scene assigned!" % card_name)
		return

	for i in spawn_count:
		var unit = unit_scene.instantiate() as Unit
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
	var target_group = "enemy_units" if team == Unit.Team.PLAYER else "player_units"
	var scene_tree = battlefield.get_tree()
	if scene_tree == null:
		return

	var enemies = scene_tree.get_nodes_in_group(target_group)

	for enemy in enemies:
		if enemy is Unit and enemy.is_alive:
			var distance = enemy.global_position.distance_to(position)
			if distance <= spell_radius:
				enemy.take_damage(spell_damage)

	# Visual effect placeholder
	var explosion = ColorRect.new()
	explosion.size = Vector2(spell_radius * 2, spell_radius * 2)
	explosion.position = position - explosion.size / 2
	explosion.color = Color(1, 0.5, 0, 0.5)  # Orange translucent

	battlefield.add_child(explosion)
	await scene_tree.create_timer(0.5).timeout
	explosion.queue_free()

## Spawn unit(s) at the 3D position
func _summon_unit_3d(position: Vector3, team: Unit3D.Team, battlefield: Node) -> void:
	if unit_scene == null:
		push_error("Card '%s' has no unit_scene assigned!" % card_name)
		return

	var gameplay_layer = battlefield.get_gameplay_layer() if battlefield.has_method("get_gameplay_layer") else battlefield

	# Get card categories from catalog
	var categories = {}
	if not catalog_id.is_empty() and CardCatalog:
		var card_def = CardCatalog.get_card(catalog_id)
		if not card_def.is_empty():
			categories = card_def.get("categories", {})

	# Build context for modifier system
	var context = {
		"card_name": card_name,
		"team": team
	}

	# Get modifiers from ModifierSystem
	var modifiers = _get_modifiers_from_system("unit", categories, context)

	# Card data for apply_modifiers
	var card_data = {
		"card_name": card_name,
		"mana_cost": mana_cost
	}

	for i in spawn_count:
		var unit = unit_scene.instantiate() as Unit3D
		if unit:
			unit.global_position = position + Vector3(i * 2.0, 0, 0)
			unit.team = team

			# Initialize with modifiers BEFORE adding to scene
			unit.initialize_with_modifiers(modifiers, card_data)

			gameplay_layer.add_child(unit)
		else:
			push_error("Card._summon_unit_3d: Failed to instantiate unit from scene!")

## Execute spell effect at the 3D position
func _cast_spell_3d(position: Vector3, team: Unit3D.Team, battlefield: Node) -> void:
	# Get card categories from catalog
	var categories = {}
	if not catalog_id.is_empty() and CardCatalog:
		var card_def = CardCatalog.get_card(catalog_id)
		if not card_def.is_empty():
			categories = card_def.get("categories", {})

	# Build context for modifier system
	var context = {
		"card_name": card_name,
		"team": team
	}

	# Get modifiers from ModifierSystem
	var modifiers = _get_modifiers_from_system("spell", categories, context)

	# Apply modifiers to spell damage
	var modified_spell_damage = _apply_spell_modifiers(spell_damage, modifiers)

	# If spell uses a projectile, spawn it instead of instant cast
	if not projectile_id.is_empty():
		_spawn_spell_projectile(position, team, battlefield, modified_spell_damage)
	elif modified_spell_damage > 0:
		# Fallback to instant AOE damage (legacy behavior)
		_apply_aoe_damage_3d(position, team, battlefield, modified_spell_damage)

## Apply modifiers to spell damage
##
## Spells can be affected by two types of modifiers:
## - "attack_damage": Generic damage modifiers (affects both units and spells)
## - "spell_damage": Spell-specific modifiers (affects only spells)
##
## NOTE: Both keys are checked and summed. If a modifier has both keys,
## both values will be applied (this allows for future flexibility).
func _apply_spell_modifiers(base_damage: float, modifiers: Array) -> float:
	var damage = base_damage

	# Phase 1: Sum additive bonuses
	var add_bonus = 0.0
	for mod in modifiers:
		var stat_adds = mod.get("stat_adds", {})
		add_bonus += stat_adds.get("attack_damage", 0.0)
		add_bonus += stat_adds.get("spell_damage", 0.0)

	damage += add_bonus

	# Phase 2: Apply multiplicative bonuses
	var mult_bonus = 0.0
	for mod in modifiers:
		var stat_mults = mod.get("stat_mults", {})
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
	var final_damage = damage if damage > 0 else spell_damage

	# Find source (player or enemy base)
	var source: Node3D = _find_base_by_team(team, battlefield)
	if not source:
		push_warning("Card: Could not find source base for spell projectile")
		# Fallback to instant damage
		_apply_aoe_damage_3d(target_position, team, battlefield, final_damage)
		return

	# Spawn projectile using ProjectileManager
	var projectile = ProjectileManager.spawn_projectile(
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
		push_error("Card: Failed to spawn spell projectile '%s'" % projectile_id)

## Find the base for the given team
func _find_base_by_team(team: Unit3D.Team, battlefield: Node) -> Node3D:
	var scene_tree = battlefield.get_tree()
	if not scene_tree:
		return null

	# Try to find base in the scene
	var bases = scene_tree.get_nodes_in_group("bases")
	for base in bases:
		if "team" in base and base.team == team:
			return base as Node3D

	# Fallback: just return battlefield root if no base found
	return battlefield as Node3D

## Apply AOE damage to enemies in 3D range
func _apply_aoe_damage_3d(position: Vector3, team: Unit3D.Team, battlefield: Node, damage: float = 0.0) -> void:
	# Use provided damage or fall back to spell_damage
	var final_damage = damage if damage > 0 else spell_damage

	var target_group = "enemy_units" if team == Unit3D.Team.PLAYER else "player_units"
	var scene_tree = battlefield.get_tree()
	if scene_tree == null:
		return

	var enemies = scene_tree.get_nodes_in_group(target_group)

	for enemy in enemies:
		if enemy is Unit3D and enemy.is_alive:
			var distance = enemy.global_position.distance_to(position)
			if distance <= spell_radius:
				enemy.take_damage(final_damage)

	# TODO: Add 3D visual effect for spell

## Helper to safely access ModifierSystem autoload from Resource context
func _get_modifiers_from_system(target_type: String, categories: Dictionary, context: Dictionary) -> Array:
	# Try to access the autoload
	# In Godot, autoloads registered in project.godot are accessible as globals
	# but Resources can't directly reference them during parse time
	# So we wrap the access in a function that runs at runtime
	var modifiers = []

	# Check if CardCatalog exists (another autoload) - if it does, ModifierSystem should too
	if not CardCatalog:
		push_warning("Card: CardCatalog autoload not found, modifiers unavailable")
		return modifiers

	# Access ModifierSystem via root node
	var root = Engine.get_main_loop().root if Engine.get_main_loop() else null
	if not root:
		push_error("Card: Failed to access scene tree root, modifiers unavailable")
		return modifiers

	var modifier_system = root.get_node_or_null("ModifierSystem")
	if not modifier_system:
		push_error("Card: ModifierSystem autoload not found, modifiers unavailable")
		return modifiers

	if not modifier_system.has_method("get_modifiers_for"):
		push_error("Card: ModifierSystem missing get_modifiers_for method")
		return modifiers

	modifiers = modifier_system.get_modifiers_for(target_type, categories, context)
	return modifiers
