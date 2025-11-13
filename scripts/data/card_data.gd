extends Resource
class_name CardData

## Pure data definition for a card
## Loaded from JSON files, contains no logic
## References UnitData for summon cards

@export var card_id: String = ""
@export var card_name: String = ""
@export var description: String = ""
@export var rarity: String = "common"  ## "common", "rare", "epic", "legendary"

## Card Properties
@export_group("Card Properties")
@export var card_type: String = "summon"  ## "summon", "spell", "structure"
@export var mana_cost: int = 3
@export var cooldown: float = 2.0

## Summon-Specific
@export_group("Summon Properties")
@export var unit_id: String = ""  ## References UnitData
@export var spawn_count: int = 1

## Spell-Specific
@export_group("Spell Properties")
@export var spell_type: String = ""  ## "damage", "heal", "buff", "debuff"
@export var spell_damage: float = 0.0
@export var spell_radius: float = 0.0
@export var spell_duration: float = 0.0
@export var spell_effect_id: String = ""  ## References VFXDefinition
@export var projectile_id: String = ""  ## If set, spell spawns a projectile instead of instant cast

## Visual
@export_group("Visual")
@export var card_icon_path: String = ""

## Metadata
@export_group("Metadata")
@export var unlock_condition: String = "default"
@export var flavor_text: String = ""

## Create from dictionary (for JSON loading)
static func from_dict(data: Dictionary) -> CardData:
	var card: CardData = CardData.new()

	var default_empty_string: String = ""
	card.card_id = data.get("card_id", default_empty_string)
	card.card_name = data.get("card_name", default_empty_string)
	card.description = data.get("description", default_empty_string)
	var default_rarity: String = "common"
	card.rarity = data.get("rarity", default_rarity)

	var default_card_type: String = "summon"
	card.card_type = data.get("card_type", default_card_type)
	var default_mana_cost: int = 3
	card.mana_cost = data.get("mana_cost", default_mana_cost)
	var default_cooldown: float = 2.0
	card.cooldown = data.get("cooldown", default_cooldown)

	card.unit_id = data.get("unit_id", default_empty_string)
	var default_spawn_count: int = 1
	card.spawn_count = data.get("spawn_count", default_spawn_count)

	card.spell_type = data.get("spell_type", default_empty_string)
	var default_zero_float: float = 0.0
	card.spell_damage = data.get("spell_damage", default_zero_float)
	card.spell_radius = data.get("spell_radius", default_zero_float)
	card.spell_duration = data.get("spell_duration", default_zero_float)
	card.spell_effect_id = data.get("spell_effect_id", default_empty_string)
	card.projectile_id = data.get("projectile_id", default_empty_string)

	card.card_icon_path = data.get("card_icon_path", default_empty_string)

	var default_unlock: String = "default"
	card.unlock_condition = data.get("unlock_condition", default_unlock)
	card.flavor_text = data.get("flavor_text", default_empty_string)

	return card

## Convert to dictionary (for saving/debugging)
func to_dict() -> Dictionary:
	var result: Dictionary = {
		"card_id": card_id,
		"card_name": card_name,
		"description": description,
		"rarity": rarity,
		"card_type": card_type,
		"mana_cost": mana_cost,
		"cooldown": cooldown,
		"unit_id": unit_id,
		"spawn_count": spawn_count,
		"spell_type": spell_type,
		"spell_damage": spell_damage,
		"spell_radius": spell_radius,
		"spell_duration": spell_duration,
		"spell_effect_id": spell_effect_id,
		"projectile_id": projectile_id,
		"card_icon_path": card_icon_path,
		"unlock_condition": unlock_condition,
		"flavor_text": flavor_text
	}
	return result
