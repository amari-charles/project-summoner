extends Control
class_name FirstCardSelection

## FirstCardSelection - Choose your first card
##
## Part of onboarding flow. Player picks Fire Wisp or Pebbloom as their starter.
## Card is granted to collection and onboarding is marked complete.

const _DeckConstants: GDScript = preload("res://scripts/infrastructure/data/deck_constants.gd")

@onready var select_fire_wisp_button: Button = %SelectFireWispButton
@onready var select_earth_sprite_button: Button = %SelectEarthSpriteButton

func _ready() -> void:
	# Connect button handlers
	select_fire_wisp_button.pressed.connect(_on_card_selected.bind(CardIDs.FIRE_WISP))
	select_earth_sprite_button.pressed.connect(_on_card_selected.bind(CardIDs.PEBBLOOM))

	# Start Merlin's introduction dialogue (buttons visible alongside dialogue)
	await get_tree().process_frame
	if DialogueManager.has_method("start_dialogue"):
		DialogueManager.call("start_dialogue", "merlin_first_card_intro")

func _on_card_selected(catalog_id: StringName) -> void:
	# Grant the chosen card to collection
	# Pass rarity as String since CardService expects string, not StringName
	var card_instance_id: String = CardService.GrantCard(catalog_id, String(RarityIDs.COMMON))

	# Find or create Starter Deck
	var deck_id: String = ""

	if card_instance_id != "":
		# Search for existing Starter Deck
		if Decks.has_method("ListDecksDict"):
			var all_decks: Array[Dictionary] = Decks.call("ListDecksDict")
			for deck_dict: Dictionary in all_decks:
				if deck_dict.get("name", "") == _DeckConstants.STARTER_DECK_NAME:
					deck_id = deck_dict.get("id", "")
					break

		# If deck exists, add card to it; otherwise create new deck
		if deck_id != "":
			if Decks.has_method("AddCardToDeck"):
				Decks.call("AddCardToDeck", deck_id, card_instance_id)
		else:
			# Get the player's unlocked summoner to assign to the deck
			var summoner_id: String = _get_first_unlocked_summoner()
			if Decks.has_method("CreateDeckFromDict"):
				var result: Variant = Decks.call("CreateDeckFromDict", _DeckConstants.STARTER_DECK_NAME, [card_instance_id], summoner_id)
				deck_id = result if result is String else ""

		# Set as active deck
		if deck_id != "":
			ProfileRepo.UpdateProfileMetaDict({"selected_deck": deck_id})

	# Return to campaign map
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

## Get the first unlocked summoner from the profile
func _get_first_unlocked_summoner() -> String:
	var unlocked: Variant = SummonerSelection.GetUnlockedSummonerIdsArray()
	if unlocked.size() > 0:
		return unlocked[0]
	return ""
