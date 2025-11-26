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
@onready var gold_reward_label: Label = %GoldRewardLabel
@onready var choice_container: HBoxContainer = %ChoiceContainer
@onready var continue_button: Button = %ContinueButton

## State
var current_battle_id: String = ""
var reward_type: StringName = &""
var chosen_reward_index: int = -1  ## Index of chosen reward (-1 = not chosen)
var is_pending_reward: bool = false  ## True if resuming a pending reward
var reward_ready_to_claim: bool = false  ## True when reward is ready to be claimed

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
	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		push_error("RewardScreen: Campaign service not found!")
		return

	# Check for pending reward first (resuming after exit/crash)
	var pending_reward: Variant = campaign.call("get_pending_reward")
	if pending_reward != null and pending_reward is Dictionary:
		var pending_dict: Dictionary = pending_reward
		current_battle_id = pending_dict.get("battle_id", "")
		reward_type = StringName(pending_dict.get("reward_type", RewardTypeIDs.FIXED))
		chosen_reward_index = pending_dict.get("choice_index", -1)
		is_pending_reward = true
		print("RewardScreen: Resuming pending reward for battle '%s'" % current_battle_id)
	else:
		# No pending reward - load from current battle
		var profile_repo: Node = get_node("/root/ProfileRepo")
		if not profile_repo:
			push_error("RewardScreen: ProfileRepository not found!")
			return

		var profile: Dictionary = profile_repo.call("get_active_profile")
		if profile.is_empty():
			return

		var empty_dict: Dictionary = {}
		var campaign_progress: Dictionary = profile.get("campaign_progress", empty_dict) if profile.get("campaign_progress", empty_dict) is Dictionary else {}
		current_battle_id = campaign_progress.get("current_battle", "")

	if current_battle_id == "":
		push_error("RewardScreen: No current battle set!")
		return

	var battle: Dictionary = campaign.call("get_battle", current_battle_id)
	if battle.is_empty():
		push_error("RewardScreen: Battle not found: %s" % current_battle_id)
		return

	# Update UI
	battle_name_label.text = battle.get("name", "Unknown Battle")
	if not is_pending_reward:
		reward_type = StringName(battle.get("reward_type", RewardTypeIDs.FIXED))

	# Check if battle was already completed (replay scenario)
	var is_replay: bool = campaign.call("is_battle_completed", current_battle_id)

	if is_replay:
		# Battle already completed - show replay message
		_show_rewards(battle, true)
	elif is_pending_reward:
		# Resuming pending reward - show appropriate UI
		_resume_pending_reward(battle)
	else:
		# First time victory - set pending reward (don't complete yet!)
		campaign.call("set_pending_reward", current_battle_id, reward_type, -1)
		_show_rewards(battle, false)

## =============================================================================
## REWARD DISPLAY
## =============================================================================

func _show_rewards(battle: Dictionary, is_replay: bool = false) -> void:
	var catalog: Node = get_node("/root/CardCatalog")

	# Validate rewards before displaying
	_validate_rewards(battle, catalog)

	# Get gold reward for display
	var gold_reward: int = battle.get("gold_reward", 0)

	if is_replay:
		# Show message for replayed battles
		reward_card_label.text = Loc.t("ui.reward.already_completed")
		reward_detail_label.text = Loc.t("ui.reward.no_replay_rewards")
		gold_reward_label.text = ""
		reward_ready_to_claim = false
		return

	# Display gold reward
	_display_gold_reward(gold_reward)

	match reward_type:
		RewardTypeIDs.FIXED, RewardTypeIDs.RANDOM:
			# Display the reward preview (don't grant yet - grant on Continue)
			var reward_cards: Array = battle.get("reward_cards", [])
			if reward_cards.size() > 0 and reward_cards[0] is Dictionary:
				_display_card_reward(reward_cards[0])
			elif reward_cards.is_empty() and gold_reward > 0:
				# Gold only reward
				reward_card_label.text = Loc.t("ui.reward.victory")
				reward_detail_label.text = ""
			reward_ready_to_claim = true

		RewardTypeIDs.CHOICE:
			# Show choice UI - player must pick before continuing
			var reward_cards: Array = battle.get("reward_cards", [])
			_show_choice_ui(reward_cards)
			reward_ready_to_claim = false  # Must choose first

		RewardTypeIDs.NONE:
			# No card rewards for this battle (may still have gold)
			reward_card_label.text = Loc.t("ui.reward.victory")
			reward_detail_label.text = ""
			reward_ready_to_claim = true

## Resume a pending reward (called when returning to screen after exit)
func _resume_pending_reward(battle: Dictionary) -> void:
	print("RewardScreen: Resuming pending reward (type: %s, choice_index: %d)" % [reward_type, chosen_reward_index])

	# Display gold reward
	var gold_reward: int = battle.get("gold_reward", 0)
	_display_gold_reward(gold_reward)

	match reward_type:
		RewardTypeIDs.FIXED, RewardTypeIDs.RANDOM:
			# Fixed/random rewards just need to show the preview
			var reward_cards: Array = battle.get("reward_cards", [])
			if reward_cards.size() > 0 and reward_cards[0] is Dictionary:
				_display_card_reward(reward_cards[0])
			elif reward_cards.is_empty() and gold_reward > 0:
				reward_card_label.text = Loc.t("ui.reward.victory")
				reward_detail_label.text = ""
			reward_ready_to_claim = true

		RewardTypeIDs.CHOICE:
			var reward_cards: Array = battle.get("reward_cards", [])
			if chosen_reward_index >= 0 and chosen_reward_index < reward_cards.size():
				# Player already made a choice - show it
				var chosen_reward: Dictionary = reward_cards[chosen_reward_index]
				_display_card_reward(chosen_reward)
				reward_ready_to_claim = true
			else:
				# Player hasn't chosen yet - show choice UI
				_show_choice_ui(reward_cards)
				reward_ready_to_claim = false

		RewardTypeIDs.NONE:
			reward_card_label.text = Loc.t("ui.reward.victory")
			reward_detail_label.text = ""
			reward_ready_to_claim = true

## Display gold reward amount
func _display_gold_reward(gold: int) -> void:
	if gold > 0:
		gold_reward_label.text = "+ %d Gold" % gold
	else:
		gold_reward_label.text = ""

func _display_card_reward(reward: Dictionary) -> void:
	var catalog: Node = get_node("/root/CardCatalog")
	if not catalog:
		return

	var catalog_id: String = reward.get("catalog_id", "")
	var rarity: StringName = reward.get("rarity", RarityIDs.COMMON)
	var count: int = reward.get("count", 1)

	var card_data: Dictionary = catalog.call("get_card", catalog_id)
	if card_data.is_empty():
		reward_card_label.text = Loc.t("ui.reward.unknown_card")
		reward_detail_label.text = ""
		return

	var card_name: String = card_data.get("card_name", "Unknown")

	if count > 1:
		reward_card_label.text = "%dx %s" % [count, card_name]
	else:
		reward_card_label.text = card_name

	reward_detail_label.text = Loc.t("ui.reward.rarity", {"rarity": rarity.capitalize()})

	# Color based on rarity
	match rarity:
		RarityIDs.COMMON:
			reward_card_label.add_theme_color_override("font_color", Color(0.7, 0.7, 0.7))
		RarityIDs.RARE:
			reward_card_label.add_theme_color_override("font_color", Color(0.4, 0.6, 1.0))
		RarityIDs.EPIC:
			reward_card_label.add_theme_color_override("font_color", Color(0.8, 0.4, 1.0))
		RarityIDs.LEGENDARY:
			reward_card_label.add_theme_color_override("font_color", Color(1.0, 0.9, 0.3))

func _show_choice_ui(reward_options: Array) -> void:
	# Hide default reward display
	reward_container.visible = false
	choice_container.visible = true

	var catalog: Node = get_node("/root/CardCatalog")
	if not catalog:
		return

	# Create choice buttons
	for i: int in range(reward_options.size()):
		var reward_variant: Variant = reward_options[i]
		if not reward_variant is Dictionary:
			push_error("RewardScreen: reward_options[%d] is not a Dictionary" % i)
			continue
		var reward: Dictionary = reward_variant
		var catalog_id: String = reward.get("catalog_id", "")
		var card_data: Dictionary = catalog.call("get_card", catalog_id)

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
	chosen_reward_index = index

	# Save choice to pending reward state (persists if player exits)
	var campaign: Node = get_node("/root/Campaign")
	if campaign:
		campaign.call("update_pending_choice", index)

	# Get the chosen reward to display
	var battle: Dictionary = campaign.call("get_battle", current_battle_id)
	var reward_cards: Array = battle.get("reward_cards", [])

	if index >= 0 and index < reward_cards.size() and reward_cards[index] is Dictionary:
		# Hide choice UI and show selected card preview
		choice_container.visible = false
		reward_container.visible = true
		_display_card_reward(reward_cards[index])

	# Mark ready to claim and enable continue
	reward_ready_to_claim = true
	continue_button.disabled = false

## =============================================================================
## NAVIGATION
## =============================================================================

func _on_continue_pressed() -> void:
	print("RewardScreen: Continue pressed")

	var campaign: Node = get_node("/root/Campaign")
	if not campaign:
		push_error("RewardScreen: Campaign service not found!")
		SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)
		return

	# Check if we have a reward to claim
	if reward_ready_to_claim:
		# Claim the pending reward (grants cards + marks battle complete)
		var granted_card: Dictionary = campaign.call("claim_pending_reward")

		# Auto-add to deck if tutorial battle
		if not granted_card.is_empty():
			_auto_add_cards_to_deck(granted_card)
			print("RewardScreen: Claimed reward for battle '%s'" % current_battle_id)
	else:
		# No reward to claim (replay or no rewards) - just clear any stale pending state
		campaign.call("clear_pending_reward")

	print("RewardScreen: Transitioning to campaign map")
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

## =============================================================================
## AUTO-FILL DECK (TUTORIAL MODE)
## =============================================================================

## Automatically add granted cards to deck if this is a tutorial battle
func _auto_add_cards_to_deck(granted_card: Dictionary) -> void:
	# Check if this is a tutorial battle
	var campaign: Node = get_node("/root/Campaign")
	if not campaign or not campaign.call("is_battle_tutorial", current_battle_id):
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

	var profile: Dictionary = profile_repo.call("get_active_profile")
	if profile.is_empty():
		push_error("RewardScreen: No active profile!")
		return

	var empty_meta: Dictionary = {}
	var meta: Dictionary = profile.get("meta", empty_meta) if profile.get("meta", empty_meta) is Dictionary else {}
	var deck_id: String = meta.get("selected_deck", "")
	if deck_id == "":
		push_warning("RewardScreen: No active deck selected!")
		return

	# Add cards to deck
	var decks: Node = get_node("/root/Decks")
	if not decks:
		push_error("RewardScreen: Decks service not found!")
		return

	var added_count: int = 0
	for card_instance_id: String in instance_ids:
		if decks.call("add_card_to_deck", deck_id, card_instance_id):
			added_count += 1
		else:
			push_warning("RewardScreen: Failed to add card %s to deck" % card_instance_id)

	if added_count > 0:
		print("RewardScreen: Auto-added %d card(s) to deck (tutorial mode)" % added_count)

## =============================================================================
## REWARD VALIDATION
## =============================================================================

## Validate that reward cards in battle config exist in catalog
## This is a runtime check to catch configuration errors
func _validate_rewards(battle: Dictionary, catalog: Node) -> void:
	if not catalog:
		push_warning("RewardScreen: CardCatalog not available for validation")
		return

	var battle_id: String = battle.get("id", "unknown")
	var reward_cards: Array = battle.get("reward_cards", [])

	if reward_cards.is_empty():
		return  # No rewards is valid (some battles have no rewards)

	var invalid_cards: Array[String] = []

	for reward_variant: Variant in reward_cards:
		if not reward_variant is Dictionary:
			push_warning("RewardScreen: Invalid reward format in battle '%s'" % battle_id)
			continue

		var reward: Dictionary = reward_variant
		var catalog_id: String = reward.get("catalog_id", "")

		if catalog_id.is_empty():
			push_warning("RewardScreen: Empty catalog_id in battle '%s' rewards" % battle_id)
			continue

		if not catalog.call("has_card", catalog_id):
			invalid_cards.append(catalog_id)

	if not invalid_cards.is_empty():
		push_error("RewardScreen: VALIDATION FAILED - Battle '%s' has invalid reward cards: %s" % [battle_id, invalid_cards])
		push_error("RewardScreen: These cards don't exist in CardCatalog! Player may not receive promised rewards.")
	else:
		print("RewardScreen: Rewards validated for battle '%s'" % battle_id)
