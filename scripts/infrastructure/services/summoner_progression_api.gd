class_name SummonerProgressionApi
extends RefCounted

static func get_summoner_progression_info(summoner_id: String) -> Dictionary:
	return SafeTypeUtils.dict(SummonerProgression.call("GetSummonerProgressionInfo", summoner_id))

static func level_up_summoner(summoner_id: String) -> bool:
	return SafeTypeUtils.bool_val(SummonerProgression.call("LevelUpSummoner", summoner_id), false)

static func get_computed_stats_for_summoner(summoner_id: String) -> Dictionary:
	return SafeTypeUtils.dict(SummonerProgression.call("GetComputedStatsForSummoner", summoner_id))

static func can_level_up(summoner_id: String) -> bool:
	return SafeTypeUtils.bool_val(SummonerProgression.call("CanLevelUp", summoner_id), false)

static func get_all_trait_ids_for_summoner(summoner_id: String) -> Array:
	return SafeTypeUtils.array(SummonerProgression.call("GetAllTraitIdsForSummoner", summoner_id))
