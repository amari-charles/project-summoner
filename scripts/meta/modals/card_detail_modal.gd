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
@onready var meta_banner: HBoxContainer = %MetaBanner
@onready var type_icon: TextureRect = %TypeIcon
@onready var rarity_badge: Label = %RarityBadgeLabel
@onready var role_badge: Label = %RoleBadgeLabel
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
@onready var stats_hint_label: Label = %StatsHintLabel
@onready var deck_action_button: Button = %DeckActionButton

const CardFullStatsModalScene: PackedScene = preload("res://scenes/meta/modals/card_full_stats_modal.tscn")
const CardStatsUiHelperScript: Script = preload("res://scripts/meta/modals/card_stats_ui_helper.gd")

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
	stats_container.columns = 2
	stats_section.gui_input.connect(_on_stats_section_input)
	stats_section.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	stats_section.tooltip_text = Loc.t("ui.collection.view_full_stats_tooltip")
	stats_hint_label.text = Loc.t("ui.collection.view_full_stats_hint")
	stats_header.mouse_filter = Control.MOUSE_FILTER_IGNORE
	stats_container.mouse_filter = Control.MOUSE_FILTER_IGNORE
	stats_hint_label.mouse_filter = Control.MOUSE_FILTER_IGNORE

	# Primary view is summary-only.
	description_label.visible = false
	traits_section.visible = false
	rarity_label.visible = false
	type_label.visible = false
	cost_label.visible = false

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
	_update_rarity_badge(String(rarity_val))
	var tactical_role: String = SafeTypeUtils.string(catalog_data.get("tactical_role", ""), "")
	_update_role_badge(tactical_role)

	var card_type_val: Variant = catalog_data.get("card_type", UnitConstants.CardType.SUMMON)
	var card_type: int = int(card_type_val)
	var type_str: String = Loc.t("ui.collection.type_summon") if card_type == UnitConstants.CardType.SUMMON else Loc.t("ui.collection.type_spell")
	type_label.text = Loc.t("ui.collection.type_label", {"type": type_str})
	_update_type_icon(catalog_data)

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

	stats_header.text = Loc.t("ui.collection.core_stats_header")

	# Determine card type to show appropriate stats
	var card_type_val: Variant = effective_stats.get("card_type", UnitConstants.CardType.SUMMON)
	var card_type: int = int(card_type_val)
	var mana_cost: int = SafeTypeUtils.int_val(effective_stats.get("mana_cost", 0), 0)
	var cast_time: float = float(effective_stats.get("summon_time", 0.0))

	if card_type == UnitConstants.CardType.SUMMON:
		_add_custom_stat_localized("mana_cost", str(mana_cost))
		_add_custom_stat_localized("cast_time", CardStatsUiHelperScript.format_seconds(cast_time))
		_add_stat_label("stat_hp", effective_stats.get("max_hp", 0))

		var damage_split: Dictionary = CardStatsUiHelperScript.get_split_damage(effective_stats)
		_add_custom_stat_localized("physical_damage", CardStatsUiHelperScript.format_number(damage_split.get("physical", 0.0)))
		_add_custom_stat_localized("magic_damage", CardStatsUiHelperScript.format_number(damage_split.get("magic", 0.0)))

		_add_stat_label("stat_attack_speed", effective_stats.get("attack_speed", 0))
	else:
		_add_custom_stat_localized("mana_cost", str(mana_cost))
		_add_custom_stat_localized("cast_time", CardStatsUiHelperScript.format_seconds(cast_time))

		# Show spell stats: Spell Damage, Spell Radius, Spell Duration (if applicable)
		var spell_damage: Variant = effective_stats.get("spell_damage", null)
		var spell_radius: Variant = effective_stats.get("spell_radius", null)
		var spell_duration: Variant = effective_stats.get("spell_duration", null)

		if spell_damage != null:
			_add_stat_label("stat_spell_damage", spell_damage)
		if spell_radius != null:
			_add_stat_label("stat_spell_radius", spell_radius)
		if spell_duration != null:
			_add_stat_label("stat_spell_duration", spell_duration)

		# Hide section if no spell stats to show
		if spell_damage == null and spell_radius == null and spell_duration == null:
			stats_section.visible = false
			return

	_normalize_stats_grid_columns()
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


func _create_stat_row(stat_id: String, stat_name: String, value_str: String) -> void:
	var row: HBoxContainer = HBoxContainer.new()
	row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_theme_constant_override("separation", 10)
	# Allow stat-row tooltips while still bubbling clicks to the section handler.
	row.mouse_filter = Control.MOUSE_FILTER_PASS

	var icon_panel: PanelContainer = PanelContainer.new()
	icon_panel.custom_minimum_size = Vector2(22, 22)
	icon_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var icon_style: StyleBoxFlat = StyleBoxFlat.new()
	var icon_color: Color = CardStatsUiHelperScript.get_icon_color(stat_id)
	icon_style.bg_color = icon_color.darkened(0.45)
	icon_style.border_color = icon_color
	icon_style.set_border_width_all(1)
	icon_style.set_corner_radius_all(4)
	icon_panel.add_theme_stylebox_override("panel", icon_style)

	var icon_label: Label = Label.new()
	icon_label.text = CardStatsUiHelperScript.get_placeholder_text(stat_id)
	icon_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	icon_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	icon_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	icon_label.add_theme_font_size_override("font_size", 10)
	icon_label.add_theme_color_override("font_color", icon_color.lightened(0.4))
	icon_panel.add_child(icon_label)

	var value_label: Label = Label.new()
	value_label.text = value_str
	value_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	value_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	value_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	value_label.add_theme_font_size_override("font_size", 15)
	value_label.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)

	row.add_child(icon_panel)
	row.add_child(value_label)
	var tooltip_desc: String = CardStatsUiHelperScript.get_tooltip_description(stat_id)
	row.tooltip_text = _build_stat_tooltip(stat_name, value_str, tooltip_desc)
	stats_container.add_child(row)


func _add_stat_label(loc_key: String, value: Variant) -> void:
	var stat_name: String = Loc.t("ui.collection." + loc_key)
	var value_str: String
	if value is float:
		value_str = CardStatsUiHelperScript.format_number(value)
	else:
		value_str = str(value)
	_create_stat_row(loc_key, stat_name, value_str)


func _add_custom_stat(stat_id: String, label: String, value_text: String) -> void:
	_create_stat_row(stat_id, label, value_text)


func _add_custom_stat_localized(stat_id: String, value_text: String) -> void:
	_add_custom_stat(stat_id, CardStatsUiHelperScript.get_custom_stat_label(stat_id), value_text)


func _build_stat_tooltip(stat_name: String, value_text: String, description: String) -> String:
	var tooltip: String = "%s: %s" % [stat_name, value_text]
	if not description.is_empty():
		tooltip += "\n" + description
	return tooltip


func _normalize_stats_grid_columns() -> void:
	var stat_count: int = stats_container.get_child_count()
	stats_container.columns = 1 if stat_count % 2 != 0 else 2


func _update_type_icon(catalog_data: Dictionary) -> void:
	var icon_path: String = CardVisualHelper.get_card_type_icon_path(catalog_data)
	if icon_path.is_empty():
		type_icon.visible = false
		return

	type_icon.texture = load(icon_path)
	type_icon.visible = true

	var type_style: StyleBoxFlat = StyleBoxFlat.new()
	type_style.bg_color = GameColorPalette.with_alpha(GameColorPalette.UI_BG_DARK, 0.85)
	type_style.border_color = GameColorPalette.TEXT_SECONDARY
	type_style.set_border_width_all(1)
	type_style.set_corner_radius_all(5)
	var type_badge: PanelContainer = type_icon.get_parent()
	type_badge.add_theme_stylebox_override("panel", type_style)


func _update_rarity_badge(rarity: String) -> void:
	var rarity_text: String = rarity.strip_edges().to_upper()
	if rarity_text.is_empty():
		rarity_text = String(RarityIDs.COMMON).to_upper()
	rarity_badge.text = rarity_text

	var rarity_color: Color = GameColorPalette.get_rarity_color(rarity.to_lower())
	var badge_style: StyleBoxFlat = StyleBoxFlat.new()
	badge_style.bg_color = rarity_color.darkened(0.7)
	badge_style.border_color = rarity_color
	badge_style.set_border_width_all(1)
	badge_style.set_corner_radius_all(6)
	var rarity_panel: PanelContainer = rarity_badge.get_parent()
	rarity_panel.add_theme_stylebox_override("panel", badge_style)
	rarity_badge.add_theme_color_override("font_color", rarity_color.lightened(0.35))
	rarity_badge.add_theme_constant_override("outline_size", 1)
	rarity_badge.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.7))


func _update_role_badge(tactical_role: String) -> void:
	var role_panel: PanelContainer = role_badge.get_parent()
	var role_id: String = tactical_role.strip_edges().to_lower()
	if role_id.is_empty():
		role_panel.visible = false
		return

	role_panel.visible = true
	role_badge.text = _get_role_display_name(role_id).to_upper()

	var role_color: Color = _get_role_color(role_id)
	var badge_style: StyleBoxFlat = StyleBoxFlat.new()
	badge_style.bg_color = role_color.darkened(0.7)
	badge_style.border_color = role_color
	badge_style.set_border_width_all(1)
	badge_style.set_corner_radius_all(6)
	role_panel.add_theme_stylebox_override("panel", badge_style)
	role_badge.add_theme_color_override("font_color", role_color.lightened(0.35))
	role_badge.add_theme_constant_override("outline_size", 1)
	role_badge.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.7))


func _get_role_display_name(role_id: String) -> String:
	match role_id:
		"frontliner":
			return Loc.t("ui.collection.role_frontliner")
		"flanker":
			return Loc.t("ui.collection.role_flanker")
		"backliner":
			return Loc.t("ui.collection.role_backliner")
		"mixed":
			return Loc.t("ui.collection.role_mixed")
		_:
			return Loc.t("ui.collection.role_unknown")


func _get_role_color(role_id: String) -> Color:
	match role_id:
		"frontliner":
			return GameColorPalette.WARNING
		"flanker":
			return GameColorPalette.INFO
		"backliner":
			return GameColorPalette.SUCCESS
		"mixed":
			return GameColorPalette.TEXT_SECONDARY
		_:
			return GameColorPalette.TEXT_SECONDARY


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

	var flow: HFlowContainer = HFlowContainer.new()
	flow.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	flow.add_theme_constant_override("h_separation", 8)
	flow.add_theme_constant_override("v_separation", 8)
	traits_container.add_child(flow)

	# Create icon chip for each applied trait
	var rendered_count: int = 0
	for trait_id: Variant in trait_ids:
		var trait_id_str: String = SafeTypeUtils.string(trait_id, "")
		if trait_id_str.is_empty():
			continue

		var trait_data: Dictionary = CardServiceApi.get_card_trait_dict(card_catalog_id, trait_id_str)
		if trait_data.is_empty():
			continue

		var icon_chip: PanelContainer = _create_trait_chip(trait_data)
		flow.add_child(icon_chip)
		rendered_count += 1

	traits_section.visible = rendered_count > 0


func _create_trait_chip(trait_data: Dictionary) -> PanelContainer:
	var box: PanelContainer = PanelContainer.new()
	box.custom_minimum_size = Vector2(42, 42)

	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = GameColorPalette.with_alpha(GameColorPalette.UI_BG_DARK, 0.8)
	style.set_border_width_all(2)
	style.border_color = GameColorPalette.SUCCESS
	style.set_corner_radius_all(4)
	box.add_theme_stylebox_override("panel", style)

	var trait_name: String = SafeTypeUtils.string(trait_data.get("name", Loc.t("ui.common.unknown")), Loc.t("ui.common.unknown"))
	var compact_summary: String = SafeTypeUtils.string(trait_data.get("summary_short", ""), "")
	if compact_summary.is_empty():
		compact_summary = SafeTypeUtils.string(trait_data.get("description", ""), "")

	var abbr: Label = Label.new()
	abbr.text = _abbreviate_trait_name(trait_name)
	abbr.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	abbr.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	abbr.add_theme_font_size_override("font_size", 12)
	abbr.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)
	box.add_child(abbr)

	var description: String = SafeTypeUtils.string(trait_data.get("description", ""), "")
	box.tooltip_text = _build_trait_tooltip(trait_name, compact_summary, description)

	return box


func _abbreviate_trait_name(name: String) -> String:
	var normalized: String = name.strip_edges()
	if normalized.is_empty():
		return "??"

	var words: PackedStringArray = normalized.split(" ", false)
	if words.size() >= 2:
		var first: String = words[0]
		var second: String = words[1]
		if not first.is_empty() and not second.is_empty():
			return (first.left(1) + second.left(1)).to_upper()

	return normalized.left(2).to_upper()


func _build_trait_tooltip(name: String, summary: String, description: String) -> String:
	var lines: PackedStringArray = [name]
	if not summary.is_empty():
		lines.append(summary)
	elif not description.is_empty():
		lines.append(description)
	return "\n".join(lines)

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


func _on_stats_section_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			_open_full_stats_modal()


func _open_full_stats_modal() -> void:
	if card_catalog_id.is_empty():
		return

	var modal: Node = CardFullStatsModalScene.instantiate()
	if not modal:
		return

	add_child(modal)
	if modal.has_method("open_for_card"):
		modal.call("open_for_card", card_instance_id, card_catalog_id)

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
