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

	# Animation state
	var hover_tween: Tween
	var is_hovered: bool = false
	var was_recently_hovered: bool = false  # Prevents pulse from restarting immediately
	var base_position: Vector2
	var base_scale: Vector2 = Vector2(1.0, 1.0)

	# 3D rotation shader
	var shader_material: ShaderMaterial
	var background_node: ColorRect  # Reference to apply shader

	# Velocity tracking for rotation
	var previous_position: Vector2
	var velocity: Vector2 = Vector2.ZERO


	# Animation constants
	const HOVER_OFFSET = -40.0  # How much card rises (negative = up)
	const HOVER_SCALE = 1.2     # Scale multiplier when hovered
	const HOVER_DURATION = 0.25 # Seconds for hover transition

	# Draw animation constants
	const DRAW_ANIMATION_DURATION = 0.4
	const DRAW_START_OFFSET = 50.0  # Start below target position
	const DRAW_STAGGER_DELAY = 0.08  # Delay between each card

	# 3D effect constants
	const MAX_TILT_DEGREES = 15.0  # Maximum rotation in degrees
	const TILT_SMOOTHING = 0.15    # Lerp factor for smooth rotation

	# Velocity rotation constants (Balatro-style)
	const VELOCITY_DIVISOR = 2000.0  # Divisor for velocity to rotation conversion (lower = more sensitive)
	const MAX_ROTATION_RADIANS = 0.4  # Maximum rotation in radians (~23 degrees)
	const ROTATION_DAMPING = 0.85  # Damping factor for rotation (lower = more damping, higher = keeps rotation longer)

	func _ready() -> void:
		mouse_filter = Control.MOUSE_FILTER_STOP
		base_position = position
		previous_position = position  # Track local position for velocity calculation

		# Connect hover signals
		mouse_entered.connect(_on_mouse_entered)
		mouse_exited.connect(_on_mouse_exited)

		# Wait for background node to be added
		await get_tree().process_frame

		# Setup 3D shader on background
		background_node = get_node_or_null("Background") as ColorRect
		if background_node:
			_setup_3d_shader()

	func _process(delta: float) -> void:
		# Track position changes for velocity-based rotation (Balatro-style)
		# Use position (not global_position) because tweens modify local position
		var current_pos = position

		# Calculate velocity from position delta
		if delta > 0:
			velocity = (current_pos - previous_position) / delta
		else:
			velocity = Vector2.ZERO

		previous_position = current_pos

		# Convert velocity to rotation contribution
		# Horizontal velocity creates tilt (negative for natural feel)
		# Vertical velocity also contributes slightly
		var velocity_rotation = velocity.x / VELOCITY_DIVISOR
		velocity_rotation = clamp(velocity_rotation, -MAX_ROTATION_RADIANS, MAX_ROTATION_RADIANS)

		# Add velocity-based rotation (negative for natural tilt - moving right tilts right)
		rotation += -velocity_rotation

		# Apply damping to gradually return to neutral
		rotation *= ROTATION_DAMPING

		# Update 3D rotation based on mouse position (only when hovered)
		if is_hovered and shader_material:
			_update_3d_rotation()

	## Setup 3D perspective shader on card background
	func _setup_3d_shader() -> void:
		if not background_node:
			return

		# Load shader
		var shader = load("res://shaders/ui/card_perspective_3d.gdshader") as Shader
		if not shader:
			push_error("Failed to load card 3D shader")
			return

		# Create shader material
		shader_material = ShaderMaterial.new()
		shader_material.shader = shader

		# Set default shader parameters
		shader_material.set_shader_parameter("fov", 70.0)
		shader_material.set_shader_parameter("rot_x_deg", 0.0)
		shader_material.set_shader_parameter("rot_y_deg", 0.0)
		shader_material.set_shader_parameter("inset", 0.0)
		shader_material.set_shader_parameter("cull_backface", true)
		shader_material.set_shader_parameter("use_front", true)

		# Apply shader to background
		background_node.material = shader_material

	## Update 3D rotation based on mouse position relative to card
	func _update_3d_rotation() -> void:
		if not shader_material:
			return

		# Get mouse position relative to card center
		var mouse_pos = get_local_mouse_position()
		var card_center = size / 2.0

		# Calculate normalized offset from center (-1 to 1)
		var offset_x = (mouse_pos.x - card_center.x) / card_center.x
		var offset_y = (mouse_pos.y - card_center.y) / card_center.y

		# Clamp to card bounds
		offset_x = clamp(offset_x, -1.0, 1.0)
		offset_y = clamp(offset_y, -1.0, 1.0)

		# Calculate target rotation (inverted for natural feel)
		var target_rot_y = offset_x * MAX_TILT_DEGREES
		var target_rot_x = -offset_y * MAX_TILT_DEGREES  # Negative for proper direction

		# Get current rotation
		var current_rot_y = shader_material.get_shader_parameter("rot_y_deg")
		var current_rot_x = shader_material.get_shader_parameter("rot_x_deg")

		# Smooth lerp to target
		var new_rot_y = lerp(current_rot_y, target_rot_y, TILT_SMOOTHING)
		var new_rot_x = lerp(current_rot_x, target_rot_x, TILT_SMOOTHING)

		# Update shader parameters
		shader_material.set_shader_parameter("rot_y_deg", new_rot_y)
		shader_material.set_shader_parameter("rot_x_deg", new_rot_x)

	## Play entrance animation when card is first drawn/created
	func play_entrance_animation(stagger_index: int = 0) -> void:
		# Set starting state - below target, small scale
		var target_pos = position
		position.y = target_pos.y + DRAW_START_OFFSET
		scale = Vector2(0.5, 0.5)
		modulate.a = 0.0  # Start invisible

		# Wait for stagger delay
		var delay = stagger_index * DRAW_STAGGER_DELAY
		await get_tree().create_timer(delay).timeout

		# Animate to target position and scale
		var entrance_tween = create_tween()
		entrance_tween.set_parallel(true)
		entrance_tween.set_trans(Tween.TRANS_ELASTIC)
		entrance_tween.set_ease(Tween.EASE_OUT)

		# Animate position (slide up)
		entrance_tween.tween_property(self, "position:y", target_pos.y, DRAW_ANIMATION_DURATION)

		# Animate scale (grow to normal size)
		entrance_tween.tween_property(self, "scale", base_scale, DRAW_ANIMATION_DURATION)

		# Fade in
		entrance_tween.tween_property(self, "modulate:a", 1.0, DRAW_ANIMATION_DURATION * 0.5)

		# Update base_position after animation completes
		entrance_tween.finished.connect(func():
			base_position = position
		)

	## Start dragging this card
	func _get_drag_data(at_position: Vector2) -> Variant:
		if not hand_ui or not hand_ui.summoner:
			return null

		# Check if we can afford this card
		if hand_ui.summoner.mana < card.mana_cost:
			return null

		# Create a visual duplicate as preview
		var preview = duplicate(DUPLICATE_USE_INSTANTIATION)
		preview.scale = Vector2(HOVER_SCALE, HOVER_SCALE)

		# Create wrapper to control preview offset
		var preview_wrapper = Control.new()
		preview_wrapper.add_child(preview)

		# Position preview so the grab point stays under cursor
		# at_position is where on THIS card the user clicked
		preview.position = -at_position * HOVER_SCALE

		set_drag_preview(preview_wrapper)

		# Card physically leaves the hand (completely invisible)
		visible = false

		# Return drag data
		return {
			"card_index": card_index,
			"card": card,
			"source": "hand"
		}

	## Called when drag ends (whether successful or cancelled)
	func _notification(what: int) -> void:
		if what == NOTIFICATION_DRAG_END:
			# Card returns to hand (if drag was cancelled)
			visible = true

	## Allow clicking to select card
	func _gui_input(event: InputEvent) -> void:
		if event is InputEventMouseButton:
			if event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
				if hand_ui:
					hand_ui._select_card(card_index)

	## Hover effect - card rises and scales up with elastic bounce
	func _on_mouse_entered() -> void:
		if is_hovered:
			return
		is_hovered = true
		was_recently_hovered = true

		# Stop any pulse glow on the border
		var border = get_node_or_null("Border") as ColorRect
		if border and border.has_meta("pulse_tween"):
			var pulse_tween = border.get_meta("pulse_tween") as Tween
			if pulse_tween and pulse_tween.is_valid():
				pulse_tween.kill()
			border.remove_meta("pulse_tween")

		# Create elastic hover tween
		if hover_tween and hover_tween.is_valid():
			hover_tween.kill()

		hover_tween = create_tween()
		hover_tween.set_parallel(true)
		hover_tween.set_trans(Tween.TRANS_ELASTIC)  # Changed from CUBIC to ELASTIC
		hover_tween.set_ease(Tween.EASE_OUT)

		# Animate position (rise up) with elastic bounce
		hover_tween.tween_property(self, "position:y", base_position.y + HOVER_OFFSET, HOVER_DURATION)

		# Animate scale with elastic bounce
		hover_tween.tween_property(self, "scale", Vector2(HOVER_SCALE, HOVER_SCALE), HOVER_DURATION)

		# Raise z_index to appear above other cards
		z_index = 10

		# Update glow if card is playable
		_update_hover_glow(true)

	## Exit hover - card returns to normal
	func _on_mouse_exited() -> void:
		if not is_hovered:
			return
		is_hovered = false

		# Create exit tween with elastic bounce
		if hover_tween and hover_tween.is_valid():
			hover_tween.kill()

		hover_tween = create_tween()
		hover_tween.set_parallel(true)
		hover_tween.set_trans(Tween.TRANS_ELASTIC)  # Changed from CUBIC to ELASTIC
		hover_tween.set_ease(Tween.EASE_OUT)

		# Return to base position
		hover_tween.tween_property(self, "position:y", base_position.y, HOVER_DURATION)

		# Return to normal scale
		hover_tween.tween_property(self, "scale", base_scale, HOVER_DURATION)

		# Reset 3D rotation smoothly
		if shader_material:
			var rotation_tween = create_tween()
			rotation_tween.set_parallel(true)
			rotation_tween.set_trans(Tween.TRANS_BACK)
			rotation_tween.set_ease(Tween.EASE_IN_OUT)
			rotation_tween.tween_method(
				func(val): shader_material.set_shader_parameter("rot_x_deg", val),
				shader_material.get_shader_parameter("rot_x_deg"),
				0.0,
				0.3
			)
			rotation_tween.tween_method(
				func(val): shader_material.set_shader_parameter("rot_y_deg", val),
				shader_material.get_shader_parameter("rot_y_deg"),
				0.0,
				0.3
			)

		# Reset z_index
		hover_tween.finished.connect(func(): z_index = 0)

		# Remove hover glow - set to static non-pulsing color
		var border = get_node_or_null("Border") as ColorRect
		if border:
			var glow_tween = create_tween()
			glow_tween.set_trans(Tween.TRANS_SINE)
			glow_tween.set_ease(Tween.EASE_OUT)
			# Set to gray, not back to pulse
			glow_tween.tween_property(border, "color", Color.GRAY, 0.15)

	## Update glow effect based on hover state and playability
	func _update_hover_glow(active: bool) -> void:
		var border = get_node_or_null("Border") as ColorRect
		if not border:
			return

		# Only glow if card is affordable
		var can_afford = hand_ui and hand_ui.summoner and hand_ui.summoner.mana >= card.mana_cost

		if not can_afford:
			return

		var glow_tween = create_tween()
		glow_tween.set_trans(Tween.TRANS_SINE)
		glow_tween.set_ease(Tween.EASE_OUT)

		if active:
			# Bright gold glow on hover
			glow_tween.tween_property(border, "color", Color(1.0, 0.9, 0.3, 1.0), 0.15)
		else:
			# Return to subtle glow or gray
			glow_tween.tween_property(border, "color", Color(0.8, 0.7, 0.2, 0.6), 0.15)

var summoner: Node  # Can be Summoner or Summoner3D
var card_displays: Array[Control] = []
var selected_card_index: int = -1  # -1 means no selection

signal card_selected(index: int)

func _ready() -> void:
	add_to_group("hand_ui")

	# Block clicks to battlefield
	mouse_filter = Control.MOUSE_FILTER_STOP

	# Find player summoner (2D or 3D)
	var summoners = get_tree().get_nodes_in_group("summoners")
	for node in summoners:
		# Check for both Summoner and Summoner3D
		if (node is Summoner and node.team == Unit.Team.PLAYER) or \
		   (node.get_script() and node.get_script().get_global_name() == "Summoner3D" and node.team == 0):
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

		# Play entrance animation with stagger
		card_display.play_entrance_animation(i)

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

func _update_selection_visual() -> void:
	for i in range(card_displays.size()):
		if i >= card_displays.size():
			continue

		var display = card_displays[i] as CardDisplay
		if not display:
			continue

		var border = display.get_node_or_null("Border") as ColorRect

		if i == selected_card_index:
			if border:
				border.color = Color.GOLD
		else:
			if border:
				# Will be updated by _update_availability for playable cards
				border.color = Color.GRAY

func _update_availability() -> void:
	if not summoner:
		return

	for i in range(card_displays.size()):
		if i >= summoner.hand.size():
			continue

		var card = summoner.hand[i]
		var display = card_displays[i] as CardDisplay
		if not display:
			continue

		var bg = display.get_node_or_null("Background") as ColorRect
		var border = display.get_node_or_null("Border") as ColorRect

		# Check affordability
		var can_afford = summoner.mana >= card.mana_cost

		if can_afford:
			# Playable: bright background
			if bg:
				bg.color = Color(0.2, 0.2, 0.3, 0.9)
				bg.modulate = Color.WHITE

			# Start subtle glow pulse on border (unless selected, hovered, or was recently hovered)
			if border and not display.is_hovered and not display.was_recently_hovered and i != selected_card_index:
				_create_glow_pulse(border)
		else:
			# Unaffordable: gray out
			if bg:
				bg.color = Color(0.15, 0.15, 0.2, 0.9)
				bg.modulate = Color(0.6, 0.6, 0.6)

			# Remove glow and kill pulse tween
			if border and i != selected_card_index:
				# Kill pulse tween if it exists
				if border.has_meta("pulse_tween"):
					var pulse_tween = border.get_meta("pulse_tween") as Tween
					if pulse_tween and pulse_tween.is_valid():
						pulse_tween.kill()
					border.remove_meta("pulse_tween")
				border.color = Color.GRAY

## Create pulsing glow effect for playable cards
func _create_glow_pulse(border: ColorRect) -> void:
	# Don't create if already pulsing
	if border.has_meta("pulse_tween"):
		var existing = border.get_meta("pulse_tween") as Tween
		if existing and existing.is_valid():
			return  # Already pulsing

	# Kill any existing tween on this border first
	if border.has_meta("pulse_tween"):
		var old_tween = border.get_meta("pulse_tween") as Tween
		if old_tween and old_tween.is_valid():
			old_tween.kill()

	# Store tween reference on the border node
	var pulse_tween = border.create_tween()
	border.set_meta("pulse_tween", pulse_tween)

	pulse_tween.set_loops()
	pulse_tween.set_trans(Tween.TRANS_SINE)
	pulse_tween.set_ease(Tween.EASE_IN_OUT)

	# Pulse between dim and bright gold
	var dim_gold = Color(0.8, 0.7, 0.2, 0.4)
	var bright_gold = Color(1.0, 0.9, 0.3, 0.8)

	pulse_tween.tween_property(border, "color", bright_gold, 1.0)
	pulse_tween.tween_property(border, "color", dim_gold, 1.0)

func _on_card_played(_card: Card) -> void:
	# Deselect after playing - no card should be selected
	selected_card_index = -1
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
