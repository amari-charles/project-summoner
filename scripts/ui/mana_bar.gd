extends Control
class_name ManaBar

## Tiered mana bar with wrap-around color system
## Each tier represents MANA_PER_TIER mana (default 10)
## When mana exceeds a tier, the bar "wraps" with a new color
## Example: 15/20 mana = Tier 2 (orange), showing 5/10 filled

## Mana per tier (bar wraps at this value)
const MANA_PER_TIER: float = 10.0

## Tier colors - each tier gets a distinct color
const TIER_COLORS: Array[Dictionary] = [
	{"fill": Color(0.2, 0.5, 0.9), "glow": Color(0.3, 0.6, 1.0, 0.6), "name": "blue"},      # Tier 1: Blue (0-10)
	{"fill": Color(0.9, 0.6, 0.1), "glow": Color(1.0, 0.7, 0.2, 0.6), "name": "orange"},    # Tier 2: Orange (10-20)
	{"fill": Color(0.7, 0.3, 0.9), "glow": Color(0.8, 0.4, 1.0, 0.6), "name": "purple"},    # Tier 3: Purple (20-30)
	{"fill": Color(0.2, 0.8, 0.4), "glow": Color(0.3, 0.9, 0.5, 0.6), "name": "green"},     # Tier 4: Green (30-40)
	{"fill": Color(0.9, 0.2, 0.3), "glow": Color(1.0, 0.3, 0.4, 0.6), "name": "red"},       # Tier 5: Red (40-50)
]

## UI styling
const BG_COLOR: Color = Color(0.08, 0.08, 0.12, 0.95)
const BORDER_COLOR: Color = Color(0.3, 0.3, 0.4, 1.0)
const HIGHLIGHT_ALPHA: float = 0.4

## Node references
@onready var background: ColorRect = $Background
@onready var fill_bar: ColorRect = $FillBar
@onready var highlight: ColorRect = $Highlight
@onready var glow_overlay: ColorRect = $GlowOverlay
@onready var mana_label: Label = $ManaLabel
@onready var tier_label: Label = $TierLabel

## State
var current_mana: float = 0.0
var max_mana: float = 10.0
var current_tier: int = 0
var fill_tween: Tween = null
var glow_tween: Tween = null
var is_regenerating: bool = false

func _ready() -> void:
	_setup_background()
	_setup_fill()
	_setup_highlight()
	_setup_glow()
	_setup_labels()

	# Initialize display after layout
	await get_tree().process_frame
	_update_display(max_mana, max_mana, false)

func _setup_background() -> void:
	if not background:
		return

	# Create styled background
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = BG_COLOR
	style.border_color = BORDER_COLOR
	style.set_border_width_all(2)
	style.set_corner_radius_all(6)

	# Subtle inner shadow
	style.shadow_color = Color(0, 0, 0, 0.3)
	style.shadow_size = 2
	style.shadow_offset = Vector2(0, 1)

	# Apply via a Panel or directly set color
	background.color = BG_COLOR

func _setup_fill() -> void:
	if not fill_bar:
		return
	fill_bar.color = TIER_COLORS[0].fill

func _setup_highlight() -> void:
	if not highlight:
		return
	highlight.color = Color(1.0, 1.0, 1.0, HIGHLIGHT_ALPHA)

func _setup_glow() -> void:
	if not glow_overlay:
		return
	glow_overlay.color = TIER_COLORS[0].glow
	glow_overlay.visible = false
	glow_overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE

func _setup_labels() -> void:
	if mana_label:
		mana_label.add_theme_color_override("font_color", Color(0.95, 0.95, 1.0))
		mana_label.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.9))
		mana_label.add_theme_constant_override("outline_size", 4)

	if tier_label:
		tier_label.add_theme_color_override("font_color", Color(1.0, 0.9, 0.5))
		tier_label.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.9))
		tier_label.add_theme_constant_override("outline_size", 3)
		tier_label.visible = false  # Hidden for tier 1

## Main update function - called by GameUI when mana changes
func update_mana(current: float, maximum: float) -> void:
	var old_mana: float = current_mana
	current_mana = current
	max_mana = maximum

	_update_display(current, maximum, true)

	# Handle regeneration glow
	var new_is_regenerating: bool = current < maximum
	if new_is_regenerating != is_regenerating:
		is_regenerating = new_is_regenerating
		if is_regenerating:
			_start_glow_pulse()
		else:
			_stop_glow_pulse()

func _update_display(current: float, maximum: float, animate: bool) -> void:
	# Calculate tier (0-indexed)
	var new_tier: int = int(current / MANA_PER_TIER)
	new_tier = clampi(new_tier, 0, TIER_COLORS.size() - 1)

	# Calculate fill within current tier
	var tier_mana: float = fmod(current, MANA_PER_TIER)
	# If exactly at tier boundary and not zero, show full bar
	if current > 0 and tier_mana == 0:
		tier_mana = MANA_PER_TIER
		new_tier = maxi(new_tier - 1, 0)  # Show previous tier as full

	var fill_percent: float = tier_mana / MANA_PER_TIER

	# Update tier if changed
	if new_tier != current_tier:
		current_tier = new_tier
		_update_tier_colors()

	# Update fill bar width
	_update_fill(fill_percent, animate)

	# Update labels
	_update_labels(current, maximum)

func _update_tier_colors() -> void:
	var tier_data: Dictionary = TIER_COLORS[current_tier]

	if fill_bar:
		fill_bar.color = tier_data.fill

	if glow_overlay:
		var glow_color: Color = tier_data.glow
		glow_overlay.color = glow_color

	if highlight:
		# Tint highlight slightly with tier color
		var fill_color: Color = tier_data.fill
		highlight.color = Color(
			lerp(1.0, fill_color.r, 0.3),
			lerp(1.0, fill_color.g, 0.3),
			lerp(1.0, fill_color.b, 0.3),
			HIGHLIGHT_ALPHA
		)

func _update_fill(fill_percent: float, animate: bool) -> void:
	if not fill_bar or not background:
		return

	# Get available width (background width minus padding)
	var bar_width: float = background.size.x - 8  # 4px padding each side
	var target_width: float = bar_width * clampf(fill_percent, 0.0, 1.0)

	if animate:
		# Cancel existing tween
		if fill_tween and fill_tween.is_running():
			fill_tween.kill()

		fill_tween = create_tween()
		fill_tween.set_ease(Tween.EASE_OUT)
		fill_tween.set_trans(Tween.TRANS_CUBIC)
		fill_tween.tween_property(fill_bar, "size:x", target_width, 0.2)

		# Animate highlight to match
		if highlight:
			fill_tween.parallel().tween_property(highlight, "size:x", target_width, 0.2)
	else:
		fill_bar.size.x = target_width
		if highlight:
			highlight.size.x = target_width

func _update_labels(current: float, maximum: float) -> void:
	# Main mana label
	if mana_label:
		mana_label.text = Loc.t("ui.mana_bar.format", {"current": int(current), "max": int(maximum)})

	# Tier indicator (only show if tier > 0)
	if tier_label:
		if current_tier > 0:
			tier_label.text = "x%d" % (current_tier + 1)
			tier_label.visible = true
		else:
			tier_label.visible = false

func _start_glow_pulse() -> void:
	if not glow_overlay:
		return

	glow_overlay.visible = true
	glow_overlay.modulate.a = 1.0

	if glow_tween and glow_tween.is_running():
		glow_tween.kill()

	var tier_data: Dictionary = TIER_COLORS[current_tier]
	var base_color: Color = tier_data.glow
	var dim_color: Color = Color(base_color.r, base_color.g, base_color.b, 0.15)
	var bright_color: Color = Color(base_color.r, base_color.g, base_color.b, 0.5)

	glow_tween = create_tween()
	glow_tween.set_loops()
	glow_tween.tween_property(glow_overlay, "color", bright_color, 0.6)
	glow_tween.tween_property(glow_overlay, "color", dim_color, 0.6)

func _stop_glow_pulse() -> void:
	if glow_tween and glow_tween.is_running():
		glow_tween.kill()

	if glow_overlay:
		var fade_tween: Tween = create_tween()
		fade_tween.tween_property(glow_overlay, "modulate:a", 0.0, 0.25)
		fade_tween.tween_callback(func() -> void: glow_overlay.visible = false)
