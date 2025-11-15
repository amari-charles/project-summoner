extends Control
class_name DialogueBox

## DialogueBox - UI component for displaying dialogue with typewriter effect
##
## Listens to DialogueManager signals and displays dialogue text, portraits, and choices.
## Implements typewriter effect using visible_ratio animation.

## Node references
@onready var panel: PanelContainer = $Panel
@onready var character_name_label: Label = %CharacterName
@onready var dialogue_text: RichTextLabel = %DialogueText
@onready var portrait: TextureRect = $Panel/MarginContainer/HBoxContainer/Portrait
@onready var choice_container: VBoxContainer = %ChoiceContainer
@onready var continue_indicator: Label = %ContinueIndicator

## Typewriter effect settings
@export var typewriter_speed: float = 0.05  ## Seconds per character
@export var allow_skip: bool = true  ## Click to skip animation

## State
var is_typing: bool = false
var typing_timer: float = 0.0
var target_visible_ratio: float = 1.0
var char_increment: float = 0.0

## DialogueManager reference (will be set via autoload)
var dialogue_manager: Node = null

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Hide by default
	visible = false
	continue_indicator.visible = false

	# Connect to DialogueManager signals
	# Wait for autoload to be available
	await get_tree().process_frame
	dialogue_manager = get_node_or_null("/root/DialogueManager")

	if dialogue_manager:
		if dialogue_manager.has_signal("dialogue_started"):
			var dialogue_started_signal: Signal = dialogue_manager.get("dialogue_started")
			dialogue_started_signal.connect(_on_dialogue_started)
		if dialogue_manager.has_signal("dialogue_line_displayed"):
			var dialogue_line_displayed_signal: Signal = dialogue_manager.get("dialogue_line_displayed")
			dialogue_line_displayed_signal.connect(_on_dialogue_line_displayed)
		if dialogue_manager.has_signal("dialogue_choices_presented"):
			var dialogue_choices_presented_signal: Signal = dialogue_manager.get("dialogue_choices_presented")
			dialogue_choices_presented_signal.connect(_on_dialogue_choices_presented)
		if dialogue_manager.has_signal("dialogue_ended"):
			var dialogue_ended_signal: Signal = dialogue_manager.get("dialogue_ended")
			dialogue_ended_signal.connect(_on_dialogue_ended)
		print("DialogueBox: Connected to DialogueManager")
	else:
		push_warning("DialogueBox: DialogueManager not found in autoloads")

func _process(delta: float) -> void:
	if is_typing:
		_update_typewriter(delta)

func _input(event: InputEvent) -> void:
	if not visible:
		return

	# Click to skip typewriter or advance dialogue
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			_on_click()
	elif event is InputEventKey:
		var key_event: InputEventKey = event
		if key_event.pressed and (key_event.keycode == KEY_SPACE or key_event.keycode == KEY_ENTER):
			_on_click()

## =============================================================================
## SIGNAL HANDLERS
## =============================================================================

func _on_dialogue_started(dialogue_data: DialogueData) -> void:
	visible = true
	_clear_choices()
	continue_indicator.visible = false

	# Set character name
	character_name_label.text = dialogue_data.character_name

	# Set portrait if available
	if dialogue_data.portrait:
		portrait.texture = dialogue_data.portrait
		portrait.visible = true
	else:
		portrait.visible = false

func _on_dialogue_line_displayed(text: String, character: String, portrait_texture: Texture2D) -> void:
	_clear_choices()
	continue_indicator.visible = false

	# Update character name
	character_name_label.text = character

	# Update portrait
	if portrait_texture:
		portrait.texture = portrait_texture
		portrait.visible = true
	else:
		portrait.visible = false

	# Start typewriter effect
	_start_typewriter(text)

func _on_dialogue_choices_presented(choices: Array[DialogueChoice]) -> void:
	_clear_choices()
	continue_indicator.visible = false

	# Create button for each choice
	for choice: DialogueChoice in choices:
		var button: Button = Button.new()
		button.text = choice.choice_text
		button.pressed.connect(_on_choice_selected.bind(choice))
		choice_container.add_child(button)

func _on_dialogue_ended() -> void:
	visible = false
	_clear_choices()

## =============================================================================
## TYPEWRITER EFFECT
## =============================================================================

func _start_typewriter(text: String) -> void:
	dialogue_text.text = text
	dialogue_text.visible_ratio = 0.0

	# Calculate character increment
	var char_count: int = text.length()
	if char_count > 0:
		char_increment = 1.0 / float(char_count)
	else:
		char_increment = 1.0

	typing_timer = 0.0
	target_visible_ratio = 1.0
	is_typing = true

func _update_typewriter(delta: float) -> void:
	typing_timer += delta

	# Increment visible ratio based on timer
	var chars_to_show: int = int(typing_timer / typewriter_speed)
	dialogue_text.visible_ratio = min(float(chars_to_show) * char_increment, target_visible_ratio)

	# Check if typing complete
	if dialogue_text.visible_ratio >= target_visible_ratio:
		is_typing = false
		dialogue_text.visible_ratio = 1.0
		_on_typing_complete()

func _skip_typewriter() -> void:
	if is_typing:
		is_typing = false
		dialogue_text.visible_ratio = 1.0
		_on_typing_complete()

func _on_typing_complete() -> void:
	# Show continue indicator if no choices
	if choice_container.get_child_count() == 0:
		continue_indicator.visible = true

## =============================================================================
## INPUT HANDLING
## =============================================================================

func _on_click() -> void:
	if is_typing and allow_skip:
		# Skip typewriter effect
		_skip_typewriter()
	elif choice_container.get_child_count() == 0:
		# No choices - advance dialogue
		if dialogue_manager and dialogue_manager.has_method("advance_dialogue"):
			dialogue_manager.call("advance_dialogue")

func _on_choice_selected(choice: DialogueChoice) -> void:
	if dialogue_manager and dialogue_manager.has_method("select_choice"):
		dialogue_manager.call("select_choice", choice)

## =============================================================================
## HELPERS
## =============================================================================

func _clear_choices() -> void:
	for child: Node in choice_container.get_children():
		child.queue_free()
