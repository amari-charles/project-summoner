extends Control
class_name CardDetailModal

## Card Detail Modal - Popup for viewing card details and progression.
##
## Level-up is delegated to CardLevelUpPanel so trait selection remains coupled.

## Signals
signal closed()
signal level_up_requested(instance_id: String)
signal deck_action_requested(instance_id: String, action: String)  ## "add" or "remove"

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
@onready var trait_points_label: Label = %TraitPointsLabel
@onready var trait_offer_header: Label = %TraitOfferHeader
@onready var trait_offers_container: VBoxContainer = %TraitOffersContainer
@onready var apply_trait_button: Button = %ApplyTraitButton
@onready var progression_status_label: Label = %ProgressionStatusLabel
@onready var close_button: Button = %CloseButton
@onready var traits_section: VBoxContainer = %UpgradesSection
@onready var traits_header: Label = %UpgradesHeader
@onready var traits_container: VBoxContainer = %UpgradesContainer
@onready var stats_section: VBoxContainer = %StatsSection
@onready var stats_header: Label = %StatsHeader
@onready var stats_container: GridContainer = %StatsContainer
@onready var deck_action_button: Button = %DeckActionButton

## State
var card_instance_id: String = ""
var card_catalog_id: String = ""

## Deck context state
var current_deck_id: String = ""
var is_card_in_deck: bool = false

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	# Connect buttons
	close_button.pressed.connect(_close)
	level_up_button.pressed.connect(_on_level_up_pressed)
	deck_action_button.pressed.connect(_on_deck_action_pressed)

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
	_update_traits_display()
	_update_deck_action_button()
	_hide_inline_trait_offer_controls()

	show()

## Set deck context for the modal (call after open_for_card if deck actions needed)
func set_deck_context(deck_id: String, card_in_deck: bool) -> void:
	current_deck_id = deck_id
	is_card_in_deck = card_in_deck
	_update_deck_action_button()

## =============================================================================
## DATA LOADING
## =============================================================================

func _load_card_data() -> void:
	var catalog_data: Dictionary = CardCatalogApi.get_card_as_dict(card_catalog_id)
	if catalog_data.is_empty():
		push_error("CardDetailModal: Failed to get catalog data for %s" % card_catalog_id)
		return

	# Update card visual
	card_visual.set_card_data(catalog_data, false)

	# Update info labels
	var card_name_val: Variant = catalog_data.get("card_name", Loc.t("ui.common.unknown"))
	card_name_label.text = SafeTypeUtils.string(card_name_val, Loc.t("ui.common.unknown"))

	var rarity_val: StringName = catalog_data.get("rarity", RarityIDs.COMMON)
	rarity_label.text = Loc.t("ui.collection.rarity_label", {"rarity": String(rarity_val).capitalize()})

	var card_type_val: Variant = catalog_data.get("card_type", UnitConstants.CardType.SUMMON)
	var card_type: int = int(card_type_val)
	var type_str: String = Loc.t("ui.collection.type_summon") if card_type == UnitConstants.CardType.SUMMON else Loc.t("ui.collection.type_spell")
	type_label.text = Loc.t("ui.collection.type_label", {"type": type_str})

	var mana_cost_val: Variant = catalog_data.get("mana_cost", 0)
	var mana_cost: int = mana_cost_val if mana_cost_val is int else 0
	cost_label.text = Loc.t("ui.collection.cost_label", {"cost": mana_cost})

	var description_val: Variant = catalog_data.get("description", "")
	var description: String = SafeTypeUtils.string(description_val, "")
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
	var card_type_val: Variant = effective_stats.get("card_type", UnitConstants.CardType.SUMMON)
	var card_type: int = int(card_type_val)

	if card_type == UnitConstants.CardType.SUMMON:
		# Show summon stats: HP, Damage, Speed, Attack Speed, Crit
		_add_stat_label("stat_hp", effective_stats.get("max_hp", 0))
		_add_stat_label("stat_damage", effective_stats.get("attack_damage", 0))

		# Show damage type based on elemental affinity
		var element_str: String = effective_stats.get("elemental_affinity", "neutral")
		var damage_type: String = _get_damage_type_display(element_str)
		_add_stat_label_text("stat_damage_type", damage_type)

		_add_stat_label("stat_move_speed", effective_stats.get("move_speed", 0))
		_add_stat_label("stat_attack_speed", effective_stats.get("attack_speed", 0))

		# Show crit stats
		var crit_chance: float = float(effective_stats.get("crit_chance", 0.0))
		_add_stat_label_percent("stat_crit_chance", crit_chance)
		var crit_damage: float = float(effective_stats.get("crit_damage", 1.5))
		_add_stat_label_multiplier("stat_crit_damage", crit_damage)

		# Show defensive stats
		var armor: float = float(effective_stats.get("armor", 0.0))
		_add_stat_label("stat_armor", armor)
		var magic_resist: float = float(effective_stats.get("magic_resist", 0.0))
		_add_stat_label("stat_magic_resist", magic_resist)
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
	var base_stats: Dictionary = CardCatalogApi.get_card_as_dict(card_catalog_id).duplicate(true)

	if base_stats.is_empty():
		return {}

	# If no instance ID, return base stats
	if card_instance_id.is_empty():
		return base_stats

	var effective: Dictionary = CardServiceApi.get_effective_stats_dict(card_instance_id)
	if not effective.is_empty():
		return effective

	return base_stats


func _create_stat_label(stat_name: String, value_str: String) -> void:
	var label: Label = Label.new()
	label.text = "%s: %s" % [stat_name, value_str]
	label.add_theme_font_size_override("font_size", 16)
	label.add_theme_color_override("font_color", GameColorPalette.TEXT_SECONDARY)
	stats_container.add_child(label)


func _add_stat_label(loc_key: String, value: Variant) -> void:
	var stat_name: String = Loc.t("ui.collection." + loc_key)
	var value_str: String
	if value is float:
		if abs(value - round(value)) < 0.01:
			value_str = str(int(round(value)))
		else:
			value_str = "%.1f" % value
	else:
		value_str = str(value)
	_create_stat_label(stat_name, value_str)


func _add_stat_label_percent(loc_key: String, value: float) -> void:
	var stat_name: String = Loc.t("ui.collection." + loc_key)
	var value_str: String = "%d%%" % int(value * 100)
	_create_stat_label(stat_name, value_str)


func _add_stat_label_multiplier(loc_key: String, value: float) -> void:
	var stat_name: String = Loc.t("ui.collection." + loc_key)
	var value_str: String = "%.1fx" % value
	_create_stat_label(stat_name, value_str)


func _add_stat_label_text(loc_key: String, value_text: String) -> void:
	var stat_name: String = Loc.t("ui.collection." + loc_key)
	_create_stat_label(stat_name, value_text)


func _get_damage_type_display(element_str: String) -> String:
	# Neutral element means physical damage
	if element_str.is_empty() or element_str == "neutral":
		return Loc.t("ui.collection.damage_type_physical")
	# Use localized element name
	return Loc.t("elements." + element_str)

## =============================================================================
## PROGRESSION DISPLAY
## =============================================================================

func _update_progression_display() -> void:
	if card_instance_id.is_empty():
		_hide_progression()
		return

	var info: Dictionary = CardServiceApi.get_card_progression_info_dict(card_instance_id)
	if info.is_empty():
		_hide_progression()
		return

	var level: int = SafeTypeUtils.int_val(info.get("level", 1), 1)
	var max_level: int = SafeTypeUtils.int_val(info.get("max_level", 10), 10)
	var current_xp: int = SafeTypeUtils.int_val(info.get("xp", 0), 0)
	var xp_for_next: int = SafeTypeUtils.int_val(info.get("xp_for_next_level", 0), 0)
	var xp_progress: float = float(info.get("xp_progress", 0.0))
	var can_level_up_val: bool = SafeTypeUtils.bool_val(info.get("can_level_up", false), false)
	var is_max_level: bool = SafeTypeUtils.bool_val(info.get("is_max_level", false), false)
	var unspent_trait_points: int = SafeTypeUtils.int_val(info.get("unspent_trait_points", 0), 0)

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
		level_up_button.text = Loc.t("ui.collection.level_up_button_simple")
		level_up_button.disabled = false
	else:
		level_up_button.visible = true
		level_up_button.text = Loc.t("ui.collection.level_up_button_locked")
		level_up_button.disabled = true

	trait_points_label.visible = true
	trait_points_label.text = Loc.t("ui.collection.unspent_trait_points_label", {"count": unspent_trait_points})

	_hide_inline_trait_offer_controls()

func _hide_progression() -> void:
	level_label.text = ""
	xp_label.text = ""
	xp_progress_bar.value = 0
	level_up_button.visible = false
	trait_points_label.visible = false
	_hide_inline_trait_offer_controls()

func _hide_inline_trait_offer_controls() -> void:
	trait_offer_header.visible = false
	apply_trait_button.visible = false
	progression_status_label.visible = false
	for child: Node in trait_offers_container.get_children():
		child.queue_free()

## =============================================================================
## TRAITS DISPLAY
## =============================================================================

func _update_traits_display() -> void:
	# Clear existing trait boxes
	for child: Node in traits_container.get_children():
		child.queue_free()

	if card_instance_id.is_empty() or card_catalog_id.is_empty():
		traits_section.visible = false
		return

	var trait_ids: Array = CardServiceApi.get_applied_traits(card_instance_id)
	if trait_ids.is_empty():
		traits_section.visible = false
		return

	# Update header with localization
	traits_header.text = Loc.t("ui.collection.traits_header")

	# Create a box for each applied trait
	var rendered_count: int = 0
	for trait_id: Variant in trait_ids:
		var trait_id_str: String = SafeTypeUtils.string(trait_id, "")
		if trait_id_str.is_empty():
			continue

		var trait_data: Dictionary = CardServiceApi.get_card_trait_dict(card_catalog_id, trait_id_str)
		if trait_data.is_empty():
			continue

		var box: PanelContainer = _create_trait_box(trait_data)
		traits_container.add_child(box)
		rendered_count += 1

	traits_section.visible = rendered_count > 0


func _create_trait_box(trait_data: Dictionary) -> PanelContainer:
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
	name_label.text = SafeTypeUtils.string(trait_data.get("name", Loc.t("ui.common.unknown")), Loc.t("ui.common.unknown"))
	name_label.add_theme_font_size_override("font_size", 16)
	name_label.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	vbox.add_child(name_label)

	# Compact summary is the primary copy.
	var compact_summary: String = SafeTypeUtils.string(trait_data.get("summary_short", ""), "")
	if compact_summary.is_empty():
		compact_summary = SafeTypeUtils.string(trait_data.get("description", ""), "")

	if not compact_summary.is_empty():
		var summary_label: Label = Label.new()
		summary_label.text = compact_summary
		summary_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		summary_label.add_theme_font_size_override("font_size", 14)
		summary_label.add_theme_color_override("font_color", GameColorPalette.SUCCESS)
		vbox.add_child(summary_label)

	var description: String = SafeTypeUtils.string(trait_data.get("description", ""), "")
	if not description.is_empty():
		box.tooltip_text = description

	return box

## =============================================================================
## DECK ACTION
## =============================================================================

func _update_deck_action_button() -> void:
	if current_deck_id.is_empty():
		deck_action_button.visible = false
		return

	deck_action_button.visible = true
	if is_card_in_deck:
		deck_action_button.text = Loc.t("ui.collection.remove_from_deck")
	else:
		deck_action_button.text = Loc.t("ui.collection.add_to_deck")

func _on_deck_action_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if card_instance_id.is_empty() or current_deck_id.is_empty():
		return

	var action: String = "remove" if is_card_in_deck else "add"
	deck_action_requested.emit(card_instance_id, action)
	_close()

## =============================================================================
## EVENT HANDLERS
## =============================================================================

func _on_level_up_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
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
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	closed.emit()
	hide()
	queue_free()
