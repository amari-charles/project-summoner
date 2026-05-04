extends Control
class_name AcademyCoursePath

@onready var title_label: Label = %TitleLabel
@onready var status_label: Label = %StatusLabel
@onready var exit_button: Button = %ExitButton
@onready var path_scroll: ScrollContainer = %PathScroll
@onready var path_canvas: Control = %PathCanvas
@onready var rewards_label: Label = %RewardsLabel

const NODE_SIZE: Vector2 = Vector2(118, 118)
const NODE_GAP: float = 230.0
const MAP_PADDING: Vector2 = Vector2(260.0, 180.0)
const PAN_THRESHOLD: float = 5.0
const COLOR_NODE_DONE: Color = Color(0.34, 0.60, 0.38, 1.0)
const COLOR_NODE_CURRENT: Color = Color(0.78, 0.59, 0.24, 1.0)
const COLOR_NODE_LOCKED: Color = Color(0.16, 0.17, 0.19, 1.0)
const COLOR_LINE_DONE: Color = Color(0.42, 0.76, 0.46, 1.0)
const COLOR_LINE_LOCKED: Color = Color(0.34, 0.35, 0.38, 1.0)
const COLOR_TEXT_MUTED: Color = Color(0.72, 0.75, 0.78, 1.0)

var _course_id: String = ""
var _course: Dictionary = {}
var _is_panning: bool = false
var _pan_start_position: Vector2 = Vector2.ZERO
var _last_mouse_position: Vector2 = Vector2.ZERO

func _ready() -> void:
	exit_button.text = Loc.t("academy.location.exit")
	exit_button.pressed.connect(_on_exit_pressed)

	_course_id = BattleContext.academy_course_id
	if _course_id.is_empty():
		call_deferred("_on_exit_pressed")
		return

	if Campaign.has_signal("CampaignProgressChanged"):
		Campaign.connect("CampaignProgressChanged", _refresh)

	_refresh()

func _refresh() -> void:
	_course = CampaignApi.get_academy_course(_course_id)
	if _course.is_empty():
		_on_exit_pressed()
		return

	title_label.text = _course_name(_course)
	status_label.text = _course_status_text(_course)
	rewards_label.text = _reward_preview_text(SafeTypeUtils.array(_course.get("reward_previews")))
	_render_path()
	call_deferred("_center_path_view")

func _render_path() -> void:
	_clear_children(path_canvas)

	var activities: Array = SafeTypeUtils.array(_course.get("activities"))
	var activity_index: int = SafeTypeUtils.int_val(_course.get("activity_index"), 0)
	if activities.is_empty():
		return

	var total_width: float = ((activities.size() - 1) * NODE_GAP) + NODE_SIZE.x
	var viewport_size: Vector2 = _path_viewport_size()
	var content_size: Vector2 = Vector2(
		maxf(viewport_size.x + MAP_PADDING.x * 2.0, total_width + MAP_PADDING.x * 2.0),
		maxf(viewport_size.y + MAP_PADDING.y * 2.0, NODE_SIZE.y + MAP_PADDING.y * 2.0)
	)
	path_canvas.custom_minimum_size = content_size
	path_canvas.size = content_size

	var start_x: float = (content_size.x - total_width) * 0.5
	var node_y: float = (content_size.y - NODE_SIZE.y) * 0.5

	for index: int in range(activities.size() - 1):
		var line: Line2D = Line2D.new()
		line.width = 6.0
		line.default_color = COLOR_LINE_DONE if index < activity_index else COLOR_LINE_LOCKED
		line.add_point(Vector2(start_x + (index * NODE_GAP) + NODE_SIZE.x, node_y + NODE_SIZE.y * 0.5))
		line.add_point(Vector2(start_x + ((index + 1) * NODE_GAP), node_y + NODE_SIZE.y * 0.5))
		path_canvas.add_child(line)

	for index: int in range(activities.size()):
		var activity: Dictionary = SafeTypeUtils.dict(activities[index])
		var node: Control = _build_activity_node(activity, index, activity_index)
		node.position = Vector2(start_x + (index * NODE_GAP), node_y)
		path_canvas.add_child(node)

func _center_path_view() -> void:
	var max_x: int = max(0, int(path_canvas.size.x - path_scroll.size.x))
	var max_y: int = max(0, int(path_canvas.size.y - path_scroll.size.y))
	path_scroll.scroll_horizontal = max_x / 2
	path_scroll.scroll_vertical = max_y / 2

func _path_viewport_size() -> Vector2:
	var size: Vector2 = path_scroll.size
	if size.x > 1.0 and size.y > 1.0:
		return size
	return get_viewport_rect().size

func _input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event as InputEventMouseButton
		if mouse_event.button_index == MOUSE_BUTTON_LEFT:
			if mouse_event.pressed:
				var scroll_rect: Rect2 = path_scroll.get_global_rect()
				if scroll_rect.has_point(mouse_event.position):
					_pan_start_position = mouse_event.position
					_last_mouse_position = mouse_event.position
			else:
				_is_panning = false
	elif event is InputEventMouseMotion:
		var motion_event: InputEventMouseMotion = event as InputEventMouseMotion
		if motion_event.button_mask & MOUSE_BUTTON_MASK_LEFT:
			if not _is_panning:
				var distance: float = motion_event.position.distance_to(_pan_start_position)
				var scroll_rect: Rect2 = path_scroll.get_global_rect()
				if distance > PAN_THRESHOLD and scroll_rect.has_point(motion_event.position):
					_is_panning = true
					_last_mouse_position = motion_event.position

			if _is_panning:
				var delta: Vector2 = motion_event.position - _last_mouse_position
				path_scroll.scroll_horizontal -= int(delta.x)
				path_scroll.scroll_vertical -= int(delta.y)
				_last_mouse_position = motion_event.position
				get_viewport().set_input_as_handled()
		else:
			_pan_start_position = Vector2.ZERO

func _build_activity_node(activity: Dictionary, index: int, activity_index: int) -> Control:
	var is_done: bool = index < activity_index
	var is_current: bool = index == activity_index
	var is_locked: bool = index > activity_index

	var panel: PanelContainer = PanelContainer.new()
	panel.custom_minimum_size = NODE_SIZE
	panel.size = NODE_SIZE
	panel.add_theme_stylebox_override(
		"panel",
		_panel_style(COLOR_NODE_DONE if is_done else (COLOR_NODE_CURRENT if is_current else COLOR_NODE_LOCKED))
	)
	panel.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND if is_current else Control.CURSOR_ARROW
	if is_current:
		panel.gui_input.connect(func(event: InputEvent) -> void:
			if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
				_activate_activity(activity)
		)

	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 10)
	margin.add_theme_constant_override("margin_top", 10)
	margin.add_theme_constant_override("margin_right", 10)
	margin.add_theme_constant_override("margin_bottom", 10)
	panel.add_child(margin)

	var root: VBoxContainer = VBoxContainer.new()
	root.add_theme_constant_override("separation", 6)
	margin.add_child(root)

	var state: Label = Label.new()
	state.text = _node_state_text(is_done, is_current, is_locked)
	state.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	state.add_theme_font_size_override("font_size", 18)
	root.add_child(state)

	var title: Label = Label.new()
	title.text = _activity_name(activity)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	title.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	title.size_flags_vertical = Control.SIZE_EXPAND_FILL
	root.add_child(title)

	return panel

func _activate_activity(activity: Dictionary) -> void:
	var activity_type: String = SafeTypeUtils.string(activity.get("type"))
	if activity_type == "PracticeBattle" or activity_type == "AssessmentBattle":
		var activity_id: String = SafeTypeUtils.string(activity.get("id"), _course_id)
		var battle_config: Dictionary = SafeTypeUtils.dict(activity.get("battle_config"))
		BattleContext.configure_academy_battle(_course_id, activity_id, battle_config)
		SceneManager.transition_to(SceneManager.SCENE_BATTLE_3D)
	else:
		CampaignApi.complete_next_academy_activity(_course_id)
		_refresh()

func _node_state_text(is_done: bool, is_current: bool, is_locked: bool) -> String:
	if is_done:
		return Loc.t("academy.course_path.done")
	if is_current:
		return Loc.t("academy.course_path.current")
	if is_locked:
		return Loc.t("academy.course_path.locked")
	return ""

func _course_status_text(course: Dictionary) -> String:
	var index: int = SafeTypeUtils.int_val(course.get("activity_index"), 0)
	var activities: Array = SafeTypeUtils.array(course.get("activities"))
	if index >= activities.size():
		return Loc.t("academy.course_path.complete")
	var next_activity: Dictionary = SafeTypeUtils.dict(course.get("next_activity"))
	return Loc.t(
		"academy.course_path.status",
		{
			"index": index + 1,
			"total": activities.size(),
			"activity": _activity_name(next_activity),
		}
	)

func _activity_name(activity: Dictionary) -> String:
	var label_key: String = SafeTypeUtils.string(activity.get("label_key"))
	return Loc.t(label_key) if not label_key.is_empty() else SafeTypeUtils.string(activity.get("type"))

func _course_name(course: Dictionary) -> String:
	var name_key: String = SafeTypeUtils.string(course.get("name_key"))
	return Loc.t(name_key) if not name_key.is_empty() else SafeTypeUtils.string(course.get("id"))

func _reward_preview_text(rewards: Array) -> String:
	var labels: Array[String] = []
	for item: Variant in rewards:
		var reward: Dictionary = SafeTypeUtils.dict(item)
		var label_key: String = SafeTypeUtils.string(reward.get("label_key"))
		if not label_key.is_empty():
			labels.append(Loc.t(label_key))
	return Loc.t("academy.hub.rewards", {"rewards": ", ".join(labels)})

func _panel_style(bg: Color) -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = bg
	style.border_color = Color(0.86, 0.72, 0.40, 1.0)
	style.set_border_width_all(2)
	style.set_corner_radius_all(16)
	return style

func _clear_children(node: Node) -> void:
	for child: Node in node.get_children():
		child.queue_free()

func _on_exit_pressed() -> void:
	SceneManager.transition_to(SceneManager.SCENE_ACADEMY_CLASS_HALL)
