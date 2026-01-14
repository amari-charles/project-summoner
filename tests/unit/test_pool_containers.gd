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
	var spatial_grid: Node = get_node_or_null("/root/SpatialGrid")
	return spatial_grid != null and spatial_grid.has_method("register_unit")


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
## HPBARSERVICE POOL CONTAINER TESTS (C#)
## =============================================================================

func test_hp_bar_service_has_pool_container() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available, HPBarService won't initialize")
		return

	# Trigger lazy initialization
	HPBarService.ForceInitialize()

	var pool_container: Node3D = HPBarService.GetPoolContainer()
	assert_not_null(pool_container, "HPBarService should have pool_container")
	assert_true(
		pool_container.get_parent() == HPBarService,
		"pool_container should be child of HPBarService"
	)


func test_hp_bar_service_pooled_bars_in_scene_tree() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available, HPBarService won't initialize")
		return

	# Trigger lazy initialization
	HPBarService.ForceInitialize()

	var pool_container: Node3D = HPBarService.GetPoolContainer()
	var pool_child_count: int = pool_container.get_child_count()
	var pooled_count: int = HPBarService.GetPooledBarCount()

	assert_eq(
		pool_child_count,
		pooled_count,
		"Pool container child count should match pooled bar count"
	)


## =============================================================================
## PROJECTILESERVICE POOL CONTAINER TESTS (C#)
## =============================================================================
## Note: ProjectileService is now implemented in C#. Pool container testing
## requires C# unit tests or public API additions to ProjectileService.

func test_projectile_service_exists() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available, ProjectileService won't initialize")
		return

	assert_not_null(
		ProjectileService,
		"ProjectileService autoload should exist"
	)
