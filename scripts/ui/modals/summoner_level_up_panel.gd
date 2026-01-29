extends Control
class_name SummonerLevelUpPanel

## Summoner Level-Up Panel - Modal for selecting trait when leveling up a summoner
##
## Opens as an overlay after battle when summoner has enough XP.
## Shows stat gains, trait choices, and handles level-up confirmation.

## UI Node References
@onready var background: ColorRect = %Background
@onready var title_label: Label = %TitleLabel
@onready var choose_trait_label: Label = %ChooseTraitLabel
@onready var summoner_name_label: Label = %SummonerNameLabel
@onready var level_transition_label: Label = %LevelTransitionLabel
@onready var stat_preview_container: VBoxContainer = %StatPreviewContainer
@onready var trait_container: HBoxContainer = %TraitContainer
@onready var confirm_button: Button = %ConfirmButton
@onready var cancel_button: Button = %CancelButton

## State
var summoner_id: String = ""
var selected_trait_id: String = ""
var trait_buttons: Array[Button] = []
var current_level: int = 1

## Signals
signal level_up_completed(summoner_id: String, trait_id: String)
signal cancelled()

## Constants
const ERROR_FEEDBACK_DURATION: float = 1.5  ## Duration to show error message before reset

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Set localized text for static UI elements
	title_label.text = Loc.t("ui.summoner_level_up.title")
	choose_trait_label.text = Loc.t("ui.summoner_level_up.choose_trait")
	confirm_button.text = Loc.t("ui.summoner_level_up.confirm")
	cancel_button.text = Loc.t("ui.summoner_level_up.cancel")

	# Connect buttons
	confirm_button.pressed.connect(_on_confirm_pressed)
	cancel_button.pressed.connect(_on_cancel_pressed)

	# Connect background click to close
	background.gui_input.connect(_on_background_input)

	# Initially disable confirm until trait selected
	confirm_button.disabled = true

## =============================================================================
## PUBLIC API
## =============================================================================

## Open the panel for a specific summoner
func open_for_summoner(p_summoner_id: String) -> void:
	summoner_id = p_summoner_id
	selected_trait_id = ""

	_load_summoner_data()
	_populate_stat_preview()
	_populate_trait_choices()
	_update_confirm_button()

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

func _populate_trait_choices() -> void:
	# Clear existing trait buttons
	for button: Button in trait_buttons:
		button.queue_free()
	trait_buttons.clear()

	# Get summoner's current traits to exclude
	var config: SummonerConfig = SummonerCatalog.get_summoner_config(summoner_id)
	var excluded_ids: Array[String] = []

	# Exclude innate traits
	if config:
		for trait_id: String in config.innate_trait_ids:
			excluded_ids.append(trait_id)

	# Exclude already acquired boons
	var summoner_data: Dictionary = ProfileRepo.get_summoner_instance(summoner_id)
	if not summoner_data.is_empty():
		var acquired: Variant = summoner_data.get("acquired_boon_ids", [])
		if acquired is Array:
			for boon_id: Variant in acquired:
				if boon_id is String:
					excluded_ids.append(boon_id)

	# Get trait pool
	var trait_pool: Array[Dictionary] = TraitCatalog.get_level_up_trait_pool(excluded_ids, 3)

	# Create trait buttons
	for trait_data: Dictionary in trait_pool:
		var button: Button = _create_trait_button(trait_data)
		trait_container.add_child(button)
		trait_buttons.append(button)

	# If no traits available (all acquired), show message
	if trait_buttons.is_empty():
		var label: Label = Label.new()
		label.text = Loc.t("ui.summoner_level_up.all_traits_acquired")
		label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		trait_container.add_child(label)
		# Allow level-up without trait selection
		selected_trait_id = "__none__"
		_update_confirm_button()

func _create_trait_button(trait_data: Dictionary) -> Button:
	var button: Button = Button.new()

	var trait_id: String = trait_data.get("id", "")
	var trait_name: String = TraitCatalog.get_trait_name(trait_id)
	var description: String = TraitCatalog.get_trait_description(trait_id)

	# Store trait_id as metadata for easy selection lookup
	button.set_meta("trait_id", trait_id)

	# Build button text with name and description
	button.text = "%s\n%s" % [trait_name, description]

	# Style
	button.custom_minimum_size = Vector2(180, 140)
	button.add_theme_font_size_override("font_size", 16)

	# Connect
	button.pressed.connect(_on_trait_selected.bind(trait_id))

	return button

func _update_confirm_button() -> void:
	var can_confirm: bool = not selected_trait_id.is_empty()
	confirm_button.disabled = not can_confirm

func _update_selection_visual() -> void:
	# Update button visuals to show selection using stored metadata
	for button: Button in trait_buttons:
		var button_trait_id: String = button.get_meta("trait_id", "")
		var is_selected: bool = button_trait_id == selected_trait_id

		if is_selected:
			button.add_theme_color_override("font_color", GameColorPalette.TEXT_HIGHLIGHT)
		else:
			button.remove_theme_color_override("font_color")

## =============================================================================
## EVENT HANDLERS
## =============================================================================

func _on_trait_selected(trait_id: String) -> void:
	selected_trait_id = trait_id
	_update_selection_visual()
	_update_confirm_button()

func _on_confirm_pressed() -> void:
	if selected_trait_id.is_empty():
		return

	var success: bool = false

	# Handle case where all traits are acquired
	if selected_trait_id == "__none__":
		success = SummonerProgression.level_up_summoner(summoner_id)
	else:
		success = SummonerProgression.level_up_summoner_with_trait(summoner_id, selected_trait_id)

	if success:
		level_up_completed.emit(summoner_id, selected_trait_id)
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
