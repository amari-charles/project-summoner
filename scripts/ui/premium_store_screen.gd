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
@onready var featured_items: VBoxContainer = %FeaturedItems
@onready var summoner_items: VBoxContainer = %SummonerItems
@onready var cosmetic_items: VBoxContainer = %CosmeticItems
@onready var emote_items: VBoxContainer = %EmoteItems

@onready var detail_panel: PanelContainer = %DetailPanel
@onready var detail_name_label: Label = %DetailNameLabel
@onready var detail_description_label: Label = %DetailDescriptionLabel
@onready var detail_price_label: Label = %DetailPriceLabel
@onready var detail_info_container: VBoxContainer = %DetailInfoContainer
@onready var purchase_button: Button = %PurchaseButton
@onready var owned_label: Label = %OwnedLabel

## State
var selected_offering: ShopOffering = null

## Offering item scene
const OFFERING_ITEM_SCENE: PackedScene = preload("res://scenes/ui/premium_store_offering_item.tscn")

func _ready() -> void:
	# Connect buttons
	close_button.pressed.connect(_on_close_pressed)
	purchase_button.pressed.connect(_on_purchase_pressed)

	# Connect shop signals
	Shop.purchase_completed.connect(_on_purchase_completed)
	Shop.purchase_failed.connect(_on_purchase_failed)

	# Connect profile signals for gold updates
	ProfileRepo.data_changed.connect(_on_data_changed)

	# Initialize display
	_update_gold_display()
	_populate_sections()
	_clear_detail_panel()

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

	# Sort offerings into sections
	for offering: ShopOffering in all_offerings:
		match offering.offering_type:
			ShopOffering.OfferingType.SUMMONER:
				# Check if this should be featured (first unowned summoner)
				if not featured_offering and not Shop.is_offering_owned(offering):
					featured_offering = offering
				_add_offering_item(summoner_items, offering)
			ShopOffering.OfferingType.COSMETIC:
				_add_offering_item(cosmetic_items, offering)
			ShopOffering.OfferingType.EMOTE:
				_add_offering_item(emote_items, offering)

	# Populate featured section
	if featured_offering:
		_add_offering_item(featured_items, featured_offering)
	else:
		# No unowned summoners - show completion message
		var complete_label: Label = Label.new()
		complete_label.text = Loc.t("ui.premium_store.all_summoners_unlocked")
		complete_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		complete_label.add_theme_color_override("font_color", Color(0.5, 0.8, 0.5))
		featured_items.add_child(complete_label)

func _add_offering_item(container: VBoxContainer, offering: ShopOffering) -> void:
	var item: Control = OFFERING_ITEM_SCENE.instantiate()
	container.add_child(item)
	if item.has_method("set_offering"):
		item.call("set_offering", offering)
	if item.has_signal("item_clicked"):
		item.connect("item_clicked", _on_offering_item_clicked.bind(offering))

func _clear_children(container: Node) -> void:
	for child: Node in container.get_children():
		child.queue_free()

## =============================================================================
## DETAIL PANEL
## =============================================================================

func _clear_detail_panel() -> void:
	selected_offering = null
	detail_name_label.text = Loc.t("ui.premium_store.select_item")
	detail_description_label.text = ""
	detail_price_label.text = ""
	purchase_button.visible = false
	owned_label.visible = false

	# Clear extra info
	for child: Node in detail_info_container.get_children():
		child.queue_free()

func _update_detail_panel(offering: ShopOffering) -> void:
	selected_offering = offering
	detail_name_label.text = offering.display_name
	detail_description_label.text = offering.description

	# Check if already owned
	var is_owned: bool = Shop.is_offering_owned(offering)

	if is_owned:
		detail_price_label.text = ""
		purchase_button.visible = false
		owned_label.visible = true
		owned_label.text = Loc.t("shop.button.owned")
	else:
		# Show price and purchase button
		var price: int = offering.base_price
		detail_price_label.text = Loc.t("ui.shop.price_format", {"price": price})
		purchase_button.visible = true
		owned_label.visible = false

		# Enable/disable purchase button based on affordability
		var resources: Dictionary = ProfileRepo.get_resources()
		var gold: int = resources.get("gold", 0)
		purchase_button.disabled = (gold < price)

	# Add extra info based on offering type
	_populate_detail_info(offering)

func _populate_detail_info(offering: ShopOffering) -> void:
	# Clear existing info
	for child: Node in detail_info_container.get_children():
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
	detail_info_container.add_child(element_label)

	# Base stats
	var stats_label: Label = Label.new()
	stats_label.text = Loc.t("ui.premium_store.base_stats", {"hp": config.base_health, "mana": config.max_mana})
	stats_label.add_theme_font_size_override("font_size", 14)
	detail_info_container.add_child(stats_label)

	# Innate traits
	if config.innate_trait_ids.size() > 0:
		var traits_header: Label = Label.new()
		traits_header.text = Loc.t("ui.premium_store.innate_traits")
		traits_header.add_theme_font_size_override("font_size", 14)
		detail_info_container.add_child(traits_header)

		for trait_id: String in config.innate_trait_ids:
			var trait_label: Label = Label.new()
			trait_label.text = "  - " + trait_id.capitalize()
			trait_label.add_theme_font_size_override("font_size", 12)
			detail_info_container.add_child(trait_label)

func _add_cosmetic_info(offering: ShopOffering) -> void:
	var type_label: Label = Label.new()
	type_label.text = Loc.t("ui.premium_store.cosmetic_type", {"type": offering.cosmetic_type.capitalize()})
	type_label.add_theme_font_size_override("font_size", 14)
	detail_info_container.add_child(type_label)

func _add_emote_info(offering: ShopOffering) -> void:
	var info_label: Label = Label.new()
	info_label.text = Loc.t("ui.premium_store.emote_info")
	info_label.add_theme_font_size_override("font_size", 14)
	detail_info_container.add_child(info_label)

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
	_update_detail_panel(offering)

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

	# Update detail panel if the purchased offering is still selected
	if selected_offering and selected_offering.offering_id == offering_id:
		_update_detail_panel(selected_offering)

func _on_purchase_failed(_offering_id: String, reason: String) -> void:
	# Show error message (could add a popup here in the future)
	push_warning("PremiumStoreScreen: Purchase failed - %s" % reason)

func _on_data_changed() -> void:
	_update_gold_display()

	# Update detail panel if an offering is selected (affordability may have changed)
	if selected_offering:
		_update_detail_panel(selected_offering)
