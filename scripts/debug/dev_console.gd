extends Node
# DevConsole is registered as an autoload, no class_name needed

## Dev Console - Debug Commands for Testing Save System
##
## Provides commands for testing and manipulating save data.
## Only accessible in debug builds.
##
## Available commands:
##   /save_wipe - Delete save and start fresh
##   /save_grant_cards <count> - Grant N random cards
##   /save_add_gold <amount> - Add gold
##   /save_add_essence <amount> - Add essence
##   /save_add_fragments <amount> - Add fragments
##   /save_corrupt - Corrupt main save file (test recovery)
##   /save_info - Print current save state
##   /save_reload - Force reload from disk
##   /save_create_deck <name> - Create a test deck
##
## Usage in game:
##   Press F12 to toggle console (future implementation)
##   Or call commands directly: DevConsole.execute_command("/save_info")

## Available card catalog IDs for testing
const TEST_CARDS = ["warrior", "archer", "fireball", "wall"]
const TEST_RARITIES = ["common", "common", "common", "rare", "epic"]  # Weighted

## Service references (injected by autoload order)
var _repo: IProfileRepo = null
var _economy: EconomyService = null
var _collection: CollectionService = null
var _decks: DeckService = null

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("DevConsole: Initializing...")

	# Wait for services to be ready
	await get_tree().process_frame

	_repo = get_node_or_null("/root/ProfileRepo")
	_economy = get_node_or_null("/root/Economy")
	_collection = get_node_or_null("/root/Collection")
	_decks = get_node_or_null("/root/Decks")

	if _repo == null:
		push_error("DevConsole: ProfileRepo not found!")

	print("DevConsole: Ready (F12 to open console - future)")

func _input(event: InputEvent) -> void:
	# Future: F12 to toggle console UI
	if event is InputEventKey and event.pressed and event.keycode == KEY_F12:
		print("DevConsole: F12 pressed (console UI not yet implemented)")

## =============================================================================
## COMMAND EXECUTION
## =============================================================================

## Execute a command string
## Returns: true if command executed successfully
func execute_command(command: String) -> bool:
	var parts = command.split(" ", false)
	if parts.size() == 0:
		return false

	var cmd = parts[0]
	var args = parts.slice(1)

	match cmd:
		"/save_wipe":
			return _cmd_save_wipe()
		"/save_grant_cards":
			return _cmd_grant_cards(args)
		"/save_add_gold":
			return _cmd_add_gold(args)
		"/save_add_essence":
			return _cmd_add_essence(args)
		"/save_add_fragments":
			return _cmd_add_fragments(args)
		"/save_corrupt":
			return _cmd_corrupt_save()
		"/save_info":
			return _cmd_save_info()
		"/save_reload":
			return _cmd_save_reload()
		"/save_create_deck":
			return _cmd_create_deck(args)
		_:
			print("DevConsole: Unknown command: %s" % cmd)
			return false

## =============================================================================
## COMMAND IMPLEMENTATIONS
## =============================================================================

func _cmd_save_wipe() -> bool:
	print("DevConsole: Wiping save data...")

	if _repo == null:
		push_error("DevConsole: Repo not available")
		return false

	_repo.reset_profile()
	print("DevConsole: Save wiped, fresh profile created")
	return true

func _cmd_grant_cards(args: Array) -> bool:
	if _collection == null:
		push_error("DevConsole: Collection service not available")
		return false

	var count = 5  # Default
	if args.size() > 0:
		count = int(args[0])

	print("DevConsole: Granting %d random cards..." % count)

	var cards_to_grant = []
	for i in range(count):
		var catalog_id = TEST_CARDS[randi() % TEST_CARDS.size()]
		var rarity = TEST_RARITIES[randi() % TEST_RARITIES.size()]
		cards_to_grant.append({"catalog_id": catalog_id, "rarity": rarity})

	var instance_ids = _collection.grant_cards(cards_to_grant)
	print("DevConsole: Granted %d cards (instance IDs: %s)" % [instance_ids.size(), str(instance_ids)])

	return true

func _cmd_add_gold(args: Array) -> bool:
	if _economy == null:
		push_error("DevConsole: Economy service not available")
		return false

	var amount = 100  # Default
	if args.size() > 0:
		amount = int(args[0])

	print("DevConsole: Adding %d gold..." % amount)
	_economy.add_gold(amount)
	print("DevConsole: Gold added (current: %d)" % _economy.get_gold())

	return true

func _cmd_add_essence(args: Array) -> bool:
	if _economy == null:
		push_error("DevConsole: Economy service not available")
		return false

	var amount = 50  # Default
	if args.size() > 0:
		amount = int(args[0])

	print("DevConsole: Adding %d essence..." % amount)
	_economy.add_essence(amount)
	print("DevConsole: Essence added (current: %d)" % _economy.get_essence())

	return true

func _cmd_add_fragments(args: Array) -> bool:
	if _economy == null:
		push_error("DevConsole: Economy service not available")
		return false

	var amount = 10  # Default
	if args.size() > 0:
		amount = int(args[0])

	print("DevConsole: Adding %d fragments..." % amount)
	_economy.add_fragments(amount)
	print("DevConsole: Fragments added (current: %d)" % _economy.get_fragments())

	return true

func _cmd_corrupt_save() -> bool:
	print("DevConsole: Corrupting main save file for recovery test...")

	if _repo == null:
		push_error("DevConsole: Repo not available")
		return false

	var profile_id = _repo.get_current_profile_id()
	var profile_dir = "user://profiles/" + profile_id
	var main_path = profile_dir + "/profile.json"

	# Write garbage to main save
	var file = FileAccess.open(main_path, FileAccess.WRITE)
	if file == null:
		push_error("DevConsole: Failed to open save file for corruption")
		return false

	file.store_string("THIS IS CORRUPTED DATA {{{{{")
	file.close()

	print("DevConsole: Main save corrupted! Reload to test backup recovery.")
	return true

func _cmd_save_info() -> bool:
	print("=== SAVE INFO ===")

	if _repo == null:
		push_error("DevConsole: Repo not available")
		return false

	var snapshot = _repo.snapshot()
	print("Profile ID: %s" % snapshot.get("profile_id", "unknown"))
	print("Version: %d" % snapshot.get("version", 0))
	print("Updated At: %d" % snapshot.get("updated_at", 0))

	if _economy:
		print("Gold: %d" % _economy.get_gold())
		print("Essence: %d" % _economy.get_essence())
		print("Fragments: %d" % _economy.get_fragments())

	if _collection:
		var collection = _collection.list_cards()
		print("Collection Size: %d cards" % collection.size())

		var summary = _collection.get_collection_summary()
		for entry in summary:
			print("  - %s: %d cards (%s)" % [entry.catalog_id, entry.count, entry.rarity])

	if _decks:
		var decks = _decks.list_decks()
		print("Decks: %d" % decks.size())
		for deck in decks:
			var valid = _decks.validate_deck(deck.id)
			print("  - %s (%s): %d cards [%s]" % [
				deck.name,
				deck.id,
				deck.card_instance_ids.size(),
				"VALID" if valid else "INVALID"
			])

	print("=================")
	return true

func _cmd_save_reload() -> bool:
	print("DevConsole: Force reloading save from disk...")

	if _repo == null:
		push_error("DevConsole: Repo not available")
		return false

	var profile_id = _repo.get_current_profile_id()
	var success = _repo.load_profile(profile_id)

	if success:
		print("DevConsole: Save reloaded successfully")
	else:
		print("DevConsole: Save reload failed")

	return success

func _cmd_create_deck(args: Array) -> bool:
	if _decks == null or _collection == null:
		push_error("DevConsole: Decks or Collection service not available")
		return false

	var deck_name = "Test Deck"
	if args.size() > 0:
		deck_name = " ".join(args)

	print("DevConsole: Creating test deck '%s'..." % deck_name)

	# Get 30 random cards from collection
	var collection = _collection.list_cards()
	if collection.size() < 30:
		print("DevConsole: Not enough cards in collection (need 30, have %d)" % collection.size())
		print("DevConsole: Granting 30 cards first...")

		# Grant cards
		var cards_to_grant = []
		for i in range(30):
			var catalog_id = TEST_CARDS[randi() % TEST_CARDS.size()]
			cards_to_grant.append({"catalog_id": catalog_id, "rarity": "common"})
		_collection.grant_cards(cards_to_grant)

		# Refresh collection
		collection = _collection.list_cards()

	# Take first 30 cards
	var card_instance_ids = []
	for i in range(min(30, collection.size())):
		card_instance_ids.append(collection[i].id)

	var deck_id = _decks.create_deck(deck_name, card_instance_ids)

	var is_valid = _decks.validate_deck(deck_id)
	print("DevConsole: Deck created (id: %s) [%s]" % [deck_id, "VALID" if is_valid else "INVALID"])

	return true
