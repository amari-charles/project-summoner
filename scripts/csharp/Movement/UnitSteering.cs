using Godot;
using ProjectSummoner.Systems;
using ProjectSummoner.Units;

namespace ProjectSummoner.Movement;

/// <summary>
/// Handles unit separation steering, flanking, and overlap correction.
/// Prevents units from stacking on top of each other during movement.
/// </summary>
public class UnitSteering
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    // Separation constants
    private const float MaxCollisionRadius = 1.5f;
    private const float SeparationMultiplier = 1.5f;
    private const float SeparationStrength = 2.0f;
    private const float LateralSeparationBoost = 3.0f;
    private const int MaxNeighborsToProcess = 8;  // Cap iterations for dense formations

    // Blocked detection constants
    private const float BlockedThreshold = 0.3f;
    private const float BlockedMoveThreshold = 0.1f;

    // Flanking constants
    private const float FlankStrength = 1.2f;
    private const float FlankAngleMin = 90.0f;
    private const float FlankAngleMax = 135.0f;
    private const float FlankAngleStep = 15.0f;
    private const float FlankProgressInterval = 0.5f;
    private const float FlankScoreThreshold = 0.2f;
    private const float FlankConeThreshold = 0.3f; // dot > 0.3 means within ~70° of direction

    // =========================================================================
    // STATE
    // =========================================================================

    private float _blockedTime;
    private Vector3 _lastPosition;
    private float _flankAngle = FlankAngleMin;
    private int _flankDirection; // -1 = left, 1 = right, 0 = not chosen
    private float _flankProgressTimer;

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Calculate separation steering force to avoid overlapping with nearby units.
    /// Separates from ALL units (both teams) EXCEPT the current attack target.
    /// </summary>
    public Vector3 CalculateSeparationForce(Unit3D unit, Node3D? currentTarget)
    {
        if (SpatialGrid.Instance == null)
            return Vector3.Zero;

        var separation = Vector3.Zero;
        float collisionRadius = unit.CollisionRadius;

        // Query radius: max separation distance (self + largest possible other unit)
        float separationRadius = (collisionRadius + MaxCollisionRadius) * SeparationMultiplier;
        var nearbyUnits = SpatialGrid.Instance.GetUnitsInRadius(
            unit.GlobalPosition,
            separationRadius,
            -1, // All teams
            unit
        );

        if (nearbyUnits.Count == 0)
            return Vector3.Zero;

        // Get movement direction (toward target) for lateral boost calculation
        var moveDir = Vector3.Zero;
        if (currentTarget != null)
        {
            moveDir = (currentTarget.GlobalPosition - unit.GlobalPosition).Normalized();
            moveDir.Y = 0;
        }

        int neighborsProcessed = 0;
        foreach (var node in nearbyUnits)
        {
            // Cap iterations for dense formations (performance optimization)
            if (neighborsProcessed >= MaxNeighborsToProcess)
                break;

            // Don't separate from current attack target - we need to approach it!
            if (node == currentTarget)
                continue;

            if (node is not Unit3D other)
                continue;

            // Calculate separation distance based on combined collision radii
            float combinedCollision = collisionRadius + other.CollisionRadius;
            float separationDist = combinedCollision * SeparationMultiplier;

            // Calculate 2D distance (ignore Y-axis)
            var delta = unit.GlobalPosition - other.GlobalPosition;
            delta.Y = 0;
            float distanceSq = delta.X * delta.X + delta.Z * delta.Z;

            // Skip if outside separation distance
            if (distanceSq >= separationDist * separationDist || distanceSq < 0.001f)
                continue;

            // Calculate push direction (away from other unit)
            float distance = Mathf.Sqrt(distanceSq);
            var pushDir = delta.Normalized();

            // Stronger push when closer (inverse relationship)
            float strength = (1.0f - distance / separationDist) * SeparationStrength;
            separation += pushDir * strength;

            // LATERAL BOOST: Add extra lateral separation when units are aligned with movement
            // This spreads units sideways as they advance, preventing tight bunching
            if (moveDir.LengthSquared() > 0.01f)
            {
                var lateralDir = new Vector3(-moveDir.Z, 0, moveDir.X);
                // Check how aligned the other unit is with our movement (front/back vs side)
                float forwardAlignment = Mathf.Abs(pushDir.Dot(moveDir));
                // More lateral push when units are in front/behind (aligned), less when already to the side
                // Use instance ID to decide which way to push (prevents oscillation)
                float lateralSign = (unit.GetInstanceId() % 2 == 0) ? 1.0f : -1.0f;
                separation += lateralDir * lateralSign * forwardAlignment * strength * LateralSeparationBoost;
            }

            neighborsProcessed++;
        }

        return separation;
    }

    /// <summary>
    /// Calculate lateral flanking force when blocked by allies.
    /// Uses smart direction choice + progressive angle rotation for wrap-around behavior.
    /// </summary>
    public Vector3 CalculateFlankForce(Unit3D unit, Node3D? currentTarget, float delta)
    {
        // Reset state when not blocked
        if (_blockedTime < BlockedThreshold)
        {
            _flankAngle = FlankAngleMin;
            _flankDirection = 0;
            _flankProgressTimer = 0.0f;
            return Vector3.Zero;
        }

        if (currentTarget == null)
            return Vector3.Zero;

        var toTarget = (currentTarget.GlobalPosition - unit.GlobalPosition).Normalized();
        toTarget.Y = 0;

        // PHASE 1: Choose direction (runs once when first blocked)
        if (_flankDirection == 0)
        {
            var scores = CalculateFlankDirectionScores(unit, currentTarget);
            float leftScore = scores.Left;
            float rightScore = scores.Right;

            // Pick less congested side, or use instance ID as tiebreaker
            if (Mathf.Abs(leftScore - rightScore) < FlankScoreThreshold)
            {
                _flankDirection = (unit.GetInstanceId() % 2 == 0) ? -1 : 1;
            }
            else
            {
                _flankDirection = leftScore < rightScore ? -1 : 1;
            }

            _flankAngle = FlankAngleMin;
            _flankProgressTimer = 0.0f;
        }

        // PHASE 2: Progressive angle increase + direction re-evaluation
        _flankProgressTimer += delta;
        if (_flankProgressTimer >= FlankProgressInterval)
        {
            if (_blockedTime > BlockedThreshold + FlankProgressInterval)
            {
                // Re-evaluate direction - maybe the other side is now better
                var scores = CalculateFlankDirectionScores(unit, currentTarget);
                float leftScore = scores.Left;
                float rightScore = scores.Right;
                float currentScore = _flankDirection < 0 ? leftScore : rightScore;
                float otherScore = _flankDirection < 0 ? rightScore : leftScore;

                // Switch sides if other direction is significantly better
                if (otherScore < currentScore - FlankScoreThreshold)
                {
                    _flankDirection = -_flankDirection; // Flip direction
                    _flankAngle = FlankAngleMin; // Reset angle for new direction
                }
                else
                {
                    // Same direction, increase angle
                    _flankAngle = Mathf.Min(_flankAngle + FlankAngleStep, FlankAngleMax);
                }

                _flankProgressTimer = 0.0f;
            }
        }

        // PHASE 3: Apply force at current angle
        float angleRad = Mathf.DegToRad(_flankAngle);
        float lateralComponent = Mathf.Sin(angleRad);
        float forwardComponent = Mathf.Cos(angleRad);

        Vector3 lateralDir;
        if (_flankDirection < 0)
        {
            lateralDir = new Vector3(-toTarget.Z, 0, toTarget.X); // Left
        }
        else
        {
            lateralDir = new Vector3(toTarget.Z, 0, -toTarget.X); // Right
        }

        var flankDir = (toTarget * forwardComponent + lateralDir * lateralComponent).Normalized();
        float strength = Mathf.Min((_blockedTime - BlockedThreshold) * 2.0f, 1.0f);

        return flankDir * strength * unit.MoveSpeed * FlankStrength;
    }

    /// <summary>
    /// Correct severe overlaps by pushing units apart after movement.
    /// This is a "hard" correction for when soft separation wasn't enough.
    /// </summary>
    public void CorrectOverlaps(Unit3D unit)
    {
        if (SpatialGrid.Instance == null)
            return;

        float collisionRadius = unit.CollisionRadius;

        // Query radius: self + largest possible other unit
        float checkRadius = collisionRadius + MaxCollisionRadius;
        var nearbyUnits = SpatialGrid.Instance.GetUnitsInRadius(
            unit.GlobalPosition,
            checkRadius,
            -1, // All teams
            unit
        );

        int neighborsProcessed = 0;
        foreach (var node in nearbyUnits)
        {
            // Cap iterations for dense formations (performance optimization)
            if (neighborsProcessed >= MaxNeighborsToProcess)
                break;

            if (node is not Unit3D other)
                continue;

            // Minimum distance is the sum of both collision radii
            float minDist = collisionRadius + other.CollisionRadius;

            // Calculate 2D distance squared first (avoid sqrt if not overlapping)
            var delta = unit.GlobalPosition - other.GlobalPosition;
            delta.Y = 0;
            float distanceSq = delta.X * delta.X + delta.Z * delta.Z;

            // Skip if not overlapping (use squared comparison)
            if (distanceSq >= minDist * minDist || distanceSq < 0.000001f)
                continue;

            // Only now calculate actual distance for push amount
            float distance = Mathf.Sqrt(distanceSq);
            float overlap = minDist - distance;
            var pushDir = delta.Normalized();

            // Push self by half the overlap (other unit will push itself too)
            unit.GlobalPosition += pushDir * overlap * 0.5f;

            neighborsProcessed++;
        }
    }

    /// <summary>
    /// Update blocked state based on movement progress.
    /// Call this after movement to track if unit is stuck.
    /// </summary>
    public void UpdateBlockedState(Unit3D unit, float delta)
    {
        var currentPos = unit.GlobalPosition;
        currentPos.Y = 0;
        var lastPos = _lastPosition;
        lastPos.Y = 0;

        // Guard against division by zero on first frame
        if (delta < 0.0001f)
        {
            _lastPosition = unit.GlobalPosition;
            return;
        }

        float movementThisFrame = currentPos.DistanceTo(lastPos);
        float movementPerSecond = movementThisFrame / delta;

        if (movementPerSecond < BlockedMoveThreshold)
        {
            _blockedTime += delta;
        }
        else
        {
            _blockedTime = 0.0f;
            // Reset flanking state when moving freely
            _flankAngle = FlankAngleMin;
            _flankDirection = 0;
            _flankProgressTimer = 0.0f;
        }

        _lastPosition = unit.GlobalPosition;
    }

    /// <summary>
    /// Reset all steering state. Call when unit spawns or respawns.
    /// </summary>
    public void Reset()
    {
        _blockedTime = 0.0f;
        _lastPosition = Vector3.Zero;
        _flankAngle = FlankAngleMin;
        _flankDirection = 0;
        _flankProgressTimer = 0.0f;
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    /// <summary>
    /// Calculate congestion scores for left and right flanking directions.
    /// Lower score = less congestion = better direction to flank.
    /// </summary>
    private (float Left, float Right) CalculateFlankDirectionScores(Unit3D unit, Node3D currentTarget)
    {
        if (SpatialGrid.Instance == null)
            return (0.0f, 0.0f);

        var toTarget = (currentTarget.GlobalPosition - unit.GlobalPosition).Normalized();
        toTarget.Y = 0;

        var leftDir = new Vector3(-toTarget.Z, 0, toTarget.X);
        var rightDir = new Vector3(toTarget.Z, 0, -toTarget.X);

        float leftScore = 0.0f;
        float rightScore = 0.0f;
        float checkDistance = (unit.CollisionRadius + MaxCollisionRadius) * 1.5f;

        var nearbyUnits = SpatialGrid.Instance.GetUnitsInRadius(
            unit.GlobalPosition,
            checkDistance,
            -1, // All teams
            unit
        );

        int neighborsProcessed = 0;
        foreach (var node in nearbyUnits)
        {
            // Cap iterations for dense formations (performance optimization)
            if (neighborsProcessed >= MaxNeighborsToProcess)
                break;

            if (node == currentTarget)
                continue;
            if (node is not Unit3D)
                continue;

            var toOther = node.GlobalPosition - unit.GlobalPosition;
            toOther.Y = 0;
            float dist = toOther.Length();

            if (dist < 0.001f)
                continue;

            var toOtherNorm = toOther / dist;
            float weight = 1.0f - (dist / checkDistance);

            // Check if unit is within cone of each flank direction
            if (toOtherNorm.Dot(leftDir) > FlankConeThreshold)
                leftScore += weight;
            if (toOtherNorm.Dot(rightDir) > FlankConeThreshold)
                rightScore += weight;

            neighborsProcessed++;
        }

        return (leftScore, rightScore);
    }
}
