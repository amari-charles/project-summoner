extends GutTest

## Unit Tests for RoomCodeService
##
## Tests room code generation and validation.

const RoomCodeServiceScript: GDScript = preload("res://scripts/multiplayer/connection/room_code_service.gd")


## =============================================================================
## CODE GENERATION TESTS
## =============================================================================

func test_generate_code_returns_6_char_string() -> void:
	var code: String = RoomCodeServiceScript.generate_code()
	assert_eq(code.length(), 6)


func test_generate_code_uses_valid_characters() -> void:
	var valid_chars: String = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"

	for _i in 10:  # Generate several codes to test
		var code: String = RoomCodeServiceScript.generate_code()
		for c: String in code:
			assert_true(c in valid_chars, "Character '%s' should be valid" % c)


func test_generate_code_excludes_ambiguous_characters() -> void:
	var ambiguous: Array = ["0", "O", "1", "I", "L"]

	for _i in 100:  # Generate many codes to increase chance of catching issues
		var code: String = RoomCodeServiceScript.generate_code()
		for c: String in ambiguous:
			assert_false(c in code, "Code should not contain ambiguous char '%s'" % c)


func test_generate_code_is_unique() -> void:
	var codes: Array = []
	for _i in 10:
		codes.append(RoomCodeServiceScript.generate_code())

	# Check for uniqueness (very unlikely to have duplicates)
	for i in codes.size():
		for j in range(i + 1, codes.size()):
			assert_ne(codes[i], codes[j], "Codes should be unique")


## =============================================================================
## CODE VALIDATION TESTS
## =============================================================================

func test_is_valid_code_accepts_valid_code() -> void:
	# Note: CODE_CHARS excludes 0, 1, I, L, O (ambiguous characters)
	assert_true(RoomCodeServiceScript.is_valid_code("ABC234"))
	assert_true(RoomCodeServiceScript.is_valid_code("XYZW98"))
	assert_true(RoomCodeServiceScript.is_valid_code("ABCDEF"))
	assert_true(RoomCodeServiceScript.is_valid_code("234567"))


func test_is_valid_code_rejects_too_short() -> void:
	assert_false(RoomCodeServiceScript.is_valid_code("ABC12"))
	assert_false(RoomCodeServiceScript.is_valid_code("A"))
	assert_false(RoomCodeServiceScript.is_valid_code(""))


func test_is_valid_code_rejects_too_long() -> void:
	assert_false(RoomCodeServiceScript.is_valid_code("ABC1234"))
	assert_false(RoomCodeServiceScript.is_valid_code("ABCDEFGHIJ"))


func test_is_valid_code_rejects_ambiguous_characters() -> void:
	# These contain 0, O, 1, I, L which are excluded
	assert_false(RoomCodeServiceScript.is_valid_code("ABC0EF"))
	assert_false(RoomCodeServiceScript.is_valid_code("ABCOEF"))
	assert_false(RoomCodeServiceScript.is_valid_code("ABC1EF"))
	assert_false(RoomCodeServiceScript.is_valid_code("ABCIEF"))
	assert_false(RoomCodeServiceScript.is_valid_code("ABCLEF"))


func test_is_valid_code_accepts_lowercase() -> void:
	# The validation converts to uppercase internally
	# Note: CODE_CHARS excludes 0, 1, I, L, O (ambiguous characters)
	assert_true(RoomCodeServiceScript.is_valid_code("abc234"))
	assert_true(RoomCodeServiceScript.is_valid_code("AbC234"))


func test_is_valid_code_rejects_special_characters() -> void:
	assert_false(RoomCodeServiceScript.is_valid_code("ABC-23"))
	assert_false(RoomCodeServiceScript.is_valid_code("ABC 23"))
	assert_false(RoomCodeServiceScript.is_valid_code("ABC@23"))


## =============================================================================
## CODE NORMALIZATION TESTS
## =============================================================================

func test_normalize_code_uppercases() -> void:
	assert_eq(RoomCodeServiceScript.normalize_code("abc123"), "ABC123")


func test_normalize_code_strips_whitespace() -> void:
	assert_eq(RoomCodeServiceScript.normalize_code("  ABC123  "), "ABC123")
	assert_eq(RoomCodeServiceScript.normalize_code("\tABC123\n"), "ABC123")


func test_normalize_code_handles_mixed_case() -> void:
	assert_eq(RoomCodeServiceScript.normalize_code("AbC123"), "ABC123")


## =============================================================================
## CONNECTION STRING TESTS
## =============================================================================

func test_parse_connection_string_ip_only() -> void:
	var result: Dictionary = RoomCodeServiceScript.parse_connection_string("192.168.1.1")

	assert_true(result.valid)
	assert_eq(result.ip, "192.168.1.1")
	assert_eq(result.port, 7777)  # Default port


func test_parse_connection_string_ip_and_port() -> void:
	var result: Dictionary = RoomCodeServiceScript.parse_connection_string("192.168.1.1:8080")

	assert_true(result.valid)
	assert_eq(result.ip, "192.168.1.1")
	assert_eq(result.port, 8080)


func test_parse_connection_string_localhost() -> void:
	var result: Dictionary = RoomCodeServiceScript.parse_connection_string("localhost")

	assert_true(result.valid)
	assert_eq(result.ip, "localhost")
	assert_eq(result.port, 7777)


func test_parse_connection_string_localhost_with_port() -> void:
	var result: Dictionary = RoomCodeServiceScript.parse_connection_string("localhost:9999")

	assert_true(result.valid)
	assert_eq(result.ip, "localhost")
	assert_eq(result.port, 9999)


func test_parse_connection_string_strips_whitespace() -> void:
	var result: Dictionary = RoomCodeServiceScript.parse_connection_string("  192.168.1.1:8080  ")

	assert_true(result.valid)
	assert_eq(result.ip, "192.168.1.1")


func test_parse_connection_string_empty_invalid() -> void:
	var result: Dictionary = RoomCodeServiceScript.parse_connection_string("")
	assert_false(result.valid)


func test_parse_connection_string_invalid_port() -> void:
	var result: Dictionary = RoomCodeServiceScript.parse_connection_string("192.168.1.1:notaport")
	assert_false(result.valid)


func test_make_connection_string_default_port() -> void:
	var result: String = RoomCodeServiceScript.make_connection_string("192.168.1.1")
	assert_eq(result, "192.168.1.1")


func test_make_connection_string_custom_port() -> void:
	var result: String = RoomCodeServiceScript.make_connection_string("192.168.1.1", 8080)
	assert_eq(result, "192.168.1.1:8080")


func test_make_connection_string_explicit_default_port() -> void:
	var result: String = RoomCodeServiceScript.make_connection_string("192.168.1.1", 7777)
	assert_eq(result, "192.168.1.1")  # Omits default port
