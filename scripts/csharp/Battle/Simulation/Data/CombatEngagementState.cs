using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Data;

/// <summary>
/// Runtime engagement state for target lock, retargeting, and slot approach.
/// Grouped under UnitData to avoid scattering commit/target fields at root level.
/// </summary>
public sealed class CombatEngagementState
{
    // Targeting lock state
    public int? TargetUnitId { get; set; }
    public float TargetLockTimer { get; set; }

    // Commit lifecycle lock/retarget state
    public CombatLifecycleState LifecycleState { get; set; } = CombatLifecycleState.AcquireTarget;
    public int? LockedTargetUnitId { get; set; }
    public RetargetReason LastRetargetReason { get; set; } = RetargetReason.None;
    public float UnreachableTimer { get; set; }
    public float UnreachableTimeoutSeconds { get; set; } = 1.2f;

    // Slot approach state
    public int? SlotTargetId { get; set; }
    public int? ReservedSlotId { get; set; }
    public int? OccupiedSlotId { get; set; }
    public float SlotWaitTimer { get; set; }
    public float SlotWaitTimeoutSeconds { get; set; } = 0.7f;
    public float LastSlotDistance { get; set; } = -1f;
    public float LastTargetDistance { get; set; } = -1f;
    public float NoProgressTimer { get; set; }
    public float LastReservationDistanceSq { get; set; } = float.MaxValue;

    // Retarget cooldown after forced drops
    public int? DroppedTargetUnitId { get; set; }
    public float DroppedTargetCooldownTimer { get; set; }
    public float DroppedTargetCooldownSeconds { get; set; } = 0.75f;

    // Forced target override (e.g. taunt/redirect)
    public int? ForcedTargetUnitId { get; set; }
    public float ForcedTargetTimer { get; set; }
}
