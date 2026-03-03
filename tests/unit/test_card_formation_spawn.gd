extends GutTest

## Unit Tests for Card Formation Spawn Positioning
##
## Tests the staggered row formation system for multi-unit spawns.


## =============================================================================
## SINGLE/ZERO UNIT TESTS
## =============================================================================

func test_single_unit_returns_zero_offset() -> void:
	var result: Vector3 = CardFormation.generate_formation_offset(0, 1)

	assert_eq(result, Vector3.ZERO)


func test_zero_units_returns_zero_offset() -> void:
	var result: Vector3 = CardFormation.generate_formation_offset(0, 0)

	assert_eq(result, Vector3.ZERO)


## =============================================================================
## FORMATION GRID DIMENSION TESTS
## =============================================================================

func test_two_units_form_two_rows() -> void:
	# 2 units with 2-row preference creates 2 rows of 1 column each (depth formation)
	var offset_0: Vector3 = CardFormation.generate_formation_offset(0, 2)
	var offset_1: Vector3 = CardFormation.generate_formation_offset(1, 2)

	# Units should be in different rows (different X offset)
	assert_ne(offset_0.x, offset_1.x, "Units should be in different rows for depth")

	# Formation should be centered (one positive X, one negative X)
	var x_sum: float = offset_0.x + offset_1.x
	assert_almost_eq(x_sum, 0.0, 0.01, "Formation should be centered around origin")


func test_twelve_units_form_two_rows() -> void:
	# 12 units (within FORMATION_TWO_ROW_MAX) should form 2 rows of 6 columns
	var rows_seen: Dictionary = {}

	for i: int in range(12):
		var offset: Vector3 = CardFormation.generate_formation_offset(i, 12)
		# Round X to detect distinct rows
		var row_key: float = snappedf(offset.x, 0.1)
		rows_seen[row_key] = true

	assert_eq(rows_seen.size(), 2, "12 units should form exactly 2 rows")


func test_large_swarm_uses_more_rows() -> void:
	# 25 units (exceeds FORMATION_TWO_ROW_MAX) should use more than 2 rows
	var rows_seen: Dictionary = {}

	for i: int in range(25):
		var offset: Vector3 = CardFormation.generate_formation_offset(i, 25)
		var row_key: float = snappedf(offset.x, 0.1)
		rows_seen[row_key] = true

	assert_gt(rows_seen.size(), 2, "25 units should use more than 2 rows")


## =============================================================================
## FORMATION CENTERING TESTS
## =============================================================================

func test_formation_centered_on_origin() -> void:
	# Sum of all offsets should be approximately zero (centered)
	var sum: Vector3 = Vector3.ZERO
	var unit_count: int = 12

	for i: int in range(unit_count):
		sum += CardFormation.generate_formation_offset(i, unit_count)

	# Average position should be near origin
	var avg: Vector3 = sum / float(unit_count)
	assert_almost_eq(avg.x, 0.0, 0.5, "Formation X center should be near origin")
	assert_almost_eq(avg.z, 0.0, 0.5, "Formation Z center should be near origin")


func test_y_offset_always_zero() -> void:
	# All offsets should have Y = 0 (ground spawning)
	for i: int in range(12):
		var offset: Vector3 = CardFormation.generate_formation_offset(i, 12)
		assert_eq(offset.y, 0.0, "Y offset should always be 0 for ground spawning")


## =============================================================================
## STAGGER OFFSET TESTS
## =============================================================================

func test_alternating_rows_have_stagger() -> void:
	# Row 0 and Row 1 should have different Z patterns due to stagger
	# With 12 units in 2 rows of 6, indices 0-5 are row 0, 6-11 are row 1

	var row0_first_z: float = CardFormation.generate_formation_offset(0, 12).z
	var row1_first_z: float = CardFormation.generate_formation_offset(6, 12).z

	# Row 1 should be offset by DEFAULT_FORMATION_ROW_OFFSET * DEFAULT_FORMATION_SPACING
	var expected_stagger: float = CardFormation.DEFAULT_FORMATION_ROW_OFFSET * CardFormation.DEFAULT_FORMATION_SPACING

	# The difference in starting Z position should equal the stagger
	var z_diff: float = row1_first_z - row0_first_z
	assert_almost_eq(z_diff, expected_stagger, 0.01, "Row 1 should be staggered from row 0")


## =============================================================================
## SPACING TESTS
## =============================================================================

func test_units_spaced_by_formation_spacing() -> void:
	# Adjacent units in same row should be DEFAULT_FORMATION_SPACING apart
	var offset_0: Vector3 = CardFormation.generate_formation_offset(0, 12)
	var offset_1: Vector3 = CardFormation.generate_formation_offset(1, 12)

	var z_distance: float = absf(offset_1.z - offset_0.z)
	assert_almost_eq(z_distance, CardFormation.DEFAULT_FORMATION_SPACING, 0.01, "Adjacent units should be DEFAULT_FORMATION_SPACING apart")


func test_rows_spaced_by_formation_spacing() -> void:
	# Units in different rows should have X positions DEFAULT_FORMATION_SPACING apart
	# Index 0 is row 0, index 6 is row 1 (for 12 units)
	var offset_row0: Vector3 = CardFormation.generate_formation_offset(0, 12)
	var offset_row1: Vector3 = CardFormation.generate_formation_offset(6, 12)

	var x_distance: float = absf(offset_row1.x - offset_row0.x)
	assert_almost_eq(x_distance, CardFormation.DEFAULT_FORMATION_SPACING, 0.01, "Rows should be DEFAULT_FORMATION_SPACING apart")


## =============================================================================
## DETERMINISM TESTS
## =============================================================================

func test_formation_is_deterministic() -> void:
	# Same inputs should always produce same outputs (no randomness)
	var offsets_first: Array[Vector3] = []
	var offsets_second: Array[Vector3] = []

	for i: int in range(12):
		offsets_first.append(CardFormation.generate_formation_offset(i, 12))

	for i: int in range(12):
		offsets_second.append(CardFormation.generate_formation_offset(i, 12))

	for i: int in range(12):
		assert_eq(offsets_first[i], offsets_second[i], "Formation should be deterministic")


## =============================================================================
## EDGE CASE TESTS
## =============================================================================

func test_odd_unit_count_handled() -> void:
	# 7 units should form 2 rows: one with 4, one with 3
	var offsets: Array[Vector3] = []
	for i: int in range(7):
		offsets.append(CardFormation.generate_formation_offset(i, 7))

	# Should not crash and all offsets should be valid
	for offset: Vector3 in offsets:
		assert_false(is_nan(offset.x), "X should not be NaN")
		assert_false(is_nan(offset.z), "Z should not be NaN")


func test_twenty_units_still_uses_two_rows() -> void:
	# Exactly FORMATION_TWO_ROW_MAX (20) units should still use 2 rows (boundary condition)
	var rows_seen: Dictionary = {}

	for i: int in range(20):
		var offset: Vector3 = CardFormation.generate_formation_offset(i, 20)
		var row_key: float = snappedf(offset.x, 0.1)
		rows_seen[row_key] = true

	assert_eq(rows_seen.size(), 2, "20 units should use exactly 2 rows")


func test_twentyone_units_uses_more_rows() -> void:
	# 21 units (exceeds FORMATION_TWO_ROW_MAX) should trigger more than 2 rows
	var rows_seen: Dictionary = {}

	for i: int in range(21):
		var offset: Vector3 = CardFormation.generate_formation_offset(i, 21)
		var row_key: float = snappedf(offset.x, 0.1)
		rows_seen[row_key] = true

	assert_gt(rows_seen.size(), 2, "21 units should use more than 2 rows")
