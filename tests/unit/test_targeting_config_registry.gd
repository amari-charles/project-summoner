extends GutTest

## Unit Tests for TargetingConfigRegistry
##
## Tests the programmatic targeting config lookup system.
## Verifies that unit configs are correctly registered and retrieved.
##
## NOTE: These tests require C# to be loaded. In headless mode without .NET,
## the tests will be skipped gracefully.

const SKIP_MSG: String = "Skipped: C# TargetingConfigRegistry not available in headless mode"

## Cached registry reference (loaded at runtime to avoid headless parse errors)
var _registry: Object = null


## =============================================================================
## C# AVAILABILITY CHECK
## =============================================================================

## Returns true if TargetingConfigRegistry C# methods are available
func _is_csharp_available() -> bool:
	if _registry != null:
		return true

	# Check if .cs files can be loaded by checking ResourceLoader capabilities
	# This avoids generating errors in headless mode without C# support
	if not ResourceLoader.exists("res://scripts/csharp/Targeting/TargetingConfigRegistry.cs"):
		return false

	# Verify the resource type is recognized (C# scripts return "Script" when available)
	var recognized_types: PackedStringArray = ResourceLoader.get_recognized_extensions_for_type("Script")
	if not "cs" in recognized_types:
		return false

	# Try to load at runtime
	var script: Resource = load("res://scripts/csharp/Targeting/TargetingConfigRegistry.cs")
	if script == null:
		return false

	# Static class - check if we can call the static method
	if script.has_method("GetConfig"):
		_registry = script
		return true

	return false


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

	assert_not_null(config, "Puff config should be registered")


func test_get_config_returns_default_for_unknown_unit() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return

	var config: Resource = _get_config("nonexistent_unit_xyz")

	# Should return default config, not null
	assert_not_null(config, "Unknown unit should get default config")


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
	assert_not_null(constraint, "Puff should have an attack constraint")

	# CompositeConstraint should contain a HorizontalConeConstraint
	if constraint.has_method("get_class"):
		var class_name_str: String = constraint.get_class()
		assert_true(
			class_name_str.contains("Composite") or class_name_str.contains("Cone"),
			"Puff constraint should include cone constraint"
		)


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
