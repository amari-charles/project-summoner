namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Player resource/currency data.
/// </summary>
public class ResourceData
{
    /// <summary>Gold - earned through gameplay, used for in-game purchases.</summary>
    public int Gold { get; set; }

    /// <summary>Gems - premium currency (purchased with real money).</summary>
    public int Gems { get; set; }

    /// <summary>Essence - used for card upgrades.</summary>
    public int Essence { get; set; }

    /// <summary>Fragments - collectible currency.</summary>
    public int Fragments { get; set; }

    /// <summary>Profile ID reference.</summary>
    public string ProfileId { get; set; } = "";

    /// <summary>Last update timestamp.</summary>
    public long UpdatedAt { get; set; }
}

/// <summary>
/// Resource type enum for type-safe resource operations.
/// </summary>
public enum ResourceType
{
    Gold,
    Gems,
    Essence,
    Fragments
}

/// <summary>
/// Extension methods for ResourceType.
/// </summary>
public static class ResourceTypeExtensions
{
    /// <summary>Convert to GDScript-compatible string key.</summary>
    public static string ToKey(this ResourceType type) => type switch
    {
        ResourceType.Gold => "gold",
        ResourceType.Gems => "gems",
        ResourceType.Essence => "essence",
        ResourceType.Fragments => "fragments",
        _ => type.ToString().ToLowerInvariant()
    };

    /// <summary>Parse from GDScript string key.</summary>
    public static ResourceType? FromKey(string key) => key switch
    {
        "gold" => ResourceType.Gold,
        "gems" => ResourceType.Gems,
        "essence" => ResourceType.Essence,
        "fragments" => ResourceType.Fragments,
        _ => null
    };
}
