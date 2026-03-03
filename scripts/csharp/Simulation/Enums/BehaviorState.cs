namespace Fateforged.Simulation.Enums;

/// <summary>
/// Discrete behavior states for unit AI state machine (used by SimBehavior).
/// Replaces raw int constants for type safety.
/// </summary>
public enum BehaviorState
{
    NoTarget = 0,
    Chasing = 1,
    InRange = 2,
    Attacking = 3,
}

/// <summary>
/// Movement intent produced by SimBehavior each tick.
/// Tells SimSteering what kind of movement to perform.
/// </summary>
public enum MovementState
{
    None = 0,
    Forward = 1,
}
