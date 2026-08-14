extends Node3D
class_name PlaceholderCampusScenery

## Visual-only Tiny Swords scenery used to establish the campus composition.
## The center stays open for navigation; dense clusters frame the island edges.

const TREE_CONIFER_DARK: Texture2D = preload("res://assets/placeholders/tiny_swords/scenery/placeholder_tree_conifer_dark.png")
const TREE_CONIFER_BRIGHT: Texture2D = preload("res://assets/placeholders/tiny_swords/scenery/placeholder_tree_conifer_bright.png")
const TREE_DECIDUOUS_GREEN: Texture2D = preload("res://assets/placeholders/tiny_swords/scenery/placeholder_tree_deciduous_green.png")
const TREE_DECIDUOUS_GOLD: Texture2D = preload("res://assets/placeholders/tiny_swords/scenery/placeholder_tree_deciduous_gold.png")
const BUSH_ROUND_LIGHT: Texture2D = preload("res://assets/placeholders/tiny_swords/scenery/placeholder_bush_round_light.png")
const BUSH_ROUND_DARK: Texture2D = preload("res://assets/placeholders/tiny_swords/scenery/placeholder_bush_round_dark.png")
const BUSH_REED_LIGHT: Texture2D = preload("res://assets/placeholders/tiny_swords/scenery/placeholder_bush_reed_light.png")
const BUSH_REED_DARK: Texture2D = preload("res://assets/placeholders/tiny_swords/scenery/placeholder_bush_reed_dark.png")
const WATER_ROCKS_01: Texture2D = preload("res://assets/placeholders/tiny_swords/scenery/placeholder_water_rocks_01.png")
const WATER_ROCKS_02: Texture2D = preload("res://assets/placeholders/tiny_swords/scenery/placeholder_water_rocks_02.png")
const WATER_ROCKS_03: Texture2D = preload("res://assets/placeholders/tiny_swords/scenery/placeholder_water_rocks_03.png")
const WATER_ROCKS_04: Texture2D = preload("res://assets/placeholders/tiny_swords/scenery/placeholder_water_rocks_04.png")
const RUBBER_DUCK: Texture2D = preload("res://assets/placeholders/tiny_swords/scenery/placeholder_rubber_duck.png")
const CUTOUT_RENDER_ORDER: Script = preload("res://scripts/meta/components/academy_cutout_render_order.gd")

const TREE_PIXEL_SIZE: float = 0.028
const BUSH_PIXEL_SIZE: float = 0.026
const WATER_ROCK_PIXEL_SIZE: float = 0.04
const DUCK_PIXEL_SIZE: float = 0.05
const WATER_FEET_Y: float = -2.22

const LAND_PLACEMENTS: Array[Dictionary] = [
	{"name": "NorthwestConifer", "texture": TREE_CONIFER_DARK, "frames": 8, "fps": 2.5, "position": Vector3(-29.0, 0.0, -17.0), "pixel_size": TREE_PIXEL_SIZE},
	{"name": "NorthwestPine", "texture": TREE_CONIFER_BRIGHT, "frames": 8, "fps": 2.8, "position": Vector3(-23.0, 0.0, -19.0), "pixel_size": TREE_PIXEL_SIZE},
	{"name": "NortheastBroadleaf", "texture": TREE_DECIDUOUS_GREEN, "frames": 8, "fps": 2.4, "position": Vector3(23.0, 0.0, -19.0), "pixel_size": TREE_PIXEL_SIZE},
	{"name": "NortheastGold", "texture": TREE_DECIDUOUS_GOLD, "frames": 8, "fps": 2.7, "position": Vector3(29.0, 0.0, -16.5), "pixel_size": TREE_PIXEL_SIZE},
	{"name": "WestConifer", "texture": TREE_CONIFER_DARK, "frames": 8, "fps": 2.6, "position": Vector3(-31.0, 0.0, -5.0), "pixel_size": TREE_PIXEL_SIZE},
	{"name": "WestBroadleaf", "texture": TREE_DECIDUOUS_GREEN, "frames": 8, "fps": 2.3, "position": Vector3(-30.0, 0.0, 8.0), "pixel_size": TREE_PIXEL_SIZE},
	{"name": "EastPine", "texture": TREE_CONIFER_BRIGHT, "frames": 8, "fps": 2.9, "position": Vector3(31.0, 0.0, -7.0), "pixel_size": TREE_PIXEL_SIZE},
	{"name": "EastGold", "texture": TREE_DECIDUOUS_GOLD, "frames": 8, "fps": 2.5, "position": Vector3(31.0, 0.0, 7.0), "pixel_size": TREE_PIXEL_SIZE},
	{"name": "SouthwestBroadleaf", "texture": TREE_DECIDUOUS_GREEN, "frames": 8, "fps": 2.6, "position": Vector3(-28.0, 0.0, 16.5), "pixel_size": TREE_PIXEL_SIZE},
	{"name": "SouthwestConifer", "texture": TREE_CONIFER_DARK, "frames": 8, "fps": 2.4, "position": Vector3(-22.0, 0.0, 18.5), "pixel_size": TREE_PIXEL_SIZE},
	{"name": "SoutheastGold", "texture": TREE_DECIDUOUS_GOLD, "frames": 8, "fps": 2.8, "position": Vector3(22.0, 0.0, 18.5), "pixel_size": TREE_PIXEL_SIZE},
	{"name": "SoutheastPine", "texture": TREE_CONIFER_BRIGHT, "frames": 8, "fps": 2.5, "position": Vector3(29.0, 0.0, 16.0), "pixel_size": TREE_PIXEL_SIZE},
	{"name": "NorthwestRoundBush", "texture": BUSH_ROUND_LIGHT, "frames": 8, "fps": 3.0, "position": Vector3(-33.0, 0.0, -13.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "NorthwestDarkBush", "texture": BUSH_ROUND_DARK, "frames": 8, "fps": 2.7, "position": Vector3(-26.0, 0.0, -14.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "NorthReedBush", "texture": BUSH_REED_LIGHT, "frames": 8, "fps": 3.2, "position": Vector3(-19.0, 0.0, -20.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "WestRoundBush", "texture": BUSH_ROUND_DARK, "frames": 8, "fps": 2.9, "position": Vector3(-32.0, 0.0, 1.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "WestReedBush", "texture": BUSH_REED_DARK, "frames": 8, "fps": 3.1, "position": Vector3(-34.0, 0.0, -8.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "SouthwestRoundBush", "texture": BUSH_ROUND_LIGHT, "frames": 8, "fps": 2.8, "position": Vector3(-31.0, 0.0, 14.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "SouthwestReedBush", "texture": BUSH_REED_LIGHT, "frames": 8, "fps": 3.0, "position": Vector3(-25.0, 0.0, 16.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "SouthRoundBush", "texture": BUSH_ROUND_DARK, "frames": 8, "fps": 2.6, "position": Vector3(-18.0, 0.0, 19.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "NortheastRoundBush", "texture": BUSH_ROUND_LIGHT, "frames": 8, "fps": 3.1, "position": Vector3(33.0, 0.0, -13.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "NortheastDarkBush", "texture": BUSH_ROUND_DARK, "frames": 8, "fps": 2.8, "position": Vector3(26.0, 0.0, -14.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "NortheastReedBush", "texture": BUSH_REED_DARK, "frames": 8, "fps": 3.0, "position": Vector3(19.0, 0.0, -20.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "EastRoundBush", "texture": BUSH_ROUND_DARK, "frames": 8, "fps": 2.7, "position": Vector3(32.0, 0.0, 1.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "EastReedBush", "texture": BUSH_REED_LIGHT, "frames": 8, "fps": 3.2, "position": Vector3(34.0, 0.0, 9.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "SoutheastRoundBush", "texture": BUSH_ROUND_LIGHT, "frames": 8, "fps": 2.9, "position": Vector3(31.0, 0.0, 13.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "SoutheastReedBush", "texture": BUSH_REED_DARK, "frames": 8, "fps": 3.1, "position": Vector3(25.0, 0.0, 16.0), "pixel_size": BUSH_PIXEL_SIZE},
	{"name": "SouthReedBush", "texture": BUSH_REED_LIGHT, "frames": 8, "fps": 2.8, "position": Vector3(18.0, 0.0, 19.0), "pixel_size": BUSH_PIXEL_SIZE},
]

const WATER_PLACEMENTS: Array[Dictionary] = [
	{"name": "FrontLeftRockLarge", "texture": WATER_ROCKS_04, "frames": 16, "fps": 5.0, "position": Vector3(-28.0, WATER_FEET_Y, 28.0), "pixel_size": WATER_ROCK_PIXEL_SIZE},
	{"name": "FrontLeftRockSmall", "texture": WATER_ROCKS_01, "frames": 16, "fps": 5.5, "position": Vector3(-22.0, WATER_FEET_Y, 30.0), "pixel_size": WATER_ROCK_PIXEL_SIZE},
	{"name": "FrontLeftDuck", "texture": RUBBER_DUCK, "frames": 3, "fps": 3.0, "position": Vector3(-15.0, WATER_FEET_Y, 28.5), "pixel_size": DUCK_PIXEL_SIZE},
	{"name": "FrontRightRock", "texture": WATER_ROCKS_02, "frames": 16, "fps": 5.2, "position": Vector3(16.0, WATER_FEET_Y, 28.0), "pixel_size": WATER_ROCK_PIXEL_SIZE},
	{"name": "FrontRightRockLarge", "texture": WATER_ROCKS_03, "frames": 16, "fps": 5.7, "position": Vector3(28.0, WATER_FEET_Y, 30.0), "pixel_size": WATER_ROCK_PIXEL_SIZE},
	{"name": "FrontRightDuck", "texture": RUBBER_DUCK, "frames": 3, "fps": 3.4, "position": Vector3(22.0, WATER_FEET_Y, 32.0), "pixel_size": DUCK_PIXEL_SIZE, "flip": true},
	{"name": "WestRock", "texture": WATER_ROCKS_03, "frames": 16, "fps": 5.4, "position": Vector3(-41.0, WATER_FEET_Y, -9.0), "pixel_size": WATER_ROCK_PIXEL_SIZE},
	{"name": "WestRockSmall", "texture": WATER_ROCKS_01, "frames": 16, "fps": 5.8, "position": Vector3(-42.0, WATER_FEET_Y, 11.0), "pixel_size": WATER_ROCK_PIXEL_SIZE},
	{"name": "WestDuck", "texture": RUBBER_DUCK, "frames": 3, "fps": 2.8, "position": Vector3(-43.0, WATER_FEET_Y, 2.0), "pixel_size": DUCK_PIXEL_SIZE},
	{"name": "EastRock", "texture": WATER_ROCKS_04, "frames": 16, "fps": 5.1, "position": Vector3(41.0, WATER_FEET_Y, -12.0), "pixel_size": WATER_ROCK_PIXEL_SIZE},
	{"name": "EastRockSmall", "texture": WATER_ROCKS_02, "frames": 16, "fps": 5.6, "position": Vector3(42.0, WATER_FEET_Y, 8.0), "pixel_size": WATER_ROCK_PIXEL_SIZE},
	{"name": "EastDuck", "texture": RUBBER_DUCK, "frames": 3, "fps": 3.2, "position": Vector3(43.0, WATER_FEET_Y, -1.0), "pixel_size": DUCK_PIXEL_SIZE, "flip": true},
]

var _sprites: Array[Sprite3D] = []
var _sprite_placements: Array[Dictionary] = []
var _elapsed: float = 0.0


func _ready() -> void:
	for placement: Dictionary in LAND_PLACEMENTS:
		_add_scenery_sprite(placement)
	for placement: Dictionary in WATER_PLACEMENTS:
		_add_scenery_sprite(placement)


func _add_scenery_sprite(placement: Dictionary) -> void:
	var sprite: Sprite3D = Sprite3D.new()
	sprite.name = placement["name"]
	sprite.texture = placement["texture"]
	sprite.hframes = placement["frames"]
	sprite.frame = _sprites.size() % int(placement["frames"])
	sprite.position = placement["position"]
	sprite.flip_h = placement.get("flip", false)
	sprite.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	sprite.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	sprite.pixel_size = placement["pixel_size"]
	CUTOUT_RENDER_ORDER.anchor_visible_bottom(sprite, sprite.texture)
	add_child(sprite)
	CUTOUT_RENDER_ORDER.apply_from_feet(sprite, global_position.z + sprite.position.z)
	_sprites.append(sprite)
	_sprite_placements.append(placement)


func _process(delta: float) -> void:
	_elapsed += delta
	for index: int in range(_sprites.size()):
		var sprite: Sprite3D = _sprites[index]
		var placement: Dictionary = _sprite_placements[index]
		var frame_count: int = placement["frames"]
		var fps: float = placement["fps"]
		sprite.frame = (int(_elapsed * fps) + index) % frame_count
