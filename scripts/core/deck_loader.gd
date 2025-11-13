extends Node
class_name DeckLoader

## DeckLoader - Converts profile deck data to Card resources for battle
##
## Static utility class that bridges the gap between:
## - Profile deck data (array of card_instance_ids)
## - Battle requirements (Array[Card] resources)

## Load a specific deck by ID and convert to Card resources
static func load_deck_for_battle(deck_id: String) -> Array[Card]:
	var cards: Array[Card] = []

	# Get services
	var decks: Variant = _get_service("/root/Decks")
	var collection: Variant = _get_service("/root/Collection")

	if not decks or not collection:
		push_error("DeckLoader: Required services not found!")
		return cards

	# Get deck data
	var deck_variant: Variant = {}
	if decks is Object:
		var decks_obj: Object = decks
		deck_variant = decks_obj.call("get_deck", deck_id)
	var deck: Dictionary = deck_variant if deck_variant is Dictionary else {}
	if deck.is_empty():
		push_error("DeckLoader: Deck not found: %s" % deck_id)
		return cards

	var card_instance_ids_variant: Variant = deck.get("card_instance_ids", [])
	var card_instance_ids: Array = card_instance_ids_variant if card_instance_ids_variant is Array else []

	var deck_name_variant: Variant = deck.get("name", "")
	var deck_name: String = deck_name_variant if deck_name_variant is String else ""
	print("DeckLoader: Deck '%s' has %d card instances" % [deck_name, card_instance_ids.size()])

	if card_instance_ids.is_empty():
		print("DeckLoader: Deck '%s' is empty!" % deck_name)
		return cards

	# Convert each card instance to a Card resource
	for instance_id_variant: Variant in card_instance_ids:
		var instance_id: String = instance_id_variant if instance_id_variant is String else ""
		var card: Card = _create_card_from_instance(instance_id, collection)
		if card:
			cards.append(card)
			print("DeckLoader: Loaded card: %s" % card.card_name)
		else:
			push_warning("DeckLoader: Skipping invalid card instance: %s" % instance_id)

	print("DeckLoader: Successfully loaded %d cards from deck '%s'" % [cards.size(), deck_name])
	return cards

## Load the player's currently selected deck from profile
static func load_player_deck() -> Array[Card]:
	var profile_repo: Variant = _get_service("/root/ProfileRepo")
	if not profile_repo:
		push_error("DeckLoader: ProfileRepo not found!")
		return []

	var profile_variant: Variant = {}
	if profile_repo is Object:
		var profile_repo_obj: Object = profile_repo
		profile_variant = profile_repo_obj.call("get_active_profile")
	var profile: Dictionary = profile_variant if profile_variant is Dictionary else {}
	if profile.is_empty():
		push_error("DeckLoader: No active profile!")
		return []

	# Get selected deck ID
	var empty_dict: Dictionary = {}
	var meta_variant: Variant = profile.get("meta", empty_dict)
	var meta: Dictionary = meta_variant if meta_variant is Dictionary else {}
	var deck_id_variant: Variant = meta.get("selected_deck", "")
	var deck_id: String = deck_id_variant if deck_id_variant is String else ""

	# Validate deck_id is a string (not an array or other type)
	if typeof(deck_id_variant) != TYPE_STRING:
		push_error("DeckLoader: selected_deck is not a string! Type: %s, Value: %s" % [typeof(deck_id_variant), deck_id_variant])
		deck_id = ""

	print("DeckLoader: Selected deck ID from profile: '%s'" % deck_id)

	# If no deck selected, use first available deck
	if deck_id == "" or deck_id == null:
		var decks: Variant = _get_service("/root/Decks")
		if decks:
			var deck_list_variant: Variant = []
			if decks is Object:
				var decks_obj: Object = decks
				deck_list_variant = decks_obj.call("list_decks")
			var deck_list: Array = deck_list_variant if deck_list_variant is Array else []
			if deck_list.size() > 0:
				var first_deck_variant: Variant = deck_list[0]
				var first_deck: Dictionary = first_deck_variant if first_deck_variant is Dictionary else {}
				var id_variant: Variant = first_deck.get("id", "")
				deck_id = id_variant if id_variant is String else ""
				var name_variant: Variant = first_deck.get("name", "")
				var first_deck_name: String = name_variant if name_variant is String else ""
				print("DeckLoader: No deck selected, using first deck: %s" % first_deck_name)
			else:
				push_error("DeckLoader: No decks available!")
				return []
		else:
			push_error("DeckLoader: Decks service not found!")
			return []

	print("DeckLoader: Loading deck with ID: %s" % deck_id)
	return load_deck_for_battle(deck_id)

## Create a Card resource from a card instance ID
static func _create_card_from_instance(instance_id: String, collection: Variant) -> Card:
	# Get card instance data
	var card_data_variant: Variant = {}
	if collection is Object:
		var collection_obj: Object = collection
		card_data_variant = collection_obj.call("get_card", instance_id)
	var card_data: Dictionary = card_data_variant if card_data_variant is Dictionary else {}
	if card_data.is_empty():
		return null

	var catalog_id_variant: Variant = card_data.get("catalog_id", "")
	var catalog_id: String = catalog_id_variant if catalog_id_variant is String else ""
	if catalog_id == "":
		return null

	# Load the Card resource for this catalog_id
	# Card resources are stored at res://resources/cards/[catalog_id]_card.tres
	var card_path: String = "res://resources/cards/%s_card.tres" % catalog_id
	var loaded_card: Resource = load(card_path)

	if not loaded_card or not loaded_card is Card:
		push_error("DeckLoader: Failed to load card resource: %s" % card_path)
		return null

	# Type narrow to Card for safe property access
	var card_template: Card = loaded_card

	# Duplicate to avoid mutating shared resource
	var duplicated_card: Resource = card_template.duplicate()
	if not duplicated_card is Card:
		push_error("DeckLoader: Card duplicate failed for: %s" % catalog_id)
		return null

	var card: Card = duplicated_card
	card.catalog_id = catalog_id

	return card

## Helper to get autoload service safely
static func _get_service(path: String) -> Variant:
	var main_loop: MainLoop = Engine.get_main_loop()
	if main_loop is SceneTree:
		var tree: SceneTree = main_loop
		if tree and tree.root:
			return tree.root.get_node_or_null(path)
	return null
