using Godot;
using ProjectSummoner.Units;

namespace ProjectSummoner.Targeting.Constraints;

/// <summary>
/// Constrains attacks to a 3D cone from the unit's facing direction.
/// Targets must be within the cone angle in 3D space (both horizontal and vertical).
///
/// Unlike HorizontalConeConstraint which only checks XZ plane, this checks the full
/// 3D angle. A target directly below a flying unit would be ~90 degrees from horizontal
/// facing and thus outside a typical 30-degree cone.
/// </summary>
[GlobalClass]
public partial class ConeConstraint3D : BaseAttackConstraint
{
    /// <summary>
    /// Half-angle of the cone in degrees. Default ±30° = 60° total cone.
    /// </summary>
    [Export]
    public float ConeHalfAngle { get; set; } = 30f;

    /// <summary>
    /// Distance threshold below which targets are always attackable.
    /// Prevents issues when target is extremely close.
    /// </summary>
    [Export]
    public float CloseRangeThreshold { get; set; } = 0.5f;

    public override bool IsAttackValid(Unit3D unit, Node3D target)
    {
        Vector3 toTarget = target.GlobalPosition - unit.GlobalPosition;

        // Very close targets are always attackable
        if (toTarget.Length() < CloseRangeThreshold)
            return true;

        // Get unit's facing direction as a 3D vector (horizontal, based on Y rotation)
        // Units face +X when facing right, -X when facing left
        Vector3 facing = unit.IsFacingRight
            ? new Vector3(1, 0, 0)
            : new Vector3(-1, 0, 0);

        // Normalize direction to target
        Vector3 toTargetNormalized = toTarget.Normalized();

        // Calculate 3D angle between facing and target direction using dot product
        float dot = facing.Dot(toTargetNormalized);

        // Clamp to avoid floating point errors in Acos
        dot = Mathf.Clamp(dot, -1f, 1f);

        float angleRad = Mathf.Acos(dot);
        float angleDeg = Mathf.RadToDeg(angleRad);

        return angleDeg <= ConeHalfAngle;
    }

    public override bool TryResolve(Unit3D unit, Node3D target)
    {
        // Don't force facing here - let strafe movement naturally bring
        // the target into the cone.
        return IsAttackValid(unit, target);
    }

    public override bool CanEverReach(Unit3D unit, Node3D target)
    {
        // Targets at extreme vertical angles can never be in the cone.
        // A target directly above/below would require a 90° angle from horizontal,
        // which exceeds any reasonable cone half-angle.
        Vector3 toTarget = target.GlobalPosition - unit.GlobalPosition;
        float horizontalDist = new Vector2(toTarget.X, toTarget.Z).Length();
        float verticalDist = Mathf.Abs(toTarget.Y);

        // If horizontal distance is near zero, the angle approaches 90°
        if (horizontalDist < CloseRangeThreshold)
        {
            // Very close targets are always reachable (same as IsAttackValid)
            return toTarget.Length() < CloseRangeThreshold;
        }

        // Calculate vertical angle from horizontal plane
        float angleToTarget = Mathf.RadToDeg(Mathf.Atan2(verticalDist, horizontalDist));
        return angleToTarget <= ConeHalfAngle;
    }

    public override AttackVisualizationData? GetVisualizationData(Unit3D unit)
    {
        Vector3 facing = unit.IsFacingRight
            ? new Vector3(1, 0, 0)
            : new Vector3(-1, 0, 0);
        return new AttackVisualizationData(true, ConeHalfAngle, facing);
    }
}
