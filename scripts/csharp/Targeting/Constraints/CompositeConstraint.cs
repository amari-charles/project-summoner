using Godot;
namespace ProjectSummoner.Targeting.Constraints;

/// <summary>
/// Combines multiple constraints with AND logic.
/// All sub-constraints must pass for attack to be valid.
/// </summary>
[GlobalClass]
public partial class CompositeConstraint : BaseAttackConstraint
{
    [Export]
    public Godot.Collections.Array<BaseAttackConstraint> Constraints { get; set; } = new();

    public override bool IsAttackValid(Node3D unit, Node3D target)
    {
        foreach (var constraint in Constraints)
        {
            if (constraint != null && !constraint.IsAttackValid(unit, target))
                return false;
        }
        return true;
    }

    public override bool TryResolve(Node3D unit, Node3D target)
    {
        // Try to resolve all constraints
        foreach (var constraint in Constraints)
        {
            if (constraint != null)
            {
                constraint.TryResolve(unit, target);
            }
        }

        // Check if all are now valid
        return IsAttackValid(unit, target);
    }

    public override bool CanEverReach(Node3D unit, Node3D target)
    {
        foreach (var constraint in Constraints)
        {
            if (constraint != null && !constraint.CanEverReach(unit, target))
                return false;
        }
        return true;
    }

    public override AttackVisualizationData? GetVisualizationData(Node3D unit)
    {
        // Return first constraint with visualization data
        foreach (var constraint in Constraints)
        {
            if (constraint == null) continue;
            var data = constraint.GetVisualizationData(unit);
            if (data != null) return data;
        }
        return null;
    }
}
