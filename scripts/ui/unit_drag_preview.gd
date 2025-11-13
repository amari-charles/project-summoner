extends Control
class_name UnitDragPreview

## Clash Royale-style unit preview during card drag
## Shows a ghost/semi-transparent unit sprite at the cursor position
## with a spawn indicator showing valid/invalid placement

## Visual components
var visual_component: Node3D = null  # Character2D5Component
var spawn_indicator: TextureRect = null

## References for position tracking
var viewport: Viewport = null
var camera_3d: Camera3D = null
var drop_zone: Node = null

## Preview configuration
const GHOST_ALPHA: float = 0.6
const INDICATOR_RADIUS: float = 30.0
const INDICATOR_VALID_COLOR: Color = Color(0.0, 1.0, 0.0, 0.5)  # Green
const INDICATOR_INVALID_COLOR: Color = Color(1.0, 0.0, 0.0, 0.5)  # Red

## Card being previewed
var card: Card = null

## Current world position (cached)
var current_world_pos: Vector3 = Vector3.ZERO

func _ready() -> void:
	# Set up container properties
	mouse_filter = Control.MOUSE_FILTER_IGNORE  # Don't interfere with drag system
	set_anchors_preset(Control.PRESET_FULL_RECT)

## Initialize preview with card data and references
func initialize(p_card: Card, p_viewport: Viewport, p_camera: Camera3D, p_drop_zone: Node) -> void:
	card = p_card
	viewport = p_viewport
	camera_3d = p_camera
	drop_zone = p_drop_zone

	# Extract and create unit visual
	_create_unit_visual()

	# Create spawn indicator
	_create_spawn_indicator()

## Extract visual component from card's unit scene and create ghost
func _create_unit_visual() -> void:
	if not card or not card.unit_scene:
		push_error("UnitDragPreview: No unit scene in card!")
		return

	# Load the sprite character component scene
	var component_scene: PackedScene = load("res://scenes/units/sprite_character_2d5_component.tscn")
	if not component_scene:
		push_error("UnitDragPreview: Failed to load sprite_character_2d5_component.tscn")
		return

	# Instantiate the visual component
	visual_component = component_scene.instantiate()
	if not visual_component:
		push_error("UnitDragPreview: Failed to instantiate visual component")
		return

	# Get sprite frames from the unit scene
	var temp_unit: Node = card.unit_scene.instantiate()
	if not temp_unit:
		push_error("UnitDragPreview: Failed to instantiate unit scene")
		return

	var sprite_frames_variant: Variant = temp_unit.get("sprite_frames")
	var sprite_frames: SpriteFrames = sprite_frames_variant if sprite_frames_variant is SpriteFrames else null

	# Get sprite scale if available
	var sprite_scale_variant: Variant = temp_unit.get("sprite_scale")
	var sprite_scale: float = sprite_scale_variant if sprite_scale_variant is float else 1.0

	# Clean up temporary unit
	temp_unit.queue_free()

	if not sprite_frames:
		push_error("UnitDragPreview: Unit has no sprite_frames!")
		visual_component.queue_free()
		visual_component = null
		return

	# Configure the visual component
	if visual_component.has_method("set_sprite_frames"):
		visual_component.call("set_sprite_frames", sprite_frames)

	if "sprite_scale" in visual_component:
		visual_component.set("sprite_scale", sprite_scale)

	# Set ghost transparency
	visual_component.modulate = Color(1.0, 1.0, 1.0, GHOST_ALPHA)

	# Play idle animation
	if visual_component.has_method("play_animation"):
		visual_component.call("play_animation", "idle", true)

	# Add to scene tree (we'll position it in _process)
	add_child(visual_component)

## Create circular spawn indicator on ground
func _create_spawn_indicator() -> void:
	spawn_indicator = TextureRect.new()
	spawn_indicator.modulate = INDICATOR_VALID_COLOR
	spawn_indicator.expand_mode = TextureRect.EXPAND_FIT_WIDTH_PROPORTIONAL
	spawn_indicator.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	spawn_indicator.mouse_filter = Control.MOUSE_FILTER_IGNORE

	# Create a simple circle texture using a placeholder
	# In production, you'd use an actual texture asset
	spawn_indicator.custom_minimum_size = Vector2(INDICATOR_RADIUS * 2, INDICATOR_RADIUS * 2)
	spawn_indicator.pivot_offset = spawn_indicator.custom_minimum_size / 2.0

	# Create a simple colored rect as indicator for now
	var circle_bg: ColorRect = ColorRect.new()
	circle_bg.color = Color(1, 1, 1, 0.3)
	circle_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	spawn_indicator.add_child(circle_bg)

	add_child(spawn_indicator)

## Update preview position every frame to follow cursor
func _process(_delta: float) -> void:
	if not viewport or not camera_3d:
		return

	# Get current mouse position
	var mouse_pos: Vector2 = viewport.get_mouse_position()

	# Convert to 3D world position
	current_world_pos = _screen_to_world_3d(mouse_pos)

	# Position visual component at world position
	if visual_component:
		visual_component.global_position = current_world_pos

	# Position spawn indicator on ground
	if spawn_indicator:
		var ground_pos: Vector3 = Vector3(current_world_pos.x, 0.0, current_world_pos.z)
		var screen_pos: Vector2 = camera_3d.unproject_position(ground_pos)
		spawn_indicator.position = screen_pos - spawn_indicator.pivot_offset

	# Update indicator color based on drop validity
	_update_indicator_validity(mouse_pos)

## Convert screen position to 3D world position on spawn plane
func _screen_to_world_3d(screen_pos: Vector2) -> Vector3:
	if not camera_3d:
		return Vector3.ZERO

	# Project ray from camera through screen position
	var from: Vector3 = camera_3d.project_ray_origin(screen_pos)
	var direction: Vector3 = camera_3d.project_ray_normal(screen_pos)
	var to: Vector3 = from + direction * 1000.0

	# Intersect with spawn plane (Y = 0.0)
	var spawn_y: float = 0.0
	var ray_dir: Vector3 = to - from

	# Avoid division by zero
	if abs(ray_dir.y) < 0.001:
		return Vector3(from.x, spawn_y, from.z)

	var t: float = (spawn_y - from.y) / ray_dir.y

	# Only intersect if ray is pointing toward plane
	if t < 0:
		return Vector3(from.x, spawn_y, from.z)

	var hit_pos: Vector3 = from + ray_dir * t

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
	spawn_indicator.modulate = INDICATOR_VALID_COLOR if is_valid else INDICATOR_INVALID_COLOR
