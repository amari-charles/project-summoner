namespace Fateforged.Data.Events;

/// <summary>
/// Application runtime surface used to present a battle.
/// </summary>
public enum BattleRuntimeSurface
{
    /// <summary>Production authored-battle surface.</summary>
    Standard,

    /// <summary>Developer arena surface with battle debugging tools.</summary>
    DebugArena,
}

/// <summary>
/// Stable bridge identifiers for battle runtime surfaces.
/// </summary>
public static class BattleRuntimeSurfaceExtensions
{
    public static string ToStringId(this BattleRuntimeSurface surface) =>
        surface switch
        {
            BattleRuntimeSurface.Standard => "standard",
            BattleRuntimeSurface.DebugArena => "debug_arena",
            _ => "standard",
        };
}
