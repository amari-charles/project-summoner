extends Control
class_name HeroSelection

## HeroSelection - Choose your starting hero
##
## Part of onboarding flow. Player picks one of five heroes representing the
## four core elements (Earth, Fire, Wind, Water) plus a random option.
## Hero choice is saved to profile via ProfileRepo.set_starting_hero()
## If random is chosen, grants "Fortune Favors the Bold" trait.

@onready var select_button1: Button = %SelectButton1
@onready var select_button2: Button = %SelectButton2
@onready var select_button3: Button = %SelectButton3
@onready var select_button4: Button = %SelectButton4
@onready var select_button5: Button = %SelectButton5

var dialogue_manager: Node = null

# Core elemental heroes (match HeroCatalog hero_ids)
const HERO_EARTH: String = "hero_earth"
const HERO_FIRE: String = "hero_fire"
const HERO_RANDOM: String = "random"
const HERO_WIND: String = "hero_wind"
const HERO_WATER: String = "hero_water"

func _ready() -> void:
	print("HeroSelection: Initializing...")

	# Hide hero buttons initially - will show after Merlin's dialogue
	select_button1.visible = false
	select_button2.visible = false
	select_button3.visible = false
	select_button4.visible = false
	select_button5.visible = false

	# Connect all hero selection buttons
	select_button1.pressed.connect(func() -> void: _on_hero_selected(HERO_EARTH))
	select_button2.pressed.connect(func() -> void: _on_hero_selected(HERO_FIRE))
	select_button3.pressed.connect(func() -> void: _on_hero_selected(HERO_RANDOM))
	select_button4.pressed.connect(func() -> void: _on_hero_selected(HERO_WIND))
	select_button5.pressed.connect(func() -> void: _on_hero_selected(HERO_WATER))

	# Start Merlin's introduction dialogue
	await get_tree().process_frame
	dialogue_manager = get_node_or_null("/root/DialogueManager")
	if dialogue_manager and dialogue_manager.has_signal("dialogue_ended") and dialogue_manager.has_method("start_dialogue"):
		dialogue_manager.connect("dialogue_ended", _on_merlin_dialogue_ended)
		dialogue_manager.call("start_dialogue", "merlin_hero_intro")
	else:
		# Fallback if dialogue system unavailable
		print("HeroSelection: DialogueManager not found, showing hero selection immediately")
		_show_hero_selection()

func _on_merlin_dialogue_ended() -> void:
	print("HeroSelection: Merlin dialogue complete, showing hero selection")
	_show_hero_selection()

func _show_hero_selection() -> void:
	select_button1.visible = true
	select_button2.visible = true
	select_button3.visible = true
	select_button4.visible = true
	select_button5.visible = true

func _on_hero_selected(hero_id: String) -> void:
	print("HeroSelection: Player selected hero: %s" % hero_id)

	# Handle random selection
	var final_hero_id: String = hero_id
	var chosen_random: bool = false
	if hero_id == HERO_RANDOM:
		chosen_random = true
		# Pick random hero from starting pool
		var random_pool: Array = [HERO_EARTH, HERO_FIRE, HERO_WIND, HERO_WATER]
		final_hero_id = random_pool[randi() % random_pool.size()]
		print("HeroSelection: Random selection chose: %s" % final_hero_id)

	# Save hero choice to profile using new ProfileRepo method
	var profile_repo: Node = get_node("/root/ProfileRepo")
	if profile_repo and profile_repo.has_method("set_starting_hero"):
		var success: bool = profile_repo.call("set_starting_hero", final_hero_id, chosen_random)
		if success:
			print("HeroSelection: Successfully set starting hero: %s (random: %s)" % [final_hero_id, chosen_random])
		else:
			push_error("HeroSelection: Failed to set starting hero!")
	else:
		push_error("HeroSelection: ProfileRepo.set_starting_hero() not available!")

	# Update starter deck with selected hero
	_assign_hero_to_starter_deck(final_hero_id)

	# Mark affinity selection event as completed
	var campaign: Node = get_node("/root/Campaign")
	if campaign and campaign.has_method("complete_battle"):
		campaign.call("complete_battle", "event_affinity")
		print("HeroSelection: Marked affinity selection as completed!")

	# Transition to reveal scene (hero data already saved in ProfileRepo)
	SceneManager.change_scene(SceneManager.SCENE_HERO_REVEAL)

## Assign the selected hero to the starter deck
func _assign_hero_to_starter_deck(hero_id: String) -> void:
	print("HeroSelection: Attempting to assign hero '%s' to Starter Deck" % hero_id)
	var decks: Node = get_node_or_null("/root/Decks")
	if not decks:
		push_error("HeroSelection: Decks service not found!")
		return

	# Find the "Starter Deck"
	const STARTER_DECK_NAME: String = "Starter Deck"
	var deck_list: Array = decks.call("list_decks")
	print("HeroSelection: Found %d decks" % deck_list.size())
	var starter_deck_id: String = ""

	for deck: Variant in deck_list:
		if deck is Dictionary:
			print("HeroSelection: Checking deck '%s'" % deck.get("name", ""))
			if deck.get("name", "") == STARTER_DECK_NAME:
				starter_deck_id = deck.get("id", "")
				print("HeroSelection: Found Starter Deck with ID: %s" % starter_deck_id)
				break

	if starter_deck_id.is_empty():
		push_warning("HeroSelection: Starter Deck not found, hero will be assigned when deck is created")
		return

	# Update the deck with the hero_id
	if decks.has_method("set_deck_hero"):
		print("HeroSelection: Calling set_deck_hero('%s', '%s')" % [starter_deck_id, hero_id])
		var success: bool = decks.call("set_deck_hero", starter_deck_id, hero_id)
		if success:
			print("HeroSelection: Successfully assigned hero '%s' to Starter Deck" % hero_id)
		else:
			push_error("HeroSelection: Failed to assign hero to Starter Deck")
	else:
		push_error("HeroSelection: set_deck_hero method not available!")
