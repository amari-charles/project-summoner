using Godot;

namespace ProjectSummoner.Services.Campaign.Models;

/// <summary>
/// Extension methods for Godot.Collections.Dictionary to provide GetValueOrDefault functionality.
/// </summary>
public static class GodotDictionaryExtensions
{
    /// <summary>
    /// Gets the value associated with the specified key, or returns a default value if the key is not found.
    /// </summary>
    public static Variant GetValueOrDefault(this Godot.Collections.Dictionary dict, string key, Variant defaultValue)
    {
        if (dict.ContainsKey(key))
        {
            return dict[key];
        }
        return defaultValue;
    }

    /// <summary>
    /// Gets the value associated with the specified key, or returns a default string if the key is not found.
    /// </summary>
    public static Variant GetValueOrDefault(this Godot.Collections.Dictionary dict, string key, string defaultValue)
    {
        if (dict.ContainsKey(key))
        {
            return dict[key];
        }
        return defaultValue;
    }

    /// <summary>
    /// Gets the value associated with the specified key, or returns a default int if the key is not found.
    /// </summary>
    public static Variant GetValueOrDefault(this Godot.Collections.Dictionary dict, string key, int defaultValue)
    {
        if (dict.ContainsKey(key))
        {
            return dict[key];
        }
        return defaultValue;
    }

    /// <summary>
    /// Gets the value associated with the specified key, or returns a default bool if the key is not found.
    /// </summary>
    public static Variant GetValueOrDefault(this Godot.Collections.Dictionary dict, string key, bool defaultValue)
    {
        if (dict.ContainsKey(key))
        {
            return dict[key];
        }
        return defaultValue;
    }
}
