extends Control
class_name SummonerSwitchScreen

## SummonerSwitchScreen - Animated carousel for switching between unlocked summoners
##
## Displays summoners as cards with smooth animations. Cards slide into position
## when navigating left/right, with the center card at full scale and side cards
## smaller and faded.

const SummonerCardScene: PackedScene = preload("res://scenes/meta/components/summoner_card.tscn")

## =============================================================================
## NODE REFERENCES
## =============================================================================

@onready var background: ColorRect = %Background
@onready var close_button: Button = %CloseButton
@onready var title_label: Label = %TitleLabel
@onready var left_arrow: Button = %LeftArrow
@onready var right_arrow: Button = %RightArrow
@onready var card_area: Control = %CardArea
@onready var confirm_button: Button = %ConfirmButton

## =============================================================================
## CAROUSEL SETTINGS
## =============================================================================

const CARD_WIDTH: float = 300.0  # SummonerCard base width
const CARD_HEIGHT: float = 400.0  # SummonerCard base height
const CARD_SPACING: float = 320.0  # Horizontal distance between card centers
const CARD_SCALE_CENTER: float = 1.0
const CARD_SCALE_SIDE: float = 0.7
const CARD_ALPHA_CENTER: float = 1.0
const CARD_ALPHA_SIDE: float = 0.4
const ANIMATION_DURATION: float = 0.35

## =============================================================================
## STATE
## =============================================================================

var _summoner_cards: Array[SummonerCard] = []
var _summoner_ids: Array[String] = []
var _selected_summoner_id: String = ""
var _active_summoner_id: String = ""
var _current_index: int = 0
var _animation_tween: Tween = null
var _is_animating: bool = false

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Connect buttons
	close_button.pressed.connect(_on_close_pressed)
	left_arrow.pressed.connect(_on_left_arrow_pressed)
	right_arrow.pressed.connect(_on_right_arrow_pressed)
	confirm_button.pressed.connect(_on_confirm_pressed)

	# Set localized text
	title_label.text = Loc.t("ui.summoner_switch.title")
	confirm_button.text = Loc.t("ui.summoner_switch.confirm")

	# Start with confirm disabled until selection
	confirm_button.disabled = true

	# Get active summoner
	_active_summoner_id = SummonerSelectionApi.get_active_summoner_id()

	# Load unlocked summoners
	_load_summoner_cards()

	# Update arrow visibility
	_update_arrow_states()

	# Update background based on active summoner's element
	_update_background()


## =============================================================================
## CARD LOADING
## =============================================================================

func _load_summoner_cards() -> void:
	# Clear existing
	for child: Node in card_area.get_children():
		child.queue_free()
	_summoner_cards.clear()
	_summoner_ids.clear()

	# Get unlocked summoners (use Godot array variant for GDScript interop)
	var unlocked_ids: Array = SummonerSelectionApi.get_unlocked_summoner_ids_array()

	# Store IDs and find active summoner index
	for summoner_id: Variant in unlocked_ids:
		var summoner_id_str: String = SafeTypeUtils.string(summoner_id, "")
		if summoner_id_str.is_empty():
			continue
		_summoner_ids.append(summoner_id_str)
		if summoner_id_str == _active_summoner_id:
			_current_index = _summoner_ids.size() - 1

	# Create cards for visible range
	_create_visible_cards()

	# Position cards after layout
	await get_tree().process_frame
	_position_cards_instant()


func _create_visible_cards() -> void:
	# Create a card for each summoner
	for i: int in range(_summoner_ids.size()):
		var summoner_id: String = _summoner_ids[i]
		var card: SummonerCard = SummonerCardScene.instantiate()
		card_area.add_child(card)
		card.set_summoner(summoner_id)
		card.summoner_selected.connect(_on_card_selected)
		_summoner_cards.append(card)


## =============================================================================
## CARD POSITIONING
## =============================================================================

func _get_wrapped_offset(card_index: int) -> int:
	# Calculate the shortest offset considering wrap-around
	var count: int = _summoner_cards.size()
	if count <= 1:
		return 0

	var direct_offset: int = card_index - _current_index

	# Calculate wrapped offsets (going the other way around)
	var wrap_left: int = direct_offset - count  # Wrap going left
	var wrap_right: int = direct_offset + count  # Wrap going right

	# Return the offset with smallest absolute value
	if abs(wrap_left) < abs(direct_offset):
		return wrap_left
	elif abs(wrap_right) < abs(direct_offset):
		return wrap_right
	else:
		return direct_offset


func _get_card_target_position(card_index: int) -> Vector2:
	# Calculate position relative to center using wrapped offset
	var offset_from_center: int = _get_wrapped_offset(card_index)
	var center_x: float = card_area.size.x / 2.0
	var center_y: float = card_area.size.y / 2.0

	var x: float = center_x + (offset_from_center * CARD_SPACING) - CARD_WIDTH / 2.0
	var y: float = center_y - CARD_HEIGHT / 2.0

	return Vector2(x, y)


func _get_card_target_scale(card_index: int) -> float:
	var offset: int = abs(_get_wrapped_offset(card_index))
	if offset == 0:
		return CARD_SCALE_CENTER
	else:
		return CARD_SCALE_SIDE


func _get_card_target_alpha(card_index: int) -> float:
	var offset: int = abs(_get_wrapped_offset(card_index))
	if offset == 0:
		return CARD_ALPHA_CENTER
	elif offset == 1:
		return CARD_ALPHA_SIDE
	else:
		return 0.0  # Hide cards beyond visible range


func _position_cards_instant() -> void:
	for i: int in range(_summoner_cards.size()):
		var card: SummonerCard = _summoner_cards[i]
		var target_pos: Vector2 = _get_card_target_position(i)
		var target_scale: float = _get_card_target_scale(i)
		var target_alpha: float = _get_card_target_alpha(i)

		card.position = target_pos
		card.pivot_offset = card.size / 2.0
		card.scale = Vector2(target_scale, target_scale)
		card.modulate.a = target_alpha
		card.visible = target_alpha > 0.0

		# Update z-index so center card is on top (use wrapped offset)
		card.z_index = 10 - abs(_get_wrapped_offset(i))


func _animate_cards_to_positions() -> void:
	if _animation_tween and _animation_tween.is_valid():
		_animation_tween.kill()

	_is_animating = true
	_animation_tween = create_tween()
	_animation_tween.set_parallel(true)
	_animation_tween.set_ease(Tween.EASE_OUT)
	_animation_tween.set_trans(Tween.TRANS_CUBIC)

	for i: int in range(_summoner_cards.size()):
		var card: SummonerCard = _summoner_cards[i]
		var target_pos: Vector2 = _get_card_target_position(i)
		var target_scale: float = _get_card_target_scale(i)
		var target_alpha: float = _get_card_target_alpha(i)

		# Make visible if it will be visible
		if target_alpha > 0.0:
			card.visible = true

		card.pivot_offset = card.size / 2.0

		# Animate position
		_animation_tween.tween_property(card, "position", target_pos, ANIMATION_DURATION)

		# Animate scale
		_animation_tween.tween_property(card, "scale", Vector2(target_scale, target_scale), ANIMATION_DURATION)

		# Animate alpha
		_animation_tween.tween_property(card, "modulate:a", target_alpha, ANIMATION_DURATION)

		# Update z-index immediately so center card goes on top (use wrapped offset)
		card.z_index = 10 - abs(_get_wrapped_offset(i))

	_animation_tween.chain().tween_callback(_on_animation_complete)


func _on_animation_complete() -> void:
	_is_animating = false
	# Hide cards that are fully transparent
	for i: int in range(_summoner_cards.size()):
		var card: SummonerCard = _summoner_cards[i]
		if card.modulate.a <= 0.0:
			card.visible = false


## =============================================================================
## NAVIGATION
## =============================================================================

func _on_left_arrow_pressed() -> void:
	if _is_animating or _summoner_cards.size() <= 1:
		return
	_current_index = wrapi(_current_index - 1, 0, _summoner_cards.size())
	_animate_cards_to_positions()


func _on_right_arrow_pressed() -> void:
	if _is_animating or _summoner_cards.size() <= 1:
		return
	_current_index = wrapi(_current_index + 1, 0, _summoner_cards.size())
	_animate_cards_to_positions()


func _update_arrow_states() -> void:
	# Arrows always enabled for infinite carousel (unless only 1 card)
	var single_card: bool = _summoner_cards.size() <= 1
	left_arrow.disabled = single_card
	right_arrow.disabled = single_card


## =============================================================================
## SELECTION
## =============================================================================

func _on_card_selected(summoner_id: String) -> void:
	# Find the index of the selected card
	var selected_index: int = _summoner_ids.find(summoner_id)

	# If clicking a side card, navigate to it first
	if selected_index != _current_index and not _is_animating:
		_current_index = selected_index
		_animate_cards_to_positions()

	_selected_summoner_id = summoner_id
	_update_selection_visuals()
	confirm_button.disabled = false


func _update_selection_visuals() -> void:
	for card: SummonerCard in _summoner_cards:
		if card.summoner_id == _selected_summoner_id:
			card.show_glow(true)  # Mark as selected (persists through hover)
		else:
			card.hide_glow(true)  # Force deselect


## =============================================================================
## CONFIRM & CLOSE
## =============================================================================

func _on_confirm_pressed() -> void:
	if _selected_summoner_id.is_empty():
		return

	# Switch summoner
	SummonerSelectionApi.set_active_summoner(_selected_summoner_id, null)

	_close()


func _on_close_pressed() -> void:
	_close()


func _close() -> void:
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_SUMMONER_SCREEN
	SceneManager.transition_to(return_scene)


## =============================================================================
## BACKGROUND
## =============================================================================

func _update_background() -> void:
	if _active_summoner_id.is_empty():
		return

	var config: SummonerConfig = SummonerConfig.from_dict(SummonerCatalogApi.get_summoner(_active_summoner_id))
	if not config:
		return

	var element: ElementTypes.Element = config.get_element()
	var gradient_colors: Array[Color] = CardVisualHelper.get_element_gradient_colors(element.id)

	var material: ShaderMaterial = background.material as ShaderMaterial
	if not material:
		return

	if gradient_colors.size() >= 2:
		material.set_shader_parameter("color_primary", gradient_colors[1])
		material.set_shader_parameter("color_secondary", gradient_colors[0])
