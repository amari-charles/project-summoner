using Godot;

namespace Fateforged.Session;

internal static class GodotDictionaryExtensions
{
    public static Variant GetValueOrDefault(
        this Godot.Collections.Dictionary dict,
        string key,
        Variant defaultValue
    )
    {
        return dict.TryGetValue(key, out var value) ? value : defaultValue;
    }
}
