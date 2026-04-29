namespace Fateforged.Simulation.Enums;

/// <summary>
/// Combat target commit lifecycle state.
/// </summary>
public enum CombatLifecycleState
{
    Idle = 0,
    AcquireTarget = 1,
    ApproachTarget = 2,
    AttackLoop = 3,
    Reacquire = 4,
}
