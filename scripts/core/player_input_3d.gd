extends Node
class_name PlayerInput3D

## Handles player input for 3D battlefield card playing

@export var summoner: Summoner3D

var camera: Camera3D
var selected_card_index: int = -1

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

func _input(event: InputEvent) -> void:
	# Number keys to select card in hand (1-4)
	if event is InputEventKey and event.pressed:
		if event.keycode >= KEY_1 and event.keycode <= KEY_4:
			var card_index = event.keycode - KEY_1
			if card_index < summoner.hand.size():
				selected_card_index = card_index
				print("Selected card %d: %s" % [card_index, summoner.hand[card_index].card_name])

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
