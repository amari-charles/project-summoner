extends Control
class_name CardLevelUpPanel

## Card Level-Up Panel - Modal for leveling up a card
##
## Opens as an overlay on the collection screen.
## Shows card info, optional trait context, and handles level-up confirmation.

## UI Node References
@onready var background: ColorRect = %Background
@onready var card_name_label: Label = %CardNameLabel
@onready var level_transition_label: Label = %LevelTransitionLabel
@onready var xp_label: Label = %XPLabel
@onready var xp_progress_bar: ProgressBar = %XPProgressBar
@onready var trait_container: VBoxContainer = %UpgradeContainer
@onready var confirm_button: Button = %ConfirmButton
@onready var cancel_button: Button = %CancelButton

## State
var card_instance_id: String = ""
var _can_level_up: bool = false
var _is_submitting: bool = false

## Signals
signal level_up_completed(card_instance_id: String)
signal cancelled()

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Connect buttons
	confirm_button.pressed.connect(_on_confirm_pressed)
	cancel_button.pressed.connect(_on_cancel_pressed)

	# Connect background click to close
	background.gui_input.connect(_on_background_input)

	confirm_button.disabled = true

## =============================================================================
## PUBLIC API
## =============================================================================

## Open the panel for a specific card instance
func open_for_card(p_card_instance_id: String) -> void:
	card_instance_id = p_card_instance_id

	_load_card_data()
	_populate_trait_status()
	_update_confirm_button()

	show()

## =============================================================================
## DATA LOADING
## =============================================================================

func _load_card_data() -> void:
	var info: Dictionary = CardServiceApi.get_card_progression_info_dict(card_instance_id)
	if info.is_empty():
		push_error("CardLevelUpPanel: Failed to get progression info for %s" % card_instance_id)
		_can_level_up = false
		return

	# Get card catalog data for name
	var catalog_id: String = info.get("catalog_id", "")
	var card_name: String = "Unknown Card"
	var catalog_data: Dictionary = CardCatalogApi.get_card_as_dict(catalog_id)
	if not catalog_data.is_empty():
		var name_val: Variant = catalog_data.get("card_name", "Unknown Card")
		card_name = SafeTypeUtils.string(name_val, "Unknown Card")

	# Update UI
	card_name_label.text = card_name

	var level: int = info.get("level", 1)
	var next_level: int = level + 1
	level_transition_label.text = Loc.t("ui.level_up.current_level", {"level": level, "next": next_level})

	var current_xp: int = info.get("xp", 0)
	var xp_for_next: int = info.get("xp_for_next_level", 0)
	var xp_progress: float = info.get("xp_progress", 0.0)
	_can_level_up = SafeTypeUtils.bool_val(info.get("can_level_up", false), false)
	xp_label.text = Loc.t("ui.collection.xp_label", {"current": current_xp, "required": xp_for_next})
	xp_progress_bar.value = xp_progress * 100.0

func _populate_trait_status() -> void:
	# Clear previous content
	for child: Node in trait_container.get_children():
		child.queue_free()

	var info: Dictionary = CardServiceApi.get_card_progression_info_dict(card_instance_id)
	var unspent_points: int = int(info.get("unspent_trait_points", 0))

	var gain_label: Label = Label.new()
	gain_label.text = "Level Up grants +1 Trait Point"
	gain_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	gain_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	gain_label.custom_minimum_size = Vector2(420, 30)
	gain_label.add_theme_font_size_override("font_size", 18)
	gain_label.add_theme_color_override("font_color", GameColorPalette.SUCCESS)
	trait_container.add_child(gain_label)

	var unspent_label: Label = Label.new()
	unspent_label.text = "Current Unspent Trait Points: %d" % unspent_points
	unspent_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	unspent_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	unspent_label.custom_minimum_size = Vector2(420, 30)
	unspent_label.add_theme_font_size_override("font_size", 16)
	unspent_label.add_theme_color_override("font_color", GameColorPalette.INFO)
	trait_container.add_child(unspent_label)

	var spend_label: Label = Label.new()
	spend_label.text = "Trait spending is handled from the Traits flow."
	spend_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	spend_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	spend_label.custom_minimum_size = Vector2(420, 40)
	spend_label.add_theme_font_size_override("font_size", 14)
	trait_container.add_child(spend_label)

func _update_confirm_button() -> void:
	var can_confirm: bool = not card_instance_id.is_empty() and _can_level_up and not _is_submitting
	confirm_button.disabled = not can_confirm
	confirm_button.text = Loc.t("ui.level_up.confirm")

## =============================================================================
## EVENT HANDLERS
## =============================================================================

func _on_confirm_pressed() -> void:
	if card_instance_id.is_empty() or _is_submitting:
		return

	_is_submitting = true
	_update_confirm_button()
	var success: bool = CardServiceApi.level_up_card(card_instance_id)

	if success:
		level_up_completed.emit(card_instance_id)
		_is_submitting = false
		_load_card_data()
		_populate_trait_status()
		_update_confirm_button()
		if _can_level_up:
			return
		_close()
	else:
		_is_submitting = false
		_update_confirm_button()
		_show_error_feedback(Loc.t("ui.level_up.failed"))

func _on_cancel_pressed() -> void:
	cancelled.emit()
	_close()

func _on_background_input(event: InputEvent) -> void:
	# Close on background click
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			cancelled.emit()
			_close()

func _close() -> void:
	hide()
	queue_free()

func _show_error_feedback(message: String) -> void:
	confirm_button.text = message
	confirm_button.add_theme_color_override("font_color", GameColorPalette.ERROR)
	await get_tree().create_timer(1.5).timeout
	if not is_inside_tree():
		return
	confirm_button.text = Loc.t("ui.level_up.confirm")
	confirm_button.remove_theme_color_override("font_color")
