extends PanelContainer
class_name PremiumStoreOfferingItem

## PremiumStoreOfferingItem - List item for Premium Store offerings
##
## Displays offering name, type indicator, price, and owned status

## Node references
@onready var name_label: Label = %NameLabel
@onready var type_label: Label = %TypeLabel
@onready var price_label: Label = %PriceLabel
@onready var owned_indicator: Label = %OwnedIndicator

## State
var offering: ShopOffering = null

## Signals
signal item_clicked()

func _ready() -> void:
	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			item_clicked.emit()

## Set offering data
func set_offering(new_offering: ShopOffering) -> void:
	offering = new_offering

	if not offering:
		return

	# Wait for nodes to be ready if needed
	if not is_node_ready():
		await ready

	# Update labels
	name_label.text = offering.display_name

	# Type indicator
	match offering.offering_type:
		ShopOffering.OfferingType.SUMMONER:
			type_label.text = Loc.t("ui.premium_store.type_summoner")
		ShopOffering.OfferingType.COSMETIC:
			type_label.text = Loc.t("ui.premium_store.type_cosmetic")
		ShopOffering.OfferingType.EMOTE:
			type_label.text = Loc.t("ui.premium_store.type_emote")
		_:
			type_label.text = ""

	# Check if owned
	var is_owned: bool = Shop.is_offering_owned(offering)

	if is_owned:
		price_label.visible = false
		owned_indicator.visible = true
		owned_indicator.text = Loc.t("shop.button.owned")
	else:
		price_label.visible = true
		price_label.text = Loc.t("ui.offering.price_format", {"price": offering.base_price})
		owned_indicator.visible = false

## =============================================================================
## HOVER EFFECTS
## =============================================================================

func _on_mouse_entered() -> void:
	modulate = Color(1.1, 1.1, 1.1)

func _on_mouse_exited() -> void:
	modulate = Color.WHITE
