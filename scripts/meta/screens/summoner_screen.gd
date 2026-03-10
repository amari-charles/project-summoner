extends Control
class_name SummonerScreen

## SummonerScreen - Full-screen summoner info and management interface
##
## Displays the active summoner's portrait, description, stats, and traits.
## Allows leveling up and navigating to summoner selection screen.

## =============================================================================
## SIGNALS
## =============================================================================

signal closed()

## =============================================================================
## NODE REFERENCES - Header
## =============================================================================

@onready var background: ColorRect = %Background
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
## NODE REFERENCES - Right Half Panels
## =============================================================================

@onready var description_panel: PanelContainer = %DescriptionPanel
@onready var description_header: Label = %DescriptionHeader
@onready var description_label: Label = %DescriptionLabel

@onready var stats_panel: PanelContainer = %StatsPanel
@onready var stats_header: Label = %StatsHeader
@onready var stats_container: VBoxContainer = %StatsContainer

@onready var traits_panel: PanelContainer = %TraitsPanel
@onready var traits_header: Label = %TraitsHeader
@onready var traits_container: VBoxContainer = %TraitsContainer

@onready var equipment_panel: PanelContainer = %EquipmentPanel
@onready var equipment_header: Label = %EquipmentHeader
@onready var equipment_container: VBoxContainer = %EquipmentContainer

## =============================================================================
## STATE
## =============================================================================

var _current_summoner_id: String = ""
var _portrait_tween: Tween = null
var _equipment_modal: EquipmentSlotModal = null


## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Connect header buttons
	close_button.pressed.connect(_on_close_pressed)
	level_up_button.pressed.connect(_on_level_up_pressed)
	switch_summoner_button.pressed.connect(_on_switch_summoner_pressed)

	# Connect to service signals
	if SummonerSelection.has_signal("SummonerChanged"):
		SummonerSelection.connect("SummonerChanged", _on_summoner_changed)

	Economy.connect("CampaignGoldChanged", _on_campaign_gold_changed)

	# Setup equipment modal
	_equipment_modal = EquipmentSlotModal.new()
	_equipment_modal.item_equipped.connect(_on_equipment_changed)
	_equipment_modal.item_unequipped.connect(_on_equipment_slot_cleared)
	add_child(_equipment_modal)

	# Set static localized text
	switch_summoner_button.text = Loc.t("ui.summoner_screen.switch_summoner")

	# Initial data load
	_refresh_gold_display()

	# Load active summoner
	var active_id: String = SummonerSelectionApi.get_active_summoner_id()
	if not active_id.is_empty():
		_current_summoner_id = active_id
		_refresh_all()


## =============================================================================
## GOLD DISPLAY
## =============================================================================

func _refresh_gold_display() -> void:
	var gold: int = EconomyApi.get_campaign_gold()
	gold_label.text = Loc.t("ui.summoner_screen.gold_display", {"gold": gold})


## =============================================================================
## MAIN REFRESH
## =============================================================================

func _refresh_all() -> void:
	if _current_summoner_id.is_empty():
		_show_no_summoner()
		return

	var config: SummonerConfig = SummonerConfig.from_dict(SummonerCatalogApi.get_summoner(_current_summoner_id))
	if not config:
		_show_no_summoner()
		return

	# Get progression info
	var info: Dictionary = SummonerProgressionApi.get_summoner_progression_info(_current_summoner_id)

	var level: int = info.get("level", 1)
	var current_xp: int = info.get("xp", 0)
	var xp_for_next: int = info.get("xp_for_next_level", 100)
	var xp_progress: float = info.get("xp_progress", 0.0)
	var can_level_up: bool = info.get("can_level_up", false)
	var is_max_level: bool = info.get("is_max_level", false)

	# Get element
	var element: ElementTypes.Element = config.get_element()
	var element_color: Color = ElementTypes.get_color(element)
	var gradient_colors: Array[Color] = CardVisualHelper.get_element_gradient_colors(element.id)

	# Update header
	summoner_name_label.text = config.summoner_name.to_upper()
	element_label.visible = false  # Summoners are associated with elements, not defined by them
	level_label.text = Loc.t("ui.summoner_panel.level_display", {"level": level})

	# Update background with element-themed energy waves
	_update_background_for_element(element)

	# Style the info panels with element accents
	_style_panels(element_color)

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
	_update_level_up_display(is_max_level, can_level_up, config)

	# Update stats
	_refresh_stats(config)

	# Update traits and equipment
	_refresh_traits(config)
	_refresh_equipment()


## =============================================================================
## BACKGROUND
## =============================================================================

func _update_background_for_element(element: ElementTypes.Element) -> void:
	var gradient_colors: Array[Color] = CardVisualHelper.get_element_gradient_colors(element.id)
	var material: ShaderMaterial = background.material as ShaderMaterial
	if not material:
		return

	# Primary is the brighter color (gradient[1]), secondary is darker (gradient[0])
	if gradient_colors.size() >= 2:
		material.set_shader_parameter("color_primary", gradient_colors[1])
		material.set_shader_parameter("color_secondary", gradient_colors[0])
	elif gradient_colors.size() == 1:
		material.set_shader_parameter("color_primary", gradient_colors[0])
		material.set_shader_parameter("color_secondary", gradient_colors[0].darkened(0.3))


## =============================================================================
## PANEL STYLING
## =============================================================================

# Warm color constants
const PANEL_BG_WARM: Color = Color(0.12, 0.10, 0.08, 0.95)  # Warm dark brown
const PANEL_BORDER_BASE: Color = Color(0.25, 0.20, 0.15, 1.0)  # Warm brown border
const HEADER_COLOR_WARM: Color = Color(0.95, 0.90, 0.80, 1.0)  # Cream/parchment
const TEXT_COLOR_WARM: Color = Color(0.85, 0.80, 0.70, 1.0)  # Warm off-white

const SUMMONER_STAT_PLACEHOLDER_ICONS: Dictionary = {
	"health": "HP",
	"max_mana": "MN",
	"cast_speed": "CS",
	"soul_guard": "SG"
}

const SUMMONER_STAT_ICON_COLORS: Dictionary = {
	"health": Color(0.90, 0.30, 0.30),
	"max_mana": Color(0.30, 0.55, 0.90),
	"cast_speed": Color(0.85, 0.70, 0.35),
	"soul_guard": Color(0.35, 0.85, 0.90)
}

const SUMMONER_STAT_TOOLTIPS: Dictionary = {
	"health": "Total health before defeat.",
	"max_mana": "Maximum mana available for casting cards.",
	"cast_speed": "Multiplier for how quickly cards resolve.",
	"soul_guard": "Reduces incoming soul-targeted damage."
}


func _style_panels(element_color: Color) -> void:
	# Style each panel with warm colors and element accent
	_style_single_panel(description_panel, description_header, element_color)
	_style_single_panel(stats_panel, stats_header, element_color)
	_style_single_panel(traits_panel, traits_header, element_color)
	_style_single_panel(equipment_panel, equipment_header, element_color)

	# Update text colors to warm tones
	description_label.add_theme_color_override("font_color", TEXT_COLOR_WARM)


func _style_single_panel(panel: PanelContainer, header: Label, accent_color: Color) -> void:
	# Create warm styled panel
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = PANEL_BG_WARM
	style.border_color = PANEL_BORDER_BASE
	style.set_border_width_all(2)
	style.border_width_left = 4  # Thicker left border for accent
	style.set_corner_radius_all(8)

	# Add element-colored left accent
	style.border_color = accent_color.darkened(0.3)

	# Add shadow for depth
	style.shadow_color = Color(0.0, 0.0, 0.0, 0.4)
	style.shadow_size = 6
	style.shadow_offset = Vector2(2, 2)

	panel.add_theme_stylebox_override("panel", style)

	# Style header with warm cream color
	header.add_theme_color_override("font_color", accent_color.lightened(0.2))


## =============================================================================
## PORTRAIT
## =============================================================================

func _update_portrait(element: ElementTypes.Element, gradient_colors: Array[Color]) -> void:
	var element_color: Color = ElementTypes.get_color(element)
	var border_color: Color = CardVisualHelper.get_element_border_color(element.id)

	# Style the frame with element-themed border - warmer and deeper
	var frame_style: StyleBoxFlat = StyleBoxFlat.new()
	frame_style.bg_color = Color(0.08, 0.06, 0.05, 1.0)  # Warm dark
	frame_style.border_color = border_color
	frame_style.set_border_width_all(5)
	frame_style.set_corner_radius_all(12)
	# Stronger shadow for depth
	frame_style.shadow_color = Color(0.0, 0.0, 0.0, 0.6)
	frame_style.shadow_size = 12
	frame_style.shadow_offset = Vector2(4, 4)
	portrait_frame.add_theme_stylebox_override("panel", frame_style)

	# Set background color (darker gradient color) with warmer tone
	var bg_color: Color = gradient_colors[0] if gradient_colors.size() > 0 else element_color
	bg_color = bg_color.darkened(0.1)
	portrait_background.color = bg_color

	# Set glow color (lighter/brighter) - more prominent
	var glow_color: Color = CardVisualHelper.get_element_glow_color(element.id)
	glow_color.a = 0.6
	portrait_glow.color = glow_color

	# Set symbol with element color tint
	portrait_symbol.text = ElementTypes.get_symbol(element)
	portrait_symbol.add_theme_color_override("font_color", element_color.lightened(0.3))

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

func _update_level_up_display(is_max: bool, can_level: bool, config: SummonerConfig) -> void:
	if is_max:
		level_up_button.text = Loc.t("ui.summoner_panel.level_up_max")
		level_up_button.disabled = true
		level_up_preview.text = ""
	elif not can_level:
		level_up_button.text = Loc.t("ui.summoner_panel.level_up_locked")
		level_up_button.disabled = true
		level_up_preview.text = ""
	else:
		# XP-only leveling - no gold cost check needed
		level_up_button.text = Loc.t("ui.summoner_panel.level_up_button_simple")
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
	var cast_speed: float = computed_stats.get("cast_speed", config.base_cast_speed)
	var soul_guard: float = computed_stats.get("soul_guard", 0.0)

	var stats_grid: GridContainer = GridContainer.new()
	stats_grid.columns = 2
	stats_grid.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	stats_grid.add_theme_constant_override("h_separation", 10)
	stats_grid.add_theme_constant_override("v_separation", 8)
	stats_container.add_child(stats_grid)

	# Main-tab stat set with placeholder icons.
	stats_grid.add_child(_create_stat_cell("health", Loc.t("ui.summoner_screen.stats_hp"), str(int(hp)), Color(0.9, 0.3, 0.3)))
	stats_grid.add_child(_create_stat_cell("max_mana", Loc.t("ui.summoner_screen.stats_mana"), str(int(mana)), Color(0.3, 0.5, 0.9)))
	stats_grid.add_child(_create_stat_cell("cast_speed", Loc.t("ui.summoner_screen.stats_cast_speed"), "%.2fx" % cast_speed, Color(0.7, 0.5, 0.9)))
	stats_grid.add_child(_create_stat_cell("soul_guard", Loc.t("ui.summoner_screen.stats_soul_guard"), _format_stat_number(soul_guard), Color(0.35, 0.85, 0.90)))


func _create_stat_cell(stat_id: String, label_text: String, value_text: String, value_color: Color) -> PanelContainer:
	var cell: PanelContainer = PanelContainer.new()
	cell.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var cell_style: StyleBoxFlat = StyleBoxFlat.new()
	cell_style.bg_color = Color(0.08, 0.06, 0.05, 0.85)
	cell_style.border_color = SUMMONER_STAT_ICON_COLORS.get(stat_id, Color(0.4, 0.4, 0.4))
	cell_style.set_border_width_all(1)
	cell_style.set_corner_radius_all(6)
	cell.add_theme_stylebox_override("panel", cell_style)

	var hbox: HBoxContainer = HBoxContainer.new()
	hbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hbox.add_theme_constant_override("separation", 10)
	cell.add_child(hbox)

	var icon_panel: PanelContainer = PanelContainer.new()
	icon_panel.custom_minimum_size = Vector2(22, 22)
	var icon_style: StyleBoxFlat = StyleBoxFlat.new()
	var icon_color: Color = SUMMONER_STAT_ICON_COLORS.get(stat_id, Color(0.6, 0.6, 0.6))
	icon_style.bg_color = icon_color.darkened(0.45)
	icon_style.border_color = icon_color
	icon_style.set_border_width_all(1)
	icon_style.set_corner_radius_all(4)
	icon_panel.add_theme_stylebox_override("panel", icon_style)

	var icon_label: Label = Label.new()
	icon_label.text = SUMMONER_STAT_PLACEHOLDER_ICONS.get(stat_id, "??")
	icon_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	icon_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	icon_label.add_theme_font_size_override("font_size", 10)
	icon_label.add_theme_color_override("font_color", icon_color.lightened(0.4))
	icon_panel.add_child(icon_label)

	var value: Label = Label.new()
	value.text = value_text
	value.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	value.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	value.add_theme_font_size_override("font_size", 17)
	value.add_theme_color_override("font_color", value_color)

	hbox.add_child(icon_panel)
	hbox.add_child(value)
	cell.tooltip_text = _build_stat_tooltip(label_text, value_text, SUMMONER_STAT_TOOLTIPS.get(stat_id, ""))

	return cell


func _build_stat_tooltip(label_text: String, value_text: String, description: String) -> String:
	var tooltip: String = "%s: %s" % [label_text, value_text]
	if not description.is_empty():
		tooltip += "\n" + description
	return tooltip


func _format_stat_number(value: float) -> String:
	if abs(value - round(value)) < 0.01:
		return str(int(round(value)))
	return "%.1f" % value


func _get_computed_stats(summoner_id: String) -> Dictionary:
	return SummonerProgressionApi.get_computed_stats_for_summoner(summoner_id)


## =============================================================================
## TRAITS
## =============================================================================

func _refresh_traits(config: SummonerConfig) -> void:
	# Clear existing
	for child: Node in traits_container.get_children():
		child.queue_free()

	# Get all trait IDs (innate + acquired) from C# service
	var service_trait_ids: Array = SummonerProgressionApi.get_all_trait_ids_for_summoner(_current_summoner_id)
	var all_trait_ids: Array[String] = []

	if service_trait_ids.size() > 0:
		for trait_id: Variant in service_trait_ids:
			all_trait_ids.append(str(trait_id))
	else:
		# Fallback to innate only if no instance
		for trait_id: String in config.innate_trait_ids:
			all_trait_ids.append(trait_id)

	var flow: HFlowContainer = HFlowContainer.new()
	flow.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	flow.add_theme_constant_override("h_separation", 8)
	flow.add_theme_constant_override("v_separation", 8)
	traits_container.add_child(flow)

	# Show all traits
	for trait_id: String in all_trait_ids:
		var is_innate: bool = trait_id in config.innate_trait_ids
		var trait_icon: PanelContainer = _create_trait_icon(trait_id, is_innate)
		if trait_icon:
			flow.add_child(trait_icon)

	# Show message if no traits
	if all_trait_ids.is_empty():
		var no_traits_label: Label = Label.new()
		no_traits_label.text = Loc.t("ui.summoner_screen.no_traits")
		no_traits_label.add_theme_color_override("font_color", TEXT_COLOR_WARM.darkened(0.3))
		no_traits_label.add_theme_font_size_override("font_size", 14)
		traits_container.add_child(no_traits_label)


## =============================================================================
## EQUIPMENT
## =============================================================================

const EQUIPMENT_BOX_SIZE: Vector2 = Vector2(100, 100)

func _refresh_equipment() -> void:
	# Clear existing
	for child: Node in equipment_container.get_children():
		child.queue_free()

	# Get equipped items from Items service
	var equipped: Dictionary = ItemsApi.get_equipped_items_dict(_current_summoner_id)

	# Create horizontal box for 4 slots
	var hbox: HBoxContainer = HBoxContainer.new()
	hbox.add_theme_constant_override("separation", 12)
	hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	equipment_container.add_child(hbox)

	# Show all 4 equipment slots as boxes
	for slot: String in ["wand", "ring1", "ring2", "robes"]:
		var item_instance_id: String = equipped.get(slot, "")
		var slot_box: PanelContainer = _create_equipment_slot_box(slot, item_instance_id)
		hbox.add_child(slot_box)


func _create_equipment_slot_box(slot: String, item_instance_id: String) -> PanelContainer:
	const SLOT_DISPLAY_NAMES: Dictionary = {"wand": "Wand", "ring1": "Ring", "ring2": "Ring", "robes": "Robes"}
	var slot_display_name: String = Loc.t("ui.summoner_screen.equipment_slot_" + slot)
	if slot_display_name.begins_with("ui.summoner_screen.equipment_slot_"):
		slot_display_name = SLOT_DISPLAY_NAMES.get(slot, slot.capitalize())

	var is_empty: bool = item_instance_id.is_empty()

	# Create panel (the box)
	var panel: PanelContainer = PanelContainer.new()
	panel.custom_minimum_size = EQUIPMENT_BOX_SIZE

	# Make clickable
	panel.gui_input.connect(_on_equipment_slot_clicked.bind(slot))
	panel.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND

	# Style the box
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.08, 0.06, 0.05, 0.95) if is_empty else Color(0.12, 0.10, 0.08, 0.95)
	var accent_color: Color = Color(0.4, 0.45, 0.5) if is_empty else Color(0.7, 0.55, 0.3)
	style.border_color = accent_color
	style.set_border_width_all(2)
	style.set_corner_radius_all(8)
	style.shadow_color = Color(0.0, 0.0, 0.0, 0.4)
	style.shadow_size = 4
	style.shadow_offset = Vector2(2, 2)
	panel.add_theme_stylebox_override("panel", style)

	# VBox for icon + label
	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 4)
	vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	panel.add_child(vbox)

	# Icon/symbol at top
	var icon_label: Label = Label.new()
	const SLOT_ICONS: Dictionary = {"wand": "W", "ring1": "R", "ring2": "R", "robes": "C"}
	icon_label.text = SLOT_ICONS.get(slot, "?")
	icon_label.add_theme_font_size_override("font_size", 28)
	icon_label.add_theme_color_override("font_color", accent_color.lightened(0.2) if is_empty else accent_color.lightened(0.3))
	icon_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(icon_label)

	# Slot name
	var name_label: Label = Label.new()
	name_label.text = slot_display_name
	name_label.add_theme_font_size_override("font_size", 11)
	name_label.add_theme_color_override("font_color", TEXT_COLOR_WARM.darkened(0.1))
	name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(name_label)

	# Item name or "Empty"
	var item_label: Label = Label.new()
	if is_empty:
		item_label.text = Loc.t("ui.summoner_screen.equipment_empty")
		item_label.add_theme_color_override("font_color", TEXT_COLOR_WARM.darkened(0.4))
	else:
		var items_for_slot: Array[Dictionary] = ItemsApi.list_items_for_slot_dict(slot, _current_summoner_id)
		var item_name: String = ""
		for item: Dictionary in items_for_slot:
			if item.get("instance_id", "") == item_instance_id:
				var name_key: String = item.get("name_key", "")
				item_name = Loc.t(name_key) if not name_key.is_empty() else item.get("id", "Unknown")
				break
		if item_name.is_empty():
			item_name = Loc.t("ui.summoner_screen.equipment_unknown")
		item_label.text = item_name
		item_label.add_theme_color_override("font_color", accent_color.lightened(0.1))
	item_label.add_theme_font_size_override("font_size", 10)
	item_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	item_label.autowrap_mode = TextServer.AUTOWRAP_WORD
	item_label.custom_minimum_size.x = EQUIPMENT_BOX_SIZE.x - 16
	vbox.add_child(item_label)

	# Tooltip with full item info
	if not is_empty:
		panel.tooltip_text = item_label.text

	return panel


func _create_trait_icon(trait_id: String, is_innate: bool) -> PanelContainer:
	var trait_name: String = TraitCatalogApi.get_trait_name(trait_id)
	var trait_desc: String = TraitCatalogApi.get_trait_description(trait_id)

	if trait_name.is_empty():
		trait_name = trait_id

	# Create icon chip
	var panel: PanelContainer = PanelContainer.new()
	panel.custom_minimum_size = Vector2(42, 42)

	# Style with warm colors
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.10, 0.08, 0.06, 0.9)
	var accent_color: Color = Color(0.85, 0.75, 0.4) if is_innate else Color(0.4, 0.6, 0.9)
	style.border_color = accent_color
	style.set_border_width_all(2)
	style.set_corner_radius_all(5)
	style.shadow_color = Color(0.0, 0.0, 0.0, 0.3)
	style.shadow_size = 2
	style.shadow_offset = Vector2(1, 1)
	panel.add_theme_stylebox_override("panel", style)

	var abbr: Label = Label.new()
	abbr.text = _abbreviate_trait_name(trait_name)
	abbr.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	abbr.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	abbr.add_theme_font_size_override("font_size", 12)
	abbr.add_theme_color_override("font_color", accent_color.lightened(0.2))
	panel.add_child(abbr)

	var type_label: String = Loc.t("ui.summoner_screen.trait_innate") if is_innate else Loc.t("ui.summoner_screen.trait_acquired")
	panel.tooltip_text = "%s (%s)\n%s" % [trait_name, type_label, trait_desc]

	return panel


func _abbreviate_trait_name(name: String) -> String:
	var normalized: String = name.strip_edges()
	if normalized.is_empty():
		return "??"

	var words: PackedStringArray = normalized.split(" ", false)
	if words.size() >= 2:
		var first: String = words[0]
		var second: String = words[1]
		if not first.is_empty() and not second.is_empty():
			return (first.left(1) + second.left(1)).to_upper()

	return normalized.left(2).to_upper()


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
	for child: Node in traits_container.get_children():
		child.queue_free()
	for child: Node in equipment_container.get_children():
		child.queue_free()


## =============================================================================
## EVENT HANDLERS
## =============================================================================

func _on_close_pressed() -> void:
	_close()


func _on_level_up_pressed() -> void:
	if _current_summoner_id.is_empty():
		return

	var success: bool = SummonerProgressionApi.level_up_summoner(_current_summoner_id)
	if success:
		_refresh_all()
		_refresh_gold_display()


func _on_switch_summoner_pressed() -> void:
	NavigationContext.push_return(SceneManager.SCENE_SUMMONER_SCREEN)
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SWITCH)


func _on_summoner_changed(_old_summoner_id: String, new_summoner_id: String) -> void:
	_current_summoner_id = new_summoner_id
	_refresh_all()


func _on_campaign_gold_changed(_summoner_id: String, _gold: int) -> void:
	_refresh_gold_display()
	_refresh_all()


func _on_equipment_slot_clicked(event: InputEvent, slot: String) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.button_index == MOUSE_BUTTON_LEFT and mouse_event.pressed:
			if _equipment_modal and not _current_summoner_id.is_empty():
				_equipment_modal.open(slot, _current_summoner_id)


func _on_equipment_changed(_slot: String, _item_instance_id: String) -> void:
	_refresh_equipment()


func _on_equipment_slot_cleared(_slot: String) -> void:
	_refresh_equipment()


## =============================================================================
## NAVIGATION
## =============================================================================

func _close() -> void:
	closed.emit()
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_CAMPAIGN_MAP
	SceneManager.transition_to(return_scene)
