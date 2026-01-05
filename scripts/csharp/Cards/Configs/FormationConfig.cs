using Godot;
using ProjectSummoner.Cards.Formations;

namespace ProjectSummoner.Cards.Configs;

/// <summary>
/// Base configuration for unit formations.
/// Subclasses define specific formation types and create their own strategy.
/// </summary>
public partial class FormationConfig : Resource
{
    /// <summary>
    /// Spacing between units (interpretation varies by formation type).
    /// </summary>
    [Export]
    public float Spacing { get; set; } = GridFormation.DefaultSpacing;

    /// <summary>
    /// Row offset for grid formations (ignored by other types).
    /// </summary>
    [Export]
    public float RowOffset { get; set; } = GridFormation.DefaultRowOffset;

    /// <summary>
    /// Create the formation strategy from this config.
    /// Override in subclasses for specific formation types.
    /// </summary>
    public virtual IFormationStrategy CreateFormation()
    {
        return new GridFormation
        {
            Spacing = Spacing,
            RowOffset = RowOffset
        };
    }
}
