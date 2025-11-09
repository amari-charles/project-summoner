extends Node

## Centralized damage number management with object pooling
## Usage: Automatically spawns damage numbers on DamageSystem.damage_taken signal
## Autoload as: /root/DamageNumberManager

## Pool settings
const INITIAL_POOL_SIZE: int = 20
const MAX_POOL_SIZE: int = 50

## Pools
var number_pool: Array = []  # Array of FloatingDamageNumber
var active_numbers: Array = []  # Array of FloatingDamageNumber

## Container
var numbers_container: Node3D = null

func _ready() -> void:
	print("DamageNumberManager: Initializing...")

	# Create container
	numbers_container = Node3D.new()
	numbers_container.name = "DamageNumbersContainer"
	add_child(numbers_container)

	# Pre-instantiate pool
	_init_pool()

	# Connect to damage system
	if DamageSystem:
		DamageSystem.damage_taken.connect(_on_damage_taken)
		print("DamageNumberManager: Connected to DamageSystem")

	print("DamageNumberManager: Initialized with pool of %d numbers" % INITIAL_POOL_SIZE)

func _init_pool() -> void:
	for i in range(INITIAL_POOL_SIZE):
		var number = FloatingDamageNumber.new()
		number.is_pooled = true
		number.reset()
		number_pool.append(number)

## Spawn damage number at position
func spawn_damage_number(value: float, position: Vector3, is_crit: bool = false, damage_type: String = "physical"):
	# Get number from pool or create new
	var number = null
	if number_pool.size() > 0:
		number = number_pool.pop_back()
		number.reset()
	else:
		number = FloatingDamageNumber.new()
		number.is_pooled = true

	if not number:
		push_error("DamageNumberManager: Failed to create damage number")
		return null

	# Add to scene FIRST so _ready() runs
	numbers_container.add_child(number)

	# THEN configure and show (after _ready() has created label)
	number.show_damage(value, position, is_crit, damage_type)

	# Track active number
	active_numbers.append(number)

	# Connect to finished signal
	if not number.number_finished.is_connected(_on_number_finished):
		number.number_finished.connect(_on_number_finished.bind(number))

	return number

## Handle damage_taken signal from DamageSystem
func _on_damage_taken(event: CombatEvent) -> void:
	if not event or not event.target:
		return

	# Calculate spawn position (above target, slightly below HP bar)
	var spawn_pos = event.target.global_position + Vector3(0, 1.5, 0)

	# Get metadata
	var is_crit = event.metadata.get("is_crit", false) if event.metadata else false
	var damage_type = event.damage_type

	# Spawn damage number
	spawn_damage_number(event.value, spawn_pos, is_crit, damage_type)

## Handler for number finished signal
func _on_number_finished(number) -> void:
	# Remove from active list
	var idx = active_numbers.find(number)
	if idx >= 0:
		active_numbers.remove_at(idx)

	# Remove from scene
	if number.get_parent():
		number.get_parent().remove_child(number)

	# Return to pool
	_return_to_pool(number)

## Return number to pool
func _return_to_pool(number) -> void:
	if number_pool.size() < MAX_POOL_SIZE:
		number.reset()
		number_pool.append(number)
	else:
		# Pool full, destroy number
		number.queue_free()

## Remove all active numbers (useful for scene transitions)
func clear_all_numbers() -> void:
	for number in active_numbers:
		if number.get_parent():
			number.get_parent().remove_child(number)
		_return_to_pool(number)

	active_numbers.clear()

## Debug: Print pool statistics
func print_pool_stats() -> void:
	print("=== DamageNumberManager Pool Statistics ===")
	print("  Numbers in pool: %d" % number_pool.size())
	print("  Active numbers: %d" % active_numbers.size())
	print("  Total numbers: %d" % (number_pool.size() + active_numbers.size()))
