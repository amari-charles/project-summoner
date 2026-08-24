class_name PostBattleReport
extends RefCounted

## Typed application-level projection of already committed battle results.
## It normalizes source-specific authority output before presentation sees it.

const OFFER_STATE_PENDING: String = "pending"

var outcome: StringName = &"defeat"
var grants: Array[Dictionary] = []
var pending_offer: Dictionary = {}
var destination: String = ""


static func from_authored_battle_result(
	result: Dictionary,
	destination_scene: String,
	fallback_outcome: String
) -> PostBattleReport:
	var report: PostBattleReport = PostBattleReport.new()
	report.outcome = StringName(SafeTypeUtils.string(result.get("outcome"), fallback_outcome))
	report.destination = destination_scene
	report._append_grants(result.get("progression_grants", []))
	for value: Variant in SafeTypeUtils.array(result.get("reward_offers")):
		var offer: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(offer.get("display_state")) == OFFER_STATE_PENDING \
		and report.pending_offer.is_empty():
			report.pending_offer = offer.duplicate(true)
			continue
		for option_value: Variant in SafeTypeUtils.array(offer.get("options")):
			var option: Dictionary = SafeTypeUtils.dict(option_value)
			if SafeTypeUtils.bool_val(option.get("is_selected"), false):
				report._append_grants(option.get("grants", []))
	return report


static func from_encounter_summary(
	summary: Dictionary,
	destination_scene: String,
	fallback_outcome: String
) -> PostBattleReport:
	var report: PostBattleReport = PostBattleReport.new()
	report.outcome = StringName(SafeTypeUtils.string(summary.get("outcome"), fallback_outcome))
	report.destination = destination_scene
	report._append_grants(summary.get("granted_rewards", []))
	return report


static func basic(outcome_id: String, destination_scene: String) -> PostBattleReport:
	var report: PostBattleReport = PostBattleReport.new()
	report.outcome = StringName(outcome_id)
	report.destination = destination_scene
	return report


func has_pending_offer() -> bool:
	return not pending_offer.is_empty()


func _append_grants(source: Variant) -> void:
	for value: Variant in SafeTypeUtils.array(source):
		var grant: Dictionary = SafeTypeUtils.dict(value)
		if not grant.is_empty():
			grants.append(grant.duplicate(true))
