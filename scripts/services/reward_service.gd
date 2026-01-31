extends Node
# RewardService is registered as autoload "RewardService", no class_name needed
# This GDScript wrapper delegates to C# RewardServiceCS for implementation.

## Reward Service - Centralized Reward Handling

## =============================================================================
## CONSTANTS
## =============================================================================

## Default number of cards to draw for flexible rewards
const DEFAULT_DRAW_COUNT: int = 3
##
## Handles all reward operations:
## - get_reward_spec() - Get unified reward specification for a battle
## - grant_rewards() - Grant rewards dictionary (gold, cards, etc.)
## - grant_battle_rewards() - Grant battle completion rewards
##
## Used by: RewardScreen, CampaignService, future achievement/daily quest systems
##
## Usage:
##   var spec = RewardService.get_reward_spec(battle_id)
##   RewardService.grant_battle_rewards(battle, card_reward)
##
## Atomicity Note:
## grant_rewards() is best-effort and does not roll back partial success internally.
## If granting 3 cards and the second fails, the first card remains in collection.
## Callers requiring atomic behavior must handle their own rollback (e.g., refunding gold).

## Signals
signal rewards_granted(rewards: Dictionary)
signal reward_grant_failed(reason: String)

## =============================================================================
## C# BRIDGE
## =============================================================================

var _cs_service: Node = null

func _ready() -> void:
	print("RewardService (GD): Initializing as thin wrapper...")
	call_deferred("_connect_to_cs")


func _connect_to_cs() -> void:
	_cs_service = RewardServiceCS
	if not _cs_service:
		push_warning("RewardService (GD): C# service not available")
		return

	# Forward C# signals to GDScript signals
	if _cs_service.has_signal("RewardsGranted"):
		_cs_service.RewardsGranted.connect(_on_cs_rewards_granted)
	if _cs_service.has_signal("RewardGrantFailed"):
		_cs_service.RewardGrantFailed.connect(_on_cs_reward_grant_failed)

	print("RewardService (GD): Connected to C# RewardServiceCS")


func _on_cs_rewards_granted(rewards_dict: Dictionary) -> void:
	rewards_granted.emit(rewards_dict)


func _on_cs_reward_grant_failed(reason: String) -> void:
	reward_grant_failed.emit(reason)

## =============================================================================
## REWARD SPECIFICATION
## =============================================================================

## Get complete reward specification for a battle.
## Returns unified structure regardless of reward_type.
## This is the primary API for UI to understand what rewards are available.
##
## @param battle_id: The battle ID to get reward spec for
## @return Dictionary with:
##   - is_replay: bool - True if battle already completed
##   - reward_type: StringName - FIXED | FLEXIBLE | NONE
##   - gold_reward: int - Gold amount
##   - summoner_xp: int - Summoner XP reward
##   - card_xp: int - Card XP reward
##   - card_options: Array[Dictionary] - Unified card option format
##   - requires_choice: bool - True if player must select from options
##   - chosen_index: int - Index of choice from pending reward (-1 if not chosen)
func get_reward_spec(battle_id: String) -> Dictionary:
	## Get reward spec from C# BattleRewardSpec (typed, via EventCatalog)
	if _cs_service == null:
		push_warning("RewardService: C# service not available")
		return _get_empty_spec()

	# Check completion and pending choice state
	var is_completed: bool = Campaign.is_battle_completed(battle_id)
	var chosen_index: int = -1

	# Check for pending reward (resuming after exit)
	var pending: Variant = Campaign.get_pending_reward()
	if pending != null and pending is Dictionary:
		var pending_dict: Dictionary = pending
		if pending_dict.get("battle_id", "") == battle_id:
			chosen_index = pending_dict.get("choice_index", -1)

	# Get typed spec from C# and convert to Dictionary
	var spec: Dictionary = _cs_service.GetBattleRewardSpecAsDict(battle_id, is_completed, chosen_index)

	# Convert reward_type string to StringName for GDScript compatibility
	if spec.has("reward_type"):
		spec["reward_type"] = StringName(spec.get("reward_type", "fixed"))

	return spec


## Get empty spec for error cases
func _get_empty_spec() -> Dictionary:
	return {
		"is_replay": false,
		"reward_type": StringName("fixed"),
		"gold_reward": 0,
		"summoner_xp": 0,
		"card_xp": 0,
		"card_options": [],
		"requires_choice": false,
		"chosen_index": -1,
	}


## =============================================================================
## REWARD GRANTING (Legacy API)
## =============================================================================

## Grant rewards to the player
##
## @param rewards Dictionary with keys:
##   - "cards": Array[Dictionary] with {catalog_id: String, count: int, rarity: String}
##   - "gold": int (can be negative for costs)
##   - "campaign_gold": int (campaign-scoped gold)
##   - "summoner": String (summoner_id to unlock)
##   - "cosmetic": String (cosmetic_id)
##   - "emote": String (emote_id)
##   - "cosmetics": Array[String] (legacy array format)
## @return bool True if all rewards granted successfully, false if any failed
func grant_rewards(rewards: Dictionary) -> bool:
	if _cs_service:
		return _cs_service.GrantRewards(rewards)
	push_warning("RewardService.grant_rewards: C# service not available")
	return false

## Grant a single reward option (from flexible reward selection)
##
## @param option Dictionary with:
##   - "type": String - "card", "campaign_gold", "gold", etc.
##   - "id": String - catalog_id for cards, item_id for items
##   - "amount": int - Quantity
##   - "rarity": String - For card rewards
## @return bool True if granted successfully
func grant_reward(option: Dictionary) -> bool:
	if _cs_service:
		return _cs_service.GrantRewardDict(option)
	push_warning("RewardService.grant_reward: C# service not available")
	return false


## =============================================================================
## BATTLE REWARD GRANTING
## =============================================================================

## Grant all rewards for a completed battle
## This is the unified entry point for granting battle rewards (gold + cards).
## @param battle: Battle config dictionary (from Campaign.get_battle())
## @param card_reward: The card reward option to grant (from flexible selection or fixed battle config)
## @return Dictionary with granted rewards {gold: int, cards: Array, instance_ids: Array}
func grant_battle_rewards(battle: Dictionary, card_reward: Dictionary) -> Dictionary:
	var result: Dictionary = {"gold": 0, "cards": [], "instance_ids": []}

	# Grant campaign gold
	var gold_reward: int = battle.get("gold_reward", 0)
	if gold_reward > 0:
		Economy.add_campaign_gold(gold_reward)
		result["gold"] = gold_reward
		print("RewardService: Granted %d campaign gold" % gold_reward)

	# Grant card reward via CardServiceCS for instance ID tracking
	if not card_reward.is_empty():
		var instance_ids: Array = _grant_card_reward(card_reward)
		if not instance_ids.is_empty():
			result["cards"].append(card_reward)
			result["instance_ids"] = instance_ids
			print("RewardService: Granted card reward with %d instance(s)" % instance_ids.size())

	return result


## Grant a card reward and return instance IDs
## Uses CardServiceCS.GrantCard to get instance IDs for deck auto-add
func _grant_card_reward(card_reward: Dictionary) -> Array:
	var instance_ids: Array = []

	# Handle both formats: {catalog_id, rarity, count} and {id, type, rarity, amount}
	var catalog_id: String = card_reward.get("id", card_reward.get("catalog_id", ""))
	var rarity: String = String(card_reward.get("rarity", "common"))
	var count: int = card_reward.get("amount", card_reward.get("count", 1))

	if catalog_id.is_empty():
		push_warning("RewardService: Card reward missing catalog_id/id")
		return instance_ids

	# Grant each copy and collect instance IDs
	for i: int in range(count):
		var instance_id: String = CardServiceCS.GrantCard(catalog_id, rarity)
		if not instance_id.is_empty():
			instance_ids.append(instance_id)
		else:
			push_warning("RewardService: Failed to grant card %s" % catalog_id)

	return instance_ids

## =============================================================================
## UTILITY
## =============================================================================

## Get IDs of all cards the player currently owns.
## Useful for excluding owned cards from reward pools.
func get_owned_catalog_ids() -> Array[String]:
	if _cs_service:
		var result: Array = _cs_service.GetOwnedCatalogIds()
		var typed_result: Array[String] = []
		for item: Variant in result:
			if item is String:
				typed_result.append(item)
		return typed_result
	push_warning("RewardService.get_owned_catalog_ids: C# service not available")
	return []
