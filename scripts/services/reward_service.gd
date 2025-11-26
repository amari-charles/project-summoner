extends Node

## Centralized reward granting service
##
## Used by: ShopService, CampaignService, future achievement/daily quest systems
## Prevents logic duplication and ensures consistent reward handling
##
## Atomicity Note:
## grant_rewards() is best-effort and does not roll back partial success internally.
## If granting 3 cards and the second fails, the first card remains in collection.
## Callers requiring atomic behavior must handle their own rollback (e.g., refunding gold).

## Grant rewards to the player
##
## @param rewards Dictionary with keys:
##   - "cards": Array[Dictionary] with {catalog_id: String, count: int, rarity: String}
##   - "gold": int (can be negative for costs)
##   - "cosmetics": Array[String] (future)
## @return bool True if all rewards granted successfully, false if any failed
func grant_rewards(rewards: Dictionary) -> bool:
	var success: bool = true

	# Grant gold
	if rewards.has("gold"):
		var gold_variant: Variant = rewards["gold"]
		var gold: int = gold_variant
		ProfileRepo.update_resources({"gold": gold})
		# update_resources doesn't return bool, assume success

	# Grant cards
	if rewards.has("cards"):
		var cards_variant: Variant = rewards["cards"]
		if cards_variant is Array:
			var cards: Array = cards_variant
			for card_grant: Variant in cards:
				if not card_grant is Dictionary:
					continue
				var card_dict: Dictionary = card_grant
				var catalog_id: String = card_dict.get("catalog_id", "")
				var count_variant: Variant = card_dict.get("count", 1)
				var count: int = count_variant
				var rarity: StringName = card_dict.get("rarity", RarityIDs.COMMON)

				# Prepare cards array for grant_cards() call
				var cards_to_grant: Array[Dictionary] = []
				for i: int in range(count):
					cards_to_grant.append({"catalog_id": catalog_id, "rarity": rarity})

				# Use Collection.grant_cards() (correct API)
				var instance_ids: Array[String] = Collection.grant_cards(cards_to_grant)
				if instance_ids.size() != count:
					push_error("RewardService: Failed to grant all %s cards (granted %d/%d)" % [catalog_id, instance_ids.size(), count])
					success = false

	# Grant cosmetics (future)
	if rewards.has("cosmetics"):
		# TODO: Implement cosmetic granting
		push_warning("RewardService: Cosmetic granting not yet implemented")

	return success
