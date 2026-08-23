class_name ProfileRepoApi
extends RefCounted

static func _repo() -> Node:
	return ProfileRepo

static func _call_first(method_names: Array[String], args: Array = []) -> Variant:
	var repo: Node = _repo()
	for method_name: String in method_names:
		if repo.has_method(method_name):
			return repo.callv(method_name, args)
	push_error("ProfileRepoApi: None of these methods exist on ProfileRepo: %s" % str(method_names))
	return null

static func reset_profile() -> void:
	_call_first(["ResetProfile", "reset_profile"])

static func get_current_profile_id() -> String:
	return SafeTypeUtils.string(_call_first(["GetCurrentProfileId", "get_current_profile_id"]), "")

static func load_profile(profile_id: String) -> bool:
	return SafeTypeUtils.bool_val(_call_first(["LoadProfile", "load_profile"], [profile_id]), false)

static func snapshot() -> Dictionary:
	return get_profile_data()

static func get_profile_data() -> Dictionary:
	return SafeTypeUtils.dict(_call_first([
		"GetProfileDataSnapshot",
		"get_profile_data_snapshot",
		"GetActiveProfileDict",
		"get_active_profile_dict"
	]))

static func load_profile_data(profile_data: Dictionary) -> bool:
	var repo: Node = _repo()
	if not repo.has_method("LoadProfileData") and not repo.has_method("load_profile_data"):
		return false
	_call_first(["LoadProfileData", "load_profile_data"], [profile_data])
	return true

static func get_resources_dict() -> Dictionary:
	return SafeTypeUtils.dict(_call_first(["GetResourcesDict", "get_resources_dict"]))

static func update_profile_meta_dict(meta_updates: Dictionary) -> void:
	_call_first(["UpdateProfileMetaDict", "update_profile_meta_dict"], [meta_updates])

static func get_active_profile_dict() -> Dictionary:
	return SafeTypeUtils.dict(_call_first(["GetActiveProfileDict", "get_active_profile_dict"]))

static func get_active_deck_array() -> Array:
	return SafeTypeUtils.array(_call_first(["GetActiveDeckArray", "get_active_deck_array"]))

static func get_deck_array(deck_id: String) -> Array:
	return SafeTypeUtils.array(_call_first(["GetDeckArray", "get_deck_array"], [deck_id]))

static func update_settings_dict(settings_updates: Dictionary) -> void:
	_call_first(["UpdateSettingsDict", "update_settings_dict"], [settings_updates])

static func update_campaign_progress_dict(progress_updates: Dictionary, summoner_id: String) -> void:
	_call_first(["UpdateCampaignProgressDict", "update_campaign_progress_dict"], [progress_updates, summoner_id])

static func get_settings_dict() -> Dictionary:
	return SafeTypeUtils.dict(_call_first(["GetSettingsDict", "get_settings_dict"]))
