using System.Collections.Generic;
using Godot;
using ProjectSummoner.Cards.Effects.Core;
using ProjectSummoner.Systems;

namespace ProjectSummoner.Cards.Effects.Targeting;

/// <summary>
/// Circular area targeting strategy.
/// Selects all units within a radius of the spell's target position.
/// Most common targeting type (Fireball, Rally, Guard, etc.).
/// </summary>
public class CircleTargeting : ITargetingStrategy
{
    /// <summary>
    /// Radius of the targeting circle in world units.
    /// </summary>
    public float Radius { get; set; }

    /// <summary>
    /// Create a circle targeting strategy.
    /// </summary>
    /// <param name="radius">Radius in world units.</param>
    public CircleTargeting(float radius = 10f)
    {
        Radius = radius;
    }

    /// <summary>
    /// Default constructor for property initialization.
    /// </summary>
    public CircleTargeting() { }

    /// <summary>
    /// Select all units within the radius of the spell position.
    /// Uses SpatialGrid for efficient querying.
    /// </summary>
    public IEnumerable<Node3D> SelectTargets(SpellContext context)
    {
        if (SpatialGrid.Instance == null)
        {
            GD.PrintErr("[CircleTargeting] SpatialGrid.Instance is null");
            return System.Array.Empty<Node3D>();
        }

        // Query all units in radius (team filtering handled by SpellEffect)
        var units = SpatialGrid.Instance.GetUnitsInRadius(
            context.Position,
            Radius,
            teamFilter: -1, // All teams - affinity filtering happens in SpellEffect
            excludeUnit: null
        );

        return units;
    }
}
