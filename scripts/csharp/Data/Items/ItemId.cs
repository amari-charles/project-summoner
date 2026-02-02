namespace ProjectSummoner.Data.Items;

/// <summary>
/// Strongly-typed identifier for item types.
/// Prevents typos and enables IDE autocomplete via ItemIds static class.
/// </summary>
public readonly record struct ItemId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(ItemId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator ItemId(string value) => new(value);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset item ID.</summary>
    public static readonly ItemId None = new("");
}

/// <summary>
/// All known item IDs. Use these instead of raw strings.
/// Example: ItemIds.VeteransMedal instead of "item_veterans_medal"
/// </summary>
public static class ItemIds
{
    // =========================================================================
    // RARE EQUIPMENT
    // =========================================================================

    public static readonly ItemId VeteransMedal = new("item_veterans_medal");
    public static readonly ItemId BattleHardenedBadge = new("item_battle_hardened_badge");
    public static readonly ItemId FortunesCharm = new("item_fortunes_charm");
    public static readonly ItemId BoldFortuneAmulet = new("item_bold_fortune_amulet");

    // =========================================================================
    // STARTER ITEMS (one per slot: Weapon, Ring1, Ring2, Vestments)
    // =========================================================================

    public static readonly ItemId TrainingBlade = new("item_training_blade");
    public static readonly ItemId SimpleRing = new("item_simple_ring");
    public static readonly ItemId LuckyBand = new("item_lucky_band");
    public static readonly ItemId TravelersCloak = new("item_travelers_cloak");
}
