extends Node3D
class_name Summoner3D

## Hero/Summoner - The player character that manages cards and mana
## NOT a battlefield entity - cannot be attacked or damaged
## The Nexus (Base3D) is what units attack to win the game

## Deck loading strategies
enum DeckLoadStrategy {
	STATIC,           ## Use starting_deck (test scenes, fallback)
	BATTLE_CONTEXT,   ## Load from BattleContext (normal enemy behavior)
	PROFILE,          ## Load from player profile (normal player behavior)
	DEFERRED         ## Don't load deck in _ready(), wait for manual override (test controllers)
}

@export var team: Unit3D.Team = Unit3D.Team.PLAYER

## Deck and hand
@export var starting_deck: Array[Card] = []
@export var max_hand_size: int = 4
@export var deck_load_strategy: DeckLoadStrategy = DeckLoadStrategy.BATTLE_CONTEXT

## Resources
@export var mana_regen_rate: float = 1.0

## Current state
var mana: float = 0.0
const MANA_MAX: float = 10.0
var hand: Array[Card] = []
var deck: Array[Card] = []
var is_alive: bool = true

## Hero instance (loaded from profile when using PROFILE strategy)
var _loaded_hero_instance: HeroInstance = null

## Track initialization state
var _initialized: bool = false

## Signals
signal card_played(card: Card)
signal card_drawn(card: Card)
signal mana_changed(current: float, max: float)
signal hand_changed(hand: Array[Card])
signal summoner_ready(summoner: Summoner3D)  ## Emitted after init() completes

func _ready() -> void:
	# Minimal setup - just add to groups for discovery
	# Full initialization happens in init() called by BattleCoordinator
	add_to_group("summoners")
	# Note: NOT in "bases" group - summoners are not attack targets
	if team == Unit3D.Team.PLAYER:
		add_to_group("player_summoners")
	else:
		add_to_group("enemy_summoners")

## Initialize summoner - called by BattleCoordinator after scene is ready
## This replaces the old self-initialization pattern
func init() -> void:
	if _initialized:
		return
	_initialized = true

	print("Summoner3D: Initializing (team: %s)..." % ("PLAYER" if team == Unit3D.Team.PLAYER else "ENEMY"))

	# Auto-correct deck loading strategy based on team if using wrong default
	if team == Unit3D.Team.PLAYER and deck_load_strategy == DeckLoadStrategy.BATTLE_CONTEXT:
		deck_load_strategy = DeckLoadStrategy.PROFILE
	elif team == Unit3D.Team.ENEMY and deck_load_strategy == DeckLoadStrategy.PROFILE:
		deck_load_strategy = DeckLoadStrategy.BATTLE_CONTEXT

	# For enemy: Auto-detect event_sequence battles (enemies spawned via dialogue/events)
	# Pattern: battle_config has "event_sequence" AND "enemy_deck" is empty array
	# This is intentional - enemies are spawned manually via BattleDialogueController/EventSequencer
	if team == Unit3D.Team.ENEMY and deck_load_strategy == DeckLoadStrategy.BATTLE_CONTEXT:
		if BattleContext.battle_config.has("event_sequence") and BattleContext.battle_config.has("enemy_deck"):
			var enemy_deck_variant: Variant = BattleContext.battle_config.get("enemy_deck")
			if enemy_deck_variant is Array:
				var enemy_deck_array: Array = enemy_deck_variant
				if enemy_deck_array.is_empty():
					print("Summoner3D: Battle uses event_sequence with empty enemy_deck - switching to DEFERRED strategy")
					deck_load_strategy = DeckLoadStrategy.DEFERRED

	# Initialize deck using strategy pattern (before HP/mana init for hero bonuses)
	deck = _load_deck_by_strategy()

	# Apply hero bonuses for player using PROFILE strategy
	if team == Unit3D.Team.PLAYER and deck_load_strategy == DeckLoadStrategy.PROFILE:
		if _loaded_hero_instance != null:
			_apply_hero_bonuses(_loaded_hero_instance)

	# Initialize mana
	mana = MANA_MAX

	# Handle empty deck - behavior depends on deck loading strategy
	if deck.is_empty():
		if deck_load_strategy == DeckLoadStrategy.DEFERRED:
			# DEFERRED strategy: Empty deck is expected, will be populated by controller
			print("Summoner3D: Deck deferred - waiting for manual population")
		elif _is_test_mode():
			# Test mode: Allow emergency fallback deck
			push_warning("Summoner3D: Failed to load deck in test mode. Creating emergency fallback deck.")
			deck = _create_emergency_deck()

			if deck.is_empty():
				push_error("Summoner3D: CRITICAL - Cannot create deck, disabling summoner")
				is_alive = false
				return
		else:
			# Production mode: HARD FAIL - configuration is broken
			var error_msg: String = "Summoner3D: CRITICAL - No deck loaded in production mode!\n"
			error_msg += "Team: %s\n" % ("PLAYER" if team == Unit3D.Team.PLAYER else "ENEMY")
			error_msg += "Strategy: %s\n" % DeckLoadStrategy.keys()[deck_load_strategy]
			error_msg += "This indicates a configuration bug - check BattleContext and player profile."
			push_error(error_msg)
			assert(false, error_msg)
			is_alive = false
			return
	else:
		print("Summoner3D: Loaded %d cards using %s strategy" % [deck.size(), DeckLoadStrategy.keys()[deck_load_strategy]])

	deck.shuffle()

	# Draw starting hand
	for i: int in max_hand_size:
		draw_card()

	mana_changed.emit(mana, MANA_MAX)
	summoner_ready.emit(self)
	print("Summoner3D: Initialization complete")

func _process(delta: float) -> void:
	if not is_alive:
		return

	if mana < MANA_MAX:
		mana = clamp(mana + mana_regen_rate * delta, 0.0, MANA_MAX)
		mana_changed.emit(mana, MANA_MAX)

func draw_card() -> void:
	if deck.is_empty():
		return

	if hand.size() >= max_hand_size:
		return

	var card: Card = deck.pop_front()
	hand.append(card)
	card_drawn.emit(card)
	hand_changed.emit(hand)

## Play a card from hand at the given 3D position
func play_card_3d(card_index: int, spawn_position: Vector3) -> bool:
	if card_index < 0 or card_index >= hand.size():
		return false

	var card: Card = hand[card_index]

	if not card.can_play(int(mana)):
		return false

	mana -= card.mana_cost
	mana_changed.emit(mana, MANA_MAX)

	var battlefield: Node = get_tree().get_first_node_in_group("battlefield")
	if battlefield == null:
		push_error("No battlefield found in scene!")
		return false

	# Get ModifierSystem for efficient access (avoid fragile scene tree lookups)
	var modifier_system: Node = get_node_or_null("/root/ModifierSystem")

	# Play the card in 3D
	card.play_3d(spawn_position, team, battlefield, modifier_system)

	hand.remove_at(card_index)
	draw_card()

	card_played.emit(card)
	hand_changed.emit(hand)

	return true

## Detect if we're running in test mode (allows emergency fallback decks)
## Note: With DEFERRED strategy, this is only used as a safety net for legacy scenarios
func _is_test_mode() -> bool:
	# Check via game_controller group
	var game_controller: Node = get_tree().get_first_node_in_group("game_controller")
	if game_controller and game_controller is TestGameController:
		return true

	# Check root node of scene (test scenes have test controller as root)
	var root: Node = get_tree().current_scene
	if root and root is TestGameController:
		return true

	# Check if BattleContext is in practice mode
	var battle_context: Node = get_node_or_null("/root/BattleContext")
	if battle_context:
		var mode_variant: Variant = battle_context.get("current_mode")
		if mode_variant is int:
			var mode: int = mode_variant
			# PRACTICE = 1 in BattleContext enum
			if mode == 1:
				return true

	return false

## =============================================================================
## DECK LOADING STRATEGY
## =============================================================================

## Load deck based on configured strategy
func _load_deck_by_strategy() -> Array[Card]:
	match deck_load_strategy:
		DeckLoadStrategy.STATIC:
			return _load_static_deck()
		DeckLoadStrategy.BATTLE_CONTEXT:
			return _load_battle_context_deck()
		DeckLoadStrategy.PROFILE:
			return _load_profile_deck()
		DeckLoadStrategy.DEFERRED:
			print("Summoner3D: Using DEFERRED strategy - deck will be set manually later")
			return []  # Empty deck, will be populated by controller
		_:
			push_error("Summoner3D: Unknown deck load strategy %d" % deck_load_strategy)
			return []

## Strategy: Load from starting_deck (test scenes, fallback)
func _load_static_deck() -> Array[Card]:
	print("Summoner3D: Using static starting_deck")
	return starting_deck.duplicate()

## Strategy: Load from BattleContext (normal enemy behavior)
func _load_battle_context_deck() -> Array[Card]:
	if team == Unit3D.Team.PLAYER:
		push_warning("Summoner3D: BATTLE_CONTEXT strategy used for player team, using static deck instead")
		return _load_static_deck()

	print("Summoner3D: Loading enemy deck from BattleContext...")
	var loaded_deck: Array[Card] = EnemyDeckLoader.load_enemy_deck_for_battle()

	if loaded_deck.is_empty():
		push_warning("Summoner3D: Failed to load from BattleContext, falling back to static deck")
		return _load_static_deck()

	return loaded_deck

## Strategy: Load from player profile (normal player behavior)
func _load_profile_deck() -> Array[Card]:
	if team == Unit3D.Team.ENEMY:
		push_warning("Summoner3D: PROFILE strategy used for enemy team, using static deck instead")
		return _load_static_deck()

	# Check for dev test deck override in BattleContext
	var battle_context: Node = get_node_or_null("/root/BattleContext")
	if battle_context:
		var config: Variant = battle_context.get("battle_config")
		if config is Dictionary:
			var battle_config: Dictionary = config
			if battle_config.has("dev_player_deck"):
				print("Summoner3D: Loading DEV TEST deck from BattleContext...")
				return _load_dev_deck_from_config(battle_config["dev_player_deck"])

	print("Summoner3D: Loading deck from player profile...")
	var deck_data: Dictionary = DeckLoader.load_player_deck()
	var loaded_deck_variant: Variant = deck_data.get("cards", [])
	var loaded_deck: Array[Card] = []
	if loaded_deck_variant is Array:
		var temp_array: Array = loaded_deck_variant
		loaded_deck.assign(temp_array)

	# Store hero instance for bonus application in init()
	var hero_instance_variant: Variant = deck_data.get("hero_instance")
	if hero_instance_variant is HeroInstance:
		_loaded_hero_instance = hero_instance_variant

	if loaded_deck.is_empty():
		push_warning("Summoner3D: Failed to load from profile, falling back to static deck")
		return _load_static_deck()

	return loaded_deck

## Load dev test deck from battle configuration
func _load_dev_deck_from_config(dev_deck_config: Variant) -> Array[Card]:
	if not dev_deck_config is Array:
		push_error("Summoner3D: dev_player_deck is not an Array")
		return []

	var loaded_deck: Array[Card] = []
	var card_configs: Array = dev_deck_config

	for config_variant: Variant in card_configs:
		if not config_variant is Dictionary:
			continue

		var config: Dictionary = config_variant
		var catalog_id: String = config.get("catalog_id", "")
		var count: int = config.get("count", 1)

		for i: int in count:
			var card: Card = CardCatalog.create_card_resource(catalog_id)
			if card:
				loaded_deck.append(card)
			else:
				push_warning("Summoner3D: Failed to create dev card: %s" % catalog_id)

	print("Summoner3D: Loaded %d cards from dev_player_deck" % loaded_deck.size())
	return loaded_deck

## Emergency fallback: Create minimal deck when all strategies fail
## Uses basic warrior cards as last resort to prevent game breaking
func _create_emergency_deck() -> Array[Card]:
	print("Summoner3D: Creating emergency fallback deck (3x warrior)")

	var emergency_deck: Array[Card] = []

	# Validate CardCatalog autoload exists
	if not CardCatalog:
		push_error("Summoner3D: CardCatalog autoload not available - cannot create emergency deck")
		return emergency_deck

	# Try to create 3 neade cards (basic unit)
	for i: int in 3:
		var card: Card = CardCatalog.create_card_resource("neade")
		if card:
			emergency_deck.append(card)
		else:
			push_error("Summoner3D: Failed to create emergency neade card %d" % i)

	if emergency_deck.is_empty():
		push_error("Summoner3D: Emergency deck creation failed - CardCatalog may be broken")
	else:
		print("Summoner3D: Created emergency deck with %d cards" % emergency_deck.size())

	return emergency_deck

## Apply hero bonuses to summoner stats
func _apply_hero_bonuses(hero_instance: HeroInstance) -> void:
	if hero_instance == null:
		push_warning("Summoner3D: Cannot apply bonuses from null HeroInstance")
		return

	# Get computed stats (includes modifiers)
	var stats: Dictionary = hero_instance.get_computed_stats()

	# Set mana regen from hero (with modifiers applied)
	var hero_mana_regen: float = stats.get("mana_regen", 1.0)
	mana_regen_rate = hero_mana_regen

	# TODO: Hero health stat should flow to Nexus (Base3D), not stored here
	# TODO: max_mana from stats should be applied (MANA_MAX is currently a constant)

	var hero_name: String = hero_instance.config.hero_name
	var modifier_count: int = hero_instance.active_modifiers.size()
	print("Summoner3D: Applied hero bonuses from '%s' (Level %d, %d modifiers) - Mana Regen: %.1f/s" % [
		hero_name, hero_instance.level, modifier_count, mana_regen_rate
	])
