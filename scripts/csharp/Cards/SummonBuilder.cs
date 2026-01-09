using ProjectSummoner.Cards.Formations;

namespace ProjectSummoner.Cards;

/// <summary>
/// Builder for creating SummonCard formations.
/// Provides helper methods for formation lookup and creation.
/// </summary>
public static class SummonBuilder
{
    /// <summary>
    /// Get the formation for a card by catalog ID.
    /// </summary>
    public static IFormationStrategy GetFormation(string catalogId)
    {
        var card = CardCatalog.GetCard(catalogId);
        return card?.Formation ?? FormationPresets.StandardGrid;
    }

    /// <summary>
    /// Get a GridFormation with custom spacing.
    /// </summary>
    public static IFormationStrategy GetFormation(float spacing, float rowOffset)
    {
        return new GridFormation
        {
            Spacing = spacing,
            RowOffset = rowOffset
        };
    }
}
