extends Node

## GameStateEvents - Specialized event hub for battle lifecycle and game state
##
## Provides signals for battle phases, unit events, and game state changes.
## Systems listen to these signals instead of directly calling each other.
##
## Example usage:
##   # Emitter:
##   GameStateEvents.battle_started.emit()
##
##   # Listener:
##   GameStateEvents.unit_spawned.connect(_on_unit_spawned)

## =============================================================================
## BATTLE LIFECYCLE SIGNALS
## =============================================================================

## Emitted when battle scene loads and initializes
signal battle_loading()

## Emitted when battle is ready to start (all systems initialized)
signal battle_ready()

## Emitted when battle actually begins (countdown ends, player can act)
signal battle_started()

## Emitted when battle is paused (different from capabilities being blocked)
signal battle_paused()

## Emitted when battle resumes from pause
signal battle_resumed()

## Emitted when battle ends (before victory/defeat screen)
## Parameters: victory (bool) - true if player won, false if lost
signal battle_ending(victory: bool)

## Emitted after battle_ending, when victory/defeat screen is shown
signal battle_ended(victory: bool)

## =============================================================================
## UNIT LIFECYCLE SIGNALS
## =============================================================================

## Emitted when a unit is spawned on the battlefield
## Parameters: unit (Node3D), team (int), spawn_position (Vector3)
signal unit_spawned(unit: Node3D, team: int, spawn_position: Vector3)

## Emitted when a unit takes damage
## Parameters: unit (Node3D), damage (float), attacker (Node3D)
signal unit_damaged(unit: Node3D, damage: float, attacker: Node3D)

## Emitted when a unit dies
## Parameters: unit (Node3D), killer (Node3D)
signal unit_died(unit: Node3D, killer: Node3D)

## =============================================================================
## BASE/OBJECTIVE SIGNALS
## =============================================================================

## Emitted when player's base takes damage
## Parameters: damage (float), current_health (float), max_health (float)
signal player_base_damaged(damage: float, current_health: float, max_health: float)

## =============================================================================
## CARD/HAND SIGNALS
## =============================================================================

## Emitted when a card is played from hand
## Parameters: card (Card), play_position (Vector3), team (int)
signal card_played(card: Card, play_position: Vector3, team: int)

## =============================================================================
## HELPER FUNCTIONS
## =============================================================================

var debug_mode: bool = false

func _ready() -> void:
	if debug_mode:
		print("GameStateEvents: Initialized")
		_connect_debug_listeners()

## Connect debug listeners to all signals (for testing)
func _connect_debug_listeners() -> void:
	battle_loading.connect(func() -> void: print("GameStateEvents: battle_loading"))
	battle_ready.connect(func() -> void: print("GameStateEvents: battle_ready"))
	battle_started.connect(func() -> void: print("GameStateEvents: battle_started"))
	battle_paused.connect(func() -> void: print("GameStateEvents: battle_paused"))
	battle_resumed.connect(func() -> void: print("GameStateEvents: battle_resumed"))
	battle_ending.connect(func(victory: bool) -> void: print("GameStateEvents: battle_ending(victory=%s)" % victory))
	battle_ended.connect(func(victory: bool) -> void: print("GameStateEvents: battle_ended(victory=%s)" % victory))

	unit_spawned.connect(func(unit: Node3D, team: int, _position: Vector3) -> void:
		print("GameStateEvents: unit_spawned(unit=%s, team=%d)" % [unit.name, team])
	)
	unit_damaged.connect(func(unit: Node3D, damage: float, _attacker: Node3D) -> void:
		print("GameStateEvents: unit_damaged(unit=%s, damage=%.1f)" % [unit.name, damage])
	)
	unit_died.connect(func(unit: Node3D, killer: Node3D) -> void:
		var killer_name: String = "none"
		if killer:
			killer_name = killer.name
		print("GameStateEvents: unit_died(unit=%s, killer=%s)" % [unit.name, killer_name])
	)

	card_played.connect(func(card: Resource, _position: Vector3, team: int) -> void:
		var card_name_val: Variant = card.call("get_card_name") if card.has_method("get_card_name") else "unknown"
		var card_name: String = card_name_val if card_name_val is String else "unknown"
		print("GameStateEvents: card_played(card=%s, team=%d)" % [card_name, team])
	)

	player_base_damaged.connect(func(damage: float, current: float, _max: float) -> void:
		print("GameStateEvents: player_base_damaged(damage=%.1f, health=%.1f)" % [damage, current])
	)

	print("GameStateEvents: Debug listeners connected")
