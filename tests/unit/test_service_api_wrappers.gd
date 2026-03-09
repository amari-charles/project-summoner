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


func test_reward_screen_hides_card_xp_section_when_no_items_render() -> void:
	BattleContext.clear()
	BattleContext.store_deck_card_ids([{"id": "missing_instance"}])

	var reward_screen_script: GDScript = load("res://scripts/meta/screens/reward_screen.gd")
	var reward_screen: RewardScreen = reward_screen_script.new()

	var card_xp_section: VBoxContainer = VBoxContainer.new()
	var card_xp_grid: HBoxContainer = HBoxContainer.new()
	var card_xp_header_label: Label = Label.new()
	var card_xp_amount_label: Label = Label.new()

	card_xp_section.visible = true
	reward_screen.card_xp_section = card_xp_section
	reward_screen.card_xp_grid = card_xp_grid
	reward_screen.card_xp_header_label = card_xp_header_label
	reward_screen.card_xp_amount_label = card_xp_amount_label

	reward_screen._display_card_xp_rewards(10)

	assert_false(card_xp_section.visible, "Card XP section should be hidden when no XP cards are renderable")
	assert_eq(card_xp_grid.get_child_count(), 0, "No card XP items should be created for missing progression data")

	reward_screen.free()
	card_xp_section.free()
	card_xp_grid.free()
	card_xp_header_label.free()
	card_xp_amount_label.free()


func test_reward_screen_extract_current_battle_id_supports_flat_campaign_progress_layout() -> void:
	var reward_screen_script: GDScript = load("res://scripts/meta/screens/reward_screen.gd")
	var reward_screen: RewardScreen = reward_screen_script.new()

	var profile: Dictionary = {
		"campaign_progress": {
			"current_battle": "flat_battle_id"
		}
	}

	var battle_id: String = reward_screen._extract_current_battle_id(profile)
	assert_eq(battle_id, "flat_battle_id", "RewardScreen should read flat campaign_progress.current_battle")

	reward_screen.free()


func test_reward_screen_extract_current_battle_id_supports_per_summoner_campaign_progress_layout() -> void:
	var reward_screen_script: GDScript = load("res://scripts/meta/screens/reward_screen.gd")
	var reward_screen: RewardScreen = reward_screen_script.new()

	var active_summoner_id: String = SummonerSelectionApi.get_active_summoner_id()
	assert_false(active_summoner_id.is_empty(), "Expected an active summoner for per-summoner progress lookup")

	var profile: Dictionary = {
		"campaign_progress": {
			active_summoner_id: {
				"current_battle": "per_summoner_battle_id"
			}
		}
	}

	var battle_id: String = reward_screen._extract_current_battle_id(profile)
	assert_eq(battle_id, "per_summoner_battle_id", "RewardScreen should read campaign_progress[active_summoner].current_battle")

	reward_screen.free()
