extends Control
class_name SummonerCard

## SummonerCard - Large card UI for displaying summoner information
##
## Designed to be more prominent than game cards.
## Shows summoner portrait, name, stats, and element.
## Used in summoner selection and deck builder.

## Signals
signal summoner_selected(summoner_id: String)
signal summoner_hovered(summoner_id: String)
signal summoner_unhovered()

## UI References (add these to the scene with unique names %)
@onready var summoner_name_label: Label = get_node_or_null("%SummonerNameLabel")
@onready var element_label: Label = get_node_or_null("%ElementLabel")
@onready var portrait_container: Control = get_node_or_null("%PortraitContainer")
@onready var hp_label: Label = get_node_or_null("%HPLabel")
@onready var mana_label: Label = get_node_or_null("%ManaLabel")
@onready var description_label: Label = get_node_or_null("%DescriptionLabel")
@onready var click_button: Button = get_node_or_null("%ClickButton")
@onready var glow_panel: Panel = get_node_or_null("%GlowPanel")

## Data
var summoner_id: String = ""
var summoner_config: SummonerConfig = null

## State
var _is_selected: bool = false

func _ready() -> void:
	# Connect click button
	if click_button:
		click_button.pressed.connect(_on_button_pressed)
		click_button.mouse_entered.connect(_on_mouse_entered)
		click_button.mouse_exited.connect(_on_mouse_exited)

	# Hide glow by default
	if glow_panel:
		glow_panel.visible = false

## Set summoner configuration and populate UI
func set_summoner(summoner_id_param: String) -> void:
	self.summoner_id = summoner_id_param

	# Get summoner config from catalog
	var config: SummonerConfig = SummonerConfig.from_dict(SummonerCatalog.GetSummoner(summoner_id))
	if not config:
		push_error("SummonerCard: Invalid summoner_id: %s" % summoner_id)
		return

	summoner_config = config
	_update_display()

## Update all UI elements with summoner config
func _update_display() -> void:
	if summoner_config == null:
		return

	# Summoner name
	if summoner_name_label:
		summoner_name_label.text = summoner_config.summoner_name

	# Element
	if element_label:
		var element: ElementTypes.Element = summoner_config.get_element()
		var element_name: String = ElementTypes.get_display_name(element)
		element_label.text = element_name

		# Color code by element
		var element_color: Color = _get_element_color(element)
		element_label.add_theme_color_override("font_color", element_color)

	# Stats (base stats from config)
	if hp_label:
		hp_label.text = Loc.t("ui.summoner_card.hp_label", {"value": "%.0f" % summoner_config.base_health})

	if mana_label:
		mana_label.text = Loc.t("ui.summoner_card.mana_label", {"value": "%.0f" % summoner_config.max_mana})

	# Description
	if description_label:
		description_label.text = summoner_config.description

## Get color for element type
func _get_element_color(element: ElementTypes.Element) -> Color:
	if element == null:
		return Color.WHITE

	# Match by element ID
	match StringName(element.id):
		ElementNameIDs.FIRE:
			return Color(1.0, 0.3, 0.2)  # Red
		ElementNameIDs.WATER:
			return Color(0.2, 0.5, 1.0)  # Blue
		ElementNameIDs.WIND:
			return Color(0.8, 1.0, 0.9)  # Cyan
		ElementNameIDs.EARTH:
			return Color(0.6, 0.4, 0.2)  # Brown
		ElementNameIDs.SHADOW:
			return Color(0.5, 0.3, 0.6)  # Purple
		_:
			return Color.WHITE

## Show glow effect (selection state persists through hover)
func show_glow(selected: bool = false) -> void:
	if selected:
		_is_selected = true
	if glow_panel:
		glow_panel.visible = true

## Hide glow effect (respects selection state)
func hide_glow(force: bool = false) -> void:
	if force:
		_is_selected = false
	if glow_panel and not _is_selected:
		glow_panel.visible = false

## Button pressed
func _on_button_pressed() -> void:
	summoner_selected.emit(summoner_id)

## Mouse hover effects
func _on_mouse_entered() -> void:
	show_glow()
	summoner_hovered.emit(summoner_id)

func _on_mouse_exited() -> void:
	hide_glow()
	summoner_unhovered.emit()
