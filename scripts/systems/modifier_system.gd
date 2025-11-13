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

func register_provider(provider_id: String, provider: Object) -> void:
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
	var all_modifiers: Array = []

	# Collect modifiers from all providers
	for provider_id: String in _providers.keys():
		var provider_variant: Variant = _providers[provider_id]
		var provider: Object = provider_variant
		if provider != null and provider.has_method("get_modifiers"):
			var provider_mods: Array = provider.call("get_modifiers")
			all_modifiers.append_array(provider_mods)

	# Filter by conditions
	var filtered: Array = []
	for mod_variant: Variant in all_modifiers:
		var mod: Dictionary = mod_variant
		if mod != null and _matches_conditions(mod, categories, context):
			filtered.append(mod)

	# Apply amplification
	filtered = _apply_amplification(filtered)

	return filtered

## =============================================================================
## CONDITION MATCHING
## =============================================================================

func _matches_conditions(modifier: Dictionary, categories: Dictionary, context: Dictionary) -> bool:
	var conditions: Dictionary = modifier.get("conditions", {})

	# If no conditions, modifier always applies
	if conditions.is_empty():
		return true

	# Check each condition
	for condition_key_variant: Variant in conditions.keys():
		var condition_key: String = condition_key_variant
		var required_value: Variant = conditions[condition_key]
		var actual_value: Variant = categories.get(condition_key)

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
func _matches_element(actual: Variant, required: Variant) -> bool:
	# Null safety - if either is null, no match
	if actual == null or required == null:
		return false

	# Handle Element objects (from ElementTypes) - check origin inheritance
	var actual_obj: Object = actual
	if actual_obj != null and actual_obj.has_method("matches_affinity"):
		return actual_obj.call("matches_affinity", required)

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
	var amplifiers: Dictionary = {}  # tag -> total multiplier

	for mod_variant: Variant in modifiers:
		var mod_dict: Dictionary = mod_variant
		if mod_dict != null and mod_dict.has("amplify_tag"):
			var tag: Variant = mod_dict.get("amplify_tag")
			var factor: float = mod_dict.get("factor", 1.0)

			if not amplifiers.has(tag):
				amplifiers[tag] = 1.0
			amplifiers[tag] *= factor

	# Step 2: Apply amplification to tagged modifiers
	for mod_variant2: Variant in modifiers:
		var mod_dict: Dictionary = mod_variant2
		if mod_dict != null and mod_dict.has("tags") and not mod_dict.has("amplify_tag"):  # Don't amplify amplifiers
			var total_amp: float = 1.0

			# Check all tags on this modifier
			var tags: Variant = mod_dict.get("tags")
			if tags is Array:
				var tags_array: Array = tags
				for tag_variant: Variant in tags_array:
					if amplifiers.has(tag_variant):
						total_amp *= amplifiers[tag_variant]

			# Amplify bonuses (not base values)
			if total_amp != 1.0:
				# Amplify additive bonuses
				var stat_adds_variant: Variant = mod_dict.get("stat_adds")
				if stat_adds_variant != null and mod_dict.has("stat_adds"):
					var stat_adds_dict: Dictionary = stat_adds_variant
					if stat_adds_dict != null:
						for stat_variant: Variant in stat_adds_dict.keys():
							var stat: String = stat_variant
							if stat != null:
								stat_adds_dict[stat] *= total_amp

				# Amplify multiplicative bonuses
				var stat_mults_variant: Variant = mod_dict.get("stat_mults")
				if stat_mults_variant != null and mod_dict.has("stat_mults"):
					var stat_mults_dict: Dictionary = stat_mults_variant
					if stat_mults_dict != null:
						for stat_variant2: Variant in stat_mults_dict.keys():
							var stat: String = stat_variant2
							if stat != null:
								var bonus: float = stat_mults_dict[stat] - 1.0
								bonus *= total_amp
								stat_mults_dict[stat] = 1.0 + bonus

	return modifiers

## =============================================================================
## DEBUGGING
## =============================================================================

func debug_print_providers() -> void:
	print("=== ModifierSystem Debug ===")
	print("Registered providers: %d" % _providers.size())
	for provider_id_variant: Variant in _providers.keys():
		var provider_id: String = provider_id_variant
		if provider_id != null:
			print("  - %s" % provider_id)

func debug_print_modifiers(categories: Dictionary = {}) -> void:
	var modifiers: Array = get_modifiers_for("unit", categories, {})
	print("=== Modifiers for categories: %s ===" % categories)
	print("Total modifiers: %d" % modifiers.size())
	for mod_variant: Variant in modifiers:
		var mod_dict: Dictionary = mod_variant
		if mod_dict != null:
			print("  - Source: %s" % mod_dict.get("source", "unknown"))
			print("    Tags: %s" % mod_dict.get("tags", []))
			print("    Stat mults: %s" % mod_dict.get("stat_mults", {}))
			print("    Stat adds: %s" % mod_dict.get("stat_adds", {}))
