extends Node
class_name AILoader

## AILoader - Factory for creating AI controllers based on battle config
## Instantiates the appropriate AI type and configures it

## Create AI controller from battle configuration
static func create_ai_for_battle(battle_config: Dictionary, summoner: Node) -> AIController:
	var ai_type = battle_config.get("ai_type", "heuristic")
	var ai: AIController = null

	match ai_type:
		"scripted":
			ai = _create_scripted_ai(battle_config)
		"heuristic":
			ai = _create_heuristic_ai(battle_config)
		_:
			push_warning("AILoader: Unknown AI type '%s', defaulting to heuristic" % ai_type)
			ai = _create_heuristic_ai(battle_config)

	if ai:
		ai.summoner = summoner
		ai.name = "AI"

	return ai

## Create ScriptedAI from config
static func _create_scripted_ai(battle_config: Dictionary) -> ScriptedAI:
	var ai = ScriptedAI.new()

	# Load spawn script
	var script_data = battle_config.get("ai_script", [])
	if script_data.size() > 0:
		# Convert position dictionaries to Vector2
		var converted_script = []
		for event in script_data:
			var converted_event = event.duplicate()
			if event.has("position") and event["position"] is Dictionary:
				var pos_dict = event["position"]
				converted_event["position"] = Vector2(
					pos_dict.get("x", 0),
					pos_dict.get("y", 0)
				)
			converted_script.append(converted_event)

		ai.load_script(converted_script)

	return ai

## Create HeuristicAI from config
static func _create_heuristic_ai(battle_config: Dictionary) -> HeuristicAI:
	var ai = HeuristicAI.new()

	# Set personality
	var personality_str = battle_config.get("ai_personality", "balanced")
	match personality_str.to_lower():
		"aggressive":
			ai.personality = HeuristicAI.Personality.AGGRESSIVE
		"defensive":
			ai.personality = HeuristicAI.Personality.DEFENSIVE
		"balanced":
			ai.personality = HeuristicAI.Personality.BALANCED
		"spell_focused":
			ai.personality = HeuristicAI.Personality.SPELL_FOCUSED
		_:
			ai.personality = HeuristicAI.Personality.BALANCED

	# Set difficulty
	ai.difficulty = battle_config.get("ai_difficulty", 3)

	# Apply additional config
	var ai_config = battle_config.get("ai_config", {})
	if ai_config.has("play_interval_min"):
		ai.play_interval_min = ai_config["play_interval_min"]
	if ai_config.has("play_interval_max"):
		ai.play_interval_max = ai_config["play_interval_max"]

	return ai
