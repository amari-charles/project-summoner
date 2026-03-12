extends GutTest

## Unit Tests for BattlefieldConstants
##
## Tests conversion helpers and battlefield display constants.


## =============================================================================
## SCREEN / WORLD CONVERSION TESTS
## =============================================================================

func test_screen_to_world_center_maps_to_origin() -> void:
	var screen_center: Vector2 = Vector2(BattlefieldConstants.SCREEN_CENTER_X, BattlefieldConstants.SCREEN_CENTER_Y)

	var result: Vector3 = BattlefieldConstants.screen_to_world_3d(screen_center)

	assert_eq(result, Vector3(0.0, BattlefieldConstants.SPAWN_PLANE_HEIGHT, 0.0))


func test_screen_to_world_applies_scale_and_offset() -> void:
	var screen_pos: Vector2 = Vector2(
		BattlefieldConstants.SCREEN_CENTER_X + BattlefieldConstants.SCREEN_TO_WORLD_SCALE * 2.0,
		BattlefieldConstants.SCREEN_CENTER_Y - BattlefieldConstants.SCREEN_TO_WORLD_SCALE * 3.0
	)

	var result: Vector3 = BattlefieldConstants.screen_to_world_3d(screen_pos)

	assert_almost_eq(result.x, 2.0, 0.0001)
	assert_almost_eq(result.z, -3.0, 0.0001)


func test_world_to_screen_reconstructs_input_from_world_coordinates() -> void:
	var world_pos: Vector3 = Vector3(2.5, 12.0, -1.25)

	var result: Vector2 = BattlefieldConstants.world_to_screen_2d(world_pos)

	assert_almost_eq(result.x, BattlefieldConstants.SCREEN_CENTER_X + 250.0, 0.0001)
	assert_almost_eq(result.y, BattlefieldConstants.SCREEN_CENTER_Y - 125.0, 0.0001)


func test_screen_to_world_and_back_round_trip_preserves_xz() -> void:
	var source_screen: Vector2 = Vector2(1337.0, 420.0)

	var world_pos: Vector3 = BattlefieldConstants.screen_to_world_3d(source_screen)
	var reconstructed: Vector2 = BattlefieldConstants.world_to_screen_2d(world_pos)

	assert_almost_eq(reconstructed.x, source_screen.x, 0.0001)
	assert_almost_eq(reconstructed.y, source_screen.y, 0.0001)


## =============================================================================
## BATTLEFIELD CONSTANT TESTS
## =============================================================================

func test_battlefield_half_width_is_positive() -> void:
	assert_gt(BattlefieldConstants.BATTLEFIELD_HALF_WIDTH, 0.0)


func test_battlefield_half_depth_is_positive() -> void:
	assert_gt(BattlefieldConstants.BATTLEFIELD_HALF_DEPTH, 0.0)


func test_spawn_boundary_is_at_zero() -> void:
	assert_eq(BattlefieldConstants.SPAWN_BOUNDARY_X, 0.0)
