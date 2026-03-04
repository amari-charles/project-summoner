extends Node

## CapabilityManager - Fine-grained permission system for game features
##
## Replaces coarse tree pausing with specific capability blocking.
## Multiple systems can block the same capability with different reasons,
## and the capability is only enabled when ALL blocks are removed.
##
## Example usage:
##   CapabilityManager.block_capability(Capability.PLAY_CARDS, BlockReason.DIALOGUE_ACTIVE)
##   if CapabilityManager.is_enabled(Capability.PLAY_CARDS):
##       play_card()
##   CapabilityManager.unblock_capability(Capability.PLAY_CARDS, BlockReason.DIALOGUE_ACTIVE)

## Capabilities that can be enabled/disabled
enum Capability {
	PLAY_CARDS,           ## Can play cards from hand
	PAUSE_GAME,           ## Can open pause menu
	MOVE_CAMERA,          ## Can pan/zoom camera
	SKIP_DIALOGUE,        ## Can skip typewriter effect
	OPEN_MENU,            ## Can access game menus
	USE_ABILITIES,        ## Can use unit abilities
	SUMMON_UNITS,         ## Can summon units
	END_TURN,             ## Can end turn (if turn-based)
	INTERACT_UI           ## Can interact with UI elements
}

## Reasons why a capability might be blocked
enum BlockReason {
	DIALOGUE_ACTIVE,      ## Dialogue is currently playing
	CUTSCENE_PLAYING,     ## Cutscene is in progress
	TUTORIAL_LOCKED,      ## Tutorial has locked this feature
	BATTLE_ENDING,        ## Battle is ending (victory/defeat screen)
	LOADING,              ## Game is loading
	CARD_EFFECT,          ## Card effect is resolving
	ANIMATION_PLAYING,    ## Critical animation in progress
	QUEST_RESTRICTION,    ## Quest objective requires restriction
	DEBUG_LOCK            ## Manual debug lock
}

## Signals
signal capability_changed(capability: Capability, enabled: bool)

## Internal state: capability -> set of blocking reasons
## Example: {Capability.PLAY_CARDS: [BlockReason.DIALOGUE_ACTIVE, BlockReason.TUTORIAL_LOCKED]}
var _blocks: Dictionary = {}

## Debug mode: print capability changes
@export var debug_mode: bool = false  # Toggle in inspector

func _ready() -> void:
	# Initialize all capabilities as enabled (empty block sets)
	for cap: Capability in Capability.values():
		_blocks[cap] = []

	if debug_mode:
		print("CapabilityManager: Initialized with %d capabilities" % Capability.size())

## Block a capability for a specific reason
func block_capability(cap: Capability, reason: BlockReason) -> void:
	if not _blocks.has(cap):
		_blocks[cap] = []

	# Check if capability was previously enabled
	var was_enabled: bool = is_enabled(cap)

	# Add block reason if not already present
	var reasons: Array = _blocks.get(cap, []) if _blocks.get(cap, []) is Array else []
	if reason not in reasons:
		reasons.append(reason)

		if debug_mode:
			print("CapabilityManager: Blocked %s (reason: %s, total blocks: %d)" % [
				Capability.keys()[cap],
				BlockReason.keys()[reason],
				reasons.size()
			])

		# Emit signal if capability state changed
		if was_enabled and not is_enabled(cap):
			capability_changed.emit(cap, false)

## Unblock a capability for a specific reason
func unblock_capability(cap: Capability, reason: BlockReason) -> void:
	if not _blocks.has(cap):
		return

	# Check if capability was previously disabled
	var was_enabled: bool = is_enabled(cap)

	# Remove block reason
	var reasons: Array = _blocks.get(cap, []) if _blocks.get(cap, []) is Array else []
	var idx: int = reasons.find(reason)
	if idx >= 0:
		reasons.remove_at(idx)

		if debug_mode:
			print("CapabilityManager: Unblocked %s (reason: %s, remaining blocks: %d)" % [
				Capability.keys()[cap],
				BlockReason.keys()[reason],
				reasons.size()
			])

		# Emit signal if capability state changed
		if not was_enabled and is_enabled(cap):
			capability_changed.emit(cap, true)

## Check if a capability is currently enabled
func is_enabled(cap: Capability) -> bool:
	if not _blocks.has(cap):
		return true
	var reasons: Array = _blocks.get(cap, []) if _blocks.get(cap, []) is Array else []
	return reasons.is_empty()

## Check if a capability is blocked by a specific reason
func is_blocked_by(cap: Capability, reason: BlockReason) -> bool:
	if not _blocks.has(cap):
		return false
	return reason in _blocks[cap]

## Get all reasons blocking a capability
func get_block_reasons(cap: Capability) -> Array:
	if not _blocks.has(cap):
		return []
	var reasons: Array = _blocks.get(cap, []) if _blocks.get(cap, []) is Array else []
	return reasons.duplicate()

## Clear all blocks for a capability
func clear_all_blocks(cap: Capability) -> void:
	if not _blocks.has(cap):
		return

	var was_enabled: bool = is_enabled(cap)
	var reasons: Array = _blocks.get(cap, []) if _blocks.get(cap, []) is Array else []
	reasons.clear()

	if debug_mode:
		print("CapabilityManager: Cleared all blocks for %s" % Capability.keys()[cap])

	if not was_enabled:
		capability_changed.emit(cap, true)

## Clear all blocks for all capabilities (emergency reset)
func clear_all() -> void:
	if debug_mode:
		print("CapabilityManager: Clearing all blocks (emergency reset)")

	for cap: Capability in Capability.values():
		clear_all_blocks(cap)

## Get debug string showing all blocked capabilities
func get_debug_state() -> String:
	var lines: PackedStringArray = PackedStringArray()
	lines.append("=== CapabilityManager State ===")

	for cap: Capability in Capability.values():
		var reasons: Array = _blocks.get(cap, []) if _blocks.get(cap, []) is Array else []
		var enabled: bool = reasons.is_empty()

		var line: String = "%s: %s" % [
			Capability.keys()[cap],
			"ENABLED" if enabled else "BLOCKED"
		]

		if not enabled:
			var reason_names: PackedStringArray = PackedStringArray()
			for reason_variant: Variant in reasons:
				var reason_int: int = reason_variant if reason_variant is int else -1
				if reason_int >= 0:
					var keys_array: PackedStringArray = BlockReason.keys()
					if reason_int < keys_array.size():
						reason_names.append(keys_array[reason_int])
			line += " (%s)" % ", ".join(reason_names)

		lines.append(line)

	return "\n".join(lines)

## Print debug state to console
func print_debug_state() -> void:
	print(get_debug_state())

## Debug helper: Print all capabilities and their block reasons
func print_debug_capabilities() -> void:
	print("\n=== Capability Debug State ===")
	for cap_name: String in Capability.keys():
		var cap: Capability = Capability[cap_name]
		var enabled: bool = is_enabled(cap)
		var reasons: Array = get_block_reasons(cap)

		var status: String = "✓ ENABLED" if enabled else "✗ BLOCKED"
		print("%s: %s" % [cap_name, status])

		if not enabled:
			print("  Reasons:")
			for reason: BlockReason in reasons:
				print("    - %s" % BlockReason.keys()[reason])
	print("=============================\n")
