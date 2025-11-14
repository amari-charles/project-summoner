extends Control
class_name UnitDragPreview

## Clash Royale-style world-space unit preview during card drag
## Shows a ghost unit in the 3D battlefield that follows the cursor
## with a spawn indicator showing valid/invalid placement

## Visual components
var spawn_indicator: ColorRect = null

## References for position tracking
var viewport: Viewport = null
var camera_3d: Camera3D = null
var drop_zone: Node = null
var battlefield: Node = null

## 3D ghost unit in the battlefield
var ghost_unit: Node3D = null

## Y offset to compensate for sprite's internal positioning
var sprite_y_offset: float = 0.0

## Preview configuration
const GHOST_ALPHA: float = 0.6
const INDICATOR_RADIUS: float = 30.0
const INDICATOR_VALID_COLOR: Color = Color(0.0, 1.0, 0.0, 0.5)  # Green
const INDICATOR_INVALID_COLOR: Color = Color(1.0, 0.0, 0.0, 0.5)  # Red
const GROUND_Y: float = 0.0  # Y level of the ground plane

## Card being previewed
var card: Card = null

## Current world position (cached)
var current_world_pos: Vector3 = Vector3.ZERO

func _ready() -> void:
	# Set up container properties
	mouse_filter = Control.MOUSE_FILTER_IGNORE  # Don't interfere with drag system
	# This Control is just a placeholder for Godot's drag system
	# The actual visual is the 3D ghost in the battlefield

## Initialize preview with card data and references
func initialize(p_card: Card, p_viewport: Viewport, p_camera: Camera3D, p_drop_zone: Node) -> void:
	card = p_card
	viewport = p_viewport
	camera_3d = p_camera
	drop_zone = p_drop_zone

	# Find battlefield
	if p_viewport:
		battlefield = p_viewport.get_tree().get_first_node_in_group("battlefield") if p_viewport.get_tree() else null

	# Create spawn indicator (UI element)
	_create_spawn_indicator()

	# Create ghost unit in battlefield (3D element)
	_create_ghost_unit()

## Create ghost unit in the 3D battlefield
func _create_ghost_unit() -> void:
	if not card or not card.unit_scene:
		push_error("UnitDragPreview: No unit scene in card!")
		return

	if not battlefield:
		push_error("UnitDragPreview: No battlefield found!")
		return

	# Load the sprite character component scene
	var component_scene: PackedScene = load("res://scenes/units/sprite_character_2d5_component.tscn")
	if not component_scene:
		push_error("UnitDragPreview: Failed to load sprite_character_2d5_component.tscn")
		return

	# Instantiate the visual component
	ghost_unit = component_scene.instantiate()
	if not ghost_unit:
		push_error("UnitDragPreview: Failed to instantiate ghost unit")
		return

	# Get sprite frames and scale from the unit scene
	var temp_unit: Node = card.unit_scene.instantiate()
	if not temp_unit:
		push_error("UnitDragPreview: Failed to instantiate unit scene")
		return

	var sprite_frames_variant: Variant = temp_unit.get("sprite_frames")
	var sprite_frames: SpriteFrames = sprite_frames_variant if sprite_frames_variant is SpriteFrames else null

	var sprite_scale_variant: Variant = temp_unit.get("sprite_scale")
	var sprite_scale: float = sprite_scale_variant if sprite_scale_variant is float else 1.0

	# Clean up temporary unit
	temp_unit.queue_free()

	if not sprite_frames:
		push_error("UnitDragPreview: Unit has no sprite_frames!")
		ghost_unit.queue_free()
		ghost_unit = null
		return

	# Add to battlefield (3D world) so it's rendered by the main camera
	battlefield.add_child(ghost_unit)

	# Wait for _ready() to complete
	if get_tree():
		await get_tree().process_frame

	# Configure the ghost unit
	if ghost_unit.has_method("set_sprite_frames"):
		ghost_unit.call("set_sprite_frames", sprite_frames)

	if "sprite_scale" in ghost_unit:
		print("UnitDragPreview: Setting sprite_scale to ", sprite_scale)
		ghost_unit.set("sprite_scale", sprite_scale)

		# Recalculate sprite alignment with the new scale
		# This updates sprite_3d.position.y based on the actual sprite_scale
		if ghost_unit.has_method("_setup_sprite_alignment"):
			print("UnitDragPreview: Calling _setup_sprite_alignment()")
			ghost_unit.call("_setup_sprite_alignment")
		else:
			print("UnitDragPreview: WARNING - _setup_sprite_alignment method not found!")

	# Wait a frame for sprite_frames to be applied
	if get_tree():
		await get_tree().process_frame

	# Play idle animation
	if ghost_unit.has_method("play_animation"):
		ghost_unit.call("play_animation", "idle", true)

	# Wait for sprite positioning to complete (sprite_3d.position.y is set in _ready())
	if get_tree():
		await get_tree().process_frame
		await get_tree().process_frame  # Extra frame to ensure positioning completes

	# Set ghost transparency (make it look ghostly)
	var sprite_3d: Node = ghost_unit.get_node_or_null("Sprite3D")
	if sprite_3d and sprite_3d is Sprite3D:
		var sprite_3d_typed: Sprite3D = sprite_3d
		sprite_3d_typed.modulate = Color(1.0, 1.0, 1.0, GHOST_ALPHA)

		# Capture the Y offset so we can compensate when positioning the ghost
		# The sprite is offset upward locally, so we need to offset the unit downward
		sprite_y_offset = -sprite_3d_typed.position.y
		print("UnitDragPreview: Sprite3D local Y=", sprite_3d_typed.position.y, ", compensating with offset=", sprite_y_offset)

	print("UnitDragPreview: Ghost unit created in battlefield")

## Create circular spawn indicator on ground (UI element)
func _create_spawn_indicator() -> void:
	spawn_indicator = ColorRect.new()
	spawn_indicator.color = INDICATOR_VALID_COLOR
	spawn_indicator.mouse_filter = Control.MOUSE_FILTER_IGNORE
	spawn_indicator.size = Vector2(INDICATOR_RADIUS * 2, INDICATOR_RADIUS * 2)
	spawn_indicator.z_index = -1  # Behind everything
	add_child(spawn_indicator)

## Update ghost position every frame to follow cursor
func _process(_delta: float) -> void:
	if not viewport or not camera_3d or not ghost_unit:
		return

	# Get current mouse position in viewport space
	var mouse_pos: Vector2 = viewport.get_mouse_position()

	# Project mouse to ground plane in 3D world
	current_world_pos = _project_mouse_to_ground(mouse_pos)

	# Move the 3D ghost unit to follow the cursor on the ground
	# Apply Y offset to compensate for sprite's internal positioning
	ghost_unit.global_position = Vector3(current_world_pos.x, sprite_y_offset, current_world_pos.z)

	# Update spawn indicator position (UI element that shows where unit will spawn)
	if spawn_indicator:
		var ground_screen_pos: Vector2 = camera_3d.unproject_position(current_world_pos)

		# Position relative to this Control (which Godot keeps at mouse cursor)
		var relative_pos: Vector2 = ground_screen_pos - mouse_pos
		spawn_indicator.position = relative_pos - Vector2(INDICATOR_RADIUS, INDICATOR_RADIUS)

	# Update indicator color based on drop validity
	_update_indicator_validity(mouse_pos)

## Project screen position to ground plane (Y = 0) in 3D world
func _project_mouse_to_ground(screen_pos: Vector2) -> Vector3:
	if not camera_3d:
		return Vector3.ZERO

	# Ray from camera through screen position
	var from: Vector3 = camera_3d.project_ray_origin(screen_pos)
	var dir: Vector3 = camera_3d.project_ray_normal(screen_pos)

	# Ray-plane intersection with ground (Y = GROUND_Y)
	# Avoid division by zero
	if abs(dir.y) < 0.001:
		return Vector3(from.x, GROUND_Y, from.z)

	# Calculate intersection parameter t
	var t: float = (GROUND_Y - from.y) / dir.y

	# Only intersect if ray points toward ground
	if t < 0:
		return Vector3(from.x, GROUND_Y, from.z)

	# Calculate intersection point
	var hit_pos: Vector3 = from + dir * t

	return hit_pos

## Update spawn indicator color based on whether drop is valid
func _update_indicator_validity(mouse_pos: Vector2) -> void:
	if not drop_zone or not spawn_indicator:
		return

	# Check if drop is valid using drop zone's validation
	var is_valid: bool = false
	if drop_zone.has_method("_can_drop_data"):
		var drag_data: Dictionary = {"card": card, "source": "hand"}
		is_valid = drop_zone.call("_can_drop_data", mouse_pos, drag_data)

	# Update indicator color
	spawn_indicator.color = INDICATOR_VALID_COLOR if is_valid else INDICATOR_INVALID_COLOR

## Clean up when preview is destroyed
func _exit_tree() -> void:
	# Remove ghost unit from battlefield
	if ghost_unit and is_instance_valid(ghost_unit):
		if ghost_unit.get_parent():
			ghost_unit.get_parent().remove_child(ghost_unit)
		ghost_unit.queue_free()
