extends Control
class_name PremiumStoreScreen

## PremiumStoreScreen - Unified store UI for account-level premium purchases
##
## Displays all offerings in a single scrollable view with sections:
## - Featured (spotlight item at top)
## - Summoners
## - Cosmetics
## - Emotes
##
## Accessed from campaign map as meta-progression store

## Node references
@onready var close_button: Button = %CloseButton
@onready var title_label: Label = %TitleLabel
@onready var currency_label: Label = %CurrencyLabel

@onready var sections_scroll: ScrollContainer = %SectionsScroll
@onready var featured_items: HFlowContainer = %FeaturedItems
@onready var summoner_items: HFlowContainer = %SummonerItems
@onready var cosmetic_items: HFlowContainer = %CosmeticItems
@onready var emote_items: HFlowContainer = %EmoteItems

## Popup references
@onready var detail_popup: Control = %DetailPopup
@onready var popup_close_button: Button = %PopupCloseButton
@onready var popup_name_label: Label = %PopupNameLabel
@onready var popup_description_label: Label = %PopupDescriptionLabel
@onready var popup_info_container: VBoxContainer = %PopupInfoContainer
@onready var popup_price_label: Label = %PopupPriceLabel
@onready var popup_owned_label: Label = %PopupOwnedLabel
@onready var popup_purchase_button: Button = %PopupPurchaseButton

## State
var selected_offering: Dictionary = {}

## Offering item scene
const OFFERING_ITEM_SCENE: PackedScene = preload("res://scenes/meta/components/premium_store_offering_item.tscn")

func _ready() -> void:
	# Connect buttons
	close_button.pressed.connect(_on_close_pressed)
	popup_close_button.pressed.connect(_on_popup_close_pressed)
	popup_purchase_button.pressed.connect(_on_purchase_pressed)

	# Connect overlay click to close popup
	var overlay: ColorRect = detail_popup.get_node("Overlay")
	overlay.gui_input.connect(_on_overlay_input)

	# Connect shop signals
	Shop.connect("PurchaseCompleted", _on_purchase_completed)
	Shop.connect("PurchaseFailed", _on_purchase_failed)

	# Connect profile signals for gold updates
	ProfileRepo.connect("DataChangedGodot", _on_data_changed)

	# Initialize display
	_update_currency_display()
	_populate_sections()

func _exit_tree() -> void:
	# Disconnect signals
	if Shop.is_connected("PurchaseCompleted", _on_purchase_completed):
		Shop.disconnect("PurchaseCompleted", _on_purchase_completed)
	if Shop.is_connected("PurchaseFailed", _on_purchase_failed):
		Shop.disconnect("PurchaseFailed", _on_purchase_failed)
	if ProfileRepo.is_connected("DataChangedGodot", _on_data_changed):
		ProfileRepo.disconnect("DataChangedGodot", _on_data_changed)

## =============================================================================
## SECTION POPULATION
## =============================================================================

func _populate_sections() -> void:
	# Clear all sections
	_clear_children(featured_items)
	_clear_children(summoner_items)
	_clear_children(cosmetic_items)
	_clear_children(emote_items)

	# Get all offerings from premium store
	var all_offerings: Array = ShopApi.get_shop_offerings("premium_store")

	# Track first unowned summoner for featured section
	var featured_offering: Dictionary = {}

	# Sort offerings into sections with appropriate sizes
	for offering: Dictionary in all_offerings:
		var offering_type_name: String = offering.get("offering_type_name", "")
		match offering_type_name:
			"summoner":
				# Check if this should be featured (first unowned summoner)
				if featured_offering.is_empty() and not ShopApi.is_offering_owned(offering.get("offering_id", ""), "premium_store"):
					featured_offering = offering
				_add_offering_item(summoner_items, offering, PremiumStoreOfferingItem.CardSize.LARGE)
			"cosmetic":
				_add_offering_item(cosmetic_items, offering, PremiumStoreOfferingItem.CardSize.MEDIUM)
			"emote":
				_add_offering_item(emote_items, offering, PremiumStoreOfferingItem.CardSize.SMALL)

	# Populate featured section with large featured card
	if not featured_offering.is_empty():
		_add_offering_item(featured_items, featured_offering, PremiumStoreOfferingItem.CardSize.FEATURED)
	else:
		# No unowned summoners - show completion message
		var complete_label: Label = Label.new()
		complete_label.text = Loc.t("ui.premium_store.all_summoners_unlocked")
		complete_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		complete_label.add_theme_color_override("font_color", Color(0.5, 0.8, 0.5))
		featured_items.add_child(complete_label)

func _add_offering_item(container: HFlowContainer, offering: Dictionary, size: PremiumStoreOfferingItem.CardSize = PremiumStoreOfferingItem.CardSize.MEDIUM) -> void:
	var item: PremiumStoreOfferingItem = OFFERING_ITEM_SCENE.instantiate()
	item.set_card_size(size)
	container.add_child(item)
	item.set_offering(offering)
	item.item_clicked.connect(_on_offering_item_clicked.bind(offering))

func _clear_children(container: Node) -> void:
	for child: Node in container.get_children():
		child.queue_free()

## =============================================================================
## POPUP MODAL
## =============================================================================

func _show_popup(offering: Dictionary) -> void:
	selected_offering = offering
	popup_name_label.text = offering.get("display_name", "")
	popup_description_label.text = offering.get("description", "")

	# Check if already owned
	var is_owned: bool = ShopApi.is_offering_owned(offering.get("offering_id", ""), "premium_store")

	if is_owned:
		popup_price_label.visible = false
		popup_purchase_button.visible = false
		popup_owned_label.visible = true
		popup_owned_label.text = Loc.t("shop.button.owned")
	else:
		# Show price and purchase button (premium store uses mana stones/gems)
		var price: int = offering.get("base_price", 0)
		popup_price_label.text = Loc.t("ui.shop.mana_stones_price", {"amount": price})
		popup_price_label.visible = true
		popup_purchase_button.visible = true
		popup_owned_label.visible = false

		# Enable/disable purchase button based on affordability (gems = mana stones)
		var resources: Dictionary = ProfileRepoApi.get_resources_dict()
		var gems: int = resources.get("gems", 0)
		popup_purchase_button.disabled = (gems < price)

	# Add extra info based on offering type
	_populate_popup_info(offering)

	# Show popup
	detail_popup.visible = true

func _hide_popup() -> void:
	detail_popup.visible = false
	selected_offering = {}

func _populate_popup_info(offering: Dictionary) -> void:
	# Clear existing info
	for child: Node in popup_info_container.get_children():
		child.queue_free()

	var offering_type_name: String = offering.get("offering_type_name", "")
	match offering_type_name:
		"summoner":
			_add_summoner_info(offering)
		"cosmetic":
			_add_cosmetic_info(offering)
		"emote":
			_add_emote_info(offering)

func _add_summoner_info(offering: Dictionary) -> void:
	# Look up summoner config
	var config: SummonerConfig = SummonerConfig.from_dict(SummonerCatalogApi.get_summoner(offering.get("summoner_id", "")))
	if not config:
		return

	# Element
	var element_label: Label = Label.new()
	var element_name: String = ElementTypes.get_display_name(config.get_element())
	element_label.text = Loc.t("ui.premium_store.element", {"element": element_name})
	element_label.add_theme_font_size_override("font_size", 16)
	popup_info_container.add_child(element_label)

	# Base stats
	var stats_label: Label = Label.new()
	stats_label.text = Loc.t("ui.premium_store.base_stats", {"hp": config.base_health, "mana": config.max_mana})
	stats_label.add_theme_font_size_override("font_size", 14)
	popup_info_container.add_child(stats_label)

	# Innate traits
	if config.innate_trait_ids.size() > 0:
		var traits_header: Label = Label.new()
		traits_header.text = Loc.t("ui.premium_store.innate_traits")
		traits_header.add_theme_font_size_override("font_size", 14)
		popup_info_container.add_child(traits_header)

		for trait_id: String in config.innate_trait_ids:
			var trait_label: Label = Label.new()
			trait_label.text = "  - " + TraitCatalogApi.get_trait_name(trait_id)
			trait_label.add_theme_font_size_override("font_size", 12)
			popup_info_container.add_child(trait_label)

func _add_cosmetic_info(offering: Dictionary) -> void:
	var type_label: Label = Label.new()
	type_label.text = Loc.t("ui.premium_store.cosmetic_type", {"type": offering.get("cosmetic_type", "").capitalize()})
	type_label.add_theme_font_size_override("font_size", 14)
	popup_info_container.add_child(type_label)

func _add_emote_info(offering: Dictionary) -> void:
	var info_label: Label = Label.new()
	info_label.text = Loc.t("ui.premium_store.emote_info")
	info_label.add_theme_font_size_override("font_size", 14)
	popup_info_container.add_child(info_label)

## =============================================================================
## DISPLAY UPDATES
## =============================================================================

func _update_currency_display() -> void:
	var resources: Dictionary = ProfileRepoApi.get_resources_dict()
	var gems: int = resources.get("gems", 0)
	currency_label.text = Loc.t("ui.shop.mana_stones_label", {"amount": gems})

## =============================================================================
## SIGNAL HANDLERS
## =============================================================================

func _on_close_pressed() -> void:
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

func _on_offering_item_clicked(offering: Dictionary) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_show_popup(offering)

func _on_popup_close_pressed() -> void:
	_hide_popup()

func _on_overlay_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			_hide_popup()

func _on_purchase_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if selected_offering.is_empty():
		return

	var _success: bool = ShopApi.purchase_offering(selected_offering.get("offering_id", ""), "premium_store")
	# Result handled by PurchaseCompleted/PurchaseFailed signals

func _on_purchase_completed(offering_id: String, shop_id: String) -> void:
	if shop_id != "premium_store":
		return

	# Refresh all sections
	_populate_sections()

	# Update popup if the purchased offering is still selected
	if not selected_offering.is_empty() and selected_offering.get("offering_id", "") == offering_id:
		_show_popup(selected_offering)

func _on_purchase_failed(_offering_id: String, reason: String) -> void:
	# TODO: Add visual feedback for purchase failures (toast notification or modal)
	push_warning("PremiumStoreScreen: Purchase failed - %s" % reason)

func _on_data_changed() -> void:
	_update_currency_display()

	# Update popup if visible (affordability may have changed)
	if detail_popup.visible and not selected_offering.is_empty():
		_show_popup(selected_offering)
