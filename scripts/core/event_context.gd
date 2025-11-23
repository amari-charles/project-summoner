extends Node

## EventContext - Manages current event state
##
## This autoload tracks the active event and its configuration,
## similar to how BattleContext manages battle state.
##
## Architecture:
## - CampaignMap/UI calls configure_event() when starting an event
## - EventScreen manages the event lifecycle and sequence execution
## - OPEN_CARAVAN and other steps can pause/resume using this context
## - complete_event() marks the event complete in Campaign service
##
## Usage:
##   EventContext.configure_event("event_caravan_tutorial")
##   # ... event runs ...
##   EventContext.complete_event()

## Signals
signal event_configured(event_id: String)
signal event_completed(event_id: String)

## Current event state
var current_event_id: String = ""
var event_config: Dictionary = {}
var return_scene: String = ""
var is_event_complete: bool = false

## Dependencies
var _campaign: Node = null

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("EventContext: Initializing...")

	# Wait for Campaign service to be ready
	await get_tree().process_frame
	_campaign = get_node_or_null("/root/Campaign")

	if not _campaign:
		push_error("EventContext: Campaign service not found!")

	print("EventContext: Ready")

## =============================================================================
## EVENT CONFIGURATION
## =============================================================================

## Configure event context (called before navigating to event screen)
func configure_event(event_id: String, return_to_scene: String = "") -> void:
	if not _campaign:
		push_error("EventContext: Cannot configure event - Campaign service not found")
		return

	# Get event config from Campaign
	var config: Dictionary = {}
	if _campaign.has_method("get_battle"):
		var result: Variant = _campaign.call("get_battle", event_id)
		if result is Dictionary:
			config = result

	if config.is_empty():
		push_error("EventContext: Event not found: %s" % event_id)
		return

	# Store context
	current_event_id = event_id
	event_config = config
	return_scene = return_to_scene
	is_event_complete = false

	print("EventContext: Configured event '%s' (return to: %s)" % [event_id, return_to_scene])
	event_configured.emit(event_id)

## Clear event context
func clear_event() -> void:
	current_event_id = ""
	event_config = {}
	return_scene = ""
	is_event_complete = false
	print("EventContext: Cleared event context")

## =============================================================================
## EVENT COMPLETION
## =============================================================================

## Mark current event as complete
func complete_event() -> void:
	if current_event_id.is_empty():
		push_warning("EventContext: complete_event() called with no active event")
		return

	if is_event_complete:
		push_warning("EventContext: Event '%s' already marked complete" % current_event_id)
		return

	# Check if event is repeatable
	var repeatable: bool = event_config.get("repeatable", false)

	# Mark complete in Campaign service (only if not repeatable)
	if not repeatable:
		if _campaign and _campaign.has_method("complete_battle"):
			_campaign.call("complete_battle", current_event_id)
			print("EventContext: Marked event '%s' as complete" % current_event_id)
	else:
		print("EventContext: Event '%s' is repeatable - not marking as complete" % current_event_id)

	is_event_complete = true
	event_completed.emit(current_event_id)

## =============================================================================
## GETTERS
## =============================================================================

## Get current event ID
func get_current_event_id() -> String:
	return current_event_id

## Get current event config
func get_event_config() -> Dictionary:
	return event_config

## Get return scene path
func get_return_scene() -> String:
	return return_scene

## Check if event is complete
func get_is_complete() -> bool:
	return is_event_complete
