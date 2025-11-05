extends Control
class_name RewardScreen

## RewardScreen - Display battle rewards and handle card choices
##
## Shows victory screen with earned cards.
## Supports fixed rewards (auto-grant) and choice rewards (player picks).

## Node references
@onready var battle_name_label: Label = %BattleNameLabel
@onready var reward_container: VBoxContainer = %RewardContainer
@onready var reward_card_label: Label = %RewardCardLabel
@onready var reward_detail_label: Label = %RewardDetailLabel
@onready var choice_container: HBoxContainer = %ChoiceContainer
@onready var continue_button: Button = %ContinueButton

## State
var current_battle_id: String = ""
var reward_type: String = ""
var chosen_reward_index: int = 0

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("RewardScreen: Initializing...")

	# Connect buttons
	continue_button.pressed.connect(_on_continue_pressed)

	# Load battle results and show rewards
	_load_battle_results()

## =============================================================================
## BATTLE RESULTS
## =============================================================================

func _load_battle_results() -> void:
	var profile_repo = get_node("/root/ProfileRepo")
	if not profile_repo:
		push_error("RewardScreen: ProfileRepository not found!")
		return

	var profile = profile_repo.get_active_profile()
	if profile.is_empty():
		return

	current_battle_id = profile.get("campaign_progress", {}).get("current_battle", "")
	if current_battle_id == "":
		push_error("RewardScreen: No current battle set!")
		return

	var campaign = get_node("/root/Campaign")
	if not campaign:
		push_error("RewardScreen: Campaign service not found!")
		return

	var battle = campaign.get_battle(current_battle_id)
	if battle.is_empty():
		push_error("RewardScreen: Battle not found: %s" % current_battle_id)
		return

	# Update UI
	battle_name_label.text = battle.get("name", "Unknown Battle")
	reward_type = battle.get("reward_type", "fixed")

	# Mark battle as completed
	if not campaign.is_battle_completed(current_battle_id):
		campaign.complete_battle(current_battle_id)

	# Show rewards based on type
	_show_rewards(battle)

## =============================================================================
## REWARD DISPLAY
## =============================================================================

func _show_rewards(battle: Dictionary) -> void:
	var campaign = get_node("/root/Campaign")
	var catalog = get_node("/root/CardCatalog")

	match reward_type:
		"fixed":
			# Auto-grant rewards and display first card
			var granted_card = campaign.grant_battle_reward(current_battle_id)
			if not granted_card.is_empty():
				_display_card_reward(granted_card)

		"choice":
			# Show choice UI
			var reward_cards = battle.get("reward_cards", [])
			_show_choice_ui(reward_cards)

		"random":
			# Roll random and display
			var granted_card = campaign.grant_battle_reward(current_battle_id)
			if not granted_card.is_empty():
				_display_card_reward(granted_card)

func _display_card_reward(reward: Dictionary) -> void:
	var catalog = get_node("/root/CardCatalog")
	if not catalog:
		return

	var catalog_id = reward.get("catalog_id", "")
	var rarity = reward.get("rarity", "common")
	var count = reward.get("count", 1)

	var card_data = catalog.get_card(catalog_id)
	if card_data.is_empty():
		reward_card_label.text = "Unknown Card"
		reward_detail_label.text = ""
		return

	var card_name = card_data.get("card_name", "Unknown")

	if count > 1:
		reward_card_label.text = "%dx %s" % [count, card_name]
	else:
		reward_card_label.text = card_name

	reward_detail_label.text = "Rarity: %s" % rarity.capitalize()

	# Color based on rarity
	match rarity:
		"common":
			reward_card_label.add_theme_color_override("font_color", Color(0.7, 0.7, 0.7))
		"rare":
			reward_card_label.add_theme_color_override("font_color", Color(0.4, 0.6, 1.0))
		"epic":
			reward_card_label.add_theme_color_override("font_color", Color(0.8, 0.4, 1.0))
		"legendary":
			reward_card_label.add_theme_color_override("font_color", Color(1.0, 0.9, 0.3))

func _show_choice_ui(reward_options: Array) -> void:
	# Hide default reward display
	reward_container.visible = false
	choice_container.visible = true

	var catalog = get_node("/root/CardCatalog")
	if not catalog:
		return

	# Create choice buttons
	for i in range(reward_options.size()):
		var reward = reward_options[i]
		var catalog_id = reward.get("catalog_id", "")
		var card_data = catalog.get_card(catalog_id)

		if card_data.is_empty():
			continue

		var button = Button.new()
		button.text = card_data.get("card_name", "Unknown")
		button.custom_minimum_size = Vector2(150, 100)
		button.add_theme_font_size_override("font_size", 24)
		button.pressed.connect(_on_choice_selected.bind(i))
		choice_container.add_child(button)

	# Disable continue until choice made
	continue_button.disabled = true

func _on_choice_selected(index: int) -> void:
	print("RewardScreen: Player chose option %d" % index)

	# Grant the chosen reward
	var campaign = get_node("/root/Campaign")
	if campaign:
		var granted_card = campaign.grant_battle_reward(current_battle_id, index)
		if not granted_card.is_empty():
			# Hide choice UI and show selected card
			choice_container.visible = false
			reward_container.visible = true
			_display_card_reward(granted_card)

	# Enable continue
	continue_button.disabled = false

## =============================================================================
## NAVIGATION
## =============================================================================

func _on_continue_pressed() -> void:
	print("RewardScreen: Continuing to campaign screen")
	get_tree().change_scene_to_file("res://scenes/ui/campaign_screen.tscn")
