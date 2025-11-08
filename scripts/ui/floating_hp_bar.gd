extends Node3D
class_name FloatingHPBar

## 3D floating health bar that follows a unit
## Managed by HPBarManager for pooling

## Visual settings
@export var bar_width: float = 0.8  ## Width in world units (smaller for units)
@export var bar_height: float = 0.08  ## Height in world units
@export var offset_y: float = 1.8  ## Height above unit
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
var debug_timer: float = 0.0  # For throttling debug output

## Visual components
var background_sprite: Sprite3D = null
var bar_sprite: Sprite3D = null
var camera: Camera3D = null

# Cached textures for bar rendering
var background_texture: ImageTexture = null
var bar_texture: ImageTexture = null

signal bar_hidden()  ## Emitted when bar fades out (for pooling)

func _ready() -> void:
	print("FloatingHPBar _ready() called")

	# Always create Sprite3D visuals (ignore scene file meshes for now)
	print("  Creating Sprite3D visuals...")
	_create_sprite_visuals()

	_find_camera()
	print("  Camera found: %s" % (camera != null))

func _process(delta: float) -> void:
	# Debug logging (throttled to once per second)
	debug_timer += delta
	var should_debug = debug_timer >= 1.0
	if should_debug:
		debug_timer = 0.0

	if not target_unit or not is_instance_valid(target_unit):
		if should_debug:
			print("FloatingHPBar._process(): No valid target_unit (target is %s)" % ("null" if not target_unit else "invalid"))
		return

	# Follow target unit
	var target_pos = target_unit.global_position + Vector3(0, offset_y, 0)
	global_position = target_pos

	if should_debug:
		print("FloatingHPBar._process(): Following %s at position %v, visible=%s" % [target_unit.name, global_position, visible])

	# Sprite3D handles billboarding automatically via billboard mode, no manual look_at needed!

	# Handle fade timer
	if show_on_damage_only and fade_timer > 0.0:
		fade_timer -= delta
		if fade_timer <= 0.0:
			_fade_out()

func _create_sprite_visuals() -> void:
	# Create solid color textures for background and bar (wider aspect ratio)
	var texture_width = 100
	var texture_height = 12
	background_texture = _create_solid_texture(texture_width, texture_height, background_color)
	bar_texture = _create_solid_texture(texture_width, texture_height, color_full)

	# Calculate pixel size to achieve desired world size
	var pixels_per_unit = texture_width / bar_width
	var pixel_size = 1.0 / pixels_per_unit

	# Create background sprite
	background_sprite = Sprite3D.new()
	background_sprite.texture = background_texture
	background_sprite.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	background_sprite.no_depth_test = true
	background_sprite.pixel_size = pixel_size
	add_child(background_sprite)

	# Create bar sprite (foreground)
	bar_sprite = Sprite3D.new()
	bar_sprite.texture = bar_texture
	bar_sprite.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	bar_sprite.no_depth_test = true
	bar_sprite.pixel_size = pixel_size
	# Centered by default (same as background), will shift position when scaled
	bar_sprite.centered = true
	bar_sprite.position = Vector3(0, 0, -0.01)  # Slightly forward
	add_child(bar_sprite)

	print("  Created Sprite3D visuals with billboard mode")
	print("    Target size: %.2f x %.2f world units" % [bar_width, bar_height])
	print("    Pixel size: %.4f" % pixel_size)
	print("    Background sprite width: %.2f units" % (texture_width * pixel_size))
	print("    Bar sprite width: %.2f units" % (texture_width * pixel_size))

func _create_solid_texture(width: int, height: int, color: Color) -> ImageTexture:
	var image = Image.create(width, height, false, Image.FORMAT_RGBA8)
	image.fill(color)
	return ImageTexture.create_from_image(image)

func _find_camera() -> void:
	# Find main camera in scene
	var viewport = get_viewport()
	if viewport:
		camera = viewport.get_camera_3d()

## Set target unit to follow
func set_target(unit: Node3D) -> void:
	print("FloatingHPBar.set_target() called for: %s" % (unit.name if unit else "null"))

	# Disconnect from previous target if exists
	if target_unit and is_instance_valid(target_unit):
		if target_unit.has_signal("hp_changed"):
			if target_unit.hp_changed.is_connected(_on_hp_changed):
				target_unit.hp_changed.disconnect(_on_hp_changed)

	target_unit = unit

	# Find camera now that we're in the scene tree
	if not camera:
		_find_camera()
		print("  Camera after find: %s" % (camera != null))

	# Connect to unit signals if available
	if target_unit and target_unit.has_signal("hp_changed"):
		if not target_unit.hp_changed.is_connected(_on_hp_changed):
			target_unit.hp_changed.connect(_on_hp_changed)
		print("  Connected to hp_changed signal")

	# Update HP immediately
	if target_unit and "current_hp" in target_unit and "max_hp" in target_unit:
		update_hp(target_unit.current_hp, target_unit.max_hp)
		print("  Initial HP: %.0f/%.0f" % [target_unit.current_hp, target_unit.max_hp])

## Update health bar display
func update_hp(current: float, maximum: float) -> void:
	current_hp = current
	max_hp = maximum

	var hp_percent = current_hp / max_hp if max_hp > 0 else 0.0
	hp_percent = clamp(hp_percent, 0.0, 1.0)

	# Update bar sprite region to show HP percentage
	if bar_sprite and bar_texture:
		# Scale the sprite horizontally to show HP
		bar_sprite.scale.x = hp_percent

		# Shift position to keep left edge aligned when scaled
		# When scale is 1.0, offset is 0 (centered)
		# When scale is 0.5, offset is -bar_width/4 (shift left by half of missing width)
		var x_offset = -bar_width * 0.5 * (1.0 - hp_percent)
		bar_sprite.position.x = x_offset

		# Update bar color based on HP percentage
		var bar_color = _get_hp_color(hp_percent)
		bar_sprite.modulate = bar_color

		print("FloatingHPBar.update_hp(): Set scale to %.2f, offset to %.2f, color to %s for HP %.0f%%" % [hp_percent, x_offset, bar_color, hp_percent * 100])

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
		var t = (hp_percent - 0.5) / 0.5
		return color_mid.lerp(color_full, t)
	else:
		# Interpolate between low and mid
		var t = hp_percent / 0.5
		return color_low.lerp(color_mid, t)

## Show the bar
func _show() -> void:
	if is_visible:
		return

	is_visible = true
	visible = true

	# Reset alpha for sprites
	if bar_sprite:
		bar_sprite.modulate.a = 1.0

	if background_sprite:
		background_sprite.modulate.a = 1.0

## Hide immediately
func _hide_immediate() -> void:
	is_visible = false
	visible = false

## Fade out animation
func _fade_out() -> void:
	if not is_visible:
		return

	# Animate alpha to 0
	var tween = create_tween()
	tween.set_parallel(true)

	if bar_sprite:
		tween.tween_property(bar_sprite, "modulate:a", 0.0, fade_duration).from(bar_sprite.modulate.a)

	if background_sprite:
		tween.tween_property(background_sprite, "modulate:a", 0.0, fade_duration).from(background_sprite.modulate.a)

	tween.finished.connect(func():
		_hide_immediate()
		bar_hidden.emit()
	)

## Reset for pooling reuse
func reset() -> void:
	# Disconnect signal from old target before clearing reference
	if target_unit and is_instance_valid(target_unit):
		if target_unit.has_signal("hp_changed"):
			if target_unit.hp_changed.is_connected(_on_hp_changed):
				target_unit.hp_changed.disconnect(_on_hp_changed)

	target_unit = null
	current_hp = 100.0
	max_hp = 100.0
	fade_timer = 0.0
	is_visible = true
	visible = true

	# Reset sprite properties
	if bar_sprite:
		bar_sprite.modulate = color_full
		bar_sprite.modulate.a = 1.0
		bar_sprite.scale = Vector3.ONE

	if background_sprite:
		background_sprite.modulate = background_color
		background_sprite.modulate.a = 1.0

## Signal handler for unit HP changes
func _on_hp_changed(new_hp: float, new_max_hp: float) -> void:
	update_hp(new_hp, new_max_hp)
