namespace Fateforged.Meta.Shop;

/// <summary>
/// Type of shop (determines behavior like card binding).
/// </summary>
public enum ShopType
{
    /// <summary>General shop - cards are account-wide.</summary>
    General,

    /// <summary>Caravan shop - cards are bound to active summoner.</summary>
    Caravan,

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
            ShopType.Caravan => "caravan",
            ShopType.Premium => "premium",
            _ => "general",
        };

    /// <summary>Parse string to ShopType.</summary>
    public static ShopType ParseShopType(string value) =>
        value switch
        {
            "general" => ShopType.General,
            "caravan" => ShopType.Caravan,
            "premium" => ShopType.Premium,
            _ => ShopType.General,
        };
}
