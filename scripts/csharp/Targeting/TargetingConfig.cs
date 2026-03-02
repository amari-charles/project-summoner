using Godot;
using System.Collections.Generic;
using System.Linq;
using ProjectSummoner.Targeting.Filters;
using ProjectSummoner.Targeting.Scorers;
using ProjectSummoner.Targeting.Constraints;
namespace ProjectSummoner.Targeting;

/// <summary>
/// Movement style when attack constraint can't be resolved.
/// </summary>
public enum FallbackMovementStyle
{
    /// <summary>Move directly toward the target (default for melee).</summary>
    MoveToward,
    /// <summary>Strafe/circle around target to maintain distance (default for ranged).</summary>
    Strafe,
    /// <summary>Stay in place and wait.</summary>
    Idle
}

/// <summary>
/// Container resource that holds all targeting configuration for a unit.
/// Assigned via a single [Export] on Node3D.
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

    [ExportGroup("Movement")]

    /// <summary>
    /// How the unit moves when attack constraint can't be immediately resolved.
    /// </summary>
    [Export]
    public FallbackMovementStyle FallbackMovement { get; set; } = FallbackMovementStyle.MoveToward;

    /// <summary>
    /// Acquire the best target from a list of candidates.
    /// Applies filtering and scoring to select the highest priority target.
    /// </summary>
    public Node3D? AcquireTarget(Node3D unit, IEnumerable<Node3D> candidates)
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
    public bool CanAttack(Node3D unit, Node3D target)
    {
        return AttackConstraint?.IsAttackValid(unit, target) ?? true;
    }

    /// <summary>
    /// Attempt to resolve any constraint violations (e.g., turn to face target).
    /// Returns true if resolved immediately, false if still pending.
    /// </summary>
    public bool TryResolveConstraint(Node3D unit, Node3D target)
    {
        return AttackConstraint?.TryResolve(unit, target) ?? true;
    }
}
