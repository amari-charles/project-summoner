using System.Collections.Generic;
using Godot;
using ProjectSummoner.Capabilities;
using ProjectSummoner.Cards.Effects.Core;
using ProjectSummoner.Systems;
using ProjectSummoner.Units;

namespace ProjectSummoner.Cards.Effects.Targeting;

/// <summary>
/// Targeting strategy that selects the single nearest enemy unit.
/// Used for auto-targeting spells like Mana Bolt.
/// </summary>
public class NearestEnemyTargeting : ITargetingStrategy
{
    /// <summary>
    /// Maximum search radius for finding enemies.
    /// </summary>
    public float SearchRadius { get; set; } = 100f;

    public NearestEnemyTargeting(float searchRadius = 100f)
    {
        SearchRadius = searchRadius;
    }

    public NearestEnemyTargeting() { }

    /// <summary>
    /// Select the single nearest enemy unit.
    /// Returns empty if no enemies found.
    /// </summary>
    public IEnumerable<Node3D> SelectTargets(SpellContext context)
    {
        if (SpatialGrid.Instance == null)
        {
            GD.PrintErr("[NearestEnemyTargeting] SpatialGrid.Instance is null");
            return System.Array.Empty<Node3D>();
        }

        // Get enemy team (opposite of caster's team)
        int enemyTeam = context.Team == Team.Player ? (int)Team.Enemy : (int)Team.Player;

        // Query all enemy units in large radius
        var enemies = SpatialGrid.Instance.GetUnitsInRadius(
            context.Position,
            SearchRadius,
            teamFilter: enemyTeam,
            excludeUnit: null
        );

        // Find the nearest one
        Node3D? nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            // Skip dead units
            if (enemy is IDamageable damageable && !damageable.IsAlive)
                continue;

            var dist = context.Position.DistanceTo(enemy.GlobalPosition);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = enemy;
            }
        }

        if (nearest != null)
        {
            return new[] { nearest };
        }

        return System.Array.Empty<Node3D>();
    }
}
