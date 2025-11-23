extends Node

## DialogueManager - Singleton service for managing dialogue state and flow
##
## Handles dialogue progression, choice selection, and variable tracking.
## Emits signals for UI to react to dialogue events.

## Signals

## Emitted when a new dialogue starts
signal dialogue_started(dialogue_data: DialogueData)

## Emitted when a new line should be displayed
signal dialogue_line_displayed(text: String, character_name: String, portrait: Texture2D)

## Emitted when choices should be presented to the player
signal dialogue_choices_presented(choices: Array[DialogueChoice])

## Emitted when the current dialogue ends
signal dialogue_ended()

## Emitted when the DialogueManager system is fully initialized and ready to handle requests
## (Both manager and UI components are connected and functional)
signal system_ready()

## State

## Track if the dialogue system is fully initialized
var _is_system_ready: bool = false

## Currently active dialogue
var current_dialogue: DialogueData = null

## Current line index within the dialogue
var current_line_index: int = 0

## Dictionary of dialogue variables for conditions/actions
## Format: {"variable_name": true/false}
var variables: Dictionary = {}

## Cache of loaded dialogue resources by ID
var dialogue_cache: Dictionary = {}

## =============================================================================
## PUBLIC API
## =============================================================================

## Notify that the UI (DialogueBox) has connected and is ready
## This should be called by DialogueBox after it completes its signal connections
func notify_ui_connected() -> void:
	if not _is_system_ready:
		_is_system_ready = true
		system_ready.emit()
		print("DialogueManager: System ready - UI connected")

## Check if the dialogue system is ready to handle requests
func is_system_ready() -> bool:
	return _is_system_ready

## Start a dialogue by ID
## Loads the dialogue resource and begins displaying it
func start_dialogue(dialogue_id: String) -> void:
	print("DialogueManager: start_dialogue called with ID: %s" % dialogue_id)

	# Debug: Check if file exists
	var expected_path: String = "res://resources/dialogue/%s.tres" % dialogue_id
	print("DialogueManager: Looking for dialogue at: %s" % expected_path)
	print("DialogueManager: File exists: %s" % ResourceLoader.exists(expected_path))

	var dialogue: DialogueData = _load_dialogue(dialogue_id)
	if not dialogue:
		push_error("DialogueManager: Dialogue not found: %s" % dialogue_id)
		push_error("DialogueManager: Expected path: %s" % expected_path)
		return

	print("DialogueManager: Dialogue loaded successfully")
	print("DialogueManager: Dialogue character_name: %s" % dialogue.character_name)
	print("DialogueManager: Dialogue lines count: %d" % dialogue.lines.size())

	# Block capabilities during dialogue
	CapabilityManager.block_capability(
		CapabilityManager.Capability.PLAY_CARDS,
		CapabilityManager.BlockReason.DIALOGUE_ACTIVE
	)
	CapabilityManager.block_capability(
		CapabilityManager.Capability.PAUSE_GAME,
		CapabilityManager.BlockReason.DIALOGUE_ACTIVE
	)

	current_dialogue = dialogue
	current_line_index = 0
	print("DialogueManager: Emitting dialogue_started signal")
	dialogue_started.emit(dialogue)

	# Display first line
	print("DialogueManager: Calling _display_current_line()")
	_display_current_line()

## Advance to the next line in the current dialogue
## If all lines are shown, either present choices or end dialogue
func advance_dialogue() -> void:
	print("DialogueManager: advance_dialogue called")
	if not current_dialogue:
		print("DialogueManager: No current dialogue, returning")
		return

	current_line_index += 1
	print("DialogueManager: Incremented line index to %d (total lines: %d)" % [current_line_index, current_dialogue.lines.size()])

	# Check if more lines remain
	if current_line_index < current_dialogue.lines.size():
		print("DialogueManager: More lines remain, displaying next line")
		_display_current_line()
	else:
		# All lines shown - check for choices or next dialogue
		print("DialogueManager: All lines shown, completing dialogue")
		_complete_dialogue()

## Select a choice and navigate to the next dialogue
func select_choice(choice: DialogueChoice) -> void:
	if not choice:
		return

	# Execute action if present
	if not choice.action.is_empty():
		_execute_action(choice.action)

	# Unblock capabilities before ending dialogue
	CapabilityManager.unblock_capability(
		CapabilityManager.Capability.PLAY_CARDS,
		CapabilityManager.BlockReason.DIALOGUE_ACTIVE
	)
	CapabilityManager.unblock_capability(
		CapabilityManager.Capability.PAUSE_GAME,
		CapabilityManager.BlockReason.DIALOGUE_ACTIVE
	)

	# End current dialogue
	dialogue_ended.emit()
	current_dialogue = null
	current_line_index = 0

	# Start next dialogue if specified
	if not choice.next_dialogue_id.is_empty():
		start_dialogue(choice.next_dialogue_id)

## End the current dialogue immediately
func end_dialogue() -> void:
	if current_dialogue:
		# Unblock capabilities before ending
		CapabilityManager.unblock_capability(
			CapabilityManager.Capability.PLAY_CARDS,
			CapabilityManager.BlockReason.DIALOGUE_ACTIVE
		)
		CapabilityManager.unblock_capability(
			CapabilityManager.Capability.PAUSE_GAME,
			CapabilityManager.BlockReason.DIALOGUE_ACTIVE
		)

		dialogue_ended.emit()
		current_dialogue = null
		current_line_index = 0

## Set a dialogue variable (for conditions)
func set_variable(variable_name: String, value: bool) -> void:
	variables[variable_name] = value

## Get a dialogue variable
func get_variable(variable_name: String) -> bool:
	return variables.get(variable_name, false)

## =============================================================================
## INTERNAL METHODS
## =============================================================================

## Display the current line
func _display_current_line() -> void:
	if not current_dialogue or current_line_index >= current_dialogue.lines.size():
		return

	var line_text: String = Loc.t(current_dialogue.lines[current_line_index])
	var character: String = Loc.t(current_dialogue.character_name)
	var portrait: Texture2D = current_dialogue.portrait

	dialogue_line_displayed.emit(line_text, character, portrait)

## Complete the current dialogue (all lines shown)
func _complete_dialogue() -> void:
	print("DialogueManager: _complete_dialogue called")
	if not current_dialogue:
		print("DialogueManager: No current dialogue in _complete_dialogue")
		return

	print("DialogueManager: Checking for choices (count: %d)" % current_dialogue.choices.size())
	# Check for choices
	if not current_dialogue.choices.is_empty():
		# Filter choices by condition
		var available_choices: Array[DialogueChoice] = []
		for choice: DialogueChoice in current_dialogue.choices:
			if _is_condition_met(choice.condition):
				available_choices.append(choice)

		if not available_choices.is_empty():
			dialogue_choices_presented.emit(available_choices)
			return

	# No choices - check for next dialogue or end
	if not current_dialogue.next_dialogue_id.is_empty():
		var next_id: String = current_dialogue.next_dialogue_id
		var should_auto_advance: bool = current_dialogue.auto_advance

		# Unblock capabilities before ending
		CapabilityManager.unblock_capability(
			CapabilityManager.Capability.PLAY_CARDS,
			CapabilityManager.BlockReason.DIALOGUE_ACTIVE
		)
		CapabilityManager.unblock_capability(
			CapabilityManager.Capability.PAUSE_GAME,
			CapabilityManager.BlockReason.DIALOGUE_ACTIVE
		)

		dialogue_ended.emit()
		current_dialogue = null
		current_line_index = 0

		if should_auto_advance:
			# start_dialogue will block capabilities again
			start_dialogue(next_id)
	else:
		# End of dialogue chain - unblock capabilities
		print("DialogueManager: No choices and no next_dialogue_id - ending dialogue")
		CapabilityManager.unblock_capability(
			CapabilityManager.Capability.PLAY_CARDS,
			CapabilityManager.BlockReason.DIALOGUE_ACTIVE
		)
		CapabilityManager.unblock_capability(
			CapabilityManager.Capability.PAUSE_GAME,
			CapabilityManager.BlockReason.DIALOGUE_ACTIVE
		)

		print("DialogueManager: Emitting dialogue_ended signal")
		dialogue_ended.emit()
		current_dialogue = null
		current_line_index = 0
		print("DialogueManager: Dialogue ended successfully")

## Load a dialogue resource by ID
## First checks cache, then attempts to load from resources/dialogue/
func _load_dialogue(dialogue_id: String) -> DialogueData:
	# Check cache first
	if dialogue_cache.has(dialogue_id):
		return dialogue_cache[dialogue_id]

	# Try to load from resources
	var path: String = "res://resources/dialogue/%s.tres" % dialogue_id
	if ResourceLoader.exists(path):
		var dialogue: DialogueData = load(path)
		dialogue_cache[dialogue_id] = dialogue
		return dialogue

	return null

## Check if a condition is met
func _is_condition_met(condition: String) -> bool:
	if condition.is_empty():
		return true

	return get_variable(condition)

## Execute an action string
## Format: "variable_name=value"
func _execute_action(action: String) -> void:
	if action.is_empty():
		return

	var parts: PackedStringArray = action.split("=")
	if parts.size() != 2:
		push_warning("DialogueManager: Invalid action format: %s" % action)
		return

	var variable_name: String = parts[0].strip_edges()
	var value_str: String = parts[1].strip_edges()
	var value: bool = value_str.to_lower() == "true"

	set_variable(variable_name, value)
	print("DialogueManager: Set %s = %s" % [variable_name, value])

## Reset the DialogueManager to initial state
## Called between battles to clear any persisted state from autoload
func reset() -> void:
	print("DialogueManager: Resetting state...")

	# End any active dialogue
	if current_dialogue:
		end_dialogue()

	# Clear all state
	current_dialogue = null
	current_line_index = 0
	variables.clear()
	# Note: dialogue_cache is intentionally preserved for performance

	print("DialogueManager: Reset complete")
