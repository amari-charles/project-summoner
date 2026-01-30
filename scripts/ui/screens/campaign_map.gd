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
@onready var event_name_label: Label = %EventNameLabel
@onready var difficulty_container: HBoxContainer = %DifficultyContainer
@onready var difficulty_label: Label = %DifficultyLabel
@onready var stars_container: HBoxContainer = %StarsContainer
@onready var description_label: Label = %DescriptionLabel
@onready var reward_label: Label = %RewardLabel
@onready var deck_column: VBoxContainer = $DetailPanel/MarginContainer/VBoxContainer/ContentColumns/RightColumn
@onready var deck_header_label: Label = $DetailPanel/MarginContainer/VBoxContainer/ContentColumns/RightColumn/DeckHeaderLabel
@onready var active_deck_label: Label = %ActiveDeckLabel
@onready var change_deck_button: Button = %ChangeDeckButton
@onready var deck_selector: ItemList = %DeckSelector
@onready var deck_info_label: Label = %DeckInfoLabel
@onready var active_deck_indicator: Label = %ActiveDeckIndicator
@onready var start_event_button: Button = %StartEventButton

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

## Kenny UI Pack star textures for difficulty display
const STAR_FILLED_TEXTURE: String = "res://assets/ui/kenny/PNG/Yellow/Default/star.png"
const STAR_EMPTY_TEXTURE: String = "res://assets/ui/kenny/PNG/Grey/Default/star_outline.png"
const STAR_SIZE: int = 24  # Size of each star icon

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

## Deck selection state
var available_decks: Array[Dictionary] = []
var selected_deck_id: String = ""
var _deck_selector_visible: bool = false

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

## Choice modal for path decisions
var choice_modal: Panel = null
var choice_options_container: VBoxContainer = null
var pending_choice_node_id: String = ""

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

## Get display name for a card from CardCatalog, with fallback
func _get_card_display_name(catalog: Node, catalog_id: String) -> String:
	if catalog and catalog.has_method("get_card"):
		var card_data: Dictionary = _safe_dict(catalog.call("get_card", catalog_id))
		if not card_data.is_empty():
			return _safe_string(card_data.get("card_name", catalog_id), catalog_id)
	# Fallback: convert catalog_id to title case (fire_wisp → Fire Wisp)
	return catalog_id.replace("_", " ").capitalize()

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CampaignMap: Initializing...")

	# Connect buttons
	locator_button.pressed.connect(_on_center_latest_pressed)
	start_event_button.pressed.connect(_on_start_event_pressed)
	deck_selector.item_selected.connect(_on_deck_selected)
	change_deck_button.pressed.connect(_on_change_deck_pressed)

	# Set localized text for static labels
	deck_header_label.text = Loc.t("campaign.map.deck_header")

	# Background texture is set directly in the scene file

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
	_update_detail_panel()
	_show_popup()
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


## Update difficulty display with Kenny star icons
func _update_difficulty_stars(difficulty: int) -> void:
	# Clear existing stars
	for child: Node in stars_container.get_children():
		child.queue_free()

	# Hide entire container if no difficulty
	if difficulty <= 0:
		difficulty_container.visible = false
		return

	difficulty_container.visible = true
	difficulty_label.text = Loc.t("campaign.map.difficulty_label")

	# Load star textures
	var filled_tex: Texture2D = load(STAR_FILLED_TEXTURE)
	var empty_tex: Texture2D = load(STAR_EMPTY_TEXTURE)

	# Create 5 stars (filled for difficulty level, empty for rest)
	for i: int in range(5):
		var star: TextureRect = TextureRect.new()
		star.texture = filled_tex if i < difficulty else empty_tex
		star.custom_minimum_size = Vector2(STAR_SIZE, STAR_SIZE)
		star.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		star.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		stars_container.add_child(star)


func _update_detail_panel() -> void:
	if selected_event_id == "":
		event_name_label.text = Loc.t("campaign.map.select_event")
		_update_difficulty_stars(0)  # Hide stars
		description_label.text = Loc.t("campaign.map.click_to_see_details")
		reward_label.text = ""
		start_event_button.disabled = true
		# Clear deck selection UI
		deck_selector.clear()
		deck_info_label.text = ""
		active_deck_indicator.text = ""
		return

	# Get event data
	var event: Dictionary = _safe_dict(Campaign.get_battle(selected_event_id))
	if event.is_empty():
		return

	# Check if this event requires deck selection
	var requires_deck: bool = _safe_bool(event.get("requires_deck", true), true)
	var event_type: StringName = StringName(event.get("event_type", EventTypeIDs.BATTLE))

	# Show/hide deck selection based on event configuration
	if deck_column:
		deck_column.visible = requires_deck

	# Load available decks only if required
	if requires_deck:
		_load_decks()

	# Update labels
	event_name_label.text = _safe_string(event.get("name", "Unknown"), "Unknown")

	# Show difficulty for battles, hide for other event types
	var difficulty: int = _safe_int(event.get("difficulty", 0), 0)
	_update_difficulty_stars(difficulty)

	description_label.text = _safe_string(event.get("description", "No description."), "No description.")

	# Reward summary - build multi-line reward text
	var reward_type: StringName = StringName(event.get("reward_type", RewardTypeIDs.FIXED))
	var reward_cards: Array = _safe_array(event.get("reward_cards", []))
	var gold_reward: int = _safe_int(event.get("gold_reward", 0), 0)
	var summoner_xp_reward: int = _safe_int(event.get("summoner_xp_reward", 0), 0)

	# Get CardCatalog for proper card names
	var catalog: Node = CardCatalog
	var reward_lines: Array[String] = []

	# Gold reward (if > 0)
	if gold_reward > 0:
		reward_lines.append(Loc.t("campaign.rewards.gold", {"amount": gold_reward}))

	# Summoner XP reward (if > 0)
	if summoner_xp_reward > 0:
		reward_lines.append(Loc.t("campaign.rewards.summoner_xp", {"amount": summoner_xp_reward}))

	# Card reward based on type
	match reward_type:
		RewardTypeIDs.FIXED:
			if reward_cards.size() > 0:
				var card_names: Array[String] = []
				for reward_item: Variant in reward_cards:
					var reward: Dictionary = _safe_dict(reward_item)
					var count: int = _safe_int(reward.get("count", 1), 1)
					var catalog_id: String = _safe_string(reward.get("catalog_id", ""))
					var card_name: String = _get_card_display_name(catalog, catalog_id)
					if count > 1:
						card_names.append("%dx %s" % [count, card_name])
					else:
						card_names.append(card_name)
				reward_lines.append(Loc.t("campaign.rewards.fixed", {"cards": ", ".join(card_names)}))

		RewardTypeIDs.FLEXIBLE:
			# Simple "1 Card" display - don't spoil the options
			reward_lines.append(Loc.t("campaign.rewards.card_choice"))

		RewardTypeIDs.NONE:
			pass  # No card reward line needed

	reward_label.text = "\n".join(reward_lines)

	# Enable/disable start button based on completion and repeatability
	var is_completed: bool = _safe_bool(Campaign.is_battle_completed(selected_event_id))
	var is_repeatable: bool = _safe_bool(event.get("repeatable", true))

	if is_completed:
		if is_repeatable:
			start_event_button.text = Loc.t("campaign.map.button_replay")
			start_event_button.disabled = false
		else:
			# Non-repeatable events cannot be replayed
			start_event_button.text = Loc.t("campaign.map.button_completed")
			start_event_button.disabled = true
	else:
		start_event_button.text = Loc.t("campaign.map.button_start_event")
		start_event_button.disabled = false

## =============================================================================
## DECK SELECTION
## =============================================================================

func _load_decks() -> void:
	# Clear existing items
	deck_selector.clear()
	available_decks.clear()

	# Get active summoner ID to filter decks
	var active_summoner_id: String = ""
	var result: Variant = SummonerSelection.GetActiveSummonerId()
	if result is String:
		active_summoner_id = result

	# Get decks filtered by active summoner
	var decks_array: Array
	if not active_summoner_id.is_empty():
		var decks_variant: Variant = Decks.list_decks_for_summoner(active_summoner_id)
		decks_array = _safe_array(decks_variant)
	else:
		var decks_variant: Variant = Decks.list_decks()
		decks_array = _safe_array(decks_variant)
	available_decks.assign(decks_array)

	if available_decks.is_empty():
		active_deck_label.text = Loc.t("campaign.map.error_create_deck_first")
		deck_info_label.text = ""
		active_deck_indicator.text = ""
		change_deck_button.visible = false
		return

	change_deck_button.visible = true

	# Populate ItemList with deck names
	for deck: Dictionary in available_decks:
		var deck_name: String = _safe_string(deck.get("name", "Unnamed Deck"), "Unnamed Deck")
		deck_selector.add_item(deck_name)

	# Get currently selected deck from profile
	var profile_variant: Variant = ProfileRepo.get_active_profile()
	var profile: Dictionary = _safe_dict(profile_variant)
	var found_deck: bool = false
	if not profile.is_empty() and profile.has("meta"):
		var meta: Dictionary = _safe_dict(profile.get("meta"))
		var active_deck: String = _safe_string(meta.get("selected_deck", ""))

		# Find the deck in available_decks and select it
		for i: int in range(available_decks.size()):
			var deck: Dictionary = available_decks[i]
			var deck_id: String = _safe_string(deck.get("id", ""))
			if deck_id == active_deck:
				deck_selector.select(i)
				selected_deck_id = deck_id
				found_deck = true
				break

	# Auto-select first deck if none selected
	if not found_deck and available_decks.size() > 0:
		var first_deck: Dictionary = available_decks[0]
		selected_deck_id = _safe_string(first_deck.get("id", ""))
		deck_selector.select(0)
		# Save auto-selection to profile
		_save_deck_selection()

	# Update deck info display
	_update_deck_info()

func _on_deck_selected(index: int) -> void:
	if index < 0 or index >= available_decks.size():
		return

	var deck: Dictionary = available_decks[index]
	selected_deck_id = _safe_string(deck.get("id", ""))

	# Save selection to profile
	_save_deck_selection()

	# Update deck info display
	_update_deck_info()

	# Hide selector after selection
	_deck_selector_visible = false
	deck_selector.visible = false
	change_deck_button.text = Loc.t("campaign.map.change_deck")

func _on_change_deck_pressed() -> void:
	_deck_selector_visible = not _deck_selector_visible
	deck_selector.visible = _deck_selector_visible
	if _deck_selector_visible:
		change_deck_button.text = Loc.t("campaign.map.done")
	else:
		change_deck_button.text = Loc.t("campaign.map.change_deck")

func _save_deck_selection() -> void:
	var profile_variant: Variant = ProfileRepo.get_active_profile()
	var profile: Dictionary = _safe_dict(profile_variant)
	if not profile.is_empty():
		if not profile.has("meta"):
			profile["meta"] = {}
		var meta: Dictionary = _safe_dict(profile.get("meta"))
		meta["selected_deck"] = selected_deck_id
		ProfileRepo.save_profile(true)  # Immediate save

func _update_deck_info() -> void:
	if selected_deck_id.is_empty():
		active_deck_label.text = Loc.t("campaign.map.no_deck_selected")
		deck_info_label.text = ""
		active_deck_indicator.text = ""
		return

	# Find the selected deck
	var selected_deck: Dictionary = {}
	for deck: Dictionary in available_decks:
		if _safe_string(deck.get("id", "")) == selected_deck_id:
			selected_deck = deck
			break

	if selected_deck.is_empty():
		active_deck_label.text = Loc.t("campaign.map.no_deck_selected")
		deck_info_label.text = ""
		active_deck_indicator.text = ""
		return

	# Show deck name
	var deck_name: String = _safe_string(selected_deck.get("name", "Unnamed Deck"), "Unnamed Deck")
	active_deck_label.text = deck_name

	# Show card count
	var card_instance_ids: Array = _safe_array(selected_deck.get("card_instance_ids", []))
	var card_count: int = card_instance_ids.size()
	deck_info_label.text = Loc.t("campaign.map.deck_card_count", {"count": card_count})

	# Validate deck and show status
	var is_valid: bool = _validate_selected_deck()
	if is_valid:
		active_deck_indicator.text = Loc.t("campaign.map.deck_status_ready")
		active_deck_indicator.modulate = Color(0.3, 1.0, 0.3)
	else:
		active_deck_indicator.text = Loc.t("campaign.map.deck_status_invalid")
		active_deck_indicator.modulate = Color(1.0, 0.5, 0.0)

func _validate_selected_deck() -> bool:
	if selected_deck_id.is_empty():
		return false

	var is_valid_variant: Variant = Decks.validate_deck(selected_deck_id)
	return _safe_bool(is_valid_variant, false)

## =============================================================================
## EVENT START
## =============================================================================

func _on_start_event_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if selected_event_id == "":
		return

	# Get event data to check event_type
	var event: Dictionary = _safe_dict(Campaign.get_battle(selected_event_id))
	if event.is_empty():
		return

	var event_type: StringName = StringName(event.get("event_type", EventTypeIDs.BATTLE))
	var requires_deck: bool = _safe_bool(event.get("requires_deck", true), true)

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
	#
	# CARAVAN EVENT FLOW:
	# 1. CampaignMap configures EventContext with event data
	# 2. Navigate directly to ShopScreen (not EventScreen)
	# 3. ShopScreen detects EventContext.current_event_id is set
	# 4. ShopScreen switches to caravan mode:
	#    - Hides back button, shows "Leave Caravan" button
	#    - Loads event sequence from event_config
	#    - Plays dialogue on top of shop UI (seamless browsing during dialogue)
	# 5. After dialogue completes, "Leave Caravan" button becomes visible
	# 6. User can browse/purchase, then click "Leave Caravan"
	# 7. Confirmation modal appears (repeatable check)
	# 8. On confirm: EventContext.complete_event() marks event done
	# 9. NavigationContext returns to SCENE_CAMPAIGN_MAP
	#
	# This approach allows dialogue to play while shop is visible, avoiding
	# the jarring dialogue → black screen → shop transition.
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

	# Handle choice events - show choice modal
	if event_type == EventTypeIDs.CHOICE:
		print("CampaignMap: Opening choice modal for: %s" % selected_event_id)
		_show_choice_modal(event)
		return

	# Handle battle events
	print("CampaignMap: Starting battle event: %s" % selected_event_id)

	# Validate deck selection only if this event requires a deck
	if requires_deck:
		if selected_deck_id.is_empty():
			push_error("CampaignMap: No deck selected!")
			# Update UI to show error
			active_deck_indicator.text = Loc.t("campaign.map.deck_status_select_first")
			active_deck_indicator.modulate = Color(1.0, 0.3, 0.0)
			return

		if not _validate_selected_deck():
			push_error("CampaignMap: Selected deck is invalid!")
			# Error already shown in UI by _update_deck_info
			return

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

	# Position to the right of summoner icon using direct position
	gold_label.position = Vector2(
		SUMMONER_ICON_MARGIN + SUMMONER_ICON_SIZE + 10,
		SUMMONER_ICON_MARGIN + (SUMMONER_ICON_SIZE - GOLD_LABEL_HEIGHT) / 2
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
## CHOICE MODAL
## =============================================================================

## Show the choice modal for a path decision
func _show_choice_modal(event: Dictionary) -> void:
	pending_choice_node_id = selected_event_id

	# Get options from event data
	var options: Array = _safe_array(event.get("options", []))
	if options.is_empty():
		push_warning("CampaignMap: Choice event '%s' has no options" % selected_event_id)
		return

	# Create modal if it doesn't exist
	if choice_modal == null:
		_create_choice_modal()

	# Clear previous options
	for child: Node in choice_options_container.get_children():
		child.queue_free()

	# Get the choice name from event
	var choice_name: String = _safe_string(event.get("name", "Choose Your Path"))

	# Add title
	var title: Label = Label.new()
	title.text = choice_name
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 24)
	choice_options_container.add_child(title)

	# Add spacing
	var spacer: Control = Control.new()
	spacer.custom_minimum_size = Vector2(0, 20)
	choice_options_container.add_child(spacer)

	# Add option buttons
	for option_variant: Variant in options:
		if not option_variant is Dictionary:
			continue
		var option: Dictionary = option_variant
		var option_id: String = _safe_string(option.get("id", ""))
		var label_key: String = _safe_string(option.get("label_key", ""))
		var desc_key: String = _safe_string(option.get("description_key", ""))

		var label_text: String = Loc.t(label_key) if not label_key.is_empty() else option_id
		var desc_text: String = Loc.t(desc_key) if not desc_key.is_empty() else ""

		var option_button: Button = Button.new()
		option_button.text = label_text
		option_button.custom_minimum_size = Vector2(300, 50)
		option_button.pressed.connect(_on_choice_option_selected.bind(option_id))
		choice_options_container.add_child(option_button)

		# Add description below button if available
		if not desc_text.is_empty():
			var desc_label: Label = Label.new()
			desc_label.text = desc_text
			desc_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			desc_label.add_theme_font_size_override("font_size", 14)
			desc_label.add_theme_color_override("font_color", Color(0.7, 0.7, 0.7))
			choice_options_container.add_child(desc_label)

			var option_spacer: Control = Control.new()
			option_spacer.custom_minimum_size = Vector2(0, 15)
			choice_options_container.add_child(option_spacer)

	# Show the modal centered
	var viewport_size: Vector2 = get_viewport_rect().size
	choice_modal.size = Vector2(400, 350)
	choice_modal.position = (viewport_size - choice_modal.size) / 2
	choice_modal.visible = true

	# Hide detail panel while choice modal is open
	detail_panel.visible = false


## Create the choice modal UI
func _create_choice_modal() -> void:
	choice_modal = Panel.new()
	choice_modal.visible = false
	add_child(choice_modal)

	# Dark background style
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.15, 0.12, 0.18, 0.98)
	style.border_color = Color(0.4, 0.35, 0.5)
	style.set_border_width_all(2)
	style.set_corner_radius_all(8)
	choice_modal.add_theme_stylebox_override("panel", style)

	# Container for content
	var margin: MarginContainer = MarginContainer.new()
	margin.set_anchors_preset(Control.PRESET_FULL_RECT)
	margin.add_theme_constant_override("margin_left", 20)
	margin.add_theme_constant_override("margin_right", 20)
	margin.add_theme_constant_override("margin_top", 20)
	margin.add_theme_constant_override("margin_bottom", 20)
	choice_modal.add_child(margin)

	choice_options_container = VBoxContainer.new()
	choice_options_container.alignment = BoxContainer.ALIGNMENT_CENTER
	margin.add_child(choice_options_container)


## Handle choice option selection
func _on_choice_option_selected(option_id: String) -> void:
	print("CampaignMap: Player chose option '%s' for node '%s'" % [option_id, pending_choice_node_id])

	# Hide modal
	choice_modal.visible = false

	# Record the choice (this affects which edges are traversable)
	Campaign.record_choice(pending_choice_node_id, option_id)

	# Mark the choice node as completed
	Campaign.complete_battle(pending_choice_node_id)

	# Refresh map to show newly unlocked paths
	_refresh_map()

	pending_choice_node_id = ""


## =============================================================================
## SIGNALS
## =============================================================================

func _on_event_completed(_event_id: String) -> void:
	_refresh_map()
	_update_detail_panel()

func _on_progress_changed() -> void:
	# Full refresh when progress changes (e.g., snapshot loaded)
	_refresh_map()
	_update_detail_panel()
	_update_campaign_banner_text()
	if summoner_icon:
		summoner_icon.refresh()

func _on_summoner_selection_changed(_old_summoner_id: String, _new_summoner_id: String) -> void:
	# Refresh summoner icon, map, and deck list when summoner changes
	if summoner_icon:
		summoner_icon.refresh()
	_refresh_map()
	_update_detail_panel()
