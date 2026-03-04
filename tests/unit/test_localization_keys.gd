extends GutTest

## Unit Tests for Localization Key Validation
##
## Scans all GDScript files for Loc.t() calls and verifies
## all keys exist in en.json. Also validates known dynamic key patterns
## that use string concatenation (e.g., Loc.t("elements." + element)).

const SCRIPTS_DIR: String = "res://scripts"
const LOCALIZATION_DIR: String = "res://localization"
const EN_JSON_PATH: String = "res://localization/data/en.json"

# Known dynamic key patterns - these are constructed via string concatenation
# and must be explicitly validated since static analysis can't determine them.
# Element names must match C# Element enum in scripts/csharp/Infrastructure/Data/Cards/Element.cs
const DYNAMIC_ELEMENTS: Array[String] = [
	"neutral", "fire", "water", "wind", "earth", "lightning", "shadow",
	"poison", "life", "death", "occultist", "holy", "ice", "metal", "spirit"
]
const DYNAMIC_STAT_KEYS: Array[String] = ["stat_hp", "stat_damage", "stat_attack_speed", "stat_move_speed", "stat_spell_damage", "stat_spell_radius"]
const DYNAMIC_EQUIPMENT_SLOTS: Array[String] = ["wand", "ring1", "ring2", "robes"]

var _localization_keys: Dictionary = {}


func before_all() -> void:
	_localization_keys = _load_localization_keys()


## =============================================================================
## STATIC KEY TESTS
## =============================================================================

func test_all_static_loc_t_keys_exist_in_en_json() -> void:
	# Collect all Loc.t() keys used in the codebase
	var used_keys: Dictionary = _collect_used_keys(SCRIPTS_DIR)
	var used_keys_loc: Dictionary = _collect_used_keys(LOCALIZATION_DIR)

	# Merge both
	for key: String in used_keys_loc:
		used_keys[key] = true

	# Find missing keys
	var missing_keys: Array[String] = []
	for key: String in used_keys:
		if not _localization_keys.has(key):
			missing_keys.append(key)

	# Report all missing keys at once for easier debugging
	if not missing_keys.is_empty():
		missing_keys.sort()
		var error_msg: String = "Missing localization keys:\n"
		for key: String in missing_keys:
			error_msg += "  - %s\n" % key
		fail_test(error_msg)

	assert_true(missing_keys.is_empty(), "All Loc.t() keys should exist in en.json")


## =============================================================================
## DYNAMIC KEY PATTERN TESTS
## =============================================================================
## These tests validate keys constructed via string concatenation like:
##   Loc.t("elements." + element)
##   Loc.t("ui.collection." + stat_key)
## Since static analysis can't determine these, we explicitly enumerate them.

func test_all_element_keys_exist() -> void:
	# Pattern: Loc.t("elements." + element) in collection_screen.gd
	var missing: Array[String] = []

	for element: String in DYNAMIC_ELEMENTS:
		var key: String = "elements." + element
		if not _localization_keys.has(key):
			missing.append(key)

	if not missing.is_empty():
		fail_test("Missing element keys:\n  - " + "\n  - ".join(missing))

	assert_true(missing.is_empty(), "All element localization keys should exist")


func test_all_stat_label_keys_exist() -> void:
	# Pattern: Loc.t("ui.collection." + loc_key) in card_detail_modal.gd
	var missing: Array[String] = []

	for stat_key: String in DYNAMIC_STAT_KEYS:
		var key: String = "ui.collection." + stat_key
		if not _localization_keys.has(key):
			missing.append(key)

	if not missing.is_empty():
		fail_test("Missing stat label keys:\n  - " + "\n  - ".join(missing))

	assert_true(missing.is_empty(), "All stat label localization keys should exist")


func test_all_equipment_slot_keys_exist() -> void:
	# Pattern: Loc.t("ui.summoner_screen.equipment_slot_" + slot) in summoner_screen.gd
	var missing: Array[String] = []

	for slot: String in DYNAMIC_EQUIPMENT_SLOTS:
		var key: String = "ui.summoner_screen.equipment_slot_" + slot
		if not _localization_keys.has(key):
			missing.append(key)

	if not missing.is_empty():
		fail_test("Missing equipment slot keys:\n  - " + "\n  - ".join(missing))

	assert_true(missing.is_empty(), "All equipment slot localization keys should exist")


## =============================================================================
## DUPLICATE KEY TESTS
## =============================================================================

func test_en_json_has_no_duplicate_keys() -> void:
	var file: FileAccess = FileAccess.open(EN_JSON_PATH, FileAccess.READ)
	assert_not_null(file, "Should be able to open en.json")

	var json_text: String = file.get_as_text()
	file.close()

	var duplicates: Array[String] = _find_duplicate_json_keys(json_text)

	if not duplicates.is_empty():
		duplicates.sort()
		var error_msg: String = "Duplicate keys found in en.json:\n"
		for key: String in duplicates:
			error_msg += "  - %s\n" % key
		fail_test(error_msg)

	assert_true(duplicates.is_empty(), "en.json should have no duplicate keys")


## =============================================================================
## HELPER FUNCTIONS
## =============================================================================

## Load en.json and return flattened keys dictionary.
## Note: This intentionally duplicates the flattening logic from LocalizationService
## for test isolation - tests should not depend on the service being correctly loaded.
func _load_localization_keys() -> Dictionary:
	var file: FileAccess = FileAccess.open(EN_JSON_PATH, FileAccess.READ)
	if file == null:
		push_error("Could not open %s" % EN_JSON_PATH)
		return {}

	var json_text: String = file.get_as_text()
	file.close()

	var json: JSON = JSON.new()
	var err: Error = json.parse(json_text)
	if err != OK:
		push_error("JSON parse error in %s: %s" % [EN_JSON_PATH, json.get_error_message()])
		return {}

	var data: Variant = json.get_data()
	if not data is Dictionary:
		return {}

	return _flatten_dictionary(data, "")


## Recursively flatten nested dictionary to dot notation.
## Converts {"menu": {"play": "Play"}} → {"menu.play": "Play"}
func _flatten_dictionary(input: Dictionary, prefix: String) -> Dictionary:
	var result: Dictionary = {}

	for key: String in input:
		var full_key: String = prefix + key if prefix == "" else prefix + "." + key
		var value: Variant = input[key]

		if value is Dictionary:
			var nested: Dictionary = _flatten_dictionary(value, full_key)
			for nested_key: String in nested:
				result[nested_key] = nested[nested_key]
		else:
			result[full_key] = str(value)

	return result


## Recursively collect Loc.t() keys from all .gd files in directory
func _collect_used_keys(dir_path: String) -> Dictionary:
	var keys: Dictionary = {}
	var dir: DirAccess = DirAccess.open(dir_path)

	if dir == null:
		return keys

	dir.list_dir_begin()
	var file_name: String = dir.get_next()

	while file_name != "":
		if file_name.begins_with("."):
			file_name = dir.get_next()
			continue

		var full_path: String = dir_path.path_join(file_name)

		if dir.current_is_dir():
			var sub_keys: Dictionary = _collect_used_keys(full_path)
			for key: String in sub_keys:
				keys[key] = true
		elif file_name.ends_with(".gd"):
			var file_keys: Array = _extract_loc_t_keys(full_path)
			for key: String in file_keys:
				keys[key] = true

		file_name = dir.get_next()

	dir.list_dir_end()
	return keys


## Extract Loc.t("key") strings from a GDScript file.
## Skips dynamic keys (those using string concatenation) - these are
## validated separately by the dynamic key pattern tests.
func _extract_loc_t_keys(file_path: String) -> Array:
	var keys: Array = []
	var file: FileAccess = FileAccess.open(file_path, FileAccess.READ)

	if file == null:
		return keys

	var content: String = file.get_as_text()
	file.close()

	var search_start: int = 0
	var pattern: String = "Loc.t(\""

	while true:
		var pos: int = content.find(pattern, search_start)
		if pos == -1:
			break

		var key_start: int = pos + pattern.length()
		var key_end: int = content.find("\"", key_start)

		if key_end == -1:
			break

		var key: String = content.substr(key_start, key_end - key_start)

		# Skip dynamic keys - these are validated by dedicated tests:
		# - Contains % (format specifiers like Loc.t("%s.key" % var))
		# - Ends with . (partial key like Loc.t("elements." + element))
		# - Ends with _ (partial key like Loc.t("prefix_" + suffix))
		# - Empty strings
		var is_dynamic: bool = key.contains("%") or key.ends_with(".") or key.ends_with("_") or key.is_empty()
		if not is_dynamic:
			keys.append(key)

		search_start = key_end + 1

	return keys


## Find duplicate JSON keys at the same nesting level.
## Standard JSON parsers silently use the last value for duplicate keys,
## which can cause localization entries to be unexpectedly overwritten.
func _find_duplicate_json_keys(json_text: String) -> Array[String]:
	var duplicates: Array[String] = []
	var lines: PackedStringArray = json_text.split("\n")

	# Track keys at each indent level
	var indent_key_stacks: Dictionary = {}  # indent_level → Array of keys at that level

	var key_regex: RegEx = RegEx.new()
	key_regex.compile("^(\\s*)\"([^\"]+)\"\\s*:")

	for line: String in lines:
		var match_result: RegExMatch = key_regex.search(line)
		if match_result:
			var whitespace: String = match_result.get_string(1)
			var key: String = match_result.get_string(2)
			var indent: int = whitespace.length()

			# Clear keys at deeper indent levels when we go back up
			for level: int in indent_key_stacks.keys():
				if level > indent:
					indent_key_stacks.erase(level)

			# Check for duplicate at current level
			if not indent_key_stacks.has(indent):
				indent_key_stacks[indent] = []

			var keys_at_level: Array = indent_key_stacks[indent]
			if key in keys_at_level:
				if not key in duplicates:
					duplicates.append(key)
			else:
				keys_at_level.append(key)

	return duplicates
