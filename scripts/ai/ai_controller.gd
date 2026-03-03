extends Node
class_name AIController

## Abstract base class for all AI implementations
## Subclasses implement specific AI strategies (heuristic, scripted, RL, etc.)

## Reference to the summoner this AI controls (SummonerVisual)
var summoner: Node3D

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

## Default battlefield bounds used when camera bounds unavailable (fallback for edge cases)
## Matches typical battlefield layout: X from -50 to +50, Z from -40 to +40
const DEFAULT_BATTLEFIELD_BOUNDS: Rect2 = Rect2(-50.0, -40.0, 100.0, 80.0)

## Get battlefield bounds in 3D world space (XZ plane)
## Returns Rect2 where position = min corner (e.g., -50, -40), size = dimensions (e.g., 100, 80)
## This represents the actual playable battlefield area in world coordinates
func get_battlefield_bounds_3d() -> Rect2:
	var viewport: Viewport = get_viewport()
	if not viewport:
		push_warning("AIController: No viewport available - using default battlefield bounds")
		return DEFAULT_BATTLEFIELD_BOUNDS

	var camera: Camera3D = viewport.get_camera_3d()
	if not camera or camera.get("map_rect_xz") == null:
		push_warning("AIController: No camera bounds available - using default battlefield bounds")
		return DEFAULT_BATTLEFIELD_BOUNDS

	return camera.map_rect_xz

## Helper: Count friendly units
func count_friendly_units() -> int:
	var summoner_team: int = summoner.Team
	var group_name: StringName = GroupIDs.ally_units_for(summoner_team)
	return get_tree().get_nodes_in_group(group_name).size()

## Helper: Count enemy units
func count_enemy_units() -> int:
	var summoner_team: int = summoner.Team
	var group_name: StringName = GroupIDs.enemy_units_for(summoner_team)
	return get_tree().get_nodes_in_group(group_name).size()

## Helper: Get our base HP ratio (0-1)
func get_our_base_hp_ratio() -> float:
	var summoner_team: int = summoner.Team
	var base_group: StringName = GroupIDs.ally_bases_for(summoner_team)
	var bases: Array[Node] = get_tree().get_nodes_in_group(base_group)
	if bases.size() > 0:
		var base: Node = bases[0]
		var current_hp: float = base.get("CurrentHp")
		var max_hp: float = base.get("MaxHp")
		if max_hp > 0:
			return current_hp / max_hp
	return 1.0

## Helper: Get enemy base HP ratio (0-1)
func get_enemy_base_hp_ratio() -> float:
	var summoner_team: int = summoner.Team
	var base_group: StringName = GroupIDs.enemy_bases_for(summoner_team)
	var bases: Array[Node] = get_tree().get_nodes_in_group(base_group)
	if bases.size() > 0:
		var base: Node = bases[0]
		var current_hp: float = base.get("CurrentHp")
		var max_hp: float = base.get("MaxHp")
		if max_hp > 0:
			return current_hp / max_hp
	return 1.0
