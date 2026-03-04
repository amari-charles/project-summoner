extends Control
class_name CampaignSelectorModal

## CampaignSelectorModal - Modal for selecting which campaign to play
##
## Displays a list of available campaigns with unlock status indicators.
## Allows switching between campaigns.

## Signals
signal campaign_selected(campaign_id: String)
signal closed()

## UI Node References
@onready var background: ColorRect = %Background
@onready var border: NinePatchRect = %Border
@onready var title_label: Label = %TitleLabel
@onready var campaign_list: VBoxContainer = %CampaignList
@onready var close_button: TextureButton = %CloseButton

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Set up border using factory
	ButtonStyleFactory.apply_panel_border(border)

	# Set localized text
	title_label.text = Loc.t("campaign.selector.title")

	# Connect buttons
	close_button.pressed.connect(_on_close_pressed)
	background.gui_input.connect(_on_background_input)

## =============================================================================
## PUBLIC API
## =============================================================================

func open() -> void:
	_refresh_campaign_list()
	show()

## =============================================================================
## DISPLAY
## =============================================================================

func _refresh_campaign_list() -> void:
	# Clear existing items
	for child: Node in campaign_list.get_children():
		child.queue_free()

	var campaigns: Array = Campaign.GetAllCampaigns()
	var current_id: String = Campaign.GetCurrentCampaignId()

	for campaign_variant: Variant in campaigns:
		if not campaign_variant is Dictionary:
			continue
		var campaign: Dictionary = campaign_variant

		var item: Control = _create_campaign_item(campaign, current_id)
		campaign_list.add_child(item)

func _create_campaign_item(campaign: Dictionary, current_campaign_id: String) -> Control:
	var campaign_id: String = campaign.get("campaign_id", "")
	var name_key: String = campaign.get("name_key", "")
	var desc_key: String = campaign.get("description_key", "")
	var is_unlocked: bool = campaign.get("is_unlocked", false)
	var is_current: bool = (campaign_id == current_campaign_id)

	# Container for the item
	var panel: PanelContainer = PanelContainer.new()

	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 15)
	margin.add_theme_constant_override("margin_top", 12)
	margin.add_theme_constant_override("margin_right", 15)
	margin.add_theme_constant_override("margin_bottom", 12)
	panel.add_child(margin)

	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 8)
	margin.add_child(vbox)

	# Campaign name with status indicator
	var name_hbox: HBoxContainer = HBoxContainer.new()
	vbox.add_child(name_hbox)

	var name_label: Label = Label.new()
	name_label.text = Loc.t(name_key) if not name_key.is_empty() else campaign_id
	name_label.add_theme_font_size_override("font_size", 24)
	name_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	name_hbox.add_child(name_label)

	# Status indicator
	var status_label: Label = Label.new()
	status_label.add_theme_font_size_override("font_size", 20)
	if not is_unlocked:
		status_label.text = "[" + Loc.t("campaign.selector.locked") + "]"
		status_label.add_theme_color_override("font_color", Color(0.6, 0.6, 0.6))
	elif is_current:
		status_label.text = "[" + Loc.t("campaign.selector.current") + "]"
		status_label.add_theme_color_override("font_color", Color(0.3, 1.0, 0.3))
	name_hbox.add_child(status_label)

	# Description
	var desc_label: Label = Label.new()
	desc_label.text = Loc.t(desc_key) if not desc_key.is_empty() else ""
	desc_label.add_theme_font_size_override("font_size", 16)
	desc_label.add_theme_color_override("font_color", Color(0.7, 0.7, 0.7))
	desc_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	vbox.add_child(desc_label)

	# Make clickable if unlocked and not current
	if is_unlocked and not is_current:
		var button: Button = Button.new()
		button.flat = true
		button.anchor_right = 1.0
		button.anchor_bottom = 1.0
		button.pressed.connect(_on_campaign_item_pressed.bind(campaign_id))
		panel.add_child(button)

	return panel

## =============================================================================
## SIGNAL HANDLERS
## =============================================================================

func _on_campaign_item_pressed(campaign_id: String) -> void:
	campaign_selected.emit(campaign_id)

func _on_close_pressed() -> void:
	closed.emit()
	hide()

func _on_background_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event as InputEventMouseButton
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			_on_close_pressed()
	elif event is InputEventScreenTouch:
		var touch_event: InputEventScreenTouch = event as InputEventScreenTouch
		if touch_event.pressed:
			_on_close_pressed()

func _input(event: InputEvent) -> void:
	if not visible:
		return

	# Close on Escape key
	if event is InputEventKey:
		var key_event: InputEventKey = event as InputEventKey
		if key_event.pressed and key_event.keycode == KEY_ESCAPE:
			_on_close_pressed()
			get_viewport().set_input_as_handled()
