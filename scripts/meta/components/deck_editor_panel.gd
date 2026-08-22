extends VBoxContainer
class_name DeckEditorPanel

signal add_card_requested(instance_id: String)
signal remove_card_requested(instance_id: String)
signal card_info_requested(instance_id: String, catalog_id: String)

const CardWidgetScene: PackedScene = preload("res://scenes/meta/components/card_widget.tscn")

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
var _available_widgets_by_id: Dictionary = {}


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


func set_available_cards(entries: Array[Dictionary], update_existing_widgets: bool = true) -> void:
	_available_entries = entries
	_render_available_cards(update_existing_widgets)


func dismiss_popup() -> void:
	# Kept as a stable cleanup hook for screens that previously hosted a card
	# action popup. Card actions now live in the card detail view.
	pass


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


func _render_available_cards(update_existing_widgets: bool) -> void:
	dismiss_popup()
	var desired_ids: Dictionary = {}
	var next_index: int = 0
	for entry: Dictionary in _available_entries:
		var instance_id: String = SafeTypeUtils.string(entry.get("instance_id"))
		if instance_id.is_empty() or instance_id in _active_ids:
			continue
		desired_ids[instance_id] = true
		var widget: CardWidget = _available_widgets_by_id.get(instance_id) as CardWidget
		if widget == null or not is_instance_valid(widget):
			widget = _add_widget(available_cards, entry, false)
			if widget == null:
				continue
			_available_widgets_by_id[instance_id] = widget
		elif update_existing_widgets:
			_configure_widget(widget, entry)
		available_cards.move_child(widget, next_index)
		next_index += 1

	for existing_id: Variant in _available_widgets_by_id.keys():
		if desired_ids.has(existing_id):
			continue
		var stale_widget: CardWidget = _available_widgets_by_id[existing_id] as CardWidget
		_available_widgets_by_id.erase(existing_id)
		if stale_widget and is_instance_valid(stale_widget):
			available_cards.remove_child(stale_widget)
			stale_widget.queue_free()


func _add_widget(parent: Control, entry: Dictionary, in_active_deck: bool) -> CardWidget:
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
		return null

	var locked: bool = SafeTypeUtils.bool_val(entry.get("locked"))
	var widget: CardWidget = CardWidgetScene.instantiate()
	parent.add_child(widget)
	_configure_widget(widget, entry, card_data, catalog_data)
	var detail_instance_id: String = SafeTypeUtils.string(entry.get("detail_instance_id", instance_id))
	widget.card_clicked.connect(
		func(_card_data: Dictionary) -> void:
			_on_card_clicked(widget, instance_id, detail_instance_id, catalog_id, in_active_deck, locked)
	)
	widget.card_inspected.connect(
		func(_card_data: Dictionary) -> void:
			_request_card_info(detail_instance_id, catalog_id)
	)
	widget.card_held.connect(
		func(_card_data: Dictionary) -> void:
			_request_card_info(detail_instance_id, catalog_id)
	)
	return widget


func _configure_widget(
	widget: CardWidget,
	entry: Dictionary,
	card_data: Dictionary = {},
	catalog_data: Dictionary = {}
) -> void:
	var instance_id: String = SafeTypeUtils.string(entry.get("instance_id"))
	if card_data.is_empty():
		card_data = SafeTypeUtils.dict(entry.get("card_data"))
	if card_data.is_empty():
		card_data = CardServiceApi.get_card_dict(instance_id)
	var catalog_id: String = SafeTypeUtils.string(
		entry.get("catalog_id", card_data.get("catalog_id"))
	)
	if catalog_data.is_empty():
		catalog_data = SafeTypeUtils.dict(entry.get("catalog_data"))
	if catalog_data.is_empty():
		catalog_data = CardCatalogApi.get_card_as_dict(catalog_id)
	if card_data.is_empty() or catalog_data.is_empty():
		return

	widget.set_card(card_data, catalog_data)
	widget.set_draggable(_has_editable_deck and not SafeTypeUtils.bool_val(entry.get("locked")))
	widget.custom_minimum_size = card_size
	widget.tooltip_text = SafeTypeUtils.string(entry.get("tooltip"))
	if widget.tooltip_text.is_empty():
		var card_name: String = SafeTypeUtils.string(catalog_data.get("card_name"), catalog_id)
		var is_locked: bool = SafeTypeUtils.bool_val(entry.get("locked"))
		var interaction_hint: String = Loc.t("ui.collection.card_inspect_tooltip")
		if _has_editable_deck and not is_locked:
			interaction_hint = Loc.t("ui.collection.deck_card_inspect_tooltip")
		widget.tooltip_text = "%s\n%s" % [card_name, interaction_hint]

	var detail_instance_id: String = SafeTypeUtils.string(entry.get("detail_instance_id", instance_id))
	var progression: Dictionary = SafeTypeUtils.dict(entry.get("progression"))
	if not entry.has("progression") and not detail_instance_id.is_empty():
		progression = CardServiceApi.get_card_progression_info_dict(detail_instance_id)
	widget.set_progression(progression)


func _on_card_clicked(
	_widget: CardWidget,
	_instance_id: String,
	detail_instance_id: String,
	catalog_id: String,
	_in_active_deck: bool,
	_locked: bool
) -> void:
	if not _has_editable_deck:
		_request_card_info(detail_instance_id, catalog_id)
		return

	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if _in_active_deck:
		if not _locked:
			_request_remove(_instance_id)
	else:
		_request_add(_instance_id)


func _request_card_info(detail_instance_id: String, catalog_id: String) -> void:
	dismiss_popup()
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	card_info_requested.emit(detail_instance_id, catalog_id)


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
