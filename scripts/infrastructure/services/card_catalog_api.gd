class_name CardCatalogApi
extends RefCounted

static func get_card_as_dict(catalog_id: String) -> Dictionary:
	return SafeTypeUtils.dict(CardCatalog.call("GetCardAsDict", catalog_id))

static func create_card(catalog_id: String) -> Variant:
	return CardCatalog.call("CreateCard", catalog_id)

static func get_all_cards_as_dict() -> Array:
	return SafeTypeUtils.array(CardCatalog.call("GetAllCardsAsDict"))
