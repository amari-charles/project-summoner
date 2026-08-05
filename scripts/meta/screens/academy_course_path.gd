extends Control
class_name AcademyCoursePath

@onready var title_label: Label = %TitleLabel
@onready var status_label: Label = %StatusLabel
@onready var exit_button: Button = %ExitButton
@onready var path_scroll: ScrollContainer = %PathScroll
@onready var path_canvas: Control = %PathCanvas
@onready var rewards_label: Label = %RewardsLabel
@onready var activity_modal: Control = %ActivityModal
@onready var modal_panel: PanelContainer = %ModalPanel
@onready var modal_title_label: Label = %ModalTitleLabel
@onready var modal_type_label: Label = %ModalTypeLabel
@onready var modal_body_label: Label = %ModalBodyLabel
@onready var modal_cancel_button: Button = %ModalCancelButton
@onready var modal_continue_button: Button = %ModalContinueButton

var _reward_modal: Control
var _reward_title_label: Label
var _reward_body_label: Label
var _reward_items_container: VBoxContainer
var _reward_continue_button: Button
var _modal_edit_deck_button: Button
var _pending_reward_offer: Dictionary = {}
var _selected_reward_option_ids: Array[String] = []
var _reward_option_buttons: Dictionary = {}

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
var _pending_activity: Dictionary = {}
var _scene_transition_override: Callable = Callable()
var _claim_reward_override: Callable = Callable()

func _ready() -> void:
	exit_button.text = Loc.t("academy.location.exit")
	exit_button.pressed.connect(_on_exit_pressed)
	modal_panel.add_theme_stylebox_override("panel", _panel_style(Color(0.12, 0.105, 0.085, 1.0)))
	modal_cancel_button.text = Loc.t("academy.course_path.cancel")
	modal_continue_button.text = Loc.t("academy.course_path.continue")
	modal_cancel_button.pressed.connect(_hide_activity_modal)
	modal_continue_button.pressed.connect(_on_modal_continue_pressed)
	_build_activity_modal_actions()
	_build_reward_modal()

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
	call_deferred("_show_reward_summary_if_available")

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
	if activity_modal.visible or (_reward_modal != null and _reward_modal.visible):
		return

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
	var is_done: bool = SafeTypeUtils.bool_val(activity.get("is_completed"), index < activity_index)
	var is_current: bool = SafeTypeUtils.bool_val(activity.get("is_current"), index == activity_index)
	var is_locked: bool = SafeTypeUtils.bool_val(activity.get("is_locked"), index > activity_index)

	var panel: PanelContainer = PanelContainer.new()
	panel.custom_minimum_size = NODE_SIZE
	panel.size = NODE_SIZE
	panel.add_theme_stylebox_override(
		"panel",
		_panel_style(COLOR_NODE_DONE if is_done else (COLOR_NODE_CURRENT if is_current else COLOR_NODE_LOCKED))
	)
	panel.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	panel.gui_input.connect(func(event: InputEvent) -> void:
		if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
			_show_activity_modal(activity)
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

func _show_activity_modal(activity: Dictionary) -> void:
	_pending_activity = activity.duplicate()
	modal_title_label.text = _course_name(_course)
	modal_type_label.text = _activity_type_text(activity)
	modal_body_label.text = _activity_modal_body(activity)
	modal_continue_button.visible = SafeTypeUtils.bool_val(activity.get("can_start"), false)
	if _modal_edit_deck_button != null:
		_modal_edit_deck_button.visible = _activity_uses_battle(activity)
	activity_modal.visible = true

func _hide_activity_modal() -> void:
	_pending_activity = {}
	activity_modal.visible = false

func _on_modal_continue_pressed() -> void:
	var activity: Dictionary = _pending_activity.duplicate()
	_hide_activity_modal()
	if activity.is_empty():
		return
	_start_activity(activity)

func _start_activity(activity: Dictionary) -> void:
	var activity_type: String = SafeTypeUtils.string(activity.get("type"))
	if activity_type == "PracticeBattle" or activity_type == "AssessmentBattle":
		var activity_id: String = SafeTypeUtils.string(activity.get("id"), _course_id)
		var launch_state: Dictionary = CampaignApi.get_academy_activity_launch_state(_course_id, activity_id)
		var deck_validation: Dictionary = SafeTypeUtils.dict(launch_state.get("deck_validation"))
		if not SafeTypeUtils.bool_val(deck_validation.get("is_valid"), true):
			_pending_activity = launch_state
			modal_body_label.text = _activity_modal_body(launch_state)
			activity_modal.visible = true
			return

		var battle_config: Dictionary = CampaignApi.resolve_academy_activity_battle_config(_course_id, activity_id)
		if battle_config.is_empty():
			battle_config = SafeTypeUtils.dict(activity.get("battle_config"))
		BattleContext.configure_academy_battle(_course_id, activity_id, battle_config)
		_transition_to(SceneManager.SCENE_BATTLE_3D)
	else:
		var activity_id: String = SafeTypeUtils.string(activity.get("id"))
		CampaignApi.complete_academy_activity(_course_id, activity_id, true)
		_refresh()

func _build_activity_modal_actions() -> void:
	_modal_edit_deck_button = Button.new()
	_modal_edit_deck_button.custom_minimum_size = Vector2(130, 40)
	_modal_edit_deck_button.text = Loc.t("academy.course_path.edit_deck")
	_modal_edit_deck_button.visible = false
	_modal_edit_deck_button.pressed.connect(_on_edit_deck_pressed)

	var action_parent: Node = modal_continue_button.get_parent()
	if action_parent != null:
		action_parent.add_child(_modal_edit_deck_button)
		action_parent.move_child(_modal_edit_deck_button, maxi(0, modal_continue_button.get_index()))

func _on_edit_deck_pressed() -> void:
	BattleContext.select_academy_course(_course_id)
	NavigationContext.push_return(SceneManager.SCENE_ACADEMY_COURSE_PATH)
	_transition_to(SceneManager.SCENE_COLLECTION_SCREEN)

func _build_reward_modal() -> void:
	_reward_modal = Control.new()
	_reward_modal.visible = false
	_reward_modal.mouse_filter = Control.MOUSE_FILTER_STOP
	_reward_modal.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(_reward_modal)

	var scrim: ColorRect = ColorRect.new()
	scrim.color = Color(0, 0, 0, 0.62)
	scrim.mouse_filter = Control.MOUSE_FILTER_STOP
	scrim.set_anchors_preset(Control.PRESET_FULL_RECT)
	_reward_modal.add_child(scrim)

	var center: CenterContainer = CenterContainer.new()
	center.mouse_filter = Control.MOUSE_FILTER_PASS
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	_reward_modal.add_child(center)

	var panel: PanelContainer = PanelContainer.new()
	panel.custom_minimum_size = Vector2(520, 240)
	panel.add_theme_stylebox_override("panel", _panel_style(Color(0.105, 0.12, 0.10, 1.0)))
	center.add_child(panel)

	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 28)
	margin.add_theme_constant_override("margin_top", 24)
	margin.add_theme_constant_override("margin_right", 28)
	margin.add_theme_constant_override("margin_bottom", 24)
	panel.add_child(margin)

	var root: VBoxContainer = VBoxContainer.new()
	root.add_theme_constant_override("separation", 14)
	margin.add_child(root)

	_reward_title_label = Label.new()
	_reward_title_label.text = Loc.t("academy.course_path.reward_title")
	_reward_title_label.add_theme_font_size_override("font_size", 30)
	root.add_child(_reward_title_label)

	_reward_body_label = Label.new()
	_reward_body_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_reward_body_label.add_theme_font_size_override("font_size", 16)
	root.add_child(_reward_body_label)

	_reward_items_container = VBoxContainer.new()
	_reward_items_container.add_theme_constant_override("separation", 6)
	_reward_items_container.size_flags_vertical = Control.SIZE_EXPAND_FILL
	root.add_child(_reward_items_container)

	var actions: HBoxContainer = HBoxContainer.new()
	actions.alignment = BoxContainer.ALIGNMENT_END
	root.add_child(actions)

	_reward_continue_button = Button.new()
	_reward_continue_button.custom_minimum_size = Vector2(150, 40)
	_reward_continue_button.text = Loc.t("academy.course_path.continue")
	_reward_continue_button.pressed.connect(_on_reward_continue_pressed)
	actions.add_child(_reward_continue_button)

func _show_reward_summary_if_available() -> void:
	var summary: Dictionary = CampaignApi.consume_last_academy_completion_summary()
	if summary.is_empty():
		_show_pending_reward_if_available()
		return

	var rewards: Array = SafeTypeUtils.array(summary.get("granted_rewards"))
	if rewards.is_empty():
		_show_pending_reward_if_available()
		return

	_show_reward_modal(summary, rewards)

func _show_pending_reward_if_available() -> void:
	for item: Variant in SafeTypeUtils.array(_course.get("reward_previews")):
		var offer: Dictionary = SafeTypeUtils.dict(item)
		if SafeTypeUtils.string(offer.get("status")) == "pending":
			_show_pending_reward_offer(offer)
			return

func _show_pending_reward_offer(offer: Dictionary) -> void:
	_pending_reward_offer = offer
	_selected_reward_option_ids.clear()
	_reward_option_buttons.clear()
	_clear_children(_reward_items_container)
	_reward_title_label.text = Loc.t("academy.course_path.reward_title")
	_reward_body_label.text = Loc.t("academy.course_path.reward_choose")
	for item: Variant in SafeTypeUtils.array(offer.get("options")):
		var option: Dictionary = SafeTypeUtils.dict(item)
		var option_id: String = SafeTypeUtils.string(option.get("option_id"))
		if option_id.is_empty():
			continue
		var button: Button = Button.new()
		button.toggle_mode = true
		button.text = _reward_option_name(option)
		button.pressed.connect(_on_reward_option_toggled.bind(option_id))
		_reward_items_container.add_child(button)
		_reward_option_buttons[option_id] = button
	_reward_continue_button.disabled = true
	_reward_modal.visible = true

func _reward_option_name(option: Dictionary) -> String:
	var label_key: String = SafeTypeUtils.string(option.get("label_key"))
	if not label_key.is_empty():
		return Loc.t(label_key)
	var grants: Array = SafeTypeUtils.array(option.get("grants"))
	if not grants.is_empty():
		return _granted_reward_name(SafeTypeUtils.dict(grants[0]))
	return SafeTypeUtils.string(option.get("option_id"))

func _on_reward_option_toggled(option_id: String) -> void:
	var button: Button = _reward_option_buttons.get(option_id) as Button
	if button == null:
		return
	if button.button_pressed:
		if not _selected_reward_option_ids.has(option_id):
			_selected_reward_option_ids.append(option_id)
	else:
		_selected_reward_option_ids.erase(option_id)
	var choose_count: int = SafeTypeUtils.int_val(_pending_reward_offer.get("choose_count"), 1)
	_reward_continue_button.disabled = _selected_reward_option_ids.size() != choose_count

func _on_reward_continue_pressed() -> void:
	if _pending_reward_offer.is_empty():
		_hide_reward_modal()
		_show_pending_reward_if_available()
		return
	var claim_id: String = SafeTypeUtils.string(_pending_reward_offer.get("claim_id"))
	var result: Dictionary = (
		SafeTypeUtils.dict(_claim_reward_override.call(claim_id, _selected_reward_option_ids))
		if _claim_reward_override.is_valid()
		else CampaignApi.claim_academy_reward(claim_id, _selected_reward_option_ids)
	)
	if not SafeTypeUtils.bool_val(result.get("success")):
		return
	_pending_reward_offer.clear()
	_selected_reward_option_ids.clear()
	_reward_modal.visible = false
	_refresh()

func _show_reward_modal(summary: Dictionary, rewards: Array) -> void:
	_pending_reward_offer.clear()
	_reward_continue_button.disabled = false
	_clear_children(_reward_items_container)
	var completed_course: bool = SafeTypeUtils.bool_val(summary.get("completed_course"), false)
	_reward_body_label.text = Loc.t(
		"academy.course_path.reward_body_course" if completed_course else "academy.course_path.reward_body_activity"
	)

	for item: Variant in rewards:
		var reward: Dictionary = SafeTypeUtils.dict(item)
		var label: Label = Label.new()
		label.text = Loc.t("academy.course_path.reward_item", {"reward": _granted_reward_name(reward)})
		label.add_theme_font_size_override("font_size", 18)
		label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		_reward_items_container.add_child(label)

	_reward_modal.visible = true

func _hide_reward_modal() -> void:
	_pending_reward_offer.clear()
	_reward_modal.visible = false

func _granted_reward_name(reward: Dictionary) -> String:
	var label_key: String = SafeTypeUtils.string(reward.get("label_key"))
	if not label_key.is_empty():
		return Loc.t(label_key)

	var card_id: String = SafeTypeUtils.string(reward.get("card_id"))
	if card_id.is_empty() and SafeTypeUtils.string(reward.get("kind")) == "card":
		card_id = SafeTypeUtils.string(reward.get("id"))
	if not card_id.is_empty():
		var card_data: Dictionary = CardCatalogApi.get_card_as_dict(card_id)
		return SafeTypeUtils.string(card_data.get("card_name"), card_id)

	return SafeTypeUtils.string(reward.get("kind"), Loc.t("academy.course_path.reward_fallback"))

func _activity_type_text(activity: Dictionary) -> String:
	var activity_type: String = SafeTypeUtils.string(activity.get("type"))
	if activity_type == "PracticeBattle":
		return Loc.t("academy.activity.practice")
	if activity_type == "AssessmentBattle":
		return Loc.t("academy.activity.assessment")
	return Loc.t("academy.activity.lesson")

func _activity_modal_body(activity: Dictionary) -> String:
	var parts: Array[String] = []
	var course_description: String = _course_description(_course)
	if not course_description.is_empty():
		parts.append(course_description)

	var activity_type: String = SafeTypeUtils.string(activity.get("type"))
	if SafeTypeUtils.bool_val(activity.get("is_locked")):
		parts.append(Loc.t("academy.course_path.locked_body"))
		return "\n\n".join(parts)
	if SafeTypeUtils.bool_val(activity.get("is_completed")) and not SafeTypeUtils.bool_val(activity.get("can_start")):
		parts.append(Loc.t("academy.course_path.completed_body"))
		return "\n\n".join(parts)
	if activity_type == "PracticeBattle":
		parts.append(Loc.t("academy.course_path.practice_body"))
		_append_activity_limitations(parts, activity)
		return "\n\n".join(parts)
	if activity_type == "AssessmentBattle":
		parts.append(Loc.t("academy.course_path.assessment_body"))
		_append_activity_limitations(parts, activity)
		return "\n\n".join(parts)
	parts.append(Loc.t("academy.course_path.lesson_body"))
	_append_activity_limitations(parts, activity)
	return "\n\n".join(parts)

func _append_activity_limitations(parts: Array[String], activity: Dictionary) -> void:
	var summaries: Array = SafeTypeUtils.array(activity.get("limitation_summary"))
	if not summaries.is_empty():
		var summary_lines: Array[String] = []
		for item: Variant in summaries:
			var line: String = SafeTypeUtils.string(item)
			if not line.is_empty():
				summary_lines.append("- %s" % line)
		if not summary_lines.is_empty():
			parts.append("%s\n%s" % [Loc.t("academy.course_path.class_rules"), "\n".join(summary_lines)])

	var deck_validation: Dictionary = SafeTypeUtils.dict(activity.get("deck_validation"))
	if not deck_validation.is_empty():
		var limitations: Dictionary = SafeTypeUtils.dict(activity.get("limitations"))
		var has_rules: bool = SafeTypeUtils.bool_val(limitations.get("has_rules"), false)
		var validation_message: String = SafeTypeUtils.string(deck_validation.get("message"))
		var invalid_reasons: Array = SafeTypeUtils.array(deck_validation.get("invalid_reasons"))
		if not has_rules and invalid_reasons.is_empty():
			return
		var status_lines: Array[String] = []
		if not validation_message.is_empty():
			status_lines.append(validation_message)
		for item: Variant in invalid_reasons:
			var reason: String = SafeTypeUtils.string(item)
			if not reason.is_empty():
				status_lines.append("- %s" % reason)
		if not status_lines.is_empty():
			parts.append("%s\n%s" % [Loc.t("academy.course_path.deck_status"), "\n".join(status_lines)])

func _activity_uses_battle(activity: Dictionary) -> bool:
	var activity_type: String = SafeTypeUtils.string(activity.get("type"))
	return activity_type == "PracticeBattle" or activity_type == "AssessmentBattle"

func _node_state_text(is_done: bool, is_current: bool, is_locked: bool) -> String:
	if is_done:
		return Loc.t("academy.course_path.done")
	if is_current:
		return Loc.t("academy.course_path.current")
	if is_locked:
		return Loc.t("academy.course_path.locked")
	return ""

func _course_status_text(course: Dictionary) -> String:
	if SafeTypeUtils.bool_val(course.get("is_completed")):
		return Loc.t("academy.course_path.complete")

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

func _course_description(course: Dictionary) -> String:
	var description_key: String = SafeTypeUtils.string(course.get("description_key"))
	return Loc.t(description_key) if not description_key.is_empty() else ""

func _reward_preview_text(rewards: Array) -> String:
	var labels: Array[String] = []
	for item: Variant in rewards:
		var reward: Dictionary = SafeTypeUtils.dict(item)
		if SafeTypeUtils.string(reward.get("status")) == "claimed":
			continue
		var label_key: String = SafeTypeUtils.string(reward.get("label_key"))
		if label_key.is_empty():
			label_key = SafeTypeUtils.string(reward.get("category_key"))
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
	_transition_to(SceneManager.SCENE_ACADEMY_CLASS_HALL)

func _transition_to(scene_path: String) -> void:
	if _scene_transition_override.is_valid():
		_scene_transition_override.call(scene_path)
		return
	SceneManager.transition_to(scene_path)
