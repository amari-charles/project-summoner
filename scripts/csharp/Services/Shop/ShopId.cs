namespace ProjectSummoner.Services.Shop;

/// <summary>
/// Strongly-typed identifier for shops.
/// Prevents typos and enables IDE autocomplete.
/// Note: For GDScript interop, use the string value via implicit conversion.
/// </summary>
public readonly record struct ShopId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(ShopId id) => id.Value;

    /// <summary>Explicit conversion from string.</summary>
    public static explicit operator ShopId(string value) => new(value);

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset shop ID.</summary>
    public static readonly ShopId None = new("");
}

/// <summary>
/// Well-known shop IDs.
/// </summary>
public static class ShopIds
{
    /// <summary>General shop (persistent UI shop).</summary>
    public static readonly ShopId General = new("general");

    /// <summary>Tutorial caravan shop.</summary>
    public static readonly ShopId CaravanTutorial = new("caravan_tutorial");

    /// <summary>Premium store (gems, summoners, cosmetics).</summary>
    public static readonly ShopId PremiumStore = new("premium_store");
}
