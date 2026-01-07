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
    /// Formation type identifier (e.g., "grid", "grouped_line").
    /// Used for serialization and factory creation.
    /// </summary>
    [Export]
    public string FormationType { get; set; } = "grid";

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
    /// Spacing between groups (used by grouped formations only).
    /// </summary>
    [Export]
    public float GroupSpacing { get; set; } = 4.0f;

    /// <summary>
    /// Number of units per group (used by grouped formations only).
    /// </summary>
    [Export]
    public int UnitsPerGroup { get; set; } = 2;

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
