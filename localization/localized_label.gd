extends Label
class_name LocalizedLabel

## LocalizedLabel - Label that automatically translates text
##
## Set localization_key in the editor, and the label will:
## - Automatically translate on _ready()
## - Update when language changes
## - Support parameter substitution via set_params()
##
## Usage in editor:
##   1. Add LocalizedLabel to scene
##   2. Set "Localization Key" to "menu.play"
##   3. Done! Text updates automatically
##
## Dynamic parameters:
##   var label: LocalizedLabel = %DamageLabel
##   label.set_params({"amount": 5})  # Updates to "Deal 5 damage"

@export var localization_key: String = "":
	set(value):
		localization_key = value
		_update_text()

var _params: Dictionary = {}


func _ready() -> void:
	# Connect to language change signal
	if Loc:
		Loc.language_changed.connect(_on_language_changed)

	# Initial translation
	_update_text()


## Set parameter values for dynamic text
##
## Example:
##   label.localization_key = "battle.damage"
##   label.set_params({"amount": 10})
##   # → "Deal 10 damage"
func set_params(params: Dictionary) -> void:
	_params = params
	_update_text()


## Update parameter value without replacing entire dict
func set_param(key: String, value: Variant) -> void:
	_params[key] = value
	_update_text()


## Clear all parameters
func clear_params() -> void:
	_params.clear()
	_update_text()


func _on_language_changed(_new_locale: String) -> void:
	_update_text()


func _update_text() -> void:
	if localization_key == "":
		return

	if not Loc:
		push_warning("LocalizedLabel: Loc autoload not found")
		text = "[NO LOC]"
		return

	text = Loc.t(localization_key, _params)
