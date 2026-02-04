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

## UI References
@onready var close_button: Button = %CloseButton
@onready var title_label: Label = $MarginContainer/VBoxContainer/Header/Title
@onready var tier_label: Label = %TierLabel
@onready var rating_label: Label = %RatingLabel
@onready var rank_label: Label = %RankLabel
@onready var wins_value: Label = %"MarginContainer/VBoxContainer/ContentHBox/LeftPanel/StatsPanel/MarginContainer/HBox/WinsBox/Value"
@onready var losses_value: Label = %"MarginContainer/VBoxContainer/ContentHBox/LeftPanel/StatsPanel/MarginContainer/HBox/LossesBox/Value"
@onready var win_rate_value: Label = %"MarginContainer/VBoxContainer/ContentHBox/LeftPanel/StatsPanel/MarginContainer/HBox/WinRateBox/Value"
@onready var status_label: Label = %StatusLabel
@onready var queue_button: Button = %QueueButton
@onready var leaderboard_header: Label = $MarginContainer/VBoxContainer/ContentHBox/RightPanel/LeaderboardHeader
@onready var leaderboard_list: VBoxContainer = %LeaderboardList

## State
var _state: ScreenState = ScreenState.LOADING
var _queue_start_time: float = 0.0
var _nakama_client: Node = null
var _matchmaking_service: Node = null
var _ranking_service: Node = null
var _leaderboard_service: Node = null


func _ready() -> void:
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
	# Get autoload services
	_nakama_client = get_node_or_null("/root/NakamaGameClient")
	_matchmaking_service = get_node_or_null("/root/MatchmakingService")
	_ranking_service = get_node_or_null("/root/RankingService")
	_leaderboard_service = get_node_or_null("/root/LeaderboardService")

	# Connect signals
	if _nakama_client:
		if _nakama_client.has_signal("Authenticated"):
			_nakama_client.Authenticated.connect(_on_authenticated)
		if _nakama_client.has_signal("AuthenticationFailed"):
			_nakama_client.AuthenticationFailed.connect(_on_authentication_failed)

	if _matchmaking_service:
		if _matchmaking_service.has_signal("MatchFound"):
			_matchmaking_service.MatchFound.connect(_on_match_found)
		if _matchmaking_service.has_signal("MatchmakingCancelled"):
			_matchmaking_service.MatchmakingCancelled.connect(_on_matchmaking_cancelled)
		if _matchmaking_service.has_signal("QueueStatusChanged"):
			_matchmaking_service.QueueStatusChanged.connect(_on_queue_status_changed)

	if _leaderboard_service:
		if _leaderboard_service.has_signal("LeaderboardRefreshed"):
			_leaderboard_service.LeaderboardRefreshed.connect(_on_leaderboard_refreshed)

	# Start authentication if not already authenticated
	if _nakama_client and _nakama_client.has_method("get_IsAuthenticated"):
		if not _nakama_client.get_IsAuthenticated():
			_set_state(ScreenState.LOADING)
			status_label.text = Loc.t("ui.ranked.authenticating")
			if _nakama_client.has_method("AuthenticateAsync"):
				_nakama_client.AuthenticateAsync()
		else:
			_set_state(ScreenState.READY)
			_refresh_data()
	else:
		# Services not available - show local data
		_set_state(ScreenState.READY)
		_refresh_local_data()


func _set_state(new_state: ScreenState) -> void:
	_state = new_state
	_update_ui()


func _update_ui() -> void:
	match _state:
		ScreenState.LOADING:
			queue_button.disabled = true
			queue_button.text = Loc.t("ui.ranked.find_match")
		ScreenState.READY:
			queue_button.disabled = false
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
	# Use local data when services aren't available
	var rating: int = 1000
	if _ranking_service and _ranking_service.has_method("GetRating"):
		rating = _ranking_service.GetRating()
	elif _ranking_service and _ranking_service.has_method("get_Rating"):
		rating = _ranking_service.get_Rating()

	_update_rating_display(rating)
	_update_stats_display(0, 0)
	_populate_mock_leaderboard()


func _refresh_rating_display() -> void:
	var rating: int = 1000

	if _ranking_service:
		if _ranking_service.has_method("GetRating"):
			rating = _ranking_service.GetRating()
		elif _ranking_service.has_method("get_Rating"):
			rating = _ranking_service.get_Rating()

	_update_rating_display(rating)


func _update_rating_display(rating: int) -> void:
	rating_label.text = str(rating)

	# Calculate tier using the same thresholds as EloCalculator
	var tier_name: String = _get_tier_name(rating)
	var division: int = _get_division(rating)
	var division_str: String = _int_to_roman(division)
	tier_label.text = tier_name + " " + division_str

	# Set tier color
	tier_label.add_theme_color_override("font_color", _get_tier_color(tier_name))

	# Rank display (placeholder until we get actual rank from leaderboard)
	rank_label.text = Loc.t("ui.ranked.your_rank") + ": -"


func _get_tier_name(rating: int) -> String:
	if rating >= 2400:
		return Loc.t("ui.ranked.tier_legend")
	elif rating >= 2200:
		return Loc.t("ui.ranked.tier_master")
	elif rating >= 2000:
		return Loc.t("ui.ranked.tier_diamond")
	elif rating >= 1600:
		return Loc.t("ui.ranked.tier_platinum")
	elif rating >= 1200:
		return Loc.t("ui.ranked.tier_gold")
	elif rating >= 800:
		return Loc.t("ui.ranked.tier_silver")
	else:
		return Loc.t("ui.ranked.tier_bronze")


func _get_division(rating: int) -> int:
	# Division within tier (1-4, where 1 is highest)
	var thresholds: Array[int] = [0, 800, 1200, 1600, 2000, 2200, 2400]
	var tier_width: int = 400

	for i in range(thresholds.size() - 1, -1, -1):
		if rating >= thresholds[i]:
			if i >= 5:  # Master and Legend don't have divisions
				return 1
			var within_tier: int = rating - thresholds[i]
			var division: int = 4 - (within_tier / 100)
			return clampi(division, 1, 4)

	return 4


func _int_to_roman(num: int) -> String:
	match num:
		1: return "I"
		2: return "II"
		3: return "III"
		4: return "IV"
		_: return ""


func _get_tier_color(tier_name: String) -> Color:
	# Return color based on tier
	if tier_name == Loc.t("ui.ranked.tier_legend"):
		return Color(1.0, 0.84, 0.0)  # Gold
	elif tier_name == Loc.t("ui.ranked.tier_master"):
		return Color(0.9, 0.3, 0.9)  # Purple
	elif tier_name == Loc.t("ui.ranked.tier_diamond"):
		return Color(0.4, 0.8, 1.0)  # Light blue
	elif tier_name == Loc.t("ui.ranked.tier_platinum"):
		return Color(0.4, 0.9, 0.7)  # Teal
	elif tier_name == Loc.t("ui.ranked.tier_gold"):
		return Color(1.0, 0.84, 0.2)  # Gold
	elif tier_name == Loc.t("ui.ranked.tier_silver"):
		return Color(0.75, 0.75, 0.75)  # Silver
	else:
		return Color(0.8, 0.5, 0.3)  # Bronze


func _refresh_stats_display() -> void:
	var wins: int = 0
	var losses: int = 0

	# Get match history from MatchReporter if available
	var match_reporter: Node = get_node_or_null("/root/MatchReporter")
	if match_reporter:
		if match_reporter.has_method("get_MatchHistory"):
			var history: Array = match_reporter.get_MatchHistory()
			for match_data in history:
				if match_data.get("LocalPlayerWon", false):
					wins += 1
				else:
					losses += 1

	_update_stats_display(wins, losses)


func _update_stats_display(wins: int, losses: int) -> void:
	wins_value.text = str(wins)
	losses_value.text = str(losses)

	var total: int = wins + losses
	if total > 0:
		var win_rate: float = float(wins) / float(total) * 100.0
		win_rate_value.text = "%.0f%%" % win_rate
	else:
		win_rate_value.text = "-%"


func _refresh_leaderboard() -> void:
	if _leaderboard_service and _leaderboard_service.has_method("GetTopPlayersAsync"):
		# Async call - will update via signal
		_leaderboard_service.GetTopPlayersAsync(10, false)
	else:
		_populate_mock_leaderboard()


func _populate_leaderboard(entries: Array) -> void:
	# Clear existing entries
	for child in leaderboard_list.get_children():
		child.queue_free()

	# Add new entries
	for entry in entries:
		var row: HBoxContainer = _create_leaderboard_row(entry)
		leaderboard_list.add_child(row)


func _populate_mock_leaderboard() -> void:
	# Clear existing entries
	for child in leaderboard_list.get_children():
		child.queue_free()

	# Create mock entries
	var mock_data: Array = [
		{"rank": 1, "name": "DragonSlayer", "rating": 1850},
		{"rank": 2, "name": "ShadowMage", "rating": 1780},
		{"rank": 3, "name": "IronKnight", "rating": 1720},
		{"rank": 4, "name": "StormBringer", "rating": 1680},
		{"rank": 5, "name": "FrostQueen", "rating": 1640},
	]

	for entry in mock_data:
		var row: HBoxContainer = _create_mock_leaderboard_row(entry)
		leaderboard_list.add_child(row)


func _create_leaderboard_row(entry: Variant) -> HBoxContainer:
	var row: HBoxContainer = HBoxContainer.new()
	row.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	# Rank
	var rank_lbl: Label = Label.new()
	rank_lbl.custom_minimum_size = Vector2(50, 0)
	rank_lbl.text = "#%d" % entry.get("Rank", 0)
	rank_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	row.add_child(rank_lbl)

	# Name
	var name_lbl: Label = Label.new()
	name_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	name_lbl.text = str(entry.get("DisplayName", "Unknown"))
	row.add_child(name_lbl)

	# Rating
	var rating_lbl: Label = Label.new()
	rating_lbl.custom_minimum_size = Vector2(80, 0)
	rating_lbl.text = str(entry.get("Rating", 0))
	rating_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	row.add_child(rating_lbl)

	return row


func _create_mock_leaderboard_row(entry: Dictionary) -> HBoxContainer:
	var row: HBoxContainer = HBoxContainer.new()
	row.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	# Rank
	var rank_lbl: Label = Label.new()
	rank_lbl.custom_minimum_size = Vector2(50, 0)
	rank_lbl.text = "#%d" % entry.get("rank", 0)
	rank_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	row.add_child(rank_lbl)

	# Name
	var name_lbl: Label = Label.new()
	name_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	name_lbl.text = entry.get("name", "Unknown")
	row.add_child(name_lbl)

	# Rating
	var rating_lbl: Label = Label.new()
	rating_lbl.custom_minimum_size = Vector2(80, 0)
	rating_lbl.text = str(entry.get("rating", 0))
	rating_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	row.add_child(rating_lbl)

	return row


## =============================================================================
## BUTTON HANDLERS
## =============================================================================

func _on_close_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)

	# Leave queue if in queue
	if _state == ScreenState.IN_QUEUE and _matchmaking_service:
		if _matchmaking_service.has_method("LeaveQueueAsync"):
			_matchmaking_service.LeaveQueueAsync()

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
	if _matchmaking_service and _matchmaking_service.has_method("JoinQueueAsync"):
		_queue_start_time = Time.get_ticks_msec() / 1000.0
		_set_state(ScreenState.IN_QUEUE)
		status_label.text = Loc.t("ui.ranked.in_queue")
		_matchmaking_service.JoinQueueAsync()
	else:
		# No matchmaking service - show message
		status_label.text = Loc.t("ui.ranked.not_connected")


func _leave_queue() -> void:
	if _matchmaking_service and _matchmaking_service.has_method("LeaveQueueAsync"):
		_matchmaking_service.LeaveQueueAsync()
	_set_state(ScreenState.READY)


## =============================================================================
## SIGNAL HANDLERS
## =============================================================================

func _on_authenticated() -> void:
	_set_state(ScreenState.READY)
	_refresh_data()


func _on_authentication_failed(error: String) -> void:
	_set_state(ScreenState.READY)
	status_label.text = Loc.t("ui.ranked.authentication_failed")
	_refresh_local_data()


func _on_match_found(match_id: String, opponent_user_id: String, opponent_username: String, opponent_rating: int) -> void:
	_set_state(ScreenState.MATCH_FOUND)
	status_label.text = Loc.t("ui.ranked.match_found")

	# Brief delay for UI feedback
	await get_tree().create_timer(1.0).timeout

	status_label.text = Loc.t("ui.ranked.connecting")

	# Store match info for battle setup
	_start_ranked_battle(match_id, opponent_user_id, opponent_username, opponent_rating)


func _start_ranked_battle(match_id: String, opponent_user_id: String, opponent_username: String, opponent_rating: int) -> void:
	# Determine who is "host" based on user ID comparison (deterministic)
	var local_user_id: String = ""
	if _nakama_client and _nakama_client.has_method("get_UserId"):
		local_user_id = _nakama_client.get_UserId()

	# Lexicographically smaller user ID is host (player 0)
	var is_host: bool = local_user_id < opponent_user_id
	var local_player_index: int = 0 if is_host else 1

	# Generate deterministic seed from match ID
	var battle_seed: int = match_id.hash()

	# Set up BattleRNG
	BattleRNG.set_battle_seed(battle_seed)

	# Get player summoner and deck
	var player_summoner_id: String = _get_active_summoner_id()
	var player_deck: Array = _get_player_deck()

	# For opponent, we don't have their deck yet - it will be synced during battle
	# In a full implementation, this would be exchanged via Nakama match data
	var opponent_summoner_id: String = "ignis"  # Placeholder - would come from matchmaking
	var opponent_deck: Array = []

	# Store opponent info for match reporting later
	BattleContext.set_ranked_match_info({
		"match_id": match_id,
		"opponent_user_id": opponent_user_id,
		"opponent_username": opponent_username,
		"opponent_rating": opponent_rating,
		"is_ranked": true
	})

	# Configure BattleContext for multiplayer
	BattleContext.configure_multiplayer_battle(
		player_summoner_id,
		opponent_summoner_id,
		player_deck,
		opponent_deck,
		is_host,
		battle_seed
	)

	# Set up multiplayer authority
	var MultiplayerAuthorityScript: GDScript = preload("res://scripts/multiplayer/authority/multiplayer_authority.gd")
	var mp_authority: RefCounted = MultiplayerAuthorityScript.new(
		null,  # MatchSession will be created in battle scene
		is_host,
		1 if is_host else 2,  # Peer ID
		local_player_index
	)
	BattleContext.set_authority_provider(mp_authority)

	# Brief delay then transition to battle
	await get_tree().create_timer(0.5).timeout

	# Transition to battle
	SceneManager.transition_to(SceneManager.SCENE_BATTLE_3D)


func _get_active_summoner_id() -> String:
	var summoner_selection: Node = get_node_or_null("/root/SummonerSelection")
	if summoner_selection and summoner_selection.has_method("get_selected_summoner_id"):
		var selected: String = summoner_selection.get_selected_summoner_id()
		if not selected.is_empty():
			return selected
	return "ignis"  # Default fallback


func _get_player_deck() -> Array:
	var profile_repo: Node = get_node_or_null("/root/ProfileRepo")
	if profile_repo and profile_repo.has_method("get_active_deck"):
		return profile_repo.get_active_deck()
	# Fallback placeholder deck
	return [
		{"catalog_id": "puff", "count": 3},
		{"catalog_id": "pebbloom", "count": 2},
	]


func _on_matchmaking_cancelled(reason: String) -> void:
	_set_state(ScreenState.READY)
	if not reason.is_empty():
		status_label.text = reason


func _on_queue_status_changed(is_in_queue: bool, players_in_queue: int) -> void:
	if is_in_queue and _state != ScreenState.IN_QUEUE:
		_set_state(ScreenState.IN_QUEUE)
	elif not is_in_queue and _state == ScreenState.IN_QUEUE:
		_set_state(ScreenState.READY)


func _on_leaderboard_refreshed() -> void:
	if _leaderboard_service and _leaderboard_service.has_method("get_TopPlayers"):
		var top_players: Array = _leaderboard_service.get_TopPlayers()
		_populate_leaderboard(top_players)

	# Update player rank if available
	if _leaderboard_service and _leaderboard_service.has_method("get_PlayerRank"):
		var player_rank: Variant = _leaderboard_service.get_PlayerRank()
		if player_rank:
			var rank_num: int = player_rank.get("Rank", 0)
			if rank_num > 0:
				rank_label.text = Loc.t("ui.ranked.your_rank") + ": #%d" % rank_num
			else:
				rank_label.text = Loc.t("ui.ranked.your_rank") + ": " + Loc.t("ui.ranked.unranked")
