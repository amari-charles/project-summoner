extends GutTest

## Unit Tests for TargetingConfigRegistry
##
## Tests the programmatic targeting config lookup system.
## Verifies that unit configs are correctly registered and retrieved.
##
## NOTE: These tests require C# to be loaded. In headless mode without .NET,
## the tests will be skipped gracefully.

const SKIP_MSG: String = "Skipped: C# TargetingConfigRegistry not available"

## Cached registry reference
var _registry: Node = null


## =============================================================================
## C# AVAILABILITY CHECK
## =============================================================================

## Returns true if TargetingConfigRegistry C# bridge is available
func _is_csharp_available() -> bool:
	if _registry != null:
		return true

	# Check if the bridge autoload exists and has the GetConfig method
	var registry: Node = get_node_or_null("/root/TargetingConfigRegistryCS")
	if registry == null:
		return false

	# Verify the C# method is accessible
	if not registry.has_method("GetConfig"):
		return false

	_registry = registry
	return true


func _get_config(unit_id: String) -> Resource:
	if not _is_csharp_available():
		return null
	return _registry.GetConfig(unit_id)


## =============================================================================
## REGISTRY LOOKUP TESTS
## =============================================================================

func test_get_config_returns_config_for_registered_unit() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return

	var config: Resource = _get_config("puff")

	# Use boolean check to avoid GUT introspection errors with C# objects
	assert_true(config != null, "Puff config should be registered")


func test_get_config_returns_default_for_unknown_unit() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return

	var config: Resource = _get_config("nonexistent_unit_xyz")

	# Should return default config, not null
	# Use boolean check to avoid GUT introspection errors with C# objects
	assert_true(config != null, "Unknown unit should get default config")


func test_puff_config_has_strafe_fallback() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return

	var config: Resource = _get_config("puff")

	# FallbackMovementStyle.Strafe = 1
	var fallback_movement: int = config.get("FallbackMovement")
	assert_eq(fallback_movement, 1, "Puff should use Strafe fallback movement")


func test_rock_config_has_idle_fallback() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return

	var config: Resource = _get_config("rock")

	# FallbackMovementStyle.Idle = 2
	var fallback_movement: int = config.get("FallbackMovement")
	assert_eq(fallback_movement, 2, "Rock should use Idle fallback movement")


func test_rock_config_has_zero_aggro_radius() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return

	var config: Resource = _get_config("rock")

	var aggro_radius: float = config.get("AggroRadius")
	assert_eq(aggro_radius, 0.0, "Rock should have 0 aggro radius (stationary dummy)")


func test_puff_config_has_cone_constraint() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return

	var config: Resource = _get_config("puff")

	var constraint: Resource = config.get("AttackConstraint")
	# Use boolean check to avoid GUT introspection errors with C# objects
	assert_true(constraint != null, "Puff should have an attack constraint")

	# CompositeConstraint has a Constraints property containing child constraints
	# Check that this property exists (indicates it's a composite constraint)
	var has_constraints_prop: bool = "Constraints" in constraint
	assert_true(has_constraints_prop, "Puff constraint should be a CompositeConstraint with nested constraints")


## =============================================================================
## FALLBACK MOVEMENT STYLE ENUM TESTS
## =============================================================================

func test_fallback_movement_move_toward_value() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return

	# Get default config which uses MoveToward
	var config: Resource = _get_config("nonexistent_unit")
	var fallback: int = config.get("FallbackMovement")

	# FallbackMovementStyle.MoveToward = 0 (default)
	assert_eq(fallback, 0, "Default fallback should be MoveToward (0)")
