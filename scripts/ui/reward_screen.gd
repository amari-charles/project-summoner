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
	var profile_repo: Node = get_node("/root/ProfileRepo")
	if not profile_repo:
		push_error("RewardScreen: ProfileRepository not found!")
		return

	var profile: Dictionary = profile_repo.get_active_profile()
	if profile.is_empty():
		return

	current_battle_id = profile.get("campaign_progress", {}).get("current_battle", "")
	if current_battle_id == "":
		push_error("RewardScreen: No current battle set!")
		return

	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		push_error("RewardScreen: Campaign service not found!")
		return

	var battle: Dictionary = campaign.get_battle(current_battle_id)
	if battle.is_empty():
		push_error("RewardScreen: Battle not found: %s" % current_battle_id)
		return

	# Update UI
	battle_name_label.text = battle.get("name", "Unknown Battle")
	reward_type = battle.get("reward_type", "fixed")

	# Check if battle was already completed (replay scenario)
	var is_replay: bool = campaign.is_battle_completed(current_battle_id)

	# Mark battle as completed (if first time)
	if not is_replay:
		campaign.complete_battle(current_battle_id)

	# Show rewards based on type (only grant if first time)
	_show_rewards(battle, is_replay)

## =============================================================================
## REWARD DISPLAY
## =============================================================================

func _show_rewards(battle: Dictionary, is_replay: bool = false) -> void:
	var campaign: Node = get_node("/root/Campaign")
	var catalog: Node = get_node("/root/CardCatalog")

	if is_replay:
		# Show message for replayed battles
		reward_card_label.text = "Battle Already Completed"
		reward_detail_label.text = "No rewards for replaying battles"
		return

	match reward_type:
		"fixed":
			# Auto-grant rewards and display first card
			var granted_card: Dictionary = campaign.grant_battle_reward(current_battle_id)
			if not granted_card.is_empty():
				_display_card_reward(granted_card)
				_auto_add_cards_to_deck(granted_card)

		"choice":
			# Show choice UI
			var reward_cards: Array = battle.get("reward_cards", [])
			_show_choice_ui(reward_cards)

		"random":
			# Roll random and display
			var granted_card: Dictionary = campaign.grant_battle_reward(current_battle_id)
			if not granted_card.is_empty():
				_display_card_reward(granted_card)
				_auto_add_cards_to_deck(granted_card)

func _display_card_reward(reward: Dictionary) -> void:
	var catalog: Node = get_node("/root/CardCatalog")
	if not catalog:
		return

	var catalog_id: String = reward.get("catalog_id", "")
	var rarity: String = reward.get("rarity", "common")
	var count: int = reward.get("count", 1)

	var card_data: Dictionary = catalog.get_card(catalog_id)
	if card_data.is_empty():
		reward_card_label.text = "Unknown Card"
		reward_detail_label.text = ""
		return

	var card_name: String = card_data.get("card_name", "Unknown")

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

	var catalog: Node = get_node("/root/CardCatalog")
	if not catalog:
		return

	# Create choice buttons
	for i in range(reward_options.size()):
		var reward: Dictionary = reward_options[i]
		var catalog_id: String = reward.get("catalog_id", "")
		var card_data: Dictionary = catalog.get_card(catalog_id)

		if card_data.is_empty():
			continue

		var button: Button = Button.new()
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
	var campaign: Node = get_node("/root/Campaign")
	if campaign:
		var granted_card: Dictionary = campaign.grant_battle_reward(current_battle_id, index)
		if not granted_card.is_empty():
			# Hide choice UI and show selected card
			choice_container.visible = false
			reward_container.visible = true
			_display_card_reward(granted_card)
			_auto_add_cards_to_deck(granted_card)

	# Enable continue
	continue_button.disabled = false

## =============================================================================
## NAVIGATION
## =============================================================================

func _on_continue_pressed() -> void:
	print("RewardScreen: Continuing to campaign screen")
	get_tree().change_scene_to_file("res://scenes/ui/campaign_screen.tscn")

## =============================================================================
## AUTO-FILL DECK (TUTORIAL MODE)
## =============================================================================

## Automatically add granted cards to deck if this is a tutorial battle
func _auto_add_cards_to_deck(granted_card: Dictionary) -> void:
	# Check if this is a tutorial battle
	var campaign: Node = get_node("/root/Campaign")
	if not campaign or not campaign.is_battle_tutorial(current_battle_id):
		return  # Not a tutorial battle, don't auto-add

	# Get card instance IDs that were granted
	var instance_ids: Array = granted_card.get("instance_ids", [])
	if instance_ids.is_empty():
		push_warning("RewardScreen: No instance_ids in granted_card for auto-fill")
		return

	# Get active deck ID from profile
	var profile_repo: Node = get_node("/root/ProfileRepo")
	if not profile_repo:
		push_error("RewardScreen: ProfileRepo not found!")
		return

	var profile: Dictionary = profile_repo.get_active_profile()
	if profile.is_empty():
		push_error("RewardScreen: No active profile!")
		return

	var deck_id: String = profile.get("meta", {}).get("selected_deck", "")
	if deck_id == "":
		push_warning("RewardScreen: No active deck selected!")
		return

	# Add cards to deck
	var decks: Node = get_node("/root/Decks")
	if not decks:
		push_error("RewardScreen: Decks service not found!")
		return

	var added_count: int = 0
	for card_instance_id in instance_ids:
		if decks.add_card_to_deck(deck_id, card_instance_id):
			added_count += 1
		else:
			push_warning("RewardScreen: Failed to add card %s to deck" % card_instance_id)

	if added_count > 0:
		print("RewardScreen: Auto-added %d card(s) to deck (tutorial mode)" % added_count)
