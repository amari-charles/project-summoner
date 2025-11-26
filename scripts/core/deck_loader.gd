extends Node
class_name DeckLoader

## DeckLoader - Converts profile deck data to Card resources and hero for battle
##
## Static utility class that bridges the gap between:
## - Profile deck data (array of card_instance_ids + hero_id)
## - Battle requirements (Array[Card] resources + HeroInstance)

## Load a specific deck by ID and convert to Card resources with HeroInstance
## Returns: Dictionary with "cards", "hero_id", and "hero_instance"
static func load_deck_for_battle(deck_id: String) -> Dictionary:
	var result: Dictionary = {
		"cards": [],
		"hero_id": "hero_fire",  # Default fallback
		"hero_instance": null
	}
	var cards: Array[Card] = []

	# Get services
	var decks: Variant = _get_service("/root/Decks")
	var collection: Variant = _get_service("/root/Collection")
	var hero_catalog: Variant = _get_service("/root/HeroCatalog")

	if not decks or not collection:
		push_error("DeckLoader: Required services not found!")
		result["cards"] = cards
		return result

	# Get deck data
	var deck_variant: Variant = {}
	if decks is Object:
		var decks_obj: Object = decks
		deck_variant = decks_obj.call("get_deck", deck_id)
	var deck: Dictionary = deck_variant if deck_variant is Dictionary else {}
	if deck.is_empty():
		push_warning("DeckLoader: Deck not found: %s" % deck_id)
		result["cards"] = cards
		return result

	var card_instance_ids_variant: Variant = deck.get("card_instance_ids", [])
	var card_instance_ids: Array = card_instance_ids_variant if card_instance_ids_variant is Array else []

	var deck_name_variant: Variant = deck.get("name", "")
	var deck_name: String = deck_name_variant if deck_name_variant is String else ""

	if card_instance_ids.is_empty():
		push_warning("DeckLoader: Deck '%s' has no cards" % deck_name)

	# Convert each card instance to a Card resource
	for instance_id_variant: Variant in card_instance_ids:
		var instance_id: String = instance_id_variant if instance_id_variant is String else ""
		var card: Card = _create_card_from_instance(instance_id, collection)
		if card:
			cards.append(card)
		else:
			push_warning("DeckLoader: Skipping invalid card instance: %s" % instance_id)

	# Load hero instance
	var hero_id_variant: Variant = deck.get("hero_id", "")
	var hero_id: String = hero_id_variant if hero_id_variant is String else ""

	# Fallback to default hero if hero_id is empty
	if hero_id.is_empty():
		hero_id = "hero_fire"
		push_warning("DeckLoader: Deck has no hero_id, using fallback: %s" % hero_id)

	result["hero_id"] = hero_id

	# Try to load existing HeroInstance from ProfileRepo
	var profile_repo: Variant = _get_service("/root/ProfileRepo")
	var hero_instance: HeroInstance = null

	if profile_repo and profile_repo is Object:
		var profile_repo_obj: Object = profile_repo
		var instance_data_variant: Variant = profile_repo_obj.call("get_hero_instance", hero_id)
		var instance_data: Dictionary = instance_data_variant if instance_data_variant is Dictionary else {}

		if not instance_data.is_empty():
			# Load from saved instance
			hero_instance = HeroInstance.from_dict(instance_data)
		else:
			# Create new instance from config
			if hero_catalog and hero_catalog is Object:
				var hero_catalog_obj: Object = hero_catalog
				var hero_config_variant: Variant = hero_catalog_obj.call("get_hero_config", hero_id)
				if hero_config_variant is HeroConfig:
					var hero_config: HeroConfig = hero_config_variant
					hero_instance = HeroInstance.new()
					hero_instance.init_from_config(hero_config)
				else:
					push_warning("DeckLoader: Hero config not found '%s', using fallback" % hero_id)
			else:
				push_warning("DeckLoader: HeroCatalog not available")

	# Fallback to default hero if loading failed
	if hero_instance == null:
		push_warning("DeckLoader: Failed to load hero '%s', creating fallback" % hero_id)
		result["hero_id"] = "hero_fire"
		if hero_catalog and hero_catalog is Object:
			var hero_catalog_obj: Object = hero_catalog
			var fallback_config_variant: Variant = hero_catalog_obj.call("get_hero_config", "hero_fire")
			if fallback_config_variant is HeroConfig:
				var fallback_config: HeroConfig = fallback_config_variant
				hero_instance = HeroInstance.new()
				hero_instance.init_from_config(fallback_config)

	result["hero_instance"] = hero_instance
	result["cards"] = cards
	return result

## Load the player's currently selected deck from profile
## Returns: Dictionary with "cards", "hero_id", and "hero_instance"
static func load_player_deck() -> Dictionary:
	var empty_result: Dictionary = {
		"cards": [],
		"hero_id": "hero_fire",
		"hero_instance": null
	}

	var profile_repo: Variant = _get_service("/root/ProfileRepo")
	if not profile_repo:
		push_error("DeckLoader: ProfileRepo not found!")
		return empty_result

	var profile_variant: Variant = {}
	if profile_repo is Object:
		var profile_repo_obj: Object = profile_repo
		profile_variant = profile_repo_obj.call("get_active_profile")
	var profile: Dictionary = profile_variant if profile_variant is Dictionary else {}
	if profile.is_empty():
		push_error("DeckLoader: No active profile!")
		return empty_result

	# Get selected deck ID
	var empty_dict: Dictionary = {}
	var meta_variant: Variant = profile.get("meta", empty_dict)
	var meta: Dictionary = meta_variant if meta_variant is Dictionary else {}
	var deck_id_variant: Variant = meta.get("selected_deck", "")
	var deck_id: String = deck_id_variant if deck_id_variant is String else ""

	# Validate deck_id is a string (not an array or other type)
	if typeof(deck_id_variant) != TYPE_STRING:
		push_warning("DeckLoader: selected_deck is not a string, type: %s" % typeof(deck_id_variant))
		deck_id = ""

	# If no deck selected, use first available deck
	if deck_id == "":
		var decks: Variant = _get_service("/root/Decks")
		if not decks:
			push_error("DeckLoader: Decks service not found!")
			return empty_result
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
			else:
				push_error("DeckLoader: No decks available!")
				return empty_result
		else:
			push_error("DeckLoader: Decks service not found!")
			return empty_result

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
	card.instance_id = instance_id  # Track for XP rewards

	return card

## Helper to get autoload service safely
static func _get_service(path: String) -> Variant:
	var main_loop: MainLoop = Engine.get_main_loop()
	if main_loop is SceneTree:
		var tree: SceneTree = main_loop
		if tree and tree.root:
			return tree.root.get_node_or_null(path)
	return null
