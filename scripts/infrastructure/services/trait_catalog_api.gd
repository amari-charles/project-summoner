class_name TraitCatalogApi
extends RefCounted

static func get_trait_name(trait_id: String) -> String:
	return SafeTypeUtils.string(TraitCatalog.call("GetTraitName", trait_id), "")

static func get_trait_description(trait_id: String) -> String:
	return SafeTypeUtils.string(TraitCatalog.call("GetTraitDescription", trait_id), "")
