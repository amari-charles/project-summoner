extends Node
class_name MockEconomyService

## Mock Economy Service for Unit Testing
##
## Tracks calls to add_gold, spend, etc. for verification in tests.

## Signals (matching real EconomyService)
signal resources_changed(gold: int, essence: int, fragments: int)
signal transaction_completed(delta: Dictionary)
signal transaction_failed(reason: String)

## Internal state
var _gold: int = 0
var _essence: int = 0
var _fragments: int = 0

## Call tracking
var _calls: Dictionary = {}


## =============================================================================
## TEST HELPERS
## =============================================================================

func reset() -> void:
	_gold = 0
	_essence = 0
	_fragments = 0
	_calls = {}


func set_gold(amount: int) -> void:
	_gold = amount


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
## ECONOMY SERVICE API
## =============================================================================

func get_gold() -> int:
	return _gold


func get_essence() -> int:
	return _essence


func get_fragments() -> int:
	return _fragments


func get_resources() -> Dictionary:
	return {"gold": _gold, "essence": _essence, "fragments": _fragments}


func can_afford(cost: Dictionary) -> bool:
	for key: String in cost:
		var required: int = cost.get(key, 0)
		var current: int = 0
		if key == "gold":
			current = _gold
		elif key == "essence":
			current = _essence
		elif key == "fragments":
			current = _fragments
		if current < required:
			return false
	return true


func add_gold(amount: int) -> void:
	_record_call("add_gold", [amount])
	if amount > 0:
		_gold += amount
		transaction_completed.emit({"gold": amount})
		resources_changed.emit(_gold, _essence, _fragments)


func add_essence(amount: int) -> void:
	_record_call("add_essence", [amount])
	if amount > 0:
		_essence += amount


func add_fragments(amount: int) -> void:
	_record_call("add_fragments", [amount])
	if amount > 0:
		_fragments += amount


func spend(cost: Dictionary) -> bool:
	_record_call("spend", [cost])
	if not can_afford(cost):
		transaction_failed.emit("Cannot afford")
		return false
	for key: String in cost:
		var amount: int = cost.get(key, 0)
		if key == "gold":
			_gold -= amount
		elif key == "essence":
			_essence -= amount
		elif key == "fragments":
			_fragments -= amount
	return true


func grant_rewards(rewards: Dictionary) -> void:
	_record_call("grant_rewards", [rewards])
	if rewards.has("gold"):
		_gold += rewards["gold"]
	if rewards.has("essence"):
		_essence += rewards["essence"]
	if rewards.has("fragments"):
		_fragments += rewards["fragments"]
