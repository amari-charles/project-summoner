extends Control
class_name TraitDevelopmentOverlay

## Selected-trait development surface shared by owner screens. The caller chooses
## the trait that anchors the visible graph; unrelated trait families stay out of
## the overlay.

signal trait_acquired(trait_id: String)

const STATUS_OWNED: String = "owned"
const STATUS_AVAILABLE: String = "available"
const STATUS_LOCKED: String = "locked"
const CARD_CORE_PATH_ID: String = "__card_core__"
const NODE_SIZE: Vector2 = Vector2(72, 72)
const NODE_X_GAP: float = 132.0
const NODE_Y_GAP: float = 120.0
const CANVAS_PADDING: Vector2 = Vector2(70, 60)

@onready var shade: ColorRect = %Shade
@onready var panel: PanelContainer = %Panel
@onready var title_label: Label = %TitleLabel
@onready var points_label: Label = %PointsLabel
@onready var close_button: Button = %CloseButton
@onready var tree_canvas: TraitTreeCanvas = %TreeCanvas
@onready var node_detail_popover: PanelContainer = %NodeDetailPopover
@onready var detail_name: Label = %DetailName
@onready var detail_status: Label = %DetailStatus
@onready var detail_description: Label = %DetailDescription
@onready var detail_requirements: Label = %DetailRequirements
@onready var action_button: Button = %ActionButton
@onready var unlock_confirmation: ConfirmationDialog = %UnlockConfirmation

var _owner_type: String = ""
var _owner_id: String = ""
var _anchor_trait_id: String = ""
var _selected_trait_id: String = ""
var _node_lookup: Dictionary = {}
var _visible_nodes: Array = []
var _tree_controls: Dictionary = {}
var _pending_unlock_trait_id: String = ""
var _active_detail_trait_id: String = ""
var _popover_pinned: bool = false


func _ready() -> void:
	close_button.pressed.connect(close)
	action_button.pressed.connect(_on_action_pressed)
	unlock_confirmation.confirmed.connect(_on_unlock_confirmed)
	unlock_confirmation.title = Loc.t("ui.trait_tree.acquire_title")
	shade.gui_input.connect(_on_shade_gui_input)
	tree_canvas.gui_input.connect(_on_tree_canvas_gui_input)
	_style_surface()
	visible = false


func open_for_summoner(summoner_id: String, trait_id: String) -> void:
	_open("summoner", summoner_id, trait_id)


func open_for_card_core(card_instance_id: String) -> void:
	_open("card", card_instance_id, CARD_CORE_PATH_ID)


func open_for_card_trait(card_instance_id: String, trait_id: String) -> void:
	_open("card", card_instance_id, trait_id)


func close() -> void:
	visible = false
	_pending_unlock_trait_id = ""
	_active_detail_trait_id = ""
	_popover_pinned = false
	node_detail_popover.visible = false


func _open(owner_type: String, owner_id: String, trait_id: String) -> void:
	if owner_id.is_empty() or trait_id.is_empty():
		return
	_owner_type = owner_type
	_owner_id = owner_id
	_anchor_trait_id = trait_id
	_selected_trait_id = ""
	_popover_pinned = false
	visible = true
	_refresh()


func _refresh() -> void:
	var view_model: Dictionary = _get_tree_view_model()
	_node_lookup = _build_node_lookup(view_model)
	if _owner_type == "card" and _anchor_trait_id == CARD_CORE_PATH_ID:
		_visible_nodes = _extract_nodes(view_model, "progression_nodes")
	else:
		_visible_nodes = _connected_trait_family(_anchor_trait_id, _node_lookup)

	if _visible_nodes.is_empty():
		push_error("TraitDevelopmentOverlay: No path data for %s owner '%s', path '%s'" % [
			_owner_type, _owner_id, _anchor_trait_id
		])
		close()
		return

	if _owner_type == "card" and _anchor_trait_id == CARD_CORE_PATH_ID:
		title_label.text = Loc.t("ui.collection.core_path_name")
	else:
		var anchor_data: Dictionary = _node_lookup.get(_anchor_trait_id, {})
		title_label.text = str(anchor_data.get("name", _anchor_trait_id))

	var points_key: String = "ui.collection.card_points_count" if _owner_type == "card" else "ui.summoner_screen.upgrade_points_count"
	points_label.text = Loc.t(points_key, {"count": int(view_model.get("unspent_trait_points", 0))})
	node_detail_popover.visible = false
	_active_detail_trait_id = ""
	_render_graph()


func _get_tree_view_model() -> Dictionary:
	if _owner_type == "summoner":
		return TraitTreeApi.get_summoner_tree_view_model(_owner_id)
	return TraitTreeApi.get_card_tree_view_model(_owner_id)


func _build_node_lookup(view_model: Dictionary) -> Dictionary:
	var lookup: Dictionary = {}
	for collection_key: String in ["progression_nodes", "one_off_nodes"]:
		for node_var: Variant in SafeTypeUtils.array(view_model.get(collection_key, [])):
			if not node_var is Dictionary:
				continue
			var node_data: Dictionary = node_var
			var trait_id: String = str(node_data.get("id", ""))
			if not trait_id.is_empty():
				lookup[trait_id] = node_data
	return lookup


func _extract_nodes(view_model: Dictionary, collection_key: String) -> Array:
	var nodes: Array = []
	for node_var: Variant in SafeTypeUtils.array(view_model.get(collection_key, [])):
		if node_var is Dictionary:
			nodes.append(node_var)
	return nodes


func _connected_trait_family(anchor_id: String, lookup: Dictionary) -> Array:
	if not lookup.has(anchor_id):
		return []

	var included: Dictionary = {anchor_id: true}
	var frontier: Array[String] = [anchor_id]
	while not frontier.is_empty():
		var current_id: String = frontier.pop_front()
		var current: Dictionary = lookup.get(current_id, {})
		for prerequisite_var: Variant in SafeTypeUtils.array(current.get("prerequisites", [])):
			var prerequisite_id: String = str(prerequisite_var)
			if lookup.has(prerequisite_id) and not included.has(prerequisite_id):
				included[prerequisite_id] = true
				frontier.append(prerequisite_id)

		for candidate_id_var: Variant in lookup.keys():
			var candidate_id: String = str(candidate_id_var)
			if included.has(candidate_id):
				continue
			var candidate: Dictionary = lookup[candidate_id]
			if current_id in SafeTypeUtils.array(candidate.get("prerequisites", [])):
				included[candidate_id] = true
				frontier.append(candidate_id)

	var family: Array = []
	for trait_id_var: Variant in included.keys():
		family.append(lookup[str(trait_id_var)])
	return family


func _render_graph() -> void:
	for child: Node in tree_canvas.get_children():
		if child == node_detail_popover:
			continue
		child.queue_free()
	tree_canvas.clear_edges()
	_tree_controls.clear()

	var depth_groups: Dictionary = {}
	var max_depth: int = 0
	for node_var: Variant in _visible_nodes:
		var node_data: Dictionary = node_var
		var depth: int = int(node_data.get("depth", 0))
		max_depth = maxi(max_depth, depth)
		if not depth_groups.has(depth):
			depth_groups[depth] = []
		var group: Array = depth_groups[depth]
		group.append(node_data)
		depth_groups[depth] = group

	var widest_row: int = 1
	for depth_var: Variant in depth_groups.keys():
		widest_row = maxi(widest_row, (depth_groups[depth_var] as Array).size())
	var canvas_width: float = CANVAS_PADDING.x * 2.0 + float(widest_row - 1) * NODE_X_GAP + NODE_SIZE.x
	var canvas_height: float = CANVAS_PADDING.y * 2.0 + float(max_depth) * NODE_Y_GAP + NODE_SIZE.y
	tree_canvas.custom_minimum_size = Vector2(maxf(canvas_width, 940.0), maxf(canvas_height, 500.0))

	for depth: int in range(max_depth + 1):
		var row: Array = depth_groups.get(depth, [])
		row.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
			return str(a.get("name", a.get("id", ""))) < str(b.get("name", b.get("id", "")))
		)
		var row_width: float = float(maxi(row.size() - 1, 0)) * NODE_X_GAP + NODE_SIZE.x
		var start_x: float = (tree_canvas.custom_minimum_size.x - row_width) * 0.5
		for index: int in range(row.size()):
			var node_data: Dictionary = row[index]
			var node: Button = _create_tree_node(node_data)
			node.position = Vector2(start_x + float(index) * NODE_X_GAP, CANVAS_PADDING.y + float(max_depth - depth) * NODE_Y_GAP)
			tree_canvas.add_child(node)
			_tree_controls[str(node_data.get("id", ""))] = node

	var edges: Array = []
	for node_var: Variant in _visible_nodes:
		var node_data: Dictionary = node_var
		var to_id: String = str(node_data.get("id", ""))
		if not _tree_controls.has(to_id):
			continue
		for prerequisite_var: Variant in SafeTypeUtils.array(node_data.get("prerequisites", [])):
			var from_id: String = str(prerequisite_var)
			if not _tree_controls.has(from_id):
				continue
			var from_node: Control = _tree_controls[from_id]
			var to_node: Control = _tree_controls[to_id]
			edges.append({
				"from": from_node.position + Vector2(NODE_SIZE.x * 0.5, 0),
				"to": to_node.position + Vector2(NODE_SIZE.x * 0.5, NODE_SIZE.y),
				"color": Color(0.48, 0.50, 0.57, 0.85),
				"width": 2.5,
			})
	tree_canvas.set_edges(edges)
	tree_canvas.move_child(node_detail_popover, tree_canvas.get_child_count() - 1)


func _create_tree_node(node_data: Dictionary) -> Button:
	var trait_id: String = str(node_data.get("id", ""))
	var button: Button = Button.new()
	button.custom_minimum_size = NODE_SIZE
	button.text = ""
	button.pressed.connect(_on_node_pressed.bind(trait_id))
	button.mouse_entered.connect(_on_node_hovered.bind(trait_id))
	button.mouse_exited.connect(_on_node_mouse_exited.bind(trait_id))
	button.focus_entered.connect(_on_node_focused.bind(trait_id))
	button.focus_exited.connect(_on_node_focus_exited.bind(trait_id))
	button.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	button.focus_mode = Control.FOCUS_ALL
	_apply_node_style(button, node_data, trait_id == _selected_trait_id)
	return button


func _apply_node_style(button: Button, node_data: Dictionary, selected: bool) -> void:
	var state: String = str(node_data.get("state", STATUS_LOCKED))

	var color: Color = Color(0.38, 0.58, 0.88)
	if state == STATUS_OWNED:
		color = Color(0.42, 0.82, 0.55)
	elif state == STATUS_LOCKED:
		color = Color(0.44, 0.44, 0.48)
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = color.darkened(0.35)
	style.border_color = color
	style.set_border_width_all(4 if selected else 2)
	style.set_corner_radius_all(36)
	button.add_theme_stylebox_override("normal", style)
	var hover: StyleBoxFlat = style.duplicate()
	hover.bg_color = style.bg_color.lightened(0.14)
	button.add_theme_stylebox_override("hover", hover)
	button.add_theme_stylebox_override("focus", hover)


func _on_node_pressed(trait_id: String) -> void:
	_popover_pinned = true
	_selected_trait_id = trait_id
	_show_node_detail(trait_id)
	_refresh_node_selection_styles()


func _on_node_hovered(trait_id: String) -> void:
	if not _popover_pinned:
		_show_node_detail(trait_id)


func _on_node_mouse_exited(trait_id: String) -> void:
	if not _popover_pinned and _active_detail_trait_id == trait_id:
		node_detail_popover.visible = false


func _on_node_focused(trait_id: String) -> void:
	if not _popover_pinned:
		_show_node_detail(trait_id)


func _on_node_focus_exited(trait_id: String) -> void:
	if not _popover_pinned and _active_detail_trait_id == trait_id:
		node_detail_popover.visible = false


func _show_node_detail(trait_id: String) -> void:
	if not _node_lookup.has(trait_id):
		return
	_active_detail_trait_id = trait_id
	var node_data: Dictionary = _node_lookup[trait_id]
	var detail: Dictionary = TraitTreeApi.get_trait_node_detail(_owner_type, _owner_id, trait_id)
	if detail.is_empty():
		detail = node_data

	detail_name.text = str(detail.get("name", trait_id))
	detail_description.text = str(detail.get("description", ""))
	var state: String = str(detail.get("state", STATUS_LOCKED))
	detail_status.text = _state_label(state, detail)
	detail_requirements.text = _requirement_label(detail)

	var show_action: bool = SafeTypeUtils.bool_val(detail.get("unlock_button_visible", false), false)
	action_button.visible = show_action
	action_button.disabled = not SafeTypeUtils.bool_val(detail.get("unlock_button_enabled", false), false)
	action_button.text = Loc.t("ui.trait_tree.unlock_button")
	node_detail_popover.visible = true
	call_deferred("_position_popover", trait_id)


func _position_popover(trait_id: String) -> void:
	if not node_detail_popover.visible or not _tree_controls.has(trait_id):
		return
	var node: Control = _tree_controls[trait_id]
	node_detail_popover.reset_size()
	var popover_size: Vector2 = node_detail_popover.size
	var canvas_size: Vector2 = tree_canvas.size
	var x: float = node.position.x + NODE_SIZE.x + 16.0
	if x + popover_size.x > canvas_size.x - 12.0:
		x = node.position.x - popover_size.x - 16.0
	x = clampf(x, 12.0, maxf(12.0, canvas_size.x - popover_size.x - 12.0))
	var y: float = node.position.y + (NODE_SIZE.y - popover_size.y) * 0.5
	y = clampf(y, 12.0, maxf(12.0, canvas_size.y - popover_size.y - 12.0))
	node_detail_popover.position = Vector2(x, y)


func _refresh_node_selection_styles() -> void:
	for trait_id_var: Variant in _tree_controls.keys():
		var trait_id: String = str(trait_id_var)
		var button: Button = _tree_controls[trait_id]
		var node_data: Dictionary = _node_lookup.get(trait_id, {})
		_apply_node_style(button, node_data, trait_id == _selected_trait_id)


func _state_label(state: String, detail: Dictionary) -> String:
	if state == STATUS_OWNED:
		return Loc.t("ui.trait_tree.status_unlocked")
	if SafeTypeUtils.bool_val(detail.get("can_unlock", false), false):
		return Loc.t("ui.trait_tree.status_ready_to_unlock")
	if SafeTypeUtils.bool_val(detail.get("is_unlockable", false), false):
		return Loc.t("ui.trait_tree.status_need_trait_point")
	return Loc.t("ui.trait_tree.status_locked")


func _requirement_label(detail: Dictionary) -> String:
	if SafeTypeUtils.bool_val(detail.get("is_owned", false), false):
		return ""
	var reason: String = str(detail.get("unlock_blocked_reason", detail.get("locked_reason", "")))
	return reason


func _on_action_pressed() -> void:
	if _active_detail_trait_id.is_empty():
		return
	_pending_unlock_trait_id = _active_detail_trait_id
	unlock_confirmation.dialog_text = Loc.t(
		"ui.trait_tree.acquire_confirmation",
		{"name": detail_name.text}
	)
	unlock_confirmation.popup_centered(Vector2i(470, 180))


func _on_unlock_confirmed() -> void:
	if _pending_unlock_trait_id.is_empty():
		return
	var trait_id: String = _pending_unlock_trait_id
	_pending_unlock_trait_id = ""
	var result: Dictionary = TraitTreeApi.try_unlock_trait(_owner_type, _owner_id, trait_id)
	if not SafeTypeUtils.bool_val(result.get("success", false), false):
		detail_requirements.text = str(result.get("reason", Loc.t("ui.trait_tree.unlock_failed_reason")))
		return
	trait_acquired.emit(trait_id)
	_refresh()


func _on_shade_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed:
		close()


func _on_tree_canvas_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed:
		_popover_pinned = false
		_selected_trait_id = ""
		_active_detail_trait_id = ""
		node_detail_popover.visible = false
		_refresh_node_selection_styles()


func _unhandled_key_input(event: InputEvent) -> void:
	if not is_visible_in_tree() or not event.is_action_pressed("ui_cancel") or event.is_echo():
		return
	get_viewport().set_input_as_handled()
	close()


func _style_surface() -> void:
	var panel_style: StyleBoxFlat = StyleBoxFlat.new()
	panel_style.bg_color = GameColorPalette.UI_SURFACE_RAISED
	panel_style.border_color = GameColorPalette.UI_BORDER_STRONG
	panel_style.set_border_width_all(2)
	panel_style.set_corner_radius_all(10)
	panel_style.shadow_color = Color(0, 0, 0, 0.45)
	panel_style.shadow_size = 18
	panel.add_theme_stylebox_override("panel", panel_style)
	var popover_style: StyleBoxFlat = StyleBoxFlat.new()
	popover_style.bg_color = GameColorPalette.UI_SURFACE_RAISED
	popover_style.border_color = GameColorPalette.BUTTON_PRIMARY_BORDER
	popover_style.set_border_width_all(2)
	popover_style.set_corner_radius_all(8)
	popover_style.shadow_color = Color(0, 0, 0, 0.38)
	popover_style.shadow_size = 10
	node_detail_popover.add_theme_stylebox_override("panel", popover_style)
	detail_name.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	detail_status.add_theme_color_override("font_color", GameColorPalette.TEXT_HIGHLIGHT)
	detail_description.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	detail_requirements.add_theme_color_override("font_color", GameColorPalette.TEXT_HIGHLIGHT)
