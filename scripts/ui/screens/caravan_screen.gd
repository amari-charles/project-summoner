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
var current_offerings: Array = []
var selected_offering: Dictionary = {}
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
	Shop.PurchaseCompleted.connect(_on_purchase_completed)
	Shop.PurchaseFailed.connect(_on_purchase_failed)

	# Connect economy signals for campaign gold updates
	Economy.CampaignGoldChanged.connect(_on_campaign_gold_changed)

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
	if Shop.PurchaseCompleted.is_connected(_on_purchase_completed):
		Shop.PurchaseCompleted.disconnect(_on_purchase_completed)
	if Shop.PurchaseFailed.is_connected(_on_purchase_failed):
		Shop.PurchaseFailed.disconnect(_on_purchase_failed)
	if Economy.CampaignGoldChanged.is_connected(_on_campaign_gold_changed):
		Economy.CampaignGoldChanged.disconnect(_on_campaign_gold_changed)

## =============================================================================
## INITIALIZATION
## =============================================================================

func _load_offerings() -> void:
	# Clear existing offering cards
	for child: Node in offering_grid.get_children():
		child.queue_free()

	# Load offerings from ShopService
	current_offerings = Shop.GetShopOfferings(shop_id)

	# Create offering cards
	for offering: Dictionary in current_offerings:
		var offering_card: OfferingCard = OFFERING_CARD_SCENE.instantiate()
		offering_grid.add_child(offering_card)
		offering_card.set_offering(offering)
		offering_card.card_clicked.connect(_on_offering_card_clicked.bind(offering))

func _update_gold_display() -> void:
	var gold: int = Economy.GetCampaignGold()
	gold_label.text = Loc.t("ui.shop.gold_label", {"amount": gold})

## =============================================================================
## DETAIL PANEL
## =============================================================================

func _clear_detail_panel() -> void:
	selected_offering = {}
	offering_name_label.text = Loc.t("ui.shop.select_offering")
	price_label.text = Loc.t("ui.shop.price_placeholder")
	description_label.text = Loc.t("ui.shop.description_placeholder")
	purchase_button.disabled = true

func _update_detail_panel(offering: Dictionary) -> void:
	selected_offering = offering
	offering_name_label.text = offering.get("display_name", "")

	var price: int = offering.get("base_price", 0)
	price_label.text = Loc.t("ui.shop.price_format", {"price": price})

	description_label.text = offering.get("description", "")

	# Enable/disable purchase button based on affordability
	var can_result: Dictionary = Shop.CanPurchaseOffering(offering.get("offering_id", ""), shop_id)
	purchase_button.disabled = not can_result.get("can_purchase", false)

## =============================================================================
## PURCHASE FLOW
## =============================================================================

func _on_purchase_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if selected_offering.is_empty():
		return

	Shop.PurchaseOffering(selected_offering.get("offering_id", ""), shop_id)

## =============================================================================
## SIGNAL HANDLERS
## =============================================================================

func _on_offering_card_clicked(offering: Dictionary) -> void:
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
	if not selected_offering.is_empty():
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
