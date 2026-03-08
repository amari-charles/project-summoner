extends Control
class_name CardLevelUpPanel

## Card Level-Up Panel - Modal for selecting trait when leveling up a card
##
## Opens as an overlay on the collection screen.
## Shows card info, trait choices, and handles level-up confirmation.

## UI Node References
@onready var background: ColorRect = %Background
@onready var card_name_label: Label = %CardNameLabel
@onready var level_transition_label: Label = %LevelTransitionLabel
@onready var xp_label: Label = %XPLabel
@onready var xp_progress_bar: ProgressBar = %XPProgressBar
@onready var trait_container: HBoxContainer = %UpgradeContainer
@onready var confirm_button: Button = %ConfirmButton
@onready var cancel_button: Button = %CancelButton

## State
var card_instance_id: String = ""
var selected_trait_id: String = ""
var trait_buttons: Array[Button] = []

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

	# Initially disable confirm until trait selected
	confirm_button.disabled = true

## =============================================================================
## PUBLIC API
## =============================================================================

## Open the panel for a specific card instance
func open_for_card(p_card_instance_id: String) -> void:
	card_instance_id = p_card_instance_id
	selected_trait_id = ""

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
		return

	# Get card catalog data for name
	var catalog_id: String = info.get("catalog_id", "")
	var card_name: String = "Unknown Card"
	var catalog_data: Dictionary = CardCatalogApi.get_card_as_dict(catalog_id)
	if not catalog_data.is_empty():
		var name_val: Variant = catalog_data.get("card_name", "Unknown Card")
		card_name = name_val if name_val is String else "Unknown Card"

	# Update UI
	card_name_label.text = card_name

	var level: int = info.get("level", 1)
	var next_level: int = level + 1
	level_transition_label.text = Loc.t("ui.level_up.current_level", {"level": level, "next": next_level})

	var current_xp: int = info.get("xp", 0)
	var xp_for_next: int = info.get("xp_for_next_level", 0)
	var xp_progress: float = info.get("xp_progress", 0.0)
	xp_label.text = Loc.t("ui.collection.xp_label", {"current": current_xp, "required": xp_for_next})
	xp_progress_bar.value = xp_progress * 100.0

func _populate_trait_choices() -> void:
	# Clear existing trait buttons
	for button: Button in trait_buttons:
		button.queue_free()
	trait_buttons.clear()

	var traits: Array = CardServiceApi.get_available_traits(card_instance_id)

	# Create trait buttons
	for trait_var: Variant in traits:
		if not trait_var is Dictionary:
			continue
		var trait_data: Dictionary = trait_var
		var button: Button = _create_trait_button(trait_data)
		trait_container.add_child(button)
		trait_buttons.append(button)

func _create_trait_button(trait_data: Dictionary) -> Button:
	var button: Button = Button.new()

	var trait_id: String = trait_data.get("id", "")
	var trait_name_val: Variant = trait_data.get("name", Loc.t("ui.common.unknown"))
	var trait_name: String = trait_name_val if trait_name_val is String else Loc.t("ui.common.unknown")
	var description_val: Variant = trait_data.get("description", "")
	var description: String = description_val if description_val is String else ""

	# Build button text with name and description
	button.text = "%s\n%s" % [trait_name, description]

	# Style
	button.custom_minimum_size = Vector2(180, 120)
	button.add_theme_font_size_override("font_size", 18)

	# Connect
	button.pressed.connect(_on_trait_selected.bind(trait_id))

	return button

func _update_confirm_button() -> void:
	# Check if trait selected (no gold cost - XP only)
	var can_confirm: bool = not selected_trait_id.is_empty()
	confirm_button.disabled = not can_confirm

func _update_selection_visual() -> void:
	# Update button visuals to show selection
	for button: Button in trait_buttons:
		# Get the trait_id from the button connection
		var is_selected: bool = false
		if button.pressed.get_connections().size() > 0:
			var connections: Array = button.pressed.get_connections()
			for conn: Variant in connections:
				if conn is Dictionary:
					var conn_dict: Dictionary = conn
					var binds_var: Variant = conn_dict.get("binds", [])
					if binds_var is Array:
						var binds: Array = binds_var
						if binds.size() > 0 and binds[0] == selected_trait_id:
							is_selected = true
							break

		if is_selected:
			button.add_theme_color_override("font_color", GameColorPalette.TEXT_HIGHLIGHT)
		else:
			button.remove_theme_color_override("font_color")

## =============================================================================
## EVENT HANDLERS
## =============================================================================

func _on_trait_selected(trait_id: String) -> void:
	selected_trait_id = trait_id
	_update_selection_visual()
	_update_confirm_button()

func _on_confirm_pressed() -> void:
	if selected_trait_id.is_empty():
		return

	var success: bool = CardServiceApi.level_up_card(card_instance_id, selected_trait_id)

	if success:
		level_up_completed.emit(card_instance_id)
		_close()
	else:
		# Show error feedback to user
		confirm_button.text = Loc.t("ui.level_up.failed")
		confirm_button.add_theme_color_override("font_color", GameColorPalette.ERROR)
		# Re-enable after brief delay so user can try again
		await get_tree().create_timer(1.5).timeout
		confirm_button.text = Loc.t("ui.level_up.confirm")
		confirm_button.remove_theme_color_override("font_color")

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
