extends TestGameController
class_name TestSlimesController

## Test Game Controller for Slime Testing
## - Inherits infinite mana and HP from TestGameController
## - Deck full of all slime types for testing animations and combat

func _ready() -> void:
	print("TestSlimesController: Initializing slime test mode...")

	# Override parent's test deck with all 9 slime types (5 of each = 45 cards)
	test_deck_cards = [
		# Small slimes (fast, low HP) - 5 of each
		"slime_green", "slime_green", "slime_green", "slime_green", "slime_green",
		"slime_pink", "slime_pink", "slime_pink", "slime_pink", "slime_pink",
		"slime_violet", "slime_violet", "slime_violet", "slime_violet", "slime_violet",

		# Medium slimes (balanced) - 5 of each
		"slime_blue", "slime_blue", "slime_blue", "slime_blue", "slime_blue",
		"slime_orange", "slime_orange", "slime_orange", "slime_orange", "slime_orange",
		"slime_yellow", "slime_yellow", "slime_yellow", "slime_yellow", "slime_yellow",

		# Large slimes (tanks) - 5 of each
		"slime_grey", "slime_grey", "slime_grey", "slime_grey", "slime_grey",
		"slime_purple", "slime_purple", "slime_purple", "slime_purple", "slime_purple",
		"slime_red", "slime_red", "slime_red", "slime_red", "slime_red"
	]

	# Configure BattleContext for practice mode
	var battle_context: Node = get_node_or_null("/root/BattleContext")
	if battle_context and battle_context.has_method("configure_practice_battle"):
		battle_context.call("configure_practice_battle", {
			"enemy_deck": [{"catalog_id": "warrior", "count": 30}],
			"enemy_hp": 999999.0
		})

	# Call grandparent ready (GameController3D)
	super._ready()

	# Override player deck with slime test deck
	if player_summoner:
		_load_test_deck_for_summoner(player_summoner)

	# Give enemy a simple deck (warriors for target practice)
	if enemy_summoner:
		_load_enemy_test_deck(enemy_summoner)

	# Set infinite HP for both bases
	await get_tree().process_frame
	if enemy_base and "max_hp" in enemy_base:
		enemy_base.set("max_hp", 999999.0)
		enemy_base.set("current_hp", 999999.0)
		print("TestSlimesController: Enemy base set to infinite HP")

	if player_base and "max_hp" in player_base:
		player_base.set("max_hp", 999999.0)
		player_base.set("current_hp", 999999.0)
		print("TestSlimesController: Player base set to infinite HP")

	print("TestSlimesController: Slime test mode ready!")
	print("  - Player deck: %d slime cards (all 9 types)" % test_deck_cards.size())
	print("  - Infinite mana enabled")
	print("  - Enemy HP: 999999")
	print("  - No time limit")
	print("  - Test death animations by letting units fight!")
