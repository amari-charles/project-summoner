using System;

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
/// </summary>
public static class ResourceTypeExtensions
{
    /// <summary>Convert to GDScript-compatible string key.</summary>
    public static string ToKey(this ResourceType type) => type.ToString().ToLowerInvariant();

    /// <summary>Parse from GDScript string key.</summary>
    public static ResourceType? FromKey(string key) =>
        Enum.TryParse<ResourceType>(key, ignoreCase: true, out var result) ? result : null;
}
