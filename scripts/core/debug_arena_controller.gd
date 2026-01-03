extends TestGameController
class_name DebugArenaController

## Debug Arena Controller
## - Infinite mana for both sides
## - Infinite HP for both towers
## - No AI spawning (empty enemy deck)
## - No player hand (manual spawning only)
## - Pause/resume without pause menu
## - Optional skip prep phase

signal unit_spawned(unit: Node3D, team: int)
signal units_cleared(count: int)

var _spawner_panel: UnitSpawnerPanel
var _debug_paused: bool = false


func _ready() -> void:
	# Configure BattleContext for debug mode
	var battle_context: Node = get_node_or_null("/root/BattleContext")
	if battle_context and battle_context.has_method("configure_practice_battle"):
		battle_context.call("configure_practice_battle", {
			"enemy_deck": [],  # Empty = no AI spawning
			"enemy_hp": 999999.0,
			"ai_type": "passive"  # AI does nothing
		})

	# Call parent ready
	super._ready()

	# Connect to spawner panel
	await get_tree().process_frame
	_connect_spawner_panel()

	# Check if we should skip prep phase
	if _spawner_panel and _spawner_panel.get_skip_prep_phase():
		skip_prep_phase()


## Override _init_summoners to set DEFERRED strategy before init() is called
func _init_summoners() -> void:
	# Discover summoners first
	if player_summoner == null:
		player_summoner = get_tree().get_first_node_in_group(GroupIDs.PLAYER_SUMMONERS)
	if enemy_summoner == null:
		enemy_summoner = get_tree().get_first_node_in_group(GroupIDs.ENEMY_SUMMONERS)

	# Set DEFERRED strategy BEFORE calling init() to allow empty decks
	if player_summoner:
		player_summoner.deck_load_strategy = Summoner.DeckLoadStrategy.DEFERRED
	if enemy_summoner:
		enemy_summoner.deck_load_strategy = Summoner.DeckLoadStrategy.DEFERRED

	# Call init() on summoners
	if player_summoner and player_summoner.has_method("init"):
		player_summoner.init()

	if enemy_summoner and enemy_summoner.has_method("init"):
		enemy_summoner.init()


## Override to prevent loading player deck (use drag-drop only)
func _load_test_deck_for_summoner(summoner: Summoner) -> void:
	# Don't load any cards - use drag-drop spawning only
	summoner.deck = []
	summoner.hand.clear()


## Override to prevent loading enemy deck
func _load_enemy_test_deck(summoner: Summoner) -> void:
	# Don't load any cards - no AI spawning
	summoner.deck = []
	summoner.hand.clear()


func _connect_spawner_panel() -> void:
	_spawner_panel = _find_spawner_panel()
	if _spawner_panel:
		if not _spawner_panel.clear_requested.is_connected(clear_all_units):
			_spawner_panel.clear_requested.connect(clear_all_units)
		if not _spawner_panel.pause_toggled.is_connected(_on_pause_toggled):
			_spawner_panel.pause_toggled.connect(_on_pause_toggled)
		if not _spawner_panel.skip_prep_toggled.is_connected(_on_skip_prep_toggled):
			_spawner_panel.skip_prep_toggled.connect(_on_skip_prep_toggled)


func _find_spawner_panel() -> UnitSpawnerPanel:
	var nodes: Array[Node] = get_tree().get_nodes_in_group("ui_layer")
	for node: Node in nodes:
		if node is UnitSpawnerPanel:
			return node as UnitSpawnerPanel
		var panel: UnitSpawnerPanel = _find_child_of_type(node)
		if panel:
			return panel

	for child: Node in get_children():
		var panel: UnitSpawnerPanel = _find_child_of_type(child)
		if panel:
			return panel

	return null


func _find_child_of_type(node: Node) -> UnitSpawnerPanel:
	if node is UnitSpawnerPanel:
		return node as UnitSpawnerPanel

	for child: Node in node.get_children():
		var result: UnitSpawnerPanel = _find_child_of_type(child)
		if result:
			return result

	return null


## Handle pause toggle from spawner panel
func _on_pause_toggled(is_paused: bool) -> void:
	_debug_paused = is_paused
	# Use Engine.time_scale instead of tree.paused so UI stays responsive
	Engine.time_scale = 0.0 if is_paused else 1.0


## Handle skip prep phase toggle
func _on_skip_prep_toggled(skip: bool) -> void:
	# If toggled on during prep phase, skip immediately
	if skip and current_phase == BattlePhase.PREPARATION:
		skip_prep_phase()


## Spawn a unit at the given position with the given team
func spawn_debug_unit(catalog_id: String, world_position: Vector3, team: int) -> Node3D:
	var card: Card = CardCatalog.create_card_resource(catalog_id)
	if not card:
		push_error("DebugArenaController: Failed to create card: %s" % catalog_id)
		return null

	# Get battlefield
	var battlefield: Node = get_tree().get_first_node_in_group("battlefield")
	if not battlefield:
		push_error("DebugArenaController: No battlefield found")
		return null

	# Get team enum
	var unit_team: UnitConstants.Team = UnitConstants.Team.PLAYER if team == 0 else UnitConstants.Team.ENEMY

	# Spawn directly using card.play_3d
	var modifier_system: Node = get_node_or_null("/root/ModifierSystem")
	card.play_3d(world_position, unit_team, battlefield, modifier_system)

	# Wait a frame for unit to spawn
	await get_tree().process_frame

	# Find and activate the spawned unit
	var units: Array[Node] = get_tree().get_nodes_in_group(GroupIDs.UNITS)
	if units.size() > 0:
		var unit: Node3D = units[-1] as Node3D
		# Activate unit immediately so it's not frozen
		if unit.has_method("Activate"):
			unit.Activate()
		unit_spawned.emit(unit, team)
		return unit

	return null


## Clear all units from the battlefield
func clear_all_units() -> void:
	var units: Array[Node] = get_tree().get_nodes_in_group(GroupIDs.UNITS)
	var count: int = units.size()

	for unit: Node in units:
		if is_instance_valid(unit):
			unit.queue_free()

	units_cleared.emit(count)
