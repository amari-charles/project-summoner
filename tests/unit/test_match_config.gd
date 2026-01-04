extends GutTest

## Unit Tests for MatchConfig
##
## Tests the match configuration data class.

const MatchConfigScript: GDScript = preload("res://scripts/multiplayer/core/match_config.gd")


## =============================================================================
## INITIALIZATION TESTS
## =============================================================================

func test_init_sets_defaults() -> void:
	var config: RefCounted = MatchConfigScript.new()

	assert_eq(config.match_id, "")
	assert_eq(config.battle_seed, 0)
	assert_eq(config.host_peer_id, 1)
	assert_eq(config.player_peer_ids.size(), 0)
	assert_eq(config.player_summoner_ids.size(), 0)
	assert_eq(config.player_deck_hashes.size(), 0)
	assert_eq(config.match_type, "casual")


func test_init_sets_created_at() -> void:
	var before: float = Time.get_unix_time_from_system()
	var config: RefCounted = MatchConfigScript.new()
	var after: float = Time.get_unix_time_from_system()

	assert_gte(config.created_at, before)
	assert_lte(config.created_at, after)


## =============================================================================
## STATIC FACTORY TESTS
## =============================================================================

func test_generate_match_id_returns_16_char_string() -> void:
	var match_id: String = MatchConfigScript.generate_match_id()
	assert_eq(match_id.length(), 16)


func test_generate_match_id_is_unique() -> void:
	var id1: String = MatchConfigScript.generate_match_id()
	var id2: String = MatchConfigScript.generate_match_id()
	var id3: String = MatchConfigScript.generate_match_id()

	# Very unlikely to be equal
	assert_ne(id1, id2)
	assert_ne(id2, id3)
	assert_ne(id1, id3)


func test_generate_battle_seed_returns_int() -> void:
	var seed_val: int = MatchConfigScript.generate_battle_seed()
	assert_typeof(seed_val, TYPE_INT)


func test_create_new_sets_host_id() -> void:
	var config: RefCounted = MatchConfigScript.create_new(5)
	assert_eq(config.host_peer_id, 5)


func test_create_new_generates_match_id() -> void:
	var config: RefCounted = MatchConfigScript.create_new(1)
	assert_eq(config.match_id.length(), 16)


func test_create_new_generates_battle_seed() -> void:
	var config: RefCounted = MatchConfigScript.create_new(1)
	# Seed should be set (may be 0 but that's valid)
	assert_typeof(config.battle_seed, TYPE_INT)


## =============================================================================
## PLAYER MANAGEMENT TESTS
## =============================================================================

func test_add_player_adds_to_arrays() -> void:
	var config: RefCounted = MatchConfigScript.new()

	config.add_player(1, "fire_summoner", 12345)

	assert_eq(config.player_peer_ids.size(), 1)
	assert_eq(config.player_peer_ids[0], 1)
	assert_eq(config.player_summoner_ids[0], "fire_summoner")
	assert_eq(config.player_deck_hashes[0], 12345)


func test_add_multiple_players() -> void:
	var config: RefCounted = MatchConfigScript.new()

	config.add_player(1, "fire_summoner", 111)
	config.add_player(2, "water_summoner", 222)

	assert_eq(config.player_peer_ids.size(), 2)
	assert_eq(config.player_peer_ids[0], 1)
	assert_eq(config.player_peer_ids[1], 2)
	assert_eq(config.player_summoner_ids[0], "fire_summoner")
	assert_eq(config.player_summoner_ids[1], "water_summoner")


func test_get_player_index_returns_correct_index() -> void:
	var config: RefCounted = MatchConfigScript.new()
	config.add_player(10, "a", 0)
	config.add_player(20, "b", 0)

	assert_eq(config.get_player_index(10), 0)
	assert_eq(config.get_player_index(20), 1)


func test_get_player_index_returns_minus_one_for_missing() -> void:
	var config: RefCounted = MatchConfigScript.new()
	config.add_player(10, "a", 0)

	assert_eq(config.get_player_index(99), -1)


func test_has_player_returns_true_for_existing() -> void:
	var config: RefCounted = MatchConfigScript.new()
	config.add_player(5, "test", 0)

	assert_true(config.has_player(5))


func test_has_player_returns_false_for_missing() -> void:
	var config: RefCounted = MatchConfigScript.new()
	config.add_player(5, "test", 0)

	assert_false(config.has_player(99))


## =============================================================================
## SERIALIZATION TESTS
## =============================================================================

func test_serialize_returns_dictionary() -> void:
	var config: RefCounted = MatchConfigScript.new()
	config.match_id = "test_match_123"
	config.battle_seed = 42
	config.host_peer_id = 1
	config.match_type = "ranked"
	config.add_player(1, "fire", 100)
	config.add_player(2, "water", 200)

	var data: Dictionary = config.serialize()

	assert_eq(data.get("match_id"), "test_match_123")
	assert_eq(data.get("battle_seed"), 42)
	assert_eq(data.get("host_peer_id"), 1)
	assert_eq(data.get("match_type"), "ranked")
	assert_eq(data.get("player_peer_ids").size(), 2)


func test_deserialize_creates_config() -> void:
	var data: Dictionary = {
		"match_id": "abc123",
		"battle_seed": 999,
		"host_peer_id": 5,
		"player_peer_ids": [5, 10],
		"player_summoner_ids": ["earth", "wind"],
		"player_deck_hashes": [111, 222],
		"created_at": 1000.0,
		"match_type": "tournament",
	}

	var config: RefCounted = MatchConfigScript.deserialize(data)

	assert_eq(config.match_id, "abc123")
	assert_eq(config.battle_seed, 999)
	assert_eq(config.host_peer_id, 5)
	assert_eq(config.player_peer_ids.size(), 2)
	assert_eq(config.player_peer_ids[0], 5)
	assert_eq(config.player_peer_ids[1], 10)
	assert_eq(config.player_summoner_ids[0], "earth")
	assert_eq(config.player_summoner_ids[1], "wind")
	assert_eq(config.match_type, "tournament")


func test_serialize_deserialize_roundtrip() -> void:
	var original: RefCounted = MatchConfigScript.create_new(1)
	original.add_player(1, "fire", 100)
	original.add_player(2, "water", 200)
	original.match_type = "ranked"

	var data: Dictionary = original.serialize()
	var restored: RefCounted = MatchConfigScript.deserialize(data)

	assert_eq(restored.match_id, original.match_id)
	assert_eq(restored.battle_seed, original.battle_seed)
	assert_eq(restored.host_peer_id, original.host_peer_id)
	assert_eq(restored.player_peer_ids.size(), original.player_peer_ids.size())
	assert_eq(restored.match_type, original.match_type)
