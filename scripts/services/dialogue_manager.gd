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

## State

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

## Start a dialogue by ID
## Loads the dialogue resource and begins displaying it
func start_dialogue(dialogue_id: String) -> void:
	var dialogue: DialogueData = _load_dialogue(dialogue_id)
	if not dialogue:
		push_error("DialogueManager: Dialogue not found: %s" % dialogue_id)
		return

	current_dialogue = dialogue
	current_line_index = 0
	dialogue_started.emit(dialogue)

	# Display first line
	_display_current_line()

## Advance to the next line in the current dialogue
## If all lines are shown, either present choices or end dialogue
func advance_dialogue() -> void:
	if not current_dialogue:
		return

	current_line_index += 1

	# Check if more lines remain
	if current_line_index < current_dialogue.lines.size():
		_display_current_line()
	else:
		# All lines shown - check for choices or next dialogue
		_complete_dialogue()

## Select a choice and navigate to the next dialogue
func select_choice(choice: DialogueChoice) -> void:
	if not choice:
		return

	# Execute action if present
	if not choice.action.is_empty():
		_execute_action(choice.action)

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

	var line_text: String = current_dialogue.lines[current_line_index]
	var character: String = current_dialogue.character_name
	var portrait: Texture2D = current_dialogue.portrait

	dialogue_line_displayed.emit(line_text, character, portrait)

## Complete the current dialogue (all lines shown)
func _complete_dialogue() -> void:
	if not current_dialogue:
		return

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
		dialogue_ended.emit()
		current_dialogue = null
		current_line_index = 0

		if should_auto_advance:
			start_dialogue(next_id)
	else:
		# End of dialogue chain
		dialogue_ended.emit()
		current_dialogue = null
		current_line_index = 0

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
