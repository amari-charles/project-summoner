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
	var report_text: String = _read("res://scripts/application/post_battle_report.gd")
	assert_true(report_text.contains("progression_grants"))
	assert_true(script_text.contains("GetBattleRewards"))
	assert_true(script_text.contains("get_encounter_completion_summary"))
	assert_true(script_text.contains("ClaimBattleReward"))
	assert_true(script_text.contains("CardVisualScene"))
	assert_false(script_text.contains("CardWidgetScene"))
	assert_false(script_text.contains("grant_xp("))
	assert_false(script_text.contains("GrantXp"))
	assert_true(report_text.contains("class_name PostBattleReport"))
	assert_true(script_text.contains("PostBattleReport.from_campaign_result"))
	assert_true(script_text.contains("PostBattleReport.from_encounter_summary"))


func test_pending_offer_detection_supports_sequential_reward_choices() -> void:
	var pending: PostBattleReport = PostBattleReport.from_campaign_result({
		"reward_offers": [
			{"display_state": "claimed"},
			{"display_state": "pending"},
		]
	}, "res://destination.tscn", "victory")
	var complete: PostBattleReport = PostBattleReport.from_campaign_result({
		"reward_offers": [
			{"display_state": "claimed"},
			{"display_state": "forfeited"},
		]
	}, "res://destination.tscn", "victory")
	assert_true(pending.has_pending_offer())
	assert_false(complete.has_pending_offer())


func test_typed_report_normalizes_no_reward_and_selected_grants() -> void:
	var report: PostBattleReport = PostBattleReport.from_campaign_result({
		"outcome": "victory",
		"progression_grants": [{"kind": "summoner_xp", "amount": 25}],
		"reward_offers": [{
			"display_state": "claimed",
			"options": [{
				"is_selected": true,
				"grants": [{"kind": "card", "content_id": "fire_wisp"}],
			}],
		}],
	}, "res://destination.tscn", "defeat")
	assert_eq(report.outcome, &"victory")
	assert_eq(report.destination, "res://destination.tscn")
	assert_eq(report.grants.size(), 2)
	assert_false(report.has_pending_offer())

	var no_reward: PostBattleReport = PostBattleReport.from_encounter_summary(
		{"outcome": "defeat"},
		"res://campus.tscn",
		"victory"
	)
	assert_eq(no_reward.outcome, &"defeat")
	assert_true(no_reward.grants.is_empty())


func _read(path: String) -> String:
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	assert_not_null(file)
	var contents: String = file.get_as_text()
	file.close()
	return contents
