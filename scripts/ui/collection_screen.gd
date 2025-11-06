extends Control
class_name CollectionScreen

## CollectionScreen - Browse cards and manage decks with tabs
##
## Two tabs:
## - COLLECTION: Browse owned cards with filtering
## - MY DECKS: View and manage decks

## Tab views
@onready var collection_view: VBoxContainer = %CollectionView
@onready var my_decks_view: VBoxContainer = %MyDecksView

## Tab buttons
@onready var collection_tab_button: Button = %CollectionTabButton
@onready var my_decks_tab_button: Button = %MyDecksTabButton

## Header
@onready var back_button: Button = %BackButton
@onready var stats_label: Label = %Stats

## Collection tab nodes
@onready var card_grid: GridContainer = %CardGrid
@onready var detail_panel: PanelContainer = %DetailPanel

## Filter buttons
@onready var all_button: Button = %AllButton
@onready var summon_button: Button = %SummonButton
@onready var spell_button: Button = %SpellButton
@onready var common_button: Button = %CommonButton
@onready var rare_button: Button = %RareButton
@onready var epic_button: Button = %EpicButton

## Detail panel labels
@onready var card_name_label: Label = %CardNameLabel
@onready var rarity_label: Label = %RarityLabel
@onready var type_label: Label = %TypeLabel
@onready var cost_label: Label = %CostLabel
@onready var description_label: Label = %DescriptionLabel
@onready var owned_label: Label = %OwnedLabel

## My Decks tab nodes
@onready var deck_list: VBoxContainer = %DeckList
@onready var new_deck_button: Button = %NewDeckButton
@onready var delete_deck_button: Button = %DeleteDeckButton

## State
enum Tab { COLLECTION, MY_DECKS }
var current_tab: Tab = Tab.COLLECTION

var current_filter_type: int = -1  # -1 = all, 0 = summon, 1 = spell
var current_filter_rarity: String = ""  # "" = all, "common", "rare", "epic"
var selected_catalog_id: String = ""
var selected_deck_id: String = ""
var collection_summary: Array = []

## Card widget scene
const CardWidgetScene = preload("res://scenes/ui/card_widget.tscn")

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CollectionScreen: Initializing...")

	# Connect tab buttons
	collection_tab_button.pressed.connect(_on_collection_tab_pressed)
	my_decks_tab_button.pressed.connect(_on_my_decks_tab_pressed)

	# Connect header buttons
	back_button.pressed.connect(_on_back_pressed)

	# Connect deck buttons
	new_deck_button.pressed.connect(_on_new_deck_pressed)
	delete_deck_button.pressed.connect(_on_delete_deck_pressed)

	# Connect filter buttons
	all_button.pressed.connect(func(): _set_type_filter(-1))
	summon_button.pressed.connect(func(): _set_type_filter(0))
	spell_button.pressed.connect(func(): _set_type_filter(1))
	common_button.pressed.connect(func(): _set_rarity_filter("common"))
	rare_button.pressed.connect(func(): _set_rarity_filter("rare"))
	epic_button.pressed.connect(func(): _set_rarity_filter("epic"))

	# Connect to collection service
	var collection = get_node("/root/Collection")
	if collection:
		collection.collection_changed.connect(_on_collection_changed)

	# Connect to deck service
	var decks = get_node("/root/Decks")
	if decks:
		decks.deck_changed.connect(_on_deck_changed)
		decks.deck_created.connect(_on_deck_created)
		decks.deck_deleted.connect(_on_deck_deleted)

	# Load initial data
	_refresh_collection()
	_refresh_deck_list()

	# Show collection tab by default
	_switch_to_tab(Tab.COLLECTION)

## =============================================================================
## TAB SWITCHING
## =============================================================================

func _on_collection_tab_pressed() -> void:
	_switch_to_tab(Tab.COLLECTION)

func _on_my_decks_tab_pressed() -> void:
	_switch_to_tab(Tab.MY_DECKS)

func _switch_to_tab(tab: Tab) -> void:
	current_tab = tab

	# Update visibility
	collection_view.visible = (tab == Tab.COLLECTION)
	my_decks_view.visible = (tab == Tab.MY_DECKS)

	# Update button states
	collection_tab_button.disabled = (tab == Tab.COLLECTION)
	my_decks_tab_button.disabled = (tab == Tab.MY_DECKS)

	print("CollectionScreen: Switched to %s tab" % ("COLLECTION" if tab == Tab.COLLECTION else "MY DECKS"))

## =============================================================================
## COLLECTION TAB
## =============================================================================

func _refresh_collection() -> void:
	var collection = get_node("/root/Collection")
	if not collection:
		push_error("CollectionScreen: Collection service not found!")
		return

	var catalog = get_node("/root/CardCatalog")
	if not catalog:
		push_error("CollectionScreen: CardCatalog not found!")
		return

	# Get collection summary (grouped by catalog_id)
	collection_summary = collection.get_collection_summary()

	# Update stats
	var total_cards = 0
	var unique_cards = collection_summary.size()
	for entry in collection_summary:
		total_cards += entry.count

	stats_label.text = "%d Cards (%d Unique)" % [total_cards, unique_cards]

	# Refresh grid
	_refresh_grid()

func _refresh_grid() -> void:
	# Clear existing cards
	for child in card_grid.get_children():
		child.queue_free()

	var catalog = get_node("/root/CardCatalog")
	if not catalog:
		return

	# Filter collection summary
	var filtered_cards = []
	for entry in collection_summary:
		var catalog_id = entry.catalog_id
		var catalog_data = catalog.get_card(catalog_id)

		if catalog_data.is_empty():
			continue

		# Apply type filter
		if current_filter_type != -1:
			if catalog_data.get("card_type", 0) != current_filter_type:
				continue

		# Apply rarity filter
		if current_filter_rarity != "":
			if catalog_data.get("rarity", "common") != current_filter_rarity:
				continue

		filtered_cards.append(entry)

	# Create card widgets - show each instance individually
	var total_widgets = 0
	for entry in filtered_cards:
		var instances = entry.instances
		var catalog_data = catalog.get_card(entry.catalog_id)

		# Create a widget for EACH individual card instance
		for card_data in instances:
			var widget = CardWidgetScene.instantiate()
			card_grid.add_child(widget)

			# Set card data (individual instance, no count badge)
			widget.set_card(card_data, catalog_data)
			widget.set_count(1, false)  # Don't show count badge
			widget.set_draggable(false)

			# Connect selection (pass instance ID, not catalog ID)
			var instance_id = card_data.get("id", "")
			widget.card_clicked.connect(_on_card_instance_selected.bind(instance_id, entry.catalog_id))

			total_widgets += 1

	print("CollectionScreen: Showing %d individual cards" % total_widgets)

func _set_type_filter(type: int) -> void:
	current_filter_type = type
	_refresh_grid()
	_update_filter_button_states()

func _set_rarity_filter(rarity: String) -> void:
	# Toggle off if clicking same rarity
	if current_filter_rarity == rarity:
		current_filter_rarity = ""
	else:
		current_filter_rarity = rarity

	_refresh_grid()
	_update_filter_button_states()

func _update_filter_button_states() -> void:
	# Type buttons
	all_button.disabled = (current_filter_type == -1)
	summon_button.disabled = (current_filter_type == 0)
	spell_button.disabled = (current_filter_type == 1)

	# Rarity buttons
	common_button.disabled = (current_filter_rarity == "common")
	rare_button.disabled = (current_filter_rarity == "rare")
	epic_button.disabled = (current_filter_rarity == "epic")

func _on_card_instance_selected(instance_id: String, catalog_id: String) -> void:
	selected_catalog_id = catalog_id

	var catalog = get_node("/root/CardCatalog")
	if not catalog:
		return

	var catalog_data = catalog.get_card(catalog_id)
	if catalog_data.is_empty():
		return

	# Update detail panel
	card_name_label.text = catalog_data.get("card_name", "Unknown")
	rarity_label.text = "Rarity: %s" % catalog_data.get("rarity", "common").capitalize()

	var card_type = catalog_data.get("card_type", 0)
	type_label.text = "Type: %s" % ("Summon" if card_type == 0 else "Spell")

	cost_label.text = "Cost: %d Mana" % catalog_data.get("mana_cost", 0)
	description_label.text = catalog_data.get("description", "No description.")

	# Get count of this card type from collection summary
	var count = 0
	for entry in collection_summary:
		if entry.catalog_id == catalog_id:
			count = entry.count
			break

	owned_label.text = "Owned: %d" % count

	print("CollectionScreen: Selected card instance: %s (%s)" % [instance_id, catalog_id])

## =============================================================================
## MY DECKS TAB
## =============================================================================

func _refresh_deck_list() -> void:
	# Clear existing deck items
	for child in deck_list.get_children():
		child.queue_free()

	var decks = get_node("/root/Decks")
	if not decks:
		push_error("CollectionScreen: Decks service not found!")
		return

	var deck_list_data = decks.list_decks()

	if deck_list_data.size() == 0:
		var label = Label.new()
		label.text = "No decks yet. Click 'NEW DECK' to create one!"
		label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		label.add_theme_font_size_override("font_size", 20)
		deck_list.add_child(label)
		return

	# Create deck list items
	for deck_data in deck_list_data:
		var deck_item = _create_deck_list_item(deck_data)
		deck_list.add_child(deck_item)

	print("CollectionScreen: Loaded %d decks" % deck_list_data.size())

func _create_deck_list_item(deck_data: Dictionary) -> PanelContainer:
	var panel = PanelContainer.new()
	var margin = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 15)
	margin.add_theme_constant_override("margin_top", 15)
	margin.add_theme_constant_override("margin_right", 15)
	margin.add_theme_constant_override("margin_bottom", 15)
	panel.add_child(margin)

	var hbox = HBoxContainer.new()
	margin.add_child(hbox)

	# Deck name
	var name_label = Label.new()
	name_label.text = deck_data.get("name", "Unnamed Deck")
	name_label.add_theme_font_size_override("font_size", 24)
	name_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hbox.add_child(name_label)

	# Card count
	var card_ids = deck_data.get("card_instance_ids", [])
	var count_label = Label.new()
	count_label.text = "%d / 30" % card_ids.size()
	count_label.add_theme_font_size_override("font_size", 20)
	count_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	hbox.add_child(count_label)

	# Validation status
	var decks = get_node("/root/Decks")
	var is_valid = decks.validate_deck(deck_data.get("id", ""))
	var status_label = Label.new()
	status_label.text = "✓" if is_valid else "⚠"
	status_label.add_theme_font_size_override("font_size", 24)
	status_label.add_theme_color_override("font_color", Color(0.3, 1.0, 0.3) if is_valid else Color(1.0, 0.7, 0.3))
	hbox.add_child(status_label)

	# Make clickable
	var button = Button.new()
	button.flat = true
	button.custom_minimum_size = panel.custom_minimum_size
	var deck_id = deck_data.get("id", "")
	button.pressed.connect(_on_deck_item_clicked.bind(deck_id))
	panel.add_child(button)

	return panel

func _on_deck_item_clicked(deck_id: String) -> void:
	print("CollectionScreen: Opening deck editor for: %s" % deck_id)
	# Store selected deck ID in profile meta temporarily so deck builder can read it
	var profile_repo = get_node("/root/ProfileRepo")
	if profile_repo:
		var profile = profile_repo.get_active_profile()
		if not profile.is_empty():
			profile["meta"]["editing_deck_id"] = deck_id
			print("CollectionScreen: Set editing_deck_id to '%s'" % deck_id)

	get_tree().change_scene_to_file("res://scenes/ui/deck_builder.tscn")

func _on_new_deck_pressed() -> void:
	var decks = get_node("/root/Decks")
	if not decks:
		return

	var deck_id = decks.create_deck("New Deck", [])
	print("CollectionScreen: Created new deck: %s" % deck_id)

func _on_delete_deck_pressed() -> void:
	# TODO: Show confirmation dialog and delete selected deck
	print("CollectionScreen: Delete deck not yet implemented")

## =============================================================================
## NAVIGATION
## =============================================================================

func _on_back_pressed() -> void:
	print("CollectionScreen: Returning to main menu")
	get_tree().change_scene_to_file("res://scenes/ui/main_menu.tscn")

## =============================================================================
## SIGNALS
## =============================================================================

func _on_collection_changed() -> void:
	print("CollectionScreen: Collection changed, refreshing...")
	_refresh_collection()

func _on_deck_changed(_deck_id: String) -> void:
	_refresh_deck_list()

func _on_deck_created(_deck_id: String) -> void:
	_refresh_deck_list()

func _on_deck_deleted(_deck_id: String) -> void:
	_refresh_deck_list()
