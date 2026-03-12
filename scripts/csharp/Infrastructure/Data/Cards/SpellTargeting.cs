namespace Fateforged.Cards;

/// <summary>
/// Targeting mode for spell cards.
/// Determines how players aim and what gets affected.
/// </summary>
public enum SpellTargeting
{
    /// <summary>Not specified.</summary>
    None,

    /// <summary>Affects a single target.</summary>
    SingleTarget,

    /// <summary>Affects an area (radius around point).</summary>
    AreaOfEffect,

    /// <summary>Affects units in a selection radius.</summary>
    SelectionRadius,
}
