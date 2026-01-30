extends Control
class_name CaravanScreen

## CaravanScreen - UI for in-campaign caravan shops
##
## Displays offerings and handles purchase flow for caravan events

## Node references
@onready var leave_button: Button = %LeaveButton
@onready var title_label: Label = $MarginContainer/VBoxContainer/Header/Title
@onready var gold_label: Label = %GoldLabel
@onready var offering_grid: GridContainer = %OfferingGrid
@onready var detail_panel: PanelContainer = %DetailPanel
@onready var offering_name_label: Label = %OfferingNameLabel
@onready var price_label: Label = %PriceLabel
@onready var description_label: Label = %DescriptionLabel
@onready var purchase_button: Button = %PurchaseButton
@onready var purchase_popup: AcceptDialog = %PurchasePopup
@onready var error_popup: AcceptDialog = %ErrorPopup

## Offering card scene
const OFFERING_CARD_SCENE: PackedScene = preload("res://scenes/ui/components/offering_card.tscn")

## State
var current_offerings: Array[ShopOffering] = []
var selected_offering: ShopOffering = null
var shop_id: String = ""
var has_purchased: bool = false

func _ready() -> void:
	# Set localized text
	title_label.text = Loc.t("shop.caravan.title")
	leave_button.text = Loc.t("shop.caravan.leave_button")
	purchase_button.text = Loc.t("shop.caravan.purchase_button")

	# Connect buttons
	leave_button.pressed.connect(_on_leave_pressed)
	purchase_button.pressed.connect(_on_purchase_pressed)

	# Connect shop signals
	Shop.purchase_completed.connect(_on_purchase_completed)
	Shop.purchase_failed.connect(_on_purchase_failed)

	# Connect economy signals for campaign gold updates
	Economy.campaign_gold_changed.connect(_on_campaign_gold_changed)

	# Get shop_id from EventContext
	var event_id: String = EventContext.get_current_event_id()
	if not event_id.is_empty():
		var event_config: Dictionary = EventContext.get_event_config()
		shop_id = event_config.get("shop_id", "")

		if shop_id.is_empty():
			push_error("CaravanScreen: Event '%s' is missing shop_id!" % event_id)

	# Initialize display
	_update_gold_display()
	_load_offerings()
	_clear_detail_panel()

func _exit_tree() -> void:
	if Shop.purchase_completed.is_connected(_on_purchase_completed):
		Shop.purchase_completed.disconnect(_on_purchase_completed)
	if Shop.purchase_failed.is_connected(_on_purchase_failed):
		Shop.purchase_failed.disconnect(_on_purchase_failed)
	if Economy.campaign_gold_changed.is_connected(_on_campaign_gold_changed):
		Economy.campaign_gold_changed.disconnect(_on_campaign_gold_changed)

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
	var gold: int = Economy.get_campaign_gold()
	gold_label.text = Loc.t("ui.shop.gold_label", {"amount": gold})

## =============================================================================
## DETAIL PANEL
## =============================================================================

func _clear_detail_panel() -> void:
	selected_offering = null
	offering_name_label.text = Loc.t("ui.shop.select_offering")
	price_label.text = Loc.t("ui.shop.price_placeholder")
	description_label.text = Loc.t("ui.shop.description_placeholder")
	purchase_button.disabled = true

func _update_detail_panel(offering: ShopOffering) -> void:
	selected_offering = offering
	offering_name_label.text = offering.display_name

	# Calculate price with current context
	var context: ShopPurchaseContext = _build_purchase_context(offering)
	var price: int = offering.get_price(context)
	price_label.text = Loc.t("ui.shop.price_format", {"price": price})

	description_label.text = offering.description

	# Enable/disable purchase button based on affordability
	var can_afford: bool = offering.can_purchase(context)
	purchase_button.disabled = not can_afford

## =============================================================================
## PURCHASE FLOW
## =============================================================================

func _build_purchase_context(offering: ShopOffering) -> ShopPurchaseContext:
	var context: ShopPurchaseContext = ShopPurchaseContext.new()
	context.player_gold = Economy.get_campaign_gold()

	# Get refresh state
	var shop_refresh_state: Dictionary = ProfileRepo.get_shop_refresh_state(shop_id)
	var epoch_variant: Variant = shop_refresh_state.get("refresh_epoch", 0)
	context.refresh_epoch = epoch_variant if epoch_variant is int else 0

	# Get purchase count from profile
	var purchase_key: String = "%s::%s::%d" % [shop_id, offering.offering_id, context.refresh_epoch]
	var purchases: Dictionary = ProfileRepo.get_shop_purchases()
	context.purchase_count = purchases.get(purchase_key, 0)

	context.summoner_affinity = ""

	return context

func _on_purchase_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if not selected_offering:
		return

	Shop.purchase_offering(selected_offering.offering_id, shop_id)

## =============================================================================
## SIGNAL HANDLERS
## =============================================================================

func _on_offering_card_clicked(offering: ShopOffering) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_update_detail_panel(offering)

func _on_purchase_completed(_offering_id: String, _shop_id: String) -> void:
	has_purchased = true

	# Refresh the grid to remove purchased card
	_load_offerings()
	_clear_detail_panel()

	# Show success popup
	purchase_popup.title = Loc.t("shop.caravan.purchase_success_title")
	purchase_popup.dialog_text = Loc.t("shop.caravan.purchase_success_message")
	purchase_popup.popup_centered()

func _on_purchase_failed(_offering_id: String, reason: String) -> void:
	error_popup.title = Loc.t("shop.caravan.purchase_failed_title")
	error_popup.dialog_text = reason
	error_popup.popup_centered()

func _on_campaign_gold_changed(_summoner_id: String, _gold: int) -> void:
	_update_gold_display()
	if selected_offering:
		_update_detail_panel(selected_offering)

func _on_leave_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_leave_caravan()

func _leave_caravan() -> void:
	var event_id: String = EventContext.get_current_event_id()
	if not event_id.is_empty():
		EventContext.complete_event()
		EventContext.clear_event()

	if NavigationContext.has_return():
		var return_to: String = NavigationContext.pop_return()
		SceneManager.transition_to(return_to)
	else:
		SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)
