extends CanvasLayer
class_name GameUI

## Manages all UI updates for the match

## Prep timer UI constants
const PREP_TIMER_FONT_SIZE: int = 72
const PREP_TIMER_OUTLINE_SIZE: int = 4
const PREP_TIMER_OFFSET_TOP: float = 60.0
const PREP_TIMER_OFFSET_BOTTOM: float = 150.0
const PREP_TIMER_OFFSET_HORIZONTAL: float = 100.0

## Prep timer color thresholds (seconds remaining)
const PREP_TIMER_WARNING_THRESHOLD: int = 10  ## Orange warning
const PREP_TIMER_CRITICAL_THRESHOLD: int = 5  ## Red critical

## Prep timer colors
const PREP_TIMER_COLOR_NORMAL: Color = Color(1.0, 0.9, 0.3)   ## Gold/yellow
const PREP_TIMER_COLOR_WARNING: Color = Color(1.0, 0.6, 0.2)  ## Orange
const PREP_TIMER_COLOR_CRITICAL: Color = Color(1.0, 0.3, 0.3) ## Red
const PREP_TIMER_OUTLINE_COLOR: Color = Color(0, 0, 0, 0.9)

@export var timer_label: Label = null
@export var game_over_label: Label = null
@export var restart_button: Button = null

## Stat bars for both players (HP = red, Mana = blue)
@export var player_hp_bar: StatBar = null
@export var player_mana_bar: StatBar = null
@export var enemy_hp_bar: StatBar = null
@export var enemy_mana_bar: StatBar = null

## Two-phase battle system UI elements
@export var phase_label: Label = null

## Dynamically created UI elements
var prep_timer_label: Label = null

var game_controller: Node = null
var player_summoner: Node3D = null
var enemy_summoner: Node3D = null
var _initialized: bool = false  # Track initialization state

## Player team value (matches UnitConstants.Team.PLAYER)
const PLAYER_TEAM_VALUE: int = 0

func _ready() -> void:
	# Minimal setup - find child nodes if not assigned via @export
	if timer_label == null:
		timer_label = get_node_or_null("TimerLabel")
	if game_over_label == null:
		game_over_label = get_node_or_null("GameOverLabel")
	if restart_button == null:
		restart_button = get_node_or_null("RestartButton")

	# Stat bars (all use StatBar now) - look in HUDContainer
	if player_hp_bar == null:
		player_hp_bar = get_node_or_null("HUDContainer/PlayerHPBar")
	if player_mana_bar == null:
		player_mana_bar = get_node_or_null("HUDContainer/PlayerManaBar")
	if enemy_hp_bar == null:
		enemy_hp_bar = get_node_or_null("HUDContainer/EnemyHPBar")
	if enemy_mana_bar == null:
		enemy_mana_bar = get_node_or_null("HUDContainer/EnemyManaBar")

	# Two-phase battle system UI elements
	if phase_label == null:
		phase_label = get_node_or_null("PhaseLabel")

	# Create prep timer label dynamically (large, center-top)
	_create_prep_timer_label()

	# Connect restart button (always safe to do in _ready)
	if restart_button:
		restart_button.pressed.connect(_on_restart_pressed)
		restart_button.visible = false  # Hidden until game over

## Initialize GameUI with controller and summoner references
## Called by BattleCoordinator after summoners are ready
func init(controller: Node, summoner: Node, enemy: Node = null) -> void:
	if _initialized:
		return
	_initialized = true

	game_controller = controller
	player_summoner = summoner
	enemy_summoner = enemy

	# Connect to game controller signals (BattleScene uses PascalCase C# signal names)
	if game_controller:
		if game_controller.has_signal("TimeUpdated"):
			game_controller.connect("TimeUpdated", _on_time_updated)
		if game_controller.has_signal("GameEnded"):
			game_controller.connect("GameEnded", _on_game_ended)
		if game_controller.has_signal("PhaseChanged"):
			game_controller.connect("PhaseChanged", _on_phase_changed)
		if game_controller.has_signal("PrepTimerUpdated"):
			game_controller.connect("PrepTimerUpdated", _on_prep_timer_updated)
	else:
		push_error("GameUI: init() called with null game_controller!")

	# Connect to player summoner signals (mana + HP)
	if player_summoner:
		_connect_to_summoner(player_summoner)
		_connect_to_hp_signals(player_summoner, true)
	else:
		push_error("GameUI: init() called with null player_summoner!")

	# Connect to enemy summoner HP and mana signals
	if enemy_summoner:
		_connect_to_hp_signals(enemy_summoner, false)
		_connect_to_enemy_mana(enemy_summoner)

	# Initialize HP bar colors
	_setup_hp_bars()

## Connect to summoner mana signals (PascalCase C# signals)
func _connect_to_summoner(summoner: Node) -> void:
	if summoner.has_signal("ManaChanged"):
		summoner.connect("ManaChanged", _on_mana_changed)
		print("GameUI: Connected to PlayerSummoner ManaChanged signal")

		# Manually trigger initial update with current values
		var current_mana: float = summoner.get("Mana") if summoner.get("Mana") != null else 0.0
		var max_mana: float = summoner.get("MaxMana") if summoner.get("MaxMana") != null else 100.0
		_on_mana_changed(current_mana, max_mana)
	else:
		push_warning("GameUI: PlayerSummoner found but has no ManaChanged signal")

## Connect to enemy mana signals (PascalCase C# signals)
func _connect_to_enemy_mana(summoner: Node) -> void:
	if summoner.has_signal("ManaChanged"):
		summoner.connect("ManaChanged", _on_enemy_mana_changed)

		# Trigger initial update
		var current_mana: float = summoner.get("Mana") if summoner.get("Mana") != null else 0.0
		var max_mana: float = summoner.get("MaxMana") if summoner.get("MaxMana") != null else 100.0
		_on_enemy_mana_changed(current_mana, max_mana)

func _on_enemy_mana_changed(current: float, maximum: float) -> void:
	if enemy_mana_bar:
		enemy_mana_bar.update_value(current, maximum)

## Connect to HP signals for a summoner (PascalCase C# signals)
func _connect_to_hp_signals(summoner: Node, is_player: bool) -> void:
	if summoner.has_signal("HpChanged"):
		if is_player:
			summoner.connect("HpChanged", _on_player_hp_changed)
		else:
			summoner.connect("HpChanged", _on_enemy_hp_changed)

		# Trigger initial update
		var current_hp: float = summoner.get("CurrentHp") if summoner.get("CurrentHp") != null else 0.0
		var max_hp: float = summoner.get("MaxHp") if summoner.get("MaxHp") != null else 300.0
		if is_player:
			_on_player_hp_changed(current_hp, max_hp)
		else:
			_on_enemy_hp_changed(current_hp, max_hp)

## Setup stat bar colors
func _setup_hp_bars() -> void:
	# Player HP bar - red
	if player_hp_bar:
		player_hp_bar.set_colors(Color(0.85, 0.25, 0.25))
		player_hp_bar.set_label_config(true, "{current}/{max}")

	# Player Mana bar - blue
	if player_mana_bar:
		player_mana_bar.set_colors(Color(0.3, 0.5, 0.9))
		player_mana_bar.set_label_config(true, "{current}/{max}")

	# Enemy HP bar - red
	if enemy_hp_bar:
		enemy_hp_bar.set_colors(Color(0.85, 0.25, 0.25))
		enemy_hp_bar.set_label_config(true, "{current}/{max}")

	# Enemy Mana bar - blue
	if enemy_mana_bar:
		enemy_mana_bar.set_colors(Color(0.3, 0.5, 0.9))
		enemy_mana_bar.set_label_config(true, "{current}/{max}")

## Handle player HP changes
func _on_player_hp_changed(current: float, maximum: float) -> void:
	if player_hp_bar:
		player_hp_bar.update_value(current, maximum)

## Handle enemy HP changes
func _on_enemy_hp_changed(current: float, maximum: float) -> void:
	if enemy_hp_bar:
		enemy_hp_bar.update_value(current, maximum)

func _on_time_updated(remaining: float) -> void:
	if timer_label:
		var minutes: int = floori(remaining / 60.0)
		var seconds: int = int(remaining) % 60
		timer_label.text = "%02d:%02d" % [minutes, seconds]

func _on_mana_changed(current: float, maximum: float) -> void:
	if player_mana_bar:
		player_mana_bar.update_value(current, maximum)

func _on_game_ended(winner: UnitConstants.Team) -> void:
	if game_over_label:
		var winner_text: String = Loc.t("ui.battle.player_wins") if winner == UnitConstants.Team.PLAYER else Loc.t("ui.battle.enemy_wins")
		game_over_label.text = winner_text
		game_over_label.visible = true

	# Show restart button
	if restart_button:
		restart_button.visible = true

func _on_restart_pressed() -> void:
	if game_controller and game_controller.has_method("RestartGame"):
		game_controller.call("RestartGame")

## =============================================================================
## TWO-PHASE BATTLE SYSTEM HANDLERS
## =============================================================================

## Create the preparation phase timer label
func _create_prep_timer_label() -> void:
	prep_timer_label = Label.new()
	prep_timer_label.name = "PrepTimerLabel"

	# Large, bold font for visibility
	prep_timer_label.add_theme_font_size_override("font_size", PREP_TIMER_FONT_SIZE)
	prep_timer_label.add_theme_color_override("font_color", PREP_TIMER_COLOR_NORMAL)
	prep_timer_label.add_theme_color_override("font_outline_color", PREP_TIMER_OUTLINE_COLOR)
	prep_timer_label.add_theme_constant_override("outline_size", PREP_TIMER_OUTLINE_SIZE)

	# Center horizontally at top of screen
	prep_timer_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	prep_timer_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	prep_timer_label.anchors_preset = Control.PRESET_CENTER_TOP
	prep_timer_label.anchor_top = 0.0
	prep_timer_label.anchor_bottom = 0.0
	prep_timer_label.anchor_left = 0.5
	prep_timer_label.anchor_right = 0.5
	prep_timer_label.offset_top = PREP_TIMER_OFFSET_TOP
	prep_timer_label.offset_bottom = PREP_TIMER_OFFSET_BOTTOM
	prep_timer_label.offset_left = -PREP_TIMER_OFFSET_HORIZONTAL
	prep_timer_label.offset_right = PREP_TIMER_OFFSET_HORIZONTAL
	prep_timer_label.grow_horizontal = Control.GROW_DIRECTION_BOTH

	prep_timer_label.visible = false
	add_child(prep_timer_label)

## Handle battle phase change (PREPARATION -> BATTLE)
func _on_phase_changed(new_phase: int) -> void:
	# BattleScene.BattlePhase: Preparation=0, Battle=1
	if phase_label:
		if new_phase == 0:  # Preparation
			phase_label.text = Loc.t("ui.battle.phase_preparation")
		else:
			phase_label.text = Loc.t("ui.battle.phase_battle")
		phase_label.visible = true

	# Hide prep timer when entering battle phase
	if prep_timer_label and new_phase == 1:  # Battle
		prep_timer_label.visible = false

## Handle preparation phase timer update
func _on_prep_timer_updated(remaining: float) -> void:
	if prep_timer_label:
		var seconds: int = ceili(remaining)
		prep_timer_label.text = "%d" % seconds
		prep_timer_label.visible = true

		# Color changes as time runs low
		if seconds <= PREP_TIMER_CRITICAL_THRESHOLD:
			prep_timer_label.add_theme_color_override("font_color", PREP_TIMER_COLOR_CRITICAL)
		elif seconds <= PREP_TIMER_WARNING_THRESHOLD:
			prep_timer_label.add_theme_color_override("font_color", PREP_TIMER_COLOR_WARNING)
		else:
			prep_timer_label.add_theme_color_override("font_color", PREP_TIMER_COLOR_NORMAL)
