extends PanelContainer
class_name CardWidget

## CardWidget - Reusable Card Display Component
##
## Displays a card thumbnail with name, cost, type, and rarity.
## Supports drag-and-drop for deck building.
## Can show count badge for collection view.

## Signals
signal card_clicked(card_data: Dictionary)
signal card_held(card_data: Dictionary)

## Layout configuration (editable in scene editor)
@export_group("Layout")
@export var border_width: int = 3
@export var corner_radius: int = 6
@export var element_badge_radius: int = 9

## Card data
var card_data: Dictionary = {}
var catalog_data: Dictionary = {}
var draggable: bool = false

## Hold detection
var hold_timer: Timer = null
const HOLD_DURATION = 0.5  # seconds

## Node references
@onready var type_icon: TextureRect = %TypeIcon
@onready var mana_cost: Label = %ManaCost
@onready var card_name: Label = %CardName
@onready var art_placeholder: ColorRect = $ContentContainer/ArtContainer/ArtPlaceholder
@onready var element_badge: Panel = $ContentContainer/ElementBadge

## Current element color
var element_color: Color = Color.GRAY

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Create hold timer
	hold_timer = Timer.new()
	hold_timer.one_shot = true
	hold_timer.timeout.connect(_on_hold_timeout)
	add_child(hold_timer)

	# Connect mouse signals
	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)

	# Set initial theme
	_update_theme()

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
		var icon_path = CardVisualHelper.get_card_type_icon_path(catalog_data)
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

	# Create theme with element-colored border
	var style = StyleBoxFlat.new()
	style.bg_color = GameColorPalette.UI_BG_DARK  # Dark background
	style.border_width_left = border_width
	style.border_width_top = border_width
	style.border_width_right = border_width
	style.border_width_bottom = border_width
	style.border_color = element_color
	style.set_corner_radius_all(corner_radius)
	style.anti_aliasing = true
	style.anti_aliasing_size = 1

	add_theme_stylebox_override("panel", style)

	# Color the art placeholder with darkened element color
	if art_placeholder:
		art_placeholder.color = element_color.darkened(0.4)

	# Style the element badge with element color
	if element_badge:
		var badge_style = StyleBoxFlat.new()
		badge_style.bg_color = element_color
		badge_style.set_corner_radius_all(element_badge_radius)
		badge_style.anti_aliasing = true
		badge_style.anti_aliasing_size = 1
		element_badge.add_theme_stylebox_override("panel", badge_style)

## =============================================================================
## MOUSE INTERACTION
## =============================================================================

func _on_mouse_entered() -> void:
	# Slight scale up on hover
	var tween = create_tween()
	tween.tween_property(self, "scale", Vector2(1.05, 1.05), 0.1)

func _on_mouse_exited() -> void:
	# Scale back to normal
	var tween = create_tween()
	tween.tween_property(self, "scale", Vector2(1.0, 1.0), 0.1)

	# Cancel hold if mouse leaves
	if hold_timer and hold_timer.time_left > 0:
		hold_timer.stop()

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
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

func _get_drag_data(_at_position: Vector2) -> Variant:
	if not draggable or card_data.is_empty():
		return null

	# Create drag preview
	var preview = PanelContainer.new()
	preview.custom_minimum_size = custom_minimum_size

	var label = Label.new()
	label.text = catalog_data.get("card_name", "Card")
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	preview.add_child(label)

	# Make preview semi-transparent
	preview.modulate = Color(1, 1, 1, 0.7)

	set_drag_preview(preview)

	# Return card data as drag data
	return {
		"type": "card",
		"card_data": card_data,
		"catalog_data": catalog_data,
		"source": self
	}
