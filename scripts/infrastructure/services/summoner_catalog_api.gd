class_name SummonerCatalogApi
extends RefCounted

static func get_summoner(summoner_id: String) -> Dictionary:
	return SafeTypeUtils.dict(SummonerCatalog.call("GetSummoner", summoner_id))
