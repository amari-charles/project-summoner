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
var selected_trait_id: String = ""
var _offer_buttons: Array[Button] = []
var _requires_trait_selection: bool = false
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
	_populate_trait_choices()
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

func _populate_trait_choices() -> void:
	# Clear previous trait content
	for child: Node in trait_container.get_children():
		child.queue_free()
	selected_trait_id = ""
	_offer_buttons.clear()
	_requires_trait_selection = false

	var offers: Array = CardServiceApi.roll_trait_offers(card_instance_id, 3)
	if offers.is_empty():
		var info_label: Label = Label.new()
		info_label.text = Loc.t("ui.collection.no_trait_offers")
		info_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		info_label.custom_minimum_size = Vector2(420, 60)
		trait_container.add_child(info_label)
		return

	_requires_trait_selection = true

	var offers_container: VBoxContainer = VBoxContainer.new()
	offers_container.add_theme_constant_override("separation", 8)
	trait_container.add_child(offers_container)

	for offer_var: Variant in offers:
		if not offer_var is Dictionary:
			continue
		var offer: Dictionary = offer_var
		var trait_id: String = SafeTypeUtils.string(offer.get("trait_id", ""), "")
		var display_name: String = SafeTypeUtils.string(offer.get("display_name", ""), "")
		var summary_short: String = SafeTypeUtils.string(offer.get("summary_short", ""), "")
		var description: String = SafeTypeUtils.string(offer.get("description", ""), "")
		if trait_id.is_empty() or display_name.is_empty():
			continue

		var button: Button = Button.new()
		button.toggle_mode = true
		button.custom_minimum_size = Vector2(0, 74)
		button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		button.alignment = HORIZONTAL_ALIGNMENT_LEFT
		button.vertical_icon_alignment = VERTICAL_ALIGNMENT_CENTER
		button.text = ""
		button.pressed.connect(_on_offer_pressed.bind(button, trait_id))
		offers_container.add_child(button)
		_offer_buttons.append(button)

		var margin: MarginContainer = MarginContainer.new()
		margin.mouse_filter = Control.MOUSE_FILTER_IGNORE
		margin.add_theme_constant_override("margin_left", 12)
		margin.add_theme_constant_override("margin_top", 10)
		margin.add_theme_constant_override("margin_right", 12)
		margin.add_theme_constant_override("margin_bottom", 10)
		button.add_child(margin)

		var row: VBoxContainer = VBoxContainer.new()
		row.mouse_filter = Control.MOUSE_FILTER_IGNORE
		row.add_theme_constant_override("separation", 4)
		margin.add_child(row)

		var name_label: Label = Label.new()
		name_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
		name_label.text = display_name
		name_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		name_label.add_theme_font_size_override("font_size", 18)
		row.add_child(name_label)

		var secondary_text: String = summary_short if not summary_short.is_empty() else description
		if not secondary_text.is_empty():
			var description_label: Label = Label.new()
			description_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
			description_label.text = secondary_text
			description_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
			description_label.add_theme_font_size_override("font_size", 15)
			description_label.add_theme_color_override("font_color", GameColorPalette.SUCCESS)
			row.add_child(description_label)

		_apply_offer_button_state(button, false)

func _update_confirm_button() -> void:
	var has_trait_selection: bool = not selected_trait_id.is_empty()
	var trait_gate_passed: bool = (not _requires_trait_selection) or has_trait_selection
	var can_confirm: bool = not card_instance_id.is_empty() and _can_level_up and trait_gate_passed and not _is_submitting
	confirm_button.disabled = not can_confirm
	confirm_button.text = Loc.t("ui.level_up.confirm")

## =============================================================================
## EVENT HANDLERS
## =============================================================================

func _on_confirm_pressed() -> void:
	if card_instance_id.is_empty() or _is_submitting:
		return

	if _requires_trait_selection and selected_trait_id.is_empty():
		_show_error_feedback(Loc.t("ui.level_up.choose_upgrade"))
		return

	_is_submitting = true
	_update_confirm_button()
	var success: bool = CardServiceApi.level_up_card(card_instance_id)

	if success:
		var trait_spend_success: bool = true
		if not selected_trait_id.is_empty():
			trait_spend_success = CardServiceApi.spend_trait_point(card_instance_id, selected_trait_id)

		if not trait_spend_success:
			push_warning("CardLevelUpPanel: Trait spend failed for selected trait '%s'; card was leveled and point remains unspent" % selected_trait_id)
			_is_submitting = false
			_load_card_data()
			_populate_trait_choices()
			_update_confirm_button()
			_show_error_feedback(Loc.t("ui.level_up.failed"))
			level_up_completed.emit(card_instance_id)
			return

		level_up_completed.emit(card_instance_id)
		_is_submitting = false
		_load_card_data()
		_populate_trait_choices()
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

func _on_offer_pressed(source_button: Button, trait_id: String) -> void:
	selected_trait_id = trait_id
	for button: Button in _offer_buttons:
		var is_selected: bool = button == source_button
		button.button_pressed = is_selected
		_apply_offer_button_state(button, is_selected)
	_update_confirm_button()

func _show_error_feedback(message: String) -> void:
	confirm_button.text = message
	confirm_button.add_theme_color_override("font_color", GameColorPalette.ERROR)
	await get_tree().create_timer(1.5).timeout
	if not is_inside_tree():
		return
	confirm_button.text = Loc.t("ui.level_up.confirm")
	confirm_button.remove_theme_color_override("font_color")

func _apply_offer_button_state(button: Button, selected: bool) -> void:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = GameColorPalette.with_alpha(
		GameColorPalette.SUCCESS if selected else GameColorPalette.UI_BG_DARK,
		0.30 if selected else 0.85
	)
	style.border_color = GameColorPalette.SUCCESS if selected else GameColorPalette.UI_BG_LIGHT
	style.border_width_left = 2
	style.border_width_top = 2
	style.border_width_right = 2
	style.border_width_bottom = 2
	style.set_corner_radius_all(6)
	button.add_theme_stylebox_override("normal", style)
	button.add_theme_stylebox_override("hover", style)
	button.add_theme_stylebox_override("pressed", style)
	button.add_theme_stylebox_override("focus", style)
