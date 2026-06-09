class_name TypedEventData
extends RefCounted

## TypedEventData - Typed accessor wrapper for event data dictionaries
##
## Provides typed property access for event data from C# EventCatalog.
## The underlying dictionary comes from EventCatalog.ToDictionary() and has
## guaranteed structure based on the EventDefinition type.
##
## Usage:
##   var event = TypedEventData.new(event_dict, event_id)
##   var name = event.name
##   var difficulty = event.difficulty
##   if event.requires_deck:
##       _load_decks()

## The underlying dictionary from EventCatalog.ToDictionary()
var _data: Dictionary = {}

## The event ID
var id: String = ""

# =============================================================================
# CORE PROPERTIES (all event types)
# =============================================================================

## Event display name (localized)
## C# outputs name_key (localization key), we translate it here
var name: String:
	get:
		var key: String = _str("name_key", "")
		return Loc.t(key) if not key.is_empty() else Loc.t("ui.common.unknown")

## Event description (localized)
## C# outputs description_key (localization key), we translate it here
var description: String:
	get:
		var key: String = _str("description_key", "")
		return Loc.t(key) if not key.is_empty() else ""

## Event type ID (battle, caravan, choice, etc.)
var event_type: StringName:
	get:
		var type_val: Variant = _data.get("event_type", EventTypeIDs.BATTLE)
		if type_val is StringName:
			return type_val
		var type_str: String = SafeTypeUtils.string(type_val, String(EventTypeIDs.BATTLE))
		return StringName(type_str)

## Whether this event can be repeated after completion
var repeatable: bool:
	get: return _bool("repeatable", false)

# =============================================================================
# BATTLE PROPERTIES
# =============================================================================

## Difficulty rating (1-5 stars, 0 for non-battle)
var difficulty: int:
	get: return _int("difficulty", 0)

## Whether a deck must be selected to start this battle
var requires_deck: bool:
	get: return _bool("requires_deck", true)

## Biome ID for visual theming
var biome_id: String:
	get: return _str("biome_id", String(BiomeIDs.DEFAULT))

## Whether this is a tutorial battle
var is_tutorial: bool:
	get: return _bool("is_tutorial", false)

## Enemy HP for battle display
var enemy_hp: float:
	get:
		var enemy_side: Variant = _data.get("enemy_side", {})
		if not enemy_side is Dictionary:
			return 0.0
		var summoner: Variant = enemy_side.get("summoner", {})
		if not summoner is Dictionary:
			return 0.0
		return SafeTypeUtils.float_val(summoner.get("hp", 0.0), 0.0)

# =============================================================================
# REWARD PROPERTIES
# =============================================================================

## Reward type (fixed, flexible, none)
var reward_type: StringName:
	get:
		var type_val: Variant = _data.get("reward_type", RewardTypeIDs.FIXED)
		if type_val is StringName:
			return type_val
		var type_str: String = SafeTypeUtils.string(type_val, String(RewardTypeIDs.FIXED))
		return StringName(type_str)

## Gold reward amount
var gold_reward: int:
	get: return _int("gold_reward", 0)

## Summoner XP reward amount
var summoner_xp_reward: int:
	get: return _int("summoner_xp_reward", 0)

## Card XP reward amount
var card_xp_reward: int:
	get: return _int("card_xp_reward", 0)

## Fixed reward cards (for FIXED reward type)
var reward_cards: Array:
	get: return _array("reward_cards")

## Player selects from reward options (for FLEXIBLE reward type)
var player_selects: bool:
	get: return _bool("player_selects", true)

# =============================================================================
# CHOICE PROPERTIES
# =============================================================================

## Choice options (for choice events)
var options: Array:
	get: return _array("options")

# =============================================================================
# CARAVAN PROPERTIES
# =============================================================================

## Shop ID for caravan events
var shop_id: String:
	get: return _str("shop_id", "")

# =============================================================================
# ELITE/BOSS PROPERTIES
# =============================================================================

## Level cap for elite battles
var level_cap: int:
	get: return _int("level_cap", 0)

## Whether this event has a level cap
var has_level_cap: bool:
	get: return _data.has("level_cap") and _int("level_cap", 0) > 0

# =============================================================================
# CONSTRUCTOR
# =============================================================================

func _init(data: Dictionary = {}, event_id: String = "") -> void:
	_data = data
	id = event_id


## Create from event ID using Campaign service
static func from_id(event_id: String) -> TypedEventData:
	var data: Dictionary = CampaignApi.get_battle(event_id)
	return TypedEventData.new(data, event_id)

# =============================================================================
# RAW ACCESS
# =============================================================================

## Get the underlying dictionary (for cases where raw access is needed)
func get_raw() -> Dictionary:
	return _data


## Check if the event data is empty/invalid
func is_empty() -> bool:
	return _data.is_empty()


## Check if a key exists in the raw data
func has_key(key: String) -> bool:
	return _data.has(key)


## Get raw value with optional default
func get_value(key: String, default: Variant = null) -> Variant:
	return _data.get(key, default)

# =============================================================================
# TYPE-SAFE ACCESSORS (private)
# =============================================================================

func _str(key: String, default: String = "") -> String:
	var value: Variant = _data.get(key, default)
	return SafeTypeUtils.string(value, default)


func _int(key: String, default: int = 0) -> int:
	var value: Variant = _data.get(key, default)
	return value if value is int else default


func _float(key: String, default: float = 0.0) -> float:
	var value: Variant = _data.get(key, default)
	if value is float:
		var float_value: float = value
		return float_value
	if value is int:
		var int_value: int = value
		return float(int_value)
	return default


func _bool(key: String, default: bool = false) -> bool:
	var value: Variant = _data.get(key, default)
	return value if value is bool else default


func _array(key: String) -> Array:
	var value: Variant = _data.get(key, [])
	return value if value is Array else []


func _dict(key: String) -> Dictionary:
	var value: Variant = _data.get(key, {})
	return value if value is Dictionary else {}

# =============================================================================
# TYPE CHECKING
# =============================================================================

## Check if this is a combat event (battle, elite, boss)
func is_combat() -> bool:
	return event_type in [EventTypeIDs.BATTLE, EventTypeIDs.ELITE, EventTypeIDs.BOSS]


## Check if this is a battle event
func is_battle() -> bool:
	return event_type == EventTypeIDs.BATTLE


## Check if this is a caravan/shop event
func is_caravan() -> bool:
	return event_type == EventTypeIDs.CARAVAN


## Check if this is a choice event
func is_choice() -> bool:
	return event_type == EventTypeIDs.CHOICE
