extends Control
class_name AcademyCourseFlow

@onready var back_button: Button = %BackButton
@onready var title_label: Label = %TitleLabel
@onready var state_label: Label = %StateLabel
@onready var description_label: Label = %DescriptionLabel
@onready var rewards_label: Label = %RewardsLabel
@onready var activities: VBoxContainer = %Activities
@onready var enroll_button: Button = %EnrollButton

var _course_id: String = ""
var _course: Dictionary = {}

func _ready() -> void:
	back_button.text = "←"
	back_button.tooltip_text = Loc.t("academy.class_hall.title")
	back_button.accessibility_name = Loc.t("academy.class_hall.title")
	back_button.pressed.connect(_go_back)
	enroll_button.pressed.connect(_enroll)
	_course_id = BattleContext.academy_course_id
	_refresh()

func _refresh() -> void:
	_course = CampaignApi.get_academy_course_flow_state(_course_id)
	if _course.is_empty():
		_go_back()
		return
	title_label.text = Loc.t(SafeTypeUtils.string(_course.get("name_key")))
	state_label.text = _course_state()
	description_label.text = Loc.t(SafeTypeUtils.string(_course.get("description_key")))
	rewards_label.text = "%s\n%s" % [Loc.t("academy.flow.course_rewards"), _reward_summary(
		SafeTypeUtils.array(_course.get("reward_previews"))
	)]
	enroll_button.visible = not SafeTypeUtils.bool_val(_course.get("is_enrolled")) \
		and not SafeTypeUtils.bool_val(_course.get("is_completed"))
	enroll_button.disabled = not SafeTypeUtils.bool_val(_course.get("is_available"))
	enroll_button.text = Loc.t("academy.hub.enroll")
	_clear_activities()
	for value: Variant in SafeTypeUtils.array(_course.get("activities")):
		var activity: Dictionary = SafeTypeUtils.dict(value)
		var button: Button = Button.new()
		button.text = "%s  •  %s  •  %s  •  %s\n%s" % [
			Loc.t(SafeTypeUtils.string(activity.get("label_key"))),
			_enum_label("role", SafeTypeUtils.string(activity.get("role"))),
			_enum_label("mode", SafeTypeUtils.string(activity.get("deck_mode"))),
			_enum_label("state", SafeTypeUtils.string(activity.get("lifecycle_state"))),
			_activity_detail(activity),
		]
		button.alignment = HORIZONTAL_ALIGNMENT_LEFT
		button.custom_minimum_size.y = 72.0
		button.pressed.connect(_inspect_activity.bind(SafeTypeUtils.string(activity.get("id"))))
		activities.add_child(button)

func _course_state() -> String:
	if SafeTypeUtils.bool_val(_course.get("is_completed")):
		return Loc.t("academy.flow.state_completed")
	if SafeTypeUtils.bool_val(_course.get("is_enrolled")):
		return Loc.t("academy.flow.state_active")
	return Loc.t("academy.flow.state_available")

func _enroll() -> void:
	if CampaignApi.enroll_academy_course(_course_id):
		_refresh()

func _inspect_activity(activity_id: String) -> void:
	BattleContext.select_academy_activity(_course_id, activity_id)
	SceneManager.transition_to(SceneManager.SCENE_ACADEMY_ACTIVITY_PREPARATION)

func _go_back() -> void:
	SceneManager.transition_to(SceneManager.SCENE_ACADEMY_CLASS_HALL)

func _clear_activities() -> void:
	for child: Node in activities.get_children():
		child.queue_free()

func _activity_detail(activity: Dictionary) -> String:
	var rule_text: String = _rule_summary(SafeTypeUtils.dict(activity.get("loadout")))
	return "%s  •  %s" % [rule_text, Loc.t("academy.flow.rewards", {"rewards": _reward_summary(SafeTypeUtils.array(activity.get("reward_previews")))})]

func _reward_summary(previews: Array) -> String:
	if previews.is_empty():
		return Loc.t("academy.flow.none")
	var labels: Array[String] = []
	for value: Variant in previews:
		var preview: Dictionary = SafeTypeUtils.dict(value)
		var status: String = SafeTypeUtils.string(preview.get("status"), "available").capitalize()
		var options: Array = SafeTypeUtils.array(preview.get("options"))
		var label: String = Loc.t("academy.flow.reward")
		if not options.is_empty():
			var option: Dictionary = SafeTypeUtils.dict(options[0])
			var grants: Array = SafeTypeUtils.array(option.get("grants"))
			if not grants.is_empty():
				var grant: Dictionary = SafeTypeUtils.dict(grants[0])
				label = _grant_label(grant)
		labels.append("%s (%s)" % [label, status])
	return ", ".join(labels)

func _enum_label(group: String, value: String) -> String:
	return Loc.t("academy.flow.%s_%s" % [group, value.to_lower()])

func _rule_summary(loadout: Dictionary) -> String:
	var rules: Dictionary = SafeTypeUtils.dict(loadout.get("rules"))
	if not SafeTypeUtils.bool_val(rules.get("has_rules")):
		return Loc.t("academy.flow.no_special_rules")
	var parts: Array[String] = []
	var allowed_types: Array = SafeTypeUtils.array(rules.get("allowed_card_types"))
	if not allowed_types.is_empty():
		parts.append(Loc.t("academy.flow.allowed_types", {"values": ", ".join(allowed_types)}))
	var allowed_elements: Array = SafeTypeUtils.array(rules.get("allowed_elements"))
	if not allowed_elements.is_empty():
		parts.append(Loc.t("academy.flow.allowed_elements", {"values": ", ".join(allowed_elements)}))
	var min_summons: int = SafeTypeUtils.int_val(rules.get("min_summons"))
	if min_summons > 0:
		parts.append(Loc.t("academy.flow.min_summons", {"count": min_summons}))
	var min_spells: int = SafeTypeUtils.int_val(rules.get("min_spells"))
	if min_spells > 0:
		parts.append(Loc.t("academy.flow.min_spells", {"count": min_spells}))
	var max_cards: int = SafeTypeUtils.int_val(rules.get("max_deck_size"))
	if max_cards > 0:
		parts.append(Loc.t("academy.flow.max_cards", {"count": max_cards}))
	return "; ".join(parts)

func _grant_label(grant: Dictionary) -> String:
	if SafeTypeUtils.string(grant.get("kind")) == "card":
		var card: Dictionary = CardCatalogApi.get_card_as_dict(SafeTypeUtils.string(grant.get("id")))
		return SafeTypeUtils.string(card.get("card_name"), SafeTypeUtils.string(grant.get("id")))
	return SafeTypeUtils.string(grant.get("id"), SafeTypeUtils.string(grant.get("kind"), Loc.t("academy.flow.reward")))
