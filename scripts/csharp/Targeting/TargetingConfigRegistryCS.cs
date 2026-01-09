using Godot;
using ProjectSummoner.Targeting;

namespace ProjectSummoner.Bridges;

/// <summary>
/// Bridge autoload for accessing TargetingConfigRegistry from GDScript.
/// The registry is a static class, so this provides instance methods
/// that GDScript can call via the autoload system.
/// </summary>
public partial class TargetingConfigRegistryCS : Node
{
    public override void _Ready()
    {
        GD.Print("TargetingConfigRegistryCS: Initialized");
    }

    /// <summary>
    /// Get targeting config for a unit ID.
    /// </summary>
    public TargetingConfig GetConfig(string unitId)
    {
        return TargetingConfigRegistry.GetConfig(unitId);
    }
}
