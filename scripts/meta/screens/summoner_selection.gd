extends Control
class_name SummonerSelectionScreen

## SummonerSelectionScreen - Choose your starting summoner
##
## Part of onboarding flow. Player picks one of five summoners representing the
## four core elements (Earth, Fire, Wind, Water) plus a random option.
## Summoner choice is saved to profile via SummonerSelectionApi.set_starting_summoner()

const _DeckConstants: GDScript = preload("res://scripts/infrastructure/data/deck_constants.gd")

## Trait granted when player chooses the random summoner option
const RANDOM_SUMMONER_TRAIT_ID: String = "trait_fortune_favors_the_bold"
const REVIEW_STARTER_ITEM_ID: String = "item_training_blade"

@onready var title_label: Label = $CenterContainer/VBoxContainer/TitleLabel
@onready var select_button1: Button = %SelectButton1
@onready var select_button2: Button = %SelectButton2
@onready var select_button3: Button = %SelectButton3
@onready var select_button4: Button = %SelectButton4
@onready var select_button5: Button = %SelectButton5

# Special constant for random selection (not a real summoner)
const SUMMONER_RANDOM: String = "random"

func _ready() -> void:

	# Set localized title
	title_label.text = Loc.t("summoner.selection_title").to_upper()

	# Hide summoner buttons initially - will show after Merlin's dialogue
	select_button1.visible = false
	select_button2.visible = false
	select_button3.visible = false
	select_button4.visible = false
	select_button5.visible = false

	# Populate button labels from SummonerCatalog
	_populate_summoner_buttons()

	# Connect all summoner selection buttons
	select_button1.pressed.connect(func() -> void: _on_summoner_selected(SummonerIDs.TEO))
	select_button2.pressed.connect(func() -> void: _on_summoner_selected(SummonerIDs.COLE))
	select_button3.pressed.connect(func() -> void: _on_summoner_selected(SUMMONER_RANDOM))
	select_button4.pressed.connect(func() -> void: _on_summoner_selected(SummonerIDs.MEI))
	select_button5.pressed.connect(func() -> void: _on_summoner_selected(SummonerIDs.SELENE))

	# Start Merlin's introduction dialogue
	await get_tree().process_frame
	var director: Node = NarrativeDirectorApi.node()
	if not director.is_connected("CueCompleted", _on_narrative_cue_completed):
		director.connect("CueCompleted", _on_narrative_cue_completed)
	NarrativeDirectorApi.publish_event(
		NarrativeDirectorApi.EventType.META_MOMENT_STARTED,
		"onboarding.summoner_selection"
	)
	if not NarrativeDirectorApi.is_cue_active_or_queued("onboarding_summoner_intro"):
		_show_summoner_selection()

func _on_narrative_cue_completed(cue_id: String) -> void:
	if cue_id == "onboarding_summoner_intro":
		_show_summoner_selection()

func _show_summoner_selection() -> void:
	select_button1.visible = true
	select_button2.visible = true
	select_button3.visible = true
	select_button4.visible = true
	select_button5.visible = true

func _on_summoner_selected(summoner_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)

	# Handle random selection
	var final_summoner_id: String = summoner_id
	var chosen_random: bool = false
	if summoner_id == SUMMONER_RANDOM:
		chosen_random = true
		# Pick random summoner from starting pool
		var random_pool: Array[StringName] = SummonerIDs.ALL_STARTING
		final_summoner_id = random_pool[randi() % random_pool.size()]
		print("SummonerSelection: Random selection chose: %s" % final_summoner_id)

	# Save summoner choice to profile
	var success: bool = SummonerSelectionApi.set_starting_summoner(final_summoner_id, chosen_random)
	if success:
		print("SummonerSelection: Successfully set starting summoner: %s (random: %s)" % [final_summoner_id, chosen_random])
	else:
		push_error("SummonerSelection: Failed to set starting summoner!")

	# Create and save SummonerInstance with proper modifiers
	_create_summoner_instance(final_summoner_id, chosen_random)

	# Create starter deck with summoner's starter card
	_create_starter_deck(final_summoner_id)

	# Give the showcase flow one real item so Inventory, item details, and
	# equipment are meaningful on a brand-new review profile.
	ItemsApi.grant_item_to_summoner(REVIEW_STARTER_ITEM_ID, final_summoner_id)

	# Preserve the exact result for the character-focused confirmation screen.
	NavigationContext.set_value(
		SummonerReveal.NAV_KEY_REVEAL_RESULT,
		{
			"summoner_id": final_summoner_id,
			"was_random": chosen_random,
		}
	)
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_REVEAL)

## Create starter deck with summoner's starter card
func _create_starter_deck(summoner_id: String) -> void:
	# Get summoner config to find starter card
	var summoner_config: SummonerConfig = SummonerConfig.from_dict(SummonerCatalogApi.get_summoner(summoner_id))
	if not summoner_config:
		push_error("SummonerSelection: Failed to get config for summoner '%s'" % summoner_id)
		return

	var starter_card_id: String = summoner_config.starter_card_id

	# Validate starter card ID
	if starter_card_id.is_empty():
		push_error("SummonerSelection: Summoner config has empty starter_card_id!")
		return

	# Grant the starter card to player's collection
	# Pass rarity as String since CardService expects string, not StringName
	var card_instance_id: String = CardServiceApi.grant_card(starter_card_id, String(RarityIDs.COMMON))
	if card_instance_id.is_empty():
		push_error("SummonerSelection: Failed to grant starter card '%s'" % starter_card_id)
		return

	# The review tour includes the real trait tree and confirmation before its
	# first battle. Start at the first upgrade tier with one legitimate choice.
	ProfileRepoApi.update_card_from_dict(card_instance_id, {
		"level": 2,
		"unspent_trait_points": 1,
	})

	# Create Starter Deck with the card
	var card_ids: Array[String] = [card_instance_id]
	var deck_id: String = DecksApi.create_deck_from_dict(_DeckConstants.STARTER_DECK_NAME, card_ids, summoner_id)
	if deck_id.is_empty():
		push_error("SummonerSelection: Failed to create Starter Deck")
		return

	# Set as active deck in profile meta
	ProfileRepoApi.update_profile_meta_dict({"selected_deck": deck_id})

## Populate button labels dynamically from SummonerCatalog
func _populate_summoner_buttons() -> void:
	# Define button -> summoner mapping
	var button_mappings: Array = [
		{button = select_button1, summoner_id = SummonerIDs.TEO},
		{button = select_button2, summoner_id = SummonerIDs.COLE},
		# button3 is Random - handle separately
		{button = select_button4, summoner_id = SummonerIDs.MEI},
		{button = select_button5, summoner_id = SummonerIDs.SELENE},
	]

	for mapping: Dictionary in button_mappings:
		var config: SummonerConfig = SummonerConfig.from_dict(SummonerCatalogApi.get_summoner(mapping.summoner_id))
		if config:
			_set_button_content(mapping.button, config)

	# Handle Random button separately
	_set_random_button_content(select_button3)


func _set_button_content(button: Button, config: SummonerConfig) -> void:
	var name_label: Label = button.find_child("SummonerName", true, false)
	var element_label: Label = button.find_child("SummonerElement", true, false)
	var placeholder_label: Label = button.find_child("CharacterPlaceholder", true, false)

	if name_label:
		name_label.text = config.summoner_name.to_upper()
	if element_label:
		element_label.text = Loc.t("summoner.element_affinity", {"element": ElementTypes.get_display_name(config.get_element())})
	if placeholder_label:
		placeholder_label.text = Loc.t("summoner.character_art_placeholder")


func _set_random_button_content(button: Button) -> void:
	var name_label: Label = button.find_child("SummonerName", true, false)
	var element_label: Label = button.find_child("SummonerElement", true, false)
	var placeholder_label: Label = button.find_child("CharacterPlaceholder", true, false)

	if name_label:
		name_label.text = Loc.t("summoner.random_summoner").to_upper()
	if element_label:
		element_label.text = Loc.t("summoner.element_affinity_unknown")
	if placeholder_label:
		placeholder_label.text = Loc.t("summoner.character_art_placeholder")


## Create and save SummonerInstance for the selected summoner
func _create_summoner_instance(summoner_id: String, chosen_random: bool) -> void:
	# Build summoner instance data directly
	var acquired_traits: Array = []
	if chosen_random:
		acquired_traits.append(RANDOM_SUMMONER_TRAIT_ID)

	var summoner_data: Dictionary = {
		"summoner_id": summoner_id,
		"level": 1,
		"xp": 0,
		"acquired_trait_ids": acquired_traits
	}

	# Save to profile via C# service
	var save_success: bool = SummonerSelectionApi.save_summoner_instance_dict(summoner_data)
	if save_success:
		print("SummonerSelection: Saved SummonerInstance for '%s' (level 1)" % summoner_id)
	else:
		push_error("SummonerSelection: Failed to save SummonerInstance!")
