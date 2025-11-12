extends Node3D
class_name FloatingHPBar

## 3D floating health bar that follows a unit
## Managed by HPBarManager for pooling

## Default height for units without visual components (bases, etc.)
const BASE_HP_BAR_HEIGHT: float = 3.2

## Visual settings
@export var bar_width: float = 0.8  ## Width in world units (smaller for units)
@export var bar_height: float = 0.08  ## Height in world units
@export var offset_y: float = 3.2  ## Height above unit (above character head)
@export var show_on_damage_only: bool = false  ## Hide when at full HP
@export var fade_delay: float = 3.0  ## Seconds before fading when damaged
@export var fade_duration: float = 0.5  ## Fade out time

## Colors
@export var color_full: Color = Color.GREEN
@export var color_mid: Color = Color.YELLOW
@export var color_low: Color = Color.RED
@export var background_color: Color = Color(0.2, 0.2, 0.2, 0.8)

## State
var target_unit: Node3D = null
var current_hp: float = 100.0
var max_hp: float = 100.0
var is_pooled: bool = false
var fade_timer: float = 0.0
var is_visible: bool = true
var cached_offset_x: float = 0.0  ## Cached horizontal offset (calculated once, not every frame)
var fade_tween: Tween = null  ## Tween for fade out animation

## Visual components
var hp_bar_sprite: Sprite3D = null
var camera: Camera3D = null

# Cached texture for bar rendering
var bar_texture: ImageTexture = null
var bar_image: Image = null

signal bar_hidden()  ## Emitted when bar fades out (for pooling)

func _ready() -> void:
	# Always create Sprite3D visuals (ignore scene file meshes for now)
	_create_sprite_visuals()
	_find_camera()

func _exit_tree() -> void:
	# Kill any active tweens to prevent lambda capture errors
	if fade_tween and fade_tween.is_valid():
		fade_tween.kill()

func _process(delta: float) -> void:
	if not target_unit or not is_instance_valid(target_unit):
		return

	# Follow target unit with cached offsets
	var target_pos: Vector3 = target_unit.global_position + Vector3(cached_offset_x, offset_y, 0)
	global_position = target_pos

	# Sprite3D handles billboarding automatically via billboard mode, no manual look_at needed!

	# Handle fade timer
	if show_on_damage_only and fade_timer > 0.0:
		fade_timer -= delta
		if fade_timer <= 0.0:
			_fade_out()

func _create_sprite_visuals() -> void:
	# Create a single sprite with dynamically drawn HP bar
	var texture_width: int = 100
	var texture_height: int = 12

	# Create image for drawing
	bar_image = Image.create(texture_width, texture_height, false, Image.FORMAT_RGBA8)

	# Draw initial full HP bar
	_redraw_bar_texture(1.0)

	# Calculate pixel size to achieve desired world size
	var pixels_per_unit: float = texture_width / bar_width
	var pixel_size: float = 1.0 / pixels_per_unit

	# Create single HP bar sprite
	hp_bar_sprite = Sprite3D.new()
	hp_bar_sprite.texture = bar_texture
	hp_bar_sprite.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	hp_bar_sprite.no_depth_test = true
	hp_bar_sprite.pixel_size = pixel_size
	hp_bar_sprite.centered = true
	add_child(hp_bar_sprite)

func _redraw_bar_texture(hp_percent: float) -> void:
	if not bar_image:
		return

	var width: int = bar_image.get_width()
	var height: int = bar_image.get_height()

	# Calculate bar width in pixels
	var bar_pixel_width: int = int(width * hp_percent)

	# Fill background (entire image)
	bar_image.fill(background_color)

	# Draw foreground bar (left-aligned)
	if bar_pixel_width > 0:
		var bar_color: Color = _get_hp_color(hp_percent)
		for y: int in range(height):
			for x: int in range(bar_pixel_width):
				bar_image.set_pixel(x, y, bar_color)

	# Update texture
	if bar_texture:
		bar_texture.update(bar_image)
	else:
		bar_texture = ImageTexture.create_from_image(bar_image)

func _find_camera() -> void:
	# Find main camera in scene
	var viewport: Viewport = get_viewport()
	if viewport:
		camera = viewport.get_camera_3d()

## Set target unit to follow
func set_target(unit: Node3D) -> void:
	# Disconnect from previous target if exists
	if target_unit and is_instance_valid(target_unit):
		if target_unit.has_signal("hp_changed"):
			var hp_signal: Signal = target_unit.get("hp_changed")
			if hp_signal.is_connected(_on_hp_changed):
				hp_signal.disconnect(_on_hp_changed)

	target_unit = unit

	# Defer offset calculation to next frame to ensure visual component is ready
	# (Visual component's _ready() might not be called yet when unit is first created)
	call_deferred("_deferred_calculate_offset")

	# Find camera now that we're in the scene tree
	if not camera:
		_find_camera()

	# Connect to unit signals if available
	if target_unit and target_unit.has_signal("hp_changed"):
		var hp_signal: Signal = target_unit.get("hp_changed")
		if not hp_signal.is_connected(_on_hp_changed):
			hp_signal.connect(_on_hp_changed)

	# Update HP immediately
	if target_unit and "current_hp" in target_unit and "max_hp" in target_unit:
		var unit_current_hp: float = target_unit.get("current_hp")
		var unit_max_hp: float = target_unit.get("max_hp")
		update_hp(unit_current_hp, unit_max_hp)

## Deferred calculation of offset (called after visual component is ready)
func _deferred_calculate_offset() -> void:
	# Safety check: unit might have been destroyed before deferred call
	if not is_instance_valid(target_unit):
		return

	# Calculate vertical offset
	offset_y = _calculate_bar_offset()

	# Cache horizontal offset (doesn't change per frame)
	cached_offset_x = 0.0
	var visual: Node = target_unit.get_node_or_null("Visual")
	if visual and visual.has_method("get_hp_bar_offset_x"):
		var offset_result: float = visual.call("get_hp_bar_offset_x")
		cached_offset_x = offset_result

## Update health bar display
func update_hp(current: float, maximum: float) -> void:
	current_hp = current
	max_hp = maximum

	var hp_percent = current_hp / max_hp if max_hp > 0 else 0.0
	hp_percent = clamp(hp_percent, 0.0, 1.0)

	# Redraw the bar texture with new HP percentage
	_redraw_bar_texture(hp_percent)

	# Handle show_on_damage_only behavior
	if show_on_damage_only:
		if hp_percent < 1.0:
			_show()
			fade_timer = fade_delay
		else:
			_hide_immediate()

## Get color based on HP percentage
func _get_hp_color(hp_percent: float) -> Color:
	if hp_percent > 0.5:
		# Interpolate between full and mid
		var t: float = (hp_percent - 0.5) / 0.5
		return color_mid.lerp(color_full, t)
	else:
		# Interpolate between low and mid
		var t: float = hp_percent / 0.5
		return color_low.lerp(color_mid, t)

## Show the bar
func _show() -> void:
	if is_visible:
		return

	is_visible = true
	visible = true

	# Reset alpha
	if hp_bar_sprite:
		hp_bar_sprite.modulate.a = 1.0

## Hide immediately
func _hide_immediate() -> void:
	is_visible = false
	visible = false

## Fade out animation
func _fade_out() -> void:
	if not is_visible:
		return

	# Animate alpha to 0
	fade_tween = create_tween()

	if hp_bar_sprite:
		fade_tween.tween_property(hp_bar_sprite, "modulate:a", 0.0, fade_duration).from(hp_bar_sprite.modulate.a)

	fade_tween.finished.connect(func() -> void:
		_hide_immediate()
		bar_hidden.emit()
	)

## Reset for pooling reuse
func reset() -> void:
	# Disconnect signal from old target before clearing reference
	if target_unit and is_instance_valid(target_unit):
		if target_unit.has_signal("hp_changed"):
			var hp_signal: Signal = target_unit.get("hp_changed")
			if hp_signal.is_connected(_on_hp_changed):
				hp_signal.disconnect(_on_hp_changed)

	target_unit = null
	current_hp = 100.0
	max_hp = 100.0
	fade_timer = 0.0
	is_visible = true
	visible = true
	cached_offset_x = 0.0

	# Reset sprite properties
	if hp_bar_sprite:
		hp_bar_sprite.modulate = Color.WHITE
		hp_bar_sprite.modulate.a = 1.0

	# Redraw at full HP
	_redraw_bar_texture(1.0)

## Calculate HP bar offset dynamically based on visual component height
func _calculate_bar_offset() -> float:
	assert(target_unit != null, "HPBar: target_unit is null")

	var visual: Node = target_unit.get_node_or_null("Visual")
	if not visual:
		# Base units don't have Visual components, use default
		return BASE_HP_BAR_HEIGHT

	# Query sprite height from visual component
	if visual.has_method("get_sprite_height"):
		var sprite_height: float = visual.call("get_sprite_height")
		return sprite_height * 1.1

	# Visual component doesn't support height calculation, use default
	return BASE_HP_BAR_HEIGHT

## Signal handler for unit HP changes
func _on_hp_changed(new_hp: float, new_max_hp: float) -> void:
	update_hp(new_hp, new_max_hp)
