using Fateforged.Cards.Formations;
using Fateforged.Constants;
using Fateforged.Stats;

namespace Fateforged.Cards.Spawning;

/// <summary>
/// Defines a single unit type to spawn as part of a SummonSpec.
/// Each entry specifies what unit to spawn, how many, and how to position/configure them.
/// </summary>
public class UnitSpawnEntry
{
    /// <summary>
    /// The unit definition ID to spawn.
    /// References a unit in UnitDefinitions.
    /// </summary>
    public required UnitId UnitId { get; init; }

    /// <summary>
    /// Number of units of this type to spawn.
    /// Default is 1.
    /// </summary>
    public int Count { get; init; } = 1;

    /// <summary>
    /// Optional formation strategy override for this entry.
    /// If null, uses Formation placement positioning.
    /// Only used when Placement is SpawnPlacement.Formation.
    /// </summary>
    public IFormationStrategy? Formation { get; init; }

    /// <summary>
    /// Optional stat modifier to apply to spawned units.
    /// Used for variant cards like swarms that spawn weaker units.
    /// Example: Fire Wisp Swarm applies {max_hp: 0.75, attack_damage: 0.75}.
    /// </summary>
    public StatModifier? Modifier { get; init; }

    /// <summary>
    /// Index of the spawn entry that this entry's units should follow for targeting.
    /// Used for companion units (e.g., ducklings follow mama's targeting).
    /// Null means units use their default targeting behavior.
    /// </summary>
    public int? FollowsIndex { get; init; }

    /// <summary>
    /// How to position units from this entry.
    /// Default is Formation (use formation strategy).
    /// </summary>
    public SpawnPlacement Placement { get; init; } = SpawnPlacement.Formation;

    /// <summary>
    /// Offset distance for BehindLeader/AroundLeader placements.
    /// Distance between units and their leader.
    /// Default is 1.5 units.
    /// </summary>
    public float PlacementOffset { get; init; } = 1.5f;
}
