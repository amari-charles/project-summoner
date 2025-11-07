extends Resource
class_name Card

## Represents a single-use card (unit summon or spell)
## Cards are drawn from the player's deck and used once per match

enum CardType { SUMMON, SPELL }

## Card identity
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
	print("Card._summon_unit_3d: Spawning '%s' at position %v" % [card_name, position])

	if unit_scene == null:
		push_error("Card '%s' has no unit_scene assigned!" % card_name)
		return

	print("Card._summon_unit_3d: Battlefield = %s" % battlefield)
	var gameplay_layer = battlefield.get_gameplay_layer() if battlefield.has_method("get_gameplay_layer") else battlefield
	print("Card._summon_unit_3d: GameplayLayer = %s" % gameplay_layer)

	for i in spawn_count:
		var unit = unit_scene.instantiate() as Unit3D
		if unit:
			unit.global_position = position + Vector3(i * 2.0, 0, 0)
			unit.team = team
			print("Card._summon_unit_3d: Adding unit %s to %s at position %v" % [unit, gameplay_layer, unit.global_position])
			gameplay_layer.add_child(unit)
			print("Card._summon_unit_3d: Unit added successfully")
		else:
			push_error("Card._summon_unit_3d: Failed to instantiate unit from scene!")

## Execute spell effect at the 3D position
func _cast_spell_3d(position: Vector3, team: Unit3D.Team, battlefield: Node) -> void:
	if spell_damage > 0:
		_apply_aoe_damage_3d(position, team, battlefield)

## Apply AOE damage to enemies in 3D range
func _apply_aoe_damage_3d(position: Vector3, team: Unit3D.Team, battlefield: Node) -> void:
	var target_group = "enemy_units" if team == Unit3D.Team.PLAYER else "player_units"
	var scene_tree = battlefield.get_tree()
	if scene_tree == null:
		return

	var enemies = scene_tree.get_nodes_in_group(target_group)

	for enemy in enemies:
		if enemy is Unit3D and enemy.is_alive:
			var distance = enemy.global_position.distance_to(position)
			if distance <= spell_radius:
				enemy.take_damage(spell_damage)

	# TODO: Add 3D visual effect for spell
