namespace Fateforged.Meta.Shop;

/// <summary>
/// Type of currency used for purchases.
/// </summary>
public enum CurrencyType
{
    /// <summary>In-game gold earned from battles.</summary>
    Gold,

    /// <summary>Premium currency (purchasable).</summary>
    Gems,

    /// <summary>Real-money purchases via platform billing.</summary>
    RealMoney
}

/// <summary>
/// Extension methods for CurrencyType.
/// </summary>
public static class CurrencyTypeExtensions
{
    /// <summary>Convert enum to string value for GDScript interop.</summary>
    public static string ToStringValue(this CurrencyType type) => type switch
    {
        CurrencyType.Gold => "gold",
        CurrencyType.Gems => "gems",
        CurrencyType.RealMoney => "real_money",
        _ => "gold"
    };

    /// <summary>Parse string to CurrencyType.</summary>
    public static CurrencyType ParseCurrencyType(string value) => value switch
    {
        "gold" => CurrencyType.Gold,
        "gems" => CurrencyType.Gems,
        "real_money" => CurrencyType.RealMoney,
        _ => CurrencyType.Gold
    };
}
