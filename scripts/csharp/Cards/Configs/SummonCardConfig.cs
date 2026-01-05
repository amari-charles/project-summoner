using Godot;

namespace ProjectSummoner.Cards.Configs;

/// <summary>
/// Configuration for summon cards.
/// Contains spawn count, unit scene, and formation configuration.
/// </summary>
public partial class SummonCardConfig : CardConfig
{
    // =========================================================================
    // SPAWN PROPERTIES
    // =========================================================================

    /// <summary>
    /// Path to the unit scene to instantiate.
    /// </summary>
    [Export]
    public string UnitScenePath { get; set; } = "";

    /// <summary>
    /// Number of units to spawn.
    /// </summary>
    [Export]
    public int SpawnCount { get; set; } = 1;

    /// <summary>
    /// Time in seconds for the summon animation.
    /// </summary>
    [Export]
    public float SummonTime { get; set; } = 1.0f;

    // =========================================================================
    // FORMATION
    // =========================================================================

    /// <summary>
    /// Formation configuration for positioning spawned units.
    /// </summary>
    [Export]
    public FormationConfig Formation { get; set; } = new FormationConfig();
}
