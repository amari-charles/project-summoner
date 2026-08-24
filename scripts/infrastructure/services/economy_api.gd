class_name EconomyApi
extends RefCounted

static func get_gold() -> int:
	return SafeTypeUtils.int_val(Economy.call("GetGold"), 0)
