class_name CardServiceApi
extends RefCounted

static func _service() -> Node:
	var tree: SceneTree = Engine.get_main_loop() as SceneTree
	if tree == null or tree.root == null:
		return null
	return tree.root.get_node_or_null(CSharpAutoloads.CARD_SERVICE)

static func grant_card(catalog_id: String, rarity: String) -> String:
	var service: Node = _service()
	if service == null:
		return ""
	return SafeTypeUtils.string(service.call("GrantCard", catalog_id, rarity), "")

static func grant_cards_from_array(cards_to_grant: Array) -> Array:
	var service: Node = _service()
	if service == null:
		return []
	return SafeTypeUtils.array(service.call("GrantCardsFromArray", cards_to_grant))

static func list_cards_dict() -> Array:
	var service: Node = _service()
	if service == null:
		return []
	return SafeTypeUtils.array(service.call("ListCardsDict"))

static func get_collection_summary_dict() -> Array:
	var service: Node = _service()
	if service == null:
		return []
	return SafeTypeUtils.array(service.call("GetCollectionSummaryDict"))

static func get_card_progression_info_dict(instance_id: String) -> Dictionary:
	var service: Node = _service()
	if service == null:
		return {}
	return SafeTypeUtils.dict(service.call("GetCardProgressionInfoDict", instance_id))

static func get_card_dict(instance_id: String) -> Dictionary:
	var service: Node = _service()
	if service == null:
		return {}
	return SafeTypeUtils.dict(service.call("GetCardDict", instance_id))

static func get_effective_stats_dict(instance_id: String) -> Dictionary:
	var service: Node = _service()
	if service == null:
		return {}
	return SafeTypeUtils.dict(service.call("GetEffectiveStatsDict", instance_id))

static func get_applied_traits(instance_id: String) -> Array:
	var service: Node = _service()
	if service == null:
		return []
	return SafeTypeUtils.array(service.call("GetAppliedTraits", instance_id))

static func get_card_trait_dict(catalog_id: String, trait_id: String) -> Dictionary:
	var service: Node = _service()
	if service == null:
		return {}
	return SafeTypeUtils.dict(service.call("GetCardTraitDict", catalog_id, trait_id))

static func get_available_traits(instance_id: String) -> Array:
	var service: Node = _service()
	if service == null:
		return []
	return SafeTypeUtils.array(service.call("GetAvailableTraits", instance_id))

static func level_up_card(instance_id: String, selected_trait_id: String) -> bool:
	var service: Node = _service()
	if service == null:
		return false
	return SafeTypeUtils.bool_val(service.call("LevelUpCard", instance_id, selected_trait_id), false)
