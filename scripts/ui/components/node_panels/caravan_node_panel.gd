class_name CaravanNodePanel
extends NodeDetailPanelBase

## CaravanNodePanel - Detail panel for traveling merchant/shop events
##
## Single column layout with event info and description.
## No deck selection required - just enter the shop.

## UI References (set from scene)
@onready var event_name_label: Label = %EventNameLabel
@onready var description_label: Label = %DescriptionLabel
@onready var start_button: Button = %StartButton


func _ready() -> void:
	start_button.pressed.connect(_on_start_pressed)


func _configure_impl() -> void:
	# Update labels using typed accessors
	event_name_label.text = event.name
	description_label.text = event.description if not event.description.is_empty() else "No description."

	# Update start button
	start_button.text = get_start_button_text()
	start_button.disabled = is_start_disabled()


func get_event_type() -> StringName:
	return EventTypeIDs.CARAVAN


func get_start_button_text() -> String:
	var is_completed: bool = SafeTypeUtils.bool_val(Campaign.is_battle_completed(event.id))

	if is_completed:
		if event.repeatable:
			return Loc.t("campaign.map.button_enter_shop_again")
		else:
			return Loc.t("campaign.map.button_completed")
	else:
		return Loc.t("campaign.map.button_enter_shop")


func _on_start_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	start_requested.emit()
