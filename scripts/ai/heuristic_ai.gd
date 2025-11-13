extends AIController
class_name HeuristicAI

## Smart rule-based AI with strategic decision making
## Adapts to battlefield state, uses personalities, and plays intelligently

enum Personality { AGGRESSIVE, DEFENSIVE, BALANCED, SPELL_FOCUSED }
enum BattlefieldState { LOSING_BADLY, LOSING, EVEN, WINNING }

## Configuration
@export var personality: Personality = Personality.BALANCED
@export var difficulty: int = 3  # 1-5, affects decision quality and speed
@export var play_interval_min: float = 3.0
@export var play_interval_max: float = 6.0

## State
var play_timer: float = 0.0
var next_play_time: float = 0.0

func _ready() -> void:
	if summoner == null:
		summoner = get_parent() as Summoner
	_set_next_play_time()

func _process(delta: float) -> void:
	var is_alive_variant: Variant = summoner.get("is_alive") if summoner else false
	var is_alive: bool = is_alive_variant if is_alive_variant is bool else false
	if summoner == null or not is_alive:
		return

	play_timer += delta

	if play_timer >= next_play_time and should_play_card():
		var card_index: int = select_card_to_play()
		if card_index != -1:
			var hand_variant: Variant = summoner.get("hand")
			var hand: Array = hand_variant if hand_variant is Array else []
			if card_index >= 0 and card_index < hand.size():
				var card_variant: Variant = hand[card_index]
				var card: Card = card_variant if card_variant is Card else null
				if card:
					# Check if summoner is 3D or 2D
					if summoner.has_method("play_card_3d"):
						var pos_2d: Vector2 = select_spawn_position(card)
						var pos_3d: Vector3 = BattlefieldConstants.screen_to_world_3d(pos_2d)
						summoner.call("play_card_3d", card_index, pos_3d)
					else:
						var position: Vector2 = select_spawn_position(card)
						summoner.call("play_card", card_index, position)
		_set_next_play_time()

func on_battle_start() -> void:
	play_timer = 0.0
	_set_next_play_time()

## Decide if we should play a card now
func should_play_card() -> bool:
	var hand_variant: Variant = summoner.get("hand")
	var hand: Array = hand_variant if hand_variant is Array else []
	if hand.is_empty():
		return false

	# Check if we have any playable cards
	var has_playable: bool = false
	var mana_variant: Variant = summoner.get("mana")
	var mana: float = mana_variant if mana_variant is float else (mana_variant if mana_variant is int else 0.0)
	var mana_int: int = int(mana)
	for card_variant: Variant in hand:
		var card: Card = card_variant if card_variant is Card else null
		if card and card.can_play(mana_int):
			has_playable = true
			break

	return has_playable

## Select which card to play based on strategy
func select_card_to_play() -> int:
	var hand_variant: Variant = summoner.get("hand")
	var hand: Array = hand_variant if hand_variant is Array else []
	if hand.is_empty():
		return -1

	var battlefield_state: BattlefieldState = _evaluate_battlefield_state()
	var best_card_index: int = -1
	var best_score: float = -INF

	var mana_variant: Variant = summoner.get("mana")
	var mana: float = mana_variant if mana_variant is float else (mana_variant if mana_variant is int else 0.0)
	var mana_int: int = int(mana)

	# Score each playable card
	for i: int in range(hand.size()):
		var card_variant: Variant = hand[i]
		var card: Card = card_variant if card_variant is Card else null
		if not card or not card.can_play(mana_int):
			continue

		var score: float = _score_card(card, battlefield_state)
		if score > best_score:
			best_score = score
			best_card_index = i

	return best_card_index

## Select spawn position based on strategy
func select_spawn_position(card: Card) -> Vector2:
	var zone: String = _select_spawn_zone(card)
	return _get_random_position_in_zone(zone)

## Score a card based on current situation
func _score_card(card: Card, state: BattlefieldState) -> float:
	var score: float = 0.0

	# Base score: mana efficiency
	score += 10.0 - card.mana_cost  # Prefer cheaper cards slightly

	# Adjust based on card type
	match card.card_type:
		Card.CardType.SUMMON:
			score += _score_summon_card(card, state)
		Card.CardType.SPELL:
			score += _score_spell_card(card, state)

	# Personality preferences
	score += _apply_personality_bonus(card)

	# Difficulty affects randomness (higher difficulty = more optimal play)
	var randomness: float = 5.0 * (6 - difficulty)  # difficulty 1 = ±25, difficulty 5 = ±5
	score += randf_range(-randomness, randomness)

	return score

## Score summon cards
func _score_summon_card(card: Card, state: BattlefieldState) -> float:
	var score: float = 10.0  # Base preference for summons

	match state:
		BattlefieldState.LOSING_BADLY:
			score += 15.0  # Desperately need units
		BattlefieldState.LOSING:
			score += 10.0
		BattlefieldState.WINNING:
			score += 5.0  # Still good but less urgent

	return score

## Score spell cards
func _score_spell_card(card: Card, state: BattlefieldState) -> float:
	var score: float = 5.0  # Base preference for spells

	# Check if there are enemy units to target
	var enemy_unit_count: int = count_enemy_units()

	match state:
		BattlefieldState.LOSING_BADLY:
			if enemy_unit_count > 3:
				score += 20.0  # Use spells to clear threats
		BattlefieldState.LOSING:
			if enemy_unit_count > 2:
				score += 10.0
		BattlefieldState.WINNING:
			score += 8.0  # Good for finishing

	# If no enemies, spells are less useful
	if enemy_unit_count == 0:
		score -= 10.0

	return score

## Apply personality bonuses to card scores
func _apply_personality_bonus(card: Card) -> float:
	var bonus: float = 0.0

	match personality:
		Personality.AGGRESSIVE:
			if card.card_type == Card.CardType.SUMMON:
				bonus += 5.0
			if card.mana_cost <= 3:  # Prefer cheaper cards for faster spam
				bonus += 3.0

		Personality.DEFENSIVE:
			# TODO: When we have wall cards, prefer them
			if card.mana_cost >= 4:  # Prefer higher cost units (stronger)
				bonus += 3.0

		Personality.SPELL_FOCUSED:
			if card.card_type == Card.CardType.SPELL:
				bonus += 10.0
			else:
				bonus -= 3.0

		Personality.BALANCED:
			# No special bonuses, balanced play
			pass

	return bonus

## Evaluate current battlefield state
func _evaluate_battlefield_state() -> BattlefieldState:
	var our_units: int = count_friendly_units()
	var enemy_units: int = count_enemy_units()
	var our_hp_ratio: float = get_our_base_hp_ratio()
	var enemy_hp_ratio: float = get_enemy_base_hp_ratio()

	# Calculate advantage scores
	var unit_advantage: float = float(our_units - enemy_units)
	var hp_advantage: float = our_hp_ratio - enemy_hp_ratio

	# Combined score
	var total_advantage: float = unit_advantage * 0.5 + hp_advantage * 0.5

	if total_advantage < -0.4 or our_hp_ratio < 0.3:
		return BattlefieldState.LOSING_BADLY
	elif total_advantage < -0.1:
		return BattlefieldState.LOSING
	elif total_advantage > 0.2:
		return BattlefieldState.WINNING
	else:
		return BattlefieldState.EVEN

## Select which zone to spawn in
func _select_spawn_zone(card: Card) -> String:
	var state: BattlefieldState = _evaluate_battlefield_state()

	# Determine zone preference based on personality and state
	match personality:
		Personality.AGGRESSIVE:
			if state == BattlefieldState.WINNING:
				return "aggressive"
			elif state == BattlefieldState.EVEN:
				return "neutral"
			else:
				return "neutral"  # Regroup when losing

		Personality.DEFENSIVE:
			if state == BattlefieldState.LOSING_BADLY:
				return "defensive"
			elif state == BattlefieldState.LOSING:
				return "defensive"
			else:
				return "neutral"

		Personality.SPELL_FOCUSED:
			return "neutral"  # Spells from center

		Personality.BALANCED:
			if state == BattlefieldState.LOSING:
				return "defensive"
			elif state == BattlefieldState.WINNING:
				return "aggressive"
			else:
				return "neutral"

	return "neutral"

## Get random position within a zone
func _get_random_position_in_zone(zone: String) -> Vector2:
	var bounds: Rect2 = get_battlefield_bounds()
	var x: float = 0.0
	var y: float = 0.0

	# X position based on zone and team
	var summoner_team_variant: Variant = summoner.get("team")
	var summoner_team: int = summoner_team_variant if summoner_team_variant is int else Unit.Team.PLAYER
	if summoner_team == Unit.Team.ENEMY:
		# Enemy spawns on right side
		match zone:
			"defensive":
				x = randf_range(bounds.size.x * 0.75, bounds.size.x * 0.95)
			"neutral":
				x = randf_range(bounds.size.x * 0.5, bounds.size.x * 0.7)
			"aggressive":
				x = randf_range(bounds.size.x * 0.3, bounds.size.x * 0.5)
			_:
				x = randf_range(bounds.size.x * 0.6, bounds.size.x * 0.8)
	else:
		# Player spawns on left side
		match zone:
			"defensive":
				x = randf_range(bounds.size.x * 0.05, bounds.size.x * 0.25)
			"neutral":
				x = randf_range(bounds.size.x * 0.3, bounds.size.x * 0.5)
			"aggressive":
				x = randf_range(bounds.size.x * 0.5, bounds.size.x * 0.7)
			_:
				x = randf_range(bounds.size.x * 0.2, bounds.size.x * 0.4)

	# Y position - full height with some margin
	y = randf_range(bounds.size.y * 0.2, bounds.size.y * 0.8)

	return Vector2(x, y)

## Set next play time based on state and difficulty
func _set_next_play_time() -> void:
	play_timer = 0.0

	var state: BattlefieldState = _evaluate_battlefield_state()
	var base_min: float = play_interval_min
	var base_max: float = play_interval_max

	# Adjust intervals based on battlefield state
	match state:
		BattlefieldState.LOSING_BADLY:
			base_min *= 0.5
			base_max *= 0.5
		BattlefieldState.LOSING:
			base_min *= 0.7
			base_max *= 0.7
		BattlefieldState.WINNING:
			base_min *= 1.3
			base_max *= 1.3

	# Adjust based on difficulty (higher difficulty = faster play)
	var difficulty_factor: float = 1.0 - (difficulty - 3) * 0.1  # diff 1 = 1.2x, diff 5 = 0.8x
	base_min *= difficulty_factor
	base_max *= difficulty_factor

	next_play_time = randf_range(base_min, base_max)
