extends Resource
class_name UnitData

## Pure data definition for a unit archetype
## Loaded from JSON files, contains no logic
## Used by ContentCatalog to create runtime Unit3D instances

@export var unit_id: String = ""
@export var unit_name: String = ""
@export var description: String = ""

## Combat Stats
@export_group("Combat Stats")
@export var max_hp: float = 100.0
@export var attack_damage: float = 10.0
@export var attack_range: float = 2.0
@export var attack_speed: float = 1.0  ## Attacks per second
@export var move_speed: float = 3.0
@export var aggro_radius: float = 20.0

## Behavior
@export_group("Behavior")
@export var is_ranged: bool = false
@export var projectile_id: String = ""  ## Reference to ProjectileData

## Targeting
@export_group("Targeting")
@export var distance_weight: float = 1.0  ## Weight for distance in target scoring (higher = prefer closer targets)
@export var hp_weight: float = 0.3  ## Weight for HP in target scoring (higher = prefer low HP targets)
@export var target_lock_duration: float = 0.5  ## Duration in seconds to keep current target before re-evaluating

## Visuals
@export_group("Visuals")
@export var sprite_frames_path: String = ""
@export var animation_config_path: String = ""
@export var scale: float = 1.0
@export var tint_color: Color = Color.WHITE

## Audio
@export_group("Audio")
@export var attack_sound: String = ""
@export var hurt_sound: String = ""
@export var death_sound: String = ""

## Metadata
@export_group("Metadata")
@export var tags: Array[String] = []

## Create from dictionary (for JSON loading)
static func from_dict(data: Dictionary) -> UnitData:
	var unit_instance: Resource = UnitData.new()
	var unit: UnitData = unit_instance

	var default_empty_string: String = ""
	unit.unit_id = data.get("unit_id", default_empty_string)
	unit.unit_name = data.get("unit_name", default_empty_string)
	unit.description = data.get("description", default_empty_string)

	var default_max_hp: float = 100.0
	unit.max_hp = data.get("max_hp", default_max_hp)
	var default_attack_damage: float = 10.0
	unit.attack_damage = data.get("attack_damage", default_attack_damage)
	var default_attack_range: float = 2.0
	unit.attack_range = data.get("attack_range", default_attack_range)
	var default_attack_speed: float = 1.0
	unit.attack_speed = data.get("attack_speed", default_attack_speed)
	var default_move_speed: float = 3.0
	unit.move_speed = data.get("move_speed", default_move_speed)
	var default_aggro_radius: float = 20.0
	unit.aggro_radius = data.get("aggro_radius", default_aggro_radius)

	var default_is_ranged: bool = false
	unit.is_ranged = data.get("is_ranged", default_is_ranged)
	unit.projectile_id = data.get("projectile_id", default_empty_string)

	var default_distance_weight: float = 1.0
	unit.distance_weight = data.get("distance_weight", default_distance_weight)
	var default_hp_weight: float = 0.3
	unit.hp_weight = data.get("hp_weight", default_hp_weight)
	var default_target_lock_duration: float = 0.5
	unit.target_lock_duration = data.get("target_lock_duration", default_target_lock_duration)

	unit.sprite_frames_path = data.get("sprite_frames_path", default_empty_string)
	unit.animation_config_path = data.get("animation_config_path", default_empty_string)
	var default_scale: float = 1.0
	unit.scale = data.get("scale", default_scale)

	var default_tint_color: String = "#ffffff"
	var color_str: String = data.get("tint_color", default_tint_color)
	unit.tint_color = Color(color_str) if color_str is String else Color.WHITE

	unit.attack_sound = data.get("attack_sound", default_empty_string)
	unit.hurt_sound = data.get("hurt_sound", default_empty_string)
	unit.death_sound = data.get("death_sound", default_empty_string)

	if data.has("tags"):
		var tags_variant: Variant = data.get("tags")
		if tags_variant is Array:
			var tags_array: Array = tags_variant
			var unit_tags: Array[String] = unit.tags
			unit_tags.clear()
			for tag: Variant in tags_array:
				if tag is String:
					var tag_str: String = tag
					unit_tags.append(tag_str)

	return unit

## Convert to dictionary (for saving/debugging)
func to_dict() -> Dictionary:
	var result: Dictionary = {
		"unit_id": unit_id,
		"unit_name": unit_name,
		"description": description,
		"max_hp": max_hp,
		"attack_damage": attack_damage,
		"attack_range": attack_range,
		"attack_speed": attack_speed,
		"move_speed": move_speed,
		"aggro_radius": aggro_radius,
		"is_ranged": is_ranged,
		"projectile_id": projectile_id,
		"distance_weight": distance_weight,
		"hp_weight": hp_weight,
		"target_lock_duration": target_lock_duration,
		"sprite_frames_path": sprite_frames_path,
		"animation_config_path": animation_config_path,
		"scale": scale,
		"tint_color": "#" + tint_color.to_html(false),
		"attack_sound": attack_sound,
		"hurt_sound": hurt_sound,
		"death_sound": death_sound,
		"tags": tags.duplicate()
	}
	return result
