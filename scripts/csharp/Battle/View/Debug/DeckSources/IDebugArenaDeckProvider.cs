namespace Fateforged.View.Debug.DeckSources;

public interface IDebugArenaDeckProvider
{
    DebugArenaDeckResolution Resolve(DebugArenaDeckResolveRequest request);
}
