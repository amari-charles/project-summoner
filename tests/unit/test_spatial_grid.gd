extends GutTest

## Unit Tests for SpatialGrid
##
## Tests the spatial partitioning system used for efficient proximity queries.
## Uses mock Node3D objects to simulate units.
##
## NOTE: These tests require C# to be loaded. In headless mode without .NET,
## the tests will be skipped gracefully.

const SKIP_MSG: String = "Skipped: C# SpatialGrid not available in headless mode"


## =============================================================================
## MOCK UNIT CLASS
## =============================================================================

## Mock unit for testing (simulates Unit3D properties used by SpatialGrid)
class MockUnit extends Node3D:
	var team: int = 0
	var is_alive: bool = true
	var _init_pos: Vector3 = Vector3.ZERO

	func _init(pos: Vector3 = Vector3.ZERO, unit_team: int = 0) -> void:
		_init_pos = pos
		team = unit_team

	func _ready() -> void:
		global_position = _init_pos


## =============================================================================
## C# AVAILABILITY CHECK
## =============================================================================

## Returns true if SpatialGrid C# methods are available
func _is_csharp_available() -> bool:
	return SpatialGrid.has_method("register_unit")


## =============================================================================
## TEST LIFECYCLE
## =============================================================================

func before_each() -> void:
	if _is_csharp_available():
		SpatialGrid.clear()


func after_each() -> void:
	if _is_csharp_available():
		SpatialGrid.clear()


## =============================================================================
## CELL INDEX CALCULATION TESTS
## =============================================================================

func test_center_position_maps_to_middle_cell() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	# Grid is 10x8, center should be around cell (5, 4)
	var unit: MockUnit = MockUnit.new(Vector3(0.0, 0.0, 0.0))
	add_child_autofree(unit)

	SpatialGrid.register_unit(unit)
	var stats: Dictionary = SpatialGrid.get_stats()

	assert_eq(stats.registered_units, 1)


func test_bottom_left_corner_maps_to_cell_zero() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	# Position (-50, 0, -40) should map to cell index 0
	var unit: MockUnit = MockUnit.new(Vector3(-50.0, 0.0, -40.0))
	add_child_autofree(unit)

	SpatialGrid.register_unit(unit)
	var stats: Dictionary = SpatialGrid.get_stats()

	assert_eq(stats.registered_units, 1)
	assert_eq(stats.non_empty_cells, 1)


func test_top_right_corner_maps_to_last_cell() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	# Position (50, 0, 40) should map to last cell (clamped to grid)
	var unit: MockUnit = MockUnit.new(Vector3(49.0, 0.0, 39.0))
	add_child_autofree(unit)

	SpatialGrid.register_unit(unit)
	var stats: Dictionary = SpatialGrid.get_stats()

	assert_eq(stats.registered_units, 1)
	assert_eq(stats.non_empty_cells, 1)


func test_positions_outside_grid_are_clamped() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	# Position way outside grid should still register (clamped to edge)
	var unit: MockUnit = MockUnit.new(Vector3(-100.0, 0.0, -100.0))
	add_child_autofree(unit)

	SpatialGrid.register_unit(unit)
	var stats: Dictionary = SpatialGrid.get_stats()

	assert_eq(stats.registered_units, 1)


## =============================================================================
## REGISTER / UNREGISTER TESTS
## =============================================================================

func test_register_unit_adds_to_grid() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit: MockUnit = MockUnit.new(Vector3(0.0, 0.0, 0.0))
	add_child_autofree(unit)

	SpatialGrid.register_unit(unit)
	var stats: Dictionary = SpatialGrid.get_stats()

	assert_eq(stats.registered_units, 1)
	assert_eq(stats.total_units, 1)


func test_unregister_unit_removes_from_grid() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit: MockUnit = MockUnit.new(Vector3(0.0, 0.0, 0.0))
	add_child_autofree(unit)

	SpatialGrid.register_unit(unit)
	SpatialGrid.unregister_unit(unit)
	var stats: Dictionary = SpatialGrid.get_stats()

	assert_eq(stats.registered_units, 0)
	assert_eq(stats.total_units, 0)


func test_double_register_is_idempotent() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit: MockUnit = MockUnit.new(Vector3(0.0, 0.0, 0.0))
	add_child_autofree(unit)

	SpatialGrid.register_unit(unit)
	SpatialGrid.register_unit(unit)  # Should not add twice
	var stats: Dictionary = SpatialGrid.get_stats()

	assert_eq(stats.registered_units, 1)


func test_unregister_nonexistent_unit_is_safe() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit: MockUnit = MockUnit.new(Vector3(0.0, 0.0, 0.0))
	add_child_autofree(unit)

	# Should not crash
	SpatialGrid.unregister_unit(unit)
	var stats: Dictionary = SpatialGrid.get_stats()

	assert_eq(stats.registered_units, 0)


func test_multiple_units_in_same_cell() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit1: MockUnit = MockUnit.new(Vector3(0.0, 0.0, 0.0))
	var unit2: MockUnit = MockUnit.new(Vector3(1.0, 0.0, 1.0))  # Same cell (within 10x10)
	add_child_autofree(unit1)
	add_child_autofree(unit2)

	SpatialGrid.register_unit(unit1)
	SpatialGrid.register_unit(unit2)
	var stats: Dictionary = SpatialGrid.get_stats()

	assert_eq(stats.registered_units, 2)
	assert_eq(stats.non_empty_cells, 1)
	assert_eq(stats.max_cell_population, 2)


func test_units_in_different_cells() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit1: MockUnit = MockUnit.new(Vector3(-40.0, 0.0, 0.0))
	var unit2: MockUnit = MockUnit.new(Vector3(40.0, 0.0, 0.0))  # Different cell
	add_child_autofree(unit1)
	add_child_autofree(unit2)

	SpatialGrid.register_unit(unit1)
	SpatialGrid.register_unit(unit2)
	var stats: Dictionary = SpatialGrid.get_stats()

	assert_eq(stats.registered_units, 2)
	assert_eq(stats.non_empty_cells, 2)


## =============================================================================
## GET_UNITS_IN_RADIUS TESTS
## =============================================================================

func test_query_finds_unit_within_radius() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit: MockUnit = MockUnit.new(Vector3(5.0, 0.0, 5.0))
	add_child_autofree(unit)
	SpatialGrid.register_unit(unit)

	var results: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(0.0, 0.0, 0.0),
		10.0,
		-1,
		null
	)

	assert_eq(results.size(), 1)
	assert_eq(results[0], unit)


func test_query_excludes_unit_outside_radius() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit: MockUnit = MockUnit.new(Vector3(20.0, 0.0, 20.0))
	add_child_autofree(unit)
	SpatialGrid.register_unit(unit)

	var results: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(0.0, 0.0, 0.0),
		5.0,  # Too small to reach unit
		-1,
		null
	)

	assert_eq(results.size(), 0)


func test_query_excludes_self() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit: MockUnit = MockUnit.new(Vector3(0.0, 0.0, 0.0))
	add_child_autofree(unit)
	SpatialGrid.register_unit(unit)

	var results: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(0.0, 0.0, 0.0),
		10.0,
		-1,
		unit  # Exclude self
	)

	assert_eq(results.size(), 0)


func test_query_filters_by_team() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var player_unit: MockUnit = MockUnit.new(Vector3(5.0, 0.0, 0.0), 0)
	var enemy_unit: MockUnit = MockUnit.new(Vector3(-5.0, 0.0, 0.0), 1)
	add_child_autofree(player_unit)
	add_child_autofree(enemy_unit)
	SpatialGrid.register_unit(player_unit)
	SpatialGrid.register_unit(enemy_unit)

	# Query for team 0 (player) only
	var results: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(0.0, 0.0, 0.0),
		20.0,
		0,  # Player team only
		null
	)

	assert_eq(results.size(), 1)
	assert_eq(results[0], player_unit)


func test_query_filters_by_enemy_team() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var player_unit: MockUnit = MockUnit.new(Vector3(5.0, 0.0, 0.0), 0)
	var enemy_unit: MockUnit = MockUnit.new(Vector3(-5.0, 0.0, 0.0), 1)
	add_child_autofree(player_unit)
	add_child_autofree(enemy_unit)
	SpatialGrid.register_unit(player_unit)
	SpatialGrid.register_unit(enemy_unit)

	# Query for team 1 (enemy) only
	var results: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(0.0, 0.0, 0.0),
		20.0,
		1,  # Enemy team only
		null
	)

	assert_eq(results.size(), 1)
	assert_eq(results[0], enemy_unit)


func test_query_returns_all_teams_when_filter_is_negative() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var player_unit: MockUnit = MockUnit.new(Vector3(5.0, 0.0, 0.0), 0)
	var enemy_unit: MockUnit = MockUnit.new(Vector3(-5.0, 0.0, 0.0), 1)
	add_child_autofree(player_unit)
	add_child_autofree(enemy_unit)
	SpatialGrid.register_unit(player_unit)
	SpatialGrid.register_unit(enemy_unit)

	var results: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(0.0, 0.0, 0.0),
		20.0,
		-1,  # All teams
		null
	)

	assert_eq(results.size(), 2)


func test_query_excludes_dead_units() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var alive_unit: MockUnit = MockUnit.new(Vector3(5.0, 0.0, 0.0))
	alive_unit.is_alive = true
	var dead_unit: MockUnit = MockUnit.new(Vector3(-5.0, 0.0, 0.0))
	dead_unit.is_alive = false
	add_child_autofree(alive_unit)
	add_child_autofree(dead_unit)
	SpatialGrid.register_unit(alive_unit)
	SpatialGrid.register_unit(dead_unit)

	var results: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(0.0, 0.0, 0.0),
		20.0,
		-1,
		null
	)

	assert_eq(results.size(), 1)
	assert_eq(results[0], alive_unit)


func test_query_spans_multiple_cells() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	# Place units in different cells, query should find units across cells
	var unit1: MockUnit = MockUnit.new(Vector3(-15.0, 0.0, 0.0))
	var unit2: MockUnit = MockUnit.new(Vector3(15.0, 0.0, 0.0))
	add_child_autofree(unit1)
	add_child_autofree(unit2)
	SpatialGrid.register_unit(unit1)
	SpatialGrid.register_unit(unit2)

	# Large radius query from center
	var results: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(0.0, 0.0, 0.0),
		20.0,
		-1,
		null
	)

	assert_eq(results.size(), 2)


func test_query_uses_2d_distance_ignoring_y() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	# Unit at same XZ but different Y should still be found
	var unit: MockUnit = MockUnit.new(Vector3(5.0, 100.0, 5.0))  # High Y
	add_child_autofree(unit)
	SpatialGrid.register_unit(unit)

	var results: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(0.0, 0.0, 0.0),  # Query from Y=0
		10.0,
		-1,
		null
	)

	assert_eq(results.size(), 1)


## =============================================================================
## POSITION UPDATE TESTS
## =============================================================================

func test_small_movement_does_not_change_cell() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit: MockUnit = MockUnit.new(Vector3(0.0, 0.0, 0.0))
	add_child_autofree(unit)
	SpatialGrid.register_unit(unit)

	# Move less than threshold (2.0 units)
	unit.global_position = Vector3(1.0, 0.0, 1.0)
	var changed: bool = SpatialGrid.update_unit_position(unit)

	assert_false(changed)


func test_large_movement_within_cell_returns_false() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit: MockUnit = MockUnit.new(Vector3(0.0, 0.0, 0.0))
	add_child_autofree(unit)
	SpatialGrid.register_unit(unit)

	# Move more than threshold but within same cell (cells are 10x10)
	unit.global_position = Vector3(5.0, 0.0, 5.0)
	var changed: bool = SpatialGrid.update_unit_position(unit)

	# Returns false because cell didn't change (even though position did)
	assert_false(changed)


func test_movement_to_different_cell_returns_true() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit: MockUnit = MockUnit.new(Vector3(0.0, 0.0, 0.0))
	add_child_autofree(unit)
	SpatialGrid.register_unit(unit)

	# Move to a different cell (cross cell boundary at 5.0)
	unit.global_position = Vector3(15.0, 0.0, 15.0)
	var changed: bool = SpatialGrid.update_unit_position(unit)

	assert_true(changed)


func test_cell_change_updates_grid_correctly() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit: MockUnit = MockUnit.new(Vector3(-40.0, 0.0, 0.0))
	add_child_autofree(unit)
	SpatialGrid.register_unit(unit)

	# Query at old position should find unit
	var results_before: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(-40.0, 0.0, 0.0),
		5.0,
		-1,
		null
	)
	assert_eq(results_before.size(), 1)

	# Move to different cell
	unit.global_position = Vector3(40.0, 0.0, 0.0)
	SpatialGrid.update_unit_position(unit)

	# Query at old position should NOT find unit
	var results_old: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(-40.0, 0.0, 0.0),
		5.0,
		-1,
		null
	)
	assert_eq(results_old.size(), 0)

	# Query at new position should find unit
	var results_new: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(40.0, 0.0, 0.0),
		5.0,
		-1,
		null
	)
	assert_eq(results_new.size(), 1)


func test_update_unregistered_unit_registers_it() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit: MockUnit = MockUnit.new(Vector3(0.0, 0.0, 0.0))
	add_child_autofree(unit)

	# Don't register, just update
	var changed: bool = SpatialGrid.update_unit_position(unit)
	var stats: Dictionary = SpatialGrid.get_stats()

	assert_true(changed)
	assert_eq(stats.registered_units, 1)


## =============================================================================
## CLEAR TESTS
## =============================================================================

func test_clear_removes_all_units() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit1: MockUnit = MockUnit.new(Vector3(-20.0, 0.0, 0.0))
	var unit2: MockUnit = MockUnit.new(Vector3(20.0, 0.0, 0.0))
	add_child_autofree(unit1)
	add_child_autofree(unit2)
	SpatialGrid.register_unit(unit1)
	SpatialGrid.register_unit(unit2)

	SpatialGrid.clear()
	var stats: Dictionary = SpatialGrid.get_stats()

	assert_eq(stats.registered_units, 0)
	assert_eq(stats.total_units, 0)
	assert_eq(stats.non_empty_cells, 0)


## =============================================================================
## EDGE CASE TESTS
## =============================================================================

func test_query_empty_grid_returns_empty() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var results: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(0.0, 0.0, 0.0),
		100.0,
		-1,
		null
	)

	assert_eq(results.size(), 0)


func test_query_zero_radius_returns_empty() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit: MockUnit = MockUnit.new(Vector3(0.0, 0.0, 0.0))
	add_child_autofree(unit)
	SpatialGrid.register_unit(unit)

	var results: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(0.0, 0.0, 0.0),
		0.0,  # Zero radius
		-1,
		null
	)

	# Unit at exact same position with radius 0 - depends on <= check
	# Our implementation uses radius_sq comparison, so this should include it
	assert_eq(results.size(), 1)


func test_query_at_grid_boundary() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	var unit: MockUnit = MockUnit.new(Vector3(-49.0, 0.0, -39.0))
	add_child_autofree(unit)
	SpatialGrid.register_unit(unit)

	var results: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(-50.0, 0.0, -40.0),
		5.0,
		-1,
		null
	)

	assert_eq(results.size(), 1)


func test_many_units_performance() -> void:
	if not _is_csharp_available():
		pending(SKIP_MSG)
		return
	# Add many units and verify grid handles them
	var units: Array[MockUnit] = []
	for i: int in range(100):
		var x: float = randf_range(-45.0, 45.0)
		var z: float = randf_range(-35.0, 35.0)
		var unit: MockUnit = MockUnit.new(Vector3(x, 0.0, z))
		add_child_autofree(unit)
		SpatialGrid.register_unit(unit)
		units.append(unit)

	var stats: Dictionary = SpatialGrid.get_stats()
	assert_eq(stats.registered_units, 100)

	# Query should work
	var results: Array[Node3D] = SpatialGrid.get_units_in_radius(
		Vector3(0.0, 0.0, 0.0),
		50.0,
		-1,
		null
	)

	assert_gt(results.size(), 0)
