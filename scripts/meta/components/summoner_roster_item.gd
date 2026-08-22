extends PanelContainer
class_name SummonerRosterItem

## SummonerRosterItem - Individual summoner row in the management panel
##
## Displays summoner info with automatic XP progress and a Select action.

## Signals
signal select_pressed(summoner_id: String)

## Node references
@onready var portrait_rect: ColorRect = %PortraitRect
@onready var element_label: Label = %ElementLabel
@onready var name_label: Label = %NameLabel
@onready var level_label: Label = %LevelLabel
@onready var stats_label: Label = %StatsLabel
@onready var xp_label: Label = %XPLabel
@onready var xp_progress_bar: ProgressBar = %XPProgressBar
@onready var select_button: Button = %SelectButton
@onready var active_indicator: Label = %ActiveIndicator

## State
var _summoner_id: String = ""
var _is_active: bool = false

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	select_button.pressed.connect(_on_select_pressed)

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
	var config: SummonerConfig = SummonerConfig.from_dict(SummonerCatalogApi.get_summoner(_summoner_id))
	if not config:
		return

	# Get progression info
	var info: Dictionary = SummonerProgressionApi.get_summoner_progression_info(_summoner_id)

	var level: int = info.get("level", 1)
	var current_xp: int = info.get("xp", 0)
	var xp_for_next: int = info.get("xp_for_next_level", 0)
	var xp_progress: float = info.get("xp_progress", 0.0)
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

	_update_active_display()

func _get_computed_stats() -> Dictionary:
	return SummonerProgressionApi.get_computed_stats_for_summoner(_summoner_id)

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
	style.bg_color = GameColorPalette.BUTTON_PRIMARY_BG
	style.border_color = GameColorPalette.BUTTON_PRIMARY_BORDER
	style.set_border_width_all(2)
	style.set_corner_radius_all(8)
	return style

## =============================================================================
## EVENT HANDLERS
## =============================================================================

func _on_select_pressed() -> void:
	select_pressed.emit(_summoner_id)
