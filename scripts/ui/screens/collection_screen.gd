extends Control
class_name CollectionScreen

## CollectionScreen - Side-by-side layout for collection and decks
##
## Left panel (2/3): Collection grid with filters and search
## Right panel (1/3): Deck list - click to select, double-click to open details
## Click card → shows action popup ("Use" / "Info")

## =============================================================================
## NODE REFERENCES
## =============================================================================

## Left panel - Header
@onready var close_button: Button = %CloseButton
@onready var gold_label: Label = %GoldLabel

## Left panel - Filters
@onready var all_button: Button = %AllButton
@onready var summon_button: Button = %SummonButton
@onready var spell_button: Button = %SpellButton
@onready var element_filter_button: Button = %ElementFilterButton
@onready var element_popup: PopupMenu = %ElementPopup
@onready var sort_dropdown: OptionButton = %SortDropdown
@onready var search_edit: LineEdit = %SearchEdit

## Left panel - Collection
@onready var collection_scroll: CollectionDropZone = %CollectionScroll
@onready var card_grid: GridContainer = %CardGrid

## Left panel - Selected Deck Panel (inline deck view with drop zone)
@onready var selected_deck_panel: DeckDropZone = %SelectedDeckPanel
@onready var selected_deck_name: Label = %SelectedDeckName
@onready var selected_deck_count: Label = %SelectedDeckCount
@onready var deck_cards_container: HBoxContainer = %DeckCardsContainer

## Right panel - Decks
@onready var decks_list: VBoxContainer = %DecksList
@onready var new_deck_button: Button = %NewDeckButton

## Dialogs
@onready var new_deck_dialog: AcceptDialog = %NewDeckDialog
@onready var deck_name_input: LineEdit = %DeckNameInput
@onready var confirm_delete_dialog: ConfirmationDialog = %ConfirmDeleteDialog
@onready var rename_dialog: AcceptDialog = %RenameDialog
@onready var rename_input: LineEdit = %RenameInput

## =============================================================================
## STATE
## =============================================================================

## Currently selected deck for adding cards
var selected_deck_id: String = ""

## Deck being renamed/deleted (for dialog callbacks)
var deck_id_for_action: String = ""

## Collection data
var collection_summary: Array = []

## Filter state
var show_summons: bool = true
var show_spells: bool = true
var selected_elements: Array[String] = []  # Empty = all elements
var search_text: String = ""

## Element list for filtering
const ELEMENTS: Array[String] = ["fire", "water", "earth", "air", "light", "dark", "neutral"]

## Sort state
enum SortField { NAME, COST, RARITY, TYPE, LEVEL, ELEMENT }
var current_sort_field: SortField = SortField.COST
var sort_ascending: bool = true

const RARITY_ORDER: Dictionary = {
	"common": 0,
	"rare": 1,
	"epic": 2,
	"legendary": 3
}

## Currently active action popup
var active_popup: Control = null

## Widget cache for reuse (instance_id -> CardWidget)
var _widget_cache: Dictionary = {}

## Double-click tracking for collection cards
var last_clicked_card_id: String = ""
var last_click_time: int = 0
const DOUBLE_CLICK_THRESHOLD_MS: int = 400

## Double-click tracking for deck cards
var last_deck_card_id: String = ""
var last_deck_click_time: int = 0

## Constants
const MAX_DECK_SIZE: int = 30
const DECK_PANEL_CARD_SIZE: Vector2 = Vector2(160, 240)  # Standard card size for inline deck view

## Scenes
const CardWidgetScene: PackedScene = preload("res://scenes/ui/components/card_widget.tscn")
const CardDetailModalScene: PackedScene = preload("res://scenes/ui/modals/card_detail_modal.tscn")
const LevelUpPanelScene: PackedScene = preload("res://scenes/ui/modals/card_level_up_panel.tscn")
const DeckListItemScene: PackedScene = preload("res://scenes/ui/components/deck_list_item.tscn")
const CardActionPopupScene: PackedScene = preload("res://scenes/ui/components/card_action_popup.tscn")


## =============================================================================
## LIFECYCLE
## =============================================================================

func _exit_tree() -> void:
	_widget_cache.clear()


func _ready() -> void:
	# Connect header buttons
	close_button.pressed.connect(_on_close_pressed)

	# Connect deck management
	new_deck_button.pressed.connect(_on_new_deck_pressed)

	# Connect dialogs
	new_deck_dialog.confirmed.connect(_on_new_deck_confirmed)
	confirm_delete_dialog.confirmed.connect(_on_delete_confirmed)
	rename_dialog.confirmed.connect(_on_rename_confirmed)

	# Connect filter buttons (toggle mode)
	all_button.pressed.connect(_on_all_filter_pressed)
	summon_button.pressed.connect(_on_summon_filter_pressed)
	spell_button.pressed.connect(_on_spell_filter_pressed)
	_update_type_filter_buttons()

	# Connect element filter
	_populate_element_popup()
	element_filter_button.pressed.connect(_on_element_filter_pressed)
	element_popup.id_pressed.connect(_on_element_toggled)

	# Connect sort dropdown
	_populate_sort_dropdown()
	sort_dropdown.item_selected.connect(_on_sort_selected)

	# Connect search
	search_edit.text_changed.connect(_on_search_changed)

	# Set up deck drop zone
	selected_deck_panel.can_drop_callback = _can_drop_card_to_deck
	selected_deck_panel.card_dropped.connect(_on_card_dropped_to_deck)

	# Set up collection drop zone (for removing cards by dragging out of deck)
	collection_scroll.can_remove_callback = _can_remove_card_from_deck
	collection_scroll.card_dropped_to_remove.connect(_on_card_dropped_to_remove)

	# Connect to services
	_connect_services()

	# Initial load
	_refresh_deck_list()
	_refresh_deck_panel()
	_refresh_collection()
	_refresh_gold_display()


func _connect_services() -> void:
	if Decks.has_signal("deck_changed"):
		Decks.deck_changed.connect(_on_deck_changed)
	if Decks.has_signal("deck_created"):
		Decks.deck_created.connect(_on_deck_created)
	if Decks.has_signal("deck_deleted"):
		Decks.deck_deleted.connect(_on_deck_deleted)

	CardServiceCS.CollectionChanged.connect(_on_collection_changed)


## =============================================================================
## DECK LIST (Right Panel)
## =============================================================================

func _refresh_deck_list() -> void:
	if not Decks.has_method("list_decks"):
		return

	var deck_list_result: Variant = Decks.call("list_decks")
	if not deck_list_result is Array:
		return

	# Clear existing items
	for child: Node in decks_list.get_children():
		child.queue_free()

	# Get active deck ID
	var active_deck_id: String = ""
	if Decks.has_method("get_active_deck_id"):
		var active_id: Variant = Decks.call("get_active_deck_id")
		if active_id is String:
			active_deck_id = active_id

	# Create deck items
	for deck_item: Variant in deck_list_result:
		if not deck_item is Dictionary:
			continue
		var deck: Dictionary = deck_item
		var deck_id: String = deck.get("id", "")
		if deck_id == "":
			continue

		var item: Control = DeckListItemScene.instantiate()
		decks_list.add_child(item)

		# Configure the item
		if item.has_method("setup"):
			var card_count: int = deck.get("card_instance_ids", []).size()
			var is_valid: bool = true
			if Decks.has_method("get_validation_errors"):
				var errors: Variant = Decks.call("get_validation_errors", deck_id)
				is_valid = errors is Array and errors.size() == 0

			item.call("setup", {
				"id": deck_id,
				"name": deck.get("name", Loc.t("ui.collection.unnamed_deck")),
				"card_count": card_count,
				"max_cards": MAX_DECK_SIZE,
				"is_active": deck_id == active_deck_id,
				"is_selected": deck_id == selected_deck_id,
				"is_valid": is_valid
			})

		# Connect signals
		if item.has_signal("clicked"):
			item.clicked.connect(_on_deck_item_clicked.bind(deck_id))
		if item.has_signal("double_clicked"):
			item.double_clicked.connect(_on_deck_item_double_clicked.bind(deck_id))
		if item.has_signal("star_clicked"):
			item.star_clicked.connect(_on_deck_star_clicked.bind(deck_id))
		if item.has_signal("rename_clicked"):
			item.rename_clicked.connect(_on_deck_rename_clicked.bind(deck_id))
		if item.has_signal("delete_clicked"):
			item.delete_clicked.connect(_on_deck_delete_clicked.bind(deck_id))

	# Create default deck if none exist (only if summoner is unlocked)
	if deck_list_result.size() == 0 and Decks.has_method("create_deck"):
		# Check if any summoners are unlocked - can't create deck without one
		if ProfileRepo.has_method("get_unlocked_summoners"):
			var unlocked: Variant = ProfileRepo.call("get_unlocked_summoners")
			if unlocked is Array and unlocked.size() > 0:
				var new_deck_id: Variant = Decks.call("create_deck", "My Deck", [])
				if new_deck_id is String and not new_deck_id.is_empty():
					_refresh_deck_list()
		return

	# Auto-select first deck if none selected
	if selected_deck_id == "" and deck_list_result.size() > 0:
		var first_deck: Dictionary = deck_list_result[0]
		selected_deck_id = first_deck.get("id", "")
		_refresh_deck_list()


func _on_deck_item_clicked(deck_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if deck_id == selected_deck_id:
		return
	selected_deck_id = deck_id
	_refresh_deck_list()
	_refresh_deck_panel()
	_refresh_collection()  # Update in-deck badges


func _on_deck_item_double_clicked(_deck_id: String) -> void:
	# Double-click does the same as single-click (inline editing now)
	pass


func _on_deck_star_clicked(deck_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if Decks.has_method("set_active_deck"):
		Decks.call("set_active_deck", deck_id)
		_refresh_deck_list()


func _on_deck_rename_clicked(deck_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	deck_id_for_action = deck_id
	# Get current deck name
	if Decks.has_method("get_deck"):
		var deck: Variant = Decks.call("get_deck", deck_id)
		if deck is Dictionary:
			rename_input.text = deck.get("name", "")
	rename_dialog.popup_centered()


func _on_deck_delete_clicked(deck_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	deck_id_for_action = deck_id
	confirm_delete_dialog.popup_centered()


## =============================================================================
## INLINE DECK PANEL
## =============================================================================

func _refresh_deck_panel() -> void:
	# Clear existing cards
	for child: Node in deck_cards_container.get_children():
		child.queue_free()

	# Update header
	if selected_deck_id == "":
		selected_deck_name.text = Loc.t("ui.collection.no_deck_selected")
		selected_deck_count.text = ""
		return

	if not Decks.has_method("get_deck"):
		return

	var deck_result: Variant = Decks.call("get_deck", selected_deck_id)
	if not deck_result is Dictionary or deck_result.is_empty():
		return

	var deck: Dictionary = deck_result
	selected_deck_name.text = deck.get("name", Loc.t("ui.collection.unnamed_deck"))
	var card_ids: Array = deck.get("card_instance_ids", [])
	selected_deck_count.text = "%d/%d" % [card_ids.size(), MAX_DECK_SIZE]

	# Populate deck cards
	for card_id: Variant in card_ids:
		if not card_id is String:
			continue

		var card_data: Variant = CardServiceCS.GetCardDict(card_id)
		if not card_data is Dictionary or card_data.is_empty():
			continue

		var catalog_id: String = card_data.get("catalog_id", "")
		var catalog_data: Variant = CardCatalog.call("get_card", catalog_id)
		if not catalog_data is Dictionary or catalog_data.is_empty():
			continue

		var widget: CardWidget = CardWidgetScene.instantiate()
		deck_cards_container.add_child(widget)
		widget.set_card(card_data, catalog_data)
		widget.set_draggable(true)  # Enable dragging to remove
		widget.custom_minimum_size = DECK_PANEL_CARD_SIZE
		widget.tooltip_text = Loc.t("ui.collection.deck_card_remove_tooltip")

		# Double-click to remove from deck
		widget.card_clicked.connect(_on_deck_card_clicked.bind(card_id))


func _on_deck_card_clicked(_card_data: Dictionary, card_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)

	# Check for double-click
	var current_time: int = Time.get_ticks_msec()
	if card_id == last_deck_card_id and current_time - last_deck_click_time < DOUBLE_CLICK_THRESHOLD_MS:
		# Double-click: remove from deck
		last_deck_card_id = ""
		last_deck_click_time = 0
		_remove_card_from_deck(card_id)
		return

	# Single click: just track for potential double-click
	last_deck_card_id = card_id
	last_deck_click_time = current_time


## =============================================================================
## DRAG AND DROP
## =============================================================================

func _can_drop_card_to_deck(instance_id: String) -> bool:
	# Don't allow if no deck selected
	if selected_deck_id == "":
		return false

	# Don't allow if deck is full
	var deck_card_ids: Array[String] = _get_selected_deck_card_ids()
	if deck_card_ids.size() >= MAX_DECK_SIZE:
		return false

	# Don't allow if card is already in deck
	if instance_id in deck_card_ids:
		return false

	return true


func _on_card_dropped_to_deck(instance_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_add_card_to_selected_deck(instance_id)


func _can_remove_card_from_deck(instance_id: String) -> bool:
	# Check if this card is in the current deck
	var deck_card_ids: Array[String] = _get_selected_deck_card_ids()
	return instance_id in deck_card_ids


func _on_card_dropped_to_remove(instance_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_remove_card_from_deck(instance_id)


## =============================================================================
## DECK CREATION/DELETION
## =============================================================================

func _on_new_deck_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	deck_name_input.text = ""
	new_deck_dialog.popup_centered()


func _on_new_deck_confirmed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	var deck_name: String = deck_name_input.text
	if deck_name == "":
		deck_name = Loc.t("ui.collection.new_deck_default")

	if Decks.has_method("create_deck"):
		Decks.call("create_deck", deck_name, [])


func _on_delete_confirmed() -> void:
	if deck_id_for_action == "":
		return

	if Decks.has_method("delete_deck"):
		var success: Variant = Decks.call("delete_deck", deck_id_for_action)
		if success is bool and success:
			if selected_deck_id == deck_id_for_action:
				selected_deck_id = ""
			deck_id_for_action = ""
			_refresh_deck_panel()


func _on_rename_confirmed() -> void:
	if deck_id_for_action == "":
		return

	var new_name: String = rename_input.text
	if new_name == "":
		return

	if Decks.has_method("rename_deck"):
		Decks.call("rename_deck", deck_id_for_action, new_name)
		deck_id_for_action = ""
		_refresh_deck_list()
		_refresh_deck_panel()


## =============================================================================
## COLLECTION DISPLAY (Left Panel)
## =============================================================================

func _refresh_collection() -> void:
	var summary_result: Variant = CardServiceCS.GetCollectionSummaryDict()
	if not summary_result is Array:
		return
	collection_summary = summary_result

	# Get deck card IDs
	var deck_card_ids: Array[String] = _get_selected_deck_card_ids()

	# Filter and sort
	var filtered_cards: Array = _get_filtered_sorted_cards()

	# Build list of instance_ids that should be visible
	var visible_ids: Array[String] = []
	for card_entry: Variant in filtered_cards:
		if not card_entry is Dictionary:
			continue
		var instance_id: String = card_entry.get("instance_id", "")
		if instance_id not in deck_card_ids:
			visible_ids.append(instance_id)

	# Remove widgets that should no longer be visible
	var ids_to_remove: Array[String] = []
	for instance_id: String in _widget_cache.keys():
		if instance_id not in visible_ids:
			ids_to_remove.append(instance_id)

	for instance_id: String in ids_to_remove:
		var widget: CardWidget = _widget_cache[instance_id]
		if is_instance_valid(widget):
			widget.queue_free()
		_widget_cache.erase(instance_id)

	# Create or reuse widgets in the correct order
	for card_entry: Variant in filtered_cards:
		if not card_entry is Dictionary:
			continue
		var instance_id: String = card_entry.get("instance_id", "")

		# Skip cards in deck
		if instance_id in deck_card_ids:
			continue

		var card_data: Dictionary = card_entry.get("card_data", {})
		var catalog_data: Dictionary = card_entry.get("catalog_data", {})
		var catalog_id: String = card_data.get("catalog_id", "")

		var widget: CardWidget
		if _widget_cache.has(instance_id) and is_instance_valid(_widget_cache[instance_id]):
			# Reuse existing widget
			widget = _widget_cache[instance_id]
		else:
			# Create new widget
			widget = CardWidgetScene.instantiate()
			card_grid.add_child(widget)
			widget.set_card(card_data, catalog_data)
			widget.set_draggable(true)
			widget.card_clicked.connect(_on_collection_card_clicked.bind(widget, instance_id, catalog_id))
			widget.card_held.connect(_on_card_held.bind(instance_id, catalog_id))
			_widget_cache[instance_id] = widget

		# Set progression info for level badge and XP bar
		var card_service: Node = get_node_or_null(CSharpAutoloads.PLAYER_CARD_SERVICE)
		if card_service:
			var prog_info: Variant = card_service.get_card_progression_info(instance_id)
			if prog_info is Dictionary:
				widget.set_progression(prog_info)

		# Ensure correct order by moving to end (builds order as we iterate)
		card_grid.move_child(widget, -1)


func _get_selected_deck_card_ids() -> Array[String]:
	var result: Array[String] = []
	if selected_deck_id == "":
		return result

	if not Decks.has_method("get_deck"):
		return result

	var deck_result: Variant = Decks.call("get_deck", selected_deck_id)
	if not deck_result is Dictionary:
		return result

	var card_ids: Array = deck_result.get("card_instance_ids", [])
	for card_id: Variant in card_ids:
		if card_id is String:
			result.append(card_id)

	return result


func _get_filtered_sorted_cards() -> Array:
	var result: Array = []

	for entry: Variant in collection_summary:
		if not entry is Dictionary:
			continue

		var catalog_id: String = entry.get("catalog_id", "")
		var catalog_data: Variant = CardCatalog.call("get_card", catalog_id)
		if not catalog_data is Dictionary or catalog_data.is_empty():
			continue

		# Apply type filter
		var card_type: int = catalog_data.get("card_type", 0)
		if card_type == 0 and not show_summons:  # 0 = SUMMON
			continue
		if card_type == 1 and not show_spells:  # 1 = SPELL
			continue

		var card_name: String = catalog_data.get("card_name", "")

		# Apply search filter
		if search_text != "" and not card_name.to_lower().contains(search_text.to_lower()):
			continue

		# Get element for filtering
		var card_element: String = "neutral"
		var categories: Dictionary = catalog_data.get("categories", {})
		var affinity: Variant = categories.get("elemental_affinity", null)
		if affinity and typeof(affinity) == TYPE_OBJECT and "id" in affinity:
			card_element = affinity.id

		# Apply element filter (if any elements selected, card must match one)
		if selected_elements.size() > 0 and card_element not in selected_elements:
			continue

		# Process each instance
		var instances: Array = entry.get("instances", [])
		for instance: Variant in instances:
			if not instance is Dictionary:
				continue

			var instance_id: String = instance.get("id", "")
			var level: int = 1
			# PlayerCardService is a C# autoload
			var card_service: Node = get_node_or_null(CSharpAutoloads.PLAYER_CARD_SERVICE)
			if not instance_id.is_empty() and card_service and card_service.has_method("get_card_progression_info"):
				var result_info: Variant = card_service.get_card_progression_info(instance_id)
				if result_info is Dictionary:
					level = result_info.get("level", 1)

			result.append({
				"instance_id": instance_id,
				"catalog_id": catalog_id,
				"card_data": instance,
				"catalog_data": catalog_data,
				"card_name": card_name,
				"mana_cost": catalog_data.get("mana_cost", 0),
				"rarity": String(catalog_data.get("rarity", "common")).to_lower(),
				"rarity_order": RARITY_ORDER.get(String(catalog_data.get("rarity", "common")).to_lower(), 0),
				"card_type": catalog_data.get("card_type", 0),
				"level": level,
				"element": card_element
			})

	# Sort
	result.sort_custom(_compare_cards)
	return result


func _compare_cards(a: Dictionary, b: Dictionary) -> bool:
	var compare_result: bool = false

	match current_sort_field:
		SortField.NAME:
			compare_result = a.card_name.to_lower() < b.card_name.to_lower()
		SortField.COST:
			compare_result = a.mana_cost < b.mana_cost
		SortField.RARITY:
			compare_result = a.rarity_order < b.rarity_order
		SortField.TYPE:
			compare_result = a.card_type < b.card_type
		SortField.LEVEL:
			compare_result = a.level < b.level
		SortField.ELEMENT:
			compare_result = a.element.to_lower() < b.element.to_lower()

	if not sort_ascending:
		compare_result = not compare_result

	return compare_result


## =============================================================================
## FILTER AND SORT
## =============================================================================

func _on_all_filter_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	# "All" selects both types
	show_summons = true
	show_spells = true
	_update_type_filter_buttons()
	_refresh_collection()


func _on_summon_filter_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	show_summons = not show_summons
	# Ensure at least one is selected
	if not show_summons and not show_spells:
		show_spells = true
	_update_type_filter_buttons()
	_refresh_collection()


func _on_spell_filter_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	show_spells = not show_spells
	# Ensure at least one is selected
	if not show_summons and not show_spells:
		show_summons = true
	_update_type_filter_buttons()
	_refresh_collection()


func _update_type_filter_buttons() -> void:
	# "All" is highlighted when both are selected
	var all_selected: bool = show_summons and show_spells
	all_button.button_pressed = all_selected
	all_button.disabled = all_selected

	# Individual buttons show their toggle state
	summon_button.button_pressed = show_summons
	spell_button.button_pressed = show_spells


func _populate_element_popup() -> void:
	element_popup.clear()
	# Add "All" option at index 0
	element_popup.add_check_item(Loc.t("ui.collection.filter_all_elements"), 0)
	element_popup.set_item_checked(0, true)
	# Add each element
	for i: int in range(ELEMENTS.size()):
		var element: String = ELEMENTS[i]
		element_popup.add_check_item(Loc.t("elements." + element), i + 1)
	_update_element_button_text()


func _on_element_filter_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	var button_rect: Rect2 = element_filter_button.get_global_rect()
	element_popup.position = Vector2i(int(button_rect.position.x), int(button_rect.position.y + button_rect.size.y))
	element_popup.popup()


func _on_element_toggled(id: int) -> void:
	if id == 0:
		# "All" was clicked - clear selection
		selected_elements.clear()
		for i: int in range(1, element_popup.item_count):
			element_popup.set_item_checked(i, false)
		element_popup.set_item_checked(0, true)
	else:
		# Specific element toggled
		var element_index: int = id - 1
		var element: String = ELEMENTS[element_index]
		var is_checked: bool = element_popup.is_item_checked(id)

		if is_checked:
			# Uncheck it
			element_popup.set_item_checked(id, false)
			selected_elements.erase(element)
		else:
			# Check it
			element_popup.set_item_checked(id, true)
			if element not in selected_elements:
				selected_elements.append(element)

		# Update "All" checkbox
		if selected_elements.is_empty():
			element_popup.set_item_checked(0, true)
		else:
			element_popup.set_item_checked(0, false)

	_update_element_button_text()
	_refresh_collection()


func _update_element_button_text() -> void:
	if selected_elements.is_empty():
		element_filter_button.text = Loc.t("ui.collection.filter_all_elements")
	elif selected_elements.size() == 1:
		element_filter_button.text = Loc.t("elements." + selected_elements[0])
	else:
		element_filter_button.text = Loc.t("ui.collection.element_count", {"count": selected_elements.size()})


func _populate_sort_dropdown() -> void:
	sort_dropdown.clear()
	sort_dropdown.add_item(Loc.t("ui.collection.sort_cost"))
	sort_dropdown.add_item(Loc.t("ui.collection.sort_name"))
	sort_dropdown.add_item(Loc.t("ui.collection.sort_rarity"))
	sort_dropdown.add_item(Loc.t("ui.collection.sort_type"))
	sort_dropdown.add_item(Loc.t("ui.collection.sort_level"))
	sort_dropdown.add_item(Loc.t("ui.collection.sort_element"))


func _on_sort_selected(index: int) -> void:
	match index:
		0: current_sort_field = SortField.COST
		1: current_sort_field = SortField.NAME
		2: current_sort_field = SortField.RARITY
		3: current_sort_field = SortField.TYPE
		4: current_sort_field = SortField.LEVEL
		5: current_sort_field = SortField.ELEMENT
	_refresh_collection()


func _on_search_changed(new_text: String) -> void:
	search_text = new_text
	_refresh_collection()


## =============================================================================
## CARD INTERACTIONS
## =============================================================================

func _on_collection_card_clicked(_card_data: Dictionary, widget: CardWidget, instance_id: String, catalog_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_dismiss_active_popup()

	# Check for double-click
	var current_time: int = Time.get_ticks_msec()
	if instance_id == last_clicked_card_id and current_time - last_click_time < DOUBLE_CLICK_THRESHOLD_MS:
		# Double-click: add to deck directly
		last_clicked_card_id = ""
		last_click_time = 0
		_add_card_to_selected_deck(instance_id)
		return

	# Single click: show popup
	last_clicked_card_id = instance_id
	last_click_time = current_time
	_show_card_action_popup(widget, instance_id, catalog_id)


func _on_card_held(_card_data: Dictionary, instance_id: String, catalog_id: String) -> void:
	_dismiss_active_popup()
	_open_card_detail_modal(instance_id, catalog_id)


func _show_card_action_popup(widget: CardWidget, instance_id: String, catalog_id: String) -> void:
	var popup: Control = CardActionPopupScene.instantiate()
	if not popup:
		return

	add_child(popup)
	active_popup = popup

	# Position below the widget
	var widget_rect: Rect2 = widget.get_global_rect()
	var popup_pos: Vector2 = Vector2(
		widget_rect.position.x + widget_rect.size.x / 2,
		widget_rect.position.y + widget_rect.size.y + 5
	)

	if popup.has_method("show_at"):
		var is_in_deck: bool = instance_id in _get_selected_deck_card_ids()
		popup.call("show_at", popup_pos, instance_id, catalog_id, is_in_deck, selected_deck_id != "")

	if popup.has_signal("use_pressed"):
		popup.use_pressed.connect(_on_popup_use_pressed)
	if popup.has_signal("info_pressed"):
		popup.info_pressed.connect(_on_popup_info_pressed)
	if popup.has_signal("dismissed"):
		popup.dismissed.connect(_on_popup_dismissed)


func _dismiss_active_popup() -> void:
	if active_popup and is_instance_valid(active_popup):
		active_popup.queue_free()
	active_popup = null


func _on_popup_use_pressed(instance_id: String, _catalog_id: String) -> void:
	_dismiss_active_popup()
	_add_card_to_selected_deck(instance_id)


func _on_popup_info_pressed(instance_id: String, catalog_id: String) -> void:
	_dismiss_active_popup()
	_open_card_detail_modal(instance_id, catalog_id)


func _on_popup_dismissed() -> void:
	_dismiss_active_popup()


## =============================================================================
## DECK OPERATIONS
## =============================================================================

func _add_card_to_selected_deck(card_instance_id: String) -> void:
	if selected_deck_id == "":
		return

	var deck_card_ids: Array[String] = _get_selected_deck_card_ids()
	if deck_card_ids.size() >= MAX_DECK_SIZE:
		return

	if Decks.has_method("add_card_to_deck"):
		var success: Variant = Decks.call("add_card_to_deck", selected_deck_id, card_instance_id)
		if success is bool and success:
			_refresh_deck_list()
			_refresh_deck_panel()
			_refresh_collection()


## =============================================================================
## CARD DETAIL MODAL
## =============================================================================

func _open_card_detail_modal(instance_id: String, catalog_id: String) -> void:
	var modal: Node = CardDetailModalScene.instantiate()
	if not modal:
		return

	add_child(modal)

	if modal.has_method("open_for_card"):
		modal.call("open_for_card", instance_id, catalog_id)

	# Set deck context so modal shows add/remove button
	var is_in_deck: bool = instance_id in _get_selected_deck_card_ids()
	if modal.has_method("set_deck_context"):
		modal.call("set_deck_context", selected_deck_id, is_in_deck)

	# Connect signals
	if modal.has_signal("level_up_requested"):
		modal.level_up_requested.connect(_on_level_up_from_modal)

	if modal.has_signal("deck_action_requested"):
		modal.deck_action_requested.connect(_on_deck_action_from_modal)

	if modal.has_signal("closed"):
		modal.closed.connect(_on_modal_closed.bind(modal))


func _on_level_up_from_modal(instance_id: String) -> void:
	var panel: Node = LevelUpPanelScene.instantiate()
	if not panel:
		return

	add_child(panel)

	if panel.has_method("open_for_card"):
		panel.call("open_for_card", instance_id)

	if panel.has_signal("level_up_completed"):
		panel.level_up_completed.connect(_on_level_up_completed)

	if panel.has_signal("cancelled"):
		panel.cancelled.connect(_on_modal_closed.bind(panel))


func _on_level_up_completed(_card_instance_id: String) -> void:
	_refresh_collection()
	_refresh_deck_list()


func _on_deck_action_from_modal(instance_id: String, action: String) -> void:
	if action == "add":
		_add_card_to_selected_deck(instance_id)
	elif action == "remove":
		_remove_card_from_deck(instance_id)


func _remove_card_from_deck(card_instance_id: String) -> void:
	if selected_deck_id == "":
		return

	if Decks.has_method("remove_card_from_deck"):
		var success: Variant = Decks.call("remove_card_from_deck", selected_deck_id, card_instance_id)
		if success is bool and success:
			_refresh_deck_list()
			_refresh_deck_panel()
			_refresh_collection()


func _on_modal_closed(modal: Node) -> void:
	if modal and is_instance_valid(modal):
		modal.queue_free()


## =============================================================================
## GOLD DISPLAY
## =============================================================================

func _refresh_gold_display() -> void:
	if ProfileRepo.has_method("get_gold"):
		var gold: Variant = ProfileRepo.call("get_gold")
		if gold is int:
			gold_label.text = str(gold)


## =============================================================================
## NAVIGATION
## =============================================================================

func _on_close_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_CAMPAIGN_MAP
	SceneManager.transition_to(return_scene)


## =============================================================================
## SERVICE SIGNALS
## =============================================================================

func _on_deck_changed(deck_id: String) -> void:
	if deck_id == selected_deck_id:
		_refresh_deck_list()
		_refresh_deck_panel()
		_refresh_collection()


func _on_deck_created(deck_id: String) -> void:
	# Select the newly created deck
	selected_deck_id = deck_id
	_refresh_deck_list()
	_refresh_deck_panel()
	_refresh_collection()


func _on_deck_deleted(deck_id: String) -> void:
	if selected_deck_id == deck_id:
		selected_deck_id = ""
	_refresh_deck_list()
	_refresh_deck_panel()
	_refresh_collection()


func _on_collection_changed() -> void:
	_refresh_collection()
	_refresh_deck_panel()
