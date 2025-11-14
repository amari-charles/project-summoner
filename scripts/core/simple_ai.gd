extends Node
class_name SimpleAI

## Simple AI controller for enemy summoner
## Plays cards at random intervals with basic strategy

@export var summoner: Node  # Can be Summoner or Summoner3D
@export var play_interval_min: float = 2.0
@export var play_interval_max: float = 5.0

var play_timer: float = 0.0
var next_play_time: float = 0.0
var is_3d: bool = false

func _ready() -> void:
	if summoner == null:
		var parent: Node = get_parent()
		if parent is Summoner:
			summoner = parent
			is_3d = false
		elif parent is Summoner3D:
			summoner = parent
			is_3d = true

	_set_next_play_time()

func _process(delta: float) -> void:
	if summoner == null:
		return
	var is_alive: bool = summoner.get("is_alive") if "is_alive" in summoner else false
	if not is_alive:
		return

	play_timer += delta

	if play_timer >= next_play_time:
		_attempt_play_card()
		_set_next_play_time()

## Try to play a random card
func _attempt_play_card() -> void:
	var playable_cards: Array[int] = _get_playable_card_indices()
	if playable_cards.is_empty():
		return

	var card_index: int = playable_cards.pick_random()
	_play_card_at_index(card_index)

## Get summoner's hand as an Array (returns empty if invalid)
func _get_hand_array() -> Array:
	if not summoner or not "hand" in summoner:
		return []
	var hand: Variant = summoner.get("hand")
	if not hand is Array:
		return []
	return hand

## Get summoner's mana value as int (returns -1 if invalid)
func _get_mana_value() -> int:
	if not summoner or not "mana" in summoner:
		return -1
	var mana_variant: Variant = summoner.get("mana")
	if mana_variant is int:
		return mana_variant
	elif mana_variant is float:
		return int(mana_variant)
	return -1

## Get indices of cards that can be played with current mana
func _get_playable_card_indices() -> Array[int]:
	var playable: Array[int] = []
	var hand_array: Array = _get_hand_array()
	if hand_array.is_empty():
		return playable

	var mana: int = _get_mana_value()
	if mana < 0:
		return playable

	for i: int in range(hand_array.size()):
		var card: Variant = hand_array[i]
		if card is Object:
			var card_obj: Object = card
			if card_obj.has_method("can_play"):
				var can_play_result: Variant = card_obj.call("can_play", mana)
				if can_play_result is bool and can_play_result:
					playable.append(i)

	return playable

## Play a card at the given index at a random position
func _play_card_at_index(card_index: int) -> void:
	if is_3d:
		# Play at a random position in 3D (enemy territory on right side)
		var spawn_x: float = randf_range(5.0, 9.0)    # Right side in 3D space
		var spawn_z: float = randf_range(-5.0, 5.0)   # Random depth
		var spawn_pos: Vector3 = Vector3(spawn_x, 1.0, spawn_z)
		if summoner and summoner.has_method("play_card_3d"):
			summoner.call("play_card_3d", card_index, spawn_pos)
	else:
		# Play at a random position in 2D (right side of screen)
		var spawn_x: float = randf_range(1400.0, 1700.0)
		var spawn_y: float = randf_range(200.0, 880.0)
		var spawn_pos: Vector2 = Vector2(spawn_x, spawn_y)
		if summoner and summoner.has_method("play_card"):
			summoner.call("play_card", card_index, spawn_pos)

## Reset the timer for next card play
func _set_next_play_time() -> void:
	play_timer = 0.0
	next_play_time = randf_range(play_interval_min, play_interval_max)
