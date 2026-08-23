extends Control
class_name PostBattleResults

## Canonical presentation for committed battle progression and rewards.
## This screen never grants XP or invents rewards. It reads authoritative
## completion data and only submits an explicit pending reward choice.

const CardVisualScene: PackedScene = preload("res://scenes/shared/card_visual.tscn")
const RESULT_CARD_SIZE: Vector2 = CardVisualHelper.CARD_SIZE_LARGE
const REWARD_CHOICE_BUTTON_SIZE: Vector2 = Vector2(180, 64)
const GRANT_KIND_CARD: String = "card"
const GRANT_KIND_CARD_XP: String = "card_xp"
const GRANT_KIND_SUMMONER_XP: String = "summoner_xp"
const OFFER_STATE_PENDING: String = "pending"
const OUTCOME_DEFEAT: String = "defeat"
const OUTCOME_VICTORY: String = "victory"

@onready var background: ColorRect = %Background
@onready var panel: PanelContainer = %Panel
@onready var results_title_label: Label = %ResultsTitleLabel
@onready var outcome_label: Label = %OutcomeLabel
@onready var progression_heading: Label = %ProgressionHeading
@onready var summoner_name_label: Label = %SummonerNameLabel
@onready var summoner_level_label: Label = %SummonerLevelLabel
@onready var summoner_xp_bar: ProgressBar = %SummonerXPBar
@onready var summoner_xp_label: Label = %SummonerXPLabel
@onready var card_progression_section: VBoxContainer = %CardProgressionSection
@onready var card_progression_heading: Label = %CardProgressionHeading
@onready var card_progression_rows: VBoxContainer = %CardProgressionRows
@onready var rewards_section: VBoxContainer = %RewardsSection
@onready var rewards_heading: Label = %RewardsHeading
@onready var rewards: HFlowContainer = %Rewards
@onready var choice_section: VBoxContainer = %ChoiceSection
@onready var choice_heading: Label = %ChoiceHeading
@onready var choice_buttons: HFlowContainer = %ChoiceButtons
@onready var continue_button: Button = %ContinueButton

var _report: Dictionary = {}
var _encounter_id: String = ""
var _attempt_id: String = ""
var _pending_claim_id: String = ""
var _selected_option_id: String = ""
var _destination: String = ""
var _completion_event_published: bool = false
var _base_reward_grants: Array[Dictionary] = []


func _ready() -> void:
	continue_button.pressed.connect(_continue)
	continue_button.text = Loc.t("ui.common.continue")
	results_title_label.text = Loc.t("ui.post_battle.title")
	progression_heading.text = Loc.t("ui.post_battle.progression")
	card_progression_heading.text = Loc.t("ui.post_battle.participating_cards")
	rewards_heading.text = Loc.t("ui.post_battle.rewards")
	choice_heading.text = Loc.t("ui.post_battle.choose_one")
	_apply_palette()
	_load_authoritative_report()


func present(report: Dictionary) -> void:
	_report = report.duplicate(true)
	_render()


func _load_authoritative_report() -> void:
	if BattleContext.current_mode == BattleContext.BattleMode.CAMPAIGN:
		_load_campaign_report()
	elif BattleContext.current_mode == BattleContext.BattleMode.ENCOUNTER:
		_load_encounter_report()
	else:
		_destination = BattleContext.get_origin_scene()
		present({"outcome": _battle_context_outcome(), "grants": []})


func _load_campaign_report() -> void:
	_attempt_id = BattleContext.get_battle_attempt_id()
	var result: Dictionary = ProgressionAuthority.GetBattleRewards(_attempt_id)
	_present_campaign_result(result)


func _present_campaign_result(result: Dictionary) -> void:
	if not SafeTypeUtils.bool_val(result.get("is_success"), false):
		push_error("PostBattleResults: completion unavailable: %s" % str(result.get("errors", [])))
		SceneManager.transition_to(BattleContext.get_origin_scene())
		return
	_destination = BattleContext.get_origin_scene()
	var grants: Array[Dictionary] = []
	_append_grants(grants, result.get("progression_grants", []))
	var pending_offer: Dictionary = {}
	for value: Variant in SafeTypeUtils.array(result.get("reward_offers")):
		var offer: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(offer.get("display_state")) == OFFER_STATE_PENDING and pending_offer.is_empty():
			pending_offer = offer
			continue
		for option_value: Variant in SafeTypeUtils.array(offer.get("options")):
			var option: Dictionary = SafeTypeUtils.dict(option_value)
			if SafeTypeUtils.bool_val(option.get("is_selected"), false):
				_append_grants(grants, option.get("grants", []))
	_report = {
		"outcome": SafeTypeUtils.string(result.get("outcome"), _battle_context_outcome()),
		"grants": grants,
		"pending_offer": pending_offer,
	}
	present(_report)


func _load_encounter_report() -> void:
	_encounter_id = BattleContext.encounter_id
	var summary: Dictionary = CampaignApi.get_encounter_completion_summary(_encounter_id)
	if summary.is_empty():
		SceneManager.transition_to(SceneManager.SCENE_WALKABLE_ACADEMY_HUB)
		return
	_destination = SceneManager.SCENE_WALKABLE_ACADEMY_HUB
	present({
		"outcome": SafeTypeUtils.string(summary.get("outcome"), _battle_context_outcome()),
		"grants": SafeTypeUtils.array(summary.get("granted_rewards")),
	})
	if not _completion_event_published:
		_completion_event_published = true
		NarrativeDirectorApi.publish_event(
			NarrativeDirectorApi.EventType.ACTIVITY_COMPLETED,
			_encounter_id,
			{"encounter_id": _encounter_id, "outcome": _report.get("outcome", "")}
		)


func _render() -> void:
	_clear_children(card_progression_rows)
	_clear_children(rewards)
	_clear_children(choice_buttons)
	_selected_option_id = ""
	_pending_claim_id = ""
	var outcome: String = SafeTypeUtils.string(_report.get("outcome"), OUTCOME_DEFEAT).to_lower()
	outcome_label.text = Loc.t("ui.post_battle.%s" % outcome)
	outcome_label.add_theme_color_override(
		"font_color", GameColorPalette.SUCCESS if outcome == OUTCOME_VICTORY else GameColorPalette.ERROR
	)
	var grants: Array[Dictionary] = []
	_append_grants(grants, _report.get("grants", []))
	_base_reward_grants.clear()
	for grant: Dictionary in grants:
		if SafeTypeUtils.string(grant.get("kind")) not in [GRANT_KIND_SUMMONER_XP, GRANT_KIND_CARD_XP]:
			_base_reward_grants.append(grant)
	_render_summoner_progression(grants)
	_render_card_progression(grants)
	_render_rewards(grants)
	_render_pending_choice(SafeTypeUtils.dict(_report.get("pending_offer")))


func _render_summoner_progression(grants: Array[Dictionary]) -> void:
	var summoner_id: String = SummonerSelectionApi.get_active_summoner_id()
	var summoner: Dictionary = SummonerSelectionApi.get_summoner_instance_dict(summoner_id)
	var info: Dictionary = SummonerProgressionApi.get_summoner_progression_info(summoner_id)
	var gained: int = _sum_grants(grants, GRANT_KIND_SUMMONER_XP)
	summoner_name_label.text = SafeTypeUtils.string(
		summoner.get("name", summoner.get("summoner_name")),
		Loc.t("ui.post_battle.summoner")
	)
	var level: int = SafeTypeUtils.int_val(info.get("level"), 1)
	summoner_level_label.text = Loc.t("ui.post_battle.level", {"level": level})
	summoner_xp_bar.value = SafeTypeUtils.float_val(info.get("xp_progress"), 0.0) * 100.0
	var current_xp: int = SafeTypeUtils.int_val(info.get("xp"), 0)
	var required_xp: int = SafeTypeUtils.int_val(info.get("xp_for_next_level"), 0)
	summoner_xp_label.text = Loc.t("ui.post_battle.xp_progress", {
		"current": current_xp,
		"required": required_xp,
		"gained": gained,
	})


func _render_card_progression(grants: Array[Dictionary]) -> void:
	var card_xp: Dictionary = {}
	for grant: Dictionary in grants:
		if SafeTypeUtils.string(grant.get("kind")) != GRANT_KIND_CARD_XP:
			continue
		var instance_id: String = SafeTypeUtils.string(grant.get("target_id"))
		var gained: int = SafeTypeUtils.int_val(grant.get("amount"), 0)
		if not instance_id.is_empty() and gained > 0:
			card_xp[instance_id] = SafeTypeUtils.int_val(card_xp.get(instance_id), 0) + gained
	for instance_id: String in card_xp:
		var card: Dictionary = CardServiceApi.get_card_dict(instance_id)
		var info: Dictionary = CardServiceApi.get_card_progression_info_dict(instance_id)
		var catalog_id: String = SafeTypeUtils.string(info.get("catalog_id", card.get("catalog_id")))
		var catalog: Dictionary = CardCatalogApi.get_card_as_dict(catalog_id)
		var row: HBoxContainer = HBoxContainer.new()
		row.add_theme_constant_override("separation", 18)
		var name_label: Label = Label.new()
		name_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		name_label.text = SafeTypeUtils.string(catalog.get("card_name"), catalog_id)
		name_label.add_theme_font_size_override("font_size", 22)
		var level_label: Label = Label.new()
		level_label.text = Loc.t("ui.post_battle.card_progress", {
			"level": SafeTypeUtils.int_val(info.get("level"), 1),
			"gained": card_xp[instance_id],
		})
		level_label.add_theme_font_size_override("font_size", 20)
		level_label.add_theme_color_override("font_color", GameColorPalette.TEXT_HIGHLIGHT)
		row.add_child(name_label)
		row.add_child(level_label)
		card_progression_rows.add_child(row)
	card_progression_section.visible = card_progression_rows.get_child_count() > 0


func _render_rewards(grants: Array[Dictionary]) -> void:
	for grant: Dictionary in grants:
		var kind: String = SafeTypeUtils.string(grant.get("kind"))
		if kind in [GRANT_KIND_SUMMONER_XP, GRANT_KIND_CARD_XP]:
			continue
		_add_reward_view(grant)
	rewards_section.visible = rewards.get_child_count() > 0


func _render_pending_choice(offer: Dictionary) -> void:
	choice_section.visible = not offer.is_empty()
	if offer.is_empty():
		continue_button.disabled = false
		return
	_pending_claim_id = SafeTypeUtils.string(offer.get("claim_id"))
	continue_button.disabled = true
	for value: Variant in SafeTypeUtils.array(offer.get("options")):
		var option: Dictionary = SafeTypeUtils.dict(value)
		var button: Button = Button.new()
		button.custom_minimum_size = REWARD_CHOICE_BUTTON_SIZE
		button.text = _option_label(option)
		button.toggle_mode = true
		button.set_meta("option_id", SafeTypeUtils.string(option.get("id")))
		button.pressed.connect(_select_reward.bind(option))
		choice_buttons.add_child(button)


func _select_reward(option: Dictionary) -> void:
	_selected_option_id = SafeTypeUtils.string(option.get("id"))
	for button: Button in choice_buttons.get_children():
		button.button_pressed = SafeTypeUtils.string(button.get_meta("option_id")) == _selected_option_id
	_clear_children(rewards)
	var selected_grants: Array[Dictionary] = []
	_append_grants(selected_grants, option.get("grants", []))
	var displayed_grants: Array[Dictionary] = _base_reward_grants.duplicate(true)
	displayed_grants.append_array(selected_grants)
	_render_rewards(displayed_grants)
	continue_button.disabled = false


func _add_reward_view(grant: Dictionary) -> void:
	var kind: String = SafeTypeUtils.string(grant.get("kind"))
	if kind == GRANT_KIND_CARD:
		var card_id: String = SafeTypeUtils.string(grant.get("card_id", grant.get("content_id", grant.get("id"))))
		var card_visual: CardVisual = CardVisualScene.instantiate() as CardVisual
		card_visual.set_display_size(RESULT_CARD_SIZE)
		card_visual.show_description = true
		card_visual.cost_font_size = 30
		card_visual.name_font_size = 17
		card_visual.description_font_size = 11
		card_visual.set_card_data(CardCatalogApi.get_card_as_dict(card_id), true)
		rewards.add_child(card_visual)
		return
	var label: Label = Label.new()
	var reward_id: String = SafeTypeUtils.string(grant.get("content_id", grant.get("id", kind)))
	var amount: int = SafeTypeUtils.int_val(grant.get("amount"), 1)
	label.text = Loc.t("ui.post_battle.reward_amount", {
		"name": reward_id.replace("_", " ").capitalize(),
		"amount": amount,
	})
	label.custom_minimum_size = Vector2(170, 80)
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.add_theme_font_size_override("font_size", 22)
	rewards.add_child(label)


func _option_label(option: Dictionary) -> String:
	var key: String = SafeTypeUtils.string(option.get("label_key"))
	if not key.is_empty():
		return Loc.t(key)
	for value: Variant in SafeTypeUtils.array(option.get("grants")):
		var grant: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(grant.get("kind")) == GRANT_KIND_CARD:
			var card_id: String = SafeTypeUtils.string(grant.get("content_id", grant.get("id")))
			return SafeTypeUtils.string(CardCatalogApi.get_card_as_dict(card_id).get("card_name"), card_id)
	return Loc.t("ui.post_battle.choose_reward")


func _continue() -> void:
	if not _pending_claim_id.is_empty():
		if _selected_option_id.is_empty():
			return
		var result: Dictionary = ProgressionAuthority.ClaimBattleReward(
			_attempt_id, _pending_claim_id, [_selected_option_id]
		)
		if not SafeTypeUtils.bool_val(result.get("is_success"), false):
			push_error("PostBattleResults: reward claim failed: %s" % str(result.get("errors", [])))
			return
		if _has_pending_offer(result):
			_present_campaign_result(result)
			return
	if not _encounter_id.is_empty():
		CampaignApi.consume_encounter_completion_summary(_encounter_id)
	SceneManager.transition_to(_destination)


func _battle_context_outcome() -> String:
	return OUTCOME_VICTORY if BattleContext.battle_state == BattleContext.BattleState.VICTORY else OUTCOME_DEFEAT


func _sum_grants(grants: Array[Dictionary], kind: String) -> int:
	var total: int = 0
	for grant: Dictionary in grants:
		if SafeTypeUtils.string(grant.get("kind")) == kind:
			total += SafeTypeUtils.int_val(grant.get("amount"), 0)
	return total


func _has_pending_offer(result: Dictionary) -> bool:
	for value: Variant in SafeTypeUtils.array(result.get("reward_offers")):
		var offer: Dictionary = SafeTypeUtils.dict(value)
		if SafeTypeUtils.string(offer.get("display_state")) == OFFER_STATE_PENDING:
			return true
	return false


func _append_grants(target: Array[Dictionary], source: Variant) -> void:
	for value: Variant in SafeTypeUtils.array(source):
		var grant: Dictionary = SafeTypeUtils.dict(value)
		if not grant.is_empty():
			target.append(grant)


func _clear_children(container: Node) -> void:
	for child: Node in container.get_children():
		container.remove_child(child)
		child.queue_free()


func _apply_palette() -> void:
	background.color = GameColorPalette.UI_BACKGROUND
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = GameColorPalette.UI_SURFACE
	style.border_color = GameColorPalette.UI_BORDER_STRONG
	style.set_border_width_all(2)
	style.set_corner_radius_all(14)
	panel.add_theme_stylebox_override("panel", style)
