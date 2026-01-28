extends Node
# CardCatalog is registered as autoload, no class_name needed

## Card Catalog - Thin Wrapper Over C# CardCatalog
##
## This GDScript autoload delegates card data lookups to the C# CardCatalog
## via the CardCatalogCS bridge. The C# catalog is the single source of truth
## for all card definitions.
##
## This wrapper handles:
## - GDScript-friendly API (StringName support, typed arrays)
## - ElementTypes conversion (C# string → GDScript Element object)
## - Card resource creation (create_card_resource creates GDScript Card objects)
##
## Usage:
##   var card_def = CardCatalog.get_card("warrior")
##   var card = CardCatalog.create_card_resource("fireball")
##   var all_commons = CardCatalog.get_cards_by_rarity("common")

## Reference to C# CardCatalogBridge
var _csharp_bridge: Node = null

## Cached Card script for efficient resource creation
const CardScript = preload("res://scripts/cards/card.gd")
const CardConfigScript = preload("res://scripts/cards/card_config.gd")

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CardCatalog: Initializing...")
	_csharp_bridge = CardCatalogCS
	# Verify C# methods are accessible
	if not _csharp_bridge.has_method("GetCardCount"):
		push_warning("CardCatalog: C# bridge exists but methods not available. Card lookups will return empty.")
		_csharp_bridge = null
		return
	var count: int = _csharp_bridge.GetCardCount()
	print("CardCatalog: Connected to C# bridge with %d cards" % count)
	_validate_card_ids_sync()

## =============================================================================
## LOOKUP METHODS (delegate to C#)
## =============================================================================

## Get card definition by catalog_id
## Returns Dictionary or empty {} if not found
## Converts elemental_affinity from string to Element object
## Accepts StringName (preferred) or String for backward compatibility
func get_card(catalog_id: StringName) -> Dictionary:
	if not _csharp_bridge:
		push_error("CardCatalog: C# bridge not available")
		return {}

	var dict: Dictionary = _csharp_bridge.GetCardAsDict(str(catalog_id))
	if dict.is_empty():
		push_error("CardCatalog: Card '%s' not found in catalog" % catalog_id)
		return {}

	# Convert elemental_affinity from string to Element object
	_convert_element_types(dict)
	return dict

## Check if a card exists in the catalog
## Accepts StringName (preferred) or String for backward compatibility
func has_card(catalog_id: StringName) -> bool:
	if not _csharp_bridge:
		return false
	return _csharp_bridge.HasCard(str(catalog_id))

## Get all card IDs
func get_all_card_ids() -> Array[String]:
	if not _csharp_bridge:
		return []
	var result: Array[String] = []
	result.assign(_csharp_bridge.GetAllCardIds())
	return result

## Get all card definitions
func list_all_cards() -> Array[Dictionary]:
	if not _csharp_bridge:
		return []
	var results: Array[Dictionary] = []
	for dict: Dictionary in _csharp_bridge.GetAllCardsAsDict():
		_convert_element_types(dict)
		results.append(dict)
	return results

## Get cards filtered by rarity
func get_cards_by_rarity(rarity: String) -> Array[Dictionary]:
	if not _csharp_bridge:
		return []
	var results: Array[Dictionary] = []
	for dict: Dictionary in _csharp_bridge.GetCardsByRarityAsDict(rarity):
		_convert_element_types(dict)
		results.append(dict)
	return results

## Get cards filtered by type (Card.CardType.SUMMON or Card.CardType.SPELL)
func get_cards_by_type(card_type: int) -> Array[Dictionary]:
	if not _csharp_bridge:
		return []
	var results: Array[Dictionary] = []
	for dict: Dictionary in _csharp_bridge.GetCardsByTypeAsDict(card_type):
		_convert_element_types(dict)
		results.append(dict)
	return results

## Get starter/default cards (unlock_condition = "default")
func get_starter_cards() -> Array[Dictionary]:
	if not _csharp_bridge:
		return []
	var results: Array[Dictionary] = []
	for dict: Dictionary in _csharp_bridge.GetStarterCardsAsDict():
		_convert_element_types(dict)
		results.append(dict)
	return results

## =============================================================================
## ELEMENT TYPE CONVERSION
## =============================================================================

## Convert elemental_affinity from C# string to GDScript Element object
func _convert_element_types(dict: Dictionary) -> void:
	if dict.has("categories"):
		var cats: Dictionary = dict["categories"]
		if cats.has("elemental_affinity"):
			var affinity_str: String = str(cats["elemental_affinity"])
			cats["elemental_affinity"] = ElementTypes.from_string(affinity_str)

## =============================================================================
## RUNTIME CARD GENERATION
## =============================================================================

## Create a Card resource from a catalog definition
## This generates a runtime Card object that can be played in-game
## - For SPELL cards with C# SpellBuilder support: creates C# SpellCard with effect
## - For SUMMON cards or spells without C# support: creates GDScript Card
## Accepts StringName (preferred) or String for backward compatibility
func create_card_resource(catalog_id: StringName) -> Resource:
	var card_def: Dictionary = get_card(catalog_id)
	if card_def.is_empty():
		push_error("CardCatalog: Cannot create card resource, '%s' not found" % catalog_id)
		return null

	# Create CardConfig from catalog dictionary
	var config: Resource = CardConfigScript.from_dict(card_def)

	# Validate config was created successfully
	if not config:
		push_error("CardCatalog: Failed to create CardConfig for '%s'" % catalog_id)
		return null

	# Create GDScript Card and attach config
	var card: Card = CardScript.new()
	card.config = config

	# Try to attach C# execution delegation based on card type
	var card_type: int = card_def.get("card_type", Card.CardType.SUMMON)
	if card_type == Card.CardType.SPELL:
		_try_attach_csharp_spell_effect(catalog_id, card)
	elif card_type == Card.CardType.SUMMON:
		_try_attach_csharp_summon(catalog_id, card)
	# Card will use C# execution if available, GDScript fallback otherwise

	return card


## Check if C# spell effect is available for this spell
## Sets card._csharp_spell_id if C# effect is available
## Returns true if C# effect will be used, false to use GDScript fallback
func _try_attach_csharp_spell_effect(catalog_id: StringName, card: Card) -> bool:
	# Get CardFactory autoload
	var factory: Node = _get_card_factory()
	if not factory:
		return false

	# Check if factory has an effect for this spell
	if not factory.has_effect(catalog_id):
		return false

	# Set the C# spell ID on the card for delegation
	card._csharp_spell_id = catalog_id
	print("CardCatalog: Attached C# spell effect for '%s'" % catalog_id)
	return true


## Check if C# summon execution is available for this summon
## Sets card._csharp_summon_id if C# summon is supported
## Returns true if C# summon will be used, false to use GDScript fallback
func _try_attach_csharp_summon(catalog_id: StringName, card: Card) -> bool:
	# Get CardFactory autoload
	var factory: Node = _get_card_factory()
	if not factory:
		return false

	# Check if factory supports summon execution
	if not factory.has_summon(catalog_id):
		return false

	# Set the C# summon ID on the card for delegation
	card._csharp_summon_id = catalog_id
	print("CardCatalog: Attached C# summon for '%s'" % catalog_id)
	return true


## Get CardFactory autoload safely
## Returns null if C# is not available or factory not loaded
func _get_card_factory() -> Node:
	return CardFactory

## =============================================================================
## UTILITY METHODS
## =============================================================================

## Get card display name (for UI)
func get_card_name(catalog_id: String) -> String:
	var card: Dictionary = get_card(catalog_id)
	return card.get("card_name", catalog_id)

## Get card rarity (for UI coloring, etc.)
func get_card_rarity(catalog_id: String) -> StringName:
	var card: Dictionary = get_card(catalog_id)
	return card.get("rarity", RarityIDs.COMMON)

## Get card mana cost (for deck building validation)
func get_card_cost(catalog_id: String) -> int:
	var card: Dictionary = get_card(catalog_id)
	return card.get("mana_cost", 0)

## Print catalog summary (debug)
func print_catalog_summary() -> void:
	print("\n=== CARD CATALOG SUMMARY ===")
	var all_cards: Array[Dictionary] = list_all_cards()
	print("Total Cards: %d" % all_cards.size())

	var by_rarity: Dictionary = {}
	var by_type: Dictionary = {"summon": 0, "spell": 0}

	for card: Dictionary in all_cards:
		# Count by rarity
		var rarity: StringName = card.get("rarity", RarityIDs.COMMON)
		if not by_rarity.has(rarity):
			by_rarity[rarity] = 0
		by_rarity[rarity] += 1

		# Count by type
		var type: int = card.get("card_type", Card.CardType.SUMMON)
		if type == Card.CardType.SUMMON:
			by_type["summon"] += 1
		else:
			by_type["spell"] += 1

	print("\nBy Rarity:")
	for rarity: StringName in by_rarity:
		print("  %s: %d" % [rarity, by_rarity[rarity]])

	print("\nBy Type:")
	print("  Summon: %d" % by_type["summon"])
	print("  Spell: %d" % by_type["spell"])

	print("\nStarter Cards:")
	for card: Dictionary in get_starter_cards():
		print("  - %s (%s, %d mana)" % [card.card_name, card.rarity, card.mana_cost])

## =============================================================================
## VALIDATION
## =============================================================================

## Validate that CardIDs constants match catalog entries
## Called in _ready() to catch desync issues at startup
func _validate_card_ids_sync() -> void:
	if not _csharp_bridge:
		push_warning("CardCatalog: Cannot validate - C# bridge not available")
		return

	# Get all constant names from CardIDs
	var card_ids_script: GDScript = load("res://scripts/data/card_ids.gd")
	var constants: Dictionary = card_ids_script.get_script_constant_map()

	var missing_in_catalog: Array[String] = []
	var missing_in_card_ids: Array[String] = []

	# Check: All CardIDs constants exist in catalog
	for const_name: String in constants.keys():
		var id_value: Variant = constants[const_name]
		if id_value is StringName or id_value is String:
			var id_string: String = str(id_value)
			if not has_card(id_string):
				missing_in_catalog.append("%s = '%s'" % [const_name, id_string])

	# Check: All catalog cards have corresponding CardIDs constant
	for catalog_id: String in get_all_card_ids():
		var found: bool = false
		for const_value: Variant in constants.values():
			if str(const_value) == catalog_id:
				found = true
				break
		if not found:
			missing_in_card_ids.append(catalog_id)

	# Report issues
	if missing_in_catalog.size() > 0:
		push_error("CardCatalog: CardIDs constants reference non-existent cards:")
		for missing: String in missing_in_catalog:
			push_error("  - CardIDs.%s" % missing)

	if missing_in_card_ids.size() > 0:
		push_warning("CardCatalog: Catalog has cards without CardIDs constants (test/mod cards?):")
		for missing: String in missing_in_card_ids:
			push_warning("  - '%s' (no constant in CardIDs)" % missing)
		print("  This is OK for test cards, but official cards should have CardIDs constants.")

	if missing_in_catalog.size() == 0 and missing_in_card_ids.size() == 0:
		print("CardCatalog: ✓ CardIDs validation passed - all %d constants match catalog" % constants.size())

	print("===========================\n")
