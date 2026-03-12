extends Control
class_name SummonerIconWidget

## SummonerIconWidget - Persistent summoner portrait button
##
## A reusable component that displays the active summoner's portrait.
## Click to open summoner management panel.
##
## Usage:
##   - Add scene instance to any screen
##   - Connect to icon_clicked signal
##   - Widget auto-updates when active summoner changes

## Emitted when the icon is clicked
signal icon_clicked()

## Node references
@onready var icon_button: Button = %IconButton
@onready var portrait_rect: ColorRect = %PortraitRect
@onready var portrait_texture: TextureRect = %PortraitTexture
@onready var element_label: Label = %ElementLabel
@onready var level_badge: Label = %LevelBadge

const SHADER_PARAM_UV_OFFSET: StringName = &"uv_offset"
const SHADER_PARAM_UV_SCALE: StringName = &"uv_scale"
const DEFAULT_PORTRAIT_UV_OFFSET: Vector2 = Vector2(0.2, 0.05)
const DEFAULT_PORTRAIT_UV_SCALE: Vector2 = Vector2(0.6, 0.45)


## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	_ensure_portrait_material_unique()
	_reset_portrait_crop()

	# Connect button
	icon_button.pressed.connect(_on_icon_pressed)

	# Connect to summoner selection changes
	SummonerSelection.connect("SummonerChanged", _on_summoner_changed)

	# Initial refresh
	refresh()

## =============================================================================
## PUBLIC API
## =============================================================================

## Refresh the display from current active summoner
func refresh() -> void:
	var summoner_id: String = SummonerSelectionApi.get_active_summoner_id()
	if summoner_id.is_empty():
		_show_no_summoner()
		return

	_update_display(summoner_id)

## =============================================================================
## PRIVATE METHODS
## =============================================================================

func _update_display(summoner_id: String) -> void:
	# Get summoner config
	var config: SummonerConfig = SummonerConfig.from_dict(SummonerCatalogApi.get_summoner(summoner_id))
	if not config:
		_show_no_summoner()
		return

	# Get summoner instance for level
	var info: Dictionary = SummonerProgressionApi.get_summoner_progression_info(summoner_id)
	var level: int = info.get("level", 1)

	# Get element
	var element: ElementTypes.Element = config.get_element()

	# Check if summoner has a portrait image
	if config.summoner_icon_path and not config.summoner_icon_path.is_empty():
		var texture: Texture2D = load(config.summoner_icon_path)
		if texture:
			portrait_texture.texture = texture
			_apply_portrait_crop(config.portrait_uv_offset, config.portrait_uv_scale)
			portrait_texture.visible = true
			portrait_rect.visible = false
			element_label.visible = false
		else:
			_show_placeholder(element)
	else:
		_show_placeholder(element)

	# Update level badge
	level_badge.text = "Lv.%d" % level
	level_badge.visible = true

	# Tooltip
	icon_button.tooltip_text = config.summoner_name


func _show_placeholder(element: ElementTypes.Element) -> void:
	_reset_portrait_crop()
	portrait_texture.visible = false
	portrait_rect.visible = true
	element_label.visible = true
	portrait_rect.color = ElementTypes.get_color(element)
	element_label.text = ElementTypes.get_symbol(element)

func _show_no_summoner() -> void:
	_reset_portrait_crop()
	portrait_texture.visible = false
	portrait_rect.visible = true
	element_label.visible = true
	portrait_rect.color = ElementTypes.get_color("neutral")
	element_label.text = ElementTypes.get_symbol("neutral")
	level_badge.text = ""
	level_badge.visible = false
	icon_button.tooltip_text = Loc.t("ui.summoner_icon.no_summoner")

func _on_icon_pressed() -> void:
	icon_clicked.emit()

func _on_summoner_changed(_old_summoner_id: String, _new_summoner_id: String) -> void:
	refresh()

func _ensure_portrait_material_unique() -> void:
	var material: Material = portrait_texture.material
	if material == null:
		return

	if material.resource_local_to_scene:
		return

	var duplicated_resource: Resource = material.duplicate()
	if not duplicated_resource is Material:
		return

	var duplicated_material: Material = duplicated_resource
	duplicated_material.resource_local_to_scene = true
	portrait_texture.material = duplicated_material

func _apply_portrait_crop(uv_offset: Vector2, uv_scale: Vector2) -> void:
	var material: Material = portrait_texture.material
	if not material is ShaderMaterial:
		return

	var shader_material: ShaderMaterial = material
	shader_material.set_shader_parameter(SHADER_PARAM_UV_OFFSET, uv_offset)
	shader_material.set_shader_parameter(SHADER_PARAM_UV_SCALE, uv_scale)

func _reset_portrait_crop() -> void:
	_apply_portrait_crop(DEFAULT_PORTRAIT_UV_OFFSET, DEFAULT_PORTRAIT_UV_SCALE)
