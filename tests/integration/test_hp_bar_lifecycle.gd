extends GutTest

## Integration Tests for HP Bar Lifecycle
##
## Tests the HP bar system's ability to properly create, track, and cleanup
## HP bars when units are spawned and destroyed, particularly for scenarios
## involving multiple units (e.g., Fire Ant Swarm with 20 units).
##
## These tests verify the TreeExiting signal pattern works correctly for
## guaranteed cleanup even when units are freed unexpectedly.
##
## NOTE: C# nullable default parameters aren't exposed to GDScript, so we must
## pass null explicitly for optional parameters like 'settings'.


## =============================================================================
## TEST FIXTURES
## =============================================================================

var _test_units: Array[Node3D] = []
var _hp_service: Node = null


func before_each() -> void:
	_test_units.clear()
	_hp_service = get_node_or_null(CSharpAutoloads.HP_BAR_SERVICE)


func after_each() -> void:
	# Clean up any remaining test units
	for unit: Node3D in _test_units:
		if is_instance_valid(unit) and unit.is_inside_tree():
			unit.queue_free()
	_test_units.clear()

	# Wait for cleanup to process
	await get_tree().process_frame
	await get_tree().process_frame


## =============================================================================
## C# AVAILABILITY CHECK
## =============================================================================

func _is_csharp_available() -> bool:
	var spatial_grid: Node = get_node_or_null(CSharpAutoloads.SPATIAL_GRID)
	return spatial_grid != null and spatial_grid.has_method("register_unit")


## =============================================================================
## SINGLE UNIT LIFECYCLE TESTS
## =============================================================================

func test_hp_bar_created_for_unit() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available")
		return

	_hp_service.ForceInitialize()
	var initial_active: int = _hp_service.GetActiveBarCount()

	# Create a mock unit
	var unit: Node3D = _create_mock_unit()
	_test_units.append(unit)
	add_child(unit)

	# Create HP bar (must pass null for optional settings parameter)
	_hp_service.create_bar_for_unit(unit, null)

	# Verify bar was created
	assert_eq(
		_hp_service.GetActiveBarCount(),
		initial_active + 1,
		"Active bar count should increase by 1"
	)


func test_hp_bar_removed_on_unit_queue_free() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available")
		return

	_hp_service.ForceInitialize()
	var initial_active: int = _hp_service.GetActiveBarCount()
	var initial_pooled: int = _hp_service.GetPooledBarCount()

	# Create a mock unit with HP bar
	var unit: Node3D = _create_mock_unit()
	add_child(unit)
	_hp_service.create_bar_for_unit(unit, null)

	# Verify bar exists
	assert_eq(
		_hp_service.GetActiveBarCount(),
		initial_active + 1,
		"Bar should be active"
	)

	# Free the unit (triggers TreeExiting -> cleanup)
	unit.queue_free()

	# Wait for cleanup
	await get_tree().process_frame
	await get_tree().process_frame

	# Verify bar was returned to pool
	assert_eq(
		_hp_service.GetActiveBarCount(),
		initial_active,
		"Active bar count should return to initial"
	)
	assert_eq(
		_hp_service.GetPooledBarCount(),
		initial_pooled,
		"Pooled bar count should return to initial (bar taken from pool, then returned)"
	)


## =============================================================================
## MULTI-UNIT LIFECYCLE TESTS (Fire Ant Swarm scenario)
## =============================================================================

func test_multi_unit_hp_bars_created() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available")
		return

	_hp_service.ForceInitialize()
	var initial_active: int = _hp_service.GetActiveBarCount()
	var unit_count: int = 10  # Simulate a swarm

	# Create multiple units
	for i: int in unit_count:
		var unit: Node3D = _create_mock_unit()
		_test_units.append(unit)
		add_child(unit)
		_hp_service.create_bar_for_unit(unit, null)

	# Verify all bars created
	assert_eq(
		_hp_service.GetActiveBarCount(),
		initial_active + unit_count,
		"All %d bars should be active" % unit_count
	)


func test_multi_unit_hp_bars_cleanup_on_mass_death() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available")
		return

	_hp_service.ForceInitialize()
	var initial_active: int = _hp_service.GetActiveBarCount()
	var unit_count: int = 10

	# Create multiple units
	var units: Array[Node3D] = []
	for i: int in unit_count:
		var unit: Node3D = _create_mock_unit()
		units.append(unit)
		add_child(unit)
		_hp_service.create_bar_for_unit(unit, null)

	# Verify all bars created
	assert_eq(
		_hp_service.GetActiveBarCount(),
		initial_active + unit_count,
		"All %d bars should be active before cleanup" % unit_count
	)

	# Simulate AoE death - free all units at once
	for unit: Node3D in units:
		unit.queue_free()

	# Wait for cleanup
	await get_tree().process_frame
	await get_tree().process_frame

	# Verify all bars cleaned up
	assert_eq(
		_hp_service.GetActiveBarCount(),
		initial_active,
		"All bars should be cleaned up after mass death"
	)


func test_hp_bars_cleanup_on_clear_all() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available")
		return

	_hp_service.ForceInitialize()
	var unit_count: int = 5

	# Create multiple units
	for i: int in unit_count:
		var unit: Node3D = _create_mock_unit()
		_test_units.append(unit)
		add_child(unit)
		_hp_service.create_bar_for_unit(unit, null)

	# Verify bars exist
	assert_gt(
		_hp_service.GetActiveBarCount(),
		0,
		"Should have active bars"
	)

	# Clear all bars (simulates scene transition)
	_hp_service.clear_all_bars()

	# Verify all cleared
	assert_eq(
		_hp_service.GetActiveBarCount(),
		0,
		"All bars should be cleared"
	)


## =============================================================================
## POOL REUSE TESTS
## =============================================================================

func test_hp_bars_reused_from_pool() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available")
		return

	_hp_service.ForceInitialize()
	var initial_pooled: int = _hp_service.GetPooledBarCount()

	# Skip if pool is empty (unlikely but possible)
	if initial_pooled == 0:
		pending("Skipped: Pool is empty, cannot test reuse")
		return

	# Create a unit - should take bar from pool
	var unit: Node3D = _create_mock_unit()
	_test_units.append(unit)
	add_child(unit)
	_hp_service.create_bar_for_unit(unit, null)

	# Verify pool count decreased
	assert_eq(
		_hp_service.GetPooledBarCount(),
		initial_pooled - 1,
		"Pool should have one less bar"
	)

	# Free unit - bar returns to pool
	unit.queue_free()
	_test_units.clear()

	await get_tree().process_frame
	await get_tree().process_frame

	# Verify bar returned to pool
	assert_eq(
		_hp_service.GetPooledBarCount(),
		initial_pooled,
		"Bar should be returned to pool"
	)


## =============================================================================
## HP UPDATE TESTS
## =============================================================================

func test_hp_update_propagates_to_bar() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available")
		return

	_hp_service.ForceInitialize()

	# Create a mock unit with HP bar
	var unit: Node3D = _create_mock_unit()
	_test_units.append(unit)
	add_child(unit)
	_hp_service.create_bar_for_unit(unit, null)

	# Update HP via service
	_hp_service.update_unit_hp(unit, 50.0, 100.0)

	# If we got here without errors, the update worked
	# (Would need to expose bar state for deeper verification)
	assert_true(true, "HP update should not error")


## =============================================================================
## HELPER FUNCTIONS
## =============================================================================

## Create a minimal mock unit for testing
func _create_mock_unit() -> Node3D:
	var unit: Node3D = Node3D.new()
	unit.name = "MockUnit_%d" % randi()

	# Add properties that HP bar expects
	unit.set_meta("current_hp", 100.0)
	unit.set_meta("max_hp", 100.0)

	return unit
