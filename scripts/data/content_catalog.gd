extends Node

## Central content database for projectile definitions
## Loads projectile data from JSON files at startup
## Note: Cards are managed by CardCatalog (hardcoded), not here
## Autoload as: /root/ContentCatalog

var projectiles: Dictionary = {}  ## projectile_id -> ProjectileData

var _is_loaded: bool = false

signal content_loaded()

func _ready() -> void:
	print("ContentCatalog: Initializing...")
	_load_projectiles()
	_validate_content()
	_is_loaded = true
	content_loaded.emit()
	print("ContentCatalog: Loaded %d projectiles" % projectiles.size())

## Load projectiles from data/projectiles/*.json
func _load_projectiles() -> void:
	var proj_dir: String = "res://data/projectiles/"
	var dir: DirAccess = DirAccess.open(proj_dir)

	if not dir:
		push_warning("ContentCatalog: projectiles directory not found: " + proj_dir)
		return

	dir.list_dir_begin()
	var file_name: String = dir.get_next()

	while file_name != "":
		if file_name.ends_with(".json"):
			var file_path: String = proj_dir + file_name
			var proj_data: ProjectileData = _load_projectile_from_file(file_path)
			if proj_data:
				projectiles[proj_data.projectile_id] = proj_data
		file_name = dir.get_next()

	dir.list_dir_end()

## Load single projectile from JSON file
func _load_projectile_from_file(file_path: String) -> ProjectileData:
	var file: FileAccess = FileAccess.open(file_path, FileAccess.READ)
	if not file:
		push_error("ContentCatalog: Failed to open file: " + file_path)
		return null

	var json_text: String = file.get_as_text()
	file.close()

	var json: JSON = JSON.new()
	var parse_result: Error = json.parse(json_text)

	if parse_result != OK:
		push_error("ContentCatalog: JSON parse error in %s at line %d: %s" % [
			file_path,
			json.get_error_line(),
			json.get_error_message()
		])
		return null

	var data: Variant = json.get_data()
	if not data is Dictionary:
		push_error("ContentCatalog: JSON root is not a dictionary: " + file_path)
		return null

	var data_dict: Dictionary = data
	return ProjectileData.from_dict(data_dict)

## Validate projectile data for consistency
func _validate_content() -> void:
	var errors: Array[String] = []

	# Check for invalid projectile data
	for proj_data: ProjectileData in projectiles.values():
		if proj_data.speed <= 0:
			errors.append("Projectile '%s' has invalid speed: %.1f" % [proj_data.projectile_id, proj_data.speed])

	# Report errors
	if errors.size() > 0:
		push_error("ContentCatalog: Found %d validation errors:" % errors.size())
		for error: String in errors:
			push_error("  - %s" % error)
	else:
		print("ContentCatalog: All projectiles validated successfully")

## Get projectile data by ID
func get_projectile(projectile_id: String) -> ProjectileData:
	return projectiles.get(projectile_id)

## Check if projectile exists
func has_projectile(projectile_id: String) -> bool:
	return projectiles.has(projectile_id)

## Check if content is loaded
func is_loaded() -> bool:
	return _is_loaded
