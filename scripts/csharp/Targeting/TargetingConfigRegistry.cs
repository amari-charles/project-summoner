using System.Collections.Generic;
using ProjectSummoner.Targeting.Filters;
using ProjectSummoner.Targeting.Scorers;
using ProjectSummoner.Targeting.Constraints;

namespace ProjectSummoner.Targeting;

/// <summary>
/// Registry for unit-specific targeting configurations.
/// Builds configs programmatically to avoid .tres resource loading issues.
/// </summary>
public static class TargetingConfigRegistry
{
    private static readonly Dictionary<string, TargetingConfig> _configs = new();
    private static bool _initialized = false;

    /// <summary>
    /// Get targeting config for a specific unit type.
    /// Returns default config if no specific config is registered.
    /// </summary>
    public static TargetingConfig GetConfig(string unitId)
    {
        EnsureInitialized();

        if (_configs.TryGetValue(unitId, out var config))
            return config;

        return DefaultTargetingConfig.Get();
    }

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        RegisterPuffConfig();
        // Add more unit configs here as needed
    }

    /// <summary>
    /// Puff: Ranged unit with horizontal cone constraint.
    /// Must face target within ±30° to attack.
    /// </summary>
    private static void RegisterPuffConfig()
    {
        // Filter: Valid targets only
        var validFilter = new ValidTargetFilter();

        // Scorer: Prefer close targets, bonus for targets below
        var distanceScorer = new DistanceScorer { MaxDistance = 24f, Weight = 1f };
        var belowScorer = new BelowTargetScorer { Radius = 6f, Weight = 5f };
        var compositeScorer = new CompositeScorer();
        compositeScorer.Scorers.Add(distanceScorer);
        compositeScorer.Scorers.Add(belowScorer);

        // Constraints: Range + Horizontal cone (must face target)
        var rangeConstraint = new RangeConstraint();
        var coneConstraint = new HorizontalConeConstraint
        {
            ConeHalfAngle = 30f,
            CloseRangeThreshold = 0.5f
        };
        var compositeConstraint = new CompositeConstraint();
        compositeConstraint.Constraints.Add(rangeConstraint);
        compositeConstraint.Constraints.Add(coneConstraint);

        var config = new TargetingConfig
        {
            Filter = validFilter,
            Scorer = compositeScorer,
            AttackConstraint = compositeConstraint,
            AggroRadius = 24f,
            FallbackMovement = FallbackMovementStyle.Strafe  // Ranged: circle around target
        };

        _configs["puff"] = config;
    }
}
