extends Control
class_name CardFullStatsModal

signal closed()

@onready var background: ColorRect = %Background
@onready var close_button: Button = %CloseButton
@onready var card_name_label: Label = %CardNameLabel
@onready var type_icon: TextureRect = %TypeIcon
@onready var rarity_badge_label: Label = %RarityBadgeLabel
@onready var meta_details_label: Label = %MetaDetailsLabel
@onready var description_label: Label = %DescriptionLabel
@onready var stats_header: Label = %StatsHeader
@onready var stats_container: GridContainer = %StatsContainer

const STAT_PLACEHOLDER_ICONS: Dictionary = {
	"mana_cost": "MC",
	"cast_time": "CT",
	"stat_hp": "HP",
	"physical_damage": "PD",
	"magic_damage": "MD",
	"stat_attack_speed": "AS",
	"stat_attack_range": "RG",
	"stat_move_speed": "MS",
	"stat_armor": "AR",
	"stat_magic_resist": "MR",
	"soul_strength": "SS",
	"stat_spell_damage": "SD",
	"stat_spell_radius": "AO",
	"stat_spell_duration": "DU"
}

const STAT_ICON_COLORS: Dictionary = {
	"mana_cost": Color(0.30, 0.55, 0.90),
	"cast_time": Color(0.90, 0.75, 0.30),
	"stat_hp": Color(0.90, 0.30, 0.30),
	"physical_damage": Color(0.92, 0.55, 0.26),
	"magic_damage": Color(0.60, 0.45, 0.92),
	"stat_attack_speed": Color(0.95, 0.85, 0.50),
	"stat_attack_range": Color(0.35, 0.90, 0.90),
	"stat_move_speed": Color(0.45, 0.95, 0.55),
	"stat_armor": Color(0.70, 0.70, 0.80),
	"stat_magic_resist": Color(0.50, 0.75, 0.95),
	"soul_strength": Color(0.40, 0.90, 0.95),
	"stat_spell_damage": Color(0.70, 0.45, 0.95),
	"stat_spell_radius": Color(0.40, 0.85, 0.80),
	"stat_spell_duration": Color(0.85, 0.70, 0.35)
}

const STAT_TOOLTIP_KEYS: Dictionary = {
	"mana_cost": "ui.collection.stat_tooltip_mana_cost",
	"cast_time": "ui.collection.stat_tooltip_cast_time",
	"stat_hp": "ui.collection.stat_tooltip_hp",
	"physical_damage": "ui.collection.stat_tooltip_physical_damage",
	"magic_damage": "ui.collection.stat_tooltip_magic_damage",
	"stat_attack_speed": "ui.collection.stat_tooltip_attack_speed",
	"stat_attack_range": "ui.collection.stat_tooltip_attack_range",
	"stat_move_speed": "ui.collection.stat_tooltip_move_speed",
	"stat_armor": "ui.collection.stat_tooltip_armor",
	"stat_magic_resist": "ui.collection.stat_tooltip_magic_resist",
	"soul_strength": "ui.collection.stat_tooltip_soul_strength",
	"stat_spell_damage": "ui.collection.stat_tooltip_spell_damage",
	"stat_spell_radius": "ui.collection.stat_tooltip_spell_radius",
	"stat_spell_duration": "ui.collection.stat_tooltip_spell_duration"
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
		type_style.bg_color = GameColorPalette.with_alpha(GameColorPalette.UI_BG_DARK, 0.85)
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
	if card_type == UnitConstants.CardType.SUMMON:
		var mana_cost: int = SafeTypeUtils.int_val(effective_stats.get("mana_cost", 0), 0)
		var cast_time: float = float(effective_stats.get("summon_time", 0.0))
		_add_custom_stat_localized("mana_cost", str(mana_cost))
		_add_custom_stat_localized("cast_time", _format_seconds(cast_time))
		_add_stat_label("stat_hp", effective_stats.get("max_hp", 0))

		var damage_split: Dictionary = _get_split_damage(effective_stats)
		_add_custom_stat_localized("physical_damage", _format_number(damage_split.get("physical", 0.0)))
		_add_custom_stat_localized("magic_damage", _format_number(damage_split.get("magic", 0.0)))
		_add_stat_label("stat_attack_speed", effective_stats.get("attack_speed", 0))

		_add_stat_label("stat_attack_range", effective_stats.get("attack_range", 0))
		_add_stat_label("stat_move_speed", effective_stats.get("move_speed", 0))
		_add_stat_label("stat_armor", effective_stats.get("armor", 0.0))
		_add_stat_label("stat_magic_resist", effective_stats.get("magic_resist", 0.0))
		if effective_stats.has("soul_strength"):
			_add_custom_stat_localized("soul_strength", _format_number(float(effective_stats.get("soul_strength", 0.0))))
	else:
		var mana_cost_spell: int = SafeTypeUtils.int_val(effective_stats.get("mana_cost", 0), 0)
		var cast_time_spell: float = float(effective_stats.get("summon_time", 0.0))
		_add_custom_stat_localized("mana_cost", str(mana_cost_spell))
		_add_custom_stat_localized("cast_time", _format_seconds(cast_time_spell))

		var spell_damage: Variant = effective_stats.get("spell_damage", 0)
		_add_stat_label("stat_spell_damage", spell_damage)

		var spell_radius: Variant = effective_stats.get("spell_radius", null)
		var spell_duration: Variant = effective_stats.get("spell_duration", null)
		_add_stat_label("stat_spell_radius", spell_radius if spell_radius != null else "-")
		_add_stat_label("stat_spell_duration", spell_duration if spell_duration != null else "-")

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


func _create_stat_row(stat_id: String, stat_name: String, value_str: String) -> void:
	var row: HBoxContainer = HBoxContainer.new()
	row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_theme_constant_override("separation", 10)

	var icon_panel: PanelContainer = PanelContainer.new()
	icon_panel.custom_minimum_size = Vector2(22, 22)
	var icon_style: StyleBoxFlat = StyleBoxFlat.new()
	var icon_color: Color = STAT_ICON_COLORS.get(stat_id, Color(0.6, 0.6, 0.6))
	icon_style.bg_color = icon_color.darkened(0.45)
	icon_style.border_color = icon_color
	icon_style.set_border_width_all(1)
	icon_style.set_corner_radius_all(4)
	icon_panel.add_theme_stylebox_override("panel", icon_style)

	var icon_label: Label = Label.new()
	icon_label.text = STAT_PLACEHOLDER_ICONS.get(stat_id, "??")
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
	var tooltip_desc: String = _get_localized_tooltip_desc(stat_id)
	row.tooltip_text = _build_stat_tooltip(stat_name, value_str, tooltip_desc)
	stats_container.add_child(row)


func _add_stat_label(loc_key: String, value: Variant) -> void:
	var stat_name: String = Loc.t("ui.collection." + loc_key)
	var value_str: String
	if value is float:
		value_str = _format_number(value)
	else:
		value_str = str(value)
	_create_stat_row(loc_key, stat_name, value_str)


func _add_custom_stat(stat_id: String, label: String, value_text: String) -> void:
	_create_stat_row(stat_id, label, value_text)


func _add_custom_stat_localized(stat_id: String, value_text: String) -> void:
	_add_custom_stat(stat_id, _get_custom_stat_label(stat_id), value_text)


func _get_custom_stat_label(stat_id: String) -> String:
	match stat_id:
		"mana_cost":
			return Loc.t("ui.collection.stat_mana_cost")
		"cast_time":
			return Loc.t("ui.collection.stat_cast_time")
		"physical_damage":
			return Loc.t("ui.collection.stat_physical_damage")
		"magic_damage":
			return Loc.t("ui.collection.stat_magic_damage")
		"soul_strength":
			return Loc.t("ui.collection.stat_soul_strength")
		_:
			return stat_id


func _get_localized_tooltip_desc(stat_id: String) -> String:
	var key: String = SafeTypeUtils.string(STAT_TOOLTIP_KEYS.get(stat_id, ""), "")
	if key.is_empty():
		return ""
	return Loc.t(key)


func _build_stat_tooltip(stat_name: String, value_text: String, description: String) -> String:
	var tooltip: String = "%s: %s" % [stat_name, value_text]
	if not description.is_empty():
		tooltip += "\n" + description
	return tooltip


func _format_number(value: float) -> String:
	if abs(value - round(value)) < 0.01:
		return str(int(round(value)))
	return "%.1f" % value


func _format_seconds(seconds: float) -> String:
	return "%ss" % _format_number(seconds)


func _get_elemental_affinity(effective_stats: Dictionary) -> String:
	var direct_affinity: String = SafeTypeUtils.string(effective_stats.get("elemental_affinity", ""), "")
	if not direct_affinity.is_empty():
		return direct_affinity

	var categories_var: Variant = effective_stats.get("categories", {})
	if categories_var is Dictionary:
		var categories: Dictionary = categories_var
		return SafeTypeUtils.string(categories.get("elemental_affinity", "neutral"), "neutral")

	return "neutral"


func _get_split_damage(effective_stats: Dictionary) -> Dictionary:
	var physical_damage: float = float(effective_stats.get("physical_damage", 0.0))
	var magic_damage: float = float(effective_stats.get("magic_damage", 0.0))

	if abs(physical_damage) < 0.001 and abs(magic_damage) < 0.001:
		var base_damage: float = float(effective_stats.get("attack_damage", 0.0))
		var elemental_affinity: String = _get_elemental_affinity(effective_stats)
		if elemental_affinity == "neutral":
			physical_damage = base_damage
		else:
			magic_damage = base_damage

	return {
		"physical": physical_damage,
		"magic": magic_damage
	}


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


func _on_background_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event
		if mouse_event.pressed and mouse_event.button_index == MOUSE_BUTTON_LEFT:
			_close()


func _close() -> void:
	closed.emit()
	hide()
	queue_free()
