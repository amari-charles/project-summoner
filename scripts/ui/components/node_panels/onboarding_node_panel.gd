class_name OnboardingNodePanel
extends NodeDetailPanelBase

## OnboardingNodePanel - Detail panel for onboarding events (affinity, first_summon)
##
## Simple panel with event info and continue button.
## Routes to the appropriate onboarding screen based on event type.

## UI References (set from scene)
@onready var event_name_label: Label = %EventNameLabel
@onready var description_label: Label = %DescriptionLabel
@onready var start_button: Button = %StartButton


func _ready() -> void:
	start_button.pressed.connect(_on_start_pressed)


func _configure_impl() -> void:
	# Update labels
	event_name_label.text = _safe_string(event_data.get("name", "Unknown"), "Unknown")
	description_label.text = _safe_string(event_data.get("description", ""), "")

	# Update start button
	start_button.text = get_start_button_text()
	start_button.disabled = is_start_disabled()


func get_event_type() -> StringName:
	var event_type: Variant = event_data.get("event_type", EventTypeIDs.ONBOARDING)
	if event_type is StringName:
		return event_type
	elif event_type is String:
		return StringName(event_type)
	return EventTypeIDs.ONBOARDING


func get_start_button_text() -> String:
	var is_completed: bool = _safe_bool(Campaign.is_battle_completed(event_id))
	var is_repeatable: bool = _safe_bool(event_data.get("repeatable", false))

	if is_completed:
		if is_repeatable:
			return Loc.t("campaign.map.button_continue_again")
		else:
			return Loc.t("campaign.map.button_completed")
	else:
		return Loc.t("campaign.map.button_continue")


func _on_start_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	start_requested.emit()
