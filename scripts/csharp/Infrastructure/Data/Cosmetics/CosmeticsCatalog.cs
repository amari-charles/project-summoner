using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Fateforged.Data.Cosmetics;

/// <summary>
/// Central registry of all cosmetic definitions.
/// Provides type-safe cosmetic lookup and query methods.
/// GDScript can call this via the CosmeticsCatalogBridge autoload.
/// </summary>
public static class CosmeticsCatalog
{
    private static readonly Dictionary<string, CosmeticDefinition> _cosmetics = new()
    {
        // Card Backs
        ["card_back_default"] = new CosmeticDefinition
        {
            Id = "card_back_default",
            Type = CosmeticType.CardBack,
            NameKey = "cosmetic.card_back_default.name",
            DescriptionKey = "cosmetic.card_back_default.description",
            Price = 0,
            Rarity = "common",
        },

        // UI Themes
        ["ui_theme_default"] = new CosmeticDefinition
        {
            Id = "ui_theme_default",
            Type = CosmeticType.UiTheme,
            NameKey = "cosmetic.ui_theme_default.name",
            DescriptionKey = "cosmetic.ui_theme_default.description",
            Price = 0,
            Rarity = "common",
        },
    };

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    public static CosmeticDefinition? GetCosmetic(string id)
    {
        return _cosmetics.GetValueOrDefault(id);
    }

    public static bool HasCosmetic(string id)
    {
        return _cosmetics.ContainsKey(id);
    }

    public static CosmeticDefinition[] GetAllCosmetics()
    {
        return [.. _cosmetics.Values];
    }

    public static int Count => _cosmetics.Count;

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    public static CosmeticDefinition[] GetCosmeticsByType(CosmeticType type)
    {
        return _cosmetics.Values.Where(c => c.Type == type).ToArray();
    }

    public static CosmeticDefinition[] GetCardBacks()
    {
        return GetCosmeticsByType(CosmeticType.CardBack);
    }

    public static CosmeticDefinition[] GetUiThemes()
    {
        return GetCosmeticsByType(CosmeticType.UiTheme);
    }

    public static CosmeticDefinition[] GetSummonerSkins(string summonerId)
    {
        return _cosmetics
            .Values.Where(c => c.Type == CosmeticType.SummonerSkin && c.SummonerId == summonerId)
            .ToArray();
    }

    public static CosmeticDefinition[] GetPurchasableCosmetics()
    {
        return _cosmetics.Values.Where(c => c.Price > 0).ToArray();
    }

    // =========================================================================
    // TYPE CONVERSION
    // =========================================================================

    public static string TypeToString(CosmeticType type)
    {
        return type switch
        {
            CosmeticType.CardBack => "card_back",
            CosmeticType.UiTheme => "ui_theme",
            CosmeticType.SummonerSkin => "summoner_skin",
            _ => "unknown",
        };
    }

    public static CosmeticType StringToType(string typeStr)
    {
        return typeStr switch
        {
            "card_back" => CosmeticType.CardBack,
            "ui_theme" => CosmeticType.UiTheme,
            "summoner_skin" => CosmeticType.SummonerSkin,
            _ => CosmeticType.CardBack,
        };
    }

    // =========================================================================
    // GODOT DICTIONARY CONVERSION (for GDScript interop)
    // =========================================================================

    /// <summary>GDScript CosmeticType enum values — must match the bridge constants.</summary>
    private static int TypeToGdInt(CosmeticType type) =>
        type switch
        {
            CosmeticType.CardBack => 0,
            CosmeticType.UiTheme => 1,
            CosmeticType.SummonerSkin => 2,
            _ => 0,
        };

    public static Godot.Collections.Dictionary ToDictionary(CosmeticDefinition cosmetic)
    {
        return new Godot.Collections.Dictionary
        {
            ["id"] = cosmetic.Id,
            ["type"] = TypeToGdInt(cosmetic.Type),
            ["display_name"] = cosmetic.NameKey,
            ["description"] = cosmetic.DescriptionKey,
            ["preview_path"] = cosmetic.PreviewPath,
            ["resource_path"] = cosmetic.ResourcePath,
            ["summoner_id"] = cosmetic.SummonerId,
            ["price"] = cosmetic.Price,
            ["rarity"] = cosmetic.Rarity,
        };
    }

    public static Godot.Collections.Dictionary GetCosmeticAsDict(string id)
    {
        var cosmetic = GetCosmetic(id);
        return cosmetic != null ? ToDictionary(cosmetic) : new Godot.Collections.Dictionary();
    }
}
