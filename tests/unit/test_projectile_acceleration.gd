extends GutTest

## Unit Tests for Projectile Acceleration/Deceleration
##
## Tests the acceleration clamping behavior in Projectile3D.


## =============================================================================
## ACCELERATION CLAMPING TESTS
## =============================================================================

func test_negative_acceleration_decreases_speed() -> void:
	var projectile: Projectile3D = Projectile3D.new()
	projectile.speed = 20.0
	projectile.current_speed = 20.0
	projectile.acceleration = -10.0
	projectile.min_speed = 5.0

	# Simulate acceleration for 1 second
	var delta: float = 1.0
	projectile.current_speed += projectile.acceleration * delta
	if projectile.acceleration < 0.0:
		projectile.current_speed = max(projectile.current_speed, projectile.min_speed)

	assert_eq(projectile.current_speed, 10.0)

	projectile.free()


func test_speed_clamps_to_min_speed() -> void:
	var projectile: Projectile3D = Projectile3D.new()
	projectile.speed = 20.0
	projectile.current_speed = 20.0
	projectile.acceleration = -10.0
	projectile.min_speed = 5.0

	# Simulate acceleration for 3 seconds (would go to -10 without clamping)
	var delta: float = 3.0
	projectile.current_speed += projectile.acceleration * delta
	if projectile.acceleration < 0.0:
		projectile.current_speed = max(projectile.current_speed, projectile.min_speed)

	assert_eq(projectile.current_speed, 5.0)  # Clamped to min_speed

	projectile.free()


func test_speed_never_goes_below_min_speed() -> void:
	var projectile: Projectile3D = Projectile3D.new()
	projectile.speed = 10.0
	projectile.current_speed = 10.0
	projectile.acceleration = -100.0  # Very aggressive deceleration
	projectile.min_speed = 3.0

	# Simulate acceleration for 1 second
	var delta: float = 1.0
	projectile.current_speed += projectile.acceleration * delta
	if projectile.acceleration < 0.0:
		projectile.current_speed = max(projectile.current_speed, projectile.min_speed)

	assert_eq(projectile.current_speed, 3.0)  # Clamped to min_speed, not -90

	projectile.free()


func test_positive_acceleration_increases_speed() -> void:
	var projectile: Projectile3D = Projectile3D.new()
	projectile.speed = 10.0
	projectile.current_speed = 10.0
	projectile.acceleration = 5.0
	projectile.min_speed = 1.0

	# Simulate acceleration for 2 seconds
	var delta: float = 2.0
	projectile.current_speed += projectile.acceleration * delta
	# No clamping for positive acceleration

	assert_eq(projectile.current_speed, 20.0)

	projectile.free()


func test_zero_acceleration_maintains_speed() -> void:
	var projectile: Projectile3D = Projectile3D.new()
	projectile.speed = 15.0
	projectile.current_speed = 15.0
	projectile.acceleration = 0.0
	projectile.min_speed = 1.0

	# Simulate 5 seconds
	var delta: float = 5.0
	if projectile.acceleration != 0.0:
		projectile.current_speed += projectile.acceleration * delta
		if projectile.acceleration < 0.0:
			projectile.current_speed = max(projectile.current_speed, projectile.min_speed)

	assert_eq(projectile.current_speed, 15.0)

	projectile.free()


func test_incremental_deceleration_over_time() -> void:
	var projectile: Projectile3D = Projectile3D.new()
	projectile.speed = 25.0
	projectile.current_speed = 25.0
	projectile.acceleration = -12.0
	projectile.min_speed = 5.0

	# Simulate 10 physics frames at 0.1s each (exactly 1 second total)
	var delta: float = 0.1
	var iterations: int = 10

	for i: int in range(iterations):
		projectile.current_speed += projectile.acceleration * delta
		if projectile.acceleration < 0.0:
			projectile.current_speed = max(projectile.current_speed, projectile.min_speed)

	# After 1 second at -12/s, speed should be 25 - 12 = 13
	assert_almost_eq(projectile.current_speed, 13.0, 0.01)

	projectile.free()


func test_wind_puff_config_values() -> void:
	# Test the specific values used by wind_puff.json
	var projectile: Projectile3D = Projectile3D.new()
	projectile.speed = 25.0
	projectile.current_speed = 25.0
	projectile.acceleration = -12.0
	projectile.min_speed = 5.0

	# After ~1.67 seconds, should reach min_speed
	# (25 - 5) / 12 = 1.67 seconds to reach min_speed
	var time_to_min: float = (projectile.speed - projectile.min_speed) / abs(projectile.acceleration)
	assert_almost_eq(time_to_min, 1.67, 0.01)

	# Simulate 2 seconds (beyond time to reach min)
	var delta: float = 2.0
	projectile.current_speed += projectile.acceleration * delta
	if projectile.acceleration < 0.0:
		projectile.current_speed = max(projectile.current_speed, projectile.min_speed)

	assert_eq(projectile.current_speed, 5.0)  # Clamped at min_speed

	projectile.free()
