class_name CardServiceApi
extends RefCounted

static func _service() -> Node:
	var tree: SceneTree = Engine.get_main_loop() as SceneTree
	if tree == null or tree.root == null:
		return null
	return tree.root.get_node_or_null(CSharpAutoloads.CARD_SERVICE)

static func _require_service(api_method: String) -> Node:
	var service: Node = _service()
	if service == null:
		push_error("CardServiceApi.%s: CardService autoload not found" % api_method)
	return service

static func grant_card(catalog_id: String, rarity: String) -> String:
	var service: Node = _require_service("grant_card")
	if service == null:
		return ""
	return SafeTypeUtils.string(service.call("GrantCard", catalog_id, rarity), "")

static func grant_cards_from_array(cards_to_grant: Array) -> Array:
	var service: Node = _require_service("grant_cards_from_array")
	if service == null:
		return []
	return SafeTypeUtils.array(service.call("GrantCardsFromArray", cards_to_grant))

static func list_cards_dict() -> Array:
	var service: Node = _require_service("list_cards_dict")
	if service == null:
		return []
	return SafeTypeUtils.array(service.call("ListCardsDict"))

static func get_collection_summary_dict() -> Array:
	var service: Node = _require_service("get_collection_summary_dict")
	if service == null:
		return []
	return SafeTypeUtils.array(service.call("GetCollectionSummaryDict"))

static func get_card_progression_info_dict(instance_id: String) -> Dictionary:
	var service: Node = _require_service("get_card_progression_info_dict")
	if service == null:
		return {}
	return SafeTypeUtils.dict(service.call("GetCardProgressionInfoDict", instance_id))

static func grant_xp(instance_id: String, amount: int) -> int:
	var service: Node = _require_service("grant_xp")
	if service == null:
		return 0
	return SafeTypeUtils.int_val(service.call("GrantXp", instance_id, amount), 0)

static func get_card_dict(instance_id: String) -> Dictionary:
	var service: Node = _require_service("get_card_dict")
	if service == null:
		return {}
	return SafeTypeUtils.dict(service.call("GetCardDict", instance_id))

static func get_effective_stats_dict(instance_id: String) -> Dictionary:
	var service: Node = _require_service("get_effective_stats_dict")
	if service == null:
		return {}
	if not service.has_method("GetEffectiveStatsDict"):
		return {}
	return SafeTypeUtils.dict(service.call("GetEffectiveStatsDict", instance_id))

static func get_applied_traits(instance_id: String) -> Array:
	var service: Node = _require_service("get_applied_traits")
	if service == null:
		return []
	return SafeTypeUtils.array(service.call("GetAppliedTraits", instance_id))

static func get_card_trait_dict(trait_id: String) -> Dictionary:
	var service: Node = _require_service("get_card_trait_dict")
	if service == null:
		return {}
	return SafeTypeUtils.dict(service.call("GetCardTraitDict", trait_id))

static func level_up_card(instance_id: String) -> bool:
	var service: Node = _require_service("level_up_card")
	if service == null:
		return false
	return SafeTypeUtils.bool_val(service.call("LevelUpCard", instance_id), false)

static func get_level_up_resource_cost_dict(instance_id: String) -> Dictionary:
	var service: Node = _require_service("get_level_up_resource_cost_dict")
	if service == null:
		return {}
	if not service.has_method("GetLevelUpResourceCostDict"):
		return {}
	return SafeTypeUtils.dict(service.call("GetLevelUpResourceCostDict", instance_id))

static func get_unspent_trait_points(instance_id: String) -> int:
	var service: Node = _require_service("get_unspent_trait_points")
	if service == null:
		return 0
	return SafeTypeUtils.int_val(service.call("GetCardUnspentTraitPoints", instance_id), 0)

static func roll_trait_offers(instance_id: String, count: int = 3) -> Array:
	var service: Node = _require_service("roll_trait_offers")
	if service == null:
		return []
	return SafeTypeUtils.array(service.call("RollCardTraitOffers", instance_id, count))

static func spend_trait_point(instance_id: String, trait_id: String) -> bool:
	var service: Node = _require_service("spend_trait_point")
	if service == null:
		return false
	return SafeTypeUtils.bool_val(service.call("SpendCardTraitPoint", instance_id, trait_id), false)
