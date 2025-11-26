extends Control
class_name HeroCard

## HeroCard - Large card UI for displaying hero information
##
## Designed to be more prominent than game cards.
## Shows hero portrait, name, stats, and element.
## Used in hero selection and deck builder.

## Signals
signal hero_selected(hero_id: String)
signal hero_hovered(hero_id: String)
signal hero_unhovered()

## UI References (add these to the scene with unique names %)
@onready var hero_name_label: Label = get_node_or_null("%HeroNameLabel")
@onready var element_label: Label = get_node_or_null("%ElementLabel")
@onready var portrait_container: Control = get_node_or_null("%PortraitContainer")
@onready var hp_label: Label = get_node_or_null("%HPLabel")
@onready var mana_label: Label = get_node_or_null("%ManaLabel")
@onready var regen_label: Label = get_node_or_null("%RegenLabel")
@onready var description_label: Label = get_node_or_null("%DescriptionLabel")
@onready var click_button: Button = get_node_or_null("%ClickButton")
@onready var glow_panel: Panel = get_node_or_null("%GlowPanel")

## Data
var hero_id: String = ""
var hero_config: HeroConfig = null

func _ready() -> void:
	# Connect click button
	if click_button:
		click_button.pressed.connect(_on_button_pressed)
		click_button.mouse_entered.connect(_on_mouse_entered)
		click_button.mouse_exited.connect(_on_mouse_exited)

	# Hide glow by default
	if glow_panel:
		glow_panel.visible = false

## Set hero configuration and populate UI
func set_hero(hero_id_param: String) -> void:
	self.hero_id = hero_id_param

	# Get hero config from catalog
	var catalog: Node = get_node_or_null("/root/HeroCatalog")
	if not catalog or not catalog.has_method("get_hero_config"):
		push_error("HeroCard: HeroCatalog not available")
		return

	var config: Variant = catalog.call("get_hero_config", hero_id)
	if not config is HeroConfig:
		push_error("HeroCard: Invalid hero_id: %s" % hero_id)
		return

	hero_config = config
	_update_display()

## Update all UI elements with hero config
func _update_display() -> void:
	if hero_config == null:
		return

	# Hero name
	if hero_name_label:
		hero_name_label.text = hero_config.hero_name

	# Element
	if element_label:
		var element: ElementTypes.Element = hero_config.get_element()
		var element_name: String = ElementTypes.get_display_name(element)
		element_label.text = element_name

		# Color code by element
		var element_color: Color = _get_element_color(element)
		element_label.add_theme_color_override("font_color", element_color)

	# Stats (base stats from config)
	if hp_label:
		hp_label.text = Loc.t("ui.hero_card.hp_label", {"value": "%.0f" % hero_config.base_health})

	if mana_label:
		mana_label.text = Loc.t("ui.hero_card.mana_label", {"value": "%.0f" % hero_config.max_mana})

	if regen_label:
		regen_label.text = Loc.t("ui.hero_card.regen_label", {"value": "%.1f" % hero_config.mana_regen})

	# Description
	if description_label:
		description_label.text = hero_config.description

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

## Show glow effect
func show_glow() -> void:
	if glow_panel:
		glow_panel.visible = true

## Hide glow effect
func hide_glow() -> void:
	if glow_panel:
		glow_panel.visible = false

## Button pressed
func _on_button_pressed() -> void:
	hero_selected.emit(hero_id)

## Mouse hover effects
func _on_mouse_entered() -> void:
	show_glow()
	hero_hovered.emit(hero_id)

func _on_mouse_exited() -> void:
	hide_glow()
	hero_unhovered.emit()
