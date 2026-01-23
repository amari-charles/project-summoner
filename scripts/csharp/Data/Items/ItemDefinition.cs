using System.Collections.Generic;
using ProjectSummoner.Data.Traits;

namespace ProjectSummoner.Data.Items;

/// <summary>
/// Defines an equippable item.
/// Items provide stat modifiers when equipped to a summoner's equipment slot.
/// </summary>
public class ItemDefinition
{
    /// <summary>Unique identifier (e.g., "item_veterans_medal").</summary>
    public required string Id { get; init; }

    /// <summary>Localization key for display name.</summary>
    public required string NameKey { get; init; }

    /// <summary>Localization key for description.</summary>
    public required string DescriptionKey { get; init; }

    /// <summary>Equipment slot this item occupies.</summary>
    public required ItemSlot Slot { get; init; }

    /// <summary>Binding type - determines if item can be shared between summoners.</summary>
    public ItemBinding Binding { get; init; } = ItemBinding.AccountWide;

    /// <summary>Rarity tier for display purposes.</summary>
    public string Rarity { get; init; } = "common";

    /// <summary>Icon resource path for UI display.</summary>
    public string? IconPath { get; init; }

    /// <summary>
    /// List of modifiers this item provides when equipped.
    /// Uses the same TraitModifier system as traits/boons.
    /// </summary>
    public List<TraitModifier> Modifiers { get; init; } = [];

    /// <summary>
    /// Optional: ID of the boon this item was converted from during migration.
    /// Used to maintain backwards compatibility and for legacy reference.
    /// </summary>
    public string? LegacyBoonId { get; init; }
}
