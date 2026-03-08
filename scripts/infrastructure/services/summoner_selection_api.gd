class_name SummonerSelectionApi
extends RefCounted

static func get_active_summoner_id() -> String:
	return SafeTypeUtils.string(SummonerSelection.call("GetActiveSummonerId"), "")

static func get_unlocked_summoner_ids_array() -> Array:
	return SafeTypeUtils.array(SummonerSelection.call("GetUnlockedSummonerIdsArray"))

static func save_summoner_instance_dict(summoner_data: Dictionary) -> bool:
	return SafeTypeUtils.bool_val(SummonerSelection.call("SaveSummonerInstanceDict", summoner_data), false)

static func set_starting_summoner(summoner_id: String, chosen_random: bool) -> bool:
	return SafeTypeUtils.bool_val(SummonerSelection.call("SetStartingSummoner", summoner_id, chosen_random), false)

static func set_active_summoner(summoner_id: String, options: Variant) -> void:
	SummonerSelection.call("SetActiveSummoner", summoner_id, options)

static func is_summoner_unlocked(summoner_id: String) -> bool:
	return SafeTypeUtils.bool_val(SummonerSelection.call("IsSummonerUnlocked", summoner_id), false)

static func get_summoner_instance_dict(summoner_id: String) -> Dictionary:
	return SafeTypeUtils.dict(SummonerSelection.call("GetSummonerInstanceDict", summoner_id))
