extends Node
class_name MockPlayerCardService

## Mock PlayerCardService for Unit Testing
##
## Tracks calls to grant_xp_to_cards for verification in tests.

## Call tracking
var _calls: Dictionary = {}


## =============================================================================
## TEST HELPERS
## =============================================================================

func reset() -> void:
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
## PLAYER CARD SERVICE API (subset needed for testing)
## =============================================================================

## Grant XP to a list of cards (GDScript method name)
## Matches signature: grant_xp_to_cards(card_instance_ids: Array, xp_amount: int)
func grant_xp_to_cards(card_instance_ids: Variant, xp_amount: int) -> void:
	# Convert to Array if needed (C# interop might send different types)
	var ids: Array = []
	if card_instance_ids is Array:
		ids = card_instance_ids
	_record_call("grant_xp_to_cards", [ids, xp_amount])


## Grant XP to a list of cards (C# method name - GDScript-friendly wrapper)
## Matches signature: GrantXpToCardsArray(card_instance_ids: Array, xp_amount: int) -> Dictionary
func GrantXpToCardsArray(card_instance_ids: Variant, xp_amount: int) -> Dictionary:
	# Convert to Array if needed
	var ids: Array = []
	if card_instance_ids is Array:
		ids = card_instance_ids
	_record_call("grant_xp_to_cards", [ids, xp_amount])
	# Return empty dictionary (mock doesn't need real results)
	return {}
