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
##   /snapshot_save <name> - Save current profile state as a snapshot
##   /snapshot_load <name> - Load a profile snapshot
##   /snapshot_list - List all available snapshots
##   /snapshot_delete <name> - Delete a snapshot
##   /unlock_summoner <id> - Unlock a summoner (e.g., summoner_fire, summoner_water)
##   /unlock_all_summoners - Unlock all starting summoners
##   /items_grant <item_id> - Grant an item to inventory
##   /items_grant_all - Grant all starter items
##   /items_list - List player's items and equipment
##   /items_equip <slot> <instance_id> - Equip an item to a summoner
##   /items_clear - Clear all items from inventory
##
## Usage in game:
##   Press F12 to toggle console (future implementation)
##   Or call commands directly: DevConsole.execute_command("/save_info")

## Command registry for autocomplete
const COMMANDS: Array[Dictionary] = [
	# Save Management
	{"cmd": "/save_wipe", "desc": "Reset profile to fresh state"},
	{"cmd": "/save_grant_cards", "args": "<count>", "desc": "Grant N random cards (default: 5)"},
	{"cmd": "/save_add_gold", "args": "<amount>", "desc": "Add gold (default: 100)"},
	{"cmd": "/save_add_essence", "args": "<amount>", "desc": "Add essence (default: 50)"},
	{"cmd": "/save_add_fragments", "args": "<amount>", "desc": "Add fragments (default: 10)"},
	{"cmd": "/save_corrupt", "desc": "Corrupt save file for recovery test"},
	{"cmd": "/save_info", "desc": "Print current save state"},
	{"cmd": "/save_reload", "desc": "Force reload save from disk"},
	{"cmd": "/save_create_deck", "args": "<name>", "desc": "Create a test deck"},
	# Snapshots
	{"cmd": "/snapshot_save", "args": "<name>", "desc": "Save profile snapshot"},
	{"cmd": "/snapshot_load", "args": "<name>", "desc": "Load profile snapshot"},
	{"cmd": "/snapshot_list", "desc": "List all snapshots"},
	{"cmd": "/snapshot_delete", "args": "<name>", "desc": "Delete a snapshot"},
	# Summoners
	{"cmd": "/unlock_summoner", "args": "<id>", "desc": "Unlock a summoner"},
	{"cmd": "/unlock_all_summoners", "desc": "Unlock all starting summoners"},
	# Items
	{"cmd": "/items_grant", "args": "<item_id>", "desc": "Grant an item"},
	{"cmd": "/items_grant_all", "desc": "Grant all starter items"},
	{"cmd": "/items_list", "desc": "List player's items"},
	{"cmd": "/items_equip", "args": "<slot> <id>", "desc": "Equip an item"},
	{"cmd": "/items_clear", "desc": "Clear all items"},
]

## Get commands matching a prefix (for autocomplete)
func get_matching_commands(prefix: String) -> Array[Dictionary]:
	var results: Array[Dictionary] = []
	var lower_prefix: String = prefix.to_lower()
	for cmd_info: Dictionary in COMMANDS:
		var cmd: String = cmd_info.get("cmd", "")
		if cmd.to_lower().begins_with(lower_prefix):
			results.append(cmd_info)
	return results

## Get all commands (for showing full list)
func get_all_commands() -> Array[Dictionary]:
	return COMMANDS

## Available card catalog IDs for testing
const TEST_CARDS: Array = ["fire_elemental", "earth_sprite", "puff", "fireball"]
const TEST_RARITIES: Array = [RarityIDs.COMMON, RarityIDs.COMMON, RarityIDs.COMMON, RarityIDs.RARE, RarityIDs.EPIC]  # Weighted

## Service references (injected by autoload order)
var _repo: Node = null  # ProfileRepo autoload
var _economy: Node = null  # Economy autoload
var _collection: Node = null  # CardServiceCS autoload
var _decks: Node = null  # Decks autoload
var _snapshots: Node = null  # DebugSnapshots autoload

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("DevConsole: Initializing...")

	# Wait for services to be ready
	await get_tree().process_frame

	_repo = ProfileRepo
	_economy = Economy
	_collection = CardServiceCS
	_decks = Decks
	_snapshots = DebugSnapshots

	print("DevConsole: Ready (F12 to open console - future)")

func _input(event: InputEvent) -> void:
	# Future: F12 to toggle console UI
	if event is InputEventKey:
		var key_event: InputEventKey = event
		if key_event.pressed and key_event.keycode == KEY_F12:
			print("DevConsole: F12 pressed (console UI not yet implemented)")

## =============================================================================
## COMMAND EXECUTION
## =============================================================================

## Execute a command string
## Returns: true if command executed successfully
func execute_command(command: String) -> bool:
	var parts: PackedStringArray = command.split(" ", false)
	if parts.size() == 0:
		return false

	var cmd: String = parts[0]
	var args: PackedStringArray = parts.slice(1)

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
		"/snapshot_save":
			return _cmd_snapshot_save(args)
		"/snapshot_load":
			return _cmd_snapshot_load(args)
		"/snapshot_list":
			return _cmd_snapshot_list()
		"/snapshot_delete":
			return _cmd_snapshot_delete(args)
		"/unlock_summoner":
			return _cmd_unlock_summoner(args)
		"/unlock_all_summoners":
			return _cmd_unlock_all_summoners()
		"/items_grant":
			return _cmd_items_grant(args)
		"/items_grant_all":
			return _cmd_items_grant_all()
		"/items_list":
			return _cmd_items_list()
		"/items_equip":
			return _cmd_items_equip(args)
		"/items_clear":
			return _cmd_items_clear()
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

	_repo.call("reset_profile")
	print("DevConsole: Save wiped, fresh profile created")
	return true

func _cmd_grant_cards(args: PackedStringArray) -> bool:
	if _collection == null:
		push_error("DevConsole: Collection service not available")
		return false

	var count: int = 5  # Default
	if args.size() > 0:
		count = int(args[0])

	print("DevConsole: Granting %d random cards..." % count)

	var cards_to_grant: Array = []
	for i: int in range(count):
		var catalog_id: String = TEST_CARDS[randi() % TEST_CARDS.size()]
		var rarity: String = TEST_RARITIES[randi() % TEST_RARITIES.size()]
		var card_grant: Dictionary = {"catalog_id": catalog_id, "rarity": rarity}
		cards_to_grant.append(card_grant)

	var instance_ids: Array = _collection.call("grant_cards", cards_to_grant)
	print("DevConsole: Granted %d cards (instance IDs: %s)" % [instance_ids.size(), str(instance_ids)])

	return true

func _cmd_add_gold(args: PackedStringArray) -> bool:
	if _economy == null:
		push_error("DevConsole: Economy service not available")
		return false

	var amount: int = 100  # Default
	if args.size() > 0:
		amount = int(args[0])

	print("DevConsole: Adding %d gold..." % amount)
	_economy.call("add_gold", amount)
	var current_gold: int = _economy.call("get_gold")
	print("DevConsole: Gold added (current: %d)" % current_gold)

	return true

func _cmd_add_essence(args: PackedStringArray) -> bool:
	if _economy == null:
		push_error("DevConsole: Economy service not available")
		return false

	var amount: int = 50  # Default
	if args.size() > 0:
		amount = int(args[0])

	print("DevConsole: Adding %d essence..." % amount)
	_economy.call("add_essence", amount)
	var current_essence: int = _economy.call("get_essence")
	print("DevConsole: Essence added (current: %d)" % current_essence)

	return true

func _cmd_add_fragments(args: PackedStringArray) -> bool:
	if _economy == null:
		push_error("DevConsole: Economy service not available")
		return false

	var amount: int = 10  # Default
	if args.size() > 0:
		amount = int(args[0])

	print("DevConsole: Adding %d fragments..." % amount)
	_economy.call("add_fragments", amount)
	var current_fragments: int = _economy.call("get_fragments")
	print("DevConsole: Fragments added (current: %d)" % current_fragments)

	return true

func _cmd_corrupt_save() -> bool:
	print("DevConsole: Corrupting main save file for recovery test...")

	if _repo == null:
		push_error("DevConsole: Repo not available")
		return false

	var profile_id: String = _repo.call("get_current_profile_id")
	var profile_dir: String = "user://profiles/" + profile_id
	var main_path: String = profile_dir + "/profile.json"

	# Write garbage to main save
	var file: FileAccess = FileAccess.open(main_path, FileAccess.WRITE)
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

	var snapshot: Dictionary = _repo.call("snapshot")
	print("Profile ID: %s" % snapshot.get("profile_id", "unknown"))
	print("Version: %d" % snapshot.get("version", 0))
	print("Updated At: %d" % snapshot.get("updated_at", 0))

	if _economy:
		var gold: int = _economy.call("get_gold")
		var essence: int = _economy.call("get_essence")
		var fragments: int = _economy.call("get_fragments")
		print("Gold: %d" % gold)
		print("Essence: %d" % essence)
		print("Fragments: %d" % fragments)

	if _collection:
		var collection: Array = _collection.call("list_cards")
		print("Collection Size: %d cards" % collection.size())

		var summary: Array = _collection.call("get_collection_summary")
		for entry: Dictionary in summary:
			print("  - %s: %d cards (%s)" % [entry.catalog_id, entry.count, entry.rarity])

	if _decks:
		var decks: Array = _decks.call("list_decks")
		print("Decks: %d" % decks.size())
		for deck: Dictionary in decks:
			var valid: bool = _decks.call("validate_deck", deck.id)
			var card_ids: Variant = deck.get("card_instance_ids", [])
			var card_ids_size: int = 0
			if card_ids is Array:
				var card_ids_array: Array = card_ids
				card_ids_size = card_ids_array.size()
			print("  - %s (%s): %d cards [%s]" % [
				deck.name,
				deck.id,
				card_ids_size,
				"VALID" if valid else "INVALID"
			])

	print("=================")
	return true

func _cmd_save_reload() -> bool:
	print("DevConsole: Force reloading save from disk...")

	if _repo == null:
		push_error("DevConsole: Repo not available")
		return false

	var profile_id: String = _repo.call("get_current_profile_id")
	var success: bool = _repo.call("load_profile", profile_id)

	if success:
		print("DevConsole: Save reloaded successfully")
	else:
		print("DevConsole: Save reload failed")

	return success

func _cmd_create_deck(args: PackedStringArray) -> bool:
	if _decks == null or _collection == null:
		push_error("DevConsole: Decks or Collection service not available")
		return false

	var deck_name: String = "Test Deck"
	if args.size() > 0:
		deck_name = " ".join(args)

	print("DevConsole: Creating test deck '%s'..." % deck_name)

	# Get 30 random cards from collection
	var collection: Array = _collection.call("list_cards")
	if collection.size() < 30:
		print("DevConsole: Not enough cards in collection (need 30, have %d)" % collection.size())
		print("DevConsole: Granting 30 cards first...")

		# Grant cards
		var cards_to_grant: Array = []
		for i: int in range(30):
			var catalog_id: String = TEST_CARDS[randi() % TEST_CARDS.size()]
			var card_grant: Dictionary = {"catalog_id": catalog_id, "rarity": RarityIDs.COMMON}
			cards_to_grant.append(card_grant)
		_collection.call("grant_cards", cards_to_grant)

		# Refresh collection
		collection = _collection.call("list_cards")

	# Take first 30 cards
	var card_instance_ids: Array[String] = []
	for i: int in range(min(30, collection.size())):
		var card_dict_variant: Variant = collection[i]
		if not card_dict_variant is Dictionary:
			push_error("DevConsole: collection[%d] is not a Dictionary" % i)
			continue
		var card_dict: Dictionary = card_dict_variant
		var card_id: String = card_dict.get("id", "")
		card_instance_ids.append(card_id)

	var deck_id: String = _decks.call("create_deck", deck_name, card_instance_ids)

	var is_valid: bool = _decks.call("validate_deck", deck_id)
	print("DevConsole: Deck created (id: %s) [%s]" % [deck_id, "VALID" if is_valid else "INVALID"])

	return true

func _cmd_snapshot_save(args: PackedStringArray) -> bool:
	if _snapshots == null:
		push_error("DevConsole: DebugSnapshots service not available")
		return false

	if args.size() == 0:
		print("DevConsole: Usage: /snapshot_save <name>")
		return false

	var snapshot_name: String = " ".join(args)
	print("DevConsole: Saving snapshot '%s'..." % snapshot_name)

	var success: bool = _snapshots.call("save_snapshot", snapshot_name)
	if success:
		print("DevConsole: Snapshot saved successfully")
	else:
		print("DevConsole: Failed to save snapshot")

	return success

func _cmd_snapshot_load(args: PackedStringArray) -> bool:
	if _snapshots == null:
		push_error("DevConsole: DebugSnapshots service not available")
		return false

	if args.size() == 0:
		print("DevConsole: Usage: /snapshot_load <name>")
		return false

	var snapshot_name: String = " ".join(args)
	print("DevConsole: Loading snapshot '%s'..." % snapshot_name)

	var success: bool = _snapshots.call("load_snapshot", snapshot_name)
	if success:
		print("DevConsole: Snapshot loaded successfully")
	else:
		print("DevConsole: Failed to load snapshot")

	return success

func _cmd_snapshot_list() -> bool:
	if _snapshots == null:
		push_error("DevConsole: DebugSnapshots service not available")
		return false

	print("=== AVAILABLE SNAPSHOTS ===")

	var snapshots: Array = _snapshots.call("list_snapshots")
	if snapshots.size() == 0:
		print("No snapshots found")
		return true

	for snapshot: Dictionary in snapshots:
		print("  - %s (created: %s)" % [snapshot.name, snapshot.created_at])

	print("===========================")
	return true

func _cmd_snapshot_delete(args: PackedStringArray) -> bool:
	if _snapshots == null:
		push_error("DevConsole: DebugSnapshots service not available")
		return false

	if args.size() == 0:
		print("DevConsole: Usage: /snapshot_delete <name>")
		return false

	var snapshot_name: String = " ".join(args)
	print("DevConsole: Deleting snapshot '%s'..." % snapshot_name)

	var success: bool = _snapshots.call("delete_snapshot", snapshot_name)
	if success:
		print("DevConsole: Snapshot deleted successfully")
	else:
		print("DevConsole: Failed to delete snapshot (may not exist)")

	return success

func _cmd_unlock_summoner(args: PackedStringArray) -> bool:
	if _repo == null:
		push_error("DevConsole: ProfileRepo not available")
		return false

	if args.size() == 0:
		print("DevConsole: Usage: /unlock_summoner <summoner_id>")
		print("DevConsole: Valid IDs: summoner_fire, summoner_water, summoner_wind, summoner_earth, summoner_shadow_initiate")
		return false

	var summoner_id: String = args[0]
	print("DevConsole: Unlocking summoner '%s'..." % summoner_id)

	# Check if valid summoner
	if not SummonerIDs.is_valid(summoner_id):
		print("DevConsole: Invalid summoner ID: %s" % summoner_id)
		print("DevConsole: Valid IDs: summoner_fire, summoner_water, summoner_wind, summoner_earth, summoner_shadow_initiate")
		return false

	# Create summoner instance data
	var summoner_data: Dictionary = {
		"summoner_id": summoner_id,
		"level": 1,
		"xp": 0.0,
		"acquired_boon_ids": []
	}

	# Save the instance (this also adds to unlocked_summoners)
	_repo.call("save_summoner_instance", summoner_data)
	print("DevConsole: Summoner '%s' unlocked!" % summoner_id)

	return true

func _cmd_unlock_all_summoners() -> bool:
	if _repo == null:
		push_error("DevConsole: ProfileRepo not available")
		return false

	print("DevConsole: Unlocking all starting summoners...")

	var summoners_to_unlock: Array[StringName] = SummonerIDs.ALL_STARTING.duplicate()
	summoners_to_unlock.append(SummonerIDs.SHADOW_INITIATE)

	var unlocked_count: int = 0
	for summoner_id: StringName in summoners_to_unlock:
		# Check if already unlocked
		if _repo.call("is_summoner_unlocked", String(summoner_id)):
			print("DevConsole: %s already unlocked, skipping" % summoner_id)
			continue

		var summoner_data: Dictionary = {
			"summoner_id": String(summoner_id),
			"level": 1,
			"xp": 0.0,
			"acquired_boon_ids": []
		}
		_repo.call("save_summoner_instance", summoner_data)
		unlocked_count += 1
		print("DevConsole: Unlocked %s" % summoner_id)

	print("DevConsole: Unlocked %d summoners!" % unlocked_count)
	return true


## =============================================================================
## ITEM COMMANDS
## =============================================================================

## Item IDs for testing
const TEST_ITEMS: Array[String] = [
	"item_training_blade",
	"item_simple_ring",
	"item_lucky_band",
	"item_travelers_cloak",
	"item_veterans_medal",
	"item_battle_hardened_badge",
	"item_fortunes_charm",
	"item_bold_fortune_amulet"
]


func _cmd_items_grant(args: PackedStringArray) -> bool:
	if args.size() == 0:
		print("DevConsole: Usage: /items_grant <item_id>")
		print("DevConsole: Available items:")
		for item_id: String in TEST_ITEMS:
			print("  - %s" % item_id)
		return false

	var item_id: String = args[0]
	print("DevConsole: Granting item '%s'..." % item_id)

	var instance_id: String = Items.grant_item(item_id)
	if instance_id.is_empty():
		print("DevConsole: Failed to grant item (invalid item_id?)")
		return false

	print("DevConsole: Granted item! Instance ID: %s" % instance_id)
	return true


func _cmd_items_grant_all() -> bool:
	print("DevConsole: Granting all starter items...")

	var granted_count: int = 0
	for item_id: String in TEST_ITEMS:
		var instance_id: String = Items.grant_item(item_id)
		if not instance_id.is_empty():
			print("  Granted: %s -> %s" % [item_id, instance_id])
			granted_count += 1
		else:
			print("  FAILED: %s" % item_id)

	print("DevConsole: Granted %d items!" % granted_count)
	return true


func _cmd_items_list() -> bool:
	print("=== PLAYER ITEMS ===")

	var summoner_id: String = SummonerSelection.GetActiveSummonerId()
	if summoner_id.is_empty():
		print("No active summoner - select a summoner first")
		print("====================")
		return true

	# Get equipped items
	var equipped: Dictionary = Items.get_equipped_items(summoner_id)
	print("Equipped on %s:" % summoner_id)
	for slot: String in Items.ALL_SLOTS:
		var instance_id: String = equipped.get(slot, "")
		if instance_id.is_empty():
			print("  [%s]: (empty)" % slot)
		else:
			print("  [%s]: %s" % [slot, instance_id])

	# Get all items for each slot
	print("\nAvailable items:")
	for slot: String in Items.ALL_SLOTS:
		var items: Array[Dictionary] = Items.list_items_for_slot(slot, summoner_id)
		print("  %s slot: %d items" % [slot, items.size()])
		for item: Dictionary in items:
			var name_key: String = item.get("name_key", "")
			var item_name: String = Loc.t(name_key) if not name_key.is_empty() else item.get("id", "?")
			var is_equipped: String = " [EQUIPPED]" if item.get("equipped_by", "") == summoner_id else ""
			print("    - %s (%s)%s" % [item_name, item.get("instance_id", "?"), is_equipped])

	print("====================")
	return true


func _cmd_items_equip(args: PackedStringArray) -> bool:
	if args.size() < 2:
		print("DevConsole: Usage: /items_equip <slot> <instance_id>")
		print("DevConsole: Slots: weapon, ring1, ring2, vestments")
		print("DevConsole: Use /items_list to see available instance IDs")
		return false

	var summoner_id: String = SummonerSelection.GetActiveSummonerId()
	if summoner_id.is_empty():
		print("DevConsole: No active summoner - select a summoner first")
		return false

	var slot: String = args[0].to_lower()
	var instance_id: String = args[1]

	print("DevConsole: Equipping item '%s' to %s's %s slot..." % [instance_id, summoner_id, slot])

	var success: bool = Items.equip_item(summoner_id, instance_id, slot)
	if success:
		print("DevConsole: Item equipped successfully!")
	else:
		print("DevConsole: Failed to equip item (check slot/instance_id)")

	return success


func _cmd_items_clear() -> bool:
	print("DevConsole: Clearing all items...")
	Items.clear_all_items()
	print("DevConsole: All items cleared!")
	return true
