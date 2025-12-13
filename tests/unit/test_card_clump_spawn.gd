extends GutTest

## Unit Tests for Card Clump Spawn Positioning
##
## Tests the clump radius calculation and offset generation for multi-unit spawns.


## =============================================================================
## CLUMP RADIUS CALCULATION TESTS
## =============================================================================

func test_single_unit_returns_zero_radius() -> void:
	var result: float = Card.calculate_clump_radius(1)

	assert_eq(result, 0.0)


func test_zero_units_returns_zero_radius() -> void:
	var result: float = Card.calculate_clump_radius(0)

	assert_eq(result, 0.0)


func test_two_units_returns_expected_radius() -> void:
	# Formula: BASE_RADIUS + sqrt(count) * SCALE = 1.5 + sqrt(2) * 0.5
	var expected: float = Card.CLUMP_BASE_RADIUS + sqrt(2) * Card.CLUMP_RADIUS_SCALE

	var result: float = Card.calculate_clump_radius(2)

	assert_almost_eq(result, expected, 0.001)


func test_ten_units_returns_expected_radius() -> void:
	# Formula: 1.5 + sqrt(10) * 0.5 ≈ 1.5 + 3.162 * 0.5 ≈ 3.08
	var expected: float = Card.CLUMP_BASE_RADIUS + sqrt(10) * Card.CLUMP_RADIUS_SCALE

	var result: float = Card.calculate_clump_radius(10)

	assert_almost_eq(result, expected, 0.001)


func test_radius_increases_with_unit_count() -> void:
	var radius_2: float = Card.calculate_clump_radius(2)
	var radius_5: float = Card.calculate_clump_radius(5)
	var radius_10: float = Card.calculate_clump_radius(10)

	assert_gt(radius_5, radius_2)
	assert_gt(radius_10, radius_5)


func test_radius_uses_sqrt_scaling() -> void:
	# Verify sqrt scaling: doubling units should NOT double radius
	var radius_4: float = Card.calculate_clump_radius(4)
	var radius_16: float = Card.calculate_clump_radius(16)

	# sqrt(16) / sqrt(4) = 4/2 = 2, so variable part doubles
	# radius_4 = 1.5 + 2*0.5 = 2.5
	# radius_16 = 1.5 + 4*0.5 = 3.5
	# ratio should be 3.5/2.5 = 1.4, NOT 2.0
	var ratio: float = radius_16 / radius_4
	assert_lt(ratio, 2.0, "Radius should scale with sqrt, not linearly")
	assert_gt(ratio, 1.0, "Radius should still increase")


## =============================================================================
## CLUMP OFFSET GENERATION TESTS
## =============================================================================

func test_single_unit_offset_is_zero() -> void:
	var result: Vector3 = Card.generate_clump_offset(1, 0.5, 0.5)

	assert_eq(result, Vector3.ZERO)


func test_zero_units_offset_is_zero() -> void:
	var result: Vector3 = Card.generate_clump_offset(0, 0.5, 0.5)

	assert_eq(result, Vector3.ZERO)


func test_offset_y_is_always_zero() -> void:
	# Test with various RNG values
	var offsets: Array[Vector3] = [
		Card.generate_clump_offset(5, 0.0, 1.0),
		Card.generate_clump_offset(5, 0.25, 0.5),
		Card.generate_clump_offset(5, 0.5, 0.75),
		Card.generate_clump_offset(5, 0.75, 0.25),
		Card.generate_clump_offset(5, 1.0, 1.0),
	]

	for offset: Vector3 in offsets:
		assert_eq(offset.y, 0.0, "Y component should always be 0 for ground spawning")


func test_offset_at_zero_distance_is_zero() -> void:
	# When rng_distance is 0, offset should be at origin (regardless of angle)
	var result: Vector3 = Card.generate_clump_offset(10, 0.5, 0.0)

	assert_eq(result, Vector3.ZERO)


func test_offset_at_max_distance_equals_clump_radius() -> void:
	# When rng_distance is 1.0, offset magnitude should equal clump_radius
	var unit_count: int = 10
	var expected_radius: float = Card.calculate_clump_radius(unit_count)

	var result: Vector3 = Card.generate_clump_offset(unit_count, 0.25, 1.0)
	var result_magnitude: float = Vector2(result.x, result.z).length()

	assert_almost_eq(result_magnitude, expected_radius, 0.001)


func test_offset_angle_zero_points_positive_x() -> void:
	# angle = 0 * TAU = 0 radians, cos(0) = 1, sin(0) = 0
	# So offset should be along positive X axis
	var result: Vector3 = Card.generate_clump_offset(5, 0.0, 1.0)

	assert_gt(result.x, 0.0, "Angle 0 should point positive X")
	assert_almost_eq(result.z, 0.0, 0.001, "Angle 0 should have zero Z")


func test_offset_angle_quarter_points_positive_z() -> void:
	# angle = 0.25 * TAU = PI/2 radians, cos(PI/2) ≈ 0, sin(PI/2) = 1
	# So offset should be along positive Z axis
	var result: Vector3 = Card.generate_clump_offset(5, 0.25, 1.0)

	assert_almost_eq(result.x, 0.0, 0.001, "Angle PI/2 should have zero X")
	assert_gt(result.z, 0.0, "Angle PI/2 should point positive Z")


func test_offset_angle_half_points_negative_x() -> void:
	# angle = 0.5 * TAU = PI radians, cos(PI) = -1, sin(PI) ≈ 0
	# So offset should be along negative X axis
	var result: Vector3 = Card.generate_clump_offset(5, 0.5, 1.0)

	assert_lt(result.x, 0.0, "Angle PI should point negative X")
	assert_almost_eq(result.z, 0.0, 0.001, "Angle PI should have near-zero Z")


func test_offset_within_clump_radius_bounds() -> void:
	# Test that all offsets are within the calculated clump radius
	var unit_count: int = 10
	var max_radius: float = Card.calculate_clump_radius(unit_count)

	# Test many random-like values
	for i: int in range(20):
		var rng_angle: float = float(i) / 20.0
		var rng_dist: float = float(i % 10) / 10.0
		var offset: Vector3 = Card.generate_clump_offset(unit_count, rng_angle, rng_dist)
		var magnitude: float = Vector2(offset.x, offset.z).length()

		assert_lte(magnitude, max_radius + 0.001, "Offset should not exceed clump radius")
