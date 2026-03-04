extends Control
class_name CardActionPopup

## CardActionPopup - Clash Royale-style action buttons below a card
##
## Shows "Use" and "Info" buttons when a card is clicked.
## Auto-dismisses when clicking elsewhere.

signal use_pressed(instance_id: String, catalog_id: String)
signal remove_pressed(instance_id: String, catalog_id: String)
signal info_pressed(instance_id: String, catalog_id: String)
signal dismissed()

@onready var popup_container: HBoxContainer = %PopupContainer
@onready var use_button: Button = %UseButton
@onready var info_button: Button = %InfoButton

var card_instance_id: String = ""
var card_catalog_id: String = ""
var is_remove_mode: bool = false


func _ready() -> void:
	use_button.pressed.connect(_on_use_pressed)
	info_button.pressed.connect(_on_info_pressed)

	# Style the popup panel with opaque background for clarity
	var panel: PanelContainer = popup_container.get_node("PanelContainer")
	if panel:
		var style: StyleBoxFlat = StyleBoxFlat.new()
		style.bg_color = Color(0.12, 0.12, 0.15, 1.0)  # Solid dark background
		style.set_corner_radius_all(6)
		style.border_width_left = 1
		style.border_width_top = 1
		style.border_width_right = 1
		style.border_width_bottom = 1
		style.border_color = Color(0.3, 0.3, 0.35, 1.0)  # Subtle border
		panel.add_theme_stylebox_override("panel", style)

	# Dismiss when clicking outside
	set_process_input(true)


func _input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			# Check if click is outside the popup
			var popup_rect: Rect2 = popup_container.get_global_rect()
			if not popup_rect.has_point(mouse_event.global_position):
				dismissed.emit()


func show_at(pos: Vector2, instance_id: String, catalog_id: String, is_in_deck: bool, has_selected_deck: bool) -> void:
	card_instance_id = instance_id
	card_catalog_id = catalog_id
	is_remove_mode = is_in_deck

	# Position the popup centered horizontally at the given position
	var popup_size: Vector2 = popup_container.size
	popup_container.global_position = Vector2(
		pos.x - popup_size.x / 2,
		pos.y
	)

	# Update button states based on context
	if is_in_deck:
		# Card is in deck - show Remove button
		use_button.text = Loc.t("ui.collection.card_action_remove")
		use_button.disabled = false
		use_button.tooltip_text = ""
	elif not has_selected_deck:
		# No deck selected - disable Use button
		use_button.text = Loc.t("ui.collection.card_action_use")
		use_button.disabled = true
		use_button.tooltip_text = Loc.t("ui.collection.no_deck_selected")
	else:
		# Normal case - show Use button
		use_button.text = Loc.t("ui.collection.card_action_use")
		use_button.disabled = false
		use_button.tooltip_text = ""

	info_button.text = Loc.t("ui.collection.card_action_info")

	show()


func _on_use_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if is_remove_mode:
		remove_pressed.emit(card_instance_id, card_catalog_id)
	else:
		use_pressed.emit(card_instance_id, card_catalog_id)


func _on_info_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	info_pressed.emit(card_instance_id, card_catalog_id)
