class_name RoomCodeService extends RefCounted

## Service for generating and validating room codes
##
## Room codes are short, human-readable strings used to join P2P matches.
## Uses characters that are visually unambiguous (no 0/O, 1/I/L confusion).

## Length of generated room codes
const CODE_LENGTH: int = 6

## Characters used in room codes (excluding ambiguous characters: 0, O, 1, I, L)
const CODE_CHARS: String = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"

## Default port for P2P hosting
const DEFAULT_PORT: int = 7777


## Generate a random room code
static func generate_code() -> String:
	var code: String = ""
	for _i: int in CODE_LENGTH:
		code += CODE_CHARS[randi() % CODE_CHARS.length()]
	return code


## Validate a room code format
static func is_valid_code(code: String) -> bool:
	if code.length() != CODE_LENGTH:
		return false

	var upper_code: String = code.to_upper()
	for c: String in upper_code:
		if c not in CODE_CHARS:
			return false

	return true


## Normalize a room code (uppercase, strip whitespace)
static func normalize_code(code: String) -> String:
	return code.strip_edges().to_upper()


## Parse a connection string in format "IP:PORT" or just "IP"
## Returns dictionary with "ip" and "port" keys
static func parse_connection_string(connection_string: String) -> Dictionary:
	var result: Dictionary = {
		"ip": "",
		"port": DEFAULT_PORT,
		"valid": false,
	}

	var parts: PackedStringArray = connection_string.strip_edges().split(":")

	if parts.size() == 0 or parts[0].is_empty():
		return result

	result.ip = parts[0]

	if parts.size() >= 2:
		if parts[1].is_valid_int():
			result.port = int(parts[1])
		else:
			return result  # Invalid port

	result.valid = true
	return result


## Generate a connection string from IP and port
static func make_connection_string(ip: String, port: int = DEFAULT_PORT) -> String:
	if port == DEFAULT_PORT:
		return ip
	return "%s:%d" % [ip, port]
