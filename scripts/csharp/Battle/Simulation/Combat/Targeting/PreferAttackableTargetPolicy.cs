using Fateforged.Simulation.Data;

namespace Fateforged.Simulation.Combat.Targeting;

/// <summary>
/// Prefer attackable-now candidates first, then baseline score.
/// </summary>
public sealed class PreferAttackableTargetPolicy : ITargetPolicy
{
    public bool ShouldKeepCurrentTarget(UnitData unit, MatchState state, int? currentTargetId) =>
        false;

    public int? SelectTarget(UnitData unit, MatchState state) =>
        SimTargeting.AcquireTargetPreferAttackable(unit, state);
}
