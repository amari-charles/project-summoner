extends BackNavigableScreen
class_name CollectionScreen

## CollectionScreen - Side-by-side layout for collection and decks
##
## Left panel (2/3): Collection grid with filters and search
## Right panel (1/3): Deck list - click to select, double-click to open details
## Click card → opens details; deck membership actions live inside that view.

signal closed()

@export var embedded_overlay: bool = false

## =============================================================================
## NODE REFERENCES
## =============================================================================

## Left panel - Header
@onready var dimmer: ColorRect = %Dimmer
@onready var window: PanelContainer = %Window
@onready var close_button: Button = %CloseButton
@onready var traits_button: Button = %TraitsButton
@onready var traits_badge: Label = %TraitsBadge

## Left panel - Filters
@onready var all_button: Button = %AllButton
@onready var summon_button: Button = %SummonButton
@onready var spell_button: Button = %SpellButton
@onready var element_filter_button: Button = %ElementFilterButton
@onready var element_popup: PopupMenu = %ElementPopup
@onready var sort_dropdown: OptionButton = %SortDropdown
@onready var search_edit: LineEdit = %SearchEdit
@onready var inspection_hint: Label = %InspectionHint

## Left panel - Shared deck editor
@onready var deck_editor: DeckEditorPanel = %DeckEditorPanel

## Right panel - Decks
@onready var decks_list: VBoxContainer = %DecksList
@onready var decks_header: Label = %DecksHeader
@onready var new_deck_button: Button = %NewDeckButton
@onready var confirm_selection_button: Button = %ConfirmSelectionButton

## Dialogs
@onready var new_deck_dialog: AcceptDialog = %NewDeckDialog
@onready var deck_name_input: LineEdit = %DeckNameInput
@onready var confirm_delete_dialog: ConfirmationDialog = %ConfirmDeleteDialog
@onready var rename_dialog: AcceptDialog = %RenameDialog
@onready var rename_input: LineEdit = %RenameInput
@onready var loadout_error_dialog: AcceptDialog = %LoadoutErrorDialog

## =============================================================================
## STATE
## =============================================================================

## Currently selected deck for adding cards
var selected_deck_id: String = ""

## Deck being renamed/deleted (for dialog callbacks)
var deck_id_for_action: String = ""

## Collection data
var collection_summary: Array = []
var _filtered_sorted_cards_cache: Array = []
var _ranked_summoner_id: String = ""
var _encounter_id: String = ""
var _encounter_source_deck_id: String = ""
var _encounter_state: Dictionary = {}

const NAV_KEY_MODE: String = "collection_mode"
const MODE_RANKED_DECK: String = "ranked_deck"
const MODE_ENCOUNTER_LOADOUT: String = "encounter_loadout"
const ENCOUNTER_DECK_CONTEXT_ID: String = "__encounter_loadout__"

enum OpenMode {
	COLLECTION,
	RANKED_DECK,
	ENCOUNTER_LOADOUT,
}

var _open_mode: OpenMode = OpenMode.COLLECTION

## Filter state
var show_summons: bool = true
var show_spells: bool = true
var selected_elements: Array[String] = []  # Empty = all elements
var search_text: String = ""

## Element list for filtering (must match keys in localization/data/en.json elements section)
const ELEMENTS: Array[String] = ["fire", "water", "wind", "earth", "lightning", "shadow", "life", "death", "neutral"]

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

var _card_service: Node = null

## Constants
const MAX_DECK_SIZE: int = DeckConstants.MAX_DECK_SIZE

## Scenes
const CardDetailModalScene: PackedScene = preload("res://scenes/meta/modals/card_detail_modal.tscn")
const DeckListItemScene: PackedScene = preload("res://scenes/meta/components/deck_list_item.tscn")


## =============================================================================
## LIFECYCLE
## =============================================================================

func _exit_tree() -> void:
	deck_editor.dismiss_popup()


func _ready() -> void:
	_configure_overlay_style()
	var initial_mode: String = ""
	if not embedded_overlay:
		initial_mode = SafeTypeUtils.string(
			NavigationContext.consume_value(NAV_KEY_MODE, ""), ""
		)
	_configure_open_mode(initial_mode)
	_card_service = get_node_or_null(CSharpAutoloads.CARD_SERVICE)

	# Connect header buttons
	close_button.pressed.connect(_on_close_pressed)
	traits_button.get_parent().visible = false
	traits_badge.visible = false

	# Connect deck management
	new_deck_button.pressed.connect(_on_new_deck_pressed)
	confirm_selection_button.visible = _open_mode == OpenMode.RANKED_DECK
	confirm_selection_button.text = Loc.t("ui.collection.use_for_ranked")
	loadout_error_dialog.title = Loc.t("academy.flow.fill_failed_title")
	confirm_selection_button.pressed.connect(_on_confirm_ranked_selection_pressed)
	_update_ranked_selection_button()

	# Connect dialogs
	new_deck_dialog.confirmed.connect(_on_new_deck_confirmed)
	new_deck_dialog.visibility_changed.connect(_on_new_deck_dialog_visibility_changed)
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

	deck_editor.add_card_requested.connect(_add_card_to_selected_deck)
	deck_editor.remove_card_requested.connect(_remove_card_from_deck)
	deck_editor.card_info_requested.connect(_open_card_detail_modal)

	# Connect to services
	_connect_services()

	# Initial load
	_refresh_deck_list()
	_refresh_deck_panel()
	_refresh_collection()
	if embedded_overlay:
		visible = false


func open_collection(mode: String = "", summoner_id: String = "") -> void:
	QuestApi.record_ui_surface_opened("spellbook")
	QuestGuidance.clear()
	_configure_open_mode(mode, summoner_id)
	visible = true
	_refresh_deck_list()
	_refresh_deck_panel()
	_refresh_collection()
	call_deferred("_refresh_quest_guidance")


func _refresh_quest_guidance() -> void:
	if QuestGuidance.is_target_active("new_deck_dialog"):
		inspection_hint.visible = false
		deck_editor.set_card_inspection_guidance(false)
		QuestGuidance.show_for(new_deck_button, "new_deck_dialog")
		return
	var show_inspection_guidance: bool = QuestGuidance.is_target_active("card_detail")
	inspection_hint.visible = show_inspection_guidance
	inspection_hint.text = Loc.t("ui.collection.showcase_inspect_hint")
	deck_editor.set_card_inspection_guidance(show_inspection_guidance)
	var first_card: Control = deck_editor.get_first_card_control()
	if first_card != null:
		QuestGuidance.show_for(
			first_card,
			"card_detail",
			"quest.guidance.right_click"
		)


func open_encounter_loadout(encounter_id: String) -> void:
	if encounter_id.is_empty():
		push_error("CollectionScreen.open_encounter_loadout requires an encounter ID")
		return
	_encounter_id = encounter_id
	_configure_open_mode(MODE_ENCOUNTER_LOADOUT)
	if not _refresh_encounter_state():
		return
	visible = true
	_refresh_deck_list()
	_refresh_deck_panel()
	_refresh_collection()


func _configure_open_mode(mode: String, summoner_id: String = "") -> void:
	match mode:
		MODE_RANKED_DECK:
			_open_mode = OpenMode.RANKED_DECK
		MODE_ENCOUNTER_LOADOUT:
			_open_mode = OpenMode.ENCOUNTER_LOADOUT
		_:
			_open_mode = OpenMode.COLLECTION
	if _open_mode != OpenMode.ENCOUNTER_LOADOUT:
		_encounter_id = ""
		_encounter_source_deck_id = ""
		_encounter_state = {}
	_ranked_summoner_id = ""
	if _open_mode == OpenMode.RANKED_DECK:
		_ranked_summoner_id = (
			summoner_id
			if not summoner_id.is_empty()
			else SummonerSelectionApi.get_active_summoner_id()
		)
		selected_deck_id = DecksApi.get_ranked_deck_id(_ranked_summoner_id)
	elif _open_mode == OpenMode.COLLECTION:
		selected_deck_id = DecksApi.get_active_deck_id()
	else:
		selected_deck_id = ""
	confirm_selection_button.visible = _open_mode == OpenMode.RANKED_DECK if is_node_ready() else false
	if is_node_ready():
		decks_header.text = Loc.t("academy.flow.fill_from_my_decks") \
			if _open_mode == OpenMode.ENCOUNTER_LOADOUT else Loc.t("ui.collection.my_decks")
		new_deck_button.visible = _open_mode != OpenMode.ENCOUNTER_LOADOUT
		_update_ranked_selection_button()


func _refresh_encounter_state() -> bool:
	_encounter_state = EncounterApi.get_preparation_state(_encounter_id)
	if _encounter_state.is_empty():
		push_error("CollectionScreen could not load encounter '%s'" % _encounter_id)
		return false
	return true


func _configure_overlay_style() -> void:
	dimmer.color = Color(0.0, 0.0, 0.0, 0.62)
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = GameColorPalette.UI_BACKGROUND
	style.border_color = GameColorPalette.UI_BORDER_STRONG
	style.set_border_width_all(2)
	style.set_corner_radius_all(12)
	style.shadow_color = GameColorPalette.BUTTON_SHADOW
	style.shadow_size = 14
	window.add_theme_stylebox_override("panel", style)


func _make_traits_button_style(bg_color: Color, border_color: Color) -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = bg_color
	style.border_color = border_color
	style.border_width_left = 2
	style.border_width_top = 2
	style.border_width_right = 2
	style.border_width_bottom = 2
	style.corner_radius_top_left = 8
	style.corner_radius_top_right = 8
	style.corner_radius_bottom_right = 8
	style.corner_radius_bottom_left = 8
	return style


func _apply_traits_button_style(has_unspent_points: bool) -> void:
	var normal_bg: Color = GameColorPalette.BUTTON_SECONDARY_BG
	var hover_bg: Color = GameColorPalette.BUTTON_SECONDARY_BG_HOVER
	var pressed_bg: Color = GameColorPalette.BUTTON_SECONDARY_BG_PRESSED
	var border: Color = GameColorPalette.BUTTON_SECONDARY_BORDER
	var font: Color = GameColorPalette.TEXT_PRIMARY

	if has_unspent_points:
		normal_bg = GameColorPalette.BUTTON_PRIMARY_BG
		hover_bg = GameColorPalette.BUTTON_PRIMARY_BG_HOVER
		pressed_bg = GameColorPalette.BUTTON_PRIMARY_BG_PRESSED
		border = GameColorPalette.BUTTON_PRIMARY_BORDER
		font = GameColorPalette.TEXT_HIGHLIGHT

	traits_button.add_theme_stylebox_override("normal", _make_traits_button_style(normal_bg, border))
	traits_button.add_theme_stylebox_override("hover", _make_traits_button_style(hover_bg, border.lightened(0.08)))
	traits_button.add_theme_stylebox_override("pressed", _make_traits_button_style(pressed_bg, border.darkened(0.08)))
	traits_button.add_theme_stylebox_override("disabled", _make_traits_button_style(GameColorPalette.BUTTON_DISABLED, GameColorPalette.UI_BORDER))
	traits_button.add_theme_color_override("font_color", font)
	traits_button.add_theme_color_override("font_hover_color", font.lightened(0.08))
	traits_button.add_theme_color_override("font_pressed_color", font)
	traits_button.add_theme_color_override("font_disabled_color", GameColorPalette.TEXT_DISABLED)


func _connect_services() -> void:
	if Decks.has_signal("DeckChanged"):
		Decks.connect("DeckChanged", _on_deck_changed)
	if Decks.has_signal("DeckCreated"):
		Decks.connect("DeckCreated", _on_deck_created)
	if Decks.has_signal("DeckDeleted"):
		Decks.connect("DeckDeleted", _on_deck_deleted)

	if _card_service and _card_service.has_signal("CollectionChanged"):
		_card_service.connect("CollectionChanged", Callable(self, "_on_collection_changed"))


## =============================================================================
## DECK LIST (Right Panel)
## =============================================================================

func _refresh_deck_list() -> void:
	if not Decks.has_method("ListDecksDict"):
		return

	var deck_list_result: Variant = DecksApi.list_decks_dict()
	if not deck_list_result is Array:
		return

	# Clear existing items
	for child: Node in decks_list.get_children():
		child.queue_free()

	# Get active deck ID
	var active_deck_id: String = ""
	if Decks.has_method("GetActiveDeckId"):
		var active_id: Variant = DecksApi.get_active_deck_id()
		active_deck_id = SafeTypeUtils.string(active_id, "")

	# Create deck items
	var first_eligible_deck_id: String = ""
	for deck_item: Variant in deck_list_result:
		if not deck_item is Dictionary:
			continue
		var deck: Dictionary = deck_item
		if _open_mode == OpenMode.RANKED_DECK and SafeTypeUtils.string(deck.get("summoner_id", ""), "") != _ranked_summoner_id:
			continue
		var deck_id: String = deck.get("id", "")
		if deck_id == "":
			continue
		if first_eligible_deck_id.is_empty():
			first_eligible_deck_id = deck_id

		var item: Control = DeckListItemScene.instantiate()
		decks_list.add_child(item)

		# Configure the item
		if item.has_method("setup"):
			var card_count: int = deck.get("card_instance_ids", []).size()
			var is_valid: bool = true
			if Decks.has_method("GetValidationErrorsArray"):
				var errors: Variant = DecksApi.get_validation_errors_array(deck_id)
				is_valid = errors is Array and errors.size() == 0

			item.call("setup", {
				"id": deck_id,
				"name": deck.get("name", Loc.t("ui.collection.unnamed_deck")),
				"card_count": card_count,
				"max_cards": MAX_DECK_SIZE,
				"is_active": deck_id == active_deck_id,
				"is_selected": deck_id == (
					_encounter_source_deck_id \
					if _open_mode == OpenMode.ENCOUNTER_LOADOUT else selected_deck_id
				),
				"is_valid": is_valid,
				"management_enabled": _open_mode != OpenMode.ENCOUNTER_LOADOUT,
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
	if deck_list_result.size() == 0 and Decks.has_method("CreateDeckFromDict"):
		# Check if any summoners are unlocked - can't create deck without one
		var unlocked: Array[String] = SummonerSelectionApi.get_unlocked_summoner_ids_array()
		if unlocked.size() > 0:
			var new_deck_id: Variant = DecksApi.create_deck_from_dict(Loc.t("ui.collection.default_deck_name"), [], "")
			if new_deck_id is String and not new_deck_id.is_empty():
				_refresh_deck_list()
		return

	# Auto-select first deck if none selected
	if _open_mode != OpenMode.ENCOUNTER_LOADOUT and selected_deck_id == "" and not first_eligible_deck_id.is_empty():
		selected_deck_id = first_eligible_deck_id
		_refresh_deck_list()
	_update_ranked_selection_button()


func _on_deck_item_clicked(deck_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if _open_mode == OpenMode.ENCOUNTER_LOADOUT:
		var result: Dictionary = EncounterApi.fill_loadout_from_deck(
			_encounter_id, deck_id
		)
		if SafeTypeUtils.bool_val(result.get("success")):
			_encounter_source_deck_id = deck_id
			_refresh_encounter_state()
			_refresh_deck_list()
			_refresh_deck_panel()
			_refresh_available_cards()
		else:
			loadout_error_dialog.dialog_text = Loc.t("academy.flow.fill_failed")
			loadout_error_dialog.popup_centered()
		return
	if deck_id == selected_deck_id:
		return
	selected_deck_id = deck_id
	_update_ranked_selection_button()
	_refresh_deck_list()
	_refresh_deck_panel()
	_refresh_available_cards()


func _on_deck_item_double_clicked(_deck_id: String) -> void:
	pass


func _on_deck_star_clicked(deck_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if Decks.has_method("SetActiveDeck"):
		DecksApi.set_active_deck(deck_id)
		_refresh_deck_list()


func _update_ranked_selection_button() -> void:
	if _open_mode != OpenMode.RANKED_DECK:
		return
	confirm_selection_button.disabled = selected_deck_id.is_empty() or not DecksApi.validate_deck(selected_deck_id)


func _on_confirm_ranked_selection_pressed() -> void:
	if _open_mode != OpenMode.RANKED_DECK or confirm_selection_button.disabled:
		return
	if not DecksApi.set_ranked_deck(_ranked_summoner_id, selected_deck_id):
		return
	_on_close_pressed()


func _on_deck_rename_clicked(deck_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	deck_id_for_action = deck_id
	# Get current deck name
	if Decks.has_method("GetDeckDict"):
		var deck: Variant = DecksApi.get_deck_dict(deck_id)
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
	if _open_mode == OpenMode.ENCOUNTER_LOADOUT:
		_refresh_encounter_deck_panel()
		return
	if selected_deck_id == "":
		deck_editor.set_active_deck(
			Loc.t("ui.collection.no_deck_selected"),
			[],
			MAX_DECK_SIZE,
			false
		)
		return

	if not Decks.has_method("GetDeckDict"):
		return

	var deck_result: Variant = DecksApi.get_deck_dict(selected_deck_id)
	if not deck_result is Dictionary or deck_result.is_empty():
		return

	var deck: Dictionary = deck_result
	var card_ids: Array = deck.get("card_instance_ids", [])
	var entries: Array[Dictionary] = []
	for card_id: Variant in card_ids:
		var card_id_str: String = SafeTypeUtils.string(card_id, "")
		if card_id_str.is_empty():
			continue
		var card_data: Dictionary = CardServiceApi.get_card_dict(card_id_str)
		if card_data.is_empty():
			continue
		entries.append({
			"instance_id": card_id_str,
			"card_data": card_data,
			"catalog_id": SafeTypeUtils.string(card_data.get("catalog_id")),
			"tooltip": Loc.t("ui.collection.deck_card_remove_tooltip"),
		})
	deck_editor.set_active_deck(
		SafeTypeUtils.string(deck.get("name"), Loc.t("ui.collection.unnamed_deck")),
		entries,
		MAX_DECK_SIZE,
		true
	)


func _refresh_encounter_deck_panel() -> void:
	var loadout: Dictionary = SafeTypeUtils.dict(_encounter_state.get("loadout"))
	var entries: Array[Dictionary] = []
	for value: Variant in SafeTypeUtils.array(loadout.get("supplied_cards")):
		var supplied: Dictionary = SafeTypeUtils.dict(value)
		var count: int = maxi(SafeTypeUtils.int_val(supplied.get("count"), 1), 1)
		for copy_index: int in range(count):
			entries.append(_encounter_editor_entry(supplied, true, copy_index))
	for value: Variant in SafeTypeUtils.array(loadout.get("selected_cards")):
		entries.append(_encounter_editor_entry(SafeTypeUtils.dict(value), false))
	var rules: Dictionary = SafeTypeUtils.dict(loadout.get("rules"))
	var authored_max: int = SafeTypeUtils.int_val(rules.get("max_deck_size"))
	var max_size: int = mini(authored_max, MAX_DECK_SIZE) if authored_max > 0 else MAX_DECK_SIZE
	deck_editor.set_active_deck(
		Loc.t("academy.flow.encounter_loadout"),
		entries,
		max_size,
		true
	)


func _encounter_editor_entry(card: Dictionary, locked: bool, copy_index: int = 0) -> Dictionary:
	var catalog_id: String = SafeTypeUtils.string(card.get("card_id", card.get("catalog_id")))
	var instance_id: String = SafeTypeUtils.string(card.get("card_instance_id"))
	var detail_instance_id: String = instance_id
	var card_data: Dictionary = CardServiceApi.get_card_dict(instance_id) \
		if not instance_id.is_empty() else {}
	if instance_id.is_empty():
		instance_id = "__encounter_supplied_%s_%d" % [catalog_id, copy_index]
		card_data = {"id": instance_id, "catalog_id": catalog_id}
	var tooltip: String = SafeTypeUtils.string(
		CardCatalogApi.get_card_as_dict(catalog_id).get("card_name"), catalog_id
	)
	if locked:
		tooltip = "%s • %s" % [tooltip, Loc.t("academy.flow.encounter_supplied")]
	return {
		"instance_id": instance_id,
		"detail_instance_id": detail_instance_id,
		"catalog_id": catalog_id,
		"card_data": card_data,
		"locked": locked,
		"tooltip": tooltip,
	}


## =============================================================================
## DECK CREATION/DELETION
## =============================================================================

func _on_new_deck_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	deck_name_input.text = ""
	new_deck_dialog.popup_centered()
	QuestApi.record_ui_surface_opened("new_deck_dialog")
	QuestGuidance.clear()
	QuestGuidance.show_for(new_deck_dialog.get_ok_button(), "card_detail")


func _on_new_deck_confirmed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	var deck_name: String = deck_name_input.text
	if deck_name == "":
		deck_name = Loc.t("ui.collection.new_deck_default")

	if Decks.has_method("CreateDeckFromDict"):
		DecksApi.create_deck_from_dict(deck_name, [], "")
	call_deferred("_refresh_quest_guidance")


func _on_new_deck_dialog_visibility_changed() -> void:
	if not new_deck_dialog.visible:
		call_deferred("_refresh_quest_guidance")


func _on_delete_confirmed() -> void:
	if deck_id_for_action == "":
		return

	if Decks.has_method("DeleteDeck"):
		var success: Variant = DecksApi.delete_deck(deck_id_for_action)
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

	if Decks.has_method("UpdateDeckFromDict"):
		DecksApi.update_deck_from_dict(deck_id_for_action, new_name, [], "")
		deck_id_for_action = ""
		_refresh_deck_list()
		_refresh_deck_panel()


## =============================================================================
## COLLECTION DISPLAY (Left Panel)
## =============================================================================

func _refresh_collection() -> void:
	var summary_result: Array = CardServiceApi.get_collection_summary_dict()
	collection_summary = summary_result
	_filtered_sorted_cards_cache = _get_filtered_sorted_cards()
	_refresh_available_cards(true)


func _refresh_available_cards(update_existing_widgets: bool = false) -> void:
	var deck_card_ids: Array[String] = _get_selected_deck_card_ids()
	var encounter_available_ids: Dictionary = {}
	if _open_mode == OpenMode.ENCOUNTER_LOADOUT:
		var loadout: Dictionary = SafeTypeUtils.dict(_encounter_state.get("loadout"))
		for value: Variant in SafeTypeUtils.array(loadout.get("available_cards")):
			var available: Dictionary = SafeTypeUtils.dict(value)
			var available_id: String = SafeTypeUtils.string(available.get("card_instance_id"))
			if not available_id.is_empty():
				encounter_available_ids[available_id] = true
	var entries: Array[Dictionary] = []
	for card_entry: Variant in _filtered_sorted_cards_cache:
		if not card_entry is Dictionary:
			continue
		var entry: Dictionary = card_entry
		var instance_id: String = SafeTypeUtils.string(entry.get("instance_id"))
		if _open_mode == OpenMode.ENCOUNTER_LOADOUT and not encounter_available_ids.has(instance_id):
			continue
		if instance_id in deck_card_ids:
			continue
		entries.append(entry)
	deck_editor.set_available_cards(entries, update_existing_widgets)

func _get_selected_deck_card_ids() -> Array[String]:
	var result: Array[String] = []
	if _open_mode == OpenMode.ENCOUNTER_LOADOUT:
		var loadout: Dictionary = SafeTypeUtils.dict(_encounter_state.get("loadout"))
		for value: Variant in SafeTypeUtils.array(loadout.get("selected_cards")):
			var selected: Dictionary = SafeTypeUtils.dict(value)
			var selected_id: String = SafeTypeUtils.string(selected.get("card_instance_id"))
			if not selected_id.is_empty():
				result.append(selected_id)
		return result
	if selected_deck_id == "":
		return result

	if not Decks.has_method("GetDeckDict"):
		return result

	var deck_result: Variant = DecksApi.get_deck_dict(selected_deck_id)
	if not deck_result is Dictionary:
		return result

	var card_ids: Array = deck_result.get("card_instance_ids", [])
	for card_id: Variant in card_ids:
		var card_id_str: String = SafeTypeUtils.string(card_id, "")
		if not card_id_str.is_empty():
			result.append(card_id_str)

	return result


func _get_filtered_sorted_cards() -> Array:
	var result: Array = []

	for entry: Variant in collection_summary:
		if not entry is Dictionary:
			continue

		var catalog_id: String = entry.get("catalog_id", "")
		var catalog_data: Variant = CardCatalogApi.get_card_as_dict(catalog_id)
		if not catalog_data is Dictionary or catalog_data.is_empty():
			continue

		# Apply type filter
		var card_type: int = catalog_data.get("card_type", UnitConstants.CardType.SUMMON)
		if card_type == UnitConstants.CardType.SUMMON and not show_summons:
			continue
		if card_type == UnitConstants.CardType.SPELL and not show_spells:
			continue

		var card_name: String = catalog_data.get("card_name", "")

		# Apply search filter
		if search_text != "" and not card_name.to_lower().contains(search_text.to_lower()):
			continue

		# Get element for filtering
		var card_element: String = "neutral"
		var categories: Dictionary = catalog_data.get("categories", {})
		var affinity: Variant = categories.get("elemental_affinity", null)
		if affinity:
			card_element = str(affinity)

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
			var progression: Dictionary = {}
			if not instance_id.is_empty():
				progression = CardServiceApi.get_card_progression_info_dict(instance_id)
				if not progression.is_empty():
					level = SafeTypeUtils.int_val(progression.get("level", 1), 1)

			var rarity: String = SafeTypeUtils.string(catalog_data.get("rarity", "common"), "common").to_lower()
			result.append({
				"instance_id": instance_id,
				"catalog_id": catalog_id,
				"card_data": instance,
				"catalog_data": catalog_data,
				"card_name": card_name,
				"mana_cost": catalog_data.get("mana_cost", 0),
				"rarity": rarity,
				"rarity_order": RARITY_ORDER.get(rarity, 0),
				"card_type": catalog_data.get("card_type", 0),
				"level": level,
				"progression": progression,
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
## DECK OPERATIONS
## =============================================================================

func _add_card_to_selected_deck(card_instance_id: String) -> void:
	if _open_mode == OpenMode.ENCOUNTER_LOADOUT:
		_toggle_encounter_card(card_instance_id)
		return
	if selected_deck_id == "":
		return

	var deck_card_ids: Array[String] = _get_selected_deck_card_ids()
	if deck_card_ids.size() >= MAX_DECK_SIZE:
		return

	if Decks.has_method("AddCardToDeck"):
		DecksApi.add_card_to_deck(selected_deck_id, card_instance_id)


## =============================================================================
## CARD DETAIL MODAL
## =============================================================================

func _open_card_detail_modal(instance_id: String, catalog_id: String) -> void:
	QuestApi.record_ui_surface_opened("card_detail")
	QuestGuidance.clear()
	var modal: Node = CardDetailModalScene.instantiate()
	if not modal:
		return

	add_child(modal)

	if modal.has_method("open_for_card"):
		modal.call("open_for_card", instance_id, catalog_id)

	# Set deck context so modal shows add/remove button
	var is_in_deck: bool = instance_id in _get_selected_deck_card_ids()
	if modal.has_method("set_deck_context"):
		modal.call(
			"set_deck_context",
			ENCOUNTER_DECK_CONTEXT_ID \
			if _open_mode == OpenMode.ENCOUNTER_LOADOUT else selected_deck_id,
			is_in_deck
		)

	# Connect signals
	if modal.has_signal("deck_action_requested"):
		modal.deck_action_requested.connect(_on_deck_action_from_modal)

	if modal.has_signal("closed"):
		modal.closed.connect(_on_modal_closed.bind(modal))


func _on_deck_action_from_modal(instance_id: String, action: String) -> void:
	if action == "add":
		_add_card_to_selected_deck(instance_id)
	elif action == "remove":
		_remove_card_from_deck(instance_id)


func _remove_card_from_deck(card_instance_id: String) -> void:
	if _open_mode == OpenMode.ENCOUNTER_LOADOUT:
		_toggle_encounter_card(card_instance_id)
		return
	if selected_deck_id == "":
		return

	if Decks.has_method("RemoveCardFromDeck"):
		DecksApi.remove_card_from_deck(selected_deck_id, card_instance_id)


func _toggle_encounter_card(card_instance_id: String) -> void:
	var selected: Array[Dictionary] = []
	var found: bool = false
	var loadout: Dictionary = SafeTypeUtils.dict(_encounter_state.get("loadout"))
	for value: Variant in SafeTypeUtils.array(loadout.get("selected_cards")):
		var card: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(card.get("card_instance_id")) == card_instance_id:
			found = true
		else:
			selected.append({
				"card_instance_id": SafeTypeUtils.string(card.get("card_instance_id"))
			})
	if not found:
		selected.append({"card_instance_id": card_instance_id})
	if EncounterApi.update_loadout(_encounter_id, selected):
		_refresh_encounter_state()
		_refresh_deck_panel()
		_refresh_available_cards()
	else:
		loadout_error_dialog.dialog_text = Loc.t("academy.flow.update_loadout_failed")
		loadout_error_dialog.popup_centered()


func _on_modal_closed(modal: Node) -> void:
	if modal and is_instance_valid(modal):
		modal.queue_free()
	QuestGuidance.show_for(close_button, "shop")


## =============================================================================
## NAVIGATION
## =============================================================================

func _on_close_pressed() -> void:
	deck_editor.dismiss_popup()
	if embedded_overlay:
		visible = false
		closed.emit()
		return
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_ACADEMY_CAMPUS
	SceneManager.transition_to(return_scene)


func _on_traits_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	NavigationContext.push_return(SceneManager.SCENE_COLLECTION_SCREEN)
	SceneManager.transition_to(SceneManager.SCENE_TRAIT_TREE_SCREEN)


## =============================================================================
## SERVICE SIGNALS
## =============================================================================

func _on_deck_changed(deck_id: String) -> void:
	if deck_id == selected_deck_id:
		_refresh_deck_list()
		_refresh_deck_panel()
		_refresh_available_cards()
		_update_ranked_selection_button()


func _on_deck_created(deck_id: String) -> void:
	# Select the newly created deck
	selected_deck_id = deck_id
	_refresh_deck_list()
	_refresh_deck_panel()
	_refresh_available_cards()


func _on_deck_deleted(deck_id: String) -> void:
	if selected_deck_id == deck_id:
		selected_deck_id = ""
	_refresh_deck_list()
	_refresh_deck_panel()
	_refresh_available_cards()


func _on_collection_changed() -> void:
	_refresh_collection()
	_refresh_deck_panel()


func _request_back_navigation() -> void:
	_on_close_pressed()
