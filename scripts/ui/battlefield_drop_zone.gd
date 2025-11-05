extends Control
class_name BattlefieldDropZone

## Drop zone overlay for the battlefield that handles card drops

var summoner: Summoner
var camera: Camera2D

func _ready() -> void:
	# Find player summoner
	var summoners = get_tree().get_nodes_in_group("summoners")
	for node in summoners:
		if node is Summoner and node.team == Unit.Team.PLAYER:
			summoner = node
			break

	if not summoner:
		push_error("BattlefieldDropZone: Could not find player Summoner!")

	# Find camera
	camera = get_viewport().get_camera_2d()
	if not camera:
		push_error("BattlefieldDropZone: Could not find camera!")

	# STOP filter is needed to receive drop events, but we're behind HandUI
	# so HandUI will receive mouse events in its area first
	mouse_filter = Control.MOUSE_FILTER_STOP

	print("BattlefieldDropZone: Ready and waiting for drops")

## Check if we can drop the card here
func _can_drop_data(at_position: Vector2, data: Variant) -> bool:
	print("BattlefieldDropZone: _can_drop_data called at ", at_position)

	# Validate drop data
	if not data is Dictionary:
		print("BattlefieldDropZone: Data is not Dictionary")
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

	# Convert screen position to world position
	var world_pos = _screen_to_world(at_position)

	# Play the card
	var card_index: int = data.card_index
	var success = summoner.play_card(card_index, world_pos)

	if success:
		print("BattlefieldDropZone: Played card at ", world_pos)
	else:
		print("BattlefieldDropZone: Failed to play card")

## Convert screen coordinates to world coordinates
func _screen_to_world(screen_pos: Vector2) -> Vector2:
	if not camera:
		return screen_pos

	# Get viewport center
	var viewport_center = get_viewport().get_visible_rect().size / 2

	# Calculate offset from center accounting for zoom
	var offset_from_center = (screen_pos - viewport_center) / camera.zoom

	# Add to camera position
	return camera.global_position + offset_from_center
