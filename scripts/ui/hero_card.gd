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
var hero_data: Dictionary = {}

func _ready() -> void:
	# Connect click button
	if click_button:
		click_button.pressed.connect(_on_button_pressed)
		click_button.mouse_entered.connect(_on_mouse_entered)
		click_button.mouse_exited.connect(_on_mouse_exited)

	# Hide glow by default
	if glow_panel:
		glow_panel.visible = false

## Set hero data and populate UI
func set_hero(hero_id_param: String) -> void:
	self.hero_id = hero_id_param

	# Get hero data from catalog
	var catalog: Node = get_node_or_null("/root/HeroCatalog")
	if not catalog or not catalog.has_method("get_hero"):
		push_error("HeroCard: HeroCatalog not available")
		return

	var data: Variant = catalog.call("get_hero", hero_id)
	if not data is Dictionary:
		push_error("HeroCard: Invalid hero_id: %s" % hero_id)
		return

	hero_data = data
	_update_display()

## Update all UI elements with hero data
func _update_display() -> void:
	if hero_data.is_empty():
		return

	# Hero name
	if hero_name_label:
		var name: String = hero_data.get("hero_name", "Unknown Hero")
		hero_name_label.text = name

	# Element
	if element_label:
		var element_var: Variant = hero_data.get("element", 0)
		var element: int = element_var if element_var is int else 0
		var element_name: String = ElementTypes.get_element_name(element)
		element_label.text = element_name

		# Color code by element
		var element_color: Color = _get_element_color(element)
		element_label.add_theme_color_override("font_color", element_color)

	# Stats
	if hp_label:
		var hp: float = hero_data.get("base_health", 0.0)
		hp_label.text = "HP: %.0f" % hp

	if mana_label:
		var mana: float = hero_data.get("max_mana", 0.0)
		mana_label.text = "Mana: %.0f" % mana

	if regen_label:
		var regen: float = hero_data.get("mana_regen", 0.0)
		regen_label.text = "Regen: %.1f/s" % regen

	# Description
	if description_label:
		var desc: String = hero_data.get("description", "")
		description_label.text = desc

## Get color for element type
func _get_element_color(element: int) -> Color:
	match element:
		ElementTypes.FIRE:
			return Color(1.0, 0.3, 0.2)  # Red
		ElementTypes.WATER:
			return Color(0.2, 0.5, 1.0)  # Blue
		ElementTypes.WIND:
			return Color(0.8, 1.0, 0.9)  # Cyan
		ElementTypes.EARTH:
			return Color(0.6, 0.4, 0.2)  # Brown
		ElementTypes.SHADOW:
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
