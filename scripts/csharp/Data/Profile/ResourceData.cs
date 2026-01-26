using System.Text.Json.Serialization;
using ProjectSummoner.Data.Serialization;

namespace ProjectSummoner.Data.Profile;

/// <summary>
/// Player resource/currency data.
/// </summary>
public class ResourceData
{
    /// <summary>Gold - earned through gameplay, used for in-game purchases.</summary>
    [JsonPropertyName("gold")]
    public int Gold { get; set; }

    /// <summary>Gems - premium currency (purchased with real money).</summary>
    [JsonPropertyName("gems")]
    public int Gems { get; set; }

    /// <summary>Essence - used for card upgrades.</summary>
    [JsonPropertyName("essence")]
    public int Essence { get; set; }

    /// <summary>Fragments - collectible currency.</summary>
    [JsonPropertyName("fragments")]
    public int Fragments { get; set; }

    /// <summary>Profile ID reference.</summary>
    [JsonPropertyName("profile_id")]
    public string ProfileId { get; set; } = "";

    /// <summary>Last update timestamp.</summary>
    [JsonPropertyName("updated_at")]
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
/// Delegates to EnumSerializers for consistency.
/// </summary>
public static class ResourceTypeExtensions
{
    /// <summary>Convert to GDScript-compatible string key.</summary>
    public static string ToKey(this ResourceType type) => EnumSerializers.Serialize(type);

    /// <summary>Parse from GDScript string key.</summary>
    public static ResourceType? FromKey(string key) => EnumSerializers.DeserializeResourceType(key);
}
