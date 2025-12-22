extends PanelContainer
class_name PremiumStoreOfferingItem

## PremiumStoreOfferingItem - Visual card for Premium Store offerings
##
## Displays offering with colored preview area, name, and price
## Designed for grid layout display

## Node references
@onready var preview_area: ColorRect = %PreviewArea
@onready var name_label: Label = %NameLabel
@onready var price_label: Label = %PriceLabel
@onready var owned_overlay: ColorRect = %OwnedOverlay

## State
var offering: ShopOffering = null

## Signals
signal item_clicked()

## Element colors for summoner previews
const ELEMENT_COLORS: Dictionary = {
	"fire": Color(0.9, 0.3, 0.2),
	"water": Color(0.2, 0.5, 0.9),
	"wind": Color(0.5, 0.8, 0.5),
	"earth": Color(0.6, 0.5, 0.3),
	"lightning": Color(0.9, 0.8, 0.2),
	"shadow": Color(0.3, 0.2, 0.4),
	"life": Color(0.3, 0.8, 0.4),
	"death": Color(0.4, 0.2, 0.5),
}

## Category colors
const COSMETIC_COLOR: Color = Color(0.6, 0.4, 0.7)
const EMOTE_COLOR: Color = Color(0.8, 0.6, 0.3)
const DEFAULT_COLOR: Color = Color(0.3, 0.3, 0.4)

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

	# Update name
	name_label.text = offering.display_name

	# Set preview color based on offering type
	_update_preview_color()

	# Check if owned
	var is_owned: bool = Shop.is_offering_owned(offering)

	if is_owned:
		price_label.text = ""
		owned_overlay.visible = true
	else:
		price_label.text = "%dg" % offering.base_price
		owned_overlay.visible = false

func _update_preview_color() -> void:
	match offering.offering_type:
		ShopOffering.OfferingType.SUMMONER:
			# Get element color for summoner
			var config: SummonerConfig = SummonerCatalog.get_summoner_config(offering.summoner_id)
			if config:
				var element: ElementTypes.Element = config.get_element()
				var element_key: String = element.id.to_lower() if element else "neutral"
				preview_area.color = ELEMENT_COLORS.get(element_key, DEFAULT_COLOR)
			else:
				preview_area.color = DEFAULT_COLOR
		ShopOffering.OfferingType.COSMETIC:
			preview_area.color = COSMETIC_COLOR
		ShopOffering.OfferingType.EMOTE:
			preview_area.color = EMOTE_COLOR
		_:
			preview_area.color = DEFAULT_COLOR

## =============================================================================
## HOVER EFFECTS
## =============================================================================

func _on_mouse_entered() -> void:
	modulate = Color(1.15, 1.15, 1.15)

func _on_mouse_exited() -> void:
	modulate = Color.WHITE
