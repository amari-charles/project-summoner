using Fateforged.Constants;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Units;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Movement pipeline: Intent Generation → ORCA → Overlap Correction.
/// Intent generation computes desired velocity/facing from behavior.
/// ORCA constrains velocity to avoid collisions via half-plane LP solver.
/// Overlap correction is a lightweight safety net for remaining edge cases.
/// </summary>
public static class SimMovement
{
    private const float DirectionThreshold = 0.001f;
    private const float VelocitySmoothingFactor = 0.15f;

    /// <summary>
    /// Execute a full movement tick for a unit based on its behavior result.
    /// Pipeline: IntentResolve → ORCA → Smooth → Apply → OverlapCorrection.
    /// </summary>
    public static void Tick(
        UnitData unit,
        SimBehavior.BehaviorResult behavior,
        MatchState state,
        float delta
    )
    {
        FacingController.Tick(unit, delta);

        if (unit.UnitType == UnitType.Melee)
        {
            TickCommitMeleeMovement(unit, behavior, state, delta);
            return;
        }

        if (behavior.Movement == MovementResult.None)
        {
            // Ensure blocked-navigation timers don't persist while unit is idle/attacking.
            BlockedNavigationController.ResetState(unit);
            unit.Velocity = SimVector3.Zero;
            SimOverlapResolver.Correct(unit, state);
            return;
        }

        // Layer 1: Resolve behavior intent
        var intent = MovementIntentResolver.Resolve(unit, behavior, state, delta);
        if (intent.DesiredVelocity.LengthSquared() < DirectionThreshold)
        {
            FacingController.Update(unit, intent, state);
            unit.Velocity = SimVector3.Zero;
            SimOverlapResolver.Correct(unit, state);
            return;
        }

        // Layer 2: ORCA → safe velocity
        var safeVelocity = OrcaAvoidance.ComputeSafeVelocity(unit, intent.DesiredVelocity, state);

        // Layer 4: Smooth velocity to dampen frame-to-frame jitter.
        // Low factor (15%) dampens oscillation ~70% per frame-pair while
        // preserving 85% of ORCA's deflection for flanking.
        var prevVelocity = unit.Velocity;
        if (prevVelocity.LengthSquared() > DirectionThreshold)
        {
            safeVelocity =
                safeVelocity * (1f - VelocitySmoothingFactor)
                + prevVelocity * VelocitySmoothingFactor;
        }

        // Layer 5: Apply
        var preMovementPos = unit.Position;
        var newPos = unit.Position + safeVelocity * delta;

        // Preserve altitude
        if (unit.MovementLayer == MovementLayer.Air)
            newPos.Y = unit.FlightAltitude;
        else
            newPos.Y = unit.Position.Y;

        // Clamp & assign
        unit.Position = BattlefieldBounds.ClampToBounds(newPos);

        // Facing is managed separately from collision-adjusted velocity.
        FacingController.Update(unit, intent, state);

        // Safety net — position-only correction.
        // Velocity is NOT reconciled with correction displacement to prevent
        // overlap correction noise from feeding back into ORCA next frame.
        SimOverlapResolver.Correct(unit, state);
        unit.Velocity = safeVelocity;

        // Track charge distance from actual displacement
        var totalDisp = unit.Position - preMovementPos;
        float moveDist = new SimVector3(totalDisp.X, 0, totalDisp.Z).Length();
        if (moveDist > DirectionThreshold)
            unit.DistanceTraveled += moveDist;
    }

    private static void TickCommitMeleeMovement(
        UnitData unit,
        SimBehavior.BehaviorResult behavior,
        MatchState state,
        float delta
    )
    {
        if (behavior.Movement == MovementResult.None || unit.AttackPhase != AttackPhase.None)
        {
            BlockedNavigationController.ResetState(unit);
            unit.Velocity = SimVector3.Zero;
            SimOverlapResolver.Correct(unit, state);
            return;
        }

        if (behavior.Movement == MovementResult.Forward)
        {
            var objectiveDirection = MovementTargetResolver.ResolveObjectiveAdvanceDirection(
                unit,
                state
            );
            if (objectiveDirection.LengthSquared() < DirectionThreshold)
            {
                unit.Velocity = SimVector3.Zero;
                SimOverlapResolver.Correct(unit, state);
                return;
            }

            float objectiveSpeed = SimEffects.GetEffectiveMoveSpeed(unit);
            var objectiveVelocity = objectiveDirection * objectiveSpeed;
            var objectivePreMovementPos = unit.Position;
            var objectiveNewPos = unit.Position + objectiveVelocity * delta;
            if (unit.MovementLayer == MovementLayer.Air)
                objectiveNewPos.Y = unit.FlightAltitude;
            else
                objectiveNewPos.Y = unit.Position.Y;

            unit.Position = BattlefieldBounds.ClampToBounds(objectiveNewPos);

            var objectiveIntent = new MovementIntent
            {
                Mode = behavior.Movement,
                TargetId = null,
                DesiredVelocity = objectiveVelocity,
                DesiredFacingDirection = objectiveDirection,
            };
            FacingController.Update(unit, objectiveIntent, state);

            SimOverlapResolver.Correct(unit, state);
            unit.Velocity = objectiveVelocity;

            var objectiveDisp = unit.Position - objectivePreMovementPos;
            float objectiveMoveDist = new SimVector3(objectiveDisp.X, 0f, objectiveDisp.Z).Length();
            if (objectiveMoveDist > DirectionThreshold)
                unit.DistanceTraveled += objectiveMoveDist;
            return;
        }

        var destination = MovementTargetResolver.Resolve(unit, behavior.MoveTargetId, state);
        if (!destination.HasValue)
        {
            BlockedNavigationController.ResetState(unit);
            unit.Velocity = SimVector3.Zero;
            SimOverlapResolver.Correct(unit, state);
            return;
        }

        var toTarget = destination.Value - unit.Position;
        toTarget.Y = 0f;
        float dist = toTarget.Length();
        if (dist < DirectionThreshold)
        {
            BlockedNavigationController.ResetState(unit);
            unit.Velocity = SimVector3.Zero;
            SimOverlapResolver.Correct(unit, state);
            return;
        }

        var dir = toTarget / dist;
        float speed = SimEffects.GetEffectiveMoveSpeed(unit);
        float maxStep = speed * delta;
        float appliedSpeed = speed;
        if (dist < maxStep && delta > 0f)
            appliedSpeed = dist / delta;
        var velocity = dir * appliedSpeed;

        var preMovementPos = unit.Position;
        var newPos = unit.Position + velocity * delta;
        if (dist <= maxStep)
            newPos = destination.Value;
        if (unit.MovementLayer == MovementLayer.Air)
            newPos.Y = unit.FlightAltitude;
        else
            newPos.Y = unit.Position.Y;

        unit.Position = BattlefieldBounds.ClampToBounds(newPos);

        var intent = new MovementIntent
        {
            Mode = behavior.Movement,
            TargetId = behavior.MoveTargetId,
            DesiredVelocity = velocity,
            DesiredFacingDirection = dir,
        };
        FacingController.Update(unit, intent, state);

        SimOverlapResolver.Correct(unit, state);
        unit.Velocity = velocity;

        var totalDisp = unit.Position - preMovementPos;
        float moveDist = new SimVector3(totalDisp.X, 0f, totalDisp.Z).Length();
        if (moveDist > DirectionThreshold)
            unit.DistanceTraveled += moveDist;
    }
}
