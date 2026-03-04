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
const EventSequenceClass: GDScript = preload("res://scripts/infrastructure/data/event_sequence.gd")
const EventStepClass: GDScript = preload("res://scripts/infrastructure/data/event_step.gd")

## Signals
signal sequence_started(sequence: Resource) #EventSequence
signal sequence_finished(sequence: Resource) #EventSequence
signal sequence_paused()
signal sequence_resumed()
signal step_started(step: Resource, index: int) #EventStep
signal step_finished(step: Resource, index: int) #EventStep

## Current state
var current_sequence: Resource = null  # EventSequence
var current_step_index: int = -1
var is_playing: bool = false
var is_paused: bool = false
var _resume_signal: Signal

## Debug mode
var debug_mode: bool = false

## Timeout constants for signal awaits (prevents hangs if signals never fire)
const DEFAULT_SIGNAL_TIMEOUT_SECONDS: float = 30.0
const CARAVAN_TIMEOUT_SECONDS: float = 300.0  # 5 minutes for shop visits

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

## Resume a paused sequence (called when returning from shop, etc.)
func resume_sequence() -> void:
	if not is_paused:
		push_warning("EventSequencer: resume_sequence() called but sequence is not paused")
		return

	if debug_mode:
		print("EventSequencer: Resuming sequence")

	is_paused = false
	sequence_resumed.emit()

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

		step_type_enum.OPEN_CARAVAN:
			await _execute_open_caravan(step)

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

	# DialogueManager is an autoload
	var dialogue_manager: Node = DialogueManager

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
		var game_controller: Node = get_tree().get_first_node_in_group(GroupIDs.GAME_CONTROLLER)
		if game_controller and game_controller.has_method("FreezeGame"):
			if debug_mode:
				print("EventSequencer: Freezing game for dialogue")
			game_controller.call("FreezeGame")

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
		if game_controller and game_controller.has_method("UnfreezeGame"):
			if debug_mode:
				print("EventSequencer: Unfreezing game after dialogue")
			game_controller.call("UnfreezeGame")
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

	# Get timeout from step or use default
	var timeout_val: Variant = step.get("timeout")
	var timeout: float = timeout_val if timeout_val is float else DEFAULT_SIGNAL_TIMEOUT_SECONDS

	# Wait for signal with timeout protection
	var received: bool = await _await_signal_with_timeout(source.get(signal_name), timeout)

	if received:
		if debug_mode:
			print("EventSequencer: Signal '%s' received!" % signal_name)
	else:
		push_warning("EventSequencer: Signal '%s' from '%s' timed out after %.1fs" % [signal_name, signal_source, timeout])

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
	var game_controller: Node = get_tree().get_first_node_in_group(GroupIDs.GAME_CONTROLLER)
	if game_controller and game_controller.has_method("UnfreezeGame"):
		if debug_mode:
			print("EventSequencer: Unfreezing game for unit spawn")
		game_controller.call("UnfreezeGame")

	# Create card from CardCatalog
	# Get SimulationNode for spawning
	var sim_node: Node = get_tree().get_first_node_in_group(GroupIDs.SIMULATION_NODE)
	if not sim_node:
		push_error("EventSequencer: SimulationNode not found")
		return

	# Collect stat overrides
	var stat_overrides_val: Variant = step.get("stat_overrides")
	var stat_overrides: Variant = stat_overrides_val if stat_overrides_val is Dictionary and not stat_overrides_val.is_empty() else null

	if debug_mode:
		print("EventSequencer: Spawning %s at %s for team %d" % [card_id, spawn_position, team])

	# Spawn directly via SpawnUnitCommand (no mana, no casting)
	sim_node.QueueSpawnUnit(card_id, team, spawn_position, true, stat_overrides)
	if debug_mode:
		print("EventSequencer: Unit spawn queued successfully")

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
	return get_tree().get_first_node_in_group(GroupIDs.HAND_UI)

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

## Reset the EventSequencer to initial state
## Called between battles to clear any persisted state from autoload
func reset() -> void:
	if debug_mode:
		print("EventSequencer: Resetting state...")

	# Stop any active sequence
	if is_playing:
		stop_sequence()

	# Clear all state
	current_sequence = null
	current_step_index = -1
	is_playing = false

	if debug_mode:
		print("EventSequencer: Reset complete")

## Execute OPEN_CARAVAN step
##
## NOTE: This step is currently unused for caravan events. Caravan events navigate
## directly to ShopScreen from CampaignMap, which detects EventContext and plays
## dialogue on top of the shop UI. This approach allows seamless browsing during dialogue.
##
## OPEN_CARAVAN is preserved for potential future use cases where:
## - EventScreen needs to navigate to shop mid-sequence
## - Multiple shops are visited within a single event sequence
## - Shop navigation needs to be conditional based on event state
##
## Current caravan flow: CampaignMap → ShopScreen (with EventContext) → Dialogue on top
## Alternative flow: EventScreen → Sequence → OPEN_CARAVAN step → ShopScreen → Resume
func _execute_open_caravan(step: Resource) -> void:
	var shop_id_val: Variant = step.get("shop_id")
	var shop_id: String = shop_id_val if shop_id_val is String else ""

	if shop_id.is_empty():
		push_warning("EventSequencer: OPEN_CARAVAN step has empty shop_id")
		return

	if debug_mode:
		print("EventSequencer: Opening caravan shop '%s'" % shop_id)

	# Push event screen as return destination
	NavigationContext.push_return(SceneManager.SCENE_EVENT_SCREEN)

	# Navigate to shop screen with the specified shop_id
	SceneManager.transition_to(SceneManager.SCENE_SHOP_SCREEN)

	# Wait for shop screen to load and then set the shop_id
	await get_tree().process_frame

	# Find the shop screen and set its shop_id
	var shop_screen: Node = get_tree().get_first_node_in_group("shop_screen")
	if shop_screen and shop_screen.has_method("set_shop_id"):
		shop_screen.call("set_shop_id", shop_id)
	else:
		push_warning("EventSequencer: Could not find shop_screen to set shop_id")

	# Pause the sequence and wait for resume
	is_paused = true
	sequence_paused.emit()
	if debug_mode:
		print("EventSequencer: Sequence paused for shop visit, awaiting resume...")

	# Wait for resume signal with timeout protection
	var resumed: bool = await _await_signal_with_timeout(sequence_resumed, CARAVAN_TIMEOUT_SECONDS)

	if resumed:
		if debug_mode:
			print("EventSequencer: Sequence resumed after shop visit")
	else:
		push_warning("EventSequencer: Caravan visit timed out after %.0fs, forcing resume" % CARAVAN_TIMEOUT_SECONDS)
		is_paused = false

## Await a signal with timeout protection to prevent hangs
## Returns true if signal was received, false if timed out
func _await_signal_with_timeout(sig: Signal, timeout_seconds: float) -> bool:
	return await AsyncUtils.await_signal_with_timeout(get_tree(), sig, timeout_seconds)
