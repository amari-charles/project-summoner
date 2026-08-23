extends HBoxContainer
class_name BattleSummonerStatus

## Compact summoner identity and resource display used by both sides of battle HUD.

@export var mirrored: bool = false

@onready var portrait_frame: Control = %PortraitFrame
@onready var portrait_rect: ColorRect = %PortraitRect
@onready var portrait_texture: TextureRect = %PortraitTexture
@onready var element_label: Label = %ElementLabel
@onready var name_label: Label = %NameLabel
@onready var hp_bar: StatBar = %HPBar
@onready var mana_bar: StatBar = %ManaBar

const SHADER_PARAM_UV_OFFSET: StringName = &"uv_offset"
const SHADER_PARAM_UV_SCALE: StringName = &"uv_scale"


func _ready() -> void:
	_ensure_portrait_material_unique()
	if mirrored:
		move_child(portrait_frame, get_child_count() - 1)
		name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT


func configure(summoner_id: String, fallback_name: String) -> void:
	if summoner_id.is_empty() or not SummonerIDs.is_valid(summoner_id):
		_show_placeholder(ElementTypes.NEUTRAL)
		name_label.text = fallback_name
		return

	var config: SummonerConfig = SummonerConfig.from_dict(
		SummonerCatalogApi.get_summoner(summoner_id)
	)
	name_label.text = config.summoner_name if not config.summoner_name.is_empty() else fallback_name

	if not config.summoner_icon_path.is_empty():
		var texture: Texture2D = load(config.summoner_icon_path) as Texture2D
		if texture != null:
			portrait_texture.texture = texture
			_apply_portrait_crop(config.portrait_uv_offset, config.portrait_uv_scale)
			portrait_texture.visible = true
			portrait_rect.visible = false
			return

	_show_placeholder(config.get_element())


func _show_placeholder(element: ElementTypes.Element) -> void:
	portrait_texture.visible = false
	portrait_rect.visible = true
	portrait_rect.color = ElementTypes.get_color(element)
	element_label.text = ElementTypes.get_symbol(element)


func _ensure_portrait_material_unique() -> void:
	if portrait_texture.material != null:
		portrait_texture.material = portrait_texture.material.duplicate()


func _apply_portrait_crop(uv_offset: Vector2, uv_scale: Vector2) -> void:
	var shader_material: ShaderMaterial = portrait_texture.material as ShaderMaterial
	if shader_material == null:
		return
	shader_material.set_shader_parameter(SHADER_PARAM_UV_OFFSET, uv_offset)
	shader_material.set_shader_parameter(SHADER_PARAM_UV_SCALE, uv_scale)
