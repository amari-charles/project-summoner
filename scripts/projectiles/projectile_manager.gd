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

	var projectile_keys: Array = ContentCatalog.projectiles.keys()
	for projectile_id: Variant in projectile_keys:
		var proj_data: Variant = ContentCatalog.projectiles[projectile_id]
		if proj_data is ProjectileData and projectile_id is String:
			var projectile_id_str: String = projectile_id
			_create_pool_for(projectile_id_str, 5)  # Smaller initial pool per type

## Create pool for a specific projectile type
func _create_pool_for(projectile_id: String, pool_size: int) -> void:
	if projectile_pools.has(projectile_id):
		return  # Already exists

	projectile_pools[projectile_id] = []
	active_projectiles[projectile_id] = []

	for i: int in range(pool_size):
		var pooled: Projectile3D = _instantiate_projectile()
		if pooled:
			pooled.is_pooled = true
			pooled.reset()
			var pool: Array = projectile_pools[projectile_id]
			pool.append(pooled)

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
		"team": (source as Node3D).get("team") if "team" in source else -1
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
	var active: Array = active_projectiles[projectile_id]
	active.append(projectile)

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
		var pooled_projectile: Projectile3D = pool.pop_back()
		pooled_projectile.reset()
		return pooled_projectile

	# Pool exhausted, create new
	var new_projectile: Projectile3D = _instantiate_projectile()
	new_projectile.is_pooled = true
	return new_projectile

## Return projectile to pool
func _return_to_pool(projectile_id: String, projectile: Projectile3D) -> void:
	# Remove from active
	if active_projectiles.has(projectile_id):
		var active: Array = active_projectiles[projectile_id]
		active.erase(projectile)

	# Remove from scene
	if projectile.get_parent():
		projectile.get_parent().remove_child(projectile)

	# Return to pool if not full
	var pool: Array = projectile_pools[projectile_id]
	if pool.size() < MAX_POOL_SIZE:
		projectile.reset()
		pool.append(projectile)
	else:
		# Pool full, destroy
		projectile.queue_free()

## Signal handler for projectile expiration
## Note: Signal emits projectile first, then bind adds projectile_id
func _on_projectile_expired(projectile: Projectile3D, projectile_id_arg: String) -> void:
	_return_to_pool(projectile_id_arg, projectile)

## Clear all active projectiles (for scene transitions)
func clear_all_projectiles() -> void:
	var active_keys: Array = active_projectiles.keys()
	for projectile_id: Variant in active_keys:
		if not projectile_id is String:
			continue
		var projectile_id_str: String = projectile_id
		var active_list: Array = active_projectiles[projectile_id_str]
		for projectile: Variant in active_list:
			if not projectile is Node:
				continue
			var projectile_node: Node = projectile
			if projectile_node.get_parent():
				projectile_node.get_parent().remove_child(projectile_node)
			if projectile is Projectile3D:
				var projectile_3d: Projectile3D = projectile
				_return_to_pool(projectile_id_str, projectile_3d)

	active_projectiles.clear()

## Clear all pools (forces reload of visuals on next init)
func clear_all_pools() -> void:
	# Free all pooled projectiles
	var pool_keys: Array = projectile_pools.keys()
	for projectile_id: Variant in pool_keys:
		var pool_list: Array = projectile_pools[projectile_id]
		for projectile: Variant in pool_list:
			if is_instance_valid(projectile) and projectile is Node:
				var proj_node: Node = projectile
				proj_node.queue_free()

	# Clear all active projectiles
	var active_keys_2: Array = active_projectiles.keys()
	for projectile_id: Variant in active_keys_2:
		var active_list_2: Array = active_projectiles[projectile_id]
		for projectile: Variant in active_list_2:
			if is_instance_valid(projectile) and projectile is Node:
				var proj_node: Node = projectile
				proj_node.queue_free()

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
	var pool_keys_debug: Array = projectile_pools.keys()
	for projectile_id: Variant in pool_keys_debug:
		var pool: Array = projectile_pools[projectile_id]
		var pool_size: int = pool.size()
		var active_size: int = 0
		if active_projectiles.has(projectile_id):
			var active: Array = active_projectiles[projectile_id]
			active_size = active.size()
		print("  %s: %d in pool, %d active" % [projectile_id, pool_size, active_size])
