extends BackNavigableScreen
class_name SummonerScreen

## SummonerScreen - Reusable summoner profile and management overlay
##
## Displays the active summoner's identity, automatic XP progress, stats,
## equipment, and trait development entry points.

@export var embedded_overlay: bool = false

## =============================================================================
## SIGNALS
## =============================================================================

signal closed()

## =============================================================================
## NODE REFERENCES - Header
## =============================================================================

@onready var dimmer: ColorRect = %Dimmer
@onready var window: PanelContainer = %Window
@onready var close_button: Button = %CloseButton
@onready var summoner_name_label: Label = %SummonerNameLabel
@onready var element_label: Label = %ElementLabel
@onready var level_label: Label = %LevelLabel
@onready var switch_summoner_button: Button = %SwitchSummonerButton

## =============================================================================
## NODE REFERENCES - Portrait Section (Left Column)
## =============================================================================

@onready var portrait_container: CenterContainer = %PortraitContainer
@onready var portrait_frame: PanelContainer = %PortraitFrame
@onready var portrait_texture: TextureRect = %PortraitTexture
@onready var portrait_symbol: Label = %PortraitSymbol

@onready var xp_label: Label = %XPLabel
@onready var xp_progress_bar: ProgressBar = %XPProgressBar

## =============================================================================
## NODE REFERENCES - Right Half Panels
## =============================================================================

@onready var stats_panel: PanelContainer = %StatsPanel
@onready var stats_header: Label = %StatsHeader
@onready var stats_container: VBoxContainer = %StatsContainer

@onready var traits_panel: PanelContainer = %TraitsPanel
@onready var traits_header: Label = %TraitsHeader
@onready var traits_container: HFlowContainer = %TraitsContainer
@onready var upgrade_points_label: Label = %UpgradePointsLabel
@onready var trait_development_overlay: TraitDevelopmentOverlay = %TraitDevelopmentOverlay

@onready var equipment_panel: PanelContainer = %EquipmentPanel
@onready var equipment_container: VBoxContainer = %EquipmentContainer
@onready var inventory_overlay: InventoryOverlay = %InventoryOverlay

## =============================================================================
## STATE
## =============================================================================

var _current_summoner_id: String = ""
## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	_configure_overlay_style()
	# Connect header buttons
	close_button.pressed.connect(_on_close_pressed)
	switch_summoner_button.pressed.connect(_on_switch_summoner_pressed)
	trait_development_overlay.trait_acquired.connect(_on_trait_acquired)

	# Connect to service signals
	if SummonerSelection.has_signal("SummonerChanged"):
		SummonerSelection.connect("SummonerChanged", _on_summoner_changed)

	inventory_overlay.item_equipped.connect(_on_equipment_changed)
	inventory_overlay.item_unequipped.connect(_on_equipment_slot_cleared)

	# Set static localized text
	switch_summoner_button.text = Loc.t("ui.summoner_screen.switch_summoner")
	stats_header.text = Loc.t("ui.summoner_screen.stats_header")
	traits_header.text = Loc.t("ui.summoner_screen.traits_header")

	# Load active summoner
	var active_id: String = SummonerSelectionApi.get_active_summoner_id()
	if not active_id.is_empty():
		_current_summoner_id = active_id
		_refresh_all()
	if embedded_overlay:
		visible = false


func open_profile(summoner_id: String = "") -> void:
	_current_summoner_id = (
		summoner_id
		if not summoner_id.is_empty()
		else SummonerSelectionApi.get_active_summoner_id()
	)
	visible = true
	_refresh_all()


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
	var is_max_level: bool = info.get("is_max_level", false)
	var unspent_trait_points: int = info.get("unspent_trait_points", 0)

	# Get element
	var element: ElementTypes.Element = config.get_element()
	var element_color: Color = ElementTypes.get_color(element)
	var gradient_colors: Array[Color] = CardVisualHelper.get_element_gradient_colors(element.id)

	# Update header
	summoner_name_label.text = config.summoner_name.to_upper()
	element_label.visible = false  # Summoners are associated with elements, not defined by them
	level_label.text = Loc.t("ui.summoner_panel.level_display", {"level": level})

	# Style the info panels with element accents
	_style_panels(element_color)

	# Update portrait
	_update_portrait(element, gradient_colors)

	# Update XP
	if is_max_level:
		xp_label.text = Loc.t("ui.summoner_panel.level_up_max")
		xp_progress_bar.value = 100.0
	else:
		xp_label.text = Loc.t("ui.summoner_panel.xp_progress", {"current": current_xp, "required": xp_for_next})
		xp_progress_bar.value = xp_progress * 100.0

	_refresh_upgrades_state(unspent_trait_points)

	# Update stats
	_refresh_stats(config)

	# Update build and item management surfaces
	_refresh_traits(config)
	_refresh_equipment()


## =============================================================================
## PANEL STYLING
## =============================================================================

const SUMMONER_STAT_PLACEHOLDER_ICONS: Dictionary = {
	"health": "HP",
	"max_mana": "MN",
	"cast_speed": "CS",
	"soul_strength": "SS"
}

const SUMMONER_STAT_ICON_COLORS: Dictionary = {
	"health": Color(0.90, 0.30, 0.30),
	"max_mana": Color(0.30, 0.55, 0.90),
	"cast_speed": Color(0.85, 0.70, 0.35),
	"soul_strength": Color(0.35, 0.85, 0.90)
}

const SUMMONER_STAT_TOOLTIP_KEYS: Dictionary = {
	"health": "ui.summoner_screen.stats_tooltip_hp",
	"max_mana": "ui.summoner_screen.stats_tooltip_mana",
	"cast_speed": "ui.summoner_screen.stats_tooltip_cast_speed",
	"soul_strength": "ui.summoner_screen.stats_tooltip_soul_strength"
}


func _style_panels(element_color: Color) -> void:
	# Style each panel with warm colors and element accent
	_style_single_panel(stats_panel, stats_header, element_color)
	_style_single_panel(traits_panel, traits_header, element_color)
	_style_single_panel(equipment_panel, null, element_color)

	stats_header.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	traits_header.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)


func _style_single_panel(panel: PanelContainer, header: Label, accent_color: Color) -> void:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color.TRANSPARENT
	style.set_border_width_all(0)

	panel.add_theme_stylebox_override("panel", style)

	if header != null:
		header.add_theme_color_override("font_color", accent_color.darkened(0.25))


## =============================================================================
## PORTRAIT
## =============================================================================

func _update_portrait(element: ElementTypes.Element, _gradient_colors: Array[Color]) -> void:
	var element_color: Color = ElementTypes.get_color(element)

	# The summoner is a character, not a card. Keep the portrait area transparent
	# so the sprite belongs to the screen rather than a collectible frame.
	var frame_style: StyleBoxFlat = StyleBoxFlat.new()
	frame_style.bg_color = Color.TRANSPARENT
	frame_style.set_border_width_all(0)
	portrait_frame.add_theme_stylebox_override("panel", frame_style)
	portrait_texture.visible = true

	# Keep the element-symbol fallback available for missing portrait resources.
	portrait_symbol.visible = portrait_texture.texture == null
	portrait_symbol.text = ElementTypes.get_symbol(element)
	portrait_symbol.add_theme_color_override("font_color", element_color.lightened(0.3))



## =============================================================================
## UPGRADES
## =============================================================================

func _refresh_upgrades_state(unspent_trait_points: int) -> void:
	upgrade_points_label.text = Loc.t("ui.summoner_screen.upgrade_points_count", {"count": unspent_trait_points})
	if unspent_trait_points > 0:
		upgrade_points_label.add_theme_color_override("font_color", Color(1.0, 0.86, 0.45))
	else:
		upgrade_points_label.remove_theme_color_override("font_color")


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
	var soul_strength: float = computed_stats.get("soul_strength", 0.0)

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
	stats_grid.add_child(_create_stat_cell("soul_strength", Loc.t("ui.summoner_screen.stats_soul_strength"), _format_stat_number(soul_strength), Color(0.35, 0.85, 0.90)))


func _create_stat_cell(stat_id: String, label_text: String, value_text: String, value_color: Color) -> PanelContainer:
	var cell: PanelContainer = PanelContainer.new()
	cell.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var cell_style: StyleBoxFlat = StyleBoxFlat.new()
	cell_style.bg_color = GameColorPalette.UI_SURFACE_RAISED
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
	var tooltip_key: String = str(SUMMONER_STAT_TOOLTIP_KEYS.get(stat_id, ""))
	var tooltip_description: String = Loc.t(tooltip_key) if not tooltip_key.is_empty() else ""
	cell.tooltip_text = _build_stat_tooltip(label_text, value_text, tooltip_description)

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
## OWNED TRAITS
## =============================================================================

func _refresh_traits(config: SummonerConfig) -> void:
	for child: Node in traits_container.get_children():
		child.queue_free()

	var service_trait_ids: Array = SummonerProgressionApi.get_all_trait_ids_for_summoner(_current_summoner_id)
	var all_trait_ids: Array[String] = []
	if service_trait_ids.is_empty():
		all_trait_ids.assign(config.innate_trait_ids)
	else:
		for value: Variant in service_trait_ids:
			all_trait_ids.append(SafeTypeUtils.string(value, ""))

	if all_trait_ids.is_empty():
		var empty_label: Label = Label.new()
		empty_label.text = Loc.t("ui.summoner_screen.no_traits")
		empty_label.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
		traits_container.add_child(empty_label)
		return

	for trait_id: String in all_trait_ids:
		if not trait_id.is_empty():
			traits_container.add_child(_create_trait_chip(trait_id, trait_id in config.innate_trait_ids))


func _create_trait_chip(trait_id: String, is_innate: bool) -> Button:
	var trait_name: String = TraitCatalogApi.get_trait_name(trait_id)
	var trait_description: String = TraitCatalogApi.get_trait_description(trait_id)
	if trait_name.begins_with("trait."):
		trait_name = Loc.t(trait_name)
	if trait_description.begins_with("trait."):
		trait_description = Loc.t(trait_description)
	if trait_name.is_empty():
		trait_name = trait_id

	var chip: Button = Button.new()
	chip.custom_minimum_size = Vector2(58, 58)
	chip.text = ""
	chip.tooltip_text = trait_name if trait_description.is_empty() else "%s\n%s" % [trait_name, trait_description]
	chip.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	chip.pressed.connect(_on_trait_pressed.bind(trait_id))
	var style: StyleBoxFlat = StyleBoxFlat.new()
	var accent: Color = Color(0.82, 0.70, 0.35) if is_innate else Color(0.38, 0.58, 0.88)
	style.bg_color = accent.darkened(0.35)
	style.border_color = accent
	style.set_border_width_all(2)
	style.set_corner_radius_all(29)
	chip.add_theme_stylebox_override("normal", style)
	var hover_style: StyleBoxFlat = style.duplicate()
	hover_style.bg_color = style.bg_color.lightened(0.14)
	chip.add_theme_stylebox_override("hover", hover_style)
	var pressed_style: StyleBoxFlat = style.duplicate()
	pressed_style.bg_color = style.bg_color.darkened(0.12)
	chip.add_theme_stylebox_override("pressed", pressed_style)
	return chip


## =============================================================================
## EQUIPMENT
## =============================================================================

const EQUIPMENT_BOX_SIZE: Vector2 = Vector2(82, 86)

func _refresh_equipment() -> void:
	# Clear existing
	for child: Node in equipment_container.get_children():
		child.queue_free()

	# Get equipped items from Items service
	var equipped: Dictionary = ItemsApi.get_equipped_items_dict(_current_summoner_id)

	# Create horizontal box for 4 slots
	var hbox: HBoxContainer = HBoxContainer.new()
	hbox.add_theme_constant_override("separation", 8)
	hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	equipment_container.add_child(hbox)

	# Show all 4 equipment slots as boxes
	for slot: String in ["robes", "ring1", "ring2", "wand"]:
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
	style.bg_color = GameColorPalette.UI_SURFACE_ALT if is_empty else GameColorPalette.UI_SURFACE_RAISED
	var accent_color: Color = GameColorPalette.UI_BORDER_STRONG if is_empty else GameColorPalette.BUTTON_PRIMARY_BORDER
	style.border_color = accent_color
	style.set_border_width_all(2)
	style.set_corner_radius_all(8)
	style.shadow_color = GameColorPalette.BUTTON_SHADOW
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
	icon_label.add_theme_font_size_override("font_size", 22)
	icon_label.add_theme_color_override("font_color", accent_color.lightened(0.2) if is_empty else accent_color.lightened(0.3))
	icon_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(icon_label)

	# Slot name
	var name_label: Label = Label.new()
	name_label.text = slot_display_name
	name_label.add_theme_font_size_override("font_size", 11)
	name_label.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(name_label)

	# Item name or "Empty"
	var item_label: Label = Label.new()
	if is_empty:
		item_label.text = Loc.t("ui.summoner_screen.equipment_empty")
		item_label.add_theme_color_override("font_color", GameColorPalette.TEXT_DISABLED)
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


## =============================================================================
## NO SUMMONER STATE
## =============================================================================

func _show_no_summoner() -> void:
	summoner_name_label.text = Loc.t("ui.summoner_icon.no_summoner")
	element_label.text = ""
	level_label.text = ""
	portrait_texture.visible = false
	portrait_symbol.visible = true
	portrait_symbol.text = "?"
	xp_label.text = ""
	xp_progress_bar.value = 0
	upgrade_points_label.text = ""

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


func _on_trait_pressed(trait_id: String) -> void:
	trait_development_overlay.open_for_summoner(_current_summoner_id, trait_id)


func _on_trait_acquired(_trait_id: String) -> void:
	_refresh_all()


func _on_switch_summoner_pressed() -> void:
	NavigationContext.push_return(
		SceneManager.SCENE_WALKABLE_ACADEMY_HUB
		if embedded_overlay
		else SceneManager.SCENE_SUMMONER_SCREEN
	)
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SWITCH)


func _on_summoner_changed(_old_summoner_id: String, new_summoner_id: String) -> void:
	_current_summoner_id = new_summoner_id
	_refresh_all()


func _on_equipment_slot_clicked(event: InputEvent, slot: String) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.button_index == MOUSE_BUTTON_LEFT and mouse_event.pressed:
			if not _current_summoner_id.is_empty():
				inventory_overlay.open_equipment_slot(_current_summoner_id, slot)


func _on_equipment_changed(_slot: String, _item_instance_id: String) -> void:
	_refresh_all()


func _on_equipment_slot_cleared(_slot: String) -> void:
	_refresh_all()


## =============================================================================
## NAVIGATION
## =============================================================================

func _close() -> void:
	if embedded_overlay:
		visible = false
		closed.emit()
		return
	closed.emit()
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_ACADEMY_CAMPUS
	SceneManager.transition_to(return_scene)


func _request_back_navigation() -> void:
	_on_close_pressed()
