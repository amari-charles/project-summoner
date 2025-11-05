extends Control
class_name MainMenu

## Main menu for Project Summoner
## Provides navigation to game modes and settings

@onready var placeholder_popup: AcceptDialog = $PlaceholderPopup

func _ready() -> void:
	print("Main Menu loaded")

	# TEMPORARY: Test save system
	await get_tree().process_frame
	_test_save_system()

## Launch the game
func _on_play_pressed() -> void:
	print("Starting game...")
	get_tree().change_scene_to_file("res://scenes/battlefield/test_game.tscn")

## PLACEHOLDER - Collection screen not yet implemented
func _on_collection_pressed() -> void:
	print("Collection button pressed (PLACEHOLDER)")
	placeholder_popup.popup_centered()

## PLACEHOLDER - Settings screen not yet implemented
func _on_settings_pressed() -> void:
	print("Settings button pressed (PLACEHOLDER)")
	placeholder_popup.popup_centered()

## Quit the game
func _on_quit_pressed() -> void:
	print("Quitting game...")
	get_tree().quit()

## TEMPORARY: Test save system functionality
func _test_save_system() -> void:
	print("\n=== SAVE SYSTEM TEST ===")

	# Test 1: Check initial state
	print("\n[Test 1] Initial State:")
	print("  Gold: %d" % Economy.get_gold())
	print("  Cards: %d" % Collection.list_cards().size())
	print("  Decks: %d" % Decks.list_decks().size())

	# Test 2: Add resources
	print("\n[Test 2] Adding resources...")
	Economy.add_gold(500)
	Economy.add_essence(100)
	Economy.add_fragments(50)
	print("  Gold after +500: %d" % Economy.get_gold())
	print("  Essence after +100: %d" % Economy.get_essence())
	print("  Fragments after +50: %d" % Economy.get_fragments())

	# Test 3: Grant cards
	print("\n[Test 3] Granting cards...")
	var new_cards = Collection.grant_cards([
		{"catalog_id": "fireball", "rarity": "rare"},
		{"catalog_id": "wall", "rarity": "common"},
		{"catalog_id": "warrior", "rarity": "common"}
	])
	print("  Granted %d new cards" % new_cards.size())
	print("  Total cards now: %d" % Collection.list_cards().size())

	# Test 4: Create deck (if we have 30+ cards)
	var all_cards = Collection.list_cards()
	if all_cards.size() >= 30:
		print("\n[Test 4] Creating test deck...")
		var deck_cards = []
		for i in range(30):
			deck_cards.append(all_cards[i].id)
		var deck_id = Decks.create_deck("Test Deck", deck_cards)
		var is_valid = Decks.validate_deck(deck_id)
		print("  Deck created: %s" % deck_id)
		print("  Deck valid: %s" % ("YES" if is_valid else "NO"))
		print("  Total decks: %d" % Decks.list_decks().size())
	else:
		print("\n[Test 4] Skipped (need 30 cards, have %d)" % all_cards.size())

	# Test 5: Check save file location
	print("\n[Test 5] Save file location:")
	print("  Windows: %%APPDATA%%\\Godot\\app_userdata\\Project Summoner\\profiles\\default\\")
	print("  Full path: %s" % OS.get_user_data_dir())

	print("\n=== TEST COMPLETE ===")
	print("Close and restart the game to verify persistence!")
	print("========================\n")
