class_name MockDeckService
extends Node

## Mock Deck Service for Unit Testing
##
## Tracks calls to deck operations for verification in tests.

## Internal state
var _decks: Array[Dictionary] = []
var _calls: Dictionary = {}  # method_name -> Array of call args


func _init() -> void:
	_calls = {}
	_decks = []


## Record a method call
func _record_call(method: String, args: Array) -> void:
	if not _calls.has(method):
		_calls[method] = []
	_calls[method].append(args)


## Get call count for a method
func get_call_count(method: String) -> int:
	if not _calls.has(method):
		return 0
	return _calls[method].size()


## Get call args for a method
func get_call_args(method: String) -> Array:
	if not _calls.has(method):
		return []
	return _calls[method]


## Reset all call tracking
func reset_calls() -> void:
	_calls = {}


## =============================================================================
## TEST SETUP HELPERS
## =============================================================================

## Add a deck to the mock (for testing list_decks)
func add_mock_deck(deck: Dictionary) -> void:
	_decks.append(deck)


## Clear all mock decks
func clear_mock_decks() -> void:
	_decks = []


## =============================================================================
## DECK SERVICE INTERFACE
## =============================================================================

func list_decks() -> Array[Dictionary]:
	_record_call("list_decks", [])
	return _decks


func add_card_to_deck(deck_id: String, card_instance_id: String) -> bool:
	_record_call("add_card_to_deck", [deck_id, card_instance_id])
	# Find the deck and add the card
	for deck: Dictionary in _decks:
		if deck.get("id", "") == deck_id:
			var card_ids: Array = deck.get("card_instance_ids", [])
			card_ids.append(card_instance_id)
			deck["card_instance_ids"] = card_ids
			return true
	return false
