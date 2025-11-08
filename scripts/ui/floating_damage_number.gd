extends Node3D
class_name FloatingDamageNumber

## Floating damage number that rises and fades out
## Managed by DamageNumberManager for pooling

## Animation settings
@export var rise_speed: float = 1.5  ## Units per second
@export var rise_distance: float = 1.0  ## Total distance to rise
@export var fade_duration: float = 0.8  ## Seconds to fade out
@export var drift_amount: float = 0.3  ## Random horizontal drift

## State
var damage_value: float = 0.0
var is_crit: bool = false
var damage_type: String = "physical"
var is_pooled: bool = false
var lifetime: float = 0.0

## Visual components
var damage_sprite: Sprite3D = null
var damage_texture: ImageTexture = null
var damage_image: Image = null

## Animation
var start_position: Vector3 = Vector3.ZERO
var drift_offset: Vector3 = Vector3.ZERO

signal number_finished()  ## Emitted when animation completes

func _ready() -> void:
	_create_sprite_visuals()

func _process(delta: float) -> void:
	lifetime += delta

	# Rise up with drift
	var progress = lifetime / fade_duration
	var rise = rise_distance * progress
	global_position = start_position + Vector3(0, rise, 0) + drift_offset * progress

	# Fade out
	if damage_sprite:
		damage_sprite.modulate.a = 1.0 - progress

	# Cleanup when done
	if lifetime >= fade_duration:
		number_finished.emit()
		set_process(false)

func _create_sprite_visuals() -> void:
	# Create damage number sprite
	damage_sprite = Sprite3D.new()
	damage_sprite.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	damage_sprite.no_depth_test = true
	damage_sprite.pixel_size = 0.005  # Smaller for better text size
	damage_sprite.texture_filter = BaseMaterial3D.TEXTURE_FILTER_LINEAR
	add_child(damage_sprite)
	print("FloatingDamageNumber: Sprite3D created")

func show_damage(value: float, position: Vector3, is_critical: bool = false, dmg_type: String = "physical") -> void:
	print("FloatingDamageNumber.show_damage() called")
	print("  Value: %.1f, Position: %v, Crit: %s" % [value, position, is_critical])

	damage_value = value
	is_crit = is_critical
	damage_type = dmg_type

	# Set starting position with random drift
	start_position = position
	drift_offset = Vector3(
		randf_range(-drift_amount, drift_amount),
		0,
		randf_range(-drift_amount * 0.5, drift_amount * 0.5)
	)
	global_position = start_position

	print("  Start position: %v" % start_position)
	print("  Drift offset: %v" % drift_offset)

	# Reset state
	lifetime = 0.0
	set_process(true)
	visible = true

	print("  Set visible to true, processing enabled")
	print("  damage_sprite exists: %s" % (damage_sprite != null))

	# Render damage text to texture
	_render_damage_text()

func _render_damage_text() -> void:
	print("FloatingDamageNumber._render_damage_text() called")

	# Check sprite exists (should be created by _ready() already)
	if not damage_sprite:
		push_error("FloatingDamageNumber: damage_sprite is null! _ready() not called yet?")
		return

	# Determine text and color
	var text = str(int(damage_value))
	var text_color = _get_damage_color()

	# Add crit indicator
	if is_crit:
		text = text + "!"

	print("  Text: '%s', Color: %s" % [text, text_color])

	# Load font
	var font = ThemeDB.fallback_font
	var font_size = 32 if is_crit else 24  # Larger text

	# Calculate text size
	var text_size = font.get_string_size(text, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size)
	var width = int(text_size.x) + 16  # More padding
	var height = int(text_size.y) + 16

	print("  Image size: %dx%d" % [width, height])

	# Create image
	damage_image = Image.create(width, height, false, Image.FORMAT_RGBA8)
	damage_image.fill(Color(0, 0, 0, 0))  # Transparent background

	# Draw outline for readability (thicker)
	for x_off in [-2, -1, 0, 1, 2]:
		for y_off in [-2, -1, 0, 1, 2]:
			if x_off == 0 and y_off == 0:
				continue
			font.draw_string(damage_image, Vector2(8 + x_off, height - 8 + y_off), text,
				HORIZONTAL_ALIGNMENT_LEFT, -1, font_size, Color.BLACK)

	# Draw main text
	font.draw_string(damage_image, Vector2(8, height - 8), text,
		HORIZONTAL_ALIGNMENT_LEFT, -1, font_size, text_color)

	print("  Text drawn to image")

	# Update texture
	if damage_texture:
		damage_texture.update(damage_image)
		print("  Texture updated")
	else:
		damage_texture = ImageTexture.create_from_image(damage_image)
		print("  Texture created")

	# Set texture on sprite
	damage_sprite.texture = damage_texture
	print("  Texture set on sprite")

func _get_damage_color() -> Color:
	if is_crit:
		return Color(1.0, 0.8, 0.0)  # Gold for crits

	match damage_type:
		"physical":
			return Color.WHITE
		"magical", "spell":
			return Color(0.5, 0.8, 1.0)  # Light blue
		"fire":
			return Color(1.0, 0.5, 0.0)  # Orange
		_:
			return Color.WHITE

func reset() -> void:
	damage_value = 0.0
	is_crit = false
	damage_type = "physical"
	lifetime = 0.0
	set_process(false)
	visible = false

	if damage_sprite:
		damage_sprite.modulate.a = 1.0
