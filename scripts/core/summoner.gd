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
const MANA_MAX: float = 10.0
var hand: Array[Card] = []
var deck: Array[Card] = []
var is_alive: bool = true

## Signals
signal summoner_died(summoner: Summoner)
signal card_played(card: Card)
signal card_drawn(card: Card)
signal mana_changed(current: float, max: float)
signal hand_changed(hand: Array[Card])

func _ready() -> void:
	# If loading enemy from campaign, override max_hp if specified
	if load_enemy_deck_from_campaign and team == Unit.Team.ENEMY:
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
					print("Summoner: Set enemy HP from campaign: %d" % max_hp)

	current_hp = max_hp
	mana = MANA_MAX

	# Initialize deck
	if load_deck_from_profile and team == Unit.Team.PLAYER:
		# Load deck from player's profile
		print("Summoner: Loading deck from profile...")
		deck = DeckLoader.load_player_deck()
		if deck.is_empty():
			push_error("Summoner: Failed to load deck from profile! Using empty deck.")
		else:
			print("Summoner: Successfully loaded %d cards from profile" % deck.size())
	elif load_enemy_deck_from_campaign and team == Unit.Team.ENEMY:
		# Load enemy deck from current campaign battle
		print("Summoner: Loading enemy deck from campaign...")
		deck = EnemyDeckLoader.load_enemy_deck_for_battle()
		if deck.is_empty():
			push_warning("Summoner: Failed to load enemy deck from campaign! Using fallback deck.")
			deck = starting_deck.duplicate()
		else:
			print("Summoner: Successfully loaded %d cards for enemy from campaign" % deck.size())
	else:
		# Use exported starting_deck (for testing/AI)
		deck = starting_deck.duplicate()
		print("Summoner: Using exported starting_deck (%d cards)" % deck.size())

	deck.shuffle()

	# Draw starting hand
	for i: int in max_hand_size:
		draw_card()

	add_to_group("summoners")
	if team == Unit.Team.PLAYER:
		add_to_group("player_summoners")
	else:
		add_to_group("enemy_summoners")

	# Emit initial mana state
	mana_changed.emit(mana, MANA_MAX)

func _process(delta: float) -> void:
	if not is_alive:
		return

	# Regenerate mana (FIXED: use float accumulation)
	if mana < MANA_MAX:
		mana = clamp(mana + mana_regen_rate * delta, 0.0, MANA_MAX)
		# Emit signal every frame while regenerating for smooth UI updates
		mana_changed.emit(mana, MANA_MAX)

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

## Play a card from hand at the given position
func play_card(card_index: int, spawn_position: Vector2) -> bool:
	if card_index < 0 or card_index >= hand.size():
		return false

	var card: Card = hand[card_index]

	if not card.can_play(int(mana)):
		return false

	# Deduct mana
	mana -= card.mana_cost
	mana_changed.emit(mana, MANA_MAX)

	# Find battlefield to spawn units in
	var battlefield: Node = get_tree().get_first_node_in_group("battlefield")
	if battlefield == null:
		push_error("No battlefield found in scene!")
		return false

	# Play the card
	card.play(spawn_position, team, battlefield)

	# Remove from hand and draw new card
	hand.remove_at(card_index)
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
