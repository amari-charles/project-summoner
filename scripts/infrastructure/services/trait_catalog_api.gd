class_name TraitCatalogApi
extends RefCounted

static func get_trait(trait_id: String) -> Dictionary:
	return SafeTypeUtils.dict(TraitCatalog.call("GetTrait", trait_id))

static func get_trait_name(trait_id: String) -> String:
	return SafeTypeUtils.string(TraitCatalog.call("GetTraitName", trait_id), "")

static func get_trait_description(trait_id: String) -> String:
	return SafeTypeUtils.string(TraitCatalog.call("GetTraitDescription", trait_id), "")

static func get_all_traits() -> Array:
	return SafeTypeUtils.array(TraitCatalog.call("GetAllTraits"))

static func get_traits_by_acquisition_mode(acquisition_mode: String) -> Array:
	return SafeTypeUtils.array(TraitCatalog.call("GetTraitsByAcquisitionMode", acquisition_mode))
