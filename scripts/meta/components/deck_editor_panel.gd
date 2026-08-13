extends VBoxContainer
class_name DeckEditorPanel

signal add_card_requested(instance_id: String)
signal remove_card_requested(instance_id: String)
signal card_info_requested(instance_id: String, catalog_id: String)

const CardWidgetScene: PackedScene = preload("res://scenes/meta/components/card_widget.tscn")
const CardActionPopupScene: PackedScene = preload("res://scenes/meta/components/card_action_popup.tscn")
const DOUBLE_CLICK_THRESHOLD_MS: int = 400

@export var card_size: Vector2 = Vector2(160, 240)
@export var active_card_separation: int = 6
@export var available_horizontal_separation: int = 10
@export var available_vertical_separation: int = 10
@export var heading_font_size: int = 18
@export var transparent_active_background: bool = false

@onready var active_deck_zone: DeckDropZone = %ActiveDeckZone
@onready var active_deck_label: Label = %ActiveDeckLabel
@onready var active_deck_count: Label = %ActiveDeckCount
@onready var active_cards: HBoxContainer = %ActiveCards
@onready var available_drop_zone: CollectionDropZone = %AvailableDropZone
@onready var available_cards: GridContainer = %AvailableCards

var can_add_callback: Callable = Callable()
var can_remove_callback: Callable = Callable()

var _active_entries: Array[Dictionary] = []
var _available_entries: Array[Dictionary] = []
var _active_ids: Array[String] = []
var _locked_ids: Array[String] = []
var _max_deck_size: int = DeckConstants.MAX_DECK_SIZE
var _has_editable_deck: bool = false
var _active_popup: CardActionPopup = null
var _last_clicked_id: String = ""
var _last_click_time: int = 0


func _ready() -> void:
	active_cards.add_theme_constant_override("separation", active_card_separation)
	available_cards.add_theme_constant_override("h_separation", available_horizontal_separation)
	available_cards.add_theme_constant_override("v_separation", available_vertical_separation)
	active_deck_label.add_theme_font_size_override("font_size", heading_font_size)
	if transparent_active_background:
		var transparent_panel: StyleBoxFlat = StyleBoxFlat.new()
		transparent_panel.bg_color = Color.TRANSPARENT
		active_deck_zone.add_theme_stylebox_override("panel", transparent_panel)
	active_deck_zone.can_drop_callback = _can_add_card
	active_deck_zone.card_dropped.connect(_on_card_dropped_to_add)
	available_drop_zone.can_remove_callback = _can_remove_card
	available_drop_zone.card_dropped_to_remove.connect(_on_card_dropped_to_remove)


func set_available_columns(columns: int) -> void:
	available_cards.columns = maxi(columns, 1)


func set_active_deck(
	title: String,
	entries: Array[Dictionary],
	max_deck_size: int = DeckConstants.MAX_DECK_SIZE,
	has_editable_deck: bool = true
) -> void:
	_active_entries = entries
	_max_deck_size = max_deck_size
	_has_editable_deck = has_editable_deck
	active_deck_label.text = title
	active_deck_count.text = "%d/%d" % [entries.size(), max_deck_size] if has_editable_deck else ""
	_render_active_cards()


func set_available_cards(entries: Array[Dictionary]) -> void:
	_available_entries = entries
	_render_available_cards()


func dismiss_popup() -> void:
	if _active_popup and is_instance_valid(_active_popup):
		_active_popup.queue_free()
	_active_popup = null


func _render_active_cards() -> void:
	dismiss_popup()
	_clear(active_cards)
	_active_ids.clear()
	_locked_ids.clear()
	for entry: Dictionary in _active_entries:
		var instance_id: String = SafeTypeUtils.string(entry.get("instance_id"))
		if instance_id.is_empty():
			continue
		_active_ids.append(instance_id)
		if SafeTypeUtils.bool_val(entry.get("locked")):
			_locked_ids.append(instance_id)
		_add_widget(active_cards, entry, true)


func _render_available_cards() -> void:
	dismiss_popup()
	_clear(available_cards)
	for entry: Dictionary in _available_entries:
		var instance_id: String = SafeTypeUtils.string(entry.get("instance_id"))
		if instance_id.is_empty() or instance_id in _active_ids:
			continue
		_add_widget(available_cards, entry, false)


func _add_widget(parent: Control, entry: Dictionary, in_active_deck: bool) -> void:
	var instance_id: String = SafeTypeUtils.string(entry.get("instance_id"))
	var card_data: Dictionary = SafeTypeUtils.dict(entry.get("card_data"))
	if card_data.is_empty():
		card_data = CardServiceApi.get_card_dict(instance_id)
	var catalog_id: String = SafeTypeUtils.string(
		entry.get("catalog_id", card_data.get("catalog_id"))
	)
	var catalog_data: Dictionary = SafeTypeUtils.dict(entry.get("catalog_data"))
	if catalog_data.is_empty():
		catalog_data = CardCatalogApi.get_card_as_dict(catalog_id)
	if card_data.is_empty() or catalog_data.is_empty():
		return

	var locked: bool = SafeTypeUtils.bool_val(entry.get("locked"))
	var widget: CardWidget = CardWidgetScene.instantiate()
	parent.add_child(widget)
	widget.set_card(card_data, catalog_data)
	widget.set_draggable(_has_editable_deck and not locked)
	widget.custom_minimum_size = card_size
	widget.tooltip_text = SafeTypeUtils.string(entry.get("tooltip"))
	if widget.tooltip_text.is_empty():
		widget.tooltip_text = SafeTypeUtils.string(catalog_data.get("card_name"), catalog_id)
	var detail_instance_id: String = SafeTypeUtils.string(entry.get("detail_instance_id", instance_id))
	if not detail_instance_id.is_empty():
		var progression: Dictionary = CardServiceApi.get_card_progression_info_dict(detail_instance_id)
		if not progression.is_empty():
			widget.set_progression(progression)
	widget.card_clicked.connect(
		func(_card_data: Dictionary) -> void:
			_on_card_clicked(widget, instance_id, detail_instance_id, catalog_id, in_active_deck, locked)
	)
	widget.card_held.connect(
		func(_card_data: Dictionary) -> void:
			dismiss_popup()
			card_info_requested.emit(detail_instance_id, catalog_id)
	)


func _on_card_clicked(
	widget: CardWidget,
	instance_id: String,
	detail_instance_id: String,
	catalog_id: String,
	in_active_deck: bool,
	locked: bool
) -> void:
	dismiss_popup()
	if locked:
		AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
		card_info_requested.emit(detail_instance_id, catalog_id)
		return
	var current_time: int = Time.get_ticks_msec()
	if instance_id == _last_clicked_id and current_time - _last_click_time < DOUBLE_CLICK_THRESHOLD_MS:
		_last_clicked_id = ""
		_last_click_time = 0
		if in_active_deck:
			_request_remove(instance_id)
		else:
			_request_add(instance_id)
		return
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_last_clicked_id = instance_id
	_last_click_time = current_time
	_show_action_popup(widget, instance_id, catalog_id, in_active_deck)


func _show_action_popup(
	widget: CardWidget,
	instance_id: String,
	catalog_id: String,
	in_active_deck: bool
) -> void:
	var popup: CardActionPopup = CardActionPopupScene.instantiate()
	add_child(popup)
	_active_popup = popup
	var widget_rect: Rect2 = widget.get_global_rect()
	popup.show_at(
		Vector2(widget_rect.position.x + widget_rect.size.x / 2.0, widget_rect.end.y + 5.0),
		instance_id,
		catalog_id,
		in_active_deck,
		_has_editable_deck
	)
	popup.use_pressed.connect(func(card_id: String, _catalog_id: String) -> void: _request_add(card_id))
	popup.remove_pressed.connect(func(card_id: String, _catalog_id: String) -> void: _request_remove(card_id))
	popup.info_pressed.connect(
		func(card_id: String, card_catalog_id: String) -> void:
			dismiss_popup()
			card_info_requested.emit(card_id, card_catalog_id)
	)
	popup.dismissed.connect(dismiss_popup)


func _can_add_card(instance_id: String) -> bool:
	if not _has_editable_deck or instance_id.is_empty():
		return false
	if _active_ids.size() >= _max_deck_size or instance_id in _active_ids:
		return false
	return not can_add_callback.is_valid() or SafeTypeUtils.bool_val(can_add_callback.call(instance_id))


func _can_remove_card(instance_id: String) -> bool:
	if not _has_editable_deck or instance_id not in _active_ids or instance_id in _locked_ids:
		return false
	return not can_remove_callback.is_valid() or SafeTypeUtils.bool_val(can_remove_callback.call(instance_id))


func _request_add(instance_id: String) -> void:
	dismiss_popup()
	if _can_add_card(instance_id):
		add_card_requested.emit(instance_id)


func _request_remove(instance_id: String) -> void:
	dismiss_popup()
	if _can_remove_card(instance_id):
		remove_card_requested.emit(instance_id)


func _on_card_dropped_to_add(instance_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_request_add(instance_id)


func _on_card_dropped_to_remove(instance_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	_request_remove(instance_id)


func _clear(parent: Control) -> void:
	for child: Node in parent.get_children():
		child.queue_free()
