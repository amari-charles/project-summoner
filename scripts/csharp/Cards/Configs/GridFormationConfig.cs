using Godot;
using ProjectSummoner.Cards.Formations;

namespace ProjectSummoner.Cards.Configs;

/// <summary>
/// Configuration for grid formations with optional column override.
/// Units are arranged in a grid pattern with staggered rows.
/// </summary>
public partial class GridFormationConfig : FormationConfig
{
    /// <summary>
    /// Override columns per row (0 = auto-calculate).
    /// When set, forces specific grid dimensions instead of auto-calculating.
    /// </summary>
    [Export]
    public int Columns { get; set; } = 0;

    /// <summary>
    /// Create a GridFormation with this config's parameters.
    /// </summary>
    public override IFormationStrategy CreateFormation()
    {
        return new GridFormation
        {
            Spacing = Spacing,
            RowOffset = RowOffset,
            Columns = Columns
        };
    }
}
