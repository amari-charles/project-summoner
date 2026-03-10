using System.Collections.Generic;
using Godot;
using Fateforged.Cards;
using Fateforged.Units;

namespace Fateforged.Stats;

/// <summary>
/// Centralized unit stat calculation with documented order of operations.
/// See docs/technical/runtime/unit-stat-pipeline.md for full architecture details.
///
/// ORDER OF OPERATIONS (Unit Stat Pipeline):
/// 1. Base stats from UnitCatalog (via card.UnitId) or CardDefinition (legacy)
/// 2. Card variant modifier (optional) - for swarms, etc.
/// 3. Card upgrade multipliers (multiplicative) - from PlayerCardService
/// 4. Modifier adds (additive) - from ModifierService providers (traits, buffs)
/// 5. Modifier mults (multiplicative) - from ModifierService providers
/// 6. Custom overrides (replacement) - from event battles, boss fights
///
/// Example MaxHp calculation for Fire Wisp Swarm:
/// - Base (UnitCatalog fire_wisp): 60
/// - Card modifier ×0.75 (swarm variant): 60 × 0.75 = 45
/// - Upgrade ×1.2 (level 5): 45 × 1.2 = 54
/// - Trait +10 (Fire Affinity): 54 + 10 = 64
/// - Buff ×1.1: 64 × 1.1 = 70.4
/// - Override (none): 70.4 (final)
/// </summary>
public static class UnitStatCalculator
{
    /// <summary>
    /// Calculates final unit stats from all sources using the Unit Stat Pipeline.
    /// </summary>
    /// <param name="card">Card definition (may reference UnitCatalog via UnitId)</param>
    /// <param name="upgradeMultipliers">Multiplicative bonuses from card upgrades (null = no upgrades)</param>
    /// <param name="modifiers">Stat modifiers from ModifierService (null = no modifiers)</param>
    /// <param name="overrides">Override values for specific stats (null = no overrides)</param>
    /// <returns>Final calculated UnitStats</returns>
    public static UnitStats Calculate(
        CardDefinition card,
        Dictionary<string, float>? upgradeMultipliers = null,
        List<StatModifier>? modifiers = null,
        Dictionary<string, float>? overrides = null)
    {
        // Step 1: Base stats from UnitCatalog (new) or CardDefinition (legacy)
        var stats = GetBaseStats(card);

        // Step 2: Card variant modifier (for swarms, etc.)
        if (card.UnitModifier != null)
        {
            stats = stats.WithModifiers([card.UnitModifier]);
        }

        // Step 3: Card upgrade multipliers (multiplicative)
        if (upgradeMultipliers != null && upgradeMultipliers.Count > 0)
        {
            stats = stats.WithUpgradeMultipliers(upgradeMultipliers);
        }

        // Steps 4-5: Modifier adds then mults (handled together in WithModifiers)
        if (modifiers != null && modifiers.Count > 0)
        {
            stats = stats.WithModifiers(modifiers);
        }

        // Step 6: Custom overrides (replacement)
        if (overrides != null && overrides.Count > 0)
        {
            stats = stats.WithOverrides(overrides);
        }

        return stats;
    }

    /// <summary>
    /// Gets base stats for a card, using UnitCatalog if UnitId is set,
    /// otherwise falling back to CardDefinition stats (legacy).
    /// </summary>
    private static UnitStats GetBaseStats(CardDefinition card)
    {
        // New pattern: Use UnitDefinitions when UnitId is set
        if (card.UnitId.HasValue)
        {
            if (UnitDefinitions.TryGet(card.UnitId, out var def) && def != null)
            {
                return def.Stats;
            }
            // UnitId set but not found in definitions - log warning and fall back
            GD.PushWarning($"[UnitStatCalculator] UnitId '{card.UnitId}' not found in UnitDefinitions, falling back to CardDefinition stats");
        }

        // Legacy pattern: Use CardDefinition stats directly
        return FromCardDefinition(card);
    }

    /// <summary>
    /// Calculates stats from a C# Dictionary.
    /// </summary>
    /// <param name="effectiveStats">Stats dictionary with upgrades already applied</param>
    /// <param name="modifiers">Additional modifiers to apply</param>
    /// <param name="overrides">Override values</param>
    /// <returns>Final calculated UnitStats</returns>
    public static UnitStats CalculateFromDictionary(
        Dictionary<string, float>? effectiveStats,
        List<StatModifier>? modifiers = null,
        Dictionary<string, float>? overrides = null)
    {
        // Step 1-2: Base + upgrades already combined in effectiveStats
        var stats = UnitStats.FromDictionary(effectiveStats);

        // Steps 3-4: Modifier adds then mults
        if (modifiers != null && modifiers.Count > 0)
        {
            stats = stats.WithModifiers(modifiers);
        }

        // Step 5: Custom overrides
        if (overrides != null && overrides.Count > 0)
        {
            stats = stats.WithOverrides(overrides);
        }

        return stats;
    }

    /// <summary>
    /// Calculates stats from a Godot Dictionary (effectiveStats from GDScript).
    /// This is the main entry point when stats come from PlayerCardService.get_effective_stats().
    /// </summary>
    public static UnitStats CalculateFromGodotDictionary(
        Godot.Collections.Dictionary? effectiveStats,
        List<StatModifier>? modifiers = null,
        Godot.Collections.Dictionary? overrides = null)
    {
        // Step 1-2: Base + upgrades already combined in effectiveStats
        var stats = UnitStats.FromGodotDictionary(effectiveStats);

        // Steps 3-4: Modifier adds then mults
        if (modifiers != null && modifiers.Count > 0)
        {
            stats = stats.WithModifiers(modifiers);
        }

        // Step 5: Custom overrides
        if (overrides != null && overrides.Count > 0)
        {
            stats = stats.WithGodotOverrides(overrides);
        }

        return stats;
    }

    /// <summary>
    /// Creates UnitStats from a CardDefinition's stat properties.
    /// LEGACY: Prefer using UnitId to reference UnitCatalog instead.
    /// </summary>
    public static UnitStats FromCardDefinition(CardDefinition card)
    {
        return new UnitStats
        {
            MaxHp = card.MaxHp > 0 ? card.MaxHp : StatKey.MaxHp.GetDefault(),
            AttackDamage = card.AttackDamage > 0 ? card.AttackDamage : StatKey.AttackDamage.GetDefault(),
            AttackSpeed = card.AttackSpeed > 0 ? card.AttackSpeed : StatKey.AttackSpeed.GetDefault(),
            MoveSpeed = card.MoveSpeed > 0 ? card.MoveSpeed : StatKey.MoveSpeed.GetDefault(),
            AttackRange = card.AttackRange > 0 ? card.AttackRange : StatKey.AttackRange.GetDefault(),
            AggroRadius = card.AggroRadius > 0 ? card.AggroRadius : StatKey.AggroRadius.GetDefault(),
            SoulStrength = card.SoulStrength
        };
    }

    /// <summary>
    /// Validates that a stats dictionary contains all expected keys.
    /// Logs warnings for missing or unknown keys.
    /// </summary>
    /// <param name="stats">Dictionary to validate</param>
    /// <param name="source">Source name for logging (e.g., "Simulation.SpawnUnitsFromCard")</param>
    /// <returns>List of validation warnings (empty if valid)</returns>
    public static List<string> ValidateStatsDictionary(Dictionary<string, float> stats, string source = "")
    {
        var warnings = new List<string>();
        var prefix = string.IsNullOrEmpty(source) ? "" : $"[{source}] ";

        // Check for expected keys
        foreach (var key in StatKeyExtensions.All)
        {
            var snakeKey = key.ToSnakeCase();
            if (!stats.ContainsKey(snakeKey))
            {
                warnings.Add($"{prefix}Missing expected stat key: {snakeKey}");
            }
        }

        // Check for unknown keys (potential typos or new stats)
        foreach (var keyString in stats.Keys)
        {
            if (!StatKeyExtensions.IsValidStatKey(keyString))
            {
                // Skip non-stat keys that are valid in card definitions
                if (!IsKnownNonStatKey(keyString))
                {
                    warnings.Add($"{prefix}Unknown stat key: {keyString}");
                }
            }
        }

        return warnings;
    }

    /// <summary>
    /// Validates a Godot Dictionary. Used for GDScript interop.
    /// </summary>
    public static List<string> ValidateGodotStatsDictionary(Godot.Collections.Dictionary stats, string source = "")
    {
        var csharpDict = new Dictionary<string, float>();
        foreach (var key in stats.Keys)
        {
            var keyStr = key.AsString();
            var value = stats[key];
            if (value.VariantType == Variant.Type.Float || value.VariantType == Variant.Type.Int)
            {
                csharpDict[keyStr] = value.AsSingle();
            }
            else
            {
                // Include non-float keys so we can report unknown keys
                csharpDict[keyStr] = 0f;
            }
        }
        return ValidateStatsDictionary(csharpDict, source);
    }

    /// <summary>
    /// Checks if a key is a known non-stat key in card definitions.
    /// These are valid keys but not unit stats.
    /// </summary>
    private static bool IsKnownNonStatKey(string key)
    {
        return key switch
        {
            // Spell stats
            "spell_damage" or "spell_radius" or "spell_duration" => true,
            // Card metadata
            "id" or "name" or "description" or "rarity" => true,
            "mana_cost" or "cooldown" or "summon_time" => true,
            "spawn_count" or "unit_type" or "is_ranged" => true,
            "card_type" or "elemental_affinity" or "tags" => true,
            "unit_scene_path" or "projectile_scene_path" or "unit_id" => true,
            "card_icon_path" or "unlock_condition" => true,
            // Command spell properties
            "command_type" or "selection_radius" or "formation_duration" => true,
            "projectile_id" or "spell_vfx" => true,
            // Override keys
            "scale_multiplier" => true,
            _ => false
        };
    }

    /// <summary>
    /// Apply modifiers to base stats and return modified stats.
    /// Uses type-safe StatKey for dictionary lookups.
    /// Relocated from ModifierService — pure static function that belongs with stat calculation.
    /// </summary>
    public static ModifiedStats ApplyModifiers(BaseStats baseStats, List<StatModifier> modifiers)
    {
        float hpAdd = 0f, damageAdd = 0f, speedAdd = 0f, moveSpeedAdd = 0f;
        float hpMult = 1f, damageMult = 1f, speedMult = 1f, moveSpeedMult = 1f;
        var flags = new Dictionary<string, bool>();

        foreach (var mod in modifiers)
        {
            foreach (var (key, value) in mod.StatAdds)
            {
                switch (key)
                {
                    case StatKey.MaxHp:
                    case StatKey.MaxHealth:
                        hpAdd += value;
                        break;
                    case StatKey.AttackDamage:
                    case StatKey.DamageBonus:
                        damageAdd += value;
                        break;
                    case StatKey.AttackSpeed:
                        speedAdd += value;
                        break;
                    case StatKey.MoveSpeed:
                        moveSpeedAdd += value;
                        break;
                }
            }

            foreach (var (key, value) in mod.StatMults)
            {
                switch (key)
                {
                    case StatKey.MaxHp:
                    case StatKey.MaxHealth:
                        hpMult *= value;
                        break;
                    case StatKey.AttackDamage:
                    case StatKey.DamageBonus:
                        damageMult *= value;
                        break;
                    case StatKey.AttackSpeed:
                        speedMult *= value;
                        break;
                    case StatKey.MoveSpeed:
                        moveSpeedMult *= value;
                        break;
                }
            }

            foreach (var kvp in mod.Flags)
            {
                flags[kvp.Key] = kvp.Value;
            }
        }

        return new ModifiedStats
        {
            MaxHp = (baseStats.MaxHp + hpAdd) * hpMult,
            AttackDamage = (baseStats.AttackDamage + damageAdd) * damageMult,
            AttackSpeed = (baseStats.AttackSpeed + speedAdd) * speedMult,
            MoveSpeed = (baseStats.MoveSpeed + moveSpeedAdd) * moveSpeedMult,
            Flags = flags
        };
    }
}
