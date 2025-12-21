extends Control
class_name SummonerScreen

## SummonerScreen - Full-screen summoner info and management interface
##
## Displays the active summoner's portrait, description, stats, traits, and boons.
## Allows leveling up and navigating to summoner selection screen.

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
@onready var summoner_name_label: Label = %SummonerNameLabel
@onready var element_label: Label = %ElementLabel
@onready var level_label: Label = %LevelLabel
@onready var switch_summoner_button: Button = %SwitchSummonerButton
@onready var gold_label: Label = %GoldLabel

## =============================================================================
## NODE REFERENCES - Portrait Section (Left Column)
## =============================================================================

@onready var portrait_container: CenterContainer = %PortraitContainer
@onready var portrait_frame: PanelContainer = %PortraitFrame
@onready var portrait_background: ColorRect = %PortraitBackground
@onready var portrait_glow: ColorRect = %PortraitGlow
@onready var portrait_symbol: Label = %PortraitSymbol

@onready var xp_label: Label = %XPLabel
@onready var xp_progress_bar: ProgressBar = %XPProgressBar
@onready var level_up_button: Button = %LevelUpButton
@onready var level_up_preview: Label = %LevelUpPreview

## =============================================================================
## NODE REFERENCES - Right Half
## =============================================================================

@onready var description_label: Label = %DescriptionLabel
@onready var stats_container: VBoxContainer = %StatsContainer
@onready var traits_boons_container: VBoxContainer = %TraitsBoonsContainer

## =============================================================================
## STATE
## =============================================================================

var _current_summoner_id: String = ""
var _portrait_tween: Tween = null


## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Connect header buttons
	close_button.pressed.connect(_on_close_pressed)
	level_up_button.pressed.connect(_on_level_up_pressed)
	switch_summoner_button.pressed.connect(_on_switch_summoner_pressed)

	# Connect to service signals
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if summoner_selection and summoner_selection.has_signal("summoner_changed"):
		summoner_selection.summoner_changed.connect(_on_summoner_changed)

	var economy: Node = get_node_or_null("/root/Economy")
	if economy and economy.has_signal("gold_changed"):
		economy.gold_changed.connect(_on_gold_changed)

	# Set static localized text
	switch_summoner_button.text = Loc.t("ui.summoner_screen.switch_summoner")

	# Initial data load
	_refresh_gold_display()

	# Load active summoner
	if summoner_selection and summoner_selection.has_method("get_active_summoner_id"):
		var result: Variant = summoner_selection.call("get_active_summoner_id")
		if result is String and not result.is_empty():
			_current_summoner_id = result
			_refresh_all()


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
## MAIN REFRESH
## =============================================================================

func _refresh_all() -> void:
	if _current_summoner_id.is_empty():
		_show_no_summoner()
		return

	var config: SummonerConfig = SummonerCatalog.get_summoner_config(_current_summoner_id)
	if not config:
		_show_no_summoner()
		return

	# Get progression info
	var summoner_progression: Node = get_node_or_null("/root/SummonerProgression")
	var info: Dictionary = {}
	if summoner_progression and summoner_progression.has_method("get_summoner_progression_info"):
		info = summoner_progression.call("get_summoner_progression_info", _current_summoner_id)

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

	# Update header
	summoner_name_label.text = config.summoner_name.to_upper()
	element_label.text = ElementTypes.get_display_name(element)
	element_label.add_theme_color_override("font_color", element_color)
	level_label.text = Loc.t("ui.summoner_panel.level_display", {"level": level})

	# Update portrait
	_update_portrait(element, gradient_colors)

	# Update description
	description_label.text = config.description if not config.description.is_empty() else Loc.t("ui.summoner_screen.no_description")

	# Update XP
	if is_max_level:
		xp_label.text = Loc.t("ui.summoner_panel.level_up_max")
		xp_progress_bar.value = 100.0
	else:
		xp_label.text = Loc.t("ui.summoner_panel.xp_progress", {"current": current_xp, "required": xp_for_next})
		xp_progress_bar.value = xp_progress * 100.0

	# Update level up button
	_update_level_up_display(is_max_level, can_level_up, can_afford, gold_cost, config)

	# Update stats
	_refresh_stats(config)

	# Update traits and boons (combined)
	_refresh_traits_and_boons(config)


## =============================================================================
## PORTRAIT
## =============================================================================

func _update_portrait(element: ElementTypes.Element, gradient_colors: Array[Color]) -> void:
	var element_color: Color = ElementTypes.get_color(element)
	var border_color: Color = CardVisualHelper.get_element_border_color(element.id)

	# Style the frame with element-themed border
	var frame_style: StyleBoxFlat = StyleBoxFlat.new()
	frame_style.bg_color = Color(0.1, 0.1, 0.12, 1.0)
	frame_style.border_color = border_color
	frame_style.set_border_width_all(4)
	frame_style.set_corner_radius_all(12)
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
	if _portrait_tween and _portrait_tween.is_valid():
		_portrait_tween.kill()

	_portrait_tween = create_tween()
	_portrait_tween.set_loops()

	var original_alpha: float = portrait_glow.color.a
	var pulse_alpha: float = original_alpha + 0.15

	_portrait_tween.tween_property(portrait_glow, "color:a", pulse_alpha, 1.5).set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)
	_portrait_tween.tween_property(portrait_glow, "color:a", original_alpha, 1.5).set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)


func _exit_tree() -> void:
	if _portrait_tween and _portrait_tween.is_valid():
		_portrait_tween.kill()


## =============================================================================
## LEVEL UP
## =============================================================================

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
## STATS
## =============================================================================

func _refresh_stats(config: SummonerConfig) -> void:
	# Clear existing stats
	for child: Node in stats_container.get_children():
		child.queue_free()

	# Get computed stats
	var computed_stats: Dictionary = _get_computed_stats(_current_summoner_id)
	var hp: float = computed_stats.get("health", config.base_health)
	var mana: float = computed_stats.get("max_mana", config.max_mana)

	# Core stats
	_add_stat_row(Loc.t("ui.summoner_screen.stats_hp"), str(int(hp)), Color(0.9, 0.3, 0.3))
	_add_stat_row(Loc.t("ui.summoner_screen.stats_mana"), str(int(mana)), Color(0.3, 0.5, 0.9))

	# Additional modifiers from traits
	var damage_bonus: float = computed_stats.get("damage_bonus", 0.0)
	if damage_bonus > 0:
		_add_stat_row(Loc.t("ui.summoner_screen.stats_damage"), "+%d%%" % int(damage_bonus * 100), Color(0.9, 0.6, 0.3))

	var damage_reduction: float = computed_stats.get("damage_reduction", 0.0)
	if damage_reduction > 0:
		_add_stat_row(Loc.t("ui.summoner_screen.stats_defense"), "+%d%%" % int(damage_reduction * 100), Color(0.5, 0.8, 0.5))


func _add_stat_row(label_text: String, value_text: String, value_color: Color) -> void:
	var hbox: HBoxContainer = HBoxContainer.new()
	hbox.add_theme_constant_override("separation", 10)

	var label: Label = Label.new()
	label.text = label_text + ":"
	label.add_theme_font_size_override("font_size", 15)
	label.add_theme_color_override("font_color", Color(0.7, 0.7, 0.7))
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hbox.add_child(label)

	var value: Label = Label.new()
	value.text = value_text
	value.add_theme_font_size_override("font_size", 15)
	value.add_theme_color_override("font_color", value_color)
	hbox.add_child(value)

	stats_container.add_child(hbox)


func _get_computed_stats(summoner_id: String) -> Dictionary:
	var summoner_instance_data: Dictionary = ProfileRepo.get_summoner_instance(summoner_id)
	if summoner_instance_data.is_empty():
		return {}

	var summoner_instance: SummonerInstance = SummonerInstance.from_dict(summoner_instance_data)
	if not summoner_instance:
		return {}

	return summoner_instance.get_computed_stats()


## =============================================================================
## TRAITS & BOONS
## =============================================================================

func _refresh_traits_and_boons(config: SummonerConfig) -> void:
	# Clear existing items
	for child: Node in traits_boons_container.get_children():
		child.queue_free()

	var trait_catalog: Node = get_node_or_null("/root/TraitCatalog")
	if not trait_catalog:
		return

	# Get acquired boons from summoner instance
	var acquired_boon_ids: Array[String] = []
	var summoner_instance_data: Dictionary = ProfileRepo.get_summoner_instance(_current_summoner_id)
	if not summoner_instance_data.is_empty():
		var summoner_instance: SummonerInstance = SummonerInstance.from_dict(summoner_instance_data)
		if summoner_instance:
			for boon_id: Variant in summoner_instance.acquired_boon_ids:
				if boon_id is String:
					acquired_boon_ids.append(boon_id)

	# Show innate traits first
	for trait_id: String in config.innate_trait_ids:
		var trait_card: PanelContainer = _create_trait_card(trait_catalog, trait_id, true)
		if trait_card:
			traits_boons_container.add_child(trait_card)

	# Then show acquired boons
	for boon_id: String in acquired_boon_ids:
		var boon_card: PanelContainer = _create_trait_card(trait_catalog, boon_id, false)
		if boon_card:
			traits_boons_container.add_child(boon_card)

	# Show message if nothing
	if config.innate_trait_ids.is_empty() and acquired_boon_ids.is_empty():
		var no_traits_label: Label = Label.new()
		no_traits_label.text = Loc.t("ui.summoner_screen.no_traits")
		no_traits_label.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5))
		no_traits_label.add_theme_font_size_override("font_size", 14)
		traits_boons_container.add_child(no_traits_label)


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
	panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	# Style
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.15, 0.15, 0.18, 1.0)
	style.border_color = Color(0.85, 0.75, 0.4) if is_innate else Color(0.4, 0.6, 0.9)
	style.border_width_left = 3
	style.border_width_right = 1
	style.border_width_top = 1
	style.border_width_bottom = 1
	style.set_corner_radius_all(4)
	panel.add_theme_stylebox_override("panel", style)

	# Margin
	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 10)
	margin.add_theme_constant_override("margin_right", 10)
	margin.add_theme_constant_override("margin_top", 6)
	margin.add_theme_constant_override("margin_bottom", 6)
	panel.add_child(margin)

	# VBox
	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 2)
	margin.add_child(vbox)

	# Name
	var name_label: Label = Label.new()
	name_label.text = trait_name
	name_label.add_theme_font_size_override("font_size", 14)
	name_label.add_theme_color_override("font_color", Color(1.0, 0.95, 0.85))
	vbox.add_child(name_label)

	# Description
	var desc_label: Label = Label.new()
	desc_label.text = trait_desc
	desc_label.add_theme_font_size_override("font_size", 12)
	desc_label.add_theme_color_override("font_color", Color(0.6, 0.6, 0.6))
	desc_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	vbox.add_child(desc_label)

	# Tooltip for full description
	panel.tooltip_text = trait_desc

	return panel


## =============================================================================
## NO SUMMONER STATE
## =============================================================================

func _show_no_summoner() -> void:
	summoner_name_label.text = Loc.t("ui.summoner_icon.no_summoner")
	element_label.text = ""
	level_label.text = ""
	description_label.text = ""
	portrait_background.color = ElementTypes.get_color("neutral")
	portrait_glow.color = Color(0.5, 0.5, 0.5, 0.2)
	portrait_symbol.text = "?"
	xp_label.text = ""
	xp_progress_bar.value = 0
	level_up_button.disabled = true
	level_up_button.text = "-"
	level_up_preview.text = ""

	# Clear containers
	for child: Node in stats_container.get_children():
		child.queue_free()
	for child: Node in traits_boons_container.get_children():
		child.queue_free()


## =============================================================================
## EVENT HANDLERS
## =============================================================================

func _on_close_pressed() -> void:
	_close()


func _on_level_up_pressed() -> void:
	if _current_summoner_id.is_empty():
		return

	var summoner_progression: Node = get_node_or_null("/root/SummonerProgression")
	if summoner_progression and summoner_progression.has_method("level_up_summoner"):
		var success: Variant = summoner_progression.call("level_up_summoner", _current_summoner_id)
		if success is bool and success:
			_refresh_all()
			_refresh_gold_display()


func _on_switch_summoner_pressed() -> void:
	# TODO: Navigate to Summoner Select Screen when implemented
	# For now, just print a message
	print("Switch Summoner pressed - Summoner Select Screen not yet implemented")


func _on_summoner_changed(_old_summoner_id: String, new_summoner_id: String) -> void:
	_current_summoner_id = new_summoner_id
	_refresh_all()


func _on_gold_changed(_new_gold: int) -> void:
	_refresh_gold_display()
	_refresh_all()


## =============================================================================
## NAVIGATION
## =============================================================================

func _close() -> void:
	closed.emit()
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_CAMPAIGN_MAP
	SceneManager.transition_to(return_scene)
