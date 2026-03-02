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

    public override bool IsValid(Node3D unit, Node3D target)
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
        var layerVar = target.Get("MovementLayer");
        if (layerVar.VariantType != Variant.Type.Nil)
            return layerVar.AsInt32();

        // Default to ground for nodes without MovementLayer
        return (int)MovementLayer.Ground;
    }
}
