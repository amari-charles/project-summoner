extends Control
class_name EncounterResults

@onready var title_label: Label = %TitleLabel
@onready var continue_button: Button = %ContinueButton

var _summary: Dictionary = {}
var _completion_event_published: bool = false
var _encounter_id: String = ""

func _ready() -> void:
	_encounter_id = BattleContext.encounter_id
	continue_button.text = Loc.t("ui.common.continue")
	continue_button.pressed.connect(_continue)
	_refresh()

func _refresh() -> void:
	_summary = CampaignApi.get_encounter_completion_summary(_encounter_id)
	if _summary.is_empty():
		SceneManager.transition_to(SceneManager.SCENE_WALKABLE_ACADEMY_HUB)
		return
	var outcome: String = SafeTypeUtils.string(_summary.get("outcome"), "Victory")
	title_label.text = Loc.t("academy.flow.outcome_%s" % outcome.to_lower())
	continue_button.disabled = false
	if not _completion_event_published:
		_completion_event_published = true
		NarrativeDirectorApi.publish_event(
			NarrativeDirectorApi.EventType.ACTIVITY_COMPLETED,
			_encounter_id,
			{"encounter_id": _encounter_id, "outcome": outcome}
		)

func _continue() -> void:
	CampaignApi.consume_encounter_completion_summary(_encounter_id)
	SceneManager.transition_to(SceneManager.SCENE_WALKABLE_ACADEMY_HUB)
