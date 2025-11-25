extends Node
class_name SceneCoordinatorClass

## SceneCoordinator - Deterministic scene transition orchestration
##
## Wraps SceneManager to provide:
## 1. Cleanup of persistent autoloads between scenes
## 2. Service readiness verification
## 3. Waiting for complex scenes' coordinators to complete initialization
##
## Usage:
##   SceneCoordinator.transition_to(SceneManager.SCENE_BATTLE_3D)
##
## For complex scenes (Battle, Deck Builder), the coordinator waits for
## the scene's initialization_complete signal before emitting scene_ready.

## Signals for scene lifecycle
signal scene_cleanup_started(from_scene: String)
signal scene_cleanup_complete(from_scene: String)
signal scene_loading(to_scene: String)
signal scene_ready(scene_path: String)

## Current scene tracking
var _current_scene_path: String = ""

## Debug mode for verbose logging
var debug_mode: bool = true

func _ready() -> void:
	if debug_mode:
		print("SceneCoordinator: Initialized")

## =============================================================================
## PUBLIC API
## =============================================================================

## Transition to a new scene with proper cleanup and initialization
## This is the preferred way to change scenes in the game.
func transition_to(scene_path: String) -> void:
	var old_scene: String = _current_scene_path

	if debug_mode:
		print("SceneCoordinator: Transitioning from '%s' to '%s'" % [old_scene, scene_path])

	# 1. Cleanup old scene's state
	scene_cleanup_started.emit(old_scene)
	_cleanup_persistent_state()
	scene_cleanup_complete.emit(old_scene)

	if debug_mode:
		print("SceneCoordinator: Cleanup complete")

	# 2. Verify critical services are ready
	_verify_services_ready()

	# 3. Change scene via SceneManager
	scene_loading.emit(scene_path)
	_current_scene_path = scene_path
	SceneManager.change_scene(scene_path)

	# 4. Wait for scene's coordinator (if it has one)
	# Use call_deferred to ensure we're after the scene change
	call_deferred("_wait_for_scene_coordinator", scene_path)

## Get current scene path
func get_current_scene() -> String:
	return _current_scene_path

## =============================================================================
## CLEANUP LOGIC
## =============================================================================

## Reset persistent autoloads that need clearing between scenes
func _cleanup_persistent_state() -> void:
	if debug_mode:
		print("SceneCoordinator: Cleaning up persistent state...")

	# Reset event sequencer (clears any running sequences)
	if EventSequencer:
		EventSequencer.reset()
		if debug_mode:
			print("  - Reset EventSequencer")

	# Reset dialogue manager (clears UI registration, dialogue state)
	if DialogueManager:
		DialogueManager.reset()
		if debug_mode:
			print("  - Reset DialogueManager")

	# Reset spell targeting (clears any active targeting state)
	if SpellTargetingManager:
		SpellTargetingManager.reset()
		if debug_mode:
			print("  - Reset SpellTargetingManager")

	# Note: BattleContext is NOT reset here
	# It's configured BEFORE scene transition by the caller (CampaignMap, etc.)

	# Note: EventContext, NavigationContext are NOT reset here
	# They carry state needed by the destination scene

	if debug_mode:
		print("SceneCoordinator: Persistent state cleanup complete")

## =============================================================================
## SERVICE VERIFICATION
## =============================================================================

## Verify critical services are initialized and ready
## This catches configuration bugs early rather than during gameplay
func _verify_services_ready() -> void:
	# Check critical autoloads exist
	# These are required for any scene to function properly
	var missing_services: Array[String] = []

	if not ProfileRepo:
		missing_services.append("ProfileRepo")
	if not Campaign:
		missing_services.append("Campaign")
	if not CardCatalog:
		missing_services.append("CardCatalog")
	if not SceneManager:
		missing_services.append("SceneManager")

	if missing_services.size() > 0:
		push_error("SceneCoordinator: Critical services missing: %s" % [missing_services])
		# Don't assert in production - let the scene fail gracefully
		# assert(false, "Missing critical services")

	if debug_mode and missing_services.size() == 0:
		print("SceneCoordinator: All critical services ready")

## =============================================================================
## SCENE COORDINATOR WAITING
## =============================================================================

## Wait for the new scene's coordinator to complete initialization
## Called via call_deferred after scene change
func _wait_for_scene_coordinator(scene_path: String) -> void:
	# Wait one frame for scene tree to update
	await get_tree().process_frame

	# Look for a scene coordinator in the new scene
	# Complex scenes (Battle, Deck Builder) add themselves to this group
	var coordinator: Node = get_tree().get_first_node_in_group("battle_coordinator")

	if coordinator:
		if debug_mode:
			print("SceneCoordinator: Found battle coordinator, waiting for initialization...")

		# Wait for the coordinator to signal completion
		if coordinator.has_signal("initialization_complete"):
			await coordinator.get("initialization_complete")
			if debug_mode:
				print("SceneCoordinator: Battle coordinator initialization complete")
	else:
		if debug_mode:
			print("SceneCoordinator: No battle coordinator found (simple scene)")

	# Scene is fully ready
	scene_ready.emit(scene_path)
	if debug_mode:
		print("SceneCoordinator: Scene '%s' is ready" % scene_path)
