extends PanelContainer
class_name SettingsPanel

## Shared categorized settings surface for full-screen and in-battle use.

const INPUT_BINDINGS_PATH: String = "user://input_bindings.cfg"
const CATEGORIES: Array[StringName] = [
	&"audio", &"display", &"controls", &"gameplay", &"accessibility"
]
const REBINDABLE_ACTIONS: Array[StringName] = [
	&"move_left", &"move_right", &"move_up", &"move_down", &"interact"
]
const ACTION_LABEL_KEYS: Dictionary = {
	&"move_left": "ui.settings.move_left",
	&"move_right": "ui.settings.move_right",
	&"move_up": "ui.settings.move_up",
	&"move_down": "ui.settings.move_down",
	&"interact": "ui.settings.interact",
}

@onready var category_buttons: VBoxContainer = %CategoryButtons
@onready var category_title: Label = %CategoryTitle
@onready var category_description: Label = %CategoryDescription
@onready var settings_list: VBoxContainer = %SettingsList
@onready var reset_button: Button = %ResetButton

var _active_category: StringName = &"audio"
var _rebinding_action: StringName = &""
var _rebinding_button: Button = null


func _ready() -> void:
	_load_input_bindings()
	_build_category_buttons()
	reset_button.text = Loc.t("ui.settings.reset_defaults")
	reset_button.pressed.connect(_on_reset_pressed)
	_show_category(_active_category)


func _unhandled_key_input(event: InputEvent) -> void:
	if _rebinding_action.is_empty() or not event is InputEventKey:
		return
	var key_event: InputEventKey = event as InputEventKey
	if not key_event.pressed or key_event.echo:
		return
	if key_event.keycode == KEY_ESCAPE:
		_cancel_rebind()
		get_viewport().set_input_as_handled()
		return
	_rebind_action(_rebinding_action, key_event)
	get_viewport().set_input_as_handled()


func _build_category_buttons() -> void:
	for category: StringName in CATEGORIES:
		var button: Button = Button.new()
		button.name = "%sButton" % String(category).capitalize().replace(" ", "")
		button.custom_minimum_size = Vector2(210, 48)
		button.text = Loc.t("ui.settings.category_%s" % category)
		button.alignment = HORIZONTAL_ALIGNMENT_LEFT
		button.pressed.connect(_show_category.bind(category))
		category_buttons.add_child(button)


func _show_category(category: StringName) -> void:
	_cancel_rebind()
	_active_category = category
	category_title.text = Loc.t("ui.settings.category_%s" % category)
	category_description.text = Loc.t("ui.settings.description_%s" % category)
	_clear_settings_list()
	match category:
		&"audio":
			_build_audio_settings()
		&"display":
			_build_display_settings()
		&"controls":
			_build_control_settings()
		&"gameplay":
			_build_gameplay_settings()
		&"accessibility":
			_build_accessibility_settings()
	_update_category_button_states()


func _build_audio_settings() -> void:
	_add_volume_slider(&"master_volume", "ui.settings.master_volume", AudioManager.BUS_MASTER)
	_add_volume_slider(&"music_volume", "ui.settings.music_volume", AudioManager.BUS_MUSIC)
	_add_volume_slider(&"sfx_volume", "ui.settings.sfx_volume", AudioManager.BUS_SFX)
	_add_toggle(
		&"mute_when_unfocused",
		"ui.settings.mute_when_unfocused",
		AudioManager.get_mute_when_unfocused()
	)


func _build_display_settings() -> void:
	_add_option(
		&"window_mode",
		"ui.settings.window_mode",
		[
			[Loc.t("ui.settings.windowed"), GameSettings.WINDOW_MODE_WINDOWED],
			[Loc.t("ui.settings.borderless"), GameSettings.WINDOW_MODE_BORDERLESS],
			[Loc.t("ui.settings.fullscreen"), GameSettings.WINDOW_MODE_FULLSCREEN],
		],
		SafeTypeUtils.string(GameSettings.get_value(&"window_mode"), GameSettings.WINDOW_MODE_FULLSCREEN)
	)
	var resolution: String = "%dx%d" % [
		SafeTypeUtils.int_val(GameSettings.get_value(&"resolution_width"), 1920),
		SafeTypeUtils.int_val(GameSettings.get_value(&"resolution_height"), 1080),
	]
	_add_option(
		&"resolution",
		"ui.settings.resolution",
		[["1280×720", "1280x720"], ["1600×900", "1600x900"], ["1920×1080", "1920x1080"]],
		resolution
	)
	_add_toggle(
		&"vsync_enabled",
		"ui.settings.vsync",
		SafeTypeUtils.bool_val(GameSettings.get_value(&"vsync_enabled"), true)
	)
	_add_option(
		&"fps_limit",
		"ui.settings.fps_limit",
		[[Loc.t("ui.settings.unlimited"), 0], ["30", 30], ["60", 60], ["120", 120], ["144", 144]],
		SafeTypeUtils.int_val(GameSettings.get_value(&"fps_limit"), 60)
	)


func _build_control_settings() -> void:
	for action: StringName in REBINDABLE_ACTIONS:
		var row: HBoxContainer = _create_row(Loc.t(SafeTypeUtils.string(ACTION_LABEL_KEYS.get(action), "")))
		var button: Button = Button.new()
		button.custom_minimum_size = Vector2(220, 42)
		button.text = _get_action_key_text(action)
		button.pressed.connect(_begin_rebind.bind(action, button))
		row.add_child(button)


func _build_gameplay_settings() -> void:
	_add_toggle(
		&"edge_pan_enabled",
		"ui.settings.edge_pan",
		SafeTypeUtils.bool_val(GameSettings.get_value(&"edge_pan_enabled"), true)
	)
	_add_number_slider(
		&"camera_speed",
		"ui.settings.camera_speed",
		0.5,
		2.0,
		0.1,
		SafeTypeUtils.float_val(GameSettings.get_value(&"camera_speed"), 1.0),
		"%.1fx"
	)


func _build_accessibility_settings() -> void:
	_add_toggle(
		&"reduce_camera_motion",
		"ui.settings.reduce_camera_motion",
		SafeTypeUtils.bool_val(GameSettings.get_value(&"reduce_camera_motion"), false)
	)
	_add_option(
		&"ui_scale",
		"ui.settings.ui_scale",
		[["90%", 0.9], ["100%", 1.0], ["115%", 1.15], ["130%", 1.3]],
		SafeTypeUtils.float_val(GameSettings.get_value(&"ui_scale"), 1.0)
	)


func _add_volume_slider(key: StringName, label_key: String, bus_name: String) -> void:
	var row: HBoxContainer = _create_row(Loc.t(label_key))
	var slider: HSlider = HSlider.new()
	slider.custom_minimum_size = Vector2(260, 30)
	slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	slider.max_value = 1.0
	slider.step = 0.05
	slider.value = AudioManager.get_volume(bus_name)
	var value_label: Label = _create_value_label(AudioManager.format_volume_percent(slider.value))
	slider.value_changed.connect(_on_volume_changed.bind(key, bus_name, value_label))
	row.add_child(slider)
	row.add_child(value_label)


func _add_number_slider(
	key: StringName,
	label_key: String,
	minimum: float,
	maximum: float,
	step: float,
	current: float,
	format: String
) -> void:
	var row: HBoxContainer = _create_row(Loc.t(label_key))
	var slider: HSlider = HSlider.new()
	slider.custom_minimum_size = Vector2(260, 30)
	slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	slider.min_value = minimum
	slider.max_value = maximum
	slider.step = step
	slider.value = current
	var value_label: Label = _create_value_label(format % current)
	slider.value_changed.connect(_on_number_changed.bind(key, value_label, format))
	row.add_child(slider)
	row.add_child(value_label)


func _add_toggle(key: StringName, label_key: String, current: bool) -> void:
	var row: HBoxContainer = _create_row(Loc.t(label_key))
	var toggle: CheckButton = CheckButton.new()
	toggle.text = Loc.t("ui.settings.on") if current else Loc.t("ui.settings.off")
	toggle.button_pressed = current
	toggle.toggled.connect(_on_toggle_changed.bind(key, toggle))
	row.add_child(toggle)


func _add_option(
	key: StringName,
	label_key: String,
	options: Array,
	current: Variant
) -> void:
	var row: HBoxContainer = _create_row(Loc.t(label_key))
	var option: OptionButton = OptionButton.new()
	option.custom_minimum_size = Vector2(260, 42)
	for entry_variant: Variant in options:
		var entry: Array = SafeTypeUtils.array(entry_variant)
		option.add_item(SafeTypeUtils.string(entry[0], ""))
		option.set_item_metadata(option.item_count - 1, entry[1])
		if entry[1] == current:
			option.select(option.item_count - 1)
	option.item_selected.connect(_on_option_selected.bind(key, option))
	row.add_child(option)


func _create_row(label_text: String) -> HBoxContainer:
	var row: HBoxContainer = HBoxContainer.new()
	row.custom_minimum_size = Vector2(0, 48)
	row.add_theme_constant_override("separation", 20)
	var label: Label = Label.new()
	label.custom_minimum_size = Vector2(280, 0)
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	label.text = label_text
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	row.add_child(label)
	settings_list.add_child(row)
	return row


func _create_value_label(text_value: String) -> Label:
	var label: Label = Label.new()
	label.custom_minimum_size = Vector2(70, 0)
	label.text = text_value
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	return label


func _on_volume_changed(
	value: float,
	_key: StringName,
	bus_name: String,
	value_label: Label
) -> void:
	AudioManager.set_volume(bus_name, value)
	value_label.text = AudioManager.format_volume_percent(value)


func _on_number_changed(
	value: float,
	key: StringName,
	value_label: Label,
	format: String
) -> void:
	GameSettings.set_value(key, value)
	value_label.text = format % value


func _on_toggle_changed(enabled: bool, key: StringName, toggle: CheckButton) -> void:
	if key == &"mute_when_unfocused":
		AudioManager.set_mute_when_unfocused(enabled)
	else:
		GameSettings.set_value(key, enabled)
	toggle.text = Loc.t("ui.settings.on") if enabled else Loc.t("ui.settings.off")


func _on_option_selected(index: int, key: StringName, option: OptionButton) -> void:
	var value: Variant = option.get_item_metadata(index)
	if key == &"resolution":
		var parts: PackedStringArray = SafeTypeUtils.string(value, "1920x1080").split("x")
		GameSettings.set_value(&"resolution_width", parts[0].to_int())
		GameSettings.set_value(&"resolution_height", parts[1].to_int())
	else:
		GameSettings.set_value(key, value)


func _begin_rebind(action: StringName, button: Button) -> void:
	_cancel_rebind()
	_rebinding_action = action
	_rebinding_button = button
	button.text = Loc.t("ui.settings.press_a_key")
	button.release_focus()


func _cancel_rebind() -> void:
	if _rebinding_button != null and not _rebinding_action.is_empty():
		_rebinding_button.text = _get_action_key_text(_rebinding_action)
	_rebinding_action = &""
	_rebinding_button = null


func _rebind_action(action: StringName, key_event: InputEventKey) -> void:
	for existing: InputEvent in InputMap.action_get_events(action):
		if existing is InputEventKey:
			InputMap.action_erase_event(action, existing)
	var binding: InputEventKey = InputEventKey.new()
	binding.physical_keycode = key_event.physical_keycode
	binding.keycode = key_event.keycode
	InputMap.action_add_event(action, binding)
	_save_input_bindings()
	if _rebinding_button != null:
		_rebinding_button.text = _get_action_key_text(action)
	_rebinding_action = &""
	_rebinding_button = null


func _get_action_key_text(action: StringName) -> String:
	for event: InputEvent in InputMap.action_get_events(action):
		if event is InputEventKey:
			var key_event: InputEventKey = event as InputEventKey
			return key_event.as_text_physical_keycode()
	return Loc.t("ui.settings.unbound")


func _save_input_bindings() -> void:
	var config: ConfigFile = ConfigFile.new()
	for action: StringName in REBINDABLE_ACTIONS:
		for event: InputEvent in InputMap.action_get_events(action):
			if event is InputEventKey:
				var key_event: InputEventKey = event as InputEventKey
				config.set_value("bindings", String(action), key_event)
				break
	config.save(INPUT_BINDINGS_PATH)


func _load_input_bindings() -> void:
	var config: ConfigFile = ConfigFile.new()
	if config.load(INPUT_BINDINGS_PATH) != OK:
		return
	for action: StringName in REBINDABLE_ACTIONS:
		var value: Variant = config.get_value("bindings", String(action), null)
		if value is InputEventKey:
			for existing: InputEvent in InputMap.action_get_events(action):
				if existing is InputEventKey:
					InputMap.action_erase_event(action, existing)
			InputMap.action_add_event(action, value as InputEventKey)


func _on_reset_pressed() -> void:
	GameSettings.reset_to_defaults()
	AudioManager.set_volume(AudioManager.BUS_MASTER, 1.0)
	AudioManager.set_volume(AudioManager.BUS_MUSIC, 1.0)
	AudioManager.set_volume(AudioManager.BUS_SFX, 1.0)
	AudioManager.set_mute_when_unfocused(false)
	InputMap.load_from_project_settings()
	var config: ConfigFile = ConfigFile.new()
	config.save(INPUT_BINDINGS_PATH)
	_show_category(_active_category)


func _clear_settings_list() -> void:
	for child: Node in settings_list.get_children():
		child.queue_free()


func _update_category_button_states() -> void:
	for child: Node in category_buttons.get_children():
		if child is Button:
			var button: Button = child as Button
			button.disabled = button.text == Loc.t("ui.settings.category_%s" % _active_category)
