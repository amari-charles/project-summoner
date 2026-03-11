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

    public static SimBehavior.BehaviorResult Tick(
        UnitData unit,
        MatchState state,
        float delta,
        List<SimEvent> events)
    {
        TickDroppedTargetCooldown(unit, delta);

        if (!EnsureCommittedTarget(unit, state, delta))
        {
            SimMeleeSlotManager.ReleaseUnitSlots(unit, state);
            SimAttackLoop.Cancel(unit, state);
            unit.BehaviorState = BehaviorState.NoTarget;
            return new SimBehavior.BehaviorResult { Movement = MovementResult.Forward };
        }

        int targetId = unit.TargetUnitId!.Value;
        if (unit.UnitType == UnitType.Melee)
        {
            if (!EnsureMeleeSlot(unit, state, targetId, delta))
            {
                unit.BehaviorState = BehaviorState.Chasing;
                SimAttackLoop.Cancel(unit, state);
                return new SimBehavior.BehaviorResult
                {
                    Movement = MovementResult.TowardTarget,
                    MoveTargetId = unit.TargetUnitId
                };
            }

            if (!TryAdvanceToReservedSlot(unit, state, out var toSlotBehavior))
            {
                SimAttackLoop.Cancel(unit, state);
                return toSlotBehavior;
            }

            SimMeleeSlotManager.SetOccupied(unit, state);
            ResetProgressTracking(unit);
        }
        else
        {
            SimMeleeSlotManager.ReleaseUnitSlots(unit, state);
        }

        if (unit.AttackPhase != AttackPhase.None)
        {
            SimAttackLoop.Tick(unit, state, delta, events);
            return new SimBehavior.BehaviorResult { Movement = MovementResult.None };
        }

        float preAttackCooldown = unit.AttackCooldown;
        var behavior = SimBehavior.TickBehavior(unit, state, delta, events);

        if (DidAttackThisTick(unit, preAttackCooldown))
            SimAttackLoop.Begin(unit, state, unit.TargetUnitId);

        SimAttackLoop.Tick(unit, state, delta, events);

        if (unit.AttackPhase != AttackPhase.None)
            return new SimBehavior.BehaviorResult { Movement = MovementResult.None };

        return behavior;
    }

    private static bool EnsureCommittedTarget(UnitData unit, MatchState state, float delta)
    {
        if (TryApplyForcedTarget(unit, state))
            return true;

        int? locked = unit.LockedTargetUnitId;
        if (locked.HasValue && IsTargetValid(locked.Value, state))
        {
            unit.TargetUnitId = locked;
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
            droppedTargetId: unit.DroppedTargetUnitId,
            droppedTargetCooldownTimer: unit.DroppedTargetCooldownTimer);

        SetLockedTarget(unit, state, acquired);
        return acquired.HasValue;
    }

    private static bool TryApplyForcedTarget(UnitData unit, MatchState state)
    {
        if (!unit.ForcedTargetUnitId.HasValue)
            return false;

        int forced = unit.ForcedTargetUnitId.Value;
        if (!IsTargetValid(forced, state))
        {
            unit.ForcedTargetUnitId = null;
            unit.ForcedTargetTimer = 0f;
            return false;
        }

        if (unit.LockedTargetUnitId.HasValue && unit.LockedTargetUnitId.Value != forced)
            state.CombatTargetSwitchCount++;

        if (unit.SlotTargetId.HasValue && unit.SlotTargetId.Value != forced)
            SimMeleeSlotManager.ReleaseUnitSlots(unit, state);

        unit.TargetUnitId = forced;
        unit.LockedTargetUnitId = forced;
        unit.LastRetargetReason = RetargetReason.ForcedOverride;
        unit.UnreachableTimer = 0f;
        unit.NoProgressTimer = 0f;
        unit.LastTargetDistance = -1f;
        unit.LastSlotDistance = -1f;
        return true;
    }

    private static bool EnsureMeleeSlot(UnitData unit, MatchState state, int targetId, float delta)
    {
        if (unit.SlotTargetId.HasValue && unit.SlotTargetId.Value != targetId)
            SimMeleeSlotManager.ReleaseUnitSlots(unit, state);

        bool hasReservation = unit.SlotTargetId.HasValue &&
                              unit.SlotTargetId.Value == targetId &&
                              unit.ReservedSlotId.HasValue;
        if (hasReservation)
        {
            unit.SlotWaitTimer = MathF.Max(0f, unit.SlotWaitTimer - (delta * ProgressRecoveryRate));
            return true;
        }

        if (SimMeleeSlotManager.TryReserveSlot(unit, state, targetId, out _))
        {
            unit.SlotWaitTimer = 0f;
            return true;
        }

        unit.SlotWaitTimer += delta;
        if (unit.SlotWaitTimer >= unit.SlotWaitTimeoutSeconds)
        {
            DropCurrentTarget(unit, state, RetargetReason.UnreachableTimeout);
            state.CombatBlockedTimeoutRetargetCount++;
        }

        return false;
    }

    private static bool TryAdvanceToReservedSlot(
        UnitData unit,
        MatchState state,
        out SimBehavior.BehaviorResult behavior)
    {
        behavior = new SimBehavior.BehaviorResult { Movement = MovementResult.None };

        var slotPos = SimMeleeSlotManager.GetReservedSlotWorldPosition(unit, state);
        if (!slotPos.HasValue)
        {
            behavior = new SimBehavior.BehaviorResult { Movement = MovementResult.None };
            return false;
        }

        float distance = DistanceXZ(unit.Position, slotPos.Value);
        unit.LastSlotDistance = distance;

        if (distance <= SimMeleeSlotManager.ResolveSlotArrivalDistance(unit))
            return true;

        behavior = new SimBehavior.BehaviorResult
        {
            Movement = MovementResult.TowardTarget,
            MoveTargetId = unit.TargetUnitId
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
        float previous = unit.LastTargetDistance;
        unit.LastTargetDistance = distance;

        bool madeProgress = previous < 0f || (previous - distance) > ProgressEpsilon;
        if (madeProgress)
        {
            unit.NoProgressTimer = MathF.Max(0f, unit.NoProgressTimer - (delta * ProgressRecoveryRate));
            unit.UnreachableTimer = MathF.Max(0f, unit.UnreachableTimer - (delta * ProgressRecoveryRate));
        }
        else
        {
            unit.NoProgressTimer += delta;
            unit.UnreachableTimer += delta;
        }

        return unit.NoProgressTimer >= unit.UnreachableTimeoutSeconds ||
               unit.UnreachableTimer >= unit.UnreachableTimeoutSeconds;
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
        int? previous = unit.LockedTargetUnitId;
        if (previous.HasValue && targetId.HasValue && previous.Value != targetId.Value)
            state.CombatTargetSwitchCount++;

        if (previous.HasValue && targetId.HasValue && previous.Value != targetId.Value)
            SimMeleeSlotManager.ReleaseUnitSlots(unit, state);

        unit.LockedTargetUnitId = targetId;
        unit.TargetUnitId = targetId;
        unit.LastRetargetReason = RetargetReason.None;
        unit.UnreachableTimer = 0f;
        unit.NoProgressTimer = 0f;
        unit.LastTargetDistance = -1f;
        unit.SlotWaitTimer = 0f;

        if (!targetId.HasValue)
            SimMeleeSlotManager.ReleaseUnitSlots(unit, state);
    }

    private static void DropCurrentTarget(UnitData unit, MatchState state, RetargetReason reason)
    {
        if (unit.LockedTargetUnitId.HasValue)
        {
            unit.DroppedTargetUnitId = unit.LockedTargetUnitId;
            unit.DroppedTargetCooldownTimer = unit.DroppedTargetCooldownSeconds;
        }

        unit.LastRetargetReason = reason;
        unit.LockedTargetUnitId = null;
        unit.TargetUnitId = null;
        unit.UnreachableTimer = 0f;
        unit.NoProgressTimer = 0f;
        unit.LastTargetDistance = -1f;
        unit.LastSlotDistance = -1f;
        unit.SlotWaitTimer = 0f;

        SimMeleeSlotManager.ReleaseUnitSlots(unit, state);
        SimAttackLoop.Cancel(unit, state);
    }

    private static void TickDroppedTargetCooldown(UnitData unit, float delta)
    {
        if (unit.DroppedTargetCooldownTimer <= 0f)
            return;

        unit.DroppedTargetCooldownTimer = MathF.Max(0f, unit.DroppedTargetCooldownTimer - delta);
        if (unit.DroppedTargetCooldownTimer <= 0f)
            unit.DroppedTargetUnitId = null;
    }

    private static bool DidAttackThisTick(UnitData unit, float preAttackCooldown)
    {
        return unit.BehaviorState == BehaviorState.Attacking &&
               preAttackCooldown <= 0f &&
               unit.AttackCooldown > 0f;
    }

    private static void ResetProgressTracking(UnitData unit)
    {
        unit.NoProgressTimer = 0f;
        unit.UnreachableTimer = 0f;
        unit.LastTargetDistance = -1f;
        unit.LastSlotDistance = -1f;
    }

    private static float DistanceXZ(SimVector3 a, SimVector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return MathF.Sqrt((dx * dx) + (dz * dz));
    }
}
