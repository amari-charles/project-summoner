extends Node
class_name AILoader

## AILoader - Factory for creating AI controllers based on battle config
## Instantiates the appropriate AI type and configures it

## Create AI controller from battle configuration
static func create_ai_for_battle(battle_config: Dictionary, summoner: Node) -> AIController:
	var ai_type_variant: Variant = battle_config.get("ai_type", "heuristic")
	var ai_type: String = ai_type_variant if ai_type_variant is String else "heuristic"
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
	var ai: ScriptedAI = ScriptedAI.new()

	# Load spawn script
	var script_data_variant: Variant = battle_config.get("ai_script", [])
	var script_data: Array = script_data_variant if script_data_variant is Array else []
	if script_data.size() > 0:
		# Convert position dictionaries to Vector2
		var converted_script: Array = []
		for event_variant: Variant in script_data:
			var event: Dictionary = event_variant if event_variant is Dictionary else {}
			var converted_event: Dictionary = event.duplicate()
			if event.has("position") and event["position"] is Dictionary:
				var pos_dict_variant: Variant = event["position"]
				var pos_dict: Dictionary = pos_dict_variant if pos_dict_variant is Dictionary else {}
				var x_variant: Variant = pos_dict.get("x", 0)
				var y_variant: Variant = pos_dict.get("y", 0)
				var x: float = x_variant if x_variant is float else (x_variant if x_variant is int else 0.0)
				var y: float = y_variant if y_variant is float else (y_variant if y_variant is int else 0.0)
				converted_event["position"] = Vector2(x, y)
			converted_script.append(converted_event)

		ai.load_script(converted_script)

	return ai

## Create HeuristicAI from config
static func _create_heuristic_ai(battle_config: Dictionary) -> HeuristicAI:
	var ai: HeuristicAI = HeuristicAI.new()

	# Set personality
	var personality_str_variant: Variant = battle_config.get("ai_personality", "balanced")
	var personality_str: String = personality_str_variant if personality_str_variant is String else "balanced"
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
	var ai_config_variant: Variant = battle_config.get("ai_config", {})
	var ai_config: Dictionary = ai_config_variant if ai_config_variant is Dictionary else {}
	if ai_config.has("play_interval_min"):
		ai.play_interval_min = ai_config["play_interval_min"]
	if ai_config.has("play_interval_max"):
		ai.play_interval_max = ai_config["play_interval_max"]

	return ai
