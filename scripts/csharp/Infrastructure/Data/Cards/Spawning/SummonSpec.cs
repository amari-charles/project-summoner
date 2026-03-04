using System.Collections.Generic;

namespace Fateforged.Cards.Spawning;

/// <summary>
/// Specification for what units a summon card spawns.
/// Separates spawning logic from card metadata, enabling data-driven multi-unit cards.
///
/// Example: Mama Duck spawns 1 mama + 3 ducklings without hardcoded special cases:
/// <code>
/// Summon = new SummonSpec
/// {
///     SummonTime = 1.5f,
///     Cooldown = 3.0f,
///     Units = [
///         new() { UnitId = UnitIds.MamaDuck, Count = 1 },
///         new() {
///             UnitId = UnitIds.Duckling,
///             Count = 3,
///             Placement = SpawnPlacement.BehindLeader,
///             FollowsIndex = 0  // Ducklings follow mama's targeting
///         }
///     ]
/// }
/// </code>
/// </summary>
public class SummonSpec
{
    /// <summary>
    /// List of unit entries to spawn.
    /// Entries are spawned in order - earlier entries are spawned first.
    /// FollowsIndex references use this ordering (0 = first entry).
    /// </summary>
    public required List<UnitSpawnEntry> Units { get; init; }

    /// <summary>
    /// Time in seconds for summon animation before units appear.
    /// This is how long the spawn VFX plays before units become active.
    /// </summary>
    public float SummonTime { get; init; } = 1.0f;

    /// <summary>
    /// Cooldown in seconds before card can be played again.
    /// </summary>
    public float Cooldown { get; init; } = 2.0f;

    /// <summary>
    /// Total number of units spawned across all entries.
    /// Used for UI display and validation.
    /// </summary>
    public int TotalUnitCount
    {
        get
        {
            var count = 0;
            foreach (var entry in Units)
                count += entry.Count;
            return count;
        }
    }
}
