extends Control
class_name MainMenu

## Main menu for Project Summoner
## Provides navigation to game modes and settings

@onready var placeholder_popup: AcceptDialog = $PlaceholderPopup

func _ready() -> void:
	print("Main Menu loaded")

	# Test CardCatalog integration
	_test_card_catalog()

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

## Test CardCatalog functionality
func _test_card_catalog() -> void:
	print("\n=== TESTING CARD CATALOG ===")

	# Test 1: Check catalog initialization
	var catalog = get_node("/root/CardCatalog")
	if catalog:
		print("✓ CardCatalog found")
		var all_ids = catalog.get_all_card_ids()
		print("  Cards in catalog: %s" % str(all_ids))
	else:
		push_error("✗ CardCatalog not found!")
		return

	# Test 2: Look up specific cards
	print("\n--- Card Lookups ---")
	for card_id in ["warrior", "archer", "fireball", "wall"]:
		var card_def = catalog.get_card(card_id)
		if not card_def.is_empty():
			print("✓ Found '%s': %s (%s, %d mana)" % [
				card_id,
				card_def.get("card_name"),
				card_def.get("rarity"),
				card_def.get("mana_cost")
			])
		else:
			push_error("✗ Card '%s' not found!" % card_id)

	# Test 3: Filter by rarity
	print("\n--- Filter by Rarity ---")
	var commons = catalog.get_cards_by_rarity("common")
	var rares = catalog.get_cards_by_rarity("rare")
	print("  Common cards: %d" % commons.size())
	print("  Rare cards: %d" % rares.size())

	# Test 4: Filter by type
	print("\n--- Filter by Type ---")
	var summons = catalog.get_cards_by_type(0)
	var spells = catalog.get_cards_by_type(1)
	print("  Summon cards: %d" % summons.size())
	print("  Spell cards: %d" % spells.size())

	# Test 5: Get starter cards
	print("\n--- Starter Cards ---")
	var starters = catalog.get_starter_cards()
	for card_def in starters:
		print("  - %s" % card_def.get("card_name"))

	# Test 6: Create runtime card resource
	print("\n--- Runtime Card Generation ---")
	var card_resource = catalog.create_card_resource("warrior")
	if card_resource:
		print("✓ Created Card resource for 'warrior'")
		print("  Name: %s" % card_resource.card_name)
		print("  Type: %d" % card_resource.card_type)
		print("  Cost: %d" % card_resource.mana_cost)
	else:
		push_error("✗ Failed to create Card resource!")

	# Test 7: Validate with CollectionService
	print("\n--- CollectionService Integration ---")
	var collection = get_node("/root/Collection")
	if collection:
		# Try granting valid cards
		var granted = collection.grant_cards([
			{"catalog_id": "warrior", "rarity": "common"},
			{"catalog_id": "fireball", "rarity": "rare"}
		])
		print("✓ Granted %d valid cards: %s" % [granted.size(), str(granted)])

		# Try granting invalid card (should be rejected)
		var invalid = collection.grant_cards([
			{"catalog_id": "nonexistent_card", "rarity": "common"}
		])
		if invalid.size() == 0:
			print("✓ Correctly rejected invalid card 'nonexistent_card'")
		else:
			push_error("✗ Should have rejected invalid card!")
	else:
		push_error("✗ CollectionService not found!")

	# Print catalog summary
	print("\n--- Catalog Summary ---")
	catalog.print_catalog_summary()

	print("=== CARD CATALOG TEST COMPLETE ===\n")
