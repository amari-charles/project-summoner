extends PanelContainer
class_name UnitSpawnerPanel

## UnitSpawnerPanel - Debug panel for spawning units via drag-and-drop
##
## Shows a list of all available units that can be dragged onto the
## battlefield. Includes a team toggle, pause button, and clear all button.

const SETTINGS_PATH: String = "user://debug_arena_settings.cfg"
const DEBUG_DECK_PATH: String = "res://data/debug/debug_deck.json"
const FILTER_ALL: String = "__all"
const FILTER_TYPE_SUMMON: String = "summon"
const FILTER_TYPE_SPELL: String = "spell"
const SORT_NAME_ASC: String = "name_asc"
const SORT_MANA_ASC: String = "mana_asc"
const SORT_MANA_DESC: String = "mana_desc"
const SPAWN_MODE_SINGLE: String = "single"
const SPAWN_MODE_BURST: String = "burst"
const SPAWN_MODE_PAINT: String = "paint"
const FORMATION_STACK: String = "stack"
const FORMATION_LINE: String = "line"
const FORMATION_ARC: String = "arc"
const FORMATION_RANDOM: String = "random"
const TEAM_PLAYER: int = 0
const TEAM_ENEMY: int = 1
const ADVANCED_DRAWER_WIDTH: float = 260.0
const ADVANCED_DRAWER_GAP: float = 8.0
const ADVANCED_TOGGLE_WIDTH: float = 22.0
const ADVANCED_TOGGLE_HEIGHT: float = 32.0
const ADVANCED_TOGGLE_TOP: float = 14.0
const PANEL_COLLAPSED_WIDTH: float = 36.0
const PANEL_COLLAPSED_HEIGHT: float = 32.0
const PANEL_COLLAPSE_BUTTON_WIDTH: float = 24.0
const PANEL_COLLAPSE_BUTTON_HEIGHT: float = 24.0
const PANEL_COLLAPSE_BUTTON_TOP: float = 4.0
const PANEL_COLLAPSE_BUTTON_RIGHT_INSET: float = 4.0

## Signal emitted when clear button is pressed
signal clear_requested()
## Signal emitted when skip prep phase is toggled
signal skip_prep_toggled(skip: bool)
## Signal emitted when enemy AI is toggled
signal enemy_ai_toggled(enabled: bool)
## Signal emitted when player AI is toggled
signal player_ai_toggled(enabled: bool)
## Signal emitted when player objective advance is held
signal player_hold_advance_toggled(enabled: bool)
## Signal emitted when clear team button is pressed
signal clear_team_requested(team: int)
## Signal emitted when undo requested
signal undo_requested()

var _unit_buttons: Array[Control] = []
var _unit_entries: Array[Dictionary] = []
var _skip_prep_phase: bool = false
var _enemy_ai_enabled: bool = false
var _player_ai_enabled: bool = false
var _player_hold_advance_enabled: bool = false
var _enemy_unit_list_container: VBoxContainer
var _player_unit_list_container: VBoxContainer
var _search_input: LineEdit
var _type_filter: OptionButton
var _element_filter: OptionButton
var _role_filter: OptionButton
var _sort_filter: OptionButton
var _sort_mode: String = SORT_NAME_ASC
var _spawn_mode_filter: OptionButton
var _burst_count_spinner: SpinBox
var _formation_mode_filter: OptionButton
var _formation_spacing_slider: HSlider
var _formation_spacing_label: Label
var _spawn_mode: String = SPAWN_MODE_SINGLE
var _burst_count: int = 3
var _formation_mode: String = FORMATION_STACK
var _formation_spacing: float = 2.0
var _spawn_log: RichTextLabel
var _advanced_controls_open: bool = false
var _advanced_controls_container: VBoxContainer
var _advanced_drawer_panel: PanelContainer
var _advanced_toggle_button: Button
var _panel_collapsed: bool = false
var _panel_margin: MarginContainer
var _panel_collapse_button: Button
var _expanded_offsets: Dictionary = {}
var _debug_deck_entries_override: Array = []


func _ready() -> void:
	_load_settings()
	_build_ui()
	if not item_rect_changed.is_connected(_on_item_rect_changed):
		item_rect_changed.connect(_on_item_rect_changed)
	call_deferred("_update_advanced_overlay_layout")


func _notification(what: int) -> void:
	if what == NOTIFICATION_RESIZED:
		_update_advanced_overlay_layout()


func _on_item_rect_changed() -> void:
	_update_advanced_overlay_layout()


func _build_ui() -> void:
	# Apply panel styling
	add_theme_stylebox_override("panel", _create_panel_style())
	clip_contents = false

	_cache_expanded_offsets()

	_panel_margin = MarginContainer.new()
	_panel_margin.add_theme_constant_override("margin_left", 12)
	_panel_margin.add_theme_constant_override("margin_right", 12)
	_panel_margin.add_theme_constant_override("margin_top", 12)
	_panel_margin.add_theme_constant_override("margin_bottom", 12)
	add_child(_panel_margin)

	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 8)
	_panel_margin.add_child(vbox)

	# Title
	var title: Label = Label.new()
	title.text = Loc.t("debug.spawner.title")
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 16)
	title.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	vbox.add_child(title)

	# Separator
	vbox.add_child(HSeparator.new())

	# Side-by-side team unit lists
	var lists_row: HBoxContainer = HBoxContainer.new()
	lists_row.add_theme_constant_override("separation", 8)
	lists_row.custom_minimum_size = Vector2(320, 220)
	lists_row.size_flags_vertical = Control.SIZE_EXPAND_FILL
	vbox.add_child(lists_row)

	var player_list: VBoxContainer = _build_team_unit_column(lists_row, Loc.t("debug.spawner.player_tab"))
	var enemy_list: VBoxContainer = _build_team_unit_column(lists_row, Loc.t("debug.spawner.enemy_tab"))
	_player_unit_list_container = player_list
	_enemy_unit_list_container = enemy_list

	# Populate both team lists from debug deck
	_populate_unit_lists(enemy_list, player_list)
	_populate_dynamic_filter_options()
	_apply_filters_and_sort()

	# Separator
	vbox.add_child(HSeparator.new())

	var clear_row: HBoxContainer = HBoxContainer.new()
	clear_row.add_theme_constant_override("separation", 6)
	vbox.add_child(clear_row)

	var clear_player_btn: StyledButton = StyledButton.new()
	clear_player_btn.text = Loc.t("debug.spawner.clear_player")
	clear_player_btn.pressed.connect(_on_clear_player_pressed)
	clear_row.add_child(clear_player_btn)

	var clear_enemy_btn: StyledButton = StyledButton.new()
	clear_enemy_btn.text = Loc.t("debug.spawner.clear_enemy")
	clear_enemy_btn.pressed.connect(_on_clear_enemy_pressed)
	clear_row.add_child(clear_enemy_btn)

	var clear_all_btn: StyledButton = StyledButton.new()
	clear_all_btn.text = Loc.t("debug.spawner.clear_all")
	clear_all_btn.variant = StyledButton.Variant.DANGER
	clear_all_btn.pressed.connect(_on_clear_pressed)
	clear_row.add_child(clear_all_btn)

	var undo_btn: StyledButton = StyledButton.new()
	undo_btn.text = Loc.t("debug.spawner.undo_last")
	undo_btn.pressed.connect(_on_undo_pressed)
	vbox.add_child(undo_btn)

	_build_advanced_drawer()
	_build_advanced_toggle_handle()
	_build_panel_collapse_button()

	_set_advanced_controls_open(false)
	_set_panel_collapsed(false)


func _build_team_unit_column(parent: HBoxContainer, title_text: String) -> VBoxContainer:
	var column: VBoxContainer = VBoxContainer.new()
	column.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	column.size_flags_vertical = Control.SIZE_EXPAND_FILL
	column.add_theme_constant_override("separation", 4)
	parent.add_child(column)

	var title: Label = Label.new()
	title.text = title_text
	title.add_theme_font_size_override("font_size", 12)
	title.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	column.add_child(title)

	var scroll: ScrollContainer = ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	column.add_child(scroll)

	var list: VBoxContainer = VBoxContainer.new()
	list.add_theme_constant_override("separation", 4)
	list.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(list)
	return list


func _build_advanced_drawer() -> void:
	_advanced_drawer_panel = PanelContainer.new()
	_advanced_drawer_panel.add_theme_stylebox_override("panel", _create_panel_style())
	_advanced_drawer_panel.top_level = true
	_advanced_drawer_panel.z_index = 20
	add_child(_advanced_drawer_panel)

	var drawer_margin: MarginContainer = MarginContainer.new()
	drawer_margin.add_theme_constant_override("margin_left", 12)
	drawer_margin.add_theme_constant_override("margin_right", 12)
	drawer_margin.add_theme_constant_override("margin_top", 12)
	drawer_margin.add_theme_constant_override("margin_bottom", 12)
	_advanced_drawer_panel.add_child(drawer_margin)

	var drawer_scroll: ScrollContainer = ScrollContainer.new()
	drawer_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	drawer_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	drawer_margin.add_child(drawer_scroll)

	_advanced_controls_container = VBoxContainer.new()
	_advanced_controls_container.add_theme_constant_override("separation", 8)
	drawer_scroll.add_child(_advanced_controls_container)

	var advanced_title: Label = Label.new()
	advanced_title.text = Loc.t("debug.spawner.advanced_title")
	advanced_title.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	advanced_title.add_theme_font_size_override("font_size", 14)
	advanced_title.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	_advanced_controls_container.add_child(advanced_title)

	_advanced_controls_container.add_child(HSeparator.new())

	var skip_prep_toggle: CheckButton = CheckButton.new()
	skip_prep_toggle.text = Loc.t("debug.spawner.skip_prep_phase")
	skip_prep_toggle.button_pressed = _skip_prep_phase
	skip_prep_toggle.toggled.connect(_on_skip_prep_toggled)
	skip_prep_toggle.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	_advanced_controls_container.add_child(skip_prep_toggle)

	var enemy_ai_toggle: CheckButton = CheckButton.new()
	enemy_ai_toggle.text = Loc.t("debug.spawner.enemy_ai")
	enemy_ai_toggle.button_pressed = _enemy_ai_enabled
	enemy_ai_toggle.toggled.connect(_on_enemy_ai_toggled)
	enemy_ai_toggle.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	_advanced_controls_container.add_child(enemy_ai_toggle)

	var player_ai_toggle: CheckButton = CheckButton.new()
	player_ai_toggle.text = Loc.t("debug.spawner.player_ai")
	player_ai_toggle.button_pressed = _player_ai_enabled
	player_ai_toggle.toggled.connect(_on_player_ai_toggled)
	player_ai_toggle.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	_advanced_controls_container.add_child(player_ai_toggle)

	var player_hold_advance_toggle: CheckButton = CheckButton.new()
	player_hold_advance_toggle.text = "Hold Player Advance"
	player_hold_advance_toggle.button_pressed = _player_hold_advance_enabled
	player_hold_advance_toggle.toggled.connect(_on_player_hold_advance_toggled)
	player_hold_advance_toggle.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	_advanced_controls_container.add_child(player_hold_advance_toggle)

	_advanced_controls_container.add_child(HSeparator.new())
	_build_spawn_controls(_advanced_controls_container)

	_advanced_controls_container.add_child(HSeparator.new())
	_build_filter_controls(_advanced_controls_container)

	_advanced_controls_container.add_child(HSeparator.new())

	var log_title: Label = Label.new()
	log_title.text = Loc.t("debug.spawner.spawn_log")
	log_title.add_theme_font_size_override("font_size", 12)
	log_title.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	_advanced_controls_container.add_child(log_title)

	_spawn_log = RichTextLabel.new()
	_spawn_log.custom_minimum_size = Vector2(160, 90)
	_spawn_log.fit_content = false
	_spawn_log.scroll_active = true
	_spawn_log.bbcode_enabled = false
	_spawn_log.add_theme_font_size_override("normal_font_size", 11)
	_advanced_controls_container.add_child(_spawn_log)


func _build_advanced_toggle_handle() -> void:
	_advanced_toggle_button = Button.new()
	_advanced_toggle_button.top_level = true
	_advanced_toggle_button.size = Vector2(ADVANCED_TOGGLE_WIDTH, ADVANCED_TOGGLE_HEIGHT)
	_advanced_toggle_button.focus_mode = Control.FOCUS_NONE
	_advanced_toggle_button.z_index = 30
	_advanced_toggle_button.pressed.connect(_on_advanced_toggle_pressed)
	add_child(_advanced_toggle_button)


func _build_panel_collapse_button() -> void:
	_panel_collapse_button = Button.new()
	_panel_collapse_button.top_level = true
	_panel_collapse_button.size = Vector2(PANEL_COLLAPSE_BUTTON_WIDTH, PANEL_COLLAPSE_BUTTON_HEIGHT)
	_panel_collapse_button.focus_mode = Control.FOCUS_NONE
	_panel_collapse_button.z_index = 40
	_panel_collapse_button.pressed.connect(_on_panel_collapse_pressed)
	add_child(_panel_collapse_button)


func _cache_expanded_offsets() -> void:
	_expanded_offsets = {
		"left": offset_left,
		"top": offset_top,
		"right": offset_right,
		"bottom": offset_bottom
	}


func _restore_expanded_offsets() -> void:
	if _expanded_offsets.is_empty():
		return
	offset_left = SafeTypeUtils.float_val(_expanded_offsets.get("left", offset_left), offset_left)
	offset_top = SafeTypeUtils.float_val(_expanded_offsets.get("top", offset_top), offset_top)
	offset_right = SafeTypeUtils.float_val(_expanded_offsets.get("right", offset_right), offset_right)
	offset_bottom = SafeTypeUtils.float_val(_expanded_offsets.get("bottom", offset_bottom), offset_bottom)


func _build_spawn_controls(vbox: VBoxContainer) -> void:
	var controls_title: Label = Label.new()
	controls_title.text = Loc.t("debug.spawner.spawn_controls")
	controls_title.add_theme_font_size_override("font_size", 14)
	controls_title.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	vbox.add_child(controls_title)

	_spawn_mode_filter = _create_labeled_option(vbox, Loc.t("debug.spawner.spawn_mode"))
	_add_option_item(_spawn_mode_filter, Loc.t("debug.spawner.spawn_mode_single"), SPAWN_MODE_SINGLE)
	_add_option_item(_spawn_mode_filter, Loc.t("debug.spawner.spawn_mode_burst"), SPAWN_MODE_BURST)
	_add_option_item(_spawn_mode_filter, Loc.t("debug.spawner.spawn_mode_paint"), SPAWN_MODE_PAINT)
	_spawn_mode_filter.item_selected.connect(_on_spawn_mode_changed)
	_select_option_by_value(_spawn_mode_filter, _spawn_mode, 0)
	_spawn_mode = _get_option_value(_spawn_mode_filter, _spawn_mode_filter.selected)

	var burst_label: Label = Label.new()
	burst_label.text = Loc.t("debug.spawner.burst_count")
	burst_label.add_theme_font_size_override("font_size", 12)
	burst_label.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	vbox.add_child(burst_label)

	_burst_count_spinner = SpinBox.new()
	_burst_count_spinner.min_value = 1
	_burst_count_spinner.max_value = 20
	_burst_count_spinner.step = 1
	_burst_count_spinner.value = _burst_count
	_burst_count_spinner.value_changed.connect(_on_burst_count_changed)
	vbox.add_child(_burst_count_spinner)

	_formation_mode_filter = _create_labeled_option(vbox, Loc.t("debug.spawner.formation"))
	_add_option_item(_formation_mode_filter, Loc.t("debug.spawner.formation_stack"), FORMATION_STACK)
	_add_option_item(_formation_mode_filter, Loc.t("debug.spawner.formation_line"), FORMATION_LINE)
	_add_option_item(_formation_mode_filter, Loc.t("debug.spawner.formation_arc"), FORMATION_ARC)
	_add_option_item(_formation_mode_filter, Loc.t("debug.spawner.formation_random"), FORMATION_RANDOM)
	_formation_mode_filter.item_selected.connect(_on_formation_mode_changed)
	_select_option_by_value(_formation_mode_filter, _formation_mode, 0)
	_formation_mode = _get_option_value(_formation_mode_filter, _formation_mode_filter.selected)

	_formation_spacing_label = Label.new()
	_formation_spacing_label.add_theme_font_size_override("font_size", 12)
	_formation_spacing_label.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	vbox.add_child(_formation_spacing_label)

	_formation_spacing_slider = HSlider.new()
	_formation_spacing_slider.min_value = 0.5
	_formation_spacing_slider.max_value = 8.0
	_formation_spacing_slider.step = 0.1
	_formation_spacing_slider.value = _formation_spacing
	_formation_spacing_slider.value_changed.connect(_on_formation_spacing_changed)
	vbox.add_child(_formation_spacing_slider)

	_refresh_spawn_controls_visibility()
	_refresh_formation_spacing_label()


func _build_filter_controls(vbox: VBoxContainer) -> void:
	var filters_title: Label = Label.new()
	filters_title.text = Loc.t("debug.spawner.search_filters")
	filters_title.add_theme_font_size_override("font_size", 14)
	filters_title.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	vbox.add_child(filters_title)

	_search_input = LineEdit.new()
	_search_input.placeholder_text = Loc.t("debug.spawner.search_placeholder")
	_search_input.text_changed.connect(_on_search_text_changed)
	vbox.add_child(_search_input)

	_type_filter = _create_labeled_option(vbox, Loc.t("debug.spawner.filter_type"))
	_add_option_item(_type_filter, Loc.t("debug.spawner.filter_type_all"), FILTER_ALL)
	_add_option_item(_type_filter, Loc.t("ui.collection.type_summon"), FILTER_TYPE_SUMMON)
	_add_option_item(_type_filter, Loc.t("ui.collection.type_spell"), FILTER_TYPE_SPELL)
	_type_filter.item_selected.connect(_on_filter_changed)

	_element_filter = _create_labeled_option(vbox, Loc.t("debug.spawner.filter_element"))
	_add_option_item(_element_filter, Loc.t("debug.spawner.filter_element_all"), FILTER_ALL)
	_element_filter.item_selected.connect(_on_filter_changed)

	_role_filter = _create_labeled_option(vbox, Loc.t("debug.spawner.filter_role"))
	_add_option_item(_role_filter, Loc.t("debug.spawner.filter_role_all"), FILTER_ALL)
	_role_filter.item_selected.connect(_on_filter_changed)

	_sort_filter = _create_labeled_option(vbox, Loc.t("debug.spawner.sort"))
	_add_option_item(_sort_filter, Loc.t("debug.spawner.sort_az"), SORT_NAME_ASC)
	_add_option_item(_sort_filter, Loc.t("debug.spawner.sort_mana_low_high"), SORT_MANA_ASC)
	_add_option_item(_sort_filter, Loc.t("debug.spawner.sort_mana_high_low"), SORT_MANA_DESC)
	_sort_filter.selected = 0
	_sort_mode = SORT_NAME_ASC
	_sort_filter.item_selected.connect(_on_sort_changed)


func _create_labeled_option(parent: VBoxContainer, label_text: String) -> OptionButton:
	var label: Label = Label.new()
	label.text = label_text
	label.add_theme_font_size_override("font_size", 12)
	label.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	parent.add_child(label)

	var option: OptionButton = OptionButton.new()
	option.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	parent.add_child(option)
	return option


func _add_option_item(option: OptionButton, text: String, value: String) -> void:
	option.add_item(text)
	option.set_item_metadata(option.item_count - 1, value)


func _select_option_by_value(option: OptionButton, value: String, fallback_index: int = 0) -> void:
	if not option:
		return

	for i: int in option.item_count:
		var item_value: String = SafeTypeUtils.string(option.get_item_metadata(i), "")
		if item_value == value:
			option.selected = i
			return

	option.selected = fallback_index


func _populate_unit_lists(enemy_container: VBoxContainer, player_container: VBoxContainer) -> void:
	# Load debug deck from file
	var deck_entries: Array = _load_debug_deck()
	_unit_buttons.clear()
	_unit_entries.clear()

	for entry: Dictionary in deck_entries:
		var catalog_id: String = entry.get("catalog_id", "")
		if catalog_id.is_empty():
			continue

		# Create Card from catalog
		var card: Card = CardCatalogApi.create_card(catalog_id)
		if not card:
			push_warning("UnitSpawnerPanel: Failed to create card for '%s'" % catalog_id)
			continue

		var card_data: Dictionary = CardCatalogApi.get_card_as_dict(catalog_id)

		# Apply stat overrides if present (for testing upgraded cards)
		if entry.has("stat_overrides"):
			card.CustomStatOverrides = entry.get("stat_overrides")

		var unit_name: String = _resolve_display_name(card, card_data, catalog_id)

		var enemy_btn: SpawnableUnitButton = SpawnableUnitButton.new()
		enemy_btn.card = card
		enemy_btn.unit_name = unit_name
		enemy_btn.panel = self
		enemy_btn.spawn_team = TEAM_ENEMY
		enemy_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		enemy_container.add_child(enemy_btn)
		_unit_buttons.append(enemy_btn)

		var player_btn: SpawnableUnitButton = SpawnableUnitButton.new()
		player_btn.card = card
		player_btn.unit_name = unit_name
		player_btn.panel = self
		player_btn.spawn_team = TEAM_PLAYER
		player_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		player_container.add_child(player_btn)
		_unit_buttons.append(player_btn)

		_register_unit_entry(enemy_btn, player_btn, card_data, catalog_id)


func _rebuild_unit_lists() -> void:
	if not _enemy_unit_list_container or not _player_unit_list_container:
		return

	for child_var: Variant in _enemy_unit_list_container.get_children():
		if child_var is Node:
			var child: Node = child_var
			_enemy_unit_list_container.remove_child(child)
			child.queue_free()

	for child_var: Variant in _player_unit_list_container.get_children():
		if child_var is Node:
			var child: Node = child_var
			_player_unit_list_container.remove_child(child)
			child.queue_free()

	_populate_unit_lists(_enemy_unit_list_container, _player_unit_list_container)
	_populate_dynamic_filter_options()
	_apply_filters_and_sort()


func _resolve_display_name(card: Card, card_data: Dictionary, catalog_id: String) -> String:
	if card != null and not card.CardName.is_empty():
		return card.CardName

	var card_name: String = SafeTypeUtils.string(card_data.get("card_name", ""), "")
	if not card_name.is_empty():
		return card_name

	return catalog_id


func _register_unit_entry(
	enemy_button: SpawnableUnitButton,
	player_button: SpawnableUnitButton,
	card_data: Dictionary,
	catalog_id: String
) -> void:
	var categories: Dictionary = SafeTypeUtils.dict(card_data.get("categories", {}))
	var elemental_affinity: String = SafeTypeUtils.string(
		categories.get("elemental_affinity", ""),
		""
	).to_lower()
	var tactical_role: String = SafeTypeUtils.string(card_data.get("tactical_role", ""), "").to_lower()
	var card_type: int = SafeTypeUtils.int_val(
		card_data.get("card_type", UnitConstants.CardType.SUMMON),
		UnitConstants.CardType.SUMMON
	)
	var mana_cost: float = SafeTypeUtils.float_val(card_data.get("mana_cost", 0.0), 0.0)
	var display_name: String = enemy_button.unit_name

	_unit_entries.append(
		{
			"enemy_button": enemy_button,
			"player_button": player_button,
			"name": display_name,
			"name_lower": display_name.to_lower(),
			"catalog_id_lower": catalog_id.to_lower(),
			"card_type": card_type,
			"element": elemental_affinity,
			"role": tactical_role,
			"mana_cost": mana_cost
		}
	)


func _populate_dynamic_filter_options() -> void:
	if not _element_filter or not _role_filter:
		return

	_reset_option_to_all(_element_filter, Loc.t("debug.spawner.filter_element_all"))
	_reset_option_to_all(_role_filter, Loc.t("debug.spawner.filter_role_all"))

	var element_ids: Array[String] = []
	var role_ids: Array[String] = []
	var seen_elements: Dictionary = {}
	var seen_roles: Dictionary = {}

	for entry: Dictionary in _unit_entries:
		var element_id: String = SafeTypeUtils.string(entry.get("element", ""), "")
		if not element_id.is_empty() and not seen_elements.has(element_id):
			seen_elements[element_id] = true
			element_ids.append(element_id)

		var role_id: String = SafeTypeUtils.string(entry.get("role", ""), "")
		if not role_id.is_empty() and not seen_roles.has(role_id):
			seen_roles[role_id] = true
			role_ids.append(role_id)

	element_ids.sort()
	role_ids.sort()

	for element_id: String in element_ids:
		_add_option_item(_element_filter, ElementTypes.get_display_name(element_id), element_id)

	for role_id: String in role_ids:
		_add_option_item(_role_filter, _get_role_filter_label(role_id), role_id)


func _reset_option_to_all(option: OptionButton, all_label: String) -> void:
	option.clear()
	_add_option_item(option, all_label, FILTER_ALL)
	option.selected = 0


func _get_role_filter_label(role_id: String) -> String:
	match role_id:
		"frontliner":
			return Loc.t("ui.collection.role_frontliner")
		"flanker":
			return Loc.t("ui.collection.role_flanker")
		"backliner":
			return Loc.t("ui.collection.role_backliner")
		"mixed":
			return Loc.t("ui.collection.role_mixed")
		_:
			return role_id.capitalize()


func _on_search_text_changed(_new_text: String) -> void:
	_apply_filters_and_sort()


func _on_filter_changed(_index: int) -> void:
	_apply_filters_and_sort()


func _on_sort_changed(index: int) -> void:
	_sort_mode = _get_option_value(_sort_filter, index)
	if _sort_mode.is_empty():
		_sort_mode = SORT_NAME_ASC
	_apply_filters_and_sort()


func _get_option_value(option: OptionButton, index: int) -> String:
	if not option or index < 0 or index >= option.item_count:
		return ""
	return SafeTypeUtils.string(option.get_item_metadata(index), "")


func _apply_filters_and_sort() -> void:
	if not _enemy_unit_list_container or not _player_unit_list_container:
		return
	if not _type_filter or not _element_filter or not _role_filter:
		return

	var visible_entries: Array[Dictionary] = []
	var hidden_entries: Array[Dictionary] = []

	for entry: Dictionary in _unit_entries:
		var enemy_button_var: Variant = entry.get("enemy_button", null)
		var player_button_var: Variant = entry.get("player_button", null)
		if not (enemy_button_var is Control) or not (player_button_var is Control):
			continue
		var enemy_button: Control = enemy_button_var
		var player_button: Control = player_button_var

		var matches: bool = _entry_matches_filters(entry)
		enemy_button.visible = matches
		player_button.visible = matches
		if matches:
			visible_entries.append(entry)
		else:
			hidden_entries.append(entry)

	visible_entries.sort_custom(_is_entry_before)
	hidden_entries.sort_custom(_is_entry_before)

	var child_index: int = 0
	for entry: Dictionary in visible_entries:
		var visible_enemy_button_var: Variant = entry.get("enemy_button", null)
		if visible_enemy_button_var is Control:
			var visible_enemy_button: Control = visible_enemy_button_var
			_enemy_unit_list_container.move_child(visible_enemy_button, child_index)
		var visible_player_button_var: Variant = entry.get("player_button", null)
		if visible_player_button_var is Control:
			var visible_player_button: Control = visible_player_button_var
			_player_unit_list_container.move_child(visible_player_button, child_index)
		child_index += 1

	for entry: Dictionary in hidden_entries:
		var hidden_enemy_button_var: Variant = entry.get("enemy_button", null)
		if hidden_enemy_button_var is Control:
			var hidden_enemy_button: Control = hidden_enemy_button_var
			_enemy_unit_list_container.move_child(hidden_enemy_button, child_index)
		var hidden_player_button_var: Variant = entry.get("player_button", null)
		if hidden_player_button_var is Control:
			var hidden_player_button: Control = hidden_player_button_var
			_player_unit_list_container.move_child(hidden_player_button, child_index)
		child_index += 1


func _entry_matches_filters(entry: Dictionary) -> bool:
	var search_query: String = _search_input.text.strip_edges().to_lower() if _search_input else ""
	if not search_query.is_empty():
		var name_lower: String = SafeTypeUtils.string(entry.get("name_lower", ""), "")
		var catalog_lower: String = SafeTypeUtils.string(entry.get("catalog_id_lower", ""), "")
		var matches_query: bool = name_lower.contains(search_query) or catalog_lower.contains(search_query)
		if not matches_query:
			return false

	var type_filter: String = FILTER_ALL
	if _type_filter:
		type_filter = _get_option_value(_type_filter, _type_filter.selected)
	var card_type: int = SafeTypeUtils.int_val(
		entry.get("card_type", UnitConstants.CardType.SUMMON),
		UnitConstants.CardType.SUMMON
	)
	if type_filter == FILTER_TYPE_SUMMON and card_type != UnitConstants.CardType.SUMMON:
		return false
	if type_filter == FILTER_TYPE_SPELL and card_type != UnitConstants.CardType.SPELL:
		return false

	var element_filter: String = FILTER_ALL
	if _element_filter:
		element_filter = _get_option_value(_element_filter, _element_filter.selected)
	if element_filter != FILTER_ALL:
		var element_id: String = SafeTypeUtils.string(entry.get("element", ""), "")
		if element_id != element_filter:
			return false

	var role_filter: String = FILTER_ALL
	if _role_filter:
		role_filter = _get_option_value(_role_filter, _role_filter.selected)
	if role_filter != FILTER_ALL:
		var role_id: String = SafeTypeUtils.string(entry.get("role", ""), "")
		if role_id != role_filter:
			return false

	return true


func _is_entry_before(a: Dictionary, b: Dictionary) -> bool:
	var name_a: String = SafeTypeUtils.string(a.get("name", ""), "")
	var name_b: String = SafeTypeUtils.string(b.get("name", ""), "")

	match _sort_mode:
		SORT_MANA_ASC:
			var mana_a: float = SafeTypeUtils.float_val(a.get("mana_cost", 0.0), 0.0)
			var mana_b: float = SafeTypeUtils.float_val(b.get("mana_cost", 0.0), 0.0)
			if not is_equal_approx(mana_a, mana_b):
				return mana_a < mana_b
			return name_a.nocasecmp_to(name_b) < 0
		SORT_MANA_DESC:
			var mana_a_desc: float = SafeTypeUtils.float_val(a.get("mana_cost", 0.0), 0.0)
			var mana_b_desc: float = SafeTypeUtils.float_val(b.get("mana_cost", 0.0), 0.0)
			if not is_equal_approx(mana_a_desc, mana_b_desc):
				return mana_a_desc > mana_b_desc
			return name_a.nocasecmp_to(name_b) < 0
		_:
			return name_a.nocasecmp_to(name_b) < 0


func _load_debug_deck() -> Array:
	if not _debug_deck_entries_override.is_empty():
		return _debug_deck_entries_override.duplicate(true)

	# Try to load from deck file
	var file: FileAccess = FileAccess.open(DEBUG_DECK_PATH, FileAccess.READ)
	if file:
		var json_text: String = file.get_as_text()
		file.close()
		var parsed: Variant = JSON.parse_string(json_text)
		if parsed is Array:
			return parsed

	# Fallback: create entries for all catalog summons
	push_warning("UnitSpawnerPanel: Debug deck not found, using all catalog summons")
	var entries: Array = []
	var all_cards: Array[Dictionary] = CardCatalogApi.get_all_cards_as_dict()
	for card_def: Dictionary in all_cards:
		if card_def.get("card_type") == UnitConstants.CardType.SUMMON:
			entries.append({"catalog_id": card_def.get("catalog_id", ""), "count": 1})
	return entries


func get_skip_prep_phase() -> bool:
	return _skip_prep_phase


func get_enemy_ai_enabled() -> bool:
	return _enemy_ai_enabled


func get_player_ai_enabled() -> bool:
	return _player_ai_enabled


func get_player_hold_advance_enabled() -> bool:
	return _player_hold_advance_enabled


func get_spawn_settings() -> Dictionary:
	return {
		"spawn_mode": _spawn_mode,
		"burst_count": _burst_count,
		"formation_mode": _formation_mode,
		"formation_spacing": _formation_spacing
	}


func set_debug_deck_entries(entries: Array) -> void:
	_debug_deck_entries_override = entries.duplicate(true)
	_rebuild_unit_lists()


func append_spawn_log(message: String) -> void:
	if not _spawn_log:
		return
	var timestamp: String = Time.get_time_string_from_system()
	_spawn_log.append_text("[%s] %s\n" % [timestamp, message])
	_spawn_log.scroll_to_line(_spawn_log.get_line_count())


func _on_spawn_mode_changed(index: int) -> void:
	_spawn_mode = _get_option_value(_spawn_mode_filter, index)
	if _spawn_mode.is_empty():
		_spawn_mode = SPAWN_MODE_SINGLE
	_refresh_spawn_controls_visibility()
	_save_settings()


func _on_burst_count_changed(value: float) -> void:
	_burst_count = maxi(1, int(round(value)))
	_save_settings()


func _on_formation_mode_changed(index: int) -> void:
	_formation_mode = _get_option_value(_formation_mode_filter, index)
	if _formation_mode.is_empty():
		_formation_mode = FORMATION_STACK
	_save_settings()


func _on_formation_spacing_changed(value: float) -> void:
	_formation_spacing = value
	_refresh_formation_spacing_label()
	_save_settings()


func _on_advanced_toggle_pressed() -> void:
	if _panel_collapsed:
		return
	_set_advanced_controls_open(not _advanced_controls_open)


func _set_advanced_controls_open(open: bool) -> void:
	if _panel_collapsed:
		open = false
	_advanced_controls_open = open
	_update_advanced_overlay_layout()
	if _advanced_drawer_panel:
		_advanced_drawer_panel.visible = _advanced_controls_open
	if _advanced_toggle_button:
		_advanced_toggle_button.visible = not _panel_collapsed
		_advanced_toggle_button.text = ">" if _advanced_controls_open else "<"
		_advanced_toggle_button.tooltip_text = (
			Loc.t("debug.spawner.advanced_hide")
			if _advanced_controls_open
			else Loc.t("debug.spawner.advanced_show")
		)


func _on_panel_collapse_pressed() -> void:
	_set_panel_collapsed(not _panel_collapsed)


func _set_panel_collapsed(collapsed: bool) -> void:
	_panel_collapsed = collapsed
	if _panel_collapsed:
		_set_advanced_controls_open(false)
		offset_left = offset_right - PANEL_COLLAPSED_WIDTH
		offset_bottom = offset_top + PANEL_COLLAPSED_HEIGHT
	else:
		_restore_expanded_offsets()
	if _panel_margin:
		_panel_margin.visible = not _panel_collapsed
	if _advanced_toggle_button:
		_advanced_toggle_button.visible = not _panel_collapsed
	_update_advanced_overlay_layout()
	_update_panel_collapse_button()


func _update_panel_collapse_button() -> void:
	if not _panel_collapse_button:
		return
	_panel_collapse_button.text = "+" if _panel_collapsed else "-"
	_panel_collapse_button.tooltip_text = (
		Loc.t("debug.spawner.expand_window")
		if _panel_collapsed
		else Loc.t("debug.spawner.collapse_window")
	)


func _update_advanced_overlay_layout() -> void:
	var panel_rect: Rect2 = get_global_rect()
	if _panel_collapse_button:
		_panel_collapse_button.position = Vector2(
			panel_rect.position.x + panel_rect.size.x - PANEL_COLLAPSE_BUTTON_WIDTH - PANEL_COLLAPSE_BUTTON_RIGHT_INSET,
			panel_rect.position.y + PANEL_COLLAPSE_BUTTON_TOP
		)

	if _panel_collapsed:
		if _advanced_drawer_panel:
			_advanced_drawer_panel.visible = false
		if _advanced_toggle_button:
			_advanced_toggle_button.visible = false
		return

	if _advanced_drawer_panel:
		_advanced_drawer_panel.position = Vector2(
			panel_rect.position.x - ADVANCED_DRAWER_WIDTH - ADVANCED_DRAWER_GAP,
			panel_rect.position.y
		)
		_advanced_drawer_panel.size = Vector2(ADVANCED_DRAWER_WIDTH, panel_rect.size.y)
	if _advanced_toggle_button:
		_advanced_toggle_button.position = Vector2(
			panel_rect.position.x - ADVANCED_TOGGLE_WIDTH,
			panel_rect.position.y + ADVANCED_TOGGLE_TOP
		)


func _refresh_spawn_controls_visibility() -> void:
	var multi_spawn: bool = _spawn_mode == SPAWN_MODE_BURST or _spawn_mode == SPAWN_MODE_PAINT
	if _burst_count_spinner:
		_burst_count_spinner.editable = _spawn_mode == SPAWN_MODE_BURST
		_burst_count_spinner.modulate.a = 1.0 if _spawn_mode == SPAWN_MODE_BURST else 0.65
	if _formation_mode_filter:
		_formation_mode_filter.disabled = not multi_spawn
	if _formation_spacing_slider:
		_formation_spacing_slider.editable = multi_spawn
		_formation_spacing_slider.modulate.a = 1.0 if multi_spawn else 0.65
	if _formation_spacing_label:
		_formation_spacing_label.modulate.a = 1.0 if multi_spawn else 0.65


func _refresh_formation_spacing_label() -> void:
	if _formation_spacing_label:
		_formation_spacing_label.text = Loc.t("debug.spawner.formation_spacing") + ": %.1f" % _formation_spacing


func _on_skip_prep_toggled(pressed: bool) -> void:
	_skip_prep_phase = pressed
	_save_settings()
	skip_prep_toggled.emit(pressed)


func _on_enemy_ai_toggled(pressed: bool) -> void:
	_enemy_ai_enabled = pressed
	_save_settings()
	enemy_ai_toggled.emit(pressed)


func _on_player_ai_toggled(pressed: bool) -> void:
	_player_ai_enabled = pressed
	_save_settings()
	player_ai_toggled.emit(pressed)


func _on_player_hold_advance_toggled(pressed: bool) -> void:
	_player_hold_advance_enabled = pressed
	_save_settings()
	player_hold_advance_toggled.emit(pressed)


func _on_clear_player_pressed() -> void:
	clear_team_requested.emit(TEAM_PLAYER)


func _on_clear_enemy_pressed() -> void:
	clear_team_requested.emit(TEAM_ENEMY)


func _on_clear_pressed() -> void:
	clear_requested.emit()


func _on_undo_pressed() -> void:
	undo_requested.emit()


func _load_settings() -> void:
	var config: ConfigFile = ConfigFile.new()
	var err: Error = config.load(SETTINGS_PATH)
	if err == OK:
		_skip_prep_phase = config.get_value("debug_arena", "skip_prep_phase", false)
		_enemy_ai_enabled = config.get_value("debug_arena", "enemy_ai_enabled", false)
		_player_ai_enabled = config.get_value("debug_arena", "player_ai_enabled", false)
		_player_hold_advance_enabled = config.get_value("debug_arena", "player_hold_advance_enabled", false)
		_spawn_mode = config.get_value("debug_arena", "spawn_mode", SPAWN_MODE_SINGLE)
		_burst_count = config.get_value("debug_arena", "burst_count", 3)
		_formation_mode = config.get_value("debug_arena", "formation_mode", FORMATION_STACK)
		_formation_spacing = config.get_value("debug_arena", "formation_spacing", 2.0)


func _save_settings() -> void:
	var config: ConfigFile = ConfigFile.new()
	config.set_value("debug_arena", "skip_prep_phase", _skip_prep_phase)
	config.set_value("debug_arena", "enemy_ai_enabled", _enemy_ai_enabled)
	config.set_value("debug_arena", "player_ai_enabled", _player_ai_enabled)
	config.set_value("debug_arena", "player_hold_advance_enabled", _player_hold_advance_enabled)
	config.set_value("debug_arena", "spawn_mode", _spawn_mode)
	config.set_value("debug_arena", "burst_count", _burst_count)
	config.set_value("debug_arena", "formation_mode", _formation_mode)
	config.set_value("debug_arena", "formation_spacing", _formation_spacing)
	config.save(SETTINGS_PATH)


func _create_panel_style() -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.1, 0.1, 0.15, 0.95)
	style.border_color = GameColorPalette.BUTTON_SECONDARY_BORDER
	style.set_border_width_all(2)
	style.set_corner_radius_all(8)
	return style
