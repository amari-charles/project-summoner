extends Control
class_name BattlefieldDropZone

## Drop zone overlay for the battlefield that handles card drops

var summoner: Node  # Can be Summoner or Summoner3D
var camera_2d: Camera2D
var camera_3d: Camera3D
var is_3d: bool = false

func _ready() -> void:
	# Find player summoner (2D or 3D)
	var summoners = get_tree().get_nodes_in_group("summoners")
	for node in summoners:
		if (node is Summoner and node.team == Unit.Team.PLAYER) or \
		   (node.get_script() and node.get_script().get_global_name() == "Summoner3D" and node.team == 0):
			summoner = node
			break

	if not summoner:
		push_error("BattlefieldDropZone: Could not find player Summoner!")

	# Find camera (2D or 3D)
	camera_2d = get_viewport().get_camera_2d()
	camera_3d = get_viewport().get_camera_3d()
	is_3d = camera_3d != null and camera_2d == null

	if not camera_2d and not camera_3d:
		push_error("BattlefieldDropZone: Could not find camera!")

	# STOP filter is needed to receive drop events, but we're behind HandUI
	# so HandUI will receive mouse events in its area first
	mouse_filter = Control.MOUSE_FILTER_STOP

## Check if we can drop the card here
func _can_drop_data(_at_position: Vector2, data: Variant) -> bool:
	# Validate drop data
	if not data is Dictionary:
		return false

	if not data.has("card_index") or not data.has("card") or not data.has("source"):
		return false

	if data.source != "hand":
		return false

	# Check if we have a summoner
	if not summoner or not summoner.is_alive:
		return false

	# Get the card
	var card_index: int = data.card_index
	if card_index < 0 or card_index >= summoner.hand.size():
		return false

	var card: Card = data.card

	# Check if we can afford it
	if summoner.mana < card.mana_cost:
		return false

	# Valid drop
	return true

## Handle the card drop
func _drop_data(at_position: Vector2, data: Variant) -> void:
	if not _can_drop_data(at_position, data):
		return

	var card_index: int = data.card_index

	if is_3d:
		# Convert screen to 3D world position
		var world_pos_3d = _screen_to_world_3d(at_position)
		summoner.play_card_3d(card_index, world_pos_3d)
	else:
		# Convert screen to 2D world position
		var world_pos_2d = _screen_to_world_2d(at_position)
		summoner.play_card(card_index, world_pos_2d)

## Convert screen coordinates to 2D world coordinates
func _screen_to_world_2d(screen_pos: Vector2) -> Vector2:
	if not camera_2d:
		return screen_pos

	var viewport_center = get_viewport().get_visible_rect().size / 2
	var offset_from_center = (screen_pos - viewport_center) / camera_2d.zoom
	return camera_2d.global_position + offset_from_center

## Convert screen coordinates to 3D world coordinates
func _screen_to_world_3d(screen_pos: Vector2) -> Vector3:
	if not camera_3d:
		return Vector3.ZERO

	# Create a ray from camera through mouse position
	var from = camera_3d.project_ray_origin(screen_pos)
	var to = from + camera_3d.project_ray_normal(screen_pos) * BattlefieldConstants.RAYCAST_DISTANCE

	# Intersect with spawn plane (where units spawn)
	var spawn_y = BattlefieldConstants.SPAWN_PLANE_HEIGHT
	var t = (spawn_y - from.y) / (to.y - from.y)
	if t < 0 or t > 1:
		return Vector3.ZERO

	var hit_pos = from + (to - from) * t
	return hit_pos
