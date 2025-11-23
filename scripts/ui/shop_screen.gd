extends Control
class_name ShopScreen

## ShopScreen - UI for general shop purchases
##
## Displays offerings from ShopService and handles purchase flow

## Node references
@onready var back_button: Button = %BackButton
@onready var gold_label: Label = %GoldLabel
@onready var offering_grid: GridContainer = %OfferingGrid
@onready var detail_panel: PanelContainer = %DetailPanel
@onready var offering_name_label: Label = %OfferingNameLabel
@onready var price_label: Label = %PriceLabel
@onready var description_label: Label = %DescriptionLabel
@onready var contents_label: Label = %ContentsLabel
@onready var purchase_button: Button = %PurchaseButton
@onready var purchase_popup: AcceptDialog = %PurchasePopup
@onready var error_popup: AcceptDialog = %ErrorPopup

## Offering card scene
const OFFERING_CARD_SCENE: PackedScene = preload("res://scenes/ui/offering_card.tscn")

## State
var current_offerings: Array[ShopOffering] = []
var selected_offering: ShopOffering = null
var shop_id: String = "general"

func _ready() -> void:
	# Connect buttons
	back_button.pressed.connect(_on_back_pressed)
	purchase_button.pressed.connect(_on_purchase_pressed)

	# Connect shop signals
	Shop.purchase_completed.connect(_on_purchase_completed)
	Shop.purchase_failed.connect(_on_purchase_failed)

	# Connect profile signals for gold updates
	ProfileRepo.resources_updated.connect(_on_resources_updated)

	# Initialize display
	_update_gold_display()
	_load_offerings()
	_clear_detail_panel()

func _exit_tree() -> void:
	# Disconnect signals to prevent errors
	if Shop.purchase_completed.is_connected(_on_purchase_completed):
		Shop.purchase_completed.disconnect(_on_purchase_completed)
	if Shop.purchase_failed.is_connected(_on_purchase_failed):
		Shop.purchase_failed.disconnect(_on_purchase_failed)
	if ProfileRepo.resources_updated.is_connected(_on_resources_updated):
		ProfileRepo.resources_updated.disconnect(_on_resources_updated)

## =============================================================================
## INITIALIZATION
## =============================================================================

func _load_offerings() -> void:
	# Clear existing offering cards
	for child: Node in offering_grid.get_children():
		child.queue_free()

	# Load offerings from ShopService
	current_offerings = Shop.get_shop_offerings(shop_id)

	# Create offering cards
	for offering: ShopOffering in current_offerings:
		var offering_card: OfferingCard = OFFERING_CARD_SCENE.instantiate()
		offering_grid.add_child(offering_card)
		offering_card.set_offering(offering)
		offering_card.card_clicked.connect(_on_offering_card_clicked.bind(offering))

func _update_gold_display() -> void:
	var resources: Dictionary = ProfileRepo.get_resources()
	var gold: int = resources.get("gold", 0)
	gold_label.text = "Gold: %d" % gold

## =============================================================================
## DETAIL PANEL
## =============================================================================

func _clear_detail_panel() -> void:
	selected_offering = null
	offering_name_label.text = "Select an offering"
	price_label.text = "Price: -"
	description_label.text = "Offering description will appear here."
	contents_label.text = "Contents:"
	purchase_button.disabled = true

func _update_detail_panel(offering: ShopOffering) -> void:
	selected_offering = offering
	offering_name_label.text = offering.display_name

	# Calculate price with current context
	var context: ShopPurchaseContext = _build_purchase_context(offering)
	var price: int = offering.get_price(context)
	price_label.text = "Price: %d gold" % price

	description_label.text = offering.description

	# Build contents text
	var contents_text: String = "Contents:\n"
	match offering.offering_type:
		ShopOffering.OfferingType.CARD:
			contents_text += "• %dx %s" % [offering.card_count, offering.display_name]
		ShopOffering.OfferingType.CARD_PACK:
			for card_data: Dictionary in offering.pack_cards:
				var catalog_id: String = card_data.get("catalog_id", "")
				var count: int = card_data.get("count", 1)
				# Look up card display name from catalog
				var card_dict: Dictionary = CardCatalog.get_card(catalog_id)
				var display_name: String = card_dict.get("name", catalog_id) if card_dict else catalog_id
				contents_text += "• %dx %s\n" % [count, display_name]
		ShopOffering.OfferingType.CURRENCY:
			contents_text += "• Currency offering"
		ShopOffering.OfferingType.SPECIAL:
			contents_text += "• Special offering"

	contents_label.text = contents_text

	# Enable/disable purchase button based on affordability
	var can_afford: bool = offering.can_purchase(context)
	purchase_button.disabled = not can_afford

## =============================================================================
## PURCHASE FLOW
## =============================================================================

func _build_purchase_context(offering: ShopOffering) -> ShopPurchaseContext:
	var context: ShopPurchaseContext = ShopPurchaseContext.new()
	var resources: Dictionary = ProfileRepo.get_resources()
	context.player_gold = resources.get("gold", 0)

	# Get refresh state
	var shop_refresh_state: Dictionary = ProfileRepo.get_shop_refresh_state(shop_id)
	var epoch_variant: Variant = shop_refresh_state.get("refresh_epoch", 0)
	context.refresh_epoch = epoch_variant if epoch_variant is int else 0

	# Get purchase count from profile
	var purchase_key: String = "%s::%s::%d" % [shop_id, offering.offering_id, context.refresh_epoch]
	var purchases: Dictionary = ProfileRepo.get_shop_purchases()
	context.purchase_count = purchases.get(purchase_key, 0)

	context.hero_affinity = ""  # TODO: Get from profile when hero system implemented

	return context

func _on_purchase_pressed() -> void:
	if not selected_offering:
		return

	# Attempt purchase
	var success: bool = Shop.purchase_offering(selected_offering.offering_id, shop_id)

	# ShopService will emit purchase_completed or purchase_failed signals
	# which are handled by _on_purchase_completed and _on_purchase_failed

## =============================================================================
## SIGNAL HANDLERS
## =============================================================================

func _on_offering_card_clicked(offering: ShopOffering) -> void:
	_update_detail_panel(offering)

func _on_purchase_completed(offering_id: String, _shop_id: String) -> void:
	# Show success popup
	purchase_popup.dialog_text = Loc.t("shop.purchased")
	purchase_popup.popup_centered()

	# Refresh detail panel (purchase count may have changed)
	if selected_offering and selected_offering.offering_id == offering_id:
		_update_detail_panel(selected_offering)

	# Gold display will update via resources_updated signal

func _on_purchase_failed(offering_id: String, reason: String) -> void:
	# Show error popup
	error_popup.dialog_text = reason
	error_popup.popup_centered()

func _on_resources_updated() -> void:
	_update_gold_display()

	# Update detail panel if an offering is selected (affordability may have changed)
	if selected_offering:
		_update_detail_panel(selected_offering)

func _on_back_pressed() -> void:
	SceneManager.change_scene(SceneManager.SCENE_MAIN_MENU)
