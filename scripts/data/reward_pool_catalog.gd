extends Node
# RewardPoolCatalog is registered as autoload "RewardPools", no class_name needed

## Reward Pool Catalog - Defines pools of cards for reward generation
##
## Pools can be:
## - Element-based: Auto-populated from CardCatalog by elemental_affinity
## - Rarity-based: Auto-populated from CardCatalog by rarity
## - Type-based: Auto-populated from CardCatalog by card_type (spell/summon)
## - Custom: Manually defined card lists
##
## Usage:
##   var pool = RewardPools.get_pool(RewardPoolIDs.FIRE_CARDS)
##   var cards = RewardPools.draw_from_pool("fire_cards", 3, exclude_owned=true)

## Cached pools - built lazily on first access
var _pools: Dictionary = {}
var _initialized: bool = false

## Custom pool definitions (card IDs that don't fit auto-generation)
## Format: pool_id -> Array[StringName] of card IDs
const CUSTOM_POOLS: Dictionary = {
	# Tutorial rewards - basic cards for new players
	&"tutorial_rewards": [
		CardIDs.CHARGE,
		CardIDs.GUARD,
		CardIDs.RALLY,
		CardIDs.FIRE_WISP,
		CardIDs.EARTH_SPRITE,
		CardIDs.PUFF,
	],
	# Starter rewards - slightly better than tutorial
	&"starter_rewards": [
		CardIDs.FIREBALL,
		CardIDs.MANA_BOLT,
		CardIDs.FIRE_ANT,
		CardIDs.WATER_FROG,
		CardIDs.CLOUD_SWARM,
	],
	# Boss loot - rare/powerful cards
	&"boss_loot": [
		CardIDs.FIRE_TITAN,
		CardIDs.FIRE_ANT_SWARM,
		CardIDs.FIRE_WISP_SWARM,
	],
}

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("RewardPoolCatalog: Initializing...")
	# Defer initialization to ensure CardCatalog is ready
	call_deferred("_initialize_pools")


func _initialize_pools() -> void:
	if _initialized:
		return

	# Build element-based pools
	_build_element_pools()

	# Build rarity-based pools
	_build_rarity_pools()

	# Build type-based pools
	_build_type_pools()

	# Add custom pools
	for pool_id: StringName in CUSTOM_POOLS:
		_pools[pool_id] = CUSTOM_POOLS[pool_id].duplicate()

	# Build "all_cards" pool
	var all_ids: Array[String] = CardCatalog.get_all_card_ids()
	var all_cards: Array[StringName] = []
	for id: String in all_ids:
		all_cards.append(StringName(id))
	_pools[RewardPoolIDs.ALL_CARDS] = all_cards

	_initialized = true
	print("RewardPoolCatalog: Initialized %d pools" % _pools.size())


## =============================================================================
## POOL BUILDING
## =============================================================================

func _build_element_pools() -> void:
	# Map element IDs to pool IDs
	var element_to_pool: Dictionary = {
		"fire": RewardPoolIDs.FIRE_CARDS,
		"water": RewardPoolIDs.WATER_CARDS,
		"wind": RewardPoolIDs.WIND_CARDS,
		"earth": RewardPoolIDs.EARTH_CARDS,
		"lightning": RewardPoolIDs.LIGHTNING_CARDS,
		"life": RewardPoolIDs.LIFE_CARDS,
		"death": RewardPoolIDs.DEATH_CARDS,
		"shadow": RewardPoolIDs.SHADOW_CARDS,
	}

	# Initialize empty pools
	for pool_id: StringName in element_to_pool.values():
		_pools[pool_id] = []

	# Populate from CardCatalog
	var all_cards: Array[Dictionary] = CardCatalog.list_all_cards()
	for card: Dictionary in all_cards:
		var categories: Variant = card.get("categories", {})
		if not categories is Dictionary:
			continue

		var affinity: Variant = categories.get("elemental_affinity", null)
		if affinity == null:
			continue

		# Get element ID (affinity is an Element object or string)
		var element_id: String = ""
		if affinity is String:
			element_id = affinity
		elif affinity.has_method("_to_string"):
			element_id = str(affinity)
		else:
			continue

		# Add to appropriate pool
		var pool_id: Variant = element_to_pool.get(element_id, null)
		if pool_id != null:
			var card_id: StringName = StringName(card.get("catalog_id", ""))
			if not card_id.is_empty():
				_pools[pool_id].append(card_id)


func _build_rarity_pools() -> void:
	var rarity_to_pool: Dictionary = {
		"common": RewardPoolIDs.COMMON_CARDS,
		"rare": RewardPoolIDs.RARE_CARDS,
		"epic": RewardPoolIDs.EPIC_CARDS,
		"legendary": RewardPoolIDs.LEGENDARY_CARDS,
	}

	for rarity: String in rarity_to_pool:
		var pool_id: StringName = rarity_to_pool[rarity]
		var cards: Array[Dictionary] = CardCatalog.get_cards_by_rarity(rarity)
		var card_ids: Array[StringName] = []
		for card: Dictionary in cards:
			var catalog_id: String = card.get("catalog_id", "")
			if not catalog_id.is_empty():
				card_ids.append(StringName(catalog_id))
		_pools[pool_id] = card_ids


func _build_type_pools() -> void:
	# Card types: 0 = SUMMON, 1 = SPELL (from Card.gd)
	const TYPE_SUMMON: int = 0
	const TYPE_SPELL: int = 1

	# Build spell pool
	var spells: Array[Dictionary] = CardCatalog.get_cards_by_type(TYPE_SPELL)
	var spell_ids: Array[StringName] = []
	for card: Dictionary in spells:
		var catalog_id: String = card.get("catalog_id", "")
		if not catalog_id.is_empty():
			spell_ids.append(StringName(catalog_id))
	_pools[RewardPoolIDs.ALL_SPELLS] = spell_ids

	# Build unit pool
	var units: Array[Dictionary] = CardCatalog.get_cards_by_type(TYPE_SUMMON)
	var unit_ids: Array[StringName] = []
	for card: Dictionary in units:
		var catalog_id: String = card.get("catalog_id", "")
		if not catalog_id.is_empty():
			unit_ids.append(StringName(catalog_id))
	_pools[RewardPoolIDs.ALL_UNITS] = unit_ids


## =============================================================================
## PUBLIC API
## =============================================================================

## Get all card IDs in a pool
## Returns empty array if pool doesn't exist
func get_pool(pool_id: StringName) -> Array[StringName]:
	_ensure_initialized()
	var pool: Variant = _pools.get(pool_id, null)
	if pool == null:
		push_warning("RewardPoolCatalog: Pool '%s' not found" % pool_id)
		return []
	var result: Array[StringName] = []
	result.assign(pool)
	return result


## Check if a pool exists
func has_pool(pool_id: StringName) -> bool:
	_ensure_initialized()
	return _pools.has(pool_id)


## Get all pool IDs
func get_all_pool_ids() -> Array[StringName]:
	_ensure_initialized()
	var result: Array[StringName] = []
	for key: StringName in _pools.keys():
		result.append(key)
	return result


## Draw random cards from a pool with filtering rules
## @param pool_id: Pool to draw from
## @param count: Number of cards to draw
## @param exclude_owned: If true, filter out cards the player already owns
## @param unique_only: If true, don't return the same card twice
## @return Array of card IDs (may be less than count if pool is exhausted)
func draw_from_pool(
	pool_id: StringName,
	count: int,
	exclude_owned: bool = false,
	unique_only: bool = true
) -> Array[StringName]:
	_ensure_initialized()

	var pool: Array[StringName] = get_pool(pool_id)
	if pool.is_empty():
		return []

	# Apply filters
	var available: Array[StringName] = pool.duplicate()

	if exclude_owned:
		var owned_ids: Array[String] = RewardService.get_owned_catalog_ids()
		available = available.filter(func(id: StringName) -> bool:
			return not String(id) in owned_ids
		)

	if available.is_empty():
		return []

	# Draw cards
	var drawn: Array[StringName] = []
	var remaining: Array[StringName] = available.duplicate()

	for i: int in range(count):
		if remaining.is_empty():
			break

		var idx: int = randi() % remaining.size()
		var card_id: StringName = remaining[idx]
		drawn.append(card_id)

		if unique_only:
			remaining.remove_at(idx)

	return drawn


## Ensure pools are initialized (for calls before _ready completes)
func _ensure_initialized() -> void:
	if not _initialized:
		_initialize_pools()
