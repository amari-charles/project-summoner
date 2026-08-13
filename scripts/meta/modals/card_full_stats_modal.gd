extends Control
class_name CardFullStatsModal

signal closed()

@onready var background: ColorRect = %Background
@onready var close_button: Button = %CloseButton
@onready var card_name_label: Label = %CardNameLabel
@onready var type_icon: TextureRect = %TypeIcon
@onready var rarity_badge_label: Label = %RarityBadgeLabel
@onready var role_badge_label: Label = %RoleBadgeLabel
@onready var meta_details_label: Label = %MetaDetailsLabel
@onready var description_label: Label = %DescriptionLabel
@onready var stats_header: Label = %StatsHeader
@onready var stats_container: GridContainer = %StatsContainer

const CardStatsUiHelperScript: Script = preload("res://scripts/meta/modals/card_stats_ui_helper.gd")

const PRIMARY_SUMMON_STAT_KEYS: Array[String] = [
	"mana_cost",
	"summon_time",
	"max_hp",
	"attack_damage",
	"attack_speed",
	"attack_range",
	"move_speed",
	"armor",
	"magic_resist",
	"soul_strength",
	"crit_chance",
	"crit_damage",
	"spawn_count",
	"aggro_radius",
	"cooldown",
	"separation_radius"
]

const PRIMARY_SPELL_STAT_KEYS: Array[String] = [
	"mana_cost",
	"summon_time",
	"spell_damage",
	"spell_radius",
	"spell_duration",
	"cooldown",
	"selection_radius",
	"formation_duration"
]

const STAT_LOCALIZATION_KEYS: Dictionary = {
	"max_hp": "stat_hp",
	"attack_damage": "stat_damage",
	"attack_speed": "stat_attack_speed",
	"attack_range": "stat_attack_range",
	"move_speed": "stat_move_speed",
	"armor": "stat_armor",
	"magic_resist": "stat_magic_resist",
	"crit_chance": "stat_crit_chance",
	"crit_damage": "stat_crit_damage",
	"spell_damage": "stat_spell_damage",
	"spell_radius": "stat_spell_radius",
	"spell_duration": "stat_spell_duration",
	"damage_type": "stat_damage_type"
}

const META_EXCLUDED_STAT_KEYS: Dictionary = {
	"catalog_id": true,
	"card_name": true,
	"description": true,
	"rarity": true,
	"card_type": true,
	"unit_id": true,
	"unit_scene_path": true,
	"unit_type": true,
	"is_ranged": true,
	"projectile_scene_path": true,
	"projectile_id": true,
	"spell_vfx": true,
	"command_type": true,
	"unlock_condition": true,
	"card_icon_path": true,
	"tactical_role": true,
	"trait_eligibility_tags": true,
	"creature_types": true,
	"roles": true,
	"spell_category": true,
	"spell_targeting": true,
	"visual_traits": true,
	"card_flags": true,
	"categories": true
}

var card_instance_id: String = ""
var card_catalog_id: String = ""


func _ready() -> void:
	close_button.pressed.connect(_close)
	background.gui_input.connect(_on_background_input)
	stats_container.columns = 2


func open_for_card(instance_id: String, catalog_id: String) -> void:
	card_instance_id = instance_id
	card_catalog_id = catalog_id
	_load_card_data()
	_update_all_stats_display()
	show()


func _load_card_data() -> void:
	var catalog_data: Dictionary = CardCatalogApi.get_card_as_dict(card_catalog_id)
	if catalog_data.is_empty():
		push_error("CardFullStatsModal: Failed to get catalog data for %s" % card_catalog_id)
		return

	var card_name_val: Variant = catalog_data.get("card_name", Loc.t("ui.common.unknown"))
	card_name_label.text = SafeTypeUtils.string(card_name_val, Loc.t("ui.common.unknown"))

	var rarity: String = SafeTypeUtils.string(catalog_data.get("rarity", String(RarityIDs.COMMON)), String(RarityIDs.COMMON))
	_update_rarity_badge(rarity)
	var tactical_role: String = SafeTypeUtils.string(catalog_data.get("tactical_role", ""), "")
	_update_role_badge(tactical_role)

	var card_type: int = SafeTypeUtils.int_val(catalog_data.get("card_type", UnitConstants.CardType.SUMMON), UnitConstants.CardType.SUMMON)
	var type_text: String = Loc.t("ui.collection.type_summon") if card_type == UnitConstants.CardType.SUMMON else Loc.t("ui.collection.type_spell")
	var mana_cost: int = SafeTypeUtils.int_val(catalog_data.get("mana_cost", 0), 0)
	meta_details_label.text = Loc.t("ui.collection.meta_type_mana_format", {"type": type_text, "mana": mana_cost})

	var icon_path: String = CardVisualHelper.get_card_type_icon_path(catalog_data)
	if icon_path.is_empty():
		type_icon.visible = false
	else:
		type_icon.texture = load(icon_path)
		type_icon.visible = true
		var type_style: StyleBoxFlat = StyleBoxFlat.new()
		type_style.bg_color = GameColorPalette.with_alpha(GameColorPalette.UI_SURFACE_ALT, 0.85)
		type_style.border_color = GameColorPalette.TEXT_SECONDARY
		type_style.set_border_width_all(1)
		type_style.set_corner_radius_all(5)
		var type_badge: PanelContainer = type_icon.get_parent()
		type_badge.add_theme_stylebox_override("panel", type_style)

	var description_val: Variant = catalog_data.get("description", "")
	var description: String = SafeTypeUtils.string(description_val, "")
	if description.is_empty():
		description = Loc.t("ui.collection.no_description")
	description_label.text = description


func _update_all_stats_display() -> void:
	for child: Node in stats_container.get_children():
		child.queue_free()

	var effective_stats: Dictionary = _get_effective_stats()
	if effective_stats.is_empty():
		return

	stats_header.text = Loc.t("ui.collection.all_stats_header")

	var card_type: int = SafeTypeUtils.int_val(effective_stats.get("card_type", UnitConstants.CardType.SUMMON), UnitConstants.CardType.SUMMON)
	var rendered_source_keys: Dictionary = {}
	var primary_keys: Array[String] = (
		PRIMARY_SUMMON_STAT_KEYS
		if card_type == UnitConstants.CardType.SUMMON
		else PRIMARY_SPELL_STAT_KEYS
	)

	for stat_key: String in primary_keys:
		_try_add_effective_stat(effective_stats, stat_key, rendered_source_keys)

	for key_var: Variant in effective_stats.keys():
		var source_key: String = str(key_var)
		if source_key.is_empty() or rendered_source_keys.has(source_key):
			continue
		if META_EXCLUDED_STAT_KEYS.has(source_key):
			continue

		var stat_value: Variant = effective_stats.get(source_key, null)
		if not _is_displayable_stat_value(stat_value):
			continue
		_add_effective_stat_row(source_key, stat_value)
		rendered_source_keys[source_key] = true

	_normalize_stats_grid_columns()


func _get_effective_stats() -> Dictionary:
	var base_stats: Dictionary = CardCatalogApi.get_card_as_dict(card_catalog_id).duplicate(true)
	if base_stats.is_empty():
		return {}

	if card_instance_id.is_empty():
		return base_stats

	var effective: Dictionary = CardServiceApi.get_effective_stats_dict(card_instance_id)
	if not effective.is_empty():
		return effective
	return base_stats


func _try_add_effective_stat(effective_stats: Dictionary, stat_key: String, rendered_source_keys: Dictionary) -> bool:
	var source_key: String = _resolve_stat_source_key(effective_stats, stat_key)
	if source_key.is_empty() or rendered_source_keys.has(source_key):
		return false

	var stat_value: Variant = effective_stats.get(source_key, null)
	if not _is_displayable_stat_value(stat_value):
		return false

	_add_effective_stat_row(stat_key, stat_value)
	rendered_source_keys[source_key] = true
	return true


func _resolve_stat_source_key(effective_stats: Dictionary, stat_key: String) -> String:
	return stat_key if effective_stats.has(stat_key) else ""


func _is_displayable_stat_value(value: Variant) -> bool:
	if value == null:
		return false
	if value is Dictionary or value is Array:
		return false
	if value is String:
		return not value.strip_edges().is_empty()
	return value is int or value is float or value is bool


func _add_effective_stat_row(stat_key: String, stat_value: Variant) -> void:
	var stat_id: String = _stat_id_for_key(stat_key)
	var stat_name: String = _stat_label_for_key(stat_key)
	var value_str: String = _format_stat_value(stat_key, stat_value)
	_create_stat_row(stat_id, stat_name, value_str)


func _stat_id_for_key(stat_key: String) -> String:
	if stat_key == "summon_time" or stat_key == "cast_time":
		return "cast_time"
	if stat_key == "mana_cost":
		return stat_key
	if STAT_LOCALIZATION_KEYS.has(stat_key):
		return str(STAT_LOCALIZATION_KEYS.get(stat_key, stat_key))
	return stat_key


func _stat_label_for_key(stat_key: String) -> String:
	if stat_key == "mana_cost":
		return CardStatsUiHelperScript.get_custom_stat_label("mana_cost")
	if stat_key == "summon_time" or stat_key == "cast_time":
		return CardStatsUiHelperScript.get_custom_stat_label("cast_time")
	if stat_key == "soul_strength":
		return CardStatsUiHelperScript.get_custom_stat_label("soul_strength")
	if STAT_LOCALIZATION_KEYS.has(stat_key):
		return Loc.t("ui.collection." + str(STAT_LOCALIZATION_KEYS.get(stat_key)))
	return _humanize_stat_key(stat_key)


func _format_stat_value(stat_key: String, value: Variant) -> String:
	if value is bool:
		return str(value)

	if stat_key == "summon_time" or stat_key == "cast_time" or stat_key == "cooldown" or stat_key == "formation_duration":
		return CardStatsUiHelperScript.format_seconds(float(value))

	if stat_key == "crit_chance":
		var crit_chance: float = float(value)
		if crit_chance <= 1.0:
			crit_chance *= 100.0
		return "%s%%" % CardStatsUiHelperScript.format_number(crit_chance)

	if stat_key == "crit_damage":
		var crit_damage: float = float(value)
		if crit_damage <= 3.0:
			crit_damage *= 100.0
		return "%s%%" % CardStatsUiHelperScript.format_number(crit_damage)

	if value is float:
		return CardStatsUiHelperScript.format_number(value)

	if value is int:
		return str(int(value))

	return str(value)


func _humanize_stat_key(stat_key: String) -> String:
	var words: PackedStringArray = stat_key.split("_", false)
	for index: int in range(words.size()):
		words[index] = str(words[index]).capitalize()
	return " ".join(words)


func _create_stat_row(stat_id: String, stat_name: String, value_str: String) -> void:
	var row: HBoxContainer = HBoxContainer.new()
	row.name = "StatRow_%s" % stat_id.replace(" ", "_")
	row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_theme_constant_override("separation", 10)

	var icon_panel: PanelContainer = PanelContainer.new()
	icon_panel.custom_minimum_size = Vector2(22, 22)
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
	icon_label.add_theme_font_size_override("font_size", 10)
	icon_label.add_theme_color_override("font_color", icon_color.lightened(0.4))
	icon_panel.add_child(icon_label)

	var value_label: Label = Label.new()
	value_label.text = value_str
	value_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	value_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	value_label.add_theme_font_size_override("font_size", 15)
	value_label.add_theme_color_override("font_color", GameColorPalette.TEXT_PRIMARY)

	row.add_child(icon_panel)
	row.add_child(value_label)
	var tooltip_desc: String = CardStatsUiHelperScript.get_tooltip_description(stat_id)
	row.tooltip_text = _build_stat_tooltip(stat_name, value_str, tooltip_desc)
	stats_container.add_child(row)


func _build_stat_tooltip(stat_name: String, value_text: String, description: String) -> String:
	var tooltip: String = "%s: %s" % [stat_name, value_text]
	if not description.is_empty():
		tooltip += "\n" + description
	return tooltip


func _normalize_stats_grid_columns() -> void:
	var stat_count: int = stats_container.get_child_count()
	stats_container.columns = 1 if stat_count % 2 != 0 else 2


func _update_rarity_badge(rarity: String) -> void:
	var rarity_text: String = rarity.strip_edges().to_upper()
	if rarity_text.is_empty():
		rarity_text = String(RarityIDs.COMMON).to_upper()
	rarity_badge_label.text = rarity_text

	var rarity_color: Color = GameColorPalette.get_rarity_color(rarity.to_lower())
	var badge_style: StyleBoxFlat = StyleBoxFlat.new()
	badge_style.bg_color = rarity_color.darkened(0.7)
	badge_style.border_color = rarity_color
	badge_style.set_border_width_all(1)
	badge_style.set_corner_radius_all(6)
	var rarity_panel: PanelContainer = rarity_badge_label.get_parent()
	rarity_panel.add_theme_stylebox_override("panel", badge_style)
	rarity_badge_label.add_theme_color_override("font_color", rarity_color.lightened(0.35))
	rarity_badge_label.add_theme_constant_override("outline_size", 1)
	rarity_badge_label.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.7))


func _update_role_badge(tactical_role: String) -> void:
	var role_panel: PanelContainer = role_badge_label.get_parent()
	var role_id: String = tactical_role.strip_edges().to_lower()
	if role_id.is_empty():
		role_panel.visible = false
		return

	role_panel.visible = true
	role_badge_label.text = _get_role_display_name(role_id).to_upper()

	var role_color: Color = _get_role_color(role_id)
	var badge_style: StyleBoxFlat = StyleBoxFlat.new()
	badge_style.bg_color = role_color.darkened(0.7)
	badge_style.border_color = role_color
	badge_style.set_border_width_all(1)
	badge_style.set_corner_radius_all(6)
	role_panel.add_theme_stylebox_override("panel", badge_style)
	role_badge_label.add_theme_color_override("font_color", role_color.lightened(0.35))
	role_badge_label.add_theme_constant_override("outline_size", 1)
	role_badge_label.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.7))


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


func _on_background_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			_close()


func _close() -> void:
	closed.emit()
	hide()
	queue_free()
