extends Node3D
class_name Summoner3D

## 3D Player base/summoner that spawns units and manages cards
## Each player has one Summoner that represents their base

## Deck loading strategies
enum DeckLoadStrategy {
	STATIC,           ## Use starting_deck (test scenes, fallback)
	BATTLE_CONTEXT,   ## Load from BattleContext (normal enemy behavior)
	PROFILE,          ## Load from player profile (normal player behavior)
	DEFERRED         ## Don't load deck in _ready(), wait for manual override (test controllers)
}

@export var max_hp: float = 1000.0
@export var team: Unit3D.Team = Unit3D.Team.PLAYER

## Deck and hand
@export var starting_deck: Array[Card] = []
@export var max_hand_size: int = 4
@export var deck_load_strategy: DeckLoadStrategy = DeckLoadStrategy.BATTLE_CONTEXT

## Resources
@export var mana_regen_rate: float = 1.0

## Current state
var current_hp: float = 0.0
var mana: float = 0.0
const MANA_MAX: float = 10.0
var hand: Array[Card] = []
var deck: Array[Card] = []
var is_alive: bool = true

## Signals
signal summoner_died(summoner: Summoner3D)
signal card_played(card: Card)
signal card_drawn(card: Card)
signal mana_changed(current: float, max: float)
signal hand_changed(hand: Array[Card])

func _ready() -> void:
	# Auto-correct deck loading strategy based on team if using wrong default
	if team == Unit3D.Team.PLAYER and deck_load_strategy == DeckLoadStrategy.BATTLE_CONTEXT:
		deck_load_strategy = DeckLoadStrategy.PROFILE
	elif team == Unit3D.Team.ENEMY and deck_load_strategy == DeckLoadStrategy.PROFILE:
		deck_load_strategy = DeckLoadStrategy.BATTLE_CONTEXT

	# For enemy summoners, load config from BattleContext
	if team == Unit3D.Team.ENEMY:
		var battle_context: Node = get_node_or_null("/root/BattleContext")
		if battle_context:
			var battle_config_variant: Variant = battle_context.get("battle_config")
			var battle_config: Dictionary = battle_config_variant if battle_config_variant is Dictionary else {}
			if not battle_config.is_empty():
				# Set enemy HP from config
				if battle_config.has("enemy_hp"):
					max_hp = battle_config.get("enemy_hp")
					print("Summoner3D: Set enemy HP from BattleContext: %d" % max_hp)

	current_hp = max_hp
	mana = MANA_MAX

	# Initialize deck using strategy pattern
	deck = _load_deck_by_strategy()

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

	add_to_group("summoners")
	add_to_group("bases")  # Allows spell cards to find summoner as projectile source
	if team == Unit3D.Team.PLAYER:
		add_to_group("player_summoners")
	else:
		add_to_group("enemy_summoners")

	mana_changed.emit(mana, MANA_MAX)

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

func take_damage(damage: float) -> void:
	if not is_alive:
		return

	current_hp -= damage

	if current_hp <= 0:
		current_hp = 0
		_die()

func _die() -> void:
	is_alive = false
	summoner_died.emit(self)

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
	if battle_context and battle_context.has("battle_config"):
		var config: Variant = battle_context.get("battle_config")
		if config is Dictionary:
			var battle_config: Dictionary = config
			if battle_config.has("dev_player_deck"):
				print("Summoner3D: Loading DEV TEST deck from BattleContext...")
				return _load_dev_deck_from_config(battle_config["dev_player_deck"])

	print("Summoner3D: Loading deck from player profile...")
	var loaded_deck: Array[Card] = DeckLoader.load_player_deck()

	if loaded_deck.is_empty():
		push_warning("Summoner3D: Failed to load from profile, falling back to static deck")
		return _load_static_deck()

	return loaded_deck

## Load dev test deck from battle configuration
func _load_dev_deck_from_config(dev_deck_config: Variant) -> Array[Card]:
	if not dev_deck_config is Array:
		push_error("Summoner3D: dev_player_deck is not an Array")
		return []

	var deck: Array[Card] = []
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
				deck.append(card)
			else:
				push_warning("Summoner3D: Failed to create dev card: %s" % catalog_id)

	print("Summoner3D: Loaded %d cards from dev_player_deck" % deck.size())
	return deck

## Emergency fallback: Create minimal deck when all strategies fail
## Uses basic warrior cards as last resort to prevent game breaking
func _create_emergency_deck() -> Array[Card]:
	print("Summoner3D: Creating emergency fallback deck (3x warrior)")

	var emergency_deck: Array[Card] = []

	# Validate CardCatalog autoload exists
	if not CardCatalog:
		push_error("Summoner3D: CardCatalog autoload not available - cannot create emergency deck")
		return emergency_deck

	# Try to create 3 warrior cards
	for i: int in 3:
		var card: Card = CardCatalog.create_card_resource("warrior")
		if card:
			emergency_deck.append(card)
		else:
			push_error("Summoner3D: Failed to create emergency warrior card %d" % i)

	if emergency_deck.is_empty():
		push_error("Summoner3D: Emergency deck creation failed - CardCatalog may be broken")
	else:
		print("Summoner3D: Created emergency deck with %d cards" % emergency_deck.size())

	return emergency_deck
