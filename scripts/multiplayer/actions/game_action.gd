class_name GameAction extends RefCounted

## Base class for all game actions in multiplayer
##
## Actions represent discrete game state changes that can be:
## - Validated by authority (check if action is legal)
## - Executed on authority (apply state change)
## - Serialized for network transmission
## - Replicated to clients
##
## Subclasses: PlayCardAction, ForfeitAction (Phase 2.1)

## Unique ID for this action (for tracking/confirmation)
var action_id: int = 0

## Peer ID of player who initiated this action
var player_id: int = 0

## Timestamp when action was created (game time, not real time)
var timestamp: float = 0.0

## Sequence number in action order (for ordering/replay)
var sequence: int = 0


## Validate if this action is legal in the current game state.
## Returns true if action can be executed.
## Override in subclasses to add specific validation logic.
func validate(_context: Node) -> bool:
	return true


## Execute this action, applying its effects to game state.
## Only called by authority after validation passes.
## Override in subclasses to implement action effects.
func execute(_context: Node) -> void:
	pass


## Serialize this action to a dictionary for network transmission.
## Override in subclasses to include action-specific data.
func serialize() -> Dictionary:
	return {
		"action_type": get_action_type(),
		"action_id": action_id,
		"player_id": player_id,
		"timestamp": timestamp,
		"sequence": sequence,
	}


## Deserialize a dictionary into an action.
## Factory method - returns appropriate GameAction subclass.
## Note: Returns RefCounted to avoid static method class name issues.
static func deserialize(data: Dictionary) -> RefCounted:
	var action_type: String = data.get("action_type", "")
	var action: RefCounted = null

	# Get our own script to create instances (can't use class_name in static methods)
	var this_script: GDScript = load("res://scripts/multiplayer/actions/game_action.gd")

	# Factory pattern - will be extended in Phase 2.1 with specific action types
	match action_type:
		"base":
			action = this_script.new()
		_:
			push_error("GameAction.deserialize: Unknown action type: %s" % action_type)
			return null

	if action:
		action.action_id = data.get("action_id", 0)
		action.player_id = data.get("player_id", 0)
		action.timestamp = data.get("timestamp", 0.0)
		action.sequence = data.get("sequence", 0)

	return action


## Get the action type identifier for serialization.
## Override in subclasses.
func get_action_type() -> String:
	return "base"
