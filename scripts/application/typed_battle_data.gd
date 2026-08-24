class_name TypedBattleData
extends RefCounted

## Typed accessor wrapper for authored battle dictionaries.
##
## The underlying dictionary comes from EventCatalog.ToDictionary() and has
## guaranteed battle structure based on BattleEventDefinition.

## The underlying dictionary from EventCatalog.ToDictionary()
var _data: Dictionary = {}

## The authored battle ID.
var id: String = ""

# =============================================================================
# CORE PROPERTIES
# =============================================================================

## Battle display name (localized)
## C# outputs name_key (localization key), we translate it here
var name: String:
	get:
		var key: String = _str("name_key", "")
		return Loc.t(key) if not key.is_empty() else Loc.t("ui.common.unknown")

## Battle description (localized)
## C# outputs description_key (localization key), we translate it here
var description: String:
	get:
		var key: String = _str("description_key", "")
		return Loc.t(key) if not key.is_empty() else ""

## Authored battle type ID (battle, elite, or boss).
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

## Summoner XP reward amount
var summoner_xp_reward: int:
	get: return _int("summoner_xp_reward", 0)

## Card XP reward amount
var card_xp_reward: int:
	get: return _int("card_xp_reward", 0)

## Universal first-clear offers authored for this battle.
var first_clear_reward_offers: Array:
	get: return _array("first_clear_reward_offers")

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


## Create from an authored battle ID.
static func from_id(battle_id: String) -> TypedBattleData:
	var data: Dictionary = SafeTypeUtils.dict(ProgressionAuthority.GetBattle(battle_id))
	return TypedBattleData.new(data, battle_id)

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


## All authored entries represented by this wrapper are combat battles.
func is_combat() -> bool:
	return event_type in [EventTypeIDs.BATTLE, EventTypeIDs.ELITE, EventTypeIDs.BOSS]


## Check if this is a battle event
func is_battle() -> bool:
	return event_type == EventTypeIDs.BATTLE
