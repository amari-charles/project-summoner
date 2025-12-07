extends AIController
class_name HeuristicAI

## Smart rule-based AI with strategic decision making
## Adapts to battlefield state, uses personalities, and plays intelligently

enum Personality { AGGRESSIVE, DEFENSIVE, BALANCED, SPELL_FOCUSED }
enum BattlefieldState { LOSING_BADLY, LOSING, EVEN, WINNING }

# === CARD SCORING CONSTANTS ===
const SCORE_MANA_EFFICIENCY_BASE: float = 10.0
const SCORE_BASE_SUMMON: float = 10.0
const SCORE_BASE_SPELL: float = 5.0
const SCORE_LOSING_BADLY_SUMMON_BONUS: float = 15.0
const SCORE_LOSING_SUMMON_BONUS: float = 10.0
const SCORE_WINNING_SUMMON_BONUS: float = 5.0
const SCORE_LOSING_BADLY_SPELL_BONUS: float = 20.0
const SCORE_LOSING_SPELL_BONUS: float = 10.0
const SCORE_WINNING_SPELL_BONUS: float = 8.0
const SCORE_NO_ENEMIES_SPELL_PENALTY: float = 10.0

# === ENEMY COUNT THRESHOLDS ===
const ENEMY_COUNT_THRESHOLD_LOSING_BADLY: int = 3
const ENEMY_COUNT_THRESHOLD_LOSING: int = 2

# === PERSONALITY BONUSES ===
const PERSONALITY_AGGRESSIVE_SUMMON_BONUS: float = 5.0
const PERSONALITY_AGGRESSIVE_CHEAP_THRESHOLD: int = 3
const PERSONALITY_AGGRESSIVE_CHEAP_BONUS: float = 3.0
const PERSONALITY_DEFENSIVE_EXPENSIVE_THRESHOLD: int = 4
const PERSONALITY_DEFENSIVE_EXPENSIVE_BONUS: float = 3.0
const PERSONALITY_SPELL_FOCUSED_BONUS: float = 10.0
const PERSONALITY_SPELL_FOCUSED_PENALTY: float = 3.0

# === BATTLEFIELD STATE THRESHOLDS ===
const STATE_UNIT_ADVANTAGE_WEIGHT: float = 0.5
const STATE_HP_ADVANTAGE_WEIGHT: float = 0.5
const STATE_LOSING_BADLY_THRESHOLD: float = -0.4
const STATE_LOSING_BADLY_HP_RATIO: float = 0.3
const STATE_LOSING_THRESHOLD: float = -0.1
const STATE_WINNING_THRESHOLD: float = 0.2

# === DIFFICULTY/RANDOMNESS ===
const DIFFICULTY_DEFAULT: int = 3
const DIFFICULTY_MAX: int = 6
const DIFFICULTY_RANDOMNESS_MULTIPLIER: float = 5.0
const DIFFICULTY_TIMING_BASELINE: int = 3
const DIFFICULTY_TIMING_SCALE: float = 0.1

# === PLAY TIMING ===
const PLAY_INTERVAL_MIN_DEFAULT: float = 3.0
const PLAY_INTERVAL_MAX_DEFAULT: float = 6.0
const TIMING_LOSING_BADLY_MULTIPLIER: float = 0.5
const TIMING_LOSING_MULTIPLIER: float = 0.7
const TIMING_WINNING_MULTIPLIER: float = 1.3

# === SPAWN POSITIONING ===
## Spawn lane ratios (relative to battlefield Z bounds from camera's map_rect_xz)
## Center offset: -0.1875 = 18.75% toward negative Z from center (battle lane is slightly offset)
## Width ratio: 0.375 = spawn area covers 37.5% of total Z range (battle lane narrower than camera view)
const SPAWN_LANE_CENTER_OFFSET_RATIO: float = -0.1875
const SPAWN_LANE_WIDTH_RATIO: float = 0.375

## Configuration
@export var personality: Personality = Personality.BALANCED
@export var difficulty: int = DIFFICULTY_DEFAULT
@export var play_interval_min: float = PLAY_INTERVAL_MIN_DEFAULT
@export var play_interval_max: float = PLAY_INTERVAL_MAX_DEFAULT

## State
var play_timer: float = 0.0
var next_play_time: float = 0.0

func _ready() -> void:
	if summoner == null:
		var parent: Node = get_parent()
		if parent is Summoner:
			summoner = parent
	_set_next_play_time()

func _process(delta: float) -> void:
	if summoner == null or not summoner.is_enabled:
		return

	play_timer += delta

	if play_timer >= next_play_time and should_play_card():
		var card_index: int = select_card_to_play()
		if card_index != -1 and card_index < summoner.hand.size():
			var card: Card = summoner.hand[card_index]
			var xz_pos: Vector2 = select_spawn_position(card)
			# Direct 3D position from world coordinates - no screen conversion needed
			var pos_3d: Vector3 = Vector3(xz_pos.x, 0.0, xz_pos.y)
			summoner.play_card_3d(card_index, pos_3d)
		_set_next_play_time()

func on_battle_start() -> void:
	play_timer = 0.0
	_set_next_play_time()

## Decide if we should play a card now
func should_play_card() -> bool:
	if summoner.hand.is_empty():
		return false

	# Check if we have any playable cards
	var mana_int: int = int(summoner.mana)
	for card: Card in summoner.hand:
		if card.can_play(mana_int):
			return true

	return false

## Select which card to play based on strategy
func select_card_to_play() -> int:
	if summoner.hand.is_empty():
		return -1

	var battlefield_state: BattlefieldState = _evaluate_battlefield_state()
	var best_card_index: int = -1
	var best_score: float = -INF
	var mana_int: int = int(summoner.mana)

	# Score each playable card
	for i: int in range(summoner.hand.size()):
		var card: Card = summoner.hand[i]
		if not card.can_play(mana_int):
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
	score += SCORE_MANA_EFFICIENCY_BASE - card.mana_cost  # Prefer cheaper cards slightly

	# Adjust based on card type
	match card.card_type:
		Card.CardType.SUMMON:
			score += _score_summon_card(card, state)
		Card.CardType.SPELL:
			score += _score_spell_card(card, state)

	# Personality preferences
	score += _apply_personality_bonus(card)

	# Difficulty affects randomness (higher difficulty = more optimal play)
	var randomness: float = DIFFICULTY_RANDOMNESS_MULTIPLIER * (DIFFICULTY_MAX - difficulty)
	score += randf_range(-randomness, randomness)

	return score

## Score summon cards
func _score_summon_card(_card: Card, state: BattlefieldState) -> float:
	var score: float = SCORE_BASE_SUMMON

	match state:
		BattlefieldState.LOSING_BADLY:
			score += SCORE_LOSING_BADLY_SUMMON_BONUS  # Desperately need units
		BattlefieldState.LOSING:
			score += SCORE_LOSING_SUMMON_BONUS
		BattlefieldState.WINNING:
			score += SCORE_WINNING_SUMMON_BONUS  # Still good but less urgent

	return score

## Score spell cards
func _score_spell_card(_card: Card, state: BattlefieldState) -> float:
	var score: float = SCORE_BASE_SPELL

	# Check if there are enemy units to target
	var enemy_unit_count: int = count_enemy_units()

	match state:
		BattlefieldState.LOSING_BADLY:
			if enemy_unit_count > ENEMY_COUNT_THRESHOLD_LOSING_BADLY:
				score += SCORE_LOSING_BADLY_SPELL_BONUS  # Use spells to clear threats
		BattlefieldState.LOSING:
			if enemy_unit_count > ENEMY_COUNT_THRESHOLD_LOSING:
				score += SCORE_LOSING_SPELL_BONUS
		BattlefieldState.WINNING:
			score += SCORE_WINNING_SPELL_BONUS  # Good for finishing

	# If no enemies, spells are less useful
	if enemy_unit_count == 0:
		score -= SCORE_NO_ENEMIES_SPELL_PENALTY

	return score

## Apply personality bonuses to card scores
func _apply_personality_bonus(card: Card) -> float:
	var bonus: float = 0.0

	match personality:
		Personality.AGGRESSIVE:
			if card.card_type == Card.CardType.SUMMON:
				bonus += PERSONALITY_AGGRESSIVE_SUMMON_BONUS
			if card.mana_cost <= PERSONALITY_AGGRESSIVE_CHEAP_THRESHOLD:
				bonus += PERSONALITY_AGGRESSIVE_CHEAP_BONUS

		Personality.DEFENSIVE:
			if card.mana_cost >= PERSONALITY_DEFENSIVE_EXPENSIVE_THRESHOLD:
				bonus += PERSONALITY_DEFENSIVE_EXPENSIVE_BONUS

		Personality.SPELL_FOCUSED:
			if card.card_type == Card.CardType.SPELL:
				bonus += PERSONALITY_SPELL_FOCUSED_BONUS
			else:
				bonus -= PERSONALITY_SPELL_FOCUSED_PENALTY

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
	var total_advantage: float = unit_advantage * STATE_UNIT_ADVANTAGE_WEIGHT + hp_advantage * STATE_HP_ADVANTAGE_WEIGHT

	if total_advantage < STATE_LOSING_BADLY_THRESHOLD or our_hp_ratio < STATE_LOSING_BADLY_HP_RATIO:
		return BattlefieldState.LOSING_BADLY
	elif total_advantage < STATE_LOSING_THRESHOLD:
		return BattlefieldState.LOSING
	elif total_advantage > STATE_WINNING_THRESHOLD:
		return BattlefieldState.WINNING
	else:
		return BattlefieldState.EVEN

## Select which zone to spawn in
func _select_spawn_zone(_card: Card) -> String:
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

## Get random position within a zone (in 3D world coordinates, XZ plane)
## Returns Vector2 where x = world X, y = world Z
func _get_random_position_in_zone(zone: String) -> Vector2:
	var bounds: Rect2 = get_battlefield_bounds_3d()  # XZ plane in world space
	var x: float = 0.0
	var z: float = 0.0

	var summoner_team: int = summoner.team

	# Calculate zone positions as percentages of the battlefield
	# bounds.position.x = left edge (negative), bounds.end.x = right edge (positive)
	var right_edge: float = bounds.end.x  # Positive X (enemy side)
	var left_edge: float = bounds.position.x  # Negative X (player side)

	if summoner_team == Unit3D.Team.ENEMY:
		# Enemy spawns on positive X side (right half of battlefield)
		match zone:
			"defensive":
				# Near enemy base (far right): 75-95% of right half
				x = randf_range(right_edge * 0.75, right_edge * 0.95)
			"neutral":
				# Middle ground: 30-60% of right half
				x = randf_range(right_edge * 0.30, right_edge * 0.60)
			"aggressive":
				# Pushing toward player: 5-30% of right half (near center)
				x = randf_range(right_edge * 0.05, right_edge * 0.30)
			_:
				# Default: safe middle position
				x = randf_range(right_edge * 0.40, right_edge * 0.70)
	else:
		# Player AI spawns on negative X side (left half of battlefield)
		match zone:
			"defensive":
				# Near player base (far left): 75-95% of left half
				x = randf_range(left_edge * 0.75, left_edge * 0.95)
			"neutral":
				# Middle ground: 30-60% of left half
				x = randf_range(left_edge * 0.30, left_edge * 0.60)
			"aggressive":
				# Pushing toward enemy: 5-30% of left half (near center)
				x = randf_range(left_edge * 0.05, left_edge * 0.30)
			_:
				# Default: safe middle position
				x = randf_range(left_edge * 0.40, left_edge * 0.70)

	# Z position - spawn in the battle lane (derived from camera bounds)
	# Battle lane is narrower than camera view and slightly offset toward negative Z
	var z_center: float = bounds.position.y + bounds.size.y * 0.5  # Center of map Z
	var z_half_height: float = bounds.size.y * 0.5
	var lane_center: float = z_center + (z_half_height * SPAWN_LANE_CENTER_OFFSET_RATIO)
	var lane_spread: float = bounds.size.y * SPAWN_LANE_WIDTH_RATIO * 0.5  # Half-width for randf_range
	z = randf_range(lane_center - lane_spread, lane_center + lane_spread)

	return Vector2(x, z)  # Return as Vector2 representing XZ world coordinates

## Set next play time based on state and difficulty
func _set_next_play_time() -> void:
	play_timer = 0.0

	var state: BattlefieldState = _evaluate_battlefield_state()
	var base_min: float = play_interval_min
	var base_max: float = play_interval_max

	# Adjust intervals based on battlefield state
	match state:
		BattlefieldState.LOSING_BADLY:
			base_min *= TIMING_LOSING_BADLY_MULTIPLIER
			base_max *= TIMING_LOSING_BADLY_MULTIPLIER
		BattlefieldState.LOSING:
			base_min *= TIMING_LOSING_MULTIPLIER
			base_max *= TIMING_LOSING_MULTIPLIER
		BattlefieldState.WINNING:
			base_min *= TIMING_WINNING_MULTIPLIER
			base_max *= TIMING_WINNING_MULTIPLIER

	# Adjust based on difficulty (higher difficulty = faster play)
	# diff 1 = 1.2x slower, diff 3 = 1.0x normal, diff 5 = 0.8x faster
	var difficulty_factor: float = 1.0 - (difficulty - DIFFICULTY_TIMING_BASELINE) * DIFFICULTY_TIMING_SCALE
	base_min *= difficulty_factor
	base_max *= difficulty_factor

	next_play_time = randf_range(base_min, base_max)
