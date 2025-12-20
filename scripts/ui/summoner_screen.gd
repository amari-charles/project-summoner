extends Control
class_name SummonerScreen

## SummonerScreen - Full-screen summoner management interface
##
## Displays the active summoner's portrait, stats, traits, and progression.
## Allows switching between summoners and leveling up.

## =============================================================================
## SIGNALS
## =============================================================================

signal closed()

## =============================================================================
## CONSTANTS
## =============================================================================

const TWEEN_DURATION: float = 0.1

## =============================================================================
## NODE REFERENCES - Header
## =============================================================================

@onready var close_button: Button = %CloseButton
@onready var title_label: Label = %TitleLabel
@onready var gold_label: Label = %GoldLabel

## =============================================================================
## NODE REFERENCES - Summoner List Panel (Left)
## =============================================================================

@onready var summoner_list_container: VBoxContainer = %SummonerListContainer

## =============================================================================
## NODE REFERENCES - Details Panel (Right)
## =============================================================================

@onready var portrait_container: CenterContainer = %PortraitContainer
@onready var portrait_frame: PanelContainer = %PortraitFrame
@onready var portrait_background: ColorRect = %PortraitBackground
@onready var portrait_glow: ColorRect = %PortraitGlow
@onready var portrait_symbol: Label = %PortraitSymbol

@onready var summoner_name_label: Label = %SummonerNameLabel
@onready var element_label: Label = %ElementLabel
@onready var level_label: Label = %LevelLabel

@onready var hp_value_label: Label = %HPValueLabel
@onready var mana_value_label: Label = %ManaValueLabel

@onready var xp_label: Label = %XPLabel
@onready var xp_progress_bar: ProgressBar = %XPProgressBar
@onready var level_up_button: Button = %LevelUpButton
@onready var level_up_preview: Label = %LevelUpPreview

## =============================================================================
## NODE REFERENCES - Traits Section (Bottom)
## =============================================================================

@onready var traits_container: HBoxContainer = %TraitsContainer

## =============================================================================
## STATE
## =============================================================================

var _selected_summoner_id: String = ""
var _portrait_tween: Tween = null


## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Connect header buttons
	close_button.pressed.connect(_on_close_pressed)
	level_up_button.pressed.connect(_on_level_up_pressed)

	# Connect to service signals
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if summoner_selection and summoner_selection.has_signal("summoner_changed"):
		summoner_selection.summoner_changed.connect(_on_summoner_changed)

	var economy: Node = get_node_or_null("/root/Economy")
	if economy and economy.has_signal("gold_changed"):
		economy.gold_changed.connect(_on_gold_changed)

	# Initial data load
	_refresh_gold_display()
	_refresh_summoner_list()

	# Select active summoner
	if summoner_selection and summoner_selection.has_method("get_active_summoner_id"):
		var result: Variant = summoner_selection.call("get_active_summoner_id")
		if result is String and not result.is_empty():
			_select_summoner(result)


## =============================================================================
## GOLD DISPLAY
## =============================================================================

func _refresh_gold_display() -> void:
	var economy: Node = get_node_or_null("/root/Economy")
	if economy and economy.has_method("get_gold"):
		var gold: int = economy.call("get_gold")
		gold_label.text = Loc.t("ui.summoner_screen.gold_display", {"gold": gold})
	else:
		gold_label.text = ""


## =============================================================================
## SUMMONER LIST
## =============================================================================

func _refresh_summoner_list() -> void:
	# Clear existing items
	for child: Node in summoner_list_container.get_children():
		child.queue_free()

	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if not summoner_selection:
		return

	# Get unlocked summoners
	var unlocked_ids: Array[String] = []
	if summoner_selection.has_method("get_unlocked_summoner_ids"):
		var result: Variant = summoner_selection.call("get_unlocked_summoner_ids")
		if result is Array:
			for id: Variant in result:
				if id is String:
					unlocked_ids.append(id)

	# Get active summoner ID
	var active_id: String = ""
	if summoner_selection.has_method("get_active_summoner_id"):
		var result: Variant = summoner_selection.call("get_active_summoner_id")
		if result is String:
			active_id = result

	# Create list items
	for summoner_id: String in unlocked_ids:
		var item: PanelContainer = _create_summoner_list_item(summoner_id, summoner_id == active_id)
		summoner_list_container.add_child(item)


func _create_summoner_list_item(summoner_id: String, is_active: bool) -> PanelContainer:
	var config: SummonerConfig = SummonerCatalog.get_summoner_config(summoner_id)
	var summoner_name: String = config.summoner_name if config else summoner_id

	# Get level
	var level: int = 1
	var summoner_progression: Node = get_node_or_null("/root/SummonerProgression")
	if summoner_progression and summoner_progression.has_method("get_summoner_progression_info"):
		var info: Dictionary = summoner_progression.call("get_summoner_progression_info", summoner_id)
		level = info.get("level", 1)

	# Get element
	var element: ElementTypes.Element = config.get_element() if config else ElementTypes.NEUTRAL
	var element_color: Color = ElementTypes.get_color(element)

	# Create panel
	var panel: PanelContainer = PanelContainer.new()
	panel.custom_minimum_size = Vector2(220, 80)

	# Style the panel
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.15, 0.15, 0.2, 1.0)
	style.border_color = element_color if is_active else Color(0.3, 0.3, 0.35, 1.0)
	style.border_width_left = 4 if is_active else 2
	style.border_width_right = 2
	style.border_width_top = 2
	style.border_width_bottom = 2
	style.corner_radius_all = 8
	panel.add_theme_stylebox_override("panel", style)

	# Margin container
	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 12)
	margin.add_theme_constant_override("margin_right", 12)
	margin.add_theme_constant_override("margin_top", 8)
	margin.add_theme_constant_override("margin_bottom", 8)
	panel.add_child(margin)

	# HBox for layout
	var hbox: HBoxContainer = HBoxContainer.new()
	hbox.add_theme_constant_override("separation", 12)
	margin.add_child(hbox)

	# Mini portrait
	var portrait: ColorRect = ColorRect.new()
	portrait.custom_minimum_size = Vector2(50, 50)
	portrait.color = element_color
	hbox.add_child(portrait)

	# Symbol on portrait
	var symbol: Label = Label.new()
	symbol.text = ElementTypes.get_symbol(element)
	symbol.add_theme_font_size_override("font_size", 24)
	symbol.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	symbol.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	symbol.anchors_preset = Control.PRESET_FULL_RECT
	portrait.add_child(symbol)

	# Info VBox
	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hbox.add_child(vbox)

	# Name row
	var name_hbox: HBoxContainer = HBoxContainer.new()
	vbox.add_child(name_hbox)

	var name_label: Label = Label.new()
	name_label.text = summoner_name
	name_label.add_theme_font_size_override("font_size", 18)
	name_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	name_hbox.add_child(name_label)

	if is_active:
		var active_label: Label = Label.new()
		active_label.text = Loc.t("ui.summoner_screen.active_indicator")
		active_label.add_theme_font_size_override("font_size", 12)
		active_label.add_theme_color_override("font_color", Color(0.3, 0.9, 0.4))
		name_hbox.add_child(active_label)

	# Level label
	var level_lbl: Label = Label.new()
	level_lbl.text = Loc.t("ui.summoner_panel.level_display", {"level": level})
	level_lbl.add_theme_font_size_override("font_size", 14)
	level_lbl.add_theme_color_override("font_color", Color(0.7, 0.7, 0.7))
	vbox.add_child(level_lbl)

	# Make clickable with button overlay
	var button: Button = Button.new()
	button.flat = true
	button.anchors_preset = Control.PRESET_FULL_RECT
	button.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	button.pressed.connect(_on_summoner_list_item_pressed.bind(summoner_id))
	panel.add_child(button)

	return panel


func _on_summoner_list_item_pressed(summoner_id: String) -> void:
	_select_summoner(summoner_id)

	# Also set as active summoner
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if summoner_selection and summoner_selection.has_method("set_active_summoner"):
		summoner_selection.call("set_active_summoner", summoner_id)


## =============================================================================
## SUMMONER DETAILS
## =============================================================================

func _select_summoner(summoner_id: String) -> void:
	_selected_summoner_id = summoner_id
	_refresh_details()


func _refresh_details() -> void:
	if _selected_summoner_id.is_empty():
		_show_no_summoner()
		return

	var config: SummonerConfig = SummonerCatalog.get_summoner_config(_selected_summoner_id)
	if not config:
		_show_no_summoner()
		return

	# Get progression info
	var summoner_progression: Node = get_node_or_null("/root/SummonerProgression")
	var info: Dictionary = {}
	if summoner_progression and summoner_progression.has_method("get_summoner_progression_info"):
		info = summoner_progression.call("get_summoner_progression_info", _selected_summoner_id)

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
	var element_color: Color = ElementTypes.get_color(element)
	var gradient_colors: Array[Color] = CardVisualHelper.get_element_gradient_colors(element.id)

	# Update portrait
	_update_portrait(element, gradient_colors)

	# Update name and info
	summoner_name_label.text = config.summoner_name
	element_label.text = ElementTypes.get_display_name(element)
	element_label.add_theme_color_override("font_color", element_color)
	level_label.text = Loc.t("ui.summoner_panel.level_display", {"level": level})

	# Update stats
	var computed_stats: Dictionary = _get_computed_stats(_selected_summoner_id)
	var hp: float = computed_stats.get("health", config.base_health)
	var mana: float = computed_stats.get("max_mana", config.max_mana)
	hp_value_label.text = str(int(hp))
	mana_value_label.text = str(int(mana))

	# Update XP
	if is_max_level:
		xp_label.text = Loc.t("ui.summoner_panel.level_up_max")
		xp_progress_bar.value = 100.0
	else:
		xp_label.text = Loc.t("ui.summoner_panel.xp_progress", {"current": current_xp, "required": xp_for_next})
		xp_progress_bar.value = xp_progress * 100.0

	# Update level up button
	_update_level_up_display(is_max_level, can_level_up, can_afford, gold_cost, config)

	# Update traits
	_refresh_traits()

	# Refresh summoner list to update active indicator
	_refresh_summoner_list()


func _update_portrait(element: ElementTypes.Element, gradient_colors: Array[Color]) -> void:
	# Get element colors
	var element_color: Color = ElementTypes.get_color(element)
	var border_color: Color = CardVisualHelper.get_element_border_color(element.id)

	# Style the frame with element-themed border
	var frame_style: StyleBoxFlat = StyleBoxFlat.new()
	frame_style.bg_color = Color(0.1, 0.1, 0.12, 1.0)
	frame_style.border_color = border_color
	frame_style.border_width_all = 4
	frame_style.corner_radius_all = 12
	frame_style.shadow_color = border_color * Color(1, 1, 1, 0.3)
	frame_style.shadow_size = 8
	portrait_frame.add_theme_stylebox_override("panel", frame_style)

	# Set background color (darker gradient color)
	portrait_background.color = gradient_colors[0] if gradient_colors.size() > 0 else element_color

	# Set glow color (lighter/brighter)
	var glow_color: Color = CardVisualHelper.get_element_glow_color(element.id)
	glow_color.a = 0.5
	portrait_glow.color = glow_color

	# Set symbol
	portrait_symbol.text = ElementTypes.get_symbol(element)

	# Start breathing animation
	_start_portrait_breathing()


func _start_portrait_breathing() -> void:
	# Kill existing tween if any
	if _portrait_tween and _portrait_tween.is_valid():
		_portrait_tween.kill()

	# Create subtle breathing effect on the glow
	_portrait_tween = create_tween()
	_portrait_tween.set_loops()

	# Pulse the glow opacity and scale slightly
	var original_alpha: float = portrait_glow.color.a
	var pulse_alpha: float = original_alpha + 0.15

	_portrait_tween.tween_property(portrait_glow, "color:a", pulse_alpha, 1.5).set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)
	_portrait_tween.tween_property(portrait_glow, "color:a", original_alpha, 1.5).set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)


func _exit_tree() -> void:
	# Clean up tween to prevent errors
	if _portrait_tween and _portrait_tween.is_valid():
		_portrait_tween.kill()


func _get_computed_stats(summoner_id: String) -> Dictionary:
	var summoner_instance_data: Dictionary = ProfileRepo.get_summoner_instance(summoner_id)
	if summoner_instance_data.is_empty():
		return {}

	var summoner_instance: SummonerInstance = SummonerInstance.from_dict(summoner_instance_data)
	if not summoner_instance:
		return {}

	return summoner_instance.get_computed_stats()


func _show_no_summoner() -> void:
	summoner_name_label.text = Loc.t("ui.summoner_icon.no_summoner")
	element_label.text = ""
	level_label.text = ""
	portrait_background.color = ElementTypes.get_color("neutral")
	portrait_glow.color = Color(0.5, 0.5, 0.5, 0.2)
	portrait_symbol.text = "?"
	hp_value_label.text = "-"
	mana_value_label.text = "-"
	xp_label.text = ""
	xp_progress_bar.value = 0
	level_up_button.disabled = true
	level_up_button.text = "-"
	level_up_preview.text = ""

	# Clear traits
	for child: Node in traits_container.get_children():
		child.queue_free()


func _update_level_up_display(is_max: bool, can_level: bool, can_afford: bool, cost: int, config: SummonerConfig) -> void:
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
	var hp_bonus: int = int(config.base_health * 0.05)
	var mana_bonus: float = config.max_mana * 0.05
	return Loc.t("ui.summoner_panel.level_up_preview", {
		"hp": hp_bonus,
		"mana": "%.1f" % mana_bonus
	})


## =============================================================================
## TRAITS
## =============================================================================

func _refresh_traits() -> void:
	# Clear existing traits
	for child: Node in traits_container.get_children():
		child.queue_free()

	if _selected_summoner_id.is_empty():
		return

	var config: SummonerConfig = SummonerCatalog.get_summoner_config(_selected_summoner_id)
	if not config:
		return

	# Get all trait IDs (innate + acquired)
	var trait_ids: Array[String] = []

	# Add innate traits from config
	for trait_id: String in config.innate_trait_ids:
		trait_ids.append(trait_id)

	# Add acquired boons from summoner instance
	var summoner_instance_data: Dictionary = ProfileRepo.get_summoner_instance(_selected_summoner_id)
	if not summoner_instance_data.is_empty():
		var summoner_instance: SummonerInstance = SummonerInstance.from_dict(summoner_instance_data)
		if summoner_instance:
			for boon_id: String in summoner_instance.acquired_boon_ids:
				if not boon_id in trait_ids:
					trait_ids.append(boon_id)

	# Create trait cards
	var trait_catalog: Node = get_node_or_null("/root/TraitCatalog")
	if not trait_catalog:
		return

	for i: int in range(trait_ids.size()):
		var trait_id: String = trait_ids[i]
		var is_innate: bool = i < config.innate_trait_ids.size()
		var trait_card: PanelContainer = _create_trait_card(trait_catalog, trait_id, is_innate)
		if trait_card:
			traits_container.add_child(trait_card)

	# Show message if no traits
	if trait_ids.is_empty():
		var no_traits_label: Label = Label.new()
		no_traits_label.text = Loc.t("ui.summoner_screen.no_traits")
		no_traits_label.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5))
		no_traits_label.add_theme_font_size_override("font_size", 16)
		traits_container.add_child(no_traits_label)


func _create_trait_card(trait_catalog: Node, trait_id: String, is_innate: bool) -> PanelContainer:
	var trait_name: String = ""
	var trait_desc: String = ""

	if trait_catalog.has_method("get_trait_name"):
		trait_name = trait_catalog.call("get_trait_name", trait_id)
	if trait_catalog.has_method("get_trait_description"):
		trait_desc = trait_catalog.call("get_trait_description", trait_id)

	if trait_name.is_empty():
		trait_name = trait_id

	# Create panel
	var panel: PanelContainer = PanelContainer.new()
	panel.custom_minimum_size = Vector2(180, 70)

	# Style
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.12, 0.12, 0.16, 1.0)
	style.border_color = Color(0.85, 0.75, 0.4) if is_innate else Color(0.4, 0.6, 0.9)
	style.border_width_all = 2
	style.corner_radius_all = 6
	panel.add_theme_stylebox_override("panel", style)

	# Margin
	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 10)
	margin.add_theme_constant_override("margin_right", 10)
	margin.add_theme_constant_override("margin_top", 8)
	margin.add_theme_constant_override("margin_bottom", 8)
	panel.add_child(margin)

	# VBox
	var vbox: VBoxContainer = VBoxContainer.new()
	margin.add_child(vbox)

	# Trait type label
	var type_label: Label = Label.new()
	type_label.text = Loc.t("ui.summoner_screen.trait_innate") if is_innate else Loc.t("ui.summoner_screen.trait_acquired")
	type_label.add_theme_font_size_override("font_size", 10)
	type_label.add_theme_color_override("font_color", Color(0.85, 0.75, 0.4) if is_innate else Color(0.4, 0.6, 0.9))
	vbox.add_child(type_label)

	# Name
	var name_label: Label = Label.new()
	name_label.text = trait_name
	name_label.add_theme_font_size_override("font_size", 14)
	name_label.add_theme_color_override("font_color", Color(1.0, 0.95, 0.8))
	vbox.add_child(name_label)

	# Description (truncated)
	var desc_label: Label = Label.new()
	var max_desc_len: int = 40
	if trait_desc.length() > max_desc_len:
		desc_label.text = trait_desc.substr(0, max_desc_len - 3) + "..."
	else:
		desc_label.text = trait_desc
	desc_label.add_theme_font_size_override("font_size", 11)
	desc_label.add_theme_color_override("font_color", Color(0.6, 0.6, 0.6))
	desc_label.autowrap_mode = TextServer.AUTOWRAP_OFF
	vbox.add_child(desc_label)

	# Tooltip for full description
	panel.tooltip_text = trait_desc

	return panel


## =============================================================================
## EVENT HANDLERS
## =============================================================================

func _on_close_pressed() -> void:
	_close()


func _on_level_up_pressed() -> void:
	if _selected_summoner_id.is_empty():
		return

	var summoner_progression: Node = get_node_or_null("/root/SummonerProgression")
	if summoner_progression and summoner_progression.has_method("level_up_summoner"):
		var success: Variant = summoner_progression.call("level_up_summoner", _selected_summoner_id)
		if success is bool and success:
			_refresh_details()
			_refresh_gold_display()


func _on_summoner_changed(_old_summoner_id: String, new_summoner_id: String) -> void:
	_select_summoner(new_summoner_id)


func _on_gold_changed(_new_gold: int) -> void:
	_refresh_gold_display()
	_refresh_details()


## =============================================================================
## NAVIGATION
## =============================================================================

func _close() -> void:
	closed.emit()
	# Return to previous screen via NavigationContext
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_CAMPAIGN_MAP
	SceneManager.transition_to(return_scene)
