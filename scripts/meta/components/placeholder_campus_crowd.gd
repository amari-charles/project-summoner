extends Node3D
class_name PlaceholderCampusCrowd

## Visual-only Tiny Swords placeholders used to judge campus density and scale.
## Placements are intentionally deterministic so layout feedback is reproducible.

const SHEEP_IDLE: Texture2D = preload("res://assets/placeholders/tiny_swords/animals/placeholder_sheep_idle.png")
const SHEEP_GRAZE: Texture2D = preload("res://assets/placeholders/tiny_swords/animals/placeholder_sheep_graze.png")
const STUDENT_IDLE: Texture2D = preload("res://assets/placeholders/tiny_swords/characters/placeholder_student_idle.png")
const GUARD_IDLE: Texture2D = preload("res://assets/placeholders/tiny_swords/characters/placeholder_guard_idle.png")

const PLACEMENTS: Array[Dictionary] = [
	{"texture": SHEEP_IDLE, "frames": 6, "fps": 4.0, "position": Vector3(-8.0, 0.65, 5.0), "flip": false},
	{"texture": SHEEP_GRAZE, "frames": 12, "fps": 5.0, "position": Vector3(8.0, 0.65, 6.0), "flip": true},
	{"texture": SHEEP_IDLE, "frames": 6, "fps": 3.5, "position": Vector3(11.0, 0.65, -2.0), "flip": true},
	{"texture": SHEEP_GRAZE, "frames": 12, "fps": 4.5, "position": Vector3(-5.0, 0.65, -6.0), "flip": false},
	{"texture": STUDENT_IDLE, "frames": 8, "fps": 5.0, "position": Vector3(-10.0, 1.4, -1.0), "flip": false},
	{"texture": STUDENT_IDLE, "frames": 8, "fps": 4.5, "position": Vector3(5.0, 1.4, 2.0), "flip": true},
	{"texture": GUARD_IDLE, "frames": 8, "fps": 5.0, "position": Vector3(12.0, 1.4, -12.0), "flip": true},
	{"texture": GUARD_IDLE, "frames": 8, "fps": 4.0, "position": Vector3(-13.0, 1.4, 11.0), "flip": false},
]

const CHARACTER_PIXEL_SIZE: float = 0.028
const SHEEP_PIXEL_SIZE: float = 0.026

var _sprites: Array[Sprite3D] = []
var _elapsed: float = 0.0


func _ready() -> void:
	for index: int in range(PLACEMENTS.size()):
		var placement: Dictionary = PLACEMENTS[index]
		var sprite: Sprite3D = Sprite3D.new()
		sprite.name = "PlaceholderCrowdMember%d" % (index + 1)
		sprite.texture = placement["texture"]
		sprite.hframes = placement["frames"]
		sprite.frame = index % int(placement["frames"])
		sprite.position = placement["position"]
		sprite.flip_h = placement["flip"]
		sprite.billboard = BaseMaterial3D.BILLBOARD_ENABLED
		sprite.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
		sprite.pixel_size = SHEEP_PIXEL_SIZE if sprite.texture.get_height() == 128 else CHARACTER_PIXEL_SIZE
		add_child(sprite)
		_sprites.append(sprite)


func _process(delta: float) -> void:
	_elapsed += delta
	for index: int in range(_sprites.size()):
		var sprite: Sprite3D = _sprites[index]
		var placement: Dictionary = PLACEMENTS[index]
		var frame_count: int = placement["frames"]
		var fps: float = placement["fps"]
		sprite.frame = (int(_elapsed * fps) + index) % frame_count
