extends Node
# EmotesCatalog is registered as autoload, no class_name needed

## Emotes Catalog - Central Database of All Emote Definitions
##
## Single source of truth for all emote data in the game.
## Emotes are battle reactions that players can trigger during combat.
## Players equip up to 4 emotes in their emote slots.
##
## Usage:
##   var emote = EmotesCatalog.get_emote("emote_laugh")
##   var all_emotes = EmotesCatalog.get_all_emotes()

## Maximum number of emotes a player can equip at once
const MAX_EQUIPPED_EMOTES: int = 4

## Emote data structure
## {
##   "id": StringName,
##   "display_name": String,
##   "description": String,
##   "icon_path": String,       ## Path to emote icon (empty for placeholder)
##   "animation_path": String,  ## Path to emote animation/VFX (empty for placeholder)
##   "sound_path": String,      ## Path to emote sound effect (empty for placeholder)
##   "price": int,              ## Gold price in Premium Store (0 = starter/free)
##   "rarity": String,          ## "common", "rare", "epic", "legendary"
##   "category": String,        ## "reaction", "taunt", "celebration", "neutral"
## }

var _catalog: Dictionary = {}  ## Key: emote_id (StringName), Value: Dictionary

## Starter emotes that all players have by default
var STARTER_EMOTES: Array[StringName] = [
	&"emote_hello",
	&"emote_good_game",
]

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("EmotesCatalog: Initializing...")
	_init_catalog()
	print("EmotesCatalog: Loaded %d emotes" % _catalog.size())

## =============================================================================
## CATALOG INITIALIZATION
## =============================================================================

func _init_catalog() -> void:
	# =========================================================================
	# STARTER EMOTES (Free, everyone has them)
	# =========================================================================

	_add_emote({
		"id": &"emote_hello",
		"display_name": Loc.t("emote.emote_hello.name"),
		"description": Loc.t("emote.emote_hello.description"),
		"icon_path": "",
		"animation_path": "",
		"sound_path": "",
		"price": 0,
		"rarity": "common",
		"category": "neutral"
	})

	_add_emote({
		"id": &"emote_good_game",
		"display_name": Loc.t("emote.emote_good_game.name"),
		"description": Loc.t("emote.emote_good_game.description"),
		"icon_path": "",
		"animation_path": "",
		"sound_path": "",
		"price": 0,
		"rarity": "common",
		"category": "celebration"
	})

	# =========================================================================
	# REACTION EMOTES (Purchasable)
	# =========================================================================

	_add_emote({
		"id": &"emote_laugh",
		"display_name": Loc.t("emote.emote_laugh.name"),
		"description": Loc.t("emote.emote_laugh.description"),
		"icon_path": "",
		"animation_path": "",
		"sound_path": "",
		"price": 150,
		"rarity": "common",
		"category": "reaction"
	})

	_add_emote({
		"id": &"emote_shocked",
		"display_name": Loc.t("emote.emote_shocked.name"),
		"description": Loc.t("emote.emote_shocked.description"),
		"icon_path": "",
		"animation_path": "",
		"sound_path": "",
		"price": 150,
		"rarity": "common",
		"category": "reaction"
	})

	_add_emote({
		"id": &"emote_thinking",
		"display_name": Loc.t("emote.emote_thinking.name"),
		"description": Loc.t("emote.emote_thinking.description"),
		"icon_path": "",
		"animation_path": "",
		"sound_path": "",
		"price": 200,
		"rarity": "rare",
		"category": "neutral"
	})

	# =========================================================================
	# TAUNT EMOTES (Purchasable)
	# =========================================================================

	_add_emote({
		"id": &"emote_taunt",
		"display_name": Loc.t("emote.emote_taunt.name"),
		"description": Loc.t("emote.emote_taunt.description"),
		"icon_path": "",
		"animation_path": "",
		"sound_path": "",
		"price": 250,
		"rarity": "rare",
		"category": "taunt"
	})

	_add_emote({
		"id": &"emote_confident",
		"display_name": Loc.t("emote.emote_confident.name"),
		"description": Loc.t("emote.emote_confident.description"),
		"icon_path": "",
		"animation_path": "",
		"sound_path": "",
		"price": 300,
		"rarity": "epic",
		"category": "taunt"
	})

	# =========================================================================
	# CELEBRATION EMOTES (Purchasable)
	# =========================================================================

	_add_emote({
		"id": &"emote_victory",
		"display_name": Loc.t("emote.emote_victory.name"),
		"description": Loc.t("emote.emote_victory.description"),
		"icon_path": "",
		"animation_path": "",
		"sound_path": "",
		"price": 350,
		"rarity": "epic",
		"category": "celebration"
	})

func _add_emote(data: Dictionary) -> void:
	_catalog[data["id"]] = data

## =============================================================================
## LOOKUP METHODS
## =============================================================================

## Get an emote by ID
func get_emote(emote_id: String) -> Dictionary:
	var key: StringName = StringName(emote_id)
	if not _catalog.has(key):
		push_warning("EmotesCatalog.get_emote: Emote not found: %s" % emote_id)
		return {}
	return _catalog[key]

## Check if an emote exists
func has_emote(emote_id: String) -> bool:
	return _catalog.has(StringName(emote_id))

## Get all emotes
func get_all_emotes() -> Array[Dictionary]:
	var emotes: Array[Dictionary] = []
	for data: Dictionary in _catalog.values():
		emotes.append(data)
	return emotes

## Get emotes by category
func get_emotes_by_category(category: String) -> Array[Dictionary]:
	var filtered: Array[Dictionary] = []
	for data: Dictionary in _catalog.values():
		if data["category"] == category:
			filtered.append(data)
	return filtered

## Get starter emotes (free emotes all players have)
func get_starter_emotes() -> Array[Dictionary]:
	var starters: Array[Dictionary] = []
	for emote_id: StringName in STARTER_EMOTES:
		if _catalog.has(emote_id):
			starters.append(_catalog[emote_id])
	return starters

## Get purchasable emotes (price > 0)
func get_purchasable_emotes() -> Array[Dictionary]:
	var purchasable: Array[Dictionary] = []
	for data: Dictionary in _catalog.values():
		if data["price"] > 0:
			purchasable.append(data)
	return purchasable

## Get emote display name
func get_emote_name(emote_id: String) -> String:
	var data: Dictionary = get_emote(emote_id)
	return data.get("display_name", "") if not data.is_empty() else ""

## Get emote price
func get_emote_price(emote_id: String) -> int:
	var data: Dictionary = get_emote(emote_id)
	return data.get("price", 0) if not data.is_empty() else 0

## Check if an emote is a starter emote
func is_starter_emote(emote_id: String) -> bool:
	return StringName(emote_id) in STARTER_EMOTES
