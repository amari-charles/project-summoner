using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fateforged.Data.Items;
using Fateforged.Data.Summoners;
using Fateforged.Data.Traits;
using Fateforged.Stats;
using ItemSlot = Fateforged.Domain.Profile.Inventory.ItemSlot;

namespace Fateforged.Domain.Profile.Summoners;

/// <summary>
/// Runtime summoner instance in a player's profile.
/// Stores progression state and computes stats from base definition + level + traits.
/// </summary>
public class SummonerInstance
{
    /// <summary>5% stat bonus per level above 1 (applied to health and mana).</summary>
    public const float LevelStatBonusPercent = 0.05f;

    /// <summary>Summoner catalog ID (e.g., "summoner_cole").</summary>
    [JsonPropertyName("summoner_id")]
    public required SummonerId SummonerId { get; set; }

    /// <summary>Current level (1-10).</summary>
    [JsonPropertyName("level")]
    public int Level { get; set; } = 1;

    /// <summary>XP towards next level.</summary>
    [JsonPropertyName("xp")]
    public int Xp { get; set; }

    /// <summary>
    /// Equipped item instance IDs by slot.
    /// Values: Item instance ID or null if slot is empty.
    /// Serialized to {"wand": "id", ...} via DtoConverters for GDScript interop.
    /// </summary>
    [JsonIgnore]
    public Dictionary<ItemSlot, ItemId?> EquippedItems { get; set; } = new()
    {
        [ItemSlot.Wand] = null,
        [ItemSlot.Ring1] = null,
        [ItemSlot.Ring2] = null,
        [ItemSlot.Robes] = null
    };

    /// <summary>
    /// Trait IDs this summoner has acquired through level-up selections.
    /// </summary>
    [JsonPropertyName("acquired_trait_ids")]
    public List<TraitId> AcquiredTraitIds { get; set; } = [];

    /// <summary>
    /// Unified trait progression points available to spend (Pass 2 ledger stub).
    /// </summary>
    [JsonPropertyName("unspent_trait_points")]
    public int UnspentTraitPoints { get; set; }

    // =========================================================================
    // STAT COMPUTATION
    // =========================================================================

    /// <summary>
    /// Get all trait IDs for this summoner (innate from definition + acquired).
    /// </summary>
    public List<string> GetAllTraitIds()
    {
        var result = new List<string>();

        var def = SummonerCatalog.GetSummoner(SummonerId);
        if (def != null)
        {
            foreach (var traitId in def.InnateTraitIds)
                result.Add(traitId);
        }

        foreach (var traitId in AcquiredTraitIds)
            result.Add(traitId);

        return result;
    }

    /// <summary>
    /// Compute final stats with level bonuses and all trait modifiers applied.
    /// Returns a dictionary with snake_case stat keys.
    /// </summary>
    public Dictionary<string, float> GetComputedStats()
    {
        var def = SummonerCatalog.GetSummoner(SummonerId);
        if (def == null)
            return new Dictionary<string, float>();

        var stats = new Dictionary<string, float>
        {
            ["health"] = def.BaseHealth,
            ["max_mana"] = def.MaxMana,
            ["cast_speed"] = 1.0f,
            // Elemental damage bonuses (default 0, modified by traits)
            ["fire_damage_bonus"] = 0f,
            ["water_damage_bonus"] = 0f,
            ["wind_damage_bonus"] = 0f,
            ["earth_damage_bonus"] = 0f,
            ["lightning_damage_bonus"] = 0f,
            ["life_damage_bonus"] = 0f,
            ["death_damage_bonus"] = 0f,
            ["shadow_damage_bonus"] = 0f,
            // General combat modifiers
            ["damage_bonus"] = 0f,
            ["damage_reduction"] = 0f,
            ["soul_strength"] = 0f
        };

        // Apply level bonuses (health and mana only)
        var levelMultiplier = 1.0f + (Level - 1) * LevelStatBonusPercent;
        stats["health"] *= levelMultiplier;
        stats["max_mana"] *= levelMultiplier;

        // Apply summoner-level trait modifiers (innate + acquired).
        foreach (var traitId in GetAllTraitIds())
        {
            var trait = TraitCatalog.GetTrait(traitId);
            if (trait == null)
                continue;

            foreach (var mod in trait.Modifiers)
            {
                // Ignore unit-targeted trait effects in summoner stat computation.
                if (!mod.HasSummonerStat || mod.IsUnitModifier || !mod.Stat.HasValue)
                    continue;

                string statKey = ToComputedStatKey(mod.Stat.Value);
                if (!stats.ContainsKey(statKey))
                    stats[statKey] = 0f;

                if (mod.Type == ModifierType.Flat)
                {
                    stats[statKey] += mod.Value;
                    continue;
                }

                // Percent semantics:
                // - Non-zero baselines are multiplicative (e.g., cast_speed, health)
                // - Zero baselines represent bonus buckets and accumulate in percentage points
                if (stats[statKey] == 0f)
                    stats[statKey] += mod.Value;
                else
                    stats[statKey] *= 1.0f + (mod.Value / 100.0f);
            }
        }

        return stats;
    }

    private static string ToComputedStatKey(StatKey stat) => stat switch
    {
        StatKey.MaxHp => "health",
        StatKey.MaxHealth => "health",
        _ => stat.ToSnakeCase()
    };

    /// <summary>Get a single computed stat by snake_case name.</summary>
    public float GetStat(string statName)
    {
        var stats = GetComputedStats();
        return stats.GetValueOrDefault(statName, 0f);
    }

}
