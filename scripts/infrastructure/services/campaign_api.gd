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

static func get_pending_reward() -> Variant:
	return Campaign.call("GetPendingReward")

static func get_campaign(campaign_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetCampaign", campaign_id))

static func complete_battle(event_id: String) -> void:
	Campaign.call("CompleteBattle", event_id)

static func clear_pending_reward() -> void:
	Campaign.call("ClearPendingReward")

static func update_pending_choice(index: int, chosen_catalog_id: String = "") -> void:
	Campaign.call("UpdatePendingChoice", index, chosen_catalog_id)

static func start_battle(event_id: String) -> void:
	Campaign.call("StartBattle", event_id)

static func set_pending_reward(event_id: String, reward_type: StringName, choice_index: int) -> void:
	Campaign.call("SetPendingReward", event_id, reward_type, choice_index)

static func record_choice(event_id: String, option_id: String) -> void:
	Campaign.call("RecordChoice", event_id, option_id)

static func is_battle_tutorial(event_id: String) -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("IsBattleTutorial", event_id), false)

static func get_choice(node_id: String) -> String:
	return SafeTypeUtils.string(Campaign.call("GetChoice", node_id), "")

static func get_all_campaigns() -> Array:
	return SafeTypeUtils.array(Campaign.call("GetAllCampaigns"))

static func claim_pending_reward() -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("ClaimPendingReward"))

static func get_academy_progress() -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetAcademyProgress"))

static func get_available_academy_courses() -> Array:
	return SafeTypeUtils.array(Campaign.call("GetAvailableAcademyCourses"))

static func enroll_academy_course(course_id: String) -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("EnrollAcademyCourse", course_id), false)

static func complete_academy_course(course_id: String, grade: String = "pass", honors: bool = false) -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("CompleteAcademyCourse", course_id, grade, honors), false)

static func complete_next_academy_activity(course_id: String) -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("CompleteNextAcademyActivity", course_id), false)

static func advance_academy_semester() -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("AdvanceAcademySemester"), false)
