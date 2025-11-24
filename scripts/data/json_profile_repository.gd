extends Node
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
const CURRENT_VERSION: int = 1

## Signals
signal profile_loaded(profile_id: String)
signal profile_saved(profile_id: String)
signal save_failed(error: String)
signal data_changed

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
	if immediate:
		_write_save()
	else:
		_pending_save = true
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
## HERO OPERATIONS
## =============================================================================

## Get list of unlocked hero IDs
func get_unlocked_heroes() -> Array:
	return _data.get("unlocked_heroes", [])

## Check if a specific hero is unlocked
func is_hero_unlocked(hero_id: String) -> bool:
	var unlocked: Array = _data.get("unlocked_heroes", [])
	return hero_id in unlocked

## Unlock a new hero
func unlock_hero(hero_id: String) -> bool:
	var unlocked: Array = _data.get("unlocked_heroes", [])
	if hero_id not in unlocked:
		unlocked.append(hero_id)
		_data["unlocked_heroes"] = unlocked
		_append_to_wal({"op": "unlock_hero", "hero_id": hero_id})
		save_profile()
		data_changed.emit()
		return true
	return false

## Set starting hero (called during onboarding)
## chosen_random: whether player selected "Random" option (passed to WAL for tracking)
func set_starting_hero(hero_id: String, chosen_random: bool) -> bool:
	# Validate this is called on a fresh profile
	var unlocked: Array = _data.get("unlocked_heroes", [])
	if not unlocked.is_empty():
		push_error("ProfileRepo: Cannot set starting hero - heroes already unlocked")
		return false

	# Add hero
	unlocked.append(hero_id)
	_data["unlocked_heroes"] = unlocked

	# Note: If chosen_random is true, the hero should have the
	# "fortune_favors_the_bold" modifier in its active_modifiers.
	# The caller should create the HeroInstance with this modifier.

	_append_to_wal({
		"op": "set_starting_hero",
		"hero_id": hero_id,
		"chosen_random": chosen_random
	})
	save_profile()
	data_changed.emit()
	return true

## =============================================================================
## HERO INSTANCE OPERATIONS (Typed Hero System)
## =============================================================================

## Get all hero instances
func get_hero_instances() -> Array:
	return _data.get("hero_instances", [])

## Get a specific hero instance by hero_id
func get_hero_instance(hero_id: String) -> Dictionary:
	var instances_variant: Variant = _data.get("hero_instances", [])
	if not instances_variant is Array:
		return {}
	var instances_array: Array = instances_variant

	for instance: Variant in instances_array:
		if instance is Dictionary:
			var instance_dict: Dictionary = instance
			var instance_hero_id_variant: Variant = instance_dict.get("hero_id")
			if instance_hero_id_variant == hero_id:
				return instance_dict
	return {}

## Save or update a hero instance
func save_hero_instance(hero_instance: HeroInstance) -> bool:
	if not hero_instance.is_valid():
		push_error("ProfileRepo: Cannot save invalid HeroInstance")
		return false

	var instances_variant: Variant = _data.get("hero_instances", [])
	if not instances_variant is Array:
		instances_variant = []
	var instances_array: Array = instances_variant

	var hero_data: Dictionary = hero_instance.to_dict()
	var hero_id: String = hero_data.get("hero_id", "")

	# Check if instance already exists
	var found_index: int = -1
	for i: int in range(instances_array.size()):
		var instance: Variant = instances_array[i]
		if instance is Dictionary:
			var instance_dict: Dictionary = instance
			var instance_hero_id_variant: Variant = instance_dict.get("hero_id")
			if instance_hero_id_variant == hero_id:
				found_index = i
				break

	if found_index >= 0:
		# Update existing
		instances_array[found_index] = hero_data
	else:
		# Add new
		instances_array.append(hero_data)
		# Also add to unlocked_heroes for legacy compatibility
		var unlocked: Array = _data.get("unlocked_heroes", [])
		if hero_id not in unlocked:
			unlocked.append(hero_id)
			_data["unlocked_heroes"] = unlocked

	_data["hero_instances"] = instances_array

	_append_to_wal({
		"action": "save_hero_instance",
		"params": {"hero_id": hero_id}
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
		var hero_id_variant: Variant = deck.get("hero_id", "")
		var hero_id: String = hero_id_variant if hero_id_variant is String else ""
		var new_deck: Dictionary = {
			"id": deck_id,
			"profile_id": _current_profile_id,
			"name": deck_name,
			"hero_id": hero_id,
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
					# Update hero_id if provided
					if deck.has("hero_id"):
						var hero_id_variant: Variant = deck.get("hero_id")
						if hero_id_variant is String:
							deck_dict["hero_id"] = hero_id_variant
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

	# Atomic write: write to temp, then rename
	if _atomic_write(_data, temp_path, main_path):
		_rotate_backups(main_path, bak1_path, bak2_path)
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
			"essence": 0,
			"fragments": 0,
			"updated_at": Time.get_unix_time_from_system()
		},
		"collection": [
			# Start with ZERO cards - build collection through campaign
		],
		"unlocked_heroes": [],  # Array of hero_id strings (legacy compatibility)
		"hero_instances": [],   # Array of HeroInstance data (level, XP, modifiers)
		"decks": [],
		"deck_cards": [],
		"campaign_progress": {
			"completed_battles": [],
			"current_battle": null
		},
		"shop_purchases": {},  # "shop_id::offering_id::refresh_epoch" -> purchase_count
		"shop_refresh_state": {  # Per-shop refresh tracking
			# "general": {"refresh_epoch": 0, "last_refresh_at": ""}
		},
		"meta": {
			"selected_deck": null,
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
			# Example: Version 0 → 1 migration
			# Add new fields, convert old data, etc.
			pass
		_:
			push_warning("JsonProfileRepo: No migration defined for version " + str(from_version))

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
	# Simple UUID-like string for instance IDs
	var timestamp: int = Time.get_ticks_msec()
	var random: int = randi()
	return "%x-%x" % [timestamp, random]

func _on_save_timer_timeout() -> void:
	if _pending_save:
		_write_save()
