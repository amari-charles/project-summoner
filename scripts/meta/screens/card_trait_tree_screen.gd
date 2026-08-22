extends BackNavigableScreen
class_name CardTraitTreeScreen

const STATUS_OWNED: String = "owned"
const STATUS_AVAILABLE: String = "available"
const STATUS_LOCKED: String = "locked"

const NAV_KEY_CARD_INSTANCE_ID: String = "trait_tree_card_instance_id"

const NODE_DIAMETER: float = 74.0
const NODE_X_SPACING: float = 136.0
const NODE_Y_SPACING: float = 132.0
const ICON_SIZE: float = 30.0
const CANVAS_MARGIN: Vector2 = Vector2(44, 44)
const PAN_PADDING: Vector2 = Vector2(220, 180)
const MIN_ZOOM: float = 0.75
const MAX_ZOOM: float = 1.8
const ZOOM_STEP: float = 0.1

const ICON_BOW: Texture2D = preload("res://assets/icons/card_types/bow.png")
const ICON_SWORD: Texture2D = preload("res://assets/icons/card_types/sword.png")
const ICON_TOWER: Texture2D = preload("res://assets/icons/card_types/tower.png")
const ICON_WIZARD_HAT: Texture2D = preload("res://assets/icons/card_types/wizard_hat.png")

const CATEGORY_ICON_TEXTURES: Dictionary = {
	"elemental": ICON_WIZARD_HAT,
	"combat": ICON_SWORD,
	"defense": ICON_TOWER,
	"utility": ICON_BOW,
	"milestone": ICON_TOWER,
	"special": ICON_WIZARD_HAT
}

const CATEGORY_COLORS: Dictionary = {
	"elemental": Color(0.93, 0.65, 0.35),
	"combat": Color(0.86, 0.34, 0.34),
	"defense": Color(0.42, 0.72, 0.92),
	"utility": Color(0.58, 0.83, 0.54),
	"milestone": Color(0.95, 0.85, 0.48),
	"special": Color(0.77, 0.61, 0.95)
}

@onready var back_button: Button = %BackButton
@onready var title_label: Label = %TitleLabel
@onready var card_subtitle_label: Label = %CardSubtitleLabel
@onready var points_label: Label = %PointsLabel
@onready var trait_tabs: TabContainer = %TraitTabs
@onready var tree_canvas: Control = %TreeCanvas
@onready var unlock_confirm_dialog: ConfirmationDialog = %UnlockConfirmDialog
@onready var info_dialog: AcceptDialog = %TraitInfoDialog
@onready var one_off_container: VBoxContainer = %OneOffContainer

var _card_instance_id: String = ""
var _card_catalog_id: String = ""
var _card_level: int = 1
var _unspent_trait_points: int = 0

var _owned_trait_id_set: Dictionary = {}
var _progression_nodes: Array = []
var _one_off_nodes: Array = []
var _progression_lookup: Dictionary = {}

var _pending_unlock_trait_id: String = ""
var _node_by_id: Dictionary = {}
var _base_canvas_size: Vector2 = Vector2(720, 480)
var _zoom: float = 1.0
var _is_panning: bool = false
var _last_pan_pos: Vector2 = Vector2.ZERO


func _ready() -> void:
	back_button.pressed.connect(_on_back_pressed)
	unlock_confirm_dialog.confirmed.connect(_on_unlock_confirmed)

	back_button.text = Loc.t("ui.common.back")
	title_label.text = Loc.t("ui.trait_tree.title_card")
	trait_tabs.set_tab_title(0, Loc.t("ui.trait_tree.tab_progression"))
	trait_tabs.set_tab_title(1, Loc.t("ui.trait_tree.tab_one_off"))
	_configure_tree_scroll()

	_refresh()


func _refresh() -> void:
	_card_instance_id = str(NavigationContext.get_value(NAV_KEY_CARD_INSTANCE_ID, ""))
	if _card_instance_id.is_empty():
		_render_missing_card_state(Loc.t("ui.trait_tree.empty_card_open_from_traits"))
		return

	var progression_info: Dictionary = CardServiceApi.get_card_progression_info_dict(_card_instance_id)
	if progression_info.is_empty():
		_render_missing_card_state(Loc.t("ui.trait_tree.empty_card_progression_unavailable"))
		return

	_card_catalog_id = str(progression_info.get("catalog_id", ""))
	_card_level = int(progression_info.get("level", 1))

	var card_data: Dictionary = CardCatalogApi.get_card_as_dict(_card_catalog_id)
	var card_name: String = str(card_data.get("card_name", _card_catalog_id))
	card_subtitle_label.text = Loc.t("ui.trait_tree.card_subtitle", {"name": card_name, "level": _card_level})

	var view_model: Dictionary = TraitTreeApi.get_card_tree_view_model(_card_instance_id)
	if view_model.is_empty():
		_render_missing_card_state(Loc.t("ui.trait_tree.empty_card_tree_unavailable"))
		return

	_unspent_trait_points = int(view_model.get("unspent_trait_points", 0))
	points_label.text = _format_points_label(_unspent_trait_points)

	_progression_nodes = _extract_trait_dicts(SafeTypeUtils.array(view_model.get("progression_nodes", [])))
	_one_off_nodes = _extract_trait_dicts(SafeTypeUtils.array(view_model.get("one_off_nodes", [])))
	_progression_lookup = _build_trait_lookup(_progression_nodes)
	_rebuild_owned_trait_set()

	_render_progression_tree()
	_render_one_off_traits()
	call_deferred("_render_progression_tree")


func _extract_trait_dicts(raw_nodes: Array) -> Array:
	var nodes: Array = []
	for entry: Variant in raw_nodes:
		if entry is Dictionary:
			nodes.append(entry)
	return nodes


func _build_trait_lookup(nodes: Array) -> Dictionary:
	var lookup: Dictionary = {}
	for node_var: Variant in nodes:
		if not node_var is Dictionary:
			continue
		var node_data: Dictionary = node_var
		var trait_id: String = str(node_data.get("id", ""))
		if trait_id.is_empty():
			continue
		lookup[trait_id] = node_data
	return lookup


func _rebuild_owned_trait_set() -> void:
	_owned_trait_id_set.clear()
	for node_var: Variant in _progression_nodes:
		if not node_var is Dictionary:
			continue
		var node_data: Dictionary = node_var
		if SafeTypeUtils.bool_val(node_data.get("is_owned", false), false):
			_owned_trait_id_set[str(node_data.get("id", ""))] = true
	for node_var: Variant in _one_off_nodes:
		if not node_var is Dictionary:
			continue
		var node_data: Dictionary = node_var
		if SafeTypeUtils.bool_val(node_data.get("is_owned", false), false):
			_owned_trait_id_set[str(node_data.get("id", ""))] = true


func _render_missing_card_state(message: String) -> void:
	points_label.text = _format_points_label(0)
	card_subtitle_label.text = Loc.t("ui.trait_tree.no_card_selected")
	_clear_tree_canvas()

	var info_label: Label = Label.new()
	info_label.text = message
	info_label.position = Vector2(30, 24)
	info_label.add_theme_font_size_override("font_size", 16)
	info_label.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	tree_canvas.add_child(info_label)

	for child: Node in one_off_container.get_children():
		child.queue_free()
	var one_off_label: Label = Label.new()
	one_off_label.text = message
	one_off_container.add_child(one_off_label)


func _clear_tree_canvas() -> void:
	for child: Node in tree_canvas.get_children():
		child.queue_free()
	tree_canvas.call("clear_edges")
	_node_by_id.clear()


func _render_progression_tree() -> void:
	_clear_tree_canvas()

	if _progression_nodes.is_empty():
		var none_label: Label = Label.new()
		none_label.text = Loc.t("ui.trait_tree.empty_no_progression_card")
		none_label.position = Vector2(30, 24)
		tree_canvas.add_child(none_label)
		return

	var depth_by_trait_id: Dictionary = _build_depth_map()
	var grouped_by_depth: Dictionary = {}
	var max_depth: int = 0
	for node_var: Variant in _progression_nodes:
		if not node_var is Dictionary:
			continue
		var node_data: Dictionary = node_var
		var trait_id: String = str(node_data.get("id", ""))
		var depth: int = int(depth_by_trait_id.get(trait_id, 0))
		max_depth = maxi(max_depth, depth)
		if not grouped_by_depth.has(depth):
			grouped_by_depth[depth] = []
		var depth_group: Array = grouped_by_depth[depth]
		depth_group.append(node_data)
		grouped_by_depth[depth] = depth_group

	var row_entries_by_depth: Dictionary = {}
	var x_by_trait_id: Dictionary = {}

	var root_nodes: Array = _sorted_traits_by_name(grouped_by_depth.get(0, []))
	var root_entries: Array = []
	for index: int in range(root_nodes.size()):
		var root_node_data: Dictionary = root_nodes[index]
		var root_id: String = str(root_node_data.get("id", ""))
		var root_x: float = float(index) * NODE_X_SPACING
		root_entries.append({
			"id": root_id,
			"node_data": root_node_data,
			"x": root_x
		})
		x_by_trait_id[root_id] = root_x
	row_entries_by_depth[0] = root_entries

	for depth: int in range(1, max_depth + 1):
		var nodes_at_depth: Array = _sorted_traits_by_name(grouped_by_depth.get(depth, []))
		var desired_entries: Array = []

		for index: int in range(nodes_at_depth.size()):
			var node_data: Dictionary = nodes_at_depth[index]
			var trait_id: String = str(node_data.get("id", ""))
			var prerequisites: Array = SafeTypeUtils.array(node_data.get("prerequisites", []))

			var desired_x: float = float(index) * NODE_X_SPACING
			var prereq_x_sum: float = 0.0
			var prereq_count: int = 0
			for prereq_var: Variant in prerequisites:
				var prereq_id: String = str(prereq_var)
				if x_by_trait_id.has(prereq_id):
					prereq_x_sum += float(x_by_trait_id[prereq_id])
					prereq_count += 1

			if prereq_count > 0:
				desired_x = prereq_x_sum / float(prereq_count)

			desired_entries.append({
				"id": trait_id,
				"node_data": node_data,
				"desired_x": desired_x
			})

		desired_entries.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
			var ax: float = float(a.get("desired_x", 0.0))
			var bx: float = float(b.get("desired_x", 0.0))
			if abs(ax - bx) > 0.01:
				return ax < bx
			return str(a.get("id", "")) < str(b.get("id", ""))
		)

		var placed_entries: Array = []
		var prev_x: float = -1000000.0
		for entry_var: Variant in desired_entries:
			if not entry_var is Dictionary:
				continue
			var entry: Dictionary = entry_var
			var desired_x: float = float(entry.get("desired_x", 0.0))
			var x_pos: float = desired_x
			if prev_x > -999999.0:
				x_pos = maxf(x_pos, prev_x + NODE_X_SPACING)
			entry["x"] = x_pos
			placed_entries.append(entry)
			prev_x = x_pos

		row_entries_by_depth[depth] = placed_entries
		for entry_var: Variant in placed_entries:
			if not entry_var is Dictionary:
				continue
			var entry: Dictionary = entry_var
			x_by_trait_id[str(entry.get("id", ""))] = float(entry.get("x", 0.0))

	var min_x: float = 0.0
	var max_x: float = 0.0
	var first_position: bool = true
	for row_var: Variant in row_entries_by_depth.values():
		if not row_var is Array:
			continue
		for entry_var: Variant in row_var:
			if not entry_var is Dictionary:
				continue
			var x_val: float = float((entry_var as Dictionary).get("x", 0.0))
			if first_position:
				min_x = x_val
				max_x = x_val
				first_position = false
			else:
				min_x = minf(min_x, x_val)
				max_x = maxf(max_x, x_val)

	var graph_width: float = maxf(0.0, max_x - min_x) + NODE_DIAMETER
	var graph_height: float = max(0.0, float(max_depth) * NODE_Y_SPACING) + NODE_DIAMETER
	var viewport_size: Vector2 = _tree_viewport_size()

	var free_width: float = maxf(0.0, viewport_size.x - graph_width)
	var free_height: float = maxf(0.0, viewport_size.y - graph_height)
	var left_padding: float = PAN_PADDING.x + free_width * 0.5
	var top_padding: float = PAN_PADDING.y + free_height * 0.5

	for depth: int in range(0, max_depth + 1):
		var row_entries: Array = row_entries_by_depth.get(depth, [])
		var y: float = top_padding + float(max_depth - depth) * NODE_Y_SPACING

		for entry_var: Variant in row_entries:
			if not entry_var is Dictionary:
				continue
			var entry: Dictionary = entry_var
			var node_data: Dictionary = entry.get("node_data", {})
			var trait_id: String = str(entry.get("id", ""))
			var x: float = left_padding + float(entry.get("x", 0.0)) - min_x
			var pos: Vector2 = Vector2(x, y)

			var node: Button = _create_progression_trait_node(node_data)
			node.position = pos
			tree_canvas.add_child(node)
			_node_by_id[trait_id] = node

	var edges: Array = []
	for node_var: Variant in _progression_nodes:
		if not node_var is Dictionary:
			continue
		var node_data: Dictionary = node_var
		var trait_id: String = str(node_data.get("id", ""))
		if not _node_by_id.has(trait_id):
			continue
		var to_node: Control = _node_by_id[trait_id]
		var prerequisites: Array = SafeTypeUtils.array(node_data.get("prerequisites", []))

		for prereq_var: Variant in prerequisites:
			var prereq_id: String = str(prereq_var)
			if not _node_by_id.has(prereq_id):
				continue
			var from_node: Control = _node_by_id[prereq_id]
			edges.append(_build_edge(from_node, to_node, _edge_color_for(prereq_id, trait_id), 2.6))

	tree_canvas.call("set_edges", edges)

	var total_width: float = maxf(left_padding * 2.0 + graph_width, 720.0)
	var total_height: float = maxf(top_padding * 2.0 + graph_height, 480.0)
	_set_canvas_base_size(Vector2(total_width, total_height))


func _tree_viewport_size() -> Vector2:
	var parent_control: Control = tree_canvas.get_parent() as Control
	if parent_control:
		return parent_control.size
	return tree_canvas.size


func _configure_tree_scroll() -> void:
	var scroll_container: ScrollContainer = _tree_scroll()
	if scroll_container == null:
		return
	scroll_container.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_SHOW_NEVER
	scroll_container.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_SHOW_NEVER
	scroll_container.gui_input.connect(_on_tree_scroll_gui_input)
	_apply_zoom(false)


func _tree_scroll() -> ScrollContainer:
	return tree_canvas.get_parent() as ScrollContainer


func _set_canvas_base_size(size: Vector2) -> void:
	_base_canvas_size = Vector2(maxf(size.x, 720.0), maxf(size.y, 480.0))
	_apply_zoom(false)


func _apply_zoom(keep_focus: bool, focus_position: Vector2 = Vector2.ZERO, old_zoom: float = 1.0) -> void:
	var scroll_container: ScrollContainer = _tree_scroll()
	if scroll_container == null:
		return

	var content_before: Vector2 = Vector2.ZERO
	if keep_focus:
		content_before = (
			Vector2(scroll_container.scroll_horizontal, scroll_container.scroll_vertical) + focus_position
		) / maxf(old_zoom, 0.001)

	tree_canvas.scale = Vector2(_zoom, _zoom)
	tree_canvas.custom_minimum_size = _base_canvas_size * _zoom

	if keep_focus:
		var new_scroll: Vector2 = content_before * _zoom - focus_position
		_set_scroll_position(scroll_container, new_scroll)
	else:
		_reset_tree_scroll_to_center_pan()


func _set_scroll_position(scroll_container: ScrollContainer, target: Vector2) -> void:
	var max_h: float = maxf(0.0, tree_canvas.custom_minimum_size.x - scroll_container.size.x)
	var max_v: float = maxf(0.0, tree_canvas.custom_minimum_size.y - scroll_container.size.y)
	scroll_container.scroll_horizontal = int(round(clampf(target.x, 0.0, max_h)))
	scroll_container.scroll_vertical = int(round(clampf(target.y, 0.0, max_v)))


func _on_tree_scroll_gui_input(event: InputEvent) -> void:
	var scroll_container: ScrollContainer = _tree_scroll()
	if scroll_container == null:
		return

	if event is InputEventMouseButton:
		var mouse_button: InputEventMouseButton = event
		if mouse_button.button_index == MOUSE_BUTTON_MIDDLE:
			_is_panning = mouse_button.pressed
			_last_pan_pos = mouse_button.position
			scroll_container.accept_event()
			return

		if mouse_button.pressed:
			var zoom_direction: float = 0.0
			if mouse_button.button_index == MOUSE_BUTTON_WHEEL_UP:
				zoom_direction = 1.0
			elif mouse_button.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				zoom_direction = -1.0

			if zoom_direction != 0.0:
				var old_zoom: float = _zoom
				_zoom = clampf(_zoom + (zoom_direction * ZOOM_STEP), MIN_ZOOM, MAX_ZOOM)
				if not is_equal_approx(old_zoom, _zoom):
					_apply_zoom(true, mouse_button.position, old_zoom)
				scroll_container.accept_event()
				return

	if event is InputEventMouseMotion and _is_panning:
		var mouse_motion: InputEventMouseMotion = event
		var delta: Vector2 = mouse_motion.position - _last_pan_pos
		var target_scroll: Vector2 = Vector2(
			scroll_container.scroll_horizontal - delta.x,
			scroll_container.scroll_vertical - delta.y
		)
		_set_scroll_position(scroll_container, target_scroll)
		_last_pan_pos = mouse_motion.position
		scroll_container.accept_event()


func _reset_tree_scroll_to_center_pan() -> void:
	var scroll_container: ScrollContainer = _tree_scroll()
	if scroll_container == null:
		return
	scroll_container.scroll_horizontal = int(round(PAN_PADDING.x * _zoom))
	scroll_container.scroll_vertical = int(round(PAN_PADDING.y * _zoom))


func _build_depth_map() -> Dictionary:
	var depth_by_trait_id: Dictionary = {}
	for node_var: Variant in _progression_nodes:
		if not node_var is Dictionary:
			continue
		var node_data: Dictionary = node_var
		var trait_id: String = str(node_data.get("id", ""))
		if trait_id.is_empty():
			continue
		depth_by_trait_id[trait_id] = int(node_data.get("depth", 0))
	return depth_by_trait_id


func _create_progression_trait_node(node_data: Dictionary) -> Button:
	var trait_id: String = str(node_data.get("id", ""))
	var category: String = str(node_data.get("category", "utility"))
	var state: String = str(node_data.get("state", STATUS_LOCKED))

	var button: Button = Button.new()
	button.custom_minimum_size = Vector2(NODE_DIAMETER, NODE_DIAMETER)
	button.size = Vector2(NODE_DIAMETER, NODE_DIAMETER)
	button.text = ""
	button.tooltip_text = _hover_tooltip_text(node_data)
	button.pressed.connect(_on_trait_node_pressed.bind(trait_id))

	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.set_corner_radius_all(int(round(NODE_DIAMETER * 0.5)))
	style.set_border_width_all(3)
	style.shadow_size = 2
	style.shadow_offset = Vector2(1, 1)

	match state:
		STATUS_OWNED:
			style.bg_color = Color(0.89, 0.94, 0.90, 1.0)
			style.border_color = Color(0.35, 0.84, 0.52, 1.0)
		STATUS_AVAILABLE:
			style.bg_color = GameColorPalette.BUTTON_PRIMARY_BG
			style.border_color = Color(0.96, 0.79, 0.34, 1.0)
		_:
			style.bg_color = GameColorPalette.UI_SURFACE_DISABLED
			style.border_color = GameColorPalette.UI_BORDER

	var hover_style: StyleBoxFlat = style.duplicate()
	hover_style.border_color = style.border_color.lightened(0.12)

	var pressed_style: StyleBoxFlat = style.duplicate()
	pressed_style.bg_color = style.bg_color.darkened(0.12)

	button.add_theme_stylebox_override("normal", style)
	button.add_theme_stylebox_override("hover", hover_style)
	button.add_theme_stylebox_override("pressed", pressed_style)

	var center: CenterContainer = CenterContainer.new()
	center.layout_mode = 1
	center.anchors_preset = Control.PRESET_FULL_RECT
	center.anchor_right = 1.0
	center.anchor_bottom = 1.0
	center.grow_horizontal = Control.GROW_DIRECTION_BOTH
	center.grow_vertical = Control.GROW_DIRECTION_BOTH
	center.mouse_filter = Control.MOUSE_FILTER_IGNORE
	button.add_child(center)

	var icon_rect: TextureRect = TextureRect.new()
	icon_rect.texture = _get_category_icon(category)
	icon_rect.custom_minimum_size = Vector2(ICON_SIZE, ICON_SIZE)
	icon_rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon_rect.modulate = _icon_modulate_for_state(category, state)
	icon_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	center.add_child(icon_rect)

	return button


func _get_category_icon(category: String) -> Texture2D:
	var icon: Variant = CATEGORY_ICON_TEXTURES.get(category, ICON_WIZARD_HAT)
	return icon as Texture2D


func _icon_modulate_for_state(category: String, state: String) -> Color:
	var base_color: Color = CATEGORY_COLORS.get(category, Color(0.86, 0.86, 0.90))
	if state == STATUS_LOCKED:
		return base_color.lerp(Color(0.58, 0.58, 0.62, 1.0), 0.55).darkened(0.20)
	return base_color.lightened(0.08)


func _hover_tooltip_text(node_data: Dictionary) -> String:
	var name_text: String = _trait_display_name(node_data)
	var desc_text: String = _trait_description(node_data)
	if desc_text.is_empty():
		return name_text
	return "%s\n%s" % [name_text, desc_text]


func _node_top_anchor(node: Control) -> Vector2:
	return node.position + Vector2(node.size.x * 0.5, 0)


func _node_bottom_anchor(node: Control) -> Vector2:
	return node.position + Vector2(node.size.x * 0.5, node.size.y)


func _build_edge(from_node: Control, to_node: Control, color: Color, width: float) -> Dictionary:
	return {
		"from": _node_top_anchor(from_node),
		"to": _node_bottom_anchor(to_node),
		"color": color,
		"width": width
	}


func _edge_color_for(from_trait_id: String, to_trait_id: String) -> Color:
	var from_owned: bool = _owned_trait_id_set.has(from_trait_id)
	var to_owned: bool = _owned_trait_id_set.has(to_trait_id)
	if from_owned and to_owned:
		return Color(0.42, 0.88, 0.58, 0.95)
	if from_owned:
		return Color(0.77, 0.82, 0.93, 0.92)
	return Color(0.48, 0.50, 0.57, 0.82)


func _render_one_off_traits() -> void:
	for child: Node in one_off_container.get_children():
		child.queue_free()

	if _one_off_nodes.is_empty():
		var none_label: Label = Label.new()
		none_label.text = Loc.t("ui.trait_tree.empty_no_one_off_card")
		one_off_container.add_child(none_label)
		return

	var sorted_one_off: Array = _sorted_one_off_traits(_one_off_nodes)
	for node_var: Variant in sorted_one_off:
		if not node_var is Dictionary:
			continue
		var node_data: Dictionary = node_var
		one_off_container.add_child(_create_one_off_card(node_data))


func _create_one_off_card(node_data: Dictionary) -> PanelContainer:
	var trait_id: String = str(node_data.get("id", ""))
	var category: String = str(node_data.get("category", "special"))
	var color: Color = CATEGORY_COLORS.get(category, Color(0.74, 0.68, 0.88))
	var is_owned: bool = _owned_trait_id_set.has(trait_id)

	var panel: PanelContainer = PanelContainer.new()
	panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	panel.add_theme_stylebox_override(
		"panel",
		_make_panel_style(
			GameColorPalette.UI_SURFACE_RAISED if is_owned else GameColorPalette.UI_SURFACE,
			color if is_owned else GameColorPalette.UI_BORDER
		)
	)

	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 10)
	margin.add_theme_constant_override("margin_top", 8)
	margin.add_theme_constant_override("margin_right", 10)
	margin.add_theme_constant_override("margin_bottom", 8)
	panel.add_child(margin)

	var row: HBoxContainer = HBoxContainer.new()
	row.add_theme_constant_override("separation", 10)
	margin.add_child(row)

	var icon_rect: TextureRect = TextureRect.new()
	icon_rect.texture = _get_category_icon(category)
	icon_rect.custom_minimum_size = Vector2(18, 18)
	icon_rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon_rect.modulate = color
	row.add_child(icon_rect)

	var text_vbox: VBoxContainer = VBoxContainer.new()
	text_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	text_vbox.add_theme_constant_override("separation", 2)
	row.add_child(text_vbox)

	var name_label: Label = Label.new()
	name_label.text = _trait_display_name(node_data)
	name_label.add_theme_font_size_override("font_size", 14)
	name_label.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	text_vbox.add_child(name_label)

	var desc_label: Label = Label.new()
	desc_label.text = _trait_description(node_data)
	desc_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	desc_label.add_theme_font_size_override("font_size", 12)
	desc_label.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	text_vbox.add_child(desc_label)

	var status_label: Label = Label.new()
	status_label.text = Loc.t("ui.trait_tree.one_off_status_unlocked") if is_owned else Loc.t("ui.trait_tree.one_off_status_not_acquired")
	status_label.add_theme_font_size_override("font_size", 12)
	status_label.add_theme_color_override("font_color", GameColorPalette.SUCCESS if is_owned else GameColorPalette.TEXT_DISABLED)
	row.add_child(status_label)

	panel.tooltip_text = _trait_description(node_data)
	return panel


func _trait_display_name(node_data: Dictionary) -> String:
	var name_text: String = str(node_data.get("name", ""))
	if not name_text.is_empty():
		return name_text

	var name_key: String = str(node_data.get("name_key", ""))
	var fallback_id: String = str(node_data.get("id", "trait"))
	if name_key.is_empty():
		return fallback_id
	var resolved: String = Loc.t(name_key)
	return resolved if resolved != name_key else fallback_id


func _trait_description(node_data: Dictionary) -> String:
	var desc_text: String = str(node_data.get("description", ""))
	if not desc_text.is_empty():
		return desc_text

	var description_key: String = str(node_data.get("description_key", ""))
	if description_key.is_empty():
		return ""
	var resolved: String = Loc.t(description_key)
	return "" if resolved == description_key else resolved


func _make_panel_style(bg_color: Color, border_color: Color) -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = bg_color
	style.border_color = border_color
	style.set_border_width_all(1)
	style.set_corner_radius_all(7)
	style.shadow_size = 2
	style.shadow_offset = Vector2(1, 1)
	style.shadow_color = GameColorPalette.BUTTON_SHADOW
	return style


func _sorted_traits_by_name(nodes: Array) -> Array:
	var names: Array = []
	var by_key: Dictionary = {}
	for node_var: Variant in nodes:
		if not node_var is Dictionary:
			continue
		var node_data: Dictionary = node_var
		var trait_id: String = str(node_data.get("id", ""))
		var key: String = "%s|%s" % [_trait_display_name(node_data), trait_id]
		names.append(key)
		by_key[key] = node_data

	names.sort()
	var sorted: Array = []
	for key_var: Variant in names:
		sorted.append(by_key[str(key_var)])
	return sorted


func _sorted_one_off_traits(nodes: Array) -> Array:
	var owned_nodes: Array = []
	var unowned_nodes: Array = []
	for node_var: Variant in nodes:
		if not node_var is Dictionary:
			continue
		var node_data: Dictionary = node_var
		var trait_id: String = str(node_data.get("id", ""))
		if _owned_trait_id_set.has(trait_id):
			owned_nodes.append(node_data)
		else:
			unowned_nodes.append(node_data)

	var sorted: Array = []
	sorted.append_array(_sorted_traits_by_name(owned_nodes))
	sorted.append_array(_sorted_traits_by_name(unowned_nodes))
	return sorted


func _on_trait_node_pressed(trait_id: String) -> void:
	if _card_instance_id.is_empty():
		return

	var detail: Dictionary = TraitTreeApi.get_trait_node_detail("card", _card_instance_id, trait_id)
	if detail.is_empty():
		return

	_show_trait_detail_dialog(trait_id, detail)


func _show_trait_detail_dialog(trait_id: String, detail: Dictionary) -> void:
	var state: String = str(detail.get("state", STATUS_LOCKED))
	var trait_name: String = str(detail.get("name", trait_id))
	var trait_desc: String = str(detail.get("description", ""))

	var status_lines: Array[String] = []
	if state == STATUS_OWNED:
		status_lines.append(Loc.t("ui.trait_tree.status_unlocked"))
	elif SafeTypeUtils.bool_val(detail.get("is_unlockable", false), false):
		if SafeTypeUtils.bool_val(detail.get("can_unlock", false), false):
			status_lines.append(Loc.t("ui.trait_tree.status_ready_to_unlock"))
		else:
			status_lines.append(Loc.t("ui.trait_tree.status_need_trait_point"))
	else:
		status_lines.append(Loc.t("ui.trait_tree.status_locked"))
		var reason_text: String = str(detail.get("unlock_blocked_reason", detail.get("locked_reason", Loc.t("ui.trait_tree.reason_not_currently_available"))))
		if not reason_text.is_empty():
			status_lines.append(reason_text)

	var description_text: String = trait_desc if not trait_desc.is_empty() else Loc.t("ui.trait_tree.no_description")
	if not status_lines.is_empty():
		description_text += "\n\n%s" % "\n".join(status_lines)

	var ok_button: Button = unlock_confirm_dialog.get_ok_button()
	var unlock_visible: bool = SafeTypeUtils.bool_val(detail.get("unlock_button_visible", false), false)
	var unlock_enabled: bool = SafeTypeUtils.bool_val(detail.get("unlock_button_enabled", false), false)

	ok_button.visible = unlock_visible
	ok_button.text = str(detail.get("unlock_button_text", Loc.t("ui.trait_tree.unlock_button")))
	ok_button.disabled = not unlock_enabled

	if unlock_visible:
		_pending_unlock_trait_id = trait_id
	else:
		_pending_unlock_trait_id = ""

	var cancel_button: Button = unlock_confirm_dialog.get_cancel_button()
	cancel_button.text = Loc.t("ui.trait_tree.close_button")

	unlock_confirm_dialog.title = trait_name
	unlock_confirm_dialog.dialog_text = description_text
	unlock_confirm_dialog.popup_centered(Vector2i(520, 240))


func _on_unlock_confirmed() -> void:
	if _card_instance_id.is_empty() or _pending_unlock_trait_id.is_empty():
		return

	var trait_id: String = _pending_unlock_trait_id
	_pending_unlock_trait_id = ""

	var result: Dictionary = TraitTreeApi.try_unlock_trait("card", _card_instance_id, trait_id)
	if SafeTypeUtils.bool_val(result.get("success", false), false):
		_refresh()
		return

	var reason: String = str(result.get("reason", Loc.t("ui.trait_tree.unlock_failed_reason")))
	_show_info_dialog(Loc.t("ui.trait_tree.unlock_failed_title"), reason)


func _show_info_dialog(title: String, message: String) -> void:
	info_dialog.title = title
	info_dialog.dialog_text = message
	info_dialog.popup_centered(Vector2i(460, 180))


func _format_points_label(points: int) -> String:
	return Loc.t("ui.trait_tree.points_label", {"count": points})


func _on_back_pressed() -> void:
	NavigationContext.clear_value(NAV_KEY_CARD_INSTANCE_ID)
	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_COLLECTION_SCREEN
	SceneManager.transition_to(return_scene)


func _request_back_navigation() -> void:
	_on_back_pressed()
