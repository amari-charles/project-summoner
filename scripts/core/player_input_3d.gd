extends Node
class_name PlayerInput3D

## Handles player input for 3D battlefield card playing

@export var summoner: Summoner3D

var camera: Camera3D
var selected_card_index: int = -1
var hand_labels: Array[Label] = []

func _ready() -> void:
	if summoner == null:
		summoner = get_parent() as Summoner3D
		if summoner == null:
			push_error("PlayerInput3D: Could not find parent Summoner3D!")

	# Find the camera
	await get_tree().process_frame
	camera = get_viewport().get_camera_3d()
	if not camera:
		push_error("PlayerInput3D: Could not find Camera3D!")

	# Find hand display labels
	var ui = get_tree().get_first_node_in_group("game_controller")
	if ui:
		ui = ui.get_node_or_null("UI/HandDisplay")
		if ui:
			hand_labels.append(ui.get_node_or_null("Card0"))
			hand_labels.append(ui.get_node_or_null("Card1"))
			hand_labels.append(ui.get_node_or_null("Card2"))
			hand_labels.append(ui.get_node_or_null("Card3"))

	# Connect to summoner signals
	if summoner:
		summoner.hand_changed.connect(_on_hand_changed)
		summoner.mana_changed.connect(_on_mana_changed)
		_update_hand_display()

func _on_hand_changed(_hand: Array) -> void:
	_update_hand_display()

func _on_mana_changed(current: float, _max: float) -> void:
	var mana_label = get_tree().get_first_node_in_group("game_controller")
	if mana_label:
		mana_label = mana_label.get_node_or_null("UI/PlayerManaLabel")
		if mana_label:
			mana_label.text = "Mana: %d/10" % int(current)

func _update_hand_display() -> void:
	for i in range(hand_labels.size()):
		if i < summoner.hand.size():
			var card = summoner.hand[i]
			var selected = " [SELECTED]" if i == selected_card_index else ""
			hand_labels[i].text = "%d: %s (%d)%s" % [i + 1, card.card_name, card.mana_cost, selected]
		else:
			hand_labels[i].text = "%d: Empty" % (i + 1)

func _input(event: InputEvent) -> void:
	# Number keys to select card in hand (1-4)
	if event is InputEventKey and event.pressed:
		if event.keycode >= KEY_1 and event.keycode <= KEY_4:
			var card_index = event.keycode - KEY_1
			if card_index < summoner.hand.size():
				selected_card_index = card_index
				print("Selected card %d: %s" % [card_index, summoner.hand[card_index].card_name])
				_update_hand_display()

	# Click to play selected card
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		if selected_card_index >= 0 and selected_card_index < summoner.hand.size():
			var spawn_pos = _get_world_position_from_mouse(event.position)
			if spawn_pos:
				print("Playing card at position: %v" % spawn_pos)
				if summoner.play_card_3d(selected_card_index, spawn_pos):
					print("Card played successfully!")
				else:
					print("Failed to play card (not enough mana?)")

## Convert mouse position to 3D world position
func _get_world_position_from_mouse(mouse_pos: Vector2) -> Vector3:
	if not camera:
		return Vector3.ZERO

	# Create a ray from camera through mouse position
	var from = camera.project_ray_origin(mouse_pos)
	var to = from + camera.project_ray_normal(mouse_pos) * 1000

	# Intersect with Y=1 plane (where units spawn)
	var t = (1.0 - from.y) / (to.y - from.y)
	if t < 0 or t > 1:
		return Vector3.ZERO

	var hit_pos = from + (to - from) * t
	return hit_pos
