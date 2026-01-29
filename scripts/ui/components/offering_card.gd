extends PanelContainer
class_name OfferingCard

## OfferingCard - Reusable component for displaying shop offerings
##
## Displays offering name, type, price, and allows selection for detail view

## Node references
@onready var offering_name_label: Label = %OfferingName
@onready var type_label: Label = %TypeLabel
@onready var price_label: Label = %PriceLabel

## State
var offering: ShopOffering = null

## Signals
signal card_clicked()

func _ready() -> void:
	# Add hover effect
	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			card_clicked.emit()

## Set offering data
func set_offering(new_offering: ShopOffering) -> void:
	offering = new_offering

	if not offering:
		return

	# Update labels
	offering_name_label.text = offering.display_name

	# Type label
	match offering.offering_type:
		ShopOffering.OfferingType.CARD:
			type_label.text = Loc.t("ui.offering.type_card")
		ShopOffering.OfferingType.CARD_PACK:
			type_label.text = Loc.t("ui.offering.type_card_pack")
		ShopOffering.OfferingType.CURRENCY:
			type_label.text = Loc.t("ui.offering.type_currency")
		ShopOffering.OfferingType.SPECIAL:
			type_label.text = Loc.t("ui.offering.type_special")
		_:
			type_label.text = Loc.t("ui.offering.type_card")

	# Price
	price_label.text = Loc.t("ui.offering.price_format", {"price": offering.base_price})

## =============================================================================
## SIGNAL HANDLERS
## =============================================================================

func _on_mouse_entered() -> void:
	# Hover effect: slightly brighten panel
	modulate = Color(1.1, 1.1, 1.1)

func _on_mouse_exited() -> void:
	# Reset to normal
	modulate = Color.WHITE
