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
	var viewport_container: SubViewportContainer  # Container with shader applied

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

		# Wait for viewport container to be added
		await get_tree().process_frame

		# Setup 3D shader on viewport container
		viewport_container = get_node_or_null("ViewportContainer") as SubViewportContainer
		if viewport_container:
			_setup_3d_shader()

	## Get CardVisual component from this card display
	func _get_card_visual() -> CardVisual:
		var viewport = get_node_or_null("ViewportContainer/Viewport")
		if viewport:
			for child in viewport.get_children():
				if child is CardVisual:
					return child as CardVisual
		return null

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

	## Setup 3D perspective shader on viewport container
	func _setup_3d_shader() -> void:
		if not viewport_container:
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

		# Apply shader to viewport container
		viewport_container.material = shader_material

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

		# Stop any pulse glow on the card border
		var card_visual = _get_card_visual()
		if card_visual and card_visual.has_meta("pulse_tween"):
			var pulse_tween = card_visual.get_meta("pulse_tween") as Tween
			if pulse_tween and pulse_tween.is_valid():
				pulse_tween.kill()
			card_visual.remove_meta("pulse_tween")

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

		# Remove hover glow - return border to base element color
		var card_visual = _get_card_visual()
		if card_visual:
			var element_color = card_visual.get_element_color()
			var border_panel = card_visual.get_node_or_null("BorderPanel") as Panel
			if border_panel:
				# Reset border to base element color via style
				var border_style = StyleBoxFlat.new()
				border_style.bg_color = element_color
				border_style.set_corner_radius_all(card_visual.corner_radius)
				border_style.anti_aliasing = true
				border_style.anti_aliasing_size = 1
				border_panel.add_theme_stylebox_override("panel", border_style)

	## Update glow effect based on hover state and playability
	func _update_hover_glow(active: bool) -> void:
		var card_visual = _get_card_visual()
		if not card_visual:
			return

		# Only glow if card is affordable
		var can_afford = hand_ui and hand_ui.summoner and hand_ui.summoner.mana >= card.mana_cost

		if not can_afford:
			return

		# Get element color for this card
		var element_color = card_visual.get_element_color()
		var border_panel = card_visual.get_node_or_null("BorderPanel") as Panel
		if not border_panel:
			return

		var glow_color = element_color.lightened(0.4) if active else element_color.lightened(0.2)

		# Apply glow via border style
		var border_style = StyleBoxFlat.new()
		border_style.bg_color = glow_color
		border_style.set_corner_radius_all(card_visual.corner_radius)
		border_style.anti_aliasing = true
		border_style.anti_aliasing_size = 1
		border_panel.add_theme_stylebox_override("panel", border_style)

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

	# Create SubViewport to render card content
	var viewport = SubViewport.new()
	viewport.name = "Viewport"
	viewport.size = Vector2i(CARD_WIDTH, CARD_HEIGHT)
	viewport.transparent_bg = true
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS

	# Create SubViewportContainer to display and apply shader
	var viewport_container = SubViewportContainer.new()
	viewport_container.name = "ViewportContainer"
	viewport_container.size = Vector2(CARD_WIDTH, CARD_HEIGHT)
	viewport_container.stretch = true
	viewport_container.mouse_filter = Control.MOUSE_FILTER_IGNORE
	container.add_child(viewport_container)
	viewport_container.add_child(viewport)

	# Load and instantiate CardVisual scene
	var card_visual_scene = load("res://scenes/ui/card_visual.tscn")
	if not card_visual_scene:
		push_error("HandUI: Failed to load card_visual.tscn")
		return container

	var card_visual = card_visual_scene.instantiate() as CardVisual
	if not card_visual:
		push_error("HandUI: Failed to instantiate CardVisual")
		return container

	# Configure card visual for in-hand display
	card_visual.size = Vector2(CARD_WIDTH, CARD_HEIGHT)
	card_visual.custom_minimum_size = Vector2(CARD_WIDTH, CARD_HEIGHT)
	card_visual.border_width = 3
	card_visual.corner_radius = 8
	card_visual.cost_circle_radius = 16
	card_visual.cost_font_size = 18
	card_visual.name_font_size = 14
	card_visual.show_description = false
	card_visual.mouse_filter = Control.MOUSE_FILTER_IGNORE

	# Get catalog data to pass to CardVisual
	var catalog_data = CardCatalog.get_card(card.catalog_id)

	# Set card data
	card_visual.set_card_data(catalog_data)

	# Add to viewport
	viewport.add_child(card_visual)

	# Store reference to CardVisual for easy access
	card_visual.set_meta("card_visual_component", card_visual)

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

		var card_visual = display._get_card_visual()
		if not card_visual:
			continue

		var border_panel = card_visual.get_node_or_null("BorderPanel") as Panel
		if not border_panel:
			continue

		var border_style = StyleBoxFlat.new()
		border_style.set_corner_radius_all(card_visual.corner_radius)
		border_style.anti_aliasing = true
		border_style.anti_aliasing_size = 1

		if i == selected_card_index:
			border_style.bg_color = Color.GOLD
		else:
			border_style.bg_color = card_visual.get_element_color()

		border_panel.add_theme_stylebox_override("panel", border_style)

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

		var card_visual = display._get_card_visual()
		if not card_visual:
			continue

		var bg_panel = card_visual.get_node_or_null("BackgroundPanel") as Panel
		var border_panel = card_visual.get_node_or_null("BorderPanel") as Panel

		# Check affordability
		var can_afford = summoner.mana >= card.mana_cost

		if can_afford:
			# Playable: normal background
			if bg_panel:
				var bg_style = StyleBoxFlat.new()
				bg_style.bg_color = GameColorPalette.UI_BG_DARK
				bg_style.set_corner_radius_all(card_visual.corner_radius - card_visual.border_width)
				bg_style.anti_aliasing = true
				bg_style.anti_aliasing_size = 1
				bg_panel.add_theme_stylebox_override("panel", bg_style)
				bg_panel.modulate = Color.WHITE

			# Start subtle glow pulse on border (unless selected, hovered, or was recently hovered)
			if card_visual and not display.is_hovered and not display.was_recently_hovered and i != selected_card_index:
				_create_glow_pulse(card_visual)
		else:
			# Unaffordable: gray out background
			if bg_panel:
				var bg_style = StyleBoxFlat.new()
				bg_style.bg_color = GameColorPalette.UI_BG_DARK
				bg_style.set_corner_radius_all(card_visual.corner_radius - card_visual.border_width)
				bg_style.anti_aliasing = true
				bg_style.anti_aliasing_size = 1
				bg_panel.add_theme_stylebox_override("panel", bg_style)
				bg_panel.modulate = Color(0.5, 0.5, 0.5)

			# Remove glow and kill pulse tween
			if card_visual and i != selected_card_index:
				# Kill pulse tween if it exists
				if card_visual.has_meta("pulse_tween"):
					var pulse_tween = card_visual.get_meta("pulse_tween") as Tween
					if pulse_tween and pulse_tween.is_valid():
						pulse_tween.kill()
					card_visual.remove_meta("pulse_tween")

				# Dim the element color for unaffordable cards
				if border_panel:
					var border_style = StyleBoxFlat.new()
					border_style.bg_color = card_visual.get_element_color().darkened(0.5)
					border_style.set_corner_radius_all(card_visual.corner_radius)
					border_style.anti_aliasing = true
					border_style.anti_aliasing_size = 1
					border_panel.add_theme_stylebox_override("panel", border_style)

## Create pulsing glow effect for playable cards
func _create_glow_pulse(card_visual: CardVisual) -> void:
	if not card_visual:
		return

	# Don't create if already pulsing
	if card_visual.has_meta("pulse_tween"):
		var existing = card_visual.get_meta("pulse_tween") as Tween
		if existing and existing.is_valid():
			return  # Already pulsing

	# Kill any existing tween first
	if card_visual.has_meta("pulse_tween"):
		var old_tween = card_visual.get_meta("pulse_tween") as Tween
		if old_tween and old_tween.is_valid():
			old_tween.kill()

	var border_panel = card_visual.get_node_or_null("BorderPanel") as Panel
	if not border_panel:
		return

	# Store tween reference on the card_visual node
	var pulse_tween = create_tween()
	card_visual.set_meta("pulse_tween", pulse_tween)

	pulse_tween.set_loops()
	pulse_tween.set_trans(Tween.TRANS_SINE)
	pulse_tween.set_ease(Tween.EASE_IN_OUT)

	# Get element color for this card
	var element_color = card_visual.get_element_color()

	# Pulse between dim and bright element color
	var dim_color = element_color.darkened(0.2)
	var bright_color = element_color.lightened(0.2)

	# Create a custom method to update border color via StyleBox
	var update_border_color = func(color: Color):
		if border_panel:
			var style = StyleBoxFlat.new()
			style.bg_color = color
			style.set_corner_radius_all(card_visual.corner_radius)
			style.anti_aliasing = true
			style.anti_aliasing_size = 1
			border_panel.add_theme_stylebox_override("panel", style)

	# Tween by calling the method repeatedly
	pulse_tween.tween_method(update_border_color, dim_color, bright_color, 1.0)
	pulse_tween.tween_method(update_border_color, bright_color, dim_color, 1.0)

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
