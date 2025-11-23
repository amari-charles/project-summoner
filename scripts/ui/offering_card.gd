extends PanelContainer
class_name OfferingCard

## OfferingCard - Reusable component for displaying shop offerings
##
## Displays offering name, type, price, and allows selection for detail view

## Node references
@onready var offering_name_label: Label = %OfferingName
@onready var type_label: Label = %TypeLabel
@onready var price_label: Label = %PriceLabel
@onready var select_button: Button = %SelectButton

## State
var offering: ShopOffering = null

## Signals
signal card_clicked()

func _ready() -> void:
	select_button.pressed.connect(_on_select_button_pressed)

	# Add hover effect
	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)

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
			type_label.text = "Type: Card"
		ShopOffering.OfferingType.CARD_PACK:
			type_label.text = "Type: Card Pack"
		ShopOffering.OfferingType.CURRENCY:
			type_label.text = "Type: Currency"
		ShopOffering.OfferingType.SPECIAL:
			type_label.text = "Type: Special"

	# Price (base price, context-dependent pricing handled by detail panel)
	price_label.text = "Price: %d gold" % offering.base_price

## =============================================================================
## SIGNAL HANDLERS
## =============================================================================

func _on_select_button_pressed() -> void:
	card_clicked.emit()

func _on_mouse_entered() -> void:
	# Hover effect: slightly brighten panel
	modulate = Color(1.1, 1.1, 1.1)

func _on_mouse_exited() -> void:
	# Reset to normal
	modulate = Color.WHITE
