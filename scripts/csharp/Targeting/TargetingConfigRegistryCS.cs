using Godot;
using ProjectSummoner.Constants;
using ProjectSummoner.Targeting;
using ProjectSummoner.Units;

namespace ProjectSummoner.Bridges;

/// <summary>
/// Bridge autoload for accessing targeting configs from GDScript.
/// Builds configs from UnitDefinitions on demand.
/// </summary>
public partial class TargetingConfigRegistryCS : Node
{
    public override void _Ready()
    {
        GD.Print("TargetingConfigRegistryCS: Initialized");
    }

    /// <summary>
    /// Get targeting config for a unit ID.
    /// Builds config from UnitDefinitions targeting behavior.
    /// </summary>
    public TargetingConfig GetConfig(string unitId)
    {
        if (UnitDefinitions.TryGet(new UnitId(unitId), out var def) && def != null)
        {
            return def.Targeting.BuildConfig();
        }
        // Return default melee targeting if unit not found
        return MeleeTargeting.Default.BuildConfig();
    }
}
