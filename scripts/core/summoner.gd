extends Node2D
class_name Summoner

## Player base/summoner that spawns units and manages cards
## Each player has one Summoner that represents their base

@export var max_hp: float = 1000.0
@export var team: Unit.Team = Unit.Team.PLAYER

## Deck and hand
@export var starting_deck: Array[Card] = []
@export var max_hand_size: int = 4

## Resources
@export var max_mana: int = 10
@export var mana_regen_rate: float = 1.0  # Mana per second

## Current state
var current_hp: float
var current_mana: int = 0
var hand: Array[Card] = []
var deck: Array[Card] = []
var is_alive: bool = true

## Signals
signal summoner_died(summoner: Summoner)
signal card_played(card: Card)
signal mana_changed(current: int, max: int)
signal hand_changed(hand: Array[Card])

func _ready() -> void:
	current_hp = max_hp
	current_mana = max_mana

	# Initialize deck
	deck = starting_deck.duplicate()
	deck.shuffle()

	# Draw starting hand
	for i in max_hand_size:
		draw_card()

	add_to_group("summoners")
	if team == Unit.Team.PLAYER:
		add_to_group("player_summoners")
	else:
		add_to_group("enemy_summoners")

	_setup_visuals()

func _process(delta: float) -> void:
	if not is_alive:
		return

	# Regenerate mana
	if current_mana < max_mana:
		current_mana = min(current_mana + int(mana_regen_rate * delta), max_mana)
		mana_changed.emit(current_mana, max_mana)

## Draw a card from the deck
func draw_card() -> void:
	if deck.is_empty():
		return

	if hand.size() >= max_hand_size:
		return

	var card = deck.pop_front()
	hand.append(card)
	hand_changed.emit(hand)

## Play a card from hand at the given position
func play_card(card_index: int, position: Vector2) -> bool:
	if card_index < 0 or card_index >= hand.size():
		return false

	var card = hand[card_index]

	if not card.can_play(current_mana):
		return false

	# Deduct mana
	current_mana -= card.mana_cost
	mana_changed.emit(current_mana, max_mana)

	# Find battlefield to spawn units in
	var battlefield = get_tree().get_first_node_in_group("battlefield")
	if battlefield == null:
		push_error("No battlefield found in scene!")
		return false

	# Play the card
	card.play(position, team, battlefield)

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

## Setup visual representation
func _setup_visuals() -> void:
	# Summoner base visual
	var base = ColorRect.new()
	base.size = Vector2(80, 120)
	base.position = Vector2(-40, -120)
	base.color = Color.DARK_BLUE if team == Unit.Team.PLAYER else Color.DARK_RED
	add_child(base)

	# HP bar
	var hp_bar_bg = ColorRect.new()
	hp_bar_bg.size = Vector2(80, 10)
	hp_bar_bg.position = Vector2(-40, -140)
	hp_bar_bg.color = Color.BLACK
	add_child(hp_bar_bg)

	var hp_bar = ColorRect.new()
	hp_bar.name = "HPBar"
	hp_bar.size = Vector2(80, 10)
	hp_bar.position = Vector2(-40, -140)
	hp_bar.color = Color.CYAN
	add_child(hp_bar)

	# Label
	var label = Label.new()
	label.name = "NameLabel"
	label.text = "PLAYER" if team == Unit.Team.PLAYER else "ENEMY"
	label.position = Vector2(-30, -160)
	add_child(label)

## Update visuals each frame
func _physics_process(_delta: float) -> void:
	if has_node("HPBar"):
		var hp_bar = get_node("HPBar") as ColorRect
		var hp_percent = current_hp / max_hp
		hp_bar.size.x = 80 * hp_percent
