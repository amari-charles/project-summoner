using Godot;
using System.Collections.Generic;
using System.Linq;
namespace ProjectSummoner.Targeting.Filters;

/// <summary>
/// Abstract base class for target filtering.
/// Filters determine which targets are valid candidates for targeting.
/// </summary>
[GlobalClass]
public abstract partial class BaseTargetFilter : Resource
{
    /// <summary>
    /// Check if a single target passes this filter.
    /// </summary>
    public abstract bool IsValid(Node3D unit, Node3D target);

    /// <summary>
    /// Filter a list of potential targets. Returns only valid targets.
    /// Default implementation uses IsValid on each candidate.
    /// </summary>
    public virtual IEnumerable<Node3D> FilterTargets(Node3D unit, IEnumerable<Node3D> candidates)
    {
        return candidates.Where(t => IsValid(unit, t));
    }
}
