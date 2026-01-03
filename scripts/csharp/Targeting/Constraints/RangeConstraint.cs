using Godot;
using ProjectSummoner.Units;

namespace ProjectSummoner.Targeting.Constraints;

/// <summary>
/// Constrains attacks by distance. Target must be within attack range.
/// </summary>
[GlobalClass]
public partial class RangeConstraint : BaseAttackConstraint
{
    public override bool IsAttackValid(Unit3D unit, Node3D target)
    {
        float distance = unit.GlobalPosition.DistanceTo(target.GlobalPosition);
        return distance <= unit.AttackRange;
    }

    public override bool CanEverReach(Unit3D unit, Node3D target)
    {
        // Check if altitude difference makes target unreachable
        float targetAlt = GetTargetAltitude(target);
        float myAlt = unit.MovementLayer == (int)MovementLayer.Air ? unit.FlightAltitude : 0f;
        float altDiff = Mathf.Abs(targetAlt - myAlt);

        return altDiff <= unit.AttackRange;
    }

    private static float GetTargetAltitude(Node3D target)
    {
        if (target is Unit3D u && u.MovementLayer == (int)MovementLayer.Air)
            return u.FlightAltitude;
        return target.GlobalPosition.Y;
    }
}
