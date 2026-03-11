namespace Fateforged.Simulation.Enums;

/// <summary>
/// Explicit reasons a unit is allowed to drop a committed target.
/// </summary>
public enum RetargetReason
{
    None = 0,
    Invalid = 1,
    ForcedOverride = 2,
    UnreachableTimeout = 3,
}
