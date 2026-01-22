extends IProfileRepo
# JsonProfileRepo is registered as autoload "ProfileRepo", no class_name needed

## JSON Profile Repository Implementation
##
## Stores profile data in JSON files with:
## - Atomic writes (temp file → rename) for corruption prevention
## - Dual backup system (profile.bak1, profile.bak2)
## - Debounced autosave (0.5s idle + immediate checkpoints)
## - Write-ahead log (WAL) for future sync support
## - DB-ready schema (UUIDs, row-oriented)
##
## File structure:
## - user://profiles/{profile_id}/profile.json (main save)
## - user://profiles/{profile_id}/profile.bak1 (backup 1)
## - user://profiles/{profile_id}/profile.bak2 (backup 2)
## - user://profiles/{profile_id}/wal.json (write-ahead log)

## Autosave timing
const AUTOSAVE_DELAY: float = 0.5  # Seconds of inactivity before autosave

## Current save version for migrations
const CURRENT_VERSION: int = 5

## Signals inherited from IProfileRepo (do not redeclare)

## In-memory profile data
var _data: Dictionary = {}
var _current_profile_id: String = ""

## Debounce timer
var _save_timer: Timer = null
var _pending_save: bool = false

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("JsonProfileRepo: Initializing...")
	_setup_save_timer()

	# Auto-load default profile (in future, let user select)
	var default_profile_id: String = _get_or_create_default_profile()
	load_profile(default_profile_id)

## Handle application lifecycle events for crash protection
func _notification(what: int) -> void:
	match what:
		NOTIFICATION_WM_CLOSE_REQUEST:
			# Desktop: Save before window closes
			save_profile(true)
		NOTIFICATION_APPLICATION_PAUSED:
			# Mobile: Save when app is backgrounded (may be killed by OS)
			save_profile(true)
		NOTIFICATION_APPLICATION_FOCUS_OUT:
			# Save when losing focus (optional extra safety)
			if _pending_save:
				save_profile(true)

func _setup_save_timer() -> void:
	_save_timer = Timer.new()
	_save_timer.one_shot = true
	_save_timer.wait_time = AUTOSAVE_DELAY
	_save_timer.timeout.connect(_on_save_timer_timeout)
	add_child(_save_timer)

## =============================================================================
## PROFILE OPERATIONS
## =============================================================================

func load_profile(profile_id: String) -> bool:
	print("JsonProfileRepo: Loading profile '%s'..." % profile_id)
	_current_profile_id = profile_id

	var profile_dir_result: Variant = _get_profile_dir(profile_id)
	var profile_dir: String = profile_dir_result
	var main_path: String = profile_dir + "/profile.json"
	var bak1_path: String = profile_dir + "/profile.bak1"
	var bak2_path: String = profile_dir + "/profile.bak2"

	# Try main save first
	if FileAccess.file_exists(main_path):
		var loaded_data_result: Variant = _load_from_file(main_path)
		var loaded_data: Variant = loaded_data_result
		if loaded_data != null:
			_data = loaded_data
			_migrate_if_needed()
			print("JsonProfileRepo: Loaded from main save")
			profile_loaded.emit(profile_id)
			data_changed.emit()
			return true
		else:
			push_warning("JsonProfileRepo: Main save corrupted, trying backup1...")

	# Try backup1
	if FileAccess.file_exists(bak1_path):
		var loaded_data_result2: Variant = _load_from_file(bak1_path)
		var loaded_data: Variant = loaded_data_result2
		if loaded_data != null:
			_data = loaded_data
			_migrate_if_needed()
			print("JsonProfileRepo: Loaded from backup1")
			save_profile(true)  # Restore main file
			profile_loaded.emit(profile_id)
			data_changed.emit()
			return true
		else:
			push_warning("JsonProfileRepo: Backup1 corrupted, trying backup2...")

	# Try backup2
	if FileAccess.file_exists(bak2_path):
		var loaded_data_result3: Variant = _load_from_file(bak2_path)
		var loaded_data: Variant = loaded_data_result3
		if loaded_data != null:
			_data = loaded_data
			_migrate_if_needed()
			print("JsonProfileRepo: Loaded from backup2")
			save_profile(true)  # Restore main file
			profile_loaded.emit(profile_id)
			data_changed.emit()
			return true
		else:
			push_error("JsonProfileRepo: All save files corrupted!")

	# No valid saves found - create fresh
	print("JsonProfileRepo: No save found, creating fresh profile")
	_create_fresh_profile()
	save_profile(true)  # Save immediately
	profile_loaded.emit(profile_id)
	data_changed.emit()
	return true

func save_profile(immediate: bool = false) -> void:
	# Always mark as pending first. This is critical because _write_save() checks
	# this flag and returns early if false - preventing redundant writes when an
	# immediate save happens before a pending timer save.
	_pending_save = true
	if immediate:
		# Cancel any pending debounced save since we're writing now.
		# This prevents a redundant write when the timer fires later.
		_save_timer.stop()
		_write_save()
	else:
		_save_timer.start(AUTOSAVE_DELAY)

func get_current_profile_id() -> String:
	return _current_profile_id

func get_active_profile() -> Dictionary:
	return _data

func reset_profile() -> void:
	print("JsonProfileRepo: Resetting profile...")
	_create_fresh_profile()
	save_profile(true)
	data_changed.emit()

func snapshot() -> Dictionary:
	return _data.duplicate(true)

## Get complete profile data for snapshots/debugging
func get_profile_data() -> Dictionary:
	return _data.duplicate(true)

## Load complete profile data from snapshot
func load_profile_data(profile_data: Dictionary) -> void:
	if profile_data.is_empty():
		push_error("JsonProfileRepo: Cannot load empty profile data")
		return

	print("JsonProfileRepo: Loading profile data from snapshot...")
	_data = profile_data.duplicate(true)

	# Update the profile_id to match current profile
	_data["profile_id"] = _current_profile_id
	if _data.has("resources"):
		_data["resources"]["profile_id"] = _current_profile_id

	# Update timestamp
	_data["updated_at"] = Time.get_unix_time_from_system()

	# Save immediately to disk
	save_profile(true)

	# Emit signals
	profile_loaded.emit(_current_profile_id)
	data_changed.emit()

	print("JsonProfileRepo: Profile data loaded successfully")

## =============================================================================
## RESOURCE OPERATIONS
## =============================================================================

func get_resources() -> Dictionary:
	var empty_dict: Dictionary = {}
	return _data.get("resources", empty_dict)

func update_resources(delta: Dictionary) -> void:
	var empty_resources: Dictionary = {}
	var resources_variant: Variant = _data.get("resources", empty_resources)
	var resources: Dictionary = resources_variant

	for key: String in delta:
		var delta_value: Variant = delta[key]
		if key in resources:
			var res_value_variant: Variant = resources[key]
			var res_value: int = res_value_variant
			var delta_int: int = delta_value
			res_value += delta_int
			# Clamp to prevent negative (except for testing)
			if res_value < 0:
				push_warning("JsonProfileRepo: Resource '%s' went negative (%d), clamping to 0" % [key, res_value])
				res_value = 0
			resources[key] = res_value

	_data["resources"]["updated_at"] = Time.get_unix_time_from_system()

	# Log to WAL
	_append_to_wal({
		"action": "update_resources",
		"params": delta
	})

	save_profile()  # Debounced
	data_changed.emit()

## =============================================================================
## SUMMONER OPERATIONS
## =============================================================================

## Get list of unlocked summoner IDs
func get_unlocked_summoners() -> Array:
	return _data.get("unlocked_summoners", [])

## Check if a specific summoner is unlocked
func is_summoner_unlocked(summoner_id: String) -> bool:
	var unlocked: Array = _data.get("unlocked_summoners", [])
	return summoner_id in unlocked

## Unlock a new summoner
func unlock_summoner(summoner_id: String) -> bool:
	var unlocked: Array = _data.get("unlocked_summoners", [])
	if summoner_id not in unlocked:
		unlocked.append(summoner_id)
		_data["unlocked_summoners"] = unlocked
		_append_to_wal({"action": "unlock_summoner", "params": {"summoner_id": summoner_id}})
		save_profile()
		data_changed.emit()
		return true
	return false

## Set starting summoner (called during onboarding)
## chosen_random: whether player selected "Random" option (passed to WAL for tracking)
func set_starting_summoner(summoner_id: String, chosen_random: bool) -> bool:
	# Validate this is called on a fresh profile
	var unlocked: Array = _data.get("unlocked_summoners", [])
	if not unlocked.is_empty():
		push_error("ProfileRepo: Cannot set starting summoner - summoners already unlocked")
		return false

	# Add summoner
	unlocked.append(summoner_id)
	_data["unlocked_summoners"] = unlocked

	# Note: If chosen_random is true, the summoner should have the
	# "fortune_favors_the_bold" modifier in its active_modifiers.
	# The caller should create the SummonerInstance with this modifier.

	_append_to_wal({
		"action": "set_starting_summoner",
		"params": {"summoner_id": summoner_id, "chosen_random": chosen_random}
	})
	save_profile()
	data_changed.emit()
	return true

## =============================================================================
## SUMMONER INSTANCE OPERATIONS (Typed Summoner System)
## =============================================================================

## Get all summoner instances
func get_summoner_instances() -> Array:
	return _data.get("summoner_instances", [])

## Get a specific summoner instance by summoner_id
func get_summoner_instance(summoner_id: String) -> Dictionary:
	var instances_variant: Variant = _data.get("summoner_instances", [])
	if not instances_variant is Array:
		return {}
	var instances_array: Array = instances_variant

	for instance: Variant in instances_array:
		if instance is Dictionary:
			var instance_dict: Dictionary = instance
			var instance_summoner_id_variant: Variant = instance_dict.get("summoner_id")
			if instance_summoner_id_variant == summoner_id:
				return instance_dict
	return {}

## Save or update a summoner instance
func save_summoner_instance(summoner_instance: SummonerInstance) -> bool:
	if not summoner_instance.is_valid():
		push_error("ProfileRepo: Cannot save invalid SummonerInstance")
		return false

	var instances_variant: Variant = _data.get("summoner_instances", [])
	if not instances_variant is Array:
		instances_variant = []
	var instances_array: Array = instances_variant

	var summoner_data: Dictionary = summoner_instance.to_dict()
	var summoner_id: String = summoner_data.get("summoner_id", "")

	# Check if instance already exists
	var found_index: int = -1
	for i: int in range(instances_array.size()):
		var instance: Variant = instances_array[i]
		if instance is Dictionary:
			var instance_dict: Dictionary = instance
			var instance_summoner_id_variant: Variant = instance_dict.get("summoner_id")
			if instance_summoner_id_variant == summoner_id:
				found_index = i
				break

	if found_index >= 0:
		# Update existing
		instances_array[found_index] = summoner_data
	else:
		# Add new
		instances_array.append(summoner_data)
		# Also add to unlocked_summoners for compatibility
		var unlocked: Array = _data.get("unlocked_summoners", [])
		if summoner_id not in unlocked:
			unlocked.append(summoner_id)
			_data["unlocked_summoners"] = unlocked

	_data["summoner_instances"] = instances_array

	_append_to_wal({
		"action": "save_summoner_instance",
		"params": {"summoner_id": summoner_id}
	})

	save_profile()
	data_changed.emit()
	return true

## =============================================================================
## SHOP OPERATIONS
## =============================================================================

func get_shop_purchases() -> Dictionary:
	var empty_dict: Dictionary = {}
	return _data.get("shop_purchases", empty_dict)

func get_purchase_count(purchase_key: String) -> int:
	var purchases: Dictionary = _data.get("shop_purchases", {})
	var count_variant: Variant = purchases.get(purchase_key, 0)
	var count: int = count_variant
	return count

func increment_purchase_count(purchase_key: String) -> bool:
	var purchases_variant: Variant = _data.get("shop_purchases", {})
	var purchases: Dictionary = purchases_variant
	var count: int = purchases.get(purchase_key, 0)
	purchases[purchase_key] = count + 1
	_data["shop_purchases"] = purchases

	_append_to_wal({
		"action": "shop_purchase",
		"params": {"key": purchase_key}
	})

	save_profile()  # Debounced
	data_changed.emit()
	return true

func get_shop_refresh_state(shop_id: String) -> Dictionary:
	var state_variant: Variant = _data.get("shop_refresh_state", {})
	var state: Dictionary = state_variant
	var empty_shop_state: Dictionary = {"refresh_epoch": 0, "last_refresh_at": ""}
	var shop_state_variant: Variant = state.get(shop_id, empty_shop_state)
	var shop_state: Dictionary = shop_state_variant
	return shop_state

func increment_shop_refresh_epoch(shop_id: String) -> bool:
	var state_variant: Variant = _data.get("shop_refresh_state", {})
	var state: Dictionary = state_variant
	var empty_shop_state: Dictionary = {"refresh_epoch": 0, "last_refresh_at": ""}
	var shop_state_variant: Variant = state.get(shop_id, empty_shop_state)
	var shop_state: Dictionary = shop_state_variant

	var epoch_variant: Variant = shop_state.get("refresh_epoch", 0)
	var epoch: int = epoch_variant
	shop_state["refresh_epoch"] = epoch + 1
	shop_state["last_refresh_at"] = Time.get_datetime_string_from_system()
	state[shop_id] = shop_state
	_data["shop_refresh_state"] = state

	_append_to_wal({
		"action": "shop_refresh",
		"params": {"shop_id": shop_id}
	})

	save_profile()  # Debounced
	data_changed.emit()
	return true

## =============================================================================
## COSMETIC OPERATIONS
## =============================================================================

## Get list of owned cosmetic IDs
func get_owned_cosmetics() -> Array:
	var cosmetics: Dictionary = _data.get("cosmetics", {"owned": []})
	return cosmetics.get("owned", [])

## Check if a specific cosmetic is owned
func is_cosmetic_owned(cosmetic_id: String) -> bool:
	return cosmetic_id in get_owned_cosmetics()

## Grant a cosmetic to the player
func grant_cosmetic(cosmetic_id: String) -> bool:
	var cosmetics: Dictionary = _data.get("cosmetics", {"owned": [], "equipped": {}, "summoner_skins": {}})
	var owned: Array = cosmetics.get("owned", [])
	if cosmetic_id not in owned:
		owned.append(cosmetic_id)
		cosmetics["owned"] = owned
		_data["cosmetics"] = cosmetics
		_append_to_wal({"action": "grant_cosmetic", "params": {"cosmetic_id": cosmetic_id}})
		save_profile()
		data_changed.emit()
		return true
	return false

## Get equipped cosmetics by slot
func get_equipped_cosmetics() -> Dictionary:
	var cosmetics: Dictionary = _data.get("cosmetics", {"equipped": {}})
	return cosmetics.get("equipped", {})

## Equip a cosmetic to a slot (card_back, ui_theme)
func equip_cosmetic(slot: String, cosmetic_id: String) -> bool:
	# Validate cosmetic is owned (empty string means unequip)
	if not cosmetic_id.is_empty() and not is_cosmetic_owned(cosmetic_id):
		push_warning("ProfileRepo: Cannot equip cosmetic '%s' - not owned" % cosmetic_id)
		return false

	var cosmetics: Dictionary = _data.get("cosmetics", {"owned": [], "equipped": {}, "summoner_skins": {}})
	var equipped: Dictionary = cosmetics.get("equipped", {})
	equipped[slot] = cosmetic_id
	cosmetics["equipped"] = equipped
	_data["cosmetics"] = cosmetics
	_append_to_wal({"action": "equip_cosmetic", "params": {"slot": slot, "cosmetic_id": cosmetic_id}})
	save_profile()
	data_changed.emit()
	return true

## Get summoner skin mappings
func get_summoner_skins() -> Dictionary:
	var cosmetics: Dictionary = _data.get("cosmetics", {"summoner_skins": {}})
	return cosmetics.get("summoner_skins", {})

## Set a skin for a specific summoner
func set_summoner_skin(summoner_id: String, skin_id: String) -> bool:
	# Validate skin is owned (empty string means remove skin)
	if not skin_id.is_empty() and not is_cosmetic_owned(skin_id):
		push_warning("ProfileRepo: Cannot set skin '%s' - not owned" % skin_id)
		return false

	var cosmetics: Dictionary = _data.get("cosmetics", {"owned": [], "equipped": {}, "summoner_skins": {}})
	var skins: Dictionary = cosmetics.get("summoner_skins", {})
	if skin_id.is_empty():
		skins.erase(summoner_id)
	else:
		skins[summoner_id] = skin_id
	cosmetics["summoner_skins"] = skins
	_data["cosmetics"] = cosmetics
	_append_to_wal({"action": "set_summoner_skin", "params": {"summoner_id": summoner_id, "skin_id": skin_id}})
	save_profile()
	data_changed.emit()
	return true

## =============================================================================
## EMOTE OPERATIONS
## =============================================================================

## Get list of owned emote IDs
func get_owned_emotes() -> Array:
	var emotes: Dictionary = _data.get("emotes", {"owned": []})
	return emotes.get("owned", [])

## Check if a specific emote is owned
func is_emote_owned(emote_id: String) -> bool:
	return emote_id in get_owned_emotes()

## Grant an emote to the player
func grant_emote(emote_id: String) -> bool:
	var emotes: Dictionary = _data.get("emotes", {"owned": [], "equipped": []})
	var owned: Array = emotes.get("owned", [])
	if emote_id not in owned:
		owned.append(emote_id)
		emotes["owned"] = owned
		_data["emotes"] = emotes
		_append_to_wal({"action": "grant_emote", "params": {"emote_id": emote_id}})
		save_profile()
		data_changed.emit()
		return true
	return false

## Get equipped emotes (array of 4 slots)
func get_equipped_emotes() -> Array:
	var emotes: Dictionary = _data.get("emotes", {"equipped": ["", "", "", ""]})
	var equipped: Array = emotes.get("equipped", ["", "", "", ""])
	# Ensure we have exactly 4 slots
	while equipped.size() < 4:
		equipped.append("")
	return equipped

## Equip an emote to a slot (0-3)
func equip_emote(slot: int, emote_id: String) -> bool:
	if slot < 0 or slot >= 4:
		push_warning("ProfileRepo: Invalid emote slot %d (must be 0-3)" % slot)
		return false

	# Validate emote is owned (empty string means unequip)
	if not emote_id.is_empty() and not is_emote_owned(emote_id):
		push_warning("ProfileRepo: Cannot equip emote '%s' - not owned" % emote_id)
		return false

	var emotes: Dictionary = _data.get("emotes", {"owned": [], "equipped": ["", "", "", ""]})
	var equipped: Array = emotes.get("equipped", ["", "", "", ""])
	# Ensure we have exactly 4 slots
	while equipped.size() < 4:
		equipped.append("")
	equipped[slot] = emote_id
	emotes["equipped"] = equipped
	_data["emotes"] = emotes
	_append_to_wal({"action": "equip_emote", "params": {"slot": slot, "emote_id": emote_id}})
	save_profile()
	data_changed.emit()
	return true

## =============================================================================
## CARD COLLECTION OPERATIONS
## =============================================================================

func grant_cards(cards: Array) -> Array:
	var collection_variant: Variant = _data.get("collection", [])
	var collection: Array = collection_variant
	var instance_ids: Array = []

	for card: Variant in cards:
		if not card is Dictionary:
			continue
		var card_dict: Dictionary = card
		var catalog_id_variant: Variant = card_dict.get("catalog_id", "")
		var catalog_id: String = catalog_id_variant
		var rarity_variant: Variant = card_dict.get("rarity", "common")
		var rarity: String = rarity_variant
		var instance: Dictionary = {
			"id": _generate_uuid(),
			"profile_id": _current_profile_id,
			"catalog_id": catalog_id,
			"rarity": rarity,
			"level": 1,         # Card progression level (1-10)
			"xp": 0,            # XP towards next level
			"upgrades": [],     # Array of chosen upgrade IDs
			"roll_json": null,  # Future: stat rolls
			"created_at": Time.get_unix_time_from_system()
		}
		collection.append(instance)
		var inst_id_variant: Variant = instance.get("id")
		var inst_id: String = inst_id_variant
		instance_ids.append(inst_id)

	_data["collection"] = collection

	# Log to WAL
	_append_to_wal({
		"action": "grant_cards",
		"params": {"cards": cards, "instance_ids": instance_ids}
	})

	save_profile()  # Debounced
	data_changed.emit()

	return instance_ids

func remove_card(card_instance_id: String) -> bool:
	var collection_variant: Variant = _data.get("collection", [])
	if not collection_variant is Array:
		return false
	var coll_array: Array = collection_variant
	var found_index: int = -1

	for i: int in range(coll_array.size()):
		var item_variant: Variant = coll_array[i]
		if item_variant is Dictionary:
			var item_dict: Dictionary = item_variant
			var item_id_variant: Variant = item_dict.get("id")
			if item_id_variant == card_instance_id:
				found_index = i
				break

	if found_index == -1:
		push_warning("JsonProfileRepo: Card instance '%s' not found" % card_instance_id)
		return false

	coll_array.remove_at(found_index)
	_data["collection"] = coll_array

	# Log to WAL
	_append_to_wal({
		"action": "remove_card",
		"params": {"card_instance_id": card_instance_id}
	})

	save_profile()  # Debounced
	data_changed.emit()

	return true

func list_cards() -> Array:
	return _data.get("collection", [])

func get_card_count(catalog_id: String) -> int:
	var collection_variant: Variant = _data.get("collection", [])
	if not collection_variant is Array:
		return 0
	var coll_array: Array = collection_variant
	var count: int = 0
	for card: Variant in coll_array:
		if card is Dictionary:
			var card_dict: Dictionary = card
			var card_catalog_id_variant: Variant = card_dict.get("catalog_id")
			if card_catalog_id_variant == catalog_id:
				count += 1
	return count

func get_card(card_instance_id: String) -> Dictionary:
	var collection_variant: Variant = _data.get("collection", [])
	if not collection_variant is Array:
		var empty: Dictionary = {}
		return empty
	var coll_array: Array = collection_variant
	for card: Variant in coll_array:
		if card is Dictionary:
			var card_dict: Dictionary = card
			var card_id_variant: Variant = card_dict.get("id")
			if card_id_variant == card_instance_id:
				return card_dict
	var not_found: Dictionary = {}
	return not_found

## Update a card instance with new data (for progression system)
## updates: Dictionary of fields to update (e.g., {"xp": 50, "level": 2})
func update_card(card_instance_id: String, updates: Dictionary) -> bool:
	var collection_variant: Variant = _data.get("collection", [])
	if not collection_variant is Array:
		return false
	var coll_array: Array = collection_variant

	for i: int in range(coll_array.size()):
		var card: Variant = coll_array[i]
		if card is Dictionary:
			var card_dict: Dictionary = card
			var card_id_variant: Variant = card_dict.get("id")
			if card_id_variant == card_instance_id:
				# Apply updates
				for key: String in updates:
					card_dict[key] = updates[key]
				coll_array[i] = card_dict
				_data["collection"] = coll_array

				# Log to WAL
				_append_to_wal({
					"action": "update_card",
					"params": {"card_instance_id": card_instance_id, "updates": updates}
				})

				save_profile()  # Debounced
				data_changed.emit()
				return true

	push_warning("JsonProfileRepo: Card instance '%s' not found for update" % card_instance_id)
	return false

## =============================================================================
## DECK OPERATIONS
## =============================================================================

func upsert_deck(deck: Dictionary) -> String:
	var decks_variant: Variant = _data.get("decks", [])
	if not decks_variant is Array:
		return ""
	var decks_array: Array = decks_variant
	var deck_id: String = ""
	var deck_id_var: Variant = deck.get("id", "")
	if deck_id_var is String:
		deck_id = deck_id_var

	# If no ID, create new deck
	if deck_id == "":
		deck_id = _generate_uuid()
		var deck_name_variant: Variant = deck.get("name", "Untitled Deck")
		var deck_name: String = deck_name_variant
		var summoner_id_variant: Variant = deck.get("summoner_id", "")
		var summoner_id: String = summoner_id_variant if summoner_id_variant is String else ""
		var new_deck: Dictionary = {
			"id": deck_id,
			"profile_id": _current_profile_id,
			"name": deck_name,
			"summoner_id": summoner_id,
			"created_at": Time.get_unix_time_from_system()
		}
		decks_array.append(new_deck)

		# Create deck_cards entries
		var deck_cards_variant: Variant = _data.get("deck_cards", [])
		if not deck_cards_variant is Array:
			deck_cards_variant = []
		var deck_cards_array: Array = deck_cards_variant
		var card_instance_ids_variant: Variant = deck.get("card_instance_ids", [])
		if card_instance_ids_variant is Array:
			var card_ids_array: Array = card_instance_ids_variant
			for i: int in range(card_ids_array.size()):
				var card_id_at_i_variant: Variant = card_ids_array[i]
				var deck_card_entry: Dictionary = {
					"deck_id": deck_id,
					"card_instance_id": card_id_at_i_variant,
					"slot_index": i
				}
				deck_cards_array.append(deck_card_entry)
		_data["deck_cards"] = deck_cards_array
	else:
		# Update existing deck
		var found: bool = false
		for i: int in range(decks_array.size()):
			var deck_item_variant: Variant = decks_array[i]
			if deck_item_variant is Dictionary:
				var deck_dict: Dictionary = deck_item_variant
				var deck_dict_id_variant: Variant = deck_dict.get("id")
				if deck_dict_id_variant == deck_id:
					var deck_name_variant: Variant = deck.get("name", deck_dict["name"])
					deck_dict["name"] = deck_name_variant
					# Update summoner_id if provided
					if deck.has("summoner_id"):
						var summoner_id_variant: Variant = deck.get("summoner_id")
						if summoner_id_variant is String:
							deck_dict["summoner_id"] = summoner_id_variant
					found = true
					break

		if not found:
			push_warning("JsonProfileRepo: Deck '%s' not found for update" % deck_id)
			return ""

		# Update deck_cards
		var deck_cards_variant2: Variant = _data.get("deck_cards", [])
		if not deck_cards_variant2 is Array:
			deck_cards_variant2 = []
		var deck_cards_array2: Array = deck_cards_variant2
		# Remove old entries
		var new_deck_cards: Array = []
		for dc: Variant in deck_cards_array2:
			if dc is Dictionary:
				var dc_dict: Dictionary = dc
				var dc_deck_id_variant: Variant = dc_dict.get("deck_id")
				if dc_deck_id_variant != deck_id:
					new_deck_cards.append(dc)
		# Add new entries
		var card_instance_ids_variant2: Variant = deck.get("card_instance_ids", [])
		if card_instance_ids_variant2 is Array:
			var card_ids_array2: Array = card_instance_ids_variant2
			for i: int in range(card_ids_array2.size()):
				var card_id_at_i_variant2: Variant = card_ids_array2[i]
				var deck_card_entry2: Dictionary = {
					"deck_id": deck_id,
					"card_instance_id": card_id_at_i_variant2,
					"slot_index": i
				}
				new_deck_cards.append(deck_card_entry2)
		_data["deck_cards"] = new_deck_cards

	_data["decks"] = decks_array

	save_profile()  # Debounced
	data_changed.emit()

	return deck_id

func delete_deck(deck_id: String) -> bool:
	var decks_variant: Variant = _data.get("decks", [])
	if not decks_variant is Array:
		return false
	var decks_array: Array = decks_variant
	var found_index: int = -1

	for i: int in range(decks_array.size()):
		var deck_item_variant: Variant = decks_array[i]
		if deck_item_variant is Dictionary:
			var deck_dict: Dictionary = deck_item_variant
			var deck_dict_id_variant: Variant = deck_dict.get("id")
			if deck_dict_id_variant == deck_id:
				found_index = i
				break

	if found_index == -1:
		push_warning("JsonProfileRepo: Deck '%s' not found" % deck_id)
		return false

	decks_array.remove_at(found_index)
	_data["decks"] = decks_array

	# Remove deck_cards entries
	var deck_cards_variant: Variant = _data.get("deck_cards", [])
	if not deck_cards_variant is Array:
		return true
	var deck_cards_array: Array = deck_cards_variant
	var new_deck_cards: Array = []
	for dc: Variant in deck_cards_array:
		if dc is Dictionary:
			var dc_dict: Dictionary = dc
			var dc_deck_id_variant: Variant = dc_dict.get("deck_id")
			if dc_deck_id_variant != deck_id:
				new_deck_cards.append(dc)
	_data["deck_cards"] = new_deck_cards

	save_profile()  # Debounced
	data_changed.emit()

	return true

func list_decks() -> Array:
	var decks_variant: Variant = _data.get("decks", [])
	if not decks_variant is Array:
		return []
	var decks_array: Array = decks_variant
	var deck_cards_variant: Variant = _data.get("deck_cards", [])
	if not deck_cards_variant is Array:
		deck_cards_variant = []
	var deck_cards_array: Array = deck_cards_variant

	# Enrich decks with card_instance_ids
	var enriched_decks: Array = []
	for deck: Variant in decks_array:
		if not deck is Dictionary:
			continue
		var deck_dict: Dictionary = deck
		var enriched_variant: Variant = deck_dict.duplicate()
		if not enriched_variant is Dictionary:
			continue
		var enriched_dict: Dictionary = enriched_variant
		var cards: Array = []
		var deck_dict_id_variant: Variant = deck_dict.get("id")
		for dc: Variant in deck_cards_array:
			if dc is Dictionary:
				var dc_dict: Dictionary = dc
				var dc_deck_id_variant: Variant = dc_dict.get("deck_id")
				if dc_deck_id_variant == deck_dict_id_variant:
					var card_inst_id_variant: Variant = dc_dict.get("card_instance_id")
					cards.append(card_inst_id_variant)
		enriched_dict["card_instance_ids"] = cards
		enriched_decks.append(enriched_dict)

	return enriched_decks

func get_deck(deck_id: String) -> Dictionary:
	var decks: Array = list_decks()
	for deck: Variant in decks:
		if deck is Dictionary:
			var deck_dict: Dictionary = deck
			var deck_dict_id_variant: Variant = deck_dict.get("id")
			if deck_dict_id_variant == deck_id:
				return deck_dict
	var not_found: Dictionary = {}
	return not_found

## =============================================================================
## CAMPAIGN PROGRESS OPERATIONS
## =============================================================================

## Get campaign progress for a specific summoner (or active summoner if not specified)
func get_campaign_progress(summoner_id: String = "") -> Dictionary:
	var empty_progress: Dictionary = {"completed_battles": [], "current_battle": null}

	# Determine which summoner to use
	var target_summoner_id: String = summoner_id
	if target_summoner_id.is_empty():
		target_summoner_id = _get_active_summoner_id()

	# Get campaign_progress data
	var campaign_progress: Variant = _data.get("campaign_progress", {})
	if not campaign_progress is Dictionary:
		return empty_progress

	var progress_dict: Dictionary = campaign_progress

	# Check for old structure (has "completed_battles" at root level - not per-summoner)
	if progress_dict.has("completed_battles") and not _is_per_summoner_structure(progress_dict):
		# Migrate old structure to new per-summoner format
		_migrate_campaign_progress_to_per_summoner(progress_dict)
		progress_dict = _data.get("campaign_progress", {})

	# Return summoner-specific progress
	if target_summoner_id.is_empty():
		return empty_progress

	var summoner_progress: Variant = progress_dict.get(target_summoner_id, empty_progress)
	if summoner_progress is Dictionary:
		return summoner_progress
	return empty_progress

## Check if progress dict is already per-summoner structure
func _is_per_summoner_structure(progress: Dictionary) -> bool:
	# Per-summoner structure has summoner IDs as keys, each containing completed_battles
	# Old structure has "completed_battles" directly at the root
	for key: String in progress.keys():
		var value: Variant = progress[key]
		# If a key maps to a Dictionary with completed_battles, it's per-summoner
		if value is Dictionary:
			var value_dict: Dictionary = value
			if value_dict.has("completed_battles"):
				return true
	return false

## Migrate old single-progress structure to per-summoner
func _migrate_campaign_progress_to_per_summoner(old_progress: Dictionary) -> void:
	print("ProfileRepo: Migrating campaign progress to per-summoner structure...")

	var active_summoner_id: String = _get_active_summoner_id()
	if active_summoner_id.is_empty():
		# Fallback to first unlocked summoner
		var unlocked: Array = get_unlocked_summoners()
		if unlocked.size() > 0:
			active_summoner_id = unlocked[0]

	if active_summoner_id.is_empty():
		# SAFETY: Preserve old progress in a backup key instead of discarding
		push_warning("ProfileRepo: No summoner available for migration, preserving old progress in _legacy_progress")
		_data["campaign_progress"] = {
			"_legacy_progress": old_progress.duplicate(true)
		}
		return

	# Move old progress to active summoner
	var new_structure: Dictionary = {}
	new_structure[active_summoner_id] = {
		"completed_battles": old_progress.get("completed_battles", []),
		"current_battle": old_progress.get("current_battle", null),
		"pending_reward": old_progress.get("pending_reward", null)
	}

	_data["campaign_progress"] = new_structure
	save_profile(true)  # Immediate save after migration
	print("ProfileRepo: Migrated campaign progress to summoner '%s'" % active_summoner_id)

func _get_active_summoner_id() -> String:
	var result: String = SummonerSelection.get_active_summoner_id()
	return result

## Update campaign progress for a specific summoner (or active summoner if not specified)
func update_campaign_progress(progress: Dictionary, summoner_id: String = "") -> void:
	# Determine which summoner to use
	var target_summoner_id: String = summoner_id
	if target_summoner_id.is_empty():
		target_summoner_id = _get_active_summoner_id()

	if target_summoner_id.is_empty():
		push_warning("ProfileRepo: Cannot update campaign progress - no active summoner")
		return

	# Ensure campaign_progress dict exists
	if not _data.has("campaign_progress"):
		_data["campaign_progress"] = {}

	var campaign_progress: Variant = _data.get("campaign_progress")
	if not campaign_progress is Dictionary:
		_data["campaign_progress"] = {}
		campaign_progress = _data["campaign_progress"]

	var campaign_dict: Dictionary = campaign_progress

	# Ensure summoner-specific progress exists
	if not campaign_dict.has(target_summoner_id):
		campaign_dict[target_summoner_id] = {"completed_battles": [], "current_battle": null}

	var summoner_progress: Variant = campaign_dict.get(target_summoner_id)
	if summoner_progress is Dictionary:
		var summoner_dict: Dictionary = summoner_progress
		for key: String in progress:
			summoner_dict[key] = progress[key]

	_append_to_wal({"action": "update_campaign_progress", "params": progress, "summoner_id": target_summoner_id})
	save_profile(true)  # Immediate save for campaign progress
	data_changed.emit()

## =============================================================================
## SHARED CAMPAIGN PROGRESS OPERATIONS (Account-wide)
## =============================================================================

## Get shared campaign progress (for onboarding and other account-wide campaigns)
func get_shared_campaign_progress() -> Dictionary:
	var empty_progress: Dictionary = {"completed_battles": [], "current_battle": null}
	var shared_progress: Variant = _data.get("shared_campaign_progress", empty_progress)
	if shared_progress is Dictionary:
		return shared_progress
	return empty_progress

## Update shared campaign progress
func update_shared_campaign_progress(progress: Dictionary) -> void:
	# Ensure shared_campaign_progress dict exists
	if not _data.has("shared_campaign_progress"):
		_data["shared_campaign_progress"] = {"completed_battles": [], "current_battle": null}

	var shared_progress: Variant = _data.get("shared_campaign_progress")
	if not shared_progress is Dictionary:
		_data["shared_campaign_progress"] = {"completed_battles": [], "current_battle": null}
		shared_progress = _data["shared_campaign_progress"]

	var shared_dict: Dictionary = shared_progress
	for key: String in progress:
		shared_dict[key] = progress[key]

	_append_to_wal({"action": "update_shared_campaign_progress", "params": progress})
	save_profile(true)  # Immediate save for campaign progress
	data_changed.emit()

## Check if onboarding is complete (convenience method)
func is_onboarding_complete() -> bool:
	var shared_progress: Dictionary = get_shared_campaign_progress()
	var completed: Array = shared_progress.get("completed_battles", [])
	# Onboarding is complete when caravan_tutorial is done (last event)
	return "event_caravan_tutorial" in completed

## =============================================================================
## METADATA OPERATIONS
## =============================================================================

func get_profile_meta() -> Dictionary:
	var empty_meta: Dictionary = {}
	return _data.get("meta", empty_meta)

func update_profile_meta(meta: Dictionary) -> void:
	var empty_meta_update: Dictionary = {}
	var current_meta_variant: Variant = _data.get("meta", empty_meta_update)
	if current_meta_variant is Dictionary:
		var meta_dict: Dictionary = current_meta_variant
		for key: String in meta:
			var meta_value_variant: Variant = meta[key]
			meta_dict[key] = meta_value_variant
	_data["meta"] = current_meta_variant
	save_profile()  # Debounced
	data_changed.emit()

func get_settings() -> Dictionary:
	var empty_settings: Dictionary = {}
	return _data.get("settings", empty_settings)


## Safely convert a Variant to float (handles int from JSON parsing).
## JSON doesn't distinguish int/float for whole numbers, so 1.0 may load as 1.
static func safe_float(value: Variant, default: float) -> float:
	if value is float:
		return value
	if value is int:
		return float(value)
	return default


func update_settings(settings: Dictionary) -> void:
	var empty_settings_update: Dictionary = {}
	var current_settings_variant: Variant = _data.get("settings", empty_settings_update)
	if current_settings_variant is Dictionary:
		var settings_dict: Dictionary = current_settings_variant
		for key: String in settings:
			var settings_value_variant: Variant = settings[key]
			settings_dict[key] = settings_value_variant
	_data["settings"] = current_settings_variant
	save_profile(true)  # Immediate for settings
	data_changed.emit()

func get_last_match() -> Dictionary:
	var empty_match: Dictionary = {}
	return _data.get("last_match", empty_match)

func update_last_match(match_info: Dictionary) -> void:
	_data["last_match"] = match_info
	save_profile(true)  # Immediate for match results
	data_changed.emit()

## =============================================================================
## INTERNAL - FILE OPERATIONS
## =============================================================================

func _get_profile_dir(profile_id: String) -> String:
	return "user://profiles/" + profile_id

func _get_or_create_default_profile() -> String:
	# In future, this would list available profiles and let user choose
	# For now, always use "default"
	var default_id: String = "default"
	var profile_dir: String = _get_profile_dir(default_id)

	# Ensure directory exists
	var dir: DirAccess = DirAccess.open("user://")
	if dir:
		if not dir.dir_exists("profiles"):
			dir.make_dir("profiles")
		if not dir.dir_exists(profile_dir):
			dir.make_dir(profile_dir)

	return default_id

func _load_from_file(path: String) -> Variant:
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	if file == null:
		push_error("JsonProfileRepo: Failed to open file: " + path)
		return null

	var json_string: String = file.get_as_text()
	file.close()

	var json: JSON = JSON.new()
	var error: Error = json.parse(json_string)
	if error != OK:
		push_error("JsonProfileRepo: JSON parse error in " + path + ": " + json.get_error_message())
		return null

	return json.data

func _write_save() -> void:
	if not _pending_save:
		return

	_pending_save = false

	print("JsonProfileRepo: Writing save to disk...")

	var profile_dir: String = _get_profile_dir(_current_profile_id)
	var main_path: String = profile_dir + "/profile.json"
	var bak1_path: String = profile_dir + "/profile.bak1"
	var bak2_path: String = profile_dir + "/profile.bak2"
	var temp_path: String = profile_dir + "/profile.tmp"

	# Rotate backups BEFORE writing (preserves old data in case of crash)
	_rotate_backups(main_path, bak1_path, bak2_path)

	# Atomic write: write to temp, then rename
	if _atomic_write(_data, temp_path, main_path):
		profile_saved.emit(_current_profile_id)
		print("JsonProfileRepo: Save completed successfully")
	else:
		save_failed.emit("Failed to write save file")
		push_error("JsonProfileRepo: Save failed!")

func _atomic_write(save_data: Dictionary, temp_path: String, main_path: String) -> bool:
	# Write to temp file first
	var file: FileAccess = FileAccess.open(temp_path, FileAccess.WRITE)
	if file == null:
		push_error("JsonProfileRepo: Failed to create temp file")
		return false

	# Pretty print in dev, compact in release
	var json_string: String = JSON.stringify(save_data, "\t")
	file.store_string(json_string)
	file.close()

	# Verify temp file was written correctly
	if not FileAccess.file_exists(temp_path):
		push_error("JsonProfileRepo: Temp file disappeared!")
		return false

	# Rename temp to main save (atomic operation)
	var profile_dir: String = _get_profile_dir(_current_profile_id)
	var dir: DirAccess = DirAccess.open(profile_dir)
	if dir == null:
		push_error("JsonProfileRepo: Failed to open profile directory")
		return false

	# Remove old main save if it exists
	var err: Error = OK
	if FileAccess.file_exists(main_path):
		err = dir.remove(main_path.get_file())
		if err != OK:
			push_error("JsonProfileRepo: Failed to remove old save")
			return false

	# Rename temp to main
	err = dir.rename(temp_path.get_file(), main_path.get_file())
	if err != OK:
		push_error("JsonProfileRepo: Failed to rename temp to main")
		return false

	return true

func _rotate_backups(main_path: String, bak1_path: String, bak2_path: String) -> void:
	var profile_dir: String = _get_profile_dir(_current_profile_id)
	var dir: DirAccess = DirAccess.open(profile_dir)
	if dir == null:
		push_warning("JsonProfileRepo: Failed to open profile directory for backup rotation")
		return

	# Rotate: bak1 → bak2
	if FileAccess.file_exists(bak1_path):
		if FileAccess.file_exists(bak2_path):
			dir.remove(bak2_path.get_file())
		dir.rename(bak1_path.get_file(), bak2_path.get_file())

	# Copy main → bak1 (using full paths)
	if FileAccess.file_exists(main_path):
		dir.copy(main_path, bak1_path)

## =============================================================================
## INTERNAL - DATA MANAGEMENT
## =============================================================================

func _create_fresh_profile() -> void:
	_data = {
		"version": CURRENT_VERSION,
		"profile_id": _current_profile_id,
		"updated_at": Time.get_unix_time_from_system(),
		"catalog_version": "1.0.0",
		"resources": {
			"profile_id": _current_profile_id,
			"gold": 100,
			"gems": 0,  # Premium currency (purchased with real money)
			"essence": 0,
			"fragments": 0,
			"updated_at": Time.get_unix_time_from_system()
		},
		"collection": [
			# Start with ZERO cards - build collection through campaign
		],
		"unlocked_summoners": [],  # Array of summoner_id strings
		"summoner_instances": [],   # Array of SummonerInstance data (level, XP, modifiers)
		"decks": [],
		"deck_cards": [],
		"campaign_progress": {},  # Per-summoner structure: { "summoner_id": { completed_battles, current_battle, pending_reward } }
		"shared_campaign_progress": {},  # Account-wide progress for shared campaigns (onboarding)
		"shop_purchases": {},  # "shop_id::offering_id::refresh_epoch" -> purchase_count
		"shop_refresh_state": {  # Per-shop refresh tracking
			# "general": {"refresh_epoch": 0, "last_refresh_at": ""}
		},
		"cosmetics": {  # Cosmetic items (skins, card backs, UI themes)
			"owned": [],                # Array of cosmetic_id strings
			"equipped": {               # Active cosmetics by slot
				"card_back": "",
				"ui_theme": ""
			},
			"summoner_skins": {}        # summoner_id -> skin_id mapping
		},
		"emotes": {  # Battle emotes
			"owned": [],                # Array of emote_id strings
			"equipped": ["", "", "", ""]  # 4 emote slots
		},
		"meta": {
			"selected_deck": "",
			"tutorial_flags": {},
			"achievements": {},
			"analytics_opt_in": false
		},
		"last_match": {
			"seed": null,
			"result": null,
			"duration_s": null
		},
		"settings": {
			"sfx_volume": 1.0,
			"music_volume": 1.0,
			"lang": "en"
		},
		"wal": []  # Write-ahead log for sync
	}

func _create_card_instance(catalog_id: String, rarity: String) -> Dictionary:
	var result: Dictionary = {
		"id": _generate_uuid(),
		"profile_id": _current_profile_id,
		"catalog_id": catalog_id,
		"rarity": rarity,
		"level": 1,         # Card progression level (1-10)
		"xp": 0,            # XP towards next level
		"upgrades": [],     # Array of chosen upgrade IDs
		"roll_json": null,
		"created_at": Time.get_unix_time_from_system()
	}
	return result

func _migrate_if_needed() -> void:
	var version_variant: Variant = _data.get("version", 0)
	if version_variant is int:
		var ver_int: int = version_variant
		if ver_int < CURRENT_VERSION:
			print("JsonProfileRepo: Migrating save from version %d to %d" % [ver_int, CURRENT_VERSION])
			_migrate(ver_int)
			_data["version"] = CURRENT_VERSION

func _migrate(from_version: int) -> void:
	match from_version:
		0:
			# Version 0 → 1 migration (legacy)
			pass
		1:
			# Version 1 → 2 migration: Add card progression fields
			_migrate_v1_to_v2()
		2:
			# Version 2 → 3 migration: Move onboarding battles to shared progress
			_migrate_v2_to_v3()
		3:
			# Version 3 → 4 migration: Add cosmetics and emotes
			_migrate_v3_to_v4()
		4:
			# Version 4 → 5 migration: Campaign-scoped gold
			_migrate_v4_to_v5()
		_:
			push_warning("JsonProfileRepo: No migration defined for version " + str(from_version))

## Migration: Add level, xp, upgrades to existing card instances
func _migrate_v1_to_v2() -> void:
	print("JsonProfileRepo: Migrating cards to include progression fields...")
	var collection_variant: Variant = _data.get("collection", [])
	if not collection_variant is Array:
		return
	var coll_array: Array = collection_variant

	for i: int in range(coll_array.size()):
		var card: Variant = coll_array[i]
		if card is Dictionary:
			var card_dict: Dictionary = card
			# Add progression fields if missing
			if not card_dict.has("level"):
				card_dict["level"] = 1
			if not card_dict.has("xp"):
				card_dict["xp"] = 0
			if not card_dict.has("upgrades"):
				card_dict["upgrades"] = []
			coll_array[i] = card_dict

	_data["collection"] = coll_array
	print("JsonProfileRepo: Card migration complete - %d cards updated" % coll_array.size())

## Migration: Move onboarding battles from per-summoner to shared progress
func _migrate_v2_to_v3() -> void:
	print("JsonProfileRepo: Migrating onboarding progress to shared storage...")

	# Onboarding battle IDs that should be shared
	var onboarding_battle_ids: Array[String] = [
		"event_affinity",
		"event_first_summon",
		"first_trial",
		"charge_tutorial",
		"event_caravan_tutorial"
	]

	# Initialize shared progress if needed
	if not _data.has("shared_campaign_progress"):
		_data["shared_campaign_progress"] = {"completed_battles": [], "current_battle": null}

	var shared_progress: Dictionary = _data["shared_campaign_progress"]
	var shared_completed: Array = shared_progress.get("completed_battles", [])

	# Scan all summoner progress for onboarding battles
	var campaign_progress: Variant = _data.get("campaign_progress", {})
	if not campaign_progress is Dictionary:
		print("JsonProfileRepo: No campaign progress to migrate")
		return

	var progress_dict: Dictionary = campaign_progress
	var migrated_count: int = 0

	for summoner_id: String in progress_dict.keys():
		var summoner_progress: Variant = progress_dict.get(summoner_id)
		if not summoner_progress is Dictionary:
			continue

		var summoner_dict: Dictionary = summoner_progress
		var completed_battles: Variant = summoner_dict.get("completed_battles", [])
		if not completed_battles is Array:
			continue

		var completed_array: Array = completed_battles
		var battles_to_remove: Array[String] = []

		# Find onboarding battles in this summoner's progress
		for battle_id: Variant in completed_array:
			if battle_id is String:
				var battle_str: String = battle_id
				if battle_str in onboarding_battle_ids:
					# Add to shared if not already there
					if battle_str not in shared_completed:
						shared_completed.append(battle_str)
						migrated_count += 1
					battles_to_remove.append(battle_str)

		# Remove onboarding battles from summoner's progress
		for battle_to_remove: String in battles_to_remove:
			completed_array.erase(battle_to_remove)

		summoner_dict["completed_battles"] = completed_array

	shared_progress["completed_battles"] = shared_completed
	_data["shared_campaign_progress"] = shared_progress

	print("JsonProfileRepo: Migrated %d onboarding battles to shared progress" % migrated_count)

## Migration: Add cosmetics and emotes structures
func _migrate_v3_to_v4() -> void:
	print("JsonProfileRepo: Adding cosmetics and emotes structures...")

	# Add cosmetics if missing
	if not _data.has("cosmetics"):
		_data["cosmetics"] = {
			"owned": [],
			"equipped": {
				"card_back": "",
				"ui_theme": ""
			},
			"summoner_skins": {}
		}

	# Add emotes if missing
	if not _data.has("emotes"):
		_data["emotes"] = {
			"owned": [],
			"equipped": ["", "", "", ""]
		}

	print("JsonProfileRepo: Cosmetics and emotes migration complete")

## Migration: Move account gold to campaign progress (campaign-scoped gold)
func _migrate_v4_to_v5() -> void:
	print("JsonProfileRepo: Migrating to campaign-scoped gold...")

	# Get current account-wide gold
	var resources: Variant = _data.get("resources", {})
	if not resources is Dictionary:
		resources = {}
	var resources_dict: Dictionary = resources
	var account_gold: int = resources_dict.get("gold", 0)

	if account_gold <= 0:
		print("JsonProfileRepo: No account gold to migrate")
		return

	# Find active summoner with an in-progress campaign
	var campaign_progress: Variant = _data.get("campaign_progress", {})
	if not campaign_progress is Dictionary:
		print("JsonProfileRepo: No campaign progress to migrate gold into - gold will be lost")
		resources_dict["gold"] = 0
		_data["resources"] = resources_dict
		return

	var progress_dict: Dictionary = campaign_progress

	# Migrate gold to the first summoner with campaign progress
	# (In practice, there's usually only one active campaign at a time)
	var migrated: bool = false
	for summoner_id: String in progress_dict.keys():
		var summoner_progress: Variant = progress_dict.get(summoner_id)
		if not summoner_progress is Dictionary:
			continue

		var summoner_dict: Dictionary = summoner_progress
		# Only migrate if summoner has some campaign activity (completed battles or pending reward)
		var completed: Array = summoner_dict.get("completed_battles", [])
		var pending: Variant = summoner_dict.get("pending_reward")

		if completed.size() > 0 or pending != null:
			# Add gold to this summoner's campaign progress
			summoner_dict["gold"] = account_gold
			progress_dict[summoner_id] = summoner_dict
			migrated = true
			print("JsonProfileRepo: Migrated %d gold to summoner '%s' campaign" % [account_gold, summoner_id])
			break

	if not migrated:
		print("JsonProfileRepo: No active campaign found - account gold (%d) will be lost" % account_gold)

	# Clear account-wide gold (it's now campaign-scoped)
	resources_dict["gold"] = 0
	_data["resources"] = resources_dict
	_data["campaign_progress"] = progress_dict

	print("JsonProfileRepo: Campaign-scoped gold migration complete")

## =============================================================================
## INTERNAL - WRITE-AHEAD LOG
## =============================================================================

func _append_to_wal(entry: Dictionary) -> void:
	var wal_variant: Variant = _data.get("wal", [])
	if not wal_variant is Array:
		wal_variant = []
	var wal_array: Array = wal_variant

	var entry_action_variant: Variant = entry.get("action")
	var entry_params_variant: Variant = entry.get("params")
	var wal_entry: Dictionary = {
		"op_id": _generate_uuid(),
		"profile_id": _current_profile_id,
		"action": entry_action_variant,
		"params": entry_params_variant,
		"timestamp": Time.get_unix_time_from_system()
	}

	wal_array.append(wal_entry)
	_data["wal"] = wal_array

	# Trim WAL if it gets too large (keep last 100 entries)
	if wal_array.size() > 100:
		var trimmed_variant: Variant = wal_array.slice(-100)
		if trimmed_variant is Array:
			var trimmed_array: Array = trimmed_variant
			_data["wal"] = trimmed_array

## =============================================================================
## INTERNAL - UTILITIES
## =============================================================================

func _generate_uuid() -> String:
	# UUID-like string with improved entropy for instance IDs
	var unix_time: int = Time.get_unix_time_from_system()
	var usec: int = Time.get_ticks_usec()
	var rand1: int = randi()
	var rand2: int = randi()
	return "%x-%x-%x-%x" % [unix_time, usec, rand1, rand2]

func _on_save_timer_timeout() -> void:
	if _pending_save:
		_write_save()
