using Godot;
using System.Collections.Generic;

namespace ProjectSummoner.Cards;

/// <summary>
/// Represents a player's owned instance of a card.
/// This is the mutable player data (levels, upgrades, XP) as opposed to
/// CardConfig which is the immutable template (base stats, formation, etc.)
/// </summary>
[GlobalClass]
public partial class PlayerCardInstance : Resource
{
    /// <summary>
    /// Unique instance ID (UUID) - identifies this specific card in the player's collection
    /// </summary>
    [Export]
    public string Id { get; set; } = "";

    /// <summary>
    /// Reference to the base card template (catalog_id from CardCatalog)
    /// </summary>
    [Export]
    public string CatalogId { get; set; } = "";

    /// <summary>
    /// Current level (1-10)
    /// </summary>
    [Export]
    public int Level { get; set; } = 1;

    /// <summary>
    /// Current XP progress toward next level
    /// </summary>
    [Export]
    public int Xp { get; set; } = 0;

    /// <summary>
    /// List of upgrade IDs that have been applied to this card
    /// </summary>
    [Export]
    public Godot.Collections.Array<string> Upgrades { get; set; } = new();

    /// <summary>
    /// Card rarity (common, uncommon, rare, epic, legendary)
    /// Affects XP thresholds and stat scaling
    /// </summary>
    [Export]
    public string Rarity { get; set; } = "common";

    /// <summary>
    /// Create a PlayerCardInstance from a GDScript dictionary (for migration)
    /// </summary>
    public static PlayerCardInstance FromDictionary(Godot.Collections.Dictionary dict)
    {
        var instance = new PlayerCardInstance();

        if (dict.TryGetValue("id", out var id))
            instance.Id = id.AsString();

        if (dict.TryGetValue("catalog_id", out var catalogId))
            instance.CatalogId = catalogId.AsString();

        if (dict.TryGetValue("level", out var level))
            instance.Level = level.AsInt32();

        if (dict.TryGetValue("xp", out var xp))
            instance.Xp = xp.AsInt32();

        if (dict.TryGetValue("upgrades", out var upgrades) && upgrades.VariantType == Variant.Type.Array)
        {
            var upgradeArray = upgrades.AsGodotArray<string>();
            instance.Upgrades = new Godot.Collections.Array<string>(upgradeArray);
        }

        if (dict.TryGetValue("rarity", out var rarity))
            instance.Rarity = rarity.AsString();

        return instance;
    }

    /// <summary>
    /// Convert to GDScript-compatible dictionary (for storage/interop)
    /// </summary>
    public Godot.Collections.Dictionary ToDictionary()
    {
        return new Godot.Collections.Dictionary
        {
            { "id", Id },
            { "catalog_id", CatalogId },
            { "level", Level },
            { "xp", Xp },
            { "upgrades", Upgrades },
            { "rarity", Rarity }
        };
    }
}
