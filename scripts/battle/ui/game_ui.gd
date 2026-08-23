extends CanvasLayer
class_name GameUI

## Manages all UI updates for the match

signal battle_conclusion_finished

const GAME_OVER_DISPLAY_DURATION: float = 1.0

## Prep timer UI constants
const PREP_TIMER_FONT_SIZE: int = 72
const PREP_TIMER_OUTLINE_SIZE: int = 4
const BATTLE_PHASE_TITLE_DURATION: float = 0.8
const BATTLE_PHASE_TITLE_FADE_DURATION: float = 0.25

## Prep timer color thresholds (seconds remaining)
const PREP_TIMER_WARNING_THRESHOLD: int = 10  ## Orange warning
const PREP_TIMER_CRITICAL_THRESHOLD: int = 5  ## Red critical

## Prep timer colors
const PREP_TIMER_COLOR_NORMAL: Color = Color(1.0, 0.9, 0.3)   ## Gold/yellow
const PREP_TIMER_COLOR_WARNING: Color = Color(1.0, 0.6, 0.2)  ## Orange
const PREP_TIMER_COLOR_CRITICAL: Color = Color(1.0, 0.3, 0.3) ## Red
const PREP_TIMER_OUTLINE_COLOR: Color = Color(0, 0, 0, 0.9)

@export var timer_label: Label = null
@export var game_over_modal: PanelContainer = null
@export var game_over_label: Label = null

## Mirrored identity and resource groups
@export var player_status: BattleSummonerStatus = null
@export var enemy_status: BattleSummonerStatus = null

## Stat bars for both players (HP = red, Mana = blue)
@export var player_hp_bar: StatBar = null
@export var player_mana_bar: StatBar = null
@export var enemy_hp_bar: StatBar = null
@export var enemy_mana_bar: StatBar = null

## Two-phase battle system UI elements
@export var phase_label: Label = null
@export var prep_timer_label: Label = null

## Dynamically created UI elements
var reconnect_label: Label = null
var _reconnect_reason: String = ""
var _phase_title_tween: Tween = null

var game_controller: Node = null
var player_summoner: Node3D = null
var enemy_summoner: Node3D = null
var _initialized: bool = false  # Track initialization state
var _game_over_transition_scheduled: bool = false

## Stat bar colors
const HP_BAR_COLOR: Color = Color(0.85, 0.25, 0.25)
const MANA_BAR_COLOR: Color = Color(0.3, 0.5, 0.9)

func _ready() -> void:
	# Minimal setup - find child nodes if not assigned via @export
	if timer_label == null:
		timer_label = get_node_or_null("TimerLabel")
	if game_over_modal == null:
		game_over_modal = get_node_or_null("GameOverModal")
	if game_over_label == null:
		game_over_label = get_node_or_null("GameOverModal/Content/GameOverLabel")
	if player_status == null:
		player_status = get_node_or_null("HUDContainer/PlayerStatus")
	if enemy_status == null:
		enemy_status = get_node_or_null("HUDContainer/EnemyStatus")

	# Stat bars (all use StatBar now) - look in HUDContainer
	if player_hp_bar == null:
		player_hp_bar = get_node_or_null("HUDContainer/PlayerStatus/Details/HPBar")
	if player_mana_bar == null:
		player_mana_bar = get_node_or_null("HUDContainer/PlayerStatus/Details/ManaBar")
	if enemy_hp_bar == null:
		enemy_hp_bar = get_node_or_null("HUDContainer/EnemyStatus/Details/HPBar")
	if enemy_mana_bar == null:
		enemy_mana_bar = get_node_or_null("HUDContainer/EnemyStatus/Details/ManaBar")

	# Two-phase battle system UI elements
	if phase_label == null:
		phase_label = get_node_or_null("PhaseLabel")
	if prep_timer_label == null:
		prep_timer_label = get_node_or_null("PrepTimerLabel")

	_create_reconnect_label()

## Initialize GameUI with controller and summoner references
## Called by BattleCoordinator after summoners are ready
func init(controller: Node, summoner: Node, enemy: Node = null) -> void:
	if _initialized:
		return
	_initialized = true

	game_controller = controller
	player_summoner = summoner
	enemy_summoner = enemy
	_configure_summoner_statuses()

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
		if game_controller.has_signal("ReconnectStateChanged"):
			game_controller.connect("ReconnectStateChanged", _on_reconnect_state_changed)
		if game_controller.has_signal("ReconnectTimerUpdated"):
			game_controller.connect("ReconnectTimerUpdated", _on_reconnect_timer_updated)
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


func _configure_summoner_statuses() -> void:
	var config: Dictionary = BattleContext.battle_config
	var player_side: Dictionary = SafeTypeUtils.dict(config.get("player_side", {}))
	var player_definition: Dictionary = SafeTypeUtils.dict(player_side.get("summoner", {}))
	var enemy_side: Dictionary = SafeTypeUtils.dict(config.get("enemy_side", {}))
	var enemy_definition: Dictionary = SafeTypeUtils.dict(enemy_side.get("summoner", {}))

	var active_summoner_id: String = SummonerSelectionApi.get_active_summoner_id()
	var player_summoner_id: String = SafeTypeUtils.string(
		config.get("player_summoner_id", player_definition.get("id", active_summoner_id)),
		active_summoner_id
	)
	var opponent_summoner_id: String = SafeTypeUtils.string(
		config.get("opponent_summoner_id", enemy_definition.get("id", "")),
		""
	)
	var player_fallback_name: String = SafeTypeUtils.string(
		player_definition.get("display_name", Loc.t("ui.battle.player_fallback")),
		Loc.t("ui.battle.player_fallback")
	)
	var opponent_fallback_name: String = SafeTypeUtils.string(
		enemy_definition.get(
			"display_name",
			config.get("opponent_username", Loc.t("ui.battle.opponent_fallback"))
		),
		Loc.t("ui.battle.opponent_fallback")
	)

	if player_status != null:
		player_status.configure(player_summoner_id, player_fallback_name)
	if enemy_status != null:
		enemy_status.configure(opponent_summoner_id, opponent_fallback_name)

## Connect to summoner mana signals (PascalCase C# signals)
func _connect_to_summoner(summoner: Node) -> void:
	if summoner.has_signal("ManaChanged"):
		summoner.connect("ManaChanged", _on_mana_changed)

		# Manually trigger initial update with current values
		var current_mana: float = SafeTypeUtils.float_val(summoner.get("Mana"), 0.0)
		var max_mana: float = SafeTypeUtils.float_val(summoner.get("MaxMana"), 100.0)
		_on_mana_changed(current_mana, max_mana)
	else:
		push_warning("GameUI: PlayerSummoner found but has no ManaChanged signal")

## Connect to enemy mana signals (PascalCase C# signals)
func _connect_to_enemy_mana(summoner: Node) -> void:
	if summoner.has_signal("ManaChanged"):
		summoner.connect("ManaChanged", _on_enemy_mana_changed)

		# Trigger initial update
		var current_mana: float = SafeTypeUtils.float_val(summoner.get("Mana"), 0.0)
		var max_mana: float = SafeTypeUtils.float_val(summoner.get("MaxMana"), 100.0)
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
		var current_hp: float = SafeTypeUtils.float_val(summoner.get("CurrentHp"), 0.0)
		var max_hp: float = SafeTypeUtils.float_val(summoner.get("MaxHp"), 300.0)
		if is_player:
			_on_player_hp_changed(current_hp, max_hp)
		else:
			_on_enemy_hp_changed(current_hp, max_hp)

## Setup stat bar colors
func _setup_hp_bars() -> void:
	if player_hp_bar:
		player_hp_bar.set_colors(HP_BAR_COLOR)
		player_hp_bar.set_label_config(true, "{current}/{max}")

	if player_mana_bar:
		player_mana_bar.set_colors(MANA_BAR_COLOR)
		player_mana_bar.set_label_config(true, "{current}/{max}")

	if enemy_hp_bar:
		enemy_hp_bar.set_colors(HP_BAR_COLOR)
		enemy_hp_bar.set_label_config(true, "{current}/{max}")

	if enemy_mana_bar:
		enemy_mana_bar.set_colors(MANA_BAR_COLOR)
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
		var clamped_remaining: float = maxf(remaining, 0.0)
		var minutes: int = floori(clamped_remaining / 60.0)
		var seconds: int = int(clamped_remaining) % 60
		timer_label.text = "%02d:%02d" % [minutes, seconds]

func _on_mana_changed(current: float, maximum: float) -> void:
	if player_mana_bar:
		player_mana_bar.update_value(current, maximum)

func _on_game_ended(winner: UnitConstants.Team) -> void:
	if _game_over_transition_scheduled:
		return
	_game_over_transition_scheduled = true

	if game_over_label:
		game_over_label.text = (
			Loc.t("ui.post_battle.victory")
			if winner == UnitConstants.Team.PLAYER
			else Loc.t("ui.post_battle.defeat")
		)

	if game_over_modal:
		game_over_modal.visible = true

	get_tree().create_timer(
		GAME_OVER_DISPLAY_DURATION,
		true,
		false,
		true
	).timeout.connect(_finish_game_over_conclusion)


func _finish_game_over_conclusion() -> void:
	if game_over_modal:
		game_over_modal.visible = false
	battle_conclusion_finished.emit()
	if game_controller and game_controller.has_method("ContinueAfterGameOver"):
		game_controller.call("ContinueAfterGameOver")

## =============================================================================
## TWO-PHASE BATTLE SYSTEM HANDLERS
## =============================================================================

func _create_reconnect_label() -> void:
	reconnect_label = Label.new()
	reconnect_label.name = "ReconnectLabel"
	reconnect_label.add_theme_font_size_override("font_size", 30)
	reconnect_label.add_theme_color_override("font_color", Color(1.0, 0.8, 0.3))
	reconnect_label.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.9))
	reconnect_label.add_theme_constant_override("outline_size", 3)
	reconnect_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	reconnect_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	reconnect_label.anchors_preset = Control.PRESET_CENTER
	reconnect_label.offset_left = -320
	reconnect_label.offset_right = 320
	reconnect_label.offset_top = -40
	reconnect_label.offset_bottom = 40
	reconnect_label.visible = false
	add_child(reconnect_label)


func _on_reconnect_state_changed(reconnecting: bool, reason: String) -> void:
	_reconnect_reason = reason
	if reconnect_label == null:
		return
	if reconnecting:
		reconnect_label.visible = true
		reconnect_label.text = "Connection lost. Reconnecting..."
	else:
		reconnect_label.visible = false
		reconnect_label.text = ""


func _on_reconnect_timer_updated(remaining_seconds: float) -> void:
	if reconnect_label == null or not reconnect_label.visible:
		return
	var seconds: int = maxi(0, ceili(remaining_seconds))
	var suffix: String = ""
	if not _reconnect_reason.is_empty():
		suffix = "\n" + _reconnect_reason
	reconnect_label.text = "Connection lost. Reconnecting... %ds" % seconds + suffix

func _exit_tree() -> void:
	if game_controller:
		if game_controller.has_signal("TimeUpdated") and game_controller.is_connected("TimeUpdated", _on_time_updated):
			game_controller.disconnect("TimeUpdated", _on_time_updated)
		if game_controller.has_signal("GameEnded") and game_controller.is_connected("GameEnded", _on_game_ended):
			game_controller.disconnect("GameEnded", _on_game_ended)
		if game_controller.has_signal("PhaseChanged") and game_controller.is_connected("PhaseChanged", _on_phase_changed):
			game_controller.disconnect("PhaseChanged", _on_phase_changed)
		if game_controller.has_signal("PrepTimerUpdated") and game_controller.is_connected("PrepTimerUpdated", _on_prep_timer_updated):
			game_controller.disconnect("PrepTimerUpdated", _on_prep_timer_updated)
		if game_controller.has_signal("ReconnectStateChanged") and game_controller.is_connected("ReconnectStateChanged", _on_reconnect_state_changed):
			game_controller.disconnect("ReconnectStateChanged", _on_reconnect_state_changed)
		if game_controller.has_signal("ReconnectTimerUpdated") and game_controller.is_connected("ReconnectTimerUpdated", _on_reconnect_timer_updated):
			game_controller.disconnect("ReconnectTimerUpdated", _on_reconnect_timer_updated)
	if player_summoner:
		if player_summoner.has_signal("ManaChanged") and player_summoner.is_connected("ManaChanged", _on_mana_changed):
			player_summoner.disconnect("ManaChanged", _on_mana_changed)
		if player_summoner.has_signal("HpChanged") and player_summoner.is_connected("HpChanged", _on_player_hp_changed):
			player_summoner.disconnect("HpChanged", _on_player_hp_changed)
	if enemy_summoner:
		if enemy_summoner.has_signal("HpChanged") and enemy_summoner.is_connected("HpChanged", _on_enemy_hp_changed):
			enemy_summoner.disconnect("HpChanged", _on_enemy_hp_changed)
		if enemy_summoner.has_signal("ManaChanged") and enemy_summoner.is_connected("ManaChanged", _on_enemy_mana_changed):
			enemy_summoner.disconnect("ManaChanged", _on_enemy_mana_changed)


func _on_phase_changed(new_phase: int) -> void:
	if _phase_title_tween != null:
		_phase_title_tween.kill()
		_phase_title_tween = null

	if timer_label:
		timer_label.visible = new_phase == UnitConstants.BattlePhase.BATTLE

	if phase_label:
		phase_label.modulate.a = 1.0
		if new_phase == UnitConstants.BattlePhase.PREPARATION:
			phase_label.text = Loc.t("ui.battle.prepare_your_field")
			phase_label.visible = true
		else:
			phase_label.text = Loc.t("ui.battle.phase_battle")
			phase_label.visible = true
			_phase_title_tween = create_tween()
			_phase_title_tween.tween_interval(BATTLE_PHASE_TITLE_DURATION)
			_phase_title_tween.tween_property(
				phase_label,
				"modulate:a",
				0.0,
				BATTLE_PHASE_TITLE_FADE_DURATION
			)
			_phase_title_tween.tween_callback(phase_label.hide)

	# Hide prep timer when entering battle phase
	if prep_timer_label and new_phase == UnitConstants.BattlePhase.BATTLE:
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
