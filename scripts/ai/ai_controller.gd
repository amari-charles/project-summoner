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
func select_spawn_position(_card: Card) -> Vector2:
	return Vector2.ZERO

## Get battlefield bounds in 3D world space (XZ plane)
## Returns Rect2 where position = min corner (e.g., -50, -40), size = dimensions (e.g., 100, 80)
## This represents the actual playable battlefield area in world coordinates
func get_battlefield_bounds_3d() -> Rect2:
	var viewport: Viewport = get_viewport()
	assert(viewport != null, "AIController: No viewport available - AI must be in scene tree")

	var camera: Camera3D = viewport.get_camera_3d()
	assert(camera != null, "AIController: No Camera3D found in viewport")
	assert(camera.get("map_rect_xz") != null, "AIController: Camera missing map_rect_xz property - must use CameraController3D")

	return camera.map_rect_xz

## Helper: Count friendly units
func count_friendly_units() -> int:
	var summoner_team: int = summoner.team
	var group_name: StringName = GroupIDs.ally_units_for(summoner_team)
	return get_tree().get_nodes_in_group(group_name).size()

## Helper: Count enemy units
func count_enemy_units() -> int:
	var summoner_team: int = summoner.team
	var group_name: StringName = GroupIDs.enemy_units_for(summoner_team)
	return get_tree().get_nodes_in_group(group_name).size()

## Helper: Get our base HP ratio (0-1)
func get_our_base_hp_ratio() -> float:
	var summoner_team: int = summoner.team
	var base_group: StringName = GroupIDs.ally_bases_for(summoner_team)
	var bases: Array[Node] = get_tree().get_nodes_in_group(base_group)
	if bases.size() > 0:
		var base: Node = bases[0]
		var current_hp: float = base.get("current_hp")
		var max_hp: float = base.get("max_hp")
		if max_hp > 0:
			return current_hp / max_hp
	return 1.0

## Helper: Get enemy base HP ratio (0-1)
func get_enemy_base_hp_ratio() -> float:
	var summoner_team: int = summoner.team
	var base_group: StringName = GroupIDs.enemy_bases_for(summoner_team)
	var bases: Array[Node] = get_tree().get_nodes_in_group(base_group)
	if bases.size() > 0:
		var base: Node = bases[0]
		var current_hp: float = base.get("current_hp")
		var max_hp: float = base.get("max_hp")
		if max_hp > 0:
			return current_hp / max_hp
	return 1.0
