extends CharacterBody3D
class_name WalkableAcademyPlayer

@export var move_speed: float = 12.0

@onready var placeholder_label: Label3D = %PlaceholderPlayerLabel


func _ready() -> void:
	add_to_group("walkable_academy_player")
	placeholder_label.text = Loc.t("academy.walkable.placeholder_player")


func _physics_process(_delta: float) -> void:
	var input_vector: Vector3 = _read_movement_input()
	velocity.x = input_vector.x * move_speed
	velocity.y = 0.0
	velocity.z = input_vector.z * move_speed
	move_and_slide()


func _read_movement_input() -> Vector3:
	var input_vector: Vector2 = Input.get_vector("move_left", "move_right", "move_up", "move_down")
	return Vector3(input_vector.x, 0.0, input_vector.y)
