extends Control
class_name SummonerIconWidget

## SummonerIconWidget - Persistent summoner portrait button
##
## A reusable component that displays the active summoner's portrait.
## Click to open summoner management panel.
##
## Usage:
##   - Add scene instance to any screen
##   - Connect to icon_clicked signal
##   - Widget auto-updates when active summoner changes

## Emitted when the icon is clicked
signal icon_clicked()

## Node references
@onready var icon_button: Button = %IconButton
@onready var portrait_rect: ColorRect = %PortraitRect
@onready var portrait_texture: TextureRect = %PortraitTexture
@onready var element_label: Label = %ElementLabel
@onready var level_badge: Label = %LevelBadge


## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Connect button
	icon_button.pressed.connect(_on_icon_pressed)

	# Connect to summoner selection changes
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if summoner_selection and summoner_selection.has_signal("summoner_changed"):
		summoner_selection.summoner_changed.connect(_on_summoner_changed)

	# Initial refresh
	refresh()

## =============================================================================
## PUBLIC API
## =============================================================================

## Refresh the display from current active summoner
func refresh() -> void:
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if not summoner_selection or not summoner_selection.has_method("get_active_summoner_id"):
		_show_no_summoner()
		return

	var result: Variant = summoner_selection.call("get_active_summoner_id")
	if not result is String:
		_show_no_summoner()
		return

	var summoner_id: String = result
	if summoner_id.is_empty():
		_show_no_summoner()
		return

	_update_display(summoner_id)

## =============================================================================
## PRIVATE METHODS
## =============================================================================

func _update_display(summoner_id: String) -> void:
	# Get summoner config
	var config: SummonerConfig = SummonerCatalog.get_summoner_config(summoner_id)
	if not config:
		_show_no_summoner()
		return

	# Get summoner instance for level
	var summoner_progression: Node = get_node_or_null("/root/SummonerProgression")
	var level: int = 1
	if summoner_progression:
		var info: Dictionary = summoner_progression.call("get_summoner_progression_info", summoner_id)
		level = info.get("level", 1)

	# Get element
	var element: ElementTypes.Element = config.get_element()

	# Check if summoner has a portrait image
	if config.summoner_icon_path and not config.summoner_icon_path.is_empty():
		var texture: Texture2D = load(config.summoner_icon_path)
		if texture:
			portrait_texture.texture = texture
			portrait_texture.visible = true
			portrait_rect.visible = false
			element_label.visible = false
		else:
			_show_placeholder(element)
	else:
		_show_placeholder(element)

	# Update level badge
	level_badge.text = "Lv.%d" % level
	level_badge.visible = true

	# Tooltip
	icon_button.tooltip_text = config.summoner_name


func _show_placeholder(element: ElementTypes.Element) -> void:
	portrait_texture.visible = false
	portrait_rect.visible = true
	element_label.visible = true
	portrait_rect.color = ElementTypes.get_color(element)
	element_label.text = ElementTypes.get_symbol(element)

func _show_no_summoner() -> void:
	portrait_texture.visible = false
	portrait_rect.visible = true
	element_label.visible = true
	portrait_rect.color = ElementTypes.get_color("neutral")
	element_label.text = ElementTypes.get_symbol("neutral")
	level_badge.text = ""
	level_badge.visible = false
	icon_button.tooltip_text = Loc.t("ui.summoner_icon.no_summoner")

func _on_icon_pressed() -> void:
	icon_clicked.emit()

func _on_summoner_changed(_old_summoner_id: String, _new_summoner_id: String) -> void:
	refresh()
