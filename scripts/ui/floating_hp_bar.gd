extends Node3D
class_name FloatingHPBar

## 3D floating health bar that follows a unit
## Managed by HPBarManager for pooling

## Visual settings
@export var bar_width: float = 0.8  ## Width in world units (smaller for units)
@export var bar_height: float = 0.08  ## Height in world units
@export var offset_y: float = 3.5  ## Height above unit
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
var hp_bar_sprite: Sprite3D = null
var camera: Camera3D = null

# Cached texture for bar rendering
var bar_texture: ImageTexture = null
var bar_image: Image = null

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

	# Sprite3D handles billboarding automatically via billboard mode, no manual look_at needed!

	# Handle fade timer
	if show_on_damage_only and fade_timer > 0.0:
		fade_timer -= delta
		if fade_timer <= 0.0:
			_fade_out()

func _create_sprite_visuals() -> void:
	# Create a single sprite with dynamically drawn HP bar
	var texture_width = 100
	var texture_height = 12

	# Create image for drawing
	bar_image = Image.create(texture_width, texture_height, false, Image.FORMAT_RGBA8)

	# Draw initial full HP bar
	_redraw_bar_texture(1.0)

	# Calculate pixel size to achieve desired world size
	var pixels_per_unit = texture_width / bar_width
	var pixel_size = 1.0 / pixels_per_unit

	# Create single HP bar sprite
	hp_bar_sprite = Sprite3D.new()
	hp_bar_sprite.texture = bar_texture
	hp_bar_sprite.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	hp_bar_sprite.no_depth_test = true
	hp_bar_sprite.pixel_size = pixel_size
	hp_bar_sprite.centered = true
	add_child(hp_bar_sprite)

	print("  Created single Sprite3D HP bar (size: %.2f x %.2f units)" % [bar_width, bar_height])

func _redraw_bar_texture(hp_percent: float) -> void:
	if not bar_image:
		return

	var width = bar_image.get_width()
	var height = bar_image.get_height()

	# Calculate bar width in pixels
	var bar_pixel_width = int(width * hp_percent)

	# Fill background (entire image)
	bar_image.fill(background_color)

	# Draw foreground bar (left-aligned)
	if bar_pixel_width > 0:
		var bar_color = _get_hp_color(hp_percent)
		for y in range(height):
			for x in range(bar_pixel_width):
				bar_image.set_pixel(x, y, bar_color)

	# Update texture
	if bar_texture:
		bar_texture.update(bar_image)
	else:
		bar_texture = ImageTexture.create_from_image(bar_image)

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
	var tween = create_tween()

	if hp_bar_sprite:
		tween.tween_property(hp_bar_sprite, "modulate:a", 0.0, fade_duration).from(hp_bar_sprite.modulate.a)

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
	if hp_bar_sprite:
		hp_bar_sprite.modulate = Color.WHITE
		hp_bar_sprite.modulate.a = 1.0

	# Redraw at full HP
	_redraw_bar_texture(1.0)

## Signal handler for unit HP changes
func _on_hp_changed(new_hp: float, new_max_hp: float) -> void:
	update_hp(new_hp, new_max_hp)
