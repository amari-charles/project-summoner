extends RefCounted
class_name CardStatsUiHelper

const PLACEHOLDER_ICONS: Dictionary = {
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

const ICON_COLORS: Dictionary = {
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

const TOOLTIP_KEYS: Dictionary = {
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

const CUSTOM_STAT_LABEL_KEYS: Dictionary = {
	"mana_cost": "ui.collection.stat_mana_cost",
	"cast_time": "ui.collection.stat_cast_time",
	"physical_damage": "ui.collection.stat_physical_damage",
	"magic_damage": "ui.collection.stat_magic_damage",
	"soul_strength": "ui.collection.stat_soul_strength"
}


static func get_placeholder_text(stat_id: String) -> String:
	return str(PLACEHOLDER_ICONS.get(stat_id, "??"))


static func get_icon_color(stat_id: String) -> Color:
	return ICON_COLORS.get(stat_id, Color(0.6, 0.6, 0.6))


static func get_custom_stat_label(stat_id: String) -> String:
	var key: String = str(CUSTOM_STAT_LABEL_KEYS.get(stat_id, ""))
	if key.is_empty():
		return stat_id
	return Loc.t(key)


static func get_tooltip_description(stat_id: String) -> String:
	var key: String = str(TOOLTIP_KEYS.get(stat_id, ""))
	if key.is_empty():
		return ""
	return Loc.t(key)


static func format_number(value: float) -> String:
	if abs(value - round(value)) < 0.01:
		return str(int(round(value)))
	return "%.1f" % value


static func format_seconds(seconds: float) -> String:
	return "%ss" % format_number(seconds)


static func get_split_damage(effective_stats: Dictionary) -> Dictionary:
	var physical_damage: float = float(effective_stats.get("physical_damage", 0.0))
	var magic_damage: float = float(effective_stats.get("magic_damage", 0.0))
	return {
		"physical": physical_damage,
		"magic": magic_damage
	}
