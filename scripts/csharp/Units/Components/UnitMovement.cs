using Godot;
using ProjectSummoner.Movement;
using ProjectSummoner.Units;

namespace ProjectSummoner.Units.Components;

/// <summary>
/// Result of a movement calculation.
/// Unit3D applies velocity via MoveAndSlide() and updates facing.
/// </summary>
public struct MovementResult
{
    /// <summary>Calculated velocity to apply.</summary>
    public Vector3 Velocity;

    /// <summary>Direction the unit should face (for UpdateFacing).</summary>
    public Vector3 FacingDirection;

    /// <summary>Whether facing was explicitly set (e.g., during strafe).</summary>
    public bool FacingExplicitlySet;

    /// <summary>Explicit facing value when FacingExplicitlySet is true.</summary>
    public bool ShouldFaceRight;

    /// <summary>Whether to extend target lock (during strafe).</summary>
    public bool ExtendTargetLock;

    public static MovementResult Zero => new() { Velocity = Vector3.Zero, FacingDirection = Vector3.Zero };
}

/// <summary>
/// Handles unit movement calculations including steering, facing, and velocity.
/// Extracted from Unit3D to consolidate movement logic.
/// </summary>
public class UnitMovement
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    // Minimum horizontal displacement for strafe calculations
    private const float MinHorizontalDisplacement = 0.01f;

    // Half-plane angle threshold for facing direction during strafe
    private const float StrafeFacingHalfAngle = 90f;

    // =========================================================================
    // COMPONENTS
    // =========================================================================

    private readonly UnitSteering _steering = new();

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Calculate movement for moving forward (no target).
    /// </summary>
    public MovementResult CalculateForwardMovement(Unit3D unit, float delta)
    {
        // Move toward enemy base direction
        float direction = unit.Team == (int)Team.Player ? 1.0f : -1.0f;
        Vector3 moveDir = new(direction, 0, 0);

        // Apply separation steering to avoid stacking with nearby units
        Vector3 separation = _steering.CalculateSeparationForce(unit, null);
        Vector3 finalDir = (moveDir + separation).Normalized();
        Vector3 velocity = finalDir * unit.MoveSpeed;

        // Maintain altitude for flying units
        if (unit.MovementLayer == (int)MovementLayer.Air)
        {
            velocity.Y = 0;
        }

        return new MovementResult
        {
            Velocity = velocity,
            FacingDirection = moveDir, // Use base direction, not separation-adjusted
            FacingExplicitlySet = false
        };
    }

    /// <summary>
    /// Calculate movement toward a target.
    /// </summary>
    public MovementResult CalculateTowardTargetMovement(Unit3D unit, Node3D target, float delta)
    {
        Vector3 targetPos = target.GlobalPosition;

        // For ground units, ignore Y difference when moving
        if (unit.MovementLayer == (int)MovementLayer.Ground)
        {
            targetPos.Y = unit.GlobalPosition.Y;
        }

        Vector3 direction = (targetPos - unit.GlobalPosition).Normalized();

        // Apply separation steering to avoid stacking with nearby units
        Vector3 separation = _steering.CalculateSeparationForce(unit, target);
        Vector3 flank = _steering.CalculateFlankForce(unit, target, delta);
        Vector3 finalDir = (direction * unit.MoveSpeed + separation + flank).Normalized();
        Vector3 velocity = finalDir * unit.MoveSpeed;

        // Maintain altitude for flying units
        if (unit.MovementLayer == (int)MovementLayer.Air)
        {
            velocity.Y = 0;
        }

        return new MovementResult
        {
            Velocity = velocity,
            FacingDirection = direction, // Use base direction, not separation-adjusted
            FacingExplicitlySet = false
        };
    }

    /// <summary>
    /// Calculate strafe movement (circling around target).
    /// Used by ranged units when they can't get target in attack cone.
    /// </summary>
    public MovementResult CalculateStrafeMovement(Unit3D unit, Node3D target, bool currentlyFacingRight, float delta)
    {
        Vector3 toTarget = target.GlobalPosition - unit.GlobalPosition;
        Vector3 horizontalToTarget = new(toTarget.X, 0, toTarget.Z);

        if (horizontalToTarget.LengthSquared() < MinHorizontalDisplacement * MinHorizontalDisplacement)
        {
            // Target directly on top of us - no movement
            return MovementResult.Zero;
        }

        // Calculate angle to target (0° = +X, 90° = +Z)
        float angleToTarget = Mathf.RadToDeg(Mathf.Atan2(horizontalToTarget.Z, horizontalToTarget.X));

        // Determine optimal facing: face toward the half-plane where target is
        bool shouldFaceRight = Mathf.Abs(angleToTarget) <= StrafeFacingHalfAngle;

        // Calculate angle difference from facing
        float facingAngle = shouldFaceRight ? 0f : 180f;
        float angleDiff = angleToTarget - facingAngle;
        while (angleDiff > 180f) angleDiff -= 360f;
        while (angleDiff < -180f) angleDiff += 360f;

        // Get perpendicular strafe direction (90° counterclockwise from toTarget)
        Vector3 strafeDir = new Vector3(-horizontalToTarget.Z, 0, horizontalToTarget.X).Normalized();

        // Choose strafe direction that reduces |angleDiff|
        if (angleDiff < 0)
            strafeDir = -strafeDir;

        // Apply separation steering to avoid stacking with nearby units
        Vector3 separation = _steering.CalculateSeparationForce(unit, target);
        Vector3 finalDir = (strafeDir + separation).Normalized();
        Vector3 velocity = finalDir * unit.MoveSpeed;

        // Maintain altitude for flying units
        if (unit.MovementLayer == (int)MovementLayer.Air)
        {
            velocity.Y = 0;
        }

        return new MovementResult
        {
            Velocity = velocity,
            FacingDirection = Vector3.Zero, // Not used when FacingExplicitlySet
            FacingExplicitlySet = true,
            ShouldFaceRight = shouldFaceRight,
            ExtendTargetLock = true // Prevent mid-strafe target switches
        };
    }

    /// <summary>
    /// Correct severe overlaps after movement.
    /// </summary>
    public void CorrectOverlaps(Unit3D unit)
    {
        _steering.CorrectOverlaps(unit);
    }

    /// <summary>
    /// Update blocked state for flanking behavior.
    /// Call after movement toward a target.
    /// </summary>
    public void UpdateBlockedState(Unit3D unit, float delta)
    {
        _steering.UpdateBlockedState(unit, delta);
    }

    /// <summary>
    /// Reset all movement/steering state.
    /// </summary>
    public void Reset()
    {
        _steering.Reset();
    }
}
