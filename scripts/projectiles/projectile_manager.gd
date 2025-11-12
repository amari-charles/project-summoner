extends Node

## Centralized projectile spawning and pooling system
## Usage: ProjectileManager.spawn_projectile("arrow", attacker, target, damage)
## Autoload as: /root/ProjectileManager

## Pool settings
const INITIAL_POOL_SIZE: int = 15
const MAX_POOL_SIZE: int = 50

## Pools per projectile type
var projectile_pools: Dictionary = {}  ## projectile_id -> Array[Projectile3D]
var active_projectiles: Dictionary = {}  ## projectile_id -> Array[Projectile3D]

## Container
var projectiles_container: Node3D = null

## Base projectile scene
var base_projectile_scene: PackedScene = null

func _ready() -> void:
	print("ProjectileManager: Initializing...")

	# Create container
	projectiles_container = Node3D.new()
	projectiles_container.name = "ProjectilesContainer"
	add_child(projectiles_container)

	# Load base projectile scene (or will instantiate from script)
	_load_projectile_scene()

	# Pre-instantiate pools for common projectiles
	_init_pools()

	print("ProjectileManager: Initialized")

func _load_projectile_scene() -> void:
	var scene_path: String = "res://scenes/projectiles/base_projectile_3d.tscn"
	if ResourceLoader.exists(scene_path):
		base_projectile_scene = load(scene_path)
	else:
		push_warning("ProjectileManager: Base projectile scene not found at '%s', will instantiate from script" % scene_path)

func _init_pools() -> void:
	# Pre-instantiate pools for projectiles defined in ContentCatalog
	if not ContentCatalog:
		return

	for projectile_id in ContentCatalog.projectiles.keys():
		var proj_data: Variant = ContentCatalog.projectiles[projectile_id]
		if proj_data is ProjectileData:
			_create_pool_for(projectile_id, 5)  # Smaller initial pool per type

## Create pool for a specific projectile type
func _create_pool_for(projectile_id: String, pool_size: int) -> void:
	if projectile_pools.has(projectile_id):
		return  # Already exists

	projectile_pools[projectile_id] = []
	active_projectiles[projectile_id] = []

	for i in range(pool_size):
		var projectile: Projectile3D = _instantiate_projectile()
		if projectile:
			projectile.is_pooled = true
			projectile.reset()
			projectile_pools[projectile_id].append(projectile)

	print("ProjectileManager: Created pool of %d for '%s'" % [pool_size, projectile_id])

func _instantiate_projectile() -> Projectile3D:
	if base_projectile_scene:
		return base_projectile_scene.instantiate() as Projectile3D
	else:
		# Fallback: create from script
		return Projectile3D.new()

## Spawn a projectile
func spawn_projectile(
	projectile_id: String,
	source: Node3D,
	target: Node3D,
	damage: float,
	damage_type: String = "physical",
	options: Dictionary = {}
) -> Projectile3D:
	# Get projectile data
	if not ContentCatalog.has_projectile(projectile_id):
		push_error("ProjectileManager: Projectile '%s' not found in ContentCatalog" % projectile_id)
		return null

	var proj_data: ProjectileData = ContentCatalog.get_projectile(projectile_id)

	# Get projectile from pool or create new
	var projectile: Projectile3D = _get_from_pool(projectile_id)
	if not projectile:
		push_error("ProjectileManager: Failed to get projectile from pool")
		return null

	# Load configuration from data
	projectile.load_from_data(proj_data)

	# Initialize with runtime data
	var init_data: Dictionary = {
		"source": source,
		"target": target,
		"damage": damage,
		"damage_type": damage_type,
		"team": source.team if "team" in source else -1
	}

	# Apply options
	if options.has("start_position"):
		init_data["start_position"] = options.start_position
	if options.has("direction"):
		init_data["direction"] = options.direction
	if options.has("target_position"):
		init_data["target_position"] = options.target_position

	# Add to scene first (required for global_transform access in initialize)
	projectiles_container.add_child(projectile)

	# Initialize after being added to tree
	projectile.initialize(init_data)

	# Track active projectile
	if not active_projectiles.has(projectile_id):
		active_projectiles[projectile_id] = []
	active_projectiles[projectile_id].append(projectile)

	# Connect signals
	if not projectile.projectile_expired.is_connected(_on_projectile_expired):
		# Signal already passes projectile, only bind projectile_id
		projectile.projectile_expired.connect(_on_projectile_expired.bind(projectile_id))

	return projectile

## Get projectile from pool
func _get_from_pool(projectile_id: String) -> Projectile3D:
	# Create pool if it doesn't exist
	if not projectile_pools.has(projectile_id):
		_create_pool_for(projectile_id, 5)

	var pool: Array = projectile_pools[projectile_id]
	if pool.size() > 0:
		var projectile: Projectile3D = pool.pop_back()
		projectile.reset()
		return projectile

	# Pool exhausted, create new
	var projectile: Projectile3D = _instantiate_projectile()
	projectile.is_pooled = true
	return projectile

## Return projectile to pool
func _return_to_pool(projectile_id: String, projectile: Projectile3D) -> void:
	# Remove from active
	if active_projectiles.has(projectile_id):
		active_projectiles[projectile_id].erase(projectile)

	# Remove from scene
	if projectile.get_parent():
		projectile.get_parent().remove_child(projectile)

	# Return to pool if not full
	if projectile_pools[projectile_id].size() < MAX_POOL_SIZE:
		projectile.reset()
		projectile_pools[projectile_id].append(projectile)
	else:
		# Pool full, destroy
		projectile.queue_free()

## Signal handler for projectile expiration
## Note: Signal emits projectile first, then bind adds projectile_id
func _on_projectile_expired(projectile: Projectile3D, projectile_id: String) -> void:
	_return_to_pool(projectile_id, projectile)

## Clear all active projectiles (for scene transitions)
func clear_all_projectiles() -> void:
	for projectile_id in active_projectiles.keys():
		for projectile in active_projectiles[projectile_id]:
			if projectile.get_parent():
				projectile.get_parent().remove_child(projectile)
			_return_to_pool(projectile_id, projectile)

	active_projectiles.clear()

## Clear all pools (forces reload of visuals on next init)
func clear_all_pools() -> void:
	# Free all pooled projectiles
	for projectile_id in projectile_pools.keys():
		for projectile in projectile_pools[projectile_id]:
			if is_instance_valid(projectile):
				projectile.queue_free()

	# Clear all active projectiles
	for projectile_id in active_projectiles.keys():
		for projectile in active_projectiles[projectile_id]:
			if is_instance_valid(projectile):
				projectile.queue_free()

	projectile_pools.clear()
	active_projectiles.clear()
	print("ProjectileManager: Cleared all pools")

## Refresh pools (clear and recreate with fresh visuals)
func refresh_pools() -> void:
	clear_all_pools()
	_init_pools()
	print("ProjectileManager: Pools refreshed with new visuals")

## Debug: Print pool statistics
func print_pool_stats() -> void:
	print("=== ProjectileManager Pool Statistics ===")
	for projectile_id in projectile_pools.keys():
		var pool_size: int = projectile_pools[projectile_id].size()
		var active_size: int = active_projectiles[projectile_id].size() if active_projectiles.has(projectile_id) else 0
		print("  %s: %d in pool, %d active" % [projectile_id, pool_size, active_size])
