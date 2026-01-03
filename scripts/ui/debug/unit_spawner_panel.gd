extends PanelContainer
class_name UnitSpawnerPanel

## UnitSpawnerPanel - Debug panel for spawning units via drag-and-drop
##
## Shows a list of all available units that can be dragged onto the
## battlefield. Includes a team toggle and clear all button.

## Spawn as enemy by default
@export var spawn_as_enemy: bool = true

## Signal emitted when clear button is pressed
signal clear_requested()

var _unit_buttons: Array[Control] = []


func _ready() -> void:
	_build_ui()


func _build_ui() -> void:
	# Apply panel styling
	add_theme_stylebox_override("panel", _create_panel_style())

	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 12)
	margin.add_theme_constant_override("margin_right", 12)
	margin.add_theme_constant_override("margin_top", 12)
	margin.add_theme_constant_override("margin_bottom", 12)
	add_child(margin)

	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 8)
	margin.add_child(vbox)

	# Title
	var title: Label = Label.new()
	title.text = Loc.t("debug.spawner.title")
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 16)
	title.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	vbox.add_child(title)

	# Team toggle
	var team_toggle: CheckButton = CheckButton.new()
	team_toggle.text = Loc.t("debug.spawner.spawn_as_enemy")
	team_toggle.button_pressed = spawn_as_enemy
	team_toggle.toggled.connect(func(pressed: bool) -> void: spawn_as_enemy = pressed)
	team_toggle.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	vbox.add_child(team_toggle)

	# Separator
	vbox.add_child(HSeparator.new())

	# Scroll container for unit list
	var scroll: ScrollContainer = ScrollContainer.new()
	scroll.custom_minimum_size = Vector2(160, 300)
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	vbox.add_child(scroll)

	var unit_list: VBoxContainer = VBoxContainer.new()
	unit_list.add_theme_constant_override("separation", 4)
	unit_list.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(unit_list)

	# Populate with summon cards from catalog
	_populate_unit_list(unit_list)

	# Separator
	vbox.add_child(HSeparator.new())

	# Clear All Button
	var clear_btn: StyledButton = StyledButton.new()
	clear_btn.text = Loc.t("debug.spawner.clear_all")
	clear_btn.variant = StyledButton.Variant.DANGER
	clear_btn.pressed.connect(_on_clear_pressed)
	vbox.add_child(clear_btn)


func _populate_unit_list(container: VBoxContainer) -> void:
	# Get all cards from catalog
	var all_cards: Array = CardCatalog.get_all_cards()

	for card_def: Dictionary in all_cards:
		# Only include summon cards
		if card_def.get("card_type") != Card.CardType.SUMMON:
			continue

		var btn: SpawnableUnitButton = SpawnableUnitButton.new()
		btn.catalog_id = card_def.get("catalog_id", "")
		btn.unit_name = card_def.get("card_name", "Unknown")
		btn.panel = self
		btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		container.add_child(btn)
		_unit_buttons.append(btn)


func get_spawn_team() -> int:
	return 1 if spawn_as_enemy else 0  # 1 = Enemy, 0 = Player


func _on_clear_pressed() -> void:
	clear_requested.emit()


func _create_panel_style() -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.1, 0.1, 0.15, 0.95)
	style.border_color = GameColorPalette.BUTTON_SECONDARY_BORDER
	style.set_border_width_all(2)
	style.set_corner_radius_all(8)
	return style
