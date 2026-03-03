extends Node

## Test Setup for Rally/Guard/Charge Spells
##
## NOTE: Uses archived command spells (Rally, Guard, Charge) for testing purposes.
## These spells are excluded from normal gameplay but kept for development testing.
##
## Attach this script to PlayerSummoner in a battle scene to:
## - Override the deck with Rally/Guard/Charge + test units
## - Start with Rally, Guard, and Charge in hand
## - Give extra mana for testing
## - Spawn some enemy units to test commands on
##
## Usage:
##   1. Open any battle scene (battle_3d.tscn)
##   2. Add this script to PlayerSummoner node
##   3. Run the scene
##   4. Rally, Guard, and Charge will be in your starting hand

## Test deck composition
var test_deck_ids: Array[String] = [
	CardIDs.FIRE_WISP,  # Test units first
	CardIDs.FIRE_WISP,
	CardIDs.CHARGE,          # Charge spell
	CardIDs.RALLY,           # Rally spell
	CardIDs.GUARD,           # Guard spell
	CardIDs.CHARGE,          # Extra charge
	CardIDs.RALLY,           # Extra rally
	CardIDs.GUARD,           # Extra guard
	CardIDs.PEBBLOOM,    # More units
	CardIDs.PEBBLOOM,
	CardIDs.PUFF,
	CardIDs.PUFF,
]

func _ready() -> void:
	# Wait for parent summoner to be ready
	await get_tree().process_frame
	await get_tree().process_frame

	var summoner: Node = get_parent()

	if not summoner:
		push_error("RallyGuardTestSetup: No parent summoner found!")
		return

	print("=== Rally/Guard/Charge Test Setup ===")

	# Build test deck from card IDs
	var test_deck: Array[Card] = []
	for card_id: String in test_deck_ids:
		var card: Card = CardCatalog.create_card_resource(card_id)
		if card:
			test_deck.append(card)
		else:
			push_warning("RallyGuardTestSetup: Failed to create card '%s'" % card_id)

	# Override summoner's deck
	if summoner.has_method("set"):
		summoner.set("deck", test_deck)
		summoner.set("hand", [])
		print("RallyGuardTestSetup: Loaded test deck with %d cards" % test_deck.size())

	# Give player extra mana for testing
	if summoner.has_method("set"):
		summoner.set("mana", 100.0)
		summoner.set("max_mana", 100.0)
		print("RallyGuardTestSetup: Set mana to 100")

	# Draw starting hand with Rally and Guard guaranteed
	await get_tree().process_frame

	if summoner.has_method("draw_card"):
		# Draw 5 cards to start
		for i: int in range(5):
			summoner.call("draw_card")
		print("RallyGuardTestSetup: Drew 5 cards")

	# Spawn some test enemy units for Rally/Guard testing
	await get_tree().create_timer(1.0).timeout
	_spawn_test_enemies()

	print("RallyGuardTestSetup: Setup complete!")
	print("  - Rally, Guard, and Charge should be in your hand")
	print("  - You have 100 mana")
	print("  - Enemy units spawned for testing")
	print("  - Try: Cast units, then use Rally/Guard/Charge on them")

## Spawn some enemy units for testing tactical commands
func _spawn_test_enemies() -> void:
	var summoner: Node = get_parent()
	if not summoner:
		return

	# Find enemy summoner
	var enemy_summoner: Node = null
	var summoners: Array[Node] = get_tree().get_nodes_in_group(GroupIDs.SUMMONERS)
	for node: Node in summoners:
		if node != summoner and "team" in node:
			var team_variant: Variant = node.get("team")
			if team_variant is int:
				var team_value: int = team_variant
				# Look for enemy team (1 = enemy in 3D)
				if team_value == 1:
					enemy_summoner = node
					break

	if not enemy_summoner:
		push_warning("RallyGuardTestSetup: Could not find enemy summoner")
		return

	# Spawn a few enemy units
	var enemy_unit_ids: Array[String] = [
		CardIDs.FIRE_WISP,
		CardIDs.FIRE_WISP,
		CardIDs.PEBBLOOM,
	]

	for card_id: String in enemy_unit_ids:
		var card: Card = CardCatalog.create_card_resource(card_id)
		if card and card.Type == UnitConstants.CardType.SUMMON:
			# Spawn in enemy territory (around x=40)
			var spawn_pos: Vector3 = Vector3(
				35.0 + randf_range(-5.0, 5.0),  # x: around enemy base
				0.0,
				randf_range(-5.0, 5.0)  # z: some spread
			)

			# Use enemy summoner's play_card_3d if available
			if enemy_summoner.has_method("get"):
				var battlefield_variant: Variant = enemy_summoner.get("battlefield")
				if battlefield_variant and battlefield_variant is Node:
					var battlefield: Node = battlefield_variant
					var team_variant: Variant = enemy_summoner.get("team")
					var team: int = team_variant if team_variant is int else 1

					# Directly spawn unit using card's play_3d
					card.SpawnAt(spawn_pos, team)

	print("RallyGuardTestSetup: Spawned %d enemy units" % enemy_unit_ids.size())
