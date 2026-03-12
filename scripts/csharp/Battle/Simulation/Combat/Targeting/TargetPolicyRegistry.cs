using System;
using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Combat.Targeting;

/// <summary>
/// Resolves target policy IDs to singleton strategy instances.
/// Unknown IDs fail fast to surface bad config during development/tests.
/// </summary>
public static class TargetPolicyRegistry
{
    private static readonly ITargetPolicy PreferAttackable = new PreferAttackableTargetPolicy();
    private static readonly ITargetPolicy PreferAttackableAndStick =
        new PreferAttackableAndStickTargetPolicy();

    public static ITargetPolicy Resolve(TargetPolicyId id) =>
        id switch
        {
            TargetPolicyId.PreferAttackable => PreferAttackable,
            TargetPolicyId.PreferAttackableAndStick => PreferAttackableAndStick,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown TargetPolicyId"),
        };
}
