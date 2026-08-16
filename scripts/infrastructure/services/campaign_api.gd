class_name CampaignApi
extends RefCounted

static func is_battle_completed(event_id: String) -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("IsBattleCompleted", event_id), false)

static func get_battle(event_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetBattle", event_id))

static func is_battle_unlocked(event_id: String) -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("IsBattleUnlocked", event_id), false)

static func get_current_campaign_id() -> String:
	return SafeTypeUtils.string(Campaign.call("GetCurrentCampaignId"), "")

static func set_current_campaign(campaign_id: String) -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("SetCurrentCampaign", campaign_id), false)

static func get_campaign(campaign_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetCampaign", campaign_id))

static func complete_battle(event_id: String) -> void:
	Campaign.call("CompleteBattle", event_id)

static func start_battle(event_id: String) -> void:
	Campaign.call("StartBattle", event_id)

static func record_choice(event_id: String, option_id: String) -> void:
	Campaign.call("RecordChoice", event_id, option_id)

static func is_battle_tutorial(event_id: String) -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("IsBattleTutorial", event_id), false)

static func get_choice(node_id: String) -> String:
	return SafeTypeUtils.string(Campaign.call("GetChoice", node_id), "")

static func get_all_campaigns() -> Array:
	return SafeTypeUtils.array(Campaign.call("GetAllCampaigns"))

static func get_academy_progress() -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetAcademyProgress"))

static func get_quest_journal_state() -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetQuestJournalState"))

static func get_available_academy_courses() -> Array:
	return SafeTypeUtils.array(Campaign.call("GetAvailableAcademyCourses"))

static func get_academy_courses_for_semester(year: int, semester: int) -> Array:
	return SafeTypeUtils.array(Campaign.call("GetAcademyCoursesForSemester", year, semester))

static func get_academy_course(course_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetAcademyCourse", course_id))

static func get_academy_course_flow_state(course_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetAcademyCourseFlowState", course_id))

static func get_academy_activity_preparation_state(course_id: String, activity_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetAcademyActivityPreparationState", course_id, activity_id))

static func update_academy_activity_loadout(course_id: String, activity_id: String, slots: Array[Dictionary]) -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("UpdateAcademyActivityLoadout", course_id, activity_id, slots), false)

static func fill_academy_activity_loadout_from_deck(course_id: String, activity_id: String, source_deck_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("FillAcademyActivityLoadoutFromDeck", course_id, activity_id, source_deck_id))

static func save_academy_activity_loadout_to_deck(course_id: String, activity_id: String, target_deck_id: String, new_deck_name: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("SaveAcademyActivityLoadoutToDeck", course_id, activity_id, target_deck_id, new_deck_name))

static func get_academy_activity_launch_state(course_id: String, activity_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetAcademyActivityLaunchState", course_id, activity_id))

static func resolve_academy_activity_battle_config(course_id: String, activity_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("ResolveAcademyActivityBattleConfig", course_id, activity_id))

static func get_last_academy_completion_summary() -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetLastAcademyCompletionSummary"))

static func consume_last_academy_completion_summary() -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("ConsumeLastAcademyCompletionSummary"))

static func enroll_academy_course(course_id: String) -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("EnrollAcademyCourse", course_id), false)

static func complete_academy_course(course_id: String, grade: String = "pass", honors: bool = false) -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("CompleteAcademyCourse", course_id, grade, honors), false)

enum AcademyActivityOutcome {
	VICTORY,
	DEFEAT,
	ABANDONED,
}

static func complete_academy_activity(
	course_id: String,
	activity_id: String,
	outcome: AcademyActivityOutcome = AcademyActivityOutcome.VICTORY
) -> bool:
	return SafeTypeUtils.bool_val(
		Campaign.call("CompleteAcademyActivity", course_id, activity_id, int(outcome)),
		false
	)

static func claim_academy_reward(claim_id: String, selected_option_ids: Array[String]) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("ClaimAcademyReward", claim_id, selected_option_ids))

static func advance_academy_semester() -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("AdvanceAcademySemester"), false)
