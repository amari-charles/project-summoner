extends Node
class_name MockSummonerProgression

## Mock SummonerProgression for Unit Testing
##
## Tracks calls to grant_active_summoner_xp for verification in tests.

## Signals (matching real SummonerProgression)
signal xp_changed(summoner_id: String, new_xp: int)
signal level_changed(summoner_id: String, new_level: int)
signal ready_to_level_up(summoner_id: String)

## Internal state
var _summoner_xp: int = 0

## Call tracking
var _calls: Dictionary = {}


## =============================================================================
## TEST HELPERS
## =============================================================================

func reset() -> void:
	_summoner_xp = 0
	_calls = {}


func set_summoner_xp(xp: int) -> void:
	_summoner_xp = xp


func get_summoner_xp() -> int:
	return _summoner_xp


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
## SUMMONER PROGRESSION API (subset needed for testing)
## =============================================================================

## Grant XP to the active summoner
## Returns the new total XP
func grant_active_summoner_xp(xp_amount: int) -> int:
	_record_call("grant_active_summoner_xp", [xp_amount])
	_summoner_xp += xp_amount
	xp_changed.emit("test_summoner", _summoner_xp)
	return _summoner_xp
