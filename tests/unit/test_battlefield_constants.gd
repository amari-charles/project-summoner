extends GutTest

## Unit Tests for BattlefieldConstants
##
## Tests spawn zone validation and battlefield dimension constants.


## =============================================================================
## SPAWN ZONE VALIDATION TESTS
## =============================================================================

func test_player_can_spawn_on_negative_x() -> void:
	var pos := Vector3(-10.0, 0.0, 0.0)

	var result: bool = BattlefieldConstants.is_valid_spawn_position_for_team(pos, Unit3D.Team.PLAYER)

	assert_true(result)


func test_player_cannot_spawn_on_positive_x() -> void:
	var pos := Vector3(10.0, 0.0, 0.0)

	var result: bool = BattlefieldConstants.is_valid_spawn_position_for_team(pos, Unit3D.Team.PLAYER)

	assert_false(result)


func test_player_cannot_spawn_at_boundary() -> void:
	var pos := Vector3(0.0, 0.0, 0.0)  # Exactly at boundary

	var result: bool = BattlefieldConstants.is_valid_spawn_position_for_team(pos, Unit3D.Team.PLAYER)

	assert_false(result)  # X < 0 required, X = 0 is invalid


func test_enemy_can_spawn_on_positive_x() -> void:
	var pos := Vector3(10.0, 0.0, 0.0)

	var result: bool = BattlefieldConstants.is_valid_spawn_position_for_team(pos, Unit3D.Team.ENEMY)

	assert_true(result)


func test_enemy_cannot_spawn_on_negative_x() -> void:
	var pos := Vector3(-10.0, 0.0, 0.0)

	var result: bool = BattlefieldConstants.is_valid_spawn_position_for_team(pos, Unit3D.Team.ENEMY)

	assert_false(result)


func test_enemy_cannot_spawn_at_boundary() -> void:
	var pos := Vector3(0.0, 0.0, 0.0)  # Exactly at boundary

	var result: bool = BattlefieldConstants.is_valid_spawn_position_for_team(pos, Unit3D.Team.ENEMY)

	assert_false(result)  # X > 0 required, X = 0 is invalid


func test_spawn_validation_ignores_y_coordinate() -> void:
	var pos_ground := Vector3(-5.0, 0.0, 0.0)
	var pos_elevated := Vector3(-5.0, 10.0, 0.0)

	var result_ground: bool = BattlefieldConstants.is_valid_spawn_position_for_team(pos_ground, Unit3D.Team.PLAYER)
	var result_elevated: bool = BattlefieldConstants.is_valid_spawn_position_for_team(pos_elevated, Unit3D.Team.PLAYER)

	assert_eq(result_ground, result_elevated)


func test_spawn_validation_ignores_z_coordinate() -> void:
	var pos_front := Vector3(-5.0, 0.0, -20.0)
	var pos_back := Vector3(-5.0, 0.0, 20.0)

	var result_front: bool = BattlefieldConstants.is_valid_spawn_position_for_team(pos_front, Unit3D.Team.PLAYER)
	var result_back: bool = BattlefieldConstants.is_valid_spawn_position_for_team(pos_back, Unit3D.Team.PLAYER)

	assert_eq(result_front, result_back)


## =============================================================================
## BATTLEFIELD DIMENSION CONSTANT TESTS
## =============================================================================

func test_battlefield_half_width_is_positive() -> void:
	assert_gt(BattlefieldConstants.BATTLEFIELD_HALF_WIDTH, 0.0)


func test_battlefield_half_depth_is_positive() -> void:
	assert_gt(BattlefieldConstants.BATTLEFIELD_HALF_DEPTH, 0.0)


func test_spawn_boundary_is_at_zero() -> void:
	assert_eq(BattlefieldConstants.SPAWN_BOUNDARY_X, 0.0)
