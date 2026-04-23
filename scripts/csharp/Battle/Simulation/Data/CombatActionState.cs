using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Data;

/// <summary>
/// Runtime attack/action state for phase progression and pending damage payloads.
/// Grouped under UnitData to avoid scattering combat execution fields at root level.
/// </summary>
public sealed class CombatActionState
{
    // Attack animation timer for presentation sync.
    public float AttackAnimationTimer { get; set; }

    // Attack loop phase state.
    public AttackPhase AttackPhase { get; set; } = AttackPhase.None;
    public float AttackPhaseTimer { get; set; }
    public int? AttackPhaseLockTargetId { get; set; }

    // Pending basic attack payload queued at attack start and resolved once at
    // windup->active commit.
    public int? PendingAttackTargetId { get; set; }
    public float PendingAttackBaseDamage { get; set; }
    public bool PendingAttackTargetsSummoner { get; set; }

    // Delayed ranged/melee resolution payload.
    public float PendingDamageTimer { get; set; }
    public int? PendingDamageTargetId { get; set; }
    public float PendingDamageAmount { get; set; }
}
