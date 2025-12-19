# Audio System Architecture

## Overview

The audio system provides centralized control for music and sound effects through the `AudioManager` autoload. It handles audio bus management, volume persistence, and smooth transitions.

## Audio Bus Structure

```
Master
├── Music    (background music, crossfade support)
└── SFX      (sound effects, UI sounds, combat sounds)
```

Buses are created dynamically at startup if they don't exist in the project settings.

## Core Components

### AudioManager (`scripts/services/audio_manager.gd`)

Central autoload that manages all audio:

- **Music playback** with crossfade transitions
- **UI sound effects** (preloaded for instant playback)
- **Volume control** with automatic persistence to ProfileRepo
- **Process mode** set to `PROCESS_MODE_ALWAYS` for pause menu support

### Key Constants

```gdscript
# Bus names
AudioManager.BUS_MASTER
AudioManager.BUS_MUSIC
AudioManager.BUS_SFX

# Music tracks
AudioManager.MUSIC_BATTLE

# Sound effect IDs
AudioManager.SFX_UI_CLICK
AudioManager.SFX_CARD_DRAW
AudioManager.SFX_CARD_PLAY

# Timing
AudioManager.DEFAULT_CROSSFADE  # 1.0 seconds
AudioManager.DEFAULT_FADE_OUT   # 0.5 seconds
AudioManager.MUTE_DB            # -80.0 dB (effectively silent)
```

## Usage Examples

### Playing Music

```gdscript
# Start battle music with default crossfade
AudioManager.play_music(AudioManager.MUSIC_BATTLE)

# Stop music with fade out
AudioManager.stop_music()

# Immediate stop (no fade)
AudioManager.stop_music(0.0)

# Check if music is playing
if AudioManager.is_music_playing():
    pass
```

### Playing Sound Effects

```gdscript
# UI sounds (non-positional)
AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)

# For positional 3D sounds, use AudioStreamPlayer3D directly:
var audio_player: AudioStreamPlayer3D = AudioStreamPlayer3D.new()
audio_player.bus = AudioManager.BUS_SFX  # Route through SFX bus
```

### Volume Control

```gdscript
# Set volume (0.0 to 1.0, automatically persisted)
AudioManager.set_volume(AudioManager.BUS_MUSIC, 0.5)
AudioManager.set_volume(AudioManager.BUS_SFX, 0.8)

# Get current volume
var music_vol: float = AudioManager.get_volume(AudioManager.BUS_MUSIC)

# Format for display
var label_text: String = AudioManager.format_volume_percent(music_vol)  # "50%"
```

## Adding New Audio

### Adding a Music Track

1. Place the audio file in `resources/audio/bgm/`
2. Add a constant to AudioManager:
   ```gdscript
   const MUSIC_MENU: String = "res://resources/audio/bgm/menu.mp3"
   ```
3. Update `resources/audio/ATTRIBUTION.md` with source/license

### Adding a Sound Effect

1. Place the audio file in `resources/audio/sfx/`
2. Add constant and path to AudioManager:
   ```gdscript
   const SFX_VICTORY: String = "victory"

   const _SFX_SOUNDS: Dictionary = {
       # ... existing entries ...
       SFX_VICTORY: "res://resources/audio/sfx/victory.wav",
   }
   ```
3. Update `resources/audio/ATTRIBUTION.md` with source/license
4. Use via `AudioManager.play_ui_sound(AudioManager.SFX_VICTORY)`

## Volume Persistence

Volume settings are stored in the player's profile via ProfileRepo:

```json
{
  "settings": {
    "music_volume": 1.0,
    "sfx_volume": 1.0
  }
}
```

- Changes are automatically persisted when using `AudioManager.set_volume()`
- Settings are loaded on AudioManager initialization
- Linear volume (0.0-1.0) is converted to decibels internally

## File Structure

```
resources/audio/
├── ATTRIBUTION.md       # License/source documentation
├── bgm/
│   └── battle.mp3       # Battle background music
└── sfx/
    ├── ui_click.wav     # Button click
    ├── card_draw.mp3    # Card drawn to hand
    └── card_play.wav    # Card played from hand
```

## Integration Points

| Component | Audio Integration |
|-----------|-------------------|
| `GameController3D` | Starts/stops battle music |
| `PauseMenu` | Stops music on quit, settings panel |
| `SettingsScreen` | Volume sliders (out-of-battle) |
| `PauseSettingsPanel` | Volume sliders (in-battle) |
| `HandUI` | Card draw/play sounds |
| `VFXManager` | Routes combat SFX through SFX bus |
| `UnitAnimationController` | Routes unit SFX through SFX bus |
| Various UI scripts | UI click sounds on button presses |

## Technical Details

### Crossfade Implementation

AudioManager uses two `AudioStreamPlayer` nodes for seamless crossfades:
- Player A and Player B alternate as the active player
- When transitioning, the inactive player starts the new track silently
- A tween fades out the old player while fading in the new player
- The old player stops when the tween completes

### Volume Conversion

```gdscript
const MUTE_DB: float = -80.0  # Effectively silent

# Linear (0.0-1.0) to decibels
func _linear_to_db(linear: float) -> float:
    if linear <= 0.0:
        return MUTE_DB
    return 20.0 * log(linear) / log(10.0)

# Decibels to linear (0.0-1.0)
func _db_to_linear(db: float) -> float:
    if db <= MUTE_DB:
        return 0.0
    return pow(10.0, db / 20.0)
```
