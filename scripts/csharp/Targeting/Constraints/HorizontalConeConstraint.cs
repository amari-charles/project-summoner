using Godot;
using ProjectSummoner.Units;

namespace ProjectSummoner.Targeting.Constraints;

/// <summary>
/// Constrains attacks to a horizontal cone on the XZ plane.
/// Unit must be facing the target within the cone angle.
///
/// Resolution is deferred to FallbackMovement (typically Strafe) rather than
/// forcing facing here. This prevents rapid oscillation when targets are near
/// the X=0 axis (directly above/below the unit).
/// </summary>
[GlobalClass]
public partial class HorizontalConeConstraint : BaseAttackConstraint
{
    /// <summary>
    /// Half-angle of the cone in degrees. Default ±30° = 60° total cone.
    /// </summary>
    [Export]
    public float ConeHalfAngle { get; set; } = 30f;

    /// <summary>
    /// Distance threshold below which targets are always attackable.
    /// </summary>
    [Export]
    public float CloseRangeThreshold { get; set; } = 0.5f;

    public override bool IsAttackValid(Unit3D unit, Node3D target)
    {
        Vector3 toTarget = target.GlobalPosition - unit.GlobalPosition;
        Vector2 horizontalDir = new Vector2(toTarget.X, toTarget.Z);

        // Very close targets are always attackable
        if (horizontalDir.Length() < CloseRangeThreshold)
            return true;

        // Calculate angle to target on XZ plane (0° = +X, 90° = +Z)
        float angleToTarget = Mathf.RadToDeg(Mathf.Atan2(horizontalDir.Y, horizontalDir.X));

        // Facing direction: right = 0°, left = 180°
        float facingAngle = unit.IsFacingRight ? 0f : 180f;

        // Calculate angular difference with wrap-around handling
        float angleDiff = angleToTarget - facingAngle;

        // Normalize to -180 to 180 range
        while (angleDiff > 180f) angleDiff -= 360f;
        while (angleDiff < -180f) angleDiff += 360f;

        return Mathf.Abs(angleDiff) <= ConeHalfAngle;
    }

    public override bool TryResolve(Unit3D unit, Node3D target)
    {
        // Don't force facing here - let strafe movement naturally bring
        // the target into the cone. This prevents rapid oscillation when
        // the target is near the X=0 axis (directly above/below).
        return IsAttackValid(unit, target);
    }
}
