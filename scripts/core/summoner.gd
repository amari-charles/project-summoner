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

@export var team: Unit3D.Team = Unit3D.Team.PLAYER

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

## Current state
var mana: float = 0.0
var max_mana: float = 50.0  ## Default max mana - fixed pool for entire battle (no regeneration)
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
var casting_vfx: Node = null  ## Active summoning circle VFX

## Summoner instance (loaded from profile when using PROFILE strategy)
var _loaded_summoner_instance: SummonerInstance = null

## Track initialization state
var _initialized: bool = false

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
	if team == Unit3D.Team.PLAYER:
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

	# Create HP bar for summoner
	HPBarManager.create_bar_for_unit(self, {
		"bar_width": HP_BAR_WIDTH,
		"offset_y": HP_BAR_OFFSET_Y,
		"show_on_damage_only": not HP_BAR_ALWAYS_VISIBLE
	})

## Initialize summoner - called by BattleCoordinator after scene is ready
## This replaces the old self-initialization pattern
func init() -> void:
	if _initialized:
		return
	_initialized = true

	print("Summoner: Initializing (team: %s)..." % ("PLAYER" if team == Unit3D.Team.PLAYER else "ENEMY"))

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
					print("Summoner: Battle uses event_sequence with empty enemy_deck - switching to DEFERRED strategy")
					deck_load_strategy = DeckLoadStrategy.DEFERRED

	# Initialize deck using strategy pattern (before HP/mana init for summoner bonuses)
	deck = _load_deck_by_strategy()

	# Apply summoner bonuses for player using PROFILE strategy
	if team == Unit3D.Team.PLAYER and deck_load_strategy == DeckLoadStrategy.PROFILE:
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
			error_msg += "Team: %s\n" % ("PLAYER" if team == Unit3D.Team.PLAYER else "ENEMY")
			error_msg += "Strategy: %s\n" % DeckLoadStrategy.keys()[deck_load_strategy]
			error_msg += "This indicates a configuration bug - check BattleContext and player profile."
			push_error(error_msg)
			assert(false, error_msg)
			is_enabled = false
			return
	else:
		print("Summoner: Loaded %d cards using %s strategy" % [deck.size(), DeckLoadStrategy.keys()[deck_load_strategy]])

	deck.shuffle()

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

func _exit_tree() -> void:
	# Kill any active tweens to prevent lambda capture errors
	if active_feedback_tween and active_feedback_tween.is_valid():
		active_feedback_tween.kill()
	# Remove HP bar
	HPBarManager.remove_bar_from_unit(self)

func draw_card() -> void:
	if deck.is_empty():
		return

	if hand.size() >= max_hand_size:
		return

	var card: Card = deck.pop_front()
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
	deck.shuffle()

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

	if summon_time > 0.0:
		# Start casting (delayed spawn)
		is_casting = true
		casting_card = card
		casting_time_remaining = summon_time
		casting_time_total = summon_time
		casting_spawn_position = spawn_position
		casting_card_index = card_index

		# Spawn summoning circle VFX at target location
		_spawn_summon_circle_vfx(spawn_position, summon_time)

		casting_started.emit(card, summon_time)
		return true
	else:
		# Instant cast (no summon_time)
		return _complete_card_play(card, card_index, spawn_position)

## Complete a card play (either immediately or after casting timer)
func _complete_card_play(card: Card, card_index: int, spawn_position: Vector3) -> bool:
	var battlefield: Node = get_tree().get_first_node_in_group("battlefield")
	if battlefield == null:
		push_error("No battlefield found in scene!")
		return false

	# Get ModifierSystem for efficient access (avoid fragile scene tree lookups)
	var modifier_system: Node = get_node_or_null("/root/ModifierSystem")

	# Play the card in 3D
	card.play_3d(spawn_position, team, battlefield, modifier_system)

	# Remove from hand and add to discard pile
	hand.remove_at(card_index)
	discard_pile.append(card)

	# Try to draw a new card
	draw_card()

	# If hand and deck are both empty, recycle discard pile and draw new hand
	if hand.is_empty() and deck.is_empty():
		_recycle_discard_pile()
		for i: int in mini(max_hand_size, deck.size()):
			draw_card()

	# Register card for XP tracking (player only)
	if team == Unit3D.Team.PLAYER and not card.instance_id.is_empty():
		BattleContext.register_card_played(card.instance_id)

	card_played.emit(card)
	hand_changed.emit(hand)

	return true

## Complete casting after summon_time delay
func _complete_casting() -> void:
	if not is_casting or not casting_card:
		return

	var card: Card = casting_card
	var index: int = casting_card_index
	var pos: Vector3 = casting_spawn_position

	# Clean up summoning circle VFX (it will auto-cleanup but we clear our reference)
	casting_vfx = null

	# Reset casting state first (before completing play which may fail)
	is_casting = false
	var completed_card: Card = casting_card  # Save for signal
	casting_card = null
	casting_card_index = -1
	casting_spawn_position = Vector3.ZERO
	casting_time_remaining = 0.0
	casting_time_total = 0.0

	# Complete the card play
	_complete_card_play(card, index, pos)
	casting_completed.emit(completed_card)

## Spawn the summoning circle VFX at the target position
func _spawn_summon_circle_vfx(spawn_pos: Vector3, duration: float) -> void:
	# Use VFXManager if available
	var vfx_manager: Node = get_node_or_null("/root/VFXManager")
	if vfx_manager and vfx_manager.has_method("play_effect"):
		casting_vfx = vfx_manager.play_effect(VFXIDs.SUMMON_CIRCLE, spawn_pos, {
			"duration": duration,
			"team": team
		})

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
	if team == Unit3D.Team.PLAYER:
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
	if team == Unit3D.Team.ENEMY:
		push_warning("Summoner: PROFILE strategy used for enemy team, using static deck instead")
		return _load_static_deck()

	# Load summoner instance directly from profile services (independent of decks)
	_load_summoner_from_profile()

	# Check for dev test deck override in BattleContext
	var battle_context: Node = get_node_or_null("/root/BattleContext")
	if battle_context:
		var config: Variant = battle_context.get("battle_config")
		if config is Dictionary:
			var battle_config: Dictionary = config
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
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if not summoner_selection:
		push_error("Summoner: SummonerSelection service not found!")
		return

	var summoner_id: String = ""
	if summoner_selection.has_method("get_active_summoner_id"):
		summoner_id = summoner_selection.call("get_active_summoner_id")

	if summoner_id.is_empty():
		push_error("Summoner: No active summoner selected in profile!")
		return

	print("Summoner: Active summoner ID: '%s'" % summoner_id)

	# Load summoner instance data from ProfileRepo
	var profile_repo: Node = get_node_or_null("/root/ProfileRepo")
	if not profile_repo:
		push_error("Summoner: ProfileRepo not found!")
		return

	var instance_data: Dictionary = {}
	if profile_repo.has_method("get_summoner_instance"):
		var instance_data_variant: Variant = profile_repo.call("get_summoner_instance", summoner_id)
		instance_data = instance_data_variant if instance_data_variant is Dictionary else {}

	if instance_data.is_empty():
		# No saved instance - create from catalog config
		print("Summoner: No saved instance, creating from catalog...")
		var summoner_catalog: Node = get_node_or_null("/root/SummonerCatalog")
		if summoner_catalog and summoner_catalog.has_method("get_summoner_config"):
			var config_variant: Variant = summoner_catalog.call("get_summoner_config", summoner_id)
			if config_variant is SummonerConfig:
				var summoner_config: SummonerConfig = config_variant
				_loaded_summoner_instance = SummonerInstance.new()
				_loaded_summoner_instance.init_from_config(summoner_config)
				print("Summoner: Created new instance from config '%s'" % summoner_config.summoner_name)
			else:
				push_error("Summoner: Could not load summoner config for '%s'" % summoner_id)
		else:
			push_error("Summoner: SummonerCatalog not available!")
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
## Uses basic warrior cards as last resort to prevent game breaking
func _create_emergency_deck() -> Array[Card]:
	print("Summoner: Creating emergency fallback deck (3x warrior)")

	var emergency_deck: Array[Card] = []

	# Validate CardCatalog autoload exists
	if not CardCatalog:
		push_error("Summoner: CardCatalog autoload not available - cannot create emergency deck")
		return emergency_deck

	# Try to create 3 neade cards (basic unit)
	for i: int in 3:
		var card: Card = CardCatalog.create_card_resource("neade")
		if card:
			emergency_deck.append(card)
		else:
			push_error("Summoner: Failed to create emergency neade card %d" % i)

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

	# Note: mana_regen is no longer used - mana is a fixed pool
	# We keep it in stats for potential future use but don't apply it

	# Set max mana from summoner (with modifiers applied)
	# In the new system, this is the player's total mana budget for the battle
	var summoner_max_mana: float = stats.get("max_mana", 50.0)
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

	# Remove HP bar
	HPBarManager.remove_bar_from_unit(self)

	summoner_destroyed.emit(self)
	print("Summoner destroyed! Team: %s" % ("PLAYER" if team == Unit3D.Team.PLAYER else "ENEMY"))

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
