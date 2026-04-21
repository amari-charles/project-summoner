namespace Fateforged.View.Debug.DeckSources;

/// <summary>
/// Selects which source pipeline resolves Debug Arena deck entries.
/// PASS 3 default prioritizes context decks, then file fallback.
/// </summary>
public enum DebugArenaDeckSourceMode
{
    FileBacked = 0,
    ContextThenFileThenFallback = 1,
    OverrideThenContextThenFileThenFallback = 2,
}
