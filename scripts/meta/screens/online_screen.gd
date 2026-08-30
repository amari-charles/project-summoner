extends BackNavigableScreen
class_name OnlineScreen

## Online matchmaking screen for competitive play.
##
## Connects to Nakama services for:
## - Authentication
## - Matchmaking queue
## - Tier and league-point display

enum ScreenState { LOADING, READY, IN_QUEUE, MATCH_FOUND }

const CARD_VISUAL_SCENE: PackedScene = preload("res://scenes/shared/card_visual.tscn")
const DECK_CARD_SIZE: Vector2 = CardVisualHelper.CARD_SIZE_STANDARD

## Timing constants
## Delay after match found to show UI feedback before connecting
const MATCH_FOUND_FEEDBACK_DELAY: float = 1.0
## Delay before transitioning to battle scene to ensure state is ready
const BATTLE_TRANSITION_DELAY: float = 0.5
## Timeout for deck exchange with opponent
const DECK_EXCHANGE_TIMEOUT: float = 10.0

## Network protocol constants
## Op-code for deck exchange messages over Nakama match data
const DECK_EXCHANGE_OP_CODE: int = 100

## Starting ELO for new players (must match STARTING_ELO in C#)
const STARTING_ELO: int = 800

## UI References
@onready var back_button: Button = %BackButton
@onready var rank_content: VBoxContainer = %RankContent
@onready var rank_details: HBoxContainer = %RankDetails
@onready var tier_label: Label = %TierLabel
@onready var league_points_label: Label = %LeaguePointsLabel
@onready var status_label: Label = %StatusLabel
@onready var queue_button: Button = %QueueButton
@onready var character_art: Button = %CharacterArt
@onready var deck_name_label: Label = %DeckNameLabel
@onready var deck_rail_button: Button = %DeckRailButton
@onready var deck_rail: HBoxContainer = %DeckRail
@onready var mode_title_button: Button = %ModeTitleButton
@onready var mode_selector: PopupPanel = %ModeSelector
@onready var mode_selector_title: Label = %Title
@onready var current_mode_label: Label = %CurrentMode
@onready var placeholder_mode_a: Label = %PlaceholderModeA
@onready var placeholder_mode_b: Label = %PlaceholderModeB
@onready var ui_animation_player: AnimationPlayer = $AnimationPlayer
@onready var collection_overlay: CollectionScreen = %CollectionOverlay

## State
var _state: ScreenState = ScreenState.LOADING
var _queue_start_time: float = 0.0
var _nakama_client: Node = null
var _matchmaking_service: Node = null
var _ranking_service: Node = null
var _leaderboard_service: Node = null
var _is_authenticated: bool = false
var _auto_queue_once: bool = false

## Deck exchange state
var _opponent_deck_received: bool = false
var _pending_opponent_deck: Array = []
var _pending_opponent_summoner_data: Dictionary = {}
var _pending_match_info: Dictionary = {}


func _ready() -> void:
	QuestApi.record_ui_surface_opened("online")
	QuestGuidance.clear()
	_ensure_ui_animations()
	_setup_localization()
	_setup_signals()
	_connect_services()
	_update_ui()
	QuestGuidance.show_for(back_button, "general_magic")


func _process(_delta: float) -> void:
	if _state == ScreenState.IN_QUEUE:
		_update_queue_time()


func _setup_localization() -> void:
	queue_button.text = Loc.t("ui.ranked.find_match")
	mode_title_button.text = "%s  ▾" % Loc.t("ui.ranked.mode_ranked_1v1")
	mode_selector_title.text = Loc.t("ui.ranked.choose_mode")
	current_mode_label.text = "✓ %s" % Loc.t("ui.ranked.mode_ranked_1v1")
	placeholder_mode_a.text = Loc.t("ui.ranked.mode_placeholder_a")
	placeholder_mode_b.text = Loc.t("ui.ranked.mode_placeholder_b")


func _setup_signals() -> void:
	back_button.pressed.connect(_on_close_pressed)
	queue_button.pressed.connect(_on_queue_pressed)
	mode_title_button.pressed.connect(_on_mode_title_pressed)
	character_art.pressed.connect(_on_change_summoner_pressed)
	deck_rail_button.pressed.connect(_on_change_deck_pressed)
	collection_overlay.closed.connect(_on_collection_overlay_closed)


func _connect_services() -> void:
	# Get autoload services (registered in project.godot)
	_nakama_client = get_tree().root.get_node_or_null("NakamaGameClient")
	_matchmaking_service = get_tree().root.get_node_or_null("MatchmakingService")
	_ranking_service = get_tree().root.get_node_or_null("RankingService")
	_leaderboard_service = get_tree().root.get_node_or_null("LeaderboardService")

	# Connect signals
	if _nakama_client:
		if _nakama_client.has_signal("Authenticated"):
			_nakama_client.Authenticated.connect(_on_authenticated)
		if _nakama_client.has_signal("AuthenticationFailed"):
			_nakama_client.AuthenticationFailed.connect(_on_authentication_failed)
		if _nakama_client.has_signal("SocketConnected"):
			_nakama_client.SocketConnected.connect(_on_socket_connected)
		if _nakama_client.has_signal("SocketDisconnected"):
			_nakama_client.SocketDisconnected.connect(_on_socket_disconnected)

	if _matchmaking_service:
		if _matchmaking_service.has_signal("MatchFound"):
			_matchmaking_service.MatchFound.connect(_on_match_found)
		if _matchmaking_service.has_signal("MatchmakingCancelled"):
			_matchmaking_service.MatchmakingCancelled.connect(_on_matchmaking_cancelled)
		if _matchmaking_service.has_signal("QueueStatusChanged"):
			_matchmaking_service.QueueStatusChanged.connect(_on_queue_status_changed)
		if _matchmaking_service.has_signal("MatchmakingError"):
			_matchmaking_service.MatchmakingError.connect(_on_matchmaking_error)

	if _leaderboard_service:
		if _leaderboard_service.has_signal("LeaderboardRefreshed"):
			_leaderboard_service.LeaderboardRefreshed.connect(_on_leaderboard_refreshed)

	# Connect match data handler once (stays connected for lifetime of this screen)
	if _nakama_client and _nakama_client.has_signal("MatchDataReceived"):
		_nakama_client.MatchDataReceived.connect(_on_match_data_received)

	# Start authentication if not already authenticated
	if _nakama_client:
		if not _nakama_client.get("IsAuthenticated"):
			_set_state(ScreenState.LOADING)
			status_label.text = Loc.t("ui.ranked.authenticating")
			print("[RANKED][AUTH] Authenticating with Nakama")
			_nakama_client.Authenticate()
		else:
			_is_authenticated = true
			_set_state(ScreenState.READY)
			_refresh_data()
			_try_auto_queue_once()
	else:
		# Services not available - show local data
		_is_authenticated = false
		_set_state(ScreenState.READY)
		_refresh_local_data()


func _set_state(new_state: ScreenState) -> void:
	_state = new_state
	_update_ui()
	_update_ui_animation(new_state)


func _update_ui() -> void:
	_refresh_loadout_display()
	rank_content.visible = _state == ScreenState.READY
	match _state:
		ScreenState.LOADING:
			queue_button.disabled = true
			queue_button.text = Loc.t("ui.ranked.find_match")
		ScreenState.READY:
			queue_button.disabled = not _is_authenticated or not _has_valid_ranked_deck()
			queue_button.text = Loc.t("ui.ranked.find_match")
			status_label.text = _get_loadout_issue()
		ScreenState.IN_QUEUE:
			queue_button.disabled = false
			queue_button.text = Loc.t("ui.ranked.cancel_queue")
		ScreenState.MATCH_FOUND:
			queue_button.disabled = true
			queue_button.text = Loc.t("ui.ranked.find_match")
			status_label.text = Loc.t("ui.ranked.match_found")


func _update_queue_time() -> void:
	if _state != ScreenState.IN_QUEUE:
		return
	var elapsed: float = Time.get_ticks_msec() / 1000.0 - _queue_start_time
	var minutes: int = int(elapsed) / 60
	var seconds: int = int(elapsed) % 60
	var time_str: String = "%d:%02d" % [minutes, seconds]
	status_label.text = Loc.t("ui.ranked.in_queue") + "\n" + Loc.t("ui.ranked.queue_time").format({"time": time_str})


func _refresh_data() -> void:
	_refresh_rating_display()
	_refresh_leaderboard()


func _refresh_local_data() -> void:
	var rating: int = STARTING_ELO
	if _ranking_service and _ranking_service.has_method("GetRating"):
		rating = _ranking_service.GetRating()

	_update_rating_display(rating)


func _refresh_rating_display() -> void:
	var rating: int = STARTING_ELO

	if _ranking_service and _ranking_service.has_method("GetRating"):
		rating = _ranking_service.GetRating()

	_update_rating_display(rating)


func _update_rating_display(rating: int) -> void:
	var is_fateforged: bool = _is_player_fateforged()
	var tier_name: String = _get_tier_name_from_service(rating)

	if is_fateforged:
		tier_label.text = Loc.t("ui.ranked.tier_fateforged")
		tier_label.add_theme_color_override("font_color", _get_tier_color("Fateforged"))
	else:
		tier_label.text = Loc.t("ui.ranked.tier_" + tier_name.to_lower())
		tier_label.add_theme_color_override("font_color", _get_tier_color(tier_name))

	var league_points: int = _get_league_points_from_service(rating)
	league_points_label.text = Loc.t("ui.ranked.league_points", {"amount": league_points})


func _is_player_fateforged() -> bool:
	if not _leaderboard_service:
		return false
	if not _leaderboard_service.has_method("IsTopTwenty"):
		return false
	var local_user_id: String = _get_local_user_id()
	if local_user_id.is_empty():
		return false
	return _leaderboard_service.IsTopTwenty(local_user_id)


func _get_local_user_id() -> String:
	if _nakama_client:
		var user_id_val: Variant = _nakama_client.get("UserId")
		if user_id_val != null:
			return str(user_id_val)
	return ""


func _get_tier_name_from_service(rating: int) -> String:
	if _ranking_service and _ranking_service.has_method("GetTierNameForRating"):
		return _ranking_service.GetTierNameForRating(rating)
	# Fallback if service not available (matches EloCalculator.cs thresholds)
	if rating >= 1600:
		return "Sage"
	elif rating >= 1400:
		return "Archmage"
	elif rating >= 1200:
		return "Mage"
	elif rating >= 1000:
		return "Adept"
	elif rating >= 800:
		return "Apprentice"
	else:
		return "Unbound"


func _get_league_points_from_service(rating: int) -> int:
	if _ranking_service and _ranking_service.has_method("GetLeaguePointsForRating"):
		return _ranking_service.GetLeaguePointsForRating(rating)
	return 0


func _get_tier_color(tier_name: String) -> Color:
	match tier_name:
		"Fateforged":
			return Color(1.0, 0.84, 0.0)  # Gold/Yellow - exclusive tier
		"Sage":
			return Color(0.9, 0.3, 0.9)  # Purple
		"Archmage":
			return Color(0.4, 0.8, 1.0)  # Light blue
		"Mage":
			return Color(0.4, 0.9, 0.7)  # Teal
		"Adept":
			return Color(1.0, 0.84, 0.2)  # Gold
		"Apprentice":
			return Color(0.75, 0.75, 0.75)  # Silver
		_:  # Unbound
			return Color(0.8, 0.5, 0.3)  # Bronze


func _refresh_leaderboard() -> void:
	if _leaderboard_service:
		if _leaderboard_service.has_method("RefreshLeaderboardAndRankAsync"):
			_leaderboard_service.RefreshLeaderboardAndRankAsync(10)
		else:
			_leaderboard_service.RefreshLeaderboard(10)


## =============================================================================
## BUTTON HANDLERS
## =============================================================================

func _on_close_pressed() -> void:
	# Leave queue if in queue
	if _state == ScreenState.IN_QUEUE and _matchmaking_service:
		_matchmaking_service.LeaveQueue()

	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_ACADEMY_CAMPUS
	SceneManager.transition_to(return_scene)


func _on_queue_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	match _state:
		ScreenState.READY:
			_join_queue()
		ScreenState.IN_QUEUE:
			_leave_queue()


func _on_change_summoner_pressed() -> void:
	NavigationContext.push_return(SceneManager.SCENE_ONLINE)
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_SWITCH)


func _on_change_deck_pressed() -> void:
	collection_overlay.open_collection(
		CollectionScreen.MODE_RANKED_DECK,
		_get_active_summoner_id()
	)


func _on_collection_overlay_closed() -> void:
	_update_ui()


func _on_mode_title_pressed() -> void:
	mode_selector.popup_centered(Vector2i(460, 300))


func _join_queue() -> void:
	if not _has_valid_ranked_deck():
		status_label.text = _get_loadout_issue()
		return
	if _matchmaking_service:
		_queue_start_time = Time.get_ticks_msec() / 1000.0
		_set_state(ScreenState.IN_QUEUE)
		status_label.text = Loc.t("ui.ranked.in_queue")
		print("[RANKED][QUEUE] Joining queue")
		_matchmaking_service.JoinQueue()
	else:
		# No matchmaking service - show message
		status_label.text = Loc.t("ui.ranked.not_connected")
		print("[RANKED][QUEUE] MatchmakingService unavailable")


func _leave_queue() -> void:
	if _matchmaking_service:
		print("[RANKED][QUEUE] Leaving queue")
		_matchmaking_service.LeaveQueue()
	_set_state(ScreenState.READY)


## =============================================================================
## SIGNAL HANDLERS
## =============================================================================

func _on_authenticated(_user_id: String, _username: String) -> void:
	print("[RANKED][AUTH] Authentication succeeded for user %s" % _user_id)
	_is_authenticated = true
	_set_state(ScreenState.READY)
	_refresh_data()
	_try_auto_queue_once()


func _on_authentication_failed(_error: String) -> void:
	print("[RANKED][AUTH] Authentication failed: %s" % _error)
	_is_authenticated = false
	if ui_animation_player:
		ui_animation_player.play("error_reset")
	_set_state(ScreenState.READY)
	_refresh_local_data()


func _on_match_found(match_id: String, opponent_user_id: String, opponent_username: String, opponent_rating: int, opponent_summoner_id: String) -> void:
	print("[RANKED][MATCH_JOIN] Match found: match_id=%s opponent=%s(%s) rating=%d summoner=%s" % [match_id, opponent_username, opponent_user_id, opponent_rating, opponent_summoner_id])
	_set_state(ScreenState.MATCH_FOUND)
	status_label.text = Loc.t("ui.ranked.match_found")

	# Reset exchange state (handler already connected from _connect_services)
	_opponent_deck_received = false
	_pending_opponent_deck = []
	_pending_opponent_summoner_data = {}
	_pending_match_info = {
		"match_id": match_id,
		"opponent_user_id": opponent_user_id,
		"opponent_username": opponent_username,
		"opponent_rating": opponent_rating,
		"opponent_summoner_id": opponent_summoner_id,
	}

	# Brief delay for UI feedback (handler is already connected)
	await get_tree().create_timer(MATCH_FOUND_FEEDBACK_DELAY).timeout

	status_label.text = Loc.t("ui.ranked.exchanging_data")

	# Exchange deck data with opponent
	_exchange_deck_data(match_id, opponent_user_id, opponent_username, opponent_rating, opponent_summoner_id)


func _exchange_deck_data(match_id: String, opponent_user_id: String, opponent_username: String, opponent_rating: int, opponent_summoner_id: String) -> void:
	# Send our deck and summoner instance data
	var player_deck: Array = _get_player_deck()
	var player_summoner_id: String = _get_active_summoner_id()
	var summoner_data: Dictionary = SummonerSelectionApi.get_summoner_instance_dict(player_summoner_id)
	if summoner_data.is_empty():
		# No saved instance — create default data
		summoner_data = {
			"summoner_id": player_summoner_id,
			"level": 1,
			"xp": 0,
			"acquired_trait_ids": []
		}

	var deck_json: String = JSON.stringify({"deck": player_deck, "summoner_instance": summoner_data})
	print("[RANKED][DECK_EXCHANGE] Sending local deck payload")
	_nakama_client.SendMatchData(DECK_EXCHANGE_OP_CODE, deck_json)

	# Wait for opponent deck (with timeout)
	var elapsed: float = 0.0
	while not _opponent_deck_received and elapsed < DECK_EXCHANGE_TIMEOUT:
		await get_tree().create_timer(0.1).timeout
		elapsed += 0.1

	if not _opponent_deck_received:
		print("[RANKED][DECK_EXCHANGE] Timed out waiting for opponent deck")
		status_label.text = Loc.t("ui.ranked.exchange_timeout")
		if ui_animation_player:
			ui_animation_player.play("error_reset")
		# Clean up the Nakama match so it doesn't linger
		if _nakama_client and _nakama_client.has_method("LeaveMatch"):
			_nakama_client.LeaveMatch()
		_set_state(ScreenState.READY)
		return

	# Got opponent deck — start battle
	_start_ranked_battle(
		_pending_match_info["match_id"],
		_pending_match_info["opponent_user_id"],
		_pending_match_info["opponent_username"],
		_pending_match_info["opponent_rating"],
		_pending_match_info["opponent_summoner_id"],
		_pending_opponent_deck
	)


func _on_match_data_received(match_id: String, op_code: int, data: String, sender_id: String) -> void:
	if op_code != DECK_EXCHANGE_OP_CODE:
		return  # Not a deck exchange message

	# Only process during active exchange
	if _state != ScreenState.MATCH_FOUND:
		return

	# Validate this is for our current match
	if match_id != _pending_match_info.get("match_id", ""):
		push_warning("OnlineScreen: Ignoring deck from wrong match %s" % match_id)
		return

	var parsed: Variant = JSON.parse_string(data)
	if parsed is Dictionary:
		var dict: Dictionary = parsed
		var deck: Array = dict.get("deck", [])
		if deck.is_empty():
			push_warning("OnlineScreen: Ignoring empty opponent deck")
			return
		_pending_opponent_deck = deck
		_pending_opponent_summoner_data = dict.get("summoner_instance", {})
		_opponent_deck_received = true
		print("[RANKED][DECK_EXCHANGE] Received opponent deck payload from %s" % sender_id)


func _start_ranked_battle(match_id: String, opponent_user_id: String, opponent_username: String, opponent_rating: int, opponent_summoner_id: String, opponent_deck: Array) -> void:
	# Determine who is "host" based on user ID comparison (deterministic)
	var local_user_id: String = _get_local_user_id()

	if local_user_id.is_empty():
		push_error("OnlineScreen: Cannot start battle — local user ID is empty")
		_set_state(ScreenState.READY)
		return

	# Lexicographically smaller user ID is host (player 0)
	var is_host: bool = local_user_id < opponent_user_id
	# Generate deterministic seed from match ID
	var battle_seed: int = match_id.hash()

	# Get player summoner and deck
	var player_summoner_id: String = _get_active_summoner_id()
	var player_deck: Array = _get_player_deck()

	# Store opponent info for match reporting later
	BattleContext.set_ranked_match_info({
		"match_id": match_id,
		"opponent_user_id": opponent_user_id,
		"opponent_username": opponent_username,
		"opponent_rating": opponent_rating,
		"is_ranked": true
	})

	# Configure BattleContext for multiplayer (opponent data comes from exchange)
	BattleContext.configure_multiplayer_battle(
		player_summoner_id,
		opponent_summoner_id,
		player_deck,
		opponent_deck,
		is_host,
		battle_seed,
		_pending_opponent_summoner_data
	)

	# Multiplayer authority/session ownership is configured in BattleScene
	# via SimulationNode.ConfigureMultiplayerSession().

	# Brief delay then transition to battle
	await get_tree().create_timer(BATTLE_TRANSITION_DELAY).timeout

	# Transition to battle
	print("[RANKED][MATCH_JOIN] Transitioning to battle scene")
	SceneManager.transition_to(SceneManager.SCENE_BATTLE_3D)


func _get_active_summoner_id() -> String:
	var selected: String = SummonerSelectionApi.get_active_summoner_id()
	if not selected.is_empty():
		return selected
	return SummonerIDs.DEFAULT


func _get_player_deck() -> Array:
	return ProfileRepoApi.get_deck_array(_get_ranked_deck_id())


func _get_ranked_deck_id() -> String:
	return DecksApi.get_ranked_deck_id(_get_active_summoner_id())


func _has_valid_ranked_deck() -> bool:
	var deck_id: String = _get_ranked_deck_id()
	return not deck_id.is_empty() and DecksApi.validate_deck(deck_id)


func _get_loadout_issue() -> String:
	var deck_id: String = _get_ranked_deck_id()
	if deck_id.is_empty():
		return Loc.t("ui.ranked.choose_deck_prompt")
	if not DecksApi.validate_deck(deck_id):
		return Loc.t("ui.ranked.invalid_deck_prompt")
	return ""


func _refresh_loadout_display() -> void:
	if not character_art or not deck_name_label or not deck_rail:
		return
	var summoner_id: String = _get_active_summoner_id()
	var config: SummonerConfig = SummonerConfig.from_dict(SummonerCatalogApi.get_summoner(summoner_id))
	var summoner_name: String = config.summoner_name if not config.summoner_name.is_empty() else summoner_id.capitalize()
	character_art.text = Loc.t("ui.ranked.character_art_placeholder")
	character_art.tooltip_text = "%s\n%s" % [summoner_name, Loc.t("ui.ranked.change_summoner")]

	var deck_id: String = _get_ranked_deck_id()
	var deck: Dictionary = DecksApi.get_deck_dict(deck_id) if not deck_id.is_empty() else {}
	_clear_deck_rail()
	if deck.is_empty():
		_show_empty_deck_state()
		return
	var deck_name: String = SafeTypeUtils.string(
		deck.get("name", ""), Loc.t("ui.collection.unnamed_deck")
	)
	deck_name_label.text = deck_name
	deck_rail_button.text = ""
	deck_rail_button.flat = true
	deck_rail_button.tooltip_text = "%s\n%s" % [deck_name, Loc.t("ui.ranked.change_deck")]
	for card_instance_id_value: Variant in SafeTypeUtils.array(deck.get("card_instance_ids", [])):
		var card_instance_id: String = SafeTypeUtils.string(card_instance_id_value)
		var card_instance: Dictionary = CardServiceApi.get_card_dict(card_instance_id)
		var catalog_id: String = SafeTypeUtils.string(card_instance.get("catalog_id"))
		var catalog_data: Dictionary = CardCatalogApi.get_card_as_dict(catalog_id)
		if not catalog_data.is_empty():
			_add_deck_card(catalog_data)


func _show_empty_deck_state() -> void:
	deck_name_label.text = Loc.t("ui.ranked.no_deck_selected")
	deck_rail_button.text = Loc.t("ui.ranked.choose_ranked_deck")
	deck_rail_button.flat = false
	deck_rail_button.tooltip_text = Loc.t("ui.ranked.choose_ranked_deck")


func _clear_deck_rail() -> void:
	for child: Node in deck_rail.get_children():
		deck_rail.remove_child(child)
		child.queue_free()


func _add_deck_card(catalog_data: Dictionary) -> void:
	var slot: Control = Control.new()
	slot.custom_minimum_size = DECK_CARD_SIZE
	slot.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var card_visual: CardVisual = CARD_VISUAL_SCENE.instantiate() as CardVisual
	card_visual.set_display_size(DECK_CARD_SIZE)
	card_visual.show_description = false
	card_visual.mouse_filter = Control.MOUSE_FILTER_IGNORE
	card_visual.set_card_data(catalog_data, false)
	_set_mouse_filter_ignored(card_visual)
	slot.add_child(card_visual)
	deck_rail.add_child(slot)


func _set_mouse_filter_ignored(node: Node) -> void:
	if node is Control:
		(node as Control).mouse_filter = Control.MOUSE_FILTER_IGNORE
	for child: Node in node.get_children():
		_set_mouse_filter_ignored(child)


func _on_matchmaking_cancelled(reason: String) -> void:
	print("[RANKED][QUEUE] Matchmaking cancelled: %s" % reason)
	if ui_animation_player:
		ui_animation_player.play("error_reset")
	_set_state(ScreenState.READY)
	if not reason.is_empty():
		status_label.text = reason


func _on_matchmaking_error(error: String) -> void:
	print("[RANKED][QUEUE] Matchmaking error: %s" % error)
	if ui_animation_player:
		ui_animation_player.play("error_reset")
	_set_state(ScreenState.READY)
	status_label.text = Loc.t("ui.ranked.not_connected") if error.is_empty() else error


func _on_queue_status_changed(is_in_queue: bool, _queue_time: float) -> void:
	if is_in_queue and _state != ScreenState.IN_QUEUE:
		_set_state(ScreenState.IN_QUEUE)
	elif not is_in_queue and _state == ScreenState.IN_QUEUE:
		_set_state(ScreenState.READY)


func _on_leaderboard_refreshed() -> void:
	# The leaderboard remains authoritative for the top-20 Fateforged tier even
	# though leaderboard rows are no longer presented on this screen.
	_refresh_rating_display()


func _on_socket_disconnected() -> void:
	print("[RANKED][RECONNECT] Socket disconnected")
	_is_authenticated = false
	if _state == ScreenState.IN_QUEUE:
		_set_state(ScreenState.READY)
	status_label.text = Loc.t("ui.ranked.not_connected")
	queue_button.disabled = true


func _on_socket_connected() -> void:
	print("[RANKED][RECONNECT] Socket connected")
	_is_authenticated = true
	if _state == ScreenState.READY:
		queue_button.disabled = not _has_valid_ranked_deck()


func _should_auto_queue() -> bool:
	return "--auto-queue" in OS.get_cmdline_user_args()


func _try_auto_queue_once() -> void:
	if _auto_queue_once:
		return
	if not _should_auto_queue():
		return
	if _state != ScreenState.READY:
		return
	_auto_queue_once = true
	print("[RANKED][QUEUE] Auto-queue enabled, joining queue")
	_join_queue()


func _ensure_ui_animations() -> void:
	if not ui_animation_player:
		return
	if ui_animation_player.has_animation("ready_idle") and ui_animation_player.has_animation("queue_active") and ui_animation_player.has_animation("match_found_reveal") and ui_animation_player.has_animation("error_reset"):
		return

	var library: AnimationLibrary
	if ui_animation_player.has_animation_library(""):
		library = ui_animation_player.get_animation_library("")
	else:
		library = AnimationLibrary.new()
		ui_animation_player.add_animation_library("", library)

	if not library.has_animation("ready_idle"):
		var ready_anim: Animation = Animation.new()
		ready_anim.length = 0.01
		ready_anim.loop_mode = Animation.LOOP_NONE
		var ready_track: int = ready_anim.add_track(Animation.TYPE_VALUE)
		ready_anim.track_set_path(ready_track, NodePath("MarginContainer:modulate"))
		ready_anim.track_insert_key(ready_track, 0.0, Color(1, 1, 1, 1))
		library.add_animation("ready_idle", ready_anim)

	if not library.has_animation("queue_active"):
		var queue_anim: Animation = Animation.new()
		queue_anim.length = 1.2
		queue_anim.loop_mode = Animation.LOOP_LINEAR
		var queue_track: int = queue_anim.add_track(Animation.TYPE_VALUE)
		queue_anim.track_set_path(queue_track, NodePath("MarginContainer:modulate"))
		queue_anim.track_insert_key(queue_track, 0.0, Color(1, 1, 1, 1))
		queue_anim.track_insert_key(queue_track, 0.6, Color(0.9, 0.95, 1.0, 1))
		queue_anim.track_insert_key(queue_track, 1.2, Color(1, 1, 1, 1))
		library.add_animation("queue_active", queue_anim)

	if not library.has_animation("match_found_reveal"):
		var match_anim: Animation = Animation.new()
		match_anim.length = 0.6
		match_anim.loop_mode = Animation.LOOP_NONE
		var match_track: int = match_anim.add_track(Animation.TYPE_VALUE)
		match_anim.track_set_path(match_track, NodePath("MarginContainer:scale"))
		match_anim.track_insert_key(match_track, 0.0, Vector2(1.0, 1.0))
		match_anim.track_insert_key(match_track, 0.2, Vector2(1.03, 1.03))
		match_anim.track_insert_key(match_track, 0.6, Vector2(1.0, 1.0))
		library.add_animation("match_found_reveal", match_anim)

	if not library.has_animation("error_reset"):
		var error_anim: Animation = Animation.new()
		error_anim.length = 0.25
		error_anim.loop_mode = Animation.LOOP_NONE
		var error_track: int = error_anim.add_track(Animation.TYPE_VALUE)
		error_anim.track_set_path(error_track, NodePath("MarginContainer:modulate"))
		error_anim.track_insert_key(error_track, 0.0, Color(1.0, 0.85, 0.85, 1))
		error_anim.track_insert_key(error_track, 0.25, Color(1, 1, 1, 1))
		library.add_animation("error_reset", error_anim)


func _update_ui_animation(state: ScreenState) -> void:
	if not ui_animation_player:
		return
	match state:
		ScreenState.LOADING, ScreenState.READY:
			ui_animation_player.play("ready_idle")
		ScreenState.IN_QUEUE:
			ui_animation_player.play("queue_active")
		ScreenState.MATCH_FOUND:
			ui_animation_player.play("match_found_reveal")


func _request_back_navigation() -> void:
	_on_close_pressed()
