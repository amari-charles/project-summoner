extends Control
class_name CampaignScreen

## CampaignScreen - Browse and select campaign battles
##
## Shows list of battles with their status (locked/available/completed).
## Player selects a battle to see details and start it.

## Node references
@onready var back_button: Button = %BackButton
@onready var progress_label: Label = %ProgressLabel
@onready var battle_list: VBoxContainer = %BattleList
@onready var detail_panel: PanelContainer = %DetailPanel
@onready var battle_name_label: Label = %BattleNameLabel
@onready var difficulty_label: Label = %DifficultyLabel
@onready var description_label: Label = %DescriptionLabel
@onready var reward_label: Label = %RewardLabel
@onready var start_battle_button: Button = %StartBattleButton

## State
var selected_battle_id: String = ""
var all_battles: Array[Dictionary] = []

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CampaignScreen: Initializing...")

	# Connect buttons
	back_button.pressed.connect(_on_back_pressed)
	start_battle_button.pressed.connect(_on_start_battle_pressed)

	# Connect to campaign service
	var campaign: Node = get_node("/root/Campaign")
	if campaign:
		if campaign.has_signal("battle_completed"):
			var battle_completed_signal: Signal = campaign.get("battle_completed")
			battle_completed_signal.connect(_on_battle_completed)
		if campaign.has_signal("campaign_progress_changed"):
			var campaign_progress_changed_signal: Signal = campaign.get("campaign_progress_changed")
			campaign_progress_changed_signal.connect(_on_progress_changed)

	# Load battles
	_refresh_battle_list()
	_update_progress_display()

	# Hide detail panel initially
	start_battle_button.disabled = true

## =============================================================================
## BATTLE LIST
## =============================================================================

func _refresh_battle_list() -> void:
	# Clear existing list
	for child: Node in battle_list.get_children():
		child.queue_free()

	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		push_error("CampaignScreen: Campaign service not found!")
		return

	var battles_variant: Variant = campaign.call("get_all_battles")
	if battles_variant is Array:
		all_battles.assign(battles_variant as Array)
	else:
		all_battles = []

	if all_battles.is_empty():
		var label: Label = Label.new()
		label.text = "No battles available."
		label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		label.add_theme_font_size_override("font_size", 20)
		battle_list.add_child(label)
		return

	# Create battle list items
	for battle: Dictionary in all_battles:
		var battle_item: PanelContainer = _create_battle_list_item(battle)
		battle_list.add_child(battle_item)

	print("CampaignScreen: Loaded %d battles" % all_battles.size())

func _create_battle_list_item(battle_data: Dictionary) -> PanelContainer:
	var panel: PanelContainer = PanelContainer.new()
	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 15)
	margin.add_theme_constant_override("margin_top", 10)
	margin.add_theme_constant_override("margin_right", 15)
	margin.add_theme_constant_override("margin_bottom", 10)
	panel.add_child(margin)

	var hbox: HBoxContainer = HBoxContainer.new()
	margin.add_child(hbox)

	var campaign: Node = get_node("/root/Campaign")
	var battle_id_variant: Variant = battle_data.get("id", "")
	var battle_id: String = battle_id_variant as String if battle_id_variant is String else ""
	var is_completed_variant: Variant = campaign.call("is_battle_completed", battle_id)
	var is_completed: bool = is_completed_variant as bool if is_completed_variant is bool else false
	var is_unlocked_variant: Variant = campaign.call("is_battle_unlocked", battle_id)
	var is_unlocked: bool = is_unlocked_variant as bool if is_unlocked_variant is bool else false

	# Battle name
	var name_label: Label = Label.new()
	var name_variant: Variant = battle_data.get("name", "Unknown Battle")
	name_label.text = name_variant as String if name_variant is String else "Unknown Battle"
	name_label.add_theme_font_size_override("font_size", 20)
	name_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hbox.add_child(name_label)

	# Status indicator
	var status_label: Label = Label.new()
	if is_completed:
		status_label.text = "✓ COMPLETE"
		status_label.add_theme_color_override("font_color", Color(0.3, 1.0, 0.3))
	elif is_unlocked:
		status_label.text = "AVAILABLE"
		status_label.add_theme_color_override("font_color", Color(1.0, 0.8, 0.3))
	else:
		status_label.text = "🔒 LOCKED"
		status_label.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5))

	status_label.add_theme_font_size_override("font_size", 18)
	hbox.add_child(status_label)

	# Make clickable if unlocked
	if is_unlocked:
		var button: Button = Button.new()
		button.flat = true
		button.custom_minimum_size = panel.custom_minimum_size
		button.pressed.connect(_on_battle_selected.bind(battle_id))
		panel.add_child(button)

		# Style for selected
		if battle_id == selected_battle_id:
			var style: StyleBoxFlat = StyleBoxFlat.new()
			style.bg_color = Color(0.3, 0.3, 0.4)
			style.border_width_left = 3
			style.border_width_right = 3
			style.border_width_top = 3
			style.border_width_bottom = 3
			style.border_color = Color(0.5, 0.7, 1.0)
			panel.add_theme_stylebox_override("panel", style)

	return panel

func _on_battle_selected(battle_id: String) -> void:
	selected_battle_id = battle_id
	_refresh_battle_list()  # Refresh to show selection
	_update_detail_panel()
	print("CampaignScreen: Selected battle: %s" % battle_id)

## =============================================================================
## DETAIL PANEL
## =============================================================================

func _update_detail_panel() -> void:
	if selected_battle_id == "":
		battle_name_label.text = "Select a Battle"
		difficulty_label.text = "Difficulty: -"
		description_label.text = "Select a battle to see details."
		reward_label.text = "Reward: -"
		start_battle_button.disabled = true
		return

	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		return

	var battle_variant: Variant = campaign.call("get_battle", selected_battle_id)
	var battle: Dictionary = battle_variant as Dictionary if battle_variant is Dictionary else {}
	if battle.is_empty():
		return

	# Update labels
	var name_variant: Variant = battle.get("name", "Unknown")
	battle_name_label.text = name_variant as String if name_variant is String else "Unknown"

	var difficulty_variant: Variant = battle.get("difficulty", 1)
	var difficulty: int = difficulty_variant as int if difficulty_variant is int else 1
	var diff_stars: String = "★".repeat(difficulty) + "☆".repeat(5 - difficulty)
	difficulty_label.text = "Difficulty: %s" % diff_stars

	var description_variant: Variant = battle.get("description", "No description.")
	description_label.text = description_variant as String if description_variant is String else "No description."

	# Reward summary
	var reward_type_variant: Variant = battle.get("reward_type", "fixed")
	var reward_type: String = reward_type_variant as String if reward_type_variant is String else "fixed"
	var reward_cards_variant: Variant = battle.get("reward_cards", [])
	var reward_cards: Array = reward_cards_variant as Array if reward_cards_variant is Array else []
	var reward_text: String = ""

	match reward_type:
		"fixed":
			var card_names: Array[String] = []
			for reward_item: Variant in reward_cards:
				var reward: Dictionary = reward_item as Dictionary if reward_item is Dictionary else {}
				var count_variant: Variant = reward.get("count", 1)
				var count: int = count_variant as int if count_variant is int else 1
				var catalog_id_variant: Variant = reward.get("catalog_id", "")
				var catalog_id: String = catalog_id_variant as String if catalog_id_variant is String else ""
				if count > 1:
					card_names.append("%dx %s" % [count, catalog_id.capitalize()])
				else:
					card_names.append(catalog_id.capitalize())
			reward_text = "Reward: " + ", ".join(card_names)

		"choice":
			var options: Array[String] = []
			for reward_item: Variant in reward_cards:
				var reward: Dictionary = reward_item as Dictionary if reward_item is Dictionary else {}
				var catalog_id_variant: Variant = reward.get("catalog_id", "")
				var catalog_id: String = catalog_id_variant as String if catalog_id_variant is String else ""
				options.append(catalog_id.capitalize())
			reward_text = "Reward: Choose from " + ", ".join(options)

		"random":
			var count: int = 0
			for reward_item: Variant in reward_cards:
				var reward: Dictionary = reward_item as Dictionary if reward_item is Dictionary else {}
				var count_variant: Variant = reward.get("count", 1)
				var reward_count: int = count_variant as int if count_variant is int else 1
				count += reward_count
			reward_text = "Reward: Random (%d cards)" % count

	reward_label.text = reward_text

	# Enable/disable start button
	var is_completed_variant: Variant = campaign.call("is_battle_completed", selected_battle_id)
	var is_completed: bool = is_completed_variant as bool if is_completed_variant is bool else false
	if is_completed:
		start_battle_button.text = "REPLAY BATTLE (no reward)"
		start_battle_button.disabled = false
	else:
		start_battle_button.text = "START BATTLE"
		start_battle_button.disabled = false

## =============================================================================
## PROGRESS DISPLAY
## =============================================================================

func _update_progress_display() -> void:
	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		return

	var completed_variant: Variant = campaign.call("get_completed_battles")
	var completed_battles: Array = completed_variant as Array if completed_variant is Array else []
	var completed: int = completed_battles.size()

	var total_variant: Variant = campaign.call("get_all_battles")
	var total_battles: Array = total_variant as Array if total_variant is Array else []
	var total: int = total_battles.size()

	progress_label.text = "%d / %d Complete" % [completed, total]

## =============================================================================
## BATTLE START
## =============================================================================

func _on_start_battle_pressed() -> void:
	if selected_battle_id == "":
		return

	print("CampaignScreen: Starting battle: %s" % selected_battle_id)

	# Store selected battle in campaign service for game to access
	var campaign: Node = get_node("/root/Campaign")
	if campaign:
		var profile_repo: Node = get_node("/root/ProfileRepo")
		var profile_variant: Variant = profile_repo.call("get_active_profile")
		var profile: Dictionary = profile_variant as Dictionary if profile_variant is Dictionary else {}
		if not profile.is_empty():
			if not profile.has("campaign_progress"):
				profile["campaign_progress"] = {}
			var campaign_progress: Variant = profile["campaign_progress"]
			if campaign_progress is Dictionary:
				(campaign_progress as Dictionary)["current_battle"] = selected_battle_id
			profile_repo.call("save_profile", true)  # Force immediate save

	# Configure battle context for campaign mode
	var battle_context: Node = get_node("/root/BattleContext")
	if battle_context:
		battle_context.call("configure_campaign_battle", selected_battle_id)

	# Launch battle scene (generic, mode-agnostic)
	get_tree().change_scene_to_file("res://scenes/battlefield/battle_3d.tscn")

## =============================================================================
## NAVIGATION
## =============================================================================

func _on_back_pressed() -> void:
	print("CampaignScreen: Returning to main menu")
	get_tree().change_scene_to_file("res://scenes/ui/main_menu.tscn")

## =============================================================================
## SIGNALS
## =============================================================================

func _on_battle_completed(_battle_id: String) -> void:
	_refresh_battle_list()
	_update_progress_display()
	_update_detail_panel()

func _on_progress_changed() -> void:
	_update_progress_display()
