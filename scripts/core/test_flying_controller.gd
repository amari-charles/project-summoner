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

	# Configure BattleContext BEFORE calling super._ready() so summoners can load decks
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

	# Call parent ready (summoners will now load decks from BattleContext)
	super._ready()

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
	print("  - Enemy deck: Loaded from BattleContext (warriors, archers, demon imps)")
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
