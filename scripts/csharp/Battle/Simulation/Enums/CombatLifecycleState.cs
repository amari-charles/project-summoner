namespace Fateforged.Simulation.Enums;

/// <summary>
/// Commit-slot combat lifecycle state.
/// </summary>
public enum CombatLifecycleState
{
    Idle = 0,
    AcquireTarget = 1,
    ReserveSlot = 2,
    MoveToSlot = 3,
    AttackLoop = 4,
    Reacquire = 5,
}
