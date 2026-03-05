using Godot;
using Fateforged.Constants;

namespace Fateforged.Debug;

/// <summary>
/// Debug-only bridge for battlefield tuning toggles.
/// Exposes shared C# boundary flags to GDScript debug UI via autoload.
/// </summary>
[GlobalClass]
public partial class BattlefieldDebugService : Node
{
    public static BattlefieldDebugService? Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsSpawnBoundaryBypassEnabled()
    {
        return BattlefieldBounds.IsDebugBypassSpawnBoundaryEnabled();
    }

    public void SetSpawnBoundaryBypassEnabled(bool enabled)
    {
        BattlefieldBounds.SetDebugBypassSpawnBoundary(enabled);
    }

    public bool ToggleSpawnBoundaryBypass()
    {
        BattlefieldBounds.ToggleDebugBypassSpawnBoundary();
        return BattlefieldBounds.IsDebugBypassSpawnBoundaryEnabled();
    }
}
