using Godot;
using ProjectSummoner.Units;

namespace ProjectSummoner.Targeting.Constraints;

/// <summary>
/// Constrains attacks by distance. Target must be within attack range.
/// </summary>
[GlobalClass]
public partial class RangeConstraint : BaseAttackConstraint
{
    public override bool IsAttackValid(Node3D unit, Node3D target)
    {
        float distance = unit.GlobalPosition.DistanceTo(target.GlobalPosition);
        float attackRange = unit.Get("AttackRange").AsSingle();
        return distance <= attackRange;
    }

    public override bool CanEverReach(Node3D unit, Node3D target)
    {
        // Check if altitude difference makes target unreachable
        float targetAlt = GetTargetAltitude(target);
        int movementLayer = unit.Get("MovementLayer").AsInt32();
        float myAlt = movementLayer == (int)MovementLayer.Air ? unit.Get("FlightAltitude").AsSingle() : 0f;
        float altDiff = Mathf.Abs(targetAlt - myAlt);

        float attackRange = unit.Get("AttackRange").AsSingle();
        return altDiff <= attackRange;
    }

    private static float GetTargetAltitude(Node3D target)
    {
        var layerVar = target.Get("MovementLayer");
        if (layerVar.VariantType != Variant.Type.Nil && layerVar.AsInt32() == (int)MovementLayer.Air)
        {
            var altVar = target.Get("FlightAltitude");
            if (altVar.VariantType != Variant.Type.Nil)
                return altVar.AsSingle();
        }
        return target.GlobalPosition.Y;
    }
}
