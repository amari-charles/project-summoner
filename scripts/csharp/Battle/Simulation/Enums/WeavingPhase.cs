namespace Fateforged.Simulation.Enums;

/// <summary>
/// Phase of weaving-homing projectile movement.
/// Phase progression: Straight → VeeringOut → VeeringBack → Homing.
/// </summary>
public enum WeavingPhase
{
    Straight = 0,
    VeeringOut = 1,
    VeeringBack = 2,
    Homing = 3,
}
