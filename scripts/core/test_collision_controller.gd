extends TestGameController
class_name TestCollisionController

## Test Controller for Unit Collision System
## - Inherits infinite mana and HP from TestGameController
## - Buttons to spawn player and enemy units for testing collisions
## - Tests: spawn preview, separation steering, overlap correction

var spawn_position_offset: float = 0.0  # Increments to spread spawns

func _ready() -> void:
	print("TestCollisionController: Initializing collision test mode...")

	# Override test deck with varied units
	test_deck_cards = [
		# Small units (small collision radius)
		CardIDs.SLIME_GREEN, CardIDs.SLIME_GREEN, CardIDs.SLIME_GREEN,
		CardIDs.SLIME_PINK, CardIDs.SLIME_PINK, CardIDs.SLIME_PINK,
		# Medium units
		CardIDs.FIRE_RECRUIT, CardIDs.FIRE_RECRUIT, CardIDs.FIRE_RECRUIT,
		CardIDs.EMBER_SLINGER, CardIDs.EMBER_SLINGER, CardIDs.EMBER_SLINGER,
		# Larger units
		CardIDs.SLIME_RED, CardIDs.SLIME_RED, CardIDs.SLIME_RED,
		CardIDs.ASH_VANGUARD, CardIDs.ASH_VANGUARD, CardIDs.ASH_VANGUARD,
		# Spells (Charge to test movement)
		CardIDs.CHARGE, CardIDs.CHARGE, CardIDs.CHARGE, CardIDs.CHARGE,
	]

	# Call parent ready
	super._ready()

	print("TestCollisionController: Collision test mode ready!")
	print("  - Drag cards to spawn player units")
	print("  - Click buttons to spawn enemy units")
	print("  - Watch spawn preview circle during drag")
	print("  - Watch units separate after spawning")
	print("  - Use Charge spell to see movement collision")

## Spawn enemy unit at a position (varied positions for testing)
func spawn_enemy_at_position(catalog_id: String) -> void:
	print("TestCollisionController: Spawning enemy: %s" % catalog_id)

	var card: Card = _load_card_resource(StringName(catalog_id))
	if not card:
		push_error("TestCollisionController: Failed to load card: %s" % catalog_id)
		return

	if not enemy_summoner:
		push_error("TestCollisionController: No enemy summoner available")
		return

	# Add card to enemy's hand temporarily
	enemy_summoner.hand.append(card)

	# Spawn at varied positions to test stacking
	var card_index: int = enemy_summoner.hand.size() - 1
	var spawn_pos: Vector3 = Vector3(5.0 + spawn_position_offset, 1.0, randf_range(-3, 3))
	spawn_position_offset = fmod(spawn_position_offset + 1.5, 10.0)

	if enemy_summoner.play_card_3d(card_index, spawn_pos):
		print("TestCollisionController: Spawned %s at %s" % [catalog_id, spawn_pos])
	else:
		print("TestCollisionController: Failed to spawn %s" % catalog_id)

## Spawn cluster of enemies at same position (stress test stacking)
func spawn_enemy_cluster(catalog_id: String, count: int = 5) -> void:
	print("TestCollisionController: Spawning cluster of %d %s" % [count, catalog_id])

	var spawn_pos: Vector3 = Vector3(8.0, 1.0, 0.0)

	for i: int in count:
		var card: Card = _load_card_resource(StringName(catalog_id))
		if not card:
			continue

		enemy_summoner.hand.append(card)
		var card_index: int = enemy_summoner.hand.size() - 1

		# All at same position to test separation
		if enemy_summoner.play_card_3d(card_index, spawn_pos):
			print("  - Spawned #%d" % (i + 1))

		# Small delay to let physics settle
		await get_tree().create_timer(0.1).timeout

## Spawn player unit at position (for testing without drag)
func spawn_player_at_position(catalog_id: String) -> void:
	print("TestCollisionController: Spawning player unit: %s" % catalog_id)

	var card: Card = _load_card_resource(StringName(catalog_id))
	if not card:
		push_error("TestCollisionController: Failed to load card: %s" % catalog_id)
		return

	if not player_summoner:
		push_error("TestCollisionController: No player summoner available")
		return

	player_summoner.hand.append(card)

	var card_index: int = player_summoner.hand.size() - 1
	var spawn_pos: Vector3 = Vector3(-5.0 - spawn_position_offset, 1.0, randf_range(-3, 3))
	spawn_position_offset = fmod(spawn_position_offset + 1.5, 10.0)

	if player_summoner.play_card_3d(card_index, spawn_pos):
		print("TestCollisionController: Spawned %s at %s" % [catalog_id, spawn_pos])

## Spawn player cluster at same position
func spawn_player_cluster(catalog_id: String, count: int = 5) -> void:
	print("TestCollisionController: Spawning player cluster of %d %s" % [count, catalog_id])

	var spawn_pos: Vector3 = Vector3(-8.0, 1.0, 0.0)

	for i: int in count:
		var card: Card = _load_card_resource(StringName(catalog_id))
		if not card:
			continue

		player_summoner.hand.append(card)
		var card_index: int = player_summoner.hand.size() - 1

		if player_summoner.play_card_3d(card_index, spawn_pos):
			print("  - Spawned #%d" % (i + 1))

		await get_tree().create_timer(0.1).timeout

## Clear all units from battlefield
func clear_all_units() -> void:
	print("TestCollisionController: Clearing all units...")

	var all_units: Array[Node] = get_tree().get_nodes_in_group(GroupIDs.UNITS)
	for unit: Node in all_units:
		unit.queue_free()

	spawn_position_offset = 0.0
	print("TestCollisionController: Cleared %d units" % all_units.size())
