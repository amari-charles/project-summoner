extends GutTest


func test_combined_results_scene_exposes_progression_rewards_and_choice_regions() -> void:
	var packed_scene: PackedScene = load("res://scenes/meta/screens/post_battle_results.tscn")
	var results: PostBattleResults = packed_scene.instantiate() as PostBattleResults
	assert_not_null(results)
	assert_not_null(results.get_node_or_null("Center/Panel/Margin/Content/ResultsHeader/ResultsTitleLabel"))
	var outcome_label: Label = results.get_node(
		"Center/Panel/Margin/Content/ResultsHeader/OutcomeLabel"
	) as Label
	assert_eq(outcome_label.get_theme_font_size("font_size"), 24)
	assert_not_null(results.get_node_or_null("Center/Panel/Margin/Content/ProgressionSection"))
	assert_not_null(results.get_node_or_null("Center/Panel/Margin/Content/ProgressionSection/SummonerRow/SummonerXPBar"))
	assert_not_null(results.get_node_or_null("Center/Panel/Margin/Content/ProgressionSection/CardProgressionSection/CardProgressionRows"))
	assert_not_null(results.get_node_or_null("Center/Panel/Margin/Content/RewardsSection/Rewards"))
	assert_not_null(results.get_node_or_null("Center/Panel/Margin/Content/ChoiceSection/ChoiceButtons"))
	assert_not_null(results.get_node_or_null("Center/Panel/Margin/Content/ContinueButton"))
	results.free()


func test_results_presents_authoritative_grants_without_mutating_xp() -> void:
	var script_text: String = _read("res://scripts/meta/screens/post_battle_results.gd")
	assert_true(script_text.contains("progression_grants"))
	assert_true(script_text.contains("GetBattleRewards"))
	assert_true(script_text.contains("get_encounter_completion_summary"))
	assert_true(script_text.contains("ClaimBattleReward"))
	assert_true(script_text.contains("CardVisualScene"))
	assert_false(script_text.contains("CardWidgetScene"))
	assert_false(script_text.contains("grant_xp("))
	assert_false(script_text.contains("GrantXp"))


func test_pending_offer_detection_supports_sequential_reward_choices() -> void:
	var packed_scene: PackedScene = load("res://scenes/meta/screens/post_battle_results.tscn")
	var results: PostBattleResults = packed_scene.instantiate() as PostBattleResults
	assert_true(results._has_pending_offer({
		"reward_offers": [
			{"display_state": "claimed"},
			{"display_state": "pending"},
		]
	}))
	assert_false(results._has_pending_offer({
		"reward_offers": [
			{"display_state": "claimed"},
			{"display_state": "forfeited"},
		]
	}))
	results.free()


func _read(path: String) -> String:
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	assert_not_null(file)
	var contents: String = file.get_as_text()
	file.close()
	return contents
