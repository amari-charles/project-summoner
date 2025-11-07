extends Node
class_name SimpleAI

## Simple AI controller for enemy summoner
## Plays cards at random intervals with basic strategy

@export var summoner  # Can be Summoner or Summoner3D
@export var play_interval_min: float = 2.0
@export var play_interval_max: float = 5.0

var play_timer: float = 0.0
var next_play_time: float = 0.0
var is_3d: bool = false

func _ready() -> void:
	if summoner == null:
		var parent = get_parent()
		if parent is Summoner:
			summoner = parent
			is_3d = false
		elif parent.get_script() and parent.get_script().get_global_name() == "Summoner3D":
			summoner = parent
			is_3d = true

	_set_next_play_time()

func _process(delta: float) -> void:
	if summoner == null or not summoner.is_alive:
		return

	play_timer += delta

	if play_timer >= next_play_time:
		_attempt_play_card()
		_set_next_play_time()

## Try to play a random card
func _attempt_play_card() -> void:
	if summoner.hand.is_empty():
		return

	# Pick a random card that we can afford
	var playable_cards: Array[int] = []
	for i in range(summoner.hand.size()):
		if summoner.hand[i].can_play(int(summoner.mana)):
			playable_cards.append(i)

	if playable_cards.is_empty():
		return

	var card_index = playable_cards.pick_random()

	if is_3d:
		# Play at a random position in 3D (enemy territory on right side)
		var spawn_x = randf_range(5, 9)    # Right side in 3D space
		var spawn_z = randf_range(-5, 5)   # Random depth
		var spawn_pos = Vector3(spawn_x, 1, spawn_z)
		summoner.play_card_3d(card_index, spawn_pos)
	else:
		# Play at a random position in 2D (right side of screen)
		var spawn_x = randf_range(1400, 1700)
		var spawn_y = randf_range(200, 880)
		var spawn_pos = Vector2(spawn_x, spawn_y)
		summoner.play_card(card_index, spawn_pos)

## Reset the timer for next card play
func _set_next_play_time() -> void:
	play_timer = 0.0
	next_play_time = randf_range(play_interval_min, play_interval_max)
