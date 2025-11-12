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

	card.card_id = data.get("card_id", "")
	card.card_name = data.get("card_name", "")
	card.description = data.get("description", "")
	card.rarity = data.get("rarity", "common")

	card.card_type = data.get("card_type", "summon")
	card.mana_cost = data.get("mana_cost", 3)
	card.cooldown = data.get("cooldown", 2.0)

	card.unit_id = data.get("unit_id", "")
	card.spawn_count = data.get("spawn_count", 1)

	card.spell_type = data.get("spell_type", "")
	card.spell_damage = data.get("spell_damage", 0.0)
	card.spell_radius = data.get("spell_radius", 0.0)
	card.spell_duration = data.get("spell_duration", 0.0)
	card.spell_effect_id = data.get("spell_effect_id", "")
	card.projectile_id = data.get("projectile_id", "")

	card.card_icon_path = data.get("card_icon_path", "")

	card.unlock_condition = data.get("unlock_condition", "default")
	card.flavor_text = data.get("flavor_text", "")

	return card

## Convert to dictionary (for saving/debugging)
func to_dict() -> Dictionary:
	return {
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
