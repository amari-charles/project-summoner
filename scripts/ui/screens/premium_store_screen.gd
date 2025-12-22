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
@onready var gold_label: Label = %GoldLabel

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
var selected_offering: ShopOffering = null

## Offering item scene
const OFFERING_ITEM_SCENE: PackedScene = preload("res://scenes/ui/components/premium_store_offering_item.tscn")

func _ready() -> void:
	# Connect buttons
	close_button.pressed.connect(_on_close_pressed)
	popup_close_button.pressed.connect(_on_popup_close_pressed)
	popup_purchase_button.pressed.connect(_on_purchase_pressed)

	# Connect overlay click to close popup
	var overlay: ColorRect = detail_popup.get_node("Overlay")
	overlay.gui_input.connect(_on_overlay_input)

	# Connect shop signals
	Shop.purchase_completed.connect(_on_purchase_completed)
	Shop.purchase_failed.connect(_on_purchase_failed)

	# Connect profile signals for gold updates
	ProfileRepo.data_changed.connect(_on_data_changed)

	# Initialize display
	_update_gold_display()
	_populate_sections()

func _exit_tree() -> void:
	# Disconnect signals
	if Shop.purchase_completed.is_connected(_on_purchase_completed):
		Shop.purchase_completed.disconnect(_on_purchase_completed)
	if Shop.purchase_failed.is_connected(_on_purchase_failed):
		Shop.purchase_failed.disconnect(_on_purchase_failed)
	if ProfileRepo.data_changed.is_connected(_on_data_changed):
		ProfileRepo.data_changed.disconnect(_on_data_changed)

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
	var all_offerings: Array[ShopOffering] = Shop.get_shop_offerings("premium_store")

	# Track first unowned summoner for featured section
	var featured_offering: ShopOffering = null

	# Sort offerings into sections with appropriate sizes
	for offering: ShopOffering in all_offerings:
		match offering.offering_type:
			ShopOffering.OfferingType.SUMMONER:
				# Check if this should be featured (first unowned summoner)
				if not featured_offering and not Shop.is_offering_owned(offering):
					featured_offering = offering
				_add_offering_item(summoner_items, offering, PremiumStoreOfferingItem.CardSize.LARGE)
			ShopOffering.OfferingType.COSMETIC:
				_add_offering_item(cosmetic_items, offering, PremiumStoreOfferingItem.CardSize.MEDIUM)
			ShopOffering.OfferingType.EMOTE:
				_add_offering_item(emote_items, offering, PremiumStoreOfferingItem.CardSize.SMALL)

	# Populate featured section with large featured card
	if featured_offering:
		_add_offering_item(featured_items, featured_offering, PremiumStoreOfferingItem.CardSize.FEATURED)
	else:
		# No unowned summoners - show completion message
		var complete_label: Label = Label.new()
		complete_label.text = Loc.t("ui.premium_store.all_summoners_unlocked")
		complete_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		complete_label.add_theme_color_override("font_color", Color(0.5, 0.8, 0.5))
		featured_items.add_child(complete_label)

func _add_offering_item(container: HFlowContainer, offering: ShopOffering, size: PremiumStoreOfferingItem.CardSize = PremiumStoreOfferingItem.CardSize.MEDIUM) -> void:
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

func _show_popup(offering: ShopOffering) -> void:
	selected_offering = offering
	popup_name_label.text = offering.display_name
	popup_description_label.text = offering.description

	# Check if already owned
	var is_owned: bool = Shop.is_offering_owned(offering)

	if is_owned:
		popup_price_label.visible = false
		popup_purchase_button.visible = false
		popup_owned_label.visible = true
		popup_owned_label.text = Loc.t("shop.button.owned")
	else:
		# Show price and purchase button
		var price: int = offering.base_price
		popup_price_label.text = Loc.t("ui.shop.price_format", {"price": price})
		popup_price_label.visible = true
		popup_purchase_button.visible = true
		popup_owned_label.visible = false

		# Enable/disable purchase button based on affordability
		var resources: Dictionary = ProfileRepo.get_resources()
		var gold: int = resources.get("gold", 0)
		popup_purchase_button.disabled = (gold < price)

	# Add extra info based on offering type
	_populate_popup_info(offering)

	# Show popup
	detail_popup.visible = true

func _hide_popup() -> void:
	detail_popup.visible = false
	selected_offering = null

func _populate_popup_info(offering: ShopOffering) -> void:
	# Clear existing info
	for child: Node in popup_info_container.get_children():
		child.queue_free()

	match offering.offering_type:
		ShopOffering.OfferingType.SUMMONER:
			_add_summoner_info(offering)
		ShopOffering.OfferingType.COSMETIC:
			_add_cosmetic_info(offering)
		ShopOffering.OfferingType.EMOTE:
			_add_emote_info(offering)

func _add_summoner_info(offering: ShopOffering) -> void:
	# Look up summoner config
	var config: SummonerConfig = SummonerCatalog.get_summoner_config(offering.summoner_id)
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
			trait_label.text = "  - " + trait_id.capitalize()
			trait_label.add_theme_font_size_override("font_size", 12)
			popup_info_container.add_child(trait_label)

func _add_cosmetic_info(offering: ShopOffering) -> void:
	var type_label: Label = Label.new()
	type_label.text = Loc.t("ui.premium_store.cosmetic_type", {"type": offering.cosmetic_type.capitalize()})
	type_label.add_theme_font_size_override("font_size", 14)
	popup_info_container.add_child(type_label)

func _add_emote_info(offering: ShopOffering) -> void:
	var info_label: Label = Label.new()
	info_label.text = Loc.t("ui.premium_store.emote_info")
	info_label.add_theme_font_size_override("font_size", 14)
	popup_info_container.add_child(info_label)

## =============================================================================
## DISPLAY UPDATES
## =============================================================================

func _update_gold_display() -> void:
	var resources: Dictionary = ProfileRepo.get_resources()
	var gold: int = resources.get("gold", 0)
	gold_label.text = Loc.t("ui.shop.gold_label", {"amount": gold})

## =============================================================================
## SIGNAL HANDLERS
## =============================================================================

func _on_close_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

func _on_offering_item_clicked(offering: ShopOffering) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_show_popup(offering)

func _on_popup_close_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_hide_popup()

func _on_overlay_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
			_hide_popup()

func _on_purchase_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if not selected_offering:
		return

	var _success: bool = Shop.purchase_offering(selected_offering.offering_id, "premium_store")
	# Result handled by purchase_completed/purchase_failed signals

func _on_purchase_completed(offering_id: String, shop_id: String) -> void:
	if shop_id != "premium_store":
		return

	# Refresh all sections
	_populate_sections()

	# Update popup if the purchased offering is still selected
	if selected_offering and selected_offering.offering_id == offering_id:
		_show_popup(selected_offering)

func _on_purchase_failed(_offering_id: String, reason: String) -> void:
	# Show error message (could add a popup here in the future)
	push_warning("PremiumStoreScreen: Purchase failed - %s" % reason)

func _on_data_changed() -> void:
	_update_gold_display()

	# Update popup if visible (affordability may have changed)
	if detail_popup.visible and selected_offering:
		_show_popup(selected_offering)
