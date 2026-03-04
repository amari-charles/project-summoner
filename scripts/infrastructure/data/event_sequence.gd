extends Resource
class_name EventSequence

## EventSequence - A scripted sequence of events
##
## Defines a series of EventSteps that execute sequentially.
## Can be created as .tres resources or built programmatically.
##
## Example:
##   var sequence: EventSequence = preload("res://sequences/tutorial_intro.tres")
##   EventSequencer.play_sequence(sequence)

@export var sequence_id: String = ""  ## Unique identifier for this sequence
@export var description: String = ""  ## Human-readable description

@export var steps: Array[EventStep] = []  ## Steps to execute in order

## Optional: Trigger condition (when to auto-play this sequence)
@export_group("Auto-Trigger")
@export var trigger_on_battle_start: bool = false
@export var trigger_on_signal: String = ""  ## Format: "EventHubName.signal_name"

## Optional: Loop behavior
@export_group("Loop")
@export var loop: bool = false  ## Whether to loop this sequence
@export var loop_count: int = -1  ## Number of loops (-1 = infinite)

## Get total number of steps
func get_step_count() -> int:
	return steps.size()

## Get step at index
func get_step(index: int) -> EventStep:
	if index < 0 or index >= steps.size():
		return null
	return steps[index]

## Add step to sequence
func add_step(step: EventStep) -> void:
	steps.append(step)

## Insert step at index
func insert_step(index: int, step: EventStep) -> void:
	steps.insert(index, step)

## Remove step at index
func remove_step(index: int) -> void:
	if index >= 0 and index < steps.size():
		steps.remove_at(index)

## Clear all steps
func clear_steps() -> void:
	steps.clear()
