class_name EconomyApi
extends RefCounted

static func get_campaign_gold() -> int:
	return SafeTypeUtils.int_val(Economy.call("GetCampaignGold"), 0)

static func get_gold() -> int:
	return SafeTypeUtils.int_val(Economy.call("GetGold"), 0)
