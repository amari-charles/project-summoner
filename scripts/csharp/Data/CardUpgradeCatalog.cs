using System.Collections.Generic;

namespace ProjectSummoner.Data;

/// <summary>
/// Card upgrade definition.
/// </summary>
public class CardUpgrade
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, float> StatMods { get; set; } = new();
}

/// <summary>
/// Card Upgrade Catalog - Defines upgrade choices for each card at each level.
/// Each card can have 2-3 upgrade choices per level (levels 2-10).
/// Upgrades should be meaningful choices that enhance the card's identity.
/// </summary>
public static class CardUpgradeCatalog
{
    // Card ID -> Level -> List of upgrades
    private static readonly Dictionary<string, Dictionary<int, List<CardUpgrade>>> Upgrades = new()
    {
        // ==========================================================================
        // FIREBALL - AOE damage spell, focus on damage or radius
        // ==========================================================================
        ["fireball"] = new Dictionary<int, List<CardUpgrade>>
        {
            [2] = new List<CardUpgrade>
            {
                new() { Id = "fireball_intense_2", Name = "Intense Flames", Description = "+12% Damage",
                    StatMods = new() { ["spell_damage"] = 1.12f } },
                new() { Id = "fireball_spread_2", Name = "Wider Spread", Description = "+10% Radius",
                    StatMods = new() { ["spell_radius"] = 1.10f } }
            },
            [3] = new List<CardUpgrade>
            {
                new() { Id = "fireball_scorching_3", Name = "Scorching Heat", Description = "+15% Damage",
                    StatMods = new() { ["spell_damage"] = 1.15f } },
                new() { Id = "fireball_expanded_3", Name = "Expanded Blast", Description = "+12% Radius",
                    StatMods = new() { ["spell_radius"] = 1.12f } }
            },
            [4] = new List<CardUpgrade>
            {
                new() { Id = "fireball_inferno_4", Name = "Inferno", Description = "+18% Damage",
                    StatMods = new() { ["spell_damage"] = 1.18f } },
                new() { Id = "fireball_conflagration_4", Name = "Conflagration", Description = "+15% Radius",
                    StatMods = new() { ["spell_radius"] = 1.15f } }
            },
            [5] = new List<CardUpgrade>
            {
                new() { Id = "fireball_meteor_5", Name = "Meteor Strike", Description = "+22% Damage",
                    StatMods = new() { ["spell_damage"] = 1.22f } },
                new() { Id = "fireball_firestorm_5", Name = "Firestorm", Description = "+18% Radius",
                    StatMods = new() { ["spell_radius"] = 1.18f } },
                new() { Id = "fireball_balanced_5", Name = "Refined Casting", Description = "+10% Damage, +10% Radius",
                    StatMods = new() { ["spell_damage"] = 1.10f, ["spell_radius"] = 1.10f } }
            },
            [6] = new List<CardUpgrade>
            {
                new() { Id = "fireball_volcanic_6", Name = "Volcanic Fury", Description = "+25% Damage",
                    StatMods = new() { ["spell_damage"] = 1.25f } },
                new() { Id = "fireball_wildfire_6", Name = "Wildfire", Description = "+20% Radius",
                    StatMods = new() { ["spell_radius"] = 1.20f } }
            },
            [7] = new List<CardUpgrade>
            {
                new() { Id = "fireball_cataclysm_7", Name = "Cataclysm", Description = "+28% Damage",
                    StatMods = new() { ["spell_damage"] = 1.28f } },
                new() { Id = "fireball_devastation_7", Name = "Devastation", Description = "+22% Radius",
                    StatMods = new() { ["spell_radius"] = 1.22f } },
                new() { Id = "fireball_efficient_7", Name = "Efficient Casting", Description = "+15% Damage, +12% Radius",
                    StatMods = new() { ["spell_damage"] = 1.15f, ["spell_radius"] = 1.12f } }
            },
            [8] = new List<CardUpgrade>
            {
                new() { Id = "fireball_hellfire_8", Name = "Hellfire", Description = "+32% Damage",
                    StatMods = new() { ["spell_damage"] = 1.32f } },
                new() { Id = "fireball_apocalypse_8", Name = "Apocalyptic Blast", Description = "+25% Radius",
                    StatMods = new() { ["spell_radius"] = 1.25f } }
            },
            [9] = new List<CardUpgrade>
            {
                new() { Id = "fireball_solar_9", Name = "Solar Flare", Description = "+35% Damage",
                    StatMods = new() { ["spell_damage"] = 1.35f } },
                new() { Id = "fireball_nova_9", Name = "Supernova", Description = "+28% Radius",
                    StatMods = new() { ["spell_radius"] = 1.28f } },
                new() { Id = "fireball_mastery_9", Name = "Fire Mastery", Description = "+20% Damage, +18% Radius",
                    StatMods = new() { ["spell_damage"] = 1.20f, ["spell_radius"] = 1.18f } }
            },
            [10] = new List<CardUpgrade>
            {
                new() { Id = "fireball_apex_power_10", Name = "Apex Destruction", Description = "+40% Damage",
                    StatMods = new() { ["spell_damage"] = 1.40f } },
                new() { Id = "fireball_apex_area_10", Name = "Apex Devastation", Description = "+32% Radius",
                    StatMods = new() { ["spell_radius"] = 1.32f } },
                new() { Id = "fireball_apex_perfect_10", Name = "Perfect Inferno", Description = "+25% Damage, +25% Radius",
                    StatMods = new() { ["spell_damage"] = 1.25f, ["spell_radius"] = 1.25f } }
            }
        },

        // ==========================================================================
        // CHARGE - Tactical spell (focus-fire command)
        // ==========================================================================
        ["charge"] = new Dictionary<int, List<CardUpgrade>>
        {
            [2] = new List<CardUpgrade>
            {
                new() { Id = "charge_inspiring_2", Name = "Inspiring Charge", Description = "+10% Damage",
                    StatMods = new() { ["spell_damage"] = 1.10f } },
                new() { Id = "charge_swift_2", Name = "Swift Charge", Description = "+12% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.12f } }
            },
            [3] = new List<CardUpgrade>
            {
                new() { Id = "charge_ferocious_3", Name = "Ferocious Assault", Description = "+15% Damage",
                    StatMods = new() { ["spell_damage"] = 1.15f } },
                new() { Id = "charge_coordinated_3", Name = "Coordinated Strike", Description = "+10% Damage, +8% Speed",
                    StatMods = new() { ["spell_damage"] = 1.10f, ["move_speed"] = 1.08f } }
            },
            [4] = new List<CardUpgrade>
            {
                new() { Id = "charge_devastating_4", Name = "Devastating Charge", Description = "+18% Damage",
                    StatMods = new() { ["spell_damage"] = 1.18f } },
                new() { Id = "charge_lightning_4", Name = "Lightning Charge", Description = "+18% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.18f } }
            },
            [5] = new List<CardUpgrade>
            {
                new() { Id = "charge_crushing_5", Name = "Crushing Assault", Description = "+22% Damage",
                    StatMods = new() { ["spell_damage"] = 1.22f } },
                new() { Id = "charge_blitz_5", Name = "Blitz", Description = "+22% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.22f } },
                new() { Id = "charge_tactical_5", Name = "Tactical Excellence", Description = "+12% Damage, +12% Speed",
                    StatMods = new() { ["spell_damage"] = 1.12f, ["move_speed"] = 1.12f } }
            },
            [6] = new List<CardUpgrade>
            {
                new() { Id = "charge_overwhelming_6", Name = "Overwhelming Force", Description = "+25% Damage",
                    StatMods = new() { ["spell_damage"] = 1.25f } },
                new() { Id = "charge_rapid_6", Name = "Rapid Assault", Description = "+25% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.25f } }
            },
            [7] = new List<CardUpgrade>
            {
                new() { Id = "charge_annihilating_7", Name = "Annihilating Charge", Description = "+28% Damage",
                    StatMods = new() { ["spell_damage"] = 1.28f } },
                new() { Id = "charge_thunder_7", Name = "Thunder Strike", Description = "+28% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.28f } },
                new() { Id = "charge_supreme_7", Name = "Supreme Command", Description = "+15% Damage, +15% Speed",
                    StatMods = new() { ["spell_damage"] = 1.15f, ["move_speed"] = 1.15f } }
            },
            [8] = new List<CardUpgrade>
            {
                new() { Id = "charge_decimating_8", Name = "Decimating Assault", Description = "+32% Damage",
                    StatMods = new() { ["spell_damage"] = 1.32f } },
                new() { Id = "charge_sonic_8", Name = "Sonic Charge", Description = "+32% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.32f } }
            },
            [9] = new List<CardUpgrade>
            {
                new() { Id = "charge_legendary_dmg_9", Name = "Legendary Assault", Description = "+35% Damage",
                    StatMods = new() { ["spell_damage"] = 1.35f } },
                new() { Id = "charge_legendary_spd_9", Name = "Legendary Speed", Description = "+35% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.35f } },
                new() { Id = "charge_legendary_bal_9", Name = "Perfect Charge", Description = "+20% Damage, +20% Speed",
                    StatMods = new() { ["spell_damage"] = 1.20f, ["move_speed"] = 1.20f } }
            },
            [10] = new List<CardUpgrade>
            {
                new() { Id = "charge_apex_power_10", Name = "Apex Devastation", Description = "+40% Damage",
                    StatMods = new() { ["spell_damage"] = 1.40f } },
                new() { Id = "charge_apex_speed_10", Name = "Apex Velocity", Description = "+40% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.40f } },
                new() { Id = "charge_apex_perfect_10", Name = "Ultimate Command", Description = "+25% Damage, +25% Speed",
                    StatMods = new() { ["spell_damage"] = 1.25f, ["move_speed"] = 1.25f } }
            }
        }
    };

    // =============================================================================
    // API
    // =============================================================================

    /// <summary>
    /// Get upgrades available for a card at a specific level.
    /// </summary>
    public static List<CardUpgrade> GetUpgradesForLevel(string catalogId, int level)
    {
        if (!Upgrades.TryGetValue(catalogId, out var cardUpgrades))
        {
            // Card doesn't have specific upgrades defined - return generic upgrades
            return GetGenericUpgrades(catalogId, level);
        }

        if (!cardUpgrades.TryGetValue(level, out var levelUpgrades))
        {
            return new List<CardUpgrade>();
        }

        return new List<CardUpgrade>(levelUpgrades);
    }

    /// <summary>
    /// Get a specific upgrade by ID.
    /// </summary>
    public static CardUpgrade? GetUpgrade(string catalogId, string upgradeId)
    {
        if (!Upgrades.TryGetValue(catalogId, out var cardUpgrades))
        {
            // Check generic upgrades
            return FindGenericUpgrade(catalogId, upgradeId);
        }

        foreach (var levelUpgrades in cardUpgrades.Values)
        {
            foreach (var upgrade in levelUpgrades)
            {
                if (upgrade.Id == upgradeId)
                {
                    return upgrade;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Check if a card has specific upgrades defined.
    /// </summary>
    public static bool HasUpgrades(string catalogId)
    {
        return Upgrades.ContainsKey(catalogId);
    }

    /// <summary>
    /// Get all upgrade IDs for a card (for validation).
    /// </summary>
    public static List<string> GetAllUpgradeIds(string catalogId)
    {
        var ids = new List<string>();

        if (!Upgrades.TryGetValue(catalogId, out var cardUpgrades))
        {
            return ids;
        }

        foreach (var levelUpgrades in cardUpgrades.Values)
        {
            foreach (var upgrade in levelUpgrades)
            {
                if (!string.IsNullOrEmpty(upgrade.Id))
                {
                    ids.Add(upgrade.Id);
                }
            }
        }

        return ids;
    }

    // =============================================================================
    // GENERIC UPGRADES (for cards without specific definitions)
    // =============================================================================

    /// <summary>
    /// Generate generic upgrades for cards without specific definitions.
    /// </summary>
    private static List<CardUpgrade> GetGenericUpgrades(string catalogId, int level)
    {
        var result = new List<CardUpgrade>();

        if (level < 2 || level > 10)
        {
            return result;
        }

        // Scale percentages with level
        float hpBonus = 1.0f + (0.08f + level * 0.02f);   // 10% at L2, up to 28% at L10
        float dmgBonus = 1.0f + (0.06f + level * 0.02f);  // 8% at L2, up to 26% at L10
        float spdBonus = 1.0f + (0.05f + level * 0.015f); // 6.5% at L2, up to 20% at L10

        int hpPercent = (int)((hpBonus - 1.0f) * 100);
        int dmgPercent = (int)((dmgBonus - 1.0f) * 100);
        int spdPercent = (int)((spdBonus - 1.0f) * 100);

        result.Add(new CardUpgrade
        {
            Id = $"{catalogId}_hp_l{level}",
            Name = "Fortitude",
            Description = $"+{hpPercent}% HP",
            StatMods = new() { ["max_hp"] = hpBonus }
        });

        result.Add(new CardUpgrade
        {
            Id = $"{catalogId}_dmg_l{level}",
            Name = "Power",
            Description = $"+{dmgPercent}% Damage",
            StatMods = new() { ["attack_damage"] = dmgBonus }
        });

        // Add third option at higher levels
        if (level >= 4)
        {
            result.Add(new CardUpgrade
            {
                Id = $"{catalogId}_spd_l{level}",
                Name = "Swiftness",
                Description = $"+{spdPercent}% Attack Speed",
                StatMods = new() { ["attack_speed"] = spdBonus }
            });
        }

        return result;
    }

    /// <summary>
    /// Find a generic upgrade by ID.
    /// </summary>
    private static CardUpgrade? FindGenericUpgrade(string catalogId, string upgradeId)
    {
        // Parse the upgrade ID to get level
        for (int level = 2; level <= 10; level++)
        {
            var upgrades = GetGenericUpgrades(catalogId, level);
            foreach (var upgrade in upgrades)
            {
                if (upgrade.Id == upgradeId)
                {
                    return upgrade;
                }
            }
        }

        return null;
    }
}
