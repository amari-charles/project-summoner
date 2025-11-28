extends Node2D
class_name Summoner

## Player base/summoner that spawns units and manages cards
## Each player has one Summoner that represents their base

@export var max_hp: float = 1000.0
@export var team: Unit.Team = Unit.Team.PLAYER

## Deck and hand
@export var starting_deck: Array[Card] = []
@export var max_hand_size: int = 4
@export var load_deck_from_profile: bool = false  # If true, load player's deck from profile
@export var load_enemy_deck_from_campaign: bool = false  # If true, load enemy deck from current campaign battle

## Resources
@export var mana_regen_rate: float = 1.0  # Mana per second

## Current state
var current_hp: float
var mana: float = 0.0
var max_mana: float = 10.0  # Set from hero stats in _apply_hero_bonuses()
var hand: Array[Card] = []
var deck: Array[Card] = []
var discard_pile: Array[Card] = []
var is_alive: bool = true

## Signals
signal summoner_died(summoner: Summoner)
signal card_played(card: Card)
signal card_drawn(card: Card)
signal mana_changed(current: float, max: float)
signal hand_changed(hand: Array[Card])
signal deck_recycled(card_count: int)  ## Emitted when discard pile is shuffled back into deck

func _ready() -> void:
	# Wait one frame to ensure autoload services are fully initialized
	await get_tree().process_frame

	# Initialize deck and apply hero bonuses (must happen before setting current_hp/mana)
	if load_deck_from_profile and team == Unit.Team.PLAYER:
		# Load deck from player's profile
		var deck_data: Dictionary = DeckLoader.load_player_deck()

		var cards_variant: Variant = deck_data.get("cards", [])
		if cards_variant is Array:
			var cards_array: Array = cards_variant
			deck.assign(cards_array)
		if deck.is_empty():
			push_error("Summoner: Failed to load deck from profile!")

		# Apply hero bonuses
		var hero_instance_variant: Variant = deck_data.get("hero_instance")
		if hero_instance_variant is HeroInstance:
			var hero_instance: HeroInstance = hero_instance_variant
			_apply_hero_bonuses(hero_instance)
		else:
			push_warning("Summoner: No valid HeroInstance in deck data")
	elif load_enemy_deck_from_campaign and team == Unit.Team.ENEMY:
		# Load enemy deck from current campaign battle
		deck = EnemyDeckLoader.load_enemy_deck_for_battle()
		if deck.is_empty():
			push_warning("Summoner: Failed to load enemy deck from campaign! Using fallback.")
			deck = starting_deck.duplicate()

		# Override enemy max_hp from campaign if specified
		var campaign: Node = get_node_or_null("/root/Campaign")
		var profile_repo: Node = get_node_or_null("/root/ProfileRepo")
		if campaign and profile_repo:
			var profile_variant: Variant = {}
			if profile_repo.has_method("get_active_profile"):
				profile_variant = profile_repo.call("get_active_profile")
			var profile: Dictionary = profile_variant if profile_variant is Dictionary else {}
			var empty_dict: Dictionary = {}
			var campaign_progress_variant: Variant = profile.get("campaign_progress", empty_dict)
			var campaign_progress: Dictionary = campaign_progress_variant if campaign_progress_variant is Dictionary else {}
			var battle_id_variant: Variant = campaign_progress.get("current_battle", "")
			var battle_id: String = battle_id_variant if battle_id_variant is String else ""
			if battle_id != "":
				var battle_variant: Variant = {}
				if campaign.has_method("get_battle"):
					battle_variant = campaign.call("get_battle", battle_id)
				var battle: Dictionary = battle_variant if battle_variant is Dictionary else {}
				if battle.has("enemy_hp"):
					max_hp = battle.get("enemy_hp")
	else:
		# Use exported starting_deck (for testing/AI)
		deck = starting_deck.duplicate()

	# Initialize HP and mana
	current_hp = max_hp
	mana = max_mana

	deck.shuffle()

	# Draw starting hand
	for i: int in max_hand_size:
		draw_card()

	add_to_group(GroupIDs.SUMMONERS)
	if team == Unit.Team.PLAYER:
		add_to_group(GroupIDs.PLAYER_SUMMONERS)
	else:
		add_to_group(GroupIDs.ENEMY_SUMMONERS)

	# Emit initial mana state
	mana_changed.emit(mana, max_mana)

func _process(delta: float) -> void:
	if not is_alive:
		return

	# Regenerate mana (FIXED: use float accumulation)
	if mana < max_mana:
		mana = clamp(mana + mana_regen_rate * delta, 0.0, max_mana)
		# Emit signal every frame while regenerating for smooth UI updates
		mana_changed.emit(mana, max_mana)

## Draw a card from the deck
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

## Play a card from hand at the given position
func play_card(card_index: int, spawn_position: Vector2) -> bool:
	if card_index < 0 or card_index >= hand.size():
		return false

	var card: Card = hand[card_index]

	if not card.can_play(int(mana)):
		return false

	# Deduct mana
	mana -= card.mana_cost
	mana_changed.emit(mana, max_mana)

	# Find battlefield to spawn units in
	var battlefield: Node = get_tree().get_first_node_in_group("battlefield")
	if battlefield == null:
		push_error("No battlefield found in scene!")
		return false

	# Play the card
	card.play(spawn_position, team, battlefield)

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

	card_played.emit(card)
	hand_changed.emit(hand)

	return true

## Take damage (when enemy units reach this summoner)
func take_damage(damage: float) -> void:
	if not is_alive:
		return

	current_hp -= damage

	if current_hp <= 0:
		current_hp = 0
		_die()

## Handle summoner death
func _die() -> void:
	is_alive = false
	summoner_died.emit(self)

## Apply hero bonuses to summoner stats
func _apply_hero_bonuses(hero_instance: HeroInstance) -> void:
	if hero_instance == null:
		push_warning("Summoner: Cannot apply bonuses from null HeroInstance")
		return

	# Get computed stats (includes modifiers)
	var stats: Dictionary = hero_instance.get_computed_stats()

	# Set health from hero (with modifiers applied)
	var health: float = stats.get("health", 1000.0)
	max_hp = health

	# Set mana regen from hero (with modifiers applied)
	var hero_mana_regen: float = stats.get("mana_regen", 1.0)
	mana_regen_rate = hero_mana_regen

	# Set max mana from hero (with modifiers applied)
	var hero_max_mana: float = stats.get("max_mana", 10.0)
	max_mana = hero_max_mana

	# Cache hero stats in BattleContext for DamageSystem to use
	BattleContext.set_player_hero_stats(stats)

	var hero_name: String = hero_instance.config.hero_name
	var trait_count: int = hero_instance.get_all_trait_ids().size()
	print("Summoner: Applied hero stats from '%s' (Level %d, %d traits) - HP: %.0f, Mana: %.0f, Regen: %.1f/s" % [
		hero_name, hero_instance.level, trait_count, max_hp, max_mana, mana_regen_rate
	])
