class_name ChoiceNodePanel
extends NodeDetailPanelBase

## ChoiceNodePanel - Detail panel for path choice/branching events
##
## Displays event description and choice option buttons.
## When a choice is made, emits choice_made signal with the option ID.

## Emitted when user selects a choice option
signal choice_made(option_id: String)

## UI References (set from scene)
@onready var event_name_label: Label = %EventNameLabel
@onready var description_label: Label = %DescriptionLabel
@onready var options_container: VBoxContainer = %OptionsContainer


func _configure_impl() -> void:
	# Update labels
	event_name_label.text = _safe_string(event_data.get("name", "Choose Your Path"), "Choose Your Path")
	description_label.text = _safe_string(event_data.get("description", ""), "")

	# Clear previous options
	for child: Node in options_container.get_children():
		child.queue_free()

	# Get options from event data
	var options: Array = _safe_array(event_data.get("options", []))
	if options.is_empty():
		push_warning("ChoiceNodePanel: Choice event '%s' has no options" % event_id)
		return

	# Add option buttons
	for option_variant: Variant in options:
		if not option_variant is Dictionary:
			continue
		var option: Dictionary = option_variant
		var option_id: String = _safe_string(option.get("id", ""))
		var label_key: String = _safe_string(option.get("label_key", ""))
		var desc_key: String = _safe_string(option.get("description_key", ""))

		var label_text: String = Loc.t(label_key) if not label_key.is_empty() else option_id
		var desc_text: String = Loc.t(desc_key) if not desc_key.is_empty() else ""

		# Create option button
		var option_button: Button = Button.new()
		option_button.text = label_text
		option_button.custom_minimum_size = Vector2(300, 50)
		option_button.pressed.connect(_on_option_selected.bind(option_id))
		options_container.add_child(option_button)

		# Add description below button if available
		if not desc_text.is_empty():
			var desc_label: Label = Label.new()
			desc_label.text = desc_text
			desc_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			desc_label.add_theme_font_size_override("font_size", 14)
			desc_label.add_theme_color_override("font_color", Color(0.7, 0.7, 0.7))
			options_container.add_child(desc_label)

			var option_spacer: Control = Control.new()
			option_spacer.custom_minimum_size = Vector2(0, 15)
			options_container.add_child(option_spacer)


func get_event_type() -> StringName:
	return EventTypeIDs.CHOICE


func _on_option_selected(option_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	choice_made.emit(option_id)
