using Godot;
using ProjectSummoner.Cards.Formations;

namespace ProjectSummoner.Cards.Configs;

/// <summary>
/// Configuration for grouped line formations.
/// Units are arranged in a horizontal line with larger gaps between groups.
/// Uses GroupSpacing and UnitsPerGroup from base FormationConfig.
/// </summary>
public partial class GroupedLineFormationConfig : FormationConfig
{
    /// <summary>
    /// Create a GroupedLineFormation with this config's parameters.
    /// </summary>
    public override IFormationStrategy CreateFormation()
    {
        return new GroupedLineFormation
        {
            UnitSpacing = Spacing,
            GroupSpacing = GroupSpacing,
            UnitsPerGroup = UnitsPerGroup
        };
    }
}
