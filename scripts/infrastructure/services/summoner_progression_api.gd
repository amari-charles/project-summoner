class_name SummonerProgressionApi
extends RefCounted

static func get_summoner_progression_info(summoner_id: String) -> Dictionary:
	return SafeTypeUtils.dict(SummonerProgression.call("GetSummonerProgressionInfo", summoner_id))

static func get_computed_stats_for_summoner(summoner_id: String) -> Dictionary:
	return SafeTypeUtils.dict(SummonerProgression.call("GetComputedStatsForSummoner", summoner_id))

static func get_all_trait_ids_for_summoner(summoner_id: String) -> Array:
	return SafeTypeUtils.array(SummonerProgression.call("GetAllTraitIdsForSummoner", summoner_id))

static func get_unspent_trait_points(summoner_id: String) -> int:
	return SafeTypeUtils.int_val(SummonerProgression.call("GetUnspentTraitPoints", summoner_id), 0)

static func roll_trait_offers(summoner_id: String, count: int = 3) -> Array:
	return SafeTypeUtils.array(SummonerProgression.call("RollTraitOffers", summoner_id, count))

static func spend_trait_point(summoner_id: String, trait_id: String) -> bool:
	return SafeTypeUtils.bool_val(SummonerProgression.call("SpendTraitPoint", summoner_id, trait_id), false)
