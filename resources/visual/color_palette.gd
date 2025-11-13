extends Resource
class_name GameColorPalette

## GameColorPalette - Centralized color definitions for consistent visuals
##
## Defines color scheme inspired by Cult of the Lamb's muted base + vibrant accents
## approach, adapted for a summoner/tower defense aesthetic.

## =============================================================================
## ENVIRONMENT COLORS (Muted Earth Tones)
## =============================================================================

## Background/Sky colors - bright and heroic
const SKY_TOP: Color = Color(0.53, 0.81, 0.98, 1.0)      # Light sky blue
const SKY_MIDDLE: Color = Color(0.39, 0.58, 0.93, 1.0)   # Azure blue
const SKY_HORIZON: Color = Color(0.95, 0.85, 0.75, 1.0)  # Warm peachy horizon

## Ground colors - vibrant grass
const GRASS_BRIGHT: Color = Color(0.45, 0.75, 0.35, 1.0)  # Vibrant green
const GRASS_MID: Color = Color(0.38, 0.65, 0.30, 1.0)     # Mid green
const GRASS_DARK: Color = Color(0.30, 0.55, 0.25, 1.0)    # Shadow green

## =============================================================================
## TERRITORY/ZONE COLORS
## =============================================================================

## Player territory - warm, welcoming tones
const PLAYER_ZONE_PRIMARY: Color = Color("#d4a574")    # Warm gold
const PLAYER_ZONE_SECONDARY: Color = Color("#8b6f47")  # Deep bronze
const PLAYER_ZONE_ACCENT: Color = Color("#f5c75c")     # Bright gold highlight

## Enemy territory - cool, ominous tones
const ENEMY_ZONE_PRIMARY: Color = Color("#5a7b8c")     # Steel blue
const ENEMY_ZONE_SECONDARY: Color = Color("#3d5368")   # Deep slate
const ENEMY_ZONE_ACCENT: Color = Color("#7a9bb0")      # Bright steel

## Neutral/midline - balanced gray tones
const NEUTRAL_DARK: Color = Color("#3a3a3a")
const NEUTRAL_MID: Color = Color("#6a6a6a")
const NEUTRAL_LIGHT: Color = Color("#9a9a9a")

## =============================================================================
## ELEMENT COLORS (High Saturation for Units/Effects)
## =============================================================================

## Fire element
const FIRE_PRIMARY: Color = Color("#ff7a2a")    # Bright orange (updated for card visuals)
const FIRE_SECONDARY: Color = Color("#ff6b4a")  # Light flame
const FIRE_DARK: Color = Color("#b83020")       # Deep ember

## Water element
const WATER_PRIMARY: Color = Color("#4a9eff")    # Bright blue
const WATER_SECONDARY: Color = Color("#6bb6ff")  # Light cyan
const WATER_DARK: Color = Color("#2d6bb8")       # Deep ocean

## Nature element
const NATURE_PRIMARY: Color = Color("#5fc75c")   # Bright green
const NATURE_SECONDARY: Color = Color("#7ed957")  # Light lime
const NATURE_DARK: Color = Color("#3d8a3a")      # Deep forest

## Storm/Lightning element
const STORM_PRIMARY: Color = Color("#a78bff")     # Bright purple
const STORM_SECONDARY: Color = Color("#c4a3ff")   # Light lavender
const STORM_DARK: Color = Color("#7256cc")        # Deep violet

## Earth/Rock element
const EARTH_PRIMARY: Color = Color("#8A3324")    # Dark reddish-brown (updated for card visuals)
const EARTH_SECONDARY: Color = Color("#d9a574")  # Light tan
const EARTH_DARK: Color = Color("#8b5a2b")       # Deep clay

## =============================================================================
## UI COLORS
## =============================================================================

## Backgrounds and panels
const UI_BG_DARK: Color = Color("#15151a")      # Very dark blue-gray
const UI_BG_MID: Color = Color("#252530")       # Mid dark
const UI_BG_LIGHT: Color = Color("#35353f")     # Lighter panel

## Text colors
const TEXT_PRIMARY: Color = Color("#f0f0f0")    # Almost white
const TEXT_SECONDARY: Color = Color("#b0b0b0")  # Gray
const TEXT_DISABLED: Color = Color("#606060")   # Dark gray
const TEXT_HIGHLIGHT: Color = Color("#f5c75c")  # Gold accent

## Button states
const BUTTON_NORMAL: Color = Color("#3a3a4a")
const BUTTON_HOVER: Color = Color("#4a4a5a")
const BUTTON_PRESSED: Color = Color("#5a5a6a")
const BUTTON_DISABLED: Color = Color("#2a2a34")

## Health/Resource colors
const HP_FULL: Color = Color("#5fc75c")      # Green
const HP_MID: Color = Color("#f5c75c")       # Yellow
const HP_LOW: Color = Color("#e84a3f")       # Red
const MANA_COLOR: Color = Color("#4a9eff")   # Blue
const SHIELD_COLOR: Color = Color("#9a9aaa") # Gray-blue

## =============================================================================
## UTILITY COLORS
## =============================================================================

## Rarity colors (for cards/units)
const RARITY_COMMON: Color = Color("#b0b0b0")     # Gray
const RARITY_RARE: Color = Color("#4a9eff")       # Blue
const RARITY_EPIC: Color = Color("#a78bff")       # Purple
const RARITY_LEGENDARY: Color = Color("#f5c75c")  # Gold

## Status/feedback colors
const SUCCESS: Color = Color("#5fc75c")    # Green
const WARNING: Color = Color("#f5c75c")    # Yellow
const ERROR: Color = Color("#e84a3f")      # Red
const INFO: Color = Color("#4a9eff")       # Blue

## Semi-transparent overlays
const OVERLAY_DARK: Color = Color(0.1, 0.1, 0.15, 0.7)
const OVERLAY_LIGHT: Color = Color(0.9, 0.9, 0.95, 0.3)

## =============================================================================
## HELPER FUNCTIONS
## =============================================================================

## Get element color by name
static func get_element_color(element_name: String) -> Color:
	match element_name.to_lower():
		"fire": return FIRE_PRIMARY
		"water": return WATER_PRIMARY
		"nature": return NATURE_PRIMARY
		"storm", "lightning": return STORM_PRIMARY
		"earth", "rock": return EARTH_PRIMARY
		_: return NEUTRAL_MID

## Get rarity color by name
static func get_rarity_color(rarity: String) -> Color:
	match rarity.to_lower():
		"common": return RARITY_COMMON
		"rare": return RARITY_RARE
		"epic": return RARITY_EPIC
		"legendary": return RARITY_LEGENDARY
		_: return RARITY_COMMON

## Get health color based on percentage
static func get_health_color(hp_percent: float) -> Color:
	if hp_percent > 0.6:
		return HP_FULL
	elif hp_percent > 0.3:
		return HP_MID
	else:
		return HP_LOW

## Create semi-transparent version of color
static func with_alpha(color: Color, alpha: float) -> Color:
	var result = color
	result.a = alpha
	return result
