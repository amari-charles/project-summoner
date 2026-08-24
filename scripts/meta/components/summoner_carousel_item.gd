extends Control
class_name SummonerCarouselItem

signal selected(summoner_id: String)

@onready var name_label: Label = %NameLabel
@onready var character_texture: TextureRect = %CharacterTexture
@onready var level_label: Label = %LevelLabel
@onready var active_label: Label = %ActiveLabel
@onready var select_button: Button = %SelectButton

var summoner_id: String = ""
var _element_color: Color = GameColorPalette.UI_BORDER_STRONG
var _is_active: bool = false


func _ready() -> void:
	select_button.pressed.connect(_on_selected)


func set_summoner(value: String, is_active: bool) -> void:
	summoner_id = value
	_is_active = is_active
	var config: SummonerConfig = SummonerConfig.from_dict(
		SummonerCatalogApi.get_summoner(summoner_id)
	)
	if not config or not config.is_valid():
		return

	var element: ElementTypes.Element = config.get_element()
	_element_color = ElementTypes.get_color(element)
	name_label.text = config.summoner_name
	character_texture.modulate = Color.WHITE.lerp(_element_color, 0.22)
	var progression: Dictionary = SummonerProgressionApi.get_summoner_progression_info(summoner_id)
	level_label.text = Loc.t(
		"ui.summoner_panel.level_display",
		{"level": SafeTypeUtils.int_val(progression.get("level"), 1)}
	)
	active_label.visible = _is_active
	_refresh_style(false)


func set_focused(is_focused: bool) -> void:
	_refresh_style(is_focused)


func _refresh_style(is_focused: bool) -> void:
	name_label.add_theme_color_override(
		"font_color",
		GameColorPalette.TEXT_PRIMARY if is_focused else GameColorPalette.TEXT_SECONDARY
	)


func _on_selected() -> void:
	selected.emit(summoner_id)
