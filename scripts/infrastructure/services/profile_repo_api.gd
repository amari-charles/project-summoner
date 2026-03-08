class_name ProfileRepoApi
extends RefCounted

static func reset_profile() -> void:
	ProfileRepo.call("reset_profile")

static func get_current_profile_id() -> String:
	return SafeTypeUtils.string(ProfileRepo.call("get_current_profile_id"), "")

static func load_profile(profile_id: String) -> bool:
	return SafeTypeUtils.bool_val(ProfileRepo.call("load_profile", profile_id), false)

static func snapshot() -> Dictionary:
	return SafeTypeUtils.dict(ProfileRepo.call("snapshot"))

static func get_profile_data() -> Dictionary:
	return SafeTypeUtils.dict(ProfileRepo.call("get_profile_data"))

static func load_profile_data(profile_data: Dictionary) -> bool:
	if not ProfileRepo.has_method("load_profile_data"):
		return false
	ProfileRepo.call("load_profile_data", profile_data)
	return true

static func get_resources_dict() -> Dictionary:
	return SafeTypeUtils.dict(ProfileRepo.call("GetResourcesDict"))

static func update_profile_meta_dict(meta_updates: Dictionary) -> void:
	ProfileRepo.call("UpdateProfileMetaDict", meta_updates)

static func get_active_profile_dict() -> Dictionary:
	return SafeTypeUtils.dict(ProfileRepo.call("GetActiveProfileDict"))

static func get_active_deck_array() -> Array:
	return SafeTypeUtils.array(ProfileRepo.call("GetActiveDeckArray"))

static func update_settings_dict(settings_updates: Dictionary) -> void:
	ProfileRepo.call("UpdateSettingsDict", settings_updates)

static func update_campaign_progress_dict(progress_updates: Dictionary, summoner_id: String) -> void:
	ProfileRepo.call("UpdateCampaignProgressDict", progress_updates, summoner_id)

static func get_settings_dict() -> Dictionary:
	return SafeTypeUtils.dict(ProfileRepo.call("GetSettingsDict"))
