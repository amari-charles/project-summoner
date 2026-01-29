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
@onready var summoner_xp_label: Label = %SummonerXPLabel
@onready var card_xp_section: VBoxContainer = %CardXPSection
@onready var card_xp_header_label: Label = %CardXPHeaderLabel
@onready var card_xp_amount_label: Label = %CardXPAmountLabel
@onready var card_xp_grid: HBoxContainer = %CardXPGrid
@onready var choice_container: HBoxContainer = %ChoiceContainer
@onready var continue_button: Button = %ContinueButton

## Preloads
const CardXPItemScene: PackedScene = preload("res://scenes/ui/components/card_xp_item.tscn")
const CardDetailModalScene: PackedScene = preload("res://scenes/ui/modals/card_detail_modal.tscn")
const LevelUpPanelScene: PackedScene = preload("res://scenes/ui/modals/card_level_up_panel.tscn")

## Constants
const CHOICE_BUTTON_SIZE: Vector2 = Vector2(150, 100)
const CHOICE_BUTTON_FONT_SIZE: int = 24

## Rarity colors for reward display
const RARITY_COLORS: Dictionary = {
	&"common": Color(0.7, 0.7, 0.7),
	&"rare": Color(0.4, 0.6, 1.0),
	&"epic": Color(0.8, 0.4, 1.0),
	&"legendary": Color(1.0, 0.9, 0.3),
}

## State
var current_battle_id: String = ""
var reward_type: StringName = &""
var chosen_reward_index: int = -1  ## Index of chosen reward (-1 = not chosen)
var is_pending_reward: bool = false  ## True if resuming a pending reward
var reward_ready_to_claim: bool = false  ## True when reward is ready to be claimed
var flexible_options: Array[Dictionary] = []  ## Generated options for FLEXIBLE rewards
var is_flexible_reward: bool = false  ## True if using FLEXIBLE reward system

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
	# Validate we're in a valid state to show rewards
	# This guards against navigating here without actually winning a battle
	if BattleContext.battle_state != BattleContext.BattleState.VICTORY:
		# Check for pending reward - player may have won, exited, and returned
		var has_pending: bool = false
		var pending: Variant = Campaign.get_pending_reward()
		has_pending = pending != null and pending is Dictionary

		if not has_pending:
			push_error("RewardScreen: Invalid battle state (%s) - not a victory!" % BattleContext.BattleState.keys()[BattleContext.battle_state])
			SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)
			return

	# Check for pending reward first (resuming after exit/crash)
	var pending_reward: Variant = Campaign.get_pending_reward()
	if pending_reward != null and pending_reward is Dictionary:
		var pending_dict: Dictionary = pending_reward
		current_battle_id = pending_dict.get("battle_id", "")
		reward_type = StringName(pending_dict.get("reward_type", RewardTypeIDs.FIXED))
		chosen_reward_index = pending_dict.get("choice_index", -1)
		is_pending_reward = true
		print("RewardScreen: Resuming pending reward for battle '%s'" % current_battle_id)
	else:
		# No pending reward - load from current battle
		var profile: Dictionary = ProfileRepo.get_active_profile()
		if profile.is_empty():
			return

		var campaign_progress: Variant = profile.get("campaign_progress", {})
		if campaign_progress is Dictionary:
			current_battle_id = campaign_progress.get("current_battle", "")

	if current_battle_id == "":
		push_error("RewardScreen: No current battle set!")
		return

	var battle: Dictionary = Campaign.get_battle(current_battle_id)
	if battle.is_empty():
		push_error("RewardScreen: Battle not found: %s" % current_battle_id)
		return

	# Update UI
	battle_name_label.text = battle.get("name", "Unknown Battle")
	if not is_pending_reward:
		reward_type = StringName(battle.get("reward_type", RewardTypeIDs.FIXED))

	# Check if battle was already completed (replay scenario)
	var is_replay: bool = Campaign.is_battle_completed(current_battle_id)

	if is_replay:
		# Battle already completed - show replay message
		_show_rewards(battle, true)
	elif is_pending_reward:
		# Resuming pending reward - show appropriate UI
		_resume_pending_reward(battle)
	else:
		# First time victory - set pending reward (don't complete yet!)
		Campaign.set_pending_reward(current_battle_id, reward_type, -1)
		_show_rewards(battle, false)

## =============================================================================
## REWARD DISPLAY
## =============================================================================

func _show_rewards(battle: Dictionary, is_replay: bool = false) -> void:
	var catalog: Node = CardCatalog

	# Validate rewards before displaying
	_validate_rewards(battle, catalog)

	# Get gold reward for display
	var gold_reward: int = battle.get("gold_reward", 0)

	if is_replay:
		# Show message for replayed battles
		reward_card_label.text = Loc.t("ui.reward.already_completed")
		reward_detail_label.text = Loc.t("ui.reward.no_replay_rewards")
		gold_reward_label.text = ""
		summoner_xp_label.text = ""
		reward_ready_to_claim = false
		return

	# Display gold, summoner XP, and card XP rewards
	_display_gold_reward(gold_reward)
	var summoner_xp: int = battle.get("summoner_xp_reward", 0)
	_display_summoner_xp_reward(summoner_xp)
	var card_xp: int = battle.get("card_xp_reward", 0)
	_display_card_xp_rewards(card_xp)

	match reward_type:
		RewardTypeIDs.FIXED:
			# Display the reward preview (don't grant yet - grant on Continue)
			var reward_cards: Array = battle.get("reward_cards", [])
			if reward_cards.size() > 0 and reward_cards[0] is Dictionary:
				_display_card_reward(reward_cards[0])
			elif reward_cards.is_empty() and gold_reward > 0:
				# Gold only reward
				reward_card_label.text = Loc.t("ui.reward.victory")
				reward_detail_label.text = ""
			reward_ready_to_claim = true

		RewardTypeIDs.FLEXIBLE:
			# Flexible reward - may have specific_options or generate dynamically
			is_flexible_reward = true
			var player_selects: bool = battle.get("player_selects", true)

			if battle.has("specific_options"):
				# Legacy CHOICE behavior: use predefined options
				var specific_options: Array = battle.get("specific_options", [])
				flexible_options = []
				for opt: Variant in specific_options:
					if opt is Dictionary:
						flexible_options.append(opt)

				if not player_selects and flexible_options.size() > 0:
					# Legacy RANDOM behavior: auto-grant random option
					var random_idx: int = randi() % flexible_options.size()
					chosen_reward_index = random_idx
					_display_card_reward(flexible_options[random_idx])
					reward_ready_to_claim = true
				elif flexible_options.size() > 0:
					# Show choice UI for specific options
					_show_flexible_choice_ui(flexible_options)
					reward_ready_to_claim = false
				else:
					reward_card_label.text = Loc.t("ui.reward.victory")
					reward_detail_label.text = ""
					reward_ready_to_claim = true
			else:
				# Dynamic generation via RewardService
				_generate_flexible_options(battle)

		RewardTypeIDs.NONE:
			# No card rewards for this battle (may still have gold)
			reward_card_label.text = Loc.t("ui.reward.victory")
			reward_detail_label.text = ""
			reward_ready_to_claim = true

## Resume a pending reward (called when returning to screen after exit)
func _resume_pending_reward(battle: Dictionary) -> void:
	print("RewardScreen: Resuming pending reward (type: %s, choice_index: %d)" % [reward_type, chosen_reward_index])

	# Display gold, summoner XP, and card XP rewards
	var gold_reward: int = battle.get("gold_reward", 0)
	_display_gold_reward(gold_reward)
	var summoner_xp: int = battle.get("summoner_xp_reward", 0)
	_display_summoner_xp_reward(summoner_xp)
	var card_xp: int = battle.get("card_xp_reward", 0)
	_display_card_xp_rewards(card_xp)

	match reward_type:
		RewardTypeIDs.FIXED:
			# Fixed rewards just need to show the preview
			var reward_cards: Array = battle.get("reward_cards", [])
			if reward_cards.size() > 0 and reward_cards[0] is Dictionary:
				_display_card_reward(reward_cards[0])
			elif reward_cards.is_empty() and gold_reward > 0:
				reward_card_label.text = Loc.t("ui.reward.victory")
				reward_detail_label.text = ""
			reward_ready_to_claim = true

		RewardTypeIDs.FLEXIBLE:
			is_flexible_reward = true
			if battle.has("specific_options"):
				# Legacy specific options
				var specific_options: Array = battle.get("specific_options", [])
				flexible_options = []
				for opt: Variant in specific_options:
					if opt is Dictionary:
						flexible_options.append(opt)

				if chosen_reward_index >= 0 and chosen_reward_index < flexible_options.size():
					# Player already made a choice - show it
					_display_card_reward(flexible_options[chosen_reward_index])
					reward_ready_to_claim = true
				else:
					# Player hasn't chosen yet - show choice UI
					_show_flexible_choice_ui(flexible_options)
					reward_ready_to_claim = false
			else:
				# Dynamic generation - regenerate options
				_generate_flexible_options(battle)

		RewardTypeIDs.NONE:
			reward_card_label.text = Loc.t("ui.reward.victory")
			reward_detail_label.text = ""
			reward_ready_to_claim = true

## Display gold reward amount
func _display_gold_reward(gold: int) -> void:
	if gold > 0:
		gold_reward_label.text = Loc.t("ui.reward.gold", {"amount": gold})
	else:
		gold_reward_label.text = ""

## Display summoner XP reward amount
func _display_summoner_xp_reward(xp: int) -> void:
	if xp > 0:
		summoner_xp_label.text = Loc.t("ui.reward.summoner_xp", {"amount": xp})
	else:
		summoner_xp_label.text = ""

## Display card XP rewards for cards played in battle
func _display_card_xp_rewards(card_xp: int) -> void:
	# Clear any existing items
	for child: Node in card_xp_grid.get_children():
		child.queue_free()

	if card_xp <= 0:
		card_xp_section.visible = false
		return

	# Get cards that were played this battle
	var cards_played: Array[String] = BattleContext.get_cards_played()
	if cards_played.is_empty():
		card_xp_section.visible = false
		return

	# Show XP amount header
	card_xp_header_label.text = Loc.t("ui.reward.card_xp_header")
	card_xp_amount_label.text = Loc.t("ui.reward.card_xp_each", {"amount": card_xp})

	# Get card service for progression info
	var card_service: Node = get_node_or_null(CSharpAutoloads.PLAYER_CARD_SERVICE)
	if not card_service:
		push_warning("RewardScreen: PlayerCardService not available for card XP display")
		card_xp_section.visible = false
		return

	# Create card items for each played card
	for instance_id: String in cards_played:
		var info: Dictionary = card_service.get_card_progression_info(instance_id)
		if info.is_empty():
			continue

		var catalog_id: String = info.get("catalog_id", "")
		var card_data: Dictionary = CardCatalog.get_card(catalog_id)
		if card_data.is_empty():
			continue

		var card_name: String = card_data.get("card_name", "Unknown")
		var level: int = info.get("level", 1)
		var can_level_up: bool = info.get("can_level_up", false)
		var xp_progress: float = info.get("xp_progress", 0.0)

		var item: Control = CardXPItemScene.instantiate()
		card_xp_grid.add_child(item)

		if item.has_method("setup"):
			item.call("setup", instance_id, catalog_id, card_name, level, can_level_up, xp_progress)

		if item.has_signal("clicked"):
			item.clicked.connect(_on_card_xp_item_clicked)

	card_xp_section.visible = true

## Handle click on a card XP item - open card detail modal to view stats
func _on_card_xp_item_clicked(instance_id: String) -> void:
	var card_service: Node = get_node_or_null(CSharpAutoloads.PLAYER_CARD_SERVICE)
	if not card_service:
		return

	var info: Dictionary = card_service.get_card_progression_info(instance_id)
	if info.is_empty():
		return

	var catalog_id: String = info.get("catalog_id", "")
	if catalog_id.is_empty():
		return

	# Open card detail modal to show full stats, progression, and upgrades
	var modal: CardDetailModal = CardDetailModalScene.instantiate() as CardDetailModal
	if not modal:
		return

	add_child(modal)
	modal.open_for_card(instance_id, catalog_id)

	# Connect to level-up signal - modal will request level-up, we open the panel
	modal.level_up_requested.connect(_on_card_detail_level_up_requested)
	modal.closed.connect(_on_card_detail_closed.bind(instance_id))

## Handle level-up request from card detail modal
func _on_card_detail_level_up_requested(instance_id: String) -> void:
	# Open level-up panel for upgrade selection
	var panel: Node = LevelUpPanelScene.instantiate()
	if not panel:
		return

	add_child(panel)

	if panel.has_method("open_for_card"):
		panel.call("open_for_card", instance_id)

	if panel.has_signal("level_up_completed"):
		panel.level_up_completed.connect(_on_level_up_completed.bind(instance_id))

	if panel.has_signal("cancelled"):
		panel.cancelled.connect(_on_level_up_cancelled.bind(panel))

## Handle card detail modal closed - refresh card display
func _on_card_detail_closed(instance_id: String) -> void:
	_refresh_card_xp_item(instance_id)

## Handle level-up completion - refresh the card item display
func _on_level_up_completed(instance_id: String) -> void:
	print("RewardScreen: Card %s leveled up, refreshing display" % instance_id)
	_refresh_card_xp_item(instance_id)

## Handle level-up cancellation - clean up panel
func _on_level_up_cancelled(panel: Node) -> void:
	if is_instance_valid(panel):
		panel.queue_free()

## Refresh a specific card item after level-up
func _refresh_card_xp_item(instance_id: String) -> void:
	var card_service: Node = get_node_or_null(CSharpAutoloads.PLAYER_CARD_SERVICE)
	if not card_service:
		return

	# Find the card item and update it
	for child: Node in card_xp_grid.get_children():
		if child is CardXPItem and child.instance_id == instance_id:
			var info: Dictionary = card_service.get_card_progression_info(instance_id)
			if info.is_empty():
				continue

			var catalog_id: String = info.get("catalog_id", "")
			var card_data: Dictionary = CardCatalog.get_card(catalog_id)
			if card_data.is_empty():
				continue

			var card_name: String = card_data.get("card_name", "Unknown")
			var level: int = info.get("level", 1)
			var can_level_up: bool = info.get("can_level_up", false)
			var xp_progress: float = info.get("xp_progress", 0.0)

			if child.has_method("setup"):
				child.call("setup", instance_id, catalog_id, card_name, level, can_level_up, xp_progress)
			break

## Display a card reward (handles both legacy {catalog_id} and flexible {id} formats)
func _display_card_reward(reward: Dictionary) -> void:
	# Handle both formats: {catalog_id, rarity, count} and {id, type, rarity, amount}
	var catalog_id: String = reward.get("id", reward.get("catalog_id", ""))
	var rarity: StringName = StringName(reward.get("rarity", RarityIDs.COMMON))
	var count: int = reward.get("amount", reward.get("count", 1))

	var card_data: Dictionary = CardCatalog.get_card(catalog_id)
	if card_data.is_empty():
		reward_card_label.text = Loc.t("ui.reward.unknown_card")
		reward_detail_label.text = ""
		return

	var card_name: String = card_data.get("card_name", "Unknown")

	if count > 1:
		reward_card_label.text = "%dx %s" % [count, card_name]
	else:
		reward_card_label.text = card_name

	reward_detail_label.text = Loc.t("ui.reward.rarity", {"rarity": String(rarity).capitalize()})
	reward_card_label.add_theme_color_override("font_color", _get_rarity_color(rarity))


## Get color for a card rarity
func _get_rarity_color(rarity: StringName) -> Color:
	return RARITY_COLORS.get(rarity, Color.WHITE)

## =============================================================================
## FLEXIBLE REWARD SYSTEM
## =============================================================================

## Generate flexible reward options via RewardService
func _generate_flexible_options(battle: Dictionary) -> void:
	# Build config from battle data
	var config: Dictionary = {
		"guaranteed_count": battle.get("guaranteed_count", 1),
		"pool_count": battle.get("pool_count", 2),
		"pool_id": battle.get("pool_id", "standard_cards"),
		"collection_filter": battle.get("collection_filter", "none")
	}

	# Get active summoner for element theming
	var summoner_id: String = _get_active_summoner_id()

	# Generate options via RewardService
	flexible_options = RewardService.generate_reward_options(config, summoner_id)

	var player_selects: bool = battle.get("player_selects", true)

	if flexible_options.size() == 0:
		# No options generated (pool exhausted, etc.)
		reward_card_label.text = Loc.t("ui.reward.victory")
		reward_detail_label.text = ""
		reward_ready_to_claim = true
	elif not player_selects:
		# Auto-grant first option (legacy RANDOM behavior)
		chosen_reward_index = 0
		_display_card_reward(flexible_options[0])
		reward_ready_to_claim = true
	else:
		# Show choice UI
		_show_flexible_choice_ui(flexible_options)
		reward_ready_to_claim = false

## Get active summoner ID for reward theming
func _get_active_summoner_id() -> String:
	var profile: Dictionary = ProfileRepo.get_active_profile()
	if profile.is_empty():
		return ""
	var meta: Variant = profile.get("meta", {})
	if not meta is Dictionary:
		return ""
	return meta.get("active_summoner", "")

## Show choice UI for flexible reward options
func _show_flexible_choice_ui(options: Array[Dictionary]) -> void:
	# Hide default reward display
	reward_container.visible = false
	choice_container.visible = true

	# Clear existing choice buttons
	for child: Node in choice_container.get_children():
		child.queue_free()

	# Create choice buttons
	for i: int in range(options.size()):
		var option: Dictionary = options[i]
		var catalog_id: String = option.get("id", option.get("catalog_id", ""))
		var card_data: Dictionary = CardCatalog.get_card(catalog_id)

		if card_data.is_empty():
			continue

		var button: Button = Button.new()
		var card_name: String = card_data.get("card_name", "Unknown")
		var is_guaranteed: bool = option.get("is_guaranteed", false)

		# Add badge for guaranteed vs pool options
		if is_guaranteed:
			button.text = "[%s] %s" % [Loc.t("ui.reward.guaranteed"), card_name]
		else:
			button.text = card_name

		button.custom_minimum_size = CHOICE_BUTTON_SIZE
		button.add_theme_font_size_override("font_size", CHOICE_BUTTON_FONT_SIZE)
		button.pressed.connect(_on_flexible_choice_selected.bind(i))
		choice_container.add_child(button)

	# Disable continue until choice made
	continue_button.disabled = true

## Handle flexible reward selection
func _on_flexible_choice_selected(index: int) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	print("RewardScreen: Player chose flexible option %d" % index)
	chosen_reward_index = index

	# Save choice to pending reward state (persists if player exits)
	Campaign.update_pending_choice(index)

	if index >= 0 and index < flexible_options.size():
		# Hide choice UI and show selected card preview
		choice_container.visible = false
		reward_container.visible = true
		_display_card_reward(flexible_options[index])

	# Mark ready to claim and enable continue
	reward_ready_to_claim = true
	continue_button.disabled = false

## =============================================================================
## NAVIGATION
## =============================================================================

func _on_continue_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	print("RewardScreen: Continue pressed")

	# Check if we have a reward to claim
	if reward_ready_to_claim:
		var granted_card: Dictionary = {}

		if is_flexible_reward and flexible_options.size() > 0 and chosen_reward_index >= 0:
			# FLEXIBLE reward with dynamically generated options - grant via RewardService
			var battle: Dictionary = Campaign.get_battle(current_battle_id)
			if not battle.has("specific_options"):
				# Only use RewardService for dynamically generated options
				var chosen_option: Dictionary = flexible_options[chosen_reward_index]
				if RewardService.grant_reward(chosen_option):
					granted_card = chosen_option
					print("RewardScreen: Granted flexible reward via RewardService")
				else:
					push_error("RewardScreen: Failed to grant flexible reward")

				# Mark battle complete via Campaign (without granting - already done)
				Campaign.complete_battle_without_reward(current_battle_id)
			else:
				# FLEXIBLE with specific_options - use Campaign.claim_pending_reward()
				granted_card = Campaign.claim_pending_reward()
		else:
			# FIXED or other rewards - use Campaign.claim_pending_reward()
			granted_card = Campaign.claim_pending_reward()

		# Auto-add to deck if tutorial battle
		if not granted_card.is_empty():
			_auto_add_cards_to_deck(granted_card)
			print("RewardScreen: Claimed reward for battle '%s'" % current_battle_id)
	else:
		# No reward to claim (replay or no rewards) - just clear any stale pending state
		Campaign.clear_pending_reward()

	print("RewardScreen: Transitioning to campaign map")
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

## =============================================================================
## AUTO-FILL DECK (TUTORIAL MODE)
## =============================================================================

## Automatically add granted cards to deck if this is a tutorial battle
func _auto_add_cards_to_deck(granted_card: Dictionary) -> void:
	# Check if this is a tutorial battle
	if not Campaign.is_battle_tutorial(current_battle_id):
		return  # Not a tutorial battle, don't auto-add

	# Get card instance IDs that were granted
	var instance_ids: Array = granted_card.get("instance_ids", [])
	if instance_ids.is_empty():
		push_warning("RewardScreen: No instance_ids in granted_card for auto-fill")
		return

	# Get active deck ID from profile
	var profile: Dictionary = ProfileRepo.get_active_profile()
	if profile.is_empty():
		push_error("RewardScreen: No active profile!")
		return

	var meta: Variant = profile.get("meta", {})
	if not meta is Dictionary:
		push_warning("RewardScreen: Invalid meta in profile!")
		return
	var deck_id: String = meta.get("selected_deck", "")
	if deck_id == "":
		push_warning("RewardScreen: No active deck selected!")
		return

	# Add cards to deck
	var added_count: int = 0
	for card_instance_id: String in instance_ids:
		if Decks.add_card_to_deck(deck_id, card_instance_id):
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

		if not CardCatalog.has_card(catalog_id):
			invalid_cards.append(catalog_id)

	if not invalid_cards.is_empty():
		push_error("RewardScreen: VALIDATION FAILED - Battle '%s' has invalid reward cards: %s" % [battle_id, invalid_cards])
		push_error("RewardScreen: These cards don't exist in CardCatalog! Player may not receive promised rewards.")
	else:
		print("RewardScreen: Rewards validated for battle '%s'" % battle_id)
