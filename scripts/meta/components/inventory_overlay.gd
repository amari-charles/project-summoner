extends CanvasLayer
class_name InventoryOverlay

## Reusable, summoner-scoped Inventory surface. The hub opens it for browsing;
## equipment slots open the same surface filtered to compatible items.

signal closed()
signal item_equipped(slot: String, item_instance_id: String)
signal item_unequipped(slot: String)

@onready var dimmer: ColorRect = %Dimmer
@onready var window: PanelContainer = %Window
@onready var title_label: Label = %TitleLabel
@onready var summoner_label: Label = %SummonerLabel
@onready var close_button: Button = %CloseButton
@onready var category_tabs: HBoxContainer = %CategoryTabs
@onready var all_tab: Button = %AllTab
@onready var equipment_tab: Button = %EquipmentTab
@onready var materials_tab: Button = %MaterialsTab
@onready var consumables_tab: Button = %ConsumablesTab
@onready var quest_items_tab: Button = %QuestItemsTab
@onready var inventory_grid: InventoryGrid = %InventoryGrid
@onready var item_detail_dimmer: ColorRect = %ItemDetailDimmer
@onready var item_detail_modal: PanelContainer = %ItemDetailModal
@onready var detail_close_button: Button = %DetailCloseButton
@onready var item_icon: TextureRect = %ItemIcon
@onready var icon_fallback: Label = %IconFallback
@onready var detail_name: Label = %DetailName
@onready var detail_type: Label = %DetailType
@onready var detail_quantity: Label = %DetailQuantity
@onready var detail_description: Label = %DetailDescription
@onready var detail_status: Label = %DetailStatus
@onready var equip_button: Button = %EquipButton
@onready var unequip_button: Button = %UnequipButton

var _summoner_id: String = ""
var _slot_filter: String = ""
var _category_filter: String = "all"
var _selected_item: Dictionary = {}

const CATEGORY_BUTTONS: Dictionary = {
	"all": "AllTab",
	"equipment": "EquipmentTab",
	"materials": "MaterialsTab",
	"consumables": "ConsumablesTab",
	"quest_items": "QuestItemsTab",
}


func _ready() -> void:
	_configure_style()
	close_button.pressed.connect(close)
	dimmer.gui_input.connect(_on_dimmer_input)
	item_detail_dimmer.gui_input.connect(_on_detail_dimmer_input)
	detail_close_button.pressed.connect(_close_item_details)
	inventory_grid.item_selected.connect(_on_item_selected)
	equip_button.pressed.connect(_on_equip_pressed)
	unequip_button.pressed.connect(_on_unequip_pressed)
	var tab_group: ButtonGroup = ButtonGroup.new()
	for category: String in CATEGORY_BUTTONS:
		var tab: Button = get_node("%" + CATEGORY_BUTTONS[category]) as Button
		tab.button_group = tab_group
		tab.pressed.connect(_select_category.bind(category))
	all_tab.text = Loc.t("ui.inventory_overlay.tab_all")
	equipment_tab.text = Loc.t("ui.inventory_overlay.tab_equipment")
	materials_tab.text = Loc.t("ui.inventory_overlay.tab_materials")
	consumables_tab.text = Loc.t("ui.inventory_overlay.tab_consumables")
	quest_items_tab.text = Loc.t("ui.inventory_overlay.tab_quest_items")
	_reset_details()


func open_inventory(summoner_id: String) -> void:
	_open(summoner_id, "")


func open_equipment_slot(summoner_id: String, slot: String) -> void:
	_open(summoner_id, slot)


func close() -> void:
	if not visible:
		return
	visible = false
	closed.emit()


func _open(summoner_id: String, slot_filter: String) -> void:
	_summoner_id = summoner_id
	_slot_filter = slot_filter
	_category_filter = "all" if _slot_filter.is_empty() else "equipment"
	_selected_item = {}
	var config: SummonerConfig = SummonerConfig.from_dict(
		SummonerCatalogApi.get_summoner(_summoner_id)
	)
	summoner_label.text = config.summoner_name.to_upper() if config else ""
	title_label.text = (
		Loc.t("ui.inventory_overlay.title")
		if _slot_filter.is_empty()
		else Loc.t("ui.inventory_overlay.slot_title", {"slot": _slot_display_name(_slot_filter)})
	)
	category_tabs.visible = _slot_filter.is_empty()
	_update_tab_selection()
	visible = true
	_refresh_grid()
	_reset_details()


func _refresh_grid() -> void:
	var equipped: Dictionary = ItemsApi.get_equipped_items_dict(_summoner_id)
	inventory_grid.set_context(_summoner_id, equipped, _slot_filter, _category_filter)


func _on_item_selected(item: Dictionary) -> void:
	_selected_item = item.duplicate()
	item_detail_dimmer.visible = true
	item_detail_modal.visible = true

	var name_key: String = SafeTypeUtils.string(item.get("name_key", ""), "")
	var description_key: String = SafeTypeUtils.string(item.get("description_key", ""), "")
	detail_name.text = Loc.t(name_key) if not name_key.is_empty() else SafeTypeUtils.string(item.get("id", "?"), "?")
	detail_description.text = Loc.t(description_key) if not description_key.is_empty() else ""
	var item_slot: String = SafeTypeUtils.string(item.get("slot", ""), "")
	detail_type.text = _item_category_label(item, item_slot)
	detail_quantity.text = Loc.t(
		"ui.inventory_overlay.quantity",
		{"count": SafeTypeUtils.int_val(item.get("quantity", 1), 1)}
	)
	item_icon.texture = ItemVisualHelper.get_icon(item)
	item_icon.visible = true
	icon_fallback.visible = false

	var instance_id: String = SafeTypeUtils.string(item.get("instance_id", ""), "")
	var equipped: Dictionary = ItemsApi.get_equipped_items_dict(_summoner_id)
	var is_equipped: bool = not instance_id.is_empty() and instance_id in equipped.values()
	detail_status.text = Loc.t("ui.inventory_overlay.equipped") if is_equipped else ""
	detail_status.visible = is_equipped
	equip_button.visible = not _slot_filter.is_empty()
	equip_button.disabled = is_equipped
	equip_button.text = (
		Loc.t("ui.inventory_overlay.equipped")
		if is_equipped
		else Loc.t("ui.inventory_overlay.equip")
	)
	unequip_button.visible = (
		not _slot_filter.is_empty()
		and not SafeTypeUtils.string(equipped.get(_slot_filter, ""), "").is_empty()
	)
	unequip_button.text = Loc.t("ui.inventory_overlay.unequip")


func _on_equip_pressed() -> void:
	if _slot_filter.is_empty() or _selected_item.is_empty():
		return
	var instance_id: String = SafeTypeUtils.string(_selected_item.get("instance_id", ""), "")
	if instance_id.is_empty():
		return
	if ItemsApi.equip_item_str(_summoner_id, instance_id, _slot_filter):
		item_equipped.emit(_slot_filter, instance_id)
		_refresh_grid()
		_on_item_selected(_selected_item)


func _on_unequip_pressed() -> void:
	if _slot_filter.is_empty():
		return
	if ItemsApi.unequip_item_str(_summoner_id, _slot_filter):
		item_unequipped.emit(_slot_filter)
		_refresh_grid()
		_reset_details()


func _reset_details() -> void:
	item_detail_dimmer.visible = false
	item_detail_modal.visible = false
	_selected_item = {}
	equip_button.visible = false
	unequip_button.visible = false


func _close_item_details() -> void:
	_reset_details()


func _select_category(category: String) -> void:
	_category_filter = category
	_update_tab_selection()
	_selected_item = {}
	_reset_details()
	_refresh_grid()


func _update_tab_selection() -> void:
	for category: String in CATEGORY_BUTTONS:
		var tab: Button = get_node("%" + CATEGORY_BUTTONS[category]) as Button
		tab.button_pressed = category == _category_filter


func _item_category_label(item: Dictionary, item_slot: String) -> String:
	if not item_slot.is_empty():
		return Loc.t("ui.inventory_overlay.item_type_equipment", {"slot": _slot_display_name(item_slot)})
	var category: String = SafeTypeUtils.string(item.get("category", "materials"), "materials")
	return Loc.t("ui.inventory_overlay.tab_" + category)


func _slot_display_name(slot: String) -> String:
	var key: String = "ui.summoner_screen.equipment_slot_" + slot
	var localized: String = Loc.t(key)
	return localized if localized != key else slot.capitalize()


func _configure_style() -> void:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = GameColorPalette.UI_BACKGROUND
	style.border_color = GameColorPalette.UI_BORDER_STRONG
	style.set_border_width_all(2)
	style.set_corner_radius_all(10)
	style.shadow_color = GameColorPalette.BUTTON_SHADOW
	style.shadow_size = 12
	window.add_theme_stylebox_override("panel", style)
	var detail_style: StyleBoxFlat = style.duplicate()
	detail_style.bg_color = GameColorPalette.UI_SURFACE_RAISED
	item_detail_modal.add_theme_stylebox_override("panel", detail_style)


func _on_dimmer_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.button_index == MOUSE_BUTTON_LEFT and mouse_event.pressed:
			close()


func _on_detail_dimmer_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.button_index == MOUSE_BUTTON_LEFT and mouse_event.pressed:
			_close_item_details()


func _unhandled_input(event: InputEvent) -> void:
	if visible and event.is_action_pressed("ui_cancel"):
		if item_detail_modal.visible:
			_close_item_details()
		else:
			close()
		var viewport: Viewport = get_viewport()
		if viewport:
			viewport.set_input_as_handled()
