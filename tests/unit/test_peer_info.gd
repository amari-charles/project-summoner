extends GutTest

## Unit Tests for PeerInfo
##
## Tests the peer information data class.

const PeerInfoScript: GDScript = preload("res://scripts/multiplayer/core/peer_info.gd")


## =============================================================================
## INITIALIZATION TESTS
## =============================================================================

func test_init_with_defaults() -> void:
	var info: RefCounted = PeerInfoScript.new()

	assert_eq(info.peer_id, 0)
	assert_eq(info.display_name, "")
	assert_false(info.is_host)
	assert_eq(info.summoner_id, "")
	assert_eq(info.latency_ms, 0)


func test_init_with_parameters() -> void:
	var info: RefCounted = PeerInfoScript.new(42, "TestPlayer")

	assert_eq(info.peer_id, 42)
	assert_eq(info.display_name, "TestPlayer")


func test_init_sets_last_seen() -> void:
	var before: float = Time.get_unix_time_from_system()
	var info: RefCounted = PeerInfoScript.new(1, "Player")
	var after: float = Time.get_unix_time_from_system()

	assert_gte(info.last_seen, before)
	assert_lte(info.last_seen, after)


## =============================================================================
## CONNECTION STATE TESTS
## =============================================================================

func test_default_connection_state_is_connecting() -> void:
	var info: RefCounted = PeerInfoScript.new()
	assert_eq(info.connection_state, PeerInfoScript.ConnectionState.CONNECTING)


func test_is_connected_peer_returns_false_when_connecting() -> void:
	var info: RefCounted = PeerInfoScript.new()
	info.connection_state = PeerInfoScript.ConnectionState.CONNECTING
	assert_false(info.is_connected_peer())


func test_is_connected_peer_returns_true_when_connected() -> void:
	var info: RefCounted = PeerInfoScript.new()
	info.connection_state = PeerInfoScript.ConnectionState.CONNECTED
	assert_true(info.is_connected_peer())


func test_is_connected_peer_returns_false_when_disconnected() -> void:
	var info: RefCounted = PeerInfoScript.new()
	info.connection_state = PeerInfoScript.ConnectionState.DISCONNECTED
	assert_false(info.is_connected_peer())


## =============================================================================
## UPDATE LAST SEEN TESTS
## =============================================================================

func test_update_last_seen_updates_timestamp() -> void:
	var info: RefCounted = PeerInfoScript.new()
	var original_time: float = info.last_seen

	# Wait a tiny bit to ensure time difference
	OS.delay_msec(10)

	info.update_last_seen()

	assert_gte(info.last_seen, original_time)


## =============================================================================
## SERIALIZATION TESTS
## =============================================================================

func test_serialize_returns_dictionary() -> void:
	var info: RefCounted = PeerInfoScript.new(5, "Alice")
	info.is_host = true
	info.summoner_id = "fire_summoner"
	info.latency_ms = 50

	var data: Dictionary = info.serialize()

	assert_eq(data.get("peer_id"), 5)
	assert_eq(data.get("display_name"), "Alice")
	assert_eq(data.get("is_host"), true)
	assert_eq(data.get("summoner_id"), "fire_summoner")
	assert_eq(data.get("latency_ms"), 50)


func test_deserialize_creates_peer_info() -> void:
	var data: Dictionary = {
		"peer_id": 10,
		"display_name": "Bob",
		"is_host": false,
		"summoner_id": "water_summoner",
		"latency_ms": 100,
	}

	var info: RefCounted = PeerInfoScript.deserialize(data)

	assert_eq(info.peer_id, 10)
	assert_eq(info.display_name, "Bob")
	assert_false(info.is_host)
	assert_eq(info.summoner_id, "water_summoner")
	assert_eq(info.latency_ms, 100)


func test_deserialize_sets_connected_state() -> void:
	var data: Dictionary = {"peer_id": 1, "display_name": "Test"}
	var info: RefCounted = PeerInfoScript.deserialize(data)

	assert_eq(info.connection_state, PeerInfoScript.ConnectionState.CONNECTED)


func test_serialize_deserialize_roundtrip() -> void:
	var original: RefCounted = PeerInfoScript.new(7, "Charlie")
	original.is_host = true
	original.summoner_id = "earth_summoner"
	original.latency_ms = 25

	var data: Dictionary = original.serialize()
	var restored: RefCounted = PeerInfoScript.deserialize(data)

	assert_eq(restored.peer_id, original.peer_id)
	assert_eq(restored.display_name, original.display_name)
	assert_eq(restored.is_host, original.is_host)
	assert_eq(restored.summoner_id, original.summoner_id)
	assert_eq(restored.latency_ms, original.latency_ms)
