namespace Fateforged.Simulation.Enums;

/// <summary>
/// Policy used by simulation targeting when selecting and retaining targets.
/// </summary>
public enum TargetPolicyId
{
    /// <summary>
    /// Prefer currently attackable targets first, then fallback to baseline score.
    /// </summary>
    PreferAttackable = 0,

    /// <summary>
    /// Prefer attackable targets and keep current target while still attackable.
    /// </summary>
    PreferAttackableAndStick = 1
}
