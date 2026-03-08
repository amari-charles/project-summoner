using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Behavior-owned movement intention for one simulation tick.
/// ORCA and overlap correction can modify velocity realization,
/// but this struct captures what the unit intended to do first.
/// </summary>
public readonly struct MovementIntent
{
    public MovementResult Mode { get; init; }
    public int? TargetId { get; init; }
    public SimVector3 DesiredVelocity { get; init; }
    public SimVector3 DesiredFacingDirection { get; init; }

    public static MovementIntent None(MovementResult mode, int? targetId) => new()
    {
        Mode = mode,
        TargetId = targetId,
        DesiredVelocity = SimVector3.Zero,
        DesiredFacingDirection = SimVector3.Zero
    };
}
