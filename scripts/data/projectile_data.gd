extends Resource
class_name ProjectileData

## Data definition for projectile behavior
## Loaded from JSON files, defines how projectiles move and look

@export var projectile_id: String = ""
@export var projectile_name: String = ""

## Visuals
@export_group("Visuals")
@export var model_scene_path: String = ""  ## Path to 3D model or sprite scene
@export var visual_scene: PackedScene = null  ## Direct scene reference (loaded at runtime)
@export var trail_effect_id: String = ""  ## VFX trail behind projectile
@export var impact_effect_id: String = ""  ## VFX on hit
@export var hit_vfx: String = ""  ## Alias for impact_effect_id
@export var trail_vfx: String = ""  ## Alias for trail_effect_id

## Behavior
@export_group("Behavior")
@export var movement_type: String = "straight"  ## "straight", "homing", "arc", "ballistic"
@export var speed: float = 15.0
@export var acceleration: float = 0.0  ## For accelerating projectiles
@export var lifetime: float = 5.0  ## Max time before despawn
@export var rotate_to_direction: bool = true

## Arc/Ballistic Specific
@export_group("Arc Properties")
@export var arc_height: float = 2.0
@export var gravity: float = -9.8

## Homing Specific
@export_group("Homing Properties")
@export var homing_strength: float = 5.0  ## Turn rate
@export var homing_delay: float = 0.0  ## Time before homing starts

## Impact
@export_group("Impact")
@export var pierce_count: int = 0  ## How many targets can it hit?
@export var aoe_radius: float = 0.0  ## AOE damage on impact

## Audio
@export_group("Audio")
@export var launch_sound: String = ""
@export var impact_sound: String = ""

## Create from dictionary (for JSON loading)
static func from_dict(data: Dictionary) -> ProjectileData:
	var proj: ProjectileData = ProjectileData.new()

	proj.projectile_id = data.get("projectile_id", "")
	proj.projectile_name = data.get("projectile_name", "")

	proj.model_scene_path = data.get("model_scene_path", "")
	proj.trail_effect_id = data.get("trail_effect_id", "")
	proj.impact_effect_id = data.get("impact_effect_id", "")
	proj.hit_vfx = data.get("hit_vfx", data.get("impact_effect_id", ""))
	proj.trail_vfx = data.get("trail_vfx", data.get("trail_effect_id", ""))

	# Load visual scene if path provided (force reload to bypass cache)
	var model_path_variant: Variant = proj.model_scene_path
	if model_path_variant is String:
		var model_path: String = model_path_variant
		if not model_path.is_empty():
			proj.visual_scene = ResourceLoader.load(model_path, "", ResourceLoader.CACHE_MODE_IGNORE)

	proj.movement_type = data.get("movement_type", "straight")
	proj.speed = data.get("speed", 15.0)
	proj.acceleration = data.get("acceleration", 0.0)
	proj.lifetime = data.get("lifetime", 5.0)
	proj.rotate_to_direction = data.get("rotate_to_direction", true)

	proj.arc_height = data.get("arc_height", 2.0)
	proj.gravity = data.get("gravity", -9.8)

	proj.homing_strength = data.get("homing_strength", 5.0)
	proj.homing_delay = data.get("homing_delay", 0.0)

	proj.pierce_count = data.get("pierce_count", 0)
	proj.aoe_radius = data.get("aoe_radius", 0.0)

	proj.launch_sound = data.get("launch_sound", "")
	proj.impact_sound = data.get("impact_sound", "")

	return proj

## Convert to dictionary (for saving/debugging)
func to_dict() -> Dictionary:
	return {
		"projectile_id": projectile_id,
		"projectile_name": projectile_name,
		"model_scene_path": model_scene_path,
		"trail_effect_id": trail_effect_id,
		"impact_effect_id": impact_effect_id,
		"movement_type": movement_type,
		"speed": speed,
		"acceleration": acceleration,
		"lifetime": lifetime,
		"rotate_to_direction": rotate_to_direction,
		"arc_height": arc_height,
		"gravity": gravity,
		"homing_strength": homing_strength,
		"homing_delay": homing_delay,
		"pierce_count": pierce_count,
		"aoe_radius": aoe_radius,
		"launch_sound": launch_sound,
		"impact_sound": impact_sound
	}
