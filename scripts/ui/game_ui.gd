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

## Casting indicator positioning
const CASTING_INDICATOR_OFFSET_X: float = -200.0  ## Left of center
const CASTING_INDICATOR_OFFSET_Y: float = -180.0  ## Above hand area (from bottom)

@export var timer_label: Label = null
@export var player_mana_bar: ManaBar = null
@export var game_over_label: Label = null
@export var restart_button: Button = null

## Two-phase battle system UI elements
@export var phase_label: Label = null

## Dynamically created UI elements
var prep_timer_label: Label = null
var casting_indicator: CastingIndicator = null

var game_controller: Node = null
var player_summoner: Summoner = null
var _initialized: bool = false  # Track initialization state

## Player team value that works for both Unit.Team.PLAYER (2D) and Unit3D.Team.PLAYER (3D)
const PLAYER_TEAM_VALUE: int = 0

func _ready() -> void:
	# Minimal setup - find child nodes if not assigned via @export
	if timer_label == null:
		timer_label = get_node_or_null("TimerLabel")
	if player_mana_bar == null:
		player_mana_bar = get_node_or_null("PlayerManaBar")
	if game_over_label == null:
		game_over_label = get_node_or_null("GameOverLabel")
	if restart_button == null:
		restart_button = get_node_or_null("RestartButton")

	# Two-phase battle system UI elements
	if phase_label == null:
		phase_label = get_node_or_null("PhaseLabel")

	# Create prep timer label dynamically (large, center-top)
	_create_prep_timer_label()

	# Create casting indicator dynamically
	casting_indicator = CastingIndicator.new()
	casting_indicator.name = "CastingIndicator"
	add_child(casting_indicator)

	# Connect restart button (always safe to do in _ready)
	if restart_button:
		restart_button.pressed.connect(_on_restart_pressed)
		restart_button.visible = false  # Hidden until game over

## Initialize GameUI with controller and summoner references
## Called by BattleCoordinator after summoners are ready
func init(controller: Node, summoner: Node) -> void:
	if _initialized:
		return
	_initialized = true

	game_controller = controller
	player_summoner = summoner

	# Connect to game controller signals
	if game_controller:
		if game_controller.has_signal("time_updated"):
			var time_updated_signal: Signal = game_controller.get("time_updated")
			time_updated_signal.connect(_on_time_updated)
		if game_controller.has_signal("game_ended"):
			var game_ended_signal: Signal = game_controller.get("game_ended")
			game_ended_signal.connect(_on_game_ended)
		# Two-phase battle system signals
		if game_controller.has_signal("phase_changed"):
			var phase_changed_signal: Signal = game_controller.get("phase_changed")
			phase_changed_signal.connect(_on_phase_changed)
		if game_controller.has_signal("prep_timer_updated"):
			var prep_timer_signal: Signal = game_controller.get("prep_timer_updated")
			prep_timer_signal.connect(_on_prep_timer_updated)
	else:
		push_error("GameUI: init() called with null game_controller!")

	# Connect to summoner signals
	if player_summoner:
		_connect_to_summoner(player_summoner)
	else:
		push_error("GameUI: init() called with null player_summoner!")

## Connect to summoner signals
func _connect_to_summoner(summoner: Node) -> void:
	if summoner.has_signal("mana_changed"):
		var mana_changed_signal: Signal = summoner.get("mana_changed")
		mana_changed_signal.connect(_on_mana_changed)
		print("GameUI: Connected to PlayerSummoner mana_changed signal")

		# Manually trigger initial update with current values
		# (signal was emitted before we connected)
		var current_mana: float = summoner.get("mana") if "mana" in summoner else 0.0
		var max_mana: float = summoner.get("max_mana") if "max_mana" in summoner else 10.0
		_on_mana_changed(current_mana, max_mana)
	else:
		push_warning("GameUI: PlayerSummoner found but has no mana_changed signal")

	# Casting signals (for summon_time feedback)
	if summoner.has_signal("casting_started"):
		var casting_started_signal: Signal = summoner.get("casting_started")
		casting_started_signal.connect(_on_casting_started)
	if summoner.has_signal("casting_progress"):
		var casting_progress_signal: Signal = summoner.get("casting_progress")
		casting_progress_signal.connect(_on_casting_progress)
	if summoner.has_signal("casting_completed"):
		var casting_completed_signal: Signal = summoner.get("casting_completed")
		casting_completed_signal.connect(_on_casting_completed)

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
		var winner_text: String = Loc.t("ui.battle.player_wins") if winner == Unit.Team.PLAYER else Loc.t("ui.battle.enemy_wins")
		game_over_label.text = winner_text
		game_over_label.visible = true

	# Show restart button
	if restart_button:
		restart_button.visible = true

func _on_restart_pressed() -> void:
	if game_controller and game_controller.has_method("restart_game"):
		game_controller.call("restart_game")

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
	if phase_label:
		if new_phase == GameController3D.BattlePhase.PREPARATION:
			phase_label.text = Loc.t("ui.battle.phase_preparation")
		else:
			phase_label.text = Loc.t("ui.battle.phase_battle")
		phase_label.visible = true

	# Hide prep timer when entering battle phase
	if prep_timer_label and new_phase == GameController3D.BattlePhase.BATTLE:
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

## Handle casting started (summon_time delay begins)
func _on_casting_started(card: Card, duration: float) -> void:
	if casting_indicator:
		# Position near the hand UI (bottom center, offset left)
		var viewport_size: Vector2 = get_viewport().get_visible_rect().size
		casting_indicator.position = Vector2(
			viewport_size.x / 2.0 + CASTING_INDICATOR_OFFSET_X,
			viewport_size.y + CASTING_INDICATOR_OFFSET_Y
		)
		casting_indicator.start_casting(card, duration)

## Handle casting progress update
func _on_casting_progress(remaining: float, total: float) -> void:
	if casting_indicator:
		casting_indicator.update_progress(remaining, total)

## Handle casting completed
func _on_casting_completed(_card: Card) -> void:
	if casting_indicator:
		casting_indicator.stop_casting()
