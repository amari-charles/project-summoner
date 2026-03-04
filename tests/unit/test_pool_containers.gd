extends GutTest

## Unit Tests for Pool Container Pattern
##
## Verifies that pooled objects are kept in the scene tree via pool_container
## nodes to avoid orphan warnings during testing.
##
## NOTE: These tests require managers to be initialized (C# must be available).
## With lazy loading, managers initialize on first use, so we trigger init first.


## =============================================================================
## C# AVAILABILITY CHECK
## =============================================================================

## Returns true if C# runtime is available
func _is_csharp_available() -> bool:
	# C# runtime availability check (SpatialGrid autoload removed)
	return true


## =============================================================================
## VFXMANAGER POOL CONTAINER TESTS
## =============================================================================

func test_vfx_manager_has_pool_container() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available, VFXManager won't initialize")
		return

	# Trigger lazy initialization
	VFXManager._ensure_initialized()

	assert_not_null(VFXManager.pool_container, "VFXManager should have pool_container")
	assert_true(
		VFXManager.pool_container.get_parent() == VFXManager,
		"pool_container should be child of VFXManager"
	)


func test_vfx_manager_pooled_effects_in_scene_tree() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available, VFXManager won't initialize")
		return

	# Trigger lazy initialization
	VFXManager._ensure_initialized()

	# Check that pooled effects are children of pool_container
	var pool_child_count: int = VFXManager.pool_container.get_child_count()
	var total_pooled: int = 0

	for effect_id: Variant in VFXManager.effect_pools.keys():
		var pool: Array = VFXManager.effect_pools[effect_id]
		total_pooled += pool.size()

	# All pooled effects should be in the pool_container
	assert_eq(
		pool_child_count,
		total_pooled,
		"Pool container child count should match total pooled effects"
	)


## =============================================================================
## PROJECTILESERVICE POOL CONTAINER TESTS (C#)
## =============================================================================
## Note: ProjectileService is now implemented in C#. Pool container testing
## requires C# unit tests. The service is tested via C# tests in
## tests/csharp/Projectiles/ProjectileServiceTest.cs
