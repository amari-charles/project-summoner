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
## Parameters: unit (Node3D), team (int), position (Vector3)
signal unit_spawned(unit: Node3D, team: int, position: Vector3)

## Emitted when a unit takes damage
## Parameters: unit (Node3D), damage (float), attacker (Node3D)
signal unit_damaged(unit: Node3D, damage: float, attacker: Node3D)

## Emitted when a unit is healed
## Parameters: unit (Node3D), amount (float), healer (Node3D)
signal unit_healed(unit: Node3D, amount: float, healer: Node3D)

## Emitted when a unit dies
## Parameters: unit (Node3D), killer (Node3D)
signal unit_died(unit: Node3D, killer: Node3D)

## Emitted when a unit is removed from battlefield (despawn)
signal unit_removed(unit: Node3D)

## Emitted when a unit uses an ability
## Parameters: unit (Node3D), ability_name (String)
signal unit_ability_used(unit: Node3D, ability_name: String)

## =============================================================================
## BASE/OBJECTIVE SIGNALS
## =============================================================================

## Emitted when player's base takes damage
## Parameters: damage (float), current_health (float), max_health (float)
signal player_base_damaged(damage: float, current_health: float, max_health: float)

## Emitted when enemy's base takes damage
signal enemy_base_damaged(damage: float, current_health: float, max_health: float)

## Emitted when player's base is destroyed
signal player_base_destroyed()

## Emitted when enemy's base is destroyed
signal enemy_base_destroyed()

## =============================================================================
## CARD/HAND SIGNALS
## =============================================================================

## Emitted when a card is played from hand
## Parameters: card (Card), position (Vector3), team (int)
signal card_played(card: Card, position: Vector3, team: int)

## Emitted when a card is drawn
signal card_drawn(card: Card)

## Emitted when hand changes (card added/removed)
signal hand_changed(hand_size: int)

## Emitted when mana changes
signal mana_changed(current: int, max: int)

## =============================================================================
## TURN SIGNALS (for turn-based modes)
## =============================================================================

## Emitted when a new turn begins
signal turn_started(turn_number: int)

## Emitted when turn ends
signal turn_ended(turn_number: int)

## Emitted when player's turn starts
signal player_turn_started()

## Emitted when enemy's turn starts
signal enemy_turn_started()

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

	unit_spawned.connect(func(unit: Node3D, team: int, position: Vector3) -> void:
		print("GameStateEvents: unit_spawned(unit=%s, team=%d, pos=%s)" % [unit.name, team, position])
	)
	unit_damaged.connect(func(unit: Node3D, damage: float, attacker: Node3D) -> void:
		print("GameStateEvents: unit_damaged(unit=%s, damage=%.1f)" % [unit.name, damage])
	)
	unit_died.connect(func(unit: Node3D, killer: Node3D) -> void:
		print("GameStateEvents: unit_died(unit=%s, killer=%s)" % [unit.name, killer.name if killer else "none"])
	)

	card_played.connect(func(card: Resource, position: Vector3, team: int) -> void:
		var card_name_val: Variant = card.call("get_card_name") if card.has_method("get_card_name") else "unknown"
		var card_name: String = card_name_val if card_name_val is String else "unknown"
		print("GameStateEvents: card_played(card=%s, team=%d)" % [card_name, team])
	)

	player_base_damaged.connect(func(damage: float, current: float, _max: float) -> void:
		print("GameStateEvents: player_base_damaged(damage=%.1f, health=%.1f)" % [damage, current])
	)

	print("GameStateEvents: Debug listeners connected")
