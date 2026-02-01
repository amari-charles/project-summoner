using ProjectSummoner.Targeting.Filters;
using ProjectSummoner.Targeting.Scorers;
using ProjectSummoner.Targeting.Constraints;
using ProjectSummoner.Units;

namespace ProjectSummoner.Targeting;

/// <summary>
/// Interface for defining how a unit acquires and validates targets.
/// Each implementation encapsulates the targeting configuration for a specific unit archetype.
/// </summary>
public interface ITargetingBehavior
{
    /// <summary>
    /// Builds the TargetingConfig for this behavior.
    /// Called once when the unit is spawned.
    /// </summary>
    TargetingConfig BuildConfig();
}

/// <summary>
/// Standard melee targeting: ground-only targets, MoveToward fallback.
/// Used by most melee units (wisps, titans, boars, etc.)
/// </summary>
public class MeleeTargeting : ITargetingBehavior
{
    public float AggroRadius { get; init; } = 20f;

    public static MeleeTargeting Default { get; } = new();

    public TargetingConfig BuildConfig()
    {
        // Filter: Valid targets + Ground only (cannot target flying units)
        var validFilter = new ValidTargetFilter();
        var layerFilter = new LayerTargetFilter { CanTarget = TargetLayer.GroundOnly };
        var compositeFilter = new CompositeTargetFilter();
        compositeFilter.Filters.Add(validFilter);
        compositeFilter.Filters.Add(layerFilter);

        // Scorer: Prefer close targets, slightly prefer low health
        var distanceScorer = new DistanceScorer { MaxDistance = AggroRadius, Weight = 1f };
        var healthScorer = new HealthScorer { Weight = 10f };
        var compositeScorer = new CompositeScorer();
        compositeScorer.Scorers.Add(distanceScorer);
        compositeScorer.Scorers.Add(healthScorer);

        // Constraints: Range only
        var rangeConstraint = new RangeConstraint();

        return new TargetingConfig
        {
            Filter = compositeFilter,
            Scorer = compositeScorer,
            AttackConstraint = rangeConstraint,
            AggroRadius = AggroRadius,
            FallbackMovement = FallbackMovementStyle.MoveToward
        };
    }
}

/// <summary>
/// Ground-based ranged targeting: targets both ground and air, MoveToward fallback.
/// Used by artillery-style units (Rock Thrower, Fire Spider).
/// </summary>
public class RangedGroundTargeting : ITargetingBehavior
{
    public float AggroRadius { get; init; } = 20f;

    public static RangedGroundTargeting Default { get; } = new();

    public TargetingConfig BuildConfig()
    {
        // Filter: Valid targets only (can target both ground and air)
        var validFilter = new ValidTargetFilter();

        // Scorer: Prefer close targets
        var distanceScorer = new DistanceScorer { MaxDistance = AggroRadius, Weight = 1f };
        var compositeScorer = new CompositeScorer();
        compositeScorer.Scorers.Add(distanceScorer);

        // Constraints: Range only
        var rangeConstraint = new RangeConstraint();

        return new TargetingConfig
        {
            Filter = validFilter,
            Scorer = compositeScorer,
            AttackConstraint = rangeConstraint,
            AggroRadius = AggroRadius,
            FallbackMovement = FallbackMovementStyle.MoveToward  // Artillery style: advance when out of range
        };
    }
}

/// <summary>
/// Flying ranged targeting: targets both layers, Strafe fallback.
/// Used by most flying ranged units.
/// </summary>
public class RangedFlyingTargeting : ITargetingBehavior
{
    public float AggroRadius { get; init; } = 20f;

    public static RangedFlyingTargeting Default { get; } = new();

    public TargetingConfig BuildConfig()
    {
        // Filter: Valid targets only (can target both ground and air)
        var validFilter = new ValidTargetFilter();

        // Scorer: Prefer close targets
        var distanceScorer = new DistanceScorer { MaxDistance = AggroRadius, Weight = 1f };
        var compositeScorer = new CompositeScorer();
        compositeScorer.Scorers.Add(distanceScorer);

        // Constraints: Range only
        var rangeConstraint = new RangeConstraint();

        return new TargetingConfig
        {
            Filter = validFilter,
            Scorer = compositeScorer,
            AttackConstraint = rangeConstraint,
            AggroRadius = AggroRadius,
            FallbackMovement = FallbackMovementStyle.Strafe  // Kite/circle around target
        };
    }
}

/// <summary>
/// Puff-specific targeting: 3D cone constraint, prefers targets below.
/// Must face target within cone angle to attack.
/// </summary>
public class PuffConeTargeting : ITargetingBehavior
{
    public float ConeHalfAngle { get; init; } = 30f;
    public float CloseRangeThreshold { get; init; } = 0.5f;
    public float AggroRadius { get; init; } = 24f;
    public float BelowTargetRadius { get; init; } = 6f;
    public float BelowTargetWeight { get; init; } = 5f;

    public static PuffConeTargeting Default { get; } = new();

    public TargetingConfig BuildConfig()
    {
        // Filter: Valid targets only (can target both ground and air)
        var validFilter = new ValidTargetFilter();

        // Scorer: Prefer close targets, bonus for targets below
        var distanceScorer = new DistanceScorer { MaxDistance = AggroRadius, Weight = 1f };
        var belowScorer = new BelowTargetScorer { Radius = BelowTargetRadius, Weight = BelowTargetWeight };
        var compositeScorer = new CompositeScorer();
        compositeScorer.Scorers.Add(distanceScorer);
        compositeScorer.Scorers.Add(belowScorer);

        // Constraints: Range + 3D cone (must face target in 3D space)
        var rangeConstraint = new RangeConstraint();
        var coneConstraint = new ConeConstraint3D
        {
            ConeHalfAngle = ConeHalfAngle,
            CloseRangeThreshold = CloseRangeThreshold
        };
        var compositeConstraint = new CompositeConstraint();
        compositeConstraint.Constraints.Add(rangeConstraint);
        compositeConstraint.Constraints.Add(coneConstraint);

        return new TargetingConfig
        {
            Filter = validFilter,
            Scorer = compositeScorer,
            AttackConstraint = compositeConstraint,
            AggroRadius = AggroRadius,
            FallbackMovement = FallbackMovementStyle.Strafe
        };
    }
}

/// <summary>
/// Passive targeting: no aggro, no movement, no attack.
/// Used by stationary target dummies (Rock).
/// </summary>
public class PassiveTargeting : ITargetingBehavior
{
    public static PassiveTargeting Default { get; } = new();

    public TargetingConfig BuildConfig()
    {
        var validFilter = new ValidTargetFilter();
        var distanceScorer = new DistanceScorer { MaxDistance = 0f, Weight = 1f };  // No aggro
        var compositeScorer = new CompositeScorer();
        compositeScorer.Scorers.Add(distanceScorer);
        var rangeConstraint = new RangeConstraint();

        return new TargetingConfig
        {
            Filter = validFilter,
            Scorer = compositeScorer,
            AttackConstraint = rangeConstraint,
            AggroRadius = 0f,  // No aggro - stationary dummy
            FallbackMovement = FallbackMovementStyle.Idle  // Can't move
        };
    }
}

/// <summary>
/// Duckling targeting: ranged, shorter aggro radius, follows mama's target.
/// Can target both ground and air to match mama.
/// </summary>
public class DucklingTargeting : ITargetingBehavior
{
    public float AggroRadius { get; init; } = 16f;

    public static DucklingTargeting Default { get; } = new();

    public TargetingConfig BuildConfig()
    {
        // Filter: Valid targets only (can target both ground and air to match mama)
        var validFilter = new ValidTargetFilter();

        // Scorer: Prefer close targets
        var distanceScorer = new DistanceScorer { MaxDistance = AggroRadius, Weight = 1f };
        var compositeScorer = new CompositeScorer();
        compositeScorer.Scorers.Add(distanceScorer);

        // Constraints: Range only
        var rangeConstraint = new RangeConstraint();

        return new TargetingConfig
        {
            Filter = validFilter,
            Scorer = compositeScorer,
            AttackConstraint = rangeConstraint,
            AggroRadius = AggroRadius,
            FallbackMovement = FallbackMovementStyle.Strafe
        };
    }
}
