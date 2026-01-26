extends Control
class_name FirstCardSelection

## FirstCardSelection - Choose your first card
##
## Part of onboarding flow. Player picks Fire Elemental or Earth Sprite as their starter.
## Card is granted to collection and onboarding is marked complete.

# Deck name constant
const STARTER_DECK_NAME: String = "Starter Deck"

@onready var select_fire_elemental_button: Button = %SelectFireElementalButton
@onready var select_earth_sprite_button: Button = %SelectEarthSpriteButton

func _ready() -> void:
	print("FirstCardSelection: Initializing...")

	# Connect button handlers
	select_fire_elemental_button.pressed.connect(_on_card_selected.bind(CardIDs.FIRE_ELEMENTAL))
	select_earth_sprite_button.pressed.connect(_on_card_selected.bind(CardIDs.EARTH_SPRITE))

	# Start Merlin's introduction dialogue (buttons visible alongside dialogue)
	await get_tree().process_frame
	if DialogueManager.has_method("start_dialogue"):
		DialogueManager.call("start_dialogue", "merlin_first_card_intro")
	else:
		print("FirstCardSelection: DialogueManager.start_dialogue not found")

func _on_card_selected(catalog_id: StringName) -> void:
	print("FirstCardSelection: Player selected card: %s" % catalog_id)

	# Check if event already completed (idempotent - safe to run multiple times)
	var already_completed: bool = false
	if Campaign.has_method("is_battle_completed"):
		var result: Variant = Campaign.call("is_battle_completed", BattleIDs.EVENT_FIRST_SUMMON)
		already_completed = result if result is bool else false

	if already_completed:
		print("FirstCardSelection: First summon event already completed, skipping")
		SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)
		return

	# Grant the chosen card to collection
	var card_instance_id: String = CardServiceCS.GrantCard(catalog_id, RarityIDs.COMMON)
	print("FirstCardSelection: Granted %s to collection (instance: %s)" % [catalog_id, card_instance_id])

	# Find or create STARTER_DECK_NAME
	var deck_id: String = ""

	if card_instance_id != "":
		# Search for existing STARTER_DECK_NAME
		if Decks.has_method("list_decks"):
			var all_decks: Array[Dictionary] = Decks.call("list_decks")
			for deck_dict: Dictionary in all_decks:
				if deck_dict.get("name", "") == STARTER_DECK_NAME:
					deck_id = deck_dict.get("id", "")
					print("FirstCardSelection: Found existing Starter Deck (id: %s)" % deck_id)
					break

		# If deck exists, add card to it; otherwise create new deck
		if deck_id != "":
			if Decks.has_method("add_card_to_deck"):
				Decks.call("add_card_to_deck", deck_id, card_instance_id)
				print("FirstCardSelection: Added card to existing Starter Deck")
		else:
			# Get the player's unlocked summoner to assign to the deck
			var summoner_id: String = _get_first_unlocked_summoner()
			if Decks.has_method("create_deck"):
				var result: Variant = Decks.call("create_deck", STARTER_DECK_NAME, [card_instance_id], summoner_id)
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

	# Mark event as completed
	if Campaign.has_method("complete_battle"):
		Campaign.call("complete_battle", BattleIDs.EVENT_FIRST_SUMMON)
		print("FirstCardSelection: Marked first summon event as completed!")

	# Return to campaign map
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)

## Get the first unlocked summoner from the profile
func _get_first_unlocked_summoner() -> String:
	if ProfileRepo.has_method("get_unlocked_summoners"):
		var unlocked_variant: Variant = ProfileRepo.call("get_unlocked_summoners")
		if unlocked_variant is Array:
			var unlocked: Array = unlocked_variant
			if unlocked.size() > 0:
				var first_summoner: Variant = unlocked[0]
				if first_summoner is String:
					return first_summoner
	return ""
