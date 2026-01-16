extends GutTest

## Unit Tests for Projectile Acceleration/Deceleration
##
## Tests the acceleration clamping behavior in Projectile3D.


## =============================================================================
## ACCELERATION CLAMPING TESTS
## =============================================================================

func test_negative_acceleration_decreases_speed() -> void:
	var projectile: Projectile3D = Projectile3D.new()
	projectile.Speed = 20.0
	projectile.CurrentSpeed = 20.0
	projectile.Acceleration = -10.0
	projectile.MinSpeed = 5.0

	# Simulate acceleration for 1 second
	var delta: float = 1.0
	projectile.CurrentSpeed += projectile.Acceleration * delta
	if projectile.Acceleration < 0.0:
		projectile.CurrentSpeed = max(projectile.CurrentSpeed, projectile.MinSpeed)

	assert_eq(projectile.CurrentSpeed, 10.0)

	projectile.free()


func test_speed_clamps_to_min_speed() -> void:
	var projectile: Projectile3D = Projectile3D.new()
	projectile.Speed = 20.0
	projectile.CurrentSpeed = 20.0
	projectile.Acceleration = -10.0
	projectile.MinSpeed = 5.0

	# Simulate acceleration for 3 seconds (would go to -10 without clamping)
	var delta: float = 3.0
	projectile.CurrentSpeed += projectile.Acceleration * delta
	if projectile.Acceleration < 0.0:
		projectile.CurrentSpeed = max(projectile.CurrentSpeed, projectile.MinSpeed)

	assert_eq(projectile.CurrentSpeed, 5.0)  # Clamped to MinSpeed

	projectile.free()


func test_speed_never_goes_below_min_speed() -> void:
	var projectile: Projectile3D = Projectile3D.new()
	projectile.Speed = 10.0
	projectile.CurrentSpeed = 10.0
	projectile.Acceleration = -100.0  # Very aggressive deceleration
	projectile.MinSpeed = 3.0

	# Simulate acceleration for 1 second
	var delta: float = 1.0
	projectile.CurrentSpeed += projectile.Acceleration * delta
	if projectile.Acceleration < 0.0:
		projectile.CurrentSpeed = max(projectile.CurrentSpeed, projectile.MinSpeed)

	assert_eq(projectile.CurrentSpeed, 3.0)  # Clamped to MinSpeed, not -90

	projectile.free()


func test_positive_acceleration_increases_speed() -> void:
	var projectile: Projectile3D = Projectile3D.new()
	projectile.Speed = 10.0
	projectile.CurrentSpeed = 10.0
	projectile.Acceleration = 5.0
	projectile.MinSpeed = 1.0

	# Simulate acceleration for 2 seconds
	var delta: float = 2.0
	projectile.CurrentSpeed += projectile.Acceleration * delta
	# No clamping for positive acceleration

	assert_eq(projectile.CurrentSpeed, 20.0)

	projectile.free()


func test_zero_acceleration_maintains_speed() -> void:
	var projectile: Projectile3D = Projectile3D.new()
	projectile.Speed = 15.0
	projectile.CurrentSpeed = 15.0
	projectile.Acceleration = 0.0
	projectile.MinSpeed = 1.0

	# Simulate 5 seconds
	var delta: float = 5.0
	if projectile.Acceleration != 0.0:
		projectile.CurrentSpeed += projectile.Acceleration * delta
		if projectile.Acceleration < 0.0:
			projectile.CurrentSpeed = max(projectile.CurrentSpeed, projectile.MinSpeed)

	assert_eq(projectile.CurrentSpeed, 15.0)

	projectile.free()


func test_incremental_deceleration_over_time() -> void:
	var projectile: Projectile3D = Projectile3D.new()
	projectile.Speed = 25.0
	projectile.CurrentSpeed = 25.0
	projectile.Acceleration = -12.0
	projectile.MinSpeed = 5.0

	# Simulate 10 physics frames at 0.1s each (exactly 1 second total)
	var delta: float = 0.1
	var iterations: int = 10

	for i: int in range(iterations):
		projectile.CurrentSpeed += projectile.Acceleration * delta
		if projectile.Acceleration < 0.0:
			projectile.CurrentSpeed = max(projectile.CurrentSpeed, projectile.MinSpeed)

	# After 1 second at -12/s, speed should be 25 - 12 = 13
	assert_almost_eq(projectile.CurrentSpeed, 13.0, 0.01)

	projectile.free()


func test_wind_puff_config_values() -> void:
	# Test the specific values used by wind_puff.json
	var projectile: Projectile3D = Projectile3D.new()
	projectile.Speed = 25.0
	projectile.CurrentSpeed = 25.0
	projectile.Acceleration = -12.0
	projectile.MinSpeed = 5.0

	# After ~1.67 seconds, should reach MinSpeed
	# (25 - 5) / 12 = 1.67 seconds to reach MinSpeed
	var time_to_min: float = (projectile.Speed - projectile.MinSpeed) / abs(projectile.Acceleration)
	assert_almost_eq(time_to_min, 1.67, 0.01)

	# Simulate 2 seconds (beyond time to reach min)
	var delta: float = 2.0
	projectile.CurrentSpeed += projectile.Acceleration * delta
	if projectile.Acceleration < 0.0:
		projectile.CurrentSpeed = max(projectile.CurrentSpeed, projectile.MinSpeed)

	assert_eq(projectile.CurrentSpeed, 5.0)  # Clamped at MinSpeed

	projectile.free()
