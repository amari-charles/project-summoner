using Godot;
using System.Collections.Generic;
using System.Linq;
namespace ProjectSummoner.Targeting.Filters;

/// <summary>
/// Combines multiple filters with AND logic.
/// Target must pass ALL sub-filters to be valid.
/// </summary>
[GlobalClass]
public partial class CompositeTargetFilter : BaseTargetFilter
{
    [Export]
    public Godot.Collections.Array<BaseTargetFilter> Filters { get; set; } = new();

    public override bool IsValid(Node3D unit, Node3D target)
    {
        foreach (var filter in Filters)
        {
            if (filter != null && !filter.IsValid(unit, target))
                return false;
        }
        return true;
    }

    public override IEnumerable<Node3D> FilterTargets(Node3D unit, IEnumerable<Node3D> candidates)
    {
        var result = candidates;
        foreach (var filter in Filters)
        {
            if (filter != null)
            {
                result = filter.FilterTargets(unit, result);
            }
        }
        return result;
    }
}
