extends Control
class_name HandUI

## Displays player's hand of cards at bottom of screen
## Shows card name, cost, and availability based on mana

## Card display size constants
## These must match the CardVisual scene dimensions to ensure proper rendering
## Changing these requires updating both this file and card_visual.tscn
const CARD_WIDTH: int = 120   ## Width of each card in pixels (matches CardVisual width)
const CARD_HEIGHT: int = 160  ## Height of each card in pixels (matches CardVisual height)
const CARD_SPACING: int = 10  ## Horizontal spacing between cards in hand
const CARD_VISUAL_SCENE: PackedScene = preload("res://scenes/ui/card_visual.tscn")

# Glow effect constants
const GLOW_BRIGHTNESS_ACTIVE: float = 0.4  # Lightening amount for active/hovered card glow
const GLOW_BRIGHTNESS_IDLE: float = 0.2    # Lightening amount for idle card glow
const PULSE_BRIGHTNESS_OFFSET: float = 0.2 # Lightening/darkening amount for pulse animation

## Inner class for draggable card displays
class CardDisplay extends Control:
	var card: Card
	var card_index: int
	var hand_ui: HandUI

	# Animation state
	var hover_tween: Tween = null
	var entrance_tween: Tween = null
	var rotation_tween: Tween = null
	var is_hovered: bool = false
	var was_recently_hovered: bool = false  # Prevents pulse from restarting immediately
	var base_position: Vector2 = Vector2.ZERO
	var base_scale: Vector2 = Vector2(1.0, 1.0)

	# 3D rotation shader
	var shader_material: ShaderMaterial = null
	var viewport_container: SubViewportContainer = null  # Container with shader applied

	# Velocity tracking for rotation
	var previous_position: Vector2 = Vector2.ZERO
	var velocity: Vector2 = Vector2.ZERO


	# Animation constants
	const HOVER_OFFSET: float = -40.0  # How much card rises (negative = up)
	const HOVER_SCALE: float = 1.2     # Scale multiplier when hovered
	const HOVER_DURATION: float = 0.25 # Seconds for hover transition

	# Draw animation constants
	const DRAW_ANIMATION_DURATION: float = 0.4
	const DRAW_START_OFFSET: float = 50.0  # Start below target position
	const DRAW_STAGGER_DELAY: float = 0.08  # Delay between each card

	# 3D effect constants
	const MAX_TILT_DEGREES: float = 15.0  # Maximum rotation in degrees
	const TILT_SMOOTHING: float = 0.15    # Lerp factor for smooth rotation

	# Velocity rotation constants (Balatro-style)
	const VELOCITY_DIVISOR: float = 2000.0  # Divisor for velocity to rotation conversion (lower = more sensitive)
	const MAX_ROTATION_RADIANS: float = 0.4  # Maximum rotation in radians (~23 degrees)
	const ROTATION_DAMPING: float = 0.85  # Damping factor for rotation (lower = more damping, higher = keeps rotation longer)

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
		var viewport_container_variant: Variant = get_node_or_null("ViewportContainer")
		viewport_container = viewport_container_variant if viewport_container_variant is SubViewportContainer else null
		if viewport_container:
			_setup_3d_shader()

	func _exit_tree() -> void:
		# Disconnect signals to prevent memory leaks
		if mouse_entered.is_connected(_on_mouse_entered):
			mouse_entered.disconnect(_on_mouse_entered)
		if mouse_exited.is_connected(_on_mouse_exited):
			mouse_exited.disconnect(_on_mouse_exited)

		# Kill any active tweens to prevent lambda capture errors
		if hover_tween and hover_tween.is_valid():
			hover_tween.kill()
		if entrance_tween and entrance_tween.is_valid():
			entrance_tween.kill()
		if rotation_tween and rotation_tween.is_valid():
			rotation_tween.kill()

		# Kill pulse tween stored in card visual metadata
		var card_visual: CardVisual = _get_card_visual()
		if card_visual and card_visual.has_meta("pulse_tween"):
			var pulse_tween_variant: Variant = card_visual.get_meta("pulse_tween")
			var pulse_tween: Tween = pulse_tween_variant if pulse_tween_variant is Tween else null
			if pulse_tween and pulse_tween.is_valid():
				pulse_tween.kill()

	## Get CardVisual component from this card display
	func _get_card_visual() -> CardVisual:
		var viewport_variant: Variant = get_node_or_null("ViewportContainer/Viewport")
		var viewport: Node = viewport_variant if viewport_variant is Node else null
		if viewport:
			var children: Array = viewport.get_children()
			for child: Node in children:
				if child is CardVisual:
					var card_visual: CardVisual = child
					return card_visual
		return null

	func _process(delta: float) -> void:
		# Track position changes for velocity-based rotation (Balatro-style)
		# Use position (not global_position) because tweens modify local position
		var current_pos: Vector2 = position

		# Calculate velocity from position delta
		if delta > 0:
			velocity = (current_pos - previous_position) / delta
		else:
			velocity = Vector2.ZERO

		previous_position = current_pos

		# Convert velocity to rotation contribution
		# Horizontal velocity creates tilt (negative for natural feel)
		# Vertical velocity also contributes slightly
		var velocity_rotation: float = velocity.x / VELOCITY_DIVISOR
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
		var loaded_shader: Resource = load("res://shaders/ui/card_perspective_3d.gdshader")
		if not loaded_shader or not loaded_shader is Shader:
			push_error("Failed to load card 3D shader")
			return

		# Type narrow to Shader for safe property access
		var shader: Shader = loaded_shader

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
		var mouse_pos: Vector2 = get_local_mouse_position()
		var card_center: Vector2 = size / 2.0

		# Calculate normalized offset from center (-1 to 1)
		var offset_x: float = (mouse_pos.x - card_center.x) / card_center.x
		var offset_y: float = (mouse_pos.y - card_center.y) / card_center.y

		# Clamp to card bounds
		offset_x = clamp(offset_x, -1.0, 1.0)
		offset_y = clamp(offset_y, -1.0, 1.0)

		# Calculate target rotation (inverted for natural feel)
		var target_rot_y: float = offset_x * MAX_TILT_DEGREES
		var target_rot_x: float = -offset_y * MAX_TILT_DEGREES  # Negative for proper direction

		# Get current rotation
		var current_rot_y_variant: Variant = shader_material.get_shader_parameter("rot_y_deg")
		var current_rot_y: float = current_rot_y_variant if current_rot_y_variant is float else 0.0
		var current_rot_x_variant: Variant = shader_material.get_shader_parameter("rot_x_deg")
		var current_rot_x: float = current_rot_x_variant if current_rot_x_variant is float else 0.0

		# Smooth lerp to target
		var new_rot_y: float = lerp(current_rot_y, target_rot_y, TILT_SMOOTHING)
		var new_rot_x: float = lerp(current_rot_x, target_rot_x, TILT_SMOOTHING)

		# Update shader parameters
		shader_material.set_shader_parameter("rot_y_deg", new_rot_y)
		shader_material.set_shader_parameter("rot_x_deg", new_rot_x)

	## Play entrance animation when card is first drawn/created
	func play_entrance_animation(stagger_index: int = 0) -> void:
		# Set starting state - below target, small scale
		var target_pos: Vector2 = position
		position.y = target_pos.y + DRAW_START_OFFSET
		scale = Vector2(0.5, 0.5)
		modulate.a = 0.0  # Start invisible

		# Wait for stagger delay
		var delay: float = stagger_index * DRAW_STAGGER_DELAY
		await get_tree().create_timer(delay).timeout

		# Animate to target position and scale
		entrance_tween = create_tween()
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
		entrance_tween.finished.connect(func() -> void:
			base_position = position
		)

	## Start dragging this card
	func _get_drag_data(at_position: Vector2) -> Variant:
		if not hand_ui or not hand_ui.summoner:
			return null

		# Check if we can afford this card
		var summoner_mana_variant: Variant = hand_ui.summoner.get("mana")
		var summoner_mana: float = summoner_mana_variant if summoner_mana_variant is float else 0.0
		if summoner_mana < card.mana_cost:
			return null

		# Create a visual duplicate as preview
		var preview_node: Node = duplicate(DUPLICATE_USE_INSTANTIATION)
		if not preview_node is Control:
			return null
		var preview: Control = preview_node
		preview.scale = Vector2(HOVER_SCALE, HOVER_SCALE)

		# Create wrapper to control preview offset
		var preview_wrapper: Control = Control.new()
		preview_wrapper.add_child(preview)

		# Position preview so the grab point stays under cursor
		# at_position is where on THIS card the user clicked
		preview.position = -at_position * HOVER_SCALE

		set_drag_preview(preview_wrapper)

		# Card physically leaves the hand (completely invisible)
		visible = false

		# Return drag data
		var drag_data: Dictionary = {
			"card_index": card_index,
			"card": card,
			"source": "hand"
		}
		return drag_data

	## Called when drag ends (whether successful or cancelled)
	func _notification(what: int) -> void:
		if what == NOTIFICATION_DRAG_END:
			# Card returns to hand (if drag was cancelled)
			visible = true

	## Allow clicking to select card
	func _gui_input(event: InputEvent) -> void:
		if event is InputEventMouseButton:
			var mouse_event: InputEventMouseButton = event
			if mouse_event.button_index == MOUSE_BUTTON_LEFT and mouse_event.pressed:
				if hand_ui:
					hand_ui._select_card(card_index)

	## Hover effect - card rises and scales up with elastic bounce
	func _on_mouse_entered() -> void:
		if is_hovered:
			return
		is_hovered = true
		was_recently_hovered = true

		# Stop any pulse glow on the card border
		var card_visual: CardVisual = _get_card_visual()
		if card_visual and card_visual.has_meta("pulse_tween"):
			var pulse_tween_variant: Variant = card_visual.get_meta("pulse_tween")
			var pulse_tween: Tween = pulse_tween_variant if pulse_tween_variant is Tween else null
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
			rotation_tween = create_tween()
			rotation_tween.set_parallel(true)
			rotation_tween.set_trans(Tween.TRANS_BACK)
			rotation_tween.set_ease(Tween.EASE_IN_OUT)
			var rot_x_variant: Variant = shader_material.get_shader_parameter("rot_x_deg")
			var rot_x_start: float = rot_x_variant if rot_x_variant is float else 0.0
			rotation_tween.tween_method(
				func(val: float) -> void:
					if shader_material:
						shader_material.set_shader_parameter("rot_x_deg", val),
				rot_x_start,
				0.0,
				0.3
			)
			var rot_y_variant: Variant = shader_material.get_shader_parameter("rot_y_deg")
			var rot_y_start: float = rot_y_variant if rot_y_variant is float else 0.0
			rotation_tween.tween_method(
				func(val: float) -> void:
					if shader_material:
						shader_material.set_shader_parameter("rot_y_deg", val),
				rot_y_start,
				0.0,
				0.3
			)

		# Reset z_index
		hover_tween.finished.connect(func() -> void:
			z_index = 0
		)

		# Remove hover glow - return border to base element color
		var card_visual: CardVisual = _get_card_visual()
		if card_visual:
			var element_color: Color = card_visual.get_element_color()
			var border_panel_variant: Variant = card_visual.get_node_or_null("BorderPanel")
			var border_panel: Panel = border_panel_variant if border_panel_variant is Panel else null
			if border_panel:
				# Reset border to base element color via style
				var border_style: StyleBoxFlat = StyleBoxFlat.new()
				border_style.bg_color = element_color
				border_style.set_corner_radius_all(card_visual.corner_radius)
				border_style.anti_aliasing = true
				border_style.anti_aliasing_size = 1
				border_panel.add_theme_stylebox_override("panel", border_style)

	## Update glow effect based on hover state and playability
	func _update_hover_glow(active: bool) -> void:
		var card_visual: CardVisual = _get_card_visual()
		if not card_visual:
			return

		# Only glow if card is affordable
		var summoner_mana_variant: Variant = hand_ui.summoner.get("mana") if hand_ui and hand_ui.summoner else null
		var summoner_mana: float = summoner_mana_variant if summoner_mana_variant is float else 0.0
		var can_afford: bool = hand_ui and hand_ui.summoner and summoner_mana >= card.mana_cost

		if not can_afford:
			return

		# Get element color for this card
		var element_color: Color = card_visual.get_element_color()
		var border_panel_variant: Variant = card_visual.get_node_or_null("BorderPanel")
		var border_panel: Panel = border_panel_variant if border_panel_variant is Panel else null
		if not border_panel:
			return

		var glow_color: Color = element_color.lightened(HandUI.GLOW_BRIGHTNESS_ACTIVE) if active else element_color.lightened(HandUI.GLOW_BRIGHTNESS_IDLE)

		# Apply glow via border style
		var border_style: StyleBoxFlat = StyleBoxFlat.new()
		border_style.bg_color = glow_color
		border_style.set_corner_radius_all(card_visual.corner_radius)
		border_style.anti_aliasing = true
		border_style.anti_aliasing_size = 1
		border_panel.add_theme_stylebox_override("panel", border_style)

var summoner: Node  # Can be Summoner or Summoner3D
var card_displays: Array[Control] = []
var selected_card_index: int = -1  # -1 means no selection
var is_rebuilding: bool = false  # Prevents concurrent rebuilds

signal card_selected(index: int)

func _ready() -> void:
	add_to_group("hand_ui")

	# Block clicks to battlefield
	mouse_filter = Control.MOUSE_FILTER_STOP

	# Wait one frame to ensure summoners have joined their groups
	await get_tree().process_frame

	# Find player summoner (2D or 3D)
	var summoners: Array[Node] = get_tree().get_nodes_in_group("summoners")
	for node: Node in summoners:
		var is_player: bool = false

		# Check for both Summoner and Summoner3D with proper type checking
		if node is Summoner:
			var summoner_2d: Summoner = node
			var team_variant: Variant = summoner_2d.get("team")
			var team_value: int = team_variant if team_variant is int else -1
			is_player = team_value == Unit.Team.PLAYER
		elif node is Summoner3D:
			var summoner_3d: Summoner3D = node
			var team_variant: Variant = summoner_3d.get("team")
			var team_value: int = team_variant if team_variant is int else -1
			is_player = team_value == Unit3D.Team.PLAYER

		if is_player:
			summoner = node
			break

	if not summoner:
		push_error("HandUI: Could not find player Summoner!")
		return

	# Connect to summoner signals
	var card_played_signal: Signal = summoner.get("card_played")
	card_played_signal.connect(_on_card_played)
	var card_drawn_signal: Signal = summoner.get("card_drawn")
	card_drawn_signal.connect(_on_card_drawn)
	var mana_changed_signal: Signal = summoner.get("mana_changed")
	mana_changed_signal.connect(_on_mana_changed)

	# Initial hand display
	_rebuild_hand_display()

func _exit_tree() -> void:
	# Disconnect summoner signals to prevent memory leaks
	if summoner:
		var card_played_signal: Signal = summoner.get("card_played")
		if card_played_signal.is_connected(_on_card_played):
			card_played_signal.disconnect(_on_card_played)
		var card_drawn_signal: Signal = summoner.get("card_drawn")
		if card_drawn_signal.is_connected(_on_card_drawn):
			card_drawn_signal.disconnect(_on_card_drawn)
		var mana_changed_signal: Signal = summoner.get("mana_changed")
		if mana_changed_signal.is_connected(_on_mana_changed):
			mana_changed_signal.disconnect(_on_mana_changed)

func _rebuild_hand_display() -> void:
	# Prevent concurrent rebuilds (race condition protection)
	if is_rebuilding:
		return
	is_rebuilding = true

	# Clear existing displays with proper cleanup
	for display: Control in card_displays:
		if display and is_instance_valid(display):
			display.queue_free()
	card_displays.clear()

	# Wait one frame to ensure old nodes are freed before creating new ones
	await get_tree().process_frame

	var hand_variant: Variant = summoner.get("hand") if summoner else null
	var hand: Array = hand_variant if hand_variant is Array else []

	if not summoner or hand.is_empty():
		is_rebuilding = false
		return

	# Create card displays
	var total_width: float = hand.size() * CARD_WIDTH + (hand.size() - 1) * CARD_SPACING
	var start_x: float = (size.x - total_width) / 2

	for i: int in range(hand.size()):
		var card: Card = hand[i]
		var card_display_variant: Variant = _create_card_display(card, i)
		if not card_display_variant is CardDisplay:
			continue
		var card_display: CardDisplay = card_display_variant
		card_display.position = Vector2(start_x + i * (CARD_WIDTH + CARD_SPACING), 10)
		add_child(card_display)
		card_displays.append(card_display)

		# Play entrance animation with stagger
		card_display.play_entrance_animation(i)

	# Highlight selected card
	_update_selection_visual()

	# Rebuild complete
	is_rebuilding = false

func _create_card_display(card: Card, index: int) -> CardDisplay:
	var container: CardDisplay = CardDisplay.new()
	container.custom_minimum_size = Vector2(CARD_WIDTH, CARD_HEIGHT)
	container.size = Vector2(CARD_WIDTH, CARD_HEIGHT)
	container.name = "CardDisplay%d" % index
	container.card = card
	container.card_index = index
	container.hand_ui = self

	# Create SubViewport to render card content
	var viewport: SubViewport = SubViewport.new()
	viewport.name = "Viewport"
	viewport.size = Vector2i(CARD_WIDTH, CARD_HEIGHT)
	viewport.transparent_bg = true
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS

	# Create SubViewportContainer to display and apply shader
	var viewport_container: SubViewportContainer = SubViewportContainer.new()
	viewport_container.name = "ViewportContainer"
	viewport_container.size = Vector2(CARD_WIDTH, CARD_HEIGHT)
	viewport_container.stretch = true
	viewport_container.mouse_filter = Control.MOUSE_FILTER_IGNORE
	container.add_child(viewport_container)
	viewport_container.add_child(viewport)

	# Instantiate CardVisual scene (preloaded at class level)
	var card_visual_variant: Variant = CARD_VISUAL_SCENE.instantiate()
	var card_visual: CardVisual = card_visual_variant if card_visual_variant is CardVisual else null
	if not card_visual:
		push_error("HandUI: Failed to instantiate CardVisual")
		return container

	# Configure card visual for in-hand display
	card_visual.size = Vector2(CARD_WIDTH, CARD_HEIGHT)
	card_visual.custom_minimum_size = Vector2(CARD_WIDTH, CARD_HEIGHT)
	card_visual.border_width = 3
	card_visual.corner_radius = 8
	card_visual.cost_font_size = 20
	card_visual.name_font_size = 14
	card_visual.show_description = false
	card_visual.mouse_filter = Control.MOUSE_FILTER_IGNORE

	# Get catalog data to pass to CardVisual
	var catalog_data: Dictionary = CardCatalog.get_card(card.catalog_id)

	# Set card data
	card_visual.set_card_data(catalog_data)

	# Add to viewport
	viewport.add_child(card_visual)

	return container

func _select_card(index: int) -> void:
	var hand_variant: Variant = summoner.get("hand") if summoner else null
	var hand: Array = hand_variant if hand_variant is Array else []

	if not summoner or index < 0 or index >= hand.size():
		return

	selected_card_index = index
	_update_selection_visual()
	card_selected.emit(index)

func _update_selection_visual() -> void:
	for i: int in range(card_displays.size()):
		if i >= card_displays.size():
			continue

		var display_variant: Variant = card_displays[i]
		var display: CardDisplay = display_variant if display_variant is CardDisplay else null
		if not display:
			continue

		var card_visual: CardVisual = display._get_card_visual()
		if not card_visual:
			continue

		var border_panel_variant: Variant = card_visual.get_node_or_null("BorderPanel")
		var border_panel: Panel = border_panel_variant if border_panel_variant is Panel else null
		if not border_panel:
			continue

		var border_style: StyleBoxFlat = StyleBoxFlat.new()
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

	var hand_variant: Variant = summoner.get("hand")
	var hand: Array = hand_variant if hand_variant is Array else []
	var mana_variant: Variant = summoner.get("mana")
	var summoner_mana: float = mana_variant if mana_variant is float else 0.0

	for i: int in range(card_displays.size()):
		if i >= hand.size():
			continue

		var card: Card = hand[i]
		var display_variant: Variant = card_displays[i]
		var display: CardDisplay = display_variant if display_variant is CardDisplay else null
		if not display:
			continue

		var card_visual: CardVisual = display._get_card_visual()
		if not card_visual:
			continue

		var bg_panel_variant: Variant = card_visual.get_node_or_null("BackgroundPanel")
		var bg_panel: Panel = bg_panel_variant if bg_panel_variant is Panel else null
		var border_panel_variant: Variant = card_visual.get_node_or_null("BorderPanel")
		var border_panel: Panel = border_panel_variant if border_panel_variant is Panel else null

		# Check affordability
		var can_afford: bool = summoner_mana >= card.mana_cost

		if can_afford:
			# Playable: normal background
			if bg_panel:
				var bg_style: StyleBoxFlat = StyleBoxFlat.new()
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
				var bg_style: StyleBoxFlat = StyleBoxFlat.new()
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
					var pulse_tween_variant: Variant = card_visual.get_meta("pulse_tween")
					var pulse_tween: Tween = pulse_tween_variant if pulse_tween_variant is Tween else null
					if pulse_tween and pulse_tween.is_valid():
						pulse_tween.kill()
					card_visual.remove_meta("pulse_tween")

				# Dim the element color for unaffordable cards
				if border_panel:
					var border_style: StyleBoxFlat = StyleBoxFlat.new()
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
		var existing_variant: Variant = card_visual.get_meta("pulse_tween")
		var existing: Tween = existing_variant if existing_variant is Tween else null
		if existing and existing.is_valid():
			return  # Already pulsing

	# Kill any existing tween first
	if card_visual.has_meta("pulse_tween"):
		var old_tween_variant: Variant = card_visual.get_meta("pulse_tween")
		var old_tween: Tween = old_tween_variant if old_tween_variant is Tween else null
		if old_tween and old_tween.is_valid():
			old_tween.kill()

	var border_panel_variant: Variant = card_visual.get_node_or_null("BorderPanel")
	var border_panel: Panel = border_panel_variant if border_panel_variant is Panel else null
	if not border_panel:
		return

	# Store tween reference on the card_visual node
	var pulse_tween: Tween = create_tween()
	card_visual.set_meta("pulse_tween", pulse_tween)

	pulse_tween.set_loops()
	pulse_tween.set_trans(Tween.TRANS_SINE)
	pulse_tween.set_ease(Tween.EASE_IN_OUT)

	# Get element color for this card
	var element_color: Color = card_visual.get_element_color()

	# Pulse between dim and bright element color
	var dim_color: Color = element_color.darkened(PULSE_BRIGHTNESS_OFFSET)
	var bright_color: Color = element_color.lightened(PULSE_BRIGHTNESS_OFFSET)

	# Create a custom method to update border color via StyleBox
	var update_border_color: Callable = func(color: Color) -> void:
		if border_panel and card_visual:
			var style: StyleBoxFlat = StyleBoxFlat.new()
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
	var hand_variant: Variant = summoner.get("hand") if summoner else null
	var hand: Array = hand_variant if hand_variant is Array else []

	if not summoner or hand.is_empty():
		return
	selected_card_index = (selected_card_index + 1) % hand.size()
	_update_selection_visual()
	card_selected.emit(selected_card_index)

func select_card_by_index(index: int) -> void:
	_select_card(index)
