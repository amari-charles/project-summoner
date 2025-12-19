extends Node

## AudioManager - Central audio system for music and volume control
##
## Manages:
## - Audio bus setup (Master, Music, SFX)
## - Background music playback with crossfade transitions
## - UI sound effects
## - Volume control with ProfileRepo persistence
##
## Usage:
##   AudioManager.play_music(AudioManager.MUSIC_BATTLE)
##   AudioManager.set_volume(AudioManager.BUS_MUSIC, 0.5)
##   AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)

signal volume_changed(bus_name: String, volume: float)
signal music_changed(track_path: String)

## Audio bus names
const BUS_MASTER: String = "Master"
const BUS_MUSIC: String = "Music"
const BUS_SFX: String = "SFX"

## Default crossfade duration in seconds
const DEFAULT_CROSSFADE: float = 1.0

## Default fade-out duration when stopping music
const DEFAULT_FADE_OUT: float = 0.5

## Music track paths
const MUSIC_BATTLE: String = "res://resources/audio/bgm/battle.mp3"

## Sound effect IDs (use with play_ui_sound)
const SFX_UI_CLICK: String = "ui_click"
const SFX_CARD_DRAW: String = "card_draw"
const SFX_CARD_PLAY: String = "card_play"

## Sound effect paths
const _SFX_SOUNDS: Dictionary = {
	SFX_UI_CLICK: "res://resources/audio/sfx/ui_click.wav",
	SFX_CARD_DRAW: "res://resources/audio/sfx/card_draw.mp3",
	SFX_CARD_PLAY: "res://resources/audio/sfx/card_play.wav",
}

## Music players for crossfade support
var _music_player_a: AudioStreamPlayer = null
var _music_player_b: AudioStreamPlayer = null
var _active_player: AudioStreamPlayer = null
var _current_music_path: String = ""
var _crossfade_tween: Tween = null

## Bus indices (cached after setup)
var _music_bus_idx: int = -1
var _sfx_bus_idx: int = -1

## UI sound player and cache
var _ui_player: AudioStreamPlayer = null
var _ui_sound_cache: Dictionary = {}


func _ready() -> void:
	# Process even when game is paused (for music fades, UI sounds)
	process_mode = Node.PROCESS_MODE_ALWAYS
	_setup_audio_buses()
	_create_music_players()
	_create_ui_player()
	_preload_ui_sounds()
	_apply_settings_volume()


## =============================================================================
## AUDIO BUS SETUP
## =============================================================================

## Create Music and SFX buses if they don't exist
func _setup_audio_buses() -> void:
	_music_bus_idx = AudioServer.get_bus_index(BUS_MUSIC)
	if _music_bus_idx == -1:
		var bus_count: int = AudioServer.bus_count
		AudioServer.add_bus(bus_count)
		AudioServer.set_bus_name(bus_count, BUS_MUSIC)
		AudioServer.set_bus_send(bus_count, BUS_MASTER)
		_music_bus_idx = bus_count

	_sfx_bus_idx = AudioServer.get_bus_index(BUS_SFX)
	if _sfx_bus_idx == -1:
		var bus_count: int = AudioServer.bus_count
		AudioServer.add_bus(bus_count)
		AudioServer.set_bus_name(bus_count, BUS_SFX)
		AudioServer.set_bus_send(bus_count, BUS_MASTER)
		_sfx_bus_idx = bus_count


## Create two AudioStreamPlayers for crossfade support
func _create_music_players() -> void:
	_music_player_a = AudioStreamPlayer.new()
	_music_player_a.name = "MusicPlayerA"
	_music_player_a.bus = BUS_MUSIC
	add_child(_music_player_a)

	_music_player_b = AudioStreamPlayer.new()
	_music_player_b.name = "MusicPlayerB"
	_music_player_b.bus = BUS_MUSIC
	add_child(_music_player_b)

	_active_player = _music_player_a


## Create a dedicated player for UI sounds (non-positional)
func _create_ui_player() -> void:
	_ui_player = AudioStreamPlayer.new()
	_ui_player.name = "UIPlayer"
	_ui_player.bus = BUS_SFX
	add_child(_ui_player)


## Preload all sound effects into cache
func _preload_ui_sounds() -> void:
	for sound_id: String in _SFX_SOUNDS:
		var path_val: Variant = _SFX_SOUNDS[sound_id]
		if path_val is String:
			var path: String = path_val
			if ResourceLoader.exists(path):
				var stream: AudioStream = load(path)
				if stream:
					_ui_sound_cache[sound_id] = stream
				else:
					push_warning("AudioManager: Failed to load UI sound: %s" % path)
			else:
				push_warning("AudioManager: UI sound file not found: %s" % path)


## =============================================================================
## PUBLIC API - UI SOUNDS
## =============================================================================

## Play a UI sound effect (non-positional)
## sound_id: One of the UI_* constants (e.g., AudioManager.UI_CLICK)
func play_ui_sound(sound_id: String) -> void:
	if not _ui_sound_cache.has(sound_id):
		push_warning("AudioManager: Unknown UI sound: %s" % sound_id)
		return

	var stream_val: Variant = _ui_sound_cache[sound_id]
	if stream_val is AudioStream:
		var stream: AudioStream = stream_val
		_ui_player.stream = stream
		_ui_player.play()


## =============================================================================
## PUBLIC API - MUSIC
## =============================================================================

## Play background music with optional crossfade
## track_path: Path to the audio resource (empty string to stop)
## crossfade: Duration of crossfade in seconds (0 = immediate switch)
func play_music(track_path: String, crossfade: float = DEFAULT_CROSSFADE) -> void:
	## Skip if already playing this track
	if track_path == _current_music_path and not track_path.is_empty():
		return

	## Kill any existing crossfade
	if _crossfade_tween and _crossfade_tween.is_valid():
		_crossfade_tween.kill()
		_crossfade_tween = null

	## Handle empty path as stop request
	if track_path.is_empty():
		stop_music(crossfade)
		return

	## Load the new track
	if not ResourceLoader.exists(track_path):
		push_error("AudioManager: Music file not found: %s" % track_path)
		return

	var stream: AudioStream = load(track_path)
	if not stream:
		push_error("AudioManager: Failed to load music: %s" % track_path)
		return

	## If starting from stopped state, ensure both players are clean
	## (previous fade-out tween may have been interrupted)
	if _current_music_path.is_empty():
		_music_player_a.stop()
		_music_player_b.stop()

	_current_music_path = track_path

	## Get the inactive player for the new track
	var new_player: AudioStreamPlayer = _music_player_b if _active_player == _music_player_a else _music_player_a
	var old_player: AudioStreamPlayer = _active_player

	## Setup new player
	new_player.stream = stream
	## Start silent if crossfading, full volume otherwise
	new_player.volume_db = _linear_to_db(0.0) if crossfade > 0.0 and old_player.playing else 0.0
	new_player.play()

	if crossfade > 0.0 and old_player.playing:
		## Crossfade between players
		_crossfade_tween = create_tween()
		_crossfade_tween.set_parallel(true)

		## Fade out old
		_crossfade_tween.tween_method(
			_set_player_volume.bind(old_player),
			_db_to_linear(old_player.volume_db),
			0.0,
			crossfade
		)

		## Fade in new
		_crossfade_tween.tween_method(
			_set_player_volume.bind(new_player),
			0.0,
			1.0,
			crossfade
		)

		## Stop old player when done
		_crossfade_tween.chain().tween_callback(old_player.stop)
	else:
		## Immediate switch
		old_player.stop()
		new_player.volume_db = 0.0

	_active_player = new_player
	music_changed.emit(track_path)


## Stop current music with optional fade out
func stop_music(fade_duration: float = DEFAULT_FADE_OUT) -> void:
	if not _active_player.playing:
		_current_music_path = ""
		return

	## Kill any existing crossfade
	if _crossfade_tween and _crossfade_tween.is_valid():
		_crossfade_tween.kill()
		_crossfade_tween = null

	if fade_duration > 0.0:
		_crossfade_tween = create_tween()
		_crossfade_tween.tween_method(
			_set_player_volume.bind(_active_player),
			_db_to_linear(_active_player.volume_db),
			0.0,
			fade_duration
		)
		_crossfade_tween.tween_callback(_active_player.stop)
	else:
		_active_player.stop()

	_current_music_path = ""
	music_changed.emit("")


## Check if music is currently playing
func is_music_playing() -> bool:
	return _active_player.playing


## Get current music track path
func get_current_music() -> String:
	return _current_music_path


## =============================================================================
## PUBLIC API - VOLUME
## =============================================================================

## Set volume for a bus (0.0 to 1.0)
## Automatically persists to ProfileRepo settings
func set_volume(bus_name: String, volume: float) -> void:
	volume = clampf(volume, 0.0, 1.0)

	## Apply to AudioServer
	var bus_idx: int = AudioServer.get_bus_index(bus_name)
	if bus_idx >= 0:
		AudioServer.set_bus_volume_db(bus_idx, _linear_to_db(volume))
	else:
		push_warning("AudioManager: Unknown bus '%s'" % bus_name)
		return

	## Persist to settings
	var setting_key: String = _bus_to_setting_key(bus_name)
	if not setting_key.is_empty():
		ProfileRepo.update_settings({setting_key: volume})

	volume_changed.emit(bus_name, volume)


## Get current volume for a bus (0.0 to 1.0)
func get_volume(bus_name: String) -> float:
	var bus_idx: int = AudioServer.get_bus_index(bus_name)
	if bus_idx >= 0:
		return _db_to_linear(AudioServer.get_bus_volume_db(bus_idx))
	return 1.0


## Format volume as percentage string (e.g., "75%")
static func format_volume_percent(volume: float) -> String:
	return "%d%%" % int(volume * 100)


## Apply volume from ProfileRepo settings to audio buses
func _apply_settings_volume() -> void:
	var settings: Dictionary = ProfileRepo.get_settings()

	var music_vol_val: Variant = settings.get("music_volume", 1.0)
	var music_vol: float = ProfileRepo.safe_float(music_vol_val, 1.0)

	var sfx_vol_val: Variant = settings.get("sfx_volume", 1.0)
	var sfx_vol: float = ProfileRepo.safe_float(sfx_vol_val, 1.0)

	## Apply without persisting (already in settings)
	if _music_bus_idx >= 0:
		AudioServer.set_bus_volume_db(_music_bus_idx, _linear_to_db(music_vol))
	if _sfx_bus_idx >= 0:
		AudioServer.set_bus_volume_db(_sfx_bus_idx, _linear_to_db(sfx_vol))


## =============================================================================
## INTERNAL HELPERS
## =============================================================================

## Convert linear volume (0.0-1.0) to decibels
func _linear_to_db(linear: float) -> float:
	if linear <= 0.0:
		return -80.0  # Effectively muted
	return 20.0 * log(linear) / log(10.0)


## Convert decibels to linear volume (0.0-1.0)
func _db_to_linear(db: float) -> float:
	if db <= -80.0:
		return 0.0
	return pow(10.0, db / 20.0)


## Helper for tween - sets player volume from linear value
func _set_player_volume(linear: float, player: AudioStreamPlayer) -> void:
	player.volume_db = _linear_to_db(linear)


## Map bus name to ProfileRepo setting key
func _bus_to_setting_key(bus_name: String) -> String:
	match bus_name:
		BUS_MUSIC:
			return "music_volume"
		BUS_SFX:
			return "sfx_volume"
		_:
			return ""
