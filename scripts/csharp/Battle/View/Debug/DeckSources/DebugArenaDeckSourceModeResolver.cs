using Godot;

namespace Fateforged.View.Debug.DeckSources;

public static class DebugArenaDeckSourceModeResolver
{
    public const string ConfigKey = "debug_arena_deck_source";
    public const string ValueFile = "file";
    public const string ValueContext = "context";
    public const string ValueOverride = "override";

    public static DebugArenaDeckSourceMode ResolveFromConfig(
        Godot.Collections.Dictionary config
    )
    {
        if (!config.ContainsKey(ConfigKey))
            return DebugArenaDeckSourceMode.ContextThenFileThenFallback;

        string raw = config[ConfigKey].ToString()?.Trim().ToLowerInvariant() ?? "";
        return raw switch
        {
            ValueContext => DebugArenaDeckSourceMode.ContextThenFileThenFallback,
            ValueOverride => DebugArenaDeckSourceMode.OverrideThenContextThenFileThenFallback,
            _ => DebugArenaDeckSourceMode.FileBacked,
        };
    }
}
