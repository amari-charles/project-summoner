extends Node
class_name BattleDialogueController

## BattleDialogueController - General-purpose dialogue system for battles
##
## Reads dialogue configuration from battle config and triggers dialogues
## based on battle events. Automatically pauses/resumes gameplay.
##
## Supports ANY battle to have story dialogues, not just tutorials.

## Reference to game controller (for pausing)
@onready var game_controller: GameController3D = get_parent()

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


func _ready() -> void:
	# Wait for game controller to be ready
	await get_tree().process_frame

	# Load dialogue configuration from battle
	_load_dialogue_config()

	# DEBUG: Skip tutorial intro events if flag is set
	if debug_skip_tutorial_intro:
		print("DEBUG: Skipping tutorial intro events")
		triggered_dialogues["battle_start_first_trial_intro"] = true
		triggered_dialogues["after_dialogue_first_trial_summon_prompt"] = true

	# Connect to game events if we have dialogues
	if dialogue_config.size() > 0:
		_connect_battle_events()


## Load dialogue configuration from BattleContext
func _load_dialogue_config() -> void:
	if not BattleContext.battle_config.has("dialogues"):
		return

	dialogue_config = BattleContext.battle_config.get("dialogues", [])
	print("BattleDialogueController: Loaded %d dialogue triggers" % dialogue_config.size())


## Connect to relevant battle signals
func _connect_battle_events() -> void:
	# Connect to game start
	if game_controller.has_signal("game_started"):
		game_controller.game_started.connect(_on_battle_started)

	# Connect to dialogue manager
	if DialogueManager:
		DialogueManager.dialogue_ended.connect(_on_dialogue_ended)


## Check for enemy proximity (for "base_damaged_first" trigger)
func _process(_delta: float) -> void:
	# Only check if we haven't already triggered and we have dialogues configured
	if checked_enemy_proximity or dialogue_config.size() == 0:
		return

	# Get player base
	var player_base: Node3D = get_tree().get_first_node_in_group("player_base") as Node3D
	if not player_base:
		return

	var base_pos: Vector3 = player_base.global_position

	# Check all units to see if any enemy units are close to player base
	var units: Array = get_tree().get_nodes_in_group("units")
	const PROXIMITY_DISTANCE: float = 15.0  # Trigger when enemy is within 15 units

	for unit: Node in units:
		# Skip if not an enemy unit
		if not unit.has_method("get") or unit.get("team") != 1:  # Team 1 = enemy
			continue

		# Get unit position - units are Node3D
		if not unit is Node3D:
			continue

		var unit_pos: Vector3 = (unit as Node3D).global_position

		var distance: float = base_pos.distance_to(unit_pos)
		if distance <= PROXIMITY_DISTANCE:
			# Enemy is close! Trigger dialogue
			print("BattleDialogueController: Enemy unit within %f units of player base - triggering proximity dialogue" % distance)
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
	print("BattleDialogueController: Dialogue ended: %s" % last_dialogue_id)

	# Unfreeze game BEFORE triggering after_dialogue events
	# This ensures any spawned units initialize in normal game state
	game_controller.unfreeze_game()

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
	print("BattleDialogueController: Checking after_dialogue triggers for: %s" % previous_dialogue_id)
	for config: Dictionary in dialogue_config:
		var trigger: String = config.get("trigger", "")
		var previous: String = config.get("previous", "")

		# Skip if not an "after_dialogue" trigger
		if trigger != "after_dialogue":
			continue

		# Skip if wrong previous dialogue
		if previous != previous_dialogue_id:
			continue

		print("BattleDialogueController: Found after_dialogue trigger - action: %s, dialogue: %s" % [config.get("action", "none"), config.get("dialogue_id", "none")])

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
	game_controller.freeze_game()
	dialogue_active = true

	# Start dialogue
	if DialogueManager:
		DialogueManager.start_dialogue(dialogue_id)


## Execute a special action (like spawning enemy)
func _execute_action(action: String) -> void:
	print("BattleDialogueController: Executing action: %s" % action)
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
	print("BattleDialogueController: _spawn_tutorial_enemy() called")

	# Create the slime card from CardCatalog (same as enemy deck loader)
	var card_id: String = "slime_green"
	print("BattleDialogueController: Creating card from CardCatalog: %s" % card_id)

	var card: Card = CardCatalog.create_card_resource(card_id)

	if not card:
		push_error("BattleDialogueController: Failed to create slime card from CardCatalog (id: %s)" % card_id)
		return

	print("BattleDialogueController: Card created successfully")

	# Get battlefield and spawn position
	# Just right of center field (2.5 units from center on enemy side)
	var spawn_pos_3d: Vector3 = Vector3(2.5, 1, 0.0)
	print("BattleDialogueController: Spawn position: %s" % spawn_pos_3d)

	var battlefield: Node = get_tree().get_first_node_in_group("battlefield")
	if not battlefield:
		push_error("BattleDialogueController: Battlefield not found")
		return

	print("BattleDialogueController: Battlefield found: %s" % battlefield.name)

	# Get ModifierSystem
	var modifier_system: Node = get_node_or_null("/root/ModifierSystem")
	print("BattleDialogueController: ModifierSystem: %s" % ("found" if modifier_system else "null"))

	# Spawn the unit directly (team 1 = enemy)
	print("BattleDialogueController: Checking if card has play_3d method...")
	if card.has_method("play_3d"):
		print("BattleDialogueController: Calling card.play_3d()...")
		card.call("play_3d", spawn_pos_3d, 1, battlefield, modifier_system)
		print("BattleDialogueController: Spawned tutorial slime at %s" % spawn_pos_3d)
	else:
		push_error("BattleDialogueController: Card doesn't have play_3d method - card type is %s" % card.get_class())


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
	print("BattleDialogueController: Showing hand (disabled)")
	var hand_ui: Node = _find_hand_ui()
	if hand_ui and hand_ui is CanvasItem:
		var canvas_item: CanvasItem = hand_ui as CanvasItem
		canvas_item.visible = true
		# Disable interaction by setting process_mode to DISABLED
		canvas_item.process_mode = Node.PROCESS_MODE_DISABLED
		print("BattleDialogueController: Hand UI shown and disabled")
	else:
		push_warning("BattleDialogueController: Could not find HandUI")


## Action: Enable hand UI for interaction
func _action_enable_hand() -> void:
	print("BattleDialogueController: Enabling hand interaction")
	var hand_ui: Node = _find_hand_ui()
	if hand_ui and hand_ui is CanvasItem:
		var canvas_item: CanvasItem = hand_ui as CanvasItem
		canvas_item.visible = true
		# Enable interaction by setting process_mode back to INHERIT
		canvas_item.process_mode = Node.PROCESS_MODE_INHERIT
		print("BattleDialogueController: Hand UI enabled")
	else:
		push_warning("BattleDialogueController: Could not find HandUI")


## Find the HandUI node in the scene tree
func _find_hand_ui() -> Node:
	# HandUI should be a child of the Battle3D scene or accessible via get_node
	return get_node_or_null("/root/Battle3D/HandUI")
