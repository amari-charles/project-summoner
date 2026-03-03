extends Control
class_name FirstCardSelection

## FirstCardSelection - Choose your first card
##
## Part of onboarding flow. Player picks Fire Wisp or Pebbloom as their starter.
## Card is granted to collection and onboarding is marked complete.

const _DeckConstants: GDScript = preload("res://scripts/data/deck_constants.gd")

@onready var select_fire_wisp_button: Button = %SelectFireWispButton
@onready var select_earth_sprite_button: Button = %SelectEarthSpriteButton

func _ready() -> void:
	print("FirstCardSelection: Initializing...")

	# Connect button handlers
	select_fire_wisp_button.pressed.connect(_on_card_selected.bind(CardIDs.FIRE_WISP))
	select_earth_sprite_button.pressed.connect(_on_card_selected.bind(CardIDs.PEBBLOOM))

	# Start Merlin's introduction dialogue (buttons visible alongside dialogue)
	await get_tree().process_frame
	if DialogueManager.has_method("start_dialogue"):
		DialogueManager.call("start_dialogue", "merlin_first_card_intro")
	else:
		print("FirstCardSelection: DialogueManager.start_dialogue not found")

func _on_card_selected(catalog_id: StringName) -> void:
	print("FirstCardSelection: Player selected card: %s" % catalog_id)

	# Grant the chosen card to collection
	# Pass rarity as String since CardServiceCS expects string, not StringName
	var card_instance_id: String = CardServiceCS.GrantCard(catalog_id, String(RarityIDs.COMMON))
	print("FirstCardSelection: Granted %s to collection (instance: %s)" % [catalog_id, card_instance_id])

	# Find or create Starter Deck
	var deck_id: String = ""

	if card_instance_id != "":
		# Search for existing Starter Deck
		if Decks.has_method("ListDecksDict"):
			var all_decks: Array[Dictionary] = Decks.call("ListDecksDict")
			for deck_dict: Dictionary in all_decks:
				if deck_dict.get("name", "") == _DeckConstants.STARTER_DECK_NAME:
					deck_id = deck_dict.get("id", "")
					print("FirstCardSelection: Found existing Starter Deck (id: %s)" % deck_id)
					break

		# If deck exists, add card to it; otherwise create new deck
		if deck_id != "":
			if Decks.has_method("AddCardToDeck"):
				Decks.call("AddCardToDeck", deck_id, card_instance_id)
				print("FirstCardSelection: Added card to existing Starter Deck")
		else:
			# Get the player's unlocked summoner to assign to the deck
			var summoner_id: String = _get_first_unlocked_summoner()
			if Decks.has_method("CreateDeckFromDict"):
				var result: Variant = Decks.call("CreateDeckFromDict", _DeckConstants.STARTER_DECK_NAME, [card_instance_id], summoner_id)
				deck_id = result if result is String else ""
				print("FirstCardSelection: Created new Starter Deck (id: %s) with summoner '%s'" % [deck_id, summoner_id])

		# Set as active deck
		if deck_id != "":
			if ProfileRepo.has_method("get_active_profile"):
				var profile_variant: Variant = ProfileRepo.call("get_active_profile")
				var profile: Dictionary = profile_variant if profile_variant is Dictionary else {}
				if not profile.is_empty():
					profile["meta"]["selected_deck"] = deck_id
					if ProfileRepo.has_method("save_profile"):
						ProfileRepo.call("save_profile", true)  # Force immediate save
					print("FirstCardSelection: Set Starter Deck as active!")

	# Return to campaign map
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

## Get the first unlocked summoner from the profile
func _get_first_unlocked_summoner() -> String:
	var unlocked = SummonerSelection.GetUnlockedSummonerIdsArray()
	if unlocked.size() > 0:
		return unlocked[0]
	return ""
