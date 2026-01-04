using ProjectSummoner.Cards.Formations;

namespace ProjectSummoner.Cards;

/// <summary>
/// Builder for creating SummonCard formations based on catalog configuration.
/// Similar to SpellBuilder for spell effects.
/// </summary>
public static class SummonBuilder
{
    /// <summary>
    /// Get the formation strategy for a summon card.
    /// Currently defaults to GridFormation for all cards.
    /// When catalog supports formation_type field, this can switch on it to return
    /// RingFormation, LineFormation, etc.
    /// </summary>
    /// <param name="catalogId">The summon card's catalog ID (reserved for future use).</param>
    /// <returns>Formation strategy for positioning units.</returns>
    public static IFormationStrategy GetFormation(string catalogId)
    {
        // All summons currently use GridFormation
        return new GridFormation();
    }

    /// <summary>
    /// Get the formation strategy for a summon card with custom spacing.
    /// </summary>
    /// <param name="catalogId">The summon card's catalog ID.</param>
    /// <param name="spacing">Custom spacing between units.</param>
    /// <param name="rowOffset">Custom row offset for stagger pattern.</param>
    /// <returns>Formation strategy for positioning units.</returns>
    public static IFormationStrategy GetFormation(string catalogId, float spacing, float rowOffset)
    {
        return new GridFormation
        {
            Spacing = spacing,
            RowOffset = rowOffset
        };
    }
}
