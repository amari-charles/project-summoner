extends Button
class_name StyledButton

## StyledButton - A polished button with smooth state transitions
##
## Provides three visual variants (primary, secondary, danger) with
## tween-animated hover and press effects. Uses StyleBoxFlat for
## rendering (no art assets required).

enum Variant { PRIMARY, SECONDARY, DANGER }

## Button variant determines color scheme
@export var variant: Variant = Variant.PRIMARY

## Enable smooth tween transitions between states
@export var enable_transitions: bool = true

## Transition timing
const HOVER_DURATION: float = 0.1
const PRESS_DURATION: float = 0.05
const HOVER_SCALE: Vector2 = Vector2(1.02, 1.02)
const PRESS_SCALE: Vector2 = Vector2(0.98, 0.98)
const NORMAL_SCALE: Vector2 = Vector2(1.0, 1.0)

## State
var _tween: Tween = null
var _is_hovered: bool = false
var _is_pressed: bool = false


func _ready() -> void:
	# Set pivot to center for scale animations
	pivot_offset = size / 2.0

	# Apply initial style
	_apply_variant_styles()

	# Connect signals for transitions
	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)
	button_down.connect(_on_button_down)
	button_up.connect(_on_button_up)
	resized.connect(_on_resized)


func _on_resized() -> void:
	# Keep pivot centered when button resizes
	pivot_offset = size / 2.0


## Apply StyleBoxFlat overrides based on variant
func _apply_variant_styles() -> void:
	match variant:
		Variant.PRIMARY:
			add_theme_stylebox_override("normal", ButtonStyleFactory.create_primary_normal())
			add_theme_stylebox_override("hover", ButtonStyleFactory.create_primary_hover())
			add_theme_stylebox_override("pressed", ButtonStyleFactory.create_primary_pressed())
			add_theme_stylebox_override("disabled", ButtonStyleFactory.create_primary_disabled())
			add_theme_stylebox_override("focus", ButtonStyleFactory.create_primary_hover())
		Variant.SECONDARY:
			add_theme_stylebox_override("normal", ButtonStyleFactory.create_secondary_normal())
			add_theme_stylebox_override("hover", ButtonStyleFactory.create_secondary_hover())
			add_theme_stylebox_override("pressed", ButtonStyleFactory.create_secondary_pressed())
			add_theme_stylebox_override("disabled", ButtonStyleFactory.create_secondary_disabled())
			add_theme_stylebox_override("focus", ButtonStyleFactory.create_secondary_hover())
		Variant.DANGER:
			add_theme_stylebox_override("normal", ButtonStyleFactory.create_danger_normal())
			add_theme_stylebox_override("hover", ButtonStyleFactory.create_danger_hover())
			add_theme_stylebox_override("pressed", ButtonStyleFactory.create_danger_pressed())
			add_theme_stylebox_override("disabled", ButtonStyleFactory.create_danger_disabled())
			add_theme_stylebox_override("focus", ButtonStyleFactory.create_danger_hover())

	# Text colors
	add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	add_theme_color_override("font_hover_color", Color.WHITE)
	add_theme_color_override("font_pressed_color", GameColorPalette.TEXT_SECONDARY)
	add_theme_color_override("font_disabled_color", GameColorPalette.TEXT_DISABLED)


func _on_mouse_entered() -> void:
	if disabled:
		return
	_is_hovered = true
	if enable_transitions and not _is_pressed:
		_tween_scale(HOVER_SCALE, HOVER_DURATION)


func _on_mouse_exited() -> void:
	_is_hovered = false
	if enable_transitions and not _is_pressed:
		_tween_scale(NORMAL_SCALE, HOVER_DURATION)


func _on_button_down() -> void:
	if disabled:
		return
	_is_pressed = true
	if enable_transitions:
		_tween_scale(PRESS_SCALE, PRESS_DURATION)


func _on_button_up() -> void:
	_is_pressed = false
	if enable_transitions:
		# Return to hover scale if still hovered, otherwise normal
		var target_scale: Vector2 = HOVER_SCALE if _is_hovered else NORMAL_SCALE
		_tween_scale(target_scale, HOVER_DURATION)


func _tween_scale(target: Vector2, duration: float) -> void:
	if _tween and _tween.is_valid():
		_tween.kill()
	_tween = create_tween()
	_tween.tween_property(self, "scale", target, duration) \
		.set_ease(Tween.EASE_OUT) \
		.set_trans(Tween.TRANS_QUAD)


## Change variant at runtime
func set_variant(new_variant: Variant) -> void:
	variant = new_variant
	_apply_variant_styles()
