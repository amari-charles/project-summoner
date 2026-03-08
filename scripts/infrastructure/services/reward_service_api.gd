class_name RewardServiceApi
extends RefCounted

static func get_battle_reward_spec_as_dict(battle_id: String, is_completed: bool, pending_choice_index: int) -> Dictionary:
	return SafeTypeUtils.dict(RewardService.call("GetBattleRewardSpecAsDict", battle_id, is_completed, pending_choice_index))
