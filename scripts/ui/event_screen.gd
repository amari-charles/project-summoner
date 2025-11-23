extends Control

## EventScreen - Manages event lifecycle and sequence execution
##
## This screen handles non-battle campaign events (affinity selection,
## first summon, caravan encounters, etc.)
##
## Architecture:
## - CampaignMap calls EventContext.configure_event() then navigates here
## - We load and execute the event sequence
## - OPEN_CARAVAN and other steps can pause/navigate elsewhere
## - On completion, we mark event complete and return to campaign
##
## Lifecycle:
##   1. _ready() loads event from EventContext
##   2. _start_event() executes event sequence
##   3. Sequence may pause (e.g., OPEN_CARAVAN navigates to shop)
##   4. On sequence completion, _on_event_sequence_complete() fires
##   5. Mark event complete and return to campaign

## UI References
@onready var dialogue_box: Panel = $DialogueBox
@onready var dialogue_label: RichTextLabel = $DialogueBox/VBoxContainer/DialogueText
@onready var speaker_label: Label = $DialogueBox/VBoxContainer/SpeakerLabel
@onready var continue_button: Button = $DialogueBox/VBoxContainer/ContinueButton

## Event state
var _event_id: String = ""
var _event_config: Dictionary = {}
var _sequence: Resource = null

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("EventScreen: Ready")

	# Hide dialogue initially
	if dialogue_box:
		dialogue_box.visible = false

	# Connect continue button
	if continue_button:
		continue_button.pressed.connect(_on_continue_pressed)

	# Check if we're resuming a paused sequence (e.g., returning from shop)
	if EventSequencer.is_paused:
		print("EventScreen: Resuming paused sequence")
		EventSequencer.resume_sequence()
		return

	# Load event from EventContext
	await _load_event()

	# Start event sequence
	await _start_event()

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

	# Connect to EventSequencer signals
	if EventSequencer.has_signal("sequence_finished"):
		if not EventSequencer.sequence_finished.is_connected(_on_event_sequence_complete):
			EventSequencer.sequence_finished.connect(_on_event_sequence_complete)

	# Connect to DialogueManager for dialogue display
	if DialogueManager.has_signal("dialogue_started"):
		if not DialogueManager.dialogue_started.is_connected(_on_dialogue_started):
			DialogueManager.dialogue_started.connect(_on_dialogue_started)

	# Play sequence
	EventSequencer.play_sequence(_sequence)

## Handle dialogue started (show dialogue UI)
func _on_dialogue_started(dialogue_id: String) -> void:
	print("EventScreen: Dialogue started: %s" % dialogue_id)

	# Get dialogue data from DialogueManager
	var dialogue_data: Dictionary = {}
	if DialogueManager.has_method("get_dialogue"):
		var result: Variant = DialogueManager.call("get_dialogue", dialogue_id)
		if result is Dictionary:
			dialogue_data = result

	if dialogue_data.is_empty():
		push_warning("EventScreen: No dialogue found for '%s'" % dialogue_id)
		return

	# Show dialogue
	_show_dialogue(dialogue_data)

## Show dialogue in UI
func _show_dialogue(dialogue_data: Dictionary) -> void:
	if not dialogue_box:
		return

	var text: String = dialogue_data.get("text", "")
	var speaker: String = dialogue_data.get("speaker", "")

	if speaker_label:
		speaker_label.text = speaker
		speaker_label.visible = not speaker.is_empty()

	if dialogue_label:
		dialogue_label.text = text

	if continue_button:
		continue_button.visible = true
		continue_button.grab_focus()

	dialogue_box.visible = true

## Hide dialogue UI
func _hide_dialogue() -> void:
	if dialogue_box:
		dialogue_box.visible = false

## Continue button pressed
func _on_continue_pressed() -> void:
	_hide_dialogue()

	# Continue sequence
	if EventSequencer.has_method("continue_sequence"):
		EventSequencer.call("continue_sequence")

## Event sequence completed
func _on_event_sequence_complete(sequence: Resource) -> void:
	var sequence_id: String = sequence.get("sequence_id") if sequence.get("sequence_id") is String else "unknown"
	print("EventScreen: Event sequence completed: %s" % sequence_id)

	# Disconnect signals
	if EventSequencer.sequence_finished.is_connected(_on_event_sequence_complete):
		EventSequencer.sequence_finished.disconnect(_on_event_sequence_complete)

	if DialogueManager.dialogue_started.is_connected(_on_dialogue_started):
		DialogueManager.dialogue_started.disconnect(_on_dialogue_started)

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
	SceneManager.change_scene(return_to)
