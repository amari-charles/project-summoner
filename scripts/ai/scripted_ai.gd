extends AIController
class_name ScriptedAI

## Scripted AI for tutorial battles
## Executes a predetermined sequence of spawns at specific times/positions
## Does not play cards autonomously - only follows the script

## Script configuration
var spawn_script: Array = []  # Array of {delay: float, card_name: String, position: Vector2}
var script_index: int = 0
var time_since_start: float = 0.0
var is_active: bool = true

func _ready() -> void:
	if summoner == null:
		var parent: Node = get_parent()
		if parent is Node3D and parent.is_in_group("summoners"):
			summoner = parent

func _process(delta: float) -> void:
	if not is_active:
		return

	if script_index >= spawn_script.size():
		return

	time_since_start += delta

	# Check if it's time to execute next spawn
	while script_index < spawn_script.size():
		var spawn_event_variant: Variant = spawn_script[script_index]
		if not spawn_event_variant is Dictionary:
			push_error("ScriptedAI: spawn_script[%d] is not a Dictionary" % script_index)
			script_index += 1
			continue
		var spawn_event: Dictionary = spawn_event_variant
		var trigger_time: float = spawn_event.get("delay", 0.0)

		if time_since_start >= trigger_time:
			_execute_spawn_event(spawn_event)
			script_index += 1
		else:
			break

func on_battle_start() -> void:
	time_since_start = 0.0
	script_index = 0
	is_active = true

## Load spawn script from battle config
func load_script(script_data: Array) -> void:
	spawn_script = script_data.duplicate(true)
	script_index = 0
	time_since_start = 0.0

## Execute a single spawn event from the script
func _execute_spawn_event(event: Dictionary) -> void:
	var card_name: String = event.get("card_name", "")
	var spawn_pos: Vector2 = event.get("position", Vector2.ZERO)

	# Find the card in hand by name
	var card_index: int = _find_card_by_name(card_name)
	if card_index == -1:
		push_warning("ScriptedAI: Card '%s' not found in hand" % card_name)
		return

	var hand: Array = summoner.Hand
	if card_index >= hand.size():
		return

	var card: Card = hand[card_index]

	# Check if we can afford it
	var mana_int: int = int(summoner.Mana)
	if not card.CanPlay(mana_int):
		push_warning("ScriptedAI: Not enough mana to play '%s'" % card_name)
		return

	# Play the card in 3D via SimulationNode
	var pos_3d: Vector3 = BattlefieldConstants.screen_to_world_3d(spawn_pos)
	var sim: Node = get_tree().get_first_node_in_group("simulation_node")
	if sim:
		sim.QueuePlayCard(summoner.Team, card_index, pos_3d, -1)

## Find card in hand by catalog ID or card name
func _find_card_by_name(card_name: String) -> int:
	for i: int in range(summoner.Hand.size()):
		var card: Card = summoner.Hand[i]
		# Match by card_name or catalog_id
		if card.CardName.to_lower() == card_name.to_lower():
			return i
	return -1

## Override base methods (scripted AI doesn't use these)
func should_play_card() -> bool:
	return false  # Scripted AI doesn't auto-play, only follows script

func select_card_to_play() -> int:
	return -1

func select_spawn_position(_card: Card) -> Vector2:
	return Vector2.ZERO  # Not used by scripted AI, positions come from script
