using System.Collections.Generic;
using ProjectSummoner.Constants;
using ProjectSummoner.Targeting.Filters;
using ProjectSummoner.Targeting.Scorers;
using ProjectSummoner.Targeting.Constraints;
using ProjectSummoner.Units;

namespace ProjectSummoner.Targeting;

/// <summary>
/// Registry for unit-specific targeting configurations.
/// Builds configs programmatically to avoid .tres resource loading issues.
/// </summary>
public static class TargetingConfigRegistry
{
    private static readonly Dictionary<UnitId, TargetingConfig> _configs = new();
    private static bool _initialized = false;

    /// <summary>
    /// Get targeting config for a specific unit type (type-safe).
    /// Returns default config if no specific config is registered.
    /// </summary>
    public static TargetingConfig GetConfig(UnitId unitId)
    {
        EnsureInitialized();

        if (_configs.TryGetValue(unitId, out var config))
            return config;

        return DefaultTargetingConfig.Get();
    }

    /// <summary>
    /// Get targeting config for a specific unit type (string overload for backwards compatibility).
    /// Returns default config if no specific config is registered.
    /// </summary>
    public static TargetingConfig GetConfig(string unitId)
    {
        return GetConfig(new UnitId(unitId));
    }

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        // Ranged units
        RegisterPuffConfig();
        RegisterRangedConfig(UnitIds.FireSpider);
        RegisterRangedConfig(UnitIds.EarthRockThrower);
        RegisterDucklingConfig();

        // Melee units (can only target ground units)
        RegisterMeleeConfig(UnitIds.FireWisp);
        RegisterMeleeConfig(UnitIds.WaterWisp);
        RegisterMeleeConfig(UnitIds.WindWisp);
        RegisterMeleeConfig(UnitIds.EarthWisp);
        RegisterMeleeConfig(UnitIds.LightningWisp);
        RegisterMeleeConfig(UnitIds.LifeWisp);
        RegisterMeleeConfig(UnitIds.DeathWisp);
        RegisterMeleeConfig(UnitIds.ShadowWisp);
        RegisterMeleeConfig(UnitIds.FireTitan);
        RegisterMeleeConfig(UnitIds.FireAnt);
        RegisterMeleeConfig(UnitIds.FireBoar);
        RegisterMeleeConfig(UnitIds.EarthSprite);
        RegisterMeleeConfig(UnitIds.StoneApe);
        RegisterMeleeConfig(UnitIds.WaterFrog);
        RegisterMeleeConfig(UnitIds.MamaDuck);

        // Special units
        RegisterRockConfig();
    }

    /// <summary>
    /// Puff: Flying ranged unit with 3D cone constraint.
    /// Must face target within ±30° in 3D space to attack.
    /// Cannot shoot at steep angles (e.g., straight down).
    /// </summary>
    private static void RegisterPuffConfig()
    {
        // Filter: Valid targets only (can target both ground and air)
        var validFilter = new ValidTargetFilter();

        // Scorer: Prefer close targets, bonus for targets below
        var distanceScorer = new DistanceScorer { MaxDistance = 24f, Weight = 1f };
        var belowScorer = new BelowTargetScorer { Radius = 6f, Weight = 5f };
        var compositeScorer = new CompositeScorer();
        compositeScorer.Scorers.Add(distanceScorer);
        compositeScorer.Scorers.Add(belowScorer);

        // Constraints: Range + 3D cone (must face target in 3D space)
        // Using 3D cone means targets directly below are outside the cone
        var rangeConstraint = new RangeConstraint();
        var coneConstraint = new ConeConstraint3D
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

        _configs[UnitIds.Puff] = config;
    }

    /// <summary>
    /// Standard melee unit config: can only target ground units.
    /// Uses default distance + health scoring.
    /// </summary>
    private static void RegisterMeleeConfig(UnitId unitId)
    {
        // Filter: Valid targets + Ground only (cannot target flying units)
        var validFilter = new ValidTargetFilter();
        var layerFilter = new LayerTargetFilter { CanTarget = TargetLayer.GroundOnly };
        var compositeFilter = new CompositeTargetFilter();
        compositeFilter.Filters.Add(validFilter);
        compositeFilter.Filters.Add(layerFilter);

        // Scorer: Prefer close targets, slightly prefer low health
        var distanceScorer = new DistanceScorer { MaxDistance = 20f, Weight = 1f };
        var healthScorer = new HealthScorer { Weight = 10f };
        var compositeScorer = new CompositeScorer();
        compositeScorer.Scorers.Add(distanceScorer);
        compositeScorer.Scorers.Add(healthScorer);

        // Constraints: Range only
        var rangeConstraint = new RangeConstraint();

        var config = new TargetingConfig
        {
            Filter = compositeFilter,
            Scorer = compositeScorer,
            AttackConstraint = rangeConstraint,
            AggroRadius = 20f,
            FallbackMovement = FallbackMovementStyle.MoveToward  // Melee: move toward target
        };

        _configs[unitId] = config;
    }

    /// <summary>
    /// Standard ranged unit config: can target both ground and air.
    /// Uses distance scoring and strafing fallback movement.
    /// </summary>
    private static void RegisterRangedConfig(UnitId unitId)
    {
        // Filter: Valid targets only (can target both ground and air)
        var validFilter = new ValidTargetFilter();

        // Scorer: Prefer close targets
        var distanceScorer = new DistanceScorer { MaxDistance = 20f, Weight = 1f };
        var compositeScorer = new CompositeScorer();
        compositeScorer.Scorers.Add(distanceScorer);

        // Constraints: Range only
        var rangeConstraint = new RangeConstraint();

        var config = new TargetingConfig
        {
            Filter = validFilter,
            Scorer = compositeScorer,
            AttackConstraint = rangeConstraint,
            AggroRadius = 20f,
            FallbackMovement = FallbackMovementStyle.Strafe  // Ranged: circle around target
        };

        _configs[unitId] = config;
    }

    /// <summary>
    /// Duckling: Ranged unit that follows mama's target.
    /// Can target both ground and air (since mama could attack either).
    /// Uses shorter aggro radius since it relies on mama for targeting.
    /// </summary>
    private static void RegisterDucklingConfig()
    {
        // Filter: Valid targets only (can target both ground and air to match mama)
        var validFilter = new ValidTargetFilter();

        // Scorer: Prefer close targets, slightly prefer mama's target (handled in AcquireTarget override)
        var distanceScorer = new DistanceScorer { MaxDistance = 16f, Weight = 1f };
        var compositeScorer = new CompositeScorer();
        compositeScorer.Scorers.Add(distanceScorer);

        // Constraints: Range only
        var rangeConstraint = new RangeConstraint();

        var config = new TargetingConfig
        {
            Filter = validFilter,
            Scorer = compositeScorer,
            AttackConstraint = rangeConstraint,
            AggroRadius = 16f,  // Shorter range - follows mama
            FallbackMovement = FallbackMovementStyle.Strafe
        };

        _configs[UnitIds.Duckling] = config;
    }

    /// <summary>
    /// Rock: Stationary target dummy that doesn't move or attack.
    /// Uses Idle fallback since it can't move.
    /// </summary>
    private static void RegisterRockConfig()
    {
        var validFilter = new ValidTargetFilter();
        var distanceScorer = new DistanceScorer { MaxDistance = 0f, Weight = 1f };  // No aggro
        var compositeScorer = new CompositeScorer();
        compositeScorer.Scorers.Add(distanceScorer);
        var rangeConstraint = new RangeConstraint();

        var config = new TargetingConfig
        {
            Filter = validFilter,
            Scorer = compositeScorer,
            AttackConstraint = rangeConstraint,
            AggroRadius = 0f,  // No aggro - stationary dummy
            FallbackMovement = FallbackMovementStyle.Idle  // Can't move
        };

        _configs[UnitIds.Rock] = config;
    }
}
