using Godot;

namespace Fateforged.View;

/// <summary>
/// Small view-layer abstraction for runtime VFX playback.
/// EntityManager consumes this interface so it does not resolve scene-tree globals itself.
/// </summary>
public interface IBattleVfxService
{
    void PlayEffect(string effectId, Vector3 position, Godot.Collections.Dictionary? data = null);
}

public sealed class NullBattleVfxService : IBattleVfxService
{
    public static NullBattleVfxService Instance { get; } = new();

    private NullBattleVfxService() { }

    public void PlayEffect(string effectId, Vector3 position, Godot.Collections.Dictionary? data = null)
    {
        // Intentionally no-op when VFX service is unavailable.
    }
}

public sealed class GodotVfxManagerService : IBattleVfxService
{
    private readonly Node _vfxManager;

    public GodotVfxManagerService(Node vfxManager)
    {
        _vfxManager = vfxManager;
    }

    public void PlayEffect(string effectId, Vector3 position, Godot.Collections.Dictionary? data = null)
    {
        if (string.IsNullOrEmpty(effectId) || !_vfxManager.HasMethod("play_effect"))
            return;

        _vfxManager.Call("play_effect", effectId, position, data ?? new Godot.Collections.Dictionary());
    }
}
