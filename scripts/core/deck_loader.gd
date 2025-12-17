extends Node
class_name DeckLoader

## DeckLoader - Converts profile deck data to Card resources and summoner for battle
##
## Static utility class that bridges the gap between:
## - Profile deck data (array of card_instance_ids + summoner_id)
## - Battle requirements (Array[Card] resources + SummonerInstance)

## Load a specific deck by ID and convert to Card resources with SummonerInstance
## Returns: Dictionary with "cards", "summoner_id", and "summoner_instance"
static func load_deck_for_battle(deck_id: String) -> Dictionary:
	var result: Dictionary = {
		"cards": [],
		"summoner_id": SummonerIDs.DEFAULT,  # Default fallback
		"summoner_instance": null
	}
	var cards: Array[Card] = []

	# Get services
	var decks: Variant = _get_service("/root/Decks")
	var collection: Variant = _get_service("/root/Collection")
	var summoner_catalog: Variant = _get_service("/root/SummonerCatalog")

	if not decks or not collection:
		push_error("DeckLoader: Required services not found!")
		result["cards"] = cards
		return result

	# Get deck data
	print("DeckLoader: Loading deck '%s'" % deck_id)
	var deck_variant: Variant = {}
	if decks is Object:
		var decks_obj: Object = decks
		deck_variant = decks_obj.call("get_deck", deck_id)
	var deck: Dictionary = deck_variant if deck_variant is Dictionary else {}
	if deck.is_empty():
		push_error("DeckLoader: Deck not found: %s - CANNOT load summoner without deck!" % deck_id)
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

	# Load summoner instance
	var summoner_id_variant: Variant = deck.get("summoner_id", "")
	var summoner_id: String = summoner_id_variant if summoner_id_variant is String else ""

	# Fallback to default summoner if summoner_id is empty
	if summoner_id.is_empty():
		summoner_id = SummonerIDs.DEFAULT
		push_warning("DeckLoader: Deck has no summoner_id, using fallback: %s" % summoner_id)

	result["summoner_id"] = summoner_id

	# Try to load existing SummonerInstance from ProfileRepo
	var profile_repo: Variant = _get_service("/root/ProfileRepo")
	var summoner_instance: SummonerInstance = null

	print("DeckLoader: Loading summoner instance for '%s'" % summoner_id)

	if profile_repo and profile_repo is Object:
		var profile_repo_obj: Object = profile_repo
		var instance_data_variant: Variant = profile_repo_obj.call("get_summoner_instance", summoner_id)
		var instance_data: Dictionary = instance_data_variant if instance_data_variant is Dictionary else {}

		print("DeckLoader: ProfileRepo returned instance_data empty=%s" % instance_data.is_empty())

		if not instance_data.is_empty():
			# Load from saved instance
			summoner_instance = SummonerInstance.from_dict(instance_data)
			print("DeckLoader: from_dict returned %s" % ("instance" if summoner_instance else "NULL"))
		else:
			# Create new instance from config
			print("DeckLoader: No saved instance, creating from catalog (catalog=%s)" % ("available" if summoner_catalog else "NULL"))
			if summoner_catalog and summoner_catalog is Object:
				var summoner_catalog_obj: Object = summoner_catalog
				var summoner_config_variant: Variant = summoner_catalog_obj.call("get_summoner_config", summoner_id)
				print("DeckLoader: get_summoner_config returned type=%s" % typeof(summoner_config_variant))
				if summoner_config_variant is SummonerConfig:
					var summoner_config: SummonerConfig = summoner_config_variant
					summoner_instance = SummonerInstance.new()
					summoner_instance.init_from_config(summoner_config)
					print("DeckLoader: Created new instance from config")
				else:
					push_warning("DeckLoader: Summoner config not found '%s', using fallback" % summoner_id)
			else:
				push_warning("DeckLoader: SummonerCatalog not available")

	# Fallback to default summoner if loading failed
	if summoner_instance == null:
		push_warning("DeckLoader: Failed to load summoner '%s', creating fallback" % summoner_id)
		result["summoner_id"] = SummonerIDs.DEFAULT
		if summoner_catalog and summoner_catalog is Object:
			var summoner_catalog_obj: Object = summoner_catalog
			var fallback_config_variant: Variant = summoner_catalog_obj.call("get_summoner_config", SummonerIDs.DEFAULT)
			if fallback_config_variant is SummonerConfig:
				var fallback_config: SummonerConfig = fallback_config_variant
				summoner_instance = SummonerInstance.new()
				summoner_instance.init_from_config(fallback_config)

	result["summoner_instance"] = summoner_instance
	result["cards"] = cards
	return result

## Load the player's currently selected deck from profile
## Returns: Dictionary with "cards", "summoner_id", and "summoner_instance"
static func load_player_deck() -> Dictionary:
	var empty_result: Dictionary = {
		"cards": [],
		"summoner_id": SummonerIDs.DEFAULT,
		"summoner_instance": null
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
		print("DeckLoader: No deck selected in profile, finding first available...")
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
			print("DeckLoader: Found %d decks" % deck_list.size())
			if deck_list.size() > 0:
				var first_deck_variant: Variant = deck_list[0]
				var first_deck: Dictionary = first_deck_variant if first_deck_variant is Dictionary else {}
				var id_variant: Variant = first_deck.get("id", "")
				deck_id = id_variant if id_variant is String else ""
				print("DeckLoader: Using first deck: '%s'" % deck_id)
			else:
				# No decks exist - still need to return a summoner instance
				push_warning("DeckLoader: No decks available - creating default summoner")
				return _create_default_summoner_result()
		else:
			push_error("DeckLoader: Decks service not found!")
			return empty_result
	else:
		print("DeckLoader: Profile has selected deck: '%s'" % deck_id)

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

## Create a result with empty cards but a valid default summoner instance
## Used when player has no decks but we still need summoner stats
static func _create_default_summoner_result() -> Dictionary:
	var result: Dictionary = {
		"cards": [],
		"summoner_id": SummonerIDs.DEFAULT,
		"summoner_instance": null
	}

	# Create default summoner instance from catalog
	var summoner_catalog: Variant = _get_service("/root/SummonerCatalog")
	if summoner_catalog and summoner_catalog is Object:
		var summoner_catalog_obj: Object = summoner_catalog
		var summoner_config_variant: Variant = summoner_catalog_obj.call("get_summoner_config", SummonerIDs.DEFAULT)
		if summoner_config_variant is SummonerConfig:
			var summoner_config: SummonerConfig = summoner_config_variant
			var summoner_instance: SummonerInstance = SummonerInstance.new()
			summoner_instance.init_from_config(summoner_config)
			result["summoner_instance"] = summoner_instance
			print("DeckLoader: Created default summoner instance '%s'" % summoner_config.summoner_name)
		else:
			push_error("DeckLoader: Could not load default summoner config!")
	else:
		push_error("DeckLoader: SummonerCatalog not available for default summoner!")

	return result
