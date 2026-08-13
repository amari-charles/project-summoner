extends Control
class_name AcademyActivityGraph

signal activity_selected(activity_id: String)

const NODE_SIZE: float = 123.0
const DEPTH_SPACING: float = 254.0
const BRANCH_SPACING: float = 176.0
const GRAPH_CENTER_X_RATIO: float = 0.4658

const ICON_PRACTICE: Texture2D = preload("res://assets/icons/card_types/wizard_hat.png")
const ICON_ASSESSMENT: Texture2D = preload("res://assets/icons/card_types/sword.png")
const ICON_STANDARD: Texture2D = preload("res://assets/ui/kenny/Vector/Extra/icon_play_dark.svg")
const ICON_COMPLETED: Texture2D = preload("res://assets/ui/kenny/Vector/Green/icon_checkmark.svg")

const STATE_LOCKED: String = "locked"
const STATE_AVAILABLE: String = "available"
const STATE_ACTIVE: String = "active"
const STATE_COMPLETED: String = "completed"

var _activities: Array[Dictionary] = []
var _buttons_by_id: Dictionary = {}
var _edges: Array[Dictionary] = []


func _ready() -> void:
	resized.connect(_layout_graph)


func set_activities(activities: Array) -> void:
	_clear_graph()
	for value: Variant in activities:
		var activity: Dictionary = SafeTypeUtils.dict(value)
		if not activity.is_empty():
			_activities.append(activity)
	for activity: Dictionary in _activities:
		var activity_id: String = SafeTypeUtils.string(activity.get("id"))
		if activity_id.is_empty():
			continue
		var button: Button = _create_activity_button(activity)
		button.name = "Activity_%s" % activity_id
		add_child(button)
		_buttons_by_id[activity_id] = button
	_layout_graph()


func _clear_graph() -> void:
	_activities.clear()
	_buttons_by_id.clear()
	_edges.clear()
	for child: Node in get_children():
		remove_child(child)
		child.queue_free()
	queue_redraw()


func _layout_graph() -> void:
	if _activities.is_empty() or size.x <= 0.0 or size.y <= 0.0:
		return

	var depth_by_id: Dictionary = _build_depth_map()
	var ids_by_depth: Dictionary = {}
	var max_depth: int = 0
	for activity: Dictionary in _activities:
		var activity_id: String = SafeTypeUtils.string(activity.get("id"))
		var depth: int = SafeTypeUtils.int_val(depth_by_id.get(activity_id, 0))
		max_depth = maxi(max_depth, depth)
		if not ids_by_depth.has(depth):
			ids_by_depth[depth] = []
		var depth_ids: Array = ids_by_depth[depth]
		depth_ids.append(activity_id)
		ids_by_depth[depth] = depth_ids

	var graph_center_x: float = size.x * GRAPH_CENTER_X_RATIO
	var first_center_x: float = graph_center_x - float(max_depth) * DEPTH_SPACING * 0.5
	var graph_center_y: float = size.y * 0.5
	for depth: int in range(max_depth + 1):
		var ids: Array = SafeTypeUtils.array(ids_by_depth.get(depth, []))
		for branch_index: int in range(ids.size()):
			var activity_id: String = SafeTypeUtils.string(ids[branch_index])
			var button: Button = _buttons_by_id.get(activity_id) as Button
			if button == null:
				continue
			var branch_offset: float = (float(branch_index) - float(ids.size() - 1) * 0.5) * BRANCH_SPACING
			button.position = Vector2(
				first_center_x + float(depth) * DEPTH_SPACING - NODE_SIZE * 0.5,
				graph_center_y + branch_offset - NODE_SIZE * 0.5
			)

	_rebuild_edges()


func _build_depth_map() -> Dictionary:
	var depth_by_id: Dictionary = {}
	for activity: Dictionary in _activities:
		_resolve_depth(SafeTypeUtils.string(activity.get("id")), depth_by_id, {})
	return depth_by_id


func _resolve_depth(activity_id: String, depths: Dictionary, visiting: Dictionary) -> int:
	if depths.has(activity_id):
		return SafeTypeUtils.int_val(depths[activity_id])
	if visiting.has(activity_id):
		return 0
	visiting[activity_id] = true
	var activity: Dictionary = _activity_by_id(activity_id)
	var depth: int = 0
	for prerequisite_var: Variant in SafeTypeUtils.array(activity.get("prerequisites")):
		var prerequisite_id: String = SafeTypeUtils.string(prerequisite_var)
		if _buttons_by_id.has(prerequisite_id):
			depth = maxi(depth, _resolve_depth(prerequisite_id, depths, visiting) + 1)
	visiting.erase(activity_id)
	depths[activity_id] = depth
	return depth


func _rebuild_edges() -> void:
	_edges.clear()
	for activity: Dictionary in _activities:
		var activity_id: String = SafeTypeUtils.string(activity.get("id"))
		var to_button: Button = _buttons_by_id.get(activity_id) as Button
		if to_button == null:
			continue
		for prerequisite_var: Variant in SafeTypeUtils.array(activity.get("prerequisites")):
			var prerequisite_id: String = SafeTypeUtils.string(prerequisite_var)
			var from_button: Button = _buttons_by_id.get(prerequisite_id) as Button
			if from_button == null:
				continue
			_edges.append({
				"from": from_button.position + Vector2(NODE_SIZE, NODE_SIZE * 0.5),
				"to": to_button.position + Vector2(0.0, NODE_SIZE * 0.5),
				"complete": _activity_state(_activity_by_id(prerequisite_id)) == STATE_COMPLETED,
			})
	queue_redraw()


func _draw() -> void:
	for edge: Dictionary in _edges:
		var color: Color = Color(0.34, 0.34, 0.38, 0.9)
		if SafeTypeUtils.bool_val(edge.get("complete")):
			color = Color(0.28, 0.67, 0.42, 0.95)
		draw_line(edge.get("from", Vector2.ZERO), edge.get("to", Vector2.ZERO), color, 8.0, true)


func _create_activity_button(activity: Dictionary) -> Button:
	var activity_id: String = SafeTypeUtils.string(activity.get("id"))
	var state: String = _activity_state(activity)
	var selectable: bool = state != STATE_LOCKED and (
		state != STATE_COMPLETED or SafeTypeUtils.bool_val(activity.get("repeatable"))
	)
	var activity_name: String = Loc.t(SafeTypeUtils.string(activity.get("label_key")))

	var button: Button = Button.new()
	button.custom_minimum_size = Vector2(NODE_SIZE, NODE_SIZE)
	button.size = Vector2(NODE_SIZE, NODE_SIZE)
	button.text = ""
	button.tooltip_text = activity_name
	button.accessibility_name = activity_name
	button.accessibility_description = _state_accessibility_label(state)
	button.focus_mode = Control.FOCUS_ALL if selectable else Control.FOCUS_NONE
	button.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND if selectable else Control.CURSOR_ARROW
	if selectable:
		button.pressed.connect(activity_selected.emit.bind(activity_id))

	_apply_state_style(button, state)

	var center: CenterContainer = CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	center.mouse_filter = Control.MOUSE_FILTER_IGNORE
	button.add_child(center)

	var icon: TextureRect = TextureRect.new()
	icon.texture = _activity_icon(activity)
	icon.custom_minimum_size = Vector2(58.0, 58.0)
	icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon.modulate = Color(0.22, 0.22, 0.24, 1.0) if state != STATE_LOCKED else Color(0.46, 0.46, 0.49, 1.0)
	icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	center.add_child(icon)

	if state == STATE_COMPLETED:
		var badge: TextureRect = TextureRect.new()
		badge.texture = ICON_COMPLETED
		badge.position = Vector2(84.0, 84.0)
		badge.size = Vector2(28.0, 28.0)
		badge.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		badge.mouse_filter = Control.MOUSE_FILTER_IGNORE
		button.add_child(badge)

	return button


func _apply_state_style(button: Button, state: String) -> void:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.set_corner_radius_all(int(NODE_SIZE * 0.5))
	style.set_border_width_all(4)
	style.shadow_color = Color(0.0, 0.0, 0.0, 0.18)
	style.shadow_size = 5
	style.shadow_offset = Vector2(0.0, 3.0)
	match state:
		STATE_COMPLETED:
			style.bg_color = Color(0.72, 0.88, 0.77, 1.0)
			style.border_color = Color(0.22, 0.58, 0.36, 1.0)
		STATE_ACTIVE:
			style.bg_color = Color(0.72, 0.82, 0.98, 1.0)
			style.border_color = Color(0.18, 0.42, 0.82, 1.0)
			style.set_border_width_all(7)
		STATE_AVAILABLE:
			style.bg_color = Color(0.96, 0.84, 0.48, 1.0)
			style.border_color = Color(0.68, 0.48, 0.08, 1.0)
		_:
			style.bg_color = Color(0.82, 0.82, 0.84, 1.0)
			style.border_color = Color(0.56, 0.56, 0.60, 1.0)

	var hover: StyleBoxFlat = style.duplicate()
	hover.bg_color = style.bg_color.lightened(0.08)
	hover.border_color = style.border_color.lightened(0.10)
	var pressed: StyleBoxFlat = style.duplicate()
	pressed.bg_color = style.bg_color.darkened(0.08)
	button.add_theme_stylebox_override("normal", style)
	button.add_theme_stylebox_override("hover", hover)
	button.add_theme_stylebox_override("focus", hover)
	button.add_theme_stylebox_override("pressed", pressed)


func _activity_icon(activity: Dictionary) -> Texture2D:
	match SafeTypeUtils.string(activity.get("role")).to_lower():
		"assessment":
			return ICON_ASSESSMENT
		"practice":
			return ICON_PRACTICE
		_:
			return ICON_STANDARD


func _activity_state(activity: Dictionary) -> String:
	return SafeTypeUtils.string(activity.get("lifecycle_state"), STATE_LOCKED).to_lower()


func _activity_by_id(activity_id: String) -> Dictionary:
	for activity: Dictionary in _activities:
		if SafeTypeUtils.string(activity.get("id")) == activity_id:
			return activity
	return {}


func _state_accessibility_label(state: String) -> String:
	return Loc.t("academy.flow.state_%s" % state)
