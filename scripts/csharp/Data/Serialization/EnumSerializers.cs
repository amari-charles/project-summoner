using System;
using Godot;
using ProjectSummoner.Data.Items;
using ProjectSummoner.Data.Profile;

namespace ProjectSummoner.Data.Serialization;

/// <summary>
/// Centralized serialization/deserialization for all enums used in profile data.
/// All enum↔string/int conversions go through this class for consistency.
/// </summary>
public static class EnumSerializers
{
    // =========================================================================
    // ItemSlot
    // =========================================================================

    /// <summary>Serialize ItemSlot to lowercase string for JSON/GDScript.</summary>
    public static string Serialize(ItemSlot slot) => slot switch
    {
        ItemSlot.Weapon => "weapon",
        ItemSlot.Ring1 => "ring1",
        ItemSlot.Ring2 => "ring2",
        ItemSlot.Vestments => "vestments",
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown ItemSlot value")
    };

    /// <summary>
    /// Deserialize string to ItemSlot.
    /// Returns null if the value is empty/null or invalid (with warning logged).
    /// </summary>
    public static ItemSlot? DeserializeSlot(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        return value switch
        {
            "weapon" => ItemSlot.Weapon,
            "ring1" => ItemSlot.Ring1,
            "ring2" => ItemSlot.Ring2,
            "vestments" => ItemSlot.Vestments,
            _ => LogAndReturnNull<ItemSlot>($"Unknown ItemSlot value: '{value}'")
        };
    }

    // =========================================================================
    // ContentBinding
    // =========================================================================

    /// <summary>Serialize ContentBinding to int for JSON/GDScript.</summary>
    public static int Serialize(ContentBinding binding) => (int)binding;

    /// <summary>
    /// Deserialize int to ContentBinding.
    /// Returns AccountWide if the value is invalid (with warning logged).
    /// </summary>
    public static ContentBinding DeserializeBinding(int value)
    {
        if (Enum.IsDefined(typeof(ContentBinding), value))
            return (ContentBinding)value;

        GD.PushWarning($"EnumSerializers: Unknown ContentBinding value: {value}, defaulting to AccountWide");
        return ContentBinding.AccountWide;
    }

    // =========================================================================
    // ResourceType
    // =========================================================================

    /// <summary>Serialize ResourceType to lowercase string for JSON/GDScript.</summary>
    public static string Serialize(ResourceType type) => type switch
    {
        ResourceType.Gold => "gold",
        ResourceType.Gems => "gems",
        ResourceType.Essence => "essence",
        ResourceType.Fragments => "fragments",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown ResourceType value")
    };

    /// <summary>
    /// Deserialize string to ResourceType.
    /// Returns null if the value is empty/null or invalid (with warning logged).
    /// </summary>
    public static ResourceType? DeserializeResourceType(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        return value switch
        {
            "gold" => ResourceType.Gold,
            "gems" => ResourceType.Gems,
            "essence" => ResourceType.Essence,
            "fragments" => ResourceType.Fragments,
            _ => LogAndReturnNull<ResourceType>($"Unknown ResourceType value: '{value}'")
        };
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>Log a warning and return null. Used for invalid enum deserialization.</summary>
    private static T? LogAndReturnNull<T>(string message) where T : struct
    {
        GD.PushWarning($"EnumSerializers: {message}");
        return null;
    }
}
