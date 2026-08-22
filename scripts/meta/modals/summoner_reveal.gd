extends Control
class_name SummonerReveal

## Character-focused confirmation after the starting summoner choice.

const NAV_KEY_SUMMONER_ID: String = "summoner_reveal.summoner_id"
const NAV_KEY_WAS_RANDOM: String = "summoner_reveal.was_random"

@onready var background: ColorRect = %Background
@onready var reveal_content: VBoxContainer = %RevealContent
@onready var title_label: Label = %TitleLabel
@onready var character_placeholder: Label = %CharacterPlaceholder
@onready var element_label: Label = %ElementLabel
@onready var continue_button: Button = %ContinueButton


func _ready() -> void:
	background.color = GameColorPalette.UI_BACKGROUND
	continue_button.text = Loc.t("ui.common.continue")
	continue_button.pressed.connect(_on_continue_pressed)

	var summoner_id: String = SafeTypeUtils.string(
		NavigationContext.consume_value(NAV_KEY_SUMMONER_ID, ""),
		""
	)
	var was_random: bool = SafeTypeUtils.bool_val(
		NavigationContext.consume_value(NAV_KEY_WAS_RANDOM, false),
		false
	)
	if summoner_id.is_empty():
		summoner_id = SummonerSelectionApi.get_active_summoner_id()

	_populate_result(summoner_id, was_random)
	_animate_result()


func _populate_result(summoner_id: String, was_random: bool) -> void:
	var config: SummonerConfig = SummonerConfig.from_dict(
		SummonerCatalogApi.get_summoner(summoner_id)
	)
	if not config:
		push_error("SummonerReveal: Invalid summoner ID '%s'" % summoner_id)
		title_label.text = Loc.t("ui.summoner_reveal.fallback_title")
		element_label.text = ""
		return

	var title_key: String = (
		"ui.summoner_reveal.random_title"
		if was_random
		else "ui.summoner_reveal.chosen_title"
	)
	title_label.text = Loc.t(title_key, {"name": config.summoner_name}).to_upper()
	element_label.text = Loc.t(
		"summoner.element_affinity",
		{"element": ElementTypes.get_display_name(config.get_element())}
	)
	character_placeholder.text = Loc.t("summoner.character_art_placeholder")


func _animate_result() -> void:
	reveal_content.modulate = Color(1, 1, 1, 0)
	reveal_content.scale = Vector2(0.94, 0.94)
	reveal_content.pivot_offset = reveal_content.size / 2.0

	var tween: Tween = create_tween().set_parallel(true)
	tween.set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	tween.tween_property(reveal_content, "modulate", Color.WHITE, 0.45)
	tween.tween_property(reveal_content, "scale", Vector2.ONE, 0.45)


func _on_continue_pressed() -> void:
	SceneManager.transition_to(SceneManager.SCENE_WALKABLE_ACADEMY_HUB)
