extends Control
class_name HandUI

## Displays player's hand of cards at bottom of screen
## Shows card name, cost, and availability based on mana

const CARD_WIDTH = 120
const CARD_HEIGHT = 160
const CARD_SPACING = 10

## Inner class for draggable card displays
class CardDisplay extends Control:
	var card: Card
	var card_index: int
	var hand_ui: HandUI

	func _ready() -> void:
		mouse_filter = Control.MOUSE_FILTER_STOP
		print("CardDisplay ready: ", name, " size: ", size, " custom_minimum_size: ", custom_minimum_size)

	## Start dragging this card
	func _get_drag_data(_at_position: Vector2) -> Variant:
		print("CardDisplay: _get_drag_data called for card ", card_index)

		if not hand_ui or not hand_ui.summoner:
			print("CardDisplay: No hand_ui or summoner")
			return null

		# Check if we can afford this card
		if hand_ui.summoner.mana < card.mana_cost:
			print("CardDisplay: Not enough mana (", hand_ui.summoner.mana, " < ", card.mana_cost, ")")
			return null

		print("CardDisplay: Starting drag for card ", card.card_name)

		# Create drag preview
		var preview = _create_drag_preview()
		set_drag_preview(preview)

		# Return drag data
		return {
			"card_index": card_index,
			"card": card,
			"source": "hand"
		}

	## Create visual preview while dragging
	func _create_drag_preview() -> Control:
		var preview = Control.new()

		# Semi-transparent card background
		var bg = ColorRect.new()
		bg.size = Vector2(CARD_WIDTH, CARD_HEIGHT)
		bg.color = Color(0.2, 0.2, 0.3, 0.7)
		preview.add_child(bg)

		# Glowing border
		var border = ColorRect.new()
		border.size = Vector2(CARD_WIDTH + 4, CARD_HEIGHT + 4)
		border.position = Vector2(-2, -2)
		border.color = Color(1.0, 0.8, 0.0, 0.8)
		border.z_index = -1
		preview.add_child(border)

		# Card name
		var name_label = Label.new()
		name_label.text = card.card_name
		name_label.position = Vector2(10, 10)
		name_label.add_theme_font_size_override("font_size", 16)
		preview.add_child(name_label)

		# Mana cost
		var cost_label = Label.new()
		cost_label.text = str(int(card.mana_cost))
		cost_label.position = Vector2(CARD_WIDTH - 30, CARD_HEIGHT - 35)
		cost_label.add_theme_font_size_override("font_size", 20)
		cost_label.add_theme_color_override("font_color", Color.CYAN)
		preview.add_child(cost_label)

		return preview

	## Allow clicking to select card (fallback to old behavior)
	func _gui_input(event: InputEvent) -> void:
		if event is InputEventMouseButton:
			if event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
				if hand_ui:
					hand_ui._select_card(card_index)

var summoner: Summoner
var card_displays: Array[Control] = []
var selected_card_index: int = 0

signal card_selected(index: int)

func _ready() -> void:
	add_to_group("hand_ui")

	# Block clicks to battlefield
	mouse_filter = Control.MOUSE_FILTER_STOP

	# Find player summoner
	var summoners = get_tree().get_nodes_in_group("summoners")
	for node in summoners:
		if node is Summoner and node.team == Unit.Team.PLAYER:
			summoner = node
			break

	if not summoner:
		push_error("HandUI: Could not find player Summoner!")
		return

	# Connect to summoner signals
	summoner.card_played.connect(_on_card_played)
	summoner.card_drawn.connect(_on_card_drawn)
	summoner.mana_changed.connect(_on_mana_changed)

	# Initial hand display
	_rebuild_hand_display()

	print("HandUI: Ready with ", summoner.hand.size(), " cards")

func _rebuild_hand_display() -> void:
	# Clear existing displays
	for display in card_displays:
		display.queue_free()
	card_displays.clear()

	if not summoner or summoner.hand.is_empty():
		return

	# Create card displays
	var total_width = summoner.hand.size() * CARD_WIDTH + (summoner.hand.size() - 1) * CARD_SPACING
	var start_x = (size.x - total_width) / 2

	for i in range(summoner.hand.size()):
		var card = summoner.hand[i]
		var card_display = _create_card_display(card, i)
		card_display.position = Vector2(start_x + i * (CARD_WIDTH + CARD_SPACING), 10)
		add_child(card_display)
		card_displays.append(card_display)

	# Highlight selected card
	_update_selection_visual()

func _create_card_display(card: Card, index: int) -> Control:
	var container = CardDisplay.new()
	container.custom_minimum_size = Vector2(CARD_WIDTH, CARD_HEIGHT)
	container.size = Vector2(CARD_WIDTH, CARD_HEIGHT)
	container.name = "CardDisplay%d" % index
	container.card = card
	container.card_index = index
	container.hand_ui = self

	# Card background
	var bg = ColorRect.new()
	bg.name = "Background"
	bg.size = Vector2(CARD_WIDTH, CARD_HEIGHT)
	bg.color = Color(0.2, 0.2, 0.3, 0.9)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	container.add_child(bg)

	# Card border
	var border = ColorRect.new()
	border.name = "Border"
	border.size = Vector2(CARD_WIDTH, CARD_HEIGHT)
	border.color = Color.GRAY
	border.z_index = -1
	border.position = Vector2(-2, -2)
	border.custom_minimum_size = Vector2(CARD_WIDTH + 4, CARD_HEIGHT + 4)
	border.mouse_filter = Control.MOUSE_FILTER_IGNORE
	container.add_child(border)

	# Card name label
	var name_label = Label.new()
	name_label.text = card.card_name
	name_label.position = Vector2(10, 10)
	name_label.add_theme_font_size_override("font_size", 16)
	name_label.custom_minimum_size = Vector2(CARD_WIDTH - 20, 0)
	name_label.autowrap_mode = TextServer.AUTOWRAP_WORD
	name_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	container.add_child(name_label)

	# Card type (icon placeholder)
	var type_label = Label.new()
	type_label.text = "SUMMON" if card.card_type == Card.CardType.SUMMON else "SPELL"
	type_label.position = Vector2(10, 40)
	type_label.add_theme_font_size_override("font_size", 12)
	type_label.add_theme_color_override("font_color", Color.YELLOW)
	type_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	container.add_child(type_label)

	# Unit icon (colored rect for now)
	if card.card_type == Card.CardType.SUMMON and card.unit_scene:
		var icon = ColorRect.new()
		icon.size = Vector2(80, 60)
		icon.position = Vector2(20, 60)
		icon.color = Color(0.3, 0.5, 0.8)
		icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
		container.add_child(icon)

	# Mana cost
	var cost_bg = ColorRect.new()
	cost_bg.size = Vector2(30, 30)
	cost_bg.position = Vector2(CARD_WIDTH - 40, CARD_HEIGHT - 40)
	cost_bg.color = Color(0.1, 0.1, 0.5, 0.9)
	cost_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	container.add_child(cost_bg)

	var cost_label = Label.new()
	cost_label.name = "CostLabel"
	cost_label.text = str(int(card.mana_cost))
	cost_label.position = Vector2(CARD_WIDTH - 35, CARD_HEIGHT - 38)
	cost_label.add_theme_font_size_override("font_size", 20)
	cost_label.add_theme_color_override("font_color", Color.CYAN)
	cost_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	container.add_child(cost_label)

	return container

func _select_card(index: int) -> void:
	if index < 0 or index >= summoner.hand.size():
		return

	selected_card_index = index
	_update_selection_visual()
	card_selected.emit(index)
	print("HandUI: Selected card %d: %s" % [index, summoner.hand[index].card_name])

func _update_selection_visual() -> void:
	for i in range(card_displays.size()):
		if i >= card_displays.size():
			continue

		var display = card_displays[i]
		var border = display.get_child(1) as ColorRect  # Border is second child

		if i == selected_card_index:
			border.color = Color.GOLD
			display.position.y = 0  # Raise selected card slightly
		else:
			border.color = Color.GRAY
			display.position.y = 10

func _update_availability() -> void:
	if not summoner:
		return

	for i in range(card_displays.size()):
		if i >= summoner.hand.size():
			continue

		var card = summoner.hand[i]
		var display = card_displays[i]
		var bg = display.get_child(0) as ColorRect

		# Gray out unaffordable cards
		if summoner.mana < card.mana_cost:
			bg.color = Color(0.15, 0.15, 0.2, 0.9)
			bg.modulate = Color(0.6, 0.6, 0.6)
		else:
			bg.color = Color(0.2, 0.2, 0.3, 0.9)
			bg.modulate = Color.WHITE

func _on_card_played(_card: Card) -> void:
	# Deselect after playing
	selected_card_index = 0
	_rebuild_hand_display()

func _on_card_drawn(_card: Card) -> void:
	_rebuild_hand_display()

func _on_mana_changed(_current: float, _maximum: float) -> void:
	_update_availability()

func get_selected_card_index() -> int:
	return selected_card_index

func select_next_card() -> void:
	if summoner.hand.is_empty():
		return
	selected_card_index = (selected_card_index + 1) % summoner.hand.size()
	_update_selection_visual()
	card_selected.emit(selected_card_index)

func select_card_by_index(index: int) -> void:
	_select_card(index)
