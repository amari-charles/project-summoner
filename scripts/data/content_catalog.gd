extends Node

## Central content database - single source of truth for all game content
## Loads data from JSON files at startup
## Provides lookup methods for units, cards, projectiles
## Autoload as: /root/ContentCatalog

var units: Dictionary = {}  ## unit_id -> UnitData
var cards: Dictionary = {}  ## card_id -> CardData
var projectiles: Dictionary = {}  ## projectile_id -> ProjectileData

var _is_loaded: bool = false

signal content_loaded()

func _ready() -> void:
	print("ContentCatalog: Initializing...")
	_load_all_content()
	_validate_content()
	_is_loaded = true
	content_loaded.emit()
	print("ContentCatalog: Loaded %d units, %d cards, %d projectiles" % [
		units.size(),
		cards.size(),
		projectiles.size()
	])

## Load all content from JSON files
func _load_all_content() -> void:
	_load_units()
	_load_cards()
	_load_projectiles()

## Load units from data/units/*.json
func _load_units() -> void:
	var units_dir = "res://data/units/"
	var dir = DirAccess.open(units_dir)

	if not dir:
		push_warning("ContentCatalog: units directory not found: " + units_dir)
		return

	dir.list_dir_begin()
	var file_name = dir.get_next()

	while file_name != "":
		if file_name.ends_with(".json"):
			var file_path = units_dir + file_name
			var unit_data = _load_unit_from_file(file_path)
			if unit_data:
				units[unit_data.unit_id] = unit_data
		file_name = dir.get_next()

	dir.list_dir_end()

## Load single unit from JSON file
func _load_unit_from_file(file_path: String) -> UnitData:
	var file = FileAccess.open(file_path, FileAccess.READ)
	if not file:
		push_error("ContentCatalog: Failed to open file: " + file_path)
		return null

	var json_text = file.get_as_text()
	file.close()

	var json = JSON.new()
	var parse_result = json.parse(json_text)

	if parse_result != OK:
		push_error("ContentCatalog: JSON parse error in %s at line %d: %s" % [
			file_path,
			json.get_error_line(),
			json.get_error_message()
		])
		return null

	var data = json.get_data()
	if not data is Dictionary:
		push_error("ContentCatalog: JSON root is not a dictionary: " + file_path)
		return null

	return UnitData.from_dict(data)

## Load cards from data/cards/*.json
func _load_cards() -> void:
	var cards_dir = "res://data/cards/"
	var dir = DirAccess.open(cards_dir)

	if not dir:
		push_warning("ContentCatalog: cards directory not found: " + cards_dir)
		return

	dir.list_dir_begin()
	var file_name = dir.get_next()

	while file_name != "":
		if file_name.ends_with(".json"):
			var file_path = cards_dir + file_name
			var card_data = _load_card_from_file(file_path)
			if card_data:
				cards[card_data.card_id] = card_data
		file_name = dir.get_next()

	dir.list_dir_end()

## Load single card from JSON file
func _load_card_from_file(file_path: String) -> CardData:
	var file = FileAccess.open(file_path, FileAccess.READ)
	if not file:
		push_error("ContentCatalog: Failed to open file: " + file_path)
		return null

	var json_text = file.get_as_text()
	file.close()

	var json = JSON.new()
	var parse_result = json.parse(json_text)

	if parse_result != OK:
		push_error("ContentCatalog: JSON parse error in %s at line %d: %s" % [
			file_path,
			json.get_error_line(),
			json.get_error_message()
		])
		return null

	var data = json.get_data()
	if not data is Dictionary:
		push_error("ContentCatalog: JSON root is not a dictionary: " + file_path)
		return null

	return CardData.from_dict(data)

## Load projectiles from data/projectiles/*.json
func _load_projectiles() -> void:
	var proj_dir = "res://data/projectiles/"
	var dir = DirAccess.open(proj_dir)

	if not dir:
		push_warning("ContentCatalog: projectiles directory not found: " + proj_dir)
		return

	dir.list_dir_begin()
	var file_name = dir.get_next()

	while file_name != "":
		if file_name.ends_with(".json"):
			var file_path = proj_dir + file_name
			var proj_data = _load_projectile_from_file(file_path)
			if proj_data:
				projectiles[proj_data.projectile_id] = proj_data
		file_name = dir.get_next()

	dir.list_dir_end()

## Load single projectile from JSON file
func _load_projectile_from_file(file_path: String) -> ProjectileData:
	var file = FileAccess.open(file_path, FileAccess.READ)
	if not file:
		push_error("ContentCatalog: Failed to open file: " + file_path)
		return null

	var json_text = file.get_as_text()
	file.close()

	var json = JSON.new()
	var parse_result = json.parse(json_text)

	if parse_result != OK:
		push_error("ContentCatalog: JSON parse error in %s at line %d: %s" % [
			file_path,
			json.get_error_line(),
			json.get_error_message()
		])
		return null

	var data = json.get_data()
	if not data is Dictionary:
		push_error("ContentCatalog: JSON root is not a dictionary: " + file_path)
		return null

	return ProjectileData.from_dict(data)

## Validate all loaded content for consistency
func _validate_content() -> void:
	var errors: Array[String] = []

	# Check that all summon cards reference valid units
	for card_data in cards.values():
		if card_data.card_type == "summon":
			if card_data.unit_id.is_empty():
				errors.append("Summon card '%s' has no unit_id" % card_data.card_id)
			elif not units.has(card_data.unit_id):
				errors.append("Card '%s' references missing unit '%s'" % [
					card_data.card_id,
					card_data.unit_id
				])

	# Check that all ranged units reference valid projectiles
	for unit_data in units.values():
		if unit_data.is_ranged:
			if unit_data.projectile_id.is_empty():
				errors.append("Ranged unit '%s' has no projectile_id" % unit_data.unit_id)
			elif not projectiles.has(unit_data.projectile_id):
				errors.append("Unit '%s' references missing projectile '%s'" % [
					unit_data.unit_id,
					unit_data.projectile_id
				])

	# Check for invalid stats
	for unit_data in units.values():
		if unit_data.max_hp <= 0:
			errors.append("Unit '%s' has invalid HP: %.1f" % [unit_data.unit_id, unit_data.max_hp])
		if unit_data.attack_speed <= 0:
			errors.append("Unit '%s' has invalid attack speed: %.1f" % [unit_data.unit_id, unit_data.attack_speed])

	# Report errors
	if errors.size() > 0:
		push_error("ContentCatalog: Found %d validation errors:" % errors.size())
		for error in errors:
			push_error("  - %s" % error)
	else:
		print("ContentCatalog: All content validated successfully")

## Get unit data by ID
func get_unit(unit_id: String) -> UnitData:
	return units.get(unit_id)

## Get card data by ID
func get_card(card_id: String) -> CardData:
	return cards.get(card_id)

## Get projectile data by ID
func get_projectile(projectile_id: String) -> ProjectileData:
	return projectiles.get(projectile_id)

## Check if projectile exists
func has_projectile(projectile_id: String) -> bool:
	return projectiles.has(projectile_id)

## Check if content is loaded
func is_loaded() -> bool:
	return _is_loaded

## Get all units with specific tag
func get_units_with_tag(tag: String) -> Array[UnitData]:
	var result: Array[UnitData] = []
	for unit_data in units.values():
		if tag in unit_data.tags:
			result.append(unit_data)
	return result

## Get all cards of specific rarity
func get_cards_by_rarity(rarity: String) -> Array[CardData]:
	var result: Array[CardData] = []
	for card_data in cards.values():
		if card_data.rarity == rarity:
			result.append(card_data)
	return result

## Get all cards of specific type
func get_cards_by_type(card_type: String) -> Array[CardData]:
	var result: Array[CardData] = []
	for card_data in cards.values():
		if card_data.card_type == card_type:
			result.append(card_data)
	return result
