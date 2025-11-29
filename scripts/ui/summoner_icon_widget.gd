extends Control
class_name HeroIconWidget

## HeroIconWidget - Persistent hero portrait button
##
## A reusable component that displays the active hero's portrait.
## Click to open hero management panel.
##
## Usage:
##   - Add scene instance to any screen
##   - Connect to icon_clicked signal
##   - Widget auto-updates when active hero changes

## Emitted when the icon is clicked
signal icon_clicked()

## Node references
@onready var icon_button: Button = %IconButton
@onready var portrait_rect: ColorRect = %PortraitRect
@onready var element_label: Label = %ElementLabel
@onready var level_badge: Label = %LevelBadge


## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Connect button
	icon_button.pressed.connect(_on_icon_pressed)

	# Connect to hero selection changes
	var hero_selection: Node = get_node_or_null("/root/HeroSelection")
	if hero_selection and hero_selection.has_signal("hero_changed"):
		hero_selection.hero_changed.connect(_on_hero_changed)

	# Initial refresh
	refresh()

## =============================================================================
## PUBLIC API
## =============================================================================

## Refresh the display from current active hero
func refresh() -> void:
	var hero_selection: Node = get_node_or_null("/root/HeroSelection")
	if not hero_selection or not hero_selection.has_method("get_active_hero_id"):
		_show_no_hero()
		return

	var result: Variant = hero_selection.call("get_active_hero_id")
	if not result is String:
		_show_no_hero()
		return

	var hero_id: String = result
	if hero_id.is_empty():
		_show_no_hero()
		return

	_update_display(hero_id)

## =============================================================================
## PRIVATE METHODS
## =============================================================================

func _update_display(hero_id: String) -> void:
	# Get hero config
	var config: HeroConfig = HeroCatalog.get_hero_config(hero_id)
	if not config:
		_show_no_hero()
		return

	# Get hero instance for level
	var hero_progression: Node = get_node_or_null("/root/HeroProgression")
	var level: int = 1
	if hero_progression:
		var info: Dictionary = hero_progression.call("get_hero_progression_info", hero_id)
		level = info.get("level", 1)

	# Get element
	var element: ElementTypes.Element = config.get_element()

	# Update portrait using centralized ElementTypes constants
	portrait_rect.color = ElementTypes.get_color(element)
	element_label.text = ElementTypes.get_symbol(element)

	# Update level badge
	level_badge.text = "Lv.%d" % level
	level_badge.visible = true

	# Tooltip
	icon_button.tooltip_text = config.hero_name

func _show_no_hero() -> void:
	portrait_rect.color = ElementTypes.get_color("neutral")
	element_label.text = ElementTypes.get_symbol("neutral")
	level_badge.text = ""
	level_badge.visible = false
	icon_button.tooltip_text = Loc.t("ui.hero_icon.no_hero")

func _on_icon_pressed() -> void:
	icon_clicked.emit()

func _on_hero_changed(_old_hero_id: String, _new_hero_id: String) -> void:
	refresh()
