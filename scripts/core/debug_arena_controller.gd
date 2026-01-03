extends TestGameController
class_name DebugArenaController

## Debug Arena Controller
## - Infinite mana for both players
## - Infinite HP for both towers
## - No AI spawning (empty enemy deck)
## - Allows manual unit spawning via drag-and-drop panel

signal unit_spawned(unit: Node3D, team: int)
signal units_cleared(count: int)


func _ready() -> void:
	print("DebugArenaController: Initializing debug arena...")

	# Configure BattleContext for debug mode
	var battle_context: Node = get_node_or_null("/root/BattleContext")
	if battle_context and battle_context.has_method("configure_practice_battle"):
		battle_context.call("configure_practice_battle", {
			"enemy_deck": [],  # Empty = no AI spawning
			"enemy_hp": 999999.0,
			"ai_type": "passive"  # AI does nothing
		})

	# Call parent ready (which handles infinite mana/HP)
	super._ready()

	# Connect to spawner panel if present
	await get_tree().process_frame
	_connect_spawner_panel()

	print("DebugArenaController: Debug arena ready!")
	print("  - Infinite mana enabled")
	print("  - Infinite HP for both sides")
	print("  - No AI unit spawning")
	print("  - Drag units from panel to spawn")


func _connect_spawner_panel() -> void:
	var spawner_panel: UnitSpawnerPanel = _find_spawner_panel()
	if spawner_panel:
		spawner_panel.clear_requested.connect(clear_all_units)
		print("DebugArenaController: Connected to UnitSpawnerPanel")


func _find_spawner_panel() -> UnitSpawnerPanel:
	# Search for spawner panel in UI layer
	var nodes: Array[Node] = get_tree().get_nodes_in_group("ui_layer")
	for node: Node in nodes:
		var panel: UnitSpawnerPanel = _find_child_of_type(node, "UnitSpawnerPanel")
		if panel:
			return panel

	# Also search direct children
	for child: Node in get_children():
		var panel: UnitSpawnerPanel = _find_child_of_type(child, "UnitSpawnerPanel")
		if panel:
			return panel

	return null


func _find_child_of_type(node: Node, type_name: String) -> UnitSpawnerPanel:
	if node.get_class() == type_name or (node is UnitSpawnerPanel):
		return node as UnitSpawnerPanel

	for child: Node in node.get_children():
		var result: UnitSpawnerPanel = _find_child_of_type(child, type_name)
		if result:
			return result

	return null


## Spawn a unit at the given position with the given team
## Called by BattlefieldDropZone when handling debug_spawn drops
func spawn_debug_unit(catalog_id: String, position: Vector3, team: int) -> Node3D:
	print("DebugArenaController: Spawning %s at %s for team %d" % [catalog_id, position, team])

	var card: Card = CardCatalog.create_card_resource(catalog_id)
	if not card:
		push_error("DebugArenaController: Failed to create card: %s" % catalog_id)
		return null

	var summoner: Summoner = player_summoner if team == 0 else enemy_summoner
	if not summoner:
		push_error("DebugArenaController: No summoner for team %d" % team)
		return null

	# Add card to summoner's hand temporarily
	summoner.hand.append(card)
	var card_index: int = summoner.hand.size() - 1

	# Play the card
	if summoner.play_card_3d(card_index, position):
		# Find the spawned unit
		await get_tree().process_frame
		var units: Array[Node] = get_tree().get_nodes_in_group(GroupIDs.UNITS)
		if units.size() > 0:
			var unit: Node3D = units[-1] as Node3D
			unit_spawned.emit(unit, team)
			print("DebugArenaController: Spawned %s successfully" % catalog_id)
			return unit
	else:
		# Remove card from hand if play failed
		summoner.hand.erase(card)
		push_error("DebugArenaController: Failed to play card %s" % catalog_id)

	return null


## Clear all units from the battlefield
func clear_all_units() -> void:
	var units: Array[Node] = get_tree().get_nodes_in_group(GroupIDs.UNITS)
	var count: int = units.size()

	for unit: Node in units:
		if is_instance_valid(unit):
			unit.queue_free()

	units_cleared.emit(count)
	print("DebugArenaController: Cleared %d units" % count)


## Override to prevent loading enemy deck
func _load_enemy_test_deck(summoner: Summoner) -> void:
	# Don't load any cards for enemy - we want manual spawning only
	summoner.deck = []
	summoner.hand.clear()
	print("DebugArenaController: Enemy deck cleared (manual spawning mode)")
