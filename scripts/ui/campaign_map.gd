extends Control
class_name CampaignMap

## Campaign Map - Visual node-based event progression
##
## Shows campaign events as nodes on a linear path.
## First event is onboarding if not yet complete.

## Node references
@onready var back_button: Button = %BackButton
@onready var progress_label: Label = %ProgressLabel
@onready var map_container: Control = %MapContainer
@onready var detail_panel: PanelContainer = %DetailPanel
@onready var event_name_label: Label = %EventNameLabel
@onready var difficulty_label: Label = %DifficultyLabel
@onready var description_label: Label = %DescriptionLabel
@onready var reward_label: Label = %RewardLabel
@onready var start_event_button: Button = %StartEventButton

## Map layout constants
const NODE_SPACING: float = 150.0  # Horizontal spacing between nodes
const NODE_SIZE: Vector2 = Vector2(80, 80)
const PATH_COLOR: Color = Color(0.4, 0.4, 0.5)
const PATH_WIDTH: float = 4.0

## State
var selected_event_id: String = ""
var all_events: Array[Dictionary] = []
var event_nodes: Dictionary = {}  # event_id -> Node2D

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
	back_button.pressed.connect(_on_back_pressed)
	start_event_button.pressed.connect(_on_start_event_pressed)

	# Connect to campaign service
	var campaign: Node = get_node("/root/Campaign")
	if campaign:
		if campaign.has_signal("battle_completed"):
			var battle_completed_signal: Signal = campaign.get("battle_completed")
			battle_completed_signal.connect(_on_event_completed)
		if campaign.has_signal("campaign_progress_changed"):
			var campaign_progress_signal: Signal = campaign.get("campaign_progress_changed")
			campaign_progress_signal.connect(_on_progress_changed)

	# Load and display map
	_refresh_map()
	_update_progress_display()

	# Hide detail panel initially
	start_event_button.disabled = true

func _draw() -> void:
	# Draw paths connecting events
	var event_list: Array = all_events.duplicate()

	# Add onboarding event if needed
	var profile_repo: Node = get_node("/root/ProfileRepo")
	var onboarding_complete: bool = false
	if profile_repo:
		var profile: Dictionary = _safe_dict(profile_repo.call("get_active_profile"))
		if not profile.is_empty():
			var meta: Dictionary = _safe_dict(profile.get("meta", {}))
			onboarding_complete = _safe_bool(meta.get("onboarding_complete", false))

	var path_start_index: int = 0 if onboarding_complete else 1

	for i: int in range(path_start_index, event_list.size()):
		var current_id: String = ""
		var next_id: String = ""

		if i == 0 and not onboarding_complete:
			current_id = "onboarding"
			if event_list.size() > 0:
				var first_event: Dictionary = _safe_dict(event_list[0])
				next_id = _safe_string(first_event.get("id", ""))
		else:
			var idx: int = i - (0 if onboarding_complete else 1)
			if idx >= 0 and idx < event_list.size():
				var current_event: Dictionary = _safe_dict(event_list[idx])
				current_id = _safe_string(current_event.get("id", ""))
				if idx + 1 < event_list.size():
					var next_event: Dictionary = _safe_dict(event_list[idx + 1])
					next_id = _safe_string(next_event.get("id", ""))

		if current_id != "" and next_id != "" and event_nodes.has(current_id) and event_nodes.has(next_id):
			var start_node: Control = event_nodes[current_id]
			var end_node: Control = event_nodes[next_id]
			var start_pos: Vector2 = start_node.position + start_node.size / 2
			var end_pos: Vector2 = end_node.position + end_node.size / 2
			draw_line(start_pos, end_pos, PATH_COLOR, PATH_WIDTH)

## =============================================================================
## MAP DISPLAY
## =============================================================================

func _refresh_map() -> void:
	# Clear existing nodes
	for child: Node in map_container.get_children():
		child.queue_free()
	event_nodes.clear()

	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		push_error("CampaignMap: Campaign service not found!")
		return

	var events_variant: Variant = campaign.call("get_all_battles")
	var events_array: Array = _safe_array(events_variant)
	all_events.assign(events_array)

	# Check if onboarding is complete
	var profile_repo: Node = get_node("/root/ProfileRepo")
	var onboarding_complete: bool = false
	if profile_repo:
		var profile: Dictionary = _safe_dict(profile_repo.call("get_active_profile"))
		if not profile.is_empty():
			var meta: Dictionary = _safe_dict(profile.get("meta", {}))
			onboarding_complete = _safe_bool(meta.get("onboarding_complete", false))

	# Add onboarding event as first node if not complete
	var node_index: int = 0
	if not onboarding_complete:
		var onboarding_node: Control = _create_event_node("onboarding", node_index, true, false)
		map_container.add_child(onboarding_node)
		event_nodes["onboarding"] = onboarding_node
		node_index += 1

	# Create nodes for all events
	for event: Dictionary in all_events:
		var event_id: String = _safe_string(event.get("id", ""))
		if event_id == "":
			continue

		var is_completed: bool = _safe_bool(campaign.call("is_battle_completed", event_id))
		var is_unlocked: bool = _safe_bool(campaign.call("is_battle_unlocked", event_id))

		var event_node: Control = _create_event_node(event_id, node_index, is_unlocked, is_completed)
		map_container.add_child(event_node)
		event_nodes[event_id] = event_node
		node_index += 1

	# Trigger redraw for paths
	queue_redraw()

	print("CampaignMap: Created %d event nodes" % event_nodes.size())

func _create_event_node(event_id: String, index: int, is_unlocked: bool, is_completed: bool) -> Control:
	var node_container: Control = Control.new()
	node_container.custom_minimum_size = NODE_SIZE
	node_container.position = Vector2(100 + index * NODE_SPACING, 200)

	# Create visual button
	var button: Button = Button.new()
	button.custom_minimum_size = NODE_SIZE
	button.size = NODE_SIZE

	# Style based on state
	if event_id == "onboarding":
		button.text = "⭐"
		button.add_theme_color_override("font_color", Color(1.0, 0.85, 0.3))
	elif is_completed:
		button.text = "✓"
		button.add_theme_color_override("font_color", Color(0.3, 1.0, 0.3))
	elif is_unlocked:
		button.text = str(index + 1)
		button.add_theme_color_override("font_color", Color(1.0, 1.0, 1.0))
	else:
		button.text = "🔒"
		button.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5))
		button.disabled = true

	# Make button circular-ish
	var style: StyleBoxFlat = StyleBoxFlat.new()
	if event_id == "onboarding":
		style.bg_color = Color(0.4, 0.35, 0.2)
		style.border_color = Color(1.0, 0.85, 0.3)
	elif is_completed:
		style.bg_color = Color(0.2, 0.4, 0.2)
		style.border_color = Color(0.3, 1.0, 0.3)
	elif is_unlocked:
		style.bg_color = Color(0.3, 0.3, 0.4)
		style.border_color = Color(0.5, 0.7, 1.0)
	else:
		style.bg_color = Color(0.2, 0.2, 0.2)
		style.border_color = Color(0.4, 0.4, 0.4)

	style.corner_radius_top_left = 40
	style.corner_radius_top_right = 40
	style.corner_radius_bottom_left = 40
	style.corner_radius_bottom_right = 40
	style.border_width_left = 3
	style.border_width_right = 3
	style.border_width_top = 3
	style.border_width_bottom = 3

	button.add_theme_stylebox_override("normal", style)
	button.add_theme_font_size_override("font_size", 32)

	# Connect click handler
	if is_unlocked or event_id == "onboarding":
		button.pressed.connect(_on_event_node_clicked.bind(event_id))

	node_container.add_child(button)
	return node_container

func _on_event_node_clicked(event_id: String) -> void:
	selected_event_id = event_id
	_update_detail_panel()
	print("CampaignMap: Selected event: %s" % event_id)

## =============================================================================
## DETAIL PANEL
## =============================================================================

func _update_detail_panel() -> void:
	if selected_event_id == "":
		event_name_label.text = "Select an Event"
		difficulty_label.text = ""
		description_label.text = "Click an event node to see details."
		reward_label.text = ""
		start_event_button.disabled = true
		return

	# Handle onboarding event
	if selected_event_id == "onboarding":
		event_name_label.text = "Begin Your Journey"
		difficulty_label.text = ""
		description_label.text = "Choose your hero and receive your first card to begin your adventure!"
		reward_label.text = ""
		start_event_button.text = "START"
		start_event_button.disabled = false
		return

	# Handle campaign events
	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		return

	var event: Dictionary = _safe_dict(campaign.call("get_battle", selected_event_id))
	if event.is_empty():
		return

	# Update labels
	event_name_label.text = _safe_string(event.get("name", "Unknown"), "Unknown")

	var difficulty: int = _safe_int(event.get("difficulty", 1), 1)
	var diff_stars: String = "★".repeat(difficulty) + "☆".repeat(5 - difficulty)
	difficulty_label.text = "Difficulty: %s" % diff_stars

	description_label.text = _safe_string(event.get("description", "No description."), "No description.")

	# Reward summary
	var reward_type: String = _safe_string(event.get("reward_type", "fixed"), "fixed")
	var reward_cards: Array = _safe_array(event.get("reward_cards", []))
	var reward_text: String = ""

	match reward_type:
		"fixed":
			var card_names: Array[String] = []
			for reward_item: Variant in reward_cards:
				var reward: Dictionary = _safe_dict(reward_item)
				var count: int = _safe_int(reward.get("count", 1), 1)
				var catalog_id: String = _safe_string(reward.get("catalog_id", ""))
				if count > 1:
					card_names.append("%dx %s" % [count, catalog_id.capitalize()])
				else:
					card_names.append(catalog_id.capitalize())
			reward_text = "Reward: " + ", ".join(card_names)

		"choice":
			var options: Array[String] = []
			for reward_item: Variant in reward_cards:
				var reward: Dictionary = _safe_dict(reward_item)
				var catalog_id: String = _safe_string(reward.get("catalog_id", ""))
				options.append(catalog_id.capitalize())
			reward_text = "Reward: Choose from " + ", ".join(options)

		"random":
			var count: int = 0
			for reward_item: Variant in reward_cards:
				var reward: Dictionary = _safe_dict(reward_item)
				var reward_count: int = _safe_int(reward.get("count", 1), 1)
				count += reward_count
			reward_text = "Reward: Random (%d cards)" % count

	reward_label.text = reward_text

	# Enable/disable start button
	var is_completed: bool = _safe_bool(campaign.call("is_battle_completed", selected_event_id))
	if is_completed:
		start_event_button.text = "REPLAY (no reward)"
		start_event_button.disabled = false
	else:
		start_event_button.text = "START EVENT"
		start_event_button.disabled = false

## =============================================================================
## PROGRESS DISPLAY
## =============================================================================

func _update_progress_display() -> void:
	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		return

	var completed_events: Array = _safe_array(campaign.call("get_completed_battles"))
	var completed: int = completed_events.size()

	var total_events: Array = _safe_array(campaign.call("get_all_battles"))
	var total: int = total_events.size()

	progress_label.text = "%d / %d Complete" % [completed, total]

## =============================================================================
## EVENT START
## =============================================================================

func _on_start_event_pressed() -> void:
	if selected_event_id == "":
		return

	# Handle onboarding event
	if selected_event_id == "onboarding":
		print("CampaignMap: Starting onboarding...")
		get_tree().change_scene_to_file("res://scenes/ui/hero_selection.tscn")
		return

	print("CampaignMap: Starting event: %s" % selected_event_id)

	# Store selected event in campaign service
	var campaign: Node = get_node("/root/Campaign")
	if campaign:
		var profile_repo: Node = get_node("/root/ProfileRepo")
		var profile: Dictionary = _safe_dict(profile_repo.call("get_active_profile"))
		if not profile.is_empty():
			if not profile.has("campaign_progress"):
				profile["campaign_progress"] = {}
			var campaign_progress: Dictionary = _safe_dict(profile["campaign_progress"])
			campaign_progress["current_battle"] = selected_event_id
			profile_repo.call("save_profile", true)

	# Configure battle context
	var battle_context: Node = get_node("/root/BattleContext")
	if battle_context:
		battle_context.call("configure_campaign_battle", selected_event_id)

	# Launch battle scene
	get_tree().change_scene_to_file("res://scenes/battlefield/battle_3d.tscn")

## =============================================================================
## NAVIGATION
## =============================================================================

func _on_back_pressed() -> void:
	print("CampaignMap: Returning to game mode menu")
	get_tree().change_scene_to_file("res://scenes/ui/game_mode_menu.tscn")

## =============================================================================
## SIGNALS
## =============================================================================

func _on_event_completed(_event_id: String) -> void:
	_refresh_map()
	_update_progress_display()
	_update_detail_panel()

func _on_progress_changed() -> void:
	_update_progress_display()
