using System;
using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat.Slots;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;

namespace Fateforged.Simulation.Combat;

/// <summary>
/// Commit-slot combat lifecycle orchestrator.
/// </summary>
public static class SimCombatStateMachine
{
    private const float ProgressEpsilon = 0.01f;
    private const float ProgressRecoveryRate = 2.0f;
    private const float SummonerSlotRebindNoProgressThresholdSeconds = 0.35f;
    private const float SummonerSlotRebindBlockedThresholdSeconds = 0.22f;

    public static SimBehavior.BehaviorResult Tick(
        UnitData unit,
        MatchState state,
        float delta,
        List<SimEvent> events
    )
    {
        TickDroppedTargetCooldown(unit, delta);

        if (
            !EnsureCommittedTarget(
                unit,
                state,
                delta,
                allowSummonerAggroPreempt: unit.Action.AttackPhase == AttackPhase.None
            )
        )
        {
            SimMeleeSlotManager.ReleaseUnitSlots(unit, state);
            SimAttackLoop.Cancel(unit, state);
            unit.BehaviorState = BehaviorState.NoTarget;
            return new SimBehavior.BehaviorResult { Movement = MovementResult.Forward };
        }

        int targetId = unit.Engagement.TargetUnitId!.Value;
        if (unit.UnitType == UnitType.Melee)
        {
            if (UsesMeleeSlots(unit))
            {
                bool isSummonerTarget = MatchState.IsSummonerTarget(targetId);
                bool targetAttackableNow = SimTargeting.IsTargetAttackableNow(unit, targetId, state);
                bool summonerAttackableNow = isSummonerTarget && targetAttackableNow;
                bool hasReservation = HasReservedSlotForTarget(unit, targetId);
                bool allowSlotlessAttack =
                    targetAttackableNow
                    && (unit.Action.AttackPhase != AttackPhase.None || unit.AttackCooldown <= 0f);

                if (isSummonerTarget && summonerAttackableNow && !hasReservation)
                {
                    // Keep stand-ring as the primary behavior, but don't force an
                    // unreachable wait state when all summoner slots are occupied.
                    if (SimMeleeSlotManager.TryReserveSlot(unit, state, targetId, out _))
                        hasReservation = true;
                    else
                        allowSlotlessAttack = true;
                }
                else
                {
                    if (
                        !EnsureMeleeSlot(
                            unit,
                            state,
                            targetId,
                            delta,
                            applySlotWaitRetargetTimeout: isSummonerTarget
                        )
                    )
                    {
                        // Slot reservation is helpful for melee approach, but if the
                        // target is already attackable and the unit can start/finish an
                        // attack now, don't hard-block on slot ownership.
                        if (!allowSlotlessAttack)
                        {
                            unit.BehaviorState = BehaviorState.Chasing;
                            SimAttackLoop.Cancel(unit, state);
                            return new SimBehavior.BehaviorResult
                            {
                                Movement = MovementResult.TowardTarget,
                                MoveTargetId = unit.Engagement.TargetUnitId,
                            };
                        }
                    }

                    hasReservation = HasReservedSlotForTarget(unit, targetId);
                }

                if (!allowSlotlessAttack)
                {
                    if (!hasReservation)
                    {
                        unit.BehaviorState = BehaviorState.Chasing;
                        SimAttackLoop.Cancel(unit, state);
                        return new SimBehavior.BehaviorResult
                        {
                            Movement = MovementResult.TowardTarget,
                            MoveTargetId = unit.Engagement.TargetUnitId,
                        };
                    }

                    if (!TryAdvanceToReservedSlot(unit, state, out var toSlotBehavior))
                    {
                        SimAttackLoop.Cancel(unit, state);
                        return toSlotBehavior;
                    }

                    SimMeleeSlotManager.SetOccupied(unit, state);
                }

                ResetProgressTracking(unit);
            }
            else
            {
                SimMeleeSlotManager.ReleaseUnitSlots(unit, state);
                if (SimTargeting.IsTargetAttackableNow(unit, targetId, state))
                    ResetProgressTracking(unit);
            }
        }
        else
        {
            SimMeleeSlotManager.ReleaseUnitSlots(unit, state);
        }

        if (unit.Action.AttackPhase != AttackPhase.None)
        {
            SimAttackLoop.Tick(unit, state, delta, events);
            return new SimBehavior.BehaviorResult { Movement = MovementResult.None };
        }

        float preAttackCooldown = unit.AttackCooldown;
        var behavior = SimBehavior.TickBehavior(unit, state, delta, events);

        bool beganAttackThisTick = DidAttackThisTick(unit, preAttackCooldown);
        if (beganAttackThisTick)
            SimAttackLoop.Begin(unit, state, unit.Engagement.TargetUnitId);

        // Preserve full authored windup duration by not consuming delta on the same
        // frame attack windup starts.
        if (!beganAttackThisTick)
            SimAttackLoop.Tick(unit, state, delta, events);

        if (unit.Action.AttackPhase != AttackPhase.None)
            return new SimBehavior.BehaviorResult { Movement = MovementResult.None };

        return behavior;
    }

    private static bool EnsureCommittedTarget(
        UnitData unit,
        MatchState state,
        float delta,
        bool allowSummonerAggroPreempt
    )
    {
        if (TryApplyForcedTarget(unit, state))
            return true;

        if (ShouldReleaseExpiredForcedCommit(unit))
            DropCurrentTarget(unit, state, RetargetReason.Invalid);

        int? locked = unit.Engagement.LockedTargetUnitId;
        if (locked.HasValue && IsTargetValid(locked.Value, state))
        {
            if (
                allowSummonerAggroPreempt
                && TryGetAggroPreemptTarget(unit, state, locked.Value, out int preemptTargetId)
            )
            {
                SetLockedTarget(unit, state, preemptTargetId);
                unit.Engagement.LastRetargetReason = RetargetReason.AggroPreempt;
                return true;
            }

            unit.Engagement.TargetUnitId = locked;
            if (IsOutsideAggroChaseRadius(unit, locked.Value, state))
            {
                DropCurrentTarget(unit, state, RetargetReason.OutOfAggroRange);
            }
            else if (IsUnreachable(unit, locked.Value, state, delta))
            {
                DropCurrentTarget(unit, state, RetargetReason.UnreachableTimeout);
                state.CombatBlockedTimeoutRetargetCount++;
            }
            else
            {
                return true;
            }
        }
        else if (locked.HasValue)
        {
            DropCurrentTarget(unit, state, RetargetReason.Invalid);
        }

        int? prev = locked;
        int? acquired = SimTargeting.AcquireTargetCommit(
            unit,
            state,
            currentTargetId: prev,
            droppedTargetId: unit.Engagement.DroppedTargetUnitId,
            droppedTargetCooldownTimer: unit.Engagement.DroppedTargetCooldownTimer
        );

        SetLockedTarget(unit, state, acquired);
        return acquired.HasValue;
    }

    private static bool TryGetAggroPreemptTarget(
        UnitData unit,
        MatchState state,
        int currentLockedTargetId,
        out int preemptTargetId
    )
    {
        preemptTargetId = default;
        if (!MatchState.IsSummonerTarget(currentLockedTargetId))
            return false;

        int? candidate = SimTargeting.AcquireTargetCommit(
            unit,
            state,
            currentTargetId: currentLockedTargetId,
            droppedTargetId: unit.Engagement.DroppedTargetUnitId,
            droppedTargetCooldownTimer: unit.Engagement.DroppedTargetCooldownTimer
        );
        if (!candidate.HasValue || MatchState.IsSummonerTarget(candidate.Value))
            return false;

        preemptTargetId = candidate.Value;
        return true;
    }

    private static bool TryApplyForcedTarget(UnitData unit, MatchState state)
    {
        if (!unit.Engagement.ForcedTargetUnitId.HasValue)
            return false;

        if (unit.Engagement.ForcedTargetTimer <= 0f)
        {
            unit.Engagement.ForcedTargetUnitId = null;
            unit.Engagement.ForcedTargetTimer = 0f;
            return false;
        }

        int forced = unit.Engagement.ForcedTargetUnitId.Value;
        if (!IsTargetValid(forced, state))
        {
            unit.Engagement.ForcedTargetUnitId = null;
            unit.Engagement.ForcedTargetTimer = 0f;
            return false;
        }

        if (unit.Engagement.LockedTargetUnitId.HasValue && unit.Engagement.LockedTargetUnitId.Value != forced)
            state.CombatTargetSwitchCount++;

        if (unit.Engagement.SlotTargetId.HasValue && unit.Engagement.SlotTargetId.Value != forced)
            SimMeleeSlotManager.ReleaseUnitSlots(unit, state);

        unit.Engagement.TargetUnitId = forced;
        unit.Engagement.LockedTargetUnitId = forced;
        unit.Engagement.LastRetargetReason = RetargetReason.ForcedOverride;
        unit.Engagement.UnreachableTimer = 0f;
        unit.Engagement.NoProgressTimer = 0f;
        unit.Engagement.LastTargetDistance = -1f;
        unit.Engagement.LastSlotDistance = -1f;
        return true;
    }

    private static bool ShouldReleaseExpiredForcedCommit(UnitData unit)
    {
        return !unit.Engagement.ForcedTargetUnitId.HasValue
            && unit.Engagement.ForcedTargetTimer <= 0f
            && unit.Engagement.LockedTargetUnitId.HasValue
            && unit.Engagement.LastRetargetReason == RetargetReason.ForcedOverride;
    }

    private static bool EnsureMeleeSlot(
        UnitData unit,
        MatchState state,
        int targetId,
        float delta,
        bool applySlotWaitRetargetTimeout
    )
    {
        if (unit.Engagement.SlotTargetId.HasValue && unit.Engagement.SlotTargetId.Value != targetId)
            SimMeleeSlotManager.ReleaseUnitSlots(unit, state);

        bool hasReservation =
            unit.Engagement.SlotTargetId.HasValue
            && unit.Engagement.SlotTargetId.Value == targetId
            && unit.Engagement.ReservedSlotId.HasValue;
        if (hasReservation)
        {
            if (ShouldRebindSummonerSlot(unit, state, targetId))
            {
                int previousSlotId = unit.Engagement.ReservedSlotId!.Value;
                SimMeleeSlotManager.ReleaseUnitSlots(unit, state);

                bool reboundToNewSlot = SimMeleeSlotManager.TryReserveSlot(
                    unit,
                    state,
                    targetId,
                    out _,
                    excludedSlotId: previousSlotId
                );
                if (!reboundToNewSlot)
                {
                    reboundToNewSlot = SimMeleeSlotManager.TryReserveSlot(
                        unit,
                        state,
                        targetId,
                        out _
                    );
                }

                if (reboundToNewSlot)
                {
                    unit.Engagement.SlotWaitTimer = 0f;
                    ResetProgressTracking(unit);
                    return true;
                }
            }

            unit.Engagement.SlotWaitTimer = MathF.Max(0f, unit.Engagement.SlotWaitTimer - (delta * ProgressRecoveryRate));
            return true;
        }

        if (SimMeleeSlotManager.TryReserveSlot(unit, state, targetId, out _))
        {
            unit.Engagement.SlotWaitTimer = 0f;
            return true;
        }

        unit.Engagement.SlotWaitTimer += delta;
        if (applySlotWaitRetargetTimeout && unit.Engagement.SlotWaitTimer >= unit.Engagement.SlotWaitTimeoutSeconds)
        {
            DropCurrentTarget(unit, state, RetargetReason.UnreachableTimeout);
            state.CombatBlockedTimeoutRetargetCount++;
        }

        return false;
    }

    private static bool HasReservedSlotForTarget(UnitData unit, int targetId)
    {
        return unit.Engagement.SlotTargetId.HasValue
            && unit.Engagement.SlotTargetId.Value == targetId
            && unit.Engagement.ReservedSlotId.HasValue;
    }

    private static bool TryAdvanceToReservedSlot(
        UnitData unit,
        MatchState state,
        out SimBehavior.BehaviorResult behavior
    )
    {
        behavior = new SimBehavior.BehaviorResult { Movement = MovementResult.None };

        var slotPos = SimMeleeSlotManager.GetReservedSlotWorldPosition(unit, state);
        if (!slotPos.HasValue)
        {
            behavior = new SimBehavior.BehaviorResult { Movement = MovementResult.None };
            return false;
        }

        float distance = DistanceXZ(unit.Position, slotPos.Value);
        unit.Engagement.LastSlotDistance = distance;

        if (distance <= SimMeleeSlotManager.ResolveSlotArrivalDistance(unit))
            return true;

        behavior = new SimBehavior.BehaviorResult
        {
            Movement = MovementResult.TowardTarget,
            MoveTargetId = unit.Engagement.TargetUnitId,
        };
        return false;
    }

    private static bool IsUnreachable(UnitData unit, int targetId, MatchState state, float delta)
    {
        if (SimTargeting.IsTargetAttackableNow(unit, targetId, state))
        {
            ResetProgressTracking(unit);
            return false;
        }

        SimVector3? destination = null;
        if (unit.UnitType == UnitType.Melee)
            destination = SimMeleeSlotManager.GetReservedSlotWorldPosition(unit, state);
        destination ??= SimUtils.ResolveTargetPosition(targetId, state);

        if (!destination.HasValue)
            return true;

        float distance = DistanceXZ(unit.Position, destination.Value);
        float previous = unit.Engagement.LastTargetDistance;
        unit.Engagement.LastTargetDistance = distance;

        bool madeProgress = previous < 0f || (previous - distance) > ProgressEpsilon;
        if (madeProgress)
        {
            unit.Engagement.NoProgressTimer = MathF.Max(
                0f,
                unit.Engagement.NoProgressTimer - (delta * ProgressRecoveryRate)
            );
            unit.Engagement.UnreachableTimer = MathF.Max(
                0f,
                unit.Engagement.UnreachableTimer - (delta * ProgressRecoveryRate)
            );
        }
        else
        {
            unit.Engagement.NoProgressTimer += delta;
            unit.Engagement.UnreachableTimer += delta;
        }

        return unit.Engagement.NoProgressTimer >= unit.Engagement.UnreachableTimeoutSeconds
            || unit.Engagement.UnreachableTimer >= unit.Engagement.UnreachableTimeoutSeconds;
    }

    private static bool ShouldRebindSummonerSlot(UnitData unit, MatchState state, int targetId)
    {
        if (!MatchState.IsSummonerTarget(targetId))
            return false;
        if (!unit.Engagement.ReservedSlotId.HasValue || unit.Engagement.OccupiedSlotId.HasValue)
            return false;
        if (SimTargeting.IsTargetAttackableNow(unit, targetId, state))
            return false;

        bool blockedByCrowd = unit.NavigationBlockedTime >= SummonerSlotRebindBlockedThresholdSeconds
            || unit.NavigationYieldTimer > 0f
            || unit.NavigationEscapeTimer > 0f;
        bool noProgress = unit.Engagement.NoProgressTimer >= SummonerSlotRebindNoProgressThresholdSeconds;
        return blockedByCrowd || noProgress;
    }

    private static bool IsOutsideAggroChaseRadius(UnitData unit, int targetId, MatchState state)
    {
        if (MatchState.IsSummonerTarget(targetId))
            return false;

        var target = state.GetAliveUnit(targetId);
        if (target == null)
            return true;

        float maxChaseDistance = MathF.Max(unit.AggroRadius, unit.AttackRange);
        if (maxChaseDistance <= 0f)
            return true;

        return DistanceXZ(unit.Position, target.Position) > maxChaseDistance;
    }

    private static bool IsTargetValid(int targetId, MatchState state)
    {
        if (MatchState.IsSummonerTarget(targetId))
        {
            int team = MatchState.GetSummonerTeamFromTargetId(targetId);
            return team >= 0 && team < state.Summoners.Length && state.Summoners[team].IsAlive;
        }

        return state.GetAliveUnit(targetId) != null;
    }

    private static void SetLockedTarget(UnitData unit, MatchState state, int? targetId)
    {
        int? previous = unit.Engagement.LockedTargetUnitId;
        if (previous.HasValue && targetId.HasValue && previous.Value != targetId.Value)
            state.CombatTargetSwitchCount++;

        if (previous.HasValue && targetId.HasValue && previous.Value != targetId.Value)
            SimMeleeSlotManager.ReleaseUnitSlots(unit, state);

        unit.Engagement.LockedTargetUnitId = targetId;
        unit.Engagement.TargetUnitId = targetId;
        unit.Engagement.LastRetargetReason = RetargetReason.None;
        unit.Engagement.UnreachableTimer = 0f;
        unit.Engagement.NoProgressTimer = 0f;
        unit.Engagement.LastTargetDistance = -1f;
        unit.Engagement.SlotWaitTimer = 0f;

        if (!targetId.HasValue)
            SimMeleeSlotManager.ReleaseUnitSlots(unit, state);
    }

    private static void DropCurrentTarget(UnitData unit, MatchState state, RetargetReason reason)
    {
        if (unit.Engagement.LockedTargetUnitId.HasValue)
        {
            unit.Engagement.DroppedTargetUnitId = unit.Engagement.LockedTargetUnitId;
            unit.Engagement.DroppedTargetCooldownTimer = unit.Engagement.DroppedTargetCooldownSeconds;
        }

        unit.Engagement.LastRetargetReason = reason;
        unit.Engagement.LockedTargetUnitId = null;
        unit.Engagement.TargetUnitId = null;
        unit.Engagement.UnreachableTimer = 0f;
        unit.Engagement.NoProgressTimer = 0f;
        unit.Engagement.LastTargetDistance = -1f;
        unit.Engagement.LastSlotDistance = -1f;
        unit.Engagement.SlotWaitTimer = 0f;

        SimMeleeSlotManager.ReleaseUnitSlots(unit, state);
        SimAttackLoop.Cancel(unit, state);
    }

    private static void TickDroppedTargetCooldown(UnitData unit, float delta)
    {
        if (unit.Engagement.DroppedTargetCooldownTimer <= 0f)
            return;

        unit.Engagement.DroppedTargetCooldownTimer = MathF.Max(0f, unit.Engagement.DroppedTargetCooldownTimer - delta);
        if (unit.Engagement.DroppedTargetCooldownTimer <= 0f)
            unit.Engagement.DroppedTargetUnitId = null;
    }

    private static bool DidAttackThisTick(UnitData unit, float preAttackCooldown)
    {
        return unit.BehaviorState == BehaviorState.Attacking
            && preAttackCooldown <= 0f
            && unit.AttackCooldown > 0f;
    }

    private static bool UsesMeleeSlots(UnitData unit)
    {
        return unit.Attack.Rules.MeleeEngagementModel == MeleeEngagementModel.SlotRing;
    }

    private static void ResetProgressTracking(UnitData unit)
    {
        unit.Engagement.NoProgressTimer = 0f;
        unit.Engagement.UnreachableTimer = 0f;
        unit.Engagement.LastTargetDistance = -1f;
        unit.Engagement.LastSlotDistance = -1f;
    }

    private static float DistanceXZ(SimVector3 a, SimVector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return MathF.Sqrt((dx * dx) + (dz * dz));
    }
}
