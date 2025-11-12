extends TestGameController
class_name TestFlyingController

## Test Game Controller for Flying Units Testing
## - Inherits infinite mana and HP from TestGameController
## - Deck includes flying units, ground melee, and ranged units
## - Tests targeting mechanics: ground melee cannot hit flying units

func _ready() -> void:
	print("TestFlyingController: Initializing flying units test mode...")

	# Override parent's test deck with flying test composition
	test_deck_cards = [
		# Flying units (5x) - Fast, fragile, immune to ground melee
		"demon_imp", "demon_imp", "demon_imp", "demon_imp", "demon_imp",

		# Ground melee (10x) - Should NOT be able to attack flying units
		"warrior", "warrior", "warrior", "warrior", "warrior",
		"warrior", "warrior", "warrior", "warrior", "warrior",

		# Ranged units (10x) - SHOULD be able to attack flying units (anti-air)
		"archer", "archer", "archer", "archer", "archer",
		"archer", "archer", "archer", "archer", "archer"
	]

	# Configure BattleContext for practice mode
	var battle_context = get_node_or_null("/root/BattleContext")
	if battle_context:
		battle_context.configure_practice_battle({
			"enemy_deck": [
				{"catalog_id": "warrior", "count": 10},      # Ground melee (cannot hit flying)
				{"catalog_id": "archer", "count": 8},        # Anti-air ranged (can hit flying)
				{"catalog_id": "demon_imp", "count": 7}      # Flying units (test air-to-air)
			],
			"enemy_hp": 999999.0
		})

	# Call grandparent ready (GameController3D)
	super._ready()

	# Override player deck with flying test deck
	if player_summoner:
		_load_test_deck_for_summoner(player_summoner)

	# Give enemy a mixed deck for comprehensive testing
	if enemy_summoner:
		_load_enemy_test_deck(enemy_summoner)

	# Set infinite HP for both bases
	await get_tree().process_frame
	if enemy_base and "max_hp" in enemy_base:
		enemy_base.max_hp = 999999.0
		enemy_base.current_hp = 999999.0
		print("TestFlyingController: Enemy base set to infinite HP")

	if player_base and "max_hp" in player_base:
		player_base.max_hp = 999999.0
		player_base.current_hp = 999999.0
		print("TestFlyingController: Player base set to infinite HP")

	print("TestFlyingController: Flying test mode ready!")
	print("  - Player deck: %d cards (demon imps, warriors, archers)" % test_deck_cards.size())
	print("  - Infinite mana enabled")
	print("  - Enemy HP: 999999")
	print("  - No time limit")
	print("")
	print("=== FLYING MECHANICS TEST ===")
	print("Expected behavior:")
	print("  ✓ Demon Imps fly at elevated altitude with scaled shadows")
	print("  ✓ Warriors (ground melee) CANNOT attack Demon Imps")
	print("  ✓ Archers (ranged) CAN attack Demon Imps")
	print("  ✓ Demon Imps CAN attack Warriors and Archers")
	print("  ✓ Demon Imps CAN attack other Demon Imps (air-to-air)")
	print("============================")

## Override enemy deck loading to use BattleContext configuration
func _load_enemy_test_deck(summoner: Summoner3D) -> void:
	var cards: Array[Card] = []

	# Load deck from BattleContext configuration (warriors, archers, demon imps)
	var battle_context = get_node_or_null("/root/BattleContext")
	if battle_context and battle_context.has_method("get_enemy_deck_config"):
		var deck_config = battle_context.get_enemy_deck_config()
		if deck_config and not deck_config.is_empty():
			for entry in deck_config:
				var catalog_id = entry.get("catalog_id", "")
				var count = entry.get("count", 1)

				for i in range(count):
					var card = _load_card_resource(catalog_id)
					if card:
						cards.append(card)
		else:
			push_warning("TestFlyingController: BattleContext has no enemy deck config, using fallback")

	# Fallback if BattleContext doesn't have config
	if cards.is_empty():
		# Use configured deck from _ready()
		for i in range(10):
			cards.append(_load_card_resource("warrior"))
		for i in range(8):
			cards.append(_load_card_resource("archer"))
		for i in range(7):
			cards.append(_load_card_resource("demon_imp"))

	summoner.deck = cards
	summoner.deck.shuffle()

	# Clear hand and redraw
	summoner.hand.clear()
	for i in summoner.max_hand_size:
		summoner.draw_card()

	print("TestFlyingController: Loaded %d test cards for enemy (mixed units)" % cards.size())
