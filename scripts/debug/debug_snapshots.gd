extends Node

## DebugSnapshots - Save/Load Profile State Checkpoints
##
## Allows saving and loading complete profile states for testing.
## Useful for jumping to specific tutorial checkpoints without replaying everything.
##
## Usage:
##   DebugSnapshots.save_snapshot("battle_00_event_2_complete")
##   DebugSnapshots.load_snapshot("battle_00_event_2_complete")
##   var snapshots: Array = DebugSnapshots.list_snapshots()

## Directory for storing snapshots (gitignored)
const SNAPSHOTS_DIR: String = "user://debug/snapshots/"

## Service references
var _repo: Variant = null  # ProfileRepo autoload (C# service)

## Cached list of available snapshots
var _snapshot_cache: Array[Dictionary] = []

## =============================================================================
## LIFECYCLE
## =============================================================================

func _ready() -> void:
	print("DebugSnapshots: Initializing...")

	# Ensure snapshots directory exists
	_ensure_directory_exists()

	# Wait for services to be ready
	await get_tree().process_frame

	_repo = ProfileRepo

	# Load snapshot list
	refresh_snapshot_list()

	print("DebugSnapshots: Ready (%d snapshots available)" % _snapshot_cache.size())

## =============================================================================
## PUBLIC API
## =============================================================================

## Save current profile state as a named snapshot
## Returns: true on success
func save_snapshot(snapshot_name: String) -> bool:
	if not _repo:
		push_error("DebugSnapshots: ProfileRepo not available")
		return false

	if snapshot_name.is_empty():
		push_error("DebugSnapshots: Snapshot name cannot be empty")
		return false

	# Get current profile data
	var profile_data: Dictionary = _repo.call("get_profile_data")
	if profile_data.is_empty():
		push_error("DebugSnapshots: Failed to get profile data")
		return false

	# Create snapshot metadata
	var snapshot: Dictionary = {
		"name": snapshot_name,
		"created_at": Time.get_datetime_string_from_system(),
		"timestamp": Time.get_unix_time_from_system(),
		"profile_data": profile_data
	}

	# Save to file
	var file_path: String = _get_snapshot_path(snapshot_name)
	var file: FileAccess = FileAccess.open(file_path, FileAccess.WRITE)
	if not file:
		push_error("DebugSnapshots: Failed to create snapshot file: %s" % file_path)
		return false

	var json_string: String = JSON.stringify(snapshot, "\t")
	file.store_string(json_string)
	file.close()

	# Refresh cache
	refresh_snapshot_list()

	print("DebugSnapshots: Saved snapshot '%s' to %s" % [snapshot_name, file_path])
	return true

## Load a snapshot and restore profile state
## Returns: true on success
func load_snapshot(snapshot_name: String) -> bool:
	if not _repo:
		push_error("DebugSnapshots: ProfileRepo not available")
		return false

	var file_path: String = _get_snapshot_path(snapshot_name)
	if not FileAccess.file_exists(file_path):
		push_error("DebugSnapshots: Snapshot not found: %s" % snapshot_name)
		return false

	# Load snapshot file
	var file: FileAccess = FileAccess.open(file_path, FileAccess.READ)
	if not file:
		push_error("DebugSnapshots: Failed to open snapshot file: %s" % file_path)
		return false

	var json_string: String = file.get_as_text()
	file.close()

	# Parse JSON
	var json: JSON = JSON.new()
	var error: Error = json.parse(json_string)
	if error != OK:
		push_error("DebugSnapshots: Failed to parse snapshot JSON: %s" % json.get_error_message())
		return false

	var snapshot: Dictionary = json.data
	if not snapshot.has("profile_data"):
		push_error("DebugSnapshots: Invalid snapshot format")
		return false

	# Restore profile data
	var profile_data: Dictionary = snapshot["profile_data"]
	if not _repo.has_method("load_profile_data"):
		push_error("DebugSnapshots: ProfileRepo doesn't support load_profile_data()")
		return false

	_repo.call("load_profile_data", profile_data)

	print("DebugSnapshots: Loaded snapshot '%s' (created %s)" % [snapshot_name, snapshot.get("created_at", "unknown")])
	return true

## Delete a snapshot
## Returns: true on success
func delete_snapshot(snapshot_name: String) -> bool:
	var file_path: String = _get_snapshot_path(snapshot_name)
	if not FileAccess.file_exists(file_path):
		push_warning("DebugSnapshots: Snapshot not found: %s" % snapshot_name)
		return false

	var dir: DirAccess = DirAccess.open(SNAPSHOTS_DIR)
	if not dir:
		push_error("DebugSnapshots: Failed to open snapshots directory")
		return false

	var error: Error = dir.remove(file_path)
	if error != OK:
		push_error("DebugSnapshots: Failed to delete snapshot: %s" % error)
		return false

	# Refresh cache
	refresh_snapshot_list()

	print("DebugSnapshots: Deleted snapshot '%s'" % snapshot_name)
	return true

## Get list of available snapshots
## Returns: Array of snapshot info dictionaries
func list_snapshots() -> Array[Dictionary]:
	return _snapshot_cache.duplicate()

## Refresh the cached snapshot list
func refresh_snapshot_list() -> void:
	_snapshot_cache.clear()

	var dir: DirAccess = DirAccess.open(SNAPSHOTS_DIR)
	if not dir:
		return

	dir.list_dir_begin()
	var file_name: String = dir.get_next()

	while file_name != "":
		if not dir.current_is_dir() and file_name.ends_with(".json"):
			var snapshot_name: String = file_name.trim_suffix(".json")
			var file_path: String = SNAPSHOTS_DIR + file_name

			# Try to load metadata
			var file: FileAccess = FileAccess.open(file_path, FileAccess.READ)
			if file:
				var json_string: String = file.get_as_text()
				file.close()

				var json: JSON = JSON.new()
				if json.parse(json_string) == OK:
					var snapshot: Dictionary = json.data
					_snapshot_cache.append({
						"name": snapshot_name,
						"created_at": snapshot.get("created_at", "unknown"),
						"timestamp": snapshot.get("timestamp", 0)
					})

		file_name = dir.get_next()

	dir.list_dir_end()

	# Sort by timestamp (newest first)
	_snapshot_cache.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		return a["timestamp"] > b["timestamp"]
	)

## =============================================================================
## PRIVATE HELPERS
## =============================================================================

func _ensure_directory_exists() -> void:
	var dir: DirAccess = DirAccess.open("user://")
	if not dir:
		push_error("DebugSnapshots: Failed to open user:// directory")
		return

	# Create debug directory
	if not dir.dir_exists("debug"):
		dir.make_dir("debug")

	# Create snapshots directory
	if not dir.dir_exists("debug/snapshots"):
		dir.make_dir("debug/snapshots")

func _get_snapshot_path(snapshot_name: String) -> String:
	return SNAPSHOTS_DIR + snapshot_name + ".json"
