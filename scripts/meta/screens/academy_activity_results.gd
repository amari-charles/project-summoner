extends Control
class_name EncounterResults

const CardWidgetScene: PackedScene = preload("res://scenes/meta/components/card_widget.tscn")

@onready var title_label: Label = %TitleLabel
@onready var earned_label: Label = %EarnedLabel
@onready var reward_reveals: HBoxContainer = %RewardReveals
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
	_render_reward_reveals(earned)
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


func _render_reward_reveals(grants: Array) -> void:
	for child: Node in reward_reveals.get_children():
		child.queue_free()
	reward_reveals.visible = not grants.is_empty()
	for value: Variant in grants:
		var grant: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(grant.get("kind")) == "card":
			_add_card_reward(grant)
		else:
			var label: Label = Label.new()
			label.text = "%s ×%d" % [
				SafeTypeUtils.string(grant.get("id")).capitalize(),
				SafeTypeUtils.int_val(grant.get("amount"), 1),
			]
			label.add_theme_font_size_override("font_size", 22)
			reward_reveals.add_child(label)


func _add_card_reward(grant: Dictionary) -> void:
	var card_id: String = SafeTypeUtils.string(grant.get("card_id", grant.get("id")))
	var card_data: Dictionary = CardCatalogApi.get_card_as_dict(card_id)
	var card_widget: CardWidget = CardWidgetScene.instantiate() as CardWidget
	card_widget.set_draggable(false)
	card_widget.ready.connect(
		func() -> void: card_widget.set_card({"catalog_id": card_id}, card_data),
		CONNECT_ONE_SHOT
	)
	reward_reveals.add_child(card_widget)
