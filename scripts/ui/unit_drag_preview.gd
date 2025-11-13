extends Control
class_name UnitDragPreview

## Clash Royale-style unit preview during card drag
## Shows a ghost/semi-transparent unit sprite at the cursor position
## with a spawn indicator showing valid/invalid placement

## Visual components
var preview_texture: TextureRect = null  # Displays the unit sprite
var spawn_indicator: ColorRect = null

## References for position tracking
var viewport: Viewport = null
var camera_3d: Camera3D = null
var drop_zone: Node = null
var battlefield: Node = null

## 3D visual component (added to battlefield, not this control)
var visual_component_3d: Node3D = null

## Preview configuration
const GHOST_ALPHA: float = 0.6
const INDICATOR_RADIUS: float = 30.0
const INDICATOR_VALID_COLOR: Color = Color(0.0, 1.0, 0.0, 0.5)  # Green
const INDICATOR_INVALID_COLOR: Color = Color(1.0, 0.0, 0.0, 0.5)  # Red
const PREVIEW_SIZE: float = 64.0  # Size of preview sprite in pixels (smaller to match card size)

## Card being previewed
var card: Card = null

## Current world position (cached)
var current_world_pos: Vector3 = Vector3.ZERO

func _ready() -> void:
	# Set up container properties
	mouse_filter = Control.MOUSE_FILTER_IGNORE  # Don't interfere with drag system
	# Don't set anchors - let drag system handle positioning

## Initialize preview with card data and references
func initialize(p_card: Card, p_viewport: Viewport, p_camera: Camera3D, p_drop_zone: Node) -> void:
	card = p_card
	viewport = p_viewport
	camera_3d = p_camera
	drop_zone = p_drop_zone

	# Find battlefield
	if p_viewport:
		battlefield = p_viewport.get_tree().get_first_node_in_group("battlefield") if p_viewport.get_tree() else null

	# Create spawn indicator
	_create_spawn_indicator()

	# Extract and create unit visual (added to battlefield)
	_create_unit_visual()

	# Create texture rect to display the unit
	_create_preview_texture()

## Extract visual component from card's unit scene and create ghost
func _create_unit_visual() -> void:
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
	visual_component_3d = component_scene.instantiate()
	if not visual_component_3d:
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
		visual_component_3d.queue_free()
		visual_component_3d = null
		return

	# Add to battlefield (3D world) FIRST so _ready() gets called
	battlefield.add_child(visual_component_3d)

	# Hide the 3D component from the main camera (we only want viewport rendering)
	# Position it far away so it doesn't appear in the game world
	visual_component_3d.global_position = Vector3(999999, 999999, 999999)

	# Wait for _ready() to complete
	if get_tree():
		await get_tree().process_frame

	# NOW configure the visual component (after _ready() has set up children)
	if visual_component_3d.has_method("set_sprite_frames"):
		visual_component_3d.call("set_sprite_frames", sprite_frames)
		print("UnitDragPreview: Set sprite frames")

	if "sprite_scale" in visual_component_3d:
		visual_component_3d.set("sprite_scale", sprite_scale)
		print("UnitDragPreview: Set sprite scale to ", sprite_scale)

	# Wait a frame for sprite_frames to be applied
	if get_tree():
		await get_tree().process_frame

	# Now play idle animation
	if visual_component_3d.has_method("play_animation"):
		visual_component_3d.call("play_animation", "idle", true)
		print("UnitDragPreview: Playing idle animation")

	# Debug: Check if sprite is actually set up
	var sprite_3d: Node = visual_component_3d.get_node_or_null("Sprite3D")
	if sprite_3d:
		var viewport_node: Node = sprite_3d.get_node_or_null("SubViewport/Model2D/CharacterSprite")
		if viewport_node and viewport_node is AnimatedSprite2D:
			var char_sprite: AnimatedSprite2D = viewport_node
			print("UnitDragPreview: CharacterSprite animation: ", char_sprite.animation)
			print("UnitDragPreview: CharacterSprite playing: ", char_sprite.is_playing())
			print("UnitDragPreview: CharacterSprite visible: ", char_sprite.visible)
			print("UnitDragPreview: CharacterSprite has frames: ", char_sprite.sprite_frames != null)

	# Set ghost transparency
	if get_tree():
		await get_tree().process_frame
		if sprite_3d and sprite_3d is Sprite3D:
			var sprite_3d_typed: Sprite3D = sprite_3d
			sprite_3d_typed.modulate = Color(1.0, 1.0, 1.0, GHOST_ALPHA)
			print("UnitDragPreview: Set Sprite3D alpha to ", GHOST_ALPHA)

## Create texture rect to display unit visual from viewport
func _create_preview_texture() -> void:
	if not visual_component_3d:
		print("UnitDragPreview: No visual_component_3d")
		return

	# Wait for viewport to be ready
	if get_tree():
		await get_tree().process_frame
		await get_tree().process_frame  # Wait extra frame for viewport to render

	# Get the viewport texture from the visual component
	var sprite_3d: Node = visual_component_3d.get_node_or_null("Sprite3D")
	if not sprite_3d:
		push_error("UnitDragPreview: No Sprite3D in visual component")
		return

	var sub_viewport: Node = sprite_3d.get_node_or_null("SubViewport")
	if not sub_viewport or not sub_viewport is SubViewport:
		push_error("UnitDragPreview: No SubViewport in Sprite3D")
		return

	var viewport_typed: SubViewport = sub_viewport
	var viewport_texture: ViewportTexture = viewport_typed.get_texture()

	print("UnitDragPreview: Viewport size: ", viewport_typed.size)
	print("UnitDragPreview: Viewport texture: ", viewport_texture)

	preview_texture = TextureRect.new()
	preview_texture.texture = viewport_texture
	preview_texture.custom_minimum_size = Vector2(PREVIEW_SIZE, PREVIEW_SIZE)
	preview_texture.size = Vector2(PREVIEW_SIZE, PREVIEW_SIZE)
	preview_texture.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	preview_texture.stretch_mode = TextureRect.STRETCH_SCALE
	preview_texture.mouse_filter = Control.MOUSE_FILTER_IGNORE

	# Position it centered at origin
	preview_texture.position = Vector2(-PREVIEW_SIZE / 2, -PREVIEW_SIZE / 2)

	# Make it semi-transparent
	preview_texture.modulate = Color(1.0, 1.0, 1.0, GHOST_ALPHA)

	# Set z_index to ensure it's above the indicator
	preview_texture.z_index = 10

	# Add a visible background to confirm the TextureRect is there
	var debug_bg: ColorRect = ColorRect.new()
	debug_bg.color = Color(1, 1, 0, 0.3)  # Yellow semi-transparent
	debug_bg.size = Vector2(PREVIEW_SIZE, PREVIEW_SIZE)
	debug_bg.position = Vector2.ZERO
	debug_bg.z_index = -1
	preview_texture.add_child(debug_bg)

	add_child(preview_texture)

	print("UnitDragPreview: Created preview texture with size ", preview_texture.size)
	print("UnitDragPreview: Preview texture position: ", preview_texture.position)
	print("UnitDragPreview: Texture rect has texture: ", preview_texture.texture != null)
	print("UnitDragPreview: Viewport texture get_size: ", viewport_texture.get_size() if viewport_texture else "null")

## Create circular spawn indicator on ground
func _create_spawn_indicator() -> void:
	spawn_indicator = ColorRect.new()
	spawn_indicator.color = INDICATOR_VALID_COLOR
	spawn_indicator.mouse_filter = Control.MOUSE_FILTER_IGNORE
	spawn_indicator.size = Vector2(INDICATOR_RADIUS * 2, INDICATOR_RADIUS * 2)
	spawn_indicator.z_index = -1  # Behind everything
	add_child(spawn_indicator)

## Update preview position every frame to follow cursor
func _process(_delta: float) -> void:
	if not viewport or not camera_3d:
		return

	# Get current mouse position in viewport space
	var mouse_pos: Vector2 = viewport.get_mouse_position()

	# Convert to 3D world position
	current_world_pos = _screen_to_world_3d(mouse_pos)

	# DON'T move the 3D visual component - keep it hidden far away
	# The viewport renders it wherever it is, and we show that via TextureRect

	# Preview texture position is already set during creation, don't move it

	# Debug: Log actual control position and preview positions
	if get_tree().get_frame() % 60 == 0:
		print("=== Drag Preview Debug ===")
		print("Control global_position: ", global_position)
		print("Control position: ", position)
		print("Mouse position: ", mouse_pos)
		if preview_texture:
			print("Preview texture position: ", preview_texture.position)
			print("Preview texture global_position: ", preview_texture.global_position)
			print("Preview texture size: ", preview_texture.size)

	# Position spawn indicator relative to this control
	# Project world ground position to screen, then make it relative to this control's position
	if spawn_indicator:
		var ground_pos: Vector3 = Vector3(current_world_pos.x, 0.0, current_world_pos.z)
		var screen_pos: Vector2 = camera_3d.unproject_position(ground_pos)

		# Make position relative to the drag preview control (which is at mouse_pos)
		# screen_pos - mouse_pos gives us the offset from cursor to ground position
		var relative_pos: Vector2 = screen_pos - mouse_pos
		# Center the indicator at that position
		spawn_indicator.position = relative_pos - Vector2(INDICATOR_RADIUS, INDICATOR_RADIUS)

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
	spawn_indicator.color = INDICATOR_VALID_COLOR if is_valid else INDICATOR_INVALID_COLOR

## Clean up when preview is destroyed
func _exit_tree() -> void:
	# Remove 3D visual component from battlefield
	if visual_component_3d and is_instance_valid(visual_component_3d):
		if visual_component_3d.get_parent():
			visual_component_3d.get_parent().remove_child(visual_component_3d)
		visual_component_3d.queue_free()
