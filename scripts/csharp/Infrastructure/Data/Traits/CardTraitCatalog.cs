using System.Collections.Generic;
using Godot;
using Fateforged.Cards;

namespace Fateforged.Data;

/// <summary>
/// Card trait definition.
/// </summary>
public class CardTrait
{
    public CardTraitId Id { get; set; } = CardTraitId.None;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, float> StatMods { get; set; } = new();
}

/// <summary>
/// Card Trait Catalog - Defines trait choices for each card at each level.
/// Each card can have 2-3 trait choices per level (levels 2-10).
/// Traits should be meaningful choices that enhance the card's identity.
/// </summary>
public static class CardTraitCatalog
{
    // Card ID -> Level -> List of traits
    private static readonly Dictionary<string, Dictionary<int, List<CardTrait>>> Traits = new()
    {
        // ==========================================================================
        // FIREBALL - AOE damage spell, focus on damage or radius
        // ==========================================================================
        ["fireball"] = new Dictionary<int, List<CardTrait>>
        {
            [2] = new List<CardTrait>
            {
                new() { Id = new("fireball_intense_2"), Name = "Intense Flames", Description = "+12% Damage",
                    StatMods = new() { ["spell_damage"] = 1.12f } },
                new() { Id = new("fireball_spread_2"), Name = "Wider Spread", Description = "+10% Radius",
                    StatMods = new() { ["spell_radius"] = 1.10f } }
            },
            [3] = new List<CardTrait>
            {
                new() { Id = new("fireball_scorching_3"), Name = "Scorching Heat", Description = "+15% Damage",
                    StatMods = new() { ["spell_damage"] = 1.15f } },
                new() { Id = new("fireball_expanded_3"), Name = "Expanded Blast", Description = "+12% Radius",
                    StatMods = new() { ["spell_radius"] = 1.12f } }
            },
            [4] = new List<CardTrait>
            {
                new() { Id = new("fireball_inferno_4"), Name = "Inferno", Description = "+18% Damage",
                    StatMods = new() { ["spell_damage"] = 1.18f } },
                new() { Id = new("fireball_conflagration_4"), Name = "Conflagration", Description = "+15% Radius",
                    StatMods = new() { ["spell_radius"] = 1.15f } }
            },
            [5] = new List<CardTrait>
            {
                new() { Id = new("fireball_meteor_5"), Name = "Meteor Strike", Description = "+22% Damage",
                    StatMods = new() { ["spell_damage"] = 1.22f } },
                new() { Id = new("fireball_firestorm_5"), Name = "Firestorm", Description = "+18% Radius",
                    StatMods = new() { ["spell_radius"] = 1.18f } },
                new() { Id = new("fireball_balanced_5"), Name = "Refined Casting", Description = "+10% Damage, +10% Radius",
                    StatMods = new() { ["spell_damage"] = 1.10f, ["spell_radius"] = 1.10f } }
            },
            [6] = new List<CardTrait>
            {
                new() { Id = new("fireball_volcanic_6"), Name = "Volcanic Fury", Description = "+25% Damage",
                    StatMods = new() { ["spell_damage"] = 1.25f } },
                new() { Id = new("fireball_wildfire_6"), Name = "Wildfire", Description = "+20% Radius",
                    StatMods = new() { ["spell_radius"] = 1.20f } }
            },
            [7] = new List<CardTrait>
            {
                new() { Id = new("fireball_cataclysm_7"), Name = "Cataclysm", Description = "+28% Damage",
                    StatMods = new() { ["spell_damage"] = 1.28f } },
                new() { Id = new("fireball_devastation_7"), Name = "Devastation", Description = "+22% Radius",
                    StatMods = new() { ["spell_radius"] = 1.22f } },
                new() { Id = new("fireball_efficient_7"), Name = "Efficient Casting", Description = "+15% Damage, +12% Radius",
                    StatMods = new() { ["spell_damage"] = 1.15f, ["spell_radius"] = 1.12f } }
            },
            [8] = new List<CardTrait>
            {
                new() { Id = new("fireball_hellfire_8"), Name = "Hellfire", Description = "+32% Damage",
                    StatMods = new() { ["spell_damage"] = 1.32f } },
                new() { Id = new("fireball_apocalypse_8"), Name = "Apocalyptic Blast", Description = "+25% Radius",
                    StatMods = new() { ["spell_radius"] = 1.25f } }
            },
            [9] = new List<CardTrait>
            {
                new() { Id = new("fireball_solar_9"), Name = "Solar Flare", Description = "+35% Damage",
                    StatMods = new() { ["spell_damage"] = 1.35f } },
                new() { Id = new("fireball_nova_9"), Name = "Supernova", Description = "+28% Radius",
                    StatMods = new() { ["spell_radius"] = 1.28f } },
                new() { Id = new("fireball_mastery_9"), Name = "Fire Mastery", Description = "+20% Damage, +18% Radius",
                    StatMods = new() { ["spell_damage"] = 1.20f, ["spell_radius"] = 1.18f } }
            },
            [10] = new List<CardTrait>
            {
                new() { Id = new("fireball_apex_power_10"), Name = "Apex Destruction", Description = "+40% Damage",
                    StatMods = new() { ["spell_damage"] = 1.40f } },
                new() { Id = new("fireball_apex_area_10"), Name = "Apex Devastation", Description = "+32% Radius",
                    StatMods = new() { ["spell_radius"] = 1.32f } },
                new() { Id = new("fireball_apex_perfect_10"), Name = "Perfect Inferno", Description = "+25% Damage, +25% Radius",
                    StatMods = new() { ["spell_damage"] = 1.25f, ["spell_radius"] = 1.25f } }
            }
        },

        // ==========================================================================
        // CHARGE - Tactical spell (focus-fire command)
        // ==========================================================================
        ["charge"] = new Dictionary<int, List<CardTrait>>
        {
            [2] = new List<CardTrait>
            {
                new() { Id = new("charge_inspiring_2"), Name = "Inspiring Charge", Description = "+10% Damage",
                    StatMods = new() { ["spell_damage"] = 1.10f } },
                new() { Id = new("charge_swift_2"), Name = "Swift Charge", Description = "+12% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.12f } }
            },
            [3] = new List<CardTrait>
            {
                new() { Id = new("charge_ferocious_3"), Name = "Ferocious Assault", Description = "+15% Damage",
                    StatMods = new() { ["spell_damage"] = 1.15f } },
                new() { Id = new("charge_coordinated_3"), Name = "Coordinated Strike", Description = "+10% Damage, +8% Speed",
                    StatMods = new() { ["spell_damage"] = 1.10f, ["move_speed"] = 1.08f } }
            },
            [4] = new List<CardTrait>
            {
                new() { Id = new("charge_devastating_4"), Name = "Devastating Charge", Description = "+18% Damage",
                    StatMods = new() { ["spell_damage"] = 1.18f } },
                new() { Id = new("charge_lightning_4"), Name = "Lightning Charge", Description = "+18% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.18f } }
            },
            [5] = new List<CardTrait>
            {
                new() { Id = new("charge_crushing_5"), Name = "Crushing Assault", Description = "+22% Damage",
                    StatMods = new() { ["spell_damage"] = 1.22f } },
                new() { Id = new("charge_blitz_5"), Name = "Blitz", Description = "+22% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.22f } },
                new() { Id = new("charge_tactical_5"), Name = "Tactical Excellence", Description = "+12% Damage, +12% Speed",
                    StatMods = new() { ["spell_damage"] = 1.12f, ["move_speed"] = 1.12f } }
            },
            [6] = new List<CardTrait>
            {
                new() { Id = new("charge_overwhelming_6"), Name = "Overwhelming Force", Description = "+25% Damage",
                    StatMods = new() { ["spell_damage"] = 1.25f } },
                new() { Id = new("charge_rapid_6"), Name = "Rapid Assault", Description = "+25% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.25f } }
            },
            [7] = new List<CardTrait>
            {
                new() { Id = new("charge_annihilating_7"), Name = "Annihilating Charge", Description = "+28% Damage",
                    StatMods = new() { ["spell_damage"] = 1.28f } },
                new() { Id = new("charge_thunder_7"), Name = "Thunder Strike", Description = "+28% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.28f } },
                new() { Id = new("charge_supreme_7"), Name = "Supreme Command", Description = "+15% Damage, +15% Speed",
                    StatMods = new() { ["spell_damage"] = 1.15f, ["move_speed"] = 1.15f } }
            },
            [8] = new List<CardTrait>
            {
                new() { Id = new("charge_decimating_8"), Name = "Decimating Assault", Description = "+32% Damage",
                    StatMods = new() { ["spell_damage"] = 1.32f } },
                new() { Id = new("charge_sonic_8"), Name = "Sonic Charge", Description = "+32% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.32f } }
            },
            [9] = new List<CardTrait>
            {
                new() { Id = new("charge_legendary_dmg_9"), Name = "Legendary Assault", Description = "+35% Damage",
                    StatMods = new() { ["spell_damage"] = 1.35f } },
                new() { Id = new("charge_legendary_spd_9"), Name = "Legendary Speed", Description = "+35% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.35f } },
                new() { Id = new("charge_legendary_bal_9"), Name = "Perfect Charge", Description = "+20% Damage, +20% Speed",
                    StatMods = new() { ["spell_damage"] = 1.20f, ["move_speed"] = 1.20f } }
            },
            [10] = new List<CardTrait>
            {
                new() { Id = new("charge_apex_power_10"), Name = "Apex Devastation", Description = "+40% Damage",
                    StatMods = new() { ["spell_damage"] = 1.40f } },
                new() { Id = new("charge_apex_speed_10"), Name = "Apex Velocity", Description = "+40% Effect Speed",
                    StatMods = new() { ["move_speed"] = 1.40f } },
                new() { Id = new("charge_apex_perfect_10"), Name = "Ultimate Command", Description = "+25% Damage, +25% Speed",
                    StatMods = new() { ["spell_damage"] = 1.25f, ["move_speed"] = 1.25f } }
            }
        }
    };

    // =============================================================================
    // API
    // =============================================================================

    /// <summary>
    /// Get traits available for a card at a specific level.
    /// Returns empty list if card has no traits defined.
    /// </summary>
    public static List<CardTrait> GetTraitsForLevel(string catalogId, int level)
    {
        if (!Traits.TryGetValue(catalogId, out var cardTraits))
        {
            // Card doesn't have traits defined - warn and return empty
            if (level >= 2 && level <= 10)
            {
                GD.PushWarning($"CardTraitCatalog: No traits defined for card '{catalogId}' at level {level}");
            }
            return new List<CardTrait>();
        }

        if (!cardTraits.TryGetValue(level, out var levelTraits))
        {
            return new List<CardTrait>();
        }

        return new List<CardTrait>(levelTraits);
    }

    /// <summary>
    /// Get a specific trait by ID.
    /// </summary>
    public static CardTrait? GetTrait(string catalogId, CardTraitId traitId)
    {
        if (!Traits.TryGetValue(catalogId, out var cardTraits))
        {
            return null;
        }

        foreach (var levelTraits in cardTraits.Values)
        {
            foreach (var trait in levelTraits)
            {
                if (trait.Id == traitId)
                {
                    return trait;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Get a specific trait by string ID.
    /// </summary>
    public static CardTrait? GetTrait(string catalogId, string traitId) =>
        GetTrait(catalogId, new CardTraitId(traitId));

    /// <summary>
    /// Check if a card has specific traits defined.
    /// </summary>
    public static bool HasTraits(string catalogId)
    {
        return Traits.ContainsKey(catalogId);
    }

    /// <summary>
    /// Get all trait IDs for a card (for validation).
    /// </summary>
    public static List<CardTraitId> GetAllTraitIds(string catalogId)
    {
        var ids = new List<CardTraitId>();

        if (!Traits.TryGetValue(catalogId, out var cardTraits))
        {
            return ids;
        }

        foreach (var levelTraits in cardTraits.Values)
        {
            foreach (var trait in levelTraits)
            {
                if (trait.Id.HasValue)
                {
                    ids.Add(trait.Id);
                }
            }
        }

        return ids;
    }
}
