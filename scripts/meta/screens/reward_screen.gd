extends Control
class_name RewardScreen

## Normalized battle-reward presentation. Reward identity, available options,
## selections, and claims all come from ProgressionAuthority.

@onready var battle_name_label: Label = %BattleNameLabel
@onready var reward_container: VBoxContainer = %RewardContainer
@onready var first_clear_header: Label = %FirstClearHeader
@onready var reward_card_label: Label = %RewardCardLabel
@onready var reward_detail_label: Label = %RewardDetailLabel
@onready var gold_reward_label: Label = %GoldRewardLabel
@onready var first_clear_status: Label = %FirstClearStatus
@onready var every_battle_section: VBoxContainer = %EveryBattleSection
@onready var choice_container: HBoxContainer = %ChoiceContainer
@onready var continue_button: Button = %ContinueButton

const CHOICE_BUTTON_SIZE: Vector2 = Vector2(150, 100)
const CHOICE_BUTTON_FONT_SIZE: int = 24
const RARITY_COLORS: Dictionary = {
	&"common": Color(0.7, 0.7, 0.7),
	&"rare": Color(0.4, 0.6, 1.0),
	&"epic": Color(0.8, 0.4, 1.0),
	&"legendary": Color(1.0, 0.9, 0.3),
}

var attempt_id: String = ""
var claim_id: String = ""
var options: Array[Dictionary] = []
var selected_option_id: String = ""

func _ready() -> void:
	continue_button.pressed.connect(_on_continue_pressed)
	_load_pending_reward()

func _load_pending_reward() -> void:
	attempt_id = BattleContext.get_battle_attempt_id()
	var result: Dictionary
	if attempt_id.is_empty():
		result = ProgressionAuthority.GetPendingBattleRewards()
	else:
		result = ProgressionAuthority.GetBattleRewards(attempt_id)

	if not result.get("is_success", false):
		push_error("RewardScreen: No durable pending battle reward: %s" % result.get("errors", []))
		_transition_to_map()
		return

	attempt_id = result.get("attempt_id", attempt_id)
	var offers: Array = result.get("reward_offers", [])
	var pending_offer: Dictionary = {}
	for value: Variant in offers:
		if value is Dictionary and value.get("display_state", "") == "pending":
			pending_offer = value
			break

	if pending_offer.is_empty():
		_transition_to_map()
		return

	claim_id = pending_offer.get("claim_id", "")
	options.clear()
	for value: Variant in pending_offer.get("options", []):
		if value is Dictionary:
			options.append(value)

	battle_name_label.text = Loc.t("ui.reward.victory")
	first_clear_header.text = Loc.t("ui.reward.first_clear_header")
	first_clear_status.visible = false
	every_battle_section.visible = false
	gold_reward_label.text = ""

	if options.size() == 1:
		selected_option_id = options[0].get("id", "")
		_display_option(options[0])
		continue_button.disabled = false
	else:
		_show_choices()

func _show_choices() -> void:
	reward_container.visible = false
	choice_container.visible = true
	continue_button.disabled = true
	for child: Node in choice_container.get_children():
		child.queue_free()
	for option: Dictionary in options:
		var button := Button.new()
		button.text = _option_label(option)
		button.custom_minimum_size = CHOICE_BUTTON_SIZE
		button.add_theme_font_size_override("font_size", CHOICE_BUTTON_FONT_SIZE)
		button.pressed.connect(_on_option_selected.bind(option))
		choice_container.add_child(button)

func _on_option_selected(option: Dictionary) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	selected_option_id = option.get("id", "")
	choice_container.visible = false
	reward_container.visible = true
	_display_option(option)
	continue_button.disabled = false

func _display_option(option: Dictionary) -> void:
	reward_card_label.text = _option_label(option)
	reward_detail_label.text = ""
	gold_reward_label.text = ""
	for value: Variant in option.get("grants", []):
		if not value is Dictionary:
			continue
		var grant: Dictionary = value
		if grant.get("kind", "") == "card":
			var rarity := StringName(grant.get("rarity", "common"))
			reward_card_label.add_theme_color_override(
				"font_color", RARITY_COLORS.get(rarity, Color.WHITE)
			)
			reward_detail_label.text = Loc.t(
				"ui.reward.rarity", {"rarity": String(rarity).capitalize()}
			)
		elif grant.get("kind", "") == "resource":
			gold_reward_label.text = Loc.t("ui.reward.gold", {"amount": grant.get("amount", 0)})

func _option_label(option: Dictionary) -> String:
	for value: Variant in option.get("grants", []):
		if value is Dictionary and value.get("kind", "") == "card":
			var card_data: Dictionary = CardCatalogApi.get_card_as_dict(value.get("content_id", ""))
			return card_data.get("card_name", value.get("content_id", Loc.t("ui.common.unknown")))
	var label_key: String = option.get("label_key", "")
	return Loc.t(label_key) if not label_key.is_empty() else Loc.t("ui.reward.victory")

func _on_continue_pressed() -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	if selected_option_id.is_empty():
		return
	var selected: Array[String] = [selected_option_id]
	var result: Dictionary = ProgressionAuthority.ClaimBattleReward(
		attempt_id, claim_id, selected
	)
	if not result.get("is_success", false):
		push_error("RewardScreen: Claim failed: %s" % result.get("errors", []))
		return
	for offer_value: Variant in result.get("reward_offers", []):
		if offer_value is Dictionary and offer_value.get("display_state", "") == "pending":
			selected_option_id = ""
			_load_pending_reward()
			return
	_check_summoner_level_up()

func _check_summoner_level_up() -> void:
	var summoner_id: String = SummonerSelectionApi.get_active_summoner_id()
	if not summoner_id.is_empty() and SummonerProgressionApi.can_level_up(summoner_id):
		var modal: SummonerLevelUpPanel = preload(
			"res://scenes/meta/modals/summoner_level_up_panel.tscn"
		).instantiate() as SummonerLevelUpPanel
		if modal:
			add_child(modal)
			modal.open_for_summoner(summoner_id)
			modal.level_up_completed.connect(_on_summoner_level_up_completed)
			modal.cancelled.connect(_transition_to_map)
			return
	_transition_to_map()

func _on_summoner_level_up_completed(summoner_id: String, _trait_id: String) -> void:
	if SummonerProgressionApi.can_level_up(summoner_id):
		_check_summoner_level_up()
	else:
		_transition_to_map()

func _transition_to_map() -> void:
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)
