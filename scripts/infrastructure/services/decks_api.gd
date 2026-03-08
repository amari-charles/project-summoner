class_name DecksApi
extends RefCounted

static func create_deck_from_dict(deck_name: String, card_ids: Array, summoner_id: String) -> String:
	return SafeTypeUtils.string(Decks.call("CreateDeckFromDict", deck_name, card_ids, summoner_id), "")

static func list_decks_dict() -> Array:
	return SafeTypeUtils.array(Decks.call("ListDecksDict"))

static func get_deck_dict(deck_id: String) -> Dictionary:
	return SafeTypeUtils.dict(Decks.call("GetDeckDict", deck_id))

static func add_card_to_deck(deck_id: String, card_instance_id: String) -> bool:
	return SafeTypeUtils.bool_val(Decks.call("AddCardToDeck", deck_id, card_instance_id), false)

static func validate_deck(deck_id: String) -> bool:
	return SafeTypeUtils.bool_val(Decks.call("ValidateDeck", deck_id), false)

static func update_deck_from_dict(deck_id: String, deck_name: String, card_ids: Array, summoner_id: String) -> bool:
	return SafeTypeUtils.bool_val(Decks.call("UpdateDeckFromDict", deck_id, deck_name, card_ids, summoner_id), false)

static func set_active_deck(deck_id: String) -> bool:
	return SafeTypeUtils.bool_val(Decks.call("SetActiveDeck", deck_id), false)

static func remove_card_from_deck(deck_id: String, card_instance_id: String) -> bool:
	return SafeTypeUtils.bool_val(Decks.call("RemoveCardFromDeck", deck_id, card_instance_id), false)

static func list_decks_for_summoner_dict(summoner_id: String) -> Array:
	return SafeTypeUtils.array(Decks.call("ListDecksForSummonerDict", summoner_id))

static func get_validation_errors_array(deck_id: String) -> Array:
	return SafeTypeUtils.array(Decks.call("GetValidationErrorsArray", deck_id))

static func get_active_deck_id() -> String:
	return SafeTypeUtils.string(Decks.call("GetActiveDeckId"), "")

static func delete_deck(deck_id: String) -> bool:
	return SafeTypeUtils.bool_val(Decks.call("DeleteDeck", deck_id), false)
