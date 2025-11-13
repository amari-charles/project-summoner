extends Control
class_name UnitDragPreview

## Clash Royale-style unit preview during card drag
## Shows a ghost/semi-transparent unit sprite at the cursor position
## with a spawn indicator showing valid/invalid placement

## Visual components
var unit_sprite: AnimatedSprite2D = null
var spawn_indicator: ColorRect = null

## References for position tracking
var viewport: Viewport = null
var camera_3d: Camera3D = null
var drop_zone: Node = null

## Preview configuration
const GHOST_ALPHA: float = 0.6
const INDICATOR_SIZE: float = 60.0
const INDICATOR_VALID_COLOR: Color = Color(0.0, 1.0, 0.0, 0.5)  # Green
const INDICATOR_INVALID_COLOR: Color = Color(1.0, 0.0, 0.0, 0.5)  # Red

## Card being previewed
var card: Card = null

func _ready() -> void:
	# Set up container properties
	mouse_filter = Control.MOUSE_FILTER_IGNORE  # Don't interfere with drag system
	set_anchors_preset(Control.PRESET_TOP_LEFT)

	# Create spawn indicator (ground circle)
	spawn_indicator = ColorRect.new()
	spawn_indicator.size = Vector2(INDICATOR_SIZE, INDICATOR_SIZE)
	spawn_indicator.pivot_offset = spawn_indicator.size / 2.0
	spawn_indicator.color = INDICATOR_VALID_COLOR
	add_child(spawn_indicator)

	# Create unit sprite container
	var sprite_container: Control = Control.new()
	sprite_container.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(sprite_container)

	# Unit sprite will be added when initialized

## Initialize preview with card data and references
func initialize(p_card: Card, p_viewport: Viewport, p_camera: Camera3D, p_drop_zone: Node) -> void:
	card = p_card
	viewport = p_viewport
	camera_3d = p_camera
	drop_zone = p_drop_zone

	# Extract and create unit visual
	_create_unit_visual()

## Extract sprite frames from card's unit scene and create ghost visual
func _create_unit_visual() -> void:
	if not card or not card.unit_scene:
		push_error("UnitDragPreview: No unit scene in card!")
		return

	# Temporarily instantiate unit to extract sprite data
	var temp_unit: Node = card.unit_scene.instantiate()
	if not temp_unit:
		push_error("UnitDragPreview: Failed to instantiate unit scene!")
		return

	# Get sprite frames from unit
	var sprite_frames_variant: Variant = temp_unit.get("sprite_frames")
	var sprite_frames: SpriteFrames = sprite_frames_variant if sprite_frames_variant is SpriteFrames else null

	if not sprite_frames:
		push_error("UnitDragPreview: Unit has no sprite_frames!")
		temp_unit.queue_free()
		return

	# Create animated sprite for preview
	unit_sprite = AnimatedSprite2D.new()
	unit_sprite.sprite_frames = sprite_frames
	unit_sprite.modulate.a = GHOST_ALPHA
	unit_sprite.play("idle")

	# Scale sprite to reasonable preview size
	unit_sprite.scale = Vector2(2.0, 2.0)

	# Add to scene (second child, after spawn_indicator)
	if get_child_count() >= 2:
		var sprite_container: Node = get_child(1)
		if sprite_container is Control:
			var container: Control = sprite_container
			container.add_child(unit_sprite)

	# Clean up temporary unit
	temp_unit.queue_free()

## Update preview position every frame to follow cursor
func _process(_delta: float) -> void:
	if not viewport or not camera_3d or not drop_zone:
		return

	# Get current mouse position
	var mouse_pos: Vector2 = viewport.get_mouse_position()

	# Convert to 3D world position
	var world_pos: Vector3 = _screen_to_world_3d(mouse_pos)

	# Convert back to screen space for indicator positioning
	var screen_pos: Vector2 = camera_3d.unproject_position(world_pos)

	# Position spawn indicator at ground level
	spawn_indicator.position = screen_pos - spawn_indicator.pivot_offset

	# Position unit sprite slightly above indicator
	if unit_sprite:
		unit_sprite.global_position = screen_pos - Vector2(0, 40)  # Offset up slightly

	# Update indicator color based on drop validity
	_update_indicator_validity(mouse_pos)

## Convert screen position to 3D world position on spawn plane
func _screen_to_world_3d(screen_pos: Vector2) -> Vector3:
	if not camera_3d:
		return Vector3.ZERO

	# Project ray from camera through screen position
	var from: Vector3 = camera_3d.project_ray_origin(screen_pos)
	var to: Vector3 = from + camera_3d.project_ray_normal(screen_pos) * 1000.0

	# Intersect with spawn plane (Y = 0.0)
	var spawn_y: float = 0.0
	var ray_dir: Vector3 = to - from

	# Avoid division by zero
	if abs(ray_dir.y) < 0.001:
		return Vector3(from.x, spawn_y, from.z)

	var t: float = (spawn_y - from.y) / ray_dir.y
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
	spawn_indicator.color = INDICATOR_VALID_COLOR if is_valid else INDICATOR_INVALID_COLOR
