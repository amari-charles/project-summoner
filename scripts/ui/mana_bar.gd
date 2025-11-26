extends Control
class_name ManaBar

## Layered tiered mana bar with stacked color system
## Each tier represents MANA_PER_TIER mana (default 10)
## Lower tiers show as full bars underneath higher tiers
## Example: 15/25 mana = full blue (tier 1) + half-filled orange (tier 2)
## Note: Maximum supported mana is MANA_PER_TIER * TIER_COLORS.size() (default 50)
##       Mana beyond this will display at max tier color

## Mana per tier (bar wraps at this value)
const MANA_PER_TIER: float = 10.0

## Tier colors - Blue intensity progression (magic-themed)
## Add more colors here to support higher max mana values
const TIER_COLORS: Array[Dictionary] = [
	{"fill": Color(0.4, 0.7, 1.0), "name": "light_blue"},    # Tier 1: Light Blue (0-10)
	{"fill": Color(0.25, 0.45, 0.95), "name": "royal_blue"}, # Tier 2: Royal Blue (10-20)
	{"fill": Color(0.3, 0.2, 0.7), "name": "indigo"},        # Tier 3: Indigo (20-30)
	{"fill": Color(0.55, 0.2, 0.8), "name": "purple"},       # Tier 4: Purple (30-40)
	{"fill": Color(0.85, 0.3, 0.75), "name": "magenta"},     # Tier 5: Magenta (40-50)
]

## UI styling
const BG_COLOR: Color = Color(0.08, 0.08, 0.12, 0.95)
const BORDER_COLOR: Color = Color(0.3, 0.3, 0.4, 1.0)
const HIGHLIGHT_ALPHA: float = 0.4
const HIGHLIGHT_HEIGHT: float = 4.0  # Thin highlight strip height
const FILL_PADDING: float = 4.0  # Padding from background edge
const FILL_ANIM_DURATION: float = 0.2  # Animation duration in seconds

## Node references
@onready var background: ColorRect = $Background
@onready var fill_container: Control = $FillContainer
@onready var highlight: ColorRect = $Highlight
@onready var mana_label: Label = $ManaLabel
@onready var tier_label: Label = $TierLabel

## Dynamically created tier fill bars (index 0 = bottom, renders first)
var tier_fills: Array[ColorRect] = []

## State
var current_mana: float = 0.0
var max_mana: float = 10.0
var current_tier: int = 0
var fill_tween: Tween = null

func _ready() -> void:
	_setup_background()
	_setup_fill_container()
	_create_tier_fills()
	_setup_highlight()
	_setup_labels()

	# Initialize display after layout
	await get_tree().process_frame
	_update_display(max_mana, max_mana, false)

func _setup_background() -> void:
	if not background:
		return
	background.color = BG_COLOR

func _setup_fill_container() -> void:
	# Create fill container if it doesn't exist
	if not fill_container:
		fill_container = Control.new()
		fill_container.name = "FillContainer"
		add_child(fill_container)
		move_child(fill_container, 1)  # After background

func _create_tier_fills() -> void:
	# Clear any existing fills
	for fill: ColorRect in tier_fills:
		fill.queue_free()
	tier_fills.clear()

	# Create a fill bar for each tier (bottom to top)
	for i: int in range(TIER_COLORS.size()):
		var fill: ColorRect = ColorRect.new()
		fill.name = "TierFill_%d" % i
		fill.color = TIER_COLORS[i].fill
		fill.size = Vector2(0, 0)  # Start with zero width
		fill.position = Vector2(FILL_PADDING, FILL_PADDING)
		fill_container.add_child(fill)
		tier_fills.append(fill)

func _setup_highlight() -> void:
	if not highlight:
		return
	highlight.color = Color(1.0, 1.0, 1.0, HIGHLIGHT_ALPHA)

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
	current_mana = current
	max_mana = maximum
	_update_display(current, maximum, true)

func _update_display(current: float, maximum: float, animate: bool) -> void:
	if not background or tier_fills.is_empty():
		return

	# Get bar dimensions
	var bar_width: float = background.size.x - (FILL_PADDING * 2)
	var bar_height: float = background.size.y - (FILL_PADDING * 2)

	# Calculate how many complete tiers we have
	var complete_tiers: int = int(current / MANA_PER_TIER)
	complete_tiers = mini(complete_tiers, TIER_COLORS.size())

	# Calculate partial fill for current tier
	var partial_mana: float = fmod(current, MANA_PER_TIER)
	var partial_tier: int = complete_tiers  # The tier that's partially filled
	var partial_percent: float = partial_mana / MANA_PER_TIER

	# Handle exact tier boundaries (e.g., exactly 10, 20, 30 mana)
	if current > 0 and partial_mana == 0:
		# At exact boundary - show previous tier as full, no partial
		partial_tier = complete_tiers
		partial_percent = 0.0

	# Track current tier for label display
	var display_tier: int = maxi(complete_tiers - 1, 0) if partial_percent == 0 and complete_tiers > 0 else complete_tiers
	display_tier = clampi(display_tier, 0, TIER_COLORS.size() - 1)
	current_tier = display_tier

	# Cancel existing animation
	if animate and fill_tween and fill_tween.is_running():
		fill_tween.kill()

	if animate:
		fill_tween = create_tween()
		fill_tween.set_ease(Tween.EASE_OUT)
		fill_tween.set_trans(Tween.TRANS_CUBIC)
		fill_tween.set_parallel(true)

	# Update each tier's fill bar
	for i: int in range(tier_fills.size()):
		var fill: ColorRect = tier_fills[i]
		var target_width: float = 0.0

		if i < complete_tiers:
			# This tier is complete - show full bar
			target_width = bar_width
		elif i == partial_tier and partial_percent > 0:
			# This tier is partially filled
			target_width = bar_width * partial_percent
		else:
			# This tier is empty
			target_width = 0.0

		# Set height (always full height)
		fill.size.y = bar_height

		if animate:
			fill_tween.tween_property(fill, "size:x", target_width, FILL_ANIM_DURATION)
		else:
			fill.size.x = target_width

	# Update highlight to match top-most visible tier
	_update_highlight(complete_tiers, partial_tier, partial_percent, bar_width, animate)

	# Update labels
	_update_labels(current, maximum, complete_tiers)

func _update_highlight(complete_tiers: int, partial_tier: int, partial_percent: float, bar_width: float, animate: bool) -> void:
	if not highlight:
		return

	# Highlight follows the topmost visible fill
	var highlight_width: float = 0.0
	var highlight_tier: int = -1

	if partial_percent > 0 and partial_tier < TIER_COLORS.size():
		highlight_width = bar_width * partial_percent
		highlight_tier = partial_tier
	elif complete_tiers > 0:
		highlight_width = bar_width
		highlight_tier = complete_tiers - 1

	# Tint highlight with tier color
	if highlight_tier >= 0 and highlight_tier < TIER_COLORS.size():
		var fill_color: Color = TIER_COLORS[highlight_tier].fill
		highlight.color = Color(
			lerpf(1.0, fill_color.r, 0.3),
			lerpf(1.0, fill_color.g, 0.3),
			lerpf(1.0, fill_color.b, 0.3),
			HIGHLIGHT_ALPHA
		)

	highlight.size.y = HIGHLIGHT_HEIGHT

	if animate and fill_tween:
		fill_tween.tween_property(highlight, "size:x", highlight_width, FILL_ANIM_DURATION)
	else:
		highlight.size.x = highlight_width

func _update_labels(current: float, maximum: float, complete_tiers: int) -> void:
	# Main mana label
	if mana_label:
		mana_label.text = Loc.t("ui.mana_bar.format", {"current": int(current), "max": int(maximum)})

	# Tier indicator (only show if we have complete tiers)
	if tier_label:
		if complete_tiers > 0:
			tier_label.text = Loc.t("ui.mana_bar.tier_multiplier", {"tier": complete_tiers + 1})
			tier_label.visible = true
		else:
			tier_label.visible = false
