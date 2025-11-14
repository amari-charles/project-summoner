extends Control
class_name ManaBar

## Animated mana bar with smooth fill and regeneration effects

@onready var progress_bar: ProgressBar = $ProgressBar
@onready var mana_label: Label = $ManaLabel
@onready var glow_overlay: ColorRect = $GlowOverlay
@onready var gradient_base: ColorRect = $GradientBase
@onready var gradient_top: ColorRect = $GradientTop
@onready var edge_highlight: ColorRect = $EdgeHighlight

## Tween for smooth bar animation
var fill_tween: Tween = null

## Glow pulse tween
var glow_tween: Tween = null

## Track if we're currently regenerating
var is_regenerating: bool = false

## Colors - Rich gradient blue/cyan palette
const MANA_DARK: Color = Color(0.1, 0.3, 0.7)     # Deep blue
const MANA_BRIGHT: Color = Color(0.3, 0.7, 1.0)   # Bright cyan
const GLOW_COLOR: Color = Color(0.5, 0.9, 1.0, 0.5)  # Bright cyan glow
const BORDER_COLOR: Color = Color(0.15, 0.4, 0.8)  # Blue border
const BG_COLOR: Color = Color(0.05, 0.05, 0.1, 0.9)  # Dark blue-black

func _ready() -> void:
	# Setup progress bar
	if progress_bar:
		progress_bar.min_value = 0.0
		progress_bar.max_value = 10.0
		progress_bar.value = 10.0

		# Fill style with bright cyan color
		var fill_style: StyleBoxFlat = StyleBoxFlat.new()
		fill_style.bg_color = MANA_BRIGHT

		# Rounded corners
		fill_style.corner_radius_top_left = 6
		fill_style.corner_radius_top_right = 6
		fill_style.corner_radius_bottom_left = 6
		fill_style.corner_radius_bottom_right = 6

		# Inner shadow for depth
		fill_style.shadow_size = 2
		fill_style.shadow_color = Color(0, 0, 0, 0.3)
		fill_style.shadow_offset = Vector2(0, 2)

		progress_bar.add_theme_stylebox_override("fill", fill_style)

		# Background style with border
		var bg_style: StyleBoxFlat = StyleBoxFlat.new()
		bg_style.bg_color = BG_COLOR

		# Rounded corners matching fill
		bg_style.corner_radius_top_left = 6
		bg_style.corner_radius_top_right = 6
		bg_style.corner_radius_bottom_left = 6
		bg_style.corner_radius_bottom_right = 6

		# Border
		bg_style.border_width_left = 2
		bg_style.border_width_right = 2
		bg_style.border_width_top = 2
		bg_style.border_width_bottom = 2
		bg_style.border_color = BORDER_COLOR

		# Outer glow/shadow
		bg_style.shadow_size = 4
		bg_style.shadow_color = Color(0.2, 0.4, 0.8, 0.4)
		bg_style.shadow_offset = Vector2(0, 0)

		progress_bar.add_theme_stylebox_override("background", bg_style)

	# Setup label styling
	if mana_label:
		mana_label.add_theme_color_override("font_color", Color(0.9, 0.95, 1.0))
		mana_label.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.8))
		mana_label.add_theme_constant_override("outline_size", 8)
		mana_label.add_theme_font_size_override("font_size", 18)

	# Setup glow overlay
	if glow_overlay:
		glow_overlay.color = GLOW_COLOR
		glow_overlay.visible = false
		glow_overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE

	# Setup gradient layers - clip to progress bar fill area
	if progress_bar:
		_update_gradient_clip(progress_bar.value)

## Update mana display with smooth animation
func update_mana(current: float, maximum: float) -> void:
	# Update label
	if mana_label:
		mana_label.text = "Mana: %d/%d" % [int(current), int(maximum)]

	# Animate progress bar
	if progress_bar:
		_animate_bar_to(current)

	# Check if regenerating (mana increasing and not at max)
	var new_is_regenerating: bool = current < maximum
	if new_is_regenerating != is_regenerating:
		is_regenerating = new_is_regenerating
		if is_regenerating:
			_start_glow_pulse()
		else:
			_stop_glow_pulse()

## Smoothly animate bar to target value
func _animate_bar_to(target_value: float) -> void:
	# Cancel existing tween
	if fill_tween and fill_tween.is_running():
		fill_tween.kill()

	# Create new tween
	fill_tween = create_tween()
	fill_tween.set_ease(Tween.EASE_OUT)
	fill_tween.set_trans(Tween.TRANS_CUBIC)
	fill_tween.set_parallel(true)

	# Animate progress bar and gradient layers together
	var current_val: float = progress_bar.value
	fill_tween.tween_property(progress_bar, "value", target_value, 0.25)
	fill_tween.tween_method(_update_gradient_clip, current_val, target_value, 0.25)

## Update gradient and highlight to match fill amount
func _update_gradient_clip(current_value: float) -> void:
	if not progress_bar:
		return

	# Calculate fill percentage
	var fill_percent: float = current_value / progress_bar.max_value
	var bar_width: float = progress_bar.size.x

	# Clip gradient layers to match fill
	if gradient_base:
		gradient_base.size.x = bar_width * fill_percent
	if gradient_top:
		gradient_top.size.x = bar_width * fill_percent
	if edge_highlight:
		edge_highlight.size.x = bar_width * fill_percent

## Start pulsing glow effect
func _start_glow_pulse() -> void:
	if not glow_overlay:
		return

	glow_overlay.visible = true

	# Cancel existing glow tween
	if glow_tween and glow_tween.is_running():
		glow_tween.kill()

	# Create pulsing animation
	glow_tween = create_tween()
	glow_tween.set_loops()

	# Pulse alpha between 0.1 and 0.4
	var pulse_color_dim: Color = Color(GLOW_COLOR.r, GLOW_COLOR.g, GLOW_COLOR.b, 0.1)
	var pulse_color_bright: Color = Color(GLOW_COLOR.r, GLOW_COLOR.g, GLOW_COLOR.b, 0.4)

	glow_tween.tween_property(glow_overlay, "color", pulse_color_bright, 0.8)
	glow_tween.tween_property(glow_overlay, "color", pulse_color_dim, 0.8)

## Stop pulsing glow effect
func _stop_glow_pulse() -> void:
	if glow_tween and glow_tween.is_running():
		glow_tween.kill()

	if glow_overlay:
		# Fade out
		var fade_tween: Tween = create_tween()
		fade_tween.tween_property(glow_overlay, "modulate:a", 0.0, 0.3)
		fade_tween.tween_callback(func() -> void: glow_overlay.visible = false)
