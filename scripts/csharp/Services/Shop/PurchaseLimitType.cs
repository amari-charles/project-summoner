namespace Fateforged.Meta.Shop;

/// <summary>
/// Type of purchase limit for shop offerings.
/// Mirrors the string values in scripts/resources/shop_offering.gd purchase_limit_type.
/// </summary>
public enum PurchaseLimitType
{
    /// <summary>No purchase limit.</summary>
    None,

    /// <summary>Limit resets when shop refreshes.</summary>
    PerRefresh,

    /// <summary>Account-wide permanent limit.</summary>
    Account
}

/// <summary>
/// Extension methods for PurchaseLimitType.
/// </summary>
public static class PurchaseLimitTypeExtensions
{
    /// <summary>Convert enum to string value for GDScript interop.</summary>
    public static string ToStringValue(this PurchaseLimitType type) => type switch
    {
        PurchaseLimitType.None => "none",
        PurchaseLimitType.PerRefresh => "per_refresh",
        PurchaseLimitType.Account => "account",
        _ => "none"
    };

    /// <summary>Parse string to PurchaseLimitType.</summary>
    public static PurchaseLimitType ParsePurchaseLimitType(string value) => value switch
    {
        "none" => PurchaseLimitType.None,
        "per_refresh" => PurchaseLimitType.PerRefresh,
        "account" => PurchaseLimitType.Account,
        _ => PurchaseLimitType.None
    };
}
