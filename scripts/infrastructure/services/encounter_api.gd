class_name EncounterApi
extends RefCounted

static func get_preparation_state(encounter_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Encounters.call("GetPreparationState", encounter_id))

static func resolve_battle_config(encounter_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Encounters.call("ResolveBattleConfig", encounter_id))

static func update_loadout(encounter_id: String, slots: Array[Dictionary]) -> bool:
	return SafeTypeUtils.bool_val(Encounters.call("UpdateLoadout", encounter_id, slots), false)

static func fill_loadout_from_deck(encounter_id: String, source_deck_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Encounters.call("FillLoadoutFromDeck", encounter_id, source_deck_id))

static func save_loadout_to_deck(
	encounter_id: String,
	target_deck_id: String,
	new_deck_name: String
) -> Dictionary:
	return SafeTypeUtils.dict(
		Encounters.call("SaveLoadoutToDeck", encounter_id, target_deck_id, new_deck_name)
	)

static func consume_completion_summary(encounter_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Encounters.call("ConsumeCompletionSummary", encounter_id))

static func get_completion_summary(encounter_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Encounters.call("GetCompletionSummary", encounter_id))
