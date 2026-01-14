using Godot;
using ProjectSummoner.Units;

namespace ProjectSummoner.Targeting.Constraints;

/// <summary>
/// Data for debug visualization of an attack constraint.
/// Constraints return this to ensure visualization matches actual validation.
/// </summary>
public record AttackVisualizationData(
    bool IsCone,
    float? ConeHalfAngle,
    Vector3 FacingDirection
);

/// <summary>
/// Abstract base class for attack constraints.
/// Constraints determine when an attack is valid and can resolve violations.
/// </summary>
[GlobalClass]
public abstract partial class BaseAttackConstraint : Resource
{
    /// <summary>
    /// Check if the unit can attack the target right now.
    /// </summary>
    public abstract bool IsAttackValid(Unit3D unit, Node3D target);

    /// <summary>
    /// Attempt to resolve a constraint violation (e.g., turn to face target).
    /// Returns true if resolved immediately, false if need to wait.
    ///
    /// Resolution strategies:
    /// 1. Immediate resolution: Modify unit state (e.g., SetFacing) and return true
    /// 2. Deferred resolution: Return false to let FallbackMovement handle it
    ///    (e.g., Strafe movement naturally brings target into cone)
    /// 3. Passive check: Just return IsAttackValid (default behavior)
    ///
    /// When false is returned, Unit3D.UpdateBehavior uses FallbackMovementStyle
    /// to determine how to move (MoveToward, Strafe, or Idle).
    /// </summary>
    public virtual bool TryResolve(Unit3D unit, Node3D target)
    {
        return IsAttackValid(unit, target);
    }

    /// <summary>
    /// Check if the target is ever reachable given this constraint.
    /// Used during target acquisition to filter out impossible targets.
    /// Default returns true (always potentially reachable).
    /// </summary>
    public virtual bool CanEverReach(Unit3D unit, Node3D target)
    {
        return true;
    }

    /// <summary>
    /// Get visualization data for debug rendering.
    /// Ensures debug visuals match actual constraint parameters.
    /// </summary>
    public virtual AttackVisualizationData? GetVisualizationData(Unit3D unit)
    {
        return null;
    }
}
