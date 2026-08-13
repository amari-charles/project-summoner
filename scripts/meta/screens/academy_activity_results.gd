extends Control
class_name AcademyActivityResults

@onready var title_label: Label = %TitleLabel
@onready var earned_label: Label = %EarnedLabel
@onready var progress_label: Label = %ProgressLabel
@onready var choices: VBoxContainer = %Choices
@onready var continue_button: Button = %ContinueButton

var _summary: Dictionary = {}
var _pending_claim_id: String = ""
var _selected_option_id: String = ""
var _completion_event_published: bool = false

func _ready() -> void:
	continue_button.text = Loc.t("ui.common.continue")
	continue_button.pressed.connect(_continue)
	_refresh()

func _refresh() -> void:
	_summary = CampaignApi.get_last_academy_completion_summary()
	if _summary.is_empty():
		SceneManager.transition_to(SceneManager.SCENE_ACADEMY_COURSE_FLOW)
		return
	var outcome: String = SafeTypeUtils.string(_summary.get("outcome"), "Victory")
	title_label.text = Loc.t("academy.flow.outcome_%s" % outcome.to_lower())
	var earned: Array = SafeTypeUtils.array(_summary.get("granted_rewards"))
	earned_label.text = "%s\n%s" % [Loc.t("academy.flow.earned_now"), _grant_summary(earned) if not earned.is_empty() else Loc.t("academy.flow.none")]
	var course_id: String = SafeTypeUtils.string(_summary.get("course_id"))
	var course: Dictionary = CampaignApi.get_academy_course_flow_state(course_id)
	var progressed: String = Loc.t("academy.flow.no_course_progress")
	if SafeTypeUtils.bool_val(course.get("is_completed")):
		progressed = Loc.t("academy.flow.complete")
	elif outcome == "Victory":
		progressed = Loc.t("academy.flow.activity_complete")
	progress_label.text = "%s\n%s" % [Loc.t("academy.flow.course_progress"), progressed]
	_render_pending(SafeTypeUtils.array(course.get("reward_previews")))
	if not _completion_event_published:
		_completion_event_published = true
		NarrativeDirectorApi.publish_event(
			NarrativeDirectorApi.EventType.ACTIVITY_COMPLETED,
			SafeTypeUtils.string(_summary.get("activity_id")),
			{"course_id": course_id, "outcome": outcome}
		)

func _render_pending(previews: Array) -> void:
	for child: Node in choices.get_children():
		child.queue_free()
	_pending_claim_id = ""
	_selected_option_id = ""
	for value: Variant in previews:
		var preview: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(preview.get("status")) != "pending":
			continue
		_pending_claim_id = SafeTypeUtils.string(preview.get("claim_id"))
		for option_value: Variant in SafeTypeUtils.array(preview.get("options")):
			var option: Dictionary = SafeTypeUtils.dict(option_value)
			var button: Button = Button.new()
			var label_key: String = SafeTypeUtils.string(option.get("label_key"))
			button.text = (
				Loc.t(label_key)
				if not label_key.is_empty()
				else _grant_summary(SafeTypeUtils.array(option.get("grants")))
			)
			button.pressed.connect(_select_option.bind(SafeTypeUtils.string(option.get("option_id")), button))
			choices.add_child(button)
		break
	continue_button.disabled = not _pending_claim_id.is_empty()

func _select_option(option_id: String, selected_button: Button) -> void:
	_selected_option_id = option_id
	for child: Node in choices.get_children():
		if child is Button:
			(child as Button).button_pressed = child == selected_button
	continue_button.disabled = false

func _continue() -> void:
	if not _pending_claim_id.is_empty():
		if _selected_option_id.is_empty():
			return
		var result: Dictionary = CampaignApi.claim_academy_reward(
			_pending_claim_id, [_selected_option_id]
		)
		if SafeTypeUtils.string(result.get("status")) not in ["Ready", "AlreadyClaimed", "ready", "already_claimed"]:
			return
		_refresh()
		return
	CampaignApi.consume_last_academy_completion_summary()
	SceneManager.transition_to(SceneManager.SCENE_ACADEMY_COURSE_FLOW)

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
