using Godot;

namespace ProjectSummoner.Cards.Configs;

/// <summary>
/// Configuration for spawning units from summon cards.
/// Contains data about what scene to load and how many units to spawn.
///
/// Contains data about what scene to load and how many units to spawn.
/// </summary>
public partial class SpawnConfig : Resource
{
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
}
