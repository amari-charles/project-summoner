extends Control
class_name AcademyCourseFlow

@onready var back_button: Button = %BackButton
@onready var title_label: Label = %TitleLabel
@onready var activity_graph: AcademyActivityGraph = %ActivityGraph
@onready var enroll_button: Button = %EnrollButton

var _course_id: String = ""
var _course: Dictionary = {}

func _ready() -> void:
	back_button.text = "←"
	back_button.tooltip_text = Loc.t("academy.class_hall.title")
	back_button.accessibility_name = Loc.t("academy.class_hall.title")
	back_button.pressed.connect(_go_back)
	enroll_button.pressed.connect(_enroll)
	activity_graph.activity_selected.connect(_inspect_activity)
	_course_id = BattleContext.academy_course_id
	_refresh()

func _refresh() -> void:
	_course = CampaignApi.get_academy_course_flow_state(_course_id)
	if _course.is_empty():
		_go_back()
		return
	title_label.text = Loc.t(SafeTypeUtils.string(_course.get("name_key")))
	enroll_button.visible = not SafeTypeUtils.bool_val(_course.get("is_enrolled")) \
		and not SafeTypeUtils.bool_val(_course.get("is_completed"))
	enroll_button.disabled = not SafeTypeUtils.bool_val(_course.get("is_available"))
	enroll_button.text = Loc.t("academy.hub.enroll")
	activity_graph.set_activities(SafeTypeUtils.array(_course.get("activities")))

func _enroll() -> void:
	if CampaignApi.enroll_academy_course(_course_id):
		_refresh()

func _inspect_activity(activity_id: String) -> void:
	var activity: Dictionary = _find_activity(activity_id)
	if activity.is_empty() or SafeTypeUtils.bool_val(activity.get("is_locked")):
		return
	if SafeTypeUtils.bool_val(activity.get("is_completed")) \
		and not SafeTypeUtils.bool_val(activity.get("repeatable")):
		return
	BattleContext.select_academy_activity(_course_id, activity_id)
	SceneManager.transition_to(SceneManager.SCENE_ACADEMY_ACTIVITY_PREPARATION)

func _go_back() -> void:
	SceneManager.transition_to(SceneManager.SCENE_ACADEMY_CLASS_HALL)

func _find_activity(activity_id: String) -> Dictionary:
	for value: Variant in SafeTypeUtils.array(_course.get("activities")):
		var activity: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(activity.get("id")) == activity_id:
			return activity
	return {}
