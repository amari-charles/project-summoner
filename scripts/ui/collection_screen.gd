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
const CardWidgetScene: PackedScene = preload("res://scenes/ui/card_widget.tscn")

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
	all_button.pressed.connect(func() -> void: _set_type_filter(-1))
	summon_button.pressed.connect(func() -> void: _set_type_filter(0))
	spell_button.pressed.connect(func() -> void: _set_type_filter(1))
	common_button.pressed.connect(func() -> void: _set_rarity_filter("common"))
	rare_button.pressed.connect(func() -> void: _set_rarity_filter("rare"))
	epic_button.pressed.connect(func() -> void: _set_rarity_filter("epic"))

	# Connect to collection service
	var collection: Node = get_node("/root/Collection")
	if collection and collection.has_signal("collection_changed"):
		var collection_changed_variant: Variant = collection.get("collection_changed")
		if collection_changed_variant is Signal:
			var collection_changed_sig: Signal = collection_changed_variant
			collection_changed_sig.connect(_on_collection_changed)

	# Connect to deck service
	var decks: Node = get_node("/root/Decks")
	if decks and decks.has_signal("deck_changed") and decks.has_signal("deck_created") and decks.has_signal("deck_deleted"):
		var deck_changed_variant: Variant = decks.get("deck_changed")
		var deck_created_variant: Variant = decks.get("deck_created")
		var deck_deleted_variant: Variant = decks.get("deck_deleted")
		if deck_changed_variant is Signal:
			var deck_changed_sig: Signal = deck_changed_variant
			deck_changed_sig.connect(_on_deck_changed)
		if deck_created_variant is Signal:
			var deck_created_sig: Signal = deck_created_variant
			deck_created_sig.connect(_on_deck_created)
		if deck_deleted_variant is Signal:
			var deck_deleted_sig: Signal = deck_deleted_variant
			deck_deleted_sig.connect(_on_deck_deleted)

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
	var collection: Node = get_node("/root/Collection")
	if not collection or not collection.has_method("get_collection_summary"):
		push_error("CollectionScreen: Collection service not found!")
		return

	var catalog: Node = get_node("/root/CardCatalog")
	if not catalog or not catalog.has_method("get_card"):
		push_error("CollectionScreen: CardCatalog not found!")
		return

	# Get collection summary (grouped by catalog_id)
	var summary_result: Variant = collection.call("get_collection_summary")
	if not summary_result is Array:
		return
	collection_summary = summary_result

	# Update stats
	var total_cards: int = 0
	var unique_cards: int = collection_summary.size()
	for entry_var: Variant in collection_summary:
		if not entry_var is Dictionary:
			continue
		var entry: Dictionary = entry_var
		var count_val: Variant = entry.get("count", 0)
		if count_val is int:
			total_cards += count_val

	stats_label.text = "%d Cards (%d Unique)" % [total_cards, unique_cards]

	# Refresh grid
	_refresh_grid()

func _refresh_grid() -> void:
	# Clear existing cards
	for child: Node in card_grid.get_children():
		child.queue_free()

	var catalog: Node = get_node("/root/CardCatalog")
	if not catalog or not catalog.has_method("get_card"):
		return

	# Filter collection summary
	var filtered_cards: Array = []
	for entry_var: Variant in collection_summary:
		if not entry_var is Dictionary:
			continue
		var entry: Dictionary = entry_var
		var catalog_id_val: Variant = entry.get("catalog_id", "")
		if not catalog_id_val is String:
			continue
		var catalog_id: String = catalog_id_val

		var catalog_data_result: Variant = catalog.call("get_card", catalog_id)
		if not catalog_data_result is Dictionary:
			continue
		var catalog_data: Dictionary = catalog_data_result

		if catalog_data.is_empty():
			continue

		# Apply type filter
		if current_filter_type != -1:
			var card_type_val: Variant = catalog_data.get("card_type", 0)
			if card_type_val is int and card_type_val != current_filter_type:
				continue

		# Apply rarity filter
		if current_filter_rarity != "":
			var rarity_val: Variant = catalog_data.get("rarity", "common")
			if rarity_val is String and rarity_val != current_filter_rarity:
				continue

		filtered_cards.append(entry)

	# Create card widgets - show each instance individually
	var total_widgets: int = 0
	for entry_var: Variant in filtered_cards:
		if not entry_var is Dictionary:
			continue
		var entry: Dictionary = entry_var
		var instances_val: Variant = entry.get("instances", [])
		if not instances_val is Array:
			continue
		var instances: Array = instances_val

		var catalog_id_val: Variant = entry.get("catalog_id", "")
		if not catalog_id_val is String:
			continue
		var entry_catalog_id: String = catalog_id_val

		var catalog_data_result: Variant = catalog.call("get_card", entry_catalog_id)
		if not catalog_data_result is Dictionary:
			continue
		var catalog_data: Dictionary = catalog_data_result

		# Create a widget for EACH individual card instance
		for card_data_var: Variant in instances:
			if not card_data_var is Dictionary:
				continue
			var card_data: Dictionary = card_data_var

			var widget_node: Node = CardWidgetScene.instantiate()
			if not widget_node is CardWidget:
				continue
			var widget: CardWidget = widget_node
			card_grid.add_child(widget)

			# Set card data
			widget.set_card(card_data, catalog_data)
			widget.set_draggable(false)

			# Connect selection (pass instance ID, not catalog ID)
			var instance_id_val: Variant = card_data.get("id", "")
			var instance_id: String = instance_id_val if instance_id_val is String else ""
			var card_clicked_sig: Signal = widget.card_clicked
			card_clicked_sig.connect(_on_card_instance_selected.bind(instance_id, entry_catalog_id))

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

	var catalog: Node = get_node("/root/CardCatalog")
	if not catalog or not catalog.has_method("get_card"):
		return

	var catalog_data_result: Variant = catalog.call("get_card", catalog_id)
	if not catalog_data_result is Dictionary:
		return
	var catalog_data: Dictionary = catalog_data_result
	if catalog_data.is_empty():
		return

	# Update detail panel
	var card_name_val: Variant = catalog_data.get("card_name", "Unknown")
	card_name_label.text = card_name_val if card_name_val is String else "Unknown"

	var rarity_val: Variant = catalog_data.get("rarity", "common")
	var rarity_str: String = rarity_val if rarity_val is String else "common"
	rarity_label.text = "Rarity: %s" % rarity_str.capitalize()

	var card_type_val: Variant = catalog_data.get("card_type", 0)
	var card_type: int = card_type_val if card_type_val is int else 0
	type_label.text = "Type: %s" % ("Summon" if card_type == 0 else "Spell")

	var mana_cost_val: Variant = catalog_data.get("mana_cost", 0)
	var mana_cost: int = mana_cost_val if mana_cost_val is int else 0
	cost_label.text = "Cost: %d Mana" % mana_cost

	var description_val: Variant = catalog_data.get("description", "No description.")
	description_label.text = description_val if description_val is String else "No description."

	# Get count of this card type from collection summary
	var count: int = 0
	for entry_var: Variant in collection_summary:
		if not entry_var is Dictionary:
			continue
		var entry: Dictionary = entry_var
		var entry_catalog_id_val: Variant = entry.get("catalog_id", "")
		if entry_catalog_id_val is String and entry_catalog_id_val == catalog_id:
			var count_val: Variant = entry.get("count", 0)
			if count_val is int:
				count = count_val
			break

	owned_label.text = "Owned: %d" % count

	print("CollectionScreen: Selected card instance: %s (%s)" % [instance_id, catalog_id])

## =============================================================================
## MY DECKS TAB
## =============================================================================

func _refresh_deck_list() -> void:
	# Clear existing deck items
	for child: Node in deck_list.get_children():
		child.queue_free()

	var decks: Node = get_node("/root/Decks")
	if not decks or not decks.has_method("list_decks"):
		push_error("CollectionScreen: Decks service not found!")
		return

	var deck_list_result: Variant = decks.call("list_decks")
	if not deck_list_result is Array:
		return
	var deck_list_data: Array = deck_list_result

	if deck_list_data.size() == 0:
		var label: Label = Label.new()
		label.text = "No decks yet. Click 'NEW DECK' to create one!"
		label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		label.add_theme_font_size_override("font_size", 20)
		deck_list.add_child(label)
		return

	# Create deck list items
	for deck_data_var: Variant in deck_list_data:
		if not deck_data_var is Dictionary:
			continue
		var deck_data: Dictionary = deck_data_var
		var deck_item: PanelContainer = _create_deck_list_item(deck_data)
		deck_list.add_child(deck_item)

	print("CollectionScreen: Loaded %d decks" % deck_list_data.size())

func _create_deck_list_item(deck_data: Dictionary) -> PanelContainer:
	var panel: PanelContainer = PanelContainer.new()
	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 15)
	margin.add_theme_constant_override("margin_top", 15)
	margin.add_theme_constant_override("margin_right", 15)
	margin.add_theme_constant_override("margin_bottom", 15)
	panel.add_child(margin)

	var hbox: HBoxContainer = HBoxContainer.new()
	margin.add_child(hbox)

	# Deck name
	var name_label: Label = Label.new()
	var name_val: Variant = deck_data.get("name", "Unnamed Deck")
	name_label.text = name_val if name_val is String else "Unnamed Deck"
	name_label.add_theme_font_size_override("font_size", 24)
	name_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hbox.add_child(name_label)

	# Card count
	var card_ids_val: Variant = deck_data.get("card_instance_ids", [])
	var card_ids: Array = card_ids_val if card_ids_val is Array else []
	var count_label: Label = Label.new()
	count_label.text = "%d / 30" % card_ids.size()
	count_label.add_theme_font_size_override("font_size", 20)
	count_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	hbox.add_child(count_label)

	# Validation status
	var decks: Node = get_node("/root/Decks")
	var deck_id_val: Variant = deck_data.get("id", "")
	var deck_id: String = deck_id_val if deck_id_val is String else ""
	var is_valid: bool = false
	if decks and decks.has_method("validate_deck"):
		var valid_result: Variant = decks.call("validate_deck", deck_id)
		if valid_result is bool:
			is_valid = valid_result
	var status_label: Label = Label.new()
	status_label.text = "✓" if is_valid else "⚠"
	status_label.add_theme_font_size_override("font_size", 24)
	status_label.add_theme_color_override("font_color", Color(0.3, 1.0, 0.3) if is_valid else Color(1.0, 0.7, 0.3))
	hbox.add_child(status_label)

	# Make clickable
	var button: Button = Button.new()
	button.flat = true
	button.custom_minimum_size = panel.custom_minimum_size
	var pressed_sig: Signal = button.pressed
	pressed_sig.connect(_on_deck_item_clicked.bind(deck_id))
	panel.add_child(button)

	return panel

func _on_deck_item_clicked(deck_id: String) -> void:
	print("CollectionScreen: Opening deck editor for: %s" % deck_id)

	# Check if deck editing is locked (tutorial not complete)
	var campaign: Node = get_node("/root/Campaign")
	if campaign and campaign.has_method("is_tutorial_complete"):
		var is_complete_result: Variant = campaign.call("is_tutorial_complete")
		if is_complete_result is bool and not is_complete_result:
			print("CollectionScreen: Deck editing locked - tutorial not complete")
			_show_deck_locked_message()
			return

	# Store selected deck ID in profile meta temporarily so deck builder can read it
	var profile_repo: Node = get_node("/root/ProfileRepo")
	if profile_repo and profile_repo.has_method("get_active_profile"):
		var profile_result: Variant = profile_repo.call("get_active_profile")
		if profile_result is Dictionary:
			var profile: Dictionary = profile_result
			if not profile.is_empty():
				var empty_dict: Dictionary = {}
				var meta_val: Variant = profile.get("meta", empty_dict)
				if meta_val is Dictionary:
					var meta: Dictionary = meta_val
					meta["editing_deck_id"] = deck_id
					print("CollectionScreen: Set editing_deck_id to '%s'" % deck_id)

	get_tree().change_scene_to_file("res://scenes/ui/deck_builder.tscn")

func _show_deck_locked_message() -> void:
	# Show popup dialog informing player deck editing is locked
	var dialog: AcceptDialog = AcceptDialog.new()
	dialog.title = "Deck Locked"
	dialog.dialog_text = "Complete the tutorial battles to unlock deck editing!\n\nYour deck will be automatically updated as you earn new cards."
	dialog.initial_position = Window.WINDOW_INITIAL_POSITION_CENTER_MAIN_WINDOW_SCREEN
	add_child(dialog)
	dialog.popup_centered()
	var confirmed_sig: Signal = dialog.confirmed
	confirmed_sig.connect(dialog.queue_free)

func _on_new_deck_pressed() -> void:
	var decks: Node = get_node("/root/Decks")
	if not decks or not decks.has_method("create_deck"):
		return

	var deck_id_result: Variant = decks.call("create_deck", "New Deck", [])
	var deck_id: String = deck_id_result if deck_id_result is String else ""
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
