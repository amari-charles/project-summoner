namespace Fateforged.Simulation.Enums;

/// <summary>
/// Attack loop phases used by commit-slot combat.
/// </summary>
public enum AttackPhase
{
    None = 0,
    Windup = 1,
    Active = 2,
    Recovery = 3,
}
