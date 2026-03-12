namespace Fateforged.Simulation.Enums;

/// <summary>
/// Fallback movement behavior when a unit is in range but cannot attack
/// (e.g., cone constraint not satisfied).
/// </summary>
public enum FallbackMovement
{
    MoveToward = 0,
    Strafe = 1,
    Idle = 2,
}
