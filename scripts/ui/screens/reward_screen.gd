extends Control
class_name RewardScreen

## RewardScreen - Display battle rewards and handle card choices
##
## Shows victory screen with earned cards.
## Supports fixed rewards (auto-grant) and choice rewards (player picks).

## Node references
@onready var battle_name_label: Label = %BattleNameLabel
@onready var reward_container: VBoxContainer = %RewardContainer
# First Clear section
@onready var first_clear_section: VBoxContainer = %FirstClearSection
@onready var first_clear_header: Label = %FirstClearHeader
@onready var reward_card_label: Label = %RewardCardLabel
@onready var reward_detail_label: Label = %RewardDetailLabel
@onready var gold_reward_label: Label = %GoldRewardLabel
@onready var first_clear_status: Label = %FirstClearStatus
# Every Battle section
@onready var every_battle_section: VBoxContainer = %EveryBattleSection
@onready var every_battle_header: Label = %EveryBattleHeader
@onready var summoner_xp_label: Label = %SummonerXPLabel
@onready var card_xp_section: VBoxContainer = %CardXPSection
@onready var card_xp_header_label: Label = %CardXPHeaderLabel
@onready var card_xp_amount_label: Label = %CardXPAmountLabel
@onready var card_xp_grid: HBoxContainer = %CardXPGrid
# Other UI
@onready var choice_container: HBoxContainer = %ChoiceContainer
@onready var continue_button: Button = %ContinueButton

## Preloads
const CardXPItemScene: PackedScene = preload("res://scenes/ui/components/card_xp_item.tscn")
const CardDetailModalScene: PackedScene = preload("res://scenes/ui/modals/card_detail_modal.tscn")
const LevelUpPanelScene: PackedScene = preload("res://scenes/ui/modals/card_level_up_panel.tscn")
const SummonerLevelUpPanelScene: PackedScene = preload("res://scenes/ui/modals/summoner_level_up_panel.tscn")

## Constants
const CHOICE_BUTTON_SIZE: Vector2 = Vector2(150, 100)
const CHOICE_BUTTON_FONT_SIZE: int = 24
const CLAIMED_DIM_COLOR: Color = Color(0.6, 0.6, 0.6, 1)

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
		var pending: Variant = Campaign.GetPendingReward()
		if pending == null or not pending is Dictionary:
			push_error("RewardScreen: Invalid battle state (%s) - not a victory!" % BattleContext.BattleState.keys()[BattleContext.battle_state])
			SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)
			return

	# Get the current battle from profile (the battle we just won)
	var profile: Dictionary = ProfileRepo.GetActiveProfileDict()
	if not profile.is_empty():
		var campaign_progress: Variant = profile.get("campaign_progress", {})
		if campaign_progress is Dictionary:
			current_battle_id = campaign_progress.get("current_battle", "")

	# Handle pending reward state
	var pending_reward: Variant = Campaign.GetPendingReward()
	if pending_reward != null and pending_reward is Dictionary:
		var pending_dict: Dictionary = pending_reward
		var pending_battle_id: String = pending_dict.get("battle_id", "")

		if pending_battle_id == current_battle_id:
			# Resume pending reward
			is_pending_reward = true
			print("RewardScreen: Resuming pending reward for battle '%s'" % current_battle_id)
		elif not pending_battle_id.is_empty():
			# Stale pending reward - clear it
			print("RewardScreen: Clearing stale pending reward (was '%s', current is '%s')" % [pending_battle_id, current_battle_id])
			Campaign.ClearPendingReward()

	if current_battle_id.is_empty():
		push_error("RewardScreen: No current battle set!")
		return

	# Build reward spec params (previously computed inside the GDScript wrapper)
	var is_completed: bool = Campaign.IsBattleCompleted(current_battle_id)
	var pending_chosen_index: int = -1
	if is_pending_reward and pending_reward is Dictionary:
		pending_chosen_index = pending_reward.get("choice_index", -1)

	# Get reward specification from C# service
	var spec: Dictionary = RewardService.GetBattleRewardSpecAsDict(current_battle_id, is_completed, pending_chosen_index)
	# Convert reward_type string to StringName for GDScript compatibility
	if spec.has("reward_type"):
		spec["reward_type"] = StringName(spec.get("reward_type", "fixed"))

	# Get battle for display info
	var battle: Dictionary = Campaign.GetBattle(current_battle_id)
	battle_name_label.text = battle.get("name", "Unknown Battle")

	# Update state from spec
	reward_type = spec.get("reward_type", RewardTypeIDs.FIXED)
	chosen_reward_index = spec.get("chosen_index", -1)

	# Display rewards using spec
	_display_reward_spec(spec)

## =============================================================================
## REWARD DISPLAY
## =============================================================================

## Display rewards using the unified spec from RewardService (C#)
func _display_reward_spec(spec: Dictionary) -> void:
	var is_replay: bool = spec.get("is_replay", false)

	# Set section headers
	first_clear_header.text = Loc.t("ui.reward.first_clear_header")
	every_battle_header.text = Loc.t("ui.reward.every_battle_header")

	# XP rewards always display (even for replays) - goes in Every Battle section
	_display_summoner_xp_reward(spec.get("summoner_xp", 0))
	_display_card_xp_rewards(spec.get("card_xp", 0))

	if is_replay:
		# Replay - show First Clear as claimed, Every Battle XP earned
		_display_first_clear_claimed(spec)
		reward_ready_to_claim = true
		return

	# Set pending reward if not already set (first time victory)
	if not is_pending_reward:
		Campaign.SetPendingReward(current_battle_id, reward_type, -1)

	# First time victory - show all rewards normally
	first_clear_status.text = ""
	first_clear_status.visible = false
	first_clear_section.modulate = Color(1, 1, 1, 1)

	# Display gold
	_display_gold_reward(spec.get("gold_reward", 0))

	# Get card options from spec
	var card_options: Array = spec.get("card_options", [])
	var requires_choice: bool = spec.get("requires_choice", false)

	# Convert to typed array for internal use
	flexible_options = []
	for opt: Variant in card_options:
		if opt is Dictionary:
			flexible_options.append(opt)

	# Determine reward display based on spec
	if reward_type == RewardTypeIDs.FLEXIBLE:
		is_flexible_reward = true

	if requires_choice:
		# Check if player already made a choice (resuming)
		if chosen_reward_index >= 0 and chosen_reward_index < flexible_options.size():
			_display_card_reward_from_spec(flexible_options[chosen_reward_index])
			reward_ready_to_claim = true
		else:
			_show_flexible_choice_ui(flexible_options)
			reward_ready_to_claim = false
	elif flexible_options.size() > 0:
		# Fixed or auto-selected reward
		if reward_type == RewardTypeIDs.FLEXIBLE and is_flexible_reward:
			# Auto-select first option for FLEXIBLE without player_selects
			chosen_reward_index = 0
		_display_card_reward_from_spec(flexible_options[0])
		reward_ready_to_claim = true
	else:
		# No card rewards (gold-only or NONE type)
		reward_card_label.text = Loc.t("ui.reward.victory")
		reward_detail_label.text = ""
		reward_ready_to_claim = true


## Display First Clear section as already claimed (for replays)
func _display_first_clear_claimed(spec: Dictionary) -> void:
	# Show what was originally earned (dimmed)
	var gold_reward: int = spec.get("original_gold_reward", spec.get("gold_reward", 0))
	if gold_reward > 0:
		gold_reward_label.text = Loc.t("ui.reward.gold", {"amount": gold_reward})
	else:
		gold_reward_label.text = ""

	# Show card reward if there was one
	var card_options: Array = spec.get("card_options", [])
	if card_options.size() > 0:
		var card_spec: Dictionary = card_options[0]
		var display_name: String = card_spec.get("display_name", "")
		if display_name.is_empty():
			var catalog_id: String = card_spec.get("catalog_id", "")
			var card_data: Dictionary = CardCatalog.GetCardAsDict(catalog_id)
			display_name = card_data.get("card_name", "Card")
		reward_card_label.text = display_name
		reward_detail_label.text = ""
	else:
		reward_card_label.text = ""
		reward_detail_label.text = ""

	# Show claimed status and dim the section
	first_clear_status.text = Loc.t("ui.reward.first_clear_claimed")
	first_clear_status.visible = true
	first_clear_section.modulate = CLAIMED_DIM_COLOR


## Display a card reward from normalized spec format
func _display_card_reward_from_spec(card_spec: Dictionary) -> void:
	var catalog_id: String = card_spec.get("catalog_id", "")
	var rarity: StringName = StringName(card_spec.get("rarity", "common"))
	var count: int = card_spec.get("count", 1)
	var display_name: String = card_spec.get("display_name", "")

	if display_name.is_empty():
		var card_data: Dictionary = CardCatalog.GetCardAsDict(catalog_id)
		display_name = card_data.get("card_name", "Unknown")

	if count > 1:
		reward_card_label.text = "%dx %s" % [count, display_name]
	else:
		reward_card_label.text = display_name

	reward_detail_label.text = Loc.t("ui.reward.rarity", {"rarity": String(rarity).capitalize()})
	reward_card_label.add_theme_color_override("font_color", _get_rarity_color(rarity))

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

	# Get all deck cards for XP rewards
	var deck_cards: Array[String] = BattleContext.get_deck_card_ids()
	if deck_cards.is_empty():
		card_xp_section.visible = false
		return

	# Show XP amount header
	card_xp_header_label.text = Loc.t("ui.reward.card_xp_header")
	card_xp_amount_label.text = Loc.t("ui.reward.card_xp_each", {"amount": card_xp})

	# Get card service for progression info
	var card_service: Node = get_node_or_null(CSharpAutoloads.CARD_SERVICE)
	if not card_service:
		push_warning("RewardScreen: PlayerCardService not available for card XP display")
		card_xp_section.visible = false
		return

	# Create card items for each deck card
	for instance_id: String in deck_cards:
		var info: Dictionary = card_service.GetCardProgressionInfoDict(instance_id)
		if info.is_empty():
			continue

		var catalog_id: String = info.get("catalog_id", "")
		var card_data: Dictionary = CardCatalog.GetCardAsDict(catalog_id)
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
	var card_service: Node = get_node_or_null(CSharpAutoloads.CARD_SERVICE)
	if not card_service:
		return

	var info: Dictionary = card_service.GetCardProgressionInfoDict(instance_id)
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
	var card_service: Node = get_node_or_null(CSharpAutoloads.CARD_SERVICE)
	if not card_service:
		return

	# Find the card item and update it
	for child: Node in card_xp_grid.get_children():
		if child is CardXPItem and child.instance_id == instance_id:
			var info: Dictionary = card_service.GetCardProgressionInfoDict(instance_id)
			if info.is_empty():
				continue

			var catalog_id: String = info.get("catalog_id", "")
			var card_data: Dictionary = CardCatalog.GetCardAsDict(catalog_id)
			if card_data.is_empty():
				continue

			var card_name: String = card_data.get("card_name", "Unknown")
			var level: int = info.get("level", 1)
			var can_level_up: bool = info.get("can_level_up", false)
			var xp_progress: float = info.get("xp_progress", 0.0)

			if child.has_method("setup"):
				child.call("setup", instance_id, catalog_id, card_name, level, can_level_up, xp_progress)
			break

## Get color for a card rarity
func _get_rarity_color(rarity: StringName) -> Color:
	return RARITY_COLORS.get(rarity, Color.WHITE)

## =============================================================================
## FLEXIBLE REWARD SYSTEM
## =============================================================================

## Get active summoner ID for reward theming
func _get_active_summoner_id() -> String:
	var profile: Dictionary = ProfileRepo.GetActiveProfileDict()
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
		# Use normalized spec format (catalog_id)
		var display_name: String = option.get("display_name", "")

		if display_name.is_empty():
			# Fallback to catalog lookup
			var catalog_id: String = option.get("catalog_id", "")
			var card_data: Dictionary = CardCatalog.GetCardAsDict(catalog_id)
			display_name = card_data.get("card_name", "Unknown")

		var button: Button = Button.new()
		var is_guaranteed: bool = option.get("is_guaranteed", false)

		# Add badge for guaranteed vs pool options
		if is_guaranteed:
			button.text = "[%s] %s" % [Loc.t("ui.reward.guaranteed"), display_name]
		else:
			button.text = display_name

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
	Campaign.UpdatePendingChoice(index)

	if index >= 0 and index < flexible_options.size():
		# Hide choice UI and show selected card preview
		choice_container.visible = false
		reward_container.visible = true
		_display_card_reward_from_spec(flexible_options[index])

	# Mark ready to claim and enable continue
	reward_ready_to_claim = true
	continue_button.disabled = false

## =============================================================================
## NAVIGATION
## =============================================================================

func _on_continue_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)

	if not reward_ready_to_claim:
		# No reward to claim (replay or no rewards) - just clear any stale pending state
		Campaign.ClearPendingReward()
		_check_summoner_level_up()
		return

	# Determine the card reward to grant from flexible_options (already normalized by spec)
	var card_reward: Dictionary = {}
	if chosen_reward_index >= 0 and chosen_reward_index < flexible_options.size():
		# Use the chosen option (FLEXIBLE with choice made)
		card_reward = flexible_options[chosen_reward_index]
	elif flexible_options.size() > 0:
		# Use first option (FIXED or auto-selected FLEXIBLE)
		card_reward = flexible_options[0]

	# Single unified call to claim all rewards (gold + cards)
	var granted: Dictionary = Campaign.ClaimPendingReward()

	# Auto-add cards to deck if tutorial battle
	if not granted.get("cards", []).is_empty():
		_auto_add_cards_to_deck(granted)

	_check_summoner_level_up()

## Check if summoner can level up and show modal if so
func _check_summoner_level_up() -> void:
	var summoner_id: String = _get_active_summoner_id()
	if summoner_id.is_empty():
		_transition_to_map()
		return

	if SummonerProgression.CanLevelUp(summoner_id):
		_show_summoner_level_up_modal(summoner_id)
	else:
		_transition_to_map()

## Show summoner level-up modal
func _show_summoner_level_up_modal(summoner_id: String) -> void:
	var modal: SummonerLevelUpPanel = SummonerLevelUpPanelScene.instantiate() as SummonerLevelUpPanel
	if not modal:
		push_error("RewardScreen: Failed to instantiate SummonerLevelUpPanel")
		_transition_to_map()
		return

	add_child(modal)
	modal.open_for_summoner(summoner_id)

	# Connect signals
	modal.level_up_completed.connect(_on_summoner_level_up_completed)
	modal.cancelled.connect(_on_summoner_level_up_cancelled)

## Handle summoner level-up completion - check for more level-ups (multi-level jumps)
func _on_summoner_level_up_completed(summoner_id: String, _trait_id: String) -> void:
	# Check if summoner can level up again (multi-level jumps)
	if SummonerProgression.CanLevelUp(summoner_id):
		_show_summoner_level_up_modal(summoner_id)
	else:
		_transition_to_map()

## Handle summoner level-up cancellation - still transition (can level up later)
func _on_summoner_level_up_cancelled() -> void:
	_transition_to_map()

## Transition to campaign map
func _transition_to_map() -> void:
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

## =============================================================================
## AUTO-FILL DECK (TUTORIAL MODE)
## =============================================================================

## Automatically add granted cards to deck if this is a tutorial battle
func _auto_add_cards_to_deck(granted_card: Dictionary) -> void:
	# Check if this is a tutorial battle
	if not Campaign.IsBattleTutorial(current_battle_id):
		return  # Not a tutorial battle, don't auto-add

	# Get card instance IDs that were granted
	var instance_ids: Array = granted_card.get("instance_ids", [])
	if instance_ids.is_empty():
		push_warning("RewardScreen: No instance_ids in granted_card for auto-fill")
		return

	# Get active deck ID from profile
	var profile: Dictionary = ProfileRepo.GetActiveProfileDict()
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
		if Decks.AddCardToDeck(deck_id, card_instance_id):
			added_count += 1
		else:
			push_warning("RewardScreen: Failed to add card %s to deck" % card_instance_id)

	if added_count > 0:
		print("RewardScreen: Auto-added %d card(s) to deck (tutorial mode)" % added_count)
