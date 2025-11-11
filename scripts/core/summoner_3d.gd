extends Node3D
class_name Summoner3D

## 3D Player base/summoner that spawns units and manages cards
## Each player has one Summoner that represents their base

@export var max_hp: float = 1000.0
@export var team: Unit3D.Team = Unit3D.Team.PLAYER

## Deck and hand
@export var starting_deck: Array[Card] = []
@export var max_hand_size: int = 4
@export var load_deck_from_profile: bool = false

## Resources
@export var mana_regen_rate: float = 1.0

## Current state
var current_hp: float
var mana: float = 0.0
const MANA_MAX := 10.0
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
	# For enemy summoners, load config from BattleContext
	if team == Unit3D.Team.ENEMY:
		var battle_context = get_node_or_null("/root/BattleContext")
		if battle_context and not battle_context.battle_config.is_empty():
			var battle_config = battle_context.battle_config

			# Set enemy HP from config
			if battle_config.has("enemy_hp"):
				max_hp = battle_config.get("enemy_hp")
				print("Summoner3D: Set enemy HP from BattleContext: %d" % max_hp)

	current_hp = max_hp
	mana = MANA_MAX

	# Initialize deck
	if load_deck_from_profile and team == Unit3D.Team.PLAYER:
		print("Summoner3D: Loading deck from profile...")
		deck = DeckLoader.load_player_deck()
		if deck.is_empty():
			push_error("Summoner3D: Failed to load deck from profile! Using empty deck.")
		else:
			print("Summoner3D: Successfully loaded %d cards from profile" % deck.size())
	elif team == Unit3D.Team.ENEMY:
		# Enemy deck always comes from BattleContext
		print("Summoner3D: Loading enemy deck from BattleContext...")
		deck = EnemyDeckLoader.load_enemy_deck_for_battle()
		if deck.is_empty():
			push_warning("Summoner3D: Failed to load enemy deck! Using fallback deck.")
			deck = starting_deck.duplicate()
		else:
			print("Summoner3D: Successfully loaded %d cards for enemy from BattleContext" % deck.size())
	else:
		deck = starting_deck.duplicate()
		print("Summoner3D: Using exported starting_deck (%d cards)" % deck.size())

	deck.shuffle()

	# Draw starting hand
	for i in max_hand_size:
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

	var card = deck.pop_front()
	hand.append(card)
	card_drawn.emit(card)
	hand_changed.emit(hand)

## Play a card from hand at the given 3D position
func play_card_3d(card_index: int, position: Vector3) -> bool:
	if card_index < 0 or card_index >= hand.size():
		return false

	var card = hand[card_index]

	if not card.can_play(int(mana)):
		return false

	mana -= card.mana_cost
	mana_changed.emit(mana, MANA_MAX)

	var battlefield = get_tree().get_first_node_in_group("battlefield")
	if battlefield == null:
		push_error("No battlefield found in scene!")
		return false

	# Play the card in 3D
	card.play_3d(position, team, battlefield)

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
