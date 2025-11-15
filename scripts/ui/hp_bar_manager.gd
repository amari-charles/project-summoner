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
	var scene_path: String = "res://scenes/ui/floating_hp_bar.tscn"
	if ResourceLoader.exists(scene_path):
		hp_bar_scene = load(scene_path)
	else:
		push_warning("HPBarManager: HP bar scene not found, will instantiate script directly")

func _init_pool() -> void:
	for i: int in range(INITIAL_POOL_SIZE):
		var bar: FloatingHPBar = _instantiate_bar()
		if bar:
			bar.is_pooled = true
			bar.reset()
			bar.visible = false  # Hide pooled bars until assigned to units
			bar_pool.append(bar)

func _instantiate_bar() -> FloatingHPBar:
	var bar: FloatingHPBar
	if hp_bar_scene:
		bar = hp_bar_scene.instantiate() as FloatingHPBar
	else:
		# Fallback: create from script
		bar = FloatingHPBar.new()
	return bar

## Create HP bar for a unit
func create_bar_for_unit(unit: Node3D, settings: Dictionary = {}) -> FloatingHPBar:
	# Check if unit already has a bar
	if active_bars.has(unit):
		push_warning("HPBarManager: Unit already has an HP bar")
		var existing_bar: FloatingHPBar = active_bars[unit]
		return existing_bar

	# Get bar from pool or create new
	var bar: FloatingHPBar = null
	if bar_pool.size() > 0:
		bar = bar_pool.pop_back()
		bar.reset()
		# print("HPBarManager: Reusing bar from pool (pool size: %d)" % bar_pool.size())
	else:
		bar = _instantiate_bar()
		if bar:
			bar.is_pooled = true
		# print("HPBarManager: Created new bar (pool was empty)")

	if not bar:
		push_error("HPBarManager: Failed to create HP bar")
		return null

	# print("HPBarManager: Bar instance valid: %s" % is_instance_valid(bar))

	# Apply custom settings
	if settings.has("bar_width"):
		var bar_width_val: Variant = settings.get("bar_width")
		if bar_width_val is float:
			var bar_width_float: float = bar_width_val
			bar.bar_width = bar_width_float
	if settings.has("bar_height"):
		var bar_height_val: Variant = settings.get("bar_height")
		if bar_height_val is float:
			var bar_height_float: float = bar_height_val
			bar.bar_height = bar_height_float
	if settings.has("offset_y"):
		var offset_y_val: Variant = settings.get("offset_y")
		if offset_y_val is float:
			var offset_y_float: float = offset_y_val
			bar.offset_y = offset_y_float
	if settings.has("show_on_damage_only"):
		var show_val: Variant = settings.get("show_on_damage_only")
		if show_val is bool:
			var show_bool: bool = show_val
			bar.show_on_damage_only = show_bool
	if settings.has("fade_delay"):
		var fade_delay_val: Variant = settings.get("fade_delay")
		if fade_delay_val is float:
			var fade_delay_float: float = fade_delay_val
			bar.fade_delay = fade_delay_float

	# Set target unit
	bar.set_target(unit)

	# Add to scene
	bars_container.add_child(bar)
	# print("HPBarManager: Bar added to scene. Position: %v, Visible: %s" % [bar.global_position, bar.visible])

	# Track active bar
	active_bars[unit] = bar
	# print("HPBarManager: Active bars count: %d" % active_bars.size())

	# Connect to bar hidden signal for auto-removal
	if not bar.bar_hidden.is_connected(_on_bar_hidden):
		bar.bar_hidden.connect(_on_bar_hidden.bind(unit, bar))

	return bar

## Remove bar from a unit
func remove_bar_from_unit(unit: Node3D) -> void:
	if not active_bars.has(unit):
		return

	var bar_variant: Variant = active_bars.get(unit)
	if not bar_variant is FloatingHPBar:
		return
	var bar: FloatingHPBar = bar_variant
	active_bars.erase(unit)

	# Disconnect signal before returning to pool to prevent memory leak
	if is_instance_valid(unit) and unit.has_signal("hp_changed"):
		var signal_obj: Signal = unit.get("hp_changed")
		if signal_obj.is_connected(bar._on_hp_changed):
			signal_obj.disconnect(bar._on_hp_changed)

	# Remove from scene
	if bar.get_parent():
		bar.get_parent().remove_child(bar)

	# Return to pool
	_return_to_pool(bar)

## Update HP bar for a unit (if it exists)
func update_unit_hp(unit: Node3D, current_hp: float, max_hp: float) -> void:
	if active_bars.has(unit):
		var bar_variant: Variant = active_bars.get(unit)
		if bar_variant is FloatingHPBar:
			var bar: FloatingHPBar = bar_variant
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
func _on_bar_hidden(_unit: Node3D, _bar: FloatingHPBar) -> void:
	# Bar faded out, optionally remove it
	# For now, keep it in active_bars but hidden
	# It will be removed when unit dies
	pass

## Remove all bars (useful for scene transitions)
func clear_all_bars() -> void:
	# Get all bars from values (don't iterate over unit keys which may be freed)
	var bars_to_clear: Array[FloatingHPBar] = []
	for bar_variant: Variant in active_bars.values():
		if bar_variant is FloatingHPBar:
			bars_to_clear.append(bar_variant as FloatingHPBar)

	# Clear all bars
	for bar: FloatingHPBar in bars_to_clear:
		if is_instance_valid(bar):
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
