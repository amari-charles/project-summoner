extends CharacterBody3D
class_name WalkableAcademyPlayer

const PLACEHOLDER_IDLE_TEXTURE: Texture2D = preload("res://assets/placeholders/tiny_swords/characters/placeholder_player_monk_idle.png")
const PLACEHOLDER_RUN_TEXTURE: Texture2D = preload("res://assets/placeholders/tiny_swords/characters/placeholder_player_monk_run.png")
const IDLE_FRAME_COUNT: int = 6
const RUN_FRAME_COUNT: int = 4
const IDLE_FRAMES_PER_SECOND: float = 5.0
const RUN_FRAMES_PER_SECOND: float = 9.0

@export var move_speed: float = 12.0

@onready var placeholder_label: Label3D = %PlaceholderPlayerLabel
@onready var player_visual: Sprite3D = %PlayerVisual

var _animation_elapsed: float = 0.0
var _is_running: bool = false


func _ready() -> void:
	add_to_group("walkable_academy_player")
	placeholder_label.text = Loc.t("academy.walkable.placeholder_player")
	_set_animation(false)


func _physics_process(delta: float) -> void:
	var input_vector: Vector3 = _read_movement_input()
	velocity.x = input_vector.x * move_speed
	velocity.y = 0.0
	velocity.z = input_vector.z * move_speed
	_update_animation(delta, input_vector)
	move_and_slide()


func _read_movement_input() -> Vector3:
	var input_vector: Vector2 = Input.get_vector("move_left", "move_right", "move_up", "move_down")
	return Vector3(input_vector.x, 0.0, input_vector.y)


func _update_animation(delta: float, input_vector: Vector3) -> void:
	var should_run: bool = not input_vector.is_zero_approx()
	if should_run != _is_running:
		_set_animation(should_run)
	_animation_elapsed += delta
	var frames_per_second: float = RUN_FRAMES_PER_SECOND if _is_running else IDLE_FRAMES_PER_SECOND
	player_visual.frame = int(_animation_elapsed * frames_per_second) % player_visual.hframes
	if not is_zero_approx(input_vector.x):
		player_visual.flip_h = input_vector.x < 0.0


func _set_animation(running: bool) -> void:
	_is_running = running
	_animation_elapsed = 0.0
	player_visual.texture = PLACEHOLDER_RUN_TEXTURE if running else PLACEHOLDER_IDLE_TEXTURE
	player_visual.hframes = RUN_FRAME_COUNT if running else IDLE_FRAME_COUNT
	player_visual.frame = 0
