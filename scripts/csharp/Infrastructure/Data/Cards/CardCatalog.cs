using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Fateforged.Constants;
using Fateforged.Stats;
using Fateforged.Units;

namespace Fateforged.Cards;

/// <summary>
/// Central registry of all card definitions.
/// Provides type-safe card lookup and query methods.
/// GDScript can call this via the CardCatalogBridge autoload.
///
/// Card definitions are stored in CardDefinitions.cs as static readonly fields.
/// This class provides lookup and query methods over those definitions.
/// </summary>
public static class CardCatalog
{
    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get a card definition by ID. Returns null if not found.</summary>
    public static CardDefinition? GetCard(string id) => CardDefinitions.Get(id);

    /// <summary>Get a card definition by typed CardId. Returns null if not found.</summary>
    public static CardDefinition? GetCard(CardId id) => CardDefinitions.Get(id);

    /// <summary>Check if a card exists in the catalog.</summary>
    public static bool HasCard(string id) => CardDefinitions.Has(id);

    /// <summary>Check if a card exists in the catalog by typed CardId.</summary>
    public static bool HasCard(CardId id) => CardDefinitions.Has(id);

    /// <summary>Get all card IDs.</summary>
    public static string[] GetAllCardIds() => [.. CardDefinitions.AllIds];

    /// <summary>Get all card definitions.</summary>
    public static CardDefinition[] GetAllCards() => [.. CardDefinitions.All];

    /// <summary>Get card count.</summary>
    public static int Count => CardDefinitions.Count;

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    /// <summary>Get cards by rarity.</summary>
    public static CardDefinition[] GetCardsByRarity(Rarity rarity)
    {
        return CardDefinitions.All.Where(c => c.Rarity == rarity).ToArray();
    }

    /// <summary>Get cards by type.</summary>
    public static CardDefinition[] GetCardsByType(CardType type)
    {
        return CardDefinitions.All.Where(c => c.Type == type).ToArray();
    }

    /// <summary>Get summon cards by creature type (any match).</summary>
    public static CardDefinition[] GetCardsByCreatureType(CreatureType creatureType)
    {
        return CardDefinitions.All
            .Where(c => c.Type == CardType.Summon && (c.CreatureTypes & creatureType) != 0)
            .ToArray();
    }

    /// <summary>Get summon cards by role (any match).</summary>
    public static CardDefinition[] GetCardsByRole(SummonRole role)
    {
        return CardDefinitions.All
            .Where(c => c.Type == CardType.Summon && (c.Roles & role) != 0)
            .ToArray();
    }

    /// <summary>Get spell cards by category.</summary>
    public static CardDefinition[] GetSpellsByCategory(SpellCategory category)
    {
        return CardDefinitions.All
            .Where(c => c.Type == CardType.Spell && c.SpellCategory == category)
            .ToArray();
    }

    /// <summary>Get spell cards by targeting mode.</summary>
    public static CardDefinition[] GetSpellsByTargeting(SpellTargeting targeting)
    {
        return CardDefinitions.All
            .Where(c => c.Type == CardType.Spell && c.SpellTargeting == targeting)
            .ToArray();
    }

    /// <summary>Get cards with specific flags.</summary>
    public static CardDefinition[] GetCardsWithFlags(CardFlags flags)
    {
        return CardDefinitions.All
            .Where(c => (c.Flags & flags) == flags)
            .ToArray();
    }

    /// <summary>Get starter/default cards.</summary>
    public static CardDefinition[] GetStarterCards()
    {
        return CardDefinitions.All.Where(c => c.UnlockCondition == UnlockCondition.Default).ToArray();
    }

    /// <summary>Get cards by elemental affinity.</summary>
    public static CardDefinition[] GetCardsByElement(Element element)
    {
        return CardDefinitions.All.Where(c => c.ElementalAffinity == element).ToArray();
    }

    // =========================================================================
    // GODOT DICTIONARY CONVERSION (for GDScript interop)
    // =========================================================================

    /// <summary>
    /// Convert a CardDefinition to a Godot Dictionary for GDScript consumption.
    /// Maintains compatibility with existing GDScript code expecting dictionary format.
    /// Enums are converted to lowercase strings to match GDScript constants.
    /// </summary>
    public static Godot.Collections.Dictionary ToDictionary(CardDefinition card)
    {
        // Resolve scene path and stats from UnitDefinitions when UnitId is set
        string scenePath = card.UnitId.HasValue && UnitDefinitions.TryGet(card.UnitId, out var unitDef) && unitDef != null
            ? unitDef.ScenePath
            : card.UnitScenePath;

        // Get base stats from UnitDefinitions (with card modifier applied) or from card directly
        UnitStats stats;
        UnitDefinition? def = null;
        if (card.UnitId.HasValue && UnitDefinitions.TryGet(card.UnitId, out def) && def != null)
        {
            stats = def.Stats;
            if (card.UnitModifier != null)
            {
                stats = stats.WithModifiers([card.UnitModifier]);
            }
        }
        else
        {
            // Legacy: use card's direct stat properties
            stats = UnitStatCalculator.FromCardDefinition(card);
        }

        var dict = new Godot.Collections.Dictionary
        {
            ["catalog_id"] = (string)card.Id,
            ["card_name"] = card.Name,
            ["description"] = card.Description,
            ["rarity"] = card.Rarity.ToString().ToLowerInvariant(),
            ["card_type"] = (int)card.Type,
            ["mana_cost"] = card.ManaCost,
            ["cooldown"] = card.Cooldown,
            ["summon_time"] = card.SummonTime,
            ["unit_id"] = (string)card.UnitId,  // Convert to string for GDScript
            ["unit_scene_path"] = scenePath,
            ["spawn_count"] = card.SpawnCount,
            ["unit_type"] = card.UnitType.ToString().ToLowerInvariant(),
            ["max_hp"] = stats.MaxHp,
            ["attack_damage"] = stats.AttackDamage,
            ["attack_range"] = stats.AttackRange,
            ["attack_speed"] = stats.AttackSpeed,
            ["move_speed"] = stats.MoveSpeed,
            ["aggro_radius"] = stats.AggroRadius,
            ["is_ranged"] = card.IsRanged,
            ["projectile_scene_path"] = card.ProjectileScenePath,
            ["spell_damage"] = card.SpellDamage,
            ["spell_radius"] = card.SpellRadius,
            ["spell_duration"] = card.SpellDuration,
            ["projectile_id"] = (string)card.ProjectileId,
            ["spell_vfx"] = (string)card.SpellVfx,
            ["command_type"] = card.CommandType?.ToString().ToLowerInvariant() ?? "",
            ["selection_radius"] = card.SelectionRadius,
            ["formation_duration"] = card.FormationDuration,
            ["unlock_condition"] = UnlockConditionToString(card.UnlockCondition),
            ["card_icon_path"] = card.CardIconPath,
            // Separation radius from UnitDefinitions
            ["separation_radius"] = def?.Visual.SeparationRadius ?? 0.5f
        };

        if (Math.Abs(stats.SoulStrength) > 0.0001f)
            dict["soul_strength"] = stats.SoulStrength;

        var traitEligibilityTags = new Godot.Collections.Array<string>();
        foreach (var tag in card.TraitEligibilityTags)
            traitEligibilityTags.Add(tag);
        dict["trait_eligibility_tags"] = traitEligibilityTags;

        // Typed card properties (replaces old Tags system)
        dict["creature_types"] = (int)card.CreatureTypes;
        dict["roles"] = (int)card.Roles;
        dict["spell_category"] = card.SpellCategory.ToString().ToLowerInvariant();
        dict["spell_targeting"] = card.SpellTargeting.ToString().ToLowerInvariant();
        dict["visual_traits"] = (int)card.VisualTraits;
        dict["card_flags"] = (int)card.Flags;

        // Categories dict for elemental affinity (matches GDScript structure)
        var categories = new Godot.Collections.Dictionary
        {
            ["elemental_affinity"] = card.ElementalAffinity.ToString().ToLowerInvariant()
        };
        dict["categories"] = categories;

        return dict;
    }

    /// <summary>Convert UnlockCondition enum to GDScript-compatible string.</summary>
    private static string UnlockConditionToString(UnlockCondition condition) => condition switch
    {
        UnlockCondition.Default => "default",
        UnlockCondition.DevOnly => "dev_only",
        _ => condition.ToString().ToLowerInvariant()
    };

    /// <summary>Get card as dictionary for GDScript. Returns empty dict if not found.</summary>
    public static Godot.Collections.Dictionary GetCardAsDict(string id)
    {
        var card = GetCard(id);
        return card != null ? ToDictionary(card) : new Godot.Collections.Dictionary();
    }

    /// <summary>Get all cards as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetAllCardsAsDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in CardDefinitions.All)
        {
            result.Add(ToDictionary(card));
        }
        return result;
    }

    /// <summary>Get cards by rarity as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetCardsByRarityAsDict(string rarity)
    {
        if (!Enum.TryParse<Rarity>(rarity, ignoreCase: true, out var rarityEnum))
            return [];

        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in GetCardsByRarity(rarityEnum))
        {
            result.Add(ToDictionary(card));
        }
        return result;
    }

    /// <summary>Get cards by type as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetCardsByTypeAsDict(int type)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in GetCardsByType((CardType)type))
        {
            result.Add(ToDictionary(card));
        }
        return result;
    }

    /// <summary>Get starter cards as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetStarterCardsAsDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in GetStarterCards())
        {
            result.Add(ToDictionary(card));
        }
        return result;
    }

    /// <summary>Get cards by element as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetCardsByElementAsDict(string element)
    {
        if (!Enum.TryParse<Element>(element, ignoreCase: true, out var elementEnum))
            return [];

        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in GetCardsByElement(elementEnum))
        {
            result.Add(ToDictionary(card));
        }
        return result;
    }
}
