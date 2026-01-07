using ProjectSummoner.Cards.Configs;
using ProjectSummoner.Cards.Formations;

namespace ProjectSummoner.Cards;

/// <summary>
/// Builder for creating SummonCard formations based on catalog configuration.
/// Similar to SpellBuilder for spell effects.
/// </summary>
public static class SummonBuilder
{
    /// <summary>
    /// Get the formation strategy from a FormationConfig.
    /// Uses polymorphism - each config subclass knows how to create its formation.
    /// </summary>
    public static IFormationStrategy GetFormation(FormationConfig config)
    {
        return config.CreateFormation();
    }

    /// <summary>
    /// Get a default GridFormation with custom spacing (backwards compat).
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
