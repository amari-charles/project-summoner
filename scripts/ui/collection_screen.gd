extends Control
class_name CollectionScreen

## CollectionScreen - Browse owned cards
##
## Displays all cards in the player's collection with filtering.
## Shows card details when selected.
## Navigate to Deck Builder with "Build Deck" button.

## Node references
@onready var back_button: Button = %BackButton
@onready var stats_label: Label = %Stats
@onready var card_grid: GridContainer = %CardGrid
@onready var detail_panel: PanelContainer = %DetailPanel
@onready var build_deck_button: Button = %BuildDeckButton

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

## State
var current_filter_type: int = -1  # -1 = all, 0 = summon, 1 = spell
var current_filter_rarity: String = ""  # "" = all, "common", "rare", "epic"
var selected_catalog_id: String = ""
var collection_summary: Array = []

## Card widget scene
const CardWidgetScene = preload("res://scenes/ui/card_widget.tscn")

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CollectionScreen: Initializing...")

	# Connect buttons
	back_button.pressed.connect(_on_back_pressed)
	build_deck_button.pressed.connect(_on_build_deck_pressed)

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

	# Load collection
	_refresh_collection()

## =============================================================================
## COLLECTION LOADING
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

## =============================================================================
## FILTERING
## =============================================================================

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

## =============================================================================
## CARD SELECTION
## =============================================================================

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

	# TODO: Future - show individual stat rolls here
	# var collection = get_node("/root/Collection")
	# var instance_data = collection.get_card(instance_id)
	# Show instance_data.roll_json stats

	print("CollectionScreen: Selected card instance: %s (%s)" % [instance_id, catalog_id])

## =============================================================================
## NAVIGATION
## =============================================================================

func _on_back_pressed() -> void:
	print("CollectionScreen: Returning to main menu")
	get_tree().change_scene_to_file("res://scenes/ui/main_menu.tscn")

func _on_build_deck_pressed() -> void:
	print("CollectionScreen: Opening deck builder")
	get_tree().change_scene_to_file("res://scenes/ui/deck_builder.tscn")

## =============================================================================
## SIGNALS
## =============================================================================

func _on_collection_changed() -> void:
	print("CollectionScreen: Collection changed, refreshing...")
	_refresh_collection()
