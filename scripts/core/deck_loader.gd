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
	var decks = _get_service("/root/Decks")
	var collection = _get_service("/root/Collection")

	if not decks or not collection:
		push_error("DeckLoader: Required services not found!")
		return cards

	# Get deck data
	var deck = decks.get_deck(deck_id)
	if deck.is_empty():
		push_error("DeckLoader: Deck not found: %s" % deck_id)
		return cards

	var card_instance_ids = deck.get("card_instance_ids", [])
	print("DeckLoader: Deck '%s' has %d card instances" % [deck.get("name", ""), card_instance_ids.size()])

	if card_instance_ids.is_empty():
		push_warning("DeckLoader: Deck '%s' is empty!" % deck.get("name", deck_id))
		return cards

	# Convert each card instance to a Card resource
	for instance_id in card_instance_ids:
		var card = _create_card_from_instance(instance_id, collection)
		if card:
			cards.append(card)
			print("DeckLoader: Loaded card: %s" % card.card_name)
		else:
			push_warning("DeckLoader: Skipping invalid card instance: %s" % instance_id)

	print("DeckLoader: Successfully loaded %d cards from deck '%s'" % [cards.size(), deck.get("name", deck_id)])
	return cards

## Load the player's currently selected deck from profile
static func load_player_deck() -> Array[Card]:
	var profile_repo = _get_service("/root/ProfileRepo")
	if not profile_repo:
		push_error("DeckLoader: ProfileRepo not found!")
		return []

	var profile = profile_repo.get_active_profile()
	if profile.is_empty():
		push_error("DeckLoader: No active profile!")
		return []

	# Get selected deck ID
	var deck_id = profile.get("meta", {}).get("selected_deck", "")

	# Validate deck_id is a string (not an array or other type)
	if typeof(deck_id) != TYPE_STRING:
		push_error("DeckLoader: selected_deck is not a string! Type: %s, Value: %s" % [typeof(deck_id), deck_id])
		deck_id = ""

	print("DeckLoader: Selected deck ID from profile: '%s'" % deck_id)

	# If no deck selected, use first available deck
	if deck_id == "" or deck_id == null:
		var decks = _get_service("/root/Decks")
		if decks:
			var deck_list = decks.list_decks()
			if deck_list.size() > 0:
				deck_id = deck_list[0].get("id", "")
				print("DeckLoader: No deck selected, using first deck: %s" % deck_list[0].get("name", ""))
			else:
				push_error("DeckLoader: No decks available!")
				return []
		else:
			push_error("DeckLoader: Decks service not found!")
			return []

	print("DeckLoader: Loading deck with ID: %s" % deck_id)
	return load_deck_for_battle(deck_id)

## Create a Card resource from a card instance ID
static func _create_card_from_instance(instance_id: String, collection) -> Card:
	# Get card instance data
	var card_data = collection.get_card(instance_id)
	if card_data.is_empty():
		return null

	var catalog_id = card_data.get("catalog_id", "")
	if catalog_id == "":
		return null

	# Load the Card resource for this catalog_id
	# Card resources are stored at res://resources/cards/[catalog_id]_card.tres
	var card_path = "res://resources/cards/%s_card.tres" % catalog_id
	var card = load(card_path) as Card

	if not card:
		push_error("DeckLoader: Failed to load card resource: %s" % card_path)
		return null

	return card

## Helper to get autoload service safely
static func _get_service(path: String):
	var tree = Engine.get_main_loop() as SceneTree
	if tree and tree.root:
		return tree.root.get_node_or_null(path)
	return null
