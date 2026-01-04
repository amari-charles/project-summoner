extends Node
class_name BattleRNGClass

## Centralized seeded RNG system for deterministic multiplayer.
##
## All gameplay-affecting randomness MUST go through this system to ensure
## identical results across network peers. Each domain has its own RNG stream
## derived from the battle seed.
##
## Usage:
##   BattleRNG.set_battle_seed(12345)  # Call at battle start (same seed for all peers)
##   var value: float = BattleRNG.randf_range(0.0, 1.0, RNGDomain.Domain.AI_DECISIONS)
##   BattleRNG.shuffle_array(deck, RNGDomain.Domain.DECK_SHUFFLE)

## Emitted when battle seed is set (useful for debugging/logging)
signal seed_initialized(seed: int)

## The master seed for this battle (shared across network)
var _battle_seed: int = 0

## Per-domain RNG instances
var _domain_rngs: Dictionary = {}  # RNGDomain.Domain -> RandomNumberGenerator

## Track if seed has been set (prevents accidental use before initialization)
var _is_initialized: bool = false

## Debug mode - logs all RNG calls for debugging determinism issues
var _debug_logging: bool = false

func _ready() -> void:
	# Initialize with a random seed for single-player / testing
	# In multiplayer, set_battle_seed() will be called with a shared seed
	_initialize_with_random_seed()

## Initialize RNG with a random seed (for single-player or testing)
func _initialize_with_random_seed() -> void:
	var random_seed: int = randi()
	set_battle_seed(random_seed)

## Set the battle seed - call this at battle start with the same seed for all peers
## In multiplayer, the host generates this seed and shares it to clients
func set_battle_seed(seed: int) -> void:
	_battle_seed = seed
	_is_initialized = true
	_domain_rngs.clear()

	# Initialize each domain with a derived seed
	# Each domain gets a unique seed derived from the master seed
	for domain_value: int in RNGDomain.Domain.values():
		var domain: RNGDomain.Domain = domain_value as RNGDomain.Domain
		var domain_seed: int = _derive_domain_seed(seed, domain)
		var rng: RandomNumberGenerator = RandomNumberGenerator.new()
		rng.seed = domain_seed
		_domain_rngs[domain] = rng

	seed_initialized.emit(seed)

	if _debug_logging:
		print("BattleRNG: Initialized with seed %d" % seed)

## Get the current battle seed (for debugging or network sync verification)
func get_battle_seed() -> int:
	return _battle_seed

## Check if the RNG system has been initialized
func is_initialized() -> bool:
	return _is_initialized

## Derive a unique seed for each domain from the master seed
## Uses a simple hash to ensure domains don't share RNG state
func _derive_domain_seed(master_seed: int, domain: RNGDomain.Domain) -> int:
	# Combine master seed with domain index using a prime multiplier
	# This ensures each domain has a unique, deterministic starting state
	return master_seed ^ (domain * 2654435761)  # Golden ratio prime

## Get the RNG instance for a domain
func _get_rng(domain: RNGDomain.Domain) -> RandomNumberGenerator:
	if not _is_initialized:
		push_warning("BattleRNG: Used before initialization! Call set_battle_seed() first.")
		_initialize_with_random_seed()

	if not _domain_rngs.has(domain):
		push_error("BattleRNG: Unknown domain %d" % domain)
		return RandomNumberGenerator.new()

	return _domain_rngs[domain]

# =============================================================================
# PUBLIC API - Random Number Generation
# =============================================================================

## Generate a random float in range [from, to] using the specified domain
func randf_range(from: float, to: float, domain: RNGDomain.Domain) -> float:
	var rng: RandomNumberGenerator = _get_rng(domain)
	var result: float = rng.randf_range(from, to)

	if _debug_logging:
		print("BattleRNG [%s]: randf_range(%.2f, %.2f) = %.4f" % [
			RNGDomain.Domain.keys()[domain], from, to, result
		])

	return result

## Generate a random float in range [0.0, 1.0) using the specified domain
func randf(domain: RNGDomain.Domain) -> float:
	var rng: RandomNumberGenerator = _get_rng(domain)
	var result: float = rng.randf()

	if _debug_logging:
		print("BattleRNG [%s]: randf() = %.4f" % [
			RNGDomain.Domain.keys()[domain], result
		])

	return result

## Generate a random integer in range [from, to] (inclusive) using the specified domain
func randi_range(from: int, to: int, domain: RNGDomain.Domain) -> int:
	var rng: RandomNumberGenerator = _get_rng(domain)
	var result: int = rng.randi_range(from, to)

	if _debug_logging:
		print("BattleRNG [%s]: randi_range(%d, %d) = %d" % [
			RNGDomain.Domain.keys()[domain], from, to, result
		])

	return result

## Generate a random unsigned 32-bit integer using the specified domain
func randi(domain: RNGDomain.Domain) -> int:
	var rng: RandomNumberGenerator = _get_rng(domain)
	var result: int = rng.randi()

	if _debug_logging:
		print("BattleRNG [%s]: randi() = %d" % [
			RNGDomain.Domain.keys()[domain], result
		])

	return result

# =============================================================================
# PUBLIC API - Array Operations
# =============================================================================

## Shuffle an array in-place using the specified domain
## This is deterministic - same seed + same array = same shuffle result
func shuffle_array(array: Array, domain: RNGDomain.Domain) -> void:
	var rng: RandomNumberGenerator = _get_rng(domain)
	var n: int = array.size()

	if _debug_logging:
		print("BattleRNG [%s]: shuffle_array (size=%d)" % [
			RNGDomain.Domain.keys()[domain], n
		])

	# Fisher-Yates shuffle algorithm
	for i: int in range(n - 1, 0, -1):
		var j: int = rng.randi_range(0, i)
		var temp: Variant = array[i]
		array[i] = array[j]
		array[j] = temp

## Pick a random element from an array using the specified domain
func pick_random(array: Array, domain: RNGDomain.Domain) -> Variant:
	if array.is_empty():
		push_warning("BattleRNG: pick_random called on empty array")
		return null

	var index: int = self.randi_range(0, array.size() - 1, domain)
	return array[index]

## Pick multiple random elements from an array (without replacement)
func pick_multiple(array: Array, count: int, domain: RNGDomain.Domain) -> Array:
	if count >= array.size():
		var result: Array = array.duplicate()
		shuffle_array(result, domain)
		return result

	var pool: Array = array.duplicate()
	var result: Array = []

	for i: int in count:
		var index: int = self.randi_range(0, pool.size() - 1, domain)
		result.append(pool[index])
		pool.remove_at(index)

	return result

# =============================================================================
# DEBUG API
# =============================================================================

## Enable debug logging (prints all RNG calls)
func enable_debug_logging(enabled: bool = true) -> void:
	_debug_logging = enabled
	print("BattleRNG: Debug logging %s" % ("enabled" if enabled else "disabled"))

## Reset the battle RNG for a new battle.
## Call this when:
## - Starting a new battle (before set_battle_seed)
## - Returning to menu from a battle
## - In tests between test cases
## Note: In multiplayer, the host should call reset() then set_battle_seed() with
## the new shared seed before the battle starts.
func reset() -> void:
	_is_initialized = false
	_domain_rngs.clear()
	_battle_seed = 0
