extends Node
# RewardService is registered as autoload "RewardService", no class_name needed
# This GDScript wrapper delegates to C# RewardServiceCS for implementation.

## Reward Service - Centralized Reward Handling
##
## Handles all reward operations (granting, flexible reward generation).
## Used by: ShopService, CampaignService, future achievement/daily quest systems
## Prevents logic duplication and ensures consistent reward handling
##
## Usage:
##   RewardService.grant_rewards({"gold": 100, "cards": [...]})
##   var options = RewardService.generate_reward_options(config, summoner_id)
##   RewardService.grant_reward(selected_option)
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

## =============================================================================
## FLEXIBLE REWARD GENERATION
## =============================================================================

## Generate reward options for a battle reward screen.
## Returns guaranteed (summoner-themed) options plus pool-drawn options.
##
## @param config Dictionary with:
##   - "guaranteed_count": int - Number of summoner-themed options
##   - "pool_count": int - Number of pool-drawn options
##   - "pool_id": String - Pool ID for non-guaranteed options (default: "standard_cards")
##   - "collection_filter": String - "none", "exclude_owned", or "exclude_duplicates"
## @param summoner_id String - Active summoner ID for element theming
## @return Array[Dictionary] - List of reward options
func generate_reward_options(config: Dictionary, summoner_id: String) -> Array[Dictionary]:
	if _cs_service:
		var result: Array = _cs_service.GenerateRewardOptionsDict(config, summoner_id)
		var typed_result: Array[Dictionary] = []
		for item: Variant in result:
			if item is Dictionary:
				typed_result.append(item)
		return typed_result
	push_warning("RewardService.generate_reward_options: C# service not available")
	return []


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

	var card_service: Node = get_node_or_null("/root/CardServiceCS")
	if card_service == null:
		push_warning("RewardService: CardServiceCS not available")
		return instance_ids

	# Grant each copy and collect instance IDs
	for i: int in range(count):
		var instance_id: String = card_service.GrantCard(catalog_id, rarity)
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
