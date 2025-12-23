extends PanelContainer
class_name DeckListItem

## DeckListItem - A single deck entry in the deck list panel
##
## Displays deck name, card count, active star, validity indicator.
## Click to select, double-click to open details.

signal clicked()
signal double_clicked()
signal star_clicked()
signal rename_clicked()
signal delete_clicked()

@onready var star_button: Button = %StarButton
@onready var deck_name_label: Label = %DeckNameLabel
@onready var card_count_label: Label = %CardCountLabel
@onready var validity_icon: Label = %ValidityIcon
@onready var rename_button: Button = %RenameButton
@onready var delete_button: Button = %DeleteButton

var deck_id: String = ""
var is_selected: bool = false
var is_active: bool = false

var last_click_time: int = 0
const DOUBLE_CLICK_THRESHOLD_MS: int = 400


func _ready() -> void:
	star_button.pressed.connect(_on_star_pressed)
	rename_button.pressed.connect(_on_rename_pressed)
	delete_button.pressed.connect(_on_delete_pressed)
	gui_input.connect(_on_gui_input)
	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)


func setup(data: Dictionary) -> void:
	deck_id = data.get("id", "")

	var deck_name: String = data.get("name", "Unnamed")
	deck_name_label.text = deck_name

	var card_count: int = data.get("card_count", 0)
	var max_cards: int = data.get("max_cards", 30)
	card_count_label.text = "%d/%d" % [card_count, max_cards]

	is_active = data.get("is_active", false)
	is_selected = data.get("is_selected", false)
	var is_valid: bool = data.get("is_valid", true)

	# Update star button (filled star if active, outline if not)
	star_button.text = "★" if is_active else "☆"
	star_button.modulate = Color(1.0, 0.85, 0.0) if is_active else Color(0.6, 0.6, 0.6)

	# Update validity icon (checkmark if valid, warning if not)
	validity_icon.text = "✓" if is_valid else "⚠"
	validity_icon.modulate = Color(0.3, 1.0, 0.3) if is_valid else Color(1.0, 0.6, 0.2)

	# Update selection style
	_update_selection_style()


func _update_selection_style() -> void:
	if is_selected:
		modulate = Color(1.0, 1.0, 1.0)
		var style: StyleBoxFlat = StyleBoxFlat.new()
		style.bg_color = Color(0.25, 0.35, 0.5, 0.8)
		style.set_corner_radius_all(6)
		style.border_width_left = 2
		style.border_width_right = 2
		style.border_width_top = 2
		style.border_width_bottom = 2
		style.border_color = Color(0.4, 0.6, 0.9)
		add_theme_stylebox_override("panel", style)
	else:
		modulate = Color(0.9, 0.9, 0.9)
		var style: StyleBoxFlat = StyleBoxFlat.new()
		style.bg_color = Color(0.18, 0.18, 0.22, 0.8)
		style.set_corner_radius_all(6)
		add_theme_stylebox_override("panel", style)


func _on_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			var current_time: int = Time.get_ticks_msec()
			if current_time - last_click_time < DOUBLE_CLICK_THRESHOLD_MS:
				double_clicked.emit()
			else:
				clicked.emit()
			last_click_time = current_time


func _on_star_pressed() -> void:
	star_clicked.emit()


func _on_rename_pressed() -> void:
	rename_clicked.emit()


func _on_delete_pressed() -> void:
	delete_clicked.emit()


func _on_mouse_entered() -> void:
	if not is_selected:
		modulate = Color(1.0, 1.0, 1.0)


func _on_mouse_exited() -> void:
	if not is_selected:
		modulate = Color(0.9, 0.9, 0.9)
