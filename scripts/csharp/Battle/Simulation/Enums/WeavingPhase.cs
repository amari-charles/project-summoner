namespace Fateforged.Simulation.Enums;

/// <summary>
/// Phase of weaving-homing projectile movement.
/// Phase progression: Straight → Veering → Homing.
/// </summary>
public enum WeavingPhase
{
    Straight = 0,
    Veering = 1,
    Homing = 2
}
