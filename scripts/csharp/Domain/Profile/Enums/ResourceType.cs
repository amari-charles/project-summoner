using ProjectSummoner.Infrastructure.Persistence;

namespace ProjectSummoner.Domain.Profile.Enums;

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
