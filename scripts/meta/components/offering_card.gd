extends PanelContainer
class_name OfferingCard

## OfferingCard - Reusable component for displaying shop offerings
##
## Displays offering name, type, price, and allows selection for detail view

## Node references
@onready var offering_name_label: Label = %OfferingName
@onready var type_label: Label = %TypeLabel
@onready var price_label: Label = %PriceLabel

const COLOR_CARD: Color = Color(0.11, 0.085, 0.13, 0.96)
const COLOR_CARD_HOVER: Color = Color(0.16, 0.11, 0.18, 0.98)
const COLOR_CARD_SELECTED: Color = Color(0.20, 0.13, 0.22, 1.0)
const COLOR_BORDER: Color = Color(0.56, 0.42, 0.22, 0.95)
const COLOR_BORDER_SELECTED: Color = Color(0.95, 0.68, 0.24, 1.0)

## State
var offering: Dictionary = {}
var _selected: bool = false

## Signals
signal card_clicked()

func _ready() -> void:
	size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
	size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	_apply_style(COLOR_CARD, COLOR_BORDER, 1)
	# Add hover effect
	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			card_clicked.emit()

## Set offering data
func set_offering(new_offering: Dictionary) -> void:
	offering = new_offering

	if offering.is_empty():
		return

	# Update labels
	offering_name_label.text = offering.get("display_name", "")

	# Type label
	var offering_type_name: String = offering.get("offering_type_name", "")
	match offering_type_name:
		"card":
			type_label.text = Loc.t("ui.offering.type_card")
		"card_pack":
			type_label.text = Loc.t("ui.offering.type_card_pack")
		"currency":
			type_label.text = Loc.t("ui.offering.type_currency")
		"special":
			type_label.text = Loc.t("ui.offering.type_special")
		_:
			type_label.text = Loc.t("ui.offering.type_card")

	# Price
	price_label.text = Loc.t("ui.offering.price_format", {"price": offering.get("base_price", 0)})

func set_selected(is_selected: bool) -> void:
	_selected = is_selected
	if _selected:
		_apply_style(COLOR_CARD_SELECTED, COLOR_BORDER_SELECTED, 2)
	else:
		_apply_style(COLOR_CARD, COLOR_BORDER, 1)

func _apply_style(bg: Color, border: Color, border_width: int) -> void:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = bg
	style.border_color = border
	style.set_border_width_all(border_width)
	style.set_corner_radius_all(7)
	style.content_margin_left = 0
	style.content_margin_top = 0
	style.content_margin_right = 0
	style.content_margin_bottom = 0
	add_theme_stylebox_override("panel", style)

## =============================================================================
## SIGNAL HANDLERS
## =============================================================================

func _on_mouse_entered() -> void:
	if not _selected:
		_apply_style(COLOR_CARD_HOVER, COLOR_BORDER_SELECTED, 1)

func _on_mouse_exited() -> void:
	if not _selected:
		_apply_style(COLOR_CARD, COLOR_BORDER, 1)
