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

	# Grant summoner unlock
	if rewards.has("summoner"):
		var summoner_id: String = rewards["summoner"]
		if not summoner_id.is_empty():
			if not ProfileRepo.is_summoner_unlocked(summoner_id):
				if not ProfileRepo.unlock_summoner(summoner_id):
					push_error("RewardService: Failed to unlock summoner %s" % summoner_id)
					success = false
				else:
					# Create SummonerInstance for the new summoner
					var summoner_instance: SummonerInstance = SummonerInstance.new()
					summoner_instance.summoner_id = summoner_id
					summoner_instance.level = 1
					summoner_instance.xp = 0
					if not ProfileRepo.save_summoner_instance(summoner_instance):
						push_error("RewardService: Failed to save summoner instance for %s" % summoner_id)
						# Don't fail the whole grant - summoner is unlocked, just instance save failed
					print("RewardService: Unlocked summoner '%s'" % summoner_id)

	# Grant cosmetic
	if rewards.has("cosmetic"):
		var cosmetic_id: String = rewards["cosmetic"]
		if not cosmetic_id.is_empty():
			if not ProfileRepo.grant_cosmetic(cosmetic_id):
				push_error("RewardService: Failed to grant cosmetic %s" % cosmetic_id)
				success = false
			else:
				print("RewardService: Granted cosmetic '%s'" % cosmetic_id)

	# Grant emote
	if rewards.has("emote"):
		var emote_id: String = rewards["emote"]
		if not emote_id.is_empty():
			if not ProfileRepo.grant_emote(emote_id):
				push_error("RewardService: Failed to grant emote %s" % emote_id)
				success = false
			else:
				print("RewardService: Granted emote '%s'" % emote_id)

	# Grant cosmetics array (legacy - kept for backwards compatibility)
	if rewards.has("cosmetics"):
		var cosmetics_variant: Variant = rewards["cosmetics"]
		if cosmetics_variant is Array:
			var cosmetics_array: Array = cosmetics_variant
			for cosmetic_item: Variant in cosmetics_array:
				if cosmetic_item is String:
					var cosmetic_id: String = cosmetic_item
					if not ProfileRepo.grant_cosmetic(cosmetic_id):
						push_error("RewardService: Failed to grant cosmetic %s" % cosmetic_id)
						success = false

	return success
