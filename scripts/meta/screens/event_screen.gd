extends Control

## EventScreen - Manages event lifecycle and sequence execution
##
## This screen handles non-battle campaign events (affinity selection,
## first summon, caravan encounters, etc.)
##
## Architecture:
## - CampaignMap calls EventContext.configure_event() then navigates here
## - We load and execute the event sequence
## - EventSequencer + DialogueManager handle dialogue display
## - OPEN_CARAVAN and other steps can pause/navigate elsewhere
## - On completion, we mark event complete and return to campaign
##
## Lifecycle:
##   1. _ready() loads event from EventContext
##   2. _start_event() executes event sequence
##   3. Sequence may pause (e.g., OPEN_CARAVAN navigates to shop)
##   4. On sequence completion, _on_event_sequence_complete() fires
##   5. Mark event complete and return to campaign

## Event state
var _event_id: String = ""
var _event_config: Dictionary = {}
var _sequence: Resource = null

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("EventScreen: Ready")

	# Check if we're resuming a paused sequence (e.g., returning from shop)
	if EventSequencer.is_paused:
		print("EventScreen: Resuming paused sequence")
		EventSequencer.resume_sequence()
		return

	# Load event from EventContext
	await _load_event()

	# Start event sequence
	await _start_event()

## Clean up signal connections when this screen is destroyed
func _exit_tree() -> void:
	# Ensure we disconnect from EventSequencer if we're destroyed mid-sequence
	if EventSequencer.sequence_finished.is_connected(_on_event_sequence_complete):
		EventSequencer.sequence_finished.disconnect(_on_event_sequence_complete)

## =============================================================================
## EVENT LOADING
## =============================================================================

func _load_event() -> void:
	# Get event from EventContext
	_event_id = EventContext.get_current_event_id()
	_event_config = EventContext.get_event_config()

	if _event_id.is_empty() or _event_config.is_empty():
		push_error("EventScreen: No event configured in EventContext!")
		_return_to_campaign()
		return

	print("EventScreen: Loaded event '%s'" % _event_id)

	# Load event sequence
	var sequence_path: String = _event_config.get("event_sequence", "")
	if sequence_path.is_empty():
		push_error("EventScreen: Event '%s' has no event_sequence!" % _event_id)
		_return_to_campaign()
		return

	_sequence = load(sequence_path)
	if not _sequence:
		push_error("EventScreen: Failed to load event sequence: %s" % sequence_path)
		_return_to_campaign()
		return

	print("EventScreen: Loaded event sequence: %s" % sequence_path)

## =============================================================================
## EVENT EXECUTION
## =============================================================================

func _start_event() -> void:
	if not _sequence:
		push_error("EventScreen: Cannot start event - no sequence loaded")
		_return_to_campaign()
		return

	print("EventScreen: Starting event sequence...")

	# Connect to EventSequencer completion signal
	if EventSequencer.has_signal("sequence_finished"):
		if not EventSequencer.sequence_finished.is_connected(_on_event_sequence_complete):
			EventSequencer.sequence_finished.connect(_on_event_sequence_complete)

	# Play sequence (EventSequencer + DialogueManager handle dialogue display)
	EventSequencer.play_sequence(_sequence)

## Event sequence completed
func _on_event_sequence_complete(sequence: Resource) -> void:
	var sequence_id: String = SafeTypeUtils.string(sequence.get("sequence_id"), "unknown")
	print("EventScreen: Event sequence completed: %s" % sequence_id)

	# Disconnect signal
	if EventSequencer.sequence_finished.is_connected(_on_event_sequence_complete):
		EventSequencer.sequence_finished.disconnect(_on_event_sequence_complete)

	# Mark event complete
	EventContext.complete_event()

	# Return to campaign
	await get_tree().create_timer(0.5).timeout  # Brief pause
	_return_to_campaign()

## =============================================================================
## NAVIGATION
## =============================================================================

func _return_to_campaign() -> void:
	# Get return scene from EventContext
	var return_to: String = EventContext.get_return_scene()

	# Clear event context
	EventContext.clear_event()

	# Return to campaign (or specified scene)
	if return_to.is_empty():
		return_to = SceneManager.SCENE_CAMPAIGN_MAP

	print("EventScreen: Returning to %s" % return_to)
	SceneManager.transition_to(return_to)
