extends Control
class_name SummonerSwitchScreen

## SummonerSwitchScreen - Carousel for switching between unlocked summoners
##
## Displays all unlocked summoners as cards in a horizontal carousel.
## User can navigate with arrow buttons or swipe, select a card, then confirm.

const SummonerCardScene: PackedScene = preload("res://scenes/ui/summoner_card.tscn")

## =============================================================================
## NODE REFERENCES
## =============================================================================

@onready var background: ColorRect = %Background
@onready var close_button: Button = %CloseButton
@onready var title_label: Label = %TitleLabel
@onready var left_arrow: Button = %LeftArrow
@onready var right_arrow: Button = %RightArrow
@onready var card_scroll: ScrollContainer = %CardScroll
@onready var card_container: HBoxContainer = %CardContainer
@onready var confirm_button: Button = %ConfirmButton

## =============================================================================
## CAROUSEL SETTINGS
## =============================================================================

const CARD_SCALE_CENTER: float = 1.0
const CARD_SCALE_SIDE: float = 0.75
const CARD_ALPHA_CENTER: float = 1.0
const CARD_ALPHA_SIDE: float = 0.5
const SNAP_THRESHOLD: float = 50.0  # Pixels from center to trigger snap

## =============================================================================
## STATE
## =============================================================================

var _summoner_cards: Array[SummonerCard] = []
var _selected_summoner_id: String = ""
var _active_summoner_id: String = ""
var _current_index: int = 0
var _scroll_tween: Tween = null
var _last_scroll_pos: int = 0
var _scroll_velocity: float = 0.0
var _is_dragging: bool = false

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
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if summoner_selection and summoner_selection.has_method("get_active_summoner_id"):
		_active_summoner_id = summoner_selection.call("get_active_summoner_id")

	# Load unlocked summoners
	_load_summoner_cards()

	# Update arrow visibility
	_update_arrow_states()

	# Update background based on active summoner's element
	_update_background()

	# Connect scroll signals for snap behavior
	card_scroll.get_h_scroll_bar().value_changed.connect(_on_scroll_changed)


func _process(_delta: float) -> void:
	_update_card_visuals()
	_check_snap_scroll()


## =============================================================================
## CARD LOADING
## =============================================================================

func _load_summoner_cards() -> void:
	# Clear existing
	for child: Node in card_container.get_children():
		child.queue_free()
	_summoner_cards.clear()

	# Get unlocked summoners
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if not summoner_selection or not summoner_selection.has_method("get_unlocked_summoner_ids"):
		return

	var unlocked_ids: Variant = summoner_selection.call("get_unlocked_summoner_ids")
	if not unlocked_ids is Array:
		return

	# Add left spacer so first card can be centered
	var left_spacer: Control = Control.new()
	left_spacer.custom_minimum_size.x = card_scroll.size.x / 2.0 - 150  # Half scroll width minus half card width
	card_container.add_child(left_spacer)

	# Create card for each unlocked summoner
	for summoner_id: Variant in unlocked_ids:
		if summoner_id is String:
			var card: SummonerCard = SummonerCardScene.instantiate()
			card_container.add_child(card)
			card.set_summoner(summoner_id)
			card.summoner_selected.connect(_on_card_selected)
			_summoner_cards.append(card)

			# Mark active summoner
			if summoner_id == _active_summoner_id:
				_current_index = _summoner_cards.size() - 1

	# Add right spacer so last card can be centered
	var right_spacer: Control = Control.new()
	right_spacer.custom_minimum_size.x = card_scroll.size.x / 2.0 - 150
	card_container.add_child(right_spacer)

	# Scroll to active summoner after layout
	await get_tree().process_frame
	await get_tree().process_frame
	_scroll_to_index(_current_index, false)
	_update_card_visuals()


## =============================================================================
## NAVIGATION
## =============================================================================

func _on_left_arrow_pressed() -> void:
	if _current_index > 0:
		_current_index -= 1
		_scroll_to_index(_current_index, true)
		_update_arrow_states()


func _on_right_arrow_pressed() -> void:
	if _current_index < _summoner_cards.size() - 1:
		_current_index += 1
		_scroll_to_index(_current_index, true)
		_update_arrow_states()


func _scroll_to_index(index: int, animate: bool) -> void:
	if _summoner_cards.is_empty():
		return

	index = clamp(index, 0, _summoner_cards.size() - 1)
	var target_card: SummonerCard = _summoner_cards[index]

	# Calculate scroll position to center the card
	var scroll_width: float = card_scroll.size.x
	var card_center: float = target_card.position.x + target_card.size.x / 2.0
	var target_scroll: float = card_center - scroll_width / 2.0
	target_scroll = max(0, target_scroll)

	if animate:
		if _scroll_tween and _scroll_tween.is_valid():
			_scroll_tween.kill()
		_scroll_tween = create_tween()
		_scroll_tween.tween_property(card_scroll, "scroll_horizontal", int(target_scroll), 0.3)\
			.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
	else:
		card_scroll.scroll_horizontal = int(target_scroll)


func _update_arrow_states() -> void:
	left_arrow.disabled = _current_index <= 0
	right_arrow.disabled = _current_index >= _summoner_cards.size() - 1


## =============================================================================
## SELECTION
## =============================================================================

func _on_card_selected(summoner_id: String) -> void:
	_selected_summoner_id = summoner_id
	_update_selection_visuals()
	confirm_button.disabled = false


func _update_selection_visuals() -> void:
	for card: SummonerCard in _summoner_cards:
		if card.summoner_id == _selected_summoner_id:
			card.show_glow()
		else:
			card.hide_glow()


## =============================================================================
## CONFIRM & CLOSE
## =============================================================================

func _on_confirm_pressed() -> void:
	if _selected_summoner_id.is_empty():
		return

	# Switch summoner
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if summoner_selection and summoner_selection.has_method("set_active_summoner"):
		summoner_selection.call("set_active_summoner", _selected_summoner_id)

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

	var config: SummonerConfig = SummonerCatalog.get_summoner_config(_active_summoner_id)
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


## =============================================================================
## CAROUSEL EFFECTS
## =============================================================================

func _update_card_visuals() -> void:
	if _summoner_cards.is_empty():
		return

	var scroll_center: float = card_scroll.scroll_horizontal + card_scroll.size.x / 2.0

	for card: SummonerCard in _summoner_cards:
		# Get card center position relative to scroll
		var card_center: float = card.position.x + card.size.x / 2.0
		var distance: float = abs(card_center - scroll_center)

		# Calculate scale and alpha based on distance from center
		# Max distance where effect applies (half the scroll width)
		var max_distance: float = card_scroll.size.x / 2.0
		var t: float = clamp(distance / max_distance, 0.0, 1.0)

		# Interpolate scale and alpha
		var target_scale: float = lerp(CARD_SCALE_CENTER, CARD_SCALE_SIDE, t)
		var target_alpha: float = lerp(CARD_ALPHA_CENTER, CARD_ALPHA_SIDE, t)

		# Apply with pivot at center
		card.pivot_offset = card.size / 2.0
		card.scale = Vector2(target_scale, target_scale)
		card.modulate.a = target_alpha


func _on_scroll_changed(_value: float) -> void:
	# Track scroll velocity for snap detection
	var current_scroll: int = card_scroll.scroll_horizontal
	_scroll_velocity = abs(current_scroll - _last_scroll_pos)
	_last_scroll_pos = current_scroll
	_is_dragging = true


func _check_snap_scroll() -> void:
	# Only snap when scroll has stopped (velocity near zero)
	if not _is_dragging:
		return

	# Check if tween is running
	var tween_running: bool = _scroll_tween != null and _scroll_tween.is_valid() and _scroll_tween.is_running()

	if _scroll_velocity < 1.0 and not tween_running:
		_is_dragging = false
		_snap_to_nearest_card()


func _snap_to_nearest_card() -> void:
	if _summoner_cards.is_empty():
		return

	var scroll_center: float = card_scroll.scroll_horizontal + card_scroll.size.x / 2.0
	var nearest_index: int = 0
	var nearest_distance: float = INF

	for i: int in range(_summoner_cards.size()):
		var card: SummonerCard = _summoner_cards[i]
		var card_center: float = card.position.x + card.size.x / 2.0
		var distance: float = abs(card_center - scroll_center)

		if distance < nearest_distance:
			nearest_distance = distance
			nearest_index = i

	# Update current index and scroll to it
	if nearest_index != _current_index:
		_current_index = nearest_index
		_update_arrow_states()

	_scroll_to_index(_current_index, true)
