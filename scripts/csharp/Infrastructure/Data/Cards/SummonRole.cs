using System;

namespace Fateforged.Cards;

/// <summary>
/// Combat role for summon cards. Supports multiple roles via flags.
/// Used for deck-building suggestions, AI priority, and filtering.
/// </summary>
[Flags]
public enum SummonRole
{
    None = 0,

    /// <summary>Many weak units that overwhelm with numbers.</summary>
    Swarm = 1 << 0,

    /// <summary>High movement speed for harassment/flanking.</summary>
    Fast = 1 << 1,

    /// <summary>High HP, absorbs damage for team.</summary>
    Tank = 1 << 2,

    /// <summary>Large single powerful unit.</summary>
    Giant = 1 << 3,

    /// <summary>Cannot move, holds position.</summary>
    Stationary = 1 << 4
}
