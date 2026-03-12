using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Traits;
using Godot;

namespace Fateforged.Data.Summoners;

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
        // FATEFORGERS (Core starting summoners)
        // =====================================================================

        [SummonerIds.Cole] = new SummonerDefinition
        {
            Id = SummonerIds.Cole,
            NameKey = "summoner.cole.name",
            DescriptionKey = "summoner.cole.description",
            ElementalAffinity = Element.Fire,
            BaseHealth = 1000.0f,
            MaxMana = 100.0f,
            IconPath = "",
            CardFrameStyle = "legendary",
            UnlockCondition = SummonerUnlockCondition.StartingChoice,
            InnateTraitIds = [TraitIds.FireAffinity, TraitIds.BurningSpirit],
            TraitEligibilityTags =
            [
                TraitTags.Summoner,
                TraitTags.Global,
                TraitTags.Fire,
                TraitTags.Cole,
            ],
            StarterCardId = CardIds.FireWisp,
        },

        [SummonerIds.Selene] = new SummonerDefinition
        {
            Id = SummonerIds.Selene,
            NameKey = "summoner.selene.name",
            DescriptionKey = "summoner.selene.description",
            ElementalAffinity = Element.Water,
            BaseHealth = 1200.0f,
            MaxMana = 100.0f,
            IconPath = "",
            CardFrameStyle = "legendary",
            UnlockCondition = SummonerUnlockCondition.StartingChoice,
            InnateTraitIds = [TraitIds.WaterAffinity, TraitIds.TidalResilience],
            TraitEligibilityTags =
            [
                TraitTags.Summoner,
                TraitTags.Global,
                TraitTags.Water,
                TraitTags.Selene,
            ],
            StarterCardId = CardIds.WaterWisp,
        },

        [SummonerIds.Mei] = new SummonerDefinition
        {
            Id = SummonerIds.Mei,
            NameKey = "summoner.mei.name",
            DescriptionKey = "summoner.mei.description",
            ElementalAffinity = Element.Wind,
            BaseHealth = 900.0f,
            MaxMana = 100.0f,
            IconPath = "",
            CardFrameStyle = "legendary",
            UnlockCondition = SummonerUnlockCondition.StartingChoice,
            InnateTraitIds = [TraitIds.WindAffinity, TraitIds.SwiftCasting],
            TraitEligibilityTags =
            [
                TraitTags.Summoner,
                TraitTags.Global,
                TraitTags.Wind,
                TraitTags.Mei,
            ],
            StarterCardId = CardIds.WindWisp,
        },

        [SummonerIds.Teo] = new SummonerDefinition
        {
            Id = SummonerIds.Teo,
            NameKey = "summoner.teo.name",
            DescriptionKey = "summoner.teo.description",
            ElementalAffinity = Element.Earth,
            BaseHealth = 1500.0f,
            MaxMana = 100.0f,
            IconPath = "",
            CardFrameStyle = "legendary",
            UnlockCondition = SummonerUnlockCondition.StartingChoice,
            InnateTraitIds = [TraitIds.EarthAffinity, TraitIds.StoneFortitude],
            TraitEligibilityTags =
            [
                TraitTags.Summoner,
                TraitTags.Global,
                TraitTags.Earth,
                TraitTags.Teo,
            ],
            StarterCardId = CardIds.EarthWisp,
        },

        // =====================================================================
        // DEV/TEST SUMMONERS
        // =====================================================================

        [SummonerIds.ManaTest] = new SummonerDefinition
        {
            Id = SummonerIds.ManaTest,
            NameKey = "summoner.summoner_mana_test.name",
            DescriptionKey = "summoner.summoner_mana_test.description",
            ElementalAffinity = Element.Neutral,
            BaseHealth = 1000.0f,
            MaxMana = 100.0f,
            IconPath = "",
            CardFrameStyle = "common",
            UnlockCondition = SummonerUnlockCondition.DevOnly,
            InnateTraitIds = [],
        },
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
        return _summoners
            .Values.Where(s => s.UnlockCondition == SummonerUnlockCondition.StartingChoice)
            .ToArray();
    }

    /// <summary>Get summoners available for "Random" option (core + starter-only).</summary>
    public static SummonerDefinition[] GetRandomPoolSummoners()
    {
        return _summoners
            .Values.Where(s =>
                s.UnlockCondition == SummonerUnlockCondition.StartingChoice
                || s.UnlockCondition == SummonerUnlockCondition.RandomStarterOnly
            )
            .ToArray();
    }

    /// <summary>Get summoners available for purchase in the Premium Store.</summary>
    public static SummonerDefinition[] GetPurchasableSummoners()
    {
        return _summoners
            .Values.Where(s => s.UnlockCondition == SummonerUnlockCondition.PremiumPurchase)
            .ToArray();
    }

    /// <summary>Get summoners by element.</summary>
    public static SummonerDefinition[] GetSummonersByElement(Element element)
    {
        return _summoners.Values.Where(s => s.ElementalAffinity == element).ToArray();
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
            traitsArray.Add((string)traitId);
        }

        return new Godot.Collections.Dictionary
        {
            ["summoner_id"] = (string)summoner.Id,
            ["name_key"] = summoner.NameKey,
            ["description_key"] = summoner.DescriptionKey,
            ["element_id"] = (int)ElementToGdElementId(summoner.ElementalAffinity),
            ["base_health"] = summoner.BaseHealth,
            ["max_mana"] = summoner.MaxMana,
            ["summoner_icon_path"] = summoner.IconPath,
            ["card_frame_style"] = summoner.CardFrameStyle,
            ["portrait_uv_offset"] = summoner.PortraitUvOffset,
            ["portrait_uv_scale"] = summoner.PortraitUvScale,
            ["unlock_condition"] = summoner.UnlockCondition.ToGdString(),
            ["innate_trait_ids"] = traitsArray,
            ["starter_card_id"] = (string)summoner.StarterCardId,
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
            _ => 0,
        };
    }
}
