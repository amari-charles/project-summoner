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
    /// Future: Read formation_type from catalog to determine which formation to use.
    /// Currently defaults to GridFormation for all cards.
    /// </summary>
    /// <param name="catalogId">The summon card's catalog ID.</param>
    /// <returns>Formation strategy for positioning units.</returns>
    public static IFormationStrategy GetFormation(string catalogId)
    {
        // TODO: When catalog supports formation_type, switch on it:
        // var formationType = CardCatalog.GetFormationType(catalogId);
        // return formationType switch
        // {
        //     "ring" => new RingFormation(),
        //     "line" => new LineFormation(),
        //     _ => new GridFormation()
        // };

        // Default: All summons use GridFormation (current behavior)
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
