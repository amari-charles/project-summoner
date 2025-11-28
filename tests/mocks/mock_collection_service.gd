extends Node
class_name MockCollectionService

## Mock Collection Service for Unit Testing
##
## Tracks calls to grant_card, etc. for verification in tests.

## Internal state
var _cards: Array = []
var _next_instance_id: int = 1

## Call tracking
var _calls: Dictionary = {}


## =============================================================================
## TEST HELPERS
## =============================================================================

func reset() -> void:
	_cards = []
	_next_instance_id = 1
	_calls = {}


func get_call_count(method_name: String) -> int:
	return _calls.get(method_name, 0)


func get_call_args(method_name: String) -> Array:
	return _calls.get(method_name + "_args", [])


func _record_call(method_name: String, args: Array = []) -> void:
	_calls[method_name] = _calls.get(method_name, 0) + 1
	if not _calls.has(method_name + "_args"):
		_calls[method_name + "_args"] = []
	_calls[method_name + "_args"].append(args)


## =============================================================================
## COLLECTION SERVICE API
## =============================================================================

func grant_card(catalog_id: String, rarity: String = "common") -> String:
	_record_call("grant_card", [catalog_id, rarity])

	var instance_id: String = "mock_card_%d" % _next_instance_id
	_next_instance_id += 1

	_cards.append({
		"instance_id": instance_id,
		"catalog_id": catalog_id,
		"rarity": rarity
	})

	return instance_id


func get_cards() -> Array:
	return _cards.duplicate(true)


func remove_card(instance_id: String) -> bool:
	_record_call("remove_card", [instance_id])
	for i: int in range(_cards.size()):
		if _cards[i].get("instance_id") == instance_id:
			_cards.remove_at(i)
			return true
	return false
