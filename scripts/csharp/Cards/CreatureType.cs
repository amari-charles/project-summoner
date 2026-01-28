using System;

namespace ProjectSummoner.Cards;

/// <summary>
/// Creature classification for units. Supports multiple types via flags.
/// Used for synergies, abilities that target creature types, and filtering.
/// </summary>
[Flags]
public enum CreatureType
{
    None = 0,

    /// <summary>Elemental beings (wisps, sprites, elementals).</summary>
    Elemental = 1 << 0,

    /// <summary>Spirit creatures (ghosts, souls, titans).</summary>
    Spirit = 1 << 1,

    /// <summary>Insect creatures (ants, beetles, etc.).</summary>
    Insect = 1 << 2,

    /// <summary>Amphibian creatures (frogs, salamanders).</summary>
    Amphibian = 1 << 3,

    /// <summary>Nature-aligned creatures (plants, treants).</summary>
    Nature = 1 << 4,

    /// <summary>Aerial creatures (clouds, birds).</summary>
    Aerial = 1 << 5,

    /// <summary>Beast creatures (wolves, bears).</summary>
    Beast = 1 << 6,

    /// <summary>Undead creatures (skeletons, zombies).</summary>
    Undead = 1 << 7,

    /// <summary>Construct creatures (golems, machines).</summary>
    Construct = 1 << 8,

    /// <summary>Humanoid creatures (soldiers, mages).</summary>
    Humanoid = 1 << 9
}
