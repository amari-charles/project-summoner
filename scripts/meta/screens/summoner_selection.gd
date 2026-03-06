extends Control
class_name SummonerSelectionScreen

## SummonerSelectionScreen - Choose your starting summoner
##
## Part of onboarding flow. Player picks one of five summoners representing the
## four core elements (Earth, Fire, Wind, Water) plus a random option.
## Summoner choice is saved to profile via SummonerSelection.SetStartingSummoner()

const _DeckConstants: GDScript = preload("res://scripts/infrastructure/data/deck_constants.gd")

## Trait granted when player chooses the random summoner option
const RANDOM_SUMMONER_TRAIT_ID: String = "trait_fortune_favors_the_bold"

@onready var title_label: Label = $CenterContainer/VBoxContainer/TitleLabel
@onready var subtitle_label: Label = $CenterContainer/VBoxContainer/SubtitleLabel
@onready var select_button1: Button = %SelectButton1
@onready var select_button2: Button = %SelectButton2
@onready var select_button3: Button = %SelectButton3
@onready var select_button4: Button = %SelectButton4
@onready var select_button5: Button = %SelectButton5

# Special constant for random selection (not a real summoner)
const SUMMONER_RANDOM: String = "random"

func _ready() -> void:

	# Set localized title and subtitle
	title_label.text = Loc.t("summoner.selection_title").to_upper()
	subtitle_label.text = Loc.t("summoner.selection_subtitle")

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
	DialogueManager.dialogue_ended.connect(_on_merlin_dialogue_ended)
	DialogueManager.start_dialogue("merlin_summoner_intro")

func _on_merlin_dialogue_ended() -> void:
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
	var success: bool = SummonerSelection.SetStartingSummoner(final_summoner_id, chosen_random)
	if success:
		print("SummonerSelection: Successfully set starting summoner: %s (random: %s)" % [final_summoner_id, chosen_random])
	else:
		push_error("SummonerSelection: Failed to set starting summoner!")

	# Create and save SummonerInstance with proper modifiers
	_create_summoner_instance(final_summoner_id, chosen_random)

	# Create starter deck with summoner's starter card
	_create_starter_deck(final_summoner_id)

	# Transition to reveal scene (summoner data already saved in ProfileRepo)
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_REVEAL)

## Create starter deck with summoner's starter card
func _create_starter_deck(summoner_id: String) -> void:
	# Get summoner config to find starter card
	var summoner_config: SummonerConfig = SummonerConfig.from_dict(SummonerCatalog.GetSummoner(summoner_id))
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
	var card_instance_id: String = CardService.GrantCard(starter_card_id, String(RarityIDs.COMMON))
	if card_instance_id.is_empty():
		push_error("SummonerSelection: Failed to grant starter card '%s'" % starter_card_id)
		return

	# Create Starter Deck with the card
	var card_ids: Array[String] = [card_instance_id]
	var deck_id: String = Decks.CreateDeckFromDict(_DeckConstants.STARTER_DECK_NAME, card_ids, summoner_id)
	if deck_id.is_empty():
		push_error("SummonerSelection: Failed to create Starter Deck")
		return

	# Set as active deck in profile meta
	ProfileRepo.UpdateProfileMetaDict({"selected_deck": deck_id})

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
		var config: SummonerConfig = SummonerConfig.from_dict(SummonerCatalog.GetSummoner(mapping.summoner_id))
		if config:
			_set_button_content(mapping.button, config)

	# Handle Random button separately
	_set_random_button_content(select_button3)


func _set_button_content(button: Button, config: SummonerConfig) -> void:
	var name_label: Label = button.find_child("SummonerName", true, false)
	var element_label: Label = button.find_child("SummonerElement", true, false)
	var desc_label: Label = button.find_child("SummonerDescription", true, false)

	if name_label:
		name_label.text = config.summoner_name.to_upper()
	if element_label:
		element_label.text = Loc.t("summoner.element_affinity", {"element": ElementTypes.get_display_name(config.get_element())})
	if desc_label:
		desc_label.text = config.description


func _set_random_button_content(button: Button) -> void:
	var name_label: Label = button.find_child("SummonerName", true, false)
	var element_label: Label = button.find_child("SummonerElement", true, false)
	var desc_label: Label = button.find_child("SummonerDescription", true, false)

	if name_label:
		name_label.text = Loc.t("summoner.random_summoner").to_upper()
	if element_label:
		element_label.text = Loc.t("summoner.element_affinity_unknown")
	if desc_label:
		desc_label.text = Loc.t("summoner.random_summoner_description")


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
	var save_success: bool = SummonerSelection.SaveSummonerInstanceDict(summoner_data)
	if save_success:
		print("SummonerSelection: Saved SummonerInstance for '%s' (level 1)" % summoner_id)
	else:
		push_error("SummonerSelection: Failed to save SummonerInstance!")
