using Godot;
using System.Collections.Generic;
using System.Linq;
using ProjectSummoner.Targeting.Filters;
using ProjectSummoner.Targeting.Scorers;
using ProjectSummoner.Targeting.Constraints;
using ProjectSummoner.Units;

namespace ProjectSummoner.Targeting;

/// <summary>
/// Container resource that holds all targeting configuration for a unit.
/// Assigned via a single [Export] on Unit3D.
/// </summary>
[GlobalClass]
public partial class TargetingConfig : Resource
{
    [ExportGroup("Targeting Pipeline")]

    /// <summary>
    /// Filter to determine valid targets. Can be a CompositeTargetFilter for multiple filters.
    /// </summary>
    [Export]
    public BaseTargetFilter? Filter { get; set; }

    /// <summary>
    /// Scorer to prioritize targets. Can be a CompositeScorer for multiple scoring factors.
    /// </summary>
    [Export]
    public BaseTargetScorer? Scorer { get; set; }

    /// <summary>
    /// Constraint to validate attacks. Can be a CompositeConstraint for multiple constraints.
    /// </summary>
    [Export]
    public BaseAttackConstraint? AttackConstraint { get; set; }

    [ExportGroup("Range Settings")]

    /// <summary>
    /// Radius to search for potential targets.
    /// </summary>
    [Export]
    public float AggroRadius { get; set; } = 20f;

    /// <summary>
    /// Acquire the best target from a list of candidates.
    /// Applies filtering and scoring to select the highest priority target.
    /// </summary>
    public Node3D? AcquireTarget(Unit3D unit, IEnumerable<Node3D> candidates)
    {
        // Apply reachability filter from constraint
        var reachable = candidates;
        if (AttackConstraint != null)
        {
            reachable = candidates.Where(c => AttackConstraint.CanEverReach(unit, c));
        }

        // Apply target filter
        var filtered = Filter?.FilterTargets(unit, reachable) ?? reachable;

        // Score and select best
        return Scorer?.GetBestTarget(unit, filtered) ?? filtered.FirstOrDefault();
    }

    /// <summary>
    /// Check if the unit can attack the target right now.
    /// </summary>
    public bool CanAttack(Unit3D unit, Node3D target)
    {
        return AttackConstraint?.IsAttackValid(unit, target) ?? true;
    }

    /// <summary>
    /// Attempt to resolve any constraint violations (e.g., turn to face target).
    /// Returns true if resolved immediately, false if still pending.
    /// </summary>
    public bool TryResolveConstraint(Unit3D unit, Node3D target)
    {
        return AttackConstraint?.TryResolve(unit, target) ?? true;
    }
}
