extends Control
class_name EncounterResults

@onready var title_label: Label = %TitleLabel
@onready var earned_label: Label = %EarnedLabel
@onready var progress_label: Label = %ProgressLabel
@onready var choices: VBoxContainer = %Choices
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
	var earned: Array = SafeTypeUtils.array(_summary.get("granted_rewards"))
	earned_label.text = "%s\n%s" % [Loc.t("academy.flow.earned_now"), _grant_summary(earned) if not earned.is_empty() else Loc.t("academy.flow.none")]
	var progressed: String = Loc.t("academy.flow.activity_complete") \
		if outcome.to_lower() == "victory" else Loc.t("academy.flow.no_course_progress")
	progress_label.text = progressed
	choices.visible = false
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

func _grant_summary(grants: Array) -> String:
	var result: Array[String] = []
	for value: Variant in grants:
		var grant: Dictionary = SafeTypeUtils.dict(value)
		var grant_id: String = SafeTypeUtils.string(grant.get("id"))
		var label: String = grant_id
		var kind: String = SafeTypeUtils.string(grant.get("kind"))
		if kind == "card":
			label = SafeTypeUtils.string(CardCatalogApi.get_card_as_dict(grant_id).get("card_name"), grant_id)
		elif not kind.is_empty():
			label = Loc.t("academy.reward.%s" % grant_id)
		else:
			label = Loc.t("academy.flow.reward")
		result.append("%s x%d" % [
			label,
			SafeTypeUtils.int_val(grant.get("amount"), 1),
		])
	return ", ".join(result)
