using Godot;

namespace Fateforged.View.Debug.DeckSources;

public readonly record struct DebugArenaDeckResolution(
    Godot.Collections.Array PlayerDeck,
    Godot.Collections.Array EnemyDeck,
    string SourceTag
);
