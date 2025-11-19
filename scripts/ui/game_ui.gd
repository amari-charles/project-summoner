extends CanvasLayer
class_name GameUI

## Manages all UI updates for the match

@export var timer_label: Label = null
@export var player_mana_bar: ManaBar = null
@export var game_over_label: Label = null
@export var restart_button: Button = null
@export var redirect_attack_button: Button = null
@export var redirect_defend_button: Button = null

var game_controller: Node = null
var player_summoner: Node = null  # Can be Summoner or Summoner3D

## Player team value that works for both Unit.Team.PLAYER (2D) and Unit3D.Team.PLAYER (3D)
const PLAYER_TEAM_VALUE: int = 0

func _ready() -> void:
	# Find nodes if not assigned
	if timer_label == null:
		timer_label = get_node_or_null("TimerLabel")
	if player_mana_bar == null:
		player_mana_bar = get_node_or_null("PlayerManaBar")
	if game_over_label == null:
		game_over_label = get_node_or_null("GameOverLabel")
	if restart_button == null:
		restart_button = get_node_or_null("RestartButton")
	if redirect_attack_button == null:
		redirect_attack_button = get_node_or_null("RedirectAttackButton")
	if redirect_defend_button == null:
		redirect_defend_button = get_node_or_null("RedirectDefendButton")

	# Connect restart button
	if restart_button:
		restart_button.pressed.connect(_on_restart_pressed)
		restart_button.visible = false  # Hidden until game over

	# Connect redirect buttons
	if redirect_attack_button:
		redirect_attack_button.pressed.connect(_on_redirect_attack_pressed)
	if redirect_defend_button:
		redirect_defend_button.pressed.connect(_on_redirect_defend_pressed)

	# Wait a frame for all nodes to be ready
	await get_tree().process_frame

	# Find game controller and summoner
	game_controller = get_tree().get_first_node_in_group("game_controller")

	# Find the player summoner (can be Summoner or Summoner3D)
	var summoners: Array[Node] = get_tree().get_nodes_in_group("summoners")
	for node: Node in summoners:
		# Check if this node has a team property
		if "team" in node:
			var team_value: Variant = node.get("team")
			# Check if it's the player team (works for both 2D and 3D)
			if team_value == PLAYER_TEAM_VALUE:
				player_summoner = node
				break

	# Connect signals
	if game_controller:
		if game_controller.has_signal("time_updated"):
			var time_updated_signal: Signal = game_controller.get("time_updated")
			time_updated_signal.connect(_on_time_updated)
		if game_controller.has_signal("game_ended"):
			var game_ended_signal: Signal = game_controller.get("game_ended")
			game_ended_signal.connect(_on_game_ended)
		print("GameUI: Connected to GameController")
	else:
		push_error("GameUI: Could not find game_controller!")

	if player_summoner:
		if player_summoner.has_signal("mana_changed"):
			var mana_changed_signal: Signal = player_summoner.get("mana_changed")
			mana_changed_signal.connect(_on_mana_changed)
			print("GameUI: Connected to PlayerSummoner mana_changed signal")
		else:
			push_warning("GameUI: PlayerSummoner found but has no mana_changed signal")
	else:
		push_error("GameUI: Could not find player Summoner!")

func _on_time_updated(remaining: float) -> void:
	if timer_label:
		var minutes: int = floori(remaining / 60.0)
		var seconds: int = int(remaining) % 60
		timer_label.text = "%02d:%02d" % [minutes, seconds]

func _on_mana_changed(current: float, maximum: float) -> void:
	if player_mana_bar:
		player_mana_bar.update_mana(current, maximum)

func _on_game_ended(winner: Unit.Team) -> void:
	if game_over_label:
		var winner_text: String = "PLAYER WINS!" if winner == Unit.Team.PLAYER else "ENEMY WINS!"
		game_over_label.text = winner_text
		game_over_label.visible = true

	# Show restart button
	if restart_button:
		restart_button.visible = true

func _on_restart_pressed() -> void:
	if game_controller and game_controller.has_method("restart_game"):
		game_controller.call("restart_game")

func _process(_delta: float) -> void:
	_update_redirect_buttons()

func _update_redirect_buttons() -> void:
	# Update attack redirect button
	if redirect_attack_button:
		var attack_available: bool = RedirectManager.is_attack_available()
		redirect_attack_button.disabled = not attack_available

		# Update button text
		if attack_available:
			redirect_attack_button.text = "Attack"
		else:
			var cd: float = RedirectManager.attack_cooldown
			redirect_attack_button.text = "Attack\n%.1fs" % cd

		# Update cooldown progress bar
		var attack_progress: TextureProgressBar = redirect_attack_button.get_node_or_null("CooldownProgress")
		if attack_progress:
			if attack_available:
				attack_progress.value = attack_progress.max_value
				attack_progress.visible = false
			else:
				var remaining: float = RedirectManager.attack_cooldown
				attack_progress.value = attack_progress.max_value - remaining
				attack_progress.visible = true

		# Highlight if active
		if RedirectManager.current_mode == RedirectManager.RedirectMode.REDIRECT_ATTACK:
			redirect_attack_button.modulate = Color(0.5, 1.0, 1.5)  # Bright blue highlight
		else:
			redirect_attack_button.modulate = Color.WHITE

	# Update defend redirect button
	if redirect_defend_button:
		var defend_available: bool = RedirectManager.is_defend_available()
		redirect_defend_button.disabled = not defend_available

		# Update button text
		if defend_available:
			redirect_defend_button.text = "Defend"
		else:
			var cd: float = RedirectManager.defend_cooldown
			redirect_defend_button.text = "Defend\n%.1fs" % cd

		# Update cooldown progress bar
		var defend_progress: TextureProgressBar = redirect_defend_button.get_node_or_null("CooldownProgress")
		if defend_progress:
			if defend_available:
				defend_progress.value = defend_progress.max_value
				defend_progress.visible = false
			else:
				var remaining: float = RedirectManager.defend_cooldown
				defend_progress.value = defend_progress.max_value - remaining
				defend_progress.visible = true

		# Highlight if active
		if RedirectManager.current_mode == RedirectManager.RedirectMode.REDIRECT_DEFEND:
			redirect_defend_button.modulate = Color(0.5, 1.5, 1.0)  # Bright green highlight
		else:
			redirect_defend_button.modulate = Color.WHITE

func _on_redirect_attack_pressed() -> void:
	RedirectManager.activate_redirect_attack()
	print("GameUI: Redirect Attack activated")

func _on_redirect_defend_pressed() -> void:
	RedirectManager.activate_redirect_defend()
	print("GameUI: Redirect Defend activated")
