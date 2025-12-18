extends Control
class_name SummonerManagementPanel

## SummonerManagementPanel - Full-screen summoner showcase
##
## Displays the active summoner's portrait, stats, traits, and progression.
## Allows leveling up and switching to a different summoner.

## Signals
signal closed()

## UI Node References - Main View
@onready var background: ColorRect = %Background
@onready var portrait_rect: ColorRect = %PortraitRect
@onready var element_label: Label = %ElementLabel
@onready var summoner_name_label: Label = %SummonerNameLabel
@onready var level_label: Label = %LevelLabel
@onready var element_name_label: Label = %ElementNameLabel

@onready var hp_value: Label = %HPValue
@onready var mana_value: Label = %ManaValue

@onready var xp_label: Label = %XPLabel
@onready var xp_progress_bar: ProgressBar = %XPProgressBar

@onready var traits_container: VBoxContainer = %TraitsContainer
@onready var level_up_button: Button = %LevelUpButton
@onready var level_up_preview: Label = %LevelUpPreview
@onready var switch_summoner_button: Button = %SwitchSummonerButton
@onready var close_button: Button = %CloseButton

## Summoner Picker Panel
@onready var summoner_picker_panel: PanelContainer = %SummonerPickerPanel
@onready var summoner_picker_list: VBoxContainer = %SummonerPickerList
@onready var picker_close_button: Button = %PickerCloseButton


## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Connect buttons
	close_button.pressed.connect(_on_close_pressed)
	background.gui_input.connect(_on_background_input)
	level_up_button.pressed.connect(_on_level_up_pressed)
	switch_summoner_button.pressed.connect(_on_switch_summoner_pressed)
	picker_close_button.pressed.connect(_on_picker_close_pressed)

	# Connect to summoner selection changes
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if summoner_selection and summoner_selection.has_signal("summoner_changed"):
		summoner_selection.summoner_changed.connect(_on_summoner_changed)

	# Connect to economy for gold updates
	var economy: Node = get_node_or_null("/root/Economy")
	if economy and economy.has_signal("gold_changed"):
		economy.gold_changed.connect(_on_gold_changed)

	# Hide summoner picker initially
	summoner_picker_panel.visible = false

## =============================================================================
## PUBLIC API
## =============================================================================

func open() -> void:
	_refresh_display()
	show()

## =============================================================================
## DISPLAY
## =============================================================================

func _refresh_display() -> void:
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if not summoner_selection:
		push_error("SummonerManagementPanel: SummonerSelection service not found")
		return

	var summoner_id: String = ""
	if summoner_selection.has_method("get_active_summoner_id"):
		var result: Variant = summoner_selection.call("get_active_summoner_id")
		if result is String:
			summoner_id = result

	if summoner_id.is_empty():
		_show_no_summoner()
		return

	# Get summoner config
	var config: SummonerConfig = SummonerCatalog.get_summoner_config(summoner_id)
	if not config:
		_show_no_summoner()
		return

	# Get progression info
	var progression_node: Node = get_node_or_null("/root/SummonerProgression")
	var info: Dictionary = {}
	if progression_node and progression_node.has_method("get_summoner_progression_info"):
		info = progression_node.call("get_summoner_progression_info", summoner_id)

	var level: int = info.get("level", 1)
	var current_xp: int = info.get("xp", 0)
	var xp_for_next: int = info.get("xp_for_next_level", 100)
	var xp_progress: float = info.get("xp_progress", 0.0)
	var can_level_up: bool = info.get("can_level_up", false)
	var can_afford: bool = info.get("can_afford_level_up", false)
	var gold_cost: int = info.get("level_up_gold_cost", 0)
	var is_max_level: bool = info.get("is_max_level", false)

	# Get element
	var element: ElementTypes.Element = config.get_element()

	# Update portrait using centralized ElementTypes constants
	portrait_rect.color = ElementTypes.get_color(element)
	element_label.text = ElementTypes.get_symbol(element)

	# Update summoner info
	summoner_name_label.text = config.summoner_name
	level_label.text = Loc.t("ui.summoner_panel.level_display", {"level": level})
	element_name_label.text = ElementTypes.get_display_name(element)

	# Update stats
	var computed_stats: Dictionary = _get_computed_stats(summoner_id)
	var hp: float = computed_stats.get("health", config.base_health)
	var mana: float = computed_stats.get("max_mana", config.max_mana)

	hp_value.text = str(int(hp))
	mana_value.text = str(int(mana))

	# Update XP
	if is_max_level:
		xp_label.text = Loc.t("ui.summoner_panel.level_up_max")
		xp_progress_bar.value = 100.0
	else:
		xp_label.text = Loc.t("ui.summoner_panel.xp_progress", {"current": current_xp, "required": xp_for_next})
		xp_progress_bar.value = xp_progress * 100.0

	# Update level up button and preview
	_update_level_up_display(is_max_level, can_level_up, can_afford, gold_cost, config, level)

	# Update traits
	_refresh_traits(summoner_id, config)

func _show_no_summoner() -> void:
	summoner_name_label.text = Loc.t("ui.summoner_icon.no_summoner")
	level_label.text = ""
	element_name_label.text = ""
	portrait_rect.color = ElementTypes.get_color("neutral")
	element_label.text = ElementTypes.get_symbol("neutral")
	hp_value.text = "-"
	mana_value.text = "-"
	xp_label.text = ""
	xp_progress_bar.value = 0
	level_up_button.disabled = true
	level_up_button.text = "-"
	level_up_preview.text = ""

	# Clear traits
	for child: Node in traits_container.get_children():
		child.queue_free()

func _get_computed_stats(summoner_id: String) -> Dictionary:
	var summoner_instance_data: Dictionary = ProfileRepo.get_summoner_instance(summoner_id)
	if summoner_instance_data.is_empty():
		return {}

	var summoner_instance: SummonerInstance = SummonerInstance.from_dict(summoner_instance_data)
	if not summoner_instance:
		return {}

	return summoner_instance.get_computed_stats()

func _update_level_up_display(is_max: bool, can_level: bool, can_afford: bool, cost: int, config: SummonerConfig, current_level: int) -> void:
	if is_max:
		level_up_button.text = Loc.t("ui.summoner_panel.level_up_max")
		level_up_button.disabled = true
		level_up_preview.text = ""
	elif not can_level:
		level_up_button.text = Loc.t("ui.summoner_panel.level_up_locked")
		level_up_button.disabled = true
		level_up_preview.text = ""
	elif not can_afford:
		level_up_button.text = Loc.t("ui.summoner_panel.level_up_button", {"cost": cost})
		level_up_button.disabled = true
		level_up_button.add_theme_color_override("font_color", Color(0.7, 0.3, 0.3))
		level_up_preview.text = _get_level_up_preview(config)
	else:
		level_up_button.text = Loc.t("ui.summoner_panel.level_up_button", {"cost": cost})
		level_up_button.disabled = false
		level_up_button.remove_theme_color_override("font_color")
		level_up_preview.text = _get_level_up_preview(config)

func _get_level_up_preview(config: SummonerConfig) -> String:
	# Simple preview - each level gives +5% to base stats
	var hp_bonus: int = int(config.base_health * 0.05)
	var mana_bonus: float = config.max_mana * 0.05

	return Loc.t("ui.summoner_panel.level_up_preview", {
		"hp": hp_bonus,
		"mana": "%.1f" % mana_bonus
	})

func _refresh_traits(summoner_id: String, config: SummonerConfig) -> void:
	# Clear existing traits
	for child: Node in traits_container.get_children():
		child.queue_free()

	# Get all trait IDs (innate + acquired)
	var trait_ids: Array[String] = []

	# Add innate traits from config
	for trait_id: String in config.innate_trait_ids:
		trait_ids.append(trait_id)

	# Add acquired boons from summoner instance
	var summoner_instance_data: Dictionary = ProfileRepo.get_summoner_instance(summoner_id)
	if not summoner_instance_data.is_empty():
		var summoner_instance: SummonerInstance = SummonerInstance.from_dict(summoner_instance_data)
		if summoner_instance:
			for boon_id: String in summoner_instance.acquired_boon_ids:
				if not boon_id in trait_ids:
					trait_ids.append(boon_id)

	# Create trait displays
	var trait_catalog: Node = get_node_or_null("/root/TraitCatalog")
	if not trait_catalog:
		return

	for trait_id: String in trait_ids:
		var trait_item: PanelContainer = _create_trait_item(trait_catalog, trait_id)
		if trait_item:
			traits_container.add_child(trait_item)

	# Show message if no traits
	if trait_ids.is_empty():
		var no_traits_label: Label = Label.new()
		no_traits_label.text = Loc.t("ui.summoner_panel.no_traits")
		no_traits_label.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5))
		traits_container.add_child(no_traits_label)

func _create_trait_item(trait_catalog: Node, trait_id: String) -> PanelContainer:
	var trait_name: String = ""
	var trait_desc: String = ""

	if trait_catalog.has_method("get_trait_name"):
		trait_name = trait_catalog.call("get_trait_name", trait_id)
	if trait_catalog.has_method("get_trait_description"):
		trait_desc = trait_catalog.call("get_trait_description", trait_id)

	if trait_name.is_empty():
		trait_name = trait_id

	var panel: PanelContainer = PanelContainer.new()
	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 10)
	margin.add_theme_constant_override("margin_top", 5)
	margin.add_theme_constant_override("margin_right", 10)
	margin.add_theme_constant_override("margin_bottom", 5)
	panel.add_child(margin)

	var vbox: VBoxContainer = VBoxContainer.new()
	margin.add_child(vbox)

	var name_label: Label = Label.new()
	name_label.text = trait_name
	name_label.add_theme_font_size_override("font_size", 16)
	name_label.add_theme_color_override("font_color", Color(1.0, 0.9, 0.5))
	vbox.add_child(name_label)

	var desc_label: Label = Label.new()
	desc_label.text = trait_desc
	desc_label.add_theme_font_size_override("font_size", 12)
	desc_label.add_theme_color_override("font_color", Color(0.7, 0.7, 0.7))
	desc_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	vbox.add_child(desc_label)

	return panel

## =============================================================================
## SUMMONER PICKER
## =============================================================================

func _show_summoner_picker() -> void:
	_populate_summoner_picker()
	summoner_picker_panel.visible = true

func _hide_summoner_picker() -> void:
	summoner_picker_panel.visible = false

func _populate_summoner_picker() -> void:
	# Clear existing items
	for child: Node in summoner_picker_list.get_children():
		child.queue_free()

	# Get unlocked summoners
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if not summoner_selection:
		return

	var unlocked_ids: Array[String] = []
	if summoner_selection.has_method("get_unlocked_summoner_ids"):
		var result: Variant = summoner_selection.call("get_unlocked_summoner_ids")
		if result is Array:
			for id: Variant in result:
				if id is String:
					unlocked_ids.append(id)

	var active_summoner_id: String = ""
	if summoner_selection.has_method("get_active_summoner_id"):
		var result: Variant = summoner_selection.call("get_active_summoner_id")
		if result is String:
			active_summoner_id = result

	# Create summoner picker items
	for summoner_id: String in unlocked_ids:
		var item: Button = _create_summoner_picker_item(summoner_id, summoner_id == active_summoner_id)
		summoner_picker_list.add_child(item)

func _create_summoner_picker_item(summoner_id: String, is_active: bool) -> Button:
	var config: SummonerConfig = SummonerCatalog.get_summoner_config(summoner_id)
	var summoner_name: String = config.summoner_name if config else summoner_id

	var button: Button = Button.new()
	button.text = summoner_name + (Loc.t("ui.summoner_panel.picker_active_suffix") if is_active else "")
	button.custom_minimum_size = Vector2(200, 40)
	button.add_theme_font_size_override("font_size", 18)
	button.disabled = is_active

	if not is_active:
		button.pressed.connect(_on_summoner_picker_selected.bind(summoner_id))

	return button

## =============================================================================
## EVENT HANDLERS
## =============================================================================

func _on_close_pressed() -> void:
	_close()

func _on_background_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			# Only close if picker is not visible
			if not summoner_picker_panel.visible:
				_close()

func _on_level_up_pressed() -> void:
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if not summoner_selection:
		return

	var summoner_id: String = ""
	if summoner_selection.has_method("get_active_summoner_id"):
		var result: Variant = summoner_selection.call("get_active_summoner_id")
		if result is String:
			summoner_id = result

	if summoner_id.is_empty():
		return

	var progression_node: Node = get_node_or_null("/root/SummonerProgression")
	if progression_node and progression_node.has_method("level_up_summoner"):
		var success: Variant = progression_node.call("level_up_summoner", summoner_id)
		if success is bool and success:
			_refresh_display()

func _on_switch_summoner_pressed() -> void:
	_show_summoner_picker()

func _on_picker_close_pressed() -> void:
	_hide_summoner_picker()

func _on_summoner_picker_selected(summoner_id: String) -> void:
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if summoner_selection and summoner_selection.has_method("set_active_summoner"):
		var success: Variant = summoner_selection.call("set_active_summoner", summoner_id)
		if success is bool and success:
			_hide_summoner_picker()
			_refresh_display()

func _on_summoner_changed(_old_summoner_id: String, _new_summoner_id: String) -> void:
	_refresh_display()

func _on_gold_changed(_new_gold: int) -> void:
	_refresh_display()

func _close() -> void:
	closed.emit()
	hide()
	queue_free()
