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

static func get_generic_quest_journal_state() -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetGenericQuestJournalState"))

static func get_npc_quest_state(npc_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetNpcQuestState", npc_id))

static func accept_quest(quest_id: String) -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("AcceptQuest", quest_id), false)

static func record_quest_world_interaction(target_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("RecordQuestWorldInteraction", target_id))

static func record_quest_npc_interaction(npc_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("RecordQuestNpcInteraction", npc_id))

static func record_quest_encounter_completed(encounter_id: String, outcome: String) -> Dictionary:
	return SafeTypeUtils.dict(
		Campaign.call("RecordQuestEncounterCompleted", encounter_id, outcome)
	)

static func get_encounter_preparation_state(encounter_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetEncounterPreparationState", encounter_id))

static func resolve_encounter_battle_config(encounter_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("ResolveEncounterBattleConfig", encounter_id))

static func update_encounter_loadout(encounter_id: String, slots: Array[Dictionary]) -> bool:
	return SafeTypeUtils.bool_val(
		Campaign.call("UpdateEncounterLoadout", encounter_id, slots),
		false
	)

static func fill_encounter_loadout_from_deck(
	encounter_id: String,
	source_deck_id: String
) -> Dictionary:
	return SafeTypeUtils.dict(
		Campaign.call("FillEncounterLoadoutFromDeck", encounter_id, source_deck_id)
	)

static func save_encounter_loadout_to_deck(
	encounter_id: String,
	target_deck_id: String,
	new_deck_name: String
) -> Dictionary:
	return SafeTypeUtils.dict(
		Campaign.call(
			"SaveEncounterLoadoutToDeck",
			encounter_id,
			target_deck_id,
			new_deck_name
		)
	)

static func complete_encounter(encounter_id: String, outcome: int = 0) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("CompleteEncounter", encounter_id, outcome))

static func consume_encounter_completion_summary(encounter_id: String) -> Dictionary:
	return SafeTypeUtils.dict(
		Campaign.call("ConsumeEncounterCompletionSummary", encounter_id)
	)

static func get_encounter_completion_summary(encounter_id: String) -> Dictionary:
	return SafeTypeUtils.dict(
		Campaign.call("GetEncounterCompletionSummary", encounter_id)
	)

static func get_professor_quest_states() -> Array:
	return SafeTypeUtils.array(Campaign.call("GetProfessorQuestStates"))

static func get_professor_quest_state(professor_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Campaign.call("GetProfessorQuestState", professor_id))

static func track_quest(quest_id: String) -> bool:
	return SafeTypeUtils.bool_val(Campaign.call("TrackQuest", quest_id), false)
