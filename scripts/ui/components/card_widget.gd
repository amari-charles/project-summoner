extends VBoxContainer
class_name CardWidget

## CardWidget - Reusable Card Display Component
##
## Displays a card thumbnail with name, cost, type, and rarity.
## Supports drag-and-drop for deck building.
## Progression info (level, XP bar) shown below the card.

## Signals
signal card_clicked(card_data: Dictionary)
signal card_held(card_data: Dictionary)

## Layout configuration (editable in scene editor)
@export_group("Layout")
@export var border_width: int = 4
@export var corner_radius: int = 8

## Card data
var card_data: Dictionary = {}
var catalog_data: Dictionary = {}
var draggable: bool = false

## In-deck state
var is_in_deck: bool = false

## Hold detection
var hold_timer: Timer = null
const HOLD_DURATION: float = 0.5  ## seconds

## Animation state
var hover_tween: Tween = null

## Drag state
var _hidden_for_drag: bool = false

## Node references - Card visual
@onready var card_panel: PanelContainer = %CardPanel
@onready var type_icon: TextureRect = %TypeIcon
@onready var mana_cost: Label = %ManaCost
@onready var card_name: Label = %CardName
@onready var art_placeholder: ColorRect = $CardPanel/ContentContainer/ArtContainer/ArtPlaceholder
@onready var element_badge: Panel = $CardPanel/ContentContainer/ElementBadge
@onready var in_deck_badge: PanelContainer = $CardPanel/ContentContainer/InDeckBadge

## Node references - Progression (below card)
@onready var progression_container: HBoxContainer = %ProgressionContainer
@onready var level_label: Label = %LevelLabel
@onready var xp_progress_bar: ProgressBar = %XPProgressBar

## Current element color
var element_color: Color = Color.GRAY

## Progression state
var progression_info: Dictionary = {}

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Create hold timer
	hold_timer = Timer.new()
	hold_timer.one_shot = true
	hold_timer.timeout.connect(_on_hold_timeout)
	add_child(hold_timer)

	# Make CardPanel pass mouse events to parent (us) for drag handling
	# but we still connect to its signals for hover effects
	if card_panel:
		card_panel.mouse_filter = Control.MOUSE_FILTER_PASS
		card_panel.mouse_entered.connect(_on_mouse_entered)
		card_panel.mouse_exited.connect(_on_mouse_exited)

	# Connect our own mouse signals
	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)

	# Set initial theme
	_update_theme()

func _exit_tree() -> void:
	# Kill any active tweens to prevent lambda capture errors
	if hover_tween and hover_tween.is_valid():
		hover_tween.kill()

## =============================================================================
## PUBLIC API
## =============================================================================

## Set card data and display
func set_card(p_card_data: Dictionary, p_catalog_data: Dictionary) -> void:
	card_data = p_card_data
	catalog_data = p_catalog_data
	_update_display()

## Enable/disable drag support
func set_draggable(p_draggable: bool) -> void:
	draggable = p_draggable

## Set whether this card is currently in the active deck
func set_in_deck(in_deck: bool) -> void:
	is_in_deck = in_deck
	_update_in_deck_visual()

## Set card progression data (level, XP, etc.)
func set_progression(info: Dictionary) -> void:
	progression_info = info
	_update_progression_display()

## =============================================================================
## DISPLAY UPDATE
## =============================================================================

func _update_display() -> void:
	if catalog_data.is_empty():
		return

	# Set card name
	if card_name:
		card_name.text = catalog_data.get("card_name", "Unknown")

	# Set mana cost
	if mana_cost:
		mana_cost.text = str(catalog_data.get("mana_cost", 0))

	# Set type icon based on card type and unit type
	if type_icon:
		var icon_path: String = CardVisualHelper.get_card_type_icon_path(catalog_data)
		if not icon_path.is_empty():
			type_icon.texture = load(icon_path)
			type_icon.visible = true
		else:
			type_icon.visible = false

	# Update element border
	_update_theme()

func _update_theme() -> void:
	if catalog_data.is_empty():
		return

	# Get element-based color
	element_color = CardVisualHelper.get_card_element_color(catalog_data)

	# Create theme with element-colored border for the card panel
	if card_panel:
		var style: StyleBoxFlat = StyleBoxFlat.new()
		style.bg_color = GameColorPalette.UI_BG_DARK  # Dark background
		style.border_width_left = border_width
		style.border_width_top = border_width
		style.border_width_right = border_width
		style.border_width_bottom = border_width
		style.border_color = element_color
		style.set_corner_radius_all(corner_radius)
		style.anti_aliasing = true
		style.anti_aliasing_size = 1
		card_panel.add_theme_stylebox_override("panel", style)

	# Color the art placeholder with darkened element color
	if art_placeholder:
		art_placeholder.color = element_color.darkened(0.4)

	# Style the element badge with element color (use large radius for circular look)
	if element_badge:
		var badge_style: StyleBoxFlat = StyleBoxFlat.new()
		badge_style.bg_color = element_color
		badge_style.set_corner_radius_all(100)  # Large value to make it circular
		badge_style.anti_aliasing = true
		badge_style.anti_aliasing_size = 1
		element_badge.add_theme_stylebox_override("panel", badge_style)

func _update_in_deck_visual() -> void:
	if in_deck_badge:
		in_deck_badge.visible = is_in_deck

	# Dim the card when it's in the deck
	if is_in_deck:
		modulate.a = 0.5
	else:
		modulate.a = 1.0

## =============================================================================
## PROGRESSION DISPLAY
## =============================================================================

func _update_progression_display() -> void:
	print("CardWidget: _update_progression_display called, info=%s, container=%s" % [progression_info, progression_container])
	if progression_info.is_empty():
		# Hide progression container if no data
		if progression_container:
			progression_container.visible = false
		return

	var level: int = progression_info.get("level", 1)
	var xp_progress: float = progression_info.get("xp_progress", 0.0)
	var can_level_up: bool = progression_info.get("can_level_up", false)
	print("CardWidget: level=%d, xp_progress=%f, can_level_up=%s" % [level, xp_progress, can_level_up])

	# Show progression container
	if progression_container:
		progression_container.visible = true
		print("CardWidget: Set progression_container visible=true")

	# Update level label
	if level_label:
		level_label.text = "Lv.%d" % level

	# Update XP bar
	if xp_progress_bar:
		xp_progress_bar.value = xp_progress * 100.0

		# Apply appropriate bar style
		if can_level_up:
			_apply_level_up_bar_style()
		else:
			_apply_normal_bar_style()

func _apply_normal_bar_style() -> void:
	if not xp_progress_bar:
		return

	# Background/track style
	var bg_style: StyleBoxFlat = StyleBoxFlat.new()
	bg_style.bg_color = Color(0.15, 0.15, 0.18)
	bg_style.set_corner_radius_all(3)
	xp_progress_bar.add_theme_stylebox_override("background", bg_style)

	# Fill style - use element color
	var fill_style: StyleBoxFlat = StyleBoxFlat.new()
	fill_style.bg_color = element_color.darkened(0.1)
	fill_style.set_corner_radius_all(3)
	xp_progress_bar.add_theme_stylebox_override("fill", fill_style)

func _apply_level_up_bar_style() -> void:
	if not xp_progress_bar:
		return

	# Background/track style
	var bg_style: StyleBoxFlat = StyleBoxFlat.new()
	bg_style.bg_color = Color(0.15, 0.15, 0.18)
	bg_style.set_corner_radius_all(3)
	xp_progress_bar.add_theme_stylebox_override("background", bg_style)

	# Fill style - bright green/gold for level-up ready
	var fill_style: StyleBoxFlat = StyleBoxFlat.new()
	fill_style.bg_color = Color(0.2, 0.9, 0.3)  # Bright green
	fill_style.set_corner_radius_all(3)
	xp_progress_bar.add_theme_stylebox_override("fill", fill_style)

## =============================================================================
## MOUSE INTERACTION
## =============================================================================

func _on_mouse_entered() -> void:
	# Slight scale up on hover
	if hover_tween and hover_tween.is_valid():
		hover_tween.kill()
	hover_tween = create_tween()
	hover_tween.tween_property(self, "scale", Vector2(1.05, 1.05), 0.1)

func _on_mouse_exited() -> void:
	# Scale back to normal
	if hover_tween and hover_tween.is_valid():
		hover_tween.kill()
	hover_tween = create_tween()
	hover_tween.tween_property(self, "scale", Vector2(1.0, 1.0), 0.1)

	# Cancel hold if mouse leaves
	if hold_timer and hold_timer.time_left > 0:
		hold_timer.stop()

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event as InputEventMouseButton
		if mouse_event.button_index == MOUSE_BUTTON_LEFT:
			if mouse_event.pressed:
				# Mouse button pressed - start hold timer
				if hold_timer:
					hold_timer.start(HOLD_DURATION)
			else:
				# Mouse button released
				if hold_timer and hold_timer.time_left > 0:
					# Released before hold duration - it's a quick click
					hold_timer.stop()
					card_clicked.emit(card_data)
				# If timer expired, card_held was already emitted

func _on_hold_timeout() -> void:
	# Hold duration reached - emit held signal
	card_held.emit(card_data)
	# print("CardWidget: Card held")

## =============================================================================
## DRAG AND DROP
## =============================================================================

func _get_drag_data(at_position: Vector2) -> Variant:
	if not draggable or card_data.is_empty():
		return null

	# Don't allow dragging cards that are already in deck (from collection view)
	if is_in_deck:
		return null

	# Make this card invisible while dragging (but keep space in grid)
	modulate.a = 0.0
	_hidden_for_drag = true

	# Create physics-enabled drag preview
	# Pass at_position so the card appears held where clicked
	var preview: DraggableCardPreview = DraggableCardPreview.new()
	preview.setup(card_data, catalog_data, size, at_position)

	set_drag_preview(preview)

	# Return card data as drag data
	var drag_data: Dictionary = {
		"type": "card",
		"card_data": card_data,
		"catalog_data": catalog_data,
		"source": self
	}
	return drag_data


func _notification(what: int) -> void:
	if what == NOTIFICATION_DRAG_END:
		# Drag ended (successful or cancelled) - show card again
		# If successfully added to deck, collection refresh will remove it
		if _hidden_for_drag:
			modulate.a = 1.0
			_hidden_for_drag = false
