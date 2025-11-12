extends RefCounted
class_name HeroModifierProvider

## HeroModifierProvider - Provides modifiers based on selected hero
##
## Each hero grants affinity bonuses to units matching their element.

var hero_id: String

func _init(id: String):
	hero_id = id

## Get all modifiers this hero provides
func get_modifiers() -> Array:
	var modifiers: Array = []

	match hero_id:
		"fire_hero":
			modifiers.append({
				"source": "fire_hero",
				"tags": ["sun_blessed"],
				"conditions": {
					"elemental_affinity": ElementTypes.FIRE
				},
				"stat_mults": {
					"attack_damage": 1.1  # +10% attack to fire units
				}
			})

		"earth_hero":
			modifiers.append({
				"source": "earth_hero",
				"tags": ["stone_guardian"],
				"conditions": {
					"elemental_affinity": ElementTypes.EARTH
				},
				"stat_mults": {
					"attack_damage": 1.1  # +10% attack to earth units
				}
			})

		"wind_hero":
			modifiers.append({
				"source": "wind_hero",
				"tags": ["wind_walker"],
				"conditions": {
					"elemental_affinity": ElementTypes.WIND
				},
				"stat_mults": {
					"attack_damage": 1.1  # +10% attack to wind units
				}
			})

		"water_hero":
			modifiers.append({
				"source": "water_hero",
				"tags": ["tide_caller"],
				"conditions": {
					"elemental_affinity": ElementTypes.WATER
				},
				"stat_mults": {
					"attack_damage": 1.1  # +10% attack to water units
				}
			})

		"random_hero":
			# Random hero gets no bonuses (for now)
			pass

		_:
			push_warning("HeroModifierProvider: Unknown hero_id '%s'" % hero_id)

	return modifiers
