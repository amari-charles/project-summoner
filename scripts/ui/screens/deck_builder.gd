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

## Summoner selection UI (optional - add to scene if not present)
@onready var summoner_selector: OptionButton = get_node_or_null("%SummonerSelector")
@onready var summoner_stats_label: Label = get_node_or_null("%SummonerStatsLabel")

## State
var current_deck_id: String = ""
var current_deck_data: Dictionary = {}
var deck_card_ids: Array[String] = []  # Array of card_instance_ids in current deck
var collection_summary: Array = []  # Array of collection entry dictionaries
var deck_editing_locked: bool = false  # Tutorial mode lock

## Card widget scene
const CardWidgetScene: PackedScene = preload("res://scenes/ui/components/card_widget.tscn")

## Duration to show temporary lock message before resetting (seconds)
const LOCK_MESSAGE_DURATION: float = 2.0

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

	# Connect summoner selector if present
	if summoner_selector:
		summoner_selector.item_selected.connect(_on_summoner_selected)
		_populate_summoner_selector()

	# Connect to services
	var decks: Node = get_node("/root/Decks")
	if decks and decks.has_signal("deck_changed") and decks.has_signal("deck_created") and decks.has_signal("deck_deleted"):
		var deck_changed_sig: Signal = decks.deck_changed
		var deck_created_sig: Signal = decks.deck_created
		var deck_deleted_sig: Signal = decks.deck_deleted
		deck_changed_sig.connect(_on_deck_changed)
		deck_created_sig.connect(_on_deck_created)
		deck_deleted_sig.connect(_on_deck_deleted)

	var collection: Node = get_node("/root/Collection")
	if collection and collection.has_signal("collection_changed"):
		var collection_changed_sig: Signal = collection.collection_changed
		collection_changed_sig.connect(_on_collection_changed)

	# Enable drag-and-drop for deck panel
	deck_grid.set_drag_forwarding(
		Callable(),  # No custom _get_drag_data needed
		_can_drop_data_on_deck,
		_drop_data_on_deck
	)

	# Check if deck editing is locked (tutorial not complete)
	var campaign: Node = get_node("/root/Campaign")
	if campaign and campaign.has_method("is_tutorial_complete"):
		var is_complete: Variant = campaign.call("is_tutorial_complete")
		if is_complete is bool:
			deck_editing_locked = not is_complete
			if deck_editing_locked:
				print("DeckBuilder: Deck editing LOCKED - tutorial not complete")
				_setup_locked_ui()
			else:
				print("DeckBuilder: Deck editing unlocked")

	# Debug: Show profile meta on startup
	var profile_repo: Node = get_node("/root/ProfileRepo")
	var deck_to_edit: String = ""
	if profile_repo and profile_repo.has_method("get_active_profile") and profile_repo.has_method("save_profile"):
		var profile_result: Variant = profile_repo.call("get_active_profile")
		if profile_result is Dictionary:
			var profile: Dictionary = profile_result
			if not profile.is_empty():
				var empty_meta_startup: Dictionary = {}
				var meta_val: Variant = profile.get("meta", empty_meta_startup)
				if meta_val is Dictionary:
					var meta: Dictionary = meta_val
					var selected: Variant = meta.get("selected_deck", null)
					var editing: Variant = meta.get("editing_deck_id", "")
					if editing is String:
						deck_to_edit = editing
					print("DeckBuilder: Profile meta at startup:")
					print("  selected_deck: '%s' (type: %s)" % [selected, typeof(selected)])
					print("  editing_deck_id: '%s'" % deck_to_edit)

					# Clear the editing_deck_id so it doesn't persist
					if deck_to_edit != "":
						meta["editing_deck_id"] = ""
						profile_repo.call("save_profile", false)

	# Load decks and collection
	_refresh_deck_list_only()  # Just populate the selector, don't auto-load

	# If we have a specific deck to edit, load it; otherwise load first
	if deck_to_edit != "":
		print("DeckBuilder: Loading requested deck: %s" % deck_to_edit)
		_load_deck(deck_to_edit)
	elif deck_selector.item_count > 0:
		print("DeckBuilder: No specific deck requested, loading first deck")
		var first_deck_meta: Variant = deck_selector.get_item_metadata(0)
		if first_deck_meta is String:
			_load_deck(first_deck_meta)

	_refresh_collection()

## =============================================================================
## DECK LOADING
## =============================================================================

func _refresh_deck_list_only() -> void:
	var decks: Node = get_node("/root/Decks")
	if not decks or not decks.has_method("list_decks") or not decks.has_method("create_deck"):
		push_error("DeckBuilder: Decks service not found!")
		return

	var deck_list_result: Variant = decks.call("list_decks")
	if not deck_list_result is Array:
		return

	var deck_list: Array = deck_list_result

	# Clear selector
	deck_selector.clear()

	# Add decks to selector
	for deck_item: Variant in deck_list:
		if not deck_item is Dictionary:
			continue
		var deck: Dictionary = deck_item
		var deck_id_val: Variant = deck.get("id", "")
		var deck_name_val: Variant = deck.get("name", "Unnamed Deck")
		if deck_id_val is String and deck_name_val is String:
			var deck_id: String = deck_id_val
			var deck_name: String = deck_name_val
			deck_selector.add_item(deck_name)
			deck_selector.set_item_metadata(deck_selector.item_count - 1, deck_id)

	# Create default deck if none exist
	if deck_list.size() == 0:
		print("DeckBuilder: No decks found, creating default deck...")
		var deck_id_result: Variant = decks.call("create_deck", "My Deck", [])
		_refresh_deck_list_only()  # Refresh after creation
		return

func _load_deck(deck_id: String) -> void:
	var decks: Node = get_node("/root/Decks")
	if not decks or not decks.has_method("get_deck"):
		return

	current_deck_id = deck_id
	var deck_result: Variant = decks.call("get_deck", deck_id)
	if not deck_result is Dictionary:
		return

	current_deck_data = deck_result

	if current_deck_data.is_empty():
		push_error("DeckBuilder: Deck not found: %s" % deck_id)
		return

	# Update UI
	var name_val: Variant = current_deck_data.get("name", "Unnamed Deck")
	if name_val is String:
		deck_name_edit.text = name_val

	var card_ids_val: Variant = current_deck_data.get("card_instance_ids", [])
	if card_ids_val is Array:
		deck_card_ids.clear()
		for id_item: Variant in card_ids_val:
			if id_item is String:
				deck_card_ids.append(id_item)

	# Check if this is the active deck
	_update_active_deck_button()

	# Refresh both collection and deck displays
	# Collection needs refresh because different cards are now in deck
	_refresh_collection()
	_refresh_deck_display()
	_update_validation()

	# Load and display summoner
	_load_deck_summoner()

	# Debug: Check for duplicate cards
	var unique_ids: Dictionary = {}
	for card_id: String in deck_card_ids:
		if unique_ids.has(card_id):
			push_warning("DeckBuilder: DUPLICATE card instance in deck: %s" % card_id)
		unique_ids[card_id] = true

	var deck_name_debug: Variant = current_deck_data.get("name")
	print("DeckBuilder: Loaded deck '%s' (%d cards, %d unique)" % [deck_name_debug, deck_card_ids.size(), unique_ids.size()])

## =============================================================================
## COLLECTION DISPLAY
## =============================================================================

func _refresh_collection() -> void:
	var collection: Node = get_node("/root/Collection")
	if not collection or not collection.has_method("get_collection_summary"):
		push_error("DeckBuilder: Collection service not found!")
		return

	var catalog: Node = get_node("/root/CardCatalog")
	if not catalog or not catalog.has_method("get_card"):
		push_error("DeckBuilder: CardCatalog not found!")
		return

	# Get collection summary
	var summary_result: Variant = collection.call("get_collection_summary")
	if not summary_result is Array:
		return
	collection_summary = summary_result

	# Clear grid
	for child: Node in collection_grid.get_children():
		child.queue_free()

	# Create card widgets - show each instance individually
	# BUT: Hide cards that are already in the current deck
	var total_widgets: int = 0
	var hidden_count: int = 0
	for entry_item: Variant in collection_summary:
		if not entry_item is Dictionary:
			continue
		var entry: Dictionary = entry_item
		var instances_val: Variant = entry.get("instances", [])
		var catalog_id_val: Variant = entry.get("catalog_id", "")

		if not instances_val is Array or not catalog_id_val is String:
			continue

		var instances: Array = instances_val
		var catalog_id: String = catalog_id_val
		var catalog_result: Variant = catalog.call("get_card", catalog_id)
		if not catalog_result is Dictionary:
			continue
		var catalog_data: Dictionary = catalog_result

		if catalog_data.is_empty():
			continue

		# Create a widget for EACH individual card instance
		for card_item: Variant in instances:
			if not card_item is Dictionary:
				continue
			var card_data: Dictionary = card_item
			var instance_id_val: Variant = card_data.get("id", "")
			if not instance_id_val is String:
				continue
			var instance_id: String = instance_id_val

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
	if not catalog or not collection or not catalog.has_method("get_card") or not collection.has_method("get_card"):
		return

	# Clear grid
	for child: Node in deck_grid.get_children():
		child.queue_free()

	# Show each card instance individually
	for card_instance_id: String in deck_card_ids:
		var card_result: Variant = collection.call("get_card", card_instance_id)
		if not card_result is Dictionary:
			continue
		var card_data: Dictionary = card_result
		if card_data.is_empty():
			continue

		var catalog_id_val: Variant = card_data.get("catalog_id", "")
		if not catalog_id_val is String:
			continue
		var catalog_id: String = catalog_id_val
		var catalog_result: Variant = catalog.call("get_card", catalog_id)
		if not catalog_result is Dictionary:
			continue
		var catalog_data: Dictionary = catalog_result

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
	card_count_label.text = Loc.t("ui.collection.card_count_format", {"count": deck_card_ids.size()})

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

	var data_dict: Dictionary = data
	var type_val: Variant = data_dict.get("type")
	if not type_val is String or type_val != "card":
		return false

	# Check if deck has room
	return deck_card_ids.size() < 30

func _drop_data_on_deck(_at_position: Vector2, data: Variant) -> void:
	if not data is Dictionary:
		return

	var data_dict: Dictionary = data
	var empty_dict: Dictionary = {}
	var card_data_val: Variant = data_dict.get("card_data", empty_dict)
	if not card_data_val is Dictionary:
		return

	var card_data: Dictionary = card_data_val
	var card_instance_id_val: Variant = card_data.get("id", "")
	if not card_instance_id_val is String:
		push_warning("DeckBuilder: Invalid card data for drop")
		return

	var card_instance_id: String = card_instance_id_val
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
	if not decks or not decks.has_method("add_card_to_deck"):
		return

	var success_result: Variant = decks.call("add_card_to_deck", current_deck_id, card_instance_id)
	if not success_result is bool:
		return

	var success: bool = success_result
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
	if not decks or not decks.has_method("remove_card_from_deck"):
		return

	var success_result: Variant = decks.call("remove_card_from_deck", current_deck_id, card_instance_id)
	if not success_result is bool:
		return

	var success: bool = success_result
	if success:
		print("DeckBuilder: Removed card from deck")
		# Deck will be reloaded via signal
	else:
		push_warning("DeckBuilder: Failed to remove card from deck")

func _on_deck_name_changed(new_name: String) -> void:
	if current_deck_id == "" or new_name == "":
		return

	var decks: Node = get_node("/root/Decks")
	if not decks or not decks.has_method("update_deck"):
		return

	decks.call("update_deck", current_deck_id, new_name, [])  # [] = keep existing cards

	print("DeckBuilder: Renamed deck to '%s'" % new_name)

## =============================================================================
## VALIDATION
## =============================================================================

func _update_validation() -> void:
	var decks: Node = get_node("/root/Decks")
	if not decks or current_deck_id == "" or not decks.has_method("get_validation_errors"):
		return

	var errors_result: Variant = decks.call("get_validation_errors", current_deck_id)
	if not errors_result is Array:
		return

	var errors: Array = errors_result
	if errors.size() == 0:
		validation_label.text = Loc.t("ui.deck_builder.valid")
		validation_label.add_theme_color_override("font_color", Color(0.3, 1.0, 0.3))
	else:
		var first_error_val: Variant = errors[0]
		if first_error_val is String:
			validation_label.text = "⚠ " + first_error_val
		else:
			validation_label.text = Loc.t("ui.deck_builder.invalid")
		validation_label.add_theme_color_override("font_color", Color(1.0, 0.3, 0.3))

## =============================================================================
## DECK MANAGEMENT
## =============================================================================

func _on_deck_selected(index: int) -> void:
	var deck_meta: Variant = deck_selector.get_item_metadata(index)
	if deck_meta is String:
		_load_deck(deck_meta)

func _on_new_deck_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	deck_name_input.text = ""
	new_deck_dialog.popup_centered()

func _on_new_deck_confirmed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	var deck_name: String = deck_name_input.text
	if deck_name == "":
		deck_name = "New Deck"

	var decks: Node = get_node("/root/Decks")
	if not decks or not decks.has_method("create_deck"):
		return

	var deck_id_result: Variant = decks.call("create_deck", deck_name, [])
	if deck_id_result is String:
		print("DeckBuilder: Created new deck '%s' (%s)" % [deck_name, deck_id_result])

func _on_delete_deck_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if current_deck_id == "":
		return

	confirm_delete_dialog.popup_centered()

func _on_set_active_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if current_deck_id == "":
		push_warning("DeckBuilder: No deck selected to set as active")
		return

	# Set this deck as the active deck in profile
	var profile_repo: Node = get_node("/root/ProfileRepo")
	if profile_repo and profile_repo.has_method("get_active_profile") and profile_repo.has_method("save_profile"):
		var profile_result: Variant = profile_repo.call("get_active_profile")
		if not profile_result is Dictionary:
			return
		var profile: Dictionary = profile_result
		if not profile.is_empty():
			var empty_dict: Dictionary = {}
			var meta_val: Variant = profile.get("meta", empty_dict)
			if not meta_val is Dictionary:
				return
			var meta: Dictionary = meta_val
			var old_active_val: Variant = meta.get("selected_deck", "")
			var old_active: String = ""
			if old_active_val is String:
				old_active = old_active_val
			print("DeckBuilder: _on_set_active_pressed()")
			print("  - Setting selected_deck to: '%s' (type: %s)" % [current_deck_id, typeof(current_deck_id)])
			print("  - Old value was: '%s'" % old_active)
			meta["selected_deck"] = current_deck_id
			profile_repo.call("save_profile", true)  # Force immediate save
			var new_selected_val: Variant = meta.get("selected_deck", "")
			print("  - Saved! New value: '%s'" % new_selected_val)

			_update_active_deck_button()
			if old_active != "":
				print("DeckBuilder: Changed active deck from '%s' to '%s'" % [old_active, current_deck_id])
			else:
				var deck_name_val: Variant = current_deck_data.get("name", current_deck_id)
				print("DeckBuilder: Set deck '%s' as active (saved immediately)" % deck_name_val)

func _update_active_deck_button() -> void:
	var profile_repo: Node = get_node("/root/ProfileRepo")
	if not profile_repo or not profile_repo.has_method("get_active_profile"):
		return

	var profile_result: Variant = profile_repo.call("get_active_profile")
	if not profile_result is Dictionary:
		return
	var profile: Dictionary = profile_result
	if profile.is_empty():
		return

	var empty_dict: Dictionary = {}
	var meta_val: Variant = profile.get("meta", empty_dict)
	if not meta_val is Dictionary:
		return
	var meta: Dictionary = meta_val
	var active_deck_id_val: Variant = meta.get("selected_deck", "")
	var active_deck_id: String = ""
	if active_deck_id_val is String:
		active_deck_id = active_deck_id_val

	# Debug logging
	print("DeckBuilder: _update_active_deck_button()")
	print("  - Current deck ID: %s" % current_deck_id)
	print("  - Active deck ID from profile: %s" % active_deck_id)
	print("  - Type of active_deck_id: %s" % typeof(active_deck_id))
	print("  - Match: %s" % (active_deck_id == current_deck_id))

	if active_deck_id == current_deck_id:
		set_active_button.text = Loc.t("ui.deck_builder.active_deck")
		set_active_button.disabled = true
	else:
		set_active_button.text = Loc.t("ui.deck_builder.set_active")
		set_active_button.disabled = false

func _on_delete_confirmed() -> void:
	if current_deck_id == "":
		return

	var decks: Node = get_node("/root/Decks")
	if not decks or not decks.has_method("delete_deck"):
		return

	var success_result: Variant = decks.call("delete_deck", current_deck_id)
	if not success_result is bool:
		return

	var success: bool = success_result
	if success:
		print("DeckBuilder: Deleted deck")
		current_deck_id = ""
		_refresh_deck_list_only()
		# Load first available deck
		if deck_selector.item_count > 0:
			var first_deck_meta: Variant = deck_selector.get_item_metadata(0)
			if first_deck_meta is String:
				_load_deck(first_deck_meta)

## =============================================================================
## CARD DETAIL POPUP
## =============================================================================

func _show_card_details(card_instance_id: String, from_collection: bool) -> void:
	var collection: Node = get_node("/root/Collection")
	var catalog: Node = get_node("/root/CardCatalog")
	if not collection or not catalog or not collection.has_method("get_card") or not catalog.has_method("get_card"):
		return

	# Get card instance and catalog data
	var card_result: Variant = collection.call("get_card", card_instance_id)
	if not card_result is Dictionary:
		return
	var card_data: Dictionary = card_result
	if card_data.is_empty():
		return

	var catalog_id_val: Variant = card_data.get("catalog_id", "")
	if not catalog_id_val is String:
		return
	var catalog_id: String = catalog_id_val
	var catalog_result: Variant = catalog.call("get_card", catalog_id)
	if not catalog_result is Dictionary:
		return
	var catalog_data: Dictionary = catalog_result
	if catalog_data.is_empty():
		return

	# Update popup labels
	var card_name_val: Variant = catalog_data.get("card_name", "Unknown")
	if card_name_val is String:
		popup_card_name.text = card_name_val

	var rarity_val: Variant = catalog_data.get("rarity", "common")
	if rarity_val is String:
		var rarity_str: String = rarity_val
		popup_rarity.text = Loc.t("ui.collection.rarity_label", {"rarity": rarity_str.capitalize()})

	var card_type_val: Variant = catalog_data.get("card_type", Card.CardType.SUMMON)
	var card_type: int = int(card_type_val)  # Works for both int and enum values
	var type_str: String = Loc.t("ui.collection.type_summon") if card_type == Card.CardType.SUMMON else Loc.t("ui.collection.type_spell")
	popup_type.text = Loc.t("ui.collection.type_label", {"type": type_str})

	var mana_cost_val: Variant = catalog_data.get("mana_cost", 0)
	if mana_cost_val is int:
		popup_cost.text = Loc.t("ui.collection.cost_label", {"cost": mana_cost_val})

	var desc_val: Variant = catalog_data.get("description", "")
	if desc_val is String and not desc_val.is_empty():
		popup_description.text = desc_val
	else:
		popup_description.text = Loc.t("ui.collection.no_description")

	# Update action label based on source
	if from_collection:
		popup_action.text = Loc.t("ui.deck_builder.action_add")
	else:
		popup_action.text = Loc.t("ui.deck_builder.action_remove")

	# Show popup
	card_detail_popup.popup_centered()

	print("DeckBuilder: Showing details for card: %s" % card_instance_id)

func _on_popup_close_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	card_detail_popup.hide()

## =============================================================================
## NAVIGATION
## =============================================================================

func _on_back_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	print("DeckBuilder: Returning to collection")
	SceneManager.transition_to(SceneManager.SCENE_COLLECTION_SCREEN)

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
	for i: int in range(deck_selector.item_count):
		var meta_val: Variant = deck_selector.get_item_metadata(i)
		if meta_val is String:
			var meta_deck_id: String = meta_val
			if meta_deck_id == deck_id:
				deck_selector.select(i)
				_load_deck(deck_id)
				break

func _on_deck_deleted(_deck_id: String) -> void:
	print("DeckBuilder: Deck deleted, refreshing list...")
	_refresh_deck_list_only()
	# Load first available deck if any exist
	if deck_selector.item_count > 0:
		var first_deck_meta: Variant = deck_selector.get_item_metadata(0)
		if first_deck_meta is String:
			_load_deck(first_deck_meta)

func _on_collection_changed() -> void:
	print("DeckBuilder: Collection changed, refreshing...")
	_refresh_collection()
	_refresh_deck_display()  # In case deck cards changed

## =============================================================================
## DECK EDITING LOCK (TUTORIAL MODE)
## =============================================================================

func _setup_locked_ui() -> void:
	# Show lock message in validation label
	validation_label.text = Loc.t("ui.deck_builder.locked")
	validation_label.add_theme_color_override("font_color", Color(1.0, 0.8, 0.3))  # Yellow/orange

	# Disable deck management buttons
	new_deck_button.disabled = true
	delete_deck_button.disabled = true
	deck_selector.disabled = true
	deck_name_edit.editable = false

func _show_locked_message() -> void:
	# Show temporary notification that editing is locked
	print("DeckBuilder: User attempted edit while locked")
	validation_label.text = Loc.t("ui.deck_builder.locked_temporary")
	validation_label.add_theme_color_override("font_color", Color(1.0, 0.5, 0.3))  # Orange

	# Reset to normal lock message after delay
	await get_tree().create_timer(LOCK_MESSAGE_DURATION).timeout
	if deck_editing_locked:
		validation_label.text = Loc.t("ui.deck_builder.locked")
		validation_label.add_theme_color_override("font_color", Color(1.0, 0.8, 0.3))

## =============================================================================
## SUMMONER MANAGEMENT
## =============================================================================

## Populate summoner selector with unlocked summoners
func _populate_summoner_selector() -> void:
	if not summoner_selector:
		return

	summoner_selector.clear()

	var profile_repo: Node = get_node("/root/ProfileRepo")
	if not profile_repo or not profile_repo.has_method("get_unlocked_summoners"):
		return

	var unlocked: Array = profile_repo.call("get_unlocked_summoners")
	if unlocked.is_empty():
		push_warning("DeckBuilder: No summoners unlocked!")
		return

	var summoner_catalog: Node = get_node("/root/SummonerCatalog")
	if not summoner_catalog or not summoner_catalog.has_method("get_summoner"):
		return

	# Add each unlocked summoner to selector
	for summoner_id: Variant in unlocked:
		if summoner_id is String:
			var summoner_data: Variant = summoner_catalog.call("get_summoner", summoner_id)
			if summoner_data is Dictionary:
				var summoner_dict: Dictionary = summoner_data
				var summoner_name: String = summoner_dict.get("summoner_name", summoner_id)
				summoner_selector.add_item(summoner_name)
				summoner_selector.set_item_metadata(summoner_selector.item_count - 1, summoner_id)

	print("DeckBuilder: Populated summoner selector with %d summoners" % summoner_selector.item_count)

## Load and display summoner for current deck
func _load_deck_summoner() -> void:
	if not summoner_selector or current_deck_data.is_empty():
		return

	var summoner_id: String = current_deck_data.get("summoner_id", "")
	if summoner_id.is_empty():
		push_warning("DeckBuilder: Deck has no summoner assigned!")
		return

	# Find and select the summoner in the dropdown
	for i: int in summoner_selector.item_count:
		var metadata: Variant = summoner_selector.get_item_metadata(i)
		if metadata is String and metadata == summoner_id:
			summoner_selector.selected = i
			break

	# Update summoner stats display
	_update_summoner_stats_display(summoner_id)

## Update summoner stats label with summoner data
func _update_summoner_stats_display(summoner_id: String) -> void:
	if not summoner_stats_label:
		return

	var summoner_catalog: Node = get_node("/root/SummonerCatalog")
	if not summoner_catalog or not summoner_catalog.has_method("get_summoner"):
		return

	var summoner_data: Variant = summoner_catalog.call("get_summoner", summoner_id)
	if not summoner_data is Dictionary:
		return

	var summoner_dict: Dictionary = summoner_data
	var health: float = summoner_dict.get("base_health", 0.0)
	var mana: float = summoner_dict.get("max_mana", 0.0)

	summoner_stats_label.text = Loc.t("ui.deck_builder.summoner_stats", {"hp": "%.0f" % health, "mana": "%.0f" % mana})

## Called when summoner selector changes
func _on_summoner_selected(index: int) -> void:
	if deck_editing_locked:
		_show_locked_message()
		_load_deck_summoner()  # Reset to original summoner
		return

	if not summoner_selector or current_deck_id.is_empty():
		return

	var summoner_id: Variant = summoner_selector.get_item_metadata(index)
	if not summoner_id is String:
		return

	var summoner_id_str: String = summoner_id

	# Update deck summoner
	var decks: Node = get_node("/root/Decks")
	if decks and decks.has_method("set_deck_summoner"):
		var success: Variant = decks.call("set_deck_summoner", current_deck_id, summoner_id_str)
		if success is bool and success:
			print("DeckBuilder: Changed deck summoner to: %s" % summoner_id_str)
			_update_summoner_stats_display(summoner_id_str)
		else:
			push_error("DeckBuilder: Failed to set deck summoner!")
			_load_deck_summoner()  # Reset to original
