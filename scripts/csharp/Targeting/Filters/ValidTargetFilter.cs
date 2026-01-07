using Godot;
using ProjectSummoner.Capabilities;
using ProjectSummoner.Units;

namespace ProjectSummoner.Targeting.Filters;

/// <summary>
/// Filters out invalid targets (dead, destroyed, etc.).
/// </summary>
[GlobalClass]
public partial class ValidTargetFilter : BaseTargetFilter
{
    public override bool IsValid(Unit3D unit, Node3D target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return false;

        // C# units implement IDamageable
        if (target is IDamageable damageable && !damageable.IsAlive)
            return false;

        // GDScript interop (Summoner, etc.)
        var isAliveVar = target.Get("is_alive");
        if (isAliveVar.VariantType != Variant.Type.Nil && !isAliveVar.AsBool())
            return false;

        return true;
    }
}
