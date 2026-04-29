using Godot;

namespace Fateforged.View.Debug.DeckSources;

public sealed class DebugArenaDeckResolveRequest
{
    public DebugArenaDeckSourceMode SourceMode { get; init; } = DebugArenaDeckSourceMode.FileBacked;

    public Godot.Collections.Dictionary ContextConfig { get; init; } =
        new Godot.Collections.Dictionary();

    public Godot.Collections.Dictionary OverrideConfig { get; init; } =
        new Godot.Collections.Dictionary();
}
