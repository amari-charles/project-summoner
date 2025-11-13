extends Control
class_name BattlefieldDropZone

## Drop zone overlay for the battlefield that handles card drops

var summoner: Node = null  # Can be Summoner or Summoner3D
var camera_2d: Camera2D = null
var camera_3d: Camera3D = null
var is_3d: bool = false

func _ready() -> void:
	# Wait one frame to ensure summoners have joined their groups
	await get_tree().process_frame

	# Find player summoner (2D or 3D)
	var summoners: Array[Node] = get_tree().get_nodes_in_group("summoners")
	for node: Node in summoners:
		var is_player: bool = false

		# Check for both Summoner and Summoner3D with proper type checking
		if node is Summoner:
			var summoner_2d: Summoner = node
			var team_variant: Variant = summoner_2d.get("team")
			var team_value: int = team_variant if team_variant is int else -1
			is_player = team_value == Unit.Team.PLAYER
		elif node is Summoner3D:
			var summoner_3d: Summoner3D = node
			var team_variant: Variant = summoner_3d.get("team")
			var team_value: int = team_variant if team_variant is int else -1
			is_player = team_value == Unit3D.Team.PLAYER

		if is_player:
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
	if not summoner:
		return false

	var is_alive_variant: Variant = summoner.get("is_alive")
	var is_alive: bool = is_alive_variant if is_alive_variant is bool else false
	if not is_alive:
		return false

	# Get the card
	var card_index: int = data.card_index
	var hand_variant: Variant = summoner.get("hand")
	var hand: Array = hand_variant if hand_variant is Array else []
	if card_index < 0 or card_index >= hand.size():
		return false

	var card: Card = data.card

	# Check if we can afford it
	var mana_variant: Variant = summoner.get("mana")
	var mana: float = mana_variant if mana_variant is float else 0.0
	if mana < card.mana_cost:
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
		var world_pos_3d: Vector3 = _screen_to_world_3d(at_position)
		if summoner.has_method("play_card_3d"):
			summoner.call("play_card_3d", card_index, world_pos_3d)
	else:
		# Convert screen to 2D world position
		var world_pos_2d: Vector2 = _screen_to_world_2d(at_position)
		if summoner.has_method("play_card"):
			summoner.call("play_card", card_index, world_pos_2d)

## Convert screen coordinates to 2D world coordinates
func _screen_to_world_2d(screen_pos: Vector2) -> Vector2:
	if not camera_2d:
		return screen_pos

	var viewport_center: Vector2 = get_viewport().get_visible_rect().size / 2
	var offset_from_center: Vector2 = (screen_pos - viewport_center) / camera_2d.zoom
	return camera_2d.global_position + offset_from_center

## Convert screen coordinates to 3D world coordinates
func _screen_to_world_3d(screen_pos: Vector2) -> Vector3:
	if not camera_3d:
		return Vector3.ZERO

	# Create a ray from camera through mouse position
	var from: Vector3 = camera_3d.project_ray_origin(screen_pos)
	var to: Vector3 = from + camera_3d.project_ray_normal(screen_pos) * BattlefieldConstants.RAYCAST_DISTANCE

	# Intersect with spawn plane (where units spawn)
	var spawn_y: float = BattlefieldConstants.SPAWN_PLANE_HEIGHT
	var t: float = (spawn_y - from.y) / (to.y - from.y)
	if t < 0 or t > 1:
		return Vector3.ZERO

	var hit_pos: Vector3 = from + (to - from) * t
	return hit_pos
