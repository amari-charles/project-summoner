extends Control
class_name CardDetailModal

## Card Detail Modal - Popup for viewing card details and progression
##
## Opens as an overlay when clicking a card in the collection screen.
## Shows enlarged card visual on left, info panel on right.
## Includes progression info and level-up button.

## Signals
signal closed()
signal level_up_requested(instance_id: String)

## UI Node References
@onready var background: ColorRect = %Background
@onready var card_visual: CardVisual = %CardVisual
@onready var card_name_label: Label = %CardNameLabel
@onready var rarity_label: Label = %RarityLabel
@onready var type_label: Label = %TypeLabel
@onready var cost_label: Label = %CostLabel
@onready var description_label: Label = %DescriptionLabel
@onready var level_label: Label = %LevelLabel
@onready var xp_label: Label = %XPLabel
@onready var xp_progress_bar: ProgressBar = %XPProgressBar
@onready var level_up_button: Button = %LevelUpButton
@onready var close_button: Button = %CloseButton
@onready var upgrades_section: VBoxContainer = %UpgradesSection
@onready var upgrades_header: Label = %UpgradesHeader
@onready var upgrades_container: VBoxContainer = %UpgradesContainer
@onready var stats_section: VBoxContainer = %StatsSection
@onready var stats_header: Label = %StatsHeader
@onready var stats_container: GridContainer = %StatsContainer

## State
var card_instance_id: String = ""
var card_catalog_id: String = ""

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Connect buttons
	close_button.pressed.connect(_close)
	level_up_button.pressed.connect(_on_level_up_pressed)

	# Connect background click to close
	background.gui_input.connect(_on_background_input)

## =============================================================================
## PUBLIC API
## =============================================================================

## Open the modal for a specific card instance
func open_for_card(instance_id: String, catalog_id: String) -> void:
	card_instance_id = instance_id
	card_catalog_id = catalog_id

	_load_card_data()
	_update_stats_display()
	_update_progression_display()
	_update_upgrades_display()

	show()

## =============================================================================
## DATA LOADING
## =============================================================================

func _load_card_data() -> void:
	var catalog: Node = get_node_or_null("/root/CardCatalog")
	if not catalog or not catalog.has_method("get_card"):
		push_error("CardDetailModal: CardCatalog not found")
		return

	var catalog_data_result: Variant = catalog.call("get_card", card_catalog_id)
	if not catalog_data_result is Dictionary:
		push_error("CardDetailModal: Failed to get catalog data for %s" % card_catalog_id)
		return
	var catalog_data: Dictionary = catalog_data_result

	if catalog_data.is_empty():
		return

	# Update card visual
	card_visual.set_card_data(catalog_data, false)

	# Update info labels
	var card_name_val: Variant = catalog_data.get("card_name", "Unknown")
	card_name_label.text = card_name_val if card_name_val is String else "Unknown"

	var rarity_val: StringName = catalog_data.get("rarity", RarityIDs.COMMON)
	rarity_label.text = Loc.t("ui.collection.rarity_label", {"rarity": String(rarity_val).capitalize()})

	var card_type_val: Variant = catalog_data.get("card_type", Card.CardType.SUMMON)
	var card_type: int = int(card_type_val)
	var type_str: String = Loc.t("ui.collection.type_summon") if card_type == Card.CardType.SUMMON else Loc.t("ui.collection.type_spell")
	type_label.text = Loc.t("ui.collection.type_label", {"type": type_str})

	var mana_cost_val: Variant = catalog_data.get("mana_cost", 0)
	var mana_cost: int = mana_cost_val if mana_cost_val is int else 0
	cost_label.text = Loc.t("ui.collection.cost_label", {"cost": mana_cost})

	var description_val: Variant = catalog_data.get("description", "")
	var description: String = description_val if description_val is String else ""
	if description.is_empty():
		description = Loc.t("ui.collection.no_description")
	description_label.text = description

## =============================================================================
## STATS DISPLAY
## =============================================================================

func _update_stats_display() -> void:
	# Clear existing stat labels
	for child: Node in stats_container.get_children():
		child.queue_free()

	var effective_stats: Dictionary = _get_effective_stats()
	if effective_stats.is_empty():
		stats_section.visible = false
		return

	# Update header with localization
	stats_header.text = Loc.t("ui.collection.stats_header")

	# Determine card type to show appropriate stats
	var card_type_val: Variant = effective_stats.get("card_type", Card.CardType.SUMMON)
	var card_type: int = int(card_type_val)

	if card_type == Card.CardType.SUMMON:
		# Show summon stats: HP, Damage, Speed, Attack Speed
		_add_stat_label("stat_hp", effective_stats.get("max_hp", 0))
		_add_stat_label("stat_damage", effective_stats.get("attack_damage", 0))
		_add_stat_label("stat_move_speed", effective_stats.get("move_speed", 0))
		_add_stat_label("stat_attack_speed", effective_stats.get("attack_speed", 0))
	else:
		# Show spell stats: Spell Damage, Spell Radius (if applicable)
		var spell_damage: Variant = effective_stats.get("spell_damage", null)
		var spell_radius: Variant = effective_stats.get("spell_radius", null)

		if spell_damage != null:
			_add_stat_label("stat_spell_damage", spell_damage)
		if spell_radius != null:
			_add_stat_label("stat_spell_radius", spell_radius)

		# Hide section if no spell stats to show
		if spell_damage == null and spell_radius == null:
			stats_section.visible = false
			return

	stats_section.visible = true


func _get_effective_stats() -> Dictionary:
	var catalog: Node = get_node_or_null("/root/CardCatalog")
	if not catalog or not catalog.has_method("get_card"):
		return {}

	var base_stats_result: Variant = catalog.call("get_card", card_catalog_id)
	if not base_stats_result is Dictionary:
		return {}
	var base_stats: Dictionary = (base_stats_result as Dictionary).duplicate(true)

	if base_stats.is_empty():
		return {}

	# If no instance ID, return base stats
	if card_instance_id.is_empty():
		return base_stats

	# Get upgrade modifiers and apply them
	var progression: Node = get_node_or_null("/root/CardProgression")
	if not progression or not progression.has_method("get_upgrade_stat_modifiers"):
		return base_stats

	var modifiers_result: Variant = progression.call("get_upgrade_stat_modifiers", card_instance_id)
	if not modifiers_result is Dictionary:
		return base_stats
	var modifiers: Dictionary = modifiers_result

	# Apply modifiers multiplicatively
	for stat_key: Variant in modifiers:
		if stat_key is String and base_stats.has(stat_key):
			var base_val: Variant = base_stats[stat_key]
			var multiplier: float = modifiers[stat_key]
			if base_val is float:
				base_stats[stat_key] = base_val * multiplier
			elif base_val is int:
				base_stats[stat_key] = int(float(base_val) * multiplier)

	return base_stats


func _add_stat_label(loc_key: String, value: Variant) -> void:
	var label: Label = Label.new()
	var stat_name: String = Loc.t("ui.collection." + loc_key)

	# Format value based on type
	var value_str: String
	if value is float:
		# Show one decimal place for floats, but hide if whole number
		if abs(value - round(value)) < 0.01:
			value_str = str(int(round(value)))
		else:
			value_str = "%.1f" % value
	else:
		value_str = str(value)

	label.text = "%s: %s" % [stat_name, value_str]
	label.add_theme_font_size_override("font_size", 16)
	label.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	stats_container.add_child(label)

## =============================================================================
## PROGRESSION DISPLAY
## =============================================================================

func _update_progression_display() -> void:
	if card_instance_id.is_empty():
		_hide_progression()
		return

	var progression_node: Node = get_node_or_null("/root/CardProgression")
	if not progression_node:
		push_warning("CardDetailModal: CardProgression service not found")
		_hide_progression()
		return

	var info: Dictionary = progression_node.call("get_card_progression_info", card_instance_id)
	if info.is_empty():
		_hide_progression()
		return

	var level: int = info.get("level", 1)
	var max_level: int = info.get("max_level", 10)
	var current_xp: int = info.get("xp", 0)
	var xp_for_next: int = info.get("xp_for_next_level", 0)
	var xp_progress: float = info.get("xp_progress", 0.0)
	var can_level_up_val: bool = info.get("can_level_up", false)
	var can_afford_val: bool = info.get("can_afford_level_up", false)
	var gold_cost: int = info.get("level_up_gold_cost", 0)
	var is_max_level: bool = info.get("is_max_level", false)

	# Update level label
	level_label.text = Loc.t("ui.collection.level_label", {"level": level, "max": max_level})

	# Update XP display
	if is_max_level:
		xp_label.text = Loc.t("ui.collection.xp_max_level")
		xp_progress_bar.value = 100.0
	else:
		xp_label.text = Loc.t("ui.collection.xp_label", {"current": current_xp, "required": xp_for_next})
		xp_progress_bar.value = xp_progress * 100.0

	# Update level-up button
	if is_max_level:
		level_up_button.visible = false
	elif can_level_up_val:
		level_up_button.visible = true
		if can_afford_val:
			level_up_button.text = Loc.t("ui.collection.level_up_button", {"cost": gold_cost})
			level_up_button.disabled = false
		else:
			level_up_button.text = Loc.t("ui.collection.level_up_button_unaffordable", {"cost": gold_cost})
			level_up_button.disabled = true
	else:
		level_up_button.visible = true
		level_up_button.text = Loc.t("ui.collection.level_up_button_locked")
		level_up_button.disabled = true

func _hide_progression() -> void:
	level_label.text = ""
	xp_label.text = ""
	xp_progress_bar.value = 0
	level_up_button.visible = false

## =============================================================================
## UPGRADES DISPLAY
## =============================================================================

func _update_upgrades_display() -> void:
	# Clear existing upgrade boxes
	for child: Node in upgrades_container.get_children():
		child.queue_free()

	if card_instance_id.is_empty() or card_catalog_id.is_empty():
		upgrades_section.visible = false
		return

	var progression_node: Node = get_node_or_null("/root/CardProgression")
	if not progression_node or not progression_node.has_method("get_applied_upgrades"):
		upgrades_section.visible = false
		return

	var upgrade_ids: Array = progression_node.call("get_applied_upgrades", card_instance_id)
	if upgrade_ids.is_empty():
		upgrades_section.visible = false
		return

	# Update header with localization
	upgrades_header.text = Loc.t("ui.collection.upgrades_header")

	# Create a box for each applied upgrade
	for upgrade_id: Variant in upgrade_ids:
		if not upgrade_id is String:
			continue

		var upgrade_data: Dictionary = CardUpgradeCatalog.get_upgrade(card_catalog_id, upgrade_id)
		if upgrade_data.is_empty():
			continue

		var box: PanelContainer = _create_upgrade_box(upgrade_data)
		upgrades_container.add_child(box)

	upgrades_section.visible = true


func _create_upgrade_box(upgrade: Dictionary) -> PanelContainer:
	var box: PanelContainer = PanelContainer.new()

	# Style the box
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = GameColorPalette.with_alpha(GameColorPalette.UI_BG_DARK, 0.8)
	style.border_width_left = 2
	style.border_width_top = 2
	style.border_width_right = 2
	style.border_width_bottom = 2
	style.border_color = GameColorPalette.UI_BG_LIGHT
	style.set_corner_radius_all(4)
	box.add_theme_stylebox_override("panel", style)

	# Add content
	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 10)
	margin.add_theme_constant_override("margin_top", 6)
	margin.add_theme_constant_override("margin_right", 10)
	margin.add_theme_constant_override("margin_bottom", 6)
	box.add_child(margin)

	var vbox: VBoxContainer = VBoxContainer.new()
	margin.add_child(vbox)

	# Name label
	var name_label: Label = Label.new()
	name_label.text = upgrade.get("name", "Unknown")
	name_label.add_theme_font_size_override("font_size", 16)
	name_label.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	vbox.add_child(name_label)

	# Description label
	var desc_label: Label = Label.new()
	desc_label.text = upgrade.get("description", "")
	desc_label.add_theme_font_size_override("font_size", 14)
	desc_label.add_theme_color_override("font_color", GameColorPalette.SUCCESS)
	vbox.add_child(desc_label)

	return box

## =============================================================================
## EVENT HANDLERS
## =============================================================================

func _on_level_up_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.UI_CLICK)
	if card_instance_id.is_empty():
		return

	level_up_requested.emit(card_instance_id)
	_close()

func _on_background_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			_close()

func _close() -> void:
	AudioManager.play_ui_sound(AudioManager.UI_CLICK)
	closed.emit()
	hide()
	queue_free()
