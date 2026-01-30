extends Control
class_name CampaignMap

## Campaign Map - Visual node-based event progression
##
## Shows campaign events as nodes in a graph structure with branching paths.

## Preloads
const SummonerIconWidgetScene: PackedScene = preload("res://scenes/ui/components/summoner_icon_widget.tscn")
const HamburgerButtonScene: PackedScene = preload("res://scenes/ui/components/hamburger_button.tscn")
const NavDrawerScene: PackedScene = preload("res://scenes/ui/components/nav_drawer.tscn")
const CampaignSelectorModalScene: PackedScene = preload("res://scenes/ui/components/campaign_selector_modal.tscn")

## Node references
@onready var locator_button: TextureButton = %LocatorButton
@onready var map_scroll: ScrollContainer = %MapScrollContainer
@onready var map_container: Control = %MapContainer
@onready var screen_background: ColorRect = %ScreenBackground
@onready var map_background: ColorRect = %MapBackground
@onready var detail_panel: Panel = %DetailPanel
@onready var panel_container: Control = %PanelContainer

## Map layout constants
const NODE_SPACING: float = 150.0  # Horizontal spacing between nodes
const NODE_SIZE: Vector2 = Vector2(80, 80)
const PATH_COLOR: Color = Color(0.4, 0.4, 0.5)
const PATH_WIDTH: float = 4.0
const MAP_CENTER_Y: float = 800.0  # Vertical center of 1600px map height
const MAP_WAVE_AMPLITUDE: float = 300.0  # Vertical variation for winding path

## UI positioning constants
const HAMBURGER_BUTTON_MARGIN: float = 20.0
const HAMBURGER_BUTTON_SIZE: float = 48.0
const CAMPAIGN_BANNER_MARGIN: float = 20.0
const CAMPAIGN_BANNER_WIDTH: float = 220.0  # Fits campaign name
const CAMPAIGN_BANNER_HEIGHT: float = 40.0
const SUMMONER_ICON_SIZE: float = 50.0
const SUMMONER_ICON_MARGIN: float = 20.0
const GOLD_LABEL_MARGIN: float = 20.0
const GOLD_LABEL_HEIGHT: float = 32.0

## Kenny UI Pack textures for event nodes
const EVENT_NODE_TEXTURE: String = "res://assets/ui/kenny/PNG/Grey/Default/button_round_depth_flat.png"
const EVENT_NODE_CHECKMARK: String = "res://assets/ui/kenny/PNG/Green/Default/icon_checkmark.png"
const CHECKMARK_SIZE: int = 32  # Size of checkmark overlay

## Graph rendering constants
const POSITION_SCALE: float = 1.5  # Scale factor for node positions from data
const EDGE_ACTIVE_COLOR: Color = Color(0.3, 0.8, 0.3)  # Green for completed paths
const EDGE_AVAILABLE_COLOR: Color = Color(0.8, 0.8, 0.3)  # Yellow for available paths
const EDGE_LOCKED_COLOR: Color = Color(0.4, 0.4, 0.5, 0.5)  # Grey for locked paths
const DASH_LENGTH: float = 12.0  # Length of each dash
const GAP_LENGTH: float = 8.0  # Length of gap between dashes

## State
var selected_event_id: String = ""
var event_nodes: Dictionary = {}  # event_id -> Control (fast lookup)
var edge_lines: Array[Node2D] = []  # Dashed line containers for edges
var graph_edges: Array[Dictionary] = []  # Edges from campaign data
var graph_nodes: Array[Dictionary] = []  # Nodes from campaign data

## Panning state
var is_panning: bool = false
var pan_start_position: Vector2 = Vector2.ZERO
var last_mouse_position: Vector2 = Vector2.ZERO
const PAN_THRESHOLD: float = 5.0  # Pixels to move before panning starts

## Active panel for detail display
var active_panel: NodeDetailPanelBase = null

## Summoner icon widget reference
var summoner_icon: SummonerIconWidget = null

## Gold display
var gold_label: Label = null

## Navigation components
var hamburger_button: HamburgerButton = null
var nav_drawer: NavDrawer = null

## Campaign selector components
var campaign_banner: Button = null
var campaign_selector_modal: CampaignSelectorModal = null

## =============================================================================
## TYPE HELPERS
## =============================================================================

func _safe_string(variant: Variant, default: String = "") -> String:
	return variant if variant is String else default

func _safe_int(variant: Variant, default: int = 0) -> int:
	return variant if variant is int else default

func _safe_dict(variant: Variant) -> Dictionary:
	return variant if variant is Dictionary else {}

func _safe_array(variant: Variant) -> Array:
	return variant if variant is Array else []

func _safe_bool(variant: Variant, default: bool = false) -> bool:
	return variant if variant is bool else default

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CampaignMap: Initializing...")

	# Connect buttons
	locator_button.pressed.connect(_on_center_latest_pressed)

	# Setup detail panel with fantasy border
	_setup_detail_panel_border()

	# Connect to campaign service
	if Campaign.has_signal("battle_completed"):
		Campaign.battle_completed.connect(_on_event_completed)
	if Campaign.has_signal("campaign_progress_changed"):
		Campaign.campaign_progress_changed.connect(_on_progress_changed)

	# Connect to summoner selection changes
	if SummonerSelection.has_signal("SummonerChanged"):
		SummonerSelection.SummonerChanged.connect(_on_summoner_selection_changed)

	# Check for summoner selection requirement
	# Handles first-game-start and post-wipe scenarios
	if SummonerSelection.GetActiveSummonerId().is_empty():
		call_deferred("_redirect_to_summoner_selection")
		return  # Skip rest of setup

	# Setup navigation (hamburger menu + nav drawer)
	_setup_navigation()

	# Setup campaign banner (top-left)
	_setup_campaign_banner()

	# Setup summoner icon
	_setup_summoner_icon()

	# Setup gold display
	_setup_gold_display()

	# Load and display map
	_refresh_map()

	# Auto-scroll to latest mission (deferred to next frame so nodes are fully laid out)
	call_deferred("_on_center_latest_pressed")

func _draw() -> void:
	# Edge drawing is now handled by Line2D nodes in map_container
	pass


## Get the color for an edge based on completion state
func _get_edge_color(from_id: String, to_id: String, edge: Dictionary) -> Color:
	var from_completed: bool = _safe_bool(Campaign.is_battle_completed(from_id))
	var to_unlocked: bool = _safe_bool(Campaign.is_battle_unlocked(to_id))

	# Check if edge has a condition (choice edges)
	var condition: Variant = edge.get("condition")
	if condition is Dictionary:
		# Source must be completed for any conditional edge to be visible
		if not from_completed:
			return EDGE_LOCKED_COLOR

		# Check if the choice matches the condition
		var choice_matches: bool = _does_choice_match_condition(from_id, condition)
		if choice_matches and to_unlocked:
			return EDGE_ACTIVE_COLOR
		elif choice_matches:
			return EDGE_AVAILABLE_COLOR
		else:
			# Wrong choice was made - this path is locked
			return EDGE_LOCKED_COLOR

	# Non-conditional edges
	if from_completed:
		if to_unlocked:
			return EDGE_ACTIVE_COLOR
		else:
			return EDGE_AVAILABLE_COLOR
	else:
		return EDGE_LOCKED_COLOR


## Check if a recorded choice matches an edge condition
func _does_choice_match_condition(source_node_id: String, condition: Dictionary) -> bool:
	# Parse condition format:
	# Shorthand: {"choice": "elite"}
	# Full: {"type": "choice", "value": "elite", "node_id": "optional"}
	var condition_type: String = ""
	var required_value: String = ""
	var choice_node_id: String = source_node_id

	if condition.has("choice"):
		# Shorthand format
		condition_type = "choice"
		required_value = _safe_string(condition.get("choice"))
	elif condition.has("type"):
		# Full format
		condition_type = _safe_string(condition.get("type"))
		required_value = _safe_string(condition.get("value"))
		var explicit_node: String = _safe_string(condition.get("node_id"))
		if not explicit_node.is_empty():
			choice_node_id = explicit_node

	# Only evaluate choice conditions here (other types handled by unlock handler)
	if condition_type != "choice":
		# For non-choice conditions, defer to unlock handler
		return true

	# Get the choice that was made at the choice node
	var made_choice: String = Campaign.get_choice(choice_node_id)
	return made_choice == required_value


## Create a dashed line between two points (returns container with line segments)
func _create_dashed_line(start: Vector2, end: Vector2, color: Color) -> Node2D:
	var container: Node2D = Node2D.new()

	var direction: Vector2 = (end - start).normalized()
	var total_length: float = start.distance_to(end)
	var current_pos: float = 0.0

	while current_pos < total_length:
		var dash_start: Vector2 = start + direction * current_pos
		var dash_end_pos: float = min(current_pos + DASH_LENGTH, total_length)
		var dash_end: Vector2 = start + direction * dash_end_pos

		var segment: Line2D = Line2D.new()
		segment.width = PATH_WIDTH
		segment.default_color = color
		segment.antialiased = true
		segment.add_point(dash_start)
		segment.add_point(dash_end)
		container.add_child(segment)

		current_pos += DASH_LENGTH + GAP_LENGTH

	return container

## =============================================================================
## MAP DISPLAY
## =============================================================================

func _refresh_map() -> void:
	# Clear existing state
	for child: Node in map_container.get_children():
		child.queue_free()
	event_nodes.clear()
	edge_lines.clear()
	graph_nodes.clear()
	graph_edges.clear()

	# Load graph data from campaign service
	var nodes_variant: Variant = Campaign.get_current_campaign_nodes()
	var nodes_array: Array = _safe_array(nodes_variant)
	graph_nodes.assign(nodes_array)

	var edges_variant: Variant = Campaign.get_current_campaign_edges()
	var edges_array: Array = _safe_array(edges_variant)
	graph_edges.assign(edges_array)

	print("CampaignMap: Loaded %d nodes, %d edges from graph data" % [graph_nodes.size(), graph_edges.size()])

	# Build a lookup for node positions (needed for edge drawing)
	var node_positions: Dictionary = {}  # node_id -> Vector2
	for node: Dictionary in graph_nodes:
		var node_id: String = _safe_string(node.get("id", ""))
		if node_id.is_empty():
			continue
		var raw_position: Variant = node.get("position", Vector2.ZERO)
		var node_position: Vector2
		if raw_position is Vector2:
			node_position = raw_position * POSITION_SCALE + Vector2(100, 200)
		else:
			node_position = Vector2(100, 300) * POSITION_SCALE + Vector2(100, 200)
		# Add half node size to get center
		node_positions[node_id] = node_position + NODE_SIZE / 2

	# Create edge lines FIRST (so they appear behind nodes)
	for edge: Dictionary in graph_edges:
		var from_id: String = edge.get("from", "")
		var to_id: String = edge.get("to", "")

		if not node_positions.has(from_id) or not node_positions.has(to_id):
			continue

		var start_pos: Vector2 = node_positions[from_id]
		var end_pos: Vector2 = node_positions[to_id]
		var edge_color: Color = _get_edge_color(from_id, to_id, edge)

		# Create dashed line
		var line: Node2D = _create_dashed_line(start_pos, end_pos, edge_color)
		map_container.add_child(line)
		edge_lines.append(line)

	# Create nodes from graph data (using positions from data)
	for node: Dictionary in graph_nodes:
		var node_id: String = _safe_string(node.get("id", ""))
		if node_id.is_empty():
			push_warning("CampaignMap: Node missing 'id', skipping")
			continue

		var is_completed: bool = _safe_bool(Campaign.is_battle_completed(node_id))
		var is_unlocked: bool = _safe_bool(Campaign.is_battle_unlocked(node_id))

		var event_node: Control = _create_graph_node(node, is_unlocked, is_completed)
		map_container.add_child(event_node)
		event_nodes[node_id] = event_node

		print("  - %s at %s (unlocked: %s, completed: %s)" % [
			node_id,
			node.get("position", Vector2.ZERO),
			is_unlocked,
			is_completed
		])

	print("CampaignMap: Created %d event nodes, %d edge lines" % [event_nodes.size(), edge_lines.size()])

## Create a node from graph data (uses position from node data)
func _create_graph_node(node_data: Dictionary, is_unlocked: bool, is_completed: bool) -> Control:
	var node_container: Control = Control.new()
	node_container.custom_minimum_size = NODE_SIZE

	# Get position from node data and scale it
	var raw_position: Variant = node_data.get("position", Vector2.ZERO)
	var node_position: Vector2
	if raw_position is Vector2:
		node_position = raw_position * POSITION_SCALE
	else:
		node_position = Vector2(100, 300) * POSITION_SCALE  # Default fallback

	# Add offset to center the map content
	node_position += Vector2(100, 200)  # Margin from top-left

	node_container.position = node_position

	var node_id: String = _safe_string(node_data.get("id", ""))
	var node_type: String = _safe_string(node_data.get("type", ""))

	# Load the round button texture
	var node_texture: Texture2D = load(EVENT_NODE_TEXTURE)

	# Create texture button with Kenny round button
	var button: TextureButton = TextureButton.new()
	button.texture_normal = node_texture
	button.custom_minimum_size = NODE_SIZE
	button.size = NODE_SIZE
	button.ignore_texture_size = true
	button.stretch_mode = TextureButton.STRETCH_KEEP_ASPECT_CENTERED

	# Set visual style based on node type and state
	var base_color: Color = _get_node_type_color(node_type)
	if not is_unlocked:
		button.modulate = Color(base_color.r, base_color.g, base_color.b, 0.5)  # Locked: 50% opacity
		button.disabled = true
	else:
		button.modulate = base_color

	# Add overlay icon for completed nodes
	if is_completed:
		var checkmark: TextureRect = TextureRect.new()
		checkmark.texture = load(EVENT_NODE_CHECKMARK)
		checkmark.custom_minimum_size = Vector2(CHECKMARK_SIZE, CHECKMARK_SIZE)
		checkmark.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		checkmark.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		# Center the checkmark on the button
		checkmark.anchor_left = 0.5
		checkmark.anchor_right = 0.5
		checkmark.anchor_top = 0.5
		checkmark.anchor_bottom = 0.5
		checkmark.offset_left = -CHECKMARK_SIZE / 2.0
		checkmark.offset_right = CHECKMARK_SIZE / 2.0
		checkmark.offset_top = -CHECKMARK_SIZE / 2.0
		checkmark.offset_bottom = CHECKMARK_SIZE / 2.0
		button.add_child(checkmark)

	# Connect click handler
	if is_unlocked:
		button.pressed.connect(_on_event_node_clicked.bind(node_id))

	node_container.add_child(button)
	return node_container


## Get color tint for node based on type
func _get_node_type_color(node_type: String) -> Color:
	match node_type:
		"battle":
			return Color(1.0, 1.0, 1.0)  # White/grey
		"elite":
			return Color(1.0, 0.8, 0.3)  # Gold
		"boss":
			return Color(1.0, 0.4, 0.4)  # Red
		"choice":
			return Color(0.6, 0.8, 1.0)  # Light blue
		"caravan":
			return Color(0.8, 1.0, 0.6)  # Light green
		"rest":
			return Color(0.7, 0.9, 1.0)  # Cyan
		"story":
			return Color(1.0, 0.7, 1.0)  # Pink
		_:
			return Color(1.0, 1.0, 1.0)  # Default white

func _on_event_node_clicked(event_id: String) -> void:
	selected_event_id = event_id
	_show_detail_panel_for_event(event_id)
	print("CampaignMap: Selected event: %s" % event_id)

func _show_popup() -> void:
	# Center the popup on screen
	var viewport_size: Vector2 = get_viewport_rect().size
	var popup_size: Vector2 = detail_panel.custom_minimum_size
	detail_panel.position = (viewport_size - popup_size) / 2
	detail_panel.size = popup_size
	detail_panel.visible = true

func _input(event: InputEvent) -> void:
	# Handle mouse button events
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event as InputEventMouseButton

		if mouse_event.button_index == MOUSE_BUTTON_LEFT:
			if mouse_event.pressed:
				# Check if clicking outside popup to dismiss it
				if detail_panel.visible:
					var popup_rect: Rect2 = Rect2(detail_panel.position, detail_panel.size)
					if not popup_rect.has_point(mouse_event.position):
						detail_panel.visible = false
						get_viewport().set_input_as_handled()
						return

				# Record potential panning start (but don't claim input yet to allow button clicks)
				if not detail_panel.visible:
					var scroll_rect: Rect2 = map_scroll.get_global_rect()
					if scroll_rect.has_point(mouse_event.position):
						pan_start_position = mouse_event.position
						last_mouse_position = mouse_event.position
			else:
				# Stop panning on release
				is_panning = false

	# Handle mouse motion for panning
	elif event is InputEventMouseMotion:
		var motion_event: InputEventMouseMotion = event as InputEventMouseMotion

		# Only start panning if mouse button is held and we've moved beyond threshold
		if motion_event.button_mask & MOUSE_BUTTON_MASK_LEFT:
			if not is_panning and not detail_panel.visible:
				# Check if we've moved beyond the threshold to start panning
				var distance: float = motion_event.position.distance_to(pan_start_position)
				if distance > PAN_THRESHOLD:
					var scroll_rect: Rect2 = map_scroll.get_global_rect()
					if scroll_rect.has_point(motion_event.position):
						is_panning = true
						last_mouse_position = motion_event.position

			if is_panning:
				var delta: Vector2 = motion_event.position - last_mouse_position

				# Update scroll positions (inverted - drag right scrolls left)
				map_scroll.scroll_horizontal -= int(delta.x)
				map_scroll.scroll_vertical -= int(delta.y)

				last_mouse_position = motion_event.position
				get_viewport().set_input_as_handled()
		else:
			# Reset pan tracking when button not held
			pan_start_position = Vector2.ZERO

## =============================================================================
## DETAIL PANEL
## =============================================================================

## Set up the detail panel border texture using ButtonStyleFactory
func _setup_detail_panel_border() -> void:
	var panel_bg: NinePatchRect = get_node_or_null("%Background")
	if not panel_bg:
		return
	ButtonStyleFactory.apply_panel_border(panel_bg)

	# Remove default panel style (we're using NinePatchRect instead)
	detail_panel.add_theme_stylebox_override("panel", StyleBoxEmpty.new())


## Show the appropriate detail panel for an event
func _show_detail_panel_for_event(event_id: String) -> void:
	var event: Dictionary = _safe_dict(Campaign.get_battle(event_id))
	if event.is_empty():
		return

	var event_type: StringName = NodePanelFactory.get_event_type(event)

	# Check if we need to swap panel types
	if active_panel and active_panel.get_event_type() != event_type:
		active_panel.queue_free()
		active_panel = null

	# Create panel if needed
	if not active_panel:
		active_panel = NodePanelFactory.create_panel(event_type)
		panel_container.add_child(active_panel)
		_connect_panel_signals(active_panel)

	# Configure the panel with event data
	active_panel.configure(event, event_id)

	# Show the popup
	_show_popup()


## Connect signals from a panel based on its type
func _connect_panel_signals(panel: NodeDetailPanelBase) -> void:
	# All panels have start_requested
	if not panel.start_requested.is_connected(_on_panel_start_requested):
		panel.start_requested.connect(_on_panel_start_requested)

	# Choice panels have choice_made
	if panel is ChoiceNodePanel:
		var choice_panel: ChoiceNodePanel = panel as ChoiceNodePanel
		if not choice_panel.choice_made.is_connected(_on_choice_made):
			choice_panel.choice_made.connect(_on_choice_made)

## =============================================================================
## PANEL EVENT HANDLERS
## =============================================================================

## Handle start button pressed from any panel type
func _on_panel_start_requested() -> void:
	if selected_event_id == "":
		return

	var event: Dictionary = _safe_dict(Campaign.get_battle(selected_event_id))
	if event.is_empty():
		return

	var event_type: StringName = NodePanelFactory.get_event_type(event)

	# Handle affinity selection event - route to summoner selection
	if event_type == EventTypeIDs.AFFINITY:
		print("CampaignMap: Starting affinity selection...")
		SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SELECTION)
		return

	# Handle first summon event - route to first card selection
	if event_type == EventTypeIDs.FIRST_SUMMON:
		print("CampaignMap: Starting first summon selection...")
		SceneManager.transition_to(SceneManager.SCENE_FIRST_CARD_SELECTION)
		return

	# Handle caravan events - navigate directly to shop with event context
	if event_type == EventTypeIDs.CARAVAN:
		print("CampaignMap: Starting caravan event: %s" % selected_event_id)

		# Check if event is already completed and not repeatable
		var is_completed: bool = _safe_bool(Campaign.is_battle_completed(selected_event_id), false)
		var is_repeatable: bool = _safe_bool(event.get("repeatable", false), false)

		if is_completed and not is_repeatable:
			push_warning("CampaignMap: Cannot start event '%s' - already completed and not repeatable" % selected_event_id)
			return

		# Configure EventContext with this event (CaravanScreen will detect this)
		EventContext.configure_event(selected_event_id, SceneManager.SCENE_CAMPAIGN_MAP)

		# Navigate to caravan screen
		NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
		SceneManager.transition_to(SceneManager.SCENE_CARAVAN_SCREEN)
		return

	# Handle battle events
	print("CampaignMap: Starting battle event: %s" % selected_event_id)

	# Store selected event in campaign service
	var profile: Dictionary = _safe_dict(ProfileRepo.get_active_profile())
	if not profile.is_empty():
		if not profile.has("campaign_progress"):
			profile["campaign_progress"] = {}
		var campaign_progress: Dictionary = _safe_dict(profile["campaign_progress"])
		campaign_progress["current_battle"] = selected_event_id
		ProfileRepo.save_profile(true)

	# Configure battle context
	print("CampaignMap: Configuring BattleContext with battle_id='%s'" % selected_event_id)
	BattleContext.configure_campaign_battle(selected_event_id)

	# Launch battle scene - use custom scene_path if specified in battle config
	var battle_scene: String = SceneManager.SCENE_BATTLE_3D
	if BattleContext.battle_config.has("scene_path"):
		var custom_scene: String = BattleContext.battle_config.get("scene_path", "")
		if not custom_scene.is_empty():
			battle_scene = custom_scene
			print("CampaignMap: Using custom battle scene: %s" % battle_scene)

	print("CampaignMap: Launching battle scene...")
	SceneManager.transition_to(battle_scene)


## Handle choice made from choice panel
func _on_choice_made(option_id: String) -> void:
	print("CampaignMap: Player chose option '%s' for node '%s'" % [option_id, selected_event_id])

	# Hide the detail panel
	detail_panel.visible = false

	# Record the choice (this affects which edges are traversable)
	Campaign.record_choice(selected_event_id, option_id)

	# Mark the choice node as completed
	Campaign.complete_battle(selected_event_id)

	# Refresh map to show newly unlocked paths
	_refresh_map()

## =============================================================================
## NAVIGATION
## =============================================================================

func _on_center_latest_pressed() -> void:
	var latest_unlocked_id: String = _find_latest_unlocked_mission()
	if latest_unlocked_id.is_empty():
		print("CampaignMap: No unlocked missions to center on")
		return

	_scroll_to_event(latest_unlocked_id)
	print("CampaignMap: Centered on latest mission: %s" % latest_unlocked_id)

func _find_latest_unlocked_mission() -> String:
	# Find the first unlocked but not completed node
	# For graph structures, prioritize nodes further to the right (higher X position)
	var best_node_id: String = ""
	var best_x: float = -1.0

	for node: Dictionary in graph_nodes:
		var node_id: String = _safe_string(node.get("id", ""))
		var is_completed: bool = _safe_bool(Campaign.is_battle_completed(node_id))
		var is_unlocked: bool = _safe_bool(Campaign.is_battle_unlocked(node_id))

		if is_unlocked and not is_completed:
			var pos: Variant = node.get("position", Vector2.ZERO)
			var x_pos: float = pos.x if pos is Vector2 else 0.0
			if x_pos > best_x:
				best_x = x_pos
				best_node_id = node_id

	# If found an unlocked incomplete node, return it
	if not best_node_id.is_empty():
		return best_node_id

	# If all completed, return the rightmost node
	for node: Dictionary in graph_nodes:
		var pos: Variant = node.get("position", Vector2.ZERO)
		var x_pos: float = pos.x if pos is Vector2 else 0.0
		if x_pos > best_x:
			best_x = x_pos
			best_node_id = _safe_string(node.get("id", ""))

	return best_node_id

func _scroll_to_event(event_id: String) -> void:
	if not event_nodes.has(event_id):
		push_warning("CampaignMap: Cannot scroll to missing event '%s'" % event_id)
		return

	var node: Control = event_nodes[event_id]
	var node_center_x: float = node.position.x + node.size.x / 2
	var node_center_y: float = node.position.y + node.size.y / 2

	# Calculate scroll position to center the node in viewport (both X and Y)
	var viewport_width: float = map_scroll.size.x
	var viewport_height: float = map_scroll.size.y

	var scroll_target_x: float = node_center_x - (viewport_width / 2)
	scroll_target_x = max(0, scroll_target_x)  # Clamp to valid range

	var scroll_target_y: float = node_center_y - (viewport_height / 2)
	scroll_target_y = max(0, scroll_target_y)  # Clamp to valid range

	# Set scroll position (both horizontal and vertical)
	map_scroll.scroll_horizontal = int(scroll_target_x)
	map_scroll.scroll_vertical = int(scroll_target_y)

## =============================================================================
## NAVIGATION (Hamburger Menu + Nav Drawer)
## =============================================================================

func _setup_navigation() -> void:
	# Create hamburger button in top-right corner
	hamburger_button = HamburgerButtonScene.instantiate()
	add_child(hamburger_button)

	hamburger_button.anchor_left = 1.0
	hamburger_button.anchor_right = 1.0
	hamburger_button.anchor_top = 0.0
	hamburger_button.anchor_bottom = 0.0
	hamburger_button.offset_left = -(HAMBURGER_BUTTON_MARGIN + HAMBURGER_BUTTON_SIZE)
	hamburger_button.offset_right = -HAMBURGER_BUTTON_MARGIN
	hamburger_button.offset_top = HAMBURGER_BUTTON_MARGIN
	hamburger_button.offset_bottom = HAMBURGER_BUTTON_MARGIN + HAMBURGER_BUTTON_SIZE

	hamburger_button.pressed.connect(_on_hamburger_pressed)

	# Create nav drawer (hidden by default)
	nav_drawer = NavDrawerScene.instantiate()
	add_child(nav_drawer)

	# Connect nav drawer signals
	nav_drawer.collection_pressed.connect(_on_nav_collection_pressed)
	nav_drawer.events_pressed.connect(_on_nav_events_pressed)
	nav_drawer.shop_pressed.connect(_on_nav_shop_pressed)
	nav_drawer.settings_pressed.connect(_on_nav_settings_pressed)
	nav_drawer.quit_pressed.connect(_on_nav_quit_pressed)

func _on_hamburger_pressed() -> void:
	if nav_drawer:
		nav_drawer.open()

func _on_nav_collection_pressed() -> void:
	print("CampaignMap: Opening Collection...")
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_COLLECTION_SCREEN)

func _on_nav_events_pressed() -> void:
	print("CampaignMap: Opening Special Events...")
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_SPECIAL_EVENTS)

func _on_nav_shop_pressed() -> void:
	print("CampaignMap: Opening Premium Store...")
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_PREMIUM_STORE)

func _on_nav_settings_pressed() -> void:
	print("CampaignMap: Opening Settings...")
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_SETTINGS)

func _on_nav_quit_pressed() -> void:
	print("CampaignMap: Quitting game...")
	get_tree().quit()

## Redirect to summoner selection when no summoner is active
func _redirect_to_summoner_selection() -> void:
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SELECTION)

## =============================================================================
## CAMPAIGN BANNER
## =============================================================================

func _setup_campaign_banner() -> void:
	# Create container for banner styling
	var banner_container: Control = Control.new()
	add_child(banner_container)

	# Position centered at top
	banner_container.anchor_left = 0.5
	banner_container.anchor_right = 0.5
	banner_container.anchor_top = 0.0
	banner_container.anchor_bottom = 0.0
	banner_container.offset_left = -CAMPAIGN_BANNER_WIDTH / 2
	banner_container.offset_right = CAMPAIGN_BANNER_WIDTH / 2
	banner_container.offset_top = CAMPAIGN_BANNER_MARGIN
	banner_container.offset_bottom = CAMPAIGN_BANNER_MARGIN + CAMPAIGN_BANNER_HEIGHT

	# Dark background
	var dark_bg: ColorRect = ColorRect.new()
	dark_bg.color = Color(0.12, 0.1, 0.15, 0.95)
	dark_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	banner_container.add_child(dark_bg)

	# Fantasy border overlay
	var border: NinePatchRect = NinePatchRect.new()
	ButtonStyleFactory.apply_panel_border(border)
	border.set_anchors_preset(Control.PRESET_FULL_RECT)
	banner_container.add_child(border)

	# Button on top
	campaign_banner = Button.new()
	campaign_banner.flat = true
	campaign_banner.alignment = HORIZONTAL_ALIGNMENT_CENTER
	campaign_banner.add_theme_font_size_override("font_size", 20)
	campaign_banner.set_anchors_preset(Control.PRESET_FULL_RECT)
	banner_container.add_child(campaign_banner)

	_update_campaign_banner_text()
	campaign_banner.pressed.connect(_on_campaign_banner_pressed)

func _update_campaign_banner_text() -> void:
	if not campaign_banner:
		return

	var campaign_id: String = Campaign.get_current_campaign_id()
	var campaign_data: Dictionary = Campaign.get_campaign(campaign_id)
	var name_key: String = campaign_data.get("name_key", "")
	if not name_key.is_empty():
		campaign_banner.text = Loc.t(name_key)
	else:
		campaign_banner.text = campaign_id

func _on_campaign_banner_pressed() -> void:
	if campaign_selector_modal == null:
		campaign_selector_modal = CampaignSelectorModalScene.instantiate()
		add_child(campaign_selector_modal)
		campaign_selector_modal.campaign_selected.connect(_on_campaign_selected)
		campaign_selector_modal.closed.connect(_on_campaign_modal_closed)

	campaign_selector_modal.open()

func _on_campaign_selected(campaign_id: String) -> void:
	var success: bool = Campaign.set_current_campaign(campaign_id)
	if success:
		_update_campaign_banner_text()
		_refresh_map()

	if campaign_selector_modal:
		campaign_selector_modal.hide()

func _on_campaign_modal_closed() -> void:
	pass  # Modal hides itself

## =============================================================================
## SUMMONER ICON
## =============================================================================

func _setup_summoner_icon() -> void:
	# Only show summoner icon if a summoner has been selected
	if SummonerSelection.GetActiveSummonerId().is_empty():
		return

	summoner_icon = SummonerIconWidgetScene.instantiate()
	add_child(summoner_icon)

	# Position in top-left corner
	summoner_icon.anchor_left = 0.0
	summoner_icon.anchor_right = 0.0
	summoner_icon.anchor_top = 0.0
	summoner_icon.anchor_bottom = 0.0
	summoner_icon.offset_left = SUMMONER_ICON_MARGIN
	summoner_icon.offset_right = SUMMONER_ICON_MARGIN + SUMMONER_ICON_SIZE
	summoner_icon.offset_top = SUMMONER_ICON_MARGIN
	summoner_icon.offset_bottom = SUMMONER_ICON_MARGIN + SUMMONER_ICON_SIZE

	# Connect signal
	summoner_icon.icon_clicked.connect(_on_summoner_icon_clicked)

func _on_summoner_icon_clicked() -> void:
	# Push current scene for return navigation
	NavigationContext.push_return(SceneManager.SCENE_CAMPAIGN_MAP)
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SCREEN)

## =============================================================================
## GOLD DISPLAY
## =============================================================================

func _setup_gold_display() -> void:
	# Create gold label
	gold_label = Label.new()
	add_child(gold_label)

	# Style the label
	gold_label.add_theme_font_size_override("font_size", 20)
	gold_label.add_theme_color_override("font_color", GameColorPalette.GOLD)
	gold_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	gold_label.size = Vector2(100, GOLD_LABEL_HEIGHT)

	# Position to the right of summoner icon using actual size
	var icon_size: float = summoner_icon.size.x if summoner_icon else SUMMONER_ICON_SIZE
	gold_label.position = Vector2(
		SUMMONER_ICON_MARGIN + icon_size + 10,
		SUMMONER_ICON_MARGIN + (icon_size - GOLD_LABEL_HEIGHT) / 2
	)

	# Connect to gold changes
	Economy.campaign_gold_changed.connect(_on_campaign_gold_changed)

	# Initial update
	_update_gold_display()

func _update_gold_display() -> void:
	if gold_label == null:
		return
	var gold: int = Economy.get_campaign_gold()
	gold_label.text = Loc.t("campaign.map.gold", {"amount": gold})

func _on_campaign_gold_changed(_summoner_id: String, _gold: int) -> void:
	_update_gold_display()

## =============================================================================
## SIGNALS
## =============================================================================

func _on_event_completed(_event_id: String) -> void:
	_refresh_map()
	# Refresh the active panel if visible
	if active_panel and detail_panel.visible and not selected_event_id.is_empty():
		var event: Dictionary = _safe_dict(Campaign.get_battle(selected_event_id))
		if not event.is_empty():
			active_panel.configure(event, selected_event_id)

func _on_progress_changed() -> void:
	# Full refresh when progress changes (e.g., snapshot loaded)
	_refresh_map()
	_update_campaign_banner_text()
	if summoner_icon:
		summoner_icon.refresh()
	# Refresh the active panel if visible
	if active_panel and detail_panel.visible and not selected_event_id.is_empty():
		var event: Dictionary = _safe_dict(Campaign.get_battle(selected_event_id))
		if not event.is_empty():
			active_panel.configure(event, selected_event_id)

func _on_summoner_selection_changed(_old_summoner_id: String, _new_summoner_id: String) -> void:
	# Refresh summoner icon, map, and panel when summoner changes
	if summoner_icon:
		summoner_icon.refresh()
	_refresh_map()
	# Refresh the active panel if visible
	if active_panel and detail_panel.visible and not selected_event_id.is_empty():
		var event: Dictionary = _safe_dict(Campaign.get_battle(selected_event_id))
		if not event.is_empty():
			active_panel.configure(event, selected_event_id)
