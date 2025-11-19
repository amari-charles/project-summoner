extends Node
class_name LocalizationService

## LocalizationService - Global localization system (Autoload: "Loc")
##
## Provides key-based string translation with parameter substitution.
## Supports flat and nested JSON structures, with English fallback.
##
## Usage:
##   Loc.t("menu.play") → "Play"
##   Loc.t("battle.damage", {"amount": 5}) → "Deal 5 damage"

signal language_changed(new_locale: String)

# Available languages
const SUPPORTED_LANGUAGES: Array[String] = ["en", "es"]
const DEFAULT_LANGUAGE: String = "en"

# Current language
var _current_language: String = DEFAULT_LANGUAGE

# Loaded translation dictionaries
var _translations: Dictionary = {}  # locale → dictionary
var _fallback_translations: Dictionary = {}  # English fallback


func _ready() -> void:
	print("LocalizationService: Initializing...")

	# Load English as fallback
	_fallback_translations = _load_language_file(DEFAULT_LANGUAGE)

	# Load current language (same as default for now)
	_translations = _fallback_translations

	print("LocalizationService: Loaded %d English strings" % _fallback_translations.size())


## Translate a key with optional parameter substitution
##
## Supports both flat keys ("menu.play") and nested access.
## Replaces {param} placeholders with values from params dict.
##
## Examples:
##   t("menu.play") → "Play"
##   t("battle.damage", {"amount": 5}) → "Deal 5 damage"
##
## Returns "[MISSING:key]" if key not found in any dictionary.
func t(key: String, params: Dictionary = {}) -> String:
	var text: String = ""

	# Try current language first
	text = _lookup_key(key, _translations)

	# Fallback to English if not found
	if text == "":
		text = _lookup_key(key, _fallback_translations)

	# Return missing key marker if still not found
	if text == "":
		push_warning("LocalizationService: Missing translation key: %s" % key)
		return "[MISSING:%s]" % key

	# Substitute parameters
	for param_key: String in params.keys():
		var placeholder: String = "{%s}" % param_key
		var value: String = str(params[param_key])
		text = text.replace(placeholder, value)

	return text


## Change current language
##
## Loads the specified language file and emits language_changed signal.
## UI components listening to this signal should refresh their text.
func set_language(locale: String) -> void:
	if not locale in SUPPORTED_LANGUAGES:
		push_warning("LocalizationService: Unsupported language: %s" % locale)
		return

	if locale == _current_language:
		return  # Already using this language

	print("LocalizationService: Switching language to %s" % locale)

	_current_language = locale
	_translations = _load_language_file(locale)

	language_changed.emit(locale)


## Get current language code
func get_language() -> String:
	return _current_language


## Load a language JSON file
##
## Supports both flat and nested JSON structures.
## Returns empty dictionary if file doesn't exist or is invalid.
func _load_language_file(locale: String) -> Dictionary:
	var file_path: String = "res://localization/data/%s.json" % locale

	if not FileAccess.file_exists(file_path):
		push_warning("LocalizationService: Language file not found: %s" % file_path)
		return {}

	var file: FileAccess = FileAccess.open(file_path, FileAccess.READ)
	if file == null:
		push_error("LocalizationService: Failed to open language file: %s" % file_path)
		return {}

	var json_text: String = file.get_as_text()
	file.close()

	var json: JSON = JSON.new()
	var parse_result: Error = json.parse(json_text)

	if parse_result != OK:
		push_error("LocalizationService: JSON parse error in %s at line %d: %s" % [file_path, json.get_error_line(), json.get_error_message()])
		return {}

	var data: Variant = json.get_data()
	if not data is Dictionary:
		push_error("LocalizationService: Language file must contain a JSON object: %s" % file_path)
		return {}

	# Flatten nested dictionaries into dot notation
	var flattened: Dictionary = _flatten_dictionary(data, "")

	return flattened


## Flatten nested dictionary into dot notation
##
## Converts {"menu": {"play": "Play"}} → {"menu.play": "Play"}
## Also preserves flat keys: {"menu.play": "Play"} → {"menu.play": "Play"}
func _flatten_dictionary(input: Variant, prefix: String = "") -> Dictionary:
	var result: Dictionary = {}

	# Safety check: input must be a Dictionary
	if not input is Dictionary:
		return result

	var dict: Dictionary = input

	for key: String in dict.keys():
		var full_key: String = prefix + key if prefix == "" else prefix + "." + key
		var value: Variant = dict[key]

		if value is Dictionary:
			# Recursively flatten nested dictionaries
			var nested: Dictionary = _flatten_dictionary(value, full_key)
			for nested_key: String in nested.keys():
				result[nested_key] = nested[nested_key]
		else:
			# Store string value
			result[full_key] = str(value)

	return result


## Look up a key in a flattened dictionary
func _lookup_key(key: String, dict: Dictionary) -> String:
	if dict.has(key):
		return dict[key]
	return ""
