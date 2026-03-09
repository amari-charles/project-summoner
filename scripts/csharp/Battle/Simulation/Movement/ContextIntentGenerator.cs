using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Subsystems;
using Fateforged.Units;

namespace Fateforged.Simulation.Movement;

/// <summary>
/// Optional context-steering intent generator.
/// Produces desired intent only; ORCA still owns collision avoidance.
/// </summary>
public sealed class ContextIntentGenerator : IMovementIntentGenerator
{
    private static readonly DirectIntentGenerator _fallbackGenerator = new();

    public MovementIntent Generate(
        UnitData unit, SimBehavior.BehaviorResult behavior, MatchState state, float delta)
    {
        // If the target is no longer resolvable, fall back to direct behavior defaults.
        if (RequiresTarget(behavior.Movement) &&
            !MovementTargetResolver.Resolve(unit, behavior.MoveTargetId, state).HasValue)
        {
            return _fallbackGenerator.Generate(unit, behavior, state, delta);
        }

        float speed = SimEffects.GetEffectiveMoveSpeed(unit);
        var preferredDir = ContextSteering.Resolve(unit, behavior, state);

        if (preferredDir.LengthSquared() < 0.001f)
        {
            return new MovementIntent
            {
                Mode = behavior.Movement,
                TargetId = behavior.MoveTargetId,
                DesiredVelocity = SimVector3.Zero,
                DesiredFacingDirection = ResolveFacingDirection(unit, behavior, state, preferredDir)
            };
        }

        var facingDir = ResolveFacingDirection(unit, behavior, state, preferredDir);
        return new MovementIntent
        {
            Mode = behavior.Movement,
            TargetId = behavior.MoveTargetId,
            DesiredVelocity = preferredDir * speed,
            DesiredFacingDirection = facingDir
        };
    }

    private static bool RequiresTarget(MovementResult mode)
        => mode == MovementResult.TowardTarget || mode == MovementResult.Strafe;

    private static SimVector3 ResolveFacingDirection(
        UnitData unit, SimBehavior.BehaviorResult behavior, MatchState state, SimVector3 preferredDir)
    {
        if (behavior.Movement == MovementResult.Forward)
        {
            float forwardX = unit.Team == Team.Player ? 1f : -1f;
            return new SimVector3(forwardX, 0f, 0f);
        }

        var targetPos = SimUtils.ResolveTargetPosition(behavior.MoveTargetId, state);
        if (!targetPos.HasValue)
            return preferredDir;

        var toTarget = targetPos.Value - unit.Position;
        toTarget.Y = 0f;
        if (toTarget.LengthSquared() < 0.001f)
            return preferredDir;

        return toTarget.Normalized();
    }
}
