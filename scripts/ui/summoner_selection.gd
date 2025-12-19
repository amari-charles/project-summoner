extends Control
class_name SummonerSelectionScreen

## SummonerSelectionScreen - Choose your starting summoner
##
## Part of onboarding flow. Player picks one of five summoners representing the
## four core elements (Earth, Fire, Wind, Water) plus a random option.
## Summoner choice is saved to profile via ProfileRepo.set_starting_summoner()
## If random is chosen, grants "Fortune Favors the Bold" trait.

@onready var select_button1: Button = %SelectButton1
@onready var select_button2: Button = %SelectButton2
@onready var select_button3: Button = %SelectButton3
@onready var select_button4: Button = %SelectButton4
@onready var select_button5: Button = %SelectButton5

var dialogue_manager: Node = null

# Special constant for random selection (not a real summoner)
const SUMMONER_RANDOM: String = "random"

func _ready() -> void:
	print("SummonerSelection: Initializing...")

	# Hide summoner buttons initially - will show after Merlin's dialogue
	select_button1.visible = false
	select_button2.visible = false
	select_button3.visible = false
	select_button4.visible = false
	select_button5.visible = false

	# Connect all summoner selection buttons
	select_button1.pressed.connect(func() -> void: _on_summoner_selected(SummonerIDs.EARTH))
	select_button2.pressed.connect(func() -> void: _on_summoner_selected(SummonerIDs.FIRE))
	select_button3.pressed.connect(func() -> void: _on_summoner_selected(SUMMONER_RANDOM))
	select_button4.pressed.connect(func() -> void: _on_summoner_selected(SummonerIDs.WIND))
	select_button5.pressed.connect(func() -> void: _on_summoner_selected(SummonerIDs.WATER))

	# Start Merlin's introduction dialogue
	await get_tree().process_frame
	dialogue_manager = get_node_or_null("/root/DialogueManager")
	if dialogue_manager and dialogue_manager.has_signal("dialogue_ended") and dialogue_manager.has_method("start_dialogue"):
		dialogue_manager.connect("dialogue_ended", _on_merlin_dialogue_ended)
		dialogue_manager.call("start_dialogue", "merlin_summoner_intro")
	else:
		# Fallback if dialogue system unavailable
		print("SummonerSelection: DialogueManager not found, showing summoner selection immediately")
		_show_summoner_selection()

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
	AudioManager.play_ui_sound(AudioManager.UI_CLICK)
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

	# Save summoner choice to profile using new ProfileRepo method
	var profile_repo: Node = get_node("/root/ProfileRepo")
	if profile_repo and profile_repo.has_method("set_starting_summoner"):
		var success: bool = profile_repo.call("set_starting_summoner", final_summoner_id, chosen_random)
		if success:
			print("SummonerSelection: Successfully set starting summoner: %s (random: %s)" % [final_summoner_id, chosen_random])
		else:
			push_error("SummonerSelection: Failed to set starting summoner!")
	else:
		push_error("SummonerSelection: ProfileRepo.set_starting_summoner() not available!")

	# Create and save SummonerInstance with proper modifiers
	_create_summoner_instance(final_summoner_id, chosen_random, profile_repo)

	# Update starter deck with selected summoner
	_assign_summoner_to_starter_deck(final_summoner_id)

	# Mark affinity selection event as completed
	var campaign: Node = get_node("/root/Campaign")
	if campaign and campaign.has_method("complete_battle"):
		campaign.call("complete_battle", BattleIDs.EVENT_AFFINITY)
		print("SummonerSelection: Marked affinity selection as completed!")

	# Transition to reveal scene (summoner data already saved in ProfileRepo)
	SceneManager.transition_to(SceneManager.SCENE_SUMMONER_REVEAL)

## Assign the selected summoner to the starter deck
func _assign_summoner_to_starter_deck(summoner_id: String) -> void:
	print("SummonerSelection: Attempting to assign summoner '%s' to Starter Deck" % summoner_id)
	var decks: Node = get_node_or_null("/root/Decks")
	if not decks:
		push_error("SummonerSelection: Decks service not found!")
		return

	# Find the "Starter Deck"
	const STARTER_DECK_NAME: String = "Starter Deck"
	var deck_list: Array = decks.call("list_decks")
	print("SummonerSelection: Found %d decks" % deck_list.size())
	var starter_deck_id: String = ""

	for deck: Variant in deck_list:
		if deck is Dictionary:
			print("SummonerSelection: Checking deck '%s'" % deck.get("name", ""))
			if deck.get("name", "") == STARTER_DECK_NAME:
				starter_deck_id = deck.get("id", "")
				print("SummonerSelection: Found Starter Deck with ID: %s" % starter_deck_id)
				break

	if starter_deck_id.is_empty():
		push_warning("SummonerSelection: Starter Deck not found, summoner will be assigned when deck is created")
		return

	# Update the deck with the summoner_id
	if decks.has_method("set_deck_summoner"):
		print("SummonerSelection: Calling set_deck_summoner('%s', '%s')" % [starter_deck_id, summoner_id])
		var success: bool = decks.call("set_deck_summoner", starter_deck_id, summoner_id)
		if success:
			print("SummonerSelection: Successfully assigned summoner '%s' to Starter Deck" % summoner_id)
		else:
			push_error("SummonerSelection: Failed to assign summoner to Starter Deck")
	else:
		push_error("SummonerSelection: set_deck_summoner method not available!")

## Create and save SummonerInstance for the selected summoner
func _create_summoner_instance(summoner_id: String, chosen_random: bool, profile_repo: Node) -> void:
	var summoner_catalog: Node = get_node_or_null("/root/SummonerCatalog")
	if not summoner_catalog:
		push_error("SummonerSelection: SummonerCatalog not found!")
		return

	# Get summoner config
	var summoner_config_variant: Variant = null
	if summoner_catalog.has_method("get_summoner_config"):
		summoner_config_variant = summoner_catalog.call("get_summoner_config", summoner_id)

	if not summoner_config_variant is SummonerConfig:
		push_error("SummonerSelection: Failed to get SummonerConfig for '%s'" % summoner_id)
		return

	var summoner_config: SummonerConfig = summoner_config_variant

	# Create SummonerInstance from config
	var summoner_instance: SummonerInstance = SummonerInstance.new()
	summoner_instance.init_from_config(summoner_config)

	# Add "Fortune Favors the Bold" trait if player chose random
	if chosen_random:
		if summoner_instance.add_boon("trait_fortune_favors_bold"):
			print("SummonerSelection: Added 'Fortune Favors the Bold' trait for random selection")
		else:
			push_warning("SummonerSelection: Failed to add fortune_favors_bold trait")

	# Save SummonerInstance to profile
	if profile_repo and profile_repo.has_method("save_summoner_instance"):
		var success: bool = profile_repo.call("save_summoner_instance", summoner_instance)
		if success:
			print("SummonerSelection: Saved SummonerInstance for '%s' (level %d, %d boons)" % [
				summoner_id, summoner_instance.level, summoner_instance.acquired_boon_ids.size()
			])
		else:
			push_error("SummonerSelection: Failed to save SummonerInstance!")
	else:
		push_error("SummonerSelection: ProfileRepo.save_summoner_instance() not available!")
