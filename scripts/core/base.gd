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
	# Load HP from campaign config if this is an enemy base in a campaign battle
	if team == Team.ENEMY:
		_load_campaign_hp()

	current_hp = max_hp

	# Add to groups
	add_to_group("bases")
	if team == Team.PLAYER:
		add_to_group("player_bases")
	else:
		add_to_group("enemy_bases")

	_setup_visuals()

## Load HP from campaign battle config
func _load_campaign_hp() -> void:
	var profile_repo: Node = get_node_or_null("/root/ProfileRepo")
	if not profile_repo:
		return

	var profile: Dictionary = profile_repo.get_active_profile()
	if profile.is_empty():
		return

	var empty_dict: Dictionary = {}
	var campaign_progress: Dictionary = profile.get("campaign_progress", empty_dict) if profile.get("campaign_progress", empty_dict) is Dictionary else {}
	var current_battle_id: String = campaign_progress.get("current_battle", "")
	if current_battle_id == "":
		return  # Not a campaign battle

	var campaign: Node = get_node_or_null("/root/Campaign")
	if not campaign:
		return

	var battle: Dictionary = campaign.get_battle(current_battle_id)
	if battle.has("enemy_hp"):
		max_hp = battle.get("enemy_hp")
		print("Base: Set enemy base HP from campaign: %d" % max_hp)

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
	# Visuals are now defined in the scene files (player_base.tscn, enemy_base.tscn)
	# Just ensure the HP bar is initialized
	_update_hp_bar()

## Update HP bar visual
func _update_hp_bar() -> void:
	# Look for HP bar in Visual node
	if has_node("Visual/HPBar/HPBarFill"):
		var hp_bar_node: Node = get_node("Visual/HPBar/HPBarFill")
		# Type narrow to ColorRect for safe property access
		if hp_bar_node is ColorRect:
			var hp_bar: ColorRect = hp_bar_node
			var hp_percent_value: Variant = current_hp / max_hp
			var hp_percent: float = hp_percent_value
			hp_bar.size.x = 50.0 * hp_percent  # 50px is the full width

			# Use GameColorPalette colors for health
			hp_bar.color = GameColorPalette.get_health_color(hp_percent)

## Update HP bar every frame
func _process(_delta: float) -> void:
	_update_hp_bar()
