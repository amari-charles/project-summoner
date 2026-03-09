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
##   /unlock_summoner <id> - Unlock a summoner (e.g., summoner_cole, summoner_selene)
##   /unlock_all_summoners - Unlock all starting summoners
##   /traits_catalog - List trait catalog IDs
##   /traits_list_summoner_options [summoner_id] - List spendable summoner traits
##   /traits_grant_summoner_points <amount> [summoner_id] - Grant summoner trait points
##   /traits_spend_summoner <trait_id> [summoner_id] - Spend a summoner trait point
##   /traits_show_summoner_stats [summoner_id] - Print computed summoner stats
##   /traits_list_cards - List card instance IDs + trait state
##   /traits_list_card_options <card_instance_id> - List spendable card traits
##   /traits_grant_card_points <card_instance_id> <amount> - Grant card trait points
##   /traits_spend_card <card_instance_id> <trait_id> - Spend a card trait point
##   /traits_runtime_status - Print simulation trait runtime status
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
	# Traits
	{"cmd": "/traits_catalog", "desc": "List trait catalog IDs"},
	{"cmd": "/traits_list_summoner_options", "args": "[summoner_id]", "desc": "List spendable summoner traits"},
	{"cmd": "/traits_grant_summoner_points", "args": "<amount> [summoner_id]", "desc": "Grant summoner trait points"},
	{"cmd": "/traits_spend_summoner", "args": "<trait_id> [summoner_id]", "desc": "Spend summoner trait point"},
	{"cmd": "/traits_show_summoner_stats", "args": "[summoner_id]", "desc": "Print computed summoner stats"},
	{"cmd": "/traits_list_cards", "desc": "List cards with trait state"},
	{"cmd": "/traits_list_card_options", "args": "<card_instance_id>", "desc": "List spendable card traits"},
	{"cmd": "/traits_grant_card_points", "args": "<card_instance_id> <amount>", "desc": "Grant card trait points"},
	{"cmd": "/traits_spend_card", "args": "<card_instance_id> <trait_id>", "desc": "Spend card trait point"},
	{"cmd": "/traits_runtime_status", "desc": "Print simulation trait runtime status"},
	{"cmd": "/traits_units_snapshot", "args": "[team]", "desc": "Print live spawned unit stats from simulation"},
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
const TEST_CARDS: Array = [
	CardIDs.FIRE_WISP,
	CardIDs.FIRE_BOAR,
	CardIDs.FIRE_WOLF,
	CardIDs.PEBBLOOM,
	CardIDs.EARTH_KOMODO_DRAGON,
	CardIDs.PUFF,
	CardIDs.MANA_BOLT,
]
const TEST_RARITIES: Array = [RarityIDs.COMMON, RarityIDs.COMMON, RarityIDs.COMMON, RarityIDs.RARE, RarityIDs.EPIC]  # Weighted

## Service references (injected by autoload order)
var _economy: Object = null  # Economy autoload (C# service)
var _decks: Object = null  # Decks autoload (C# service)
var _snapshots: Node = null  # DebugSnapshots autoload

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("DevConsole: Initializing...")

	# Wait for services to be ready
	await get_tree().process_frame

	_economy = Economy
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
		"/traits_catalog":
			return _cmd_traits_catalog()
		"/traits_list_summoner_options":
			return _cmd_traits_list_summoner_options(args)
		"/traits_grant_summoner_points":
			return _cmd_traits_grant_summoner_points(args)
		"/traits_spend_summoner":
			return _cmd_traits_spend_summoner(args)
		"/traits_show_summoner_stats":
			return _cmd_traits_show_summoner_stats(args)
		"/traits_list_cards":
			return _cmd_traits_list_cards()
		"/traits_list_card_options":
			return _cmd_traits_list_card_options(args)
		"/traits_grant_card_points":
			return _cmd_traits_grant_card_points(args)
		"/traits_spend_card":
			return _cmd_traits_spend_card(args)
		"/traits_runtime_status":
			return _cmd_traits_runtime_status()
		"/traits_units_snapshot":
			return _cmd_traits_units_snapshot(args)
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

	ProfileRepoApi.reset_profile()
	print("DevConsole: Save wiped, fresh profile created")
	print("DevConsole: Returning to title screen...")
	SceneManager.transition_to(SceneManager.SCENE_TITLE_SCREEN)
	return true

func _cmd_grant_cards(args: PackedStringArray) -> bool:
	var count: int = 5  # Default
	if args.size() > 0:
		var count_arg: String = args[0]
		count = count_arg.to_int()

	print("DevConsole: Granting %d random cards..." % count)

	var cards_to_grant: Array = []
	for i: int in range(count):
		var catalog_id: String = TEST_CARDS[randi() % TEST_CARDS.size()]
		var rarity: String = TEST_RARITIES[randi() % TEST_RARITIES.size()]
		var card_grant: Dictionary = {"catalog_id": catalog_id, "rarity": rarity}
		cards_to_grant.append(card_grant)

	# Use PascalCase for C# method and correct method name for array input
	var instance_ids: Array = CardServiceApi.grant_cards_from_array(cards_to_grant)
	print("DevConsole: Granted %d cards (instance IDs: %s)" % [instance_ids.size(), str(instance_ids)])

	return true

func _cmd_add_gold(args: PackedStringArray) -> bool:
	if _economy == null:
		push_error("DevConsole: Economy service not available")
		return false

	var amount: int = 100  # Default
	if args.size() > 0:
		var amount_arg: String = args[0]
		amount = amount_arg.to_int()

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
		var amount_arg: String = args[0]
		amount = amount_arg.to_int()

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
		var amount_arg: String = args[0]
		amount = amount_arg.to_int()

	print("DevConsole: Adding %d fragments..." % amount)
	_economy.call("add_fragments", amount)
	var current_fragments: int = _economy.call("get_fragments")
	print("DevConsole: Fragments added (current: %d)" % current_fragments)

	return true

func _cmd_corrupt_save() -> bool:
	print("DevConsole: Corrupting main save file for recovery test...")

	var profile_id: String = ProfileRepoApi.get_current_profile_id()
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

	var snapshot: Dictionary = ProfileRepoApi.snapshot()
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

	var collection: Array = CardServiceApi.list_cards_dict()
	print("Collection Size: %d cards" % collection.size())

	var summary: Array = CardServiceApi.get_collection_summary_dict()
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

	var profile_id: String = ProfileRepoApi.get_current_profile_id()
	var success: bool = ProfileRepoApi.load_profile(profile_id)

	if success:
		print("DevConsole: Save reloaded successfully")
	else:
		print("DevConsole: Save reload failed")

	return success

func _cmd_create_deck(args: PackedStringArray) -> bool:
	if _decks == null:
		push_error("DevConsole: Decks service not available")
		return false

	var deck_name: String = "Test Deck"
	if args.size() > 0:
		deck_name = " ".join(args)

	print("DevConsole: Creating test deck '%s'..." % deck_name)

	# Get 30 random cards from collection
	var collection: Array = CardServiceApi.list_cards_dict()
	if collection.size() < 30:
		print("DevConsole: Not enough cards in collection (need 30, have %d)" % collection.size())
		print("DevConsole: Granting 30 cards first...")

		# Grant cards
		var cards_to_grant: Array = []
		for i: int in range(30):
			var catalog_id: String = TEST_CARDS[randi() % TEST_CARDS.size()]
			var card_grant: Dictionary = {"catalog_id": catalog_id, "rarity": String(RarityIDs.COMMON)}
			cards_to_grant.append(card_grant)
		CardServiceApi.grant_cards_from_array(cards_to_grant)

		# Refresh collection
		collection = CardServiceApi.list_cards_dict()

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
	if args.size() == 0:
		print("DevConsole: Usage: /unlock_summoner <summoner_id>")
		print("DevConsole: Valid IDs: summoner_cole, summoner_selene, summoner_mei, summoner_teo")
		return false

	var summoner_id: String = args[0]
	print("DevConsole: Unlocking summoner '%s'..." % summoner_id)

	# Check if valid summoner
	if not SummonerIDs.is_valid(summoner_id):
		print("DevConsole: Invalid summoner ID: %s" % summoner_id)
		print("DevConsole: Valid IDs: summoner_cole, summoner_selene, summoner_mei, summoner_teo")
		return false

	# Create summoner instance data
	var summoner_data: Dictionary = {
		"summoner_id": summoner_id,
		"level": 1,
		"xp": 0.0
	}

	# Save the instance (this also adds to unlocked_summoners)
	SummonerSelectionApi.save_summoner_instance_dict(summoner_data)
	print("DevConsole: Summoner '%s' unlocked!" % summoner_id)

	return true

func _cmd_unlock_all_summoners() -> bool:
	print("DevConsole: Unlocking all starting summoners...")

	var summoners_to_unlock: Array[StringName] = SummonerIDs.ALL_STARTING.duplicate()

	var unlocked_count: int = 0
	for summoner_id: StringName in summoners_to_unlock:
		var summoner_id_str: String = str(summoner_id)
		# Check if already unlocked
		if SummonerSelectionApi.is_summoner_unlocked(summoner_id_str):
			print("DevConsole: %s already unlocked, skipping" % summoner_id)
			continue

		var summoner_data: Dictionary = {
			"summoner_id": summoner_id_str,
			"level": 1,
			"xp": 0.0
		}
		SummonerSelectionApi.save_summoner_instance_dict(summoner_data)
		unlocked_count += 1
		print("DevConsole: Unlocked %s" % summoner_id)

	print("DevConsole: Unlocked %d summoners!" % unlocked_count)
	return true


## =============================================================================
## TRAIT COMMANDS
## =============================================================================

func _cmd_traits_catalog() -> bool:
	var trait_ids: Array = SafeTypeUtils.array(TraitCatalog.call("GetAllTraitIds"))
	print("=== TRAIT CATALOG (%d) ===" % trait_ids.size())
	for trait_id_var: Variant in trait_ids:
		var trait_id: String = SafeTypeUtils.string(trait_id_var, "")
		if trait_id.is_empty():
			continue
		var name: String = TraitCatalogApi.get_trait_name(trait_id)
		print("  - %s :: %s" % [trait_id, name])
	print("==========================")
	return true


func _cmd_traits_list_summoner_options(args: PackedStringArray) -> bool:
	var summoner_id: String = _resolve_summoner_id_arg(args, 0)
	if summoner_id.is_empty():
		return false

	var unspent: int = SummonerProgressionApi.get_unspent_trait_points(summoner_id)
	var options: Array[Dictionary] = _get_spendable_summoner_traits(summoner_id)

	print("=== SUMMONER TRAIT OPTIONS ===")
	print("Summoner: %s" % summoner_id)
	print("Unspent points: %d" % unspent)
	if options.is_empty():
		print("No spendable summoner traits found at current level/prereqs.")
		print("==============================")
		return true

	for trait_dict: Dictionary in options:
		var trait_id: String = SafeTypeUtils.string(trait_dict.get("id", ""), "")
		var min_level: int = SafeTypeUtils.int_val(trait_dict.get("min_level", 1), 1)
		var name: String = TraitCatalogApi.get_trait_name(trait_id)
		print("  - %s (min=%d) :: %s" % [trait_id, min_level, name])
	print("==============================")
	return true


func _cmd_traits_grant_summoner_points(args: PackedStringArray) -> bool:
	if args.size() < 1:
		print("DevConsole: Usage: /traits_grant_summoner_points <amount> [summoner_id]")
		return false

	var amount: int = args[0].to_int()
	if amount <= 0:
		print("DevConsole: amount must be > 0")
		return false

	var summoner_id: String = _resolve_summoner_id_arg(args, 1)
	if summoner_id.is_empty():
		return false

	var new_total: int = SafeTypeUtils.int_val(
		SummonerProgression.call("GrantTraitPoints", summoner_id, amount, "dev_console"), 0)
	print("DevConsole: Granted %d summoner trait point(s) to %s (unspent now: %d)" % [
		amount, summoner_id, new_total
	])
	return true


func _cmd_traits_spend_summoner(args: PackedStringArray) -> bool:
	if args.size() < 1:
		print("DevConsole: Usage: /traits_spend_summoner <trait_id> [summoner_id]")
		return false

	var trait_id: String = args[0]
	var summoner_id: String = _resolve_summoner_id_arg(args, 1)
	if summoner_id.is_empty():
		return false

	var before_points: int = SummonerProgressionApi.get_unspent_trait_points(summoner_id)
	var success: bool = SummonerProgressionApi.spend_trait_point(summoner_id, trait_id)
	var after_points: int = SummonerProgressionApi.get_unspent_trait_points(summoner_id)

	if success:
		print("DevConsole: Spent summoner trait point: %s -> %s (%d -> %d points)" % [
			summoner_id, trait_id, before_points, after_points
		])
	else:
		print("DevConsole: FAILED spending summoner trait point: %s -> %s (%d points)" % [
			summoner_id, trait_id, before_points
		])
	return success


func _cmd_traits_show_summoner_stats(args: PackedStringArray) -> bool:
	var summoner_id: String = _resolve_summoner_id_arg(args, 0)
	if summoner_id.is_empty():
		return false

	var stats: Dictionary = SummonerProgressionApi.get_computed_stats_for_summoner(summoner_id)
	if stats.is_empty():
		print("DevConsole: No computed stats found for summoner '%s'" % summoner_id)
		return false

	print("=== SUMMONER STATS (%s) ===" % summoner_id)
	var keys: Array = stats.keys()
	keys.sort()
	for key_var: Variant in keys:
		var key: String = SafeTypeUtils.string(key_var, "")
		print("  - %s: %s" % [key, str(stats.get(key, 0.0))])
	print("===========================")
	return true


func _cmd_traits_list_cards() -> bool:
	var cards: Array = CardServiceApi.list_cards_dict()
	print("=== CARD TRAIT STATE (%d cards) ===" % cards.size())
	for card_var: Variant in cards:
		if not card_var is Dictionary:
			continue
		var card: Dictionary = card_var
		var instance_id: String = SafeTypeUtils.string(card.get("id", ""), "")
		var catalog_id: String = SafeTypeUtils.string(card.get("catalog_id", ""), "")
		var level: int = SafeTypeUtils.int_val(card.get("level", 1), 1)
		var points: int = CardServiceApi.get_unspent_trait_points(instance_id)
		var traits: Array = CardServiceApi.get_applied_traits(instance_id)
		print("  - %s :: %s (lvl=%d points=%d traits=%s)" % [
			instance_id, catalog_id, level, points, str(traits)
		])
	print("===================================")
	return true


func _cmd_traits_list_card_options(args: PackedStringArray) -> bool:
	if args.size() < 1:
		print("DevConsole: Usage: /traits_list_card_options <card_instance_id>")
		return false

	var instance_id: String = args[0]
	var info: Dictionary = CardServiceApi.get_card_progression_info_dict(instance_id)
	if info.is_empty():
		print("DevConsole: Card not found: %s" % instance_id)
		return false

	var options: Array[Dictionary] = _get_spendable_card_traits(instance_id)
	var points: int = CardServiceApi.get_unspent_trait_points(instance_id)
	print("=== CARD TRAIT OPTIONS ===")
	print("Card: %s (%s)" % [instance_id, info.get("catalog_id", "")])
	print("Unspent points: %d" % points)
	if options.is_empty():
		print("No spendable card traits found at current level/prereqs.")
		print("==========================")
		return true

	for trait_dict: Dictionary in options:
		var trait_id: String = SafeTypeUtils.string(trait_dict.get("id", ""), "")
		var min_level: int = SafeTypeUtils.int_val(trait_dict.get("min_level", 1), 1)
		var name: String = TraitCatalogApi.get_trait_name(trait_id)
		print("  - %s (min=%d) :: %s" % [trait_id, min_level, name])
	print("==========================")
	return true


func _cmd_traits_grant_card_points(args: PackedStringArray) -> bool:
	if args.size() < 2:
		print("DevConsole: Usage: /traits_grant_card_points <card_instance_id> <amount>")
		return false

	var instance_id: String = args[0]
	var amount: int = args[1].to_int()
	if amount <= 0:
		print("DevConsole: amount must be > 0")
		return false

	var card_service: Node = get_tree().root.get_node_or_null(CSharpAutoloads.CARD_SERVICE)
	if card_service == null:
		print("DevConsole: CardService autoload not available")
		return false

	var new_total: int = SafeTypeUtils.int_val(
		card_service.call("GrantCardTraitPoints", instance_id, amount, "dev_console"), 0)
	print("DevConsole: Granted %d card trait point(s) to %s (unspent now: %d)" % [
		amount, instance_id, new_total
	])
	return true


func _cmd_traits_spend_card(args: PackedStringArray) -> bool:
	if args.size() < 2:
		print("DevConsole: Usage: /traits_spend_card <card_instance_id> <trait_id>")
		return false

	var instance_id: String = args[0]
	var trait_id: String = args[1]
	var before_points: int = CardServiceApi.get_unspent_trait_points(instance_id)
	var success: bool = CardServiceApi.spend_trait_point(instance_id, trait_id)
	var after_points: int = CardServiceApi.get_unspent_trait_points(instance_id)

	if success:
		print("DevConsole: Spent card trait point: %s -> %s (%d -> %d points)" % [
			instance_id, trait_id, before_points, after_points
		])
	else:
		print("DevConsole: FAILED spending card trait point: %s -> %s (%d points)" % [
			instance_id, trait_id, before_points
		])
	return success


func _cmd_traits_runtime_status() -> bool:
	var sim_node: Node = get_tree().get_first_node_in_group("simulation_node")
	if sim_node == null:
		print("DevConsole: No active SimulationNode (enter a battle first).")
		return false

	if not sim_node.has_method("GetTraitRuntimeStatus"):
		print("DevConsole: SimulationNode lacks GetTraitRuntimeStatus().")
		return false

	var status: Dictionary = SafeTypeUtils.dict(sim_node.call("GetTraitRuntimeStatus"))
	print("=== TRAIT RUNTIME STATUS ===")
	print("ruleset_version: %s" % SafeTypeUtils.string(status.get("ruleset_version", ""), ""))
	print("is_stub: %s" % str(SafeTypeUtils.bool_val(status.get("is_stub", true), true)))
	print("diagnostic_count: %d" % SafeTypeUtils.int_val(status.get("diagnostic_count", 0), 0))

	var diagnostics: Array = SafeTypeUtils.array(status.get("diagnostics", []))
	for diag_var: Variant in diagnostics:
		if not diag_var is Dictionary:
			continue
		var diag: Dictionary = diag_var
		print("  - [%s] %s :: %s" % [
			SafeTypeUtils.string(diag.get("severity", ""), ""),
			SafeTypeUtils.string(diag.get("code", ""), ""),
			SafeTypeUtils.string(diag.get("message", ""), "")
		])
	print("============================")
	return true


func _cmd_traits_units_snapshot(args: PackedStringArray) -> bool:
	var sim_node: Node = get_tree().get_first_node_in_group("simulation_node")
	if sim_node == null:
		print("DevConsole: No active SimulationNode (enter a battle first).")
		return false

	if not sim_node.has_method("GetUnitStatsSnapshot"):
		print("DevConsole: SimulationNode lacks GetUnitStatsSnapshot().")
		return false

	var team_filter: int = -1
	if args.size() > 0:
		team_filter = args[0].to_int()

	var units: Array = SafeTypeUtils.array(sim_node.call("GetUnitStatsSnapshot", team_filter))
	print("=== UNIT STATS SNAPSHOT (%d units, team_filter=%d) ===" % [units.size(), team_filter])
	for unit_var: Variant in units:
		if not unit_var is Dictionary:
			continue
		var unit: Dictionary = unit_var
		print("  - unit=%s net=%s team=%s catalog=%s hp=%s/%s ad=%s as=%s ms=%s range=%s alive=%s" % [
			str(unit.get("unit_id", "?")),
			str(unit.get("network_id", "?")),
			str(unit.get("team", "?")),
			str(unit.get("catalog_id", "?")),
			str(unit.get("current_hp", "?")),
			str(unit.get("max_hp", "?")),
			str(unit.get("attack_damage", "?")),
			str(unit.get("attack_speed", "?")),
			str(unit.get("move_speed", "?")),
			str(unit.get("attack_range", "?")),
			str(unit.get("is_alive", "?"))
		])
	print("======================================================")
	return true


func _resolve_summoner_id_arg(args: PackedStringArray, index: int) -> String:
	if args.size() > index:
		return args[index]
	var active: String = SummonerSelectionApi.get_active_summoner_id()
	if active.is_empty():
		print("DevConsole: No active summoner. Pass summoner_id explicitly.")
	return active


func _get_spendable_summoner_traits(summoner_id: String) -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	var progression_info: Dictionary = SummonerProgressionApi.get_summoner_progression_info(summoner_id)
	if progression_info.is_empty():
		return result

	var level: int = SafeTypeUtils.int_val(progression_info.get("level", 1), 1)
	var owned_traits: Array = SummonerProgressionApi.get_all_trait_ids_for_summoner(summoner_id)
	var all_trait_ids: Array = SafeTypeUtils.array(TraitCatalog.call("GetAllTraitIds"))

	for trait_id_var: Variant in all_trait_ids:
		var trait_id: String = SafeTypeUtils.string(trait_id_var, "")
		if trait_id.is_empty():
			continue
		if owned_traits.has(trait_id):
			continue

		var trait_dict: Dictionary = SafeTypeUtils.dict(TraitCatalog.call("GetTrait", trait_id))
		if trait_dict.is_empty():
			continue
		if SafeTypeUtils.bool_val(trait_dict.get("is_innate", true), true):
			continue
		if not _trait_has_tag(trait_dict, "summoner"):
			continue
		if not _trait_matches_level(trait_dict, level):
			continue
		if not SafeTypeUtils.bool_val(TraitCatalog.call("MeetsPrerequisites", trait_id, owned_traits), false):
			continue

		result.append(trait_dict)

	return result


func _get_spendable_card_traits(card_instance_id: String) -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	var card_info: Dictionary = CardServiceApi.get_card_progression_info_dict(card_instance_id)
	if card_info.is_empty():
		return result

	var catalog_id: String = SafeTypeUtils.string(card_info.get("catalog_id", ""), "")
	var card_def: Dictionary = CardCatalogApi.get_card_as_dict(catalog_id)
	var card_type: int = SafeTypeUtils.int_val(card_def.get("card_type", 0), 0)
	var owner_tag: String = "spell" if card_type == 1 else "summon"
	var level: int = SafeTypeUtils.int_val(card_info.get("level", 1), 1)
	var owned_traits: Array = CardServiceApi.get_applied_traits(card_instance_id)
	var all_trait_ids: Array = SafeTypeUtils.array(TraitCatalog.call("GetAllTraitIds"))

	for trait_id_var: Variant in all_trait_ids:
		var trait_id: String = SafeTypeUtils.string(trait_id_var, "")
		if trait_id.is_empty():
			continue
		if owned_traits.has(trait_id):
			continue

		var trait_dict: Dictionary = SafeTypeUtils.dict(TraitCatalog.call("GetTrait", trait_id))
		if trait_dict.is_empty():
			continue
		if SafeTypeUtils.bool_val(trait_dict.get("is_innate", true), true):
			continue
		if not _trait_has_tag(trait_dict, owner_tag):
			continue
		if not _trait_matches_level(trait_dict, level):
			continue
		if not SafeTypeUtils.bool_val(TraitCatalog.call("MeetsPrerequisites", trait_id, owned_traits), false):
			continue

		result.append(trait_dict)

	return result


func _trait_has_tag(trait_dict: Dictionary, tag: String) -> bool:
	var tags_var: Variant = trait_dict.get("tags", [])
	if not tags_var is Array:
		return false
	var tags: Array = tags_var
	for entry: Variant in tags:
		if SafeTypeUtils.string(entry, "") == tag:
			return true
	return false


func _trait_matches_level(trait_dict: Dictionary, level: int) -> bool:
	var min_level: int = SafeTypeUtils.int_val(trait_dict.get("min_level", 1), 1)
	var max_level: int = SafeTypeUtils.int_val(trait_dict.get("max_level", 0), 0)
	if level < min_level:
		return false
	if max_level > 0 and level > max_level:
		return false
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

	var instance_id: String = ItemsApi.grant_item(item_id)
	if instance_id.is_empty():
		print("DevConsole: Failed to grant item (invalid item_id?)")
		return false

	print("DevConsole: Granted item! Instance ID: %s" % instance_id)
	return true


func _cmd_items_grant_all() -> bool:
	print("DevConsole: Granting all starter items...")

	var granted_count: int = 0
	for item_id: String in TEST_ITEMS:
		var instance_id: String = ItemsApi.grant_item(item_id)
		if not instance_id.is_empty():
			print("  Granted: %s -> %s" % [item_id, instance_id])
			granted_count += 1
		else:
			print("  FAILED: %s" % item_id)

	print("DevConsole: Granted %d items!" % granted_count)
	return true


func _cmd_items_list() -> bool:
	print("=== PLAYER ITEMS ===")

	var summoner_id: String = SummonerSelectionApi.get_active_summoner_id()
	if summoner_id.is_empty():
		print("No active summoner - select a summoner first")
		print("====================")
		return true

	# Get equipped items
	var equipped: Dictionary = ItemsApi.get_equipped_items_dict(summoner_id)
	print("Equipped on %s:" % summoner_id)
	for slot: String in ["wand", "ring1", "ring2", "robes"]:
		var instance_id: String = equipped.get(slot, "")
		if instance_id.is_empty():
			print("  [%s]: (empty)" % slot)
		else:
			print("  [%s]: %s" % [slot, instance_id])

	# Get all items for each slot
	print("\nAvailable items:")
	for slot: String in ["wand", "ring1", "ring2", "robes"]:
		var items: Array[Dictionary] = ItemsApi.list_items_for_slot_dict(slot, summoner_id)
		print("  %s slot: %d items" % [slot, items.size()])
		for item: Dictionary in items:
			var name_key: String = item.get("name_key", "")
			var item_name: String = Loc.t(name_key) if not name_key.is_empty() else SafeTypeUtils.string(item.get("id", "?"), "?")
			var is_equipped: String = " [EQUIPPED]" if item.get("equipped_by", "") == summoner_id else ""
			print("    - %s (%s)%s" % [item_name, item.get("instance_id", "?"), is_equipped])

	print("====================")
	return true


func _cmd_items_equip(args: PackedStringArray) -> bool:
	if args.size() < 2:
		print("DevConsole: Usage: /items_equip <slot> <instance_id>")
		print("DevConsole: Slots: wand, ring1, ring2, robes")
		print("DevConsole: Use /items_list to see available instance IDs")
		return false

	var summoner_id: String = SummonerSelectionApi.get_active_summoner_id()
	if summoner_id.is_empty():
		print("DevConsole: No active summoner - select a summoner first")
		return false

	var slot: String = args[0].to_lower()
	var instance_id: String = args[1]

	print("DevConsole: Equipping item '%s' to %s's %s slot..." % [instance_id, summoner_id, slot])

	var success: bool = ItemsApi.equip_item_str(summoner_id, instance_id, slot)
	if success:
		print("DevConsole: Item equipped successfully!")
	else:
		print("DevConsole: Failed to equip item (check slot/instance_id)")

	return success


func _cmd_items_clear() -> bool:
	print("DevConsole: Clearing all items...")
	ItemsApi.clear_all_items()
	print("DevConsole: All items cleared!")
	return true
