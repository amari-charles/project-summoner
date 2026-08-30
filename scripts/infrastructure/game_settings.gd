extends Node

## Applies and persists player-facing settings that are shared across screens.

signal setting_changed(key: StringName, value: Variant)

const WINDOW_MODE_WINDOWED: String = "windowed"
const WINDOW_MODE_BORDERLESS: String = "borderless"
const WINDOW_MODE_FULLSCREEN: String = "fullscreen"

const DEFAULTS: Dictionary = {
	"master_volume": 1.0,
	"music_volume": 1.0,
	"sfx_volume": 1.0,
	"mute_when_unfocused": false,
	"window_mode": WINDOW_MODE_FULLSCREEN,
	"resolution_width": 1920,
	"resolution_height": 1080,
	"vsync_enabled": true,
	"fps_limit": 60,
	"edge_pan_enabled": true,
	"camera_speed": 1.0,
	"reduce_camera_motion": false,
	"ui_scale": 1.0,
	"lang": "en",
}

var _settings: Dictionary = {}


func _ready() -> void:
	refresh()
	call_deferred("apply_all")


func refresh() -> void:
	_settings = DEFAULTS.duplicate(true)
	_settings.merge(ProfileRepoApi.get_settings_dict(), true)


func get_value(key: StringName) -> Variant:
	return _settings.get(String(key), DEFAULTS.get(String(key)))


func set_value(key: StringName, value: Variant) -> void:
	var string_key: String = String(key)
	if not DEFAULTS.has(string_key):
		push_warning("GameSettings: Unknown setting '%s'" % string_key)
		return
	_settings[string_key] = value
	ProfileRepoApi.update_settings_dict({string_key: value})
	_apply_setting(key, value)
	setting_changed.emit(key, value)


func reset_to_defaults() -> void:
	_settings = DEFAULTS.duplicate(true)
	ProfileRepoApi.update_settings_dict(_settings)
	apply_all()
	for key: String in _settings:
		setting_changed.emit(StringName(key), _settings[key])


func apply_all() -> void:
	for key: String in _settings:
		_apply_setting(StringName(key), _settings[key])


func _apply_setting(key: StringName, value: Variant) -> void:
	match key:
		&"window_mode":
			_apply_window_mode(SafeTypeUtils.string(value, WINDOW_MODE_FULLSCREEN))
		&"resolution_width", &"resolution_height":
			_apply_resolution()
		&"vsync_enabled":
			if not _is_headless():
				DisplayServer.window_set_vsync_mode(
					DisplayServer.VSYNC_ENABLED if SafeTypeUtils.bool_val(value, true)
					else DisplayServer.VSYNC_DISABLED
				)
		&"fps_limit":
			Engine.max_fps = maxi(SafeTypeUtils.int_val(value, 60), 0)
		&"ui_scale":
			if not _is_headless():
				get_tree().root.content_scale_factor = clampf(
					SafeTypeUtils.float_val(value, 1.0),
					0.8,
					1.3
				)


func _apply_window_mode(mode: String) -> void:
	if _is_headless():
		return
	match mode:
		WINDOW_MODE_WINDOWED:
			DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, false)
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
			_apply_resolution()
		WINDOW_MODE_BORDERLESS:
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
			DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, true)
		_:
			DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, false)
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_EXCLUSIVE_FULLSCREEN)


func _apply_resolution() -> void:
	if _is_headless() or SafeTypeUtils.string(
		_settings.get("window_mode"),
		WINDOW_MODE_FULLSCREEN
	) != WINDOW_MODE_WINDOWED:
		return
	var width: int = SafeTypeUtils.int_val(_settings.get("resolution_width"), 1920)
	var height: int = SafeTypeUtils.int_val(_settings.get("resolution_height"), 1080)
	DisplayServer.window_set_size(Vector2i(width, height))


func _is_headless() -> bool:
	return DisplayServer.get_name() == "headless"
