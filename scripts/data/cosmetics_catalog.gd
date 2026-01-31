extends Node
# CosmeticsCatalog is registered as autoload, no class_name needed

## Cosmetics Catalog - Central Database of All Cosmetic Definitions
##
## Single source of truth for all cosmetic data in the game.
## Cosmetics include card backs, UI themes, and summoner skins.
##
## Usage:
##   var cosmetic = CosmeticsCatalog.get_cosmetic("card_back_gold")
##   var all_card_backs = CosmeticsCatalog.get_cosmetics_by_type("card_back")

## Cosmetic types
enum CosmeticType {
	CARD_BACK,      ## Visual back of cards in deck/hand
	UI_THEME,       ## Color scheme/styling for UI elements
	SUMMONER_SKIN,  ## Alternate visual for a summoner
}

## Cosmetic data structure
## {
##   "id": StringName,
##   "type": CosmeticType,
##   "display_name": String,
##   "description": String,
##   "preview_path": String,  ## Path to preview image (empty for placeholder)
##   "resource_path": String, ## Path to actual resource (theme, texture, etc.)
##   "summoner_id": String,   ## For SUMMONER_SKIN only, which summoner this applies to
##   "price": int,            ## Gold price in Premium Store
##   "rarity": String,        ## "common", "rare", "epic", "legendary"
## }

var _catalog: Dictionary = {}  ## Key: cosmetic_id (StringName), Value: Dictionary

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("CosmeticsCatalog: Initializing...")
	_init_catalog()
	print("CosmeticsCatalog: Loaded %d cosmetics" % _catalog.size())

## =============================================================================
## CATALOG INITIALIZATION
## =============================================================================

func _init_catalog() -> void:
	# =========================================================================
	# CARD BACKS
	# =========================================================================

	# Default card back (free, everyone has it)
	_add_cosmetic({
		"id": &"card_back_default",
		"type": CosmeticType.CARD_BACK,
		"display_name": Loc.t("cosmetic.card_back_default.name"),
		"description": Loc.t("cosmetic.card_back_default.description"),
		"preview_path": "",
		"resource_path": "",
		"summoner_id": "",
		"price": 0,
		"rarity": "common"
	})

	# =========================================================================
	# UI THEMES
	# =========================================================================

	# Default UI theme (free)
	_add_cosmetic({
		"id": &"ui_theme_default",
		"type": CosmeticType.UI_THEME,
		"display_name": Loc.t("cosmetic.ui_theme_default.name"),
		"description": Loc.t("cosmetic.ui_theme_default.description"),
		"preview_path": "",
		"resource_path": "",
		"summoner_id": "",
		"price": 0,
		"rarity": "common"
	})

func _add_cosmetic(data: Dictionary) -> void:
	_catalog[data["id"]] = data

## =============================================================================
## LOOKUP METHODS
## =============================================================================

## Get a cosmetic by ID
func get_cosmetic(cosmetic_id: String) -> Dictionary:
	var key: StringName = StringName(cosmetic_id)
	if not _catalog.has(key):
		push_warning("CosmeticsCatalog.get_cosmetic: Cosmetic not found: %s" % cosmetic_id)
		return {}
	return _catalog[key]

## Check if a cosmetic exists
func has_cosmetic(cosmetic_id: String) -> bool:
	return _catalog.has(StringName(cosmetic_id))

## Get all cosmetics
func get_all_cosmetics() -> Array[Dictionary]:
	var cosmetics: Array[Dictionary] = []
	for data: Dictionary in _catalog.values():
		cosmetics.append(data)
	return cosmetics

## Get cosmetics by type
func get_cosmetics_by_type(type: CosmeticType) -> Array[Dictionary]:
	var filtered: Array[Dictionary] = []
	for data: Dictionary in _catalog.values():
		if data["type"] == type:
			filtered.append(data)
	return filtered

## Get all card backs
func get_card_backs() -> Array[Dictionary]:
	return get_cosmetics_by_type(CosmeticType.CARD_BACK)

## Get all UI themes
func get_ui_themes() -> Array[Dictionary]:
	return get_cosmetics_by_type(CosmeticType.UI_THEME)

## Get summoner skins for a specific summoner
func get_summoner_skins(summoner_id: String) -> Array[Dictionary]:
	var skins: Array[Dictionary] = []
	for data: Dictionary in _catalog.values():
		if data["type"] == CosmeticType.SUMMONER_SKIN and data["summoner_id"] == summoner_id:
			skins.append(data)
	return skins

## Get purchasable cosmetics (price > 0)
func get_purchasable_cosmetics() -> Array[Dictionary]:
	var purchasable: Array[Dictionary] = []
	for data: Dictionary in _catalog.values():
		if data["price"] > 0:
			purchasable.append(data)
	return purchasable

## Get cosmetic display name
func get_cosmetic_name(cosmetic_id: String) -> String:
	var data: Dictionary = get_cosmetic(cosmetic_id)
	return data.get("display_name", "") if not data.is_empty() else ""

## Get cosmetic price
func get_cosmetic_price(cosmetic_id: String) -> int:
	var data: Dictionary = get_cosmetic(cosmetic_id)
	return data.get("price", 0) if not data.is_empty() else 0

## Convert CosmeticType to string for display
static func type_to_string(type: CosmeticType) -> String:
	match type:
		CosmeticType.CARD_BACK:
			return "card_back"
		CosmeticType.UI_THEME:
			return "ui_theme"
		CosmeticType.SUMMONER_SKIN:
			return "summoner_skin"
		_:
			return "unknown"

## Convert string to CosmeticType
static func string_to_type(type_str: String) -> CosmeticType:
	match type_str:
		"card_back":
			return CosmeticType.CARD_BACK
		"ui_theme":
			return CosmeticType.UI_THEME
		"summoner_skin":
			return CosmeticType.SUMMONER_SKIN
		_:
			return CosmeticType.CARD_BACK
