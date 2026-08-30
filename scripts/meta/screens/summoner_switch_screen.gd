extends BackNavigableScreen
class_name SummonerSwitchScreen

## Animated, wrap-around carousel for choosing an unlocked summoner.

const SummonerCarouselItemScene: PackedScene = preload(
	"res://scenes/meta/components/summoner_carousel_item.tscn"
)
const ITEM_SPACING: float = 340.0
const CENTER_SCALE: float = 1.0
const SIDE_SCALE: float = 0.72
const SIDE_ALPHA: float = 0.48
const ANIMATION_DURATION: float = 0.28
const ITEM_SIZE: Vector2 = Vector2(300, 480)

@onready var background: ColorRect = %Background
@onready var close_button: Button = %CloseButton
@onready var title_label: Label = %TitleLabel
@onready var left_arrow: Button = %LeftArrow
@onready var right_arrow: Button = %RightArrow
@onready var carousel_area: Control = %CarouselArea
@onready var confirm_button: Button = %ConfirmButton

var _items: Array[Control] = []
var _summoner_ids: Array[String] = []
var _active_summoner_id: String = ""
var _selected_summoner_id: String = ""
var _current_index: int = 0
var _animation_tween: Tween
var _is_animating: bool = false


func _ready() -> void:
	QuestApi.record_ui_surface_opened("summoner_switch")
	QuestGuidance.clear()
	background.color = GameColorPalette.UI_BACKGROUND
	close_button.pressed.connect(_on_close_pressed)
	left_arrow.pressed.connect(_on_left_arrow_pressed)
	right_arrow.pressed.connect(_on_right_arrow_pressed)
	confirm_button.pressed.connect(_on_confirm_pressed)
	title_label.text = Loc.t("ui.summoner_switch.title")
	confirm_button.text = Loc.t("ui.summoner_switch.confirm")
	_active_summoner_id = SummonerSelectionApi.get_active_summoner_id()
	_load_carousel()
	QuestGuidance.show_for(close_button, "inventory")


func _load_carousel() -> void:
	for child: Node in carousel_area.get_children():
		child.queue_free()
	_items.clear()
	_summoner_ids.clear()
	_current_index = 0

	for value: Variant in SummonerSelectionApi.get_unlocked_summoner_ids_array():
		var summoner_id: String = SafeTypeUtils.string(value, "")
		if summoner_id.is_empty():
			continue
		_summoner_ids.append(summoner_id)
		if summoner_id == _active_summoner_id:
			_current_index = _summoner_ids.size() - 1

	for summoner_id: String in _summoner_ids:
		var item: Control = SummonerCarouselItemScene.instantiate() as Control
		carousel_area.add_child(item)
		item.call("set_summoner", summoner_id, summoner_id == _active_summoner_id)
		item.connect("selected", _on_item_selected)
		_items.append(item)

	_selected_summoner_id = _active_summoner_id
	_update_controls()
	await get_tree().process_frame
	_position_items(false)


func _wrapped_offset(item_index: int) -> int:
	var count: int = _items.size()
	if count <= 1:
		return 0
	var direct: int = item_index - _current_index
	var wrapped_left: int = direct - count
	var wrapped_right: int = direct + count
	if abs(wrapped_left) < abs(direct):
		return wrapped_left
	if abs(wrapped_right) < abs(direct):
		return wrapped_right
	return direct


func _target_position(item_index: int) -> Vector2:
	var offset: int = _wrapped_offset(item_index)
	return Vector2(
		(carousel_area.size.x - ITEM_SIZE.x) * 0.5 + offset * ITEM_SPACING,
		(carousel_area.size.y - ITEM_SIZE.y) * 0.5
	)


func _position_items(animated: bool) -> void:
	if _animation_tween and _animation_tween.is_valid():
		_animation_tween.kill()
	if animated:
		_is_animating = true
		_animation_tween = create_tween().set_parallel(true)
		_animation_tween.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)

	for index: int in range(_items.size()):
		var item: Control = _items[index]
		var offset: int = abs(_wrapped_offset(index))
		var target_alpha: float = 1.0 if offset == 0 else (SIDE_ALPHA if offset == 1 else 0.0)
		var target_scale: float = CENTER_SCALE if offset == 0 else SIDE_SCALE
		item.visible = target_alpha > 0.0
		item.pivot_offset = ITEM_SIZE * 0.5
		item.z_index = 10 - offset
		item.call("set_focused", offset == 0)
		if animated:
			_animation_tween.tween_property(item, "position", _target_position(index), ANIMATION_DURATION)
			_animation_tween.tween_property(item, "scale", Vector2.ONE * target_scale, ANIMATION_DURATION)
			_animation_tween.tween_property(item, "modulate:a", target_alpha, ANIMATION_DURATION)
		else:
			item.position = _target_position(index)
			item.scale = Vector2.ONE * target_scale
			item.modulate.a = target_alpha

	if animated:
		_animation_tween.chain().tween_callback(_on_animation_finished)
	_update_controls()


func _on_animation_finished() -> void:
	_is_animating = false
	for item: Control in _items:
		item.visible = item.modulate.a > 0.0


func _move(direction: int) -> void:
	if _is_animating or _items.size() <= 1:
		return
	_current_index = wrapi(_current_index + direction, 0, _items.size())
	_selected_summoner_id = _summoner_ids[_current_index]
	_position_items(true)


func _on_left_arrow_pressed() -> void:
	_move(-1)


func _on_right_arrow_pressed() -> void:
	_move(1)


func _on_item_selected(summoner_id: String) -> void:
	var index: int = _summoner_ids.find(summoner_id)
	if index < 0 or _is_animating:
		return
	_current_index = index
	_selected_summoner_id = summoner_id
	_position_items(true)


func _update_controls() -> void:
	var can_browse: bool = _items.size() > 1
	left_arrow.disabled = not can_browse
	right_arrow.disabled = not can_browse
	confirm_button.disabled = (
		_selected_summoner_id.is_empty()
		or _selected_summoner_id == _active_summoner_id
	)


func _on_confirm_pressed() -> void:
	if confirm_button.disabled:
		return
	SummonerSelectionApi.set_active_summoner(_selected_summoner_id, {})
	_close()


func _on_close_pressed() -> void:
	_close()


func _close() -> void:
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_SUMMONER_SCREEN
	SceneManager.transition_to(return_scene)


func _request_back_navigation() -> void:
	_on_close_pressed()
