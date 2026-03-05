using Fateforged.Simulation.Data;

namespace Fateforged.Simulation.Combat.Targeting;

/// <summary>
/// Target selection and retention policy for simulation units.
/// </summary>
public interface ITargetPolicy
{
    /// <summary>
    /// Decide whether to keep the current target when lock has expired.
    /// </summary>
    bool ShouldKeepCurrentTarget(UnitData unit, MatchState state, int? currentTargetId);

    /// <summary>
    /// Select a target ID for this unit.
    /// </summary>
    int? SelectTarget(UnitData unit, MatchState state);
}
