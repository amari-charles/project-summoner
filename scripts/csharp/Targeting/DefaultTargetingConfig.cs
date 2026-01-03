using ProjectSummoner.Targeting.Filters;
using ProjectSummoner.Targeting.Scorers;
using ProjectSummoner.Targeting.Constraints;
using ProjectSummoner.Units;

namespace ProjectSummoner.Targeting;

/// <summary>
/// Static factory for creating default targeting configuration.
/// Used when no custom TargetingConfig is assigned to a unit.
/// </summary>
public static class DefaultTargetingConfig
{
    private static TargetingConfig? _cached;

    /// <summary>
    /// Get the default targeting configuration.
    /// Creates a standard config: ValidTarget + Layer filter, Distance + Health scorer, Range constraint.
    /// </summary>
    public static TargetingConfig Get()
    {
        if (_cached != null)
            return _cached;

        // Build default filter: ValidTarget + Layer (both ground and air)
        var validFilter = new ValidTargetFilter();
        var layerFilter = new LayerTargetFilter { CanTarget = TargetLayer.Both };
        var compositeFilter = new CompositeTargetFilter();
        compositeFilter.Filters.Add(validFilter);
        compositeFilter.Filters.Add(layerFilter);

        // Build default scorer: Distance + Health
        var distanceScorer = new DistanceScorer { MaxDistance = 20f, Weight = 1f };
        var healthScorer = new HealthScorer { Weight = 10f };
        var compositeScorer = new CompositeScorer();
        compositeScorer.Scorers.Add(distanceScorer);
        compositeScorer.Scorers.Add(healthScorer);

        // Build default constraint: Range only
        var rangeConstraint = new RangeConstraint();

        _cached = new TargetingConfig
        {
            Filter = compositeFilter,
            Scorer = compositeScorer,
            AttackConstraint = rangeConstraint,
            AggroRadius = 20f
        };

        return _cached;
    }
}
