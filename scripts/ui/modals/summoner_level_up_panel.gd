extends Control
class_name SummonerLevelUpPanel

## Summoner Level-Up Panel - Modal for leveling up a summoner
##
## Opens as an overlay after battle when summoner has enough XP.
## Shows stat gains and handles level-up confirmation.

## UI Node References
@onready var background: ColorRect = %Background
@onready var title_label: Label = %TitleLabel
@onready var summoner_name_label: Label = %SummonerNameLabel
@onready var level_transition_label: Label = %LevelTransitionLabel
@onready var stat_preview_container: VBoxContainer = %StatPreviewContainer
@onready var confirm_button: Button = %ConfirmButton
@onready var cancel_button: Button = %CancelButton

## State
var summoner_id: String = ""
var current_level: int = 1

## Signals
signal level_up_completed(summoner_id: String)
signal cancelled()

## Constants
const ERROR_FEEDBACK_DURATION: float = 1.5  ## Duration to show error message before reset

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Set localized text for static UI elements
	title_label.text = Loc.t("ui.summoner_level_up.title")
	confirm_button.text = Loc.t("ui.summoner_level_up.confirm")
	cancel_button.text = Loc.t("ui.summoner_level_up.cancel")

	# Connect buttons
	confirm_button.pressed.connect(_on_confirm_pressed)
	cancel_button.pressed.connect(_on_cancel_pressed)

	# Connect background click to close
	background.gui_input.connect(_on_background_input)

## =============================================================================
## PUBLIC API
## =============================================================================

## Open the panel for a specific summoner
func open_for_summoner(p_summoner_id: String) -> void:
	summoner_id = p_summoner_id

	_load_summoner_data()
	_populate_stat_preview()

	show()

## =============================================================================
## DATA LOADING
## =============================================================================

func _load_summoner_data() -> void:
	# Get summoner progression info
	var info: Dictionary = SummonerProgression.get_summoner_progression_info(summoner_id)
	if info.is_empty():
		push_error("SummonerLevelUpPanel: Failed to get progression info for %s" % summoner_id)
		return

	current_level = info.get("level", 1)
	var next_level: int = current_level + 1

	# Get summoner name from config
	var config: SummonerConfig = SummonerCatalog.get_summoner_config(summoner_id)
	var summoner_name: String = summoner_id
	if config:
		summoner_name = Loc.t(config.name_key)

	# Update UI
	summoner_name_label.text = summoner_name
	level_transition_label.text = Loc.t("ui.summoner_level_up.level_transition", {
		"current": current_level,
		"next": next_level
	})

func _populate_stat_preview() -> void:
	# Clear existing previews
	for child: Node in stat_preview_container.get_children():
		child.queue_free()

	# Get current config for base stats
	var config: SummonerConfig = SummonerCatalog.get_summoner_config(summoner_id)
	if not config:
		return

	# Calculate stat gains using shared constant from SummonerInstance
	var bonus_percent: float = SummonerInstance.LEVEL_STAT_BONUS_PERCENT
	var hp_gain: int = roundi(config.base_health * bonus_percent)
	var mana_gain: float = config.max_mana * bonus_percent

	# Create stat preview labels
	var hp_label: Label = Label.new()
	hp_label.text = Loc.t("ui.summoner_level_up.stat_hp_increase", {"amount": hp_gain})
	hp_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	hp_label.add_theme_font_size_override("font_size", 20)
	hp_label.add_theme_color_override("font_color", GameColorPalette.SUCCESS)
	stat_preview_container.add_child(hp_label)

	var mana_label: Label = Label.new()
	mana_label.text = Loc.t("ui.summoner_level_up.stat_mana_increase", {"amount": snapped(mana_gain, 0.1)})
	mana_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	mana_label.add_theme_font_size_override("font_size", 20)
	mana_label.add_theme_color_override("font_color", GameColorPalette.INFO)
	stat_preview_container.add_child(mana_label)

## =============================================================================
## EVENT HANDLERS
## =============================================================================

func _on_confirm_pressed() -> void:
	var success: bool = SummonerProgression.level_up_summoner(summoner_id)

	if success:
		level_up_completed.emit(summoner_id)
		_close()
	else:
		# Show error feedback to user
		confirm_button.text = Loc.t("ui.summoner_level_up.failed")
		confirm_button.add_theme_color_override("font_color", GameColorPalette.ERROR)
		# Re-enable after brief delay so user can try again
		await get_tree().create_timer(ERROR_FEEDBACK_DURATION).timeout
		confirm_button.text = Loc.t("ui.summoner_level_up.confirm")
		confirm_button.remove_theme_color_override("font_color")

func _on_cancel_pressed() -> void:
	cancelled.emit()
	_close()

func _on_background_input(event: InputEvent) -> void:
	# Close on background click
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			cancelled.emit()
			_close()

func _close() -> void:
	hide()
	queue_free()
