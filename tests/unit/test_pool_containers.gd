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
## HPBARMANAGER POOL CONTAINER TESTS
## =============================================================================

func test_hp_bar_manager_has_pool_container() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available, HPBarManager won't initialize")
		return

	# Trigger lazy initialization
	HPBarManager._ensure_initialized()

	assert_not_null(HPBarManager.pool_container, "HPBarManager should have pool_container")
	assert_true(
		HPBarManager.pool_container.get_parent() == HPBarManager,
		"pool_container should be child of HPBarManager"
	)


func test_hp_bar_manager_pooled_bars_in_scene_tree() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available, HPBarManager won't initialize")
		return

	# Trigger lazy initialization
	HPBarManager._ensure_initialized()

	var pool_child_count: int = HPBarManager.pool_container.get_child_count()
	var pooled_count: int = HPBarManager.bar_pool.size()

	assert_eq(
		pool_child_count,
		pooled_count,
		"Pool container child count should match pooled bar count"
	)


## =============================================================================
## PROJECTILEMANAGER POOL CONTAINER TESTS
## =============================================================================

func test_projectile_manager_has_pool_container() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available, ProjectileManager won't initialize")
		return

	# Trigger lazy initialization
	ProjectileManager._ensure_initialized()

	assert_not_null(
		ProjectileManager.pool_container,
		"ProjectileManager should have pool_container"
	)
	assert_true(
		ProjectileManager.pool_container.get_parent() == ProjectileManager,
		"pool_container should be child of ProjectileManager"
	)


func test_projectile_manager_pooled_projectiles_in_scene_tree() -> void:
	if not _is_csharp_available():
		pending("Skipped: C# not available, ProjectileManager won't initialize")
		return

	# Trigger lazy initialization
	ProjectileManager._ensure_initialized()

	var pool_child_count: int = ProjectileManager.pool_container.get_child_count()
	var total_pooled: int = 0

	for projectile_id: Variant in ProjectileManager.projectile_pools.keys():
		var pool: Array = ProjectileManager.projectile_pools[projectile_id]
		total_pooled += pool.size()

	assert_eq(
		pool_child_count,
		total_pooled,
		"Pool container child count should match total pooled projectiles"
	)
