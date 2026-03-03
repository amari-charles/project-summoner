extends Node
class_name SimpleAI

## Simple AI controller for enemy summoner
## Plays cards at random intervals with basic strategy

@export var summoner: Summoner
@export var play_interval_min: float = 2.0
@export var play_interval_max: float = 5.0

var play_timer: float = 0.0
var next_play_time: float = 0.0

func _ready() -> void:
	if summoner == null:
		var parent: Node = get_parent()
		if parent is Summoner:
			summoner = parent

	_set_next_play_time()

func _process(delta: float) -> void:
	if summoner == null or not summoner.is_enabled:
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

## Get indices of cards that can be played with current mana
func _get_playable_card_indices() -> Array[int]:
	var playable: Array[int] = []
	if summoner.hand.is_empty():
		return playable

	var mana: int = int(summoner.mana)

	for i: int in range(summoner.hand.size()):
		var card: Card = summoner.hand[i]
		if card.CanPlay(mana):
			playable.append(i)

	return playable

## Play a card at the given index at a random position
func _play_card_at_index(card_index: int) -> void:
	# Play at a random position in 3D (enemy territory on right side)
	var spawn_x: float = randf_range(5.0, 9.0)
	var spawn_z: float = randf_range(-5.0, 5.0)
	var spawn_pos: Vector3 = Vector3(spawn_x, 1.0, spawn_z)
	summoner.play_card_3d(card_index, spawn_pos)

## Reset the timer for next card play
func _set_next_play_time() -> void:
	play_timer = 0.0
	next_play_time = randf_range(play_interval_min, play_interval_max)
