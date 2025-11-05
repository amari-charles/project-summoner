extends "res://scripts/data/profile_repository.gd"
class_name JsonProfileRepo

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
const AUTOSAVE_DELAY = 0.5  # Seconds of inactivity before autosave

## Current save version for migrations
const CURRENT_VERSION = 1

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
	var default_profile_id = _get_or_create_default_profile()
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

	var profile_dir = _get_profile_dir(profile_id)
	var main_path = profile_dir + "/profile.json"
	var bak1_path = profile_dir + "/profile.bak1"
	var bak2_path = profile_dir + "/profile.bak2"

	# Try main save first
	if FileAccess.file_exists(main_path):
		var loaded_data = _load_from_file(main_path)
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
		var loaded_data = _load_from_file(bak1_path)
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
		var loaded_data = _load_from_file(bak2_path)
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

func reset_profile() -> void:
	print("JsonProfileRepo: Resetting profile...")
	_create_fresh_profile()
	save_profile(true)
	data_changed.emit()

func snapshot() -> Dictionary:
	return _data.duplicate(true)

## =============================================================================
## RESOURCE OPERATIONS
## =============================================================================

func get_resources() -> Dictionary:
	return _data.get("resources", {})

func update_resources(delta: Dictionary) -> void:
	var resources = _data.get("resources", {})

	for key in delta:
		if key in resources:
			resources[key] += delta[key]
			# Clamp to prevent negative (except for testing)
			if resources[key] < 0:
				push_warning("JsonProfileRepo: Resource '%s' went negative (%d), clamping to 0" % [key, resources[key]])
				resources[key] = 0

	_data["resources"]["updated_at"] = Time.get_unix_time_from_system()

	# Log to WAL
	_append_to_wal({
		"action": "update_resources",
		"params": delta
	})

	save_profile()  # Debounced
	data_changed.emit()

## =============================================================================
## CARD COLLECTION OPERATIONS
## =============================================================================

func grant_cards(cards: Array) -> Array:
	var collection = _data.get("collection", [])
	var instance_ids = []

	for card in cards:
		var instance = {
			"id": _generate_uuid(),
			"profile_id": _current_profile_id,
			"catalog_id": card.get("catalog_id", ""),
			"rarity": card.get("rarity", "common"),
			"roll_json": null,  # Future: stat rolls
			"created_at": Time.get_unix_time_from_system()
		}
		collection.append(instance)
		instance_ids.append(instance["id"])

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
	var collection = _data.get("collection", [])
	var found_index = -1

	for i in range(collection.size()):
		if collection[i].get("id") == card_instance_id:
			found_index = i
			break

	if found_index == -1:
		push_warning("JsonProfileRepo: Card instance '%s' not found" % card_instance_id)
		return false

	collection.remove_at(found_index)
	_data["collection"] = collection

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
	var collection = _data.get("collection", [])
	var count = 0
	for card in collection:
		if card.get("catalog_id") == catalog_id:
			count += 1
	return count

func get_card(card_instance_id: String) -> Dictionary:
	var collection = _data.get("collection", [])
	for card in collection:
		if card.get("id") == card_instance_id:
			return card
	return {}

## =============================================================================
## DECK OPERATIONS
## =============================================================================

func upsert_deck(deck: Dictionary) -> String:
	var decks = _data.get("decks", [])
	var deck_id = deck.get("id", "")

	# If no ID, create new deck
	if deck_id == "":
		deck_id = _generate_uuid()
		var new_deck = {
			"id": deck_id,
			"profile_id": _current_profile_id,
			"name": deck.get("name", "Untitled Deck"),
			"created_at": Time.get_unix_time_from_system()
		}
		decks.append(new_deck)

		# Create deck_cards entries
		var deck_cards = _data.get("deck_cards", [])
		var card_instance_ids = deck.get("card_instance_ids", [])
		for i in range(card_instance_ids.size()):
			deck_cards.append({
				"deck_id": deck_id,
				"card_instance_id": card_instance_ids[i],
				"slot_index": i
			})
		_data["deck_cards"] = deck_cards
	else:
		# Update existing deck
		var found = false
		for i in range(decks.size()):
			if decks[i].get("id") == deck_id:
				decks[i]["name"] = deck.get("name", decks[i]["name"])
				found = true
				break

		if not found:
			push_warning("JsonProfileRepo: Deck '%s' not found for update" % deck_id)
			return ""

		# Update deck_cards
		var deck_cards = _data.get("deck_cards", [])
		# Remove old entries
		var new_deck_cards = []
		for dc in deck_cards:
			if dc.get("deck_id") != deck_id:
				new_deck_cards.append(dc)
		# Add new entries
		var card_instance_ids = deck.get("card_instance_ids", [])
		for i in range(card_instance_ids.size()):
			new_deck_cards.append({
				"deck_id": deck_id,
				"card_instance_id": card_instance_ids[i],
				"slot_index": i
			})
		_data["deck_cards"] = new_deck_cards

	_data["decks"] = decks

	save_profile()  # Debounced
	data_changed.emit()

	return deck_id

func delete_deck(deck_id: String) -> bool:
	var decks = _data.get("decks", [])
	var found_index = -1

	for i in range(decks.size()):
		if decks[i].get("id") == deck_id:
			found_index = i
			break

	if found_index == -1:
		push_warning("JsonProfileRepo: Deck '%s' not found" % deck_id)
		return false

	decks.remove_at(found_index)
	_data["decks"] = decks

	# Remove deck_cards entries
	var deck_cards = _data.get("deck_cards", [])
	var new_deck_cards = []
	for dc in deck_cards:
		if dc.get("deck_id") != deck_id:
			new_deck_cards.append(dc)
	_data["deck_cards"] = new_deck_cards

	save_profile()  # Debounced
	data_changed.emit()

	return true

func list_decks() -> Array:
	var decks = _data.get("decks", [])
	var deck_cards = _data.get("deck_cards", [])

	# Enrich decks with card_instance_ids
	var enriched_decks = []
	for deck in decks:
		var enriched = deck.duplicate()
		var cards = []
		for dc in deck_cards:
			if dc.get("deck_id") == deck.get("id"):
				cards.append(dc.get("card_instance_id"))
		enriched["card_instance_ids"] = cards
		enriched_decks.append(enriched)

	return enriched_decks

func get_deck(deck_id: String) -> Dictionary:
	var decks = list_decks()
	for deck in decks:
		if deck.get("id") == deck_id:
			return deck
	return {}

## =============================================================================
## METADATA OPERATIONS
## =============================================================================

func get_meta() -> Dictionary:
	return _data.get("meta", {})

func update_meta(meta: Dictionary) -> void:
	var current_meta = _data.get("meta", {})
	for key in meta:
		current_meta[key] = meta[key]
	_data["meta"] = current_meta
	save_profile()  # Debounced
	data_changed.emit()

func get_settings() -> Dictionary:
	return _data.get("settings", {})

func update_settings(settings: Dictionary) -> void:
	var current_settings = _data.get("settings", {})
	for key in settings:
		current_settings[key] = settings[key]
	_data["settings"] = current_settings
	save_profile(true)  # Immediate for settings
	data_changed.emit()

func get_last_match() -> Dictionary:
	return _data.get("last_match", {})

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
	var default_id = "default"
	var profile_dir = _get_profile_dir(default_id)

	# Ensure directory exists
	var dir = DirAccess.open("user://")
	if dir:
		if not dir.dir_exists("profiles"):
			dir.make_dir("profiles")
		if not dir.dir_exists(profile_dir):
			dir.make_dir(profile_dir)

	return default_id

func _load_from_file(path: String) -> Variant:
	var file = FileAccess.open(path, FileAccess.READ)
	if file == null:
		push_error("JsonProfileRepo: Failed to open file: " + path)
		return null

	var json_string = file.get_as_text()
	file.close()

	var json = JSON.new()
	var error = json.parse(json_string)
	if error != OK:
		push_error("JsonProfileRepo: JSON parse error in " + path + ": " + json.get_error_message())
		return null

	return json.data

func _write_save() -> void:
	if not _pending_save:
		return

	_pending_save = false

	print("JsonProfileRepo: Writing save to disk...")

	var profile_dir = _get_profile_dir(_current_profile_id)
	var main_path = profile_dir + "/profile.json"
	var bak1_path = profile_dir + "/profile.bak1"
	var bak2_path = profile_dir + "/profile.bak2"
	var temp_path = profile_dir + "/profile.tmp"

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
	var file = FileAccess.open(temp_path, FileAccess.WRITE)
	if file == null:
		push_error("JsonProfileRepo: Failed to create temp file")
		return false

	# Pretty print in dev, compact in release
	var json_string = JSON.stringify(save_data, "\t")
	file.store_string(json_string)
	file.close()

	# Verify temp file was written correctly
	if not FileAccess.file_exists(temp_path):
		push_error("JsonProfileRepo: Temp file disappeared!")
		return false

	# Rename temp to main save (atomic operation)
	var profile_dir = _get_profile_dir(_current_profile_id)
	var dir = DirAccess.open(profile_dir)
	if dir == null:
		push_error("JsonProfileRepo: Failed to open profile directory")
		return false

	# Remove old main save if it exists
	if FileAccess.file_exists(main_path):
		var err = dir.remove(main_path.get_file())
		if err != OK:
			push_error("JsonProfileRepo: Failed to remove old save")
			return false

	# Rename temp to main
	var err = dir.rename(temp_path.get_file(), main_path.get_file())
	if err != OK:
		push_error("JsonProfileRepo: Failed to rename temp to main")
		return false

	return true

func _rotate_backups(main_path: String, bak1_path: String, bak2_path: String) -> void:
	var profile_dir = _get_profile_dir(_current_profile_id)
	var dir = DirAccess.open(profile_dir)
	if dir == null:
		push_warning("JsonProfileRepo: Failed to open profile directory for backup rotation")
		return

	# Rotate: bak1 → bak2
	if FileAccess.file_exists(bak1_path):
		if FileAccess.file_exists(bak2_path):
			dir.remove(bak2_path.get_file())
		dir.rename(bak1_path.get_file(), bak2_path.get_file())

	# Copy main → bak1
	if FileAccess.file_exists(main_path):
		dir.copy(main_path.get_file(), bak1_path.get_file())

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
			# Starter cards (3x Warrior, 3x Archer)
			_create_card_instance("warrior", "common"),
			_create_card_instance("warrior", "common"),
			_create_card_instance("warrior", "common"),
			_create_card_instance("archer", "common"),
			_create_card_instance("archer", "common"),
			_create_card_instance("archer", "common"),
		],
		"decks": [],
		"deck_cards": [],
		"meta": {
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
	return {
		"id": _generate_uuid(),
		"profile_id": _current_profile_id,
		"catalog_id": catalog_id,
		"rarity": rarity,
		"roll_json": null,
		"created_at": Time.get_unix_time_from_system()
	}

func _migrate_if_needed() -> void:
	var version = _data.get("version", 0)
	if version < CURRENT_VERSION:
		print("JsonProfileRepo: Migrating save from version %d to %d" % [version, CURRENT_VERSION])
		_migrate(version)
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
	var wal = _data.get("wal", [])

	var wal_entry = {
		"op_id": _generate_uuid(),
		"profile_id": _current_profile_id,
		"action": entry.get("action"),
		"params": entry.get("params"),
		"timestamp": Time.get_unix_time_from_system()
	}

	wal.append(wal_entry)
	_data["wal"] = wal

	# Trim WAL if it gets too large (keep last 100 entries)
	if wal.size() > 100:
		_data["wal"] = wal.slice(-100)

## =============================================================================
## INTERNAL - UTILITIES
## =============================================================================

func _generate_uuid() -> String:
	# Simple UUID-like string for instance IDs
	var timestamp = Time.get_ticks_msec()
	var random = randi()
	return "%x-%x" % [timestamp, random]

func _on_save_timer_timeout() -> void:
	if _pending_save:
		_write_save()
