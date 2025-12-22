extends IProfileRepo
class_name MockProfileRepo

## Mock Profile Repository for Unit Testing
##
## In-memory implementation of IProfileRepo for fast, isolated tests.
## Does not touch disk - all data lives in memory and is reset each test.

## Internal state
var _profile_id: String = "test_profile"
var _resources: Dictionary = {"gold": 0, "essence": 0, "fragments": 0}
var _cards: Array = []
var _decks: Array = []
var _campaign_progress: Dictionary = {"completed_battles": [], "current_battle": null}
var _shared_campaign_progress: Dictionary = {"completed_battles": [], "current_battle": null}
var _profile_meta: Dictionary = {}
var _settings: Dictionary = {}
var _last_match: Dictionary = {}

## Call tracking for assertions
var _calls: Dictionary = {}


## =============================================================================
## TEST HELPERS
## =============================================================================

## Reset all state to defaults (call in before_each)
func reset() -> void:
	_profile_id = "test_profile"
	_resources = {"gold": 0, "essence": 0, "fragments": 0}
	_cards = []
	_decks = []
	_campaign_progress = {"completed_battles": [], "current_battle": null}
	_shared_campaign_progress = {"completed_battles": [], "current_battle": null}
	_profile_meta = {}
	_settings = {}
	_last_match = {}
	_calls = {}
	_shop_purchases = {}
	_shop_refresh_states = {}
	_unlocked_summoners = []
	_owned_cosmetics = []
	_owned_emotes = []


## Set initial resources for test setup
func set_resources(resources: Dictionary) -> void:
	_resources = resources.duplicate()


## Set initial cards for test setup
func set_cards(cards: Array) -> void:
	_cards = cards.duplicate(true)


## Set campaign progress for test setup
func set_campaign_progress(progress: Dictionary) -> void:
	_campaign_progress = progress.duplicate(true)


## Set shared campaign progress for test setup
func set_shared_campaign_progress(progress: Dictionary) -> void:
	_shared_campaign_progress = progress.duplicate(true)


## Get call count for a method (for spy assertions)
func get_call_count(method_name: String) -> int:
	return _calls.get(method_name, 0)


## Get call arguments for a method (returns array of all calls)
func get_call_args(method_name: String) -> Array:
	return _calls.get(method_name + "_args", [])


func _record_call(method_name: String, args: Array = []) -> void:
	_calls[method_name] = _calls.get(method_name, 0) + 1
	if not _calls.has(method_name + "_args"):
		_calls[method_name + "_args"] = []
	_calls[method_name + "_args"].append(args)


## =============================================================================
## PROFILE OPERATIONS
## =============================================================================

func load_profile(profile_id: String) -> bool:
	_record_call("load_profile", [profile_id])
	_profile_id = profile_id
	profile_loaded.emit(profile_id)
	return true


func save_profile(_immediate: bool = false) -> void:
	_record_call("save_profile", [_immediate])
	profile_saved.emit(_profile_id)


func get_current_profile_id() -> String:
	return _profile_id


func reset_profile() -> void:
	_record_call("reset_profile")
	reset()
	data_changed.emit()


func snapshot() -> Dictionary:
	return {
		"profile_id": _profile_id,
		"resources": _resources.duplicate(),
		"cards": _cards.duplicate(true),
		"decks": _decks.duplicate(true),
		"campaign_progress": _campaign_progress.duplicate(true),
		"shared_campaign_progress": _shared_campaign_progress.duplicate(true),
		"profile_meta": _profile_meta.duplicate(true),
		"settings": _settings.duplicate(true),
		"last_match": _last_match.duplicate(true),
	}


## =============================================================================
## RESOURCE OPERATIONS
## =============================================================================

func get_resources() -> Dictionary:
	return _resources.duplicate()


func update_resources(delta: Dictionary) -> void:
	_record_call("update_resources", [delta.duplicate()])
	for key: String in delta:
		var current: int = _resources.get(key, 0)
		var change: int = delta[key] if delta[key] is int else 0
		_resources[key] = current + change
	data_changed.emit()


## =============================================================================
## CARD COLLECTION OPERATIONS
## =============================================================================

func grant_cards(cards: Array) -> Array:
	_record_call("grant_cards", [cards.duplicate(true)])
	var instance_ids: Array = []
	for card: Dictionary in cards:
		var instance_id: String = "test_card_%d" % (_cards.size() + 1)
		var instance: Dictionary = {
			"instance_id": instance_id,
			"catalog_id": card.get("catalog_id", "unknown"),
			"rarity": card.get("rarity", "common"),
			"xp": 0,
			"level": 1,
		}
		_cards.append(instance)
		instance_ids.append(instance_id)
	data_changed.emit()
	return instance_ids


func remove_card(card_instance_id: String) -> bool:
	_record_call("remove_card", [card_instance_id])
	for i: int in range(_cards.size()):
		if _cards[i].get("instance_id") == card_instance_id:
			_cards.remove_at(i)
			data_changed.emit()
			return true
	return false


func list_cards() -> Array:
	return _cards.duplicate(true)


func get_card_count(catalog_id: String) -> int:
	var count: int = 0
	for card: Dictionary in _cards:
		if card.get("catalog_id") == catalog_id:
			count += 1
	return count


func get_card(card_instance_id: String) -> Dictionary:
	for card: Dictionary in _cards:
		if card.get("instance_id") == card_instance_id:
			return card.duplicate()
	return {}


func update_card(card_instance_id: String, updates: Dictionary) -> bool:
	_record_call("update_card", [card_instance_id, updates.duplicate()])
	for card: Dictionary in _cards:
		if card.get("instance_id") == card_instance_id:
			for key: String in updates:
				card[key] = updates[key]
			data_changed.emit()
			return true
	return false


## =============================================================================
## DECK OPERATIONS
## =============================================================================

func upsert_deck(deck: Dictionary) -> String:
	_record_call("upsert_deck", [deck.duplicate(true)])
	var deck_id: String = deck.get("id", "deck_%d" % (_decks.size() + 1))

	# Check if updating existing deck
	for i: int in range(_decks.size()):
		if _decks[i].get("id") == deck_id:
			_decks[i] = deck.duplicate(true)
			_decks[i]["id"] = deck_id
			data_changed.emit()
			return deck_id

	# New deck
	var new_deck: Dictionary = deck.duplicate(true)
	new_deck["id"] = deck_id
	_decks.append(new_deck)
	data_changed.emit()
	return deck_id


func delete_deck(deck_id: String) -> bool:
	_record_call("delete_deck", [deck_id])
	for i: int in range(_decks.size()):
		if _decks[i].get("id") == deck_id:
			_decks.remove_at(i)
			data_changed.emit()
			return true
	return false


func list_decks() -> Array:
	return _decks.duplicate(true)


func get_deck(deck_id: String) -> Dictionary:
	for deck: Dictionary in _decks:
		if deck.get("id") == deck_id:
			return deck.duplicate(true)
	return {}


## =============================================================================
## CAMPAIGN PROGRESS OPERATIONS
## =============================================================================

func get_campaign_progress() -> Dictionary:
	return _campaign_progress.duplicate(true)


func update_campaign_progress(progress: Dictionary) -> void:
	_record_call("update_campaign_progress", [progress.duplicate(true)])
	for key: String in progress:
		_campaign_progress[key] = progress[key]
	data_changed.emit()


## =============================================================================
## SHARED CAMPAIGN PROGRESS OPERATIONS
## =============================================================================

func get_shared_campaign_progress() -> Dictionary:
	return _shared_campaign_progress.duplicate(true)


func update_shared_campaign_progress(progress: Dictionary) -> void:
	_record_call("update_shared_campaign_progress", [progress.duplicate(true)])
	for key: String in progress:
		_shared_campaign_progress[key] = progress[key]
	data_changed.emit()


func is_onboarding_complete() -> bool:
	var completed: Array = _shared_campaign_progress.get("completed_battles", [])
	return "event_caravan_tutorial" in completed


## =============================================================================
## SUMMONER INSTANCE OPERATIONS
## =============================================================================

func get_summoner_instances() -> Array:
	return []


func get_summoner_instance(_summoner_id: String) -> Dictionary:
	return {}


func save_summoner_instance(_summoner_instance: SummonerInstance) -> bool:
	_record_call("save_summoner_instance", [])
	return true


## =============================================================================
## METADATA OPERATIONS
## =============================================================================

func get_profile_meta() -> Dictionary:
	return _profile_meta.duplicate(true)


func update_profile_meta(meta: Dictionary) -> void:
	_record_call("update_profile_meta", [meta.duplicate(true)])
	for key: String in meta:
		_profile_meta[key] = meta[key]
	data_changed.emit()


func get_settings() -> Dictionary:
	return _settings.duplicate(true)


func update_settings(settings: Dictionary) -> void:
	_record_call("update_settings", [settings.duplicate(true)])
	for key: String in settings:
		_settings[key] = settings[key]
	data_changed.emit()


func get_last_match() -> Dictionary:
	return _last_match.duplicate(true)


func update_last_match(match_info: Dictionary) -> void:
	_record_call("update_last_match", [match_info.duplicate(true)])
	_last_match = match_info.duplicate(true)
	data_changed.emit()


## =============================================================================
## SHOP OPERATIONS
## =============================================================================

var _shop_purchases: Dictionary = {}
var _shop_refresh_states: Dictionary = {}

func get_shop_purchases() -> Dictionary:
	return _shop_purchases.duplicate()


func get_shop_refresh_state(shop_id: String) -> Dictionary:
	return _shop_refresh_states.get(shop_id, {"refresh_epoch": 0})


func increment_purchase_count(purchase_key: String) -> bool:
	_record_call("increment_purchase_count", [purchase_key])
	_shop_purchases[purchase_key] = _shop_purchases.get(purchase_key, 0) + 1
	return true


## =============================================================================
## OWNERSHIP CHECKS (Summoners, Cosmetics, Emotes)
## =============================================================================

var _unlocked_summoners: Array = []
var _owned_cosmetics: Array = []
var _owned_emotes: Array = []


## Set unlocked summoners for test setup
func set_unlocked_summoners(summoner_ids: Array) -> void:
	_unlocked_summoners = summoner_ids.duplicate()


## Set owned cosmetics for test setup
func set_owned_cosmetics(cosmetic_ids: Array) -> void:
	_owned_cosmetics = cosmetic_ids.duplicate()


## Set owned emotes for test setup
func set_owned_emotes(emote_ids: Array) -> void:
	_owned_emotes = emote_ids.duplicate()


func is_summoner_unlocked(summoner_id: String) -> bool:
	return summoner_id in _unlocked_summoners


func is_cosmetic_owned(cosmetic_id: String) -> bool:
	return cosmetic_id in _owned_cosmetics


func is_emote_owned(emote_id: String) -> bool:
	return emote_id in _owned_emotes


func unlock_summoner(summoner_id: String) -> bool:
	_record_call("unlock_summoner", [summoner_id])
	if summoner_id not in _unlocked_summoners:
		_unlocked_summoners.append(summoner_id)
	data_changed.emit()
	return true


func grant_cosmetic(cosmetic_id: String) -> bool:
	_record_call("grant_cosmetic", [cosmetic_id])
	if cosmetic_id not in _owned_cosmetics:
		_owned_cosmetics.append(cosmetic_id)
	data_changed.emit()
	return true


func grant_emote(emote_id: String) -> bool:
	_record_call("grant_emote", [emote_id])
	if emote_id not in _owned_emotes:
		_owned_emotes.append(emote_id)
	data_changed.emit()
	return true


func get_owned_cosmetics() -> Array:
	return _owned_cosmetics.duplicate()


func get_owned_emotes() -> Array:
	return _owned_emotes.duplicate()
