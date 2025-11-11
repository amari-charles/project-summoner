extends Node
# Note: No class_name needed - this is registered as an autoload

## ModifierSystem - Central service for managing and applying modifiers
##
## Collects modifiers from various providers (heroes, items, buffs, etc.),
## filters by conditions, and provides them to targets for application.

## Registered modifier providers
var _providers: Dictionary = {}  # provider_id -> provider object

## =============================================================================
## PROVIDER REGISTRATION
## =============================================================================

func register_provider(provider_id: String, provider) -> void:
	if _providers.has(provider_id):
		push_warning("ModifierSystem: Provider '%s' already registered, replacing" % provider_id)

	_providers[provider_id] = provider

func unregister_provider(provider_id: String) -> void:
	_providers.erase(provider_id)

func clear_providers() -> void:
	_providers.clear()

## =============================================================================
## MODIFIER COLLECTION
## =============================================================================

## Get all modifiers that apply to a target
##
## @param target_type: Type of target ("unit", "card", "summoner", etc.)
## @param categories: Dictionary of card/unit categories for condition matching
## @param context: Additional context (hero_id, team, etc.)
## @return Array of modifier dictionaries
func get_modifiers_for(target_type: String, categories: Dictionary, context: Dictionary = {}) -> Array:
	var all_modifiers = []

	# Collect modifiers from all providers
	for provider_id in _providers.keys():
		var provider = _providers[provider_id]
		if provider.has_method("get_modifiers"):
			var provider_mods = provider.get_modifiers()
			all_modifiers.append_array(provider_mods)

	# Filter by conditions
	var filtered = []
	for mod in all_modifiers:
		if _matches_conditions(mod, categories, context):
			filtered.append(mod)

	# Apply amplification
	filtered = _apply_amplification(filtered)

	return filtered

## =============================================================================
## CONDITION MATCHING
## =============================================================================

func _matches_conditions(modifier: Dictionary, categories: Dictionary, context: Dictionary) -> bool:
	var conditions = modifier.get("conditions", {})

	# If no conditions, modifier always applies
	if conditions.is_empty():
		return true

	# Check each condition
	for condition_key in conditions.keys():
		var required_value = conditions[condition_key]
		var actual_value = categories.get(condition_key)

		# Handle array values (tags)
		if actual_value is Array:
			if not required_value in actual_value:
				return false
		# Special handling for elemental_affinity with Element objects
		elif condition_key == "elemental_affinity":
			if not _matches_element(actual_value, required_value):
				return false
		else:
			if actual_value != required_value:
				return false

	return true

## Helper: Check if actual element matches required (including origin check)
func _matches_element(actual, required) -> bool:
	# Null safety - if either is null, no match
	if actual == null or required == null:
		return false

	# Handle Element objects (from ElementTypes) - check origin inheritance
	if actual != null and actual.has_method("matches_affinity"):
		return actual.matches_affinity(required)

	# Handle string comparison (backwards compatibility)
	if actual is String and required is String:
		return actual == required

	# Incompatible types - no match
	return false

## =============================================================================
## AMPLIFICATION
## =============================================================================

## Apply amplification modifiers to other modifiers
## Amplifiers multiply the bonuses provided by tagged modifiers
func _apply_amplification(modifiers: Array) -> Array:
	# Step 1: Find all amplifiers and calculate total amplification per tag
	var amplifiers = {}  # tag -> total multiplier

	for mod in modifiers:
		if mod.has("amplify_tag"):
			var tag = mod.amplify_tag
			var factor = mod.get("factor", 1.0)

			if not amplifiers.has(tag):
				amplifiers[tag] = 1.0
			amplifiers[tag] *= factor

	# Step 2: Apply amplification to tagged modifiers
	for mod in modifiers:
		if mod.has("tags") and not mod.has("amplify_tag"):  # Don't amplify amplifiers
			var total_amp = 1.0

			# Check all tags on this modifier
			for tag in mod.tags:
				if amplifiers.has(tag):
					total_amp *= amplifiers[tag]

			# Amplify bonuses (not base values)
			if total_amp != 1.0:
				# Amplify additive bonuses
				if mod.has("stat_adds"):
					for stat in mod.stat_adds.keys():
						mod.stat_adds[stat] *= total_amp

				# Amplify multiplicative bonuses
				if mod.has("stat_mults"):
					for stat in mod.stat_mults.keys():
						var bonus = mod.stat_mults[stat] - 1.0
						bonus *= total_amp
						mod.stat_mults[stat] = 1.0 + bonus

	return modifiers

## =============================================================================
## DEBUGGING
## =============================================================================

func debug_print_providers() -> void:
	print("=== ModifierSystem Debug ===")
	print("Registered providers: %d" % _providers.size())
	for provider_id in _providers.keys():
		print("  - %s" % provider_id)

func debug_print_modifiers(categories: Dictionary = {}) -> void:
	var modifiers = get_modifiers_for("unit", categories, {})
	print("=== Modifiers for categories: %s ===" % categories)
	print("Total modifiers: %d" % modifiers.size())
	for mod in modifiers:
		print("  - Source: %s" % mod.get("source", "unknown"))
		print("    Tags: %s" % mod.get("tags", []))
		print("    Stat mults: %s" % mod.get("stat_mults", {}))
		print("    Stat adds: %s" % mod.get("stat_adds", {}))
