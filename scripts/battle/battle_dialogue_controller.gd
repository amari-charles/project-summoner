extends Node
class_name BattleDialogueController

## BattleDialogueController - General-purpose dialogue system for battles
##
## Reads dialogue configuration from battle config and triggers dialogues
## based on battle events. Automatically pauses/resumes gameplay.
##
## Supports ANY battle to have story dialogues, not just tutorials.

## Card used for the tutorial enemy spawn
const TUTORIAL_ENEMY_CARD_ID: String = "pebbloom"
const TUTORIAL_ENEMY_SPAWN_POS: Vector3 = Vector3(2.5, 1, 0.0)

## Reference to game controller (for pausing)
@onready var game_controller: Node = get_parent()

## Dialogue configuration from battle
var dialogue_config: Array = []

## DEBUG: Set to true to skip first two tutorial events (for testing proximity trigger)
@export var debug_skip_tutorial_intro: bool = false

## Track which dialogues have been triggered (for "first time" triggers)
var triggered_dialogues: Dictionary = {}

## Track if we're currently in a dialogue
var dialogue_active: bool = false

## Track the currently playing dialogue ID
var current_dialogue_id: String = ""

## Track if we've checked for nearby enemies (for "base_damaged_first" trigger)
var checked_enemy_proximity: bool = false

## Set to true to enable verbose logging
var debug_mode: bool = false


func _ready() -> void:
	# Wait for game controller to be ready
	await get_tree().process_frame

	# Load dialogue configuration from battle
	_load_dialogue_config()

	# DEBUG: Skip tutorial intro events if flag is set
	if debug_skip_tutorial_intro:
		if debug_mode: print("DEBUG: Skipping tutorial intro events")
		triggered_dialogues["battle_start_first_trial_intro"] = true
		triggered_dialogues["after_dialogue_first_trial_summon_prompt"] = true

	# Connect to game events if we have dialogues
	if dialogue_config.size() > 0:
		_connect_battle_events()


## Load dialogue configuration from BattleContext
func _load_dialogue_config() -> void:
	# Phase 3: Check for EventSequence first (new system)
	if BattleContext.battle_config.has("event_sequence"):
		var sequence_path: String = BattleContext.battle_config.get("event_sequence", "")
		if not sequence_path.is_empty():
			_load_and_play_event_sequence(sequence_path)
			return  # Use new system, skip old dialogue config

	# Fallback to old dialogue config system
	if not BattleContext.battle_config.has("dialogues"):
		return

	dialogue_config = BattleContext.battle_config.get("dialogues", [])
	if debug_mode: print("BattleDialogueController: Loaded %d dialogue triggers" % dialogue_config.size())


## Load and play an EventSequence (Phase 3: New System)
func _load_and_play_event_sequence(sequence_path: String) -> void:
	var sequence: Resource = load(sequence_path)
	if not sequence:
		push_error("BattleDialogueController: Failed to load EventSequence: %s" % sequence_path)
		return

	var seq_id: String = sequence.get("sequence_id") if sequence.get("sequence_id") else "unknown"
	if debug_mode: print("BattleDialogueController: Playing EventSequence '%s'" % seq_id)

	# Enable debug mode for tutorial sequences
	if BattleContext.battle_config.get("is_tutorial", false):
		EventSequencer.debug_mode = true

	# Play the sequence
	# EventSequencer will now wait for DialogueManager.system_ready signal
	EventSequencer.play_sequence(sequence)


## Connect to relevant battle signals
func _connect_battle_events() -> void:
	# Connect to game start
	if game_controller.has_signal("GameStarted"):
		game_controller.connect("GameStarted", _on_battle_started)

	# Connect to dialogue manager
	if DialogueManager:
		DialogueManager.dialogue_ended.connect(_on_dialogue_ended)


func _exit_tree() -> void:
	if game_controller and game_controller.has_signal("GameStarted") and game_controller.is_connected("GameStarted", _on_battle_started):
		game_controller.disconnect("GameStarted", _on_battle_started)
	if DialogueManager and DialogueManager.dialogue_ended.is_connected(_on_dialogue_ended):
		DialogueManager.dialogue_ended.disconnect(_on_dialogue_ended)


## Check for enemy proximity (for "base_damaged_first" trigger)
func _process(_delta: float) -> void:
	# Only check if we haven't already triggered and we have dialogues configured
	if checked_enemy_proximity or dialogue_config.size() == 0:
		return

	# Get player base
	var player_base: Node3D = get_tree().get_first_node_in_group(GroupIDs.PLAYER_BASES) as Node3D
	if not player_base:
		return

	var base_pos: Vector3 = player_base.global_position

	# Check enemy units to see if any are close to player base
	var enemy_group: StringName = GroupIDs.enemy_units_for(UnitConstants.Team.PLAYER)
	var units: Array = get_tree().get_nodes_in_group(enemy_group)
	const PROXIMITY_DISTANCE: float = 15.0  # Trigger when enemy is within 15 units

	for unit: Node in units:
		# Get unit position - units are Node3D
		if not unit is Node3D:
			continue

		var unit_pos: Vector3 = (unit as Node3D).global_position

		var distance: float = base_pos.distance_to(unit_pos)
		if distance <= PROXIMITY_DISTANCE:
			# Enemy is close! Trigger dialogue
			if debug_mode: print("BattleDialogueController: Enemy unit within %f units of player base - triggering proximity dialogue" % distance)
			checked_enemy_proximity = true
			_trigger_dialogues_by_type("base_damaged_first")
			return


## Battle started - check for "battle_start" triggers
func _on_battle_started() -> void:
	_trigger_dialogues_by_type("battle_start")


## Dialogue ended - check for "after_dialogue" triggers and resume game
func _on_dialogue_ended() -> void:
	dialogue_active = false

	# Find the dialogue that just ended
	var last_dialogue_id: String = _get_last_played_dialogue_id()
	if debug_mode: print("BattleDialogueController: Dialogue ended: %s" % last_dialogue_id)

	# Unfreeze game BEFORE triggering after_dialogue events
	# This ensures any spawned units initialize in normal game state
	game_controller.UnfreezeGame()

	# Check for after_dialogue triggers (may freeze again if showing new dialogue)
	_trigger_after_dialogue(last_dialogue_id)


## Trigger all dialogues of a specific type
func _trigger_dialogues_by_type(trigger_type: String) -> void:
	for config: Dictionary in dialogue_config:
		var trigger: String = config.get("trigger", "")

		# Skip if wrong trigger type
		if trigger != trigger_type:
			continue

		# Skip if already triggered (for "first time" triggers)
		if _is_first_time_trigger(trigger) and _was_triggered(config):
			continue

		# Trigger this dialogue
		_show_dialogue(config)


## Trigger dialogues that happen after a specific dialogue
func _trigger_after_dialogue(previous_dialogue_id: String) -> void:
	if debug_mode: print("BattleDialogueController: Checking after_dialogue triggers for: %s" % previous_dialogue_id)
	for config: Dictionary in dialogue_config:
		var trigger: String = config.get("trigger", "")
		var previous: String = config.get("previous", "")

		# Skip if not an "after_dialogue" trigger
		if trigger != "after_dialogue":
			continue

		# Skip if wrong previous dialogue
		if previous != previous_dialogue_id:
			continue

		if debug_mode: print("BattleDialogueController: Found after_dialogue trigger - action: %s, dialogue: %s" % [config.get("action", "none"), config.get("dialogue_id", "none")])

		# Execute action if specified
		if config.has("action"):
			var action: String = config["action"]
			_execute_action(action)

		# Show dialogue if specified
		if config.has("dialogue_id"):
			_show_dialogue(config)


## Show a dialogue and pause the game
func _show_dialogue(config: Dictionary) -> void:
	var dialogue_id: String = config.get("dialogue_id", "")

	if dialogue_id.is_empty():
		return

	# Mark as triggered
	triggered_dialogues[_get_config_key(config)] = true

	# Track current dialogue
	current_dialogue_id = dialogue_id

	# Freeze game (stop gameplay without activating pause menu)
	game_controller.FreezeGame()
	dialogue_active = true

	# Start dialogue
	if DialogueManager:
		DialogueManager.start_dialogue(dialogue_id)


## Execute a special action (like spawning enemy)
func _execute_action(action: String) -> void:
	if debug_mode: print("BattleDialogueController: Executing action: %s" % action)
	match action:
		"spawn_enemy":
			_spawn_tutorial_enemy()
		"show_hand":
			_action_show_hand()
		"enable_hand":
			_action_enable_hand()
		_:
			push_warning("BattleDialogueController: Unknown action: %s" % action)


## Spawn the tutorial enemy (for first_trial)
func _spawn_tutorial_enemy() -> void:
	if debug_mode: print("BattleDialogueController: _spawn_tutorial_enemy() called")

	var card_id: String = TUTORIAL_ENEMY_CARD_ID
	var spawn_pos_3d: Vector3 = TUTORIAL_ENEMY_SPAWN_POS
	if debug_mode: print("BattleDialogueController: Spawn position: %s" % spawn_pos_3d)

	# Spawn via sim command queue (authoritative path).
	var sim_node: Node = get_tree().get_first_node_in_group(GroupIDs.SIMULATION_NODE)
	if not sim_node:
		push_error("BattleDialogueController: SimulationNode not found")
		return

	if not sim_node.has_method("QueueSpawnUnit"):
		push_error("BattleDialogueController: SimulationNode missing QueueSpawnUnit()")
		return

	if debug_mode:
		print("BattleDialogueController: Queueing tutorial enemy spawn via SimulationNode")
	sim_node.call("QueueSpawnUnit", card_id, int(UnitConstants.Team.ENEMY), spawn_pos_3d, true, null)


## Check if a trigger should only happen once
func _is_first_time_trigger(trigger: String) -> bool:
	return trigger in ["battle_start", "base_damaged_first"]


## Check if a dialogue config was already triggered
func _was_triggered(config: Dictionary) -> bool:
	var key: String = _get_config_key(config)
	return triggered_dialogues.get(key, false)


## Get unique key for a dialogue config
func _get_config_key(config: Dictionary) -> String:
	var trigger: String = config.get("trigger", "")
	var dialogue_id: String = config.get("dialogue_id", "")
	return "%s_%s" % [trigger, dialogue_id]


## Get the ID of the last played dialogue
func _get_last_played_dialogue_id() -> String:
	return current_dialogue_id


## Action: Show hand UI but keep it disabled
func _action_show_hand() -> void:
	if debug_mode: print("BattleDialogueController: Showing hand (disabled)")
	var hand_ui: Node = _find_hand_ui()
	if hand_ui and hand_ui is CanvasItem:
		var canvas_item: CanvasItem = hand_ui as CanvasItem
		canvas_item.visible = true
		# Disable interaction by setting process_mode to DISABLED
		canvas_item.process_mode = Node.PROCESS_MODE_DISABLED
		if debug_mode: print("BattleDialogueController: Hand UI shown and disabled")
	else:
		push_warning("BattleDialogueController: Could not find HandUI")


## Action: Enable hand UI for interaction
func _action_enable_hand() -> void:
	if debug_mode: print("BattleDialogueController: Enabling hand interaction")
	var hand_ui: Node = _find_hand_ui()
	if hand_ui and hand_ui is CanvasItem:
		var canvas_item: CanvasItem = hand_ui as CanvasItem
		canvas_item.visible = true
		# Enable interaction by setting process_mode back to INHERIT
		canvas_item.process_mode = Node.PROCESS_MODE_INHERIT
		if debug_mode: print("BattleDialogueController: Hand UI enabled")
	else:
		push_warning("BattleDialogueController: Could not find HandUI")


## Find the HandUI node in the scene tree
func _find_hand_ui() -> Node:
	return get_tree().get_first_node_in_group(GroupIDs.HAND_UI)
