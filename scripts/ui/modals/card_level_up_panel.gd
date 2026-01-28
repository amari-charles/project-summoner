extends Control
class_name CardLevelUpPanel

## Card Level-Up Panel - Modal for selecting upgrade when leveling up a card
##
## Opens as an overlay on the collection screen.
## Shows card info, upgrade choices, and handles level-up confirmation.

## UI Node References
@onready var background: ColorRect = %Background
@onready var card_name_label: Label = %CardNameLabel
@onready var level_transition_label: Label = %LevelTransitionLabel
@onready var xp_label: Label = %XPLabel
@onready var xp_progress_bar: ProgressBar = %XPProgressBar
@onready var upgrade_container: HBoxContainer = %UpgradeContainer
@onready var confirm_button: Button = %ConfirmButton
@onready var cancel_button: Button = %CancelButton

## State
var card_instance_id: String = ""
var selected_upgrade_id: String = ""
var upgrade_buttons: Array[Button] = []

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

	# Initially disable confirm until upgrade selected
	confirm_button.disabled = true

## =============================================================================
## PUBLIC API
## =============================================================================

## Open the panel for a specific card instance
func open_for_card(p_card_instance_id: String) -> void:
	card_instance_id = p_card_instance_id
	selected_upgrade_id = ""

	_load_card_data()
	_populate_upgrade_choices()
	_update_confirm_button()

	show()

## =============================================================================
## DATA LOADING
## =============================================================================

func _load_card_data() -> void:
	# PlayerCardService is a C# autoload - access via get_node
	var card_service: Node = get_node_or_null(CSharpAutoloads.PLAYER_CARD_SERVICE)
	if not card_service:
		push_error("CardLevelUpPanel: PlayerCardService not found")
		return
	var info: Dictionary = card_service.get_card_progression_info(card_instance_id)
	if info.is_empty():
		push_error("CardLevelUpPanel: Failed to get progression info for %s" % card_instance_id)
		return

	# Get card catalog data for name
	var catalog_id: String = info.get("catalog_id", "")
	var card_name: String = "Unknown Card"
	var catalog_data: Dictionary = CardCatalog.get_card(catalog_id)
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

func _populate_upgrade_choices() -> void:
	# Clear existing upgrade buttons
	for button: Button in upgrade_buttons:
		button.queue_free()
	upgrade_buttons.clear()

	# Get available upgrades from PlayerCardService (C# autoload)
	var card_service: Node = get_node_or_null(CSharpAutoloads.PLAYER_CARD_SERVICE)
	if not card_service:
		return
	var upgrades: Array = card_service.get_available_upgrades(card_instance_id)

	# Create upgrade buttons
	for upgrade_var: Variant in upgrades:
		if not upgrade_var is Dictionary:
			continue
		var upgrade: Dictionary = upgrade_var
		var button: Button = _create_upgrade_button(upgrade)
		upgrade_container.add_child(button)
		upgrade_buttons.append(button)

func _create_upgrade_button(upgrade: Dictionary) -> Button:
	var button: Button = Button.new()

	var upgrade_id: String = upgrade.get("id", "")
	var upgrade_name_val: Variant = upgrade.get("name", "Unknown")
	var upgrade_name: String = upgrade_name_val if upgrade_name_val is String else "Unknown"
	var description_val: Variant = upgrade.get("description", "")
	var description: String = description_val if description_val is String else ""

	# Build button text with name and description
	button.text = "%s\n%s" % [upgrade_name, description]

	# Style
	button.custom_minimum_size = Vector2(180, 120)
	button.add_theme_font_size_override("font_size", 18)

	# Connect
	button.pressed.connect(_on_upgrade_selected.bind(upgrade_id))

	return button

func _update_confirm_button() -> void:
	# Check if upgrade selected (no gold cost - XP only)
	var can_confirm: bool = not selected_upgrade_id.is_empty()
	confirm_button.disabled = not can_confirm

func _update_selection_visual() -> void:
	# Update button visuals to show selection
	for button: Button in upgrade_buttons:
		# Get the upgrade_id from the button connection
		var is_selected: bool = false
		if button.pressed.get_connections().size() > 0:
			var connections: Array = button.pressed.get_connections()
			for conn: Variant in connections:
				if conn is Dictionary:
					var conn_dict: Dictionary = conn
					var binds_var: Variant = conn_dict.get("binds", [])
					if binds_var is Array:
						var binds: Array = binds_var
						if binds.size() > 0 and binds[0] == selected_upgrade_id:
							is_selected = true
							break

		if is_selected:
			button.add_theme_color_override("font_color", GameColorPalette.TEXT_HIGHLIGHT)
		else:
			button.remove_theme_color_override("font_color")

## =============================================================================
## EVENT HANDLERS
## =============================================================================

func _on_upgrade_selected(upgrade_id: String) -> void:
	selected_upgrade_id = upgrade_id
	_update_selection_visual()
	_update_confirm_button()

func _on_confirm_pressed() -> void:
	if selected_upgrade_id.is_empty():
		return

	# PlayerCardService is a C# autoload
	var card_service: Node = get_node_or_null(CSharpAutoloads.PLAYER_CARD_SERVICE)
	if not card_service:
		push_error("CardLevelUpPanel: PlayerCardService not found")
		return
	var success: bool = card_service.level_up_card(card_instance_id, selected_upgrade_id)

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
