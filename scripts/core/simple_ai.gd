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
	if not "hand" in summoner:
		return
	var hand: Variant = summoner.get("hand")
	if not hand is Array:
		return
	var hand_array: Array = hand
	if hand_array.is_empty():
		return

	# Pick a random card that we can afford
	var playable_cards: Array[int] = []
	var hand_size: int = hand_array.size()

	if not "mana" in summoner:
		return
	var mana_variant: Variant = summoner.get("mana")
	var mana_value: int = 0
	if mana_variant is int:
		var mana_int: int = mana_variant
		mana_value = mana_int
	elif mana_variant is float:
		var mana_float: float = mana_variant
		mana_value = int(mana_float)

	for i: int in range(hand_size):
		var card: Variant = hand_array[i]
		if card is Object:
			var card_obj: Object = card
			if card_obj.has_method("can_play"):
				var can_play_variant: Variant = card_obj.call("can_play", mana_value)
				var can_play_result: bool = can_play_variant if can_play_variant is bool else false
				if can_play_result:
					playable_cards.append(i)

	if playable_cards.is_empty():
		return

	var card_index: int = playable_cards.pick_random()

	if is_3d:
		# Play at a random position in 3D (enemy territory on right side)
		var spawn_x: float = randf_range(5.0, 9.0)    # Right side in 3D space
		var spawn_z: float = randf_range(-5.0, 5.0)   # Random depth
		var spawn_pos: Vector3 = Vector3(spawn_x, 1.0, spawn_z)
		if summoner.has_method("play_card_3d"):
			summoner.call("play_card_3d", card_index, spawn_pos)
	else:
		# Play at a random position in 2D (right side of screen)
		var spawn_x: float = randf_range(1400.0, 1700.0)
		var spawn_y: float = randf_range(200.0, 880.0)
		var spawn_pos: Vector2 = Vector2(spawn_x, spawn_y)
		if summoner.has_method("play_card"):
			summoner.call("play_card", card_index, spawn_pos)

## Reset the timer for next card play
func _set_next_play_time() -> void:
	play_timer = 0.0
	next_play_time = randf_range(play_interval_min, play_interval_max)
