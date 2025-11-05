extends StaticBody2D
class_name Base

## Base structure that units attack
## Each team has one base - destroying it wins the game

enum Team { PLAYER, ENEMY }

@export var max_hp: float = 300.0
@export var team: Team = Team.PLAYER

var current_hp: float
var is_alive: bool = true

## Signals
signal base_destroyed(base: Base)
signal base_damaged(base: Base, damage: float)

func _ready() -> void:
	current_hp = max_hp

	# Add to groups
	add_to_group("bases")
	if team == Team.PLAYER:
		add_to_group("player_bases")
	else:
		add_to_group("enemy_bases")

	_setup_visuals()

## Take damage from units
func take_damage(damage: float) -> void:
	if not is_alive:
		return

	current_hp -= damage
	base_damaged.emit(self, damage)

	# Update HP bar
	_update_hp_bar()

	if current_hp <= 0:
		current_hp = 0
		_destroy()

## Destroy the base
func _destroy() -> void:
	is_alive = false
	base_destroyed.emit(self)
	print("Base destroyed! Team: ", team)

## Setup visual representation
func _setup_visuals() -> void:
	# Border (behind wall)
	var border = ColorRect.new()
	border.name = "Border"
	border.size = Vector2(64, 204)
	border.position = Vector2(-32, -102)
	border.color = Color.WHITE
	add_child(border)

	# Wall/tower structure
	var wall = ColorRect.new()
	wall.name = "Wall"
	wall.size = Vector2(60, 200)
	wall.position = Vector2(-30, -100)
	wall.color = Color.DARK_BLUE if team == Team.PLAYER else Color.DARK_RED
	add_child(wall)

	# HP bar background
	var hp_bar_bg = ColorRect.new()
	hp_bar_bg.name = "HPBarBG"
	hp_bar_bg.size = Vector2(70, 12)
	hp_bar_bg.position = Vector2(-35, -120)
	hp_bar_bg.color = Color.BLACK
	add_child(hp_bar_bg)

	# HP bar (on top of background)
	var hp_bar = ColorRect.new()
	hp_bar.name = "HPBar"
	hp_bar.size = Vector2(70, 12)
	hp_bar.position = Vector2(-35, -120)
	hp_bar.color = Color.GREEN
	add_child(hp_bar)

## Update HP bar visual
func _update_hp_bar() -> void:
	if has_node("HPBar"):
		var hp_bar = get_node("HPBar") as ColorRect
		var hp_percent = current_hp / max_hp
		hp_bar.size.x = 70 * hp_percent

		# Change color based on HP
		if hp_percent > 0.5:
			hp_bar.color = Color.GREEN
		elif hp_percent > 0.25:
			hp_bar.color = Color.YELLOW
		else:
			hp_bar.color = Color.RED

## Update HP bar every frame
func _process(_delta: float) -> void:
	_update_hp_bar()
