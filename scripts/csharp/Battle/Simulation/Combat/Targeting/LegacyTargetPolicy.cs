using Fateforged.Simulation.Data;

namespace Fateforged.Simulation.Combat.Targeting;

/// <summary>
/// Baseline policy: score-based acquisition only, no sticky hold.
/// </summary>
public sealed class LegacyTargetPolicy : ITargetPolicy
{
    public bool ShouldKeepCurrentTarget(UnitData unit, MatchState state, int? currentTargetId) => false;

    public int? SelectTarget(UnitData unit, MatchState state)
        => SimTargeting.AcquireTargetLegacy(unit, state);
}
