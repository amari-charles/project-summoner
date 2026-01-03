using Godot;
using ProjectSummoner.Units;

namespace ProjectSummoner.Targeting.Filters;

/// <summary>
/// Filters targets by movement layer (Ground, Air, or Both).
/// </summary>
[GlobalClass]
public partial class LayerTargetFilter : BaseTargetFilter
{
    [Export]
    public TargetLayer CanTarget { get; set; } = TargetLayer.Both;

    public override bool IsValid(Unit3D unit, Node3D target)
    {
        int targetLayer = GetMovementLayer(target);

        return CanTarget switch
        {
            TargetLayer.GroundOnly => targetLayer == (int)MovementLayer.Ground,
            TargetLayer.AirOnly => targetLayer == (int)MovementLayer.Air,
            TargetLayer.Both => true,
            _ => true
        };
    }

    private static int GetMovementLayer(Node3D target)
    {
        if (target is Unit3D u)
            return u.MovementLayer;

        // Default to ground for non-unit targets
        return (int)MovementLayer.Ground;
    }
}
