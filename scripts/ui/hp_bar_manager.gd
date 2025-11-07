extends Node

## Centralized HP bar management with object pooling
## Usage: HPBarManager.create_bar_for_unit(unit)
## Autoload as: /root/HPBarManager

## Pool settings
const INITIAL_POOL_SIZE: int = 20
const MAX_POOL_SIZE: int = 50

## Pools
var bar_pool: Array[FloatingHPBar] = []
var active_bars: Dictionary = {}  ## unit -> FloatingHPBar

## Container
var bars_container: Node3D = null

## HP bar scene
var hp_bar_scene: PackedScene = null

func _ready() -> void:
	print("HPBarManager: Initializing...")

	# Create container
	bars_container = Node3D.new()
	bars_container.name = "HPBarsContainer"
	add_child(bars_container)

	# Load HP bar scene (will create if not exists)
	_load_hp_bar_scene()

	# Pre-instantiate pool
	_init_pool()

	print("HPBarManager: Initialized with pool of %d bars" % INITIAL_POOL_SIZE)

func _load_hp_bar_scene() -> void:
	# Try to load scene
	var scene_path = "res://scenes/ui/floating_hp_bar.tscn"
	if ResourceLoader.exists(scene_path):
		hp_bar_scene = load(scene_path)
	else:
		push_warning("HPBarManager: HP bar scene not found, will instantiate script directly")

func _init_pool() -> void:
	for i in range(INITIAL_POOL_SIZE):
		var bar = _instantiate_bar()
		if bar:
			bar.is_pooled = true
			bar.reset()
			bar_pool.append(bar)

func _instantiate_bar() -> FloatingHPBar:
	if hp_bar_scene:
		return hp_bar_scene.instantiate() as FloatingHPBar
	else:
		# Fallback: create from script
		var bar = FloatingHPBar.new()
		return bar

## Create HP bar for a unit
func create_bar_for_unit(unit: Node3D, settings: Dictionary = {}) -> FloatingHPBar:
	# Check if unit already has a bar
	if active_bars.has(unit):
		push_warning("HPBarManager: Unit already has an HP bar")
		return active_bars[unit]

	# Get bar from pool or create new
	var bar: FloatingHPBar = null
	if bar_pool.size() > 0:
		bar = bar_pool.pop_back()
		bar.reset()
	else:
		bar = _instantiate_bar()
		bar.is_pooled = true

	if not bar:
		push_error("HPBarManager: Failed to create HP bar")
		return null

	# Apply custom settings
	if settings.has("bar_width"):
		bar.bar_width = settings.bar_width
	if settings.has("bar_height"):
		bar.bar_height = settings.bar_height
	if settings.has("offset_y"):
		bar.offset_y = settings.offset_y
	if settings.has("show_on_damage_only"):
		bar.show_on_damage_only = settings.show_on_damage_only
	if settings.has("fade_delay"):
		bar.fade_delay = settings.fade_delay

	# Set target unit
	bar.set_target(unit)

	# Add to scene
	bars_container.add_child(bar)

	# Track active bar
	active_bars[unit] = bar

	# Connect to bar hidden signal for auto-removal
	if not bar.bar_hidden.is_connected(_on_bar_hidden):
		bar.bar_hidden.connect(_on_bar_hidden.bind(unit, bar))

	return bar

## Remove bar from a unit
func remove_bar_from_unit(unit: Node3D) -> void:
	if not active_bars.has(unit):
		return

	var bar = active_bars[unit]
	active_bars.erase(unit)

	# Remove from scene
	if bar.get_parent():
		bar.get_parent().remove_child(bar)

	# Return to pool
	_return_to_pool(bar)

## Update HP bar for a unit (if it exists)
func update_unit_hp(unit: Node3D, current_hp: float, max_hp: float) -> void:
	if active_bars.has(unit):
		var bar = active_bars[unit]
		bar.update_hp(current_hp, max_hp)

## Return bar to pool
func _return_to_pool(bar: FloatingHPBar) -> void:
	if bar_pool.size() < MAX_POOL_SIZE:
		bar.reset()
		bar_pool.append(bar)
	else:
		# Pool full, destroy bar
		bar.queue_free()

## Handler for bar hidden signal
func _on_bar_hidden(unit: Node3D, bar: FloatingHPBar) -> void:
	# Bar faded out, optionally remove it
	# For now, keep it in active_bars but hidden
	# It will be removed when unit dies
	pass

## Remove all bars (useful for scene transitions)
func clear_all_bars() -> void:
	for unit in active_bars.keys():
		var bar = active_bars[unit]
		if bar.get_parent():
			bar.get_parent().remove_child(bar)
		_return_to_pool(bar)

	active_bars.clear()

## Debug: Print pool statistics
func print_pool_stats() -> void:
	print("=== HPBarManager Pool Statistics ===")
	print("  Bars in pool: %d" % bar_pool.size())
	print("  Active bars: %d" % active_bars.size())
	print("  Total bars: %d" % (bar_pool.size() + active_bars.size()))
