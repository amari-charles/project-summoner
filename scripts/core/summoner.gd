extends Node3D
class_name Summoner

## Summoner - The player character that manages cards, mana, and HP
## This is the attack target that units try to destroy to win the game

## Deck loading strategies
enum DeckLoadStrategy {
	STATIC,           ## Use starting_deck (test scenes, fallback)
	BATTLE_CONTEXT,   ## Load from BattleContext (normal enemy behavior)
	PROFILE,          ## Load from player profile (normal player behavior)
	DEFERRED         ## Don't load deck in _ready(), wait for manual override (test controllers)
}

@export var team: UnitConstants.Team = UnitConstants.Team.PLAYER

## HP (summoner is the attack target)
## Default 300 HP provides ~60 seconds of survivability against typical early-game damage
@export var max_hp: float = 300.0

## Deck and hand
@export var starting_deck: Array[Card] = []
@export var max_hand_size: int = 4
@export var deck_load_strategy: DeckLoadStrategy = DeckLoadStrategy.BATTLE_CONTEXT

## Hit feedback animation constants
const DEFAULT_FLASH_DURATION: float = 0.3
const MIN_FLASH_DURATION: float = 0.05
const FLASH_SPEED_MULTIPLIER: float = 0.3
const RECENT_HITS_DECAY_RATE: float = 2.0

## HP bar display configuration (summoner uses larger bar than units)
const HP_BAR_WIDTH: float = 1.5  # Wider than unit bars for visibility
const HP_BAR_OFFSET_Y: float = 2.5  # Height above summoner position
const HP_BAR_ALWAYS_VISIBLE: bool = true  # Always show, not just on damage

## Projectile targeting configuration
const PROJECTILE_TARGET_HEIGHT: float = 1.5  # Chest height for projectile targeting

## Hurtbox configuration (for combat hit detection)
const HURTBOX_RADIUS: float = 2.0    # Capsule radius around summoner
const HURTBOX_HEIGHT: float = 6.25   # Character sprite height

## Current state
var mana: float = 0.0
var max_mana: float = SummonerConfig.DEFAULT_MAX_MANA  ## Fixed pool for entire battle (no regeneration)
var hand: Array[Card] = []
var deck: Array[Card] = []
var discard_pile: Array[Card] = []
var is_enabled: bool = true  ## False if initialization failed (e.g., deck loading error)

## HP state (summoner is attackable)
var current_hp: float = 0.0
var is_alive: bool = true

## Hit feedback state
var recent_hits: float = 0.0
var visual: Sprite3D = null
var original_color: Color = Color.WHITE
var original_visual_position: Vector3 = Vector3.ZERO
var active_feedback_tween: Tween = null

## Casting state (player is locked during summon_time)
var is_casting: bool = false
var casting_card: Card = null
var casting_time_remaining: float = 0.0
var casting_time_total: float = 0.0
var casting_spawn_position: Vector3 = Vector3.ZERO
var casting_card_index: int = -1

## Summoner instance (loaded from profile when using PROFILE strategy)
var _loaded_summoner_instance: SummonerInstance = null

## Track initialization state
var _initialized: bool = false

## Hurtbox component (for combat hit detection)
var _hurtbox: Node = null

## Debug visualization marker
var _debug_hurtbox_marker: MeshInstance3D = null

## Signals
signal card_played(card: Card)
signal card_drawn(card: Card)
signal mana_changed(current: float, max: float)
signal hand_changed(hand: Array[Card])
signal summoner_ready(summoner: Summoner)  ## Emitted after init() completes
signal deck_recycled(card_count: int)  ## Emitted when discard pile is shuffled back into deck

## HP signals (summoner is attackable)
signal summoner_destroyed(summoner: Summoner)
signal summoner_damaged(summoner: Summoner, damage: float)
signal hp_changed(new_hp: float, new_max_hp: float)

## Casting signals (for UI feedback)
signal casting_started(card: Card, duration: float)
signal casting_progress(remaining: float, total: float)
signal casting_completed(card: Card)

func _ready() -> void:
	# Minimal setup - just add to groups for discovery
	# Full initialization happens in init() called by BattleCoordinator
	add_to_group(GroupIDs.SUMMONERS)
	add_to_group(GroupIDs.BASES)  # Summoner is the attack target
	if team == UnitConstants.Team.PLAYER:
		add_to_group(GroupIDs.PLAYER_SUMMONERS)
		add_to_group(GroupIDs.PLAYER_BASES)
	else:
		add_to_group(GroupIDs.ENEMY_SUMMONERS)
		add_to_group(GroupIDs.ENEMY_BASES)

	# Initialize HP
	current_hp = max_hp

	# Initialize visual reference for hit feedback
	if has_node("Visual"):
		visual = $Visual
		original_color = visual.modulate
		original_visual_position = visual.position

	# Create HP bar for summoner (HPBarService is C# autoload)
	var hp_bar_service: Node = get_node_or_null(CSharpAutoloads.HP_BAR_SERVICE)
	if hp_bar_service:
		hp_bar_service.create_bar_for_unit(self, {
			"bar_width": HP_BAR_WIDTH,
			"offset_y": HP_BAR_OFFSET_Y,
			"show_on_damage_only": not HP_BAR_ALWAYS_VISIBLE
		})

	# Setup hurtbox for combat hit detection
	_setup_hurtbox()

	# Configure collision shape to match hurtbox radius (single source of truth)
	_configure_collision_shape()

## Initialize summoner - called by BattleCoordinator after scene is ready
## This replaces the old self-initialization pattern
func init() -> void:
	if _initialized:
		return
	_initialized = true

	print("Summoner: Initializing (team: %s)..." % ("PLAYER" if team == UnitConstants.Team.PLAYER else "ENEMY"))

	# Auto-correct deck loading strategy based on team if using wrong default
	if team == UnitConstants.Team.PLAYER and deck_load_strategy == DeckLoadStrategy.BATTLE_CONTEXT:
		deck_load_strategy = DeckLoadStrategy.PROFILE
	elif team == UnitConstants.Team.ENEMY and deck_load_strategy == DeckLoadStrategy.PROFILE:
		deck_load_strategy = DeckLoadStrategy.BATTLE_CONTEXT

	# For enemy: Auto-detect event_sequence battles (enemies spawned via dialogue/events)
	# Pattern: battle_config has "event_sequence" AND "enemy_deck" is empty array
	# This is intentional - enemies are spawned manually via BattleDialogueController/EventSequencer
	if team == UnitConstants.Team.ENEMY and deck_load_strategy == DeckLoadStrategy.BATTLE_CONTEXT:
		if BattleContext.battle_config.has("event_sequence") and BattleContext.battle_config.has("enemy_deck"):
			var enemy_deck_variant: Variant = BattleContext.battle_config.get("enemy_deck")
			if enemy_deck_variant is Array:
				var enemy_deck_array: Array = enemy_deck_variant
				if enemy_deck_array.is_empty():
					print("Summoner: Battle uses event_sequence with empty enemy_deck - switching to DEFERRED strategy")
					deck_load_strategy = DeckLoadStrategy.DEFERRED

	# Initialize deck using strategy pattern (before HP/mana init for summoner bonuses)
	deck = _load_deck_by_strategy()

	# Apply summoner bonuses for player using PROFILE strategy
	if team == UnitConstants.Team.PLAYER and deck_load_strategy == DeckLoadStrategy.PROFILE:
		if _loaded_summoner_instance != null:
			_apply_summoner_bonuses(_loaded_summoner_instance)
		else:
			push_error("Summoner: CRITICAL - No summoner instance loaded! This is a bug.")

	# Initialize mana
	mana = max_mana

	# Handle empty deck - behavior depends on deck loading strategy
	if deck.is_empty():
		if deck_load_strategy == DeckLoadStrategy.DEFERRED:
			# DEFERRED strategy: Empty deck is expected, will be populated by controller
			print("Summoner: Deck deferred - waiting for manual population")
		elif _is_test_mode():
			# Test mode: Allow emergency fallback deck
			push_warning("Summoner: Failed to load deck in test mode. Creating emergency fallback deck.")
			deck = _create_emergency_deck()

			if deck.is_empty():
				push_error("Summoner: CRITICAL - Cannot create deck, disabling summoner")
				is_enabled = false
				return
		else:
			# Production mode: HARD FAIL - configuration is broken
			var error_msg: String = "Summoner: CRITICAL - No deck loaded in production mode!\n"
			error_msg += "Team: %s\n" % ("PLAYER" if team == UnitConstants.Team.PLAYER else "ENEMY")
			error_msg += "Strategy: %s\n" % DeckLoadStrategy.keys()[deck_load_strategy]
			error_msg += "This indicates a configuration bug - check BattleContext and player profile."
			push_error(error_msg)
			assert(false, error_msg)
			is_enabled = false
			return
	else:
		print("Summoner: Loaded %d cards using %s strategy" % [deck.size(), DeckLoadStrategy.keys()[deck_load_strategy]])

	BattleRNG.shuffle_array(deck, RNGDomain.Domain.DECK_SHUFFLE)

	# Draw starting hand
	for i: int in max_hand_size:
		draw_card()

	mana_changed.emit(mana, max_mana)
	summoner_ready.emit(self)
	print("Summoner: Initialization complete")

func _process(delta: float) -> void:
	if not is_enabled:
		return

	# Decay recent hits counter (for hit feedback animation speed)
	if recent_hits > 0:
		recent_hits -= RECENT_HITS_DECAY_RATE * delta
		recent_hits = max(recent_hits, 0.0)

	# Handle casting timer (player is locked during summon_time)
	if is_casting:
		casting_time_remaining -= delta
		casting_progress.emit(casting_time_remaining, casting_time_total)

		if casting_time_remaining <= 0.0:
			_complete_casting()

	# Update debug visualization (follows Unit3D's debug toggle)
	_update_debug_visualization()

func _exit_tree() -> void:
	# Kill any active tweens to prevent lambda capture errors
	if active_feedback_tween and active_feedback_tween.is_valid():
		active_feedback_tween.kill()
	# Clean up debug marker
	if _debug_hurtbox_marker != null:
		_debug_hurtbox_marker.queue_free()
		_debug_hurtbox_marker = null
	# Remove HP bar (HPBarService is C# autoload)
	var hp_bar_service: Node = get_node_or_null(CSharpAutoloads.HP_BAR_SERVICE)
	if hp_bar_service:
		hp_bar_service.remove_bar_from_unit(self)

## Setup hurtbox component for combat hit detection
func _setup_hurtbox() -> void:
	var hurtbox_script: Script = load("res://scripts/csharp/Combat/Hitbox/HurtboxComponent.cs")
	if hurtbox_script == null:
		push_error("Summoner: Failed to load HurtboxComponent class")
		return

	_hurtbox = hurtbox_script.new()
	add_child(_hurtbox)
	_hurtbox.configure(
		int(team),
		UnitConstants.HurtboxCategory.SUMMONER,
		HURTBOX_RADIUS,
		HURTBOX_HEIGHT
	)


## Configure collision shape from code (single source of truth with hurtbox)
func _configure_collision_shape() -> void:
	if not has_node("CollisionBody/CollisionShape3D"):
		return

	var collision_shape: CollisionShape3D = $CollisionBody/CollisionShape3D
	if collision_shape.shape is CylinderShape3D:
		collision_shape.shape.radius = HURTBOX_RADIUS
		collision_shape.shape.height = HURTBOX_HEIGHT


## Draw a card from deck into hand
## If target_index is provided, insert at that position (for in-place replacement)
## Otherwise append to end of hand
func draw_card(target_index: int = -1) -> void:
	if deck.is_empty():
		return

	if hand.size() >= max_hand_size:
		return

	var card: Card = deck.pop_front()
	if target_index >= 0 and target_index <= hand.size():
		hand.insert(target_index, card)
	else:
		hand.append(card)
	card_drawn.emit(card)
	hand_changed.emit(hand)

## Shuffle discard pile back into deck when deck is exhausted
func _recycle_discard_pile() -> void:
	if discard_pile.is_empty():
		return

	var card_count: int = discard_pile.size()
	deck = discard_pile.duplicate()
	discard_pile.clear()
	BattleRNG.shuffle_array(deck, RNGDomain.Domain.DECK_SHUFFLE)

	deck_recycled.emit(card_count)

## Play a card from hand at the given 3D position
## Returns true if the card play was accepted (may be instant or delayed by summon_time)
func play_card_3d(card_index: int, spawn_position: Vector3) -> bool:
	# Block if already casting another card
	if is_casting:
		return false

	if card_index < 0 or card_index >= hand.size():
		return false

	var card: Card = hand[card_index]

	if not card.can_play(int(mana)):
		return false

	# Deduct mana immediately (committed to the cast)
	mana -= card.mana_cost
	mana_changed.emit(mana, max_mana)

	var summon_time: float = card.summon_time

	if summon_time > 0.0 and card.card_type == Card.CardType.SUMMON:
		# Summon with spawn reveal effect (ghost materialize animation)
		# Unit spawns immediately but animates in over summon_time
		# Player is locked from playing another card during this time
		is_casting = true
		casting_card = card
		casting_time_remaining = summon_time
		casting_time_total = summon_time
		casting_spawn_position = spawn_position
		casting_card_index = card_index

		# Spawn the unit immediately with reveal effect
		_complete_card_play(card, card_index, spawn_position, summon_time)

		casting_started.emit(card, summon_time)
		return true
	else:
		# Instant cast (spells or units with no summon_time)
		return _complete_card_play(card, card_index, spawn_position)

## Complete a card play (spawns unit/casts spell and manages hand)
## spawn_duration: If > 0, applies spawn reveal effect (ghost materialize animation)
func _complete_card_play(card: Card, card_index: int, spawn_position: Vector3, spawn_duration: float = 0.0) -> bool:
	var battlefield: Node = get_tree().get_first_node_in_group("battlefield")
	if battlefield == null:
		push_error("No battlefield found in scene!")
		return false

	# Get ModifierService (C# autoload - must use get_node_or_null)
	var modifier_service: Node = get_node_or_null(CSharpAutoloads.MODIFIER_SERVICE)

	# Play the card in 3D (with optional spawn reveal effect)
	card.play_3d(spawn_position, team, battlefield, modifier_service, spawn_duration)

	# Remove from hand and add to discard pile
	hand.remove_at(card_index)
	discard_pile.append(card)

	# Draw a new card into the same slot (in-place replacement)
	draw_card(card_index)

	# If hand and deck are both empty, recycle discard pile and draw new hand
	if hand.is_empty() and deck.is_empty():
		_recycle_discard_pile()
		for i: int in mini(max_hand_size, deck.size()):
			draw_card()

	# Register card for XP tracking (player only)
	if team == UnitConstants.Team.PLAYER and not card.instance_id.is_empty():
		BattleContext.register_card_played(card.instance_id)

	card_played.emit(card)
	hand_changed.emit(hand)

	return true

## Complete casting after summon_time delay
## With spawn reveal effect, the unit is already spawned - this just cleans up casting state
func _complete_casting() -> void:
	if not is_casting or not casting_card:
		return

	# Save card reference for signal before clearing
	var completed_card: Card = casting_card

	# Reset casting state
	is_casting = false
	casting_card = null
	casting_card_index = -1
	casting_spawn_position = Vector3.ZERO
	casting_time_remaining = 0.0
	casting_time_total = 0.0

	# Emit signal
	casting_completed.emit(completed_card)

## Detect if we're running in test mode (allows emergency fallback decks)
## Note: With DEFERRED strategy, this is only used as a safety net for legacy scenarios
func _is_test_mode() -> bool:
	# Check via game_controller group
	var game_controller: Node = get_tree().get_first_node_in_group(GroupIDs.GAME_CONTROLLER)
	if game_controller and game_controller is TestGameController:
		return true

	# Check root node of scene (test scenes have test controller as root)
	var root: Node = get_tree().current_scene
	if root and root is TestGameController:
		return true

	# Check if BattleContext is in practice mode
	# PRACTICE = 1 in BattleContext enum
	if BattleContext.current_mode == 1:
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
			print("Summoner: Using DEFERRED strategy - deck will be set manually later")
			return []  # Empty deck, will be populated by controller
		_:
			push_error("Summoner: Unknown deck load strategy %d" % deck_load_strategy)
			return []

## Strategy: Load from starting_deck (test scenes, fallback)
func _load_static_deck() -> Array[Card]:
	print("Summoner: Using static starting_deck")
	return starting_deck.duplicate()

## Strategy: Load from BattleContext (normal enemy behavior)
func _load_battle_context_deck() -> Array[Card]:
	if team == UnitConstants.Team.PLAYER:
		push_warning("Summoner: BATTLE_CONTEXT strategy used for player team, using static deck instead")
		return _load_static_deck()

	print("Summoner: Loading enemy deck from BattleContext...")
	var loaded_deck: Array[Card] = EnemyDeckLoader.load_enemy_deck_for_battle()

	if loaded_deck.is_empty():
		push_warning("Summoner: Failed to load from BattleContext, falling back to static deck")
		return _load_static_deck()

	return loaded_deck

## Strategy: Load from player profile (normal player behavior)
func _load_profile_deck() -> Array[Card]:
	if team == UnitConstants.Team.ENEMY:
		push_warning("Summoner: PROFILE strategy used for enemy team, using static deck instead")
		return _load_static_deck()

	# Load summoner instance directly from profile services (independent of decks)
	_load_summoner_from_profile()

	# Check for dev test deck override in BattleContext
	var battle_config: Dictionary = BattleContext.battle_config
	if battle_config.has("dev_player_deck"):
		print("Summoner: Loading DEV TEST deck (summoner stats still apply)...")
		return _load_dev_deck_from_config(battle_config["dev_player_deck"])

	# Normal path: use profile deck via DeckLoader
	print("Summoner: Loading deck from player profile...")
	var deck_data: Dictionary = DeckLoader.load_player_deck()
	var loaded_deck_variant: Variant = deck_data.get("cards", [])
	var loaded_deck: Array[Card] = []
	if loaded_deck_variant is Array:
		var temp_array: Array = loaded_deck_variant
		loaded_deck.assign(temp_array)

	if loaded_deck.is_empty():
		push_warning("Summoner: Failed to load from profile, falling back to static deck")
		return _load_static_deck()

	return loaded_deck

## Load summoner instance directly from profile services
## This is independent of deck loading - summoners exist even without decks
func _load_summoner_from_profile() -> void:
	print("Summoner: Loading summoner from player profile...")

	# Get active summoner ID via SummonerSelection service
	var summoner_id: String = SummonerSelection.GetActiveSummonerId()

	if summoner_id.is_empty():
		push_error("Summoner: No active summoner selected in profile!")
		return

	print("Summoner: Active summoner ID: '%s'" % summoner_id)

	# Load summoner instance data from ProfileRepo
	var instance_data: Dictionary = ProfileRepo.get_summoner_instance(summoner_id)

	if instance_data.is_empty():
		# No saved instance - create from catalog config
		print("Summoner: No saved instance, creating from catalog...")
		var summoner_config: SummonerConfig = SummonerCatalog.get_summoner_config(summoner_id)
		if summoner_config:
			_loaded_summoner_instance = SummonerInstance.new()
			_loaded_summoner_instance.init_from_config(summoner_config)
			print("Summoner: Created new instance from config '%s'" % summoner_config.summoner_name)
		else:
			push_error("Summoner: Could not load summoner config for '%s'" % summoner_id)
	else:
		# Load from saved instance data
		_loaded_summoner_instance = SummonerInstance.from_dict(instance_data)
		if _loaded_summoner_instance:
			print("Summoner: Loaded summoner instance '%s' (Level %d)" % [
				_loaded_summoner_instance.config.summoner_name if _loaded_summoner_instance.config else "Unknown",
				_loaded_summoner_instance.level
			])
		else:
			push_error("Summoner: Failed to create SummonerInstance from saved data!")

## Load dev test deck from battle configuration
func _load_dev_deck_from_config(dev_deck_config: Variant) -> Array[Card]:
	if not dev_deck_config is Array:
		push_error("Summoner: dev_player_deck is not an Array")
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
				push_warning("Summoner: Failed to create dev card: %s" % catalog_id)

	print("Summoner: Loaded %d cards from dev_player_deck" % loaded_deck.size())
	return loaded_deck

## Emergency fallback: Create minimal deck when all strategies fail
## Uses basic fire_elemental cards as last resort to prevent game breaking
func _create_emergency_deck() -> Array[Card]:
	print("Summoner: Creating emergency fallback deck (3x fire_elemental)")

	var emergency_deck: Array[Card] = []

	# Validate CardCatalog autoload exists
	if not CardCatalog:
		push_error("Summoner: CardCatalog autoload not available - cannot create emergency deck")
		return emergency_deck

	# Try to create 3 fire_elemental cards (basic unit)
	for i: int in 3:
		var card: Card = CardCatalog.create_card_resource("fire_elemental")
		if card:
			emergency_deck.append(card)
		else:
			push_error("Summoner: Failed to create emergency fire_elemental card %d" % i)

	if emergency_deck.is_empty():
		push_error("Summoner: Emergency deck creation failed - CardCatalog may be broken")
	else:
		print("Summoner: Created emergency deck with %d cards" % emergency_deck.size())

	return emergency_deck

## Apply summoner bonuses to summoner stats
func _apply_summoner_bonuses(summoner_instance: SummonerInstance) -> void:
	if summoner_instance == null:
		push_warning("Summoner: Cannot apply bonuses from null SummonerInstance")
		return

	# Get computed stats (includes modifiers)
	var stats: Dictionary = summoner_instance.get_computed_stats()

	# Set max mana from summoner (with modifiers applied)
	# In the new system, this is the player's total mana budget for the battle
	var summoner_max_mana: float = stats.get("max_mana", SummonerConfig.DEFAULT_MAX_MANA)
	max_mana = summoner_max_mana

	# Cache summoner stats in BattleContext for DamageSystem to use
	BattleContext.set_player_summoner_stats(stats)

	# Apply max_hp from summoner stats if available
	var summoner_max_hp: float = stats.get("max_hp", 300.0)
	max_hp = summoner_max_hp
	current_hp = max_hp
	hp_changed.emit(current_hp, max_hp)

	var summoner_name: String = summoner_instance.config.summoner_name
	var trait_count: int = summoner_instance.get_all_trait_ids().size()
	print("Summoner: Applied summoner bonuses from '%s' (Level %d, %d traits) - Max Mana: %.0f" % [
		summoner_name, summoner_instance.level, trait_count, max_mana
	])

## =============================================================================
## COMBAT (Summoner is attackable)
## =============================================================================

## Take damage from units
func take_damage(damage: float) -> void:
	if not is_alive:
		return

	# Track attack intensity for dynamic feedback
	recent_hits += 1.0

	# Play hit feedback animation
	_play_hit_feedback()

	current_hp -= damage
	current_hp = max(current_hp, 0.0)

	# Emit signals for HP bar and damage feedback
	hp_changed.emit(current_hp, max_hp)
	summoner_damaged.emit(self, damage)

	if current_hp <= 0:
		_destroy()

## Destroy the summoner (game over for this team)
func _destroy() -> void:
	is_alive = false

	# Kill any active feedback animations
	if active_feedback_tween and active_feedback_tween.is_valid():
		active_feedback_tween.kill()
	active_feedback_tween = null

	# Restore visual to original state
	if visual and is_instance_valid(visual):
		visual.modulate = original_color
		visual.position = original_visual_position

	# Remove HP bar (HPBarService is C# autoload)
	var hp_bar_service: Node = get_node_or_null(CSharpAutoloads.HP_BAR_SERVICE)
	if hp_bar_service:
		hp_bar_service.remove_bar_from_unit(self)

	summoner_destroyed.emit(self)
	print("Summoner destroyed! Team: %s" % ("PLAYER" if team == UnitConstants.Team.PLAYER else "ENEMY"))

## Play hit feedback animation (flash + shake)
func _play_hit_feedback() -> void:
	if not visual or not is_alive:
		return

	# Kill previous tween if still running
	if active_feedback_tween and active_feedback_tween.is_valid():
		active_feedback_tween.kill()

	# Calculate duration based on attack intensity
	var intensity_factor: float = 1.0 + (recent_hits * FLASH_SPEED_MULTIPLIER)
	var flash_duration: float = max(MIN_FLASH_DURATION, DEFAULT_FLASH_DURATION / intensity_factor)

	var flash_to_white: float = flash_duration * 0.4
	var flash_return: float = flash_duration * 0.6
	var shake_out: float = flash_duration * 0.35
	var shake_return: float = flash_duration * 0.25

	active_feedback_tween = create_tween()
	active_feedback_tween.set_parallel(true)

	# Flash effect
	active_feedback_tween.tween_property(visual, "modulate", Color.WHITE, flash_to_white)
	active_feedback_tween.chain().tween_property(visual, "modulate", original_color, flash_return)

	# Shake effect
	var shake_offset: Vector3 = Vector3(randf_range(-0.15, 0.15), randf_range(-0.15, 0.15), 0)
	active_feedback_tween.tween_property(visual, "position", original_visual_position + shake_offset, shake_out)
	active_feedback_tween.chain().tween_property(visual, "position", original_visual_position, shake_return)

## Get the position where projectiles should aim at this summoner.
## Returns center mass (roughly chest height for visual feedback).
func get_projectile_target_position() -> Vector3:
	return global_position + Vector3(0, PROJECTILE_TARGET_HEIGHT, 0)


## =============================================================================
## DEBUG VISUALIZATION
## =============================================================================

## Update debug visualization marker based on Unit3D's debug toggle state
func _update_debug_visualization() -> void:
	var debug_enabled: bool = Unit3D.IsDebugHurtboxEnabled()

	if debug_enabled:
		_update_debug_hurtbox_marker()
	else:
		# Clean up marker when debug is disabled
		if _debug_hurtbox_marker != null:
			_debug_hurtbox_marker.queue_free()
			_debug_hurtbox_marker = null


## Create/update the hurtbox debug marker (capsule)
func _update_debug_hurtbox_marker() -> void:
	if _hurtbox == null:
		return

	if _debug_hurtbox_marker == null:
		_debug_hurtbox_marker = _create_debug_capsule_mesh(
			HURTBOX_RADIUS,
			HURTBOX_HEIGHT,
			Color(0.2, 1.0, 0.2, 0.3)  # Green (matches unit hurtbox color)
		)
		add_child(_debug_hurtbox_marker)

	# Position at center height
	_debug_hurtbox_marker.position = Vector3(0, HURTBOX_HEIGHT / 2, 0)


## Create a debug capsule mesh for visualization
func _create_debug_capsule_mesh(radius: float, height: float, color: Color) -> MeshInstance3D:
	var mesh_instance: MeshInstance3D = MeshInstance3D.new()

	var capsule: CapsuleMesh = CapsuleMesh.new()
	capsule.radius = radius
	capsule.height = height
	mesh_instance.mesh = capsule

	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = color
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	material.depth_draw_mode = BaseMaterial3D.DEPTH_DRAW_DISABLED
	material.no_depth_test = true
	material.render_priority = 100
	mesh_instance.material_override = material

	return mesh_instance
