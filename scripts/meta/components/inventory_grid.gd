extends Control
class_name InventoryGrid

## Reusable presentation for the items available to a summoner. The component
## owns no item mutations; consumers decide what selecting an item does.

signal item_selected(item: Dictionary)

const ITEM_SLOT_SIZE: Vector2 = Vector2(88, 88)
const VISIBLE_COLUMNS: int = 12
const VISIBLE_ROWS: int = 5
const VISIBLE_SLOT_CAPACITY: int = VISIBLE_COLUMNS * VISIBLE_ROWS

@onready var item_flow: GridContainer = %ItemFlow
@onready var empty_label: Label = %EmptyLabel

var _summoner_id: String = ""
var _equipped_items: Dictionary = {}
var _slot_filter: String = ""
var _category_filter: String = "all"


func _ready() -> void:
	empty_label.text = Loc.t("ui.summoner_screen.inventory_empty")
	if not _summoner_id.is_empty():
		refresh()


func set_summoner(summoner_id: String, equipped_items: Dictionary = {}) -> void:
	set_context(summoner_id, equipped_items)


func set_context(
	summoner_id: String,
	equipped_items: Dictionary = {},
	slot_filter: String = "",
	category_filter: String = "all"
) -> void:
	_summoner_id = summoner_id
	_equipped_items = equipped_items.duplicate()
	_slot_filter = slot_filter
	_category_filter = category_filter
	if is_node_ready():
		refresh()


func refresh() -> void:
	for child: Node in item_flow.get_children():
		child.queue_free()

	if _summoner_id.is_empty():
		empty_label.visible = true
		item_flow.visible = false
		return

	var items: Array = (
		ItemsApi.get_owned_items_dict(_summoner_id)
		if _slot_filter.is_empty()
		else ItemsApi.list_items_for_slot_dict(_slot_filter, _summoner_id)
	)
	empty_label.visible = false
	var item_count: int = 0
	for value: Variant in items:
		if value is Dictionary and _matches_category(value):
			item_flow.add_child(_create_item_button(value))
			item_count += 1
	# Keep the slot field visible even when this inventory/category is empty. The
	# empty squares communicate the collection structure to the player and provide
	# the intended layout scaffold for final UI design.
	empty_label.visible = false
	item_flow.visible = true

	# Fill a stable 12x5 design-space field without imposing an inventory cap.
	# Owned items can continue beyond the visible capacity into the scroll area.
	for _index: int in range(maxi(0, VISIBLE_SLOT_CAPACITY - item_count)):
		item_flow.add_child(_create_empty_slot())


func _matches_category(item: Dictionary) -> bool:
	if _category_filter == "all":
		return true
	var slot: String = SafeTypeUtils.string(item.get("slot", ""), "")
	var category: String = SafeTypeUtils.string(
		item.get("category", "equipment" if not slot.is_empty() else "materials"),
		"materials"
	)
	return category == _category_filter


func _create_item_button(item: Dictionary) -> Button:
	var button: Button = Button.new()
	button.custom_minimum_size = ITEM_SLOT_SIZE
	button.tooltip_text = _item_tooltip(item)
	button.pressed.connect(_on_item_pressed.bind(item))

	# Button icon sizing differs across supported Godot versions. Keep the art at
	# an authored design-space size instead of allowing the button to stretch it.
	var icon_rect: TextureRect = TextureRect.new()
	icon_rect.texture = ItemVisualHelper.get_icon(item)
	icon_rect.set_anchors_preset(Control.PRESET_CENTER)
	icon_rect.position = Vector2(-26, -26)
	icon_rect.size = Vector2(52, 52)
	icon_rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	icon_rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon_rect.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	icon_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	button.add_child(icon_rect)

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


func _item_tooltip(item: Dictionary) -> String:
	var name_key: String = SafeTypeUtils.string(item.get("name_key", ""), "")
	var description_key: String = SafeTypeUtils.string(item.get("description_key", ""), "")
	var display_name: String = Loc.t(name_key) if not name_key.is_empty() else SafeTypeUtils.string(item.get("id", "?"), "?")
	var description: String = Loc.t(description_key) if not description_key.is_empty() else ""
	return display_name if description.is_empty() else "%s\n%s" % [display_name, description]


func _is_item_equipped(instance_id: String) -> bool:
	return not instance_id.is_empty() and instance_id in _equipped_items.values()


func _rarity_color(rarity: String) -> Color:
	match rarity:
		"uncommon": return Color(0.3, 0.7, 0.35)
		"rare": return Color(0.3, 0.5, 0.95)
		"epic": return Color(0.7, 0.35, 0.9)
		"legendary": return Color(0.95, 0.62, 0.15)
		_: return GameColorPalette.UI_BORDER_STRONG


func _on_item_pressed(item: Dictionary) -> void:
	item_selected.emit(item)
