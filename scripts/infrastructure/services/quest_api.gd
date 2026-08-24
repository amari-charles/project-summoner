class_name QuestApi
extends RefCounted

static func get_journal_state() -> Dictionary:
	return SafeTypeUtils.dict(Quests.call("GetJournalState"))

static func get_npc_quest_state(npc_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Quests.call("GetNpcQuestState", npc_id))

static func accept_quest(quest_id: String) -> bool:
	return SafeTypeUtils.bool_val(Quests.call("AcceptQuest", quest_id), false)

static func record_world_interaction(target_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Quests.call("RecordWorldInteraction", target_id))

static func record_npc_interaction(npc_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Quests.call("RecordNpcInteraction", npc_id))

static func get_professor_quest_states() -> Array:
	return SafeTypeUtils.array(Quests.call("GetProfessorQuestStates"))

static func get_professor_quest_state(professor_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Quests.call("GetProfessorQuestState", professor_id))

static func track_quest(quest_id: String) -> bool:
	return SafeTypeUtils.bool_val(Quests.call("TrackQuest", quest_id), false)
