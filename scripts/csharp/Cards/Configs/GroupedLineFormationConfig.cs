using Godot;
using ProjectSummoner.Cards.Formations;

namespace ProjectSummoner.Cards.Configs;

/// <summary>
/// Configuration for grouped line formations.
/// Units are arranged in a horizontal line with larger gaps between groups.
/// </summary>
public partial class GroupedLineFormationConfig : FormationConfig
{
    /// <summary>
    /// Spacing between groups (larger gap than unit spacing).
    /// </summary>
    [Export]
    public float GroupSpacing { get; set; } = 4.0f;

    /// <summary>
    /// Number of units per group.
    /// </summary>
    [Export]
    public int UnitsPerGroup { get; set; } = 2;

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
