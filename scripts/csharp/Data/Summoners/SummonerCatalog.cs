using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Cards;
using ProjectSummoner.Data.Traits;

namespace ProjectSummoner.Data.Summoners;

/// <summary>
/// Central registry of all summoner definitions.
/// Provides type-safe summoner lookup and query methods.
/// GDScript can call this via the SummonerCatalogBridge autoload.
/// </summary>
public static class SummonerCatalog
{
    // =========================================================================
    // SUMMONER DEFINITIONS
    // =========================================================================

    private static readonly Dictionary<string, SummonerDefinition> _summoners = new()
    {
        // =====================================================================
        // STARTING SUMMONERS (Core 4 - Always available at start)
        // =====================================================================

        [SummonerId.Fire] = new SummonerDefinition
        {
            Id = SummonerId.Fire,
            NameKey = "summoner.summoner_fire.name",
            DescriptionKey = "summoner.summoner_fire.description",
            ElementalAffinity = Element.Fire,
            BaseHealth = 1000.0f,
            MaxMana = 100.0f,
            IconPath = "",
            CardFrameStyle = "legendary",
            UnlockCondition = SummonerUnlockCondition.StartingChoice,
            InnateTraitIds = [TraitId.FireAffinity, TraitId.BurningSpirit]
        },

        [SummonerId.Water] = new SummonerDefinition
        {
            Id = SummonerId.Water,
            NameKey = "summoner.summoner_water.name",
            DescriptionKey = "summoner.summoner_water.description",
            ElementalAffinity = Element.Water,
            BaseHealth = 1200.0f,
            MaxMana = 100.0f,
            IconPath = "",
            CardFrameStyle = "legendary",
            UnlockCondition = SummonerUnlockCondition.StartingChoice,
            InnateTraitIds = [TraitId.WaterAffinity, TraitId.TidalResilience]
        },

        [SummonerId.Wind] = new SummonerDefinition
        {
            Id = SummonerId.Wind,
            NameKey = "summoner.summoner_wind.name",
            DescriptionKey = "summoner.summoner_wind.description",
            ElementalAffinity = Element.Wind,
            BaseHealth = 900.0f,
            MaxMana = 100.0f,
            IconPath = "",
            CardFrameStyle = "legendary",
            UnlockCondition = SummonerUnlockCondition.StartingChoice,
            InnateTraitIds = [TraitId.WindAffinity, TraitId.SwiftCasting]
        },

        [SummonerId.Earth] = new SummonerDefinition
        {
            Id = SummonerId.Earth,
            NameKey = "summoner.summoner_earth.name",
            DescriptionKey = "summoner.summoner_earth.description",
            ElementalAffinity = Element.Earth,
            BaseHealth = 1500.0f,
            MaxMana = 100.0f,
            IconPath = "res://assets/characters/summoners/terravorn_portrait.jpg",
            CardFrameStyle = "legendary",
            UnlockCondition = SummonerUnlockCondition.StartingChoice,
            InnateTraitIds = [TraitId.EarthAffinity, TraitId.StoneFortitude]
        },

        // =====================================================================
        // RANDOM POOL SUMMONERS (Starter-only)
        // =====================================================================

        [SummonerId.ShadowInitiate] = new SummonerDefinition
        {
            Id = SummonerId.ShadowInitiate,
            NameKey = "summoner.summoner_shadow_initiate.name",
            DescriptionKey = "summoner.summoner_shadow_initiate.description",
            ElementalAffinity = Element.Shadow,
            BaseHealth = 950.0f,
            MaxMana = 100.0f,
            IconPath = "",
            CardFrameStyle = "rare",
            UnlockCondition = SummonerUnlockCondition.RandomStarterOnly,
            InnateTraitIds = []
        },

        // =====================================================================
        // PURCHASABLE SUMMONERS (Premium Store)
        // =====================================================================

        [SummonerId.LightningAdept] = new SummonerDefinition
        {
            Id = SummonerId.LightningAdept,
            NameKey = "summoner.summoner_lightning_adept.name",
            DescriptionKey = "summoner.summoner_lightning_adept.description",
            ElementalAffinity = Element.Lightning,
            BaseHealth = 800.0f,
            MaxMana = 100.0f,
            IconPath = "",
            CardFrameStyle = "epic",
            UnlockCondition = SummonerUnlockCondition.PremiumPurchase,
            InnateTraitIds = [TraitId.LightningAffinity]
        },

        [SummonerId.VerdantSage] = new SummonerDefinition
        {
            Id = SummonerId.VerdantSage,
            NameKey = "summoner.summoner_verdant_sage.name",
            DescriptionKey = "summoner.summoner_verdant_sage.description",
            ElementalAffinity = Element.Life,
            BaseHealth = 1100.0f,
            MaxMana = 100.0f,
            IconPath = "",
            CardFrameStyle = "epic",
            UnlockCondition = SummonerUnlockCondition.PremiumPurchase,
            InnateTraitIds = [TraitId.LifeAffinity]
        },

        [SummonerId.VoidWalker] = new SummonerDefinition
        {
            Id = SummonerId.VoidWalker,
            NameKey = "summoner.summoner_void_walker.name",
            DescriptionKey = "summoner.summoner_void_walker.description",
            ElementalAffinity = Element.Death,
            BaseHealth = 950.0f,
            MaxMana = 100.0f,
            IconPath = "",
            CardFrameStyle = "epic",
            UnlockCondition = SummonerUnlockCondition.PremiumPurchase,
            InnateTraitIds = [TraitId.DeathAffinity]
        },

        // =====================================================================
        // DEV/TEST SUMMONERS
        // =====================================================================

        [SummonerId.ManaTest] = new SummonerDefinition
        {
            Id = SummonerId.ManaTest,
            NameKey = "summoner.summoner_mana_test.name",
            DescriptionKey = "summoner.summoner_mana_test.description",
            ElementalAffinity = Element.Neutral,
            BaseHealth = 1000.0f,
            MaxMana = 100.0f,
            IconPath = "",
            CardFrameStyle = "common",
            UnlockCondition = SummonerUnlockCondition.DevOnly,
            InnateTraitIds = []
        }
    };

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get a summoner definition by ID. Returns null if not found.</summary>
    public static SummonerDefinition? GetSummoner(string id)
    {
        return _summoners.GetValueOrDefault(id);
    }

    /// <summary>Check if a summoner exists in the catalog.</summary>
    public static bool HasSummoner(string id)
    {
        return _summoners.ContainsKey(id);
    }

    /// <summary>Get all summoner IDs.</summary>
    public static string[] GetAllSummonerIds()
    {
        return [.. _summoners.Keys];
    }

    /// <summary>Get all summoner definitions.</summary>
    public static SummonerDefinition[] GetAllSummoners()
    {
        return [.. _summoners.Values];
    }

    /// <summary>Get summoner count.</summary>
    public static int Count => _summoners.Count;

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    /// <summary>Get summoners that can be selected as starting summoners (4 core).</summary>
    public static SummonerDefinition[] GetStartingSummoners()
    {
        return _summoners.Values
            .Where(s => s.UnlockCondition == SummonerUnlockCondition.StartingChoice)
            .ToArray();
    }

    /// <summary>Get summoners available for "Random" option (core + starter-only).</summary>
    public static SummonerDefinition[] GetRandomPoolSummoners()
    {
        return _summoners.Values
            .Where(s => s.UnlockCondition == SummonerUnlockCondition.StartingChoice
                     || s.UnlockCondition == SummonerUnlockCondition.RandomStarterOnly)
            .ToArray();
    }

    /// <summary>Get summoners available for purchase in the Premium Store.</summary>
    public static SummonerDefinition[] GetPurchasableSummoners()
    {
        return _summoners.Values
            .Where(s => s.UnlockCondition == SummonerUnlockCondition.PremiumPurchase)
            .ToArray();
    }

    /// <summary>Get summoners by element.</summary>
    public static SummonerDefinition[] GetSummonersByElement(Element element)
    {
        return _summoners.Values
            .Where(s => s.ElementalAffinity == element)
            .ToArray();
    }

    // =========================================================================
    // GODOT DICTIONARY CONVERSION (for GDScript interop)
    // =========================================================================

    /// <summary>Convert a SummonerDefinition to a Godot Dictionary for GDScript consumption.</summary>
    public static Godot.Collections.Dictionary ToDictionary(SummonerDefinition summoner)
    {
        var traitsArray = new Godot.Collections.Array();
        foreach (var traitId in summoner.InnateTraitIds)
        {
            traitsArray.Add(traitId);
        }

        return new Godot.Collections.Dictionary
        {
            ["summoner_id"] = summoner.Id,
            ["name_key"] = summoner.NameKey,
            ["description_key"] = summoner.DescriptionKey,
            ["element_id"] = (int)ElementToGdElementId(summoner.ElementalAffinity),
            ["base_health"] = summoner.BaseHealth,
            ["max_mana"] = summoner.MaxMana,
            ["summoner_icon_path"] = summoner.IconPath,
            ["card_frame_style"] = summoner.CardFrameStyle,
            ["unlock_condition"] = summoner.UnlockCondition.ToGdString(),
            ["innate_trait_ids"] = traitsArray
        };
    }

    /// <summary>Get summoner as dictionary for GDScript. Returns empty dict if not found.</summary>
    public static Godot.Collections.Dictionary GetSummonerAsDict(string id)
    {
        var summoner = GetSummoner(id);
        return summoner != null ? ToDictionary(summoner) : new Godot.Collections.Dictionary();
    }

    /// <summary>Get all summoners as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetAllSummonersAsDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var summoner in _summoners.Values)
        {
            result.Add(ToDictionary(summoner));
        }
        return result;
    }

    /// <summary>Get starting summoners as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetStartingSummonersAsDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var summoner in GetStartingSummoners())
        {
            result.Add(ToDictionary(summoner));
        }
        return result;
    }

    /// <summary>Get random pool summoners as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetRandomPoolSummonersAsDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var summoner in GetRandomPoolSummoners())
        {
            result.Add(ToDictionary(summoner));
        }
        return result;
    }

    /// <summary>Get purchasable summoners as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetPurchasableSummonersAsDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var summoner in GetPurchasableSummoners())
        {
            result.Add(ToDictionary(summoner));
        }
        return result;
    }

    // =========================================================================
    // ELEMENT CONVERSION HELPERS
    // =========================================================================

    /// <summary>
    /// Convert C# Element enum to GDScript ElementRegistry.ElementId integer.
    /// Values must match scripts/data/element_registry.gd ElementId enum.
    /// </summary>
    public static int ElementToGdElementId(Element element)
    {
        return element switch
        {
            Element.Neutral => 0,
            Element.Fire => 1,
            Element.Water => 2,
            Element.Wind => 3,
            Element.Earth => 4,
            Element.Lightning => 5,
            Element.Shadow => 6,
            Element.Poison => 7,
            Element.Life => 8,
            Element.Death => 9,
            Element.Occultist => 10,
            Element.Holy => 11,
            Element.Ice => 12,
            Element.Metal => 13,
            Element.Spirit => 14,
            _ => 0
        };
    }
}
