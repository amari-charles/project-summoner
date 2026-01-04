extends Resource
class_name CardConfig

## Pure data configuration for cards
## Separates card data from card execution logic (Card class)
## Similar pattern to TargetingConfig for units

## =============================================================================
## IDENTITY
## =============================================================================

@export var catalog_id: String = ""
@export var card_name: String = "Unknown Card"
@export var card_type: int = 0  # Card.CardType enum value
@export var description: String = ""

## =============================================================================
## GAMEPLAY
## =============================================================================

@export var mana_cost: int = 1
@export var cooldown: float = 2.0  ## Seconds before another card can be played
@export var summon_time: float = 1.0  ## Seconds to cast (player locked during cast)

## =============================================================================
## SUMMON-SPECIFIC
## =============================================================================

@export var unit_scene: PackedScene = null
@export var spawn_count: int = 1

## =============================================================================
## FORMATION (for multi-unit spawns)
## =============================================================================

@export var formation_spacing: float = 1.8  ## Distance between units
@export var formation_row_offset: float = 0.5  ## Stagger for alternating rows (0-1)

## =============================================================================
## SPELL-SPECIFIC
## =============================================================================

@export var spell_damage: float = 0.0
@export var spell_radius: float = 0.0
@export var spell_duration: float = 0.0
@export var projectile_id: String = ""
@export var spell_vfx: String = ""

## =============================================================================
## VISUAL
## =============================================================================

@export var card_icon: Texture2D = null

## =============================================================================
## UNIT STATS (applied to spawned units)
## =============================================================================

@export var unit_stats: Dictionary = {}


## Create a CardConfig from a catalog dictionary
static func from_dict(data: Dictionary) -> Resource:
	var config: Resource = (load("res://scripts/cards/card_config.gd") as GDScript).new()

	# Identity
	config.catalog_id = data.get("catalog_id", "")
	config.card_name = data.get("card_name", "Unknown Card")
	config.card_type = data.get("card_type", 0)
	config.description = data.get("description", "")

	# Gameplay
	config.mana_cost = data.get("mana_cost", 1)
	config.cooldown = data.get("cooldown", 2.0)
	config.summon_time = data.get("summon_time", 1.0)

	# Summon-specific
	var scene_path: String = data.get("unit_scene_path", "")
	if not scene_path.is_empty():
		config.unit_scene = load(scene_path) as PackedScene
	config.spawn_count = data.get("spawn_count", 1)

	# Formation
	config.formation_spacing = data.get("formation_spacing", 1.8)
	config.formation_row_offset = data.get("formation_row_offset", 0.5)

	# Spell-specific
	config.spell_damage = data.get("spell_damage", 0.0)
	config.spell_radius = data.get("spell_radius", 0.0)
	config.spell_duration = data.get("spell_duration", 0.0)
	config.projectile_id = data.get("projectile_id", "")
	config.spell_vfx = data.get("spell_vfx", "")

	# Visual
	var icon_path: String = data.get("card_icon_path", "")
	if not icon_path.is_empty():
		config.card_icon = load(icon_path) as Texture2D

	# Unit stats - collect all stat fields
	config.unit_stats = {
		"max_hp": data.get("max_hp", 0.0),
		"attack_damage": data.get("attack_damage", 0.0),
		"attack_range": data.get("attack_range", 0.0),
		"attack_speed": data.get("attack_speed", 0.0),
		"move_speed": data.get("move_speed", 0.0),
		"collision_radius": data.get("collision_radius", 0.5),
	}

	return config
