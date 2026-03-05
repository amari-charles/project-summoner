namespace Fateforged.Simulation.Enums;

/// <summary>
/// Policy used by simulation targeting when selecting and retaining targets.
/// </summary>
public enum TargetPolicyId
{
    /// <summary>
    /// Legacy behavior: select by baseline score only and never sticky-hold current target.
    /// </summary>
    Legacy = 0,

    /// <summary>
    /// Prefer currently attackable targets first, then fallback to baseline score.
    /// </summary>
    PreferAttackable = 1,

    /// <summary>
    /// Prefer attackable targets and keep current target while still attackable.
    /// </summary>
    PreferAttackableAndStick = 2
}
