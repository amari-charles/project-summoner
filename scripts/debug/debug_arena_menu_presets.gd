extends RefCounted
class_name DebugArenaMenuPresets

## Preset-driven debug arena quick-launch list catalog.

const PRESETS_PATH: String = "res://data/debug/debug_arena_menu_presets.json"
const FALLBACK_DEFAULT_PRESET_ID: String = "all_test_arena"


static func get_default_preset_id() -> String:
	var parsed: Dictionary = _parse_catalog()
	var default_id: String = SafeTypeUtils.string(parsed.get("default_preset_id", ""), "")
	if not default_id.is_empty():
		return default_id
	return FALLBACK_DEFAULT_PRESET_ID


static func has_preset(preset_id: String) -> bool:
	return _find_preset(preset_id).size() > 0


static func get_preset_entries(preset_id: String) -> Array[Dictionary]:
	var preset: Dictionary = _find_preset(preset_id)
	if preset.is_empty():
		preset = _find_preset(get_default_preset_id())
	if preset.is_empty():
		return []
	return _normalize_entries(SafeTypeUtils.array(preset.get("entries", [])))


static func get_available_presets() -> Array[Dictionary]:
	var parsed: Dictionary = _parse_catalog()
	var presets: Array = SafeTypeUtils.array(parsed.get("presets", []))
	var normalized: Array[Dictionary] = []
	for preset_var: Variant in presets:
		var preset: Dictionary = SafeTypeUtils.dict(preset_var)
		var id: String = SafeTypeUtils.string(preset.get("id", ""), "")
		if id.is_empty():
			continue
		normalized.append(
			{
				"id": id,
				"label": SafeTypeUtils.string(preset.get("label", id), id)
			}
		)
	return normalized


static func _find_preset(preset_id: String) -> Dictionary:
	if preset_id.is_empty():
		return {}
	var parsed: Dictionary = _parse_catalog()
	var presets: Array = SafeTypeUtils.array(parsed.get("presets", []))
	for preset_var: Variant in presets:
		var preset: Dictionary = SafeTypeUtils.dict(preset_var)
		if SafeTypeUtils.string(preset.get("id", ""), "") == preset_id:
			return preset
	return {}


static func _normalize_entries(entries: Array) -> Array[Dictionary]:
	var normalized: Array[Dictionary] = []
	for entry_var: Variant in entries:
		var entry: Dictionary = SafeTypeUtils.dict(entry_var)
		var battle_id: String = SafeTypeUtils.string(entry.get("battle_id", ""), "")
		if battle_id.is_empty():
			continue
		var label: String = SafeTypeUtils.string(entry.get("label", battle_id), battle_id)
		normalized.append(
			{
				"label": label,
				"battle_id": battle_id
			}
		)
	return normalized


static func _parse_catalog() -> Dictionary:
	var file: FileAccess = FileAccess.open(PRESETS_PATH, FileAccess.READ)
	if file == null:
		push_warning("DebugArenaMenuPresets: Missing preset catalog at %s" % PRESETS_PATH)
		return {}

	var text: String = file.get_as_text()
	file.close()
	var parsed: Variant = JSON.parse_string(text)
	if not parsed is Dictionary:
		push_warning("DebugArenaMenuPresets: Invalid preset catalog JSON")
		return {}
	return parsed
