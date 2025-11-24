extends Resource
class_name ModifierEffect

## ModifierEffect - Defines a single effect that a modifier (trait/boon) applies
##
## Can modify hero stats, unit stats by tag, or unlock cards.
## Used by ModifierConfig to define what a trait/boon does.

enum EffectType {
	HERO_STAT,       ## Modifies hero stat (health, mana, mana_regen)
	UNIT_TAG_STAT,   ## Modifies units with specific tag (e.g., all FIRE units)
	UNLOCK_CARD      ## Unlocks a card for use
}

enum StatMode {
	ADD,             ## Additive bonus (e.g., +100 HP)
	MULTIPLY         ## Multiplicative bonus (e.g., +10% = 0.1)
}

## Effect configuration
@export var type: EffectType = EffectType.HERO_STAT
@export var stat: String = ""           ## "health", "mana", "mana_regen", "attack_damage", etc.
@export var mode: StatMode = StatMode.ADD
@export var value: float = 0.0

## For UNIT_TAG_STAT effects
@export var tag: String = ""            ## Unit tag to affect (e.g., "FIRE", "MELEE")

## For UNLOCK_CARD effects
@export var card_id: String = ""        ## Card catalog_id to unlock
