namespace Fateforged.Meta.Shop;

/// <summary>
/// Type of shop (determines behavior like card binding).
/// </summary>
public enum ShopType
{
    /// <summary>General shop - cards are account-wide.</summary>
    General,

    /// <summary>Premium store - summoners, cosmetics, emotes.</summary>
    Premium,
}

/// <summary>
/// Extension methods for ShopType.
/// </summary>
public static class ShopTypeExtensions
{
    /// <summary>Convert enum to string value for GDScript interop.</summary>
    public static string ToStringValue(this ShopType type) =>
        type switch
        {
            ShopType.General => "general",
            ShopType.Premium => "premium",
            _ => "general",
        };

    /// <summary>Parse string to ShopType.</summary>
    public static ShopType ParseShopType(string value) =>
        value switch
        {
            "general" => ShopType.General,
            "premium" => ShopType.Premium,
            _ => ShopType.General,
        };
}
