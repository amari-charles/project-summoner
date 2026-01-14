extends GameController3D
class_name TestGameController

## Test Game Controller for VFX Testing
## - Infinite mana
## - Infinite enemy HP
## - Hardcoded test deck (no profile loading)
## - No time limit

## Test deck configuration - edit this to test different cards
var test_deck_cards: Array[StringName] = [
	CardIDs.FIRE_ELEMENTAL, CardIDs.FIRE_ELEMENTAL, CardIDs.FIRE_ELEMENTAL, CardIDs.FIRE_ELEMENTAL, CardIDs.FIRE_ELEMENTAL,
	CardIDs.EARTH_SPRITE, CardIDs.EARTH_SPRITE, CardIDs.EARTH_SPRITE, CardIDs.EARTH_SPRITE, CardIDs.EARTH_SPRITE,
	CardIDs.FIREBALL, CardIDs.FIREBALL, CardIDs.FIREBALL, CardIDs.FIREBALL, CardIDs.FIREBALL,
	CardIDs.FIREBALL, CardIDs.FIREBALL, CardIDs.FIREBALL, CardIDs.FIREBALL, CardIDs.FIREBALL,
	CardIDs.PUFF, CardIDs.PUFF, CardIDs.PUFF, CardIDs.PUFF, CardIDs.PUFF,
	CardIDs.FIRE_TITAN, CardIDs.FIRE_TITAN, CardIDs.FIRE_TITAN, CardIDs.FIRE_TITAN, CardIDs.FIRE_TITAN
]

func _ready() -> void:
	print("TestGameController: Initializing VFX test mode...")

	# Configure BattleContext for practice mode
	var battle_context: Node = get_node_or_null("/root/BattleContext")
	if battle_context and battle_context.has_method("configure_practice_battle"):
		battle_context.call("configure_practice_battle", {
			"enemy_deck": [{"catalog_id": CardIDs.FIRE_ELEMENTAL, "count": 30}],
			"enemy_hp": 999999.0
		})

	# Force reload ContentCatalog projectiles to bypass resource cache
	ContentCatalog._load_projectiles()
	print("TestGameController: Reloaded projectile data from disk")

	# Force projectile pool refresh to reload visuals (fixes color not updating)
	ProjectileService.refresh_pools()

	# Call parent ready
	super._ready()

	# Override player deck with test deck
	if player_summoner:
		_load_test_deck_for_summoner(player_summoner)

	# Give enemy a simple deck
	if enemy_summoner:
		_load_enemy_test_deck(enemy_summoner)

	# Set infinite HP for both summoners (summoner is the attack target)
	await get_tree().process_frame
	if enemy_summoner and "max_hp" in enemy_summoner:
		enemy_summoner.set("max_hp", 999999.0)
		enemy_summoner.set("current_hp", 999999.0)
		print("TestGameController: Enemy summoner set to infinite HP")

	if player_summoner and "max_hp" in player_summoner:
		player_summoner.set("max_hp", 999999.0)
		player_summoner.set("current_hp", 999999.0)
		print("TestGameController: Player summoner set to infinite HP")

	print("TestGameController: Test mode ready!")
	print("  - Player deck: %d cards (mostly fireballs)" % test_deck_cards.size())
	print("  - Infinite mana enabled")
	print("  - Enemy HP: 999999")
	print("  - No time limit")

func _process(_delta: float) -> void:
	# Grant infinite mana to both player and enemy
	if player_summoner and "mana" in player_summoner:
		player_summoner.mana = 999

	if enemy_summoner and "mana" in enemy_summoner:
		enemy_summoner.mana = 999

	# Don't run timer
	# Skip parent _process to disable time limit
	pass

## Load hardcoded test deck for player
func _load_test_deck_for_summoner(summoner: Summoner) -> void:
	var cards: Array[Card] = []

	for catalog_id: StringName in test_deck_cards:
		var card: Card = _load_card_resource(catalog_id)
		if card:
			cards.append(card)

	# Set the deck directly
	summoner.deck = cards
	summoner.deck.shuffle()

	# Clear hand and redraw
	summoner.hand.clear()
	for i: int in summoner.max_hand_size:
		summoner.draw_card()

	print("TestGameController: Loaded %d test cards for player" % cards.size())

## Load simple enemy deck (just fire elementals)
func _load_enemy_test_deck(summoner: Summoner) -> void:
	var cards: Array[Card] = []

	# Enemy gets 30 fire elementals (easy target practice)
	for i: int in range(30):
		var card: Card = _load_card_resource(CardIDs.FIRE_ELEMENTAL)
		if card:
			cards.append(card)

	summoner.deck = cards
	summoner.deck.shuffle()

	# Clear hand and redraw
	summoner.hand.clear()
	for i: int in summoner.max_hand_size:
		summoner.draw_card()

	print("TestGameController: Loaded %d test cards for enemy" % cards.size())

## Load a card resource from catalog ID
func _load_card_resource(catalog_id: StringName) -> Card:
	# Use CardCatalog to create card dynamically
	if not CardCatalog:
		push_error("TestGameController: CardCatalog autoload not available")
		return null

	var card: Card = CardCatalog.create_card_resource(catalog_id)

	if not card:
		push_error("TestGameController: Failed to create card from catalog: %s" % catalog_id)
		return null

	return card

## Spawn a test enemy unit on the battlefield (for testing)
## Called by spawn buttons in test_battle_abilities.tscn
func _spawn_test_enemy(catalog_id: String) -> void:
	print("TestGameController: Spawning test enemy: %s" % catalog_id)

	# Create card from catalog
	var card: Card = _load_card_resource(StringName(catalog_id))
	if not card:
		push_error("TestGameController: Failed to load card for spawn: %s" % catalog_id)
		return

	# Spawn the unit using enemy summoner
	if not enemy_summoner:
		push_error("TestGameController: No enemy summoner available")
		return

	# Add card to enemy's hand temporarily
	enemy_summoner.hand.append(card)

	# Play the card from hand (use correct API)
	var card_index: int = enemy_summoner.hand.size() - 1
	var spawn_pos: Vector3 = Vector3(5.0, 1.0, 0.0)  # Enemy territory

	if enemy_summoner.play_card_3d(card_index, spawn_pos):
		print("TestGameController: Spawned %s successfully at %s" % [catalog_id, spawn_pos])
	else:
		print("TestGameController: Failed to spawn %s (not enough mana?)" % catalog_id)

## Override game end to prevent auto-transition
func end_game(winner: UnitConstants.Team) -> void:
	if current_state == GameState.GAME_OVER:
		return

	current_state = GameState.GAME_OVER
	state_changed.emit(current_state)
	game_ended.emit(winner)
	get_tree().paused = true

	print("TestGameController: Game ended - Winner: %s" % ("Player" if winner == UnitConstants.Team.PLAYER else "Enemy"))
	print("TestGameController: Restart scene (F5) to test again")
