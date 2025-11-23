extends Resource
class_name EventStep

## EventStep - A single step in an EventSequence
##
## Defines one action to take as part of a scripted event sequence.
## Steps are executed sequentially by EventSequencer.

## Step types
enum StepType {
	# Phase 1: Foundation (Implemented)
	DIALOGUE,              ## Show dialogue
	WAIT_TIME,             ## Wait for specified duration
	SET_CAPABILITY,        ## Enable/disable a capability
	SET_HAND_VISIBILITY,   ## Show/hide hand UI

	# Phase 2+: Future expansion (Not yet implemented)
	WAIT_SIGNAL,           ## Wait for a signal to emit
	SPAWN_UNIT,            ## Spawn a unit on battlefield
	EMIT_SIGNAL,           ## Emit a game event signal
	CUSTOM_FUNCTION,       ## Call a custom GDScript function
	MOVE_CAMERA,           ## Move camera to position
	PLAY_ANIMATION,        ## Play an animation
	FADE_SCREEN,           ## Fade screen in/out
	ENABLE_HAND,           ## Enable/disable hand interaction
	OPEN_CARAVAN,          ## Open caravan shop UI
}

## =============================================================================
## COMMON PROPERTIES
## =============================================================================

@export var step_type: StepType = StepType.DIALOGUE
@export var description: String = ""  ## Human-readable description for debugging

## =============================================================================
## DIALOGUE STEP
## =============================================================================

@export_group("Dialogue")
@export var dialogue_id: String = ""

## =============================================================================
## WAIT_TIME STEP
## =============================================================================

@export_group("Wait Time")
@export var wait_duration: float = 1.0  ## Seconds to wait

## =============================================================================
## WAIT_SIGNAL STEP
## =============================================================================

@export_group("Wait Signal")
@export var signal_source: String = ""  ## Node path or autoload name
@export var signal_name: String = ""

## =============================================================================
## SPAWN_UNIT STEP
## =============================================================================

@export_group("Spawn Unit")
@export var card_id: String = ""  ## Card ID from CardCatalog
@export var spawn_position: Vector3 = Vector3.ZERO
@export var team: int = 1  ## 0 = player, 1 = enemy
@export var stat_overrides: Dictionary = {}  ## Override unit stats (e.g., {"move_speed": 0.0})

## =============================================================================
## SET_CAPABILITY STEP
## =============================================================================

@export_group("Set Capability")
## Which capability to modify (use CapabilityManager.Capability enum value)
@export var capability: int = 0  # CapabilityManager.Capability
## Whether to block (false) or unblock (true) the capability
@export var enable: bool = false
## Reason for blocking (use CapabilityManager.BlockReason enum value)
@export var block_reason: int = 0  # CapabilityManager.BlockReason

## =============================================================================
## EMIT_SIGNAL STEP
## =============================================================================

@export_group("Emit Signal")
@export var event_hub: String = ""  ## Event hub name (e.g., "GameStateEvents")
@export var emit_signal_name: String = ""
@export var signal_args: Array = []  ## Arguments to pass to signal

## =============================================================================
## CUSTOM_FUNCTION STEP
## =============================================================================

@export_group("Custom Function")
@export var target_node_path: String = ""  ## Node path
@export var function_name: String = ""
@export var function_args: Array = []

## =============================================================================
## MOVE_CAMERA STEP
## =============================================================================

@export_group("Move Camera")
@export var camera_position: Vector3 = Vector3.ZERO
@export var camera_duration: float = 1.0  ## Duration of camera movement
@export var camera_smooth: bool = true

## =============================================================================
## FADE_SCREEN STEP
## =============================================================================

@export_group("Fade Screen")
@export var fade_to_black: bool = true  ## true = fade to black, false = fade from black
@export var fade_duration: float = 0.5

## =============================================================================
## SET_HAND_VISIBILITY STEP
## =============================================================================

@export_group("Hand Visibility")
@export var hand_visible: bool = true

## =============================================================================
## ENABLE_HAND STEP
## =============================================================================

@export_group("Enable Hand")
@export var hand_enabled: bool = true

## =============================================================================
## OPEN_CARAVAN STEP
## =============================================================================

@export_group("Open Caravan")
@export var shop_id: String = ""  ## Shop ID to open (e.g., "caravan_tutorial")
