extends Node
class_name AIController

## Abstract base class for all AI implementations
## Subclasses implement specific AI strategies (heuristic, scripted, RL, etc.)

## Reference to the summoner this AI controls
var summoner: Summoner

## Called when the battle starts
func on_battle_start() -> void:
	pass

## Called when the AI should make a decision
## Returns true if the AI wants to play a card
func should_play_card() -> bool:
	return false

## Select which card from hand to play
## Returns the index of the card to play, or -1 if no card should be played
func select_card_to_play() -> int:
	return -1

## Select where to spawn the card
## Returns the world position to spawn at
func select_spawn_position(card: Card) -> Vector2:
	return Vector2.ZERO

## Helper: Get battlefield dimensions
func get_battlefield_bounds() -> Rect2:
	# Default battlefield bounds (can be overridden)
	return Rect2(0, 0, 1920, 1080)

## Helper: Count friendly units
func count_friendly_units() -> int:
	var group_name = "enemy_units" if summoner.team == Unit.Team.ENEMY else "player_units"
	return get_tree().get_nodes_in_group(group_name).size()

## Helper: Count enemy units
func count_enemy_units() -> int:
	var group_name = "player_units" if summoner.team == Unit.Team.ENEMY else "enemy_units"
	return get_tree().get_nodes_in_group(group_name).size()

## Helper: Get our base HP ratio (0-1)
func get_our_base_hp_ratio() -> float:
	var base_group = "enemy_bases" if summoner.team == Unit.Team.ENEMY else "player_bases"
	var bases = get_tree().get_nodes_in_group(base_group)
	if bases.size() > 0:
		var base = bases[0]
		return base.current_hp / base.max_hp
	return 1.0

## Helper: Get enemy base HP ratio (0-1)
func get_enemy_base_hp_ratio() -> float:
	var base_group = "player_bases" if summoner.team == Unit.Team.ENEMY else "enemy_bases"
	var bases = get_tree().get_nodes_in_group(base_group)
	if bases.size() > 0:
		var base = bases[0]
		return base.current_hp / base.max_hp
	return 1.0
