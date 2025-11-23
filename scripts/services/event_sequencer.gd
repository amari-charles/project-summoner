extends Node

## EventSequencer - Executes scripted EventSequences step-by-step
##
## Handles async execution of event sequences with proper capability management.
## Systems can queue sequences to play and check if a sequence is currently running.
##
## Example usage:
##   EventSequencer.play_sequence(my_sequence)
##   await EventSequencer.sequence_finished
##   print("Sequence complete!")

## Preload resource classes to ensure they're available
const EventSequenceClass: GDScript = preload("res://scripts/resources/event_sequence.gd")
const EventStepClass: GDScript = preload("res://scripts/resources/event_step.gd")

## Signals
signal sequence_started(sequence: Resource) #EventSequence
signal sequence_finished(sequence: Resource) #EventSequence
signal step_started(step: Resource, index: int) #EventStep
signal step_finished(step: Resource, index: int) #EventStep

## Current state
var current_sequence: Resource = null  # EventSequence
var current_step_index: int = -1
var is_playing: bool = false

## Debug mode
var debug_mode: bool = false

func _ready() -> void:
	# CRITICAL: EventSequencer must continue running even when game is paused
	# This allows it to control dialogue sequences and other scripted events
	process_mode = Node.PROCESS_MODE_ALWAYS

	if debug_mode:
		print("EventSequencer: Initialized")

## Play an event sequence
func play_sequence(sequence: Resource) -> void:  # EventSequence parameter
	if is_playing:
		push_warning("EventSequencer: Already playing a sequence, queuing is not yet implemented")
		return

	if not sequence:
		push_error("EventSequencer: Tried to play null sequence")
		return

	var seq_id: String = sequence.get("sequence_id") if sequence.get("sequence_id") is String else "unknown"
	var step_count_val: Variant = sequence.call("get_step_count")
	var step_count: int = step_count_val if step_count_val is int else 0

	if debug_mode:
		print("EventSequencer: Starting sequence '%s' with %d steps" % [seq_id, step_count])

	is_playing = true
	current_sequence = sequence
	current_step_index = -1

	sequence_started.emit(sequence)

	# Execute all steps sequentially
	for i: int in range(step_count):
		current_step_index = i
		var step_result: Variant = sequence.call("get_step", i)
		if not step_result is Resource:
			push_error("EventSequencer: Step %d is not a Resource" % i)
			continue
		var step: Resource = step_result if step_result is Resource else Resource.new()

		var step_desc: String = step.get("description") if step.get("description") is String else "no description"
		var step_type_val: Variant = step.get("step_type")
		var step_type: int = step_type_val if step_type_val is int else 0
		var step_type_name: String = EventStepClass.StepType.keys()[step_type] if EventStepClass else "unknown"

		if debug_mode:
			print("EventSequencer: Executing step %d/%d: %s (%s)" % [
				i + 1,
				step_count,
				step_desc,
				step_type_name
			])

		step_started.emit(step, i)
		await _execute_step(step)
		step_finished.emit(step, i)

	# Sequence complete
	if debug_mode:
		print("EventSequencer: Sequence '%s' complete" % seq_id)

	var finished_sequence: Resource = current_sequence  # EventSequence
	current_sequence = null
	current_step_index = -1
	is_playing = false

	sequence_finished.emit(finished_sequence)

## Execute a single step (async)
func _execute_step(step: Resource) -> void:  # EventStep parameter
	var step_type_variant: Variant = step.get("step_type")
	var step_type_val: int = step_type_variant if step_type_variant is int else 0

	if not EventStepClass:
		push_error("EventSequencer: EventStep class not loaded")
		return

	var step_type_enum: Variant = EventStepClass.StepType

	match step_type_val:
		step_type_enum.DIALOGUE:
			await _execute_dialogue(step)

		step_type_enum.WAIT_TIME:
			await _execute_wait_time(step)

		step_type_enum.WAIT_SIGNAL:
			await _execute_wait_signal(step)

		step_type_enum.SPAWN_UNIT:
			_execute_spawn_unit(step)

		step_type_enum.SET_CAPABILITY:
			_execute_set_capability(step)

		step_type_enum.EMIT_SIGNAL:
			_execute_emit_signal(step)

		step_type_enum.CUSTOM_FUNCTION:
			await _execute_custom_function(step)

		step_type_enum.MOVE_CAMERA:
			await _execute_move_camera(step)

		step_type_enum.PLAY_ANIMATION:
			_execute_play_animation(step)

		step_type_enum.FADE_SCREEN:
			await _execute_fade_screen(step)

		step_type_enum.SET_HAND_VISIBILITY:
			_execute_set_hand_visibility(step)

		step_type_enum.ENABLE_HAND:
			_execute_enable_hand(step)

		_:
			push_warning("EventSequencer: Unknown step type: %d" % step_type_val)

## =============================================================================
## STEP EXECUTORS
## =============================================================================

func _execute_dialogue(step: Resource) -> void:  # EventStep parameter
	var dialogue_id_val: Variant = step.get("dialogue_id")
	var dialogue_id: String = dialogue_id_val if dialogue_id_val is String else ""

	if debug_mode:
		print("EventSequencer: _execute_dialogue called with dialogue_id='%s'" % dialogue_id)

	if dialogue_id.is_empty():
		push_warning("EventSequencer: DIALOGUE step has empty dialogue_id")
		return

	# DialogueManager now owns capability blocking/unblocking during dialogue
	# See DialogueManager.start_dialogue() and dialogue_ended signal handlers

	# Try to find DialogueManager (could be autoload or scene node)
	var dialogue_manager: Node = get_node_or_null("/root/DialogueManager")
	if not dialogue_manager:
		dialogue_manager = get_tree().get_first_node_in_group("dialogue_manager")

	if debug_mode:
		print("EventSequencer: Found DialogueManager: %s" % (dialogue_manager != null))

	if dialogue_manager and dialogue_manager.has_method("start_dialogue"):
		# Wait for DialogueManager system to be ready (UI connected)
		if dialogue_manager.has_method("is_system_ready"):
			var is_ready: bool = dialogue_manager.call("is_system_ready")
			if not is_ready:
				if debug_mode:
					print("EventSequencer: Waiting for DialogueManager system to be ready...")
				if dialogue_manager.has_signal("system_ready"):
					await dialogue_manager.get("system_ready")
					if debug_mode:
						print("EventSequencer: DialogueManager ready!")
				else:
					push_warning("EventSequencer: DialogueManager not ready and no system_ready signal")

		# CRITICAL: Freeze game during dialogue
		var game_controller: Node = get_tree().get_first_node_in_group("game_controller")
		if game_controller and game_controller.has_method("freeze_game"):
			if debug_mode:
				print("EventSequencer: Freezing game for dialogue")
			game_controller.call("freeze_game")

		# Create a completion tracker before starting dialogue
		# Use a Dictionary to capture by reference (bools are captured by value in GDScript!)
		var dialogue_state: Dictionary = {"complete": false}
		var completion_callback: Callable = func() -> void:
			if debug_mode:
				print("EventSequencer: dialogue_ended callback fired! Setting complete flag")
			dialogue_state["complete"] = true
			if debug_mode:
				print("EventSequencer: complete flag is now: %s" % dialogue_state["complete"])

		# Connect to the signal
		if dialogue_manager.has_signal("dialogue_ended"):
			if debug_mode:
				print("EventSequencer: Connecting to dialogue_ended signal")
			dialogue_manager.connect("dialogue_ended", completion_callback, CONNECT_ONE_SHOT)
		else:
			push_error("EventSequencer: DialogueManager doesn't have 'dialogue_ended' signal")
			return

		if debug_mode:
			print("EventSequencer: Starting dialogue '%s'" % dialogue_id)

		# Start the dialogue
		dialogue_manager.call("start_dialogue", dialogue_id)

		if debug_mode:
			print("EventSequencer: Waiting for dialogue to complete...")

		# Wait for completion
		while not dialogue_state["complete"]:
			await get_tree().process_frame

		if debug_mode:
			print("EventSequencer: Dialogue complete!")

		# CRITICAL: Unfreeze game after dialogue
		if game_controller and game_controller.has_method("unfreeze_game"):
			if debug_mode:
				print("EventSequencer: Unfreezing game after dialogue")
			game_controller.call("unfreeze_game")
	else:
		push_error("EventSequencer: DialogueManager not found or doesn't have start_dialogue method")

func _execute_wait_time(step: Resource) -> void:  # EventStep parameter
	var wait_duration_val: Variant = step.get("wait_duration")
	var wait_duration: float = wait_duration_val if wait_duration_val is float else 0.0
	await get_tree().create_timer(wait_duration).timeout

func _execute_wait_signal(step: Resource) -> void:  # EventStep parameter
	var signal_source_val: Variant = step.get("signal_source")
	var signal_name_val: Variant = step.get("signal_name")
	var signal_source: String = signal_source_val if signal_source_val is String else ""
	var signal_name: String = signal_name_val if signal_name_val is String else ""

	if debug_mode:
		print("EventSequencer: _execute_wait_signal - source: %s, signal: %s" % [signal_source, signal_name])

	if signal_source.is_empty() or signal_name.is_empty():
		push_warning("EventSequencer: WAIT_SIGNAL step has empty source or signal name")
		return

	# Try to get signal source (autoload or node path)
	var source: Node = get_node_or_null("/root/" + signal_source)
	if not source:
		source = get_node_or_null(signal_source)

	if not source:
		push_error("EventSequencer: Signal source not found: %s" % signal_source)
		return

	if not source.has_signal(signal_name):
		push_error("EventSequencer: Source '%s' doesn't have signal '%s'" % [signal_source, signal_name])
		return

	if debug_mode:
		print("EventSequencer: Waiting for signal '%s' from '%s'..." % [signal_name, signal_source])

	# Wait for signal
	await source.get(signal_name)

	if debug_mode:
		print("EventSequencer: Signal '%s' received!" % signal_name)

func _execute_spawn_unit(step: Resource) -> void:  # EventStep parameter
	var card_id_val: Variant = step.get("card_id")
	var card_id: String = card_id_val if card_id_val is String else ""
	if card_id.is_empty():
		push_warning("EventSequencer: SPAWN_UNIT step has empty card_id")
		return

	# Extract step properties
	var spawn_position_val: Variant = step.get("spawn_position")
	var team_val: Variant = step.get("team")
	var spawn_position: Vector3 = spawn_position_val if spawn_position_val is Vector3 else Vector3.ZERO
	var team: int = team_val if team_val is int else 0

	# CRITICAL: Unfreeze game before spawning
	# Units need a running game state to initialize properly
	var game_controller: Node = get_tree().get_first_node_in_group("game_controller")
	if game_controller and game_controller.has_method("unfreeze_game"):
		if debug_mode:
			print("EventSequencer: Unfreezing game for unit spawn")
		game_controller.call("unfreeze_game")

	# Create card from CardCatalog
	var card: Card = CardCatalog.create_card_resource(card_id)
	if not card:
		push_error("EventSequencer: Failed to create card: %s" % card_id)
		return

	# Get battlefield
	var battlefield: Node = get_tree().get_first_node_in_group("battlefield")
	if not battlefield:
		push_error("EventSequencer: Battlefield not found")
		return

	# Get ModifierSystem
	var modifier_system: Node = get_node_or_null("/root/ModifierSystem")

	if debug_mode:
		print("EventSequencer: Spawning %s at %s for team %d" % [card_id, spawn_position, team])

	# Spawn unit
	if card.has_method("play_3d"):
		card.call("play_3d", spawn_position, team, battlefield, modifier_system)
		if debug_mode:
			print("EventSequencer: Unit spawned successfully")

		# Apply stat overrides if specified
		var stat_overrides_val: Variant = step.get("stat_overrides")
		var stat_overrides: Dictionary = stat_overrides_val if stat_overrides_val is Dictionary else {}
		if not stat_overrides.is_empty():
			# Find the unit we just spawned (most recently added unit at spawn_position)
			await get_tree().process_frame  # Wait for unit to be added to scene tree

			const SPAWN_POSITION_TOLERANCE: float = 0.1
			const ALLOWED_STAT_OVERRIDES: Array[String] = [
				"move_speed", "attack_damage", "max_hp", "attack_range",
				"attack_speed", "aggro_radius", "attack_range_depth", "attack_range_vertical",
				"scale_multiplier"  # Special: multiplies the unit's scale
			]

			var units: Array[Node] = get_tree().get_nodes_in_group("units")
			for unit_node: Node in units:
				# Type check and narrow to Node3D for safe position access
				if unit_node is Node3D:
					var unit_3d: Node3D = unit_node as Node3D
					if unit_3d.global_position.distance_to(spawn_position) < SPAWN_POSITION_TOLERANCE:
						# Apply each stat override (with whitelist validation)
						for stat_key: String in stat_overrides.keys():
							if stat_key not in ALLOWED_STAT_OVERRIDES:
								push_warning("EventSequencer: Stat override '%s' not in whitelist, skipping" % stat_key)
								continue

							var stat_value: Variant = stat_overrides[stat_key]

							# Special handling for scale_multiplier
							if stat_key == "scale_multiplier":
								var multiplier: float = stat_value if stat_value is float else 1.0
								unit_3d.scale = Vector3.ONE * multiplier
								if debug_mode:
									print("EventSequencer: Applied scale multiplier %f" % multiplier)
							# Special handling for max_hp - also update current_hp
							elif stat_key == "max_hp":
								if unit_node.has_method("set"):
									var hp_value: float = stat_value if stat_value is float else 100.0
									unit_node.set("max_hp", hp_value)
									unit_node.set("current_hp", hp_value)  # Start at full HP
									if debug_mode:
										print("EventSequencer: Applied max_hp override %f (and set current_hp)" % hp_value)
								else:
									push_warning("EventSequencer: Unit doesn't support setting max_hp")
							elif unit_node.has_method("set"):
								unit_node.set(stat_key, stat_value)
								if debug_mode:
									print("EventSequencer: Applied stat override %s = %s" % [stat_key, stat_value])
							else:
								push_warning("EventSequencer: Unit doesn't support setting property: %s" % stat_key)
						break
	else:
		push_error("EventSequencer: Card doesn't have play_3d method")

func _execute_set_capability(step: Resource) -> void:  # EventStep parameter
	var enable_val: Variant = step.get("enable")
	var capability_val: Variant = step.get("capability")
	var block_reason_val: Variant = step.get("block_reason")
	var enable: bool = enable_val if enable_val is bool else false
	var capability: int = capability_val if capability_val is int else 0
	var block_reason: int = block_reason_val if block_reason_val is int else 0

	if enable:
		CapabilityManager.unblock_capability(capability, block_reason)
	else:
		CapabilityManager.block_capability(capability, block_reason)

func _execute_emit_signal(step: Resource) -> void:  # EventStep parameter
	var event_hub_val: Variant = step.get("event_hub")
	var emit_signal_name_val: Variant = step.get("emit_signal_name")
	var signal_args_val: Variant = step.get("signal_args")
	var event_hub: String = event_hub_val if event_hub_val is String else ""
	var emit_signal_name: String = emit_signal_name_val if emit_signal_name_val is String else ""
	var signal_args: Array = signal_args_val if signal_args_val is Array else []

	if event_hub.is_empty() or emit_signal_name.is_empty():
		push_warning("EventSequencer: EMIT_SIGNAL step has empty hub or signal name")
		return

	# Get event hub
	var hub: Node = get_node_or_null("/root/" + event_hub)
	if not hub:
		push_error("EventSequencer: Event hub not found: %s" % event_hub)
		return

	if not hub.has_signal(emit_signal_name):
		push_error("EventSequencer: Hub '%s' doesn't have signal '%s'" % [event_hub, emit_signal_name])
		return

	# Emit signal with args
	var signal_to_emit: Signal = hub.get(emit_signal_name)
	match signal_args.size():
		0:
			signal_to_emit.emit()
		1:
			signal_to_emit.emit(signal_args[0])
		2:
			signal_to_emit.emit(signal_args[0], signal_args[1])
		3:
			signal_to_emit.emit(signal_args[0], signal_args[1], signal_args[2])
		_:
			push_warning("EventSequencer: EMIT_SIGNAL with >3 args not yet supported")

func _execute_custom_function(step: Resource) -> void:  # EventStep parameter
	var target_node_path_val: Variant = step.get("target_node_path")
	var function_name_val: Variant = step.get("function_name")
	var function_args_val: Variant = step.get("function_args")
	var target_node_path: String = target_node_path_val if target_node_path_val is String else ""
	var function_name: String = function_name_val if function_name_val is String else ""
	var function_args: Array = function_args_val if function_args_val is Array else []

	if target_node_path.is_empty() or function_name.is_empty():
		push_warning("EventSequencer: CUSTOM_FUNCTION step has empty node path or function name")
		return

	# Get target node
	var target: Node = get_node_or_null(target_node_path)
	if not target:
		push_error("EventSequencer: Target node not found: %s" % target_node_path)
		return

	if not target.has_method(function_name):
		push_error("EventSequencer: Node '%s' doesn't have method '%s'" % [target_node_path, function_name])
		return

	# Call function with args
	var result: Variant
	match function_args.size():
		0:
			result = target.call(function_name)
		1:
			result = target.call(function_name, function_args[0])
		2:
			result = target.call(function_name, function_args[0], function_args[1])
		3:
			result = target.call(function_name, function_args[0], function_args[1], function_args[2])
		_:
			push_warning("EventSequencer: CUSTOM_FUNCTION with >3 args not yet supported")
			return

	# If function returned a coroutine, wait for it
	if result is Callable:
		await result

func _execute_move_camera(step: Resource) -> void:  # EventStep parameter
	var camera_duration_val: Variant = step.get("camera_duration")
	var camera_duration: float = camera_duration_val if camera_duration_val is float else 0.0

	# TODO: Implement camera movement
	push_warning("EventSequencer: MOVE_CAMERA not yet implemented")
	await get_tree().create_timer(camera_duration).timeout

func _execute_play_animation(_step: Resource) -> void:  # EventStep parameter
	# TODO: Implement animation playback
	push_warning("EventSequencer: PLAY_ANIMATION not yet implemented")

func _execute_fade_screen(step: Resource) -> void:  # EventStep parameter
	var fade_duration_val: Variant = step.get("fade_duration")
	var fade_duration: float = fade_duration_val if fade_duration_val is float else 0.0

	# TODO: Implement screen fade
	push_warning("EventSequencer: FADE_SCREEN not yet implemented")
	await get_tree().create_timer(fade_duration).timeout

func _execute_set_hand_visibility(step: Resource) -> void:  # EventStep parameter
	var hand_visible_val: Variant = step.get("hand_visible")
	var hand_visible: bool = hand_visible_val if hand_visible_val is bool else false

	var hand_ui: Node = _find_hand_ui()
	if hand_ui and hand_ui is CanvasItem:
		(hand_ui as CanvasItem).visible = hand_visible
	else:
		push_warning("EventSequencer: Could not find HandUI")

func _execute_enable_hand(step: Resource) -> void:  # EventStep parameter
	var hand_enabled_val: Variant = step.get("hand_enabled")
	var hand_enabled: bool = hand_enabled_val if hand_enabled_val is bool else false

	var hand_ui: Node = _find_hand_ui()
	if hand_ui and hand_ui is CanvasItem:
		var canvas_item: CanvasItem = hand_ui as CanvasItem
		canvas_item.visible = true
		canvas_item.process_mode = Node.PROCESS_MODE_INHERIT if hand_enabled else Node.PROCESS_MODE_DISABLED
	else:
		push_warning("EventSequencer: Could not find HandUI")

## =============================================================================
## HELPERS
## =============================================================================

func _find_hand_ui() -> Node:
	# Try to find HandUI in current scene
	return get_tree().get_first_node_in_group("hand_ui")

## Stop current sequence (emergency stop)
func stop_sequence() -> void:
	if not is_playing:
		return

	var stopped_sequence: Resource = current_sequence
	var sequence_id_val: Variant = stopped_sequence.get("sequence_id") if stopped_sequence else ""
	var sequence_id: String = sequence_id_val if sequence_id_val is String else "unknown"

	if debug_mode:
		print("EventSequencer: Stopping sequence '%s' (emergency stop)" % sequence_id)

	current_sequence = null
	current_step_index = -1
	is_playing = false

	sequence_finished.emit(stopped_sequence)
