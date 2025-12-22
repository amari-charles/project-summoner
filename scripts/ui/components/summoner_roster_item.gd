extends PanelContainer
class_name SummonerRosterItem

## SummonerRosterItem - Individual summoner row in the management panel
##
## Displays summoner info with Select and Level Up actions.

## Signals
signal select_pressed(summoner_id: String)
signal level_up_pressed(summoner_id: String)

## Node references
@onready var portrait_rect: ColorRect = %PortraitRect
@onready var element_label: Label = %ElementLabel
@onready var name_label: Label = %NameLabel
@onready var level_label: Label = %LevelLabel
@onready var stats_label: Label = %StatsLabel
@onready var xp_label: Label = %XPLabel
@onready var xp_progress_bar: ProgressBar = %XPProgressBar
@onready var select_button: Button = %SelectButton
@onready var level_up_button: Button = %LevelUpButton
@onready var active_indicator: Label = %ActiveIndicator

## State
var _summoner_id: String = ""
var _is_active: bool = false

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	select_button.pressed.connect(_on_select_pressed)
	level_up_button.pressed.connect(_on_level_up_pressed)

## =============================================================================
## PUBLIC API
## =============================================================================

## Set summoner data for display
func set_summoner_data(summoner_id: String) -> void:
	_summoner_id = summoner_id
	refresh()

## Mark this summoner as active (currently selected)
func set_active(is_active: bool) -> void:
	_is_active = is_active
	_update_active_display()

## Refresh display from services
func refresh() -> void:
	if _summoner_id.is_empty():
		return

	# Get summoner config
	var config: SummonerConfig = SummonerCatalog.get_summoner_config(_summoner_id)
	if not config:
		return

	# Get progression info
	var progression_node: Node = get_node_or_null("/root/SummonerProgression")
	var info: Dictionary = {}
	if progression_node:
		info = progression_node.call("get_summoner_progression_info", _summoner_id)

	var level: int = info.get("level", 1)
	var current_xp: int = info.get("xp", 0)
	var xp_for_next: int = info.get("xp_for_next_level", 0)
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

	# Name and level
	name_label.text = config.summoner_name
	level_label.text = Loc.t("ui.summoner_panel.level_display", {"level": level})

	# Stats
	var computed_stats: Dictionary = _get_computed_stats()
	var hp: float = computed_stats.get("health", config.base_health)
	var mana: float = computed_stats.get("max_mana", config.max_mana)
	stats_label.text = Loc.t("ui.summoner_panel.stats_summary", {"hp": int(hp), "mana": int(mana)})

	# XP Progress
	if is_max_level:
		xp_label.text = Loc.t("ui.summoner_panel.level_up_max")
		xp_progress_bar.value = 100.0
	else:
		xp_label.text = Loc.t("ui.summoner_panel.xp_progress", {"current": current_xp, "required": xp_for_next})
		xp_progress_bar.value = xp_progress * 100.0

	# Level up button state
	if is_max_level:
		level_up_button.text = Loc.t("ui.summoner_panel.level_up_max")
		level_up_button.disabled = true
	elif not can_level_up:
		level_up_button.text = Loc.t("ui.summoner_panel.level_up_locked")
		level_up_button.disabled = true
	elif not can_afford:
		level_up_button.text = Loc.t("ui.summoner_panel.level_up_button", {"cost": gold_cost})
		level_up_button.disabled = true
		level_up_button.add_theme_color_override("font_color", Color(0.7, 0.3, 0.3))
	else:
		level_up_button.text = Loc.t("ui.summoner_panel.level_up_button", {"cost": gold_cost})
		level_up_button.disabled = false
		level_up_button.remove_theme_color_override("font_color")

	_update_active_display()

func _get_computed_stats() -> Dictionary:
	var summoner_instance_data: Dictionary = ProfileRepo.get_summoner_instance(_summoner_id)
	if summoner_instance_data.is_empty():
		return {}

	var summoner_instance: SummonerInstance = SummonerInstance.from_dict(summoner_instance_data)
	if not summoner_instance:
		return {}

	return summoner_instance.get_computed_stats()

func _update_active_display() -> void:
	if _is_active:
		active_indicator.text = Loc.t("ui.summoner_panel.active_indicator")
		active_indicator.visible = true
		select_button.text = Loc.t("ui.summoner_panel.selected_button")
		select_button.disabled = true
		# Highlight panel
		add_theme_stylebox_override("panel", _create_highlight_style())
	else:
		active_indicator.visible = false
		select_button.text = Loc.t("ui.summoner_panel.select_button")
		select_button.disabled = false
		# Normal panel
		remove_theme_stylebox_override("panel")

func _create_highlight_style() -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.2, 0.3, 0.4, 0.8)
	style.border_color = Color(0.4, 0.6, 0.8)
	style.set_border_width_all(2)
	style.set_corner_radius_all(8)
	return style

## =============================================================================
## EVENT HANDLERS
## =============================================================================

func _on_select_pressed() -> void:
	select_pressed.emit(_summoner_id)

func _on_level_up_pressed() -> void:
	level_up_pressed.emit(_summoner_id)
