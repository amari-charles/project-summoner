extends Control
class_name SummonerSelectionScreen

## SummonerSelectionScreen - Choose your starting summoner
##
## Part of onboarding flow. Player picks one of five summoners representing the
## four core elements (Earth, Fire, Wind, Water) plus a random option.
## Summoner choice is saved to profile via ProfileRepo.set_starting_summoner()

const _DeckConstants: GDScript = preload("res://scripts/data/deck_constants.gd")

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
	print("SummonerSelection: Initializing...")

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
	select_button5.pressed.connect(func() -> void: _on_summoner_selected(SummonerIDs.CELINE))

	# Start Merlin's introduction dialogue
	await get_tree().process_frame
	DialogueManager.dialogue_ended.connect(_on_merlin_dialogue_ended)
	DialogueManager.start_dialogue("merlin_summoner_intro")

func _on_merlin_dialogue_ended() -> void:
	print("SummonerSelection: Merlin dialogue complete, showing summoner selection")
	_show_summoner_selection()

func _show_summoner_selection() -> void:
	select_button1.visible = true
	select_button2.visible = true
	select_button3.visible = true
	select_button4.visible = true
	select_button5.visible = true

func _on_summoner_selected(summoner_id: String) -> void:
	AudioManager.play_ui_sound(AudioManager.SFX_UI_CLICK)
	print("SummonerSelection: Player selected summoner: %s" % summoner_id)

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
	var success: bool = ProfileRepo.set_starting_summoner(final_summoner_id, chosen_random)
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
	var summoner_config: SummonerConfig = SummonerCatalog.get_summoner_config(summoner_id)
	if not summoner_config:
		push_error("SummonerSelection: Failed to get config for summoner '%s'" % summoner_id)
		return

	var starter_card_id: String = summoner_config.starter_card_id

	# Validate starter card ID
	if starter_card_id.is_empty():
		push_error("SummonerSelection: Summoner config has empty starter_card_id!")
		return

	# Grant the starter card to player's collection
	# Pass rarity as String since CardServiceCS expects string, not StringName
	var card_instance_id: String = CardServiceCS.GrantCard(starter_card_id, String(RarityIDs.COMMON))
	if card_instance_id.is_empty():
		push_error("SummonerSelection: Failed to grant starter card '%s'" % starter_card_id)
		return

	# Create Starter Deck with the card
	var card_ids: Array[String] = [card_instance_id]
	var deck_id: String = Decks.create_deck(_DeckConstants.STARTER_DECK_NAME, card_ids, summoner_id)
	if deck_id.is_empty():
		push_error("SummonerSelection: Failed to create Starter Deck")
		return

	# Set as active deck in profile meta
	var profile: Dictionary = ProfileRepo.get_active_profile()
	if not profile.is_empty():
		if not profile.has("meta"):
			profile["meta"] = {}
		profile["meta"]["selected_deck"] = deck_id
		ProfileRepo.save_profile(true)

## Populate button labels dynamically from SummonerCatalog
func _populate_summoner_buttons() -> void:
	# Define button -> summoner mapping
	var button_mappings: Array = [
		{button = select_button1, summoner_id = SummonerIDs.TEO},
		{button = select_button2, summoner_id = SummonerIDs.COLE},
		# button3 is Random - handle separately
		{button = select_button4, summoner_id = SummonerIDs.MEI},
		{button = select_button5, summoner_id = SummonerIDs.CELINE},
	]

	for mapping: Dictionary in button_mappings:
		var config: SummonerConfig = SummonerCatalog.get_summoner_config(mapping.summoner_id)
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
	# Get summoner config
	var summoner_config: SummonerConfig = SummonerCatalog.get_summoner_config(summoner_id)
	if not summoner_config:
		push_error("SummonerSelection: Failed to get SummonerConfig for '%s'" % summoner_id)
		return

	# Create SummonerInstance from config
	var summoner_instance: SummonerInstance = SummonerInstance.new()
	summoner_instance.init_from_config(summoner_config)

	# Grant "Fortune Favors the Bold" trait for random selection
	if chosen_random:
		summoner_instance.add_trait("trait_fortune_favors_the_bold")

	# Save SummonerInstance to profile
	var save_success: bool = ProfileRepo.save_summoner_instance(summoner_instance)
	if save_success:
		print("SummonerSelection: Saved SummonerInstance for '%s' (level %d)" % [
			summoner_id, summoner_instance.level
		])
	else:
		push_error("SummonerSelection: Failed to save SummonerInstance!")
