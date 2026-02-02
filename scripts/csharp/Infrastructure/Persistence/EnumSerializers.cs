using System;
using Godot;
using ProjectSummoner.Domain.Profile.Enums;
using ProjectSummoner.Domain.Profile.Inventory;

namespace ProjectSummoner.Infrastructure.Persistence;

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
        ItemSlot.Wand => "wand",
        ItemSlot.Ring1 => "ring1",
        ItemSlot.Ring2 => "ring2",
        ItemSlot.Robes => "robes",
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown ItemSlot value")
    };

    /// <summary>
    /// Deserialize string to ItemSlot.
    /// Returns null if the value is empty/null or invalid (with warning logged).
    /// Use DeserializeSlotStrict for critical paths where invalid values should throw.
    /// </summary>
    public static ItemSlot? DeserializeSlot(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        return value switch
        {
            "wand" => ItemSlot.Wand,
            "ring1" => ItemSlot.Ring1,
            "ring2" => ItemSlot.Ring2,
            "robes" => ItemSlot.Robes,
            _ => LogAndReturnNull<ItemSlot>($"Unknown ItemSlot value: '{value}'")
        };
    }

    /// <summary>
    /// Deserialize string to ItemSlot (strict mode).
    /// Throws ArgumentException if the value is empty/null or invalid.
    /// Use this in critical paths where data integrity is paramount.
    /// </summary>
    public static ItemSlot DeserializeSlotStrict(string? value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("ItemSlot value cannot be null or empty", nameof(value));

        return value switch
        {
            "wand" => ItemSlot.Wand,
            "ring1" => ItemSlot.Ring1,
            "ring2" => ItemSlot.Ring2,
            "robes" => ItemSlot.Robes,
            _ => throw new ArgumentException($"Unknown ItemSlot value: '{value}'", nameof(value))
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
    /// Use DeserializeBindingStrict for critical paths where invalid values should throw.
    /// </summary>
    public static ContentBinding DeserializeBinding(int value)
    {
        if (Enum.IsDefined(typeof(ContentBinding), value))
            return (ContentBinding)value;

        GD.PushWarning($"EnumSerializers: Unknown ContentBinding value: {value}, defaulting to AccountWide");
        return ContentBinding.AccountWide;
    }

    /// <summary>
    /// Deserialize int to ContentBinding (strict mode).
    /// Throws ArgumentException if the value is invalid.
    /// Use this in critical paths where data integrity is paramount.
    /// </summary>
    public static ContentBinding DeserializeBindingStrict(int value)
    {
        if (Enum.IsDefined(typeof(ContentBinding), value))
            return (ContentBinding)value;

        throw new ArgumentException($"Unknown ContentBinding value: {value}", nameof(value));
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
    /// Use DeserializeResourceTypeStrict for critical paths where invalid values should throw.
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

    /// <summary>
    /// Deserialize string to ResourceType (strict mode).
    /// Throws ArgumentException if the value is empty/null or invalid.
    /// Use this in critical paths where data integrity is paramount (e.g., currency transactions).
    /// </summary>
    public static ResourceType DeserializeResourceTypeStrict(string? value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("ResourceType value cannot be null or empty", nameof(value));

        return value switch
        {
            "gold" => ResourceType.Gold,
            "gems" => ResourceType.Gems,
            "essence" => ResourceType.Essence,
            "fragments" => ResourceType.Fragments,
            _ => throw new ArgumentException($"Unknown ResourceType value: '{value}'", nameof(value))
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
