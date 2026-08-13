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

## Player UI surfaces - warm neutral placeholder theme
const UI_BACKGROUND: Color = Color("#e8e6e1")
const UI_SURFACE: Color = Color("#f2f0ec")
const UI_SURFACE_RAISED: Color = Color("#faf8f4")
const UI_SURFACE_ALT: Color = Color("#dedbd4")
const UI_SURFACE_DISABLED: Color = Color("#d2cfc8")
const UI_BORDER: Color = Color("#9c978d")
const UI_BORDER_STRONG: Color = Color("#6f6a62")

## Compatibility names for card/gameplay surfaces that still expect depth steps.
const UI_BG_DARK: Color = UI_SURFACE_ALT
const UI_BG_MID: Color = UI_SURFACE
const UI_BG_LIGHT: Color = UI_SURFACE_RAISED

## Text colors
const TEXT_PRIMARY: Color = Color("#25231f")
const TEXT_SECONDARY: Color = Color("#59554e")
const TEXT_DISABLED: Color = Color("#8c877f")
const TEXT_HIGHLIGHT: Color = Color("#8a6420")

## Button states (generic fallback)
const BUTTON_NORMAL: Color = Color("#e1ded7")
const BUTTON_HOVER: Color = Color("#f3f0ea")
const BUTTON_PRESSED: Color = Color("#cbc7be")
const BUTTON_DISABLED: Color = UI_SURFACE_DISABLED

## Primary button (gold accent - main actions)
const BUTTON_PRIMARY_BG: Color = Color("#eee6d5")
const BUTTON_PRIMARY_BG_HOVER: Color = Color("#f7f0df")
const BUTTON_PRIMARY_BG_PRESSED: Color = Color("#d8cdb7")
const BUTTON_PRIMARY_BORDER: Color = Color("#9a742d")

## Secondary button (neutral - cancel, back)
const BUTTON_SECONDARY_BG: Color = BUTTON_NORMAL
const BUTTON_SECONDARY_BG_HOVER: Color = BUTTON_HOVER
const BUTTON_SECONDARY_BG_PRESSED: Color = BUTTON_PRESSED
const BUTTON_SECONDARY_BORDER: Color = UI_BORDER

## Danger button (red accent - delete, quit)
const BUTTON_DANGER_BG: Color = Color("#f0dcda")
const BUTTON_DANGER_BG_HOVER: Color = Color("#f7e7e5")
const BUTTON_DANGER_BG_PRESSED: Color = Color("#ddc1be")
const BUTTON_DANGER_BORDER: Color = Color("#b5483f")

## Button shadow (solid for raised 3D effect)
const BUTTON_SHADOW: Color = Color(0.25, 0.23, 0.20, 0.28)

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

## Currency colors
const GOLD: Color = Color(1.0, 0.85, 0.2)  # Bright gold for currency displays

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
	match rarity:
		RarityIDs.COMMON: return RARITY_COMMON
		RarityIDs.RARE: return RARITY_RARE
		RarityIDs.EPIC: return RARITY_EPIC
		RarityIDs.LEGENDARY: return RARITY_LEGENDARY
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
	var result: Color = color
	result.a = alpha
	return result
