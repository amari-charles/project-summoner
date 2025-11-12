extends Control
class_name DeckBuilder

## DeckBuilder - Create and edit decks with click-to-move or drag-and-drop
##
## Features:
## - Multi-deck support with dropdown selector
## - Click card in collection → instantly add to deck
## - Click card in deck → instantly remove from deck
## - Hold card (0.5s) → show detailed popup
## - Drag cards also works for add/remove
## - Real-time validation feedback
## - Auto-save on changes

## Node references
@onready var back_button: Button = %BackButton
@onready var deck_selector: OptionButton = %DeckSelector
@onready var new_deck_button: Button = %NewDeckButton
@onready var delete_deck_button: Button = %DeleteDeckButton
@onready var set_active_button: Button = %SetActiveButton
@onready var deck_name_edit: LineEdit = %DeckNameEdit
@onready var card_count_label: Label = %CardCount
@onready var validation_label: Label = %ValidationLabel
@onready var collection_grid: GridContainer = %CollectionGrid
@onready var deck_grid: GridContainer = %DeckGrid
@onready var new_deck_dialog: AcceptDialog = %NewDeckDialog
@onready var deck_name_input: LineEdit = %DeckNameInput
@onready var confirm_delete_dialog: ConfirmationDialog = %ConfirmDeleteDialog

## Card detail popup
@onready var card_detail_popup: PopupPanel = %CardDetailPopup
@onready var popup_card_name: Label = %CardNameLabel
@onready var popup_rarity: Label = %RarityLabel
@onready var popup_type: Label = %TypeLabel
@onready var popup_cost: Label = %CostLabel
@onready var popup_description: Label = %DescriptionLabel
@onready var popup_action: Label = %ActionLabel
@onready var popup_close_button: Button = %CloseButton

## State
var current_deck_id: String = ""
var current_deck_data: Dictionary = {}
var deck_card_ids: Array = []  # Array of card_instance_ids in current deck
var collection_summary: Array = []
var deck_editing_locked: bool = false  # Tutorial mode lock

## Card widget scene
const CardWidgetScene = preload("res://scenes/ui/card_widget.tscn")

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("DeckBuilder: Initializing...")

	# Connect buttons
	back_button.pressed.connect(_on_back_pressed)
	new_deck_button.pressed.connect(_on_new_deck_pressed)
	delete_deck_button.pressed.connect(_on_delete_deck_pressed)
	set_active_button.pressed.connect(_on_set_active_pressed)
	deck_selector.item_selected.connect(_on_deck_selected)
	deck_name_edit.text_submitted.connect(_on_deck_name_changed)

	# Connect dialogs
	new_deck_dialog.confirmed.connect(_on_new_deck_confirmed)
	confirm_delete_dialog.confirmed.connect(_on_delete_confirmed)
	popup_close_button.pressed.connect(_on_popup_close_pressed)

	# Connect to services
	var decks = get_node("/root/Decks")
	if decks:
		decks.deck_changed.connect(_on_deck_changed)
		decks.deck_created.connect(_on_deck_created)
		decks.deck_deleted.connect(_on_deck_deleted)

	var collection = get_node("/root/Collection")
	if collection:
		collection.collection_changed.connect(_on_collection_changed)

	# Enable drag-and-drop for deck panel
	deck_grid.set_drag_forwarding(
		Callable(),  # No custom _get_drag_data needed
		_can_drop_data_on_deck,
		_drop_data_on_deck
	)

	# Check if deck editing is locked (tutorial not complete)
	var campaign = get_node("/root/Campaign")
	if campaign:
		deck_editing_locked = not campaign.is_tutorial_complete()
		if deck_editing_locked:
			print("DeckBuilder: Deck editing LOCKED - tutorial not complete")
			_setup_locked_ui()
		else:
			print("DeckBuilder: Deck editing unlocked")

	# Debug: Show profile meta on startup
	var profile_repo: Node = get_node("/root/ProfileRepo")
	var deck_to_edit: String = ""
	if profile_repo:
		var profile: Dictionary = profile_repo.get_active_profile()
		if not profile.is_empty():
			var selected: Variant = profile.get("meta", {}).get("selected_deck", null)
			deck_to_edit = profile.get("meta", {}).get("editing_deck_id", "")
			print("DeckBuilder: Profile meta at startup:")
			print("  selected_deck: '%s' (type: %s)" % [selected, typeof(selected)])
			print("  editing_deck_id: '%s'" % deck_to_edit)

			# Clear the editing_deck_id so it doesn't persist
			if deck_to_edit != "":
				profile["meta"]["editing_deck_id"] = ""

	# Load decks and collection
	_refresh_deck_list_only()  # Just populate the selector, don't auto-load

	# If we have a specific deck to edit, load it; otherwise load first
	if deck_to_edit != "":
		print("DeckBuilder: Loading requested deck: %s" % deck_to_edit)
		_load_deck(deck_to_edit)
	elif deck_selector.item_count > 0:
		print("DeckBuilder: No specific deck requested, loading first deck")
		var first_deck_id: String = deck_selector.get_item_metadata(0)
		_load_deck(first_deck_id)

	_refresh_collection()

## =============================================================================
## DECK LOADING
## =============================================================================

func _refresh_deck_list_only() -> void:
	var decks: Node = get_node("/root/Decks")
	if not decks:
		push_error("DeckBuilder: Decks service not found!")
		return

	var deck_list: Array = decks.list_decks()

	# Clear selector
	deck_selector.clear()

	# Add decks to selector
	for deck in deck_list:
		var deck_id: String = deck.get("id", "")
		var deck_name: String = deck.get("name", "Unnamed Deck")
		deck_selector.add_item(deck_name)
		deck_selector.set_item_metadata(deck_selector.item_count - 1, deck_id)

	# Create default deck if none exist
	if deck_list.size() == 0:
		print("DeckBuilder: No decks found, creating default deck...")
		var deck_id: String = decks.create_deck("My Deck", [])
		_refresh_deck_list_only()  # Refresh after creation
		return

func _load_deck(deck_id: String) -> void:
	var decks: Node = get_node("/root/Decks")
	if not decks:
		return

	current_deck_id = deck_id
	current_deck_data = decks.get_deck(deck_id)

	if current_deck_data.is_empty():
		push_error("DeckBuilder: Deck not found: %s" % deck_id)
		return

	# Update UI
	deck_name_edit.text = current_deck_data.get("name", "Unnamed Deck")
	deck_card_ids = current_deck_data.get("card_instance_ids", [])

	# Check if this is the active deck
	_update_active_deck_button()

	# Refresh both collection and deck displays
	# Collection needs refresh because different cards are now in deck
	_refresh_collection()
	_refresh_deck_display()
	_update_validation()

	# Debug: Check for duplicate cards
	var unique_ids: Dictionary = {}
	for card_id in deck_card_ids:
		if unique_ids.has(card_id):
			push_warning("DeckBuilder: DUPLICATE card instance in deck: %s" % card_id)
		unique_ids[card_id] = true

	print("DeckBuilder: Loaded deck '%s' (%d cards, %d unique)" % [current_deck_data.get("name"), deck_card_ids.size(), unique_ids.size()])

## =============================================================================
## COLLECTION DISPLAY
## =============================================================================

func _refresh_collection() -> void:
	var collection: Node = get_node("/root/Collection")
	if not collection:
		push_error("DeckBuilder: Collection service not found!")
		return

	var catalog: Node = get_node("/root/CardCatalog")
	if not catalog:
		push_error("DeckBuilder: CardCatalog not found!")
		return

	# Get collection summary
	collection_summary = collection.get_collection_summary()

	# Clear grid
	for child in collection_grid.get_children():
		child.queue_free()

	# Create card widgets - show each instance individually
	# BUT: Hide cards that are already in the current deck
	var total_widgets: int = 0
	var hidden_count: int = 0
	for entry in collection_summary:
		var instances: Array = entry.instances
		var catalog_data: Dictionary = catalog.get_card(entry.catalog_id)

		if catalog_data.is_empty():
			continue

		# Create a widget for EACH individual card instance
		for card_data in instances:
			var instance_id: String = card_data.get("id", "")

			# Skip cards that are already in deck
			if instance_id in deck_card_ids:
				hidden_count += 1
				continue

			var widget: CardWidget = CardWidgetScene.instantiate()
			collection_grid.add_child(widget)

			# Set card data
			widget.set_card(card_data, catalog_data)
			widget.set_draggable(true)  # Enable drag from collection

			# Connect click to add to deck, hold to show details
			widget.card_clicked.connect(_on_collection_card_clicked.bind(instance_id))
			widget.card_held.connect(_on_collection_card_held.bind(instance_id))

			total_widgets += 1

	print("DeckBuilder: Showing %d available cards (%d in deck)" % [total_widgets, hidden_count])

## =============================================================================
## DECK DISPLAY
## =============================================================================

func _refresh_deck_display() -> void:
	var catalog: Node = get_node("/root/CardCatalog")
	var collection: Node = get_node("/root/Collection")
	if not catalog or not collection:
		return

	# Clear grid
	for child in deck_grid.get_children():
		child.queue_free()

	# Show each card instance individually
	for card_instance_id in deck_card_ids:
		var card_data: Dictionary = collection.get_card(card_instance_id)
		if card_data.is_empty():
			continue

		var catalog_id: String = card_data.get("catalog_id", "")
		var catalog_data: Dictionary = catalog.get_card(catalog_id)

		if catalog_data.is_empty():
			continue

		# Create widget for this individual instance
		var widget: CardWidget = CardWidgetScene.instantiate()
		deck_grid.add_child(widget)

		# Set card data
		widget.set_card(card_data, catalog_data)
		widget.set_draggable(false)  # No drag within deck

		# Connect click to remove, hold to show details (pass specific instance ID)
		widget.card_clicked.connect(_on_deck_card_instance_clicked.bind(card_instance_id))
		widget.card_held.connect(_on_deck_card_held.bind(card_instance_id))

	# Update card count
	card_count_label.text = "%d / 30" % deck_card_ids.size()

	print("DeckBuilder: Displaying deck with %d individual cards" % deck_card_ids.size())

## =============================================================================
## DRAG AND DROP
## =============================================================================

func _can_drop_data_on_deck(_at_position: Vector2, data: Variant) -> bool:
	# Check if editing is locked
	if deck_editing_locked:
		return false

	if not data is Dictionary:
		return false

	if data.get("type") != "card":
		return false

	# Check if deck has room
	return deck_card_ids.size() < 30

func _drop_data_on_deck(_at_position: Vector2, data: Variant) -> void:
	if not data is Dictionary:
		return

	var card_data: Dictionary = data.get("card_data", {})
	var card_instance_id: String = card_data.get("id", "")

	if card_instance_id == "":
		push_warning("DeckBuilder: Invalid card data for drop")
		return

	# Add card to deck
	_add_card_to_deck(card_instance_id)

func _add_card_to_deck(card_instance_id: String) -> void:
	# Check if editing is locked
	if deck_editing_locked:
		_show_locked_message()
		return

	if deck_card_ids.size() >= 30:
		push_warning("DeckBuilder: Deck is full!")
		return

	if current_deck_id == "":
		push_warning("DeckBuilder: No deck selected!")
		return

	var decks: Node = get_node("/root/Decks")
	if not decks:
		return

	var success: bool = decks.add_card_to_deck(current_deck_id, card_instance_id)

	if success:
		print("DeckBuilder: Added card to deck")
		# Deck will be reloaded via signal
	else:
		push_warning("DeckBuilder: Failed to add card to deck")

## =============================================================================
## DECK EDITING
## =============================================================================

func _on_collection_card_clicked(_card_data: Dictionary, card_instance_id: String) -> void:
	# Check if editing is locked
	if deck_editing_locked:
		_show_locked_message()
		return

	# Quick click - instantly add to deck
	_add_card_to_deck(card_instance_id)

func _on_collection_card_held(_card_data: Dictionary, card_instance_id: String) -> void:
	# Hold - show card details
	_show_card_details(card_instance_id, true)  # true = from collection

func _on_deck_card_instance_clicked(_card_data: Dictionary, card_instance_id: String) -> void:
	# Check if editing is locked
	if deck_editing_locked:
		_show_locked_message()
		return

	# Quick click - instantly remove from deck
	_remove_card_from_deck(card_instance_id)

func _on_deck_card_held(_card_data: Dictionary, card_instance_id: String) -> void:
	# Hold - show card details
	_show_card_details(card_instance_id, false)  # false = from deck

func _remove_card_from_deck(card_instance_id: String) -> void:
	# Check if editing is locked
	if deck_editing_locked:
		_show_locked_message()
		return

	if current_deck_id == "":
		return

	var decks: Node = get_node("/root/Decks")
	if not decks:
		return

	var success: bool = decks.remove_card_from_deck(current_deck_id, card_instance_id)

	if success:
		print("DeckBuilder: Removed card from deck")
		# Deck will be reloaded via signal
	else:
		push_warning("DeckBuilder: Failed to remove card from deck")

func _on_deck_name_changed(new_name: String) -> void:
	if current_deck_id == "" or new_name == "":
		return

	var decks = get_node("/root/Decks")
	if not decks:
		return

	decks.update_deck(current_deck_id, new_name, [])  # [] = keep existing cards

	print("DeckBuilder: Renamed deck to '%s'" % new_name)

## =============================================================================
## VALIDATION
## =============================================================================

func _update_validation() -> void:
	var decks: Node = get_node("/root/Decks")
	if not decks or current_deck_id == "":
		return

	var errors: Array = decks.get_validation_errors(current_deck_id)

	if errors.size() == 0:
		validation_label.text = "✓ Deck is valid and ready for battle!"
		validation_label.add_theme_color_override("font_color", Color(0.3, 1.0, 0.3))
	else:
		validation_label.text = "⚠ " + errors[0]  # Show first error
		validation_label.add_theme_color_override("font_color", Color(1.0, 0.3, 0.3))

## =============================================================================
## DECK MANAGEMENT
## =============================================================================

func _on_deck_selected(index: int) -> void:
	var deck_id: String = deck_selector.get_item_metadata(index)
	_load_deck(deck_id)

func _on_new_deck_pressed() -> void:
	deck_name_input.text = ""
	new_deck_dialog.popup_centered()

func _on_new_deck_confirmed() -> void:
	var deck_name: String = deck_name_input.text
	if deck_name == "":
		deck_name = "New Deck"

	var decks: Node = get_node("/root/Decks")
	if not decks:
		return

	var deck_id: String = decks.create_deck(deck_name, [])
	print("DeckBuilder: Created new deck '%s' (%s)" % [deck_name, deck_id])

func _on_delete_deck_pressed() -> void:
	if current_deck_id == "":
		return

	confirm_delete_dialog.popup_centered()

func _on_set_active_pressed() -> void:
	if current_deck_id == "":
		push_warning("DeckBuilder: No deck selected to set as active")
		return

	# Set this deck as the active deck in profile
	var profile_repo: Node = get_node("/root/ProfileRepo")
	if profile_repo:
		var profile: Dictionary = profile_repo.get_active_profile()
		if not profile.is_empty():
			var old_active: String = profile.get("meta", {}).get("selected_deck", "")
			print("DeckBuilder: _on_set_active_pressed()")
			print("  - Setting selected_deck to: '%s' (type: %s)" % [current_deck_id, typeof(current_deck_id)])
			print("  - Old value was: '%s'" % old_active)
			profile["meta"]["selected_deck"] = current_deck_id
			profile_repo.save_profile(true)  # Force immediate save
			print("  - Saved! New value: '%s'" % profile.get("meta", {}).get("selected_deck", ""))

			_update_active_deck_button()
			if old_active != "":
				print("DeckBuilder: Changed active deck from '%s' to '%s'" % [old_active, current_deck_id])
			else:
				print("DeckBuilder: Set deck '%s' as active (saved immediately)" % current_deck_data.get("name", current_deck_id))

func _update_active_deck_button() -> void:
	var profile_repo: Node = get_node("/root/ProfileRepo")
	if not profile_repo:
		return

	var profile: Dictionary = profile_repo.get_active_profile()
	if profile.is_empty():
		return

	var active_deck_id: String = profile.get("meta", {}).get("selected_deck", "")

	# Debug logging
	print("DeckBuilder: _update_active_deck_button()")
	print("  - Current deck ID: %s" % current_deck_id)
	print("  - Active deck ID from profile: %s" % active_deck_id)
	print("  - Type of active_deck_id: %s" % typeof(active_deck_id))
	print("  - Match: %s" % (active_deck_id == current_deck_id))

	if active_deck_id == current_deck_id:
		set_active_button.text = "✓ ACTIVE DECK"
		set_active_button.disabled = true
	else:
		set_active_button.text = "SET AS ACTIVE"
		set_active_button.disabled = false

func _on_delete_confirmed() -> void:
	if current_deck_id == "":
		return

	var decks: Node = get_node("/root/Decks")
	if not decks:
		return

	var success: bool = decks.delete_deck(current_deck_id)

	if success:
		print("DeckBuilder: Deleted deck")
		current_deck_id = ""
		_refresh_deck_list_only()
		# Load first available deck
		if deck_selector.item_count > 0:
			var first_deck_id: String = deck_selector.get_item_metadata(0)
			_load_deck(first_deck_id)

## =============================================================================
## CARD DETAIL POPUP
## =============================================================================

func _show_card_details(card_instance_id: String, from_collection: bool) -> void:
	var collection: Node = get_node("/root/Collection")
	var catalog: Node = get_node("/root/CardCatalog")
	if not collection or not catalog:
		return

	# Get card instance and catalog data
	var card_data: Dictionary = collection.get_card(card_instance_id)
	if card_data.is_empty():
		return

	var catalog_id: String = card_data.get("catalog_id", "")
	var catalog_data: Dictionary = catalog.get_card(catalog_id)
	if catalog_data.is_empty():
		return

	# Update popup labels
	popup_card_name.text = catalog_data.get("card_name", "Unknown")
	popup_rarity.text = "Rarity: %s" % catalog_data.get("rarity", "common").capitalize()

	var card_type: int = catalog_data.get("card_type", 0)
	popup_type.text = "Type: %s" % ("Summon" if card_type == 0 else "Spell")

	popup_cost.text = "Cost: %d Mana" % catalog_data.get("mana_cost", 0)
	popup_description.text = catalog_data.get("description", "No description.")

	# Update action label based on source
	if from_collection:
		popup_action.text = "Click card to ADD to deck"
	else:
		popup_action.text = "Click card to REMOVE from deck"

	# Show popup
	card_detail_popup.popup_centered()

	print("DeckBuilder: Showing details for card: %s" % card_instance_id)

func _on_popup_close_pressed() -> void:
	card_detail_popup.hide()

## =============================================================================
## NAVIGATION
## =============================================================================

func _on_back_pressed() -> void:
	print("DeckBuilder: Returning to collection")
	get_tree().change_scene_to_file("res://scenes/ui/collection_screen.tscn")

## =============================================================================
## SIGNALS
## =============================================================================

func _on_deck_changed(deck_id: String) -> void:
	if deck_id == current_deck_id:
		print("DeckBuilder: Current deck changed, reloading...")
		_load_deck(deck_id)

func _on_deck_created(deck_id: String) -> void:
	print("DeckBuilder: Deck created, refreshing list...")
	_refresh_deck_list_only()
	# Select the newly created deck
	for i in range(deck_selector.item_count):
		var meta_deck_id: String = deck_selector.get_item_metadata(i)
		if meta_deck_id == deck_id:
			deck_selector.select(i)
			_load_deck(deck_id)
			break

func _on_deck_deleted(_deck_id: String) -> void:
	print("DeckBuilder: Deck deleted, refreshing list...")
	_refresh_deck_list_only()
	# Load first available deck if any exist
	if deck_selector.item_count > 0:
		var first_deck_id: String = deck_selector.get_item_metadata(0)
		_load_deck(first_deck_id)

func _on_collection_changed() -> void:
	print("DeckBuilder: Collection changed, refreshing...")
	_refresh_collection()
	_refresh_deck_display()  # In case deck cards changed

## =============================================================================
## DECK EDITING LOCK (TUTORIAL MODE)
## =============================================================================

func _setup_locked_ui() -> void:
	# Show lock message in validation label
	validation_label.text = "🔒 DECK LOCKED - Complete tutorial battles to unlock editing"
	validation_label.add_theme_color_override("font_color", Color(1.0, 0.8, 0.3))  # Yellow/orange

	# Disable deck management buttons
	new_deck_button.disabled = true
	delete_deck_button.disabled = true
	deck_selector.disabled = true
	deck_name_edit.editable = false

func _show_locked_message() -> void:
	# Show temporary notification that editing is locked
	print("DeckBuilder: User attempted edit while locked")
	validation_label.text = "🔒 Complete tutorial battles to unlock deck editing"
	validation_label.add_theme_color_override("font_color", Color(1.0, 0.5, 0.3))  # Orange

	# Reset to normal lock message after 2 seconds
	await get_tree().create_timer(2.0).timeout
	if deck_editing_locked:
		validation_label.text = "🔒 DECK LOCKED - Complete tutorial battles to unlock editing"
		validation_label.add_theme_color_override("font_color", Color(1.0, 0.8, 0.3))
