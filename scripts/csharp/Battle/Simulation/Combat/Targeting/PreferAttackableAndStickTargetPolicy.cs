using Fateforged.Simulation.Data;

namespace Fateforged.Simulation.Combat.Targeting;

/// <summary>
/// Prefer attackable-now candidates and hold current target while it remains attackable.
/// </summary>
public sealed class PreferAttackableAndStickTargetPolicy : ITargetPolicy
{
    public bool ShouldKeepCurrentTarget(UnitData unit, MatchState state, int? currentTargetId) =>
        currentTargetId.HasValue
        && SimTargeting.IsTargetAttackableNow(unit, currentTargetId.Value, state);

    public int? SelectTarget(UnitData unit, MatchState state) =>
        SimTargeting.AcquireTargetPreferAttackable(unit, state);
}
