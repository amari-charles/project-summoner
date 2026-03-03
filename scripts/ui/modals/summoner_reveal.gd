extends Control
class_name SummonerReveal

## SummonerReveal - Animated reveal of selected summoner
##
## Shows the full summoner card with stats after selection.
## Animates the card in with a dramatic reveal.

@onready var title_label: Label = %TitleLabel
@onready var card_container: CenterContainer = %CardContainer
@onready var continue_button: Button = %ContinueButton
@onready var animation_player: AnimationPlayer = %AnimationPlayer

const SummonerCardScene: PackedScene = preload("res://scenes/ui/components/summoner_card.tscn")

var summoner_id: String = ""
var was_random: bool = false

func _ready() -> void:
	# Hide continue button initially
	if continue_button:
		continue_button.visible = false
		continue_button.pressed.connect(_on_continue_pressed)

	# Read summoner data from ProfileRepo (just saved by summoner_selection)
	var unlocked = SummonerSelection.GetUnlockedSummonerIdsArray()
	if not unlocked.is_empty():
		summoner_id = unlocked[0]  # Starting summoner is first unlocked

	# Check WAL for whether random was chosen (for title text)
	# For now, just use default title
	# TODO: Add get_was_random_chosen() to ProfileRepo if needed

	# Update title for random selection
	if was_random and title_label:
		title_label.text = Loc.t("ui.summoner_reveal.random_title")

	# Create and add summoner card
	_create_summoner_card()

	# Start reveal animation
	_animate_reveal()

## Create the summoner card
func _create_summoner_card() -> void:
	if not card_container:
		return

	var card: SummonerCard = SummonerCardScene.instantiate()
	card_container.add_child(card)
	card.set_summoner(summoner_id)

	# Hide card initially for animation
	card.modulate = Color(1, 1, 1, 0)
	card.scale = Vector2(0.5, 0.5)

## Animate the reveal
func _animate_reveal() -> void:
	await get_tree().create_timer(0.5).timeout

	# Get the card
	if not card_container or card_container.get_child_count() == 0:
		_show_continue_button()
		return

	var card: Node = card_container.get_child(0)

	# Fade in and scale up
	var tween: Tween = create_tween()
	tween.set_parallel(true)
	tween.tween_property(card, "modulate", Color.WHITE, 1.0).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	tween.tween_property(card, "scale", Vector2.ONE, 1.0).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)

	# Show glow effect on card
	if card.has_method("show_glow"):
		await tween.finished
		card.call("show_glow")

	# Show continue button after animation
	await get_tree().create_timer(0.5).timeout
	_show_continue_button()

## Show the continue button
func _show_continue_button() -> void:
	if not continue_button:
		return

	continue_button.visible = true
	var tween: Tween = create_tween()
	continue_button.modulate = Color(1, 1, 1, 0)
	tween.tween_property(continue_button, "modulate", Color.WHITE, 0.5)

## Continue to campaign map
func _on_continue_pressed() -> void:
	SceneManager.transition_to(SceneManager.SCENE_CAMPAIGN_MAP)
