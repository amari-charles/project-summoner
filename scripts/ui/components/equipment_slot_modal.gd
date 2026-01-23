extends Control
class_name EquipmentSlotModal

## Equipment Slot Modal - Shows slot info and allows equipping/unequipping items
##
## Usage:
##   var modal = EquipmentSlotModal.new()
##   add_child(modal)
##   modal.open("weapon", summoner_id)

signal closed()
signal item_equipped(slot: String, item_instance_id: String)
signal item_unequipped(slot: String)

## Colors
const BG_COLOR: Color = Color(0.08, 0.06, 0.05, 0.98)
const HEADER_COLOR: Color = Color(0.95, 0.90, 0.80, 1.0)
const TEXT_COLOR: Color = Color(0.85, 0.80, 0.70, 1.0)
const ACCENT_COLOR: Color = Color(0.7, 0.55, 0.3)
const EMPTY_COLOR: Color = Color(0.5, 0.5, 0.5)

## State
var _current_slot: String = ""
var _summoner_id: String = ""

## UI references
var _canvas_layer: CanvasLayer
var _root: Control
var _panel: PanelContainer
var _slot_label: Label
var _equipped_container: VBoxContainer
var _available_container: VBoxContainer
var _close_button: Button
var _unequip_button: Button


func _ready() -> void:
	_build_ui()
	_canvas_layer.visible = false


func _build_ui() -> void:
	# Use CanvasLayer to render on top of everything
	_canvas_layer = CanvasLayer.new()
	_canvas_layer.layer = 100
	add_child(_canvas_layer)

	# Root control that fills the viewport
	_root = Control.new()
	_root.set_anchors_preset(Control.PRESET_FULL_RECT)
	_root.mouse_filter = Control.MOUSE_FILTER_STOP
	_canvas_layer.add_child(_root)

	# Full screen dimmer
	var dimmer: ColorRect = ColorRect.new()
	dimmer.color = Color(0, 0, 0, 0.6)
	dimmer.set_anchors_preset(Control.PRESET_FULL_RECT)
	dimmer.gui_input.connect(_on_dimmer_input)
	_root.add_child(dimmer)

	# Center panel
	_panel = PanelContainer.new()
	_panel.custom_minimum_size = Vector2(400, 500)
	_panel.set_anchors_preset(Control.PRESET_CENTER)
	_panel.grow_horizontal = Control.GROW_DIRECTION_BOTH
	_panel.grow_vertical = Control.GROW_DIRECTION_BOTH
	_root.add_child(_panel)

	# Panel style
	var panel_style: StyleBoxFlat = StyleBoxFlat.new()
	panel_style.bg_color = BG_COLOR
	panel_style.border_color = ACCENT_COLOR.darkened(0.2)
	panel_style.set_border_width_all(3)
	panel_style.set_corner_radius_all(12)
	panel_style.shadow_color = Color(0, 0, 0, 0.5)
	panel_style.shadow_size = 10
	_panel.add_theme_stylebox_override("panel", panel_style)

	# Main margin
	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 20)
	margin.add_theme_constant_override("margin_right", 20)
	margin.add_theme_constant_override("margin_top", 20)
	margin.add_theme_constant_override("margin_bottom", 20)
	_panel.add_child(margin)

	# Main VBox
	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 15)
	margin.add_child(vbox)

	# Slot name header
	_slot_label = Label.new()
	_slot_label.add_theme_font_size_override("font_size", 24)
	_slot_label.add_theme_color_override("font_color", HEADER_COLOR)
	_slot_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(_slot_label)

	# Separator
	var sep1: HSeparator = HSeparator.new()
	vbox.add_child(sep1)

	# Equipped section
	var equipped_label: Label = Label.new()
	equipped_label.text = Loc.t("ui.equipment_modal.equipped")
	equipped_label.add_theme_font_size_override("font_size", 14)
	equipped_label.add_theme_color_override("font_color", TEXT_COLOR.darkened(0.2))
	vbox.add_child(equipped_label)

	_equipped_container = VBoxContainer.new()
	_equipped_container.add_theme_constant_override("separation", 8)
	vbox.add_child(_equipped_container)

	# Unequip button (initially hidden)
	_unequip_button = Button.new()
	_unequip_button.text = Loc.t("ui.equipment_modal.unequip")
	_unequip_button.add_theme_font_size_override("font_size", 14)
	_unequip_button.pressed.connect(_on_unequip_pressed)
	_unequip_button.visible = false
	vbox.add_child(_unequip_button)

	# Separator
	var sep2: HSeparator = HSeparator.new()
	vbox.add_child(sep2)

	# Available items section
	var available_label: Label = Label.new()
	available_label.text = Loc.t("ui.equipment_modal.available_items")
	available_label.add_theme_font_size_override("font_size", 14)
	available_label.add_theme_color_override("font_color", TEXT_COLOR.darkened(0.2))
	vbox.add_child(available_label)

	# Scroll container for available items
	var scroll: ScrollContainer = ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	vbox.add_child(scroll)

	_available_container = VBoxContainer.new()
	_available_container.add_theme_constant_override("separation", 8)
	_available_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(_available_container)

	# Close button
	_close_button = Button.new()
	_close_button.text = Loc.t("ui.equipment_modal.close")
	_close_button.add_theme_font_size_override("font_size", 16)
	_close_button.pressed.connect(_on_close_pressed)
	vbox.add_child(_close_button)


func open(slot: String, summoner_id: String) -> void:
	_current_slot = slot
	_summoner_id = summoner_id
	_refresh()
	_canvas_layer.visible = true


func _refresh() -> void:
	var slot_display: String = Items.SLOT_DISPLAY_NAMES.get(_current_slot, _current_slot.capitalize())
	_slot_label.text = Loc.t("ui.equipment_modal.slot_title").replace("{slot}", slot_display.to_upper())

	# Clear containers
	for child: Node in _equipped_container.get_children():
		child.queue_free()
	for child: Node in _available_container.get_children():
		child.queue_free()

	# Get equipped item for this slot
	var equipped: Dictionary = Items.get_equipped_items(_summoner_id)
	var equipped_instance_id: String = equipped.get(_current_slot, "")

	# Get all items for this slot
	var items_for_slot: Array[Dictionary] = Items.list_items_for_slot(_current_slot, _summoner_id)

	# Show equipped item info
	if equipped_instance_id.is_empty():
		var empty_label: Label = Label.new()
		empty_label.text = Loc.t("ui.equipment_modal.no_item_equipped")
		empty_label.add_theme_font_size_override("font_size", 16)
		empty_label.add_theme_color_override("font_color", EMPTY_COLOR)
		_equipped_container.add_child(empty_label)
		_unequip_button.visible = false
	else:
		# Find the equipped item in the list
		for item: Dictionary in items_for_slot:
			if item.get("instance_id", "") == equipped_instance_id:
				var item_card: PanelContainer = _create_item_card(item, true)
				_equipped_container.add_child(item_card)
				break
		_unequip_button.visible = true

	# Show available items (not currently equipped)
	var has_available: bool = false
	for item: Dictionary in items_for_slot:
		var instance_id: String = item.get("instance_id", "")
		if instance_id != equipped_instance_id:
			var item_card: PanelContainer = _create_item_card(item, false)
			_available_container.add_child(item_card)
			has_available = true

	if not has_available:
		var no_items_label: Label = Label.new()
		no_items_label.text = Loc.t("ui.equipment_modal.no_items_available")
		no_items_label.add_theme_font_size_override("font_size", 14)
		no_items_label.add_theme_color_override("font_color", EMPTY_COLOR)
		_available_container.add_child(no_items_label)


func _create_item_card(item: Dictionary, is_equipped: bool) -> PanelContainer:
	var panel: PanelContainer = PanelContainer.new()
	panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	# Style
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.12, 0.10, 0.08, 0.95) if is_equipped else Color(0.10, 0.08, 0.06, 0.9)
	style.border_color = ACCENT_COLOR if is_equipped else ACCENT_COLOR.darkened(0.3)
	style.set_border_width_all(2)
	style.set_corner_radius_all(6)
	panel.add_theme_stylebox_override("panel", style)

	# Make clickable if not equipped
	if not is_equipped:
		var instance_id: String = item.get("instance_id", "")
		panel.gui_input.connect(_on_item_card_input.bind(instance_id))
		panel.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND

	# Margin
	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 12)
	margin.add_theme_constant_override("margin_right", 12)
	margin.add_theme_constant_override("margin_top", 10)
	margin.add_theme_constant_override("margin_bottom", 10)
	panel.add_child(margin)

	# Content VBox
	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 4)
	margin.add_child(vbox)

	# Item name
	var name_key: String = item.get("name_key", "")
	var item_name: String = Loc.t(name_key) if not name_key.is_empty() else item.get("id", "Unknown")
	var name_label: Label = Label.new()
	name_label.text = item_name
	name_label.add_theme_font_size_override("font_size", 16)
	name_label.add_theme_color_override("font_color", ACCENT_COLOR.lightened(0.2) if is_equipped else TEXT_COLOR)
	vbox.add_child(name_label)

	# Item description/modifiers
	var desc_key: String = item.get("description_key", "")
	var description: String = Loc.t(desc_key) if not desc_key.is_empty() else ""
	if not description.is_empty():
		var desc_label: Label = Label.new()
		desc_label.text = description
		desc_label.add_theme_font_size_override("font_size", 12)
		desc_label.add_theme_color_override("font_color", TEXT_COLOR.darkened(0.2))
		desc_label.autowrap_mode = TextServer.AUTOWRAP_WORD
		vbox.add_child(desc_label)

	# Rarity
	var rarity: String = item.get("rarity", "common")
	var rarity_label: Label = Label.new()
	rarity_label.text = rarity.capitalize()
	rarity_label.add_theme_font_size_override("font_size", 11)
	rarity_label.add_theme_color_override("font_color", _get_rarity_color(rarity))
	vbox.add_child(rarity_label)

	return panel


func _get_rarity_color(rarity: String) -> Color:
	match rarity:
		"common": return Color(0.7, 0.7, 0.7)
		"uncommon": return Color(0.3, 0.8, 0.3)
		"rare": return Color(0.3, 0.5, 1.0)
		"epic": return Color(0.7, 0.3, 0.9)
		"legendary": return Color(1.0, 0.6, 0.1)
		_: return Color(0.7, 0.7, 0.7)


func _on_item_card_input(event: InputEvent, instance_id: String) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.button_index == MOUSE_BUTTON_LEFT and mouse_event.pressed:
			_equip_item(instance_id)


func _equip_item(instance_id: String) -> void:
	var success: bool = Items.equip_item(_summoner_id, instance_id, _current_slot)
	if success:
		item_equipped.emit(_current_slot, instance_id)
		_refresh()


func _on_unequip_pressed() -> void:
	var success: bool = Items.unequip_item(_summoner_id, _current_slot)
	if success:
		item_unequipped.emit(_current_slot)
		_refresh()


func _on_close_pressed() -> void:
	_canvas_layer.visible = false
	closed.emit()


func _on_dimmer_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.button_index == MOUSE_BUTTON_LEFT and mouse_event.pressed:
			_canvas_layer.visible = false
			closed.emit()
