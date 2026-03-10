class_name TraitTreeApi
extends RefCounted

static func _service() -> Node:
	var tree: SceneTree = Engine.get_main_loop() as SceneTree
	if tree == null or tree.root == null:
		return null
	return tree.root.get_node_or_null(CSharpAutoloads.TRAIT_TREE_SERVICE)

static func _require_service(api_method: String) -> Node:
	var service: Node = _service()
	if service == null:
		push_error("TraitTreeApi.%s: TraitTreeService autoload not found" % api_method)
	return service

static func get_summoner_tree_view_model(summoner_id: String) -> Dictionary:
	var service: Node = _require_service("get_summoner_tree_view_model")
	if service == null:
		return {}
	return SafeTypeUtils.dict(service.call("GetSummonerTreeViewModel", summoner_id))

static func get_card_tree_view_model(card_instance_id: String) -> Dictionary:
	var service: Node = _require_service("get_card_tree_view_model")
	if service == null:
		return {}
	return SafeTypeUtils.dict(service.call("GetCardTreeViewModel", card_instance_id))

static func get_trait_node_detail(owner_type: String, owner_id: String, trait_id: String) -> Dictionary:
	var service: Node = _require_service("get_trait_node_detail")
	if service == null:
		return {}
	return SafeTypeUtils.dict(service.call("GetTraitNodeDetail", owner_type, owner_id, trait_id))

static func try_unlock_trait(owner_type: String, owner_id: String, trait_id: String) -> Dictionary:
	var service: Node = _require_service("try_unlock_trait")
	if service == null:
		return {
			"success": false,
			"reason": "TraitTreeService unavailable"
		}
	return SafeTypeUtils.dict(service.call("TryUnlockTrait", owner_type, owner_id, trait_id))
