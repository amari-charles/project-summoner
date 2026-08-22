extends Control
class_name InventoryGrid

## Reusable presentation for the items available to a summoner. The component
## owns no item mutations; consumers decide what selecting an item does.

signal item_selected(item: Dictionary)

const ITEM_SLOT_SIZE: Vector2 = Vector2(88, 88)
const MIN_VISIBLE_SLOTS: int = 18

@onready var item_flow: GridContainer = %ItemFlow
@onready var empty_label: Label = %EmptyLabel

var _summoner_id: String = ""
var _equipped_items: Dictionary = {}


func _ready() -> void:
	empty_label.text = Loc.t("ui.summoner_screen.inventory_empty")
	if not _summoner_id.is_empty():
		refresh()


func set_summoner(summoner_id: String, equipped_items: Dictionary = {}) -> void:
	_summoner_id = summoner_id
	_equipped_items = equipped_items.duplicate()
	if is_node_ready():
		refresh()


func refresh() -> void:
	for child: Node in item_flow.get_children():
		child.queue_free()

	if _summoner_id.is_empty():
		empty_label.visible = true
		return

	var items: Array = ItemsApi.get_owned_items_dict(_summoner_id)
	empty_label.visible = false
	var item_count: int = 0
	for value: Variant in items:
		if value is Dictionary:
			item_flow.add_child(_create_item_button(value))
			item_count += 1

	# These placeholders communicate the grid without imposing an inventory cap;
	# the grid grows beyond this minimum when the player owns more items.
	for _index: int in range(maxi(0, MIN_VISIBLE_SLOTS - item_count)):
		item_flow.add_child(_create_empty_slot())


func _create_item_button(item: Dictionary) -> Button:
	var button: Button = Button.new()
	button.custom_minimum_size = ITEM_SLOT_SIZE
	button.text = _item_button_text(item)
	button.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	button.add_theme_font_size_override("font_size", 11)
	button.tooltip_text = _item_tooltip(item)
	button.pressed.connect(_on_item_pressed.bind(item))

	var rarity: String = SafeTypeUtils.string(item.get("rarity", "common"), "common")
	var is_equipped: bool = _is_item_equipped(SafeTypeUtils.string(item.get("instance_id", ""), ""))
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = GameColorPalette.BUTTON_PRIMARY_BG if is_equipped else GameColorPalette.UI_SURFACE_RAISED
	style.border_color = _rarity_color(rarity)
	style.set_border_width_all(2 if is_equipped else 1)
	style.set_corner_radius_all(6)
	button.add_theme_stylebox_override("normal", style)
	return button


func _create_empty_slot() -> PanelContainer:
	var slot: PanelContainer = PanelContainer.new()
	slot.custom_minimum_size = ITEM_SLOT_SIZE
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = GameColorPalette.UI_SURFACE_ALT
	style.border_color = GameColorPalette.UI_BORDER
	style.set_border_width_all(1)
	style.set_corner_radius_all(6)
	slot.add_theme_stylebox_override("panel", style)
	return slot


func _item_button_text(item: Dictionary) -> String:
	var name_key: String = SafeTypeUtils.string(item.get("name_key", ""), "")
	var display_name: String = Loc.t(name_key) if not name_key.is_empty() else SafeTypeUtils.string(item.get("id", "?"), "?")
	var slot: String = SafeTypeUtils.string(item.get("slot", ""), "")
	var prefix: String = _slot_symbol(slot)
	var instance_id: String = SafeTypeUtils.string(item.get("instance_id", ""), "")
	var equipped_suffix: String = "\n" + Loc.t("ui.summoner_screen.inventory_equipped") if _is_item_equipped(instance_id) else ""
	return "%s\n%s%s" % [prefix, display_name, equipped_suffix]


func _item_tooltip(item: Dictionary) -> String:
	var name_key: String = SafeTypeUtils.string(item.get("name_key", ""), "")
	var description_key: String = SafeTypeUtils.string(item.get("description_key", ""), "")
	var display_name: String = Loc.t(name_key) if not name_key.is_empty() else SafeTypeUtils.string(item.get("id", "?"), "?")
	var description: String = Loc.t(description_key) if not description_key.is_empty() else ""
	return display_name if description.is_empty() else "%s\n%s" % [display_name, description]


func _is_item_equipped(instance_id: String) -> bool:
	return not instance_id.is_empty() and instance_id in _equipped_items.values()


func _slot_symbol(slot: String) -> String:
	match slot:
		"wand": return "W"
		"robes": return "C"
		"ring1", "ring2": return "R"
		_: return "?"


func _rarity_color(rarity: String) -> Color:
	match rarity:
		"uncommon": return Color(0.3, 0.7, 0.35)
		"rare": return Color(0.3, 0.5, 0.95)
		"epic": return Color(0.7, 0.35, 0.9)
		"legendary": return Color(0.95, 0.62, 0.15)
		_: return GameColorPalette.UI_BORDER_STRONG


func _on_item_pressed(item: Dictionary) -> void:
	item_selected.emit(item)
