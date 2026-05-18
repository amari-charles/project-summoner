extends Control
class_name OnlineScreen

## Online/Ranked Screen - Shows rating, queue status, and leaderboards
##
## Connects to Nakama services for:
## - Authentication
## - Matchmaking queue
## - Rating/ranking display
## - Leaderboards

enum ScreenState { LOADING, READY, IN_QUEUE, MATCH_FOUND }

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

## Top 20 threshold for Fateforged tier
const FATEFORGED_THRESHOLD: int = 20

## UI References
@onready var close_button: Button = %CloseButton
@onready var title_label: Label = $MarginContainer/VBoxContainer/Header/Title
@onready var tier_label: Label = %TierLabel
@onready var rating_label: Label = %RatingLabel
@onready var rank_label: Label = %RankLabel
@onready var wins_value: Label = $MarginContainer/VBoxContainer/ContentHBox/LeftPanel/StatsPanel/MarginContainer/HBox/WinsBox/Value
@onready var losses_value: Label = $MarginContainer/VBoxContainer/ContentHBox/LeftPanel/StatsPanel/MarginContainer/HBox/LossesBox/Value
@onready var win_rate_value: Label = $MarginContainer/VBoxContainer/ContentHBox/LeftPanel/StatsPanel/MarginContainer/HBox/WinRateBox/Value
@onready var status_label: Label = %StatusLabel
@onready var queue_button: Button = %QueueButton
@onready var leaderboard_header: Label = $MarginContainer/VBoxContainer/ContentHBox/RightPanel/LeaderboardHeader
@onready var leaderboard_list: VBoxContainer = %LeaderboardList
@onready var ui_animation_player: AnimationPlayer = $AnimationPlayer

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
	_ensure_ui_animations()
	_setup_localization()
	_setup_signals()
	_connect_services()
	_update_ui()


func _process(delta: float) -> void:
	if _state == ScreenState.IN_QUEUE:
		_update_queue_time()


func _setup_localization() -> void:
	title_label.text = Loc.t("ui.ranked.title")
	leaderboard_header.text = Loc.t("ui.ranked.leaderboard_title")
	queue_button.text = Loc.t("ui.ranked.find_match")


func _setup_signals() -> void:
	close_button.pressed.connect(_on_close_pressed)
	queue_button.pressed.connect(_on_queue_pressed)


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
	match _state:
		ScreenState.LOADING:
			queue_button.disabled = true
			queue_button.text = Loc.t("ui.ranked.find_match")
		ScreenState.READY:
			queue_button.disabled = not _is_authenticated
			queue_button.text = Loc.t("ui.ranked.find_match")
			status_label.text = ""
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
	_refresh_stats_display()
	_refresh_leaderboard()


func _refresh_local_data() -> void:
	var rating: int = STARTING_ELO
	if _ranking_service and _ranking_service.has_method("GetRating"):
		rating = _ranking_service.GetRating()

	_update_rating_display(rating)
	_update_stats_display(0, 0)
	_populate_mock_leaderboard()


func _refresh_rating_display() -> void:
	var rating: int = STARTING_ELO

	if _ranking_service and _ranking_service.has_method("GetRating"):
		rating = _ranking_service.GetRating()

	_update_rating_display(rating)


func _update_rating_display(rating: int) -> void:
	rating_label.text = str(rating)

	# Check if player is Fateforged (top 20)
	var is_fateforged: bool = _is_player_fateforged()

	if is_fateforged:
		tier_label.text = Loc.t("ui.ranked.tier_fateforged")
		tier_label.add_theme_color_override("font_color", _get_tier_color("Fateforged"))
	else:
		# Get tier name from RankingService (single source of truth)
		var tier_name: String = _get_tier_name_from_service(rating)
		var division: int = _get_division_from_service(rating)
		var division_str: String = _int_to_roman(division)
		tier_label.text = Loc.t("ui.ranked.tier_" + tier_name.to_lower()) + " " + division_str
		tier_label.add_theme_color_override("font_color", _get_tier_color(tier_name))

	# Rank display (placeholder until we get actual rank from leaderboard)
	rank_label.text = Loc.t("ui.ranked.your_rank") + ": -"


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


func _get_division_from_service(rating: int) -> int:
	if _ranking_service and _ranking_service.has_method("GetDivisionForRating"):
		return _ranking_service.GetDivisionForRating(rating)
	return 1


func _int_to_roman(num: int) -> String:
	match num:
		1: return "I"
		2: return "II"
		3: return "III"
		4: return "IV"
		_: return ""


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


func _refresh_stats_display() -> void:
	var wins: int = 0
	var losses: int = 0

	# Get stats from RankingService
	if _ranking_service:
		if _ranking_service.has_method("GetWins"):
			wins = _ranking_service.GetWins()
		if _ranking_service.has_method("GetLosses"):
			losses = _ranking_service.GetLosses()

	_update_stats_display(wins, losses)


func _update_stats_display(wins: int, losses: int) -> void:
	wins_value.text = str(wins)
	losses_value.text = str(losses)

	var total: int = wins + losses
	if total > 0:
		# GetWinRate() returns 0.0-1.0 ratio, multiply by 100 for display
		var win_rate: float = float(wins) / float(total) * 100.0
		win_rate_value.text = "%.0f%%" % win_rate
	else:
		win_rate_value.text = "-%"


func _refresh_leaderboard() -> void:
	if _leaderboard_service:
		if _leaderboard_service.has_method("RefreshLeaderboardAndRankAsync"):
			_leaderboard_service.RefreshLeaderboardAndRankAsync(10)
		else:
			_leaderboard_service.RefreshLeaderboard(10)
	else:
		_populate_mock_leaderboard()


func _populate_leaderboard(entries: Array) -> void:
	# Clear existing entries
	for child: Node in leaderboard_list.get_children():
		child.queue_free()

	# Add new entries
	for entry: Variant in entries:
		var row: HBoxContainer = _create_leaderboard_row(entry)
		leaderboard_list.add_child(row)


func _populate_mock_leaderboard() -> void:
	# Clear existing entries
	for child: Node in leaderboard_list.get_children():
		child.queue_free()

	# Create mock entries (placeholder data for offline mode)
	var mock_data: Array = [
		{"rank": 1, "name": "PLACEHOLDER_PLAYER_1", "rating": 1850},
		{"rank": 2, "name": "PLACEHOLDER_PLAYER_2", "rating": 1780},
		{"rank": 3, "name": "PLACEHOLDER_PLAYER_3", "rating": 1720},
		{"rank": 4, "name": "PLACEHOLDER_PLAYER_4", "rating": 1680},
		{"rank": 5, "name": "PLACEHOLDER_PLAYER_5", "rating": 1640},
	]

	for entry: Dictionary in mock_data:
		var row: HBoxContainer = _create_leaderboard_row(entry)
		leaderboard_list.add_child(row)


func _create_leaderboard_row(entry: Variant) -> HBoxContainer:
	# Normalize keys — service returns PascalCase, mock data uses snake_case
	var rank_val: int = entry.get("Rank", entry.get("rank", 0))
	var name_val: String = str(entry.get("DisplayName", entry.get("name", Loc.t("ui.ranked.unknown_player"))))
	var rating_val: int = entry.get("Rating", entry.get("rating", 0))

	var row: HBoxContainer = HBoxContainer.new()
	row.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	var rank_lbl: Label = Label.new()
	rank_lbl.custom_minimum_size = Vector2(50, 0)
	rank_lbl.text = "#%d" % rank_val
	rank_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	row.add_child(rank_lbl)

	var name_lbl: Label = Label.new()
	name_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	name_lbl.text = name_val
	row.add_child(name_lbl)

	var rating_lbl: Label = Label.new()
	rating_lbl.custom_minimum_size = Vector2(80, 0)
	rating_lbl.text = str(rating_val)
	rating_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	row.add_child(rating_lbl)

	return row


## =============================================================================
## BUTTON HANDLERS
## =============================================================================

func _on_close_pressed() -> void:
	# Leave queue if in queue
	if _state == ScreenState.IN_QUEUE and _matchmaking_service:
		_matchmaking_service.LeaveQueue()

	var return_scene: String = NavigationContext.pop_return()
	if return_scene.is_empty():
		return_scene = SceneManager.SCENE_CAMPAIGN_MAP
	SceneManager.transition_to(return_scene)


func _on_queue_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)

	match _state:
		ScreenState.READY:
			_join_queue()
		ScreenState.IN_QUEUE:
			_leave_queue()


func _join_queue() -> void:
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
	status_label.text = Loc.t("ui.ranked.authentication_failed")
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
	return ProfileRepoApi.get_active_deck_array()


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
	if _leaderboard_service:
		var top_players: Variant = _leaderboard_service.get("TopPlayers")
		if top_players is Array:
			_populate_leaderboard(top_players)

	# Update player rank if available
	var rank_num: int = 0
	if _leaderboard_service:
		var player_rank: Variant = _leaderboard_service.get("PlayerRank")
		if player_rank is Dictionary:
			rank_num = int(player_rank.get("Rank", 0))
		elif player_rank is Object:
			var rank_val: Variant = player_rank.get("Rank")
			if rank_val != null:
				rank_num = int(rank_val)

	if rank_num > 0:
		rank_label.text = Loc.t("ui.ranked.your_rank") + ": #%d" % rank_num
		print("[RANKED][REPORT] Player rank refreshed: #%d" % rank_num)
	else:
		rank_label.text = Loc.t("ui.ranked.your_rank") + ": " + Loc.t("ui.ranked.unranked")
		print("[RANKED][REPORT] Player rank refreshed: unranked")

	# Re-check Fateforged status after leaderboard refresh
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
		queue_button.disabled = false


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
