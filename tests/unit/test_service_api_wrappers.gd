extends GutTest

## Focused tests for GDScript service wrapper behavior and UI guards.

var _removed_card_service: Node = null


func after_each() -> void:
	if _removed_card_service:
		get_tree().root.add_child(_removed_card_service)
		_removed_card_service = null
	BattleContext.clear()


func test_card_service_api_logs_error_and_returns_empty_when_service_missing() -> void:
	var root: Window = get_tree().root
	var card_service: Node = root.get_node_or_null("CardService")
	assert_true(card_service != null, "Expected CardService autoload to exist for test setup")

	_removed_card_service = card_service
	root.remove_child(card_service)

	var result: Dictionary = CardServiceApi.get_card_dict("missing_instance")
	assert_true(result.is_empty(), "Wrapper should return empty dictionary when service is unavailable")
	assert_push_error("CardServiceApi.get_card_dict: CardService autoload not found")


func test_bpa_c01_progression_authority_reports_ready() -> void:
	var status: Dictionary = ProgressionAuthority.GetProgressionAuthorityStatus()
	assert_eq(status.get("status"), "ready")
	assert_true(status.get("can_start_battle", false))


func test_item_adapter_contract_exposes_every_retained_developer_operation() -> void:
	for method_name: String in [
		"GrantItemToSummoner",
		"GrantSharedEventItem",
		"GetOwnedItemsDict",
		"GetEquippedItemsDict",
		"ListItemsForSlotDict",
		"EquipItemStr",
		"UnequipItemStr",
		"ClearAllItems",
	]:
		assert_true(Items.has_method(method_name), "Missing item adapter method: %s" % method_name)
