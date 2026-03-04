extends Control
class_name NavDrawer

## Navigation drawer that slides in from the right
## Contains navigation options: Collection, Shop, Settings

signal collection_pressed
signal events_pressed
signal online_pressed
signal shop_pressed
signal settings_pressed
signal quit_pressed
signal closed

@onready var overlay: ColorRect = $Overlay
@onready var panel: Control = $Panel
@onready var border: NinePatchRect = %Border
@onready var title_label: Label = $Panel/MarginContainer/VBoxContainer/Header/Title
@onready var close_button: TextureButton = $Panel/MarginContainer/VBoxContainer/Header/CloseButton
@onready var collection_button: Button = $Panel/MarginContainer/VBoxContainer/NavButtons/CollectionButton
@onready var events_button: Button = $Panel/MarginContainer/VBoxContainer/NavButtons/EventsButton
@onready var online_button: Button = $Panel/MarginContainer/VBoxContainer/NavButtons/OnlineButton
@onready var shop_button: Button = $Panel/MarginContainer/VBoxContainer/NavButtons/ShopButton
@onready var settings_button: Button = $Panel/MarginContainer/VBoxContainer/NavButtons/SettingsButton
@onready var quit_button: Button = $Panel/MarginContainer/VBoxContainer/NavButtons/QuitButton

## Animation timing for drawer slide in/out - fast enough to feel responsive
const SLIDE_DURATION: float = 0.25
const PANEL_WIDTH: float = 300.0
const OVERLAY_OPACITY: float = 0.6

var _is_open: bool = false
var _tween: Tween

func _ready() -> void:
	# Set up border using factory
	ButtonStyleFactory.apply_panel_border(border)

	# Set localized text
	title_label.text = Loc.t("ui.nav.menu")
	collection_button.text = Loc.t("ui.nav.collection")
	events_button.text = Loc.t("ui.nav.events")
	online_button.text = Loc.t("ui.nav.online")
	shop_button.text = Loc.t("ui.nav.shop")
	settings_button.text = Loc.t("ui.nav.settings")
	quit_button.text = Loc.t("ui.nav.quit")

	# Start hidden
	visible = false
	overlay.modulate.a = 0.0
	panel.position.x = get_viewport_rect().size.x

	# Connect buttons
	close_button.pressed.connect(_close)
	collection_button.pressed.connect(_on_collection_pressed)
	events_button.pressed.connect(_on_events_pressed)
	online_button.pressed.connect(_on_online_pressed)
	shop_button.pressed.connect(_on_shop_pressed)
	settings_button.pressed.connect(_on_settings_pressed)
	quit_button.pressed.connect(_on_quit_pressed)

	# Close when clicking overlay
	overlay.gui_input.connect(_on_overlay_input)

func _on_overlay_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event as InputEventMouseButton
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			_close()
	elif event is InputEventScreenTouch:
		var touch_event: InputEventScreenTouch = event as InputEventScreenTouch
		if touch_event.pressed:
			_close()

func open() -> void:
	if _is_open:
		return

	_is_open = true
	visible = true

	# Disable collection if no summoner is unlocked
	_update_collection_button_state()

	# Position panel off-screen to the right
	var viewport_width: float = get_viewport_rect().size.x
	panel.position.x = viewport_width
	panel.size.x = PANEL_WIDTH

	# Animate in
	if _tween:
		_tween.kill()
	_tween = create_tween()
	_tween.set_parallel(true)
	_tween.tween_property(overlay, "modulate:a", OVERLAY_OPACITY, SLIDE_DURATION)
	_tween.tween_property(panel, "position:x", viewport_width - PANEL_WIDTH, SLIDE_DURATION).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)

func _close() -> void:
	if not _is_open:
		return

	_is_open = false

	var viewport_width: float = get_viewport_rect().size.x

	# Animate out
	if _tween:
		_tween.kill()
	_tween = create_tween()
	_tween.set_parallel(true)
	_tween.tween_property(overlay, "modulate:a", 0.0, SLIDE_DURATION)
	_tween.tween_property(panel, "position:x", viewport_width, SLIDE_DURATION).set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_CUBIC)
	_tween.chain().tween_callback(func() -> void:
		visible = false
		closed.emit()
	)

func _on_collection_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	collection_pressed.emit()
	_close()

func _on_events_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	events_pressed.emit()
	_close()

func _on_online_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	online_pressed.emit()
	_close()

func _on_shop_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	shop_pressed.emit()
	_close()

func _on_settings_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	settings_pressed.emit()
	_close()

func _on_quit_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	quit_pressed.emit()
	_close()


func _update_collection_button_state() -> void:
	var unlocked: Array = SummonerSelection.GetUnlockedSummonerIdsArray()
	var has_summoner: bool = unlocked.size() > 0

	collection_button.disabled = not has_summoner
	if not has_summoner:
		collection_button.tooltip_text = Loc.t("ui.nav.collection_locked")
	else:
		collection_button.tooltip_text = ""

func _input(event: InputEvent) -> void:
	if not _is_open:
		return

	# Close on Escape key
	if event is InputEventKey:
		var key_event: InputEventKey = event as InputEventKey
		if key_event.pressed and key_event.keycode == KEY_ESCAPE:
			_close()
			get_viewport().set_input_as_handled()
